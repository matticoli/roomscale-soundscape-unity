using System.Collections;
using UnityEngine;

namespace Bose.Wearable
{
	/// <summary>
	/// Shown when a device connection attempt has succeeded
	/// </summary>
	internal sealed class SuccessfulWearableConnectDisplay : WearableConnectDisplayBase
	{
		[Header("Sound Clips")]
		[SerializeField]
		private AudioClip _sfxSuccess;

		[Tooltip("The amount of time to show the success state before hiding this display.")]
		[Range(0, 3f)]
		[SerializeField]
		private float _showSuccessDelay;

		private WaitForSecondsRealtimeCacheable _wait;

		protected override void Awake()
		{
			SetupAudio();

			_wait = new WaitForSecondsRealtimeCacheable(_showSuccessDelay);

			base.Awake();
		}

		private void OnEnable()
		{
			_panel.DeviceConnectSuccess += OnDeviceConnectionSuccess;
		}

		private void OnDisable()
		{
			_panel.DeviceConnectSuccess -= OnDeviceConnectionSuccess;
		}

		private void OnDeviceConnectionSuccess()
		{
			StartCoroutine(ShowSuccess());
		}

		private IEnumerator ShowSuccess()
		{
			PlaySuccessSting();

			Show();

			yield return _wait.Restart();

			Hide();
		}

		protected override void Show()
		{
			_messageText.text = WearableConstants.DeviceConnectSuccessMessage;
			_panel.DisableCloseButton();

			base.Show();
		}

		protected override void Hide()
		{
			if (WearableControl.Instance.ConnectedDevice != null && _panel.IsVisible)
			{
				_panel.Hide();
			}

			base.Hide();
		}

		private void PlaySuccessSting()
		{
			_audioControl.PlayOneShot(_sfxSuccess);
		}
	}
}
