using System;
using UnityEditor;
using UnityEngine;

namespace Bose.Wearable.Editor.Inspectors
{
	[CustomPropertyDrawer(typeof(DynamicDeviceInfo))]
	public class DynamicDeviceInfoDrawer : PropertyDrawer
	{
		private const float TopPadding = 5f;
		private const string StatusValuesField = "deviceStatus._value";
		
		private DeviceStatusFlags[] Flags
		{
			get
			{
				if (_flags == null)
				{
					_flags = (DeviceStatusFlags[]) Enum.GetValues(typeof(DeviceStatusFlags));
				}

				return _flags;
			}
		}
		
		private DeviceStatusFlags[] _flags;
		
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			int indent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;

			EditorGUI.BeginProperty(position, label, property);
			
			GUI.Box(position, GUIContent.none);
			Rect line = new Rect(
				position.x,
				position.y + TopPadding,
				position.width,
				WearableConstants.SingleLineHeight);

			
			var statusValueProp = property.FindPropertyRelative(StatusValuesField);
			DeviceStatus status = statusValueProp.intValue;
			for (int i = 0; i < Flags.Length; i++)
			{
				DeviceStatusFlags flag = Flags[i];
				if (flag == DeviceStatusFlags.None ||
				    flag == DeviceStatusFlags.SensorServiceSuspended)
				{
					continue;
				}

				using (new EditorGUI.DisabledScope(flag == DeviceStatusFlags.SensorServiceSuspended))
				{
					bool value = EditorGUI.Toggle(
						line, 
						flag.ToString(),
						status.GetFlagValue(flag));
					
					status.SetFlagValue(flag, value);
				}

				line.y += WearableConstants.SingleLineHeight;
			}
			statusValueProp.intValue = status;

			EditorGUI.EndProperty();
			property.serializedObject.ApplyModifiedProperties();
			EditorGUI.indentLevel = indent;
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return WearableConstants.SingleLineHeight * (Flags.Length - 2) + TopPadding;
		}
	}
}
