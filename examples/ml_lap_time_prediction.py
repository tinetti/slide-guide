#!/usr/bin/env python3
"""
Example: Lap Time Prediction using iRacing Telemetry Data

This script demonstrates how to:
1. Load exported telemetry CSV
2. Engineer features
3. Train a lap time prediction model
4. Evaluate performance

Requirements:
    pip install pandas numpy scikit-learn xgboost matplotlib pyarrow
"""

import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
from sklearn.model_selection import train_test_split
from sklearn.preprocessing import StandardScaler
from sklearn.metrics import mean_absolute_error, r2_score, mean_squared_error
from xgboost import XGBRegressor


def load_telemetry_data(parquet_path):
    """Load telemetry data from exported Parquet"""
    print(f"Loading data from {parquet_path}...")
    df = pd.read_parquet(parquet_path)
    print(f"Loaded {len(df)} samples from {df['session_id'].nunique()} sessions")
    return df


def engineer_features(df):
    """Create derived features for ML"""
    print("Engineering features...")

    # Parse array columns (acceleration, tire temps, etc.)
    # Arrays are stored as "[value1;value2;value3]"
    def parse_array_column(col):
        if col.dtype == object and col.str.contains('[', na=False).any():
            return col.str.strip('[]"').str.split(';', expand=True).astype(float)
        return col

    # Speed-based features
    df['acceleration'] = df.groupby('session_id')['Speed'].diff().fillna(0)
    df['braking_intensity'] = df['Brake'] * df['Speed']
    df['throttle_application_rate'] = df.groupby('session_id')['Throttle'].diff().fillna(0)

    # Rolling statistics (smoothing over 5 samples @ 60Hz = ~83ms)
    df['speed_ma_5'] = df.groupby('session_id')['Speed'].rolling(window=5, min_periods=1).mean().reset_index(0, drop=True)
    df['rpm_ma_5'] = df.groupby('session_id')['RPM'].rolling(window=5, min_periods=1).mean().reset_index(0, drop=True)

    # Steering smoothness
    df['steering_change'] = df.groupby('session_id')['SteeringWheelAngle'].diff().abs().fillna(0)
    df['steering_smoothness'] = 1 / (df['steering_change'] + 0.01)

    # Efficiency metrics
    df['throttle_efficiency'] = df['Speed'] / (df['Throttle'] + 0.01)
    df['rpm_per_speed'] = df['RPM'] / (df['Speed'] + 0.01)

    # Tire temperature average (if available)
    tire_temp_cols = [col for col in df.columns if 'temp' in col.lower() and any(x in col for x in ['LF', 'RF', 'LR', 'RR'])]
    if tire_temp_cols:
        df['avg_tire_temp'] = df[tire_temp_cols].mean(axis=1)

    print(f"Created features. Total columns: {len(df.columns)}")
    return df


def create_lap_features(df):
    """Aggregate telemetry by lap for prediction"""
    print("Creating lap-level features...")

    # Group by session and lap
    lap_features = df.groupby(['session_id', 'Lap']).agg({
        # Speed metrics
        'Speed': ['mean', 'max', 'std', 'min'],
        'speed_ma_5': 'mean',

        # RPM metrics
        'RPM': ['mean', 'max'],
        'rpm_ma_5': 'mean',

        # Driver inputs
        'Throttle': ['mean', 'sum', 'std'],
        'Brake': ['sum', 'mean'],
        'Clutch': ['sum'],
        'SteeringWheelAngle': ['mean', 'std'],

        # Derived features
        'acceleration': ['mean', 'std'],
        'braking_intensity': ['mean', 'sum'],
        'throttle_efficiency': 'mean',
        'steering_smoothness': 'mean',

        # Target variable
        'LapCurrentLapTime': 'max',  # Final lap time

        # Context
        'FuelLevelPct': 'mean',
    }).reset_index()

    # Flatten column names
    lap_features.columns = ['_'.join(col).strip('_') for col in lap_features.columns]

    # Remove incomplete laps (lap time = 0)
    lap_features = lap_features[lap_features['LapCurrentLapTime_max'] > 0]

    print(f"Created {len(lap_features)} lap feature rows")
    return lap_features


def train_model(lap_features):
    """Train XGBoost model for lap time prediction"""
    print("\nTraining model...")

    # Prepare features and target
    feature_cols = [col for col in lap_features.columns
                   if col not in ['session_id', 'Lap', 'LapCurrentLapTime_max']]

    X = lap_features[feature_cols]
    y = lap_features['LapCurrentLapTime_max']

    # Split by session to avoid data leakage
    unique_sessions = lap_features['session_id'].unique()
    train_sessions, test_sessions = train_test_split(
        unique_sessions, test_size=0.2, random_state=42
    )

    train_mask = lap_features['session_id'].isin(train_sessions)
    test_mask = lap_features['session_id'].isin(test_sessions)

    X_train, X_test = X[train_mask], X[test_mask]
    y_train, y_test = y[train_mask], y[test_mask]

    print(f"Train samples: {len(X_train)}, Test samples: {len(X_test)}")

    # Normalize features
    scaler = StandardScaler()
    X_train_scaled = scaler.fit_transform(X_train)
    X_test_scaled = scaler.transform(X_test)

    # Train XGBoost model
    model = XGBRegressor(
        n_estimators=100,
        max_depth=5,
        learning_rate=0.05,
        subsample=0.8,
        random_state=42
    )

    model.fit(X_train_scaled, y_train)

    # Predictions
    y_pred_train = model.predict(X_train_scaled)
    y_pred_test = model.predict(X_test_scaled)

    # Evaluate
    print("\n=== Model Performance ===")
    print(f"Train MAE: {mean_absolute_error(y_train, y_pred_train):.3f} seconds")
    print(f"Test MAE:  {mean_absolute_error(y_test, y_pred_test):.3f} seconds")
    print(f"Train R²:  {r2_score(y_train, y_pred_train):.3f}")
    print(f"Test R²:   {r2_score(y_test, y_pred_test):.3f}")
    print(f"Test RMSE: {np.sqrt(mean_squared_error(y_test, y_pred_test)):.3f} seconds")

    # Feature importance
    print("\n=== Top 10 Most Important Features ===")
    importance_df = pd.DataFrame({
        'feature': feature_cols,
        'importance': model.feature_importances_
    }).sort_values('importance', ascending=False)

    for idx, row in importance_df.head(10).iterrows():
        print(f"{row['feature']:30s} {row['importance']:.4f}")

    # Plot predictions vs actual
    plot_predictions(y_test, y_pred_test)

    return model, scaler, feature_cols


def plot_predictions(y_true, y_pred):
    """Plot predicted vs actual lap times"""
    plt.figure(figsize=(10, 6))

    plt.subplot(1, 2, 1)
    plt.scatter(y_true, y_pred, alpha=0.6)
    plt.plot([y_true.min(), y_true.max()], [y_true.min(), y_true.max()],
             'r--', lw=2, label='Perfect prediction')
    plt.xlabel('Actual Lap Time (s)')
    plt.ylabel('Predicted Lap Time (s)')
    plt.title('Lap Time Predictions')
    plt.legend()
    plt.grid(True, alpha=0.3)

    plt.subplot(1, 2, 2)
    errors = y_pred - y_true
    plt.hist(errors, bins=30, edgecolor='black')
    plt.xlabel('Prediction Error (s)')
    plt.ylabel('Count')
    plt.title(f'Prediction Error Distribution\nMean: {errors.mean():.3f}s, Std: {errors.std():.3f}s')
    plt.grid(True, alpha=0.3)

    plt.tight_layout()
    plt.savefig('lap_time_prediction_results.png', dpi=150, bbox_inches='tight')
    print("\nSaved plot to: lap_time_prediction_results.png")
    plt.show()


def main():
    """Main execution"""
    # Load data
    parquet_path = 'telemetry_ml.parquet'  # Change to your exported Parquet path
    df = load_telemetry_data(parquet_path)

    # Engineer features
    df = engineer_features(df)

    # Create lap-level features
    lap_features = create_lap_features(df)

    if len(lap_features) < 10:
        print("\nWarning: Not enough lap data for training.")
        print("Collect more telemetry sessions with complete laps.")
        return

    # Train model
    model, scaler, feature_cols = train_model(lap_features)

    print("\n✓ Training complete!")
    print("\nNext steps:")
    print("1. Collect more telemetry data (50+ sessions)")
    print("2. Experiment with different features")
    print("3. Try LSTM for time-series prediction")
    print("4. Deploy model for real-time predictions")


if __name__ == '__main__':
    main()
