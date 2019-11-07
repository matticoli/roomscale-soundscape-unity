using System;

namespace Bose.Wearable
{
	[Serializable]
	public enum ConnectionStatus
	{
		/// <summary>
		/// No device is connected, and we are not searching for devices.
		/// </summary>
		Disconnected = 0,

		/// <summary>
		/// Trying to connect to the last successfully connected device, if discovered in a search,
		/// </summary>
		AutoReconnect = 9,

		/// <summary>
		/// Searching for available devices.
		/// </summary>
		Searching = 1,

		/// <summary>
		/// Attempting to connect to a device.
		/// </summary>
		Connecting = 2,

		/// <summary>
		/// Waiting for secure pairing to complete (which may or may not involve user intervention).
		/// </summary>
		SecurePairingRequired = 3,

		/// <summary>
		/// A firmware update is available; waiting for user to resolve.
		/// </summary>
		FirmwareUpdateAvailable = 4,

		/// <summary>
		/// A firmware update is required to connect; waiting for user to resolve.
		/// </summary>
		FirmwareUpdateRequired = 5,

		/// <summary>
		/// The device is successfully connected.
		/// </summary>
		Connected = 6,

		/// <summary>
		/// The device failed to connect.
		/// </summary>
		Failed = 7,

		/// <summary>
		/// The device connection was cancelled before it could complete.
		/// </summary>
		Cancelled = 8
	}
}
