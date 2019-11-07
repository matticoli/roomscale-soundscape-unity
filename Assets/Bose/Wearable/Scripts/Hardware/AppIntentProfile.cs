using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Bose.Wearable
{
	[CreateAssetMenu(fileName = "App Intent Profile", menuName = "Bose Wearable/App Intent Profile")]
	public class AppIntentProfile : ScriptableObject
	{
		[SerializeField]
		private List<SensorId> _sensors;

		[SerializeField]
		private List<GestureId> _gestures;

		[SerializeField]
		private List<SensorUpdateInterval> _intervals;

		internal bool IsDirty
		{
			get { return _dirty; }
			set { _dirty = value; }
		}

		[SerializeField]
		private bool _dirty;

		private static StringBuilder _stringBuilder;

		/// <summary>
		/// Dynamically create a new intent profile. Optionally takes lists of requested sensors/gestures/intervals.
		/// </summary>
		/// <param name="sensors"></param>
		/// <param name="gestures"></param>
		/// <param name="intervals"></param>
		/// <returns></returns>
		public static AppIntentProfile Create(
			List<SensorId> sensors = null,
			List<GestureId> gestures = null,
			List<SensorUpdateInterval> intervals = null)
		{
			var profile = (AppIntentProfile)CreateInstance(typeof(AppIntentProfile));

			if (sensors != null)
			{
				profile.SetSensorIntent(sensors);
			}

			if (gestures != null)
			{
				profile.SetGestureIntent(gestures);
			}

			if (intervals != null)
			{
				profile.SetIntervalIntent(intervals);
			}

			return profile;
		}

		/// <summary>
		/// Add a sensor to the profile.
		/// </summary>
		/// <param name="id"></param>
		public void AddSensor(SensorId id)
		{
			if (!GetSensorInProfile(id))
			{
				_sensors.Add(id);
				_dirty = true;
			}
		}

		/// <summary>
		/// Removes a sensor from the profile. Has no effect if the sensor is not already in the profile.
		/// </summary>
		/// <param name="id"></param>
		public void RemoveSensor(SensorId id)
		{
			_sensors.Remove(id);
			_dirty = true;
		}

		/// <summary>
		/// Returns whether or not a sensor ID is specified by the intent profile
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public bool GetSensorInProfile(SensorId id)
		{
			return _sensors.Contains(id);
		}

		/// <summary>
		/// Add a gesture to the profile.
		/// </summary>
		/// <param name="id"></param>
		public void AddGesture(GestureId id)
		{
			if (id == GestureId.None)
			{
				return;
			}

			if (!GetGestureInProfile(id))
			{
				_gestures.Add(id);
				_dirty = true;
			}
		}

		/// <summary>
		/// Removes a gesture from the profile. Has no effect if the gesture is not already in the profile.
		/// </summary>
		/// <param name="id"></param>
		public void RemoveGesture(GestureId id)
		{
			_gestures.Remove(id);
			_dirty = true;
		}

		/// <summary>
		/// Returns whether or not a gesture ID is specified by the intent profile
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public bool GetGestureInProfile(GestureId id)
		{
			return _gestures.Contains(id);
		}

		/// <summary>
		/// Adds an interval to the profile.
		/// </summary>
		/// <param name="interval"></param>
		public void AddInterval(SensorUpdateInterval interval)
		{
			if (!GetIntervalInProfile(interval))
			{
				_intervals.Add(interval);
				_dirty = true;
			}
		}

		/// <summary>
		/// Remove an interval from the profile. Has no effect if the interval is not already in the profile.
		/// </summary>
		/// <param name="interval"></param>
		public void RemoveInterval(SensorUpdateInterval interval)
		{
			_intervals.Remove(interval);
			_dirty = true;
		}

		/// <summary>
		/// Returns whether or not a sensor update interval is specified by the intent profile
		/// </summary>
		/// <param name="interval"></param>
		/// <returns></returns>
		public bool GetIntervalInProfile(SensorUpdateInterval interval)
		{
			return _intervals.Contains(interval);
		}


		internal void SetSensorIntent(List<SensorId> sensors)
		{
			_sensors.Clear();
			_sensors.AddRange(sensors);
			_dirty = true;
		}

		internal void SetGestureIntent(List<GestureId> gestures)
		{
			_gestures.Clear();
			_gestures.AddRange(gestures);
			_dirty = true;
		}

		internal void SetIntervalIntent(List<SensorUpdateInterval> intervals)
		{
			_intervals.Clear();
			_intervals.AddRange(intervals);
			_dirty = true;
		}

		public override string ToString()
		{
			const string ItemSeparator = ", ";
			const string NoneLabel = "None";
			const string SensorsLabel = "Sensors: ";
			const string GesturesLabel = "Gestures: ";
			const string IntervalsLabel = "Intervals: ";
			const string IntervalFormat = "{0}ms";

			if (_stringBuilder == null)
			{
				_stringBuilder = new StringBuilder();
			}
			else
			{
				_stringBuilder.Length = 0;
			}

			_stringBuilder.Append(SensorsLabel);
			for (int i = 0; i < _sensors.Count; i++)
			{
				SensorId id = _sensors[i];

				_stringBuilder.Append(_sensors[i].ToString());

				if (i != _sensors.Count - 1)
				{
					_stringBuilder.Append(ItemSeparator);
				}
			}

			if (_sensors.Count == 0)
			{
				_stringBuilder.Append(NoneLabel);
			}

			_stringBuilder.AppendLine();
			_stringBuilder.Append(IntervalsLabel);
			for (int i = 0; i < _intervals.Count; i++)
			{
				_stringBuilder.AppendFormat(
					IntervalFormat,
					((int) WearableTools.SensorUpdateIntervalToMilliseconds(_intervals[i])).ToString());

				if (i != _intervals.Count - 1)
				{
					_stringBuilder.Append(ItemSeparator);
				}
			}

			if (_intervals.Count == 0)
			{
				_stringBuilder.Append(NoneLabel);
			}

			_stringBuilder.AppendLine();
			_stringBuilder.Append(GesturesLabel);
			for (int i = 0; i < _gestures.Count; i++)
			{
				_stringBuilder.Append(_gestures[i].ToString());

				if (i != _gestures.Count - 1)
				{
					_stringBuilder.Append(ItemSeparator);
				}
			}

			if (_gestures.Count == 0)
			{
				_stringBuilder.Append(NoneLabel);
			}

			return _stringBuilder.ToString();
		}

		private void OnValidate()
		{
			if (_sensors == null)
			{
				_sensors = new List<SensorId>();
			}

			if (_gestures == null)
			{
				_gestures = new List<GestureId>();
			}

			if (_intervals == null)
			{
				_intervals = new List<SensorUpdateInterval>();
			}

			Debug.Assert(!_gestures.Contains(GestureId.None));
		}

		private void OnEnable()
		{
			_dirty = true;

			if (_sensors == null)
			{
				_sensors = new List<SensorId>();
			}

			if (_gestures == null)
			{
				_gestures = new List<GestureId>();
			}

			if (_intervals == null)
			{
				_intervals = new List<SensorUpdateInterval>();
			}
		}
	}
}
