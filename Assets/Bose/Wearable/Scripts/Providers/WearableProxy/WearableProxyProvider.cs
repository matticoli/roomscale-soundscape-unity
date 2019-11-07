using System;
using System.Net.Sockets;
using UnityEngine;

namespace Bose.Wearable.Proxy
{
	/// <summary>
	/// Acts as a client for a WearableProxy server, allowing control of a remote device over a network connection.
	/// </summary>
	[Serializable]
	public class WearableProxyProvider : WearableProviderBase
	{

		#region Provider Unique

		public event Action ProxyConnected;
		public event Action ProxyDisconnected;

		public bool IsConnectedToProxy
		{
			get { return _server.Connected; }
		}

		/// <summary>
		/// Connect to a proxy at the specified address and port. Blocks up to the specified network timeout.
		/// </summary>
		/// <param name="hostname"></param>
		/// <param name="port"></param>
		/// <param name="onSuccess">Invoked on successful connection</param>
		/// <param name="onFailure">Invoked on failed connection</param>
		public void Connect(string hostname, int port, Action onSuccess = null, Action<Exception> onFailure = null)
		{
			if (_server.Connected)
			{
				return;
			}

			try
			{
				_server.Connect(hostname, port);
				_server.GetStream().WriteTimeout = (int)(_networkTimeout * 1000);
			}
			catch (Exception exception)
			{
				Debug.LogFormat(WearableConstants.ProxyProviderConnectionFailedWarning, hostname, port.ToString());

				if (onFailure == null)
				{
					throw;
				}

				onFailure.Invoke(exception);
			}

			_hostname = hostname;
			_portNumber = port;

			if (onSuccess != null)
			{
				onSuccess.Invoke();
			}

			if (ProxyConnected != null)
			{
				ProxyConnected.Invoke();
			}
		}

		/// <summary>
		/// Disconnect from the connected server.
		/// </summary>
		public void Disconnect()
		{
			if (!_server.Connected)
			{
				return;
			}

			_server.Close();

			_hostname = string.Empty;
			_portNumber = 0;

			if (ProxyDisconnected != null)
			{
				ProxyDisconnected.Invoke();
			}
		}

		#endregion

		#region Provider API

		internal override void SearchForDevices(
			AppIntentProfile appIntentProfile,
			Action<Device[]> onDevicesUpdated,
			bool autoReconnect,
			float autoReconnectTimeout)
		{
			base.SearchForDevices(appIntentProfile, onDevicesUpdated, autoReconnect, autoReconnectTimeout);

			if (autoReconnect)
			{
				throw new NotImplementedException();
			}

			_transmitIndex = 0;
			WearableProxyClientProtocol.EncodeInitiateDeviceSearch(_transmitBuffer, ref _transmitIndex);
			SendTransmitBuffer();
			_searchingForDevices = true;
		}

		internal override void StopSearchingForDevices()
		{
			base.StopSearchingForDevices();

			_transmitIndex = 0;
			WearableProxyClientProtocol.EncodeStopDeviceSearch(_transmitBuffer, ref _transmitIndex);
			SendTransmitBuffer();
			_searchingForDevices = false;
		}

		internal override void CancelDeviceConnection()
		{
			throw new NotImplementedException();
		}

		internal override void ConnectToDevice(Device device, Action onSuccess, Action onFailure)
		{
			_transmitIndex = 0;
			WearableProxyClientProtocol.EncodeConnectToDevice(_transmitBuffer, ref _transmitIndex, device.uid);
			SendTransmitBuffer();
			_connectingToDevice = true;
			_deviceConnectSuccessCallback = onSuccess;
			_deviceConnectFailureCallback = onFailure;
		}

		internal override void DisconnectFromDevice()
		{
			_transmitIndex = 0;
			WearableProxyClientProtocol.EncodeDisconnectFromDevice(_transmitBuffer, ref _transmitIndex);
			SendTransmitBuffer();

			if (_connectedDevice != null)
			{
				// We can immediately disconnect the client without waiting for a response.
				OnConnectionStatusChanged(ConnectionStatus.Disconnected, _connectedDevice);
				_connectedDevice = null;
				ResetDeviceStatus();
				_sendConfigSuccessNextFrame = false;
			}
		}

		internal override FirmwareUpdateInformation GetFirmwareUpdateInformation()
		{
			return WearableConstants.DefaultFirmwareUpdateInformation;
		}

		internal override void SelectFirmwareUpdateOption(int index)
		{

		}

		internal override WearableDeviceConfig GetCachedDeviceConfiguration()
		{
			// If we've never received data, there's not much we can do but warn the user, request the data, and return defaults.
			if (_config == null)
			{
				Debug.LogWarning(WearableConstants.ProxyProviderNoDataWarning);
				RequestDeviceConfigurationInternal();
				return WearableConstants.DisabledDeviceConfig;
			}

			return _config;
		}

		protected override void RequestDeviceConfigurationInternal()
		{
			_transmitIndex = 0;
			WearableProxyClientProtocol.EncodeQueryConfig(_transmitBuffer, ref _transmitIndex);
			SendTransmitBuffer();
		}

		protected override void RequestIntentProfileValidationInternal(AppIntentProfile appIntentProfile)
		{
			// FIXME: Temporarily force this to succeed until the proxy functionality is implemented
			OnReceivedIntentValidationResponse(true);
		}

		internal override void SetDeviceConfiguration(WearableDeviceConfig config)
		{
			_transmitIndex = 0;
			WearableProxyClientProtocol.EncodeSetNewConfig(_transmitBuffer, ref _transmitIndex, config);
			SendTransmitBuffer();

			// Since failed attempts, retries, etc are handled on the <i>server</i> side, all configuration attempts
			// should be considered a success as long as the packet was sent out.
			_sendConfigSuccessNextFrame = true;
		}

		internal override DynamicDeviceInfo GetDynamicDeviceInfo()
		{
			throw new NotImplementedException();
		}

		internal override void OnEnableProvider()
		{
			if (_enabled)
			{
				return;
			}

			if (!string.IsNullOrEmpty(_hostname) && _portNumber != 0)
			{
				// If we were previously connected, try to reconnect, but ignore failures here
				try
				{
					Connect(_hostname, _portNumber);
				}
				catch
				{
					// Suppress errors; this is a convenience feature.
				}
			}

			base.OnEnableProvider();
			_sendConfigSuccessNextFrame = false;
		}

		internal override void OnDisableProvider()
		{
			if (!_enabled)
			{
				return;
			}

			base.OnDisableProvider();

			// If connected, temporarily disconnect until provider is re-enabled
			if (_server.Connected)
			{
				Disconnect();
			}
		}

		internal override void OnUpdate()
		{
			if (!_server.Connected)
			{
				return;
			}

			_currentSensorFrames.Clear();

			if (_sendConfigSuccessNextFrame)
			{
				_sendConfigSuccessNextFrame = false;
				OnConfigurationSucceeded();
			}

			// Receive data from the server
			try
			{
				NetworkStream stream = _server.GetStream();
				while (stream.DataAvailable)
				{

					int bufferSpaceRemaining = _receiveBuffer.Length - _receiveIndex;
					if (bufferSpaceRemaining <= 0)
					{
						// Can't fit any more packets or consume more buffer; dump the buffer to free space.
						Debug.LogWarning(WearableConstants.ProxyProviderBufferFullWarning);
						_receiveIndex = 0;
						bufferSpaceRemaining = _receiveBuffer.Length;
					}

					int actualBytesRead = stream.Read(_receiveBuffer, _receiveIndex, bufferSpaceRemaining);
					_receiveIndex += actualBytesRead;

					ProcessReceiveBuffer();
				}
			}
			catch (Exception exception)
			{
				// The server has disconnected, or some other error
				HandleProxyDisconnect(exception);
			}

			// Allow the provider base to do its own update work
			base.OnUpdate();
		}
		#endregion

		#region Private

		[SerializeField]
		private float _networkTimeout;

		[SerializeField]
		private int _portNumber;

		[SerializeField]
		private string _hostname;

		private WearableDeviceConfig _config;

		private readonly TcpClient _server;

		private readonly WearableProxyClientProtocol _protocol;
		private bool _issuedWarningLastPacket;
		private int _receiveIndex;
		private readonly byte[] _receiveBuffer;
		private int _transmitIndex;
		private readonly byte[] _transmitBuffer;

		private bool _sendConfigSuccessNextFrame;

		private bool _searchingForDevices;

		private bool _connectingToDevice;
		private Device _deviceToConnect;
		private Action _deviceConnectFailureCallback;
		private Action _deviceConnectSuccessCallback;

		internal WearableProxyProvider()
		{
			_config = null;

			ResetDeviceStatus();

			_networkTimeout = 1.0f;

			_server = new TcpClient();

			_protocol = new WearableProxyClientProtocol();
			_protocol.ConnectionStatus += OnConnectionStatus;
			_protocol.DeviceList += OnDeviceList;
			_protocol.KeepAlive += OnKeepAlive;
			_protocol.ConfigStatus += OnConfigStatus;
			_protocol.NewSensorFrame += OnNewSensorFrame;
			_protocol.PingQuery += OnPingQuery;

			_portNumber = 0;
			_hostname = string.Empty;

			_receiveIndex = 0;
			_receiveBuffer = new byte[WearableProxyProtocolBase.SuggestedServerToClientBufferSize];
			_transmitIndex = 0;
			_transmitBuffer = new byte[WearableProxyProtocolBase.SuggestedClientToServerBufferSize];

			_issuedWarningLastPacket = false;
		}

		/// <summary>
		/// Called when a Keep Alive packet is received
		/// </summary>
		private void OnKeepAlive()
		{
			// No-op
		}

		/// <summary>
		/// Called when a new Sensor Frame is received. Updates the stored frames.
		/// </summary>
		/// <param name="frame"></param>
		private void OnNewSensorFrame(SensorFrame frame)
		{
			_currentSensorFrames.Add(frame);
			_lastSensorFrame = frame;

			OnSensorsUpdated(frame);

			if (frame.gestureId != GestureId.None)
			{
				OnGestureDetected(frame.gestureId);
			}
		}

		/// <summary>
		/// Called when a Connection Status packet is received. Invokes connection & disconnection events as needed.
		/// </summary>
		/// <param name="state"></param>
		/// <param name="device"></param>
		private void OnConnectionStatus(WearableProxyProtocolBase.ConnectionState state, Device? device)
		{
			switch (state)
			{
				case WearableProxyProtocolBase.ConnectionState.Disconnected:
					if (_connectedDevice != null)
					{
						OnConnectionStatusChanged(ConnectionStatus.Disconnected, _connectedDevice);
					}
					_connectedDevice = null;

					ResetDeviceStatus();
					break;

				case WearableProxyProtocolBase.ConnectionState.Connecting:
					if (device != null)
					{
						OnConnectionStatusChanged(ConnectionStatus.Connecting, device);
					}
					break;

				case WearableProxyProtocolBase.ConnectionState.Connected:
					if (device != null && (_connectedDevice == null || _connectedDevice != device))
					{
						// If this is a newly connected device, fire off events
						_connectedDevice = device;

						if (_connectingToDevice)
						{
							// Indicates the connection was initiated by this client
							if (_deviceConnectSuccessCallback != null)
							{
								_deviceConnectSuccessCallback.Invoke();
							}
						}

						OnConnectionStatusChanged(ConnectionStatus.Connected, device);
					}
					else
					{
						// Otherwise, it's just a query, and doesn't need anything special.
						_connectedDevice = device;
					}

					_connectingToDevice = false;
					break;

				case WearableProxyProtocolBase.ConnectionState.Failed:
					_connectedDevice = null;

					if (_connectingToDevice)
					{
						// Indicates the connection was initiated by this client
						if (_deviceConnectFailureCallback != null)
						{
							_deviceConnectFailureCallback.Invoke();
						}
					}

					_connectingToDevice = false;
					break;

				default:
					Debug.LogWarning(WearableConstants.ProxyProviderInvalidPacketError);
					break;
			}
		}

		/// <summary>
		/// Called when a Device List packet is received. If the client initiated the request, return the list to the user.
		/// </summary>
		/// <param name="devices"></param>
		private void OnDeviceList(Device[] devices)
		{
			if (_searchingForDevices)
			{
				// This is in response to a search
				OnReceivedSearchDevices(devices);
			}
			// ReSharper disable once RedundantIfElseBlock
			else
			{
				// Unsolicited device list indicates that someone else on the network is searching
				// This is safe to ignore.
			}
		}

		/// <summary>
		/// Called when the server sends an update to the device config information. Copies this config into local state.
		/// </summary>
		/// <param name="config"></param>
		private void OnConfigStatus(WearableDeviceConfig config)
		{
			_config = config.Clone();

			// It's fine to invoke this every time since the provider base will determine if the call was in response to
			// a request.
			OnReceivedDeviceConfiguration(config);
		}

		private void OnPingQuery()
		{
			_transmitIndex = 0;
			WearableProxyProtocolBase.EncodePingResponse(_transmitBuffer, ref _transmitIndex);
			SendTransmitBuffer();
		}

		/// <summary>
		/// Send the accumulated transmit buffer to the connected server.
		/// </summary>
		private void SendTransmitBuffer()
		{
			if (!_server.Connected)
			{
				// If we're not connected, we can't really do anything here. Show a warning and quit.
				Debug.LogWarning(WearableConstants.ProxyProviderNotConnectedWarning);
				return;
			}

			try
			{
				NetworkStream stream = _server.GetStream();
				stream.WriteTimeout = (int) (1000 * _networkTimeout);
				stream.Write(_transmitBuffer, 0, _transmitIndex);
			}
			catch (Exception exception)
			{
				HandleProxyDisconnect(exception);
			}
		}

		/// <summary>
		/// Process all packets in the buffer and delegate to relevant packet events. If a partial packet is left at the
		/// end of the buffer, it is copied back to the front to be processed next time data arrives.
		/// Dumps the buffer if a corrupt or unknown packet is encountered.
		/// </summary>
		private void ProcessReceiveBuffer()
		{

			int packetIndex = 0;
			while (packetIndex < _receiveIndex)
			{
				int packetStart = packetIndex;
				try
				{
					_protocol.ProcessPacket(_receiveBuffer, ref packetIndex);
				}
				catch (WearableProxyProtocolException exception)
				{
					// A packet could not be parsed, which means the whole buffer needs to be thrown away.
					if (!_issuedWarningLastPacket)
					{
						// Only issue warnings if we've previously parsed a packet correctly. This prevents flooding
						// in the case of mismatched versions, etc.
						Debug.LogWarning(exception.ToString());
						_issuedWarningLastPacket = true;
					}

					_receiveIndex = 0;
					return;
				}
				catch (IndexOutOfRangeException)
				{
					// The packet could not be completely decoded, meaning it is likely split across buffers.
					// Copy the fragment to the beginning of the buffer and try again the next time a buffer comes in.
					for (int i = packetStart; i < _receiveBuffer.Length; i++)
					{
						_receiveBuffer[i - packetStart] = _receiveBuffer[i];
					}

					// Position the receive index right after the partial packet
					_receiveIndex = _receiveBuffer.Length - packetStart;
					return;

				}
				_issuedWarningLastPacket = false;
			}

			_receiveIndex = 0;
		}

		/// <summary>
		/// Called when the proxy disconnected because of a socket error. Handles retries and event invocation.
		/// </summary>
		/// <param name="exception"></param>
		private void HandleProxyDisconnect(Exception exception = null)
		{
			// TODO: Automatic reconnection attempts

			if (exception != null)
			{
				Debug.Log(exception.ToString());
			}

			_server.Close();
			if (ProxyDisconnected != null)
			{
				ProxyDisconnected.Invoke();
			}
		}

		/// <summary>
		/// Call when we don't know the status of the device.
		/// </summary>
		private void ResetDeviceStatus()
		{
			_config = null;
		}

		#endregion
	}
}
