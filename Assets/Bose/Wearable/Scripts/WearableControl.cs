using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bose.Wearable.Extensions;
using Bose.Wearable.Proxy;
using UnityEngine;

namespace Bose.Wearable
{
	[AddComponentMenu("Bose/Wearable/WearableControl")]
	public sealed class WearableControl : Singleton<WearableControl>
	{
		/// <summary>
		/// Represents a sensor available to the WearablePlugin.
		/// </summary>
		public sealed class WearableSensor
		{
			/// <summary>
			/// Returns true or false depending on whether or not the sensor is enabled and
			/// retrieving updates.
			/// </summary>
			public bool IsActive
			{
				get { return _wearableControl.GetSensorActive(_id); }
			}

			/// <summary>
			/// Returns true if the sensor is available for use, otherwise false.
			/// </summary>
			public bool IsAvailable
			{
				get { return _wearableControl.IsSensorAvailable(_id); }
			}

			private readonly WearableControl _wearableControl;
			private readonly SensorId _id;

			internal WearableSensor(WearableControl wearableControl, SensorId id)
			{
				_wearableControl = wearableControl;
				_id = id;
			}

			[Obsolete(WearableConstants.ManualSensorControlsDiscouraged)]
			public void Start()
			{
				_wearableControl.StartSensor(_id);
			}

			[Obsolete(WearableConstants.ManualSensorControlsDiscouraged)]
			public void Stop()
			{
				_wearableControl.StopSensor(_id);
			}
		}

		/// <summary>
		/// Represents a Gesture available to the WearablePlugin.
		/// </summary>
		public sealed class WearableGesture
		{
			/// <summary>
			/// Returns true or false depending on whether or not the Gesture is enabled and
			/// retrieving updates.
			/// </summary>
			public bool IsActive
			{
				get { return _wearableControl.GetGestureEnabled(_gestureId); }
			}

			/// <summary>
			/// Returns true if the gesture is available for use, otherwise false.
			/// </summary>
			public bool IsAvailable
			{
				get { return _wearableControl.IsGestureAvailable(_gestureId); }
			}

			private readonly WearableControl _wearableControl;

			private readonly GestureId _gestureId;

			internal WearableGesture(WearableControl wearableControl, GestureId gestureId)
			{
				_wearableControl = wearableControl;
				_gestureId = gestureId;
			}

			[Obsolete(WearableConstants.ManualGestureControlsDiscouraged)]
			public void Enable()
			{
				_wearableControl.EnableGesture(_gestureId);
			}

			[Obsolete(WearableConstants.ManualGestureControlsDiscouraged)]
			public void Disable()
			{
				_wearableControl.DisableGesture(_gestureId);
			}
		}

		#region Public API

		/// <summary>
		/// Invoked when the connection status changes. Will also invoke the corresponding events below.
		/// </summary>
		public event Action<ConnectionStatus, Device?> ConnectionStatusChanged;

		/// <summary>
		/// This event is invoked when the connection status changes to <see cref="ConnectionStatus.Connected"/>.
		/// It is invoked immediately after <see cref="ConnectionStatusChanged"/>.
		/// </summary>
		public event Action<Device> DeviceConnected;

		/// <summary>
		/// This event is invoked when the connection status changes to <see cref="ConnectionStatus.Connected"/>.
		/// It is invoked immediately after <see cref="ConnectionStatusChanged"/>.
		/// </summary>
		public event Action<Device> DeviceDisconnected;

		/// <summary>
		/// This event is obsolete and will be removed in a future release. Please use
		/// <see cref="ConnectionStatusChanged"/> instead.
		/// </summary>
		[Obsolete(WearableConstants.ConnectionEventObsoleteWarning)]
		public event Action<Device> DeviceConnecting;

		/// <summary>
		/// This event is obsolete and will be removed in a future release. Please use
		/// <see cref="ConnectionStatusChanged"/> instead.
		/// </summary>
		[Obsolete(WearableConstants.ConnectionEventObsoleteWarning)]
		public event Action<Device> DeviceSecurePairingRequired;

		/// <summary>
		/// This event is obsolete and will be removed in a future release. Please use
		/// <see cref="ConnectionStatusChanged"/> instead.
		/// </summary>
		[Obsolete(WearableConstants.ConnectionEventObsoleteWarning)]
		public event Action<Device, FirmwareUpdateInformation> DeviceFirmwareUpdateAvailable;

		/// <summary>
		/// This event is obsolete and will be removed in a future release. Please use
		/// <see cref="ConnectionStatusChanged"/> instead.
		/// </summary>
		[Obsolete(WearableConstants.ConnectionEventObsoleteWarning)]
		public event Action<Device, FirmwareUpdateInformation> DeviceFirmwareUpdateRequired;

		/// <summary>
		/// Invoked when a device suspends service.
		/// </summary>
		public event Action<SensorServiceSuspendedReason> SensorServiceSuspended;

		/// <summary>
		/// Invoked when suspended service is resumed.
		/// </summary>
		public event Action SensorServiceResumed;

		/// <summary>
		/// Invoked when there are sensor updates from the Wearable device.
		/// </summary>
		public event Action<SensorFrame> SensorsUpdated;

		/// <summary>
		/// Invoked when a sensor frame includes a gesture.
		/// </summary>
		public event Action<GestureId> GestureDetected;

		/// <summary>
		/// Invoked when a double-tap gesture has completed
		/// </summary>
		[Obsolete(WearableConstants.DeviceSpecificGestureDiscouragedWarning)]
		public event Action DoubleTapDetected;

		/// <summary>
		/// Invoked when a head shake gesture has completed
		/// </summary>
		[Obsolete(WearableConstants.DeviceSpecificGestureDiscouragedWarning)]
		public event Action HeadShakeDetected;

		/// <summary>
		/// Invoked when a head nod gesture has completed
		/// </summary>
		[Obsolete(WearableConstants.DeviceSpecificGestureDiscouragedWarning)]
		public event Action HeadNodDetected;

		/// <summary>
		/// Invoked when an abstract input gesture has completed
		/// </summary>
		public event Action InputGestureDetected;

		/// <summary>
		/// Invoked when an abstract affirmative gesture has completed
		/// </summary>
		public event Action AffirmativeGestureDetected;

		/// <summary>
		/// Invoked when an abstract negative gesture has completed
		/// </summary>
		public event Action NegativeGestureDetected;

		/// <summary>
		/// Invoked when the active app intent profile is successfully validated
		/// </summary>
		public event Action AppIntentValidationSucceeded;

		/// <summary>
		/// Invoked when the active app intent profile is deemed invalid
		/// </summary>
		public event Action AppIntentValidationFailed;

		/// <summary>
		/// The most recent <see cref="SensorFrame"/> returned by the active provider. Retains its value even if no
		/// new <see cref="SensorFrame"/>s were emitted during the last render frame.
		/// </summary>
		public SensorFrame LastSensorFrame
		{
			get { return _activeProvider.LastSensorFrame; }
		}

		/// <summary>
		/// A list of <see cref="SensorFrame"/>s returned from the active provider during the most recent render frame,
		/// from oldest to newest. May be empty if no <see cref="SensorFrame"/>s were reported during the last frame.
		/// </summary>
		public List<SensorFrame> CurrentSensorFrames
		{
			get { return _activeProvider.CurrentSensorFrames; }
		}

		/// <summary>
		/// A list of GestureData returned from the bridge in order from oldest to most recent.
		/// The list is empty if no gestures were returned in the most recent update.
		/// </summary>
		internal List<GestureData> CurrentGestureData
		{
			get { return _activeProvider.CurrentGestureData; }
		}

		/// <summary>
		/// The Accelerometer sensor on the Wearable device.
		/// </summary>
		public WearableSensor AccelerometerSensor
		{
			get { return _accelerometerSensor; }
		}

		private WearableSensor _accelerometerSensor;

		/// <summary>
		/// The Gyroscope sensor on the Wearable device.
		/// </summary>
		public WearableSensor GyroscopeSensor
		{
			get { return _gyroscopeSensor; }
		}

		private WearableSensor _gyroscopeSensor;

		/// <summary>
		/// The Nine-DOF rotation sensor on the Wearable device.
		/// </summary>
		public WearableSensor RotationSensorNineDof
		{
			get { return _rotationSensorNineDof; }
		}

		private WearableSensor _rotationSensorNineDof;

		/// <summary>
		/// The Six-DOF rotation sensor on the Wearable device.
		/// </summary>
		public WearableSensor RotationSensorSixDof
		{
			get { return _rotationSensorSixDof; }
		}

		private WearableSensor _rotationSensorSixDof;

		/// <summary>
		/// Get object for double-tap gesture.
		/// </summary>
		[Obsolete(WearableConstants.DeviceSpecificGestureDiscouragedWarning)]
		public WearableGesture DoubleTapGesture
		{
			get { return _wearableGestures[GestureId.DoubleTap]; }
		}

		/// <summary>
		/// Get object for head nod gesture
		/// </summary>
		[Obsolete(WearableConstants.DeviceSpecificGestureDiscouragedWarning)]
		public WearableGesture HeadNodGesture
		{
			get { return _wearableGestures[GestureId.HeadNod]; }
		}

		/// <summary>
		/// Get object for head shake gesture
		/// </summary>
		[Obsolete(WearableConstants.DeviceSpecificGestureDiscouragedWarning)]
		public WearableGesture HeadShakeGesture
		{
			get { return _wearableGestures[GestureId.HeadShake]; }
		}

		/// <summary>
		/// Get object for input device-agnostic gesture
		/// </summary>
		public WearableGesture InputGesture
		{
			get { return _wearableGestures[GestureId.Input]; }
		}

		/// <summary>
		/// Get object for affirmative device-agnostic gesture
		/// </summary>
		public WearableGesture AffirmativeGesture
		{
			get { return _wearableGestures[GestureId.Affirmative]; }
		}
		/// <summary>
		/// Get object for negative device-agnostic gesture
		/// </summary>
		public WearableGesture NegativeGesture
		{
			get { return _wearableGestures[GestureId.Negative]; }
		}


		/// <summary>
		/// Returns a <see cref="WearableSensor"/> based on the passed <see cref="SensorId"/>.
		/// <paramref name="sensorId"/>
		/// </summary>
		/// <param name="sensorId"></param>
		/// <returns></returns>
		/// <exception cref="Exception"></exception>
		public WearableSensor GetWearableSensorById(SensorId sensorId)
		{
			WearableSensor sensor;
			if (_wearableSensors.TryGetValue(sensorId, out sensor))
			{
				return sensor;
			}

			throw new Exception(string.Format(WearableConstants.WearableSensorNotYetSupported, sensorId));
		}

		/// <summary>
		/// Returns a <see cref="WearableGesture"/> based on the passed <see cref="GestureId"/>.
		/// <paramref name="gestureId"/>
		/// </summary>
		/// <param name="gestureId"></param>
		/// <returns></returns>
		/// <exception cref="Exception"></exception>
		public WearableGesture GetWearableGestureById(GestureId gestureId)
		{
			if (gestureId == GestureId.None)
			{
				throw new Exception(WearableConstants.GestureIdNoneInvalidError);
			}

			WearableGesture wearableGesture;
			if (_wearableGestures.TryGetValue(gestureId, out wearableGesture))
			{
				return wearableGesture;
			}

			throw new Exception(string.Format(WearableConstants.WearableGestureNotYetSupported, gestureId));
		}

		/// <summary>
		/// The Wearable device that is currently connected in Unity.
		/// </summary>
		public Device? ConnectedDevice
		{
			get
			{
				// Safeguard against uninitialized active provider
				return _activeProvider == null ? null : _activeProvider.ConnectedDevice;
			}
		}

		/// <summary>
		/// Searches for all Wearable devices that can be connected to.
		/// </summary>
		/// <param name="onDevicesUpdated"></param>
		/// <param name="autoReconnect"></param>
		/// <param name="autoReconnectTimeout"></param>
		public void SearchForDevices(
			Action<Device[]> onDevicesUpdated,
			bool autoReconnect = false,
			float autoReconnectTimeout = WearableConstants.DefaultAutoReconnectTimeout)
		{
			_activeProvider.SearchForDevices(_activeAppIntentProfile, onDevicesUpdated, autoReconnect, autoReconnectTimeout);
		}

		/// <summary>
		/// Stops searching for Wearable devices that can be connected to.
		/// </summary>
		public void StopSearchingForDevices()
		{
			_activeProvider.StopSearchingForDevices();
		}

		/// <summary>
		/// Cancels any currently running device connection if there is one.
		/// </summary>
		public void CancelDeviceConnection()
		{
			_activeProvider.CancelDeviceConnection();
		}

		/// <summary>
		/// Connects to a specified device and invokes either <paramref name="onSuccess"/> or <paramref name="onFailure"/>
		/// depending on the result.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="onSuccess"></param>
		/// <param name="onFailure"></param>
		public void ConnectToDevice(Device device, Action onSuccess = null, Action onFailure = null)
		{
			_activeProvider.ConnectToDevice(device, onSuccess, onFailure);
		}

		/// <summary>
		/// Stops all attempts to connect to or monitor a device and disconnects from a device if connected.
		/// </summary>
		public void DisconnectFromDevice()
		{
			_activeProvider.DisconnectFromDevice();
		}

		/// <summary>
		/// The update interval of all sensors on the active provider
		/// </summary>
		public SensorUpdateInterval UpdateInterval
		{
			get { return _activeProvider.GetCachedDeviceConfiguration().updateInterval; }
		}

		/// <summary>
		/// Sets the update interval for the WearableDevice.
		/// </summary>
		public void SetSensorUpdateInterval(SensorUpdateInterval interval)
		{
			// If we've made a change that would result in the device's state changing,
			// mark the device config as dirty.
			if (SetSensorUpdateIntervalInternal(interval))
			{
				LockDeviceStateUpdate();
			}
		}

		/// <summary>
		/// Set the update mode, determining when SensorFrame updates are polled and made available.
		/// </summary>
		/// <param name="unityUpdateMode"></param>
		public void SetUnityUpdateMode(UnityUpdateMode unityUpdateMode)
		{
			_updateMode = unityUpdateMode;
		}

		/// <summary>
		/// The Unity Update method sensor updates should be retrieved and dispatched on.
		/// </summary>
		public UnityUpdateMode UpdateMode
		{
			get { return _updateMode; }
		}

		[SerializeField]
		private UnityUpdateMode _updateMode;

		/// <summary>
		/// An instance of the currently-active provider for configuration
		/// </summary>
		public WearableProviderBase ActiveProvider
		{
			get { return _activeProvider; }
		}

		private WearableProviderBase _activeProvider;


		/// <summary>
		/// Set the active provider to a specific provider instance
		/// </summary>
		/// <param name="provider"></param>
		public void SetActiveProvider(WearableProviderBase provider)
		{
			// Uninitialized providers should never have OnEnable/Disable called
			if (_activeProvider != null)
			{
				if (_activeProvider.Initialized)
				{
					_activeProvider.OnDisableProvider();
				}

				// Unsubscribe after disabling in case OnDisableProvider invokes an event
				// Using an invocation method here rather than the event proper ensures that any events added or removed
				// after setting the provider will be accounted for.
				_activeProvider.ConnectionStatusChanged -= OnConnectionStatusChanged;
				_activeProvider.SensorsUpdated -= OnSensorsUpdated;
				_activeProvider.GestureDetected -= OnGestureDetected;
				_activeProvider.ConfigurationSucceeded -= OnConfigurationSucceeded;
				_activeProvider.ConfigurationFailed -= OnConfigurationFailed;
				_activeProvider.SensorServiceSuspended -= OnSensorServiceSuspended;
				_activeProvider.SensorServiceResumed -= OnSensorServiceResumed;
			}

			_activeProvider = provider;

			// Initialize if this is the first time the provider is active
			if (!_activeProvider.Initialized)
			{
				_activeProvider.OnInitializeProvider();
			}

			// Subscribe to the provider's events
			_activeProvider.ConnectionStatusChanged += OnConnectionStatusChanged;
			_activeProvider.SensorsUpdated += OnSensorsUpdated;
			_activeProvider.GestureDetected += OnGestureDetected;
			_activeProvider.ConfigurationSucceeded += OnConfigurationSucceeded;
			_activeProvider.ConfigurationFailed += OnConfigurationFailed;
			_activeProvider.SensorServiceSuspended += OnSensorServiceSuspended;
			_activeProvider.SensorServiceResumed += OnSensorServiceResumed;

			// Enable the new provider after subscribing in case enabling the provider invokes an event
			_activeProvider.OnEnableProvider();
		}

		/// <summary>
		/// Set the active provider by provider type
		/// </summary>
		public void SetActiveProvider<T>()
			where T : WearableProviderBase
		{
			SetActiveProvider(GetOrCreateProvider<T>());
		}

		/// <summary>
		///  Returns a provider of the specified provider type for manipulation
		/// </summary>
		public T GetOrCreateProvider<T>()
			where T : WearableProviderBase
		{
			if (_debugProvider is T)
			{
				return (T)GetOrCreateProvider(ProviderId.DebugProvider);
			}

			if (_deviceProvider is T)
			{
				return (T)GetOrCreateProvider(ProviderId.WearableDevice);
			}

			if (_usbProvider is T)
			{
				return (T)GetOrCreateProvider(ProviderId.USBProvider);
			}

			if (_proxyProvider is T)
			{
				return (T)GetOrCreateProvider(ProviderId.WearableProxy);
			}

			throw new ArgumentOutOfRangeException(string.Format(WearableConstants.InvalidProviderTypeError, typeof(T)));
		}

		/// <summary>
		/// Request that the active provider validate an arbitrary app intent profile. Query the status of this
		/// validation using <see cref="GetIntentValidationStatus"/>.
		/// Providing an intent profile to this method will make it the active profile.
		/// </summary>
		/// <param name="profile"></param>
		public void SetIntentProfile(AppIntentProfile profile)
		{
			if (_activeProvider.ConnectionStatus == ConnectionStatus.Searching)
			{
				Debug.LogWarning(WearableConstants.IntentChangedWhileSearchingWarning);
			}

			_activeAppIntentProfile = profile;

			if (_activeAppIntentProfile == null)
			{
				_intentValidationStatus = IntentValidationStatus.Disabled;
			}
			else
			{
				_intentValidationStatus = IntentValidationStatus.Unknown;
			}
		}

		/// <summary>
		/// Validate the app intent profile as set by the inspector or by <see cref="SetIntentProfile"/>
		/// </summary>
		public void ValidateIntentProfile()
		{
			// Return early if asked to validate but no profiles are active.
			if (_activeAppIntentProfile == null)
			{
				_intentValidationStatus = IntentValidationStatus.Disabled;
				return;
			}

			_activeProvider.RequestIntentProfileValidation(_activeAppIntentProfile, OnIntentValidationResponse);

			// Check to make sure the request actually started before changing the status.
			if (_activeProvider.WaitingForIntentValidation)
			{
				_intentValidationStatus = IntentValidationStatus.Validating;
			}
		}

		/// <summary>
		/// Get the intent profile set by the inspector or by <see cref="SetIntentProfile"/>.
		/// </summary>
		/// <returns></returns>
		public AppIntentProfile GetActiveAppIntentProfile()
		{
			return _activeAppIntentProfile;
		}

		/// <summary>
		/// Query the current status of the intent validation procedure.
		/// </summary>
		/// <returns></returns>
		public IntentValidationStatus GetIntentValidationStatus()
		{
			if (_activeAppIntentProfile == null)
			{
				return IntentValidationStatus.Disabled;
			}

			if (!ConnectedDevice.HasValue || _activeAppIntentProfile.IsDirty)
			{
				return IntentValidationStatus.Unknown;
			}

			return _intentValidationStatus;
		}

		#endregion

		#region Private

		internal event Action<bool> AppFocusChanged;

		[SerializeField]
		private AppIntentProfile _activeAppIntentProfile;

		[SerializeField]
		private WearableDebugProvider _debugProvider;

		[SerializeField]
		private WearableDeviceProvider _deviceProvider;

		[SerializeField]
		private WearableUSBProvider _usbProvider;

		[SerializeField]
		private WearableProxyProvider _proxyProvider;

		#pragma warning disable 0414
		[SerializeField]
		private ProviderId _editorDefaultProvider = WearableConstants.EditorDefaultProvider;

		[SerializeField]
		private ProviderId _runtimeDefaultProvider = WearableConstants.RuntimeDefaultProvider;
		#pragma warning restore 0414

		/// <summary>
		/// The wearable device config used for public methods on WearableControl that are intended to
		/// update device state.
		/// </summary>
		private WearableDeviceConfig _wearableDeviceConfig;

		/// <summary>
		/// The <see cref="WearableDeviceConfig"/> that is aggregated from all requirements and sent to the
		/// device.
		/// </summary>
		internal WearableDeviceConfig FinalWearableDeviceConfig
		{
			get { return _finalWearableDeviceConfig; }
		}

		/// <summary>
		/// The <see cref="WearableDeviceConfig"/> used as the resolved version after all registered requirements have been
		/// processed into a single config where enabled sensors/gestures are preferred over disabled ones and
		/// faster sensor update intervals are preferred over slower ones.
		/// </summary>
		[SerializeField]
		private WearableDeviceConfig _finalWearableDeviceConfig;

		/// <summary>
		/// The <see cref="WearableDeviceConfig"/> used to override the requirements resolved device config.
		/// </summary>
		internal WearableDeviceConfig OverrideDeviceConfig
		{
			get { return _overrideDeviceConfig; }
		}

		[SerializeField]
		private WearableDeviceConfig _overrideDeviceConfig;

		/// <summary>
		/// Returns true if an override <see cref="WearableDeviceConfig"/> is present, otherwise false.
		/// </summary>
		/// <returns></returns>
		internal bool IsOverridingDeviceConfig
		{
			get { return _isOverridingDeviceConfig; }
		}

		/// <summary>
		///  The <see cref="WearableDeviceConfig"/> currently used to represent and update device state.
		/// </summary>
		internal WearableDeviceConfig CurrentDeviceConfig
		{
			get
			{
				return _isOverridingDeviceConfig
					? _overrideDeviceConfig
					: _finalWearableDeviceConfig;
			}
		}

		private bool _isOverridingDeviceConfig;

		/// <summary>
		/// Have we made a device state update on this frame or a previous frame
		/// such that we consider it locked?
		/// </summary>
		private bool _isDeviceStateUpdateLocked;

		/// <summary>
		/// Have we applied a device state update this frame?
		/// </summary>
		private bool _hasDeviceUpdateBeenApplied;

		/// <summary>
		/// Is a device state update pending during the lockout?
		/// </summary>
		private bool _isDeviceStateUpdatePendingDuringLock;

		/// <summary>
		/// The time since the app started when we last updated the device config.
		/// </summary>
		private float _appTimeSinceDeviceStateUpdateLocked;

		/// <summary>
		/// Indicates if the user has been warned about null intent profiles to avoid log-spamming.
		/// </summary>
		private bool _hasWarnedNullIntentProfile;

		/// <summary>
		/// The status of the current intent validation attempt.
		/// </summary>
		private IntentValidationStatus _intentValidationStatus;

		private Dictionary<GestureId, WearableGesture> _wearableGestures;

		private Dictionary<SensorId, WearableSensor> _wearableSensors;

		// Reference Counting State. Initialized inline to support
		#pragma warning disable 0414
		private readonly List<WearableRequirement> _wearableRequirements = new List<WearableRequirement>();
		#pragma warning restore 0414

		// Helper objects for generating log entries
		private List<string> _stringList;
		private StringBuilder _stringBuilder;
		private const string ItemSeparator = ", ";

		private WearableProviderBase GetOrCreateProvider(ProviderId providerId)
		{
			switch (providerId)
			{
				case ProviderId.DebugProvider:
					if (_debugProvider == null)
					{
						_debugProvider = new WearableDebugProvider();
					}
					return _debugProvider;

				case ProviderId.WearableDevice:
					if (_deviceProvider == null)
					{
						_deviceProvider = new WearableDeviceProvider();
					}
					return _deviceProvider;

				case ProviderId.USBProvider:
					if (_usbProvider == null)
					{
						_usbProvider = new WearableUSBProvider();
					}

					return _usbProvider;

				case ProviderId.WearableProxy:
					if (_proxyProvider == null)
					{
						_proxyProvider = new WearableProxyProvider();
					}

					return _proxyProvider;

				#pragma warning disable 618
				case ProviderId.MobileProvider:
					Debug.LogWarning(WearableConstants.MobileProviderCreateWarning);
					if (_debugProvider == null)
					{
						_debugProvider = new WearableDebugProvider();
					}
					return _debugProvider;
				#pragma warning restore 618

				default:
					throw new ArgumentOutOfRangeException(string.Format(
						WearableConstants.InvalidProviderTypeError,
						providerId.ToString()));
			}
		}

		private void OnConnectionStatusChanged(ConnectionStatus status, Device? device)
		{
			if (ConnectionStatusChanged != null)
			{
				ConnectionStatusChanged.Invoke(status, device);
			}

			if (status == ConnectionStatus.Connected)
			{
				if (DeviceConnected != null)
				{
					DeviceConnected.Invoke(device.GetValueOrDefault());
				}

				// When a device has been connected, immediately clear any locks and then re-lock
				// so that we update the state of the device at the end of this frame or the next.
				UnlockDeviceStateUpdate();
				LockDeviceStateUpdate();
			}
			else if (status == ConnectionStatus.Disconnected)
			{
				if (DeviceDisconnected != null)
				{
					DeviceDisconnected.Invoke(device.GetValueOrDefault());
				}
			}

			// NB These events will be removed in a future PR, and are here for backwards-compatibility.
			#pragma warning disable 618
			switch (status)
			{
				case ConnectionStatus.Connecting:
					if (DeviceConnecting != null)
					{
						DeviceConnecting.Invoke(device.GetValueOrDefault());
					}
					break;

				case ConnectionStatus.SecurePairingRequired:
					if (DeviceSecurePairingRequired != null)
					{
						DeviceSecurePairingRequired.Invoke(device.GetValueOrDefault());
					}
					break;

				case ConnectionStatus.FirmwareUpdateAvailable:
					if (DeviceFirmwareUpdateAvailable != null)
					{
						FirmwareUpdateInformation updateInformation = _activeProvider.GetFirmwareUpdateInformation();
						DeviceFirmwareUpdateAvailable.Invoke(device.GetValueOrDefault(), updateInformation);
					}
					break;

				case ConnectionStatus.FirmwareUpdateRequired:
					if (DeviceFirmwareUpdateRequired != null)
					{
						FirmwareUpdateInformation updateInformation = _activeProvider.GetFirmwareUpdateInformation();
						DeviceFirmwareUpdateRequired.Invoke(device.GetValueOrDefault(), updateInformation);
					}
					break;

				case ConnectionStatus.Connected:
				case ConnectionStatus.Disconnected:
				case ConnectionStatus.Failed:
				case ConnectionStatus.Cancelled:
				case ConnectionStatus.AutoReconnect:
				case ConnectionStatus.Searching:
					// No-op
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}
			#pragma warning restore 618
		}

		private void OnIntentValidationResponse(bool valid)
		{
			if (valid)
			{
				_intentValidationStatus = IntentValidationStatus.Success;

				if (AppIntentValidationSucceeded != null)
				{
					AppIntentValidationSucceeded.Invoke();
				}
			}
			else
			{
				_intentValidationStatus = IntentValidationStatus.Failure;

				Debug.LogWarning(WearableConstants.InvalidIntentsWarning);

				if (AppIntentValidationFailed != null)
				{
					AppIntentValidationFailed.Invoke();
				}
			}
		}

		/// <summary>
		/// Invokes the <see cref="SensorServiceSuspended"/> event.
		/// </summary>
		/// <param name="reason"></param>
		private void OnSensorServiceSuspended(SensorServiceSuspendedReason reason)
		{
			Debug.LogWarningFormat(WearableConstants.SensorServiceSuspendedWarning, reason.ToString());

			if (SensorServiceSuspended != null)
			{
				SensorServiceSuspended.Invoke(reason);
			}
		}

		/// <summary>
		/// Invokes the <see cref="SensorServiceResumed"/> event.
		/// </summary>
		private void OnSensorServiceResumed()
		{
			Debug.Log(WearableConstants.SensorServiceResumedInfo);

			if (SensorServiceResumed != null)
			{
				SensorServiceResumed.Invoke();
			}
		}

		/// <summary>
		/// Invokes the <see cref="SensorsUpdated"/> event.
		/// </summary>
		/// <param name="frame"></param>
		private void OnSensorsUpdated(SensorFrame frame)
		{
			if (SensorsUpdated != null)
			{
				SensorsUpdated.Invoke(frame);
			}
		}

		/// <summary>
		/// Invokes events for the specific gesture, plus GestureDetected.
		/// </summary>
		/// <param name="gestureId"></param>
		private void OnGestureDetected(GestureId gestureId)
		{
			if (GestureDetected != null)
			{
				GestureDetected.Invoke(gestureId);
			}

			// Gesture actions are only provided for device-specific gestures for older devices.
			// Use device-agnostic gestures instead.
			switch (gestureId)
			{
				#pragma warning disable 0618
				case GestureId.DoubleTap:
					if (DoubleTapDetected != null)
					{
						DoubleTapDetected.Invoke();
					}
					break;
				case GestureId.HeadShake:
					if (HeadShakeDetected != null)
					{
						HeadShakeDetected.Invoke();
					}
					break;
				case GestureId.HeadNod:
					if (HeadNodDetected != null)
					{
						HeadNodDetected.Invoke();
					}
					break;
				case GestureId.TouchAndHold:
					// no-op. section to be deprecated.
					break;
				#pragma warning restore 0618
				case GestureId.Input:
					if (InputGestureDetected != null)
					{
						InputGestureDetected.Invoke();
					}
					break;
				case GestureId.Affirmative:
					if (AffirmativeGestureDetected != null)
					{
						AffirmativeGestureDetected.Invoke();
					}
					break;
				case GestureId.Negative:
					if (NegativeGestureDetected != null)
					{
						NegativeGestureDetected.Invoke();
					}
					break;
				case GestureId.None:
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void OnConfigurationSucceeded()
		{
			UnlockDeviceStateUpdate();
		}

		private void OnConfigurationFailed(ConfigStatus sensor, ConfigStatus gesture)
		{
			UnlockDeviceStateUpdate();

			Debug.LogWarningFormat(WearableConstants.ConfigFailedWarning, sensor.ToString(), gesture.ToString());
		}

		/// <summary>
		/// Forces the device state to be resolved.
		/// </summary>
		internal void ForceDeviceStateResolution()
		{
			LockDeviceStateUpdate();
		}

		/// <summary>
		/// Sets the override <see cref="WearableDeviceConfig"/> config that will take priority over any
		/// requirement resolved <see cref="WearableDeviceConfig"/>.
		/// </summary>
		/// <param name="config"></param>
		internal void RegisterOverrideConfig(WearableDeviceConfig config)
		{
			_overrideDeviceConfig = config;
			_isOverridingDeviceConfig = true;

			LockDeviceStateUpdate();
		}

		/// <summary>
		/// Removes the override <see cref="WearableDeviceConfig"/> and triggers a pending device update.
		/// </summary>
		internal void UnregisterOverrideConfig()
		{
			// Don't trigger a pending update if there isn't a device config set for the override.
			if (!_isOverridingDeviceConfig)
			{
				return;
			}

			_isOverridingDeviceConfig = false;

			LockDeviceStateUpdate();
		}

		/// <summary>
		/// Registers the <see cref="WearableRequirement"/> <paramref name="requirement"/>
		/// </summary>
		/// <param name="requirement"></param>
		internal void RegisterRequirement(WearableRequirement requirement)
		{
			if (_wearableRequirements.Contains(requirement))
			{
				return;
			}

			requirement.Updated += LockDeviceStateUpdate;

			_wearableRequirements.Add(requirement);

			LockDeviceStateUpdate();
		}

		/// <summary>
		/// Unregisters the <see cref="WearableRequirement"/> <paramref name="requirement"/>
		/// </summary>
		/// <param name="requirement"></param>
		internal void UnregisterRequirement(WearableRequirement requirement)
		{
			if (!_wearableRequirements.Contains(requirement))
			{
				return;
			}

			requirement.Updated -= LockDeviceStateUpdate;

			_wearableRequirements.Remove(requirement);

			LockDeviceStateUpdate();
		}

		/// <summary>
		/// Sets the <see cref="SensorUpdateInterval"/> <paramref name="newInterval"/>.
		/// </summary>
		private bool SetSensorUpdateIntervalInternal(SensorUpdateInterval newInterval)
		{
			var hasDeviceStateChanged = false;
			if (_wearableDeviceConfig.updateInterval != newInterval)
			{
				_wearableDeviceConfig.updateInterval = newInterval;

				hasDeviceStateChanged = true;
			}

			return hasDeviceStateChanged;
		}

		/// <summary>
		/// Marks the device state update functionality to be unlocked such that device updates can take place again.
		/// </summary>
		private void UnlockDeviceStateUpdate()
		{
			_isDeviceStateUpdateLocked = false;
			_appTimeSinceDeviceStateUpdateLocked = Time.time;
			_hasDeviceUpdateBeenApplied = false;
		}

		/// <summary>
		/// Marks the device state update functionality as locked such that we have indicated a device state
		/// update needs to take place. If an update has already taken place, but is still currently locked,
		/// a pending update flag will be set instead to indicate another device state update is needed.
		/// </summary>
		private void LockDeviceStateUpdate()
		{
			var timeSinceLocked = Time.time - _appTimeSinceDeviceStateUpdateLocked;
			if (_isDeviceStateUpdateLocked && timeSinceLocked > 0f)
			{
				Debug.LogWarning(WearableConstants.OnlyOneSensorFrequencyUpdatePerFrameWarning, this);

				_isDeviceStateUpdatePendingDuringLock = true;
			}
			else
			{
				_isDeviceStateUpdateLocked = true;
				_hasDeviceUpdateBeenApplied = false;
				_appTimeSinceDeviceStateUpdateLocked = Time.time;
			}
		}

		/// <summary>
		/// Updates the device state for sensors and gestures via the current provider. This should only be
		/// done once per frame with an interval of frames/time between subsequent updates.
		/// </summary>
		private void UpdateDeviceFromConfig()
		{
			if (!ConnectedDevice.HasValue)
			{
				return;
			}

			// Resolve the final device state based on all requirements and device configs
			ResolveFinalDeviceConfig();

			// Get the appropriate WearableDeviceConfig
			var deviceConfig = CurrentDeviceConfig;

			// Ensure we don't have any fatal configurations
			PreventInvalidConfigurations(deviceConfig);

			// Make sure that the config doesn't violate the specified intent
			if (_intentValidationStatus != IntentValidationStatus.Disabled)
			{
				CheckConfigurationAgainstIntent(deviceConfig);
			}

			// If the current device state is the same as our final resolved config, return and do not call
			// any native bridge code.
			if (!ShouldUpdateDeviceState(deviceConfig))
			{
				UnlockDeviceStateUpdate();
				return;
			}

			_activeProvider.SetDeviceConfiguration(deviceConfig);

			_hasDeviceUpdateBeenApplied = true;
		}

		/// <summary>
		/// Resolves all <see cref="WearableRequirement.DeviceConfig"/>'s registered into WearableControl into
		/// a final device config that is an aggregate of all intended state with priority for enabled
		/// sensors/gestures over disabled and faster sensor update interval over slower.
		/// </summary>
		/// <returns></returns>
		private void ResolveFinalDeviceConfig()
		{
			// Reset all state in the final device config to off/slowest speeds.
			ResetFinalDeviceConfig();

			// Process all registered wearable requirement's device config and internal device config to
			// additively update state on the final config
			for (var i = _wearableRequirements.Count - 1; i >= 0; i--)
			{
				var wr = _wearableRequirements[i];
				// If we encounter a destroyed requirement, remove it
				if (ReferenceEquals(wr, null))
				{
					_wearableRequirements.RemoveAt(i);
					continue;
				}

				UpdateFinalDeviceConfig(wr.DeviceConfig);
			}

			UpdateFinalDeviceConfig(_wearableDeviceConfig);
		}

		/// <summary>
		/// Inspects the <see cref="WearableDeviceConfig" /> <paramref name="config"/> for any invalid
		/// configurations and if any are present rolls back or throttles the configuration to prevent them.
		/// </summary>
		/// <param name="config"></param>
		private void PreventInvalidConfigurations(WearableDeviceConfig config)
		{
			// Check for the invalid configuration of three or more sensors and TwentyMs and if
			// this is present, throttle back the SensorUpdateInterval to FortyMs.
			if (config.HasThreeOrMoreSensorsEnabled() &&
			    config.updateInterval == SensorUpdateInterval.TwentyMs)
			{
				Debug.LogWarning(WearableConstants.SensorUpdateIntervalDecreasedWarning, this);
				config.updateInterval = SensorUpdateInterval.FortyMs;
			}

			// Check for unavailable sensors and gestures, warn, then disable them

			if (!_activeProvider.ConnectedDevice.HasValue)
			{
				return;
			}

			Device device = _activeProvider.ConnectedDevice.Value;

			for (int i = 0; i < WearableConstants.SensorIds.Length; i++)
			{
				var sensorId = WearableConstants.SensorIds[i];
				var sensorConfig = config.GetSensorConfig(sensorId);
				if (sensorConfig.isEnabled && !device.IsSensorAvailable(sensorId))
				{
					Debug.LogWarningFormat(
						WearableConstants.RequestedSensorNotAvailableWarningFormat,
						sensorId.ToString());
					sensorConfig.isEnabled = false;
				}
			}

			for (int i = 0; i < WearableConstants.GestureIds.Length; i++)
			{
				var gestureId = WearableConstants.GestureIds[i];
				if (gestureId == GestureId.None)
				{
					continue;
				}

				var gestureConfig = config.GetGestureConfig(gestureId);
				if (gestureConfig.isEnabled && !device.IsGestureAvailable(gestureId))
				{
					Debug.LogWarningFormat(
						WearableConstants.RequestedGestureNotAvailableWarningFormat,
						gestureId.ToString());
					gestureConfig.isEnabled = false;
				}
			}
		}

		/// <summary>
		/// Resets all sensors/gestures on the final device config to be false and sets the sensor update
		/// interval to the slowest speeds.
		/// </summary>
		private void ResetFinalDeviceConfig()
		{
			_finalWearableDeviceConfig.updateInterval = SensorUpdateInterval.ThreeHundredTwentyMs;

			// Set all sensor state and update intervals
			for (var i = 0; i < WearableConstants.SensorIds.Length; i++)
			{
				var finalSensorConfig = _finalWearableDeviceConfig.GetSensorConfig(WearableConstants.SensorIds[i]);
				finalSensorConfig.isEnabled = false;
			}

			// Set all gesture state
			for (var i = 0; i < WearableConstants.GestureIds.Length; i++)
			{
				if (WearableConstants.GestureIds[i] == GestureId.None)
				{
					continue;
				}

				var finalGestureConfig = _finalWearableDeviceConfig.GetGestureConfig(WearableConstants.GestureIds[i]);
				finalGestureConfig.isEnabled = false;
			}
		}

		/// <summary>
		/// Additively updates the final device config with <see cref="WearableDeviceConfig"/> <paramref name="config"/>
		/// </summary>
		/// <param name="config"></param>
		private void UpdateFinalDeviceConfig(WearableDeviceConfig config)
		{
			// Set all sensor state and update intervals
			for (var i = 0; i < WearableConstants.SensorIds.Length; i++)
			{
				var sensorId = WearableConstants.SensorIds[i];
				var finalSensorConfig = _finalWearableDeviceConfig.GetSensorConfig(sensorId);
				var reqSensorConfig = config.GetSensorConfig(sensorId);

				finalSensorConfig.isEnabled |= reqSensorConfig.isEnabled;
			}

			// Set all gesture state.
			for (var i = 0; i < WearableConstants.GestureIds.Length; i++)
			{
				if (WearableConstants.GestureIds[i] == GestureId.None)
				{
					continue;
				}

				var finalGestureConfig = _finalWearableDeviceConfig.GetGestureConfig(WearableConstants.GestureIds[i]);
				var reqGestureConfig = config.GetGestureConfig(WearableConstants.GestureIds[i]);

				finalGestureConfig.isEnabled |= reqGestureConfig.isEnabled;
			}

			if (config.HasAnySensorsEnabled())
			{
				if (_finalWearableDeviceConfig.updateInterval.IsSlowerThan(config.updateInterval))
				{
					_finalWearableDeviceConfig.updateInterval = config.updateInterval;
				}
			}
		}

		/// <summary>
		/// True if the device state needs to be updated because it differs from our the
		/// <see cref="WearableDeviceConfig"/> <paramref name="config"/>, otherwise false.
		/// </summary>
		/// <param name="config"></param>
		/// <returns></returns>
		private bool ShouldUpdateDeviceState(WearableDeviceConfig config)
		{
			// Check all sensors to see if we need to update the device.
			var deviceShouldBeUpdated = false;
			for (var i = 0; i < WearableConstants.SensorIds.Length; i++)
			{
				var sensorId = WearableConstants.SensorIds[i];
				var sensorConfig = config.GetSensorConfig(sensorId);
				if (sensorConfig.isEnabled != GetSensorActive(sensorId) &&
				    IsSensorAvailable(sensorId))
				{
					deviceShouldBeUpdated = true;
				}
			}

			// Check the sensor update interval to see if we need to update the device.
			if (config.updateInterval != UpdateInterval)
			{
				deviceShouldBeUpdated = true;
			}

			// Check all gestures to see if we need to update the device state.
			if (!deviceShouldBeUpdated)
			{
				for (var i = 0; i < WearableConstants.GestureIds.Length; i++)
				{
					var gestureId = WearableConstants.GestureIds[i];
					if (gestureId == GestureId.None)
					{
						continue;
					}

					var gestureConfig = config.GetGestureConfig(gestureId);
					if (gestureConfig.isEnabled != GetGestureEnabled(gestureId) &&
					    IsGestureAvailable(gestureId))
					{
						deviceShouldBeUpdated = true;
						break;
					}
				}
			}

			return deviceShouldBeUpdated;
		}

		private void CheckConfigurationAgainstIntent(WearableDeviceConfig config)
		{
			if (_intentValidationStatus == IntentValidationStatus.Disabled)
			{
				return;
			}

			if (_activeAppIntentProfile == null)
			{
				if (!_hasWarnedNullIntentProfile)
				{
					Debug.LogWarning(WearableConstants.ValidatingIntentsButNoProfileWarning);
				}

				_hasWarnedNullIntentProfile = true;
				return;
			}
			_hasWarnedNullIntentProfile = false;

			// Check for violated intents
			_stringList.Clear();

			// Check sensors
			for (int i = 0; i < WearableConstants.SensorIds.Length; i++)
			{
				SensorId id = WearableConstants.SensorIds[i];

				bool configEnabled = config.GetSensorConfig(id).isEnabled;
				bool intentEnabled = _activeAppIntentProfile.GetSensorInProfile(id);
				if (configEnabled && !intentEnabled)
				{
					_stringList.Add(id.ToString());
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

				bool configEnabled = config.GetGestureConfig(id).isEnabled;
				bool intentEnabled = _activeAppIntentProfile.GetGestureInProfile(id);
				if (configEnabled && !intentEnabled)
				{
					_stringList.Add(id.ToString());
				}
			}

			// Check intervals. Skip this test if there are no sensors enabled, since we fall back to the slowest
			// rate (which the intent profile may not contain).
			if (config.HasAnySensorsEnabled() && !_activeAppIntentProfile.GetIntervalInProfile(config.updateInterval))
			{
				_stringList.Add(config.updateInterval.ToString());
			}

			// Warn if mismatched, but continue normal operations regardless
			if (_stringList.Count > 0)
			{
				_stringBuilder.Length = 0;
				for (int i = 0; i < _stringList.Count; i++)
				{
					_stringBuilder.Append(_stringList[i]);

					if (i != _stringList.Count - 1)
					{
						_stringBuilder.Append(ItemSeparator);
					}
				}

				Debug.LogWarningFormat(WearableConstants.ViolatedIntentProfileWarningFormat, _stringBuilder);
			}
		}

		/// <summary>
		/// Returns true if a connected device supports using a sensor with <see cref="SensorId"/>
		/// <paramref name="sensorId"/>, otherwise false if it does not or if a device is not connected.
		/// </summary>
		/// <param name="sensorId"></param>
		/// <returns></returns>
		private bool IsSensorAvailable(SensorId sensorId)
		{
			if (!ConnectedDevice.HasValue)
			{
				Debug.Log(WearableConstants.DeviceIsNotCurrentlyConnected);
				return false;
			}

			return ConnectedDevice.Value.IsSensorAvailable(sensorId);
		}

		/// <summary>
		/// Starts the <see cref="WearableSensor"/> for <see cref="SensorId"/> <paramref name="sensorId"/>
		/// </summary>
		/// <param name="sensorId"></param>
		private void StartSensor(SensorId sensorId)
		{
			if (StartSensorInternal(sensorId))
			{
				LockDeviceStateUpdate();
			}
		}

		/// <summary>
		/// Start a sensor with a given interval <see cref="SensorId"/>. Returns true if the sensor was started,
		/// otherwise false.
		/// </summary>
		/// <param name="sensorId"></param>
		private bool StartSensorInternal(SensorId sensorId)
		{
			var sensorConfig = _wearableDeviceConfig.GetSensorConfig(sensorId);
			if (sensorConfig.isEnabled)
			{
				return false;
			}

			sensorConfig.isEnabled = true;

			return true;
		}

		/// <summary>
		/// Stops the <see cref="WearableSensor"/> for <see cref="SensorId"/> <paramref name="sensorId"/>
		/// </summary>
		/// <param name="sensorId"></param>
		private void StopSensor(SensorId sensorId)
		{
			if (StopSensorInternal(sensorId))
			{
				LockDeviceStateUpdate();
			}
		}

		/// <summary>
		/// Stop a sensor with a given <see cref="SensorId"/>. Returns true if the sensor was stopped,
		/// otherwise false.
		/// </summary>
		/// <param name="sensorId"></param>
		private bool StopSensorInternal(SensorId sensorId)
		{
			var sensorConfig = _wearableDeviceConfig.GetSensorConfig(sensorId);
			if (!sensorConfig.isEnabled)
			{
				return false;
			}

			sensorConfig.isEnabled = false;

			return true;
		}

		/// <summary>
		/// Returns whether or not a sensor with a given <see cref="SensorId"/> is active or not.
		/// </summary>
		/// <param name="sensorId"></param>
		/// <returns></returns>
		private bool GetSensorActive(SensorId sensorId)
		{
			return _activeProvider.GetCachedDeviceConfiguration().GetSensorConfig(sensorId).isEnabled;
		}

		/// <summary>
		/// Returns true if a connected device supports using a gesture with <see cref="GestureId"/>
		/// <paramref name="gestureId"/>, otherwise false if it does not or if a device is not connected.
		/// </summary>
		/// <param name="gestureId"></param>
		/// <returns></returns>
		private bool IsGestureAvailable(GestureId gestureId)
		{
			if (!ConnectedDevice.HasValue)
			{
				Debug.Log(WearableConstants.DeviceIsNotCurrentlyConnected);
				return false;
			}

			return ConnectedDevice.Value.IsGestureAvailable(gestureId);
		}

		/// <summary>
		/// Enables the <see cref="WearableGesture"/> for <see cref="GestureId"/> <paramref name="gestureId"/>
		/// </summary>
		/// <param name="gestureId"></param>
		private void EnableGesture(GestureId gestureId)
		{
			if (gestureId == GestureId.None)
			{
				throw new Exception(WearableConstants.GestureIdNoneInvalidError);
			}

			if (EnableGestureInternal(gestureId))
			{
				LockDeviceStateUpdate();
			}
		}

		/// <summary>
		/// Start a gesture with a given interval <see cref="GestureId"/>.
		/// </summary>
		/// <param name="gestureId"></param>
		private bool EnableGestureInternal(GestureId gestureId)
		{
			var gestureConfig = _wearableDeviceConfig.GetGestureConfig(gestureId);
			if (gestureConfig.isEnabled)
			{
				return false;
			}

			gestureConfig.isEnabled = true;

			return true;
		}

		/// <summary>
		/// Disables the <see cref="WearableGesture"/> for <see cref="GestureId"/> <paramref name="gestureId"/>
		/// </summary>
		/// <param name="gestureId"></param>
		private void DisableGesture(GestureId gestureId)
		{
			if (gestureId == GestureId.None)
			{
				throw new Exception(WearableConstants.GestureIdNoneInvalidError);
			}

			if (DisableGestureInternal(gestureId))
			{
				LockDeviceStateUpdate();
			}
		}

		/// <summary>
		/// Stop a gesture with a given <see cref="GestureId"/>.
		/// </summary>
		/// <param name="gestureId"></param>
		private bool DisableGestureInternal(GestureId gestureId)
		{
			var gestureConfig = _wearableDeviceConfig.GetGestureConfig(gestureId);
			if (!gestureConfig.isEnabled)
			{
				return false;
			}

			gestureConfig.isEnabled = false;

			return true;
		}

		/// <summary>
		/// Returns whether or not a gesture with a given <see cref="GestureId"/> is enabled.
		/// </summary>
		/// <param name="gestureId"></param>
		/// <returns></returns>
		private bool GetGestureEnabled(GestureId gestureId)
		{
			return _activeProvider.GetCachedDeviceConfiguration().GetGestureConfig(gestureId).isEnabled;
		}

		protected override void Awake()
		{
			_stringList = new List<string>();
			_stringBuilder = new StringBuilder();

			_wearableDeviceConfig = new WearableDeviceConfig();
			_finalWearableDeviceConfig = new WearableDeviceConfig();

			// populate sensors and dictionary
			_accelerometerSensor = new WearableSensor(this, SensorId.Accelerometer);
			_gyroscopeSensor = new WearableSensor(this, SensorId.Gyroscope);
			_rotationSensorNineDof = new WearableSensor(this, SensorId.RotationNineDof);
			_rotationSensorSixDof = new WearableSensor(this, SensorId.RotationSixDof);

			_wearableSensors = new Dictionary<SensorId, WearableSensor>
			{
				{SensorId.Accelerometer, _accelerometerSensor},
				{SensorId.Gyroscope, _gyroscopeSensor},
				{SensorId.RotationNineDof, _rotationSensorNineDof},
				{SensorId.RotationSixDof, _rotationSensorSixDof}
			};

			// populate wearable gesture dictionary
			_wearableGestures = new Dictionary<GestureId, WearableGesture>();
			for (var i = 0; i < WearableConstants.GestureIds.Length; ++i)
			{
				if (WearableConstants.GestureIds[i] != GestureId.None)
				{
					_wearableGestures[WearableConstants.GestureIds[i]] =
						new WearableGesture(this, WearableConstants.GestureIds[i]);
				}
			}

			_intentValidationStatus = GetIntentValidationStatus();

			// Activate the default provider depending on the platform
			#if UNITY_EDITOR
			SetActiveProvider(GetOrCreateProvider(_editorDefaultProvider));
			#else
			SetActiveProvider(GetOrCreateProvider(_runtimeDefaultProvider));
			#endif

			base.Awake();
		}

		#if UNITY_EDITOR

		private void OnValidate()
		{
			// Convert mobile provider to debug provider
			const string RECORD_MESSAGE = "UpdatedProvider";

			#pragma warning disable 618
			if (_editorDefaultProvider == ProviderId.MobileProvider)
			{
				UnityEditor.Undo.RecordObject(this, RECORD_MESSAGE);

				Debug.LogWarning(WearableConstants.MobileProviderRemovedWarning);

				_editorDefaultProvider = ProviderId.DebugProvider;
			}

			if (_runtimeDefaultProvider == ProviderId.MobileProvider)
			{
				UnityEditor.Undo.RecordObject(this, RECORD_MESSAGE);

				Debug.LogWarning(WearableConstants.MobileProviderRemovedWarning);

				_runtimeDefaultProvider = ProviderId.DebugProvider;
			}
			#pragma warning restore 618

			if (WearableConstants.DisallowedEditorProviders.Contains(_editorDefaultProvider))
			{
				UnityEditor.Undo.RecordObject(this, RECORD_MESSAGE);

				Debug.LogWarningFormat(
					WearableConstants.DisallowedEditorProviderFormat,
					_editorDefaultProvider.ToString(),
					WearableConstants.EditorDefaultProvider
				);

				_editorDefaultProvider = WearableConstants.EditorDefaultProvider;
			}

			if (WearableConstants.DisallowedRuntimeProviders.Contains(_runtimeDefaultProvider))
			{
				UnityEditor.Undo.RecordObject(this, RECORD_MESSAGE);

				Debug.LogWarningFormat(
					WearableConstants.DisallowedRuntimeProviderFormat,
					_runtimeDefaultProvider.ToString(),
					WearableConstants.RuntimeDefaultProvider
				);

				_runtimeDefaultProvider = WearableConstants.RuntimeDefaultProvider;
			}

			// Set using the variable not the method, so the provider doesn't get prematurely initialized
			_activeProvider = GetOrCreateProvider(_editorDefaultProvider);
		}

		#endif

		/// <summary>
		/// When destroyed, stop all sensors and disconnect from the Wearable device.
		/// </summary>
		protected override void OnDestroy()
		{
			if (ConnectedDevice.HasValue)
			{
				_activeProvider.SetDeviceConfiguration(WearableConstants.DisabledDeviceConfig);
				DisconnectFromDevice();
			}

			// Clean up providers
			_activeProvider.OnDisableProvider();

			if (_deviceProvider != null && _deviceProvider.Initialized)
			{
				_deviceProvider.OnDestroyProvider();
			}

			if (_debugProvider != null && _debugProvider.Initialized)
			{
				_debugProvider.OnDestroyProvider();
			}

			base.OnDestroy();
		}

		/// <summary>
		/// When enabled, resume monitoring the device session if necessary.
		/// </summary>
		private void OnEnable()
		{
			if (!_activeProvider.Enabled)
			{
				_activeProvider.OnEnableProvider();
			}
		}

		/// <summary>
		/// When disabled, stop actively searching for devices.
		/// </summary>
		private void OnDisable()
		{
			_activeProvider.OnDisableProvider();
		}

		private void Update()
		{
			if (UpdateMode != UnityUpdateMode.Update)
			{
				return;
			}

			_activeProvider.OnUpdate();
		}

		/// <summary>
		/// For each sensor, prompt them to get their buffer of updates from native code per fixed physics update step.
		/// </summary>
		private void FixedUpdate()
		{
			if (UpdateMode != UnityUpdateMode.FixedUpdate)
			{
				return;
			}

			_activeProvider.OnUpdate();
		}

		/// <summary>
		/// For each sensor, prompt them to get their buffer of updates from native code per late update step.
		/// If there are any device config updates, apply them at the end of the frame during a device update lock.
		/// </summary>
		private void LateUpdate()
		{
			if (_isDeviceStateUpdateLocked)
			{
				var secondsSinceLockStart = Time.time - _appTimeSinceDeviceStateUpdateLocked;

				// If we have not yet applied the device update, execute the update now.
				if (!_hasDeviceUpdateBeenApplied)
				{
					// Execute the update.
					UpdateDeviceFromConfig();
				}
				else if (secondsSinceLockStart >= WearableConstants.NumberOfSecondsToLockSensorFrequencyUpdate)
				{
					// If another pending update during the lock, execute it immediately/refresh the lock
					if (_isDeviceStateUpdatePendingDuringLock)
					{
						_isDeviceStateUpdatePendingDuringLock = false;

						// Execute the update.
						UpdateDeviceFromConfig();

						// Refresh the lock
						_appTimeSinceDeviceStateUpdateLocked = Time.time;
					}
					// Otherwise release the lock.
					else
					{
						UnlockDeviceStateUpdate();
					}
				}
			}

			if (UpdateMode != UnityUpdateMode.LateUpdate)
			{
				return;
			}

			_activeProvider.OnUpdate();
		}

		/// <summary>
		/// Both the underlying SDK and certain flows in the Connection UI need to operate with an
		/// understanding of the current application focus. We make that privately available through
		/// an internal-only event.
		/// </summary>
		/// <param name="hasFocus"></param>
		private void OnApplicationFocus(bool hasFocus)
		{
			_activeProvider.SetAppFocusChanged(hasFocus);

			if (AppFocusChanged != null)
			{
				AppFocusChanged.Invoke(hasFocus);
			}
		}

		#endregion
	}
}
