using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine.Assertions;

namespace Bose.Wearable.Proxy
{
	/// <summary>
	/// Implements packet definitions for the WearableProxy Transport Protocol. Client and Server protocols inherit from this.
	/// </summary>
	internal abstract class WearableProxyProtocolBase
	{
		public enum ConnectionState
		{
			Disconnected,
			Connecting,
			Connected,
			Failed
		}

		#region Public

		public const int SuggestedServerToClientBufferSize = 8192; // Big enough to hold a list of 60 devices
		public const int SuggestedClientToServerBufferSize = 256;

		public abstract void ProcessPacket(byte[] buffer, ref int index);

		/// <summary>
		/// Encode a Keep Alive packet
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="index"></param>
		public static void EncodeKeepAlive(byte[] buffer, ref int index)
		{
			PacketHeader header = new PacketHeader(PacketTypeCode.KeepAlive);
			SerializeGenericPacket(buffer, ref index, header);
			// No payload
			SerializeGenericPacket(buffer, ref index, _footer);
		}

		/// <summary>
		/// Encode a Ping Query packet
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="index"></param>
		/// <returns></returns>
		public static void EncodePingQuery(byte[] buffer, ref int index)
		{
			PacketHeader header = new PacketHeader(PacketTypeCode.PingQuery);
			SerializeGenericPacket(buffer, ref index, header);
			// No payload
			SerializeGenericPacket(buffer, ref index, _footer);
		}

		/// <summary>
		/// Encode a Ping Response packet
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="index"></param>
		/// <returns></returns>
		public static void EncodePingResponse(byte[] buffer, ref int index)
		{
			PacketHeader header = new PacketHeader(PacketTypeCode.PingResponse);
			SerializeGenericPacket(buffer, ref index, header);
			// No payload
			SerializeGenericPacket(buffer, ref index, _footer);
		}

		#endregion

		#region Private
		private const byte Version = 0x07;
		private const int Terminator = 0x424F5345; // "BOSE"

		private static readonly Encoding _stringEncoding;

		protected enum PacketTypeCode : byte
		{
			// Server <-> Client
			KeepAlive = 0x00,
			PingQuery = 0x20,
			PingResponse = 0x21,

			// Server -> Client
			SensorFrame = 0x01,
			DeviceList = 0x02,
			// 0x03 - 0x05 RESERVED
			ConnectionStatus = 0x06,
			// 0x07 - 0x10 RESERVED
			ConfigStatus = 0x11,

			// Client -> Server
			SetRssiFilter = 0x71,
			InitiateDeviceSearch = 0x72,
			StopDeviceSearch = 0x73,
			ConnectToDevice = 0x74,
			DisconnectFromDevice = 0x75,
			QueryConnectionStatus = 0x76,
			// 0x77 - 0x7D RESERVED
			SetNewConfig = 0x7E,
			QueryConfig = 0x7F
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		protected struct PacketHeader
		{
			public byte typeCode;
			public byte version;

			public PacketHeader(PacketTypeCode type)
			{
				typeCode = (byte)type;
				version = Version;
			}
		}

		protected static readonly int _headerSize;

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		protected struct PacketFooter
		{
			public int terminator;
		}

		protected static readonly int _footerSize;

		protected static readonly PacketFooter _footer;

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		protected unsafe struct DeviceName
		{
			public const int Length = 32;
			public fixed byte value[Length];
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		protected unsafe struct DeviceUID
		{
			public const int Length = 36;
			public fixed byte value[Length];
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		protected unsafe struct DeviceFirmwareVersion
		{
			public const int Length = 16;
			public fixed byte value[Length];
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		protected struct DeviceInfoPacket
		{
			public DeviceUID uid;
			public DeviceName name;
			public DeviceFirmwareVersion firmwareVersion;
			public int rssi;
			public ProductId productId;
			public byte variantId;
			public SensorFlags availableSensors;
			public GestureFlags availableGestures;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		protected struct DeviceListPacketHeader
		{
			public int deviceCount;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		protected struct ConnectionStatusPacket
		{
			public int statusId;
			public DeviceInfoPacket device;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		protected struct RSSIFilterControlPacket
		{
			public int threshold;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		protected struct DeviceConnectPacket
		{
			public DeviceUID uid;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		protected struct DeviceConfigPacketHeader
		{
			public int sensorCount;
			public int gestureCount;
			public SensorUpdateInterval updateInterval;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		protected struct SensorConfigPacket
		{
			public SensorId sensorId;
			public byte enabled;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		protected struct GestureConfigPacket
		{
			public GestureId gestureId;
			public byte enabled;
		}

		static WearableProxyProtocolBase()
		{
			_footer = new PacketFooter{ terminator = Terminator };
			_headerSize = Marshal.SizeOf(typeof(PacketHeader));
			_footerSize = Marshal.SizeOf(typeof(PacketFooter));
			_stringEncoding = Encoding.ASCII;
		}

		/// <summary>
		/// Decodes a header and returns the associated packet type code.
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="index"></param>
		/// <returns></returns>
		/// <exception cref="WearableProxyProtocolException">Thrown if the packet is invalid or the version number mismatches.</exception>
		protected static PacketTypeCode DecodePacketType(byte[] buffer, ref int index)
		{
			PacketHeader header = DeserializeGenericPacket<PacketHeader>(buffer, ref index);
			if (header.version != Version)
			{
				// This is an unreadable packet, either invalid version or corrupt
				throw new WearableProxyProtocolException(
					string.Format(
						WearableConstants.ProxyProviderInvalidVersionError,
						header.version,
						Version));
			}

			return (PacketTypeCode)header.typeCode;
		}

		/// <summary>
		/// Checks that the buffer provided has a valid footer at the current index.
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="index"></param>
		/// <exception cref="WearableProxyProtocolException">Thrown if the footer was invalid.</exception>
		protected static void CheckFooter(byte[] buffer, ref int index)
		{
			PacketFooter footer = DeserializeGenericPacket<PacketFooter>(buffer, ref index);
			if (footer.terminator != Terminator)
			{
				// This is a corrupt or de-synchronized packet
				throw new WearableProxyProtocolException(WearableConstants.ProxyProviderInvalidPacketError);
			}
		}

		/// <summary>
		/// Recreate a <see cref="Device"/> from a <see cref="DeviceInfoPacket"/> and <see cref="ConnectionState"/>
		/// </summary>
		/// <param name="deviceInfoPacket"></param>
		/// <param name="connectionState"></param>
		/// <returns></returns>
		protected static Device DeserializeDeviceInfo(DeviceInfoPacket deviceInfoPacket, ConnectionState connectionState)
		{
			Device device;

			unsafe
			{
				device = new Device
				{
					name = DeserializeFixedString((IntPtr)deviceInfoPacket.name.value, DeviceName.Length),
					uid = DeserializeFixedString((IntPtr)deviceInfoPacket.uid.value, DeviceUID.Length),
					firmwareVersion = DeserializeFixedString((IntPtr)deviceInfoPacket.firmwareVersion.value, DeviceFirmwareVersion.Length),
					productId = deviceInfoPacket.productId,
					variantId = deviceInfoPacket.variantId,
					rssi = deviceInfoPacket.rssi,
					isConnected = (connectionState == ConnectionState.Connected),
					availableSensors = deviceInfoPacket.availableSensors,
					availableGestures = deviceInfoPacket.availableGestures
				};
			}

			return device;
		}

		/// <summary>
		/// Create a <see cref="DeviceInfoPacket"/> from a <see cref="Device"/>
		/// </summary>
		/// <param name="device"></param>
		/// <returns></returns>
		protected static DeviceInfoPacket SerializeDeviceInfo(Device device)
		{
			DeviceInfoPacket packet = new DeviceInfoPacket();

			unsafe
			{
				SerializeFixedString(device.name, (IntPtr)packet.name.value, DeviceName.Length);
				SerializeFixedString(device.uid, (IntPtr)packet.uid.value, DeviceUID.Length);
				SerializeFixedString(device.firmwareVersion, (IntPtr)packet.firmwareVersion.value, DeviceFirmwareVersion.Length);
				packet.productId = device.productId;
				packet.variantId = device.variantId;
				packet.rssi = device.rssi;
				packet.availableSensors = device.availableSensors;
				packet.availableGestures = device.availableGestures;
			}

			return packet;
		}

		/// <summary>
		/// Serialize a <see cref="WearableDeviceConfig"/> into a byte buffer.
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="index"></param>
		/// <param name="config"></param>
		protected static void SerializeDeviceConfig(byte[] buffer, ref int index, WearableDeviceConfig config)
		{
			// Encode sub-header
			SerializeGenericPacket(buffer, ref index, new DeviceConfigPacketHeader
			{
				sensorCount = WearableConstants.SensorIds.Length,
				gestureCount = WearableConstants.GestureIds.Length - 1,  // Subtract 1 for "None"
				updateInterval = config.updateInterval
			});

			// Encode payload
			for (int i = 0; i < WearableConstants.SensorIds.Length; i++)
			{
				SensorId sensorId = WearableConstants.SensorIds[i];

				var sensorConfigPacket = new SensorConfigPacket
				{
					sensorId = sensorId,
					enabled = (byte) (config.GetSensorConfig(sensorId).isEnabled ? 1 : 0)
				};

				SerializeGenericPacket(buffer, ref index, sensorConfigPacket);
			}

			for (int i = 0; i < WearableConstants.GestureIds.Length; i++)
			{
				GestureId gestureId = WearableConstants.GestureIds[i];

				if (gestureId == GestureId.None)
				{
					continue;
				}

				var gestureConfigPacket = new GestureConfigPacket
				{
					gestureId = gestureId,
					enabled = (byte) (config.GetGestureConfig(gestureId).isEnabled ? 1 : 0)
				};

				SerializeGenericPacket(buffer, ref index, gestureConfigPacket);
			}
		}

		/// <summary>
		/// Deserializes a byte stream into a <see cref="WearableDeviceConfig"/>. Allocates the return object if one
		/// is not provided.
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="index"></param>
		/// <param name="config">The config to fill into, or null to allocate a new one.</param>
		/// <returns></returns>
		protected static WearableDeviceConfig DeserializeDeviceConfig(byte[] buffer, ref int index, WearableDeviceConfig config=null)
		{
			if (config == null)
			{
				config = new WearableDeviceConfig();
			}

			// Decode header
			DeviceConfigPacketHeader header = DeserializeGenericPacket<DeviceConfigPacketHeader>(buffer, ref index);
			config.updateInterval = header.updateInterval;

			// Decode sensor configs
			for (int i = 0; i < header.sensorCount; i++)
			{
				SensorConfigPacket sensorConfig = DeserializeGenericPacket<SensorConfigPacket>(buffer, ref index);
				config.GetSensorConfig(sensorConfig.sensorId).isEnabled = sensorConfig.enabled != 0;
			}

			// Decode gesture configs
			for (int i = 0; i < header.gestureCount; i++)
			{
				GestureConfigPacket gestureConfig = DeserializeGenericPacket<GestureConfigPacket>(buffer, ref index);
				config.GetGestureConfig(gestureConfig.gestureId).isEnabled = gestureConfig.enabled != 0;
			}

			return config;
		}


		/// <summary>
		/// Encode a packet of type T into a byte stream.
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="index"></param>
		/// <param name="packet"></param>
		/// <typeparam name="T"></typeparam>
		/// <exception cref="IndexOutOfRangeException">Thrown if the buffer was not big enough to fit the whole packet.</exception>
		protected static void SerializeGenericPacket<T>(byte[] buffer, ref int index, T packet)
		{
			int size = Marshal.SizeOf(typeof(T));

			if (buffer.Length < index + size)
			{
				// The buffer isn't big enough to hold the full packet
				throw new IndexOutOfRangeException();
			}

			unsafe
			{
				fixed (byte* bufferPtr = &buffer[index])
				{
					Marshal.StructureToPtr(packet, (IntPtr)bufferPtr, false);
					index += size;
				}
			}
		}

		/// <summary>
		/// Decode a packet of type T from a byte stream.
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="index"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		/// <exception cref="IndexOutOfRangeException">Thrown if the stream did not have enough data to decode a full packet</exception>
		protected static T DeserializeGenericPacket<T>(byte[] buffer, ref int index)
		{
			int size = Marshal.SizeOf(typeof(T));
			if (buffer.Length < index + size)
			{
				// The buffer isn't big enough to contain a full packet
				throw new IndexOutOfRangeException();
			}

			unsafe
			{
				fixed (byte* bufferPtr = &buffer[index])
				{
					T packet = (T) Marshal.PtrToStructure((IntPtr) bufferPtr, typeof(T));
					index += size;
					return packet;
				}
			}
		}

		/// <summary>
		/// Return a string as encoded from a fixed buffer. Works with or without a null terminator.
		/// </summary>
		/// <param name="fixedString"></param>
		/// <param name="length"></param>
		/// <returns></returns>
		protected static string DeserializeFixedString(IntPtr fixedString, int length)
		{
			if (length == 0)
			{
				return string.Empty;
			}

			// Marshal to a managed buffer
			byte[] buffer = new byte[length];
			Marshal.Copy(fixedString, buffer, 0, length);

			// Strip off null-terminators
			while (length > 0 && buffer[length - 1] == 0)
			{
				length--;
			}

			// Decode back to string
			return _stringEncoding.GetString(buffer, 0, length);
		}

		/// <summary>
		/// Serialized a string into a fixed buffer. The buffer must be allocated to the proper size already.
		/// Truncates or null-pads input as necessary.
		/// </summary>
		/// <param name="input"></param>
		/// <param name="fixedString"></param>
		/// <param name="length"></param>
		protected static void SerializeFixedString(string input, IntPtr fixedString, int length)
		{
			if (length == 0)
			{
				return;
			}

			if (input == null)
			{
				input = string.Empty;
			}

			// Encode as bytes
			byte[] buffer = new byte[length];
			_stringEncoding.GetBytes(input, 0, input.Length > length ? length : input.Length, buffer, 0);

			// Marshal to unmanaged pointer
			Marshal.Copy(buffer, 0, fixedString, length);
		}

		#endregion

		#region Testing

		/// <summary>
		/// Asserts that the packet structs match the sizes specified by the transport protocol.
		/// </summary>
		public static void AssertCorrectStructSizes()
		{
			unsafe
			{
				Assert.AreEqual(2, sizeof(PacketHeader));
				Assert.AreEqual(4, sizeof(PacketFooter));
				Assert.AreEqual(84, sizeof(SensorFrame));
				Assert.AreEqual(99, sizeof(DeviceInfoPacket));
				Assert.AreEqual(4, sizeof(DeviceListPacketHeader));
				Assert.AreEqual(103, sizeof(ConnectionStatusPacket));
				Assert.AreEqual(12, sizeof(DeviceConfigPacketHeader));
				Assert.AreEqual(4, sizeof(RSSIFilterControlPacket));
				Assert.AreEqual(36, sizeof(DeviceConnectPacket));
			}
		}

		#endregion
	}
}
