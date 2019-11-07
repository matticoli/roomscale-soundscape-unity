using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bose.Wearable.Examples
{
	internal sealed class ReturnToMainMenuOnDisconnect : MonoBehaviour
	{
		private WearableControl _wearableControl;

		private void Start()
		{
			_wearableControl = WearableControl.Instance;
			_wearableControl.DeviceDisconnected += OnDeviceDisconnected;
		}

		private void OnDestroy()
		{
			_wearableControl.DeviceDisconnected -= OnDeviceDisconnected;
		}

		private void OnDeviceDisconnected(Device device)
		{
			if (LoadingUIPanel.Exists)
			{
				LoadingUIPanel.Instance.LoadScene(WearableConstants.MainMenuScene, LoadSceneMode.Single);
			}
		}
	}
}
