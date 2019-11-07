using UnityEditor;
using UnityEngine;

namespace Bose.Wearable
{
	[CustomEditor(typeof(SafeAreaHelper))]
	internal sealed class SafeAreaHelperInspector : Editor
	{
		private const string SetCurrentSafeAreaButtonText = "Set Current Safe Area";
		private const string ApplySimulatedSafeAreaButtonText = "Apply Simulated Safe Area";

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			var safeAreaHelper = (SafeAreaHelper)target;

			if (GUILayout.Button(SetCurrentSafeAreaButtonText))
			{
				safeAreaHelper.SetSafeAreaAsSimulatedSafeArea();
			}

			using (new EditorGUI.DisabledScope(!Application.isPlaying))
			{
				if (GUILayout.Button(ApplySimulatedSafeAreaButtonText))
				{
					safeAreaHelper.ApplySimulatedSafeArea();
				}
			}
		}
	}
}
