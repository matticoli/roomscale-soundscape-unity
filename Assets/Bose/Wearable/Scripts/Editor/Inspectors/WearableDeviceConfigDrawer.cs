using UnityEditor;
using UnityEngine;

namespace Bose.Wearable.Editor.Inspectors
{
	[CustomPropertyDrawer(typeof(WearableDeviceConfig))]
	public sealed class WearableDeviceConfigDrawer : PropertyDrawer
	{
		// Property names
		private const string SensorUpdateIntervalPropertyName = "updateInterval";
		private const string AccelerometerConfigPropertyName = "accelerometer";
		private const string GyroscopeConfigPropertyName = "gyroscope";
		private const string RotationNineDofConfigPropertyName = "rotationNineDof";
		private const string RotationSixDofConfigPropertyName = "rotationSixDof";
		private const string DoubleTapPropertyName = "doubleTapGesture";
		private const string HeadNodPropertyName = "headNodGesture";
		private const string HeadShakePropertyName = "headShakeGesture";
		private const string TouchAndHoldPropertyName = "touchAndHoldGesture";
		private const string InputPropertyName = "inputGesture";
		private const string AffirmativePropertyName = "affirmativeGesture";
		private const string NegativePropertyName = "negativeGesture";

		private const string EnabledPropertyName = "isEnabled";

		// UI
		private const string UnavailableSensorWarning = "{0} Sensor Not Available on the Connected Device.";
		private const string UnavailableGestureWarning = "{0} Gesture Not Available on the Connected Device.";

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			var wearableControl = Application.isPlaying && WearableControl.Exists
				? WearableControl.Instance
				: null;
			var canShowWarning = Application.isPlaying &&
			                     wearableControl != null &&
			                     wearableControl.ConnectedDevice.HasValue;

			// Title
			var titleRect = new Rect(
				position.x,
				position.y,
				position.width,
				WearableConstants.SingleLineHeight);
			GUI.Label(titleRect, "Sensors", EditorStyles.boldLabel);

			var indent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;

			// Update interval
			var sensorUpdateIntervalRect = new Rect(
				position.x,
				titleRect.y + titleRect.height,
				position.width,
				WearableConstants.SingleLineHeight);

			EditorGUI.BeginDisabledGroup(HasAnySensorsEnabled(property));
			var sensorUpdateProp = property.FindPropertyRelative(SensorUpdateIntervalPropertyName);
			EditorGUI.PropertyField(sensorUpdateIntervalRect, sensorUpdateProp);
			EditorGUI.EndDisabledGroup();


			// Accelerometer
			var accelProp = property.FindPropertyRelative(AccelerometerConfigPropertyName);
			var accelRect = new Rect(
				position.x,
				sensorUpdateIntervalRect.y + sensorUpdateIntervalRect.height,
				position.width,
				EditorGUI.GetPropertyHeight(accelProp));
			EditorGUI.PropertyField(accelRect, accelProp);

			var accelWarningRect = new Rect(accelRect);
			if (canShowWarning && !wearableControl.GetWearableSensorById(SensorId.Accelerometer).IsAvailable)
			{
				accelWarningRect = new Rect(
					position.x,
					accelRect.y + accelRect.height,
					position.width,
					WearableConstants.SingleLineHeight * 2);
				EditorGUI.HelpBox(
					accelWarningRect,
					string.Format(UnavailableSensorWarning, SensorId.Accelerometer),
					MessageType.Warning);
			}

			// Gyroscope
			var gyroProp = property.FindPropertyRelative(GyroscopeConfigPropertyName);
			var gyroRect = new Rect(
				position.x,
				accelWarningRect.y + accelWarningRect.height,
				position.width,
				EditorGUI.GetPropertyHeight(gyroProp));
			EditorGUI.PropertyField(gyroRect, gyroProp);

			var gyroWarningRect = new Rect(gyroRect);
			if (canShowWarning && !wearableControl.GetWearableSensorById(SensorId.Gyroscope).IsAvailable)
			{
				gyroWarningRect = new Rect(
					position.x,
					gyroRect.y + gyroRect.height,
					position.width,
					WearableConstants.SingleLineHeight * 2);
				EditorGUI.HelpBox(
					gyroWarningRect,
					string.Format(UnavailableSensorWarning, SensorId.Gyroscope),
					MessageType.Warning);
			}

			// Rotation (Nine Dof)
			var rotNineProp = property.FindPropertyRelative(RotationNineDofConfigPropertyName);
			var rotNineRect = new Rect(
				position.x,
				gyroWarningRect.y + gyroWarningRect.height,
				position.width,
				EditorGUI.GetPropertyHeight(rotNineProp));
			EditorGUI.PropertyField(rotNineRect, rotNineProp);

			var rotNineWarningRect = new Rect(rotNineRect);
			if (canShowWarning && !wearableControl.GetWearableSensorById(SensorId.RotationNineDof).IsAvailable)
			{
				rotNineWarningRect = new Rect(
					position.x,
					rotNineRect.y + rotNineRect.height,
					position.width,
					WearableConstants.SingleLineHeight * 2);
				EditorGUI.HelpBox(
					rotNineWarningRect,
					string.Format(UnavailableSensorWarning, SensorId.RotationNineDof),
					MessageType.Warning);
			}

			// Rotation (Six Dof)
			var rotSixProp = property.FindPropertyRelative(RotationSixDofConfigPropertyName);
			var rotSixRect = new Rect(
				position.x,
				rotNineRect.y + rotNineRect.height,
				position.width,
				EditorGUI.GetPropertyHeight(rotSixProp));
			EditorGUI.PropertyField(rotSixRect, rotSixProp);

			var rotSixWarningRect = new Rect(rotSixRect);
			if (canShowWarning && !wearableControl.GetWearableSensorById(SensorId.RotationSixDof).IsAvailable)
			{
				rotSixWarningRect = new Rect(
					position.x,
					rotSixRect.y + rotSixRect.height,
					position.width,
					WearableConstants.SingleLineHeight * 2);
				EditorGUI.HelpBox(
					rotSixWarningRect,
					string.Format(UnavailableSensorWarning, SensorId.RotationSixDof),
					MessageType.Warning);
			}

			// Gestures
			var gesturesLabelRect = new Rect(
				position.x,
				rotSixWarningRect.y + rotSixWarningRect.height,
				position.width,
				WearableConstants.SingleLineHeight);
			EditorGUI.LabelField(gesturesLabelRect, "Gestures", EditorStyles.boldLabel);

			// Double Tap
			var doubleTapProp = property.FindPropertyRelative(DoubleTapPropertyName);
			var doubleTapRect = new Rect(
				position.x,
				gesturesLabelRect.y + gesturesLabelRect.height,
				position.width,
				EditorGUI.GetPropertyHeight(doubleTapProp));
			EditorGUI.PropertyField(doubleTapRect, doubleTapProp, new GUIContent(GestureId.DoubleTap.ToString()));

			var doubleTapWarningRect = new Rect(doubleTapRect);
			if (canShowWarning && !wearableControl.GetWearableGestureById(GestureId.DoubleTap).IsAvailable)
			{
				doubleTapWarningRect = new Rect(
					position.x,
					doubleTapRect.y + doubleTapRect.height,
					position.width,
					WearableConstants.SingleLineHeight * 2);
				EditorGUI.HelpBox(
					doubleTapWarningRect,
					string.Format(UnavailableGestureWarning, GestureId.DoubleTap),
					MessageType.Warning);
			}

			// Head Nod
			var headNodProp = property.FindPropertyRelative(HeadNodPropertyName);
			var headNodRect = new Rect(
				position.x,
				doubleTapWarningRect.y + doubleTapWarningRect.height,
				position.width,
				EditorGUI.GetPropertyHeight(headNodProp));
			EditorGUI.PropertyField(headNodRect, headNodProp, new GUIContent(GestureId.HeadNod.ToString()));

			var headNodWarningRect = new Rect(headNodRect);
			if (canShowWarning && !wearableControl.GetWearableGestureById(GestureId.HeadNod).IsAvailable)
			{
				headNodWarningRect = new Rect(
					position.x,
					headNodRect.y + headNodRect.height,
					position.width,
					WearableConstants.SingleLineHeight * 2);
				EditorGUI.HelpBox(
					headNodWarningRect,
					string.Format(UnavailableGestureWarning, GestureId.HeadNod),
					MessageType.Warning);
			}

			// Head Shake
			var headShakeProp = property.FindPropertyRelative(HeadShakePropertyName);
			var headShakeRect = new Rect(
				position.x,
				headNodWarningRect.y + headNodWarningRect.height,
				position.width,
				EditorGUI.GetPropertyHeight(headShakeProp));
			EditorGUI.PropertyField(headShakeRect, headShakeProp, new GUIContent(GestureId.HeadShake.ToString()));

			var headShakeWarningRect = new Rect(headShakeRect);
			if (canShowWarning && !wearableControl.GetWearableGestureById(GestureId.HeadShake).IsAvailable)
			{
				headShakeWarningRect = new Rect(
					position.x,
					headShakeRect.y + headShakeRect.height,
					position.width,
					WearableConstants.SingleLineHeight * 2);
				EditorGUI.HelpBox(
					headShakeWarningRect,
					string.Format(UnavailableGestureWarning, GestureId.HeadShake),
					MessageType.Warning);
			}

			// Touch and Hold
			var touchAndHoldProp = property.FindPropertyRelative(TouchAndHoldPropertyName);
			var touchAndHoldRect = new Rect(
				position.x,
				headShakeWarningRect.y + headShakeWarningRect.height,
				position.width,
				EditorGUI.GetPropertyHeight(touchAndHoldProp));
			EditorGUI.PropertyField(touchAndHoldRect, touchAndHoldProp, new GUIContent(GestureId.TouchAndHold.ToString()));

			var touchAndHoldWarningRect = new Rect(touchAndHoldRect);
			if (canShowWarning && !wearableControl.GetWearableGestureById(GestureId.TouchAndHold).IsAvailable)
			{
				touchAndHoldWarningRect = new Rect(
					position.x,
					touchAndHoldRect.y + touchAndHoldRect.height,
					position.width,
					WearableConstants.SingleLineHeight * 2);
				EditorGUI.HelpBox(
					touchAndHoldWarningRect,
					string.Format(UnavailableGestureWarning, GestureId.TouchAndHold),
					MessageType.Warning);
			}

			// Input
			var inputProp = property.FindPropertyRelative(InputPropertyName);
			var inputRect = new Rect(
				position.x,
				touchAndHoldWarningRect.y + touchAndHoldWarningRect.height,
				position.width,
				EditorGUI.GetPropertyHeight(inputProp));
			EditorGUI.PropertyField(inputRect, inputProp, new GUIContent(GestureId.Input.ToString()));

			var inputWarningRect = new Rect(inputRect);
			if (canShowWarning && !wearableControl.GetWearableGestureById(GestureId.Input).IsAvailable)
			{
				inputWarningRect = new Rect(
					position.x,
					inputRect.y + inputRect.height,
					position.width,
					WearableConstants.SingleLineHeight * 2);
				EditorGUI.HelpBox(
					inputWarningRect,
					string.Format(UnavailableGestureWarning, GestureId.Input),
					MessageType.Warning);
			}

			// Affirmative
			var affirmativeProp = property.FindPropertyRelative(AffirmativePropertyName);
			var affirmativeRect = new Rect(
				position.x,
				inputWarningRect.y + inputWarningRect.height,
				position.width,
				EditorGUI.GetPropertyHeight(affirmativeProp));
			EditorGUI.PropertyField(affirmativeRect, affirmativeProp, new GUIContent(GestureId.Affirmative.ToString()));

			var affirmativeWarningRect = new Rect(affirmativeRect);
			if (canShowWarning && !wearableControl.GetWearableGestureById(GestureId.Affirmative).IsAvailable)
			{
				affirmativeWarningRect = new Rect(
					position.x,
					affirmativeRect.y + affirmativeRect.height,
					position.width,
					WearableConstants.SingleLineHeight * 2);
				EditorGUI.HelpBox(
					affirmativeWarningRect,
					string.Format(UnavailableGestureWarning, GestureId.Affirmative),
					MessageType.Warning);
			}

			// Negative
			var negativeProp = property.FindPropertyRelative(NegativePropertyName);
			var negativeRect = new Rect(
				position.x,
				affirmativeWarningRect.y + affirmativeWarningRect.height,
				position.width,
				EditorGUI.GetPropertyHeight(negativeProp));
			EditorGUI.PropertyField(negativeRect, negativeProp, new GUIContent(GestureId.Negative.ToString()));

			var negativeWarningRect = new Rect(negativeRect);
			if (canShowWarning && !wearableControl.GetWearableGestureById(GestureId.Negative).IsAvailable)
			{
				negativeWarningRect = new Rect(
					position.x,
					negativeRect.y + negativeRect.height,
					position.width,
					WearableConstants.SingleLineHeight * 2);
				EditorGUI.HelpBox(
					negativeWarningRect,
					string.Format(UnavailableGestureWarning, GestureId.Negative),
					MessageType.Warning);
			}

			EditorGUI.indentLevel = indent;
			EditorGUI.EndProperty();
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var newProps = new[]
			{
				property.FindPropertyRelative(SensorUpdateIntervalPropertyName),
				property.FindPropertyRelative(AccelerometerConfigPropertyName),
				property.FindPropertyRelative(GyroscopeConfigPropertyName),
				property.FindPropertyRelative(RotationNineDofConfigPropertyName),
				property.FindPropertyRelative(RotationSixDofConfigPropertyName),
				property.FindPropertyRelative(DoubleTapPropertyName),
				property.FindPropertyRelative(HeadNodPropertyName),
				property.FindPropertyRelative(HeadShakePropertyName),
				property.FindPropertyRelative(TouchAndHoldPropertyName),
				property.FindPropertyRelative(InputPropertyName),
				property.FindPropertyRelative(AffirmativePropertyName),
				property.FindPropertyRelative(NegativePropertyName)
			};

			var height = WearableConstants.SingleLineHeight * 2;
			for (var i = 0; i < newProps.Length; i++)
			{
				height += EditorGUI.GetPropertyHeight(newProps[i]);
			}

			var wearableControl = Application.isPlaying && WearableControl.Exists
				? WearableControl.Instance
				: null;
			var canShowWarning = Application.isPlaying &&
			                     wearableControl != null &&
			                     wearableControl.ConnectedDevice.HasValue;

			if (canShowWarning)
			{
				for (var i = 0; i < WearableConstants.SensorIds.Length; i++)
				{
					var sensorId = WearableConstants.SensorIds[i];
					var sensor = wearableControl.GetWearableSensorById(sensorId);
					if (!sensor.IsAvailable)
					{
						height += WearableConstants.SingleLineHeight * 2;
					}
				}

				for (var i = 0; i < WearableConstants.GestureIds.Length; i++)
				{
					var gestureId = WearableConstants.GestureIds[i];
					if (gestureId == GestureId.None)
					{
						continue;
					}

					var gesture = wearableControl.GetWearableGestureById(gestureId);
					if (!gesture.IsAvailable)
					{
						height += WearableConstants.SingleLineHeight * 2;
					}
				}
			}

			return height;
		}

		/// <summary>
		/// Returns true if any sensors are enabled.
		/// </summary>
		/// <param name="property"></param>
		/// <returns></returns>
		private static bool HasAnySensorsEnabled(SerializedProperty property)
		{
			var newProps = new[]
			{
				property.FindPropertyRelative(AccelerometerConfigPropertyName),
				property.FindPropertyRelative(GyroscopeConfigPropertyName),
				property.FindPropertyRelative(RotationNineDofConfigPropertyName),
				property.FindPropertyRelative(RotationSixDofConfigPropertyName)
			};

			var numberOfSensorsActive = 0;
			for (var i = 0; i < newProps.Length; i++)
			{
				if (!newProps[i].FindPropertyRelative(EnabledPropertyName).boolValue)
				{
					continue;
				}

				numberOfSensorsActive++;
			}

			return numberOfSensorsActive == 0;
		}
	}
}
