using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bose.Wearable.Examples
{
	public class MainMenuUIPanel : MonoBehaviour
	{

        public Scene ARCoreScene;

		[SerializeField]
		private CanvasGroup _canvasGroup;

		[SerializeField]
		private GameObject _buttonParentGameObject;

		[SerializeField]
		private GameObject _connectParentGameObject;

		[SerializeField]
		private Button _showConnectUIButton;

		[SerializeField]
		private Button _basicDemoButton;

		[SerializeField]
		private Button _gestureDemoButton;

		[SerializeField]
		private Button _advancedDemoButton;

		[SerializeField]
		private Button _debugDemoButton;

		private WearableControl _wearableControl;

		private void Awake()
		{
			_showConnectUIButton.onClick.AddListener(OnShowConnectUIButtonClicked);
			_basicDemoButton.onClick.AddListener(OnBasicDemoButtonClicked);
			_advancedDemoButton.onClick.AddListener(OnAdvancedDemoButtonClicked);
			_gestureDemoButton.onClick.AddListener(OnGestureDemoButtonClicked);
			_debugDemoButton.onClick.AddListener(OnDebugDemoButtonClicked);

			ToggleInteractivity(true);
		}

		private void Start()
		{
			_wearableControl = WearableControl.Instance;
			_wearableControl.DeviceConnected += OnDeviceConnected;
			_wearableControl.DeviceDisconnected += OnDeviceDisconnected;

			var deviceIsConnected = _wearableControl.ConnectedDevice.HasValue;
			_buttonParentGameObject.gameObject.SetActive(deviceIsConnected);
			_connectParentGameObject.gameObject.SetActive(!deviceIsConnected);
		}

		private void OnDestroy()
		{
			_wearableControl.DeviceConnected -= OnDeviceConnected;
			_wearableControl.DeviceDisconnected -= OnDeviceDisconnected;

			_showConnectUIButton.onClick.RemoveAllListeners();
			_basicDemoButton.onClick.RemoveAllListeners();
			_advancedDemoButton.onClick.RemoveAllListeners();
			_gestureDemoButton.onClick.RemoveAllListeners();
			_debugDemoButton.onClick.RemoveAllListeners();
		}

		private void OnShowConnectUIButtonClicked()
		{
			WearableConnectUIPanel.Instance.Show();
		}

		private void OnAdvancedDemoButtonClicked()
		{
			LoadingUIPanel.Instance.LoadScene(WearableConstants.AdvancedDemoScene, LoadSceneMode.Single);

			ToggleInteractivity(false);
		}

		private void OnBasicDemoButtonClicked()
		{
			LoadingUIPanel.Instance.LoadScene("HelloAR", LoadSceneMode.Single);

			ToggleInteractivity(false);
		}

		private void OnGestureDemoButtonClicked()
		{
			LoadingUIPanel.Instance.LoadScene(WearableConstants.GestureDemoScene, LoadSceneMode.Single);

			ToggleInteractivity(false);
		}

		private void OnDebugDemoButtonClicked()
		{
			LoadingUIPanel.Instance.LoadScene(WearableConstants.DebugDemoScene, LoadSceneMode.Single);

			ToggleInteractivity(false);
		}

		private void OnDeviceConnected(Device device)
		{
			_buttonParentGameObject.gameObject.SetActive(true);
			_connectParentGameObject.gameObject.SetActive(false);
		}

		private void OnDeviceDisconnected(Device device)
		{
			_buttonParentGameObject.gameObject.SetActive(false);
			_connectParentGameObject.gameObject.SetActive(true);
		}

		private void ToggleInteractivity(bool isOn)
		{
			_canvasGroup.interactable = isOn;
		}
	}
}
