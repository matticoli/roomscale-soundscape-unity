/* WearableUSBProvider.cs
*
* Provider class to connect to a device over USB.  We do not expect this class to be used
* on phones or tablets, since the point is to keep someone from walking off with the device.
* The native function implementations are in BoseWearableUSB.dll.
*/

using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace Bose.Wearable
{
	[Serializable]
	public sealed class WearableUSBProvider : WearableProviderBase
	{
		#region Provider-Specific

		/// <summary>
		/// Represents a session with an Wearable Device.
		/// </summary>
		private enum SessionStatus
		{
			Closed,
			Opening,
			Open
		}


		#pragma warning disable 0414
		[SerializeField]
		// This flag controls printing of log messages which aren't warnings or errors, both here
		// and in the DLL.
		private bool _debugLogging;

		private char[] _statusMessageSeparators;

		// log any status accumulated since the previous update
		private SessionStatus _sessionStatus;
		private StringBuilder _statusMessage;
		#pragma warning restore 0414


		public void SetDebugLoggingInPlugin()
		{
			#if UNITY_EDITOR
			WearableUSBSetDebugLogging(_debugLogging);
			#endif // UNITY_EDITOR
		}

		#endregion // Provider-Specific

		#region Provider API

		internal override void SearchForDevices(
			AppIntentProfile appIntentProfile,
			Action<Device[]> onDevicesUpdated,
			bool autoReconnect,
			float autoReconnectTimeout)
		{
			#if UNITY_EDITOR
			StopSearchingForDevices();

			base.SearchForDevices(appIntentProfile, onDevicesUpdated, autoReconnect, autoReconnectTimeout);

			if (onDevicesUpdated == null)
			{
				return;
			}

			USBAppIntentProfile usbProfile = MakeUSBProfile(appIntentProfile);
			unsafe
			{
				WearableUSBSetAppIntentProfile(&usbProfile);
			}
			WearableUSBRefreshDeviceList();
			_performDeviceSearch = true;
			_nextDeviceSearchTime = Time.unscaledTime + WearableConstants.DeviceUSBConnectUpdateIntervalInSeconds;

			OnConnectionStatusChanged(autoReconnect ? ConnectionStatus.AutoReconnect : ConnectionStatus.Searching);
			#else
			Debug.LogError(WearableConstants.UnsupportedPlatformError);
			OnReceivedSearchDevices(WearableConstants.EmptyDeviceList);
			#endif // UNITY_EDITOR
		}

		internal override void StopSearchingForDevices()
		{
			base.StopSearchingForDevices();

			if (_performDeviceSearch)
			{
				_performDeviceSearch = false;
				_nextDeviceSearchTime = float.PositiveInfinity;

				if (!_connectedDevice.HasValue && ConnectionStatus == ConnectionStatus.Searching)
				{
					OnConnectionStatusChanged(ConnectionStatus.Disconnected);
				}
			}
		}

		internal override void CancelDeviceConnection()
		{
			#if UNITY_EDITOR
			WearableUSBCloseSession();
			WearableUSBRefreshDeviceList();
			#endif // UNITY_EDITOR
			StopDeviceConnection();
			OnConnectionStatusChanged(ConnectionStatus.Cancelled, _deviceToConnect);
		}

		internal override void ConnectToDevice(Device device, Action onSuccess, Action onFailure)
		{
			StopSearchingForDevices();
			DisconnectFromDevice();

			_performDeviceConnection = true;
			_deviceConnectSuccessCallback = onSuccess;
			_deviceConnectFailureCallback = onFailure;
			_deviceToConnect = device;
			_nextDeviceConnectTime = Time.unscaledTime + WearableConstants.DeviceUSBConnectUpdateIntervalInSeconds;

			#if UNITY_EDITOR
			WearableUSBSetDebugLogging(_debugLogging);
			WearableUSBOpenSession(_deviceToConnect.uid);
			#endif // UNITY_EDITOR

			OnConnectionStatusChanged(ConnectionStatus.Connecting, _deviceToConnect);
		}

		internal override void DisconnectFromDevice()
		{
			StopDeviceConnection();
			StopDeviceMonitor();

			_config.DisableAllSensors();
			_config.DisableAllGestures();

			if (_connectedDevice == null)
			{
				return;
			}

			OnConnectionStatusChanged(ConnectionStatus.Disconnected, _connectedDevice);
			_connectedDevice = null;
			_sendConfigSuccessNextFrame = false;

			#if UNITY_EDITOR
			WearableUSBCloseSession();
			#endif // UNITY_EDITOR
		}

		internal override FirmwareUpdateInformation GetFirmwareUpdateInformation()
		{
			return WearableConstants.DefaultFirmwareUpdateInformation;
		}

		internal override void SelectFirmwareUpdateOption(int index)
		{

		}

		internal override void SetDeviceConfiguration(WearableDeviceConfig config)
		{
			#if UNITY_EDITOR
			USBDeviceConfiguration deviceConfig = new USBDeviceConfiguration
			{
				intervalMilliseconds = (int)WearableTools.SensorUpdateIntervalToMilliseconds(config.updateInterval),
				sensorRotationNineDof = config.rotationNineDof.isEnabled ? 1 : 0,
				sensorRotationSixDof = config.rotationSixDof.isEnabled ? 1 : 0,
				sensorGyroscope = config.gyroscope.isEnabled ? 1 : 0,
				sensorAccelerometer = config.accelerometer.isEnabled ? 1 : 0,
				gestureHeadNod = config.headNodGesture.isEnabled ? 1 : 0,
				gestureHeadShake = config.headShakeGesture.isEnabled ? 1 : 0,
				gestureDoubleTap = config.doubleTapGesture.isEnabled ? 1 : 0,
				gestureTouchAndHold = config.touchAndHoldGesture.isEnabled ? 1 : 0,
				gestureInput = config.inputGesture.isEnabled ? 1 : 0,
				gestureAffirmative = config.affirmativeGesture.isEnabled ? 1 : 0,
				gestureNegative = config.negativeGesture.isEnabled ? 1 : 0
			};

			WearableUSBSetDeviceConfiguration(deviceConfig);
			#endif

			// Assume the configuration succeeded, because it generally will. Failed configs will show up when we poll
			// for status flags, and adjust the internal config as necessary.
			_config = config.Clone();
		}

		internal override WearableDeviceConfig GetCachedDeviceConfiguration()
		{
			return _connectedDevice.HasValue ? _config : WearableConstants.DisabledDeviceConfig;
		}

		protected override void RequestDeviceConfigurationInternal()
		{
			#if UNITY_EDITOR
			WearableUSBRequestDeviceConfiguration();
			#else
			// Nothing we can do, so fall back to cached.
			OnReceivedDeviceConfiguration(GetCachedDeviceConfiguration());
			#endif
		}

		internal override DynamicDeviceInfo GetDynamicDeviceInfo()
		{
			// This approach of non-blocking & rate-limiting the USB DynamicDeviceInfo polling occurs on
			// the USB provider for two reasons:
			// * TAP has seen delays in response to commands that would not be conducive to a blocking call.
			// * The USB provider builds up a queue of commands, and we're hesitant to request device information
			//   between 30-60x/sec.
			// Ultimately, the result of this polling means that events and data from the DynamicDeviceInfo may come
			// later than expected by the duration of the update interval in the worst case.
			float time = Time.unscaledTime;
			if (time >= _nextDynamicDeviceInfoTime)
			{
				_nextDynamicDeviceInfoTime = time + WearableConstants.DeviceUSBDynamicInfoUpdateIntervalInSeconds;

				#if UNITY_EDITOR
				unsafe
				{
					USBDynamicDeviceInfo dynamicUSBInfo = new USBDynamicDeviceInfo();
					WearableUSBGetDynamicDeviceInfo(&dynamicUSBInfo);
					_latestDynamicDeviceInfo.deviceStatus = dynamicUSBInfo.deviceStatus;
					_latestDynamicDeviceInfo.transmissionPeriod = dynamicUSBInfo.transmissionPeriod;
				}
				#endif
			}


			return _latestDynamicDeviceInfo;
		}

		protected override void RequestIntentProfileValidationInternal(AppIntentProfile appIntentProfile)
		{
			#if UNITY_EDITOR
			unsafe
			{
				USBAppIntentProfile usbProfile = MakeUSBProfile(appIntentProfile);
				bool valid = WearableUSBCheckAppIntentProfileForConnectedDevice(&usbProfile);
				OnReceivedIntentValidationResponse(valid);
			}
			#endif
		}

		internal override void OnInitializeProvider()
		{
			if (_initialized)
			{
				return;
			}

			#if UNITY_EDITOR
			WearableUSBInitialize();
			WearableUSBSetDebugLogging(_debugLogging);
			#endif // UNITY_EDITOR

			base.OnInitializeProvider();

			StopDeviceConnection();
			StopDeviceMonitor();
			StopSearchingForDevices();
		}

		internal override void OnDestroyProvider()
		{
			base.OnDestroyProvider();

			DisconnectFromDevice();

			StopDeviceConnection();
			StopDeviceMonitor();
			StopSearchingForDevices();
		}

		/// <summary>
		/// When enabled, resume monitoring the device session if necessary.
		/// </summary>
		internal override void OnEnableProvider()
		{
			if (_enabled)
			{
				return;
			}

			base.OnEnableProvider();
			_sendConfigSuccessNextFrame = false;

			if (_connectedDevice != null)
			{
				StartDeviceMonitor();
			}
		}

		/// <summary>
		/// When disabled, stop actively searching for, connecting to, and monitoring devices.
		/// </summary>
		internal override void OnDisableProvider()
		{
			if (!_enabled)
			{
				return;
			}

			base.OnDisableProvider();


			StopSearchingForDevices();
			StopDeviceMonitor();
			StopDeviceConnection();
		}

		internal override void OnUpdate()
		{
			UpdateDeviceConnection();

			// Request the latest updates for this frame
			if (_connectedDevice != null)
			{
				GetLatestSensorUpdates();
				PollForDeviceConfig();
				PollForDeviceConfigCompletion();
			}

			if (_sendConfigSuccessNextFrame)
			{
				_sendConfigSuccessNextFrame = false;
				OnConfigurationSucceeded();
			}

			#if UNITY_EDITOR
			// Check if it's time to query discovered devices
			if (_performDeviceSearch)
			{
				if (Time.unscaledTime >= _nextDeviceSearchTime)
				{
					_nextDeviceSearchTime += WearableConstants.DeviceUSBConnectUpdateIntervalInSeconds;
					Device[] devices = GetDiscoveredDevices();
					OnReceivedSearchDevices(devices);
				}
			}

			// Check if it's time to query the connection routine
			if (_performDeviceConnection && Time.unscaledTime >= _nextDeviceConnectTime)
			{
				_nextDeviceConnectTime += WearableConstants.DeviceUSBConnectUpdateIntervalInSeconds;
				PerformDeviceConnection();
			}

			// Check if it's time to query the device monitor
			if (_pollDeviceMonitor && Time.unscaledTime >= _nextDeviceMonitorTime)
			{
				// NB: The monitor uses the same time interval
				_nextDeviceMonitorTime += WearableConstants.DeviceUSBConnectUpdateIntervalInSeconds;
				MonitorDeviceSession();
			}
			#endif // UNITY_EDITOR

			// Allow the provider base to do its own update work
			base.OnUpdate();
		}

		#endregion // Provider API

		#region Private

		#pragma warning disable CS0414

		// Sensor status
		private WearableDeviceConfig _config;

		// Device search
		private bool _performDeviceSearch;
		private float _nextDeviceSearchTime;
		private StringBuilder _uidBuilder;
		private StringBuilder _nameBuilder;
		private StringBuilder _firmwareVersionBuilder;

		// Device connection
		private bool _performDeviceConnection;
		private Device _deviceToConnect;
		private Action _deviceConnectSuccessCallback;
		private Action _deviceConnectFailureCallback;
		private float _nextDeviceConnectTime;

		// Device connection monitoring
		private bool _pollDeviceMonitor;
		private float _nextDeviceMonitorTime;
		private bool _sendConfigSuccessNextFrame;

		// DynamicDeviceInfo monitoring
		private float _nextDynamicDeviceInfoTime;
		private DynamicDeviceInfo _latestDynamicDeviceInfo;

		#pragma warning restore CS0414

		internal WearableUSBProvider()
		{
			_statusMessageSeparators = new[] {'\n'};
			_sessionStatus = SessionStatus.Closed;
			_statusMessage = new StringBuilder(8192);
			_uidBuilder = new StringBuilder(256);
			_nameBuilder = new StringBuilder(256);
			_firmwareVersionBuilder = new StringBuilder(256);

			_config = new WearableDeviceConfig();
		}

		/// <summary>
		/// Used internally by WearableControl to get the latest buffer of SensorFrame updates from
		/// the Wearable Device; the newest frame in that batch is set as the CurrentSensorFrame.
		/// </summary>
		private void GetLatestSensorUpdates()
		{
			_currentSensorFrames.Clear();
			_currentGestureData.Clear();

			#if UNITY_EDITOR
			unsafe
			{
				bool anyNewSensorFrames = false;
				USBSensorFrame frame;
				while (WearableUSBGetNextSensorFrame(&frame))
				{
					anyNewSensorFrames = true;

					_currentSensorFrames.Add(new SensorFrame
					{
						timestamp = WearableConstants.Sensor2UnityTime * frame.timestamp,
						deltaTime = WearableConstants.Sensor2UnityTime * frame.deltaTime,
						acceleration = frame.acceleration,
						angularVelocity = frame.angularVelocity,
						rotationNineDof = frame.rotationNineDof,
						rotationSixDof = frame.rotationSixDof,
						gestureId = GestureId.None
					});
				}

				if (anyNewSensorFrames)
				{
					_lastSensorFrame = _currentSensorFrames[_currentSensorFrames.Count - 1];
					OnSensorsUpdated(_lastSensorFrame);
				}

				USBGestureData usbGestureData;
				while (WearableUSBGetNextGestureData(&usbGestureData))
				{
					var gestureData = new GestureData
					{
						timestamp = WearableConstants.Sensor2UnityTime * usbGestureData.timestamp,
						gestureId = usbGestureData.gesture
					};
					_currentGestureData.Add(gestureData);
					OnGestureDetected(gestureData.gestureId);
				}
			}
			#endif // UNITY_EDITOR
		}


		private void UpdateDeviceConnection()
		{
			#if UNITY_EDITOR
			WearableUSBUpdate();

			// log any status accumulated since the previous update
			_statusMessage.Length = 0;
			_sessionStatus = (SessionStatus)WearableUSBGetSessionStatus(_statusMessage, _statusMessage.Capacity);

			if (_statusMessage.Length > 0)
			{
				string[] lines = _statusMessage.ToString().Split(_statusMessageSeparators);
				int numLines = lines.Length;
				for (int i = 0; i < numLines; ++i)
				{
					if (lines[i].Length > 1)
					{
						Debug.Log(lines[i]);
					}
				}
			}
			#endif // UNITY_EDITOR
		}


		private void PollForDeviceConfigCompletion()
		{
			#if UNITY_EDITOR
			unsafe
			{
				ConfigStatus sensorConfigStatus = ConfigStatus.Idle;
				ConfigStatus gestureConfigStatus = ConfigStatus.Idle;

				// WearableUSBGetConfigStatus returns true if the bridge has just received responses from the
				// device for an attempt to set the device configuration.  So this check will pass once for
				// every call to WearableUSBSetDeviceConfiguration, some number of frames later.
				if (WearableUSBGetConfigStatus(&sensorConfigStatus, &gestureConfigStatus))
				{
					if (sensorConfigStatus == ConfigStatus.Failure || gestureConfigStatus == ConfigStatus.Failure)
					{
						OnConfigurationFailed(sensorConfigStatus, gestureConfigStatus);
					}
					else
					{
						OnConfigurationSucceeded();
					}
				}
			}
			#endif // UNITY_EDITOR
		}


		private void PollForDeviceConfig()
		{
			if (WaitingForDeviceConfig)
			{
				#if UNITY_EDITOR
				bool complete = WearableUSBHasReceivedDeviceConfiguration();
				if (complete)
				{
					USBDeviceConfiguration config = WearableUSBGetDeviceConfiguration();
					_config.updateInterval = WearableTools.MillisecondsToClosestSensorUpdateInterval(config.intervalMilliseconds);
					_config.accelerometer.isEnabled = config.sensorAccelerometer != 0;
					_config.gyroscope.isEnabled = config.sensorGyroscope != 0;
					_config.rotationNineDof.isEnabled = config.sensorRotationNineDof != 0;
					_config.rotationSixDof.isEnabled = config.sensorRotationSixDof != 0;
					_config.headNodGesture.isEnabled = config.gestureHeadNod != 0;
					_config.headShakeGesture.isEnabled = config.gestureHeadShake != 0;
					_config.doubleTapGesture.isEnabled = config.gestureDoubleTap != 0;
					_config.touchAndHoldGesture.isEnabled = config.gestureTouchAndHold != 0;
					_config.inputGesture.isEnabled = config.gestureInput != 0;
					_config.affirmativeGesture.isEnabled = config.gestureAffirmative != 0;
					_config.negativeGesture.isEnabled = config.gestureNegative != 0;
					OnReceivedDeviceConfiguration(_config.Clone());
				}
				#endif
			}
		}


		/// <summary>
		/// Used internally to get the latest list of discovered devices from
		/// the native SDK.
		/// </summary>
		private Device[] GetDiscoveredDevices()
		{
			Device[] devices = WearableConstants.EmptyDeviceList;

			#if UNITY_EDITOR
			WearableUSBRefreshDeviceList();

			int count = WearableUSBGetNumDiscoveredDevices();
			if (count > 0)
			{
				devices = new Device[count];
				for (int i = 0; i < count; i++)
				{
					_uidBuilder.Length = 0;
					WearableUSBGetDiscoveredDeviceUID(i, _uidBuilder, _uidBuilder.Capacity);
					_nameBuilder.Length = 0;
					WearableUSBGetDiscoveredDeviceName(i, _nameBuilder, _nameBuilder.Capacity);
					_firmwareVersionBuilder.Length = 0;
					WearableUSBGetDiscoveredDeviceFirmwareVersion(i, _firmwareVersionBuilder, _firmwareVersionBuilder.Capacity);

					devices[i] = new Device
					{
						uid = _uidBuilder.ToString(),
						name = _nameBuilder.ToString(),
						firmwareVersion = _firmwareVersionBuilder.ToString(),
						isConnected = (WearableUSBGetDiscoveredDeviceIsConnected(i) == 0) ? false : true,
						productId = ProductId.Undefined,
						variantId = (byte)VariantType.Undefined
					};
				}
			}

			#endif // UNITY_EDITOR

			return devices;
		}

		/// <summary>
		/// Attempts to create a session to a specified device and then checks for the session status perpetually until
		/// a SessionStatus of either Open or Closed is returned, equating to either successful or failed.
		/// </summary>
		private void PerformDeviceConnection()
		{
			#if UNITY_EDITOR
			switch (_sessionStatus)
			{
				// Receiving a session status of Closed while attempting to open a session indicates an error occured.
				case SessionStatus.Closed:
					if (string.IsNullOrEmpty(_statusMessage.ToString()))
					{
						Debug.LogWarning(WearableConstants.DeviceConnectionFailed);
					}
					else
					{
						Debug.LogWarningFormat(WearableConstants.DeviceConnectionFailedWithMessage, _statusMessage);
					}

					// It's OK to not have an open session if we're just searching for a device.
					if (ConnectionStatus != ConnectionStatus.Searching)
					{
						OnConnectionStatusChanged(ConnectionStatus.Failed, _deviceToConnect);

						if (_deviceConnectFailureCallback != null)
						{
							_deviceConnectFailureCallback.Invoke();
						}

						StopDeviceConnection();
					}
					break;

				case SessionStatus.Opening:
					// Device is still connecting.

					OnConnectionStatusChanged(ConnectionStatus.Connecting, _deviceToConnect);

					break;

				case SessionStatus.Open:
					if (_debugLogging)
					{
						Debug.Log(WearableConstants.DeviceConnectionOpened);
					}

					// Add sensor and gesture availability and other invariant device info
					unsafe
					{
						USBStaticDeviceInfo staticUSBInfo = new USBStaticDeviceInfo();
						WearableUSBGetStaticDeviceInfo(&staticUSBInfo);

						_deviceToConnect.productId = (ProductId)staticUSBInfo.productId;
						_deviceToConnect.variantId = (byte)staticUSBInfo.variantId;
						_deviceToConnect.availableSensors = (SensorFlags)staticUSBInfo.availableSensors;
						_deviceToConnect.availableGestures = (GestureFlags)staticUSBInfo.availableGestures;
						_deviceToConnect.maximumActiveSensors = staticUSBInfo.maximumActiveSensors;
						_deviceToConnect.maximumPayloadPerTransmissionPeriod = staticUSBInfo.maximumPayloadPerTransmissionPeriod;

						GetDynamicDeviceInfo();

						_deviceToConnect.deviceStatus = _latestDynamicDeviceInfo.deviceStatus;
						_deviceToConnect.transmissionPeriod = _latestDynamicDeviceInfo.transmissionPeriod;
					}

					// Make sure productId value is defined.
					if (!Enum.IsDefined(typeof(ProductId), _deviceToConnect.productId))
					{
						_deviceToConnect.productId = ProductId.Undefined;
					}

					if (!Enum.IsDefined(typeof(VariantType), (VariantType)_deviceToConnect.variantId))
					{
						_deviceToConnect.variantId = (byte)VariantType.Undefined;
					}

					_connectedDevice = _deviceToConnect;

					if (_deviceConnectSuccessCallback != null)
					{
						_deviceConnectSuccessCallback.Invoke();
					}

					OnConnectionStatusChanged(ConnectionStatus.Connected, _connectedDevice);

					StartDeviceMonitor();

					StopDeviceConnection();
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}
			#endif // UNITY_EDITOR
		}

		/// <summary>
		/// Enables the device monitor
		/// </summary>
		private void StartDeviceMonitor()
		{
			_pollDeviceMonitor = true;

			// NB The device monitor runs on the same time interval as the connection routine
			_nextDeviceMonitorTime = Time.unscaledTime + WearableConstants.DeviceUSBConnectUpdateIntervalInSeconds;
		}

		/// <summary>
		/// Halts the device monitor
		/// </summary>
		private void StopDeviceMonitor()
		{
			_pollDeviceMonitor = false;
			_nextDeviceMonitorTime = float.PositiveInfinity;
		}

		/// <summary>
		/// Monitors the current device SessionStatus until a non-Open session status is returned. Once this has occured,
		/// the device has become disconnected and should render all state as such.
		/// </summary>
		private void MonitorDeviceSession()
		{
			#if UNITY_EDITOR
			if (_sessionStatus != SessionStatus.Open)
			{
				if (string.IsNullOrEmpty(_statusMessage.ToString()))
				{
					Debug.Log(WearableConstants.DeviceConnectionMonitorWarning);
				}
				else
				{
					Debug.LogFormat(WearableConstants.DeviceConnectionMonitorWarningWithMessage, _statusMessage);
				}

				if (_connectedDevice != null)
				{
					OnConnectionStatusChanged(ConnectionStatus.Disconnected, _connectedDevice);
				}

				_config.DisableAllSensors();
				_config.DisableAllGestures();

				StopDeviceMonitor();

				_connectedDevice = null;
			}
			#endif // UNITY_EDITOR
		}

		/// <summary>
		/// Halts the device connection routine
		/// </summary>
		private void StopDeviceConnection()
		{
			_performDeviceConnection = false;
			_deviceConnectFailureCallback = null;
			_deviceConnectSuccessCallback = null;
			_nextDeviceConnectTime = float.PositiveInfinity;
		}

		#if UNITY_EDITOR
		private USBAppIntentProfile MakeUSBProfile(AppIntentProfile appIntentProfile)
		{
			USBAppIntentProfile usbProfile = new USBAppIntentProfile();

			if (appIntentProfile != null)
			{
				// Sensors
				usbProfile.sensorBitmask = 0;
				for (int i = 0; i < WearableConstants.SensorIds.Length; i++)
				{
					SensorId sensor = WearableConstants.SensorIds[i];

					// Does this profile require this sensor?
					if (appIntentProfile.GetSensorInProfile(sensor))
					{
						SensorFlags sensorBit = WearableTools.GetSensorFlag(sensor);
						usbProfile.sensorBitmask |= (int)sensorBit;
					}
				}

				// Gestures
				usbProfile.gestureBitmask = 0;
				for (int i = 0; i < WearableConstants.GestureIds.Length; i++)
				{
					GestureId gesture = WearableConstants.GestureIds[i];

					// Does this profile require this gesture?
					if (appIntentProfile.GetGestureInProfile(gesture))
					{
						GestureFlags gestureBit = WearableTools.GetGestureFlag(gesture);
						usbProfile.gestureBitmask |= (int)gestureBit;
					}
				}

				usbProfile.updateIntervalBitmask = 0;
				for (int i = 0; i < WearableConstants.UpdateIntervals.Length; i++)
				{
					SensorUpdateInterval interval = WearableConstants.UpdateIntervals[i];

					// Does this profile require this update interval?
					if (appIntentProfile.GetIntervalInProfile(interval))
					{
						int intervalBit = WearableTools.SensorUpdateIntervalToBit(interval);
						usbProfile.updateIntervalBitmask |= intervalBit;
					}
				}
			}

			return usbProfile;
		}
		#endif

		#endregion // Private

		#region DLL Imports

		#if UNITY_EDITOR

		/// <summary>
		/// This struct matches the plugin bridge definition and is only used as a temporary convert from the native
		/// code struct to the public struct.
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		private struct USBSensorFrame
		{
			public int timestamp;
			public int deltaTime;
			public SensorVector3 acceleration;
			public SensorVector3 angularVelocity;
			public SensorQuaternion rotationNineDof;
			public SensorQuaternion rotationSixDof;
		}

		/// <summary>
		/// This struct matches the plugin bridge definition and is only used as a temporary convert from the native
		/// code struct to the public struct.
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		private struct USBGestureData
		{
			public int timestamp;
			public GestureId gesture;
		}

		/// <summary>
		/// This struct matches the plugin bridge definition.  It contains
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		private struct USBAppIntentProfile
		{
			public int sensorBitmask;
			public int gestureBitmask;
			public int updateIntervalBitmask;
		}

		/// <summary>
		/// This struct allows passing a device configuration to and from the USB bridge.
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		private struct USBDeviceConfiguration
		{
			public int intervalMilliseconds;

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
		}

		/// <summary>
		/// This struct helps us populate fields in Device.  It has information which does not change at runtime.
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		private struct USBStaticDeviceInfo
		{
			public int productId;
			public int variantId;
			public int availableSensors;
			public int availableGestures;
			public int maximumActiveSensors;
			public int availableUpdateIntervals;
			public int maximumPayloadPerTransmissionPeriod;
		}

		/// <summary>
		/// This struct helps us populate fields in Device.  It has information which could change at runtime.
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		private struct USBDynamicDeviceInfo
		{
			public int deviceStatus;
			public int transmissionPeriod;
		}

		/// <summary>
		/// Initializes the USB DLL.  This only needs to be called once per session.
		/// </summary>
		[DllImport("BoseWearableUSBBridge")]
		private static extern void WearableUSBInitialize();

		/// <summary>
		/// Starts the search for available Wearable devices on USB.
		/// </summary>
		[DllImport("BoseWearableUSBBridge")]
		private static extern void WearableUSBRefreshDeviceList();

		/// <summary>
		/// Returns number of available USB Wearable devices.
		/// </summary>
		[DllImport("BoseWearableUSBBridge")]
		private static extern int WearableUSBGetNumDiscoveredDevices();

		/// <summary>
		/// Returns UID of available Wearable device at index.  Returns false if index is invalid.
		/// </summary>
		[DllImport("BoseWearableUSBBridge", CharSet = CharSet.Unicode)]
		private static extern int WearableUSBGetDiscoveredDeviceUID(int index, StringBuilder uidBuilder, int builderLength);

		/// <summary>
		/// Returns name of available Wearable device at index.  Returns false if index is invalid.
		/// </summary>
		[DllImport("BoseWearableUSBBridge", CharSet = CharSet.Unicode)]
		private static extern int WearableUSBGetDiscoveredDeviceName(int index, StringBuilder nameBuilder, int builderLength);

		/// <summary>
		/// Returns name of available Wearable device at index.  Returns false if index is invalid.
		/// </summary>
		[DllImport("BoseWearableUSBBridge", CharSet = CharSet.Unicode)]
		private static extern int WearableUSBGetDiscoveredDeviceFirmwareVersion(int index, StringBuilder firmware, int builderLength);

		/// <summary>
		/// Returns name of available Wearable device at index.  Returns false if index is invalid.
		/// </summary>
		[DllImport("BoseWearableUSBBridge")]
		private static extern int WearableUSBGetDiscoveredDeviceIsConnected(int index);

		/// <summary>
		/// Gets info which does not change at runtime.  This is only available once a session has been opened.
		/// Returns zero if no device is connected.
		/// </summary>
		[DllImport("BoseWearableUSBBridge")]
		private static extern unsafe bool WearableUSBGetStaticDeviceInfo(USBStaticDeviceInfo *staticInfo);

		/// <summary>
		/// Returns the VariantId of a device. This will default to 0 if there is not an open session yet.
		/// The VariantId of a device is only available once a session has been opened.
		/// Returns zero if no device is connected.
		/// </summary>
		[DllImport("BoseWearableUSBBridge")]
		private static extern unsafe bool WearableUSBGetDynamicDeviceInfo(USBDynamicDeviceInfo *dynamicInfo);

		/// <summary>
		/// Sets device profile data in the bridge.  This data is used to confirm that the device chosen
		/// for a connection meets the needs in the profile.  If it does not, the connection fails.
		/// </summary>
		[DllImport("BoseWearableUSBBridge")]
		private static extern unsafe void WearableUSBSetAppIntentProfile(USBAppIntentProfile* appIntentProfile);

		/// <summary>
		/// Check whether the given app intent profile is valid for the connected AR device.
		/// </summary>
		[DllImport("BoseWearableUSBBridge")]
		private static extern unsafe bool WearableUSBCheckAppIntentProfileForConnectedDevice(USBAppIntentProfile* appIntentProfile);

		/// <summary>
		/// Attempts to open a session with a specific Wearable Device by way of <paramref name="deviceUid"/>.
		/// </summary>
		[DllImport("BoseWearableUSBBridge", CharSet = CharSet.Unicode)]
		private static extern void WearableUSBOpenSession(string deviceUid);

		/// <summary>
		/// Assesses the SessionStatus of the currently opened session for a specific Wearable device. If there has
		/// been an error, <paramref name="errorMsg"/> will be populated with the text contents of the error message.
		/// The return value is really a SessionStatus.
		/// </summary>
		[DllImport("BoseWearableUSBBridge", CharSet = CharSet.Unicode)]
		private static extern int WearableUSBGetSessionStatus(StringBuilder errorMsg, int bufferLength);

		/// <summary>
		/// Closes the session.  You can open another.
		/// </summary>
		[DllImport("BoseWearableUSBBridge")]
		private static extern void WearableUSBCloseSession();

		/// <summary>
		/// Have the DLL fetch the latest state from the device.  Returns 0 if not connected.
		/// </summary>
		[DllImport("BoseWearableUSBBridge")]
		private static extern int WearableUSBUpdate();

		/// <summary>
		/// Returns the next unread USBSensorFrame chronologically.  Returns false if no additional frames are available.
		/// </summary>
		[DllImport("BoseWearableUSBBridge")]
		private static extern unsafe bool WearableUSBGetNextSensorFrame(USBSensorFrame* sensorFrame);

		/// <summary>
		/// Returns the next unread USBGestureData chronologically.  Returns false if no additional data is available.
		/// </summary>
		[DllImport("BoseWearableUSBBridge")]
		private static extern unsafe bool WearableUSBGetNextGestureData(USBGestureData* gestureData);

		/// <summary>
		/// Tells us about the most recent effort to configure sensors and gestures.  Each value could be ConfigStatus::Pending
		/// if a configuration command is still making its round trip.  Returns true if a call to set the device configuration
		/// has completed since the previous call to this function.
		/// </summary>
		[DllImport("BoseWearableUSBBridge")]
		private static extern unsafe bool WearableUSBGetConfigStatus(ConfigStatus *sensorStatus, ConfigStatus* gestureStatus);

		/// <summary>
		/// Sets the device configuration (sensors, gestures, and update rate)
		/// </summary>
		/// <param name="deviceConfig"></param>
		/// <returns></returns>
		[DllImport("BoseWearableUSBBridge")]
		private static extern void WearableUSBSetDeviceConfiguration(USBDeviceConfiguration deviceConfig);

		/// <summary>
		/// Requests that the device report its configuration. Poll <see cref="WearableUSBHasReceivedDeviceConfiguration"/>
		/// to determine when a call to <see cref="WearableUSBGetDeviceConfiguration"/> will report the most up-to-date config.
		/// </summary>
		/// <returns></returns>
		[DllImport("BoseWearableUSBBridge")]
		private static extern void WearableUSBRequestDeviceConfiguration();

		/// <summary>
		/// Returns true when the data requested by <see cref="WearableUSBRequestDeviceConfiguration"/> is ready to read
		/// using <see cref="WearableUSBGetDeviceConfiguration"/>.
		/// </summary>
		/// <returns></returns>
		[DllImport("BoseWearableUSBBridge")]
		private static extern bool WearableUSBHasReceivedDeviceConfiguration();

		/// <summary>
		/// Gets the device config. Call <see cref="WearableUSBRequestDeviceConfiguration"/> beforehand to ensure that
		/// this data is up-to-date.
		/// </summary>
		/// <returns></returns>
		[DllImport("BoseWearableUSBBridge")]
		private static extern USBDeviceConfiguration WearableUSBGetDeviceConfiguration();

		/// <summary>
		/// Turn on or off logging of TAP commands sent, responses received, and warnings.  Errors are logged even
		/// if this is off.
		/// </summary>
		[DllImport("BoseWearableUSBBridge")]
		private static extern void WearableUSBSetDebugLogging(bool loggingOn);

		#endif // UNITY_EDITOR

		#endregion // DLL Imports
	}
}
