#!/usr/bin/env python3
"""
Train ML model to maintain optimal oversteer while cornering

This script:
1. Loads telemetry with calculated slip angles
2. Labels high-quality oversteer sections
3. Trains model to predict optimal inputs (throttle, brake, steering)
4. Evaluates performance
5. Saves model for inference

Requirements:
    pip install pandas numpy scikit-learn xgboost matplotlib joblib
"""

import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
from sklearn.model_selection import train_test_split
from sklearn.metrics import mean_absolute_error, r2_score
import xgboost as xgb
import joblib
import sys


class OversteerController:
    """
    ML model to maintain optimal oversteer during cornering
    """

    def __init__(self, target_balance=-2.0):
        """
        Args:
            target_balance: Target slip angle balance (negative = oversteer)
                           -2° means rear slip angle is 2° more than front
        """
        self.target_balance = target_balance
        self.models = None
        self.feature_cols = None
        self.target_cols = ['Throttle', 'Brake', 'SteeringWheelAngle']

    def label_quality(self, df):
        """
        Label samples by how well they exhibit target oversteer

        Returns quality score 0-1 (1 = perfect oversteer management)
        """
        print(f"Labeling samples for target balance: {self.target_balance}°...")

        # How close to target balance?
        balance_error = np.abs(df['balance'] - self.target_balance)
        balance_quality = np.exp(-balance_error / 2.0)  # Exponential decay

        # Speed factor (faster is better, normalize to top 90%)
        speed_factor = df['Speed'] / df['Speed'].quantile(0.90)
        speed_factor = np.clip(speed_factor, 0, 1)

        # Stability factor (penalize high yaw rate variance)
        yaw_stability = 1 / (1 + df['YawRate'].abs() / 0.5)

        # Combined quality
        quality = balance_quality * speed_factor * yaw_stability

        print(f"  High quality samples (>0.6): {(quality > 0.6).sum()} / {len(quality)}")
        print(f"  Mean quality: {quality.mean():.3f}")

        return quality

    def engineer_features(self, df):
        """
        Create features for oversteer control model
        """
        print("Engineering features...")

        # Basic vehicle state
        features = df[[
            'Speed', 'RPM', 'Gear',
            'slip_angle_front_deg', 'slip_angle_rear_deg', 'balance',
            'LapDistPct'
        ]].copy()

        # Add current inputs (to learn adjustments)
        features['current_throttle'] = df['Throttle']
        features['current_brake'] = df['Brake']
        features['current_steering'] = df['SteeringWheelAngle']

        # Parse array columns if they exist
        def safe_parse_array(col):
            if col.dtype == object:
                return col.apply(lambda x: self._parse_array(x))
            return col

        if 'YawRate' in df.columns:
            features['yaw_rate'] = safe_parse_array(df['YawRate'])
        if 'LatAccel' in df.columns:
            features['lat_accel'] = safe_parse_array(df['LatAccel'])
        if 'LongAccel' in df.columns:
            features['long_accel'] = safe_parse_array(df['LongAccel'])

        # Fill missing values
        features = features.fillna(0)

        # Rolling averages (last 30 samples = 0.5s @ 60Hz)
        window = min(30, len(df) // 10)
        if window > 1:
            features['balance_ma'] = df['balance'].rolling(window, min_periods=1).mean()
            features['slip_front_ma'] = df['slip_angle_front_deg'].rolling(window, min_periods=1).mean()

        # Rate of change
        features['balance_rate'] = df['balance'].diff().fillna(0)

        # Track curvature (estimated from steering and speed)
        features['curvature'] = df['SteeringWheelAngle'] / (df['Speed'] + 0.1)

        # Cornering indicator
        features['in_corner'] = (features['curvature'].abs() > 0.02).astype(int)

        # Fill any remaining NaN
        features = features.fillna(0)

        print(f"  Created {len(features.columns)} features")
        return features

    def _parse_array(self, arr_str):
        """Parse array from CSV format: '[v1;v2;v3]'"""
        if pd.isna(arr_str) or arr_str == '':
            return 0.0
        try:
            cleaned = str(arr_str).strip('[]"').replace('"', '').split(';')
            values = [float(v) for v in cleaned if v.strip()]
            return values[-1] if values else 0.0
        except:
            return 0.0

    def prepare_training_data(self, df):
        """
        Prepare training data from telemetry

        Only use high-quality cornering samples
        """
        print("\nPreparing training data...")

        # Calculate quality labels
        df['quality'] = self.label_quality(df)

        # Engineer features
        features = self.engineer_features(df)

        # Add quality and in_corner to main df
        df['in_corner'] = features['in_corner']

        # Filter to high-quality cornering samples
        mask = (
            (df['in_corner'] == 1) &           # In a corner
            (df['Speed'] > 15) &                # Above minimum speed
            (df['quality'] > 0.5)               # High quality oversteer
        )

        print(f"  Total samples: {len(df)}")
        print(f"  Cornering samples: {df['in_corner'].sum()}")
        print(f"  High-quality samples: {mask.sum()}")

        if mask.sum() < 100:
            print("\n⚠️  Warning: Very few training samples!")
            print("  Tips:")
            print("  - Collect more laps focused on cornering")
            print("  - Ensure slip angles are calculated")
            print("  - Try lowering quality threshold")

        df_train = df[mask].copy()
        features_train = features[mask].copy()

        # Select feature columns (exclude future/target info)
        self.feature_cols = [col for col in features_train.columns
                            if col not in ['in_corner'] and col in features_train.columns]

        X = features_train[self.feature_cols]
        y = df_train[self.target_cols]
        weights = df_train['quality'].values

        print(f"  Training samples: {len(X)}")
        print(f"  Features: {len(self.feature_cols)}")

        return X, y, weights

    def train(self, X, y, sample_weights=None):
        """
        Train multi-output regression models
        """
        print("\nTraining oversteer control models...")

        self.models = []

        for i, target in enumerate(self.target_cols):
            print(f"\n  Training {target} model...")

            # Split data
            X_train, X_val, y_train, y_val = train_test_split(
                X, y.iloc[:, i], test_size=0.2, random_state=42
            )

            if sample_weights is not None:
                sw_train, sw_val = train_test_split(
                    sample_weights, test_size=0.2, random_state=42
                )
            else:
                sw_train, sw_val = None, None

            # Train XGBoost
            model = xgb.XGBRegressor(
                n_estimators=150,
                max_depth=5,
                learning_rate=0.05,
                subsample=0.8,
                colsample_bytree=0.8,
                random_state=42
            )

            model.fit(
                X_train, y_train,
                sample_weight=sw_train,
                eval_set=[(X_val, y_val)],
                verbose=False
            )

            # Evaluate
            y_pred = model.predict(X_val)
            mae = mean_absolute_error(y_val, y_pred)
            r2 = r2_score(y_val, y_pred)

            print(f"    Validation MAE: {mae:.4f}")
            print(f"    Validation R²:  {r2:.4f}")

            self.models.append(model)

        print("\n✓ Training complete!")

    def predict(self, state):
        """
        Predict optimal inputs given current state

        Args:
            state: DataFrame with current state features

        Returns:
            DataFrame with predicted throttle, brake, steering
        """
        if self.models is None:
            raise RuntimeError("Model not trained. Call train() first.")

        # Extract features
        X = state[self.feature_cols]

        # Predict each output
        predictions = pd.DataFrame()
        predictions['Throttle'] = np.clip(self.models[0].predict(X), 0, 1)
        predictions['Brake'] = np.clip(self.models[1].predict(X), 0, 1)
        predictions['SteeringWheelAngle'] = np.clip(self.models[2].predict(X), -1, 1)

        # Safety: can't have both throttle and brake
        both_mask = (predictions['Throttle'] > 0.1) & (predictions['Brake'] > 0.1)
        predictions.loc[both_mask, 'Throttle'] = 0  # Prefer brake for safety

        return predictions

    def evaluate_oversteer_maintenance(self, df, predictions):
        """
        Evaluate how well predicted inputs would maintain target oversteer
        """
        print("\n" + "=" * 70)
        print("OVERSTEER CONTROL EVALUATION")
        print("=" * 70)

        # Filter to cornering sections
        corners = df[df['in_corner'] == 1]

        if len(corners) == 0:
            print("No cornering sections found")
            return {}

        # Metrics
        metrics = {}

        # 1. Balance accuracy
        balance_error = np.abs(corners['balance'] - self.target_balance)
        metrics['mean_balance_error'] = balance_error.mean()
        metrics['median_balance_error'] = balance_error.median()
        metrics['balance_in_target_pct'] = (balance_error < 1.0).sum() / len(balance_error) * 100

        # 2. Average corner speed
        metrics['avg_corner_speed'] = corners['Speed'].mean()

        # 3. Safety (dangerous oversteer)
        metrics['dangerous_oversteer_pct'] = (corners['balance'] < -5).sum() / len(corners) * 100

        # 4. Stability
        if 'YawRate' in corners.columns:
            yaw_rate = corners['YawRate'].apply(self._parse_array)
            metrics['yaw_rate_std'] = yaw_rate.std()

        print(f"\nTarget balance: {self.target_balance}°")
        print(f"Mean balance error: {metrics['mean_balance_error']:.2f}°")
        print(f"Time in target (±1°): {metrics['balance_in_target_pct']:.1f}%")
        print(f"Avg corner speed: {metrics['avg_corner_speed']:.1f} m/s")
        print(f"Dangerous oversteer: {metrics['dangerous_oversteer_pct']:.1f}%")

        return metrics

    def plot_results(self, df, save_path='oversteer_control_results.png'):
        """
        Plot model training results
        """
        print("\nGenerating plots...")

        corners = df[df['in_corner'] == 1]
        if len(corners) == 0:
            print("No cornering data to plot")
            return

        fig, axes = plt.subplots(2, 2, figsize=(16, 10))

        # Plot 1: Balance over time
        ax = axes[0, 0]
        ax.plot(corners['SessionTime'], corners['balance'], alpha=0.7, linewidth=1)
        ax.axhline(y=self.target_balance, color='r', linestyle='--', linewidth=2, label='Target')
        ax.axhline(y=self.target_balance-1, color='r', linestyle=':', alpha=0.5)
        ax.axhline(y=self.target_balance+1, color='r', linestyle=':', alpha=0.5)
        ax.fill_between([corners['SessionTime'].min(), corners['SessionTime'].max()],
                        self.target_balance-1, self.target_balance+1, alpha=0.2, color='green')
        ax.set_xlabel('Session Time (s)')
        ax.set_ylabel('Balance (deg)')
        ax.set_title('Slip Angle Balance During Cornering\n(Green = Target Zone)')
        ax.legend()
        ax.grid(True, alpha=0.3)

        # Plot 2: Balance distribution
        ax = axes[0, 1]
        ax.hist(corners['balance'], bins=50, edgecolor='black', alpha=0.7)
        ax.axvline(x=self.target_balance, color='r', linestyle='--', linewidth=2, label='Target')
        ax.axvline(x=self.target_balance-1, color='r', linestyle=':', alpha=0.5)
        ax.axvline(x=self.target_balance+1, color='r', linestyle=':', alpha=0.5)
        ax.set_xlabel('Balance (deg)')
        ax.set_ylabel('Frequency')
        ax.set_title('Balance Distribution')
        ax.legend()
        ax.grid(True, alpha=0.3)

        # Plot 3: Input comparison (actual vs what model would predict)
        ax = axes[1, 0]
        sample = corners.iloc[::10]  # Subsample for visibility
        ax.plot(sample['SessionTime'], sample['Throttle'], label='Actual Throttle', alpha=0.7)
        ax.plot(sample['SessionTime'], sample['Brake'], label='Actual Brake', alpha=0.7)
        ax.set_xlabel('Session Time (s)')
        ax.set_ylabel('Input Value')
        ax.set_title('Driver Inputs During Cornering')
        ax.legend()
        ax.grid(True, alpha=0.3)

        # Plot 4: Speed vs Balance (colored by quality)
        ax = axes[1, 1]
        scatter = ax.scatter(corners['Speed'], corners['balance'],
                            c=corners['quality'], cmap='RdYlGn',
                            alpha=0.6, s=10)
        ax.axhline(y=self.target_balance, color='r', linestyle='--', linewidth=2)
        ax.set_xlabel('Speed (m/s)')
        ax.set_ylabel('Balance (deg)')
        ax.set_title('Speed vs Balance (colored by quality)')
        plt.colorbar(scatter, ax=ax, label='Quality')
        ax.grid(True, alpha=0.3)

        plt.tight_layout()
        plt.savefig(save_path, dpi=150, bbox_inches='tight')
        print(f"✓ Saved plots to {save_path}")
        plt.show()

    def save_model(self, path='oversteer_controller.pkl'):
        """Save trained model"""
        if self.models is None:
            raise RuntimeError("No model to save")

        joblib.dump({
            'models': self.models,
            'feature_cols': self.feature_cols,
            'target_balance': self.target_balance,
            'target_cols': self.target_cols
        }, path)

        print(f"\n✓ Saved model to {path}")

    def load_model(self, path='oversteer_controller.pkl'):
        """Load trained model"""
        data = joblib.load(path)
        self.models = data['models']
        self.feature_cols = data['feature_cols']
        self.target_balance = data['target_balance']
        self.target_cols = data['target_cols']

        print(f"✓ Loaded model from {path}")


def main():
    if len(sys.argv) < 2:
        print("Usage: python train_oversteer_controller.py <telemetry_with_slip_angles.csv>")
        print("\nThis script requires telemetry data WITH slip angles calculated.")
        print("Run calculate_slip_angles.py first!")
        print("\nExample workflow:")
        print("  1. Export telemetry: dotnet run --project src/IbtTelemetry.Cli -- export sample.ibt data.csv --all")
        print("  2. Calculate slip angles: python calculate_slip_angles.py data.csv")
        print("  3. Train controller: python train_oversteer_controller.py data_with_slip_angles.csv")
        sys.exit(1)

    csv_path = sys.argv[1]

    # Load telemetry with slip angles
    print(f"Loading telemetry from {csv_path}...")
    try:
        df = pd.read_csv(csv_path)
    except FileNotFoundError:
        print(f"Error: File '{csv_path}' not found")
        sys.exit(1)

    print(f"Loaded {len(df)} samples")

    # Check required columns
    required = ['slip_angle_front_deg', 'slip_angle_rear_deg', 'balance',
                'Speed', 'Throttle', 'Brake', 'SteeringWheelAngle']
    missing = [col for col in required if col not in df.columns]

    if missing:
        print(f"\n❌ Error: Missing required columns: {missing}")
        print("\nMake sure to:")
        print("  1. Export with --all flag")
        print("  2. Run calculate_slip_angles.py first")
        sys.exit(1)

    # Initialize controller
    target_balance = -2.0  # 2 degrees oversteer (adjust as needed)

    print(f"\nInitializing oversteer controller with target balance: {target_balance}°")
    controller = OversteerController(target_balance=target_balance)

    # Prepare training data
    X, y, weights = controller.prepare_training_data(df)

    if len(X) < 50:
        print("\n❌ Error: Not enough training samples")
        print("Collect more laps with good cornering!")
        sys.exit(1)

    # Train model
    controller.train(X, y, sample_weights=weights)

    # Evaluate
    controller.evaluate_oversteer_maintenance(df, None)

    # Plot results
    controller.plot_results(df)

    # Save model
    controller.save_model('oversteer_controller.pkl')

    print("\n" + "=" * 70)
    print("NEXT STEPS")
    print("=" * 70)
    print("1. Review plots to understand model behavior")
    print("2. Collect more training data (50+ laps recommended)")
    print("3. Test model predictions on new laps")
    print("4. Adjust target_balance if needed (-1° to -4°)")
    print("5. Use model for real-time suggestions (if simulator supports)")
    print("\n✓ Training complete!")


if __name__ == '__main__':
    main()
