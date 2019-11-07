#if UNITY_IOS && !UNITY_EDITOR

using System;
using System.Runtime.InteropServices;

namespace Bose.Wearable
{
	internal sealed partial class WearableDeviceProvider : IWearableDeviceProviderPlatform
	{
		#region IWearableDeviceProviderPlatform implementation

		public void WearableDeviceInitialize(bool simulated)
		{
			WearableInitialize(simulated);
		}

		public void StartSearch(AppIntentProfile appIntentProfile, int rssiThreshold)
		{
			BridgeAppIntentProfile bridgeAppIntentProfile = CreateBridgeAppIntentProfile(appIntentProfile);
			WearableStartDeviceSearch(bridgeAppIntentProfile, rssiThreshold);
		}

		public void CancelDeviceConnectionInternal()
		{
			// no-op, due to current native limitations.
		}

		public void StopSearch()
		{
			WearableStopDeviceSearch();
		}

		public void OpenSession(string uid)
		{
			WearableOpenSession(uid);
		}

		public void CloseSession()
		{
			WearableCloseSession();
		}

		public int GetSessionStatus(ref string errorMessage)
		{
			return WearableGetSessionStatus(ref errorMessage);
		}

		public ConnectionStatus GetConnectionStatus(ref string errorMessage)
		{
			return (ConnectionStatus)WearableGetConnectionStatus(ref errorMessage);
		}

		public void GetDeviceInfo(ref Device device)
		{
			device.productId = (ProductId)WearableGetDeviceProductID();
			device.variantId = (byte)WearableGetDeviceVariantID();
			device.availableSensors = (SensorFlags)WearableGetDeviceAvailableSensors();
			device.availableGestures = (GestureFlags)WearableGetDeviceAvailableGestures();
			string firmware = WearableConstants.DefaultFirmwareVersion;
			WearableGetDeviceFirmwareVersion(ref firmware);
			device.firmwareVersion = firmware;
			device.deviceStatus = WearableGetDeviceStatus();
			device.transmissionPeriod = WearableGetTransmissionPeriod();
			device.maximumPayloadPerTransmissionPeriod = WearableGetMaximumPayloadPerTransmissionPeriod();
			device.maximumActiveSensors = WearableGetMaximumActiveSensors();
		}

		public Device[] GetDiscoveredDevicesInternal()
		{
			Device[] devices = WearableConstants.EmptyDeviceList;
			unsafe
			{
				BridgeDevice* nativeDevices = null;
				int count = 0;
				WearableGetDiscoveredDevices(&nativeDevices, &count);
				if (count > 0)
				{
					devices = new Device[count];
					for (int i = 0; i < count; i++)
					{
						devices[i] = (Device)Marshal.PtrToStructure(new IntPtr(nativeDevices + i), typeof(Device));
					}
				}
			}

			return devices;
		}

		public DeviceConnectionInfo GetDeviceConnectionInfoInternal()
		{
			BridgeDeviceConnectionInformation bridgeConnectionInfo = WearableGetDeviceConnectionInformation();
			DeviceConnectionInfo deviceConnectionInfo = new DeviceConnectionInfo();
			deviceConnectionInfo.productId = (Bose.Wearable.ProductId)bridgeConnectionInfo.productId;
			deviceConnectionInfo.variantId = bridgeConnectionInfo.variantId;
			return deviceConnectionInfo;
		}

		public FirmwareUpdateInformation GetFirmwareUpdateInformationInternal()
		{
			BridgeFirmwareUpdateInformation bridgeFirmwareInformation = new BridgeFirmwareUpdateInformation();
			WearableGetFirmwareUpdateInformation(ref bridgeFirmwareInformation);
			FirmwareUpdateInformation firmwareUpdateInformation = new FirmwareUpdateInformation();
			firmwareUpdateInformation.icon = (BoseUpdateIcon)bridgeFirmwareInformation.updateIcon;
			firmwareUpdateInformation.title = bridgeFirmwareInformation.title;
			firmwareUpdateInformation.message = bridgeFirmwareInformation.message;
			firmwareUpdateInformation.options = new FirmwareUpdateAlertOption[bridgeFirmwareInformation.numOptions];

			for (int i = 0; i < bridgeFirmwareInformation.numOptions; i++)
			{
				BridgeFirmwareUpdateAlertOption alertOption = new BridgeFirmwareUpdateAlertOption();
				WearableGetFirmwareUpdateAlertOption(ref alertOption, i);
				firmwareUpdateInformation.options[i].style = (AlertStyle)alertOption.style;
				firmwareUpdateInformation.options[i].title = alertOption.title;
			}

			return firmwareUpdateInformation;
		}

		public void SelectFirmwareUpdateOptionInternal(int index)
		{
			WearableSelectAlertOption(index);
		}

		public void GetLatestSensorUpdatesInternal()
		{
			unsafe
			{
				BridgeSensorFrame* frames = null;
				int count = 0;
				WearableGetSensorFrames(&frames, &count);
				if (count > 0)
				{
					for (int i = 0; i < count; i++)
					{
						var frame = frames + i;
						_currentSensorFrames.Add(new SensorFrame
						{
							timestamp = WearableConstants.Sensor2UnityTime * frame->timestamp,
							deltaTime = WearableConstants.Sensor2UnityTime * frame->deltaTime,
							acceleration = frame->acceleration,
							angularVelocity = frame->angularVelocity,
							rotationNineDof = frame->rotationNineDof,
							rotationSixDof = frame->rotationSixDof,
							gestureId = frame->gestureId
						});
					}
				}
			}
		}

		public void GetLatestGestureUpdatesInternal()
		{
			unsafe
			{
				BridgeGestureData* gestureData = null;
				int count = 0;
				WearableGetGestureData(&gestureData, &count);
				if (count > 0)
				{
					for (int i = 0; i < count; i++)
					{
						var gesture = gestureData + i;
						_currentGestureData.Add(new GestureData
						{
							timestamp = WearableConstants.Sensor2UnityTime * gesture->timestamp,
							gestureId = gesture->gestureId
						});
					}
				}
			}
		}

		public WearableDeviceConfig GetDeviceConfigurationInternal()
		{
			BridgeDeviceConfiguration config = new BridgeDeviceConfiguration();
			WearableGetDeviceConfiguration(ref config);

			return CreateWearableConfig(config);
		}

		public void SetDeviceConfigurationInternal(WearableDeviceConfig config)
		{
			WearableSetDeviceConfiguration(CreateBridgeConfig(config));
		}

		public ConfigStatus GetSensorConfigStatusInternal()
		{
			return (ConfigStatus)WearableDeviceGetSensorConfigurationStatus();
		}

		public ConfigStatus GetGestureConfigStatusInternal()
		{
			return (ConfigStatus)WearableDeviceGetGestureConfigurationStatus();
		}

		public DynamicDeviceInfo GetDynamicDeviceInfoInternal()
		{
			BridgeDynamicDeviceInfo bridgeDynamicDeviceInfo = new BridgeDynamicDeviceInfo();
			WearableGetDynamicDeviceInfo(ref bridgeDynamicDeviceInfo);

			return CreateDynamicDeviceInfo(bridgeDynamicDeviceInfo);
		}

		public bool IsAppIntentProfileValid(AppIntentProfile appIntentProfile)
		{
			BridgeAppIntentProfile bridgeAppIntentProfile = CreateBridgeAppIntentProfile(appIntentProfile);
			return WearableValidateAppIntents(bridgeAppIntentProfile);
		}

		public void SetAppFocusChangedInternal(bool hasFocus)
		{
			WearableSetAppFocusChanged(hasFocus);
		}

		#endregion

		#region iOS Interop

		/// <summary>
		/// This struct matches the plugin bridge definition and is only used as a temporary convert from the native
		/// code struct to the public struct.
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		private unsafe struct BridgeDeviceConnectionInformation
		{
			public int productId;
			public byte variantId;
		}

		/// <summary>
		/// This struct matches the plugin bridge definition and is only used as a temporary convert from the native
		/// code struct to the public struct.
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		private unsafe struct BridgeDevice
		{
			public char* uid;
			public char* name;
			public char* firmwareVersion;
			public bool isConnected;
			public int rssi;
			public SensorFlags availableSensors;
			public GestureFlags availableGestures;
			public int productId;
			public int variantId;
			public int transmissionPeriod;
			public int maximumPayloadPerTransmission;
			public int maximumActiveSensors;
		}

		/// <summary>
		/// This struct matches the plugin bridge definition and is only used as a temporary convert from the native
		/// code struct to the public struct.
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		private struct BridgeSensorFrame
		{
			public int timestamp;
			public int deltaTime;
			public SensorVector3 acceleration;
			public SensorVector3 angularVelocity;
			public SensorQuaternion rotationNineDof;
			public SensorQuaternion rotationSixDof;
			public GestureId gestureId;
		}

		/// <summary>
		/// This struct matches the plugin bridge definition and is only used as a temporary convert from the native
		/// code struct to the public struct.
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		private struct BridgeGestureData
		{
			public int timestamp;
			public GestureId gestureId;
		}

		/// <summary>
		/// This struct allows passing a device configuration to the bridge, and acts only as a temporary passthrough.
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		private struct BridgeDeviceConfiguration
		{
			public int updateInterval;

			public int sensorAccelerometer;
			public int sensorGyroscope;
			public int sensorRotationNineDof;
			public int sensorRotationSixDof;

			public int gestureDoubleTap;
			public int gestureHeadNod;
			public int gestureHeadShake;
			public int gestureTouchAndHold;

			public int gestureInput;
			public int gestureAffirmative;
			public int gestureNegative;
		};

		/// <summary>
		/// Struct that acts as a go-between for getting information about firmware alerts.
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		private struct BridgeFirmwareUpdateInformation
		{
			public int updateIcon;
			public string title;
			public string message;
			public int numOptions;
		}

		/// <summary>
		/// Struct that acts as a go-between for getting information about firmware alert options.
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		private struct BridgeFirmwareUpdateAlertOption
		{
			public int style;
			public string title;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct BridgeDynamicDeviceInfo
		{
			public int deviceStatus;
			public int transmissionPeriod;
		}

		/// <summary>
		/// This struct allows passing an AppIntentProfile to the bridge as bitfields instead of arrays.
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		private struct BridgeAppIntentProfile
		{
			public int sensors;
			public int samplePeriods;
			public int gestures;
		}

		/// <summary>
		/// Helper function to convert our AppIntentProfile to something more easily consumable
		/// by the bridge layer.
		/// </summary>
		private static BridgeAppIntentProfile CreateBridgeAppIntentProfile(AppIntentProfile appIntentProfile)
		{
			BridgeAppIntentProfile bridgeAppIntentProfile = new BridgeAppIntentProfile();
			int sensors = 0;
			int samplePeriods = 0;
			int gestures = 0;

			if (appIntentProfile != null)
			{
				for (int i = 0; i < WearableConstants.SensorIds.Length; i++)
				{
					if (appIntentProfile.GetSensorInProfile(WearableConstants.SensorIds[i]))
					{
						sensors |= (1 << i);
					}
				}

				for (int i = 0; i < WearableConstants.UpdateIntervals.Length; i++)
				{
					if (appIntentProfile.GetIntervalInProfile(WearableConstants.UpdateIntervals[i]))
					{
						samplePeriods |= (1 << i);
					}
				}

				for (int i = 0; i < WearableConstants.GestureIds.Length; i++)
				{
					if (WearableConstants.GestureIds[i] == GestureId.None)
					{
						continue;
					}

					if (appIntentProfile.GetGestureInProfile(WearableConstants.GestureIds[i]))
					{
						gestures |= (1 << (i - 1));
					}
				}
			}

			bridgeAppIntentProfile.sensors = sensors;
			bridgeAppIntentProfile.samplePeriods = samplePeriods;
			bridgeAppIntentProfile.gestures = gestures;

			return bridgeAppIntentProfile;
		}

		/// <summary>
		/// Helper function to convert our WearableDeviceConfig to something more easily consumable
		/// by the bridge layer.
		/// </summary>
		private static BridgeDeviceConfiguration CreateBridgeConfig(WearableDeviceConfig config)
		{
			BridgeDeviceConfiguration bridgeConfig;
			bridgeConfig.updateInterval = (int)config.updateInterval;
			bridgeConfig.sensorAccelerometer = config.accelerometer.isEnabled ? 1 : 0;
			bridgeConfig.sensorGyroscope = config.gyroscope.isEnabled ? 1 : 0;
			bridgeConfig.sensorRotationNineDof = config.rotationNineDof.isEnabled ? 1 : 0;
			bridgeConfig.sensorRotationSixDof = config.rotationSixDof.isEnabled ? 1 : 0;
			bridgeConfig.gestureDoubleTap = config.doubleTapGesture.isEnabled ? 1 : 0;
			bridgeConfig.gestureHeadNod = config.headNodGesture.isEnabled ? 1 : 0;
			bridgeConfig.gestureHeadShake = config.headShakeGesture.isEnabled ? 1 : 0;
			bridgeConfig.gestureTouchAndHold = config.touchAndHoldGesture.isEnabled ? 1 : 0;
			bridgeConfig.gestureInput = config.inputGesture.isEnabled ? 1 : 0;
			bridgeConfig.gestureAffirmative = config.affirmativeGesture.isEnabled ? 1 : 0;
			bridgeConfig.gestureNegative = config.negativeGesture.isEnabled ? 1 : 0;

			return bridgeConfig;
		}

		/// <summary>
		/// Helper function to convert our WearableDeviceConfig to something more easily consumable
		/// by the bridge layer.
		/// </summary>
		private static WearableDeviceConfig CreateWearableConfig(BridgeDeviceConfiguration config)
		{
			WearableDeviceConfig wearableConfig = new WearableDeviceConfig();
			wearableConfig.updateInterval = (SensorUpdateInterval)config.updateInterval;
			wearableConfig.accelerometer.isEnabled = config.sensorAccelerometer != 0;
			wearableConfig.gyroscope.isEnabled = config.sensorGyroscope != 0;
			wearableConfig.rotationNineDof.isEnabled = config.sensorRotationNineDof != 0;
			wearableConfig.rotationSixDof.isEnabled = config.sensorRotationSixDof != 0;
			wearableConfig.doubleTapGesture.isEnabled = config.gestureDoubleTap != 0;
			wearableConfig.headNodGesture.isEnabled = config.gestureHeadNod != 0;
			wearableConfig.headShakeGesture.isEnabled = config.gestureHeadShake != 0;
			wearableConfig.touchAndHoldGesture.isEnabled = config.gestureTouchAndHold != 0;
			wearableConfig.inputGesture.isEnabled = config.gestureInput != 0;
			wearableConfig.affirmativeGesture.isEnabled = config.gestureAffirmative != 0;
			wearableConfig.negativeGesture.isEnabled = config.gestureNegative != 0;

			return wearableConfig;
		}

		private static DynamicDeviceInfo CreateDynamicDeviceInfo(BridgeDynamicDeviceInfo bridgeDynamicDeviceInfo)
		{
			DynamicDeviceInfo dynamicDeviceInfo = new DynamicDeviceInfo();
			dynamicDeviceInfo.deviceStatus = bridgeDynamicDeviceInfo.deviceStatus;
			dynamicDeviceInfo.transmissionPeriod = bridgeDynamicDeviceInfo.transmissionPeriod;

			return dynamicDeviceInfo;
		}

		/// <summary>
		/// Initializes the Wearable SDK and Plugin Bridge; where <paramref name="simulateDevices"/> is true, only simulated
		/// devices will be able to connect and get data from. If false, only real Wearable devices can be used.
		/// </summary>
		[DllImport("__Internal")]
		private static extern void WearableInitialize(bool simulateDevices);

		/// <summary>
		/// Starts the search for available Wearable devices in range whose RSSI is greater than <paramref name="rssiThreshold"/>
		/// </summary>
		/// <param name="bridgeAppIntentProfile"></param>
		/// <param name="rssiThreshold"></param>
		[DllImport("__Internal")]
		private static extern void WearableStartDeviceSearch(BridgeAppIntentProfile bridgeAppIntentProfile, int rssiThreshold);

		/// <summary>
		/// Returns all available Wearable devices.
		/// </summary>
		/// <param name="devices"></param>
		/// <param name="count"></param>
		[DllImport("__Internal")]
		private static extern unsafe void WearableGetDiscoveredDevices(BridgeDevice** devices, int* count);

		/// <summary>
		/// Stops searching for available Wearable devices in range.
		/// </summary>
		[DllImport("__Internal")]
		private static extern void WearableStopDeviceSearch();

		/// <summary>
		/// Attempts to open a session with a specific Wearable Device by way of <paramref name="deviceUid"/>.
		/// </summary>
		/// <param name="deviceUid"></param>
		[DllImport("__Internal")]
		private static extern void WearableOpenSession(string deviceUid);

		/// <summary>
		/// Assesses the SessionStatus of the currently opened session for a specific Wearable device. If there has
		/// been an error, <paramref name="errorMsg"/> will be populated with the text contents of the error message.
		/// </summary>
		/// <param name="errorMsg"></param>
		/// <returns></returns>
		[DllImport("__Internal")]
		private static extern int WearableGetSessionStatus(ref string errorMsg);

		/// <summary>
		/// Assesses the ConnectionStatus of the currently opened session for a specific Wearable device. If there has
		/// been an error, <paramref name="errorMsg"/> will be populated with the text contents of the error message.
		/// </summary>
		/// <param name="errorMsg"></param>
		/// <returns></returns>
		[DllImport("__Internal")]
		private static extern int WearableGetConnectionStatus(ref string errorMsg);

		[DllImport("__Internal")]
		private static extern void WearableCloseSession();

		/// <summary>
		/// Grabs the deviceStatus of the currently opened session for a Wearable device.
		/// </summary>
		/// <returns></returns>
		[DllImport("__Internal")]
		private static extern int WearableGetDeviceStatus();

		/// <summary>
		/// Grabs necessary information to identify a device before we connect.
		/// </summary>
		/// <returns></returns>
		[DllImport("__Internal")]
		private static extern BridgeDeviceConnectionInformation WearableGetDeviceConnectionInformation();

		/// <summary>
		/// Grabs necessary information to present a firmware update screen to the user.
		/// </summary>
		/// <returns></returns>
		[DllImport("__Internal")]
		private static extern void WearableGetFirmwareUpdateInformation(ref BridgeFirmwareUpdateInformation updateInformation);

		/// <summary>
		/// Gets information about an individual alert option in a firmware update screen.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		[DllImport("__Internal")]
		private static extern void WearableGetFirmwareUpdateAlertOption(ref BridgeFirmwareUpdateAlertOption alertOption, int index);

		/// <summary>
		/// Conveys to the native sdk that the user has selected an option to deal with out-of-date firmware.
		/// </summary>
		/// <param name="index">The index of the button that was selected.</param>
		/// <returns></returns>
		[DllImport("__Internal")]
		private static extern void WearableSelectAlertOption(int index);

		/// <summary>
		/// Returns all unread BridgeSensorFrames from the Wearable Device.
		/// </summary>
		/// <returns></returns>
		[DllImport("__Internal")]
		private static extern unsafe void WearableGetSensorFrames(BridgeSensorFrame** sensorFrames, int* count);

		/// <summary>
		/// Returns all unread BridgeGestureData from the Wearable Device.
		/// </summary>
		/// <returns></returns>
		[DllImport("__Internal")]
		private static extern unsafe void WearableGetGestureData(BridgeGestureData** sensorFrames, int* count);


		/// <summary>
		/// Enables and disables all sensors, gestures, rotation source, and update interval.
		/// </summary>
		/// <param name="config"></param>
		/// <returns></returns>
		[DllImport("__Internal")]
		private static extern void WearableSetDeviceConfiguration(BridgeDeviceConfiguration config);

		/// <summary>
		/// Returns the current device configuration of all sensors, gestures, rotation source, and update interval.
		/// </summary>
		/// <returns>BridgeDeviceConfiguration</returns>
		[DllImport("__Internal")]
		private static extern void WearableGetDeviceConfiguration(ref BridgeDeviceConfiguration config);

		/// <summary>
		/// Returns the ProductId of a device. This will default to 0 if there is not an open session yet. The ProductId of a device is only available once a session has been opened.
		/// </summary>
		[DllImport("__Internal")]
		private static extern int WearableGetDeviceProductID();

		/// <summary>
		/// Returns the VariantId of a device. This will default to 0 if there is not an open session yet. The VariantId of a device is only available once a session has been opened.
		/// </summary>
		[DllImport("__Internal")]
		private static extern int WearableGetDeviceVariantID();

		/// <summary>
		/// Returns the sensors available on a device. This will default to 0 if there is not an open session yet. The available sensors of a device is only available once a session has been opened.
		/// </summary>
		[DllImport("__Internal")]
		private static extern int WearableGetDeviceAvailableSensors();

		/// <summary>
		/// Returns the gestures available on a device. This will default to 0 if there is not an open session yet. The available gestures of a device is only available once a session has been opened.
		/// </summary>
		[DllImport("__Internal")]
		private static extern int WearableGetDeviceAvailableGestures();

		/// <summary>
		/// Returns the Firmware Version of a device. This will default to an empty string if there is not an open session yet.
		/// The Firmware Version of a device is only available once a session has been opened.
		/// </summary>
		[DllImport("__Internal")]
		private static extern void WearableGetDeviceFirmwareVersion(ref string version);

		/// <summary>
		/// Fetches the sensor configuration status of the bridge.
		/// </summary>
		/// <returns></returns>
		[DllImport("__Internal")]
		private static extern int WearableDeviceGetSensorConfigurationStatus();

		/// <summary>
		/// Fetches the gesture configuration status of the bridge.
		/// </summary>
		/// <returns></returns>
		[DllImport("__Internal")]
		private static extern int WearableDeviceGetGestureConfigurationStatus();

		/// <summary>
		/// Grabs the transmissionPeriod of the currently opened session for a Wearable device.
		/// </summary>
		/// <returns></returns>
		[DllImport("__Internal")]
		private static extern int WearableGetTransmissionPeriod();

		/// <summary>
		/// Grabs the maximumPayloadPerTransmissionPeriod of the currently opened session for a Wearable device.
		/// </summary>
		/// <returns></returns>
		[DllImport("__Internal")]
		private static extern int WearableGetMaximumPayloadPerTransmissionPeriod();

		/// <summary>
		/// Grabs the maximumActiveSensors of the currently opened session for a Wearable device.
		/// </summary>
		/// <returns></returns>
		[DllImport("__Internal")]
		private static extern int WearableGetMaximumActiveSensors();

		/// <summary>
		/// Grabs the dynamicDeviceInfo of the currently opened session for a Wearable device.
		/// </summary>
		/// <returns></returns>
		[DllImport("__Internal")]
		private static extern void WearableGetDynamicDeviceInfo(ref BridgeDynamicDeviceInfo dynamicDeviceInfo);

		/// <summary>
		/// Validates an app intent against the connected device.
		/// </summary>
		/// <returns></returns>
		[DllImport("__Internal")]
		private static extern bool WearableValidateAppIntents(BridgeAppIntentProfile bridgeAppIntentProfile);

		/// <summary>
		/// Notifies the underlying SDK that the application focus has changed.
		/// </summary>
		[DllImport("__Internal")]
		private static extern void WearableSetAppFocusChanged(bool hasFocus);

		#endregion
	}
}

#endif
