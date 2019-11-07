using UnityEngine;
using UnityEngine.UI;

namespace Bose.Wearable
{
	/// <summary>
	/// <see cref="DataDebugUIWindow"/> is a debug window for showing the device's metrics.
	/// </summary>
	internal sealed class DataDebugUIWindow : MonoBehaviour
	{
		[Header("Accelerometer UI Refs")]
		[SerializeField]
		private Image _accelerometerTitleImage;

		[SerializeField]
		private Text _accelerometerTitleText;

		[SerializeField]
		private Text _accelerometerX;

		[SerializeField]
		private Text _accelerometerY;

		[SerializeField]
		private Text _accelerometerZ;

		[Header("Gyroscope UI Refs")]
		[SerializeField]
		private Image _gyroscopeTitleImage;

		[SerializeField]
		private Text _gyroscopeTitleText;

		[SerializeField]
		private Text _gyroscopeX;

		[SerializeField]
		private Text _gyroscopeY;

		[SerializeField]
		private Text _gyroscopeZ;

		[Header("Rotation UI Refs")]
		[SerializeField]
		private RotationDataToggleDebugUIControl _rotationControlNineDof;

		[SerializeField]
		private RotationDataToggleDebugUIControl _rotationControlSixDof;

		[Header("Style Data Refs")]
		[SerializeField]
		private WearableUIColorPalette _colorPalette;

		private WearableControl _wearableControl;


		private void Start()
		{
			_wearableControl = WearableControl.Instance;
		}

		private void Update()
		{
			UpdateUI(_wearableControl.LastSensorFrame);
		}

		private void UpdateUI(SensorFrame frame)
		{
			var currentDeviceConfig = _wearableControl.CurrentDeviceConfig;
			var style = _colorPalette.GetCustomizedActiveStyle();

			// Update Accelerometer
			var isAccelerometerEnabled = currentDeviceConfig.GetSensorConfig(SensorId.Accelerometer).isEnabled;
			var accelChildElementColor = isAccelerometerEnabled
				? _colorPalette.ActiveDataTextColor
				: _colorPalette.InactiveDataTextColor;

			Color accelTitleTextColor;
			Color accelTitleElementColor;
			if (isAccelerometerEnabled)
			{
				accelTitleTextColor = style.textColor;
				accelTitleElementColor = style.elementColor;
			}
			else
			{
				accelTitleTextColor = _colorPalette.InactiveTitleElementStyle.textColor;
				accelTitleElementColor = _colorPalette.InactiveTitleElementStyle.elementColor;
			}

			_accelerometerTitleText.color = accelTitleTextColor;
			_accelerometerTitleImage.color = accelTitleElementColor;

			_accelerometerX.color = accelChildElementColor;
			_accelerometerX.text = string.Format(
				frame.acceleration.value.x >= 0
					? DebuggingConstants.DataComponentFormatPositive
					: DebuggingConstants.DataComponentFormatNegative,
				DebuggingConstants.XField,
				Mathf.Abs(frame.acceleration.value.x));

			_accelerometerY.color = accelChildElementColor;
			_accelerometerY.text = string.Format(
				frame.acceleration.value.y >= 0
					? DebuggingConstants.DataComponentFormatPositive
					: DebuggingConstants.DataComponentFormatNegative,
					DebuggingConstants.YField,
					Mathf.Abs(frame.acceleration.value.y));

			_accelerometerZ.color = accelChildElementColor;
			_accelerometerZ.text = string.Format(
				frame.acceleration.value.z >= 0
					? DebuggingConstants.DataComponentFormatPositive
					: DebuggingConstants.DataComponentFormatNegative,
					DebuggingConstants.ZField,
					Mathf.Abs(frame.acceleration.value.z));

			// Update Gyroscope
			var isGyroscopeEnabled = currentDeviceConfig.GetSensorConfig(SensorId.Gyroscope).isEnabled;
			var gyroChildElementColor = isGyroscopeEnabled
				? _colorPalette.ActiveDataTextColor
				: _colorPalette.InactiveDataTextColor;

			Color gyroTitleTextColor;
			Color gyroTitleElementColor;
			if (isGyroscopeEnabled)
			{
				gyroTitleTextColor = style.textColor;
				gyroTitleElementColor = style.elementColor;
			}
			else
			{
				gyroTitleTextColor = _colorPalette.InactiveTitleElementStyle.textColor;
				gyroTitleElementColor = _colorPalette.InactiveTitleElementStyle.elementColor;
			}

			_gyroscopeTitleText.color = gyroTitleTextColor;
			_gyroscopeTitleImage.color = gyroTitleElementColor;

			_gyroscopeX.color = gyroChildElementColor;
			_gyroscopeX.text = string.Format(
				frame.angularVelocity.value.x >= 0
					? DebuggingConstants.DataComponentFormatPositive
					: DebuggingConstants.DataComponentFormatNegative,
					DebuggingConstants.XField,
					Mathf.Abs(frame.angularVelocity.value.x));

			_gyroscopeY.color = gyroChildElementColor;
			_gyroscopeY.text = string.Format(
				frame.angularVelocity.value.y >= 0
					? DebuggingConstants.DataComponentFormatPositive
					: DebuggingConstants.DataComponentFormatNegative,
					DebuggingConstants.YField,
					Mathf.Abs(frame.angularVelocity.value.y));

			_gyroscopeZ.color = gyroChildElementColor;
			_gyroscopeZ.text = string.Format(
				frame.angularVelocity.value.z >= 0
					? DebuggingConstants.DataComponentFormatPositive
					: DebuggingConstants.DataComponentFormatNegative,
					DebuggingConstants.ZField,
					Mathf.Abs(frame.angularVelocity.value.z));

			_rotationControlNineDof.UpdateUI(frame.rotationNineDof);
			_rotationControlSixDof.UpdateUI(frame.rotationSixDof);
		}
	}
}
