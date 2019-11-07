using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bose.Wearable
{
	/// <summary>
	/// Provides a minimal data provider that allows connection to a virtual device, and logs messages when provider
	/// methods are called. Never generates data frames.
	/// </summary>
	[Serializable]
	public sealed class WearableDebugProvider : WearableProviderBase
	{
		[Serializable]
		private enum RotationType
		{
			Euler,
			AxisAngle
		}

		[Serializable]
		public enum MovementSimulationMode
		{
			Off,
			ConstantRate,
			MobileDevice
		}

		/// <summary>
		/// Keeps track of the connection state machine's current phase. This is internal to the debug provider,
		/// and is designed to mimic some hidden states within the SDK.
		/// </summary>
		private enum ConnectionPhase
		{
			Idle,
			Connecting,
			CheckFirmware,
			AwaitFirmwareResponse,
			SecurePairing,
			CheckIntents,
			GenerateIntentsResponse,
			ConnectingBeforeFailed,
			Failed,
			Succeeded,
			Cancelled,
			DisconnectedForUpdate
		}

		public string Name
		{
			get { return _name; }
			set {_name = value; }
		}

		public int RSSI
		{
			get { return _rssi; }
			set { _rssi = value; }
		}

		public string FirmwareVersion
		{
			get { return _firmwareVersion; }
			set
			{
				if (_connectedDevice.HasValue)
				{
					Debug.LogWarning(WearableConstants.DebugProviderCannotModifyWhileConnected);
					return;
				}

				_firmwareVersion = value;
			}
		}

		/// <summary>
		/// If true, act like the firmware version is sufficient for connection or intent validation.
		/// If false, the virtual device will act as through it is unsupported.
		/// </summary>
		public bool BoseAREnabled
		{
			get { return _boseArEnabled; }
			set
			{
				if (_connectedDevice.HasValue)
				{
					Debug.LogWarning(WearableConstants.DebugProviderCannotModifyWhileConnected);
					return;
				}

				_boseArEnabled = value;
			}
		}

		/// <summary>
		/// If true, act like a newer firmware version is available for update.
		/// </summary>
		public bool FirmwareUpdateAvailable
		{
			get { return _firmwareUpdateAvailable; }
			set
			{
				if (_connectedDevice.HasValue)
				{
					Debug.LogWarning(WearableConstants.DebugProviderCannotModifyWhileConnected);
					return;
				}

				_firmwareUpdateAvailable = value;
			}
		}

		public SensorFlags AvailableSensors
		{
			get { return _availableSensors; }
			set
			{
				if (_connectedDevice.HasValue)
				{
					Debug.LogWarning(WearableConstants.CannotModifySensorFlagsWarning);
					return;
				}

				_availableSensors = value;
			}
		}

		public GestureFlags AvailableGestures
		{
			get { return _availableGestures; }
			set
			{
				if (_connectedDevice.HasValue)
				{
					Debug.LogWarning(WearableConstants.CannotModifyGestureFlagsWarning);
					return;
				}

				_availableGestures = value;
			}
		}

		public string UID
		{
			get { return _uid; }
			set
			{
				if (_connectedDevice.HasValue)
				{
					Debug.LogWarning(WearableConstants.DebugProviderCannotModifyWhileConnected);
					return;
				}

				_uid = value;
			}
		}

		public ProductType ProductType
		{
			get { return WearableTools.GetProductType(_productId); }
			set
			{
				if (_connectedDevice.HasValue)
				{
					Debug.LogWarning(WearableConstants.DebugProviderCannotModifyWhileConnected);
					return;
				}

				_productId = WearableTools.GetProductId(value);
			}
		}

		public VariantType VariantType
		{
			get { return WearableTools.GetVariantType(ProductType, _variantId); }
			set
			{
				if (_connectedDevice.HasValue)
				{
					Debug.LogWarning(WearableConstants.DebugProviderCannotModifyWhileConnected);
					return;
				}

				_variantId = WearableTools.GetVariantId(ProductType, value);
			}
		}

		public bool Verbose
		{
			get { return _verbose; }
			set { _verbose = value; }
		}

		public float SimulatedDelayTime
		{
			get { return _simulatedDelayTime; }
			set { _simulatedDelayTime = Mathf.Max(0.0f, value); }
		}

		public MovementSimulationMode SimulatedMovement
		{
			get { return _simulatedMovementMode;  }
			set { _simulatedMovementMode = value; }
		}

		#region Provider Unique

		public void SimulateDisconnect()
		{
			if (_verbose)
			{
				Debug.Log(WearableConstants.DebugProviderSimulateDisconnect);
			}

			DisconnectFromDevice();
		}

		/// <summary>
		/// Simulate a triggered gesture. If multiple gestures are triggered in a single update, they will be
		/// triggered simultaneously.
		/// </summary>
		/// <param name="gesture"></param>
		public void SimulateGesture(GestureId gesture)
		{
			if (gesture != GestureId.None)
			{
				GestureData gestureData = new GestureData
				{
					gestureId = gesture,
					timestamp = _nextSensorUpdateTime
				};
				_pendingGestures.Enqueue(gestureData);
			}
		}

		/// <summary>
		/// Simulate the device status of the virtual device. Be aware that status is cleared upon connection.
		/// </summary>
		public void SetDeviceStatusFlagState(DeviceStatusFlags flag, bool state)
		{
			_dynamicDeviceInfo.deviceStatus.SetFlagValue(flag, state);
		}

		public void SimulateSensorServiceSuspended(SensorServiceSuspendedReason reason)
		{
			_dynamicDeviceInfo.deviceStatus.SetFlagValue(DeviceStatusFlags.SensorServiceSuspended, true);
			_dynamicDeviceInfo.deviceStatus.SetServiceSuspendedReason(reason);
		}

		public void SimulateSensorServiceResumed()
		{
			_dynamicDeviceInfo.deviceStatus.SetFlagValue(DeviceStatusFlags.SensorServiceSuspended, false);
			_dynamicDeviceInfo.deviceStatus.SetServiceSuspendedReason(0);
		}

		#endregion

		#region WearableProvider Implementation

		internal override void SearchForDevices(
			AppIntentProfile appIntentProfile,
			Action<Device[]> onDevicesUpdated,
			bool autoReconnect,
			float autoReconnectTimeout)
		{
			_connectionIntentProfile = appIntentProfile;
			StopSearchingForDevices();

			base.SearchForDevices(appIntentProfile, onDevicesUpdated, autoReconnect, autoReconnectTimeout);

			if (_verbose)
			{
				Debug.Log(WearableConstants.DebugProviderSearchingForDevices);
			}

			OnConnectionStatusChanged(autoReconnect ? ConnectionStatus.AutoReconnect : ConnectionStatus.Searching);
			_searchingForDevice = true;
			_nextDeviceSearchUpdateTime = Time.unscaledTime;
		}

		internal override void StopSearchingForDevices()
		{
			base.StopSearchingForDevices();

			if (!_searchingForDevice)
			{
				return;
			}

			_searchingForDevice = false;

			OnConnectionStatusChanged(ConnectionStatus.Disconnected);

			if (_verbose)
			{
				Debug.Log(WearableConstants.DebugProviderStoppedSearching);
			}
		}

		internal override void CancelDeviceConnection()
		{
			if (_searchingForDevice)
			{
				return;
			}

			// If the ConnectionStatus is not at a state where we can cancel the connection, return early
			if (!WearableConstants.ConnectingStates.Contains(ConnectionStatus))
			{
				return;
			}

			SetConnectionPhase(ConnectionPhase.Cancelled);

			if (_verbose)
			{
				Debug.Log(WearableConstants.DebugProviderCancelledConnectionPrompted);
			}
		}

		internal override void ConnectToDevice(Device device, Action onSuccess, Action onFailure)
		{
			_onConnectionSuccessCallback = onSuccess;
			_onConnectionFailureCallback = onFailure;

			StopSearchingForDevices();
			DisconnectFromDevice();

			UpdateVirtualDeviceInfo();

			if (_verbose)
			{
				Debug.Log(WearableConstants.DebugProviderConnectingToDevice);
			}

			// Disallow connection to anything but the virtual device
			if (device != _virtualDevice)
			{
				Debug.LogWarning(WearableConstants.DebugProviderInvalidConnectionWarning);
				SetConnectionPhase(ConnectionPhase.ConnectingBeforeFailed);
				return;
			}

			SetConnectionPhase(ConnectionPhase.Connecting);
			OnConnectionStatusChanged(ConnectionStatus.Connecting);
		}

		internal override void DisconnectFromDevice()
		{
			_config.DisableAllSensors();
			_config.DisableAllGestures();

			if (_connectedDevice == null)
			{
				return;
			}

			if (_verbose)
			{
				Debug.Log(WearableConstants.DebugProviderDisconnectedToDevice);
			}

			OnConnectionStatusChanged(ConnectionStatus.Disconnected, _virtualDevice);

			SetConnectionPhase(ConnectionPhase.Idle);
			_virtualDevice.isConnected = false;
			_connectedDevice = null;
			_waitingToSendConfigSuccess = false;
			_waitingToSendConfigFailure = false;
			_waitingToSendIntentValidation = false;
			_waitingToSendConfigRequestResponse = false;
		}

		internal override FirmwareUpdateInformation GetFirmwareUpdateInformation()
		{
			return _updateInformation;
		}

		internal override void SelectFirmwareUpdateOption(int index)
		{
			// If a connection is not in process and waiting for a firmware update response, then this method
			// was called in error. Abort, so as not to restart the state machine halfway through.
			if (ConnectionStatus != ConnectionStatus.FirmwareUpdateRequired &&
				ConnectionStatus != ConnectionStatus.FirmwareUpdateAvailable)
			{
				return;
			}

			AlertStyle style = _updateInformation.options[index].style;
			if (style == AlertStyle.Affirmative)
			{
				// In a real flow, the user would be taken to the Bose app and the firmware updated.
				// Here, all we can do is spit out a warning and cancel the attempt.
				Debug.LogWarning(WearableConstants.DebugProviderFirmwareUpdateWarning);
				SetConnectionPhase(ConnectionPhase.Cancelled);
			}
			else
			{
				if (ConnectionStatus == ConnectionStatus.FirmwareUpdateRequired)
				{
					// The cancelled firmware update was mandatory; connection must fail.
					if (_verbose)
					{
						Debug.LogError(WearableConstants.DebugProviderSkippedRequiredUpdate);
					}

					SetConnectionPhase(ConnectionPhase.Failed);
				}
				else
				{
					// The cancelled firmware update was optional, so we can move on and finalize the connection.
					if (_verbose)
					{
						Debug.Log(WearableConstants.DebugProviderSkippedOptionalUpdate);
					}

					SetConnectionPhase(ConnectionPhase.Succeeded);
				}
			}
		}

		internal override WearableDeviceConfig GetCachedDeviceConfiguration()
		{
			return _config;
		}

		protected override void RequestDeviceConfigurationInternal()
		{
			_waitingToSendConfigRequestResponse = true;
			_sendConfigRequestResponseTime = Time.unscaledTime + _simulatedDelayTime;
		}

		protected override void RequestIntentProfileValidationInternal(AppIntentProfile appIntentProfile)
		{
			if (_verbose)
			{
				Debug.Log(WearableConstants.DebugProviderIntentValidationRequested);
			}

			_waitingToSendIntentValidation = true;
			_sendIntentValidationTime = Time.unscaledTime + _simulatedDelayTime;
			_intentResponse = CheckIntentValidity(appIntentProfile);
		}

		internal override void SetDeviceConfiguration(WearableDeviceConfig config)
		{
			if (_dynamicDeviceInfo.deviceStatus.ServiceSuspended)
			{
				Debug.LogWarning(WearableConstants.DebugProviderSetConfigWhileSuspendedWarning);
				_waitingToSendConfigFailure = true;
				_sendConfigFailureTime = Time.unscaledTime + _simulatedDelayTime;
				return;
			}

			if (_verbose)
			{
				// Sensor info
				for (int i = 0; i < WearableConstants.SensorIds.Length; i++)
				{
					SensorId sensorId = WearableConstants.SensorIds[i];
					bool oldSensor = _config.GetSensorConfig(sensorId).isEnabled;
					bool newSensor = config.GetSensorConfig(sensorId).isEnabled;

					if (newSensor == oldSensor) continue;

					Debug.LogFormat(
						newSensor ? WearableConstants.DebugProviderStartSensor : WearableConstants.DebugProviderStopSensor,
						Enum.GetName(typeof(SensorId), sensorId));
				}

				// Gesture info
				for (int i = 0; i < WearableConstants.GestureIds.Length; i++)
				{
					GestureId gestureId = WearableConstants.GestureIds[i];

					if (gestureId == GestureId.None)
					{
						continue;
					}

					bool oldGesture = _config.GetGestureConfig(gestureId).isEnabled;
					bool newGesture = config.GetGestureConfig(gestureId).isEnabled;
					if (newGesture == oldGesture) continue;

					Debug.LogFormat(
						newGesture ? WearableConstants.DebugProviderEnableGesture : WearableConstants.DebugProviderDisableGesture,
						Enum.GetName(typeof(GestureId), gestureId));
				}


				// Update interval
				SensorUpdateInterval oldInterval = _config.updateInterval;
				SensorUpdateInterval newInterval = config.updateInterval;
				if (oldInterval != newInterval)
				{
					Debug.LogFormat(
						WearableConstants.DebugProviderSetUpdateInterval,
						Enum.GetName(typeof(SensorUpdateInterval), newInterval));
				}
			}

			_config.CopyValuesFrom(config);
			_waitingToSendConfigSuccess = true;
			_sendConfigSuccessTime = Time.unscaledTime + _simulatedDelayTime;
		}

		internal override DynamicDeviceInfo GetDynamicDeviceInfo()
		{
			return _dynamicDeviceInfo;
		}

		internal override void SetAppFocusChanged(bool hasFocus)
		{
			if (_verbose)
			{
				if (hasFocus)
				{
					Debug.Log(WearableConstants.DebugProviderAppHasGainedFocus);
				}
				else
				{
					Debug.Log(WearableConstants.DebugProviderAppHasLostFocus);
				}
			}
		}

		internal override void OnInitializeProvider()
		{
			base.OnInitializeProvider();

			if (_verbose)
			{
				Debug.Log(WearableConstants.DebugProviderInit);
			}

			// NB: Must be done here, and not in the constructor, to avoid a serialization error.
			_gyro = Input.gyro;
		}

		internal override void OnDestroyProvider()
		{
			base.OnDestroyProvider();

			if (_verbose)
			{
				Debug.Log(WearableConstants.DebugProviderDestroy);
			}
		}

		internal override void OnEnableProvider()
		{
			base.OnEnableProvider();

			if (_verbose)
			{
				Debug.Log(WearableConstants.DebugProviderEnable);
			}

			_wasGyroEnabled = _gyro.enabled;
			_gyro.enabled = true;
			_nextSensorUpdateTime = Time.unscaledTime;
			_pendingGestures.Clear();
			_waitingToSendConfigSuccess = false;
			_waitingToSendConfigFailure = false;
			_waitingToSendIntentValidation = false;
			_waitingToSendConfigRequestResponse = false;
		}

		internal override void OnDisableProvider()
		{
			base.OnDisableProvider();

			if (_verbose)
			{
				Debug.Log(WearableConstants.DebugProviderDisable);
			}

			_gyro.enabled = _wasGyroEnabled;
		}

		internal override void OnUpdate()
		{
			UpdateVirtualDeviceInfo();

			// Report found devices if searching.
			if (_searchingForDevice && Time.unscaledTime >= _nextDeviceSearchUpdateTime)
			{
				_nextDeviceSearchUpdateTime += WearableConstants.DeviceSearchUpdateIntervalInSeconds;

				if (_verbose)
				{
					Debug.Log(WearableConstants.DebugProviderFoundDevices);
				}

				var devices = new[] { _virtualDevice };

				OnReceivedSearchDevices(devices);
			}

			// Handle connection states
			if (_connectionPhase != ConnectionPhase.Idle
			    && Time.unscaledTime >= _nextConnectionStateTime)
			{
				PerformDeviceConnection();
			}

			// Clear the current frames; _lastSensorFrame will retain its previous value.
			_currentSensorFrames.Clear();
			_currentGestureData.Clear();

			if (_connectedDevice.HasValue)
			{
				// Configuration status
				if (_waitingToSendConfigSuccess && Time.unscaledTime >= _sendConfigSuccessTime)
				{
					_waitingToSendConfigSuccess = false;
					OnConfigurationSucceeded();
				}

				if (_waitingToSendConfigFailure && Time.unscaledTime >= _sendConfigFailureTime)
				{
					_waitingToSendConfigFailure = false;
					OnConfigurationFailed(ConfigStatus.Failure, ConfigStatus.Failure);
				}

				// Device configuration requests
				if (_waitingToSendConfigRequestResponse && Time.unscaledTime >= _sendConfigRequestResponseTime)
				{
					_waitingToSendConfigRequestResponse = false;
					OnReceivedDeviceConfiguration(_config.Clone());
				}

				// Intent validation
				if (_waitingToSendIntentValidation && Time.unscaledTime >= _sendIntentValidationTime)
				{
					_waitingToSendIntentValidation = false;
					OnReceivedIntentValidationResponse(_intentResponse);
				}

				// Sensor and gesture data
				while (Time.unscaledTime >= _nextSensorUpdateTime)
				{
					// If it's time to emit frames, do so until we have caught up.
					float deltaTime = WearableTools.SensorUpdateIntervalToSeconds(_config.updateInterval);
					_nextSensorUpdateTime += deltaTime;

					// If the service is mock-suspended, don't update any data. Continue to iterate through this loop,
					// however, so we don't fall behind when the service resumes. Drop all gestures that are pending.
					if (_dynamicDeviceInfo.deviceStatus.ServiceSuspended)
					{
						_pendingGestures.Clear();
						continue;
					}

					// Check if sensors need to be updated
					bool anySensorsEnabled = _config.HasAnySensorsEnabled();

					// Prepare the frame's timestamp for frame emission
					if (anySensorsEnabled)
					{
						// Update the timestamp and delta-time
						_lastSensorFrame.deltaTime = deltaTime;
						_lastSensorFrame.timestamp = _nextSensorUpdateTime;

						// Simulate movement
						if (_simulatedMovementMode == MovementSimulationMode.ConstantRate)
						{
							// Calculate rotation, which is used by all sensors.
							if (_rotationType == RotationType.Euler)
							{
								_rotation = Quaternion.Euler(_eulerSpinRate * _lastSensorFrame.timestamp);
							}
							else if (_rotationType == RotationType.AxisAngle)
							{
								_rotation = Quaternion.AngleAxis(
									_axisAngleSpinRate.w * _lastSensorFrame.timestamp,
									new Vector3(_axisAngleSpinRate.x, _axisAngleSpinRate.y, _axisAngleSpinRate.z).normalized);
							}
						}
						else
						{
							_rotation = Quaternion.identity;
						}

						// Update all active sensors, even if motion is not simulated
						if (_config.accelerometer.isEnabled && _virtualDevice.IsSensorAvailable(SensorId.Accelerometer))
						{
							UpdateAccelerometerData();
						}

						if (_config.gyroscope.isEnabled && _virtualDevice.IsSensorAvailable(SensorId.Gyroscope))
						{
							UpdateGyroscopeData();
						}

						if ((_config.rotationSixDof.isEnabled && _virtualDevice.IsSensorAvailable(SensorId.RotationSixDof)) ||
						    (_config.rotationNineDof.isEnabled && _virtualDevice.IsSensorAvailable(SensorId.RotationNineDof)))
						{
							UpdateRotationSensorData();
						}

						// Emit the frame
						_currentSensorFrames.Add(_lastSensorFrame);
						OnSensorsUpdated(_lastSensorFrame);
					}

					// Add any gestures simulated in the past sensor frame.
					UpdateGestureData();
					for (int i = 0; i < _currentGestureData.Count; i++)
					{
						OnGestureDetected(_currentGestureData[i].gestureId);
					}
				}
			}

			// Allow the provider base to do its own update work
			base.OnUpdate();
		}

		#endregion

		#region Private

		[SerializeField]
		private string _name;

		[SerializeField]
		private string _firmwareVersion;

		[SerializeField]
		private bool _boseArEnabled;

		[SerializeField]
		private bool _firmwareUpdateAvailable;

		[SerializeField]
		private bool _acceptSecurePairing;

		[SerializeField]
		private int _rssi;

		[SerializeField]
		private SensorFlags _availableSensors;

		[SerializeField]
		private GestureFlags _availableGestures;

		[SerializeField]
		private ProductId _productId;

		[SerializeField]
		private byte _variantId;

		[SerializeField]
		private string _uid;

		[SerializeField]
		private bool _verbose;

		[SerializeField]
		private float _simulatedDelayTime;

		[SerializeField]
		private MovementSimulationMode _simulatedMovementMode;

		[SerializeField]
		private Vector3 _eulerSpinRate;

		[SerializeField]
		private Vector4 _axisAngleSpinRate;

		[SerializeField]
		private RotationType _rotationType;

		[SerializeField]
		private DynamicDeviceInfo _dynamicDeviceInfo;

		private Quaternion _rotation;
		private readonly Queue<GestureData> _pendingGestures;

		private float _nextSensorUpdateTime;

		private readonly WearableDeviceConfig _config;

		private Device _virtualDevice;
		private bool _searchingForDevice;
		private float _nextDeviceSearchUpdateTime;

		private bool _waitingToSendConfigSuccess;
		private float _sendConfigSuccessTime;

		private bool _waitingToSendConfigFailure;
		private float _sendConfigFailureTime;

		private bool _waitingToSendConfigRequestResponse;
		private float _sendConfigRequestResponseTime;

		private bool _waitingToSendIntentValidation;
		private float _sendIntentValidationTime;
		private bool _intentResponse;

		private Gyroscope _gyro;
		private bool _wasGyroEnabled;

		private AppIntentProfile _connectionIntentProfile;
		private Action _onConnectionSuccessCallback;
		private Action _onConnectionFailureCallback;
		private float _nextConnectionStateTime;
		private ConnectionPhase _connectionPhase;

		private readonly FirmwareUpdateInformation _updateInformation;

		internal WearableDebugProvider()
		{
			_virtualDevice = new Device
			{
				name = _name,
				firmwareVersion = _firmwareVersion,
				rssi = _rssi,
				availableSensors = _availableSensors,
				availableGestures = _availableGestures,
				productId = _productId,
				variantId = _variantId,
				uid = _uid,
				transmissionPeriod = 0,
				maximumPayloadPerTransmissionPeriod = 0,
				// NB: an extra sensor needs to be added to account for RotationSource
				maximumActiveSensors = WearableConstants.SensorIds.Length + 1
			};

			_name = WearableConstants.DebugProviderDefaultDeviceName;
			_firmwareVersion = WearableConstants.DefaultFirmwareVersion;
			_boseArEnabled = true;
			_firmwareUpdateAvailable = false;
			_acceptSecurePairing = true;
			_rssi = WearableConstants.DebugProviderDefaultRSSI;
			_availableSensors = WearableConstants.AllSensors;
			_availableGestures = WearableConstants.AllGestures;
			_productId = WearableConstants.DebugProviderDefaultProductId;
			_variantId = WearableConstants.DebugProviderDefaultVariantId;
			_uid = WearableConstants.DebugProviderDefaultUID;
			_simulatedDelayTime = WearableConstants.DebugProviderDefaultDelayTime;

			_searchingForDevice = false;

			_verbose = true;

			_eulerSpinRate = Vector3.zero;
			_axisAngleSpinRate = Vector3.up;

			_config = new WearableDeviceConfig();

			_pendingGestures = new Queue<GestureData>();

			_nextSensorUpdateTime = 0.0f;
			_rotation = Quaternion.identity;

			_dynamicDeviceInfo = WearableConstants.EmptyDynamicDeviceInfo;

			_updateInformation = new FirmwareUpdateInformation
			{
				icon = BoseUpdateIcon.Music,
				options = new[]
				{
					new FirmwareUpdateAlertOption
					{
						style = AlertStyle.Affirmative
					},
					new FirmwareUpdateAlertOption
					{
						style = AlertStyle.Negative
					}
				}
			};
		}

		private void UpdateVirtualDeviceInfo()
		{
			_virtualDevice.name = _name;
			_virtualDevice.firmwareVersion = _firmwareVersion;
			_virtualDevice.rssi = _rssi;
			_virtualDevice.availableSensors = _availableSensors;
			_virtualDevice.availableGestures = _availableGestures;
			_virtualDevice.productId = _productId;
			_virtualDevice.variantId = _variantId;
			_virtualDevice.uid = _uid;

			// Dynamic info needs to be updated outside of ProviderBase's loop since it can change even when disconnected.
			_virtualDevice.transmissionPeriod = _dynamicDeviceInfo.transmissionPeriod;
			_virtualDevice.deviceStatus = _dynamicDeviceInfo.deviceStatus;
		}

		private void PerformDeviceConnection()
		{
			const float PhaseTransitionDelaySeconds = 0.75f;

			if (ConnectedDevice.HasValue)
			{
				return;
			}

			switch (_connectionPhase)
			{
				case ConnectionPhase.Idle:
					// Do nothing.
					return;

				case ConnectionPhase.Connecting:
					// Add a delay to simulate the SDK opening the session.

					SetConnectionPhase(ConnectionPhase.CheckFirmware, PhaseTransitionDelaySeconds);
					OnConnectionStatusChanged(ConnectionStatus.Connecting, _virtualDevice);
					break;

				case ConnectionPhase.CheckFirmware:
					// Request that the device's firmware be checked. In a real device, this is done by the SDK, but
					// here we emulate it using configurable flags.

					if (_boseArEnabled)
					{
						// Firmware is good; continue.
						if (_verbose)
						{
							Debug.Log(WearableConstants.DebugProviderFirmwareSufficient);
						}

						if (_virtualDevice.deviceStatus.SecurePairingRequired &&
						    !_virtualDevice.deviceStatus.AlreadyPairedToClient)
						{
							SetConnectionPhase(ConnectionPhase.SecurePairing);
						}
						else
						{
							SetConnectionPhase(ConnectionPhase.CheckIntents, PhaseTransitionDelaySeconds);
						}
					}
					else if (_firmwareUpdateAvailable)
					{
						// The firmware version is insufficient, but an update is available that adds support.
						if (_verbose)
						{
							Debug.Log(WearableConstants.DebugProviderFirmwareUpdateRequiredInfo);
						}

						OnConnectionStatusChanged(ConnectionStatus.FirmwareUpdateRequired);
						SetConnectionPhase(ConnectionPhase.AwaitFirmwareResponse);
					}
					else
					{
						// Firmware is insufficient, and no updates are available. Immediately fail; this device is
						// not supported.
						if (_verbose)
						{
							Debug.LogError(WearableConstants.DebugProviderNoFirmwareUpdateAvailableError);
						}

						SetConnectionPhase(ConnectionPhase.Failed);
					}
					break;

				case ConnectionPhase.AwaitFirmwareResponse:
					// Wait for the firmware update dialog to pass in a response.
					// The state transition itself happens in SelectFirmwareUpdateOption()
					break;

				case ConnectionPhase.SecurePairing:
					// Secure pairing was requested. Answer the request based on the set config.

					if (_verbose)
					{
						Debug.Log(WearableConstants.DebugProviderStartSecurePairing);
					}

					OnConnectionStatusChanged(ConnectionStatus.SecurePairingRequired, _virtualDevice);

					if (_acceptSecurePairing)
					{
						if (_verbose)
						{
							Debug.Log(WearableConstants.DebugProviderSecurePairingAccepted);
						}

						SetConnectionPhase(ConnectionPhase.CheckIntents, PhaseTransitionDelaySeconds);
					}
					else
					{
						if (_verbose)
						{
							Debug.LogWarning(WearableConstants.DebugProviderSecurePairingRejectedWarning);
						}

						SetConnectionPhase(ConnectionPhase.Failed, PhaseTransitionDelaySeconds);
					}

					break;

				case ConnectionPhase.CheckIntents:
					// Add a small delay to simulate waiting for an intent validation response to return.

					if (_verbose)
					{
						Debug.Log(WearableConstants.DebugProviderCheckingIntents);
					}

					OnConnectionStatusChanged(ConnectionStatus.Connecting, _virtualDevice);
					SetConnectionPhase(ConnectionPhase.GenerateIntentsResponse, PhaseTransitionDelaySeconds);
					break;

				case ConnectionPhase.GenerateIntentsResponse:
					// Generate the response to the intent validation request.

					if (_connectionIntentProfile == null)
					{
						Debug.Log(WearableConstants.DebugProviderNoIntentsSpecified);
					}

					// Unspecified intents are, by definition, valid.
					bool intentValid = _connectionIntentProfile == null ||
										CheckIntentValidity(_connectionIntentProfile);

					if (intentValid)
					{
						if (_verbose)
						{
							Debug.Log(WearableConstants.DebugProviderIntentsValid);
						}

						if (_firmwareUpdateAvailable)
						{
							// The current firmware version is good, but there is a newer version available.

							if (_verbose)
							{
								Debug.Log(WearableConstants.DebugProviderFirmwareUpdateAvailable);
							}

							OnConnectionStatusChanged(ConnectionStatus.FirmwareUpdateAvailable);
							SetConnectionPhase(ConnectionPhase.AwaitFirmwareResponse);
						}
						else
						{
							// The current firmware is good, and there are no updates available. We're done!
							SetConnectionPhase(ConnectionPhase.Succeeded);
						}
					}
					else
					{
						if (_verbose)
						{
							Debug.LogWarning(WearableConstants.DebugProviderIntentsNotValidWarning);
						}

						if (_firmwareUpdateAvailable)
						{
							// Intents not valid, but there is an update available that adds the requested functionality.

							if (_verbose)
							{
								Debug.Log(WearableConstants.DebugProviderFirmwareUpdateRequiredInfo);
							}

							OnConnectionStatusChanged(ConnectionStatus.FirmwareUpdateRequired);
							SetConnectionPhase(ConnectionPhase.AwaitFirmwareResponse);
						}
						else
						{
							// Intents not valid, and nothing we can do about it. Fail the connection.
							if (_verbose)
							{
								Debug.LogError(WearableConstants.DebugProviderNoFirmwareUpdateAvailableError);
							}

							SetConnectionPhase(ConnectionPhase.Failed);
						}
					}
					break;

				case ConnectionPhase.Cancelled:
					// The connection process was cancelled for some reason. (Does not invoke success or failure events)

					if (_verbose)
					{
						Debug.Log(WearableConstants.DebugProviderCancelledConnection);
					}

					SetConnectionPhase(ConnectionPhase.Idle);
					OnConnectionStatusChanged(ConnectionStatus.Cancelled, _virtualDevice);
					break;

				case ConnectionPhase.ConnectingBeforeFailed:
					// Add a delay to simulate the SDK taking some time to fail the connection
					SetConnectionPhase(ConnectionPhase.Failed, PhaseTransitionDelaySeconds);
					OnConnectionStatusChanged(ConnectionStatus.Connecting, _virtualDevice);
					break;

				case ConnectionPhase.Failed:
					// The connection process has failed. Halt the state machine.

					if (_verbose)
					{
						Debug.LogWarning(WearableConstants.DebugProviderFailedToConnect);
					}

					if (_onConnectionFailureCallback != null)
					{
						_onConnectionFailureCallback.Invoke();
					}

					SetConnectionPhase(ConnectionPhase.Idle);
					OnConnectionStatusChanged(ConnectionStatus.Failed, _virtualDevice);
					break;

				case ConnectionPhase.Succeeded:
					// The connection process has succeeded. Connect the virtual device and halt the state machine.

					if (_verbose)
					{
						Debug.Log(WearableConstants.DebugProviderConnectedToDevice);
					}

					_virtualDevice.isConnected = true;
					_connectedDevice = _virtualDevice;
					_nextSensorUpdateTime = Time.unscaledTime;

					if (_onConnectionSuccessCallback != null)
					{
						_onConnectionSuccessCallback.Invoke();
					}

					SetConnectionPhase(ConnectionPhase.Idle);
					OnConnectionStatusChanged(ConnectionStatus.Connected, _virtualDevice);
					break;

				case ConnectionPhase.DisconnectedForUpdate:
					// Connection must be aborted to allow for a firmware update.

					if (_verbose)
					{
						Debug.Log(WearableConstants.DebugProviderDisconnectedForUpdate);
					}

					SetConnectionPhase(ConnectionPhase.Idle);
					OnConnectionStatusChanged(ConnectionStatus.Disconnected, _virtualDevice);
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		/// <summary>
		/// Set the phase of the internal connection state machine, optionally adding a delay.
		/// </summary>
		/// <param name="phase"></param>
		/// <param name="delay"></param>
		private void SetConnectionPhase(ConnectionPhase phase, float delay = 0.0f)
		{
			if (delay > 0.0f)
			{
				_nextConnectionStateTime = Time.unscaledTime + delay;
			}
			else
			{
				_nextConnectionStateTime = 0.0f;
			}

			_connectionPhase = phase;
		}


		/// <summary>
		/// Simulate some acceleration data.
		/// </summary>
		private void UpdateAccelerometerData()
		{
			const float GravitationalAcceleration = 9.80665f;

			if (_simulatedMovementMode == MovementSimulationMode.MobileDevice)
			{
				Vector3 raw = Input.acceleration * GravitationalAcceleration;
				// Switches from right- to left-handed coördinates
				_lastSensorFrame.acceleration.value.Set(-raw.x, -raw.y, raw.z);
			}
			else
			{
				Quaternion invRot = new Quaternion(-_rotation.x, -_rotation.y, -_rotation.z, _rotation.w);
				_lastSensorFrame.acceleration.value = invRot * new Vector3(0.0f, GravitationalAcceleration, 0.0f);
			}

			_lastSensorFrame.acceleration.accuracy = SensorAccuracy.High;
		}

		/// <summary>
		/// Simulate some gyro data.
		/// </summary>
		private void UpdateGyroscopeData()
		{
			if (_simulatedMovementMode == MovementSimulationMode.MobileDevice)
			{
				Vector3 raw = _gyro.rotationRate;
				// Switches from right- to left-handed coördinates
				_lastSensorFrame.angularVelocity.value.Set(-raw.x, -raw.y, raw.z);
			}
			else
			{
				if (_rotationType == RotationType.Euler)
				{
					Quaternion invRot = new Quaternion(-_rotation.x, -_rotation.y, -_rotation.z, _rotation.w);
					_lastSensorFrame.angularVelocity.value = invRot * (_eulerSpinRate * Mathf.Deg2Rad);
				}
				else
				{
					// NB This doesn't need multiplication by invRot because _axisAnglesSpinRate.xyz is an eigenvector
					// of the rotation transform.
					Vector3 axis = new Vector3(_axisAngleSpinRate.x, _axisAngleSpinRate.y, _axisAngleSpinRate.z).normalized;
					_lastSensorFrame.angularVelocity.value = axis * _axisAngleSpinRate.w * Mathf.Deg2Rad;
				}
			}

			_lastSensorFrame.angularVelocity.accuracy = SensorAccuracy.High;
		}

		/// <summary>
		/// Simulate some rotation data.
		/// </summary>
		private void UpdateRotationSensorData()
		{
			SensorQuaternion rotation;
			if (_simulatedMovementMode == MovementSimulationMode.MobileDevice)
			{
				// This is based on an iPhone 6, but should be cross-compatible with other devices.
				Quaternion raw = _gyro.attitude;
				const float InverseRootTwo = 0.7071067812f; // 1 / sqrt(2)
				rotation.value = new Quaternion(
					InverseRootTwo * (raw.w - raw.x),
					InverseRootTwo * -(raw.y + raw.z),
					InverseRootTwo * (raw.z - raw.y),
					InverseRootTwo * (raw.w + raw.x)
				);
			}
			else
			{
				// This is already calculated for us since the other sensors need it too.
				rotation.value = _rotation;
			}

			rotation.measurementUncertainty = 0.0f;

			if (_config.rotationNineDof.isEnabled)
			{
				_lastSensorFrame.rotationNineDof = rotation;
			}

			if (_config.rotationSixDof.isEnabled)
			{
				_lastSensorFrame.rotationSixDof = rotation;
			}
		}

		/// <summary>
		/// Adds any gestures that were simulated during the last sensor frame to the current gesture data.
		/// Warns when unavailable or inactive gestures are simulated, and skips them.
		/// </summary>
		private void UpdateGestureData()
		{
			while (_pendingGestures.Count > 0)
			{
				GestureData gestureData = _pendingGestures.Dequeue();
				if (_config.GetGestureConfig(gestureData.gestureId).isEnabled &&
				    _virtualDevice.IsGestureAvailable(gestureData.gestureId))
				{
					// If the gesture is enabled and available, go ahead and trigger it.
					if (_verbose)
					{
						Debug.LogFormat(WearableConstants.DebugProviderTriggerGesture, Enum.GetName(typeof(GestureId), gestureData.gestureId));
					}

					_currentGestureData.Add(gestureData);
				}
				else
				{
					// Otherwise, warn, and drop the gesture from the queue.
					Debug.LogWarning(WearableConstants.DebugProviderTriggerDisabledGestureWarning);
				}
			}
		}

		/// <summary>
		/// Check an arbitrary intent for validity against the configurable sensor and gesture availability.
		/// </summary>
		/// <param name="profile"></param>
		/// <returns></returns>
		private bool CheckIntentValidity(AppIntentProfile profile)
		{
			// Sensors
			for (int i = 0; i < WearableConstants.SensorIds.Length; i++)
			{
				SensorId id = WearableConstants.SensorIds[i];

				if (profile.GetSensorInProfile(id) && !_virtualDevice.IsSensorAvailable(id))
				{
					return false;
				}
			}

			// Check gestures
			for (int i = 0; i < WearableConstants.GestureIds.Length; i++)
			{
				GestureId id = WearableConstants.GestureIds[i];

				if (id == GestureId.None)
				{
					continue;
				}

				if (profile.GetGestureInProfile(id) && !_virtualDevice.IsGestureAvailable(id))
				{
					return false;
				}
			}

			// NB All intervals are supported by the debug provider, so this part of the intent profile is not validated.

			return true;
		}

		#endregion
	}
}
