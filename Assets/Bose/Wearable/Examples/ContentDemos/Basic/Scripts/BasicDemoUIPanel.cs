using UnityEngine;
using UnityEngine.UI;

namespace Bose.Wearable.Examples
{
	internal sealed class BasicDemoUIPanel : MonoBehaviour
	{
		[Header("UX Refs")]
		[SerializeField]
		private Toggle _absoluteToggle;

		[SerializeField]
		private Toggle _referenceToggle;

		[Header("Sounds"), Space(5)]
		[SerializeField]
		private AudioClip _buttonPressClip;

		[SerializeField]
		private BasicDemoController _basicDemoController;

		private AudioControl _audioControl;

		private void Awake()
		{
			_absoluteToggle.onValueChanged.AddListener(OnAbsoluteButtonClicked);
			_referenceToggle.onValueChanged.AddListener(OnReferenceButtonClicked);

			_audioControl = AudioControl.Instance;
		}

		private void OnDestroy()
		{
			_absoluteToggle.onValueChanged.RemoveAllListeners();
			_referenceToggle.onValueChanged.RemoveAllListeners();
		}

		private void OnAbsoluteButtonClicked(bool isOn)
		{
			if (isOn)
			{
				_basicDemoController.SetAbsoluteReference();
				_audioControl.PlayOneShot(_buttonPressClip);
			}
		}

		private void OnReferenceButtonClicked(bool isOn)
		{
			if (isOn)
			{
				_basicDemoController.SetRelativeReference();
				_audioControl.PlayOneShot(_buttonPressClip);
			}
		}
	}
}
