#!/usr/bin/env python3
"""
Calculate tire slip angles from iRacing telemetry

Requirements:
    pip install pandas numpy matplotlib
"""

import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
import sys


class SlipAngleCalculator:
    def __init__(self, wheelbase=2.7, track_width=1.6, cg_position_pct=0.45, steering_ratio=12.0):
        """
        Args:
            wheelbase: Total wheelbase (m) - typical: 2.5-3.0m
            track_width: Track width (m) - typical: 1.5-1.8m
            cg_position_pct: CG position as % from front (0.45 = 45% from front)
            steering_ratio: Steering wheel to road wheel ratio (12:1 typical)
        """
        self.wheelbase = wheelbase
        self.track_width = track_width
        self.wheelbase_front = wheelbase * cg_position_pct
        self.wheelbase_rear = wheelbase * (1 - cg_position_pct)
        self.steering_ratio = steering_ratio

        print(f"Vehicle parameters:")
        print(f"  Wheelbase: {wheelbase:.2f}m")
        print(f"  Front: {self.wheelbase_front:.2f}m, Rear: {self.wheelbase_rear:.2f}m")
        print(f"  Track width: {track_width:.2f}m")
        print(f"  Steering ratio: {steering_ratio:.1f}:1")

    def parse_velocity_array(self, velocity_str):
        """Parse velocity array from CSV format: '[v1;v2;v3;v4;v5;v6]'"""
        if pd.isna(velocity_str) or velocity_str == '':
            return None

        # Remove brackets and quotes, split by semicolon
        cleaned = str(velocity_str).strip('[]"').replace('"', '').split(';')
        try:
            values = [float(v) for v in cleaned if v.strip()]
            # Return most recent value (last in array)
            return values[-1] if values else None
        except (ValueError, IndexError):
            return None

    def calculate_slip_angles(self, df):
        """
        Calculate slip angles for entire dataframe

        Args:
            df: DataFrame with telemetry data

        Returns:
            DataFrame with added slip angle columns
        """
        print("\nCalculating slip angles...")

        # Parse velocity arrays (take most recent sample from array)
        print("  Parsing velocity data...")
        df['vx'] = df['VelocityX'].apply(self.parse_velocity_array)
        df['vy'] = df['VelocityY'].apply(self.parse_velocity_array)
        df['yaw_rate'] = df['YawRate'].apply(self.parse_velocity_array)

        # Fill any NaN values with 0
        df['vx'] = df['vx'].fillna(0)
        df['vy'] = df['vy'].fillna(0)
        df['yaw_rate'] = df['yaw_rate'].fillna(0)

        # Convert steering wheel angle to road wheel angle
        df['steering_angle'] = df['SteeringWheelAngle'] / self.steering_ratio

        print("  Computing slip angles...")

        # Calculate body slip angle (beta) - vehicle's sideslip
        with np.errstate(divide='ignore', invalid='ignore'):
            df['body_slip_angle'] = np.arctan2(df['vy'], df['vx'])
            df['body_slip_angle'] = df['body_slip_angle'].fillna(0)

        # Front slip angle
        df['slip_angle_front'] = self.calc_front_slip(
            df['vx'].values,
            df['vy'].values,
            df['yaw_rate'].values,
            df['steering_angle'].values
        )

        # Rear slip angle
        df['slip_angle_rear'] = self.calc_rear_slip(
            df['vx'].values,
            df['vy'].values,
            df['yaw_rate'].values
        )

        # Convert to degrees for readability
        df['slip_angle_front_deg'] = np.degrees(df['slip_angle_front'])
        df['slip_angle_rear_deg'] = np.degrees(df['slip_angle_rear'])
        df['body_slip_angle_deg'] = np.degrees(df['body_slip_angle'])

        # Calculate balance metric (positive = understeer, negative = oversteer)
        df['balance'] = df['slip_angle_front_deg'] - df['slip_angle_rear_deg']

        print(f"✓ Calculated slip angles for {len(df)} samples")
        return df

    def calc_front_slip(self, vx, vy, yaw_rate, steering_angle):
        """Vectorized front slip angle calculation"""
        result = np.zeros_like(vx, dtype=float)

        # Only calculate where speed > threshold (avoid division by zero)
        valid_mask = np.abs(vx) > 0.5

        vy_front = vy + yaw_rate * self.wheelbase_front
        beta = np.arctan2(vy, vx)

        with np.errstate(divide='ignore', invalid='ignore'):
            result[valid_mask] = (
                steering_angle[valid_mask] -
                beta[valid_mask] -
                (yaw_rate[valid_mask] * self.wheelbase_front / vx[valid_mask])
            )

        return result

    def calc_rear_slip(self, vx, vy, yaw_rate):
        """Vectorized rear slip angle calculation"""
        result = np.zeros_like(vx, dtype=float)

        valid_mask = np.abs(vx) > 0.5

        vy_rear = vy - yaw_rate * self.wheelbase_rear

        with np.errstate(divide='ignore', invalid='ignore'):
            result[valid_mask] = -np.arctan2(vy_rear[valid_mask], vx[valid_mask])

        return result

    def plot_slip_angles(self, df, save_path='slip_angles.png'):
        """Plot slip angles over time and characteristics"""
        print("\nGenerating plots...")

        # Filter to only when moving (speed > 5 m/s)
        df_moving = df[df['Speed'] > 5].copy()

        if len(df_moving) == 0:
            print("Warning: No data with speed > 5 m/s")
            return

        # Parse lateral acceleration if available
        if 'LatAccel' in df.columns:
            df_moving['lat_accel'] = df_moving['LatAccel'].apply(self.parse_velocity_array)
        else:
            df_moving['lat_accel'] = 0

        fig, axes = plt.subplots(2, 2, figsize=(16, 10))

        # Plot 1: Slip angles over time
        ax = axes[0, 0]
        ax.plot(df_moving['SessionTime'], df_moving['slip_angle_front_deg'],
                label='Front Slip Angle', linewidth=1.5, alpha=0.8)
        ax.plot(df_moving['SessionTime'], df_moving['slip_angle_rear_deg'],
                label='Rear Slip Angle', linewidth=1.5, alpha=0.8)
        ax.set_xlabel('Session Time (s)')
        ax.set_ylabel('Slip Angle (deg)')
        ax.set_title('Tire Slip Angles Over Time')
        ax.legend()
        ax.grid(True, alpha=0.3)
        ax.axhline(y=0, color='k', linestyle='--', alpha=0.3)

        # Plot 2: Slip angle vs lateral acceleration (tire characteristic)
        ax = axes[0, 1]
        # Subsample for plotting (every 10th point)
        subsample = df_moving.iloc[::10]
        ax.scatter(subsample['slip_angle_front_deg'], subsample['lat_accel'],
                  alpha=0.6, s=15, label='Front', color='blue')
        ax.scatter(subsample['slip_angle_rear_deg'], subsample['lat_accel'],
                  alpha=0.6, s=15, label='Rear', color='red')
        ax.set_xlabel('Slip Angle (deg)')
        ax.set_ylabel('Lateral Acceleration (m/s²)')
        ax.set_title('Tire Characteristic Curve\n(Slip Angle vs Lateral Acceleration)')
        ax.legend()
        ax.grid(True, alpha=0.3)
        ax.axvline(x=0, color='k', linestyle='--', alpha=0.3)
        ax.axhline(y=0, color='k', linestyle='--', alpha=0.3)

        # Plot 3: Balance over time
        ax = axes[1, 0]
        ax.plot(df_moving['SessionTime'], df_moving['balance'],
                linewidth=1.5, color='purple')
        ax.fill_between(df_moving['SessionTime'], 0, df_moving['balance'],
                        where=(df_moving['balance'] > 0), alpha=0.3,
                        color='red', label='Understeer')
        ax.fill_between(df_moving['SessionTime'], 0, df_moving['balance'],
                        where=(df_moving['balance'] < 0), alpha=0.3,
                        color='blue', label='Oversteer')
        ax.set_xlabel('Session Time (s)')
        ax.set_ylabel('Balance (deg)')
        ax.set_title('Vehicle Balance\n(Front - Rear Slip Angle)')
        ax.legend()
        ax.grid(True, alpha=0.3)
        ax.axhline(y=0, color='k', linestyle='-', linewidth=2, alpha=0.5)

        # Plot 4: Slip angle distribution
        ax = axes[1, 1]
        ax.hist(df_moving['slip_angle_front_deg'], bins=50, alpha=0.6,
               label='Front', color='blue', edgecolor='black')
        ax.hist(df_moving['slip_angle_rear_deg'], bins=50, alpha=0.6,
               label='Rear', color='red', edgecolor='black')
        ax.set_xlabel('Slip Angle (deg)')
        ax.set_ylabel('Frequency')
        ax.set_title('Slip Angle Distribution')
        ax.legend()
        ax.grid(True, alpha=0.3)
        ax.axvline(x=0, color='k', linestyle='--', alpha=0.5)

        plt.tight_layout()
        plt.savefig(save_path, dpi=150, bbox_inches='tight')
        print(f"✓ Saved plots to {save_path}")
        plt.show()

    def print_statistics(self, df):
        """Print slip angle statistics"""
        df_moving = df[df['Speed'] > 5].copy()

        if len(df_moving) == 0:
            print("\nNo data with speed > 5 m/s")
            return

        print("\n" + "=" * 70)
        print("SLIP ANGLE STATISTICS (Speed > 5 m/s)")
        print("=" * 70)

        print(f"\n{'Metric':<35} {'Front':>15} {'Rear':>15}")
        print("-" * 70)

        metrics = [
            ('Mean (deg)', lambda x: x.mean()),
            ('Std Dev (deg)', lambda x: x.std()),
            ('Max Absolute (deg)', lambda x: x.abs().max()),
            ('95th Percentile Abs (deg)', lambda x: x.abs().quantile(0.95)),
            ('Max Positive (deg)', lambda x: x.max()),
            ('Max Negative (deg)', lambda x: x.min()),
        ]

        for metric_name, func in metrics:
            front_val = func(df_moving['slip_angle_front_deg'])
            rear_val = func(df_moving['slip_angle_rear_deg'])
            print(f"{metric_name:<35} {front_val:>15.2f} {rear_val:>15.2f}")

        # Balance statistics
        print("\n" + "-" * 70)
        print("BALANCE METRICS")
        print("-" * 70)

        balance_mean = df_moving['balance'].mean()
        understeer_pct = (df_moving['balance'] > 0.5).sum() / len(df_moving) * 100
        oversteer_pct = (df_moving['balance'] < -0.5).sum() / len(df_moving) * 100
        neutral_pct = 100 - understeer_pct - oversteer_pct

        print(f"Average balance: {balance_mean:+.2f}° "
              f"({'UNDERSTEER' if balance_mean > 0 else 'OVERSTEER' if balance_mean < 0 else 'NEUTRAL'})")
        print(f"Understeer time: {understeer_pct:.1f}%")
        print(f"Oversteer time:  {oversteer_pct:.1f}%")
        print(f"Neutral time:    {neutral_pct:.1f}%")

        # Typical ranges
        print("\n" + "-" * 70)
        print("INTERPRETATION")
        print("-" * 70)
        print("Typical slip angle ranges:")
        print("  Straight/mild: 0-4°")
        print("  Cornering:     4-10°")
        print("  At limit:      10-15°")
        print(f"\nYour max slip angles: Front={df_moving['slip_angle_front_deg'].abs().max():.1f}°, "
              f"Rear={df_moving['slip_angle_rear_deg'].abs().max():.1f}°")


def main():
    if len(sys.argv) < 2:
        print("Usage: python calculate_slip_angles.py <telemetry.csv>")
        print("\nExample:")
        print("  python calculate_slip_angles.py telemetry_ml.csv")
        sys.exit(1)

    csv_path = sys.argv[1]

    # Load telemetry data
    print(f"Loading telemetry from {csv_path}...")
    try:
        df = pd.read_csv(csv_path)
    except FileNotFoundError:
        print(f"Error: File '{csv_path}' not found")
        sys.exit(1)

    print(f"Loaded {len(df)} samples")

    # Check required columns
    required_cols = ['VelocityX', 'VelocityY', 'YawRate', 'SteeringWheelAngle', 'Speed']
    missing = [col for col in required_cols if col not in df.columns]
    if missing:
        print(f"\nError: Missing required columns: {missing}")
        print("\nMake sure to export with --all flag:")
        print("  dotnet run --project src/IbtTelemetry.Cli -- export sample.ibt telemetry.csv --all")
        sys.exit(1)

    # Initialize calculator
    # Adjust these parameters for your specific car!
    calc = SlipAngleCalculator(
        wheelbase=2.7,          # Total wheelbase in meters
        track_width=1.6,        # Track width in meters
        cg_position_pct=0.45,   # CG at 45% from front
        steering_ratio=12.0     # 12:1 steering ratio
    )

    # Calculate slip angles
    df = calc.calculate_slip_angles(df)

    # Print statistics
    calc.print_statistics(df)

    # Plot results
    calc.plot_slip_angles(df)

    # Export with slip angles
    output_path = csv_path.replace('.csv', '_with_slip_angles.csv')
    df.to_csv(output_path, index=False)
    print(f"\n✓ Exported data with slip angles to {output_path}")

    print("\n" + "=" * 70)
    print("NEXT STEPS")
    print("=" * 70)
    print("1. Verify vehicle parameters (wheelbase, track width, CG position)")
    print("2. Compare slip angles between different setups")
    print("3. Identify optimal slip angle for max grip")
    print("4. Analyze balance (understeer vs oversteer)")
    print("5. Use insights to optimize car setup")


if __name__ == '__main__':
    main()
