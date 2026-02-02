# Machine Learning Examples

This directory contains example scripts demonstrating how to use iRacing telemetry data for machine learning applications.

## Setup

### 1. Export Telemetry Data

First, export your telemetry data to CSV format:

```bash
# Export single session
dotnet run --project ../src/IbtTelemetry.Cli -- export ../sample.ibt telemetry_ml.csv

# Export multiple sessions (recommended for ML training)
dotnet run --project ../src/IbtTelemetry.Cli -- export /path/to/telemetry_folder/ telemetry_ml.csv
```

### 2. Install Python Dependencies

```bash
pip install pandas numpy scikit-learn xgboost matplotlib
```

### 3. Run Example

```bash
python ml_lap_time_prediction.py
```

## Examples

### `calculate_slip_angles.py`

**Goal**: Calculate tire slip angles from telemetry data

**Features**:
- Parses velocity vectors (VelocityX, VelocityY)
- Computes front and rear slip angles
- Calculates vehicle balance (understeer/oversteer)
- Plots tire characteristic curves
- Analyzes slip angle distributions

**Use Cases**:
- Setup optimization (balance tuning)
- Tire analysis (peak slip angle)
- Driver coaching (slip angle management)
- Vehicle dynamics understanding

**Usage**:
```bash
# Export telemetry with all variables
dotnet run --project ../src/IbtTelemetry.Cli -- export ../sample.ibt telemetry.csv --all

# Calculate slip angles
python calculate_slip_angles.py telemetry.csv
```

**Expected Output**:
- Slip angle statistics
- Balance metrics (understeer %)
- Plots showing slip angles over time
- Tire characteristic curves

See [docs/SLIP_ANGLE_CALCULATION.md](../docs/SLIP_ANGLE_CALCULATION.md) for detailed theory.

---

### `train_oversteer_controller.py` ⭐ Advanced

**Goal**: Train ML model to maintain optimal oversteer during cornering

**Features**:
- Labels high-quality oversteer sections
- Trains XGBoost models to predict throttle/brake/steering
- Learns optimal inputs for target slip angle balance
- Evaluates oversteer maintenance performance
- Saves trained model for inference

**Use Cases**:
- Driver assist system (maintain optimal oversteer)
- Learning from expert drivers
- Setup optimization (understand input patterns)
- Autonomous racing agents

**Usage**:
```bash
# 1. Export telemetry
dotnet run --project ../src/IbtTelemetry.Cli -- export sample.ibt data.csv --all

# 2. Calculate slip angles
python calculate_slip_angles.py data.csv

# 3. Train oversteer controller
python train_oversteer_controller.py data_with_slip_angles.csv
```

**Target Balance**:
- `-2°`: Moderate oversteer (recommended starting point)
- `-1°`: Slight oversteer (safer, more stable)
- `-3°`: Aggressive oversteer (faster but riskier)
- `0°`: Neutral balance
- `+2°`: Understeer (safer for beginners)

**Expected Output**:
- Trained models for throttle, brake, steering
- Balance maintenance metrics
- Quality distribution plots
- Saved model (.pkl file)

See [docs/OVERSTEER_CONTROL_ML.md](../docs/OVERSTEER_CONTROL_ML.md) for detailed methodology and advanced techniques (RL, hybrid approaches).

---

### `ml_lap_time_prediction.py`

**Goal**: Predict lap times based on telemetry data

**Features**:
- Loads exported CSV data
- Engineers features (speed metrics, driver inputs, efficiency)
- Aggregates telemetry by lap
- Trains XGBoost regression model
- Evaluates performance (MAE, R², RMSE)
- Shows feature importance
- Plots predictions vs actual

**Use Cases**:
- Setup optimization (predict impact of changes)
- Driver coaching (compare predicted vs actual)
- Race strategy (estimate stint times)

**Expected Performance** (with 50+ sessions):
- Mean Absolute Error: 0.5-2.0 seconds
- R² Score: 0.85-0.95

## Data Requirements

### Minimum for Training
- **10+ sessions**: Basic model training
- **50+ sessions**: Good generalization
- **100+ sessions**: Production-ready model

### Data Quality Tips
1. **Complete laps only**: Filter out incomplete/invalid laps
2. **Same track**: Start with single track, then expand
3. **Consistent conditions**: Similar weather, time of day
4. **Multiple drivers**: Improves model robustness
5. **Clean telemetry**: Remove crashes, disconnects

## Next Steps

### More ML Use Cases

1. **Tire Degradation Prediction**
   - Input: Tire temps, pressures, lap number
   - Output: Predicted wear rate, optimal pit lap

2. **Optimal Racing Line**
   - Input: Track position, car state
   - Output: Optimal throttle/brake/steering inputs
   - Approach: Reinforcement Learning (PPO, SAC)

3. **Incident Prediction**
   - Input: Speed, steering changes, nearby cars
   - Output: Probability of incident in next N seconds
   - Approach: Binary classification (Random Forest, Neural Network)

4. **Driver Style Classification**
   - Input: Telemetry patterns
   - Output: Driver category (aggressive, smooth, conservative)
   - Approach: Clustering (K-means, DBSCAN) or classification

5. **Setup Optimizer**
   - Input: Car setup parameters + track
   - Output: Predicted lap time
   - Approach: Bayesian Optimization, Genetic Algorithm

### Advanced Techniques

**Deep Learning (LSTM/Transformer)**
```python
import tensorflow as tf

# Sequential model for time-series
model = tf.keras.Sequential([
    tf.keras.layers.LSTM(128, return_sequences=True),
    tf.keras.layers.LSTM(64),
    tf.keras.layers.Dense(1)
])
```

**Reinforcement Learning**
```python
from stable_baselines3 import PPO

# Train RL agent for optimal racing
env = RacingEnv(telemetry_data)
model = PPO("MlpPolicy", env)
model.learn(total_timesteps=100000)
```

## Resources

- [ML_TELEMETRY_GUIDE.md](../ML_TELEMETRY_GUIDE.md): Comprehensive ML guide
- [IBT_FORMAT_SPECIFICATION.md](../IBT_FORMAT_SPECIFICATION.md): File format details
- [README.md](../README.md): Project documentation

## Contributing

Have a cool ML use case? Create a PR with your example script!

### Example Template

```python
#!/usr/bin/env python3
"""
Example: [Your Use Case]

Description: [What this does]
Requirements: [pip packages]
"""

def load_data():
    pass

def train_model():
    pass

def main():
    pass

if __name__ == '__main__':
    main()
```
