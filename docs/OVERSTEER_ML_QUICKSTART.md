# Quick Start: Train Oversteer Control ML Model

## Goal

Train an ML model that predicts optimal driving inputs (throttle, brake, steering) to maintain your car in a **slight oversteer state** while cornering, resulting in faster, more controlled cornering.

## What is Oversteer Control?

**Oversteer**: When the rear tires slip more than the front tires, causing the car to rotate/turn more than intended.

**Slip Angle Balance**:
```
Balance = Front Slip Angle - Rear Slip Angle

Balance = -2°  → Rear slipping 2° more (slight oversteer) ✓ TARGET
Balance =  0°  → Neutral (both equal)
Balance = +2°  → Front slipping 2° more (understeer)
```

**Why Slight Oversteer?**
- Faster corner entry (better rotation)
- Reduced understeer (more responsive)
- Better corner exit (point and shoot)
- More engaging driving style

**Risk**: Too much oversteer → spin

## Complete Workflow

### Step 1: Collect Telemetry Data

Drive 10-20 laps on a track, focusing on:
- Clean, fast laps
- Smooth inputs
- Good cornering technique
- Various conditions (fuel loads, tire wear)

```bash
# Your .ibt files will be in:
# Documents/iRacing/telemetry/
```

### Step 2: Export Telemetry

Export with ALL variables (needed for velocity vectors):

```bash
# Single session
dotnet run --project src/IbtTelemetry.Cli -- \
  export ~/Documents/iRacing/telemetry/myrace.ibt telemetry.csv --all

# Multiple sessions (recommended)
dotnet run --project src/IbtTelemetry.Cli -- \
  export ~/Documents/iRacing/telemetry/ telemetry_combined.csv --all
```

### Step 3: Calculate Slip Angles

```bash
cd examples
python calculate_slip_angles.py ../telemetry_combined.csv
```

**Output**: `telemetry_combined_with_slip_angles.csv`

**What this does**:
- Parses velocity vectors (VelocityX, VelocityY)
- Calculates front/rear tire slip angles
- Computes balance (front - rear)
- Plots slip angle characteristics

**Review the plots**:
- Check if you're already maintaining oversteer
- Identify where you're understeering
- See peak slip angles in fast corners

### Step 4: Train Oversteer Controller

```bash
python train_oversteer_controller.py ../telemetry_combined_with_slip_angles.csv
```

**What this does**:
1. Labels sections where you maintained good oversteer
2. Engineers features (current state, rates of change)
3. Trains XGBoost models to predict throttle/brake/steering
4. Evaluates how well it maintains target balance
5. Saves trained model

**Output**:
```
=== OVERSTEER CONTROL EVALUATION ===
Target balance: -2.0°
Mean balance error: 0.85°
Time in target (±1°): 68.2%
Avg corner speed: 32.4 m/s
Dangerous oversteer: 2.1%

✓ Saved model to oversteer_controller.pkl
```

### Step 5: Analyze Results

Review the generated plots:

1. **Balance over time**: Are you maintaining target oversteer?
2. **Balance distribution**: Histogram showing typical balance
3. **Driver inputs**: What throttle/brake/steering patterns work?
4. **Speed vs Balance**: Where oversteer helps most

### Step 6: Improve & Iterate

**If results are poor** (balance error > 2°):
- Collect more training data (30+ laps)
- Try different target balance (-1° to -3°)
- Focus on specific corner types
- Verify slip angle calculations (check vehicle parameters)

**If results are good** (balance error < 1°):
- Test on new laps not in training data
- Compare model predictions to your actual inputs
- Use insights to adjust your driving style
- Train on multiple tracks

## Understanding the Model

### What the Model Learns

The model learns patterns like:

**Corner Entry** (Braking):
- Reduce brake gradually (trail braking)
- Initial steering input to rotate car
- Throttle timing to maintain rotation

**Mid-Corner** (Maintenance):
- Throttle modulation to control oversteer
- Steering adjustments to hold line
- Balance between slip and traction

**Corner Exit** (Acceleration):
- Progressive throttle application
- Steering unwind timing
- Transition from oversteer to traction

### Feature Importance

After training, the model will show which features matter most:

```
Top features for throttle prediction:
  1. balance              (current oversteer state)
  2. Speed                (how fast you're going)
  3. slip_angle_rear_deg  (rear tire slip)
  4. balance_rate         (how fast balance is changing)
  5. curvature            (how tight the corner is)
```

## Using the Trained Model

### Offline Analysis (Recommended)

```python
import joblib
import pandas as pd

# Load model
controller = joblib.load('oversteer_controller.pkl')

# Load new telemetry
new_lap = pd.read_csv('new_lap_with_slip_angles.csv')

# Get model predictions
predictions = controller.predict(new_lap)

# Compare to actual inputs
print("Throttle difference:", (predictions['Throttle'] - new_lap['Throttle']).mean())
```

### Real-time Suggestions (Advanced)

If you can read iRacing telemetry in real-time:

```python
def get_oversteer_suggestions():
    # Get current state from iRacing
    current_state = read_iracing_telemetry()

    # Calculate slip angles
    current_state = calculate_slip_angles(current_state)

    # Get model suggestion
    suggestion = controller.predict(current_state)

    # Display to driver
    print(f"Suggested: Throttle={suggestion['Throttle']:.2f}")
```

## Advanced: Adjusting Target Balance

Different driving styles prefer different balance:

| Style | Target Balance | Characteristics |
|-------|---------------|-----------------|
| Safe/Stable | 0° to -1° | Neutral to slight oversteer, forgiving |
| Balanced | -1° to -2° | Moderate oversteer, fast and controlled |
| Aggressive | -2° to -3° | Strong oversteer, requires skill |
| Drift | -4° to -6° | Extreme oversteer, showmanship |

**Edit the script** to change target:
```python
# In train_oversteer_controller.py
target_balance = -1.5  # Change this value
```

## Expected Performance

With good training data (20+ laps):

| Metric | Good | Excellent |
|--------|------|-----------|
| Balance Error | < 1.5° | < 0.8° |
| Time in Target | > 50% | > 70% |
| Corner Speed | +1-2 m/s | +2-4 m/s |
| Lap Time | -0.5% | -1.5% |

## Troubleshooting

### "Not enough training samples"

**Problem**: < 100 high-quality samples found

**Solutions**:
- Drive more laps focusing on corners
- Lower quality threshold in code
- Collect data from multiple sessions
- Verify slip angles are calculated correctly

### "Model predictions are erratic"

**Problem**: Predictions vary wildly between samples

**Solutions**:
- Increase data collection (need more examples)
- Add more smoothing (increase rolling window)
- Filter outliers from training data
- Check for data quality issues

### "Balance error is high"

**Problem**: Model can't maintain target oversteer

**Solutions**:
- Adjust target balance (try -1° instead of -2°)
- Collect data from better drivers
- Focus on specific corner types
- Verify vehicle parameters (wheelbase, CG)

### "Model always predicts conservative inputs"

**Problem**: Model learns too much from safe sections

**Solutions**:
- Increase quality threshold (only learn from best)
- Weight fast sections more heavily
- Filter out slow/cautious laps
- Add more aggressive driving data

## Next Steps

### Immediate
1. ✅ Run complete workflow on your data
2. ✅ Analyze results and plots
3. ✅ Compare model predictions to your inputs
4. ✅ Identify where you can improve

### Short-term
- Collect 50+ laps for robust training
- Test on multiple tracks
- Compare with other drivers' data
- Refine target balance for your style

### Advanced
- Implement reinforcement learning (see OVERSTEER_CONTROL_ML.md)
- Train track-specific models
- Create real-time driver assist
- Integrate with telemetry overlay

## Safety Reminders

1. **Model is advisory only** - You always have final control
2. **Start conservative** - Begin with target balance around -1°
3. **Test incrementally** - Don't trust model immediately
4. **Validate predictions** - Compare to known good inputs
5. **Track conditions vary** - Model trained on specific conditions

## References

- [OVERSTEER_CONTROL_ML.md](OVERSTEER_CONTROL_ML.md): Detailed methodology
- [SLIP_ANGLE_CALCULATION.md](SLIP_ANGLE_CALCULATION.md): Slip angle theory
- [ML_TELEMETRY_GUIDE.md](../ML_TELEMETRY_GUIDE.md): General ML guide

## Success Stories

Once trained, you should see:
- ✓ More consistent cornering balance
- ✓ Faster corner speeds
- ✓ Reduced lap time variance
- ✓ Better understanding of optimal inputs
- ✓ Improved racecraft

Good luck, and enjoy the oversteer! 🏎️💨
