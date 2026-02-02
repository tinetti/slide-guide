# Tire Slip Angle Calculation from iRacing Telemetry

## Overview

**Slip angle** (α) is the angle between the direction a tire is pointing and the direction it is actually traveling. It's a critical parameter in vehicle dynamics, tire modeling, and race engineering.

```
           Tire Direction (heading)
                    ↑
                    |  ← Slip Angle (α)
                    | /
                    |/
         Travel Direction →
```

## Available Data in iRacing Telemetry

### Key Variables

| Variable | Type | Unit | Description |
|----------|------|------|-------------|
| **VelocityX** | Float[6] | m/s | Longitudinal velocity (forward/back) |
| **VelocityY** | Float[6] | m/s | Lateral velocity (left/right) |
| **VelocityZ** | Float[6] | m/s | Vertical velocity (up/down) |
| **YawRate** | Float[6] | rad/s | Rate of rotation about vertical axis |
| **SteeringWheelAngle** | Float | rad | Steering wheel angle |
| **Speed** | Float | m/s | GPS vehicle speed (magnitude) |
| **LFspeed** | Float | m/s | Left front wheel speed |
| **RFspeed** | Float | m/s | Right front wheel speed |
| **LRspeed** | Float | m/s | Left rear wheel speed |
| **RRspeed** | Float | m/s | Right rear wheel speed |
| **Yaw** | Float | rad | Yaw angle |

## Calculation Methods

### Method 1: Velocity-Based Slip Angle (Most Accurate)

This method uses the vehicle's velocity vector to calculate slip angle.

#### Front Wheels

```python
import numpy as np

def calculate_front_slip_angle(vx, vy, yaw_rate, steering_angle, wheelbase_front):
    """
    Calculate front tire slip angle

    Args:
        vx: Longitudinal velocity (m/s)
        vy: Lateral velocity (m/s)
        yaw_rate: Yaw rate (rad/s)
        steering_angle: Steering wheel angle (rad)
        wheelbase_front: Distance from CG to front axle (m)

    Returns:
        Front slip angle (rad)
    """
    # Velocity at front axle
    vy_front = vy + yaw_rate * wheelbase_front

    # Vehicle slip angle (beta)
    if abs(vx) < 0.1:  # Avoid division by zero
        return 0.0

    beta = np.arctan2(vy, vx)

    # Front slip angle = steering angle - (beta + yaw contribution)
    alpha_front = steering_angle - (beta + (yaw_rate * wheelbase_front / vx))

    return alpha_front


def calculate_front_slip_angle_simple(vx, vy, steering_angle):
    """
    Simplified front slip angle (ignores yaw rate)

    Args:
        vx: Longitudinal velocity (m/s)
        vy: Lateral velocity (m/s)
        steering_angle: Steering angle at wheels (rad)

    Returns:
        Front slip angle (rad)
    """
    if abs(vx) < 0.1:
        return 0.0

    # Body slip angle
    beta = np.arctan2(vy, vx)

    # Front slip angle
    alpha_front = steering_angle - beta

    return alpha_front
```

#### Rear Wheels

```python
def calculate_rear_slip_angle(vx, vy, yaw_rate, wheelbase_rear):
    """
    Calculate rear tire slip angle

    Args:
        vx: Longitudinal velocity (m/s)
        vy: Lateral velocity (m/s)
        yaw_rate: Yaw rate (rad/s)
        wheelbase_rear: Distance from CG to rear axle (m)

    Returns:
        Rear slip angle (rad)
    """
    # Velocity at rear axle
    vy_rear = vy - yaw_rate * wheelbase_rear

    if abs(vx) < 0.1:
        return 0.0

    # Rear slip angle = -arctan(vy_rear / vx)
    alpha_rear = -np.arctan2(vy_rear, vx)

    return alpha_rear
```

#### Left vs Right Slip Angles

For individual wheel slip angles (accounting for track width):

```python
def calculate_individual_slip_angles(vx, vy, yaw_rate, steering_angle,
                                     wheelbase_front, wheelbase_rear,
                                     track_width):
    """
    Calculate slip angle for each wheel

    Returns:
        dict: {'LF': alpha_lf, 'RF': alpha_rf, 'LR': alpha_lr, 'RR': alpha_rr}
    """
    half_track = track_width / 2

    if abs(vx) < 0.1:
        return {'LF': 0, 'RF': 0, 'LR': 0, 'RR': 0}

    # Left front
    vy_lf = vy + yaw_rate * wheelbase_front + yaw_rate * half_track
    alpha_lf = steering_angle - np.arctan2(vy_lf, vx)

    # Right front
    vy_rf = vy + yaw_rate * wheelbase_front - yaw_rate * half_track
    alpha_rf = steering_angle - np.arctan2(vy_rf, vx)

    # Left rear
    vy_lr = vy - yaw_rate * wheelbase_rear + yaw_rate * half_track
    alpha_lr = -np.arctan2(vy_lr, vx)

    # Right rear
    vy_rr = vy - yaw_rate * wheelbase_rear - yaw_rate * half_track
    alpha_rr = -np.arctan2(vy_rr, vx)

    return {
        'LF': alpha_lf,
        'RF': alpha_rf,
        'LR': alpha_lr,
        'RR': alpha_rr
    }
```

### Method 2: Wheel Speed-Based (Longitudinal Slip Only)

This method uses individual wheel speeds to calculate longitudinal slip ratio:

```python
def calculate_longitudinal_slip(wheel_speed, vehicle_speed, wheel_radius=0.33):
    """
    Calculate longitudinal slip ratio

    Args:
        wheel_speed: Wheel rotational speed (m/s surface speed)
        vehicle_speed: Vehicle speed (m/s)
        wheel_radius: Effective wheel radius (m)

    Returns:
        Longitudinal slip ratio (dimensionless)
    """
    if abs(vehicle_speed) < 0.1:
        return 0.0

    # Slip ratio = (wheel_speed - vehicle_speed) / vehicle_speed
    slip_ratio = (wheel_speed - vehicle_speed) / vehicle_speed

    return slip_ratio
```

## Complete Implementation

### Python Script

```python
#!/usr/bin/env python3
"""
Calculate tire slip angles from iRacing telemetry
"""

import pandas as pd
import numpy as np
import matplotlib.pyplot as plt


class SlipAngleCalculator:
    def __init__(self, wheelbase=2.7, track_width=1.6):
        """
        Args:
            wheelbase: Total wheelbase (m) - typical: 2.5-3.0m
            track_width: Track width (m) - typical: 1.5-1.8m
        """
        self.wheelbase = wheelbase
        self.track_width = track_width

        # Assume CG is 45% of wheelbase from front (typical for sports cars)
        self.wheelbase_front = wheelbase * 0.45
        self.wheelbase_rear = wheelbase * 0.55

        # Steering ratio (wheel angle to road wheel angle)
        # Typical: 12-18:1 for road cars, 10-12:1 for race cars
        self.steering_ratio = 12.0

    def parse_velocity_array(self, velocity_str):
        """Parse velocity array from CSV format: '[v1;v2;v3;v4;v5;v6]'"""
        if pd.isna(velocity_str) or velocity_str == '':
            return None

        # Remove brackets and quotes, split by semicolon
        cleaned = str(velocity_str).strip('[]"').split(';')
        values = [float(v) for v in cleaned if v]

        # Return most recent value (last in array)
        return values[-1] if values else None

    def calculate_slip_angles(self, df):
        """
        Calculate slip angles for entire dataframe

        Args:
            df: DataFrame with telemetry data

        Returns:
            DataFrame with added slip angle columns
        """
        print("Calculating slip angles...")

        # Parse velocity arrays (take most recent sample from array)
        df['vx'] = df['VelocityX'].apply(self.parse_velocity_array)
        df['vy'] = df['VelocityY'].apply(self.parse_velocity_array)
        df['yaw_rate'] = df['YawRate'].apply(self.parse_velocity_array)

        # Convert steering wheel angle to road wheel angle
        df['steering_angle'] = df['SteeringWheelAngle'] / self.steering_ratio

        # Calculate body slip angle (beta)
        df['body_slip_angle'] = np.arctan2(df['vy'], df['vx'])

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

        print(f"✓ Calculated slip angles for {len(df)} samples")
        return df

    def calc_front_slip(self, vx, vy, yaw_rate, steering_angle):
        """Vectorized front slip angle calculation"""
        result = np.zeros_like(vx)

        # Only calculate where speed > threshold
        valid_mask = np.abs(vx) > 0.5

        vy_front = vy + yaw_rate * self.wheelbase_front
        beta = np.arctan2(vy, vx)

        result[valid_mask] = (
            steering_angle[valid_mask] -
            beta[valid_mask] -
            (yaw_rate[valid_mask] * self.wheelbase_front / vx[valid_mask])
        )

        return result

    def calc_rear_slip(self, vx, vy, yaw_rate):
        """Vectorized rear slip angle calculation"""
        result = np.zeros_like(vx)

        valid_mask = np.abs(vx) > 0.5

        vy_rear = vy - yaw_rate * self.wheelbase_rear
        result[valid_mask] = -np.arctan2(vy_rear[valid_mask], vx[valid_mask])

        return result

    def plot_slip_angles(self, df, save_path='slip_angles.png'):
        """Plot slip angles over time"""
        fig, axes = plt.subplots(3, 1, figsize=(14, 10))

        # Filter to only when moving
        df_moving = df[df['Speed'] > 5]

        # Plot 1: Slip angles
        axes[0].plot(df_moving['SessionTime'], df_moving['slip_angle_front_deg'],
                    label='Front Slip Angle', linewidth=1.5)
        axes[0].plot(df_moving['SessionTime'], df_moving['slip_angle_rear_deg'],
                    label='Rear Slip Angle', linewidth=1.5)
        axes[0].plot(df_moving['SessionTime'], df_moving['body_slip_angle_deg'],
                    label='Body Slip Angle', linewidth=1, alpha=0.7)
        axes[0].set_ylabel('Slip Angle (deg)')
        axes[0].set_title('Tire Slip Angles Over Time')
        axes[0].legend()
        axes[0].grid(True, alpha=0.3)
        axes[0].axhline(y=0, color='k', linestyle='--', alpha=0.3)

        # Plot 2: Slip angle vs lateral acceleration
        axes[1].scatter(df_moving['slip_angle_front_deg'],
                       df_moving['LatAccel'].apply(self.parse_velocity_array),
                       alpha=0.5, s=10, label='Front')
        axes[1].scatter(df_moving['slip_angle_rear_deg'],
                       df_moving['LatAccel'].apply(self.parse_velocity_array),
                       alpha=0.5, s=10, label='Rear')
        axes[1].set_xlabel('Slip Angle (deg)')
        axes[1].set_ylabel('Lateral Acceleration (m/s²)')
        axes[1].set_title('Slip Angle vs Lateral Acceleration (Tire Characteristic Curve)')
        axes[1].legend()
        axes[1].grid(True, alpha=0.3)
        axes[1].axvline(x=0, color='k', linestyle='--', alpha=0.3)
        axes[1].axhline(y=0, color='k', linestyle='--', alpha=0.3)

        # Plot 3: Speed vs slip angles
        axes[2].scatter(df_moving['Speed'], df_moving['slip_angle_front_deg'],
                       alpha=0.5, s=10, label='Front', c=df_moving['SteeringWheelAngle'])
        axes[2].set_xlabel('Speed (m/s)')
        axes[2].set_ylabel('Slip Angle (deg)')
        axes[2].set_title('Speed vs Front Slip Angle (colored by steering input)')
        axes[2].colorbar(label='Steering Angle (rad)')
        axes[2].grid(True, alpha=0.3)

        plt.tight_layout()
        plt.savefig(save_path, dpi=150, bbox_inches='tight')
        print(f"✓ Saved plot to {save_path}")
        plt.show()

    def print_statistics(self, df):
        """Print slip angle statistics"""
        df_moving = df[df['Speed'] > 5]

        print("\n=== Slip Angle Statistics (Speed > 5 m/s) ===\n")
        print(f"{'Metric':<30} {'Front':<15} {'Rear':<15}")
        print("-" * 60)

        for metric, func in [
            ('Mean (deg)', lambda x: x.mean()),
            ('Std Dev (deg)', lambda x: x.std()),
            ('Max Absolute (deg)', lambda x: x.abs().max()),
            ('95th Percentile (deg)', lambda x: x.abs().quantile(0.95))
        ]:
            front_val = func(df_moving['slip_angle_front_deg'])
            rear_val = func(df_moving['slip_angle_rear_deg'])
            print(f"{metric:<30} {front_val:>14.2f} {rear_val:>14.2f}")


def main():
    # Load telemetry data
    csv_path = 'telemetry_ml.csv'
    print(f"Loading telemetry from {csv_path}...")
    df = pd.read_csv(csv_path)

    # Initialize calculator (adjust wheelbase/track for your car)
    calc = SlipAngleCalculator(
        wheelbase=2.7,      # meters (adjust for your car)
        track_width=1.6     # meters (adjust for your car)
    )

    # Calculate slip angles
    df = calc.calculate_slip_angles(df)

    # Print statistics
    calc.print_statistics(df)

    # Plot results
    calc.plot_slip_angles(df)

    # Export with slip angles
    output_path = 'telemetry_with_slip_angles.csv'
    df.to_csv(output_path, index=False)
    print(f"\n✓ Exported data with slip angles to {output_path}")


if __name__ == '__main__':
    main()
```

## Important Considerations

### 1. **Vehicle Parameters**

You need to know or estimate:
- **Wheelbase**: Distance between front and rear axles (~2.5-3.0m for race cars)
- **Track width**: Width between left and right wheels (~1.5-1.8m)
- **CG position**: Usually 40-50% from front axle
- **Steering ratio**: Wheel angle to road wheel angle (10-18:1)

These can be found in:
- Car setup screens
- iRacing forums/documentation
- Real-world car specifications

### 2. **Coordinate System**

iRacing uses:
- **VelocityX**: Positive = forward
- **VelocityY**: Positive = right (passenger side)
- **VelocityZ**: Positive = up

### 3. **Accuracy Limitations**

**Good accuracy when:**
- Speed > 5 m/s (low-speed data is noisy)
- Smooth driving (not during impacts/off-track)
- Steady-state or mild transients

**Reduced accuracy:**
- Very low speeds (< 5 m/s)
- High-frequency transients (curbs, bumps)
- Extreme slip angles (> 15°, tire model nonlinearities)

### 4. **Typical Slip Angle Ranges**

| Condition | Front Slip Angle | Rear Slip Angle |
|-----------|------------------|-----------------|
| Straight line | 0° | 0° |
| Mild cornering | 2-4° | 1-3° |
| Hard cornering | 6-10° | 4-8° |
| At limit | 10-15° | 8-12° |
| Oversteering | < rear | > front |
| Understeering | > rear | < front |

## Applications

### 1. **Setup Optimization**
- Compare slip angles with different setups
- Identify under/oversteer balance
- Optimize tire pressures for peak slip angle

### 2. **Driver Analysis**
- Compare slip angle usage between drivers
- Identify driving style (smooth vs aggressive)
- Coach optimal slip angle management

### 3. **Tire Model Validation**
- Plot slip angle vs lateral force curve
- Identify peak slip angle (max lateral grip)
- Validate tire temperature effects

### 4. **Vehicle Dynamics Study**
- Understand weight transfer effects
- Analyze balance changes during lap
- Optimize suspension geometry

## Next Steps

1. **Collect data**: Export telemetry with velocity variables
2. **Estimate parameters**: Research your car's wheelbase/track width
3. **Run calculator**: Execute slip angle calculation script
4. **Analyze results**: Compare front/rear balance, find optimal angles
5. **Iterate setup**: Use insights to optimize car balance

## References

- Milliken & Milliken: "Race Car Vehicle Dynamics"
- Pacejka: "Tire and Vehicle Dynamics"
- iRacing Forums: Vehicle specifications and setup guides
