using UnityEngine;
using UnityEngine.UI;

namespace Bose.Wearable
{
	/// <summary>
	/// <see cref="DebugUIPanel"/> is a debug UI screen that when connected to a WearableDevice allows for
	/// interactive manipulation of its state as well as visualization.
	/// </summary>
	internal sealed class DebugUIPanel : Singleton<DebugUIPanel>
	{
		[Header("UI Refs")]
		[SerializeField]
		private Canvas _canvas;

		[SerializeField]
		private CanvasGroup _mainPanelCanvasGroup;

		[SerializeField]
		private RectTransform _uiBlocker;

		[SerializeField]
		private DraggableUIButton _openMainPanelButton;

		[SerializeField]
		private Button _closeMainPanelButton;

		[SerializeField]
		private Image _closeMainPanelButtonIcon;

		[SerializeField]
		private Button _overrideButton;

		[SerializeField]
		private Image _overrideButtonImage;

		[SerializeField]
		private Text _overrideButtonText;

		[Header("Data Refs"), Space(5)]
		[SerializeField]
		private WearableUIColorPalette _colorPalette;

		[Header("UI Settings"), Space(5)]
		[SerializeField]
		private int _buttonSortOrder;

		[SerializeField]
		private int _panelSortOrder;

		[Header("Options"), Space(5)]
		[Tooltip(DebuggingConstants.ResetOverrideConfigOnHideTooltip)]
		[SerializeField]
		private bool _resetOverrideOnHide;

		private bool _isHidden;

		private WearableControl _wearableControl;

		protected override void Awake()
		{
			base.Awake();

			_openMainPanelButton.Clicked += OnOpenMainPanelButtonClick;

			_closeMainPanelButton.onClick.AddListener(OnCloseMainPanelButtonClick);
			_overrideButton.onClick.AddListener(OnOverrideButtonClick);
		}

		private void Start()
		{
			_wearableControl = WearableControl.Instance;

			Hide();
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			_openMainPanelButton.Clicked -= OnOpenMainPanelButtonClick;

			_closeMainPanelButton.onClick.RemoveAllListeners();
			_overrideButton.onClick.RemoveAllListeners();
		}

		private void Update()
		{
			var style = _colorPalette.GetCustomizedActiveStyle();
			var isOverriding = _wearableControl.IsOverridingDeviceConfig;
			var color = isOverriding
				? style.elementColor
				: _colorPalette.InactiveChildElementStyle.elementColor;

			_closeMainPanelButtonIcon.color = style.elementColor;

			_overrideButtonText.color = color;
			_overrideButtonText.text = isOverriding
				? DebuggingConstants.ResetOverrideButtonText
				: DebuggingConstants.OverrideConfigButtonText;

			_overrideButtonImage.color = color;
		}

		private void OnOpenMainPanelButtonClick()
		{
			Show();
		}

		private void OnCloseMainPanelButtonClick()
		{
			Hide();
		}

		private void OnOverrideButtonClick()
		{
			if (_wearableControl.IsOverridingDeviceConfig)
			{
				_wearableControl.UnregisterOverrideConfig();
			}
			else
			{
				// Get a copy of the current requirements resolved config and set that as our override config.
				var currentConfigCopy = _wearableControl.FinalWearableDeviceConfig.Clone();
				_wearableControl.RegisterOverrideConfig(currentConfigCopy);
			}
		}

		/// <summary>
		/// Shows the <see cref="DebugUIPanel"/>.
		/// </summary>
		public void Show()
		{
			if (!_isHidden)
			{
				return;
			}

			// Set the debug panel sort order to the absolute top.
			_canvas.sortingOrder = _panelSortOrder;

			_uiBlocker.gameObject.SetActive(true);
			_closeMainPanelButton.gameObject.SetActive(true);
			_openMainPanelButton.gameObject.SetActive(false);

			_mainPanelCanvasGroup.alpha = 1f;
			_mainPanelCanvasGroup.blocksRaycasts = true;
			_isHidden = false;
		}

		/// <summary>
		/// Hides the <see cref="DebugUIPanel"/>.
		/// </summary>
		public void Hide()
		{
			if (_isHidden)
			{
				return;
			}

			// If enabled, make sure to unregister the override config on exit.
			if (_resetOverrideOnHide)
			{
				_wearableControl.UnregisterOverrideConfig();
			}

			_canvas.sortingOrder = _buttonSortOrder;

			_uiBlocker.gameObject.SetActive(false);
			_closeMainPanelButton.gameObject.SetActive(false);
			_openMainPanelButton.gameObject.SetActive(true);

			_mainPanelCanvasGroup.alpha = 0f;
			_mainPanelCanvasGroup.blocksRaycasts = false;
			_isHidden = true;
		}
	}
}
