using System;

namespace Bose.Wearable.Proxy
{
	/// <summary>
	/// Implements client-side functionality of the WearableProxy Transport Protocol
	/// </summary>
	internal sealed class WearableProxyClientProtocol : WearableProxyProtocolBase
	{
		public event Action KeepAlive;
		public event Action PingQuery;
		public event Action PingResponse;
		public event Action<SensorFrame> NewSensorFrame;
		public event Action<Device[]> DeviceList;
		public event Action<ConnectionState, Device?> ConnectionStatus;
		public event Action<WearableDeviceConfig> ConfigStatus;

		#region Decoding

		/// <summary>
		/// Consume a packet from the buffer if possible, then advance the buffer index.
		/// </summary>
		/// <param name="buffer">Byte buffer to decode</param>
		/// <param name="index">(Ref) Index to read into buffer</param>
		/// <exception cref="WearableProxyProtocolException">Thrown when a packet cannot be decoded and the buffer
		/// must be discarded.</exception>
		/// <exception cref="IndexOutOfRangeException">Thrown when a packet was partially consumed but ran out of
		/// buffer contents.</exception>
		public override void ProcessPacket(byte[] buffer, ref int index)
		{
			PacketTypeCode packetType = DecodePacketType(buffer, ref index);

			switch (packetType)
			{
				case PacketTypeCode.KeepAlive:
				{
					CheckFooter(buffer, ref index);

					if (KeepAlive != null)
					{
						KeepAlive.Invoke();
					}

					break;
				}
				case PacketTypeCode.PingQuery:
				{
					CheckFooter(buffer, ref index);

					if (PingQuery != null)
					{
						PingQuery.Invoke();
					}

					break;
				}
				case PacketTypeCode.PingResponse:
				{
					CheckFooter(buffer, ref index);

					if (PingResponse != null)
					{
						PingResponse.Invoke();
					}

					break;
				}
				case PacketTypeCode.SensorFrame:
				{
					SensorFrame frame = DecodeSensorFrame(buffer, ref index);
					CheckFooter(buffer, ref index);

					if (NewSensorFrame != null)
					{
						NewSensorFrame.Invoke(frame);
					}

					break;
				}
				case PacketTypeCode.DeviceList:
				{
					Device[] devices = DecodeDeviceList(buffer, ref index);
					CheckFooter(buffer, ref index);

					if (DeviceList != null)
					{
						DeviceList.Invoke(devices);
					}

					break;
				}
				case PacketTypeCode.ConnectionStatus:
				{
					Device? device;
					ConnectionState status = DecodeConnectionStatus(buffer, ref index, out device);
					CheckFooter(buffer, ref index);

					if (ConnectionStatus != null)
					{
						ConnectionStatus.Invoke(status, device);
					}

					break;
				}
				case PacketTypeCode.ConfigStatus:
				{
					WearableDeviceConfig config = DeserializeDeviceConfig(buffer, ref index);
					CheckFooter(buffer, ref index);

					if (ConfigStatus != null)
					{
						ConfigStatus.Invoke(config);
					}
					break;
				}
				case PacketTypeCode.SetRssiFilter:
				case PacketTypeCode.InitiateDeviceSearch:
				case PacketTypeCode.StopDeviceSearch:
				case PacketTypeCode.ConnectToDevice:
				case PacketTypeCode.DisconnectFromDevice:
				case PacketTypeCode.QueryConnectionStatus:
				case PacketTypeCode.SetNewConfig:
				case PacketTypeCode.QueryConfig:
					// This is a known, but contextually-invalid packet type
					throw new WearableProxyProtocolException(WearableConstants.ProxyProviderInvalidPacketError);
				default:
					// This is an unknown or invalid packet type
					throw new WearableProxyProtocolException(WearableConstants.ProxyProviderInvalidPacketError);
			}
		}

		#endregion

		#region Encoding

		/// <summary>
		/// Encode an RSSI Filter packet into the specified buffer
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="index"></param>
		/// <param name="threshold"></param>
		public static void EncodeRSSIFilterControl(byte[] buffer, ref int index, int threshold)
		{
			// Encode header
			PacketHeader header = new PacketHeader(PacketTypeCode.SetRssiFilter);
			SerializeGenericPacket(buffer, ref index, header);

			// Encode payload
			RSSIFilterControlPacket packet = new RSSIFilterControlPacket
			{
				threshold = threshold
			};
			SerializeGenericPacket(buffer, ref index, packet);

			// Encode footer
			SerializeGenericPacket(buffer, ref index, _footer);
		}

		/// <summary>
		/// Encode an Initiate Device Search packet into the specified buffer
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="index"></param>
		public static void EncodeInitiateDeviceSearch(byte[] buffer, ref int index)
		{
			// Encode header
			PacketHeader header = new PacketHeader(PacketTypeCode.InitiateDeviceSearch);
			SerializeGenericPacket(buffer, ref index, header);

			// No payload

			// Encode footer
			SerializeGenericPacket(buffer, ref index, _footer);
		}

		/// <summary>
		/// Encode a Stop Device Search packet into the specified buffer
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="index"></param>
		public static void EncodeStopDeviceSearch(byte[] buffer, ref int index)
		{
			// Encode header
			PacketHeader header = new PacketHeader(PacketTypeCode.StopDeviceSearch);
			SerializeGenericPacket(buffer, ref index, header);

			// No payload

			// Encode footer
			SerializeGenericPacket(buffer, ref index, _footer);
		}

		/// <summary>
		/// Encode a Connect to Device packet into the specified buffer
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="index"></param>
		/// <param name="uid"></param>
		/// <exception cref="InvalidOperationException"></exception>
		public static void EncodeConnectToDevice(byte[] buffer, ref int index, string uid)
		{
			// Encode header
			PacketHeader header = new PacketHeader(PacketTypeCode.ConnectToDevice);
			SerializeGenericPacket(buffer, ref index, header);

			// Encode payload
			DeviceConnectPacket packet = new DeviceConnectPacket();
			unsafe
			{
				SerializeFixedString(uid, (IntPtr) packet.uid.value, DeviceUID.Length);
			}
			SerializeGenericPacket(buffer, ref index, packet);

			// Encode footer
			SerializeGenericPacket(buffer, ref index, _footer);
		}

		/// <summary>
		/// Encode a Disconnect from Device packet into the specified buffer
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="index"></param>
		public static void EncodeDisconnectFromDevice(byte[] buffer, ref int index)
		{
			// Encode header
			PacketHeader header = new PacketHeader(PacketTypeCode.DisconnectFromDevice);
			SerializeGenericPacket(buffer, ref index, header);

			// No payload

			// Encode footer
			SerializeGenericPacket(buffer, ref index, _footer);
		}

		/// <summary>
		/// Encode a Query Connection Status packet into the specified buffer
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="index"></param>
		public static void EncodeQueryConnectionStatus(byte[] buffer, ref int index)
		{
			// Encode header
			PacketHeader header = new PacketHeader(PacketTypeCode.QueryConnectionStatus);
			SerializeGenericPacket(buffer, ref index, header);

			// No payload

			// Encode footer
			SerializeGenericPacket(buffer, ref index, _footer);
		}

		/// <summary>
		/// Encode a Set New Config packet into the specified buffer
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="index"></param>
		/// <param name="config"></param>
		public static void EncodeSetNewConfig(byte[] buffer, ref int index, WearableDeviceConfig config)
		{
			// Encode header
			PacketHeader header = new PacketHeader(PacketTypeCode.SetNewConfig);
			SerializeGenericPacket(buffer, ref index, header);

			// Payload
			SerializeDeviceConfig(buffer, ref index, config);

			// Encode footer
			SerializeGenericPacket(buffer, ref index, _footer);
		}

		/// <summary>
		/// Encode a Query Config packet into the specified buffer
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="index"></param>
		public static void EncodeQueryConfig(byte[] buffer, ref int index)
		{
			// Encode header
			PacketHeader header = new PacketHeader(PacketTypeCode.QueryConfig);
			SerializeGenericPacket(buffer, ref index, header);

			// No payload

			// Encode footer
			SerializeGenericPacket(buffer, ref index, _footer);
		}

		#endregion

		#region Private

		/// <summary>
		/// Decode a sensor frame from a byte stream
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="index"></param>
		/// <returns></returns>
		private static SensorFrame DecodeSensorFrame(byte[] buffer, ref int index)
		{
			return DeserializeGenericPacket<SensorFrame>(buffer, ref index);
		}

		/// <summary>
		/// Decode a device list from a byte stream
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="index"></param>
		/// <returns></returns>
		private static Device[] DecodeDeviceList(byte[] buffer, ref int index)
		{
			DeviceListPacketHeader header = DeserializeGenericPacket<DeviceListPacketHeader>(buffer, ref index);

			Device[] devices = new Device[header.deviceCount];
			for (int i = 0; i < header.deviceCount; i++)
			{
				DeviceInfoPacket deviceInfo = DeserializeGenericPacket<DeviceInfoPacket>(buffer, ref index);
				Device device = DeserializeDeviceInfo(deviceInfo, ConnectionState.Disconnected);
				device.isConnected = false;
				devices[i] = device;
			}

			return devices;
		}

		/// <summary>
		/// Decode a Connection Status packet from a byte stream. If state is Failed, the device will be null.
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="index"></param>
		/// <param name="device"></param>
		/// <returns></returns>
		private static ConnectionState DecodeConnectionStatus(byte[] buffer, ref int index, out Device? device)
		{
			ConnectionStatusPacket status = DeserializeGenericPacket<ConnectionStatusPacket>(buffer, ref index);
			ConnectionState state = (ConnectionState)status.statusId;
			if (state == ConnectionState.Failed)
			{
				device = null;
				return state;
			}

			device = DeserializeDeviceInfo(status.device, state);
			return state;
		}

		#endregion
	}
}
