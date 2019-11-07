using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bose.Wearable
{
	[Serializable]
	public abstract class WearableProviderBase
	{
		/// <summary>
		/// Invoked when the connection status changes, such as when a device connects, disconnects, requires a
		/// firmware update, etc. Not all statuses will provide an associated device.
		/// </summary>
		internal event Action<ConnectionStatus, Device?> ConnectionStatusChanged;

		/// <summary>
		/// Invoked when there are sensor updates from the Wearable device.
		/// </summary>
		internal event Action<SensorFrame> SensorsUpdated;

		/// <summary>
		/// Invoked when there are gesture updates from the Wearable device.
		/// </summary>
		internal event Action<GestureId> GestureDetected;

		/// <summary>
		/// Invoked when a configuration attempt was successful.
		/// </summary>
		internal event Action ConfigurationSucceeded;

		/// <summary>
		/// Invoked when a configuration attempt failed.
		/// </summary>
		internal event Action<ConfigStatus, ConfigStatus> ConfigurationFailed;

		/// <summary>
		/// Invoked when service is suspended on the device.
		/// </summary>
		internal event Action<SensorServiceSuspendedReason> SensorServiceSuspended;
		/// <summary>

		/// Invoked when service is resumed.
		/// </summary>
		internal event Action SensorServiceResumed;

		/// <summary>
		/// Whether or not the provider has been initialized
		/// </summary>
		internal bool Initialized
		{
			get { return _initialized; }
		}

		protected bool _initialized;

		/// <summary>
		/// Whether or not the provider is enabled
		/// </summary>
		internal bool Enabled
		{
			get { return _enabled; }
		}

		protected bool _enabled;

		/// <summary>
		/// The last reported value for the sensors.
		/// </summary>
		internal SensorFrame LastSensorFrame
		{
			get { return _lastSensorFrame; }
		}

		protected SensorFrame _lastSensorFrame;

		/// <summary>
		/// A list of SensorFrames returned from the plugin bridge in order from oldest to most recent.
		/// </summary>
		internal List<SensorFrame> CurrentSensorFrames
		{
			get { return _currentSensorFrames; }
		}

		protected List<SensorFrame> _currentSensorFrames;

		/// <summary>
		/// A list of GestureData returned from the bridge in order from oldest to most recent.
		/// The list is empty if no gestures were returned in the most recent update.
		/// </summary>
		internal List<GestureData> CurrentGestureData
		{
			get { return _currentGestureData; }
		}

		protected List<GestureData> _currentGestureData;

		/// <summary>
		/// The Wearable device that is currently connected in Unity.
		/// </summary>
		internal Device? ConnectedDevice
		{
			get { return _connectedDevice; }
		}

		protected Device? _connectedDevice;

		/// <summary>
		/// Whether or not the user has requested fresh device configuration, and is waiting on a response.
		/// </summary>
		protected bool WaitingForDeviceConfig
		{
			get { return _waitingForDeviceConfig; }
		}

		private bool _waitingForDeviceConfig;

		/// <summary>
		/// Currently waiting for a response to an outstanding intent validation request.
		/// </summary>
		public bool WaitingForIntentValidation
		{
			get { return _waitingForIntentValidation; }
		}

		private bool _waitingForIntentValidation;

		/// <summary>
		/// The last received device status.
		/// </summary>
		private DeviceStatus _lastDeviceStatus;

		/// <summary>
		/// An event holding any callbacks that have subscribed to the request for new configuration.
		/// </summary>
		private event Action<WearableDeviceConfig> DeviceConfigRequestSubscribers;

		/// <summary>
		/// An event holding callbacks that have subscribed for a list of devices during searching.
		/// </summary>
		private event Action<Device[]> DeviceSearchCallback;

		/// <summary>
		/// The provider's connection status.
		/// </summary>
		public ConnectionStatus ConnectionStatus
		{
			get { return _connectionStatus; }
		}

		private ConnectionStatus _connectionStatus;

		/// <summary>
		/// Should the provider attempt to reconnect to the last successfully connected device?
		/// </summary>
		protected bool _autoReconnect;

		/// <summary>
		/// How long should we search for the last successfully connected device before giving
		/// the ability to switch back to searching for devices.
		/// </summary>
		protected float _autoReconnectTimeout;

		/// <summary>
		/// The UID of the last successfully connected (including secure connection, firmware updates, and
		/// intent validation) device. Stored and accessed through PlayerPrefs.
		/// </summary>
		private string LastConnectedDeviceUID
		{
			get { return PlayerPrefs.GetString(WearableConstants.PrefLastConnectedDeviceUID, string.Empty); }
			set { PlayerPrefs.SetString(WearableConstants.PrefLastConnectedDeviceUID, value);}
		}

		/// <summary>
		/// Returns the <see cref="DeviceConnectionInfo"/> for the current connection
		/// </summary>
		/// <returns></returns>
		public virtual DeviceConnectionInfo GetDeviceConnectionInfo()
		{
			return new DeviceConnectionInfo();
		}

		/// <summary>
		/// Searches for all Wearable devices that can be connected to.
		/// </summary>
		/// <param name="appIntentProfile"></param>
		/// <param name="onDevicesUpdated"></param>
		/// <param name="autoReconnect"></param>
		/// <param name="autoReconnectTimeout"></param>
		internal virtual void SearchForDevices(
			AppIntentProfile appIntentProfile,
			Action<Device[]> onDevicesUpdated,
			bool autoReconnect,
			float autoReconnectTimeout)
		{
			DeviceSearchCallback += onDevicesUpdated;
			_autoReconnect = autoReconnect;
			_autoReconnectTimeout = Time.unscaledTime + autoReconnectTimeout;
		}

		/// <summary>
		/// Stops searching for Wearable devices that can be connected to.
		/// </summary>
		internal virtual void StopSearchingForDevices()
		{
			DeviceSearchCallback = null;
		}

		internal void OnReceivedSearchDevices(Device[] devices)
		{
			// If we are currently attempting to auto-reconnect, we will compare every series of devices received
			// against the last connected device UID. If we find a match, we immediately connect to that device.
			if (_connectionStatus == ConnectionStatus.AutoReconnect)
			{
				string uid = LastConnectedDeviceUID;

				for (int i = 0; i < devices.Length; ++i)
				{
					if (devices[i].uid == uid)
					{
						ConnectToDevice(devices[i], null, null);
						break;
					}
				}
			}
			else
			{
				if (DeviceSearchCallback != null)
				{
					DeviceSearchCallback.Invoke(devices);
				}
			}
		}

		/// <summary>
		/// Cancels any currently running device connection if there is one.
		/// </summary>
		internal abstract void CancelDeviceConnection();

		/// <summary>
		/// Connects to a specified device and invokes either <paramref name="onSuccess"/> or <paramref name="onFailure"/>
		/// depending on the result.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="onSuccess"></param>
		/// <param name="onFailure"></param>
		internal abstract void ConnectToDevice(Device device, Action onSuccess, Action onFailure);

		/// <summary>
		/// Stops all attempts to connect to or monitor a device and disconnects from a device if connected.
		/// </summary>
		internal abstract void DisconnectFromDevice();

		/// <summary>
		/// If the device needs a firmware update, grab the relevant information.
		/// </summary>
		internal abstract FirmwareUpdateInformation GetFirmwareUpdateInformation();

		/// <summary>
		/// If the device is waiting for the user to choose to update out-of-date firmware, this tells the
		/// provider which option they have selected.
		/// </summary>
		internal abstract void SelectFirmwareUpdateOption(int index);

		/// <summary>
		/// Set all relevant device information for a WearableDevice, including sensors, gestures, and update interval.
		/// </summary>
		/// <param name="config"></param>
		internal abstract void SetDeviceConfiguration(WearableDeviceConfig config);

		/// <summary>
		/// Returns a reference to the provider's cached internal configuration state. May not be up-to-date with the
		/// connected device.
		/// </summary>
		/// <returns></returns>
		internal abstract WearableDeviceConfig GetCachedDeviceConfiguration();

		/// <summary>
		/// Instructs the provider to request up-to-date configuration from the attached device. As this operation may
		/// take an indeterminate amount of time, a callback is invoked when the operation completes.
		/// Multiple calls to this method while a request is waiting for completion will not request the configuration
		/// multiple times, but will invoke all callbacks set during the request.
		/// Providers are permitted to invoke the callback from this call if they have up-to-date configuration already.
		/// </summary>
		internal void RequestDeviceConfiguration(Action<WearableDeviceConfig> callback)
		{
			if (!_connectedDevice.HasValue)
			{
				Debug.LogWarning(WearableConstants.DeviceIsNotCurrentlyConnected);
				_waitingForDeviceConfig = false;
				return;
			}

			DeviceConfigRequestSubscribers += callback;
			if (!_waitingForDeviceConfig)
			{
				_waitingForDeviceConfig = true;
				RequestDeviceConfigurationInternal();
			}
		}

		/// <summary>
		/// Makes the actual request for fresh device configuration. This request should call
		/// <see cref="OnReceivedDeviceConfiguration"/> on completion to trigger subscribed callbacks.
		/// </summary>
		protected abstract void RequestDeviceConfigurationInternal();

		/// <summary>
		/// Returns the device status of the connected device.
		/// </summary>
		/// <returns></returns>
		internal abstract DynamicDeviceInfo GetDynamicDeviceInfo();

		/// <summary>
		/// Triggers a request for validation of a given <see cref="AppIntentProfile"/>. This request will
		/// call the <see cref="callback"/> when the validation is complete.
		/// </summary>
		/// <param name="profile"></param>
		internal void RequestIntentProfileValidation(AppIntentProfile profile, Action<bool> callback)
		{
			if (!_connectedDevice.HasValue)
			{
				Debug.LogWarning(WearableConstants.DeviceIsNotCurrentlyConnected);
				_waitingForIntentValidation = false;
				return;
			}

			if (!_waitingForIntentValidation)
			{
				IntentValidationSubscribers += callback;
				_waitingForIntentValidation = true;
				RequestIntentProfileValidationInternal(profile);

				// Clear the dirty flag after the request has been sent out, so if any
				// change occurs after this point, we know that a re-validation would
				// be necessary.
				profile.IsDirty = false;
			}
			else
			{
				// If there is an outstanding request, generate an error and ignore this call.
				Debug.LogError(WearableConstants.TooManyValidationRequestsError);
			}
		}

		/// <summary>
		/// Makes the actual request to validate a device configuration. This request should call
		/// <see cref="OnReceivedIntentValidationResponse"/> on completion to trigger subscribed callbacks.
		/// </summary>
		/// <param name="appIntentProfile"></param>
		protected abstract void RequestIntentProfileValidationInternal(AppIntentProfile appIntentProfile);

		/// <summary>
		/// Informs the provider that the application focus has been changed. This is called directly
		/// from WearableControl on the active provider and the event is a relay from Unity's OnApplicationFocus
		/// event.
		/// </summary>
		/// <param name="hasFocus"></param>
		internal virtual void SetAppFocusChanged(bool hasFocus)
		{
			// no-op.
		}

		/// <summary>
		/// Called by <see cref="WearableControl"/> when the provider is first initialized.
		/// Providers must call <code>base.OnInitializeProvider()</code> when overriding to update internal state.
		/// </summary>
		internal virtual void OnInitializeProvider()
		{
			_initialized = true;
			_enabled = false;
			_waitingForDeviceConfig = false;
			_waitingForIntentValidation = false;
			DeviceConfigRequestSubscribers = null;
			_lastDeviceStatus = WearableConstants.EmptyDeviceStatus;
			_connectionStatus = ConnectionStatus.Disconnected;
		}

		/// <summary>
		/// Called by <see cref="WearableControl"/> when the provider is destroyed at application quit.
		/// Providers must call <code>base.OnDestroyProvider()</code> when overriding to update internal state.
		/// </summary>
		internal virtual void OnDestroyProvider()
		{
			_initialized = false;
			_enabled = false;
			_waitingForDeviceConfig = false;
			DeviceConfigRequestSubscribers = null;
			_lastDeviceStatus = WearableConstants.EmptyDeviceStatus;
			_connectionStatus = ConnectionStatus.Disconnected;
		}

		/// <summary>
		/// Called by <see cref="WearableControl"/> when the provider is being enabled.
		/// Automatically sets the connection status to Connected if a device is still connected.
		/// Providers must call <code>base.OnEnableProvider()</code> when overriding to update internal state.
		/// </summary>
		internal virtual void OnEnableProvider()
		{
			_enabled = true;

			if (_connectedDevice != null)
			{
				OnConnectionStatusChanged(ConnectionStatus.Connected, _connectedDevice.Value);
			}
		}

		/// <summary>
		/// Called by <see cref="WearableControl"/> when the provider is being disabled.
		/// Automatically sets the connection status to disconnected if a device is still connected.
		/// Providers must call <code>base.OnDisableProvider()</code> when overriding to update internal state.
		/// </summary>
		internal virtual void OnDisableProvider()
		{
			_enabled = false;

			StopSearchingForDevices();

			if (_connectedDevice != null)
			{
				OnConnectionStatusChanged(ConnectionStatus.Disconnected, _connectedDevice.Value);
			}
		}

		/// <summary>
		/// Called by <see cref="WearableControl"/> during the appropriate Unity update method if the provider is active.
		/// </summary>
		internal virtual void OnUpdate()
		{
			if (_connectionStatus == ConnectionStatus.AutoReconnect)
			{
				if (string.IsNullOrEmpty(LastConnectedDeviceUID) || Time.unscaledTime > _autoReconnectTimeout)
				{
					OnConnectionStatusChanged(ConnectionStatus.Searching);
				}
			}

			if (_connectedDevice != null)
			{
				MonitorDynamicDeviceInfo();
			}
		}

		/// <summary>
		/// Checks the device for dynamic data changes during a session.
		/// </summary>
		private void MonitorDynamicDeviceInfo()
		{
			DynamicDeviceInfo dynamicDeviceInfo = GetDynamicDeviceInfo();

			if (_connectedDevice.HasValue)
			{
				Device device = _connectedDevice.Value;
				device.deviceStatus = dynamicDeviceInfo.deviceStatus;
				device.transmissionPeriod = dynamicDeviceInfo.transmissionPeriod;
				_connectedDevice = device;
			}

			// Calculate incoming outgoing events
			DeviceStatus deviceStatus = dynamicDeviceInfo.deviceStatus;
			DeviceStatus risingEvents = deviceStatus.GetRisingEdges(_lastDeviceStatus);
			DeviceStatus fallingEvents = deviceStatus.GetFallingEdges(_lastDeviceStatus);
			_lastDeviceStatus = deviceStatus;

			// Service suspended/resumes
			if (risingEvents.GetFlagValue(DeviceStatusFlags.SensorServiceSuspended))
			{
				SensorServiceSuspendedReason reason = deviceStatus.GetServiceSuspendedReason();

				if (SensorServiceSuspended != null)
				{
					SensorServiceSuspended.Invoke(reason);
				}
			}

			if (fallingEvents.GetFlagValue(DeviceStatusFlags.SensorServiceSuspended))
			{
				if (SensorServiceResumed != null)
				{
					SensorServiceResumed.Invoke();
				}
			}
		}

		/// <summary>
		/// An event holding any callbacks that have subscribed to the request for intent validation
		/// </summary>
		private event Action<bool> IntentValidationSubscribers;

		protected WearableProviderBase()
		{
			_currentSensorFrames = new List<SensorFrame>();
			_lastSensorFrame = WearableConstants.EmptyFrame;

			_currentGestureData = new List<GestureData>();
		}

		/// <summary>
		/// Invokes any callbacks registered by <see cref="RequestDeviceConfiguration"/> and marks the request as
		/// complete.
		/// </summary>
		/// <param name="config"></param>
		internal void OnReceivedDeviceConfiguration(WearableDeviceConfig config)
		{
			if (!_enabled)
			{
				return;
			}

			if (_waitingForDeviceConfig && DeviceConfigRequestSubscribers != null)
			{
				DeviceConfigRequestSubscribers.Invoke(config);
			}

			_waitingForDeviceConfig = false;
			DeviceConfigRequestSubscribers = null;
		}

		/// <summary>
		/// Invokes any callbacks registered by <see cref="RequestIntentProfileValidation"/> and marks the request as
		/// complete.
		/// </summary>
		/// <param name="valid"></param>
		protected void OnReceivedIntentValidationResponse(bool valid)
		{
			if (!_enabled)
			{
				return;
			}

			if (_waitingForIntentValidation && IntentValidationSubscribers != null)
			{
				IntentValidationSubscribers.Invoke(valid);
			}

			_waitingForIntentValidation = false;
			IntentValidationSubscribers = null;
		}

		/// <summary>
		/// Used by inheriting providers to indicate that the connection status has changed.
		/// </summary>
		/// <param name="status"></param>
		/// <param name="device"></param>
		protected void OnConnectionStatusChanged(ConnectionStatus status, Device? device = null)
		{
			if (status == ConnectionStatus.Disconnected)
			{
				_waitingForDeviceConfig = false;
				DeviceConfigRequestSubscribers = null;

				_waitingForIntentValidation = false;
				IntentValidationSubscribers = null;
			}
			else if (status == ConnectionStatus.Connected && device.HasValue)
			{
				// we store the UID of the device for auto-reconnection in the future.
				LastConnectedDeviceUID = device.Value.uid;
			}

			if (_connectionStatus == status)
			{
				return;
			}

			_connectionStatus = status;

			if (ConnectionStatusChanged != null)
			{
				ConnectionStatusChanged.Invoke(_connectionStatus, device);
			}
		}

		/// <summary>
		/// Invokes the <see cref="SensorsUpdated"/> event.
		/// </summary>
		/// <param name="frame"></param>
		protected void OnSensorsUpdated(SensorFrame frame)
		{
			if (SensorsUpdated != null)
			{
				SensorsUpdated.Invoke(frame);
			}
		}

		/// <summary>
		/// Invokes the <see cref="GestureDetected"/> event.
		/// </summary>
		/// <param name="gestureId"></param>
		protected void OnGestureDetected(GestureId gestureId)
		{
			if (GestureDetected != null)
			{
				GestureDetected.Invoke(gestureId);
			}
		}

		/// <summary>
		/// Invokes the <see cref="OnConfigurationSucceeded"/> event.
		/// </summary>
		protected void OnConfigurationSucceeded()
		{
			if (ConfigurationSucceeded != null)
			{
				ConfigurationSucceeded.Invoke();
			}
		}

		/// <summary>
		/// Invokes the <see cref="OnConfigurationFailed"/> event.
		/// </summary>
		/// <param name="sensor"></param>
		/// <param name="gesture"></param>
		internal void OnConfigurationFailed(ConfigStatus sensor, ConfigStatus gesture)
		{
			if (ConfigurationFailed != null)
			{
				ConfigurationFailed.Invoke(sensor, gesture);
			}
		}
	}
}
