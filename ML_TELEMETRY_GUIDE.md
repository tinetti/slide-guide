# Machine Learning with iRacing Telemetry Data

## Overview

iRacing telemetry data is ideal for machine learning applications due to its high-frequency sampling (60Hz), rich feature set (287 variables), and clear performance metrics (lap times, positions, incidents).

---

## ML Use Cases

### 1. **Lap Time Prediction**
- **Input**: Car telemetry during a lap
- **Output**: Predicted lap time
- **Use**: Setup optimization, real-time performance estimation

### 2. **Optimal Racing Line**
- **Input**: Track position, speed, steering, throttle/brake
- **Output**: Optimal inputs for fastest lap
- **Use**: Driver coaching, AI racing agent

### 3. **Tire Wear & Degradation**
- **Input**: Tire temps, pressures, lap number, track conditions
- **Output**: Predicted tire wear rate, optimal pit window
- **Use**: Race strategy optimization

### 4. **Incident Prediction**
- **Input**: Speed, steering angle changes, track position, nearby cars
- **Output**: Probability of incident in next N seconds
- **Use**: Safety systems, driver alerts

### 5. **Fuel Consumption Modeling**
- **Input**: Throttle, speed, RPM, track characteristics
- **Output**: Fuel usage per lap
- **Use**: Race strategy, pit stop planning

### 6. **Setup Optimization**
- **Input**: Car setup parameters + track characteristics
- **Output**: Performance metrics (lap time, stability, tire wear)
- **Use**: Automated setup tuning

### 7. **Driver Style Classification**
- **Input**: Telemetry patterns (braking points, throttle application, steering)
- **Output**: Driver style category (aggressive, smooth, conservative)
- **Use**: Personalized coaching, team matching

---

## Data Extraction Strategy

### Recommended Approach

**1. Export to Parquet or CSV**
- Parquet: Better for large datasets (efficient compression, columnar storage)
- CSV: Better for small datasets and quick analysis

**2. Structure Data by Use Case**

#### Time Series Format (for LSTM/Transformer models)
```
session_id, sample_idx, timestamp, speed, rpm, throttle, brake, gear, ...
12345, 0, 0.000, 0.0, 1500, 0.0, 1.0, 0, ...
12345, 1, 0.017, 0.2, 1600, 0.1, 0.9, 0, ...
12345, 2, 0.033, 0.5, 1800, 0.2, 0.8, 1, ...
```

#### Lap-Aggregated Format (for traditional ML models)
```
session_id, lap_num, lap_time, avg_speed, max_speed, avg_rpm, max_rpm, total_throttle_time, ...
12345, 1, 78.234, 45.2, 68.3, 5600, 8200, 52.3, ...
12345, 2, 77.891, 45.8, 69.1, 5650, 8250, 53.1, ...
```

#### Segment-Based Format (for corner analysis)
```
session_id, lap_num, segment_id, entry_speed, apex_speed, exit_speed, min_radius, ...
12345, 1, turn_1, 65.3, 48.2, 58.9, 25.4, ...
12345, 1, turn_2, 78.4, 62.1, 71.3, 42.8, ...
```

---

## Feature Selection

### Core Features (Universal)

**Vehicle Dynamics:**
- Speed, RPM, Gear
- Throttle, Brake, Clutch
- SteeringWheelAngle
- Lat/Long/Vertical acceleration (Lat/LongAccel, VertAccel)
- Yaw/Pitch/Roll rates

**Position & Track:**
- LapDist, LapDistPct (track position)
- TrackTemp, TrackTempCrew
- OnTrack, OnPitRoad
- TrackSurface (TrkLoc, TrkSurf)

**Tires:**
- Tire temps (LF/RF/LR/RR tempCL/CM/CR)
- Tire pressures (LF/RF/LR/RR TempPress)
- Tire wear (LF/RF/LR/RR wear L/M/R)

**Time & Performance:**
- SessionTime, LapCurrentLapTime
- LapDeltaToBestLap
- LapLastLapTime, LapBestLapTime

### Advanced Features (Use Case Specific)

**For Fuel Strategy:**
- FuelLevel, FuelLevelPct
- FuelUsePerHour
- Throttle application patterns

**For Incident Prediction:**
- RelativeSpeed (to nearby cars)
- SteeringWheelAngleMax (steering aggression)
- BrakeRaw, ThrottleRaw (input smoothness)
- CarIdxLapDistPct (nearby car positions)

**For Setup Optimization:**
- RideHeight (LF/RF/LR/RR)
- ShockDefl, ShockVel (suspension)
- RollRate (stability)
- Aero balance parameters

---

## Feature Engineering

### Derived Features

```python
# Speed-based features
df['acceleration'] = df['Speed'].diff() / df['SessionTime'].diff()
df['braking_intensity'] = df['Brake'] * df['Speed']
df['corner_speed'] = df['Speed'] * (1 - abs(df['SteeringWheelAngle']))

# Efficiency metrics
df['throttle_efficiency'] = df['Speed'] / (df['Throttle'] + 0.01)
df['rpm_per_speed'] = df['RPM'] / (df['Speed'] + 0.01)

# Rolling statistics (smoothing)
df['speed_ma_5'] = df['Speed'].rolling(window=5).mean()
df['throttle_variance'] = df['Throttle'].rolling(window=10).var()

# Track position features
df['sector'] = (df['LapDistPct'] * 10).astype(int)  # Divide track into 10 sectors
df['distance_to_next_turn'] = calculate_turn_distance(df['LapDistPct'])

# Lap context
df['lap_progress'] = df['LapDistPct']
df['time_into_lap'] = df['LapCurrentLapTime']
```

### Normalization

```python
from sklearn.preprocessing import StandardScaler, MinMaxScaler

# Normalize by variable type
scaler = StandardScaler()
df[['Speed', 'RPM', 'LapCurrentLapTime']] = scaler.fit_transform(
    df[['Speed', 'RPM', 'LapCurrentLapTime']]
)

# For bounded variables (0-1), use MinMaxScaler or keep as-is
# Throttle, Brake, Clutch are already 0-1
```

---

## Data Preprocessing Pipeline

### 1. Extract Multiple Sessions

```csharp
// Create a telemetry exporter
public class TelemetryMlExporter
{
    public async Task ExportToParquet(string ibtFile, string outputFile)
    {
        using var telemetry = await _service.LoadFromFileAsync(ibtFile);

        var records = new List<TelemetrySample>();
        await foreach (var sample in telemetry.GetSamplesAsync())
        {
            records.Add(sample);
        }

        // Convert to DataFrame and write Parquet
        // (requires Apache.Arrow or similar library)
    }

    public async Task ExportToCsv(string ibtFile, string outputFile,
                                   string[] selectedVars = null)
    {
        using var telemetry = await _service.LoadFromFileAsync(ibtFile);
        using var writer = new StreamWriter(outputFile);

        // Get variable subset if specified
        var varsToExport = selectedVars ?? GetDefaultMlVariables();

        // Write header
        await writer.WriteLineAsync(
            "session_id,sample_idx," + string.Join(",", varsToExport)
        );

        var sessionId = telemetry.GetUniqueId();
        var sampleIdx = 0;

        await foreach (var sample in telemetry.GetSamplesAsync())
        {
            var values = new List<string> { sessionId, sampleIdx.ToString() };

            foreach (var varName in varsToExport)
            {
                var param = sample.GetParameter(varName);
                values.Add(FormatValue(param?.Value));
            }

            await writer.WriteLineAsync(string.Join(",", values));
            sampleIdx++;
        }
    }

    private string[] GetDefaultMlVariables() => new[]
    {
        "SessionTime", "Speed", "RPM", "Gear",
        "Throttle", "Brake", "Clutch", "SteeringWheelAngle",
        "LapDist", "LapDistPct", "LapCurrentLapTime",
        "LatAccel", "LongAccel", "VertAccel",
        "LFtempCL", "LFtempCM", "LFtempCR",
        "RFtempCL", "RFtempCM", "RFtempCR",
        "LRtempCL", "LRtempCM", "LRtempCR",
        "RRtempCL", "RRtempCM", "RRtempCR",
        "FuelLevel", "FuelLevelPct"
    };
}
```

### 2. Python Data Loading

```python
import pandas as pd
import numpy as np
from pathlib import Path

class TelemetryDataLoader:
    def __init__(self, data_dir):
        self.data_dir = Path(data_dir)

    def load_session(self, filename):
        """Load a single telemetry session"""
        df = pd.read_csv(self.data_dir / filename)
        return df

    def load_all_sessions(self):
        """Load all CSV files in directory"""
        dfs = []
        for csv_file in self.data_dir.glob("*.csv"):
            df = pd.read_csv(csv_file)
            dfs.append(df)
        return pd.concat(dfs, ignore_index=True)

    def create_lap_features(self, df):
        """Aggregate telemetry by lap"""
        lap_features = df.groupby(['session_id', 'Lap']).agg({
            'Speed': ['mean', 'max', 'std'],
            'RPM': ['mean', 'max'],
            'Throttle': ['mean', 'sum'],
            'Brake': ['sum'],
            'LapCurrentLapTime': 'max',
            'LFtempCL': 'mean',
            'RFtempCL': 'mean',
            'LRtempCL': 'mean',
            'RRtempCL': 'mean',
        })

        lap_features.columns = ['_'.join(col) for col in lap_features.columns]
        return lap_features.reset_index()

    def create_sequences(self, df, sequence_length=60):
        """Create sequences for LSTM/RNN models"""
        # Group by session and lap
        sequences = []
        labels = []

        for (session, lap), group in df.groupby(['session_id', 'Lap']):
            if len(group) < sequence_length:
                continue

            # Create overlapping windows
            for i in range(0, len(group) - sequence_length, 10):
                seq = group.iloc[i:i+sequence_length]
                sequences.append(seq[self.feature_cols].values)
                labels.append(group.iloc[i+sequence_length]['LapCurrentLapTime'])

        return np.array(sequences), np.array(labels)
```

---

## Model Architectures

### 1. Lap Time Prediction (Regression)

**Traditional ML (Random Forest, XGBoost):**
```python
from sklearn.ensemble import RandomForestRegressor
from xgboost import XGBRegressor

# Use lap-aggregated features
X = lap_features[['Speed_mean', 'Speed_max', 'Throttle_mean', ...]]
y = lap_features['LapCurrentLapTime_max']

model = XGBRegressor(n_estimators=100, max_depth=5)
model.fit(X_train, y_train)

# Feature importance
import matplotlib.pyplot as plt
pd.Series(model.feature_importances_, index=X.columns).sort_values().plot(kind='barh')
```

**Deep Learning (LSTM for time series):**
```python
import tensorflow as tf
from tensorflow.keras import layers

# Input: sequence of telemetry samples
# Output: lap time prediction

model = tf.keras.Sequential([
    layers.LSTM(128, return_sequences=True, input_shape=(sequence_length, num_features)),
    layers.Dropout(0.2),
    layers.LSTM(64),
    layers.Dropout(0.2),
    layers.Dense(32, activation='relu'),
    layers.Dense(1)  # Lap time prediction
])

model.compile(optimizer='adam', loss='mse', metrics=['mae'])
model.fit(X_train, y_train, epochs=50, batch_size=32, validation_split=0.2)
```

### 2. Optimal Racing Line (Reinforcement Learning)

```python
import gymnasium as gym
from stable_baselines3 import PPO

# Define custom environment
class RacingEnv(gym.Env):
    def __init__(self, telemetry_data):
        super().__init__()

        # Actions: [throttle, brake, steering]
        self.action_space = gym.spaces.Box(
            low=np.array([0, 0, -1]),
            high=np.array([1, 1, 1]),
            dtype=np.float32
        )

        # Observations: current telemetry state
        self.observation_space = gym.spaces.Box(
            low=-np.inf, high=np.inf,
            shape=(20,),  # speed, position, steering, etc.
            dtype=np.float32
        )

    def step(self, action):
        # Apply action, get next state from telemetry
        # Reward = progress - time penalty - incident penalty
        pass

    def reset(self):
        # Reset to start of lap
        pass

# Train RL agent
env = RacingEnv(telemetry_data)
model = PPO("MlpPolicy", env, verbose=1)
model.learn(total_timesteps=100000)
```

### 3. Driver Style Classification

```python
from sklearn.ensemble import RandomForestClassifier
from sklearn.preprocessing import LabelEncoder

# Extract driver style features
def extract_driver_features(telemetry_df):
    features = {
        'avg_braking_intensity': (telemetry_df['Brake'] * telemetry_df['Speed']).mean(),
        'throttle_smoothness': 1 / (telemetry_df['Throttle'].diff().abs().mean() + 0.01),
        'steering_aggression': telemetry_df['SteeringWheelAngle'].abs().mean(),
        'late_braking_score': calculate_late_braking(telemetry_df),
        'corner_entry_speed': telemetry_df[telemetry_df['sector'] == 'corner_entry']['Speed'].mean(),
    }
    return features

# Train classifier
X = driver_features_df[feature_cols]
y = driver_features_df['driver_style']  # 'aggressive', 'smooth', 'conservative'

le = LabelEncoder()
y_encoded = le.fit_transform(y)

model = RandomForestClassifier(n_estimators=100)
model.fit(X_train, y_train)
```

---

## Recommended Workflow

### Phase 1: Data Collection (1-2 weeks)
1. Collect 50-100+ .ibt files (different tracks, conditions, drivers)
2. Export to CSV/Parquet with selected features
3. Store with metadata (track, car, weather, driver)

### Phase 2: Exploratory Analysis (1 week)
1. Load data into pandas/Jupyter
2. Visualize distributions, correlations
3. Identify patterns (fast vs slow laps)
4. Feature engineering experiments

### Phase 3: Baseline Models (1 week)
1. Start with simple models (Linear Regression, Random Forest)
2. Establish baseline performance
3. Validate train/test split strategy
4. Measure key metrics (RMSE, MAE, R²)

### Phase 4: Advanced Models (2-4 weeks)
1. Implement deep learning (LSTM, Transformer)
2. Hyperparameter tuning
3. Cross-validation across tracks/cars
4. Model ensembling

### Phase 5: Deployment (1-2 weeks)
1. Create inference pipeline
2. Real-time prediction from live telemetry
3. Integration with dashboards/apps
4. A/B testing and monitoring

---

## Tools & Libraries

### Python Stack
```bash
# Data processing
pip install pandas numpy pyarrow fastparquet

# Machine learning
pip install scikit-learn xgboost lightgbm

# Deep learning
pip install tensorflow torch

# Visualization
pip install matplotlib seaborn plotly

# Time series
pip install statsmodels prophet

# Reinforcement learning
pip install stable-baselines3 gymnasium
```

### Alternative: Use Databricks / AWS SageMaker
- For large-scale processing (1000+ sessions)
- Distributed training
- MLOps pipeline (model versioning, A/B testing)

---

## Example: Complete Lap Time Prediction

```python
# Step 1: Load and prepare data
loader = TelemetryDataLoader('telemetry_data/')
df = loader.load_all_sessions()

# Step 2: Feature engineering
lap_features = loader.create_lap_features(df)

# Add track/car context
lap_features['track_id'] = df.groupby(['session_id', 'Lap'])['TrackID'].first()
lap_features['car_id'] = df.groupby(['session_id', 'Lap'])['CarId'].first()

# Step 3: Train/test split (by session, not random!)
session_ids = lap_features['session_id'].unique()
train_sessions = session_ids[:int(len(session_ids) * 0.8)]

train = lap_features[lap_features['session_id'].isin(train_sessions)]
test = lap_features[~lap_features['session_id'].isin(train_sessions)]

X_train = train.drop(['session_id', 'Lap', 'LapCurrentLapTime_max'], axis=1)
y_train = train['LapCurrentLapTime_max']

X_test = test.drop(['session_id', 'Lap', 'LapCurrentLapTime_max'], axis=1)
y_test = test['LapCurrentLapTime_max']

# Step 4: Train model
from xgboost import XGBRegressor
from sklearn.metrics import mean_absolute_error, r2_score

model = XGBRegressor(
    n_estimators=200,
    max_depth=6,
    learning_rate=0.05,
    subsample=0.8
)

model.fit(X_train, y_train)

# Step 5: Evaluate
y_pred = model.predict(X_test)

print(f"MAE: {mean_absolute_error(y_test, y_pred):.3f} seconds")
print(f"R²: {r2_score(y_test, y_pred):.3f}")

# Step 6: Feature importance
import pandas as pd
importance_df = pd.DataFrame({
    'feature': X_train.columns,
    'importance': model.feature_importances_
}).sort_values('importance', ascending=False)

print(importance_df.head(10))
```

---

## Key Considerations

### Data Quality
- **Handle missing values**: Some variables may be NaN
- **Outlier detection**: Remove invalid telemetry (crashes, resets)
- **Sampling rate**: 60Hz is high - consider downsampling to 10Hz for some models

### Temporal Structure
- **Don't shuffle time series**: Preserve order within laps
- **Split by session/date**: Prevent data leakage
- **Consider autocorrelation**: Telemetry samples are highly correlated

### Track/Car Variability
- **One model per track**: Or use track features as input
- **Car class encoding**: Different cars have different characteristics
- **Transfer learning**: Train on one track, fine-tune on another

### Real-time Constraints
- **Inference latency**: Model must predict quickly (< 100ms)
- **Feature lag**: Use only past data, not future
- **Model size**: Edge deployment may require small models

---

## Next Steps

1. **Create CSV exporter** in the CLI tool
2. **Collect diverse data** (multiple tracks, cars, drivers)
3. **Start with simple use case** (lap time prediction)
4. **Iterate and expand** to more complex models

Would you like me to implement a CSV/Parquet exporter for the CLI tool?
