using UnityEngine;
using UnityEngine.UI;

namespace Bose.Wearable
{
	/// <summary>
	/// Shown when a device connection attempt is made
	/// </summary>
	internal sealed class ConnectingWearableConnectDisplay : WearableConnectDisplayBase
	{
		[Header("Connecting UI Refs")]
		[SerializeField]
		private Button _cancelConnectionButton;

		[Header("Sound Clips")]
		[SerializeField]
		private AudioClip _sfxConnecting;

		// Audio
		private AudioSource _srcConnecting;
		private const float TIME_BACKGROUND_FADE = 0.5f;

		protected override void Awake()
		{
			SetupAudio();

			_cancelConnectionButton.onClick.AddListener(OnCancelDeviceConnection);

			_panel.DeviceSearching += OnDeviceSearching;
			_panel.DeviceConnecting += OnDeviceConnecting;
			_panel.FirmwareCheckStarted += OnFirmwareCheckStarted;
			_panel.DeviceSecurePairingRequired += OnDeviceConnectEnded;
			_panel.DeviceConnectFailure += OnDeviceConnectEnded;
			_panel.DeviceConnectSuccess += OnDeviceConnectEnded;

			base.Awake();
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			_cancelConnectionButton.onClick.RemoveAllListeners();

			_panel.DeviceSearching -= OnDeviceSearching;
			_panel.DeviceConnecting -= OnDeviceConnecting;
			_panel.FirmwareCheckStarted -= OnFirmwareCheckStarted;
			_panel.DeviceSecurePairingRequired -= OnDeviceConnectEnded;
			_panel.DeviceConnectFailure -= OnDeviceConnectEnded;
			_panel.DeviceConnectSuccess -= OnDeviceConnectEnded;

			TeardownAudio();
		}

		private void OnCancelDeviceConnection()
		{
			Hide();

			_wearableControl.CancelDeviceConnection();
		}

		private void OnDeviceSearching()
		{
			Hide();
		}

		private void OnDeviceConnecting()
		{
			Show();
		}

		private void OnDeviceConnectEnded()
		{
			Hide();
		}

		private void OnFirmwareCheckStarted(
			bool isRequired,
			Device device,
			FirmwareUpdateInformation updateInformation)
		{
			Hide();
		}

		protected override void Show()
		{
			_messageText.text = WearableConstants.DeviceConnectionUnderwayMessage;
			_panel.DisableCloseButton();

			base.Show();

			StartConnectingLoop();
		}

		protected override void Hide()
		{
			base.Hide();

			StopConnectingLoop();
		}

		private void StartConnectingLoop()
		{
			if (_srcConnecting == null)
			{
				_srcConnecting = _audioControl.GetSource(true);
				_srcConnecting.clip = _sfxConnecting;
				_srcConnecting.loop = true;
			}

			_audioControl.FadeIn(_srcConnecting, TIME_BACKGROUND_FADE);
		}

		private void StopConnectingLoop()
		{
			if (_srcConnecting == null)
			{
				return;
			}

			_audioControl.FadeOut(_srcConnecting, TIME_BACKGROUND_FADE);
		}

		protected override void TeardownAudio()
		{
			if (_srcConnecting != null)
			{
				Destroy(_srcConnecting.gameObject);
			}
		}
	}
}
