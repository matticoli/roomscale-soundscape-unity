
namespace Bose.Wearable
{
	internal interface IWearableDeviceProviderPlatform
	{
		void WearableDeviceInitialize(bool simulated);

		void StartSearch(AppIntentProfile appIntentProfile, int rssiThreshold);
		void CancelDeviceConnectionInternal();
		void StopSearch();
		void OpenSession(string uid);
		void CloseSession();
		DeviceConnectionInfo GetDeviceConnectionInfoInternal();
		FirmwareUpdateInformation GetFirmwareUpdateInformationInternal();
		void SelectFirmwareUpdateOptionInternal(int index);
		int GetSessionStatus(ref string errorMessage);
		ConnectionStatus GetConnectionStatus(ref string errorMessage);
		void GetDeviceInfo(ref Device device);

		Device[] GetDiscoveredDevicesInternal();
		void GetLatestSensorUpdatesInternal();
		void GetLatestGestureUpdatesInternal();

		WearableDeviceConfig GetDeviceConfigurationInternal();
		void SetDeviceConfigurationInternal(WearableDeviceConfig config);

		bool IsAppIntentProfileValid(AppIntentProfile appIntentProfile);

		ConfigStatus GetSensorConfigStatusInternal();
		ConfigStatus GetGestureConfigStatusInternal();

		DynamicDeviceInfo GetDynamicDeviceInfoInternal();

		void SetAppFocusChangedInternal(bool hasChanged);
	}
}
