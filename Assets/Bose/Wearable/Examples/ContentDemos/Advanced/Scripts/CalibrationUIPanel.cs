using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Bose.Wearable.Examples
{
	public class CalibrationUIPanel : MonoBehaviour
	{
		[Header("Scene Refs")]
		[SerializeField]
		private AdvancedDemoController _advancedDemoController;

		[Header("UX Refs"), Space(5)]
		[SerializeField]
		private Canvas _canvas;

		[SerializeField]
		private CanvasGroup _panelCanvasGroup;

		[SerializeField]
		private Text _labelText;

		[Header("UX Refs"), Space(5)]
		[SerializeField]
		[Range(0, float.MaxValue)]
		private float _fadeDuration = 0.66f;

		private Coroutine _fadeCoroutine;

		private void Awake()
		{
			_labelText.text = WearableConstants.WaitForCalibrationMessage;

			_advancedDemoController.CalibrationCompleted += OnCalibrationCompleted;
		}

		private void Start()
		{
			Show();
		}

		private void OnDestroy()
		{
			_advancedDemoController.CalibrationCompleted -= OnCalibrationCompleted;

			if (_fadeCoroutine != null)
			{
				StopCoroutine(_fadeCoroutine);
				_fadeCoroutine = null;
			}
		}

		private void Show()
		{
			_panelCanvasGroup.alpha = 1f;
		}

		private void Hide()
		{
			if (_fadeCoroutine != null)
			{
				StopCoroutine(_fadeCoroutine);
			}

			_fadeCoroutine = StartCoroutine(FadePanelUI());
		}

		private void OnCalibrationCompleted()
		{
			Hide();
		}

		private IEnumerator FadePanelUI()
		{
			var waitForEndOfFrame = new WaitForEndOfFrame();
			var time = 0f;
			var startAlpha = _panelCanvasGroup.alpha;
			while (_panelCanvasGroup.alpha > 0f)
			{
				time += Time.unscaledDeltaTime;
				_panelCanvasGroup.alpha = startAlpha * Mathf.Clamp01(1f - (time / _fadeDuration));

				yield return waitForEndOfFrame;
			}

			_panelCanvasGroup.alpha = 0f;
			_panelCanvasGroup.interactable = _panelCanvasGroup.blocksRaycasts = false;
			_canvas.enabled = false;

			_fadeCoroutine = null;
		}
	}
}
