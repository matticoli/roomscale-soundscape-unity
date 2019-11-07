using System;

namespace Bose.Wearable
{
	public enum ProviderId
	{
		DebugProvider = 0,
		WearableDevice = 1,
		[Obsolete(WearableConstants.MobileProviderObsolete)] MobileProvider = 2,
		USBProvider = 3,
		WearableProxy = 4
	}
}
