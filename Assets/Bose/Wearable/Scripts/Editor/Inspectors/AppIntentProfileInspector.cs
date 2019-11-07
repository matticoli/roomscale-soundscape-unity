using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Bose.Wearable.Editor.Inspectors
{
	[CustomEditor(typeof(AppIntentProfile))]
	public class AppIntentProfileInspector : UnityEditor.Editor
	{
		private const string SensorsLabel = "Sensors";
		private const string GesturesLabel = "Gestures";
		private const string IntervalsLabel = "Sensor Update Intervals";
		private const string NineDofLabel = "Rotation (9DOF)";
		private const string SixDofLabel = "Rotation (6DOF)";
		private const string IntervalFormat = "{0} ms";

		private readonly List<SensorId> _newSensors;
		private readonly List<GestureId> _newGestures;
		private readonly List<SensorUpdateInterval> _newIntervals;

		private AppIntentProfileInspector()
		{
			_newSensors = new List<SensorId>();
			_newGestures = new List<GestureId>();
			_newIntervals = new List<SensorUpdateInterval>();
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			GUI.changed = false;

			AppIntentProfile profile = target as AppIntentProfile;
			if (profile == null)
			{
				// Nothing we can do, so give up.
				return;
			}

			// Sensors
			EditorGUILayout.LabelField(SensorsLabel, EditorStyles.boldLabel);
			_newSensors.Clear();
			bool sensorsChanged = false;
			for (int i = 0; i < WearableConstants.SensorIds.Length; i++)
			{
				SensorId id = WearableConstants.SensorIds[i];

				bool prior = profile.GetSensorInProfile(id);
				bool post = EditorGUILayout.Toggle(id.ToString(), prior, WearableConstants.EmptyLayoutOptions);
				sensorsChanged |= prior != post;

				if (post)
				{
					_newSensors.Add(id);
				}
			}

			if (sensorsChanged)
			{
				profile.SetSensorIntent(_newSensors);
			}

			// Intervals
			GUILayoutTools.LineSeparator();
			EditorGUILayout.LabelField(IntervalsLabel, EditorStyles.boldLabel);
			_newIntervals.Clear();
			bool intervalsChanged = false;
			for (int i = 0; i < WearableConstants.UpdateIntervals.Length; i++)
			{
				SensorUpdateInterval interval = WearableConstants.UpdateIntervals[i];
				string label = string.Format(
					IntervalFormat,
					((int) WearableTools.SensorUpdateIntervalToMilliseconds(interval)).ToString());
				bool prior = profile.GetIntervalInProfile(interval);
				bool post = EditorGUILayout.Toggle(label, prior, WearableConstants.EmptyLayoutOptions);
				intervalsChanged |= prior != post;

				if (post)
				{
					_newIntervals.Add(interval);
				}
			}

			if (intervalsChanged)
			{
				profile.SetIntervalIntent(_newIntervals);
			}


			// Gestures
			GUILayoutTools.LineSeparator();
			EditorGUILayout.LabelField(GesturesLabel, EditorStyles.boldLabel);

			_newGestures.Clear();
			bool gesturesChanged = false;
			for (int i = 0; i < WearableConstants.GestureIds.Length; i++)
			{
				GestureId id = WearableConstants.GestureIds[i];

				if (id == GestureId.None)
				{
					continue;
				}

				bool prior = profile.GetGestureInProfile(id);
				bool post = EditorGUILayout.Toggle(id.ToString(), prior, WearableConstants.EmptyLayoutOptions);
				gesturesChanged |= prior != post;

				if (post)
				{
					_newGestures.Add(id);
				}
			}

			if (gesturesChanged)
			{
				profile.SetGestureIntent(_newGestures);
			}

			if (HasDeviceSpecificGesturesEnabled(profile))
			{
				EditorGUILayout.HelpBox(WearableConstants.DeviceSpecificGestureDiscouragedWarning, MessageType.Warning);
			}

			if (GUI.changed)
			{
				serializedObject.ApplyModifiedProperties();
				EditorUtility.SetDirty(target);
			}
		}

		private bool HasDeviceSpecificGesturesEnabled(AppIntentProfile profile)
		{
			for (int i = 0; i < WearableConstants.GestureIds.Length; i++)
			{
				GestureId id = WearableConstants.GestureIds[i];

				if (id == GestureId.None)
				{
					continue;
				}

				if (profile.GetGestureInProfile(id) && id.IsGestureDeviceSpecific())
				{
					return true;
				}
			}

			return false;
		}
	}
}
