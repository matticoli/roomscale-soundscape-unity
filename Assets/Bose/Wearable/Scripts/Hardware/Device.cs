using System;
using System.Runtime.InteropServices;

namespace Bose.Wearable
{
	/// <summary>
	/// Represents an Wearable device.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct Device : IEquatable<Device>
	{
		/// <summary>
		/// The Unique Identifier for this device. On Android this will be the device's address, since
		/// we cannot get the uuid of the device in some circumstances.
		/// </summary>
		public string uid;

		/// <summary>
		/// The name of this device.
		/// </summary>
		public string name;

		/// <summary>
		/// The firmware version of the device.
		/// </summary>
		public string firmwareVersion;

		/// <summary>
		/// The connection state of this device.
		/// </summary>
		public bool isConnected;

		/// <summary>
		/// A bitfield that contains status of this device.
		/// </summary>
		public DeviceStatus deviceStatus;

		/// <summary>
		/// The RSSI of the device at the time it was first located.
		/// NB: this value is not updated after searching is stopped.
		/// </summary>
		public int rssi;

		/// <summary>
		/// A bitfield listing the available sensor on a device.
		/// </summary>
		public SensorFlags availableSensors;

		/// <summary>
		/// A bitfield listing the available gestures on a device.
		/// </summary>
		public GestureFlags availableGestures;

		/// <summary>
		/// The ProductId of the device.
		/// </summary>
		internal ProductId productId;

		/// <summary>
		/// The VariantId of the device.
		/// </summary>
		internal byte variantId;

		/// <summary>
		/// Indicates how often samples are transmitted from the product over the air, in milliseconds. A special value
		/// of zero indicates that the samples are sent as soon as they are available.
		/// </summary>
		internal int transmissionPeriod;

		/// <summary>
		/// Indicates the maximum payload size in bytes of all combined active sensors that can be sent every transmission
		/// period. The special value of zero indicates that there are no limitations on the sensor payload size.
		/// </summary>
		internal int maximumPayloadPerTransmissionPeriod;

		/// <summary>
		/// Indicates the maximum number of sensors that can be active simultaneously.
		/// </summary>
		internal int maximumActiveSensors;

		/// <summary>
		/// Returns the <see cref="ProductType"/> of the device.
		/// </summary>
		/// <returns></returns>
		public ProductType GetProductType()
		{
			return WearableTools.GetProductType(productId);
		}

		/// <summary>
		/// Returns the Product <see cref="VariantType"/> of the device.
		/// </summary>
		/// <returns></returns>
		public VariantType GetVariantType()
		{
			return WearableTools.GetVariantType(GetProductType(), variantId);
		}

		/// <summary>
		/// Returns true if a device supports using a sensor with <see cref="SensorId"/>
		/// <paramref name="sensorId"/>, otherwise false if it does not.
		/// </summary>
		/// <param name="sensorId"></param>
		/// <returns></returns>
		public bool IsSensorAvailable(SensorId sensorId)
		{
			var sensorFlag = WearableTools.GetSensorFlag(sensorId);
			return (availableSensors & sensorFlag) == sensorFlag;
		}

		/// <summary>
		/// Returns true if a device supports using a gesture with <see cref="GestureId"/>
		/// <paramref name="gestureId"/>, otherwise false if it does not.
		/// </summary>
		/// <param name="gestureId"></param>
		/// <returns></returns>
		public bool IsGestureAvailable(GestureId gestureId)
		{
			var gestureFlag = WearableTools.GetGestureFlag(gestureId);
			return (availableGestures & gestureFlag) == gestureFlag;
		}

		/// <summary>
		/// Returns the device's <see cref="SignalStrength"/> based on its <seealso cref="rssi"/>.
		/// </summary>
		/// <returns></returns>
		public SignalStrength GetSignalStrength()
		{
			var signalStrength = SignalStrength.Weak;
			if (rssi > -35)
			{
				signalStrength = SignalStrength.Full;
			}
			else if (rssi > -45)
			{
				signalStrength = SignalStrength.Strong;
			}
			else if (rssi > -55)
			{
				signalStrength = SignalStrength.Moderate;
			}

			return signalStrength;
		}

		#region IEquatable<Device>

		public bool Equals(Device other)
		{
			return uid == other.uid;
		}

		public static bool operator ==(Device lhs, Device rhs)
		{
			return lhs.Equals(rhs);
		}

		public static bool operator !=(Device lhs, Device rhs)
		{
			return !(lhs == rhs);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			return obj is Device && Equals((Device) obj);
		}

		public override int GetHashCode()
		{
			return (uid != null ? uid.GetHashCode() : 0);
		}

		#endregion
	}
}
