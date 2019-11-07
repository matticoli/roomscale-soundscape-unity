using System;
using UnityEngine;
using UnityEngine.UI;

namespace Bose.Wearable
{
	internal sealed class FirmwareWearableConnectDisplay : WearableConnectDisplayBase
	{
		[Header("UI Refs"), Space(5)]
		[SerializeField]
		private Image _appGroupIcon;

		[SerializeField]
		private Text _headerText;

		[SerializeField]
		private Text _scrollText;

		[SerializeField]
		private Button _updateButton;

		[SerializeField]
		private Button _continueButton;

		[SerializeField]
		private Text _continueButtonText;

		[Header("Data"), Space(5)]
		[SerializeField]
		private Sprite _boseConnectIcon;

		[SerializeField]
		private Sprite _boseMusicIcon;

		private bool _isVisible;
		private bool _clickedUpdateButton;
		private FirmwareUpdateInformation _updateInformation;

		private const string UpdateAvailableHeaderText = "An update is available.";
		private const string UpdateRequiredHeaderText = "An update is required.";

		private const string UpdateAvailableBodyText = "To get the most out of your Bose AR-enabled product, " +
		                                               "update in the Bose {0} app.";
		private const string UpdateRequiredBodyText =
			"Before you can experience all the advanced capabilities featured in Bose AR, you'll need to " +
			"update your product in the Bose {0} app.";

		private const string UpdateAvailableSecondaryButtonText = "continue without updating";
		private const string UpdateRequiredSecondaryButtonText = "continue without Bose AR";

		protected override void Awake()
		{
			base.Awake();

			_panel.DeviceSearching += OnDeviceSearching;
			_panel.FirmwareCheckStarted += OnFirmwareCheckStarted;
			_panel.DeviceSecurePairingRequired += OnDeviceSecurePairingRequired;

			_updateButton.onClick.AddListener(OnPrimaryButtonClick);
			_continueButton.onClick.AddListener(OnContinueButtonClick);
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			_panel.DeviceSearching -= OnDeviceSearching;
			_panel.FirmwareCheckStarted -= OnFirmwareCheckStarted;
			_panel.DeviceSecurePairingRequired -= OnDeviceSecurePairingRequired;

			_updateButton.onClick.RemoveAllListeners();
			_continueButton.onClick.RemoveAllListeners();
		}

		protected override void Show()
		{
			base.Show();

			_clickedUpdateButton = false;
			_isVisible = true;
		}

		protected override void Hide()
		{
			base.Hide();

			_isVisible = false;
		}

		private void OnDeviceSearching()
		{
			Hide();
		}

		private void OnFirmwareCheckStarted(
			bool isRequired,
			Device device,
			FirmwareUpdateInformation updateInformation)
		{
			_messageText.text = string.Empty;
			_updateInformation = updateInformation;

			if (isRequired)
			{
				_headerText.text = UpdateRequiredHeaderText;
				_scrollText.text = string.Format(UpdateRequiredBodyText, _updateInformation.icon);
				_continueButtonText.text = UpdateRequiredSecondaryButtonText;
			}
			else
			{
				_headerText.text = UpdateAvailableHeaderText;
				_scrollText.text = string.Format(UpdateAvailableBodyText, _updateInformation.icon);
				_continueButtonText.text = UpdateAvailableSecondaryButtonText;
			}

			switch (updateInformation.icon)
			{
				case BoseUpdateIcon.Connect:
					_appGroupIcon.sprite = _boseConnectIcon;
					break;
				case BoseUpdateIcon.Music:
					_appGroupIcon.sprite = _boseMusicIcon;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			Show();
		}

		private void OnDeviceSecurePairingRequired()
		{
			Hide();
		}

		private void OnPrimaryButtonClick()
		{
			_clickedUpdateButton = true;

			_wearableControl.ActiveProvider.SelectFirmwareUpdateOption(GetIndex(AlertStyle.Affirmative));
			_wearableControl.DisconnectFromDevice();
		}

		private void OnContinueButtonClick()
		{
			_wearableControl.ActiveProvider.SelectFirmwareUpdateOption(GetIndex(AlertStyle.Negative));

			Hide();
		}

		private void OnApplicationFocus(bool hasFocus)
		{
			// If we've gained back app focus and this panel is the last thing the user saw before they left the app
			if (hasFocus && _isVisible && _clickedUpdateButton)
			{
				_panel.CheckForPermissionsAndTrySearch();
				Hide();
			}
		}

		private int GetIndex(AlertStyle style)
		{
			var index = 0;
			for (var i = 0; i < _updateInformation.options.Length; i++)
			{
				if (_updateInformation.options[i].style == style)
				{
					index = i;
				}
			}

			return index;
		}
	}
}
