# ML Model for Oversteer Control

## Objective

Train a machine learning model to predict optimal driving inputs (throttle, brake, steering) that maintain the car in a **slight oversteer state** while cornering for maximum speed and control.

## Problem Definition

### Target State

**Slight Oversteer** characteristics:
- Rear slip angle > Front slip angle by 1-3°
- Balance: -1° to -3° (negative = oversteer)
- Benefits: Rotation, agility, faster corner exit
- Risk: Too much oversteer → spin

### Inputs to Predict

| Output | Range | Description |
|--------|-------|-------------|
| Throttle | [0, 1] | Accelerator pedal position |
| Brake | [0, 1] | Brake pedal position |
| Steering | [-1, 1] | Steering wheel angle (normalized) |

### State Features (Model Inputs)

**Vehicle State:**
- Speed, lateral/longitudinal acceleration
- Current slip angles (front, rear)
- Current balance
- Yaw rate
- Throttle/brake/steering (current values)

**Track Context:**
- Lap distance percentage
- Corner radius (derived from track curvature)
- Distance to next turn
- Track surface temperature

**Temporal Features:**
- Recent history (last 0.5s of states)
- Rate of change (slip angle derivatives)

## Approaches

### Approach 1: Supervised Learning (Imitation Learning)

Learn from expert laps that exhibit optimal oversteer.

**Pros:**
- Simpler to implement
- Fast training
- Learns from human expertise

**Cons:**
- Limited to driver skill level
- May not generalize to new tracks
- No exploration of better strategies

**Method:**
1. Collect telemetry from fast laps
2. Label samples where balance is in target range (-1° to -3°)
3. Train model to predict inputs given state
4. Use regression (predict continuous throttle/brake/steering values)

---

### Approach 2: Reinforcement Learning

Learn optimal policy through trial and error with reward function.

**Pros:**
- Can discover strategies better than human
- Generalizes better
- Continuous improvement

**Cons:**
- Requires simulation or many practice laps
- Longer training time
- More complex implementation

**Method:**
1. Define reward function (reward maintaining target oversteer)
2. Use PPO, SAC, or TD3 algorithm
3. Train in simulation or with safe exploration
4. Fine-tune on real telemetry

---

### Approach 3: Hybrid (Recommended)

Combine both approaches:

1. **Bootstrap with imitation**: Pre-train on expert data
2. **Refine with RL**: Fine-tune to optimize performance
3. **Safe exploration**: Use imitation as baseline, RL explores nearby

## Implementation Strategy

### Phase 1: Data Collection & Labeling

```python
def label_oversteer_quality(df):
    """
    Label samples by oversteer quality

    Returns:
        quality: 0 (bad) to 1 (optimal)
    """
    # Target: slight oversteer (-1° to -3° balance)
    target_balance = -2.0  # Ideal balance
    tolerance = 1.0  # ±1° tolerance

    # Calculate quality score
    balance_error = np.abs(df['balance'] - target_balance)
    quality = np.clip(1 - balance_error / tolerance, 0, 1)

    # Penalize if too slow or too unstable
    speed_penalty = df['Speed'] / df['Speed'].quantile(0.9)
    stability_penalty = 1 / (1 + df['YawRate'].abs())

    quality = quality * speed_penalty * stability_penalty

    return quality
```

### Phase 2: Feature Engineering

```python
def engineer_features(df):
    """
    Create features for oversteer control model
    """
    # Current state
    features = df[[
        'Speed', 'RPM', 'Gear',
        'slip_angle_front_deg', 'slip_angle_rear_deg', 'balance',
        'YawRate', 'LatAccel', 'LongAccel',
        'LapDistPct',
        'Throttle', 'Brake', 'SteeringWheelAngle'
    ]].copy()

    # Historical features (last 0.5s = 30 samples @ 60Hz)
    window = 30
    features['slip_angle_front_ma'] = df['slip_angle_front_deg'].rolling(window).mean()
    features['slip_angle_rear_ma'] = df['slip_angle_rear_deg'].rolling(window).mean()
    features['balance_ma'] = df['balance'].rolling(window).mean()

    # Rate of change
    features['balance_rate'] = df['balance'].diff() / df['SessionTime'].diff()
    features['yaw_accel'] = df['YawRate'].diff() / df['SessionTime'].diff()

    # Track curvature (estimated from steering and speed)
    features['curvature'] = df['SteeringWheelAngle'] / (df['Speed'] + 0.1)

    # Corner phase detection
    features['in_corner'] = (features['curvature'].abs() > 0.01).astype(int)

    # Tire state
    tire_temp_cols = [col for col in df.columns if 'temp' in col.lower()]
    if tire_temp_cols:
        features['avg_tire_temp'] = df[tire_temp_cols].mean(axis=1)

    return features
```

### Phase 3: Supervised Learning Model

```python
import numpy as np
import pandas as pd
from sklearn.ensemble import RandomForestRegressor
from sklearn.multioutput import MultiOutputRegressor
import xgboost as xgb


class OversteerController:
    def __init__(self):
        self.model = None
        self.feature_cols = None
        self.target_balance = -2.0  # Target oversteer (degrees)

    def prepare_training_data(self, df):
        """
        Prepare training data from telemetry

        Only use samples that are:
        1. In corners (curvature > threshold)
        2. At speed (> 20 m/s)
        3. High quality (good oversteer management)
        """
        # Label quality
        df['quality'] = label_oversteer_quality(df)

        # Filter to high-quality cornering samples
        mask = (
            (df['in_corner'] == 1) &
            (df['Speed'] > 20) &
            (df['quality'] > 0.6)  # Only learn from good examples
        )

        df_train = df[mask].copy()

        # Features: current state (exclude future/target variables)
        feature_cols = [
            'Speed', 'RPM', 'Gear',
            'slip_angle_front_deg', 'slip_angle_rear_deg', 'balance',
            'YawRate', 'LatAccel', 'LongAccel',
            'LapDistPct', 'curvature',
            'slip_angle_front_ma', 'balance_ma',
            'balance_rate', 'yaw_accel',
            'avg_tire_temp'
        ]

        # Targets: next inputs (what driver will do)
        target_cols = ['Throttle', 'Brake', 'SteeringWheelAngle']

        X = df_train[feature_cols]
        y = df_train[target_cols]

        # Store feature columns for inference
        self.feature_cols = feature_cols

        return X, y, df_train['quality'].values

    def train(self, X, y, sample_weights=None):
        """
        Train multi-output regression model
        """
        print("Training oversteer control model...")

        # Use XGBoost for each output
        models = []
        for i, target in enumerate(['Throttle', 'Brake', 'Steering']):
            print(f"  Training {target} model...")

            model = xgb.XGBRegressor(
                n_estimators=200,
                max_depth=6,
                learning_rate=0.05,
                subsample=0.8,
                colsample_bytree=0.8
            )

            model.fit(
                X, y.iloc[:, i],
                sample_weight=sample_weights,
                verbose=False
            )

            models.append(model)

        self.model = models
        print("✓ Training complete")

    def predict(self, state):
        """
        Predict optimal inputs given current state

        Args:
            state: dict or DataFrame with current state features

        Returns:
            dict: {'throttle': float, 'brake': float, 'steering': float}
        """
        if isinstance(state, dict):
            state = pd.DataFrame([state])

        # Extract features
        X = state[self.feature_cols]

        # Predict each output
        throttle = self.model[0].predict(X)[0]
        brake = self.model[1].predict(X)[0]
        steering = self.model[2].predict(X)[0]

        # Ensure valid ranges
        throttle = np.clip(throttle, 0, 1)
        brake = np.clip(brake, 0, 1)
        steering = np.clip(steering, -1, 1)

        # Safety: can't have both throttle and brake
        if throttle > 0.1 and brake > 0.1:
            # Prefer brake for safety
            throttle = 0

        return {
            'throttle': throttle,
            'brake': brake,
            'steering': steering
        }

    def evaluate(self, X_test, y_test):
        """
        Evaluate model on test set
        """
        from sklearn.metrics import mean_absolute_error, r2_score

        predictions = []
        for model in self.model:
            predictions.append(model.predict(X_test))

        predictions = np.column_stack(predictions)

        print("\n=== Model Evaluation ===")
        for i, target in enumerate(['Throttle', 'Brake', 'Steering']):
            mae = mean_absolute_error(y_test.iloc[:, i], predictions[:, i])
            r2 = r2_score(y_test.iloc[:, i], predictions[:, i])
            print(f"{target:10s} - MAE: {mae:.4f}, R²: {r2:.4f}")
```

### Phase 4: Reinforcement Learning (Advanced)

```python
import gymnasium as gym
from stable_baselines3 import SAC
from stable_baselines3.common.callbacks import BaseCallback


class OversteerEnv(gym.Env):
    """
    Custom environment for oversteer control
    """
    def __init__(self, telemetry_data):
        super().__init__()

        self.telemetry = telemetry_data
        self.current_idx = 0
        self.target_balance = -2.0

        # Action space: [throttle, brake, steering]
        self.action_space = gym.spaces.Box(
            low=np.array([0, 0, -1]),
            high=np.array([1, 1, 1]),
            dtype=np.float32
        )

        # Observation space: vehicle state
        self.observation_space = gym.spaces.Box(
            low=-np.inf, high=np.inf,
            shape=(15,),  # 15 state features
            dtype=np.float32
        )

    def reset(self, seed=None, options=None):
        """Reset to random cornering state"""
        super().reset(seed=seed)

        # Find random cornering section
        corners = self.telemetry[
            (self.telemetry['in_corner'] == 1) &
            (self.telemetry['Speed'] > 20)
        ]

        self.current_idx = np.random.choice(corners.index)

        return self._get_observation(), {}

    def step(self, action):
        """
        Apply action and get next state

        In real scenario, this would interact with simulator.
        Here we use recorded telemetry for demonstration.
        """
        throttle, brake, steering = action

        # Get current state
        current = self.telemetry.loc[self.current_idx]

        # Move to next timestep
        self.current_idx += 1
        if self.current_idx >= len(self.telemetry):
            self.current_idx = 0

        next_state = self.telemetry.loc[self.current_idx]

        # Calculate reward
        reward = self._calculate_reward(next_state, action)

        # Check if done (end of corner or unsafe)
        done = (
            next_state['in_corner'] == 0 or
            abs(next_state['balance']) > 10 or  # Too much oversteer
            next_state['Speed'] < 10  # Spun out
        )

        return self._get_observation(), reward, done, False, {}

    def _calculate_reward(self, state, action):
        """
        Reward function for maintaining optimal oversteer
        """
        # Primary: How close to target oversteer?
        balance_error = abs(state['balance'] - self.target_balance)
        balance_reward = np.exp(-balance_error / 2.0)  # Exponential decay

        # Speed: Faster is better
        speed_reward = state['Speed'] / 50.0  # Normalize to ~1.0

        # Stability: Penalize excessive yaw rate
        stability_penalty = -abs(state['YawRate']) * 0.1

        # Smoothness: Penalize jerky inputs
        throttle_change = abs(action[0] - state['Throttle'])
        brake_change = abs(action[1] - state['Brake'])
        steering_change = abs(action[2] - state['SteeringWheelAngle'])
        smoothness_penalty = -(throttle_change + brake_change + steering_change) * 0.5

        # Total reward
        reward = (
            balance_reward * 2.0 +      # Most important
            speed_reward * 1.0 +
            stability_penalty +
            smoothness_penalty
        )

        return reward

    def _get_observation(self):
        """Get current state observation"""
        state = self.telemetry.loc[self.current_idx]

        obs = np.array([
            state['Speed'], state['RPM'], state['Gear'],
            state['slip_angle_front_deg'], state['slip_angle_rear_deg'],
            state['balance'],
            state['YawRate'], state['LatAccel'], state['LongAccel'],
            state['LapDistPct'], state['curvature'],
            state['Throttle'], state['Brake'], state['SteeringWheelAngle'],
            state['avg_tire_temp']
        ], dtype=np.float32)

        return obs


def train_rl_agent(telemetry_df):
    """
    Train RL agent for oversteer control
    """
    # Create environment
    env = OversteerEnv(telemetry_df)

    # Create SAC agent (good for continuous control)
    model = SAC(
        "MlpPolicy",
        env,
        learning_rate=3e-4,
        buffer_size=100000,
        batch_size=256,
        tau=0.005,
        gamma=0.99,
        verbose=1
    )

    # Train
    print("Training RL agent...")
    model.learn(total_timesteps=100000)

    return model
```

## Evaluation Metrics

### Online Metrics (During Lap)

```python
def evaluate_oversteer_control(telemetry_df, predictions_df):
    """
    Evaluate how well the model maintains target oversteer
    """
    metrics = {}

    # Filter to cornering sections
    corners = telemetry_df[telemetry_df['in_corner'] == 1]

    # 1. Balance accuracy
    target_balance = -2.0
    balance_error = np.abs(corners['balance'] - target_balance)
    metrics['mean_balance_error'] = balance_error.mean()
    metrics['balance_in_target_pct'] = (balance_error < 1.0).sum() / len(balance_error) * 100

    # 2. Lap time (if available)
    if 'LapCurrentLapTime' in telemetry_df.columns:
        metrics['lap_time'] = telemetry_df['LapCurrentLapTime'].max()

    # 3. Stability (lower yaw rate variance = smoother)
    metrics['yaw_rate_std'] = corners['YawRate'].std()

    # 4. Average corner speed
    metrics['avg_corner_speed'] = corners['Speed'].mean()

    # 5. Safety (% time in dangerous oversteer)
    metrics['dangerous_oversteer_pct'] = (corners['balance'] < -5).sum() / len(corners) * 100

    return metrics
```

### Offline Metrics (Model Performance)

- **Input prediction accuracy**: MAE for throttle/brake/steering
- **Balance tracking**: How well predicted inputs achieve target balance
- **Safety**: No predictions that cause spin/crash
- **Consistency**: Low variance across similar states

## Deployment Strategy

### 1. Simulation Testing

Before real-world use:
1. Test in replay mode (feed recorded states, compare predictions to actuals)
2. Validate on unseen laps
3. Check edge cases (low grip, high speed, tight corners)

### 2. Gradual Rollout

```python
class HybridController:
    """
    Blend human input with ML suggestions
    """
    def __init__(self, ml_model, blend_factor=0.3):
        self.model = ml_model
        self.blend_factor = blend_factor  # 0 = human only, 1 = ML only

    def get_inputs(self, state, human_inputs):
        """
        Blend ML and human inputs
        """
        ml_inputs = self.model.predict(state)

        blended = {
            'throttle': (1 - self.blend_factor) * human_inputs['throttle'] +
                       self.blend_factor * ml_inputs['throttle'],
            'brake': (1 - self.blend_factor) * human_inputs['brake'] +
                    self.blend_factor * ml_inputs['brake'],
            'steering': (1 - self.blend_factor) * human_inputs['steering'] +
                       self.blend_factor * ml_inputs['steering']
        }

        return blended
```

### 3. Real-time Inference

For live use (if iRacing supports):
```python
def real_time_control_loop():
    """
    Real-time control loop
    """
    controller = OversteerController()
    controller.load_model('oversteer_model.pkl')

    while racing:
        # Get current telemetry from iRacing API
        state = get_current_telemetry()

        # Calculate slip angles
        state = calculate_slip_angles(state)

        # Get ML predictions
        inputs = controller.predict(state)

        # Apply inputs (if supported by simulator)
        apply_inputs(inputs)

        # Sleep until next update (60Hz)
        time.sleep(1/60)
```

## Training Data Requirements

### Minimum Requirements

- **10+ laps** of high-quality driving on same track
- **Focus on corners** where oversteer matters
- **Multiple sessions** (different conditions, fuel loads)

### Optimal Requirements

- **50+ laps** across multiple sessions
- **Multiple drivers** (different styles, all fast)
- **Various conditions** (tire wear, fuel load, temperature)
- **Label quality** (mark best laps/sections)

## Safety Considerations

1. **Sanity checks**: Validate predictions before applying
2. **Fallback**: Revert to human input if model uncertain
3. **Gradual blend**: Start with low blend factor (10-20%)
4. **Emergency override**: Human can always override
5. **Testing**: Extensive simulation before real use

## Next Steps

1. Collect high-quality telemetry data
2. Calculate slip angles and balance
3. Label high-quality oversteer sections
4. Train supervised model (start here)
5. Evaluate on test laps
6. (Advanced) Train RL agent for refinement
7. Test in simulation/replay mode
8. Deploy with low blend factor
9. Iterate based on performance

## Expected Performance

With good training data:
- **Balance tracking**: Within ±1° of target 60-80% of time
- **Lap time improvement**: 0.5-2% faster with optimal oversteer
- **Consistency**: Reduced lap time variance
- **Safety**: No spin-inducing predictions

The model learns the subtle balance between:
- Throttle application timing (rotation vs traction)
- Steering input (initiate vs maintain vs exit oversteer)
- Brake trailing (weight transfer for rotation)
