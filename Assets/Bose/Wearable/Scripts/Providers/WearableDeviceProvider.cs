using System;
using UnityEngine;

namespace Bose.Wearable
{
	[Serializable]
	internal sealed partial class WearableDeviceProvider : WearableProviderBase
	{
		#region Provider Unique
		/// <summary>
		/// Represents a session with an Wearable Device.
		/// </summary>
		private enum SessionStatus
		{
			Closed,
			Opening,
			Open
		}

		private ConnectionStatus _currentConnectionStatus;

		/// <summary>
		/// The RSSI threshold below which devices will be filtered out.
		/// </summary>
		public int RSSIFilterThreshold
		{
			get
			{
				return _RSSIFilterThreshold == 0
					? WearableConstants.DefaultRSSIThreshold
					: _RSSIFilterThreshold;
			}
		}

		/// <summary>
		/// Sets the Received Signal Strength Indication (RSSI) filter level; devices underneath the rssiThreshold filter
		/// threshold will not be made available to connect to. A valid value for <paramref name="rssiThreshold"/> is
		/// set between -70 and -30; anything outside of that range will be clamped to the nearest allowed value.
		/// </summary>
		/// <param name="rssiThreshold"></param>
		public void SetRssiFilter(int rssiThreshold)
		{
			_RSSIFilterThreshold = Mathf.Clamp(rssiThreshold, WearableConstants.MinimumRSSIValue, WearableConstants.MaximumRSSIValue);
		}

		/// <summary>
		/// Indicates whether the SDK has been initialized to simulate available and connected devices.
		/// </summary>
		public bool SimulateHardwareDevices
		{
			get { return _simulateHardwareDevices; }
		}

		[Tooltip(WearableConstants.SimulateHardwareDeviceTooltip), SerializeField]
		private bool _simulateHardwareDevices;

		#endregion

		#region Provider API

		internal override void SearchForDevices(
			AppIntentProfile appIntentProfile,
			Action<Device[]> onDevicesUpdated,
			bool autoReconnect,
			float autoReconnectTimeout)
		{
			StopSearchingForDevices();

			base.SearchForDevices(appIntentProfile, onDevicesUpdated, autoReconnect, autoReconnectTimeout);

			if (onDevicesUpdated == null)
			{
				return;
			}

			#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
			StartSearch(appIntentProfile, RSSIFilterThreshold);
			_performDeviceSearch = true;
			_nextDeviceSearchTime = Time.unscaledTime + WearableConstants.DeviceSearchUpdateIntervalInSeconds;
			OnConnectionStatusChanged(autoReconnect ? ConnectionStatus.AutoReconnect : ConnectionStatus.Searching);
			#else
			OnReceivedSearchDevices(WearableConstants.EmptyDeviceList);
			#endif
		}

		internal override void StopSearchingForDevices()
		{
			base.StopSearchingForDevices();

			if (_performDeviceSearch)
			{
				_performDeviceSearch = false;
				_nextDeviceSearchTime = float.PositiveInfinity;

				StopSearch();
			}
		}

		internal override void CancelDeviceConnection()
		{
			CancelDeviceConnectionInternal();

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
			_nextDeviceConnectTime = Time.unscaledTime + WearableConstants.DeviceConnectUpdateIntervalInSeconds;
			_pollForConfigStatus = false;

			OpenSession(_deviceToConnect.uid);

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

			CloseSession();
		}

		public override DeviceConnectionInfo GetDeviceConnectionInfo()
		{
			return GetDeviceConnectionInfoInternal();
		}

		internal override FirmwareUpdateInformation GetFirmwareUpdateInformation()
		{
			return GetFirmwareUpdateInformationInternal();
		}

		internal override void SelectFirmwareUpdateOption(int index)
		{
			SelectFirmwareUpdateOptionInternal(index);
		}

		internal override void SetDeviceConfiguration(WearableDeviceConfig config)
		{
			SetDeviceConfigurationInternal(config);
			_pollForConfigStatus = true;
		}

		internal override WearableDeviceConfig GetCachedDeviceConfiguration()
		{
			return _connectedDevice.HasValue ? _config : WearableConstants.DisabledDeviceConfig;
		}

		protected override void RequestDeviceConfigurationInternal()
		{
			OnReceivedDeviceConfiguration(GetDeviceConfigurationInternal());
		}

		protected override void RequestIntentProfileValidationInternal(AppIntentProfile appIntentProfile)
		{
			bool profileIsValid = IsAppIntentProfileValid(appIntentProfile);

			OnReceivedIntentValidationResponse(profileIsValid);
		}

		internal override DynamicDeviceInfo GetDynamicDeviceInfo()
		{
			return GetDynamicDeviceInfoInternal();
		}

		internal override void OnInitializeProvider()
		{
			if (_initialized)
			{
				return;
			}

			WearableDeviceInitialize(_simulateHardwareDevices);

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
			// Request the latest updates for this frame
			if (_connectedDevice != null)
			{
				GetLatestSensorUpdates();
			}

			// Check if it's time to query discovered devices
			if (_performDeviceSearch && Time.unscaledTime >= _nextDeviceSearchTime)
			{
				_nextDeviceSearchTime += WearableConstants.DeviceSearchUpdateIntervalInSeconds;
				Device[] devices = GetDiscoveredDevices();
				OnReceivedSearchDevices(devices);
			}

			// Check if it's time to query the connection routine
			if (_performDeviceConnection && Time.unscaledTime >= _nextDeviceConnectTime)
			{
				_nextDeviceConnectTime += WearableConstants.DeviceConnectUpdateIntervalInSeconds;
				PerformDeviceConnection();
			}

			// Check if it's time to query the device monitor
			if (_pollDeviceMonitor && Time.unscaledTime >= _nextDeviceMonitorTime)
			{
				// NB: The monitor uses the same time interval
				_nextDeviceMonitorTime += WearableConstants.DeviceConnectUpdateIntervalInSeconds;
				MonitorDeviceSession();
			}

			// Allow the provider base to do its own update work
			base.OnUpdate();
		}

		#endregion

		#region Private

		// Config status
		private WearableDeviceConfig _config;

		// Device search
		private bool _performDeviceSearch;
		private float _nextDeviceSearchTime;

		// Device connection
		private bool _performDeviceConnection;
		private Device _deviceToConnect;
		#pragma warning disable 0414
		private Action _deviceConnectSuccessCallback;
		private Action _deviceConnectFailureCallback;
		#pragma warning restore 0414
		private float _nextDeviceConnectTime;

		// Device monitoring
		private bool _pollDeviceMonitor;
		private float _nextDeviceMonitorTime;
		private bool _pollForConfigStatus;

		private int _RSSIFilterThreshold;


		internal WearableDeviceProvider()
		{
			_config = new WearableDeviceConfig();
		}

		/// <summary>
		/// Used internally by WearableControl to get the latest buffer of SensorFrame updates from
		/// the Wearable Device; the newest frame in that batch is set as the CurrentSensorFrame.
		/// </summary>
		private void GetLatestSensorUpdates()
		{
			_currentSensorFrames.Clear();

			GetLatestSensorUpdatesInternal();

			if (_currentSensorFrames.Count > 0)
			{
				_lastSensorFrame = _currentSensorFrames[_currentSensorFrames.Count - 1];

				OnSensorsUpdated(_lastSensorFrame);
			}

			_currentGestureData.Clear();

			GetLatestGestureUpdatesInternal();

			if (_currentGestureData.Count > 0)
			{
				for (int currentGestureIndex = 0; currentGestureIndex < _currentGestureData.Count; ++currentGestureIndex)
				{
					OnGestureDetected(_currentGestureData[currentGestureIndex].gestureId);
				}
			}

			if (_pollForConfigStatus)
			{
				ConfigStatus sensor = GetSensorConfigStatusInternal();
				ConfigStatus gesture = GetGestureConfigStatusInternal();

				if (!(sensor  == ConfigStatus.Pending || sensor  == ConfigStatus.Idle) &&
				    !(gesture == ConfigStatus.Pending || gesture == ConfigStatus.Idle))
				{
					_pollForConfigStatus = false;
				}

				if (sensor == ConfigStatus.Failure || gesture == ConfigStatus.Failure)
				{
					OnConfigurationFailed(sensor, gesture);
				}

				if (sensor == ConfigStatus.Success && gesture == ConfigStatus.Success)
				{
					OnConfigurationSucceeded();
				}
			}

			_config = GetDeviceConfigurationInternal();
		}

		/// <summary>
		/// Used internally to get the latest list of discovered devices from
		/// the native SDK.
		/// </summary>
		private Device[] GetDiscoveredDevices()
		{
			return GetDiscoveredDevicesInternal();
		}

		/// <summary>
		/// Attempts to create a session to a specified device and then checks for the session status perpetually until
		/// a SessionStatus of either Open or Closed is returned, equating to either successful or failed.
		/// </summary>
		private void PerformDeviceConnection()
		{
			if (Application.isEditor)
			{
				return;
			}

			string errorMessage = string.Empty;

			ConnectionStatus sessionStatus = GetConnectionStatus(ref errorMessage);
			switch (sessionStatus)
			{
				// Receiving a session status of Closed while attempting to open a session indicates an error occured.
				case ConnectionStatus.Failed:
					if (string.IsNullOrEmpty(errorMessage))
					{
						Debug.LogWarning(WearableConstants.DeviceConnectionFailed);
					}
					else
					{
						Debug.LogWarningFormat(WearableConstants.DeviceConnectionFailedWithMessage, errorMessage);
					}

					StopDeviceConnection();

					if (_deviceConnectFailureCallback != null)
					{
						_deviceConnectFailureCallback.Invoke();
					}

					OnConnectionStatusChanged(ConnectionStatus.Failed, _deviceToConnect);

					break;

				case ConnectionStatus.Connecting:
					// Device is still connecting, just wait
					break;

				case ConnectionStatus.SecurePairingRequired:
					if (_currentConnectionStatus != ConnectionStatus.SecurePairingRequired)
					{
						OnConnectionStatusChanged(ConnectionStatus.SecurePairingRequired, _deviceToConnect);
					}
					break;

				case ConnectionStatus.FirmwareUpdateAvailable:
					if (_currentConnectionStatus != ConnectionStatus.FirmwareUpdateAvailable)
					{
						OnConnectionStatusChanged(ConnectionStatus.FirmwareUpdateAvailable, _deviceToConnect);
					}
					break;

				case ConnectionStatus.FirmwareUpdateRequired:
					if (_currentConnectionStatus != ConnectionStatus.FirmwareUpdateRequired)
					{
						OnConnectionStatusChanged(ConnectionStatus.FirmwareUpdateRequired, _deviceToConnect);
					}
					break;

				case ConnectionStatus.Connected:
					Debug.Log(WearableConstants.DeviceConnectionOpened);

					// ProductId and VariantId are only accessible after a connection has been opened. Update the values for the _connectDevice.
					GetDeviceInfo(ref _deviceToConnect);

					// Make sure productId and variantId values are defined.
					if (!Enum.IsDefined(typeof(ProductId), _deviceToConnect.productId))
					{
						_deviceToConnect.productId = ProductId.Undefined;
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
					throw new ArgumentOutOfRangeException("sessionStatus", sessionStatus, null);
			}

			_currentConnectionStatus = sessionStatus;
		}

		/// <summary>
		/// Enables the device monitor
		/// </summary>
		private void StartDeviceMonitor()
		{
			_pollDeviceMonitor = true;

			// NB The device monitor runs on the same time interval as the connection routine
			_nextDeviceMonitorTime = Time.unscaledTime + WearableConstants.DeviceConnectUpdateIntervalInSeconds;
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
			if (Application.isEditor)
			{
				return;
			}

			string errorMessage = string.Empty;

			SessionStatus sessionStatus = (SessionStatus)GetSessionStatus(ref errorMessage);
			if (sessionStatus != SessionStatus.Open)
			{
				if (string.IsNullOrEmpty(errorMessage))
				{
					Debug.Log(WearableConstants.DeviceConnectionMonitorWarning);
				}
				else
				{
					Debug.LogFormat(WearableConstants.DeviceConnectionMonitorWarningWithMessage, errorMessage);
				}

				if (_connectedDevice != null)
				{
					OnConnectionStatusChanged(Wearable.ConnectionStatus.Disconnected, _connectedDevice);
				}

				_config.DisableAllSensors();
				_config.DisableAllGestures();

				StopDeviceMonitor();

				_connectedDevice = null;
			}
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

		internal override void SetAppFocusChanged(bool hasFocus)
		{
			SetAppFocusChangedInternal(hasFocus);
		}

		#endregion
	}
}
