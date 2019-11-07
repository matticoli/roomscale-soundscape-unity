using UnityEditor;

namespace Bose.Wearable.Editor.Inspectors
{
	[CustomEditor(typeof(GestureDetector))]
	public sealed class GestureDetectorInspector : UnityEditor.Editor
	{
		private SerializedProperty _gestureId;
		private SerializedProperty _gestureEvent;

		private const string GestureIdField = "_gesture";
		private const string GestureEventField = "_onGestureDetected";

		private const string GestureSelectMessage = "Please select a Gesture.";

		private void OnEnable()
		{
			_gestureId = serializedObject.FindProperty(GestureIdField);
			_gestureEvent = serializedObject.FindProperty(GestureEventField);
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			GestureId gestureId = (GestureId)_gestureId.intValue;
			bool isGestureSelected = gestureId != (int) GestureId.None;

			EditorGUILayout.PropertyField(_gestureId, WearableConstants.EmptyLayoutOptions);

			if (!isGestureSelected)
			{
				EditorGUILayout.HelpBox(GestureSelectMessage, MessageType.Warning);
			}
			else if (gestureId.IsGestureDeviceSpecific())
			{
				EditorGUILayout.HelpBox(WearableConstants.DeviceSpecificGestureDiscouragedWarning, MessageType.Warning);
			}

			EditorGUILayout.PropertyField(_gestureEvent, WearableConstants.EmptyLayoutOptions);

			serializedObject.ApplyModifiedProperties();
		}
	}
}
