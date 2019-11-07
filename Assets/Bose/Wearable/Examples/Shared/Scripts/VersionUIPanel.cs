using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Bose.Wearable
{
	internal class VersionUIPanel : MonoBehaviour, IPointerClickHandler
	{
		[SerializeField]
		private Text _versionText;

		private bool _showSdkVersion;
		private string _sdkVersion;
		private string _unityVersion;

		private const string _sdkVersionFormat = "SDK v{0}";
		private const string _unityVersionFormat = "Unity {0}";

		private void Awake()
		{
			_showSdkVersion = true;

			_sdkVersion = string.Format(_sdkVersionFormat, WearableVersion.UnitySdkVersion);
			_unityVersion = string.Format(_unityVersionFormat, Application.unityVersion);

			UpdateVersionLabel();
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			_showSdkVersion = !_showSdkVersion;
			UpdateVersionLabel();
		}

		private void UpdateVersionLabel()
		{
			_versionText.text = _showSdkVersion ? _sdkVersion : _unityVersion;
		}
	}
}

