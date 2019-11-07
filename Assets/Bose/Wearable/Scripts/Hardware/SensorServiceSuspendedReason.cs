using System;

namespace Bose.Wearable
{
	/// <summary>
	/// The reason that the sensor service was suspended. Formed from the upper bits of the Device Status field,
	/// packed in backwards.
	/// </summary>
	[Serializable]
	public enum SensorServiceSuspendedReason
	{
		MusicSharingActive = 1 << 12,
		MultipointConnectionActive = 1 << 13,
		VoiceAssistantInUse = 1 << 14,
		UnknownReason = 1 << 15
	}
}
