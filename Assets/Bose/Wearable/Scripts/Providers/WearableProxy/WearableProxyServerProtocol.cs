using System;
using System.Runtime.InteropServices;

namespace Bose.Wearable.Proxy
{
	/// <summary>
	/// Implements server-size functionality of the WearableProxy Transport Protocol
	/// </summary>
	internal sealed class WearableProxyServerProtocol : WearableProxyProtocolBase
	{
		public event Action KeepAlive;
		public event Action PingQuery;
		public event Action PingResponse;
		public event Action<int> RSSIFilterValueChange;
		public event Action InitiateDeviceSearch;
		public event Action StopDeviceSearch;
		public event Action<string> ConnectToDevice;
		public event Action DisconnectFromDevice;
		public event Action QueryConnectionStatus;
		public event Action QueryConfigStatus;
		public event Action<WearableDeviceConfig> SetNewConfig;

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
				case PacketTypeCode.SetRssiFilter:
				{
					int value = DecodeRSSIFilterControlPacket(buffer, ref index);
					CheckFooter(buffer, ref index);

					if (RSSIFilterValueChange != null)
					{
						RSSIFilterValueChange.Invoke(value);
					}

					break;
				}
				case PacketTypeCode.InitiateDeviceSearch:
				{
					CheckFooter(buffer, ref index);

					if (InitiateDeviceSearch != null)
					{
						InitiateDeviceSearch.Invoke();
					}

					break;
				}
				case PacketTypeCode.StopDeviceSearch:
				{
					CheckFooter(buffer, ref index);

					if (StopDeviceSearch != null)
					{
						StopDeviceSearch.Invoke();
					}

					break;
				}
				case PacketTypeCode.ConnectToDevice:
				{
					string uid = DecodeDeviceConnectPacket(buffer, ref index);
					CheckFooter(buffer, ref index);

					if (ConnectToDevice != null)
					{
						ConnectToDevice.Invoke(uid);
					}

					break;
				}
				case PacketTypeCode.DisconnectFromDevice:
				{
					CheckFooter(buffer, ref index);

					if (DisconnectFromDevice != null)
					{
						DisconnectFromDevice.Invoke();
					}

					break;
				}
				case PacketTypeCode.QueryConnectionStatus:
				{
					CheckFooter(buffer, ref index);

					if (QueryConnectionStatus != null)
					{
						QueryConnectionStatus.Invoke();
					}

					break;
				}
				case PacketTypeCode.QueryConfig:
				{
					CheckFooter(buffer, ref index);

					if (QueryConfigStatus != null)
					{
						QueryConfigStatus.Invoke();
					}

					break;
				}
				case PacketTypeCode.SetNewConfig:
				{
					// N.B. This generates a tiny bit of garbage, but avoids race conditions by allocating for every packet
					WearableDeviceConfig config = DeserializeDeviceConfig(buffer, ref index);
					CheckFooter(buffer, ref index);

					if (SetNewConfig != null)
					{
						SetNewConfig.Invoke(config);
					}

					break;
				}
				case PacketTypeCode.ConfigStatus:
				case PacketTypeCode.SensorFrame:
				case PacketTypeCode.DeviceList:
				case PacketTypeCode.ConnectionStatus:
					// Known, but contextually-invalid packet
					throw new WearableProxyProtocolException(WearableConstants.ProxyProviderInvalidPacketError);
				default:
					// Unknown or corrupt packet
					throw new WearableProxyProtocolException(WearableConstants.ProxyProviderInvalidPacketError);
			}
		}

		#endregion

		#region Encoding

		/// <summary>
		/// Encode a <see cref="SensorFrame"/> into the buffer
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="index"></param>
		/// <param name="frame"></param>
		public static void EncodeSensorFrame(byte[] buffer, ref int index, SensorFrame frame)
		{
			// Encode header
			PacketHeader header = new PacketHeader(PacketTypeCode.SensorFrame);
			SerializeGenericPacket(buffer, ref index, header);

			// Encode payload
			SerializeGenericPacket(buffer, ref index, frame);

			// Encode footer
			SerializeGenericPacket(buffer, ref index, _footer);
		}

		/// <summary>
		/// Encode a Device List packet into the buffer
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="index"></param>
		/// <param name="devices"></param>
		public static void EncodeDeviceList(byte[] buffer, ref int index, Device[] devices)
		{
			// Encode header
			PacketHeader header = new PacketHeader(PacketTypeCode.DeviceList);
			SerializeGenericPacket(buffer, ref index, header);

			// Encode sub-header
			SerializeGenericPacket(buffer, ref index, new DeviceListPacketHeader { deviceCount = devices.Length });

			// Encode payload
			for (int i = 0; i < devices.Length; i++)
			{
				DeviceInfoPacket packet = SerializeDeviceInfo(devices[i]);
				SerializeGenericPacket(buffer, ref index, packet);
			}

			// Encode footer
			SerializeGenericPacket(buffer, ref index, _footer);
		}

		/// <summary>
		/// Encode a Connection Status packet into the buffer
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="index"></param>
		/// <param name="state"></param>
		/// <param name="device"></param>
		public static void EncodeConnectionStatus(byte[] buffer, ref int index, ConnectionState state, Device device)
		{
			// Encode header
			PacketHeader header = new PacketHeader(PacketTypeCode.ConnectionStatus);
			SerializeGenericPacket(buffer, ref index, header);

			// Encode payload
			ConnectionStatusPacket packet = new ConnectionStatusPacket
			{
				statusId = (int)state,
				device = SerializeDeviceInfo(device)
			};
			SerializeGenericPacket(buffer, ref index, packet);

			// Encode footer
			SerializeGenericPacket(buffer, ref index, _footer);
		}

		public static void EncodeConfigStatus(byte[] buffer, ref int index, WearableDeviceConfig config)
		{
			// Encode header
			PacketHeader header = new PacketHeader(PacketTypeCode.ConfigStatus);
			SerializeGenericPacket(buffer, ref index, header);

			// Encode payload
			SerializeDeviceConfig(buffer, ref index, config);

			// Encode footer
			SerializeGenericPacket(buffer, ref index, _footer);
		}

		#endregion

		#region Private

		/// <summary>
		/// Decode an RSSI Filter Control packet from the byte stream
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="index"></param>
		/// <returns></returns>
		private static int DecodeRSSIFilterControlPacket(byte[] buffer, ref int index)
		{
			RSSIFilterControlPacket value = DeserializeGenericPacket<RSSIFilterControlPacket>(buffer, ref index);
			return value.threshold;
		}

		/// <summary>
		/// Decode a Device Connect packet from the byte stream
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="index"></param>
		/// <returns></returns>
		private static string DecodeDeviceConnectPacket(byte[] buffer, ref int index)
		{
			DeviceConnectPacket device = DeserializeGenericPacket<DeviceConnectPacket>(buffer, ref index);
			unsafe
			{
				return DeserializeFixedString((IntPtr)device.uid.value, sizeof(DeviceUID));
			}
		}

		#endregion
	}
}
