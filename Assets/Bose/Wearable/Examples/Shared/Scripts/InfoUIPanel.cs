using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bose.Wearable.Examples
{
	internal sealed class InfoUIPanel : MonoBehaviour
	{
		[Header("UX Refs")]
		[SerializeField]
		private Button _backButton;

		[SerializeField]
		private Button _openInfoButton;

		[SerializeField]
		private Button _closeInfoButton;

		[SerializeField]
		private Button _backgroundButton;

		[SerializeField]
		private Canvas _canvas;

		[SerializeField]
		private CanvasGroup _canvasGroup;

		[SerializeField]
		private RectTransform _footerRectTransform;

		[Header("UI Settings"), Space(5)]
		[SerializeField]
		private int _sortOrder;

		[Header("Animation"), Space(5)]
		[SerializeField]
		private AnimationCurve _openAndCloseAnimationCurve;

		[Range(0, 10f)]
		[SerializeField]
		private float _duration;

		[Range(0, 1f)]
		[SerializeField]
		private float _maxCanvasGroupAlpha;

		private float _footerHeight;
		private bool _isVisible;
		private WaitForEndOfFrame _wait;
		private Coroutine _animateCoroutine;

		private void Awake()
		{
			_backButton.onClick.AddListener(OnBackButtonClicked);
			_openInfoButton.onClick.AddListener(OnOpenOrCloseInfoButtonClicked);
			_closeInfoButton.onClick.AddListener(OnOpenOrCloseInfoButtonClicked);
			_backgroundButton.onClick.AddListener(OnOpenOrCloseInfoButtonClicked);

			_wait = new WaitForEndOfFrame();
		}

		private void Start()
		{
			_footerHeight = _footerRectTransform.rect.height;

			var position = _footerRectTransform.anchoredPosition;
			position.y = -_footerHeight;
			_footerRectTransform.anchoredPosition = position;

			_canvas.sortingOrder = _sortOrder;
			_canvasGroup.alpha = 0f;
			_canvasGroup.interactable = false;
			_canvasGroup.blocksRaycasts = false;

			_backgroundButton.gameObject.SetActive(false);
		}

		private void OnDestroy()
		{
			_backButton.onClick.RemoveAllListeners();
			_openInfoButton.onClick.RemoveAllListeners();
			_closeInfoButton.onClick.RemoveAllListeners();
		}

		private void OnBackButtonClicked()
		{
			LoadingUIPanel.Instance.LoadScene(WearableConstants.MainMenuScene, LoadSceneMode.Single);
		}

		private void OnOpenOrCloseInfoButtonClicked()
		{
			if (_animateCoroutine != null)
			{
				StopCoroutine(_animateCoroutine);
			}

			_animateCoroutine = StartCoroutine(AnimateInfoPanelFooter());
		}

		private IEnumerator AnimateInfoPanelFooter()
		{
			_isVisible = !_isVisible;

			_backgroundButton.gameObject.SetActive(_isVisible);

			var current = 0f;
			var currentPosition = _footerRectTransform.anchoredPosition;
			var lerpPosition = currentPosition;
			lerpPosition.y = (_isVisible ? _footerHeight : -_footerHeight) / 2f;

			var currentAlpha = _canvasGroup.alpha;
			var targetAlpha = _isVisible ? _maxCanvasGroupAlpha : 0f;
			_canvasGroup.blocksRaycasts = _isVisible;
			_canvasGroup.interactable = _isVisible;

			while (current < _duration)
			{
				current += Time.unscaledDeltaTime;

				var ease = _openAndCloseAnimationCurve.Evaluate(Mathf.Clamp01(current / _duration));
				_footerRectTransform.anchoredPosition = Vector3.Lerp(currentPosition, lerpPosition, ease);
				_canvasGroup.alpha = Mathf.Lerp(currentAlpha, targetAlpha, ease);

				yield return _wait;
			}

			_animateCoroutine = null;
		}
	}
}
