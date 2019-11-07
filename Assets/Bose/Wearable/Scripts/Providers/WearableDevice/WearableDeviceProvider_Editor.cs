#if UNITY_EDITOR

using UnityEngine;

namespace Bose.Wearable
{
	internal sealed partial class WearableDeviceProvider : IWearableDeviceProviderPlatform
	{
		#region IWearableDeviceProviderPlatform implementation

		public void WearableDeviceInitialize(bool simulated)
		{
			Debug.LogError(WearableConstants.UnsupportedPlatformError);
		}

		public void StartSearch(AppIntentProfile appIntentProfile, int rssiThreshold) { }
		public void CancelDeviceConnectionInternal() { }
		public void StopSearch() { }
		public void OpenSession(string uid) { }
		public void CloseSession() { }

		public DeviceConnectionInfo GetDeviceConnectionInfoInternal()
		{
			return new DeviceConnectionInfo();
		}

		public FirmwareUpdateInformation GetFirmwareUpdateInformationInternal()
		{
			return WearableConstants.DefaultFirmwareUpdateInformation;
		}

		public void SelectFirmwareUpdateOptionInternal(int index) { }

		public int GetSessionStatus(ref string errorMessage)
		{
			return (int)SessionStatus.Closed;
		}

		public ConnectionStatus GetConnectionStatus(ref string errorMessage)
		{
			return ConnectionStatus.Connected;
		}

		public void GetDeviceInfo(ref Device device) { }

		public Device[] GetDiscoveredDevicesInternal()
		{
			return WearableConstants.EmptyDeviceList;
		}

		public void GetLatestSensorUpdatesInternal() { }
		public void GetLatestGestureUpdatesInternal() { }

		public WearableDeviceConfig GetDeviceConfigurationInternal()
		{
			return WearableConstants.DisabledDeviceConfig;
		}

		public void SetDeviceConfigurationInternal(WearableDeviceConfig config) { }

		public ConfigStatus GetSensorConfigStatusInternal()
		{
			return ConfigStatus.Idle;
		}

		public ConfigStatus GetGestureConfigStatusInternal()
		{
			return ConfigStatus.Idle;
		}

		public bool IsAppIntentProfileValid(AppIntentProfile appIntentProfile)
		{
			return true;
		}

		public DynamicDeviceInfo GetDynamicDeviceInfoInternal()
		{
			return WearableConstants.EmptyDynamicDeviceInfo;
		}

		public void SetAppFocusChangedInternal(bool hasFocus)
		{
			// no-op.
		}

		#endregion
	}
}

#endif
