using Bose.Wearable.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Bose.Wearable.Editor.Inspectors
{
	[CustomPropertyDrawer(typeof(WearableDebugProvider))]
	public sealed class WearableDebugProviderDrawer : PropertyDrawer
	{
		private const string DeviceNameField = "_name";
		private const string FirmwareVersionField = "_firmwareVersion";
		private const string BoseArEnabledField = "_boseArEnabled";
		private const string FirmwareUpdateAvailableField = "_firmwareUpdateAvailable";
		private const string AcceptSecurePairingField = "_acceptSecurePairing";
		private const string RSSIField = "_rssi";
		private const string UIDField = "_uid";
		private const string ProductIdField = "_productId";
		private const string VariantIdField = "_variantId";
		private const string SensorFlagsField = "_availableSensors";
		private const string GestureFlagsField = "_availableGestures";
		private const string VerboseField = "_verbose";
		private const string DelayTimeField = "_simulatedDelayTime";
		private const string MovementSimulationModeField = "_simulatedMovementMode";
		private const string RotationTypeField = "_rotationType";
		private const string EulerSpinRateField = "_eulerSpinRate";
		private const string AxisAngleSpinRateField = "_axisAngleSpinRate";
		private const string DynamicDeviceInfoField = "_dynamicDeviceInfo";

		private const string RotationTypeEuler = "Euler";
		private const string RotationTypeAxisAngle = "AxisAngle";

		private const string DescriptionBox =
			"Provides a minimal data provider that allows connection to a virtual device, and " +
			"logs messages when provider methods are called. If Simulate Movement is enabled, data " +
			"will be generated for all enabled sensors.";

		private const string ProductTypeLabel = "Product Type";
		private const string VariantTypeLabel = "Variant Type";
		private const string GesturesLabel = "Simulate Gesture";
		private const string DisconnectLabel = "Simulate Device Disconnect";
		private const string EulerRateBox =
			"Simulates device rotation by changing each Euler angle (pitch, yaw, roll) at a fixed rate in degrees per second.";
		private const string RateLabel = "Rate";
		private const string AxisAngleBox =
			"Simulates rotation around a fixed world-space axis at a specified rate in degrees per second.";
		private const string AxisLabel = "Axis";
		private const string DeviceConfigFieldsDisabledBoxFormat =
			"Some properties of the virtual device are not configurable while the device is connected. To change " +
			"these fields, disconnect the device using the \"{0}\" button below.";
		private const string UseSimulatedMovementMobileDeviceHelpBox =
			"You may simulate movement with a mobile device's IMU by either:\n"+
			"• Connecting a mobile device via USB and running the Unity Remote app. (Unity may take a few "+
			"seconds to recognize that the Remote has been connected.)\n" +
			"• Building and deploying to your mobile device.";
		private const string FirmwareDeviceNotSupportedBox =
			"The virtual firmware is not sufficient for connection, and no updates are available. Connection to the " +
			"virtual device will not be supported.";
		private const string FirmwareUpdateRequiredBox =
			"The virtual firmware is not sufficient for connection, but an update is available. A firmware update will " +
			"be required before connection will succeed.";
		private const string FirmwareGoodBox = "The virtual firmware is sufficient for connection.";
		private const string FirmwareUpdateAvailableBox =
			"The virtual firmware is sufficient for connection, but an optional newer version is available.";
		private const string SecurePairingAcceptedBox =
			"If the device status indicates that secure pairing is required, the provider will simulate a user " +
			"accepting that request.";
		private const string SecurePairingRejectedBox =
			"If the device status indicates that secure pairing is required, the provider will simulate a user " +
			"denying that request, and connection will fail.";
		private const string SensorServiceSuspensionReasonLabel = "Sensor Service Suspension Reason";
		private const string SuspendSensorServiceLabel = "Suspend Sensor Service";
		private const string ResumeSensorServiceLabel = "Resume Sensor Service";
		private const string DeviceStatusHeading = "Device Status";

		private SensorServiceSuspendedReason _sensorServiceSuspendedReason;

		private ProductType _productType;
		private Dictionary<string, byte> _editorVariantMap;
		private string[] _editorVariantOptions;
		private int _editorVariantIndex;
		private bool _editorVariantOptionsAreDirty;

		public WearableDebugProviderDrawer()
		{
			_sensorServiceSuspendedReason = SensorServiceSuspendedReason.UnknownReason;
			_editorVariantOptionsAreDirty = true;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			WearableDebugProvider provider = (WearableDebugProvider)fieldInfo.GetValue(property.serializedObject.targetObject);

			EditorGUI.BeginProperty(position, label, property);

			EditorGUILayout.HelpBox(DescriptionBox, MessageType.None);
			EditorGUILayout.Space();

			// Virtual device config
			if (provider.ConnectedDevice.HasValue)
			{
				EditorGUILayout.HelpBox(string.Format(DeviceConfigFieldsDisabledBoxFormat, DisconnectLabel), MessageType.Info);
			}

			using (new EditorGUI.DisabledScope(provider.ConnectedDevice.HasValue))
			{
				// Device properties
				EditorGUILayout.IntSlider(
					property.FindPropertyRelative(RSSIField),
					WearableConstants.MinimumRSSIValue,
					WearableConstants.MaximumRSSIValue,
					WearableConstants.EmptyLayoutOptions);

				EditorGUILayout.Separator();
				EditorGUILayout.PropertyField(property.FindPropertyRelative(DeviceNameField), WearableConstants.EmptyLayoutOptions);
				EditorGUILayout.PropertyField(property.FindPropertyRelative(UIDField), WearableConstants.EmptyLayoutOptions);

				// draw product and variant types based on ids
				var productIdProp = property.FindPropertyRelative(ProductIdField);
				_productType = WearableTools.GetProductType((ProductId)productIdProp.intValue);
				var variantProp = property.FindPropertyRelative(VariantIdField);

				EditorGUI.BeginChangeCheck();
				_productType = (ProductType)EditorGUILayout.EnumPopup(
					ProductTypeLabel,
					_productType
				);
				if (EditorGUI.EndChangeCheck())
				{
					// if we have changed the product, we need to reset the variants
					productIdProp.intValue = (int)WearableTools.GetProductId(_productType);
					variantProp.intValue = 0;
					_editorVariantOptionsAreDirty = true;
				}

				if (_editorVariantOptionsAreDirty)
				{
					_editorVariantMap = GetVariantMap(_productType);
					_editorVariantOptions = _editorVariantMap.Keys.ToArray();
					_editorVariantOptionsAreDirty = false;
				}

				string variantName = GetNameForProductAndVariantId(_productType, (byte)variantProp.intValue);
				var optionIndex = Array.IndexOf(_editorVariantOptions, variantName);
				_editorVariantIndex = EditorGUILayout.Popup(
					VariantTypeLabel,
					optionIndex >= 0 ? optionIndex : 0,
					_editorVariantOptions
				);

				variantProp.intValue = _editorVariantMap[_editorVariantOptions[_editorVariantIndex]];

				// Firmware simulation
				EditorGUILayout.PropertyField(property.FindPropertyRelative(FirmwareVersionField), WearableConstants.EmptyLayoutOptions);
				var firmwareSufficient = property.FindPropertyRelative(BoseArEnabledField);
				var firmwareAvailable = property.FindPropertyRelative(FirmwareUpdateAvailableField);
				EditorGUILayout.PropertyField(firmwareSufficient, WearableConstants.EmptyLayoutOptions);
				EditorGUILayout.PropertyField(firmwareAvailable, WearableConstants.EmptyLayoutOptions);

				if (firmwareSufficient.boolValue)
				{
					if (firmwareAvailable.boolValue)
					{
						EditorGUILayout.HelpBox(FirmwareUpdateAvailableBox, MessageType.Info);
					}
					else
					{
						EditorGUILayout.HelpBox(FirmwareGoodBox, MessageType.Info);
					}
				}
				else
				{
					if (firmwareAvailable.boolValue)
					{
						EditorGUILayout.HelpBox(FirmwareUpdateRequiredBox, MessageType.Warning);
					}
					else
					{
						EditorGUILayout.HelpBox(FirmwareDeviceNotSupportedBox, MessageType.Error);
					}
				}

				// Secure pairing
				var acceptSecurePairing = property.FindPropertyRelative(AcceptSecurePairingField);
				EditorGUILayout.PropertyField(acceptSecurePairing, WearableConstants.EmptyLayoutOptions);
				if (acceptSecurePairing.boolValue)
				{
					EditorGUILayout.HelpBox(SecurePairingAcceptedBox, MessageType.Info);
				}
				else
				{
					EditorGUILayout.HelpBox(SecurePairingRejectedBox, MessageType.Error);
				}

				// Sensor and gesture availability
				var sensorFlagsProp = property.FindPropertyRelative(SensorFlagsField);
				var sensorFlagsEnumValue = EditorGUILayout.EnumFlagsField(sensorFlagsProp.displayName, provider.AvailableSensors);
				sensorFlagsProp.intValue = (int) Convert.ChangeType(sensorFlagsEnumValue, typeof(SensorFlags));

				var gestureFlagsProp = property.FindPropertyRelative(GestureFlagsField);
				var gestureFlagsEnumValue = EditorGUILayout.EnumFlagsField(gestureFlagsProp.displayName, provider.AvailableGestures);
				gestureFlagsProp.intValue = (int) Convert.ChangeType(gestureFlagsEnumValue, typeof(GestureFlags));
			}
			EditorGUILayout.Space();

			// Verbose mode
			EditorGUILayout.PropertyField(property.FindPropertyRelative(VerboseField), WearableConstants.EmptyLayoutOptions);

			// Simulated delay
			var delayProp = property.FindPropertyRelative(DelayTimeField);
			EditorGUILayout.PropertyField(delayProp, WearableConstants.EmptyLayoutOptions);
			if (delayProp.floatValue < 0.0f)
			{
				delayProp.floatValue = 0.0f;
			}

			EditorGUILayout.Space();

			// Device status
			EditorGUILayout.LabelField(DeviceStatusHeading);
			EditorGUILayout.PropertyField(property.FindPropertyRelative(DynamicDeviceInfoField));

			DynamicDeviceInfo dynamicDeviceInfo = provider.GetDynamicDeviceInfo();
			DeviceStatus status = dynamicDeviceInfo.deviceStatus;
			using (new EditorGUI.DisabledScope(!provider.ConnectedDevice.HasValue))
			{
				bool serviceSuspended = status.GetFlagValue(DeviceStatusFlags.SensorServiceSuspended);

				// Service suspended
				using (new EditorGUI.DisabledScope(serviceSuspended))
				{
					// Only allow selecting a reason if the service isn't suspended
					_sensorServiceSuspendedReason = (SensorServiceSuspendedReason)EditorGUILayout.EnumPopup(
						SensorServiceSuspensionReasonLabel,
						_sensorServiceSuspendedReason);
				}

				if (serviceSuspended)
				{
					bool shouldResume = GUILayout.Button(ResumeSensorServiceLabel, WearableConstants.EmptyLayoutOptions);
					if (shouldResume)
					{
						provider.SimulateSensorServiceResumed();
					}
				}
				else
				{
					bool shouldSuspend = GUILayout.Button(SuspendSensorServiceLabel, WearableConstants.EmptyLayoutOptions);
					if (shouldSuspend)
					{
						provider.SimulateSensorServiceSuspended(_sensorServiceSuspendedReason);
					}
				}
			}

			EditorGUILayout.Space();

			// Movement simulation
			SerializedProperty simulateMovementProperty = property.FindPropertyRelative(MovementSimulationModeField);
			EditorGUILayout.PropertyField(simulateMovementProperty, WearableConstants.EmptyLayoutOptions);
			var simulatedMovementMode = (WearableDebugProvider.MovementSimulationMode)simulateMovementProperty.enumValueIndex;

			if (simulatedMovementMode == WearableDebugProvider.MovementSimulationMode.ConstantRate)
			{
				SerializedProperty rotationTypeProperty = property.FindPropertyRelative(RotationTypeField);
				EditorGUILayout.PropertyField(rotationTypeProperty, WearableConstants.EmptyLayoutOptions);

				string rotationType = rotationTypeProperty.enumNames[rotationTypeProperty.enumValueIndex];
				if (rotationType == RotationTypeEuler)
				{
					EditorGUILayout.HelpBox(EulerRateBox, MessageType.None);
					EditorGUILayout.PropertyField(property.FindPropertyRelative(EulerSpinRateField), WearableConstants.EmptyLayoutOptions);
				}
				else if (rotationType == RotationTypeAxisAngle)
				{
					EditorGUILayout.HelpBox(AxisAngleBox, MessageType.None);
					SerializedProperty axisAngleProperty = property.FindPropertyRelative(AxisAngleSpinRateField);
					Vector4 previousValue = axisAngleProperty.vector4Value;
					Vector4 newValue = EditorGUILayout.Vector3Field(
						AxisLabel,
						new Vector3(previousValue.x, previousValue.y, previousValue.z),
						WearableConstants.EmptyLayoutOptions);
					if (newValue.sqrMagnitude < float.Epsilon)
					{
						newValue.x = 1.0f;
					}

					newValue.w = EditorGUILayout.FloatField(RateLabel, previousValue.w, WearableConstants.EmptyLayoutOptions);
					axisAngleProperty.vector4Value = newValue;
				}
			}
			else if (simulatedMovementMode == WearableDebugProvider.MovementSimulationMode.MobileDevice)
			{
				EditorGUILayout.HelpBox(UseSimulatedMovementMobileDeviceHelpBox, MessageType.Info);
			}

			// Gesture triggers
			GUILayout.Label(GesturesLabel, WearableConstants.EmptyLayoutOptions);
			for (int i = 0; i < WearableConstants.GestureIds.Length; i++)
			{
				GestureId gesture = WearableConstants.GestureIds[i];

				if (gesture == GestureId.None)
				{
					continue;
				}

				using (new EditorGUI.DisabledScope(
					!(provider.GetCachedDeviceConfiguration().GetGestureConfig(gesture).isEnabled &&
					  EditorApplication.isPlaying)))
				{
					bool shouldTrigger = GUILayout.Button(Enum.GetName(typeof(GestureId), gesture), WearableConstants.EmptyLayoutOptions);
					if (shouldTrigger)
					{
						provider.SimulateGesture(gesture);
					}
				}
			}

			// Disconnect button
			EditorGUILayout.Space();
			using (new EditorGUI.DisabledScope(!provider.ConnectedDevice.HasValue))
			{
				bool shouldDisconnect = GUILayout.Button(DisconnectLabel, WearableConstants.EmptyLayoutOptions);
				if (shouldDisconnect)
				{
					provider.SimulateDisconnect();
				}
			}

			EditorGUI.EndProperty();
		}

		private Dictionary<string, byte> GetVariantMap(ProductType productType)
		{
			var map = new Dictionary<string, byte>();
			var names = WearableTools.GetVariantNamesForProduct(productType);
			var values = WearableTools.GetVariantValuesForProduct(productType);

			if (names != null)
			{
				for (int i = 0; i < names.Length; ++i)
				{
					map.Add(names[i].Nicify(), values[i]);
				}
			}

			return map;
		}

		private string GetNameForProductAndVariantId(ProductType productType, byte variantId)
		{
			string result = string.Empty;

			switch (productType)
			{
				case ProductType.Frames:
					result = Enum.GetName(typeof(FramesVariantId), variantId);
					break;
				case ProductType.QuietComfort35Two:
					result = Enum.GetName(typeof(QuietComfort35TwoVariantId), variantId);
					break;
				case ProductType.NoiseCancellingHeadphones700:
					result = Enum.GetName(typeof(NoiseCancellingHeadphones700VariantId), variantId);
					break;
				case ProductType.Unknown:
					result = ProductId.Undefined.ToString();
					break;
			}

			result = result == null ? string.Empty : result.Nicify();

			return result;
		}
	}
}
