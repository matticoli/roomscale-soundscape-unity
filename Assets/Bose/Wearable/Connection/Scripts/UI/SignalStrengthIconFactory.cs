using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Bose.Wearable
{
	internal sealed class SignalStrengthIconFactory : ScriptableObject
	{
		[Serializable]
		private class IconMapping
		{
			#pragma warning disable 0649
			public SignalStrength signalStrength;

			public Sprite sprite;
			#pragma warning restore 0649
		}

		[SerializeField]
		private List<IconMapping> _iconMappings;

		private Dictionary<SignalStrength, IconMapping> iconMappingLookup;

		private const string MappingMissingWarningFormat =
			"[Bose Wearable] A mapping is missing for SignalStrength [{0}] on SignalStrengthIconFactory instance.";
		private const string IconUnassignedWarningFormat =
			"[Bose Wearable] An icon is not assigned for SignalStrength [{0}] on SignalStrengthIconFactory instance.";
		private const string IconMappingIsDuplicated =
			"[Bose Wearable] There is more than one icon mapping for SignalStrength [{0}] on SignalStrengthIconFactory instance.";

		private void OnEnable()
		{
			iconMappingLookup = new Dictionary<SignalStrength, IconMapping>();
			for (var i = 0; i < _iconMappings.Count; i++)
			{
				if (iconMappingLookup.ContainsKey(_iconMappings[i].signalStrength))
				{
					continue;
				}

				iconMappingLookup.Add(_iconMappings[i].signalStrength, _iconMappings[i]);
			}
		}

		/// <summary>
		/// Returns true if a mapping is found for <see cref="SignalStrength"/> <paramref name="signalStrength"/> to a
		/// <see cref="Sprite"/>, otherwise false.
		/// </summary>
		/// <param name="signalStrength"></param>
		/// <param name="sprite"></param>
		/// <returns></returns>
		public bool TryGetIcon(SignalStrength signalStrength, out Sprite sprite)
		{
			sprite = null;

			IconMapping iconMapping;
			if (!iconMappingLookup.TryGetValue(signalStrength, out iconMapping))
			{
				Debug.LogWarningFormat(this, MappingMissingWarningFormat, signalStrength);
				return false;
			}

			sprite = iconMapping.sprite;

			return true;
		}

		#if UNITY_EDITOR

		private void OnValidate()
		{
			// Iterate through all SignalStrength values and ensure there is a single icon mapping for each one.
			// Flag any duplicate icon mappings for SignalStrength.
			for (var i = 0; i < WearableConstants.SignalStrengths.Length; i++)
			{
				var signalStrength = WearableConstants.SignalStrengths[i];

				// If we have an existing mapping for this SignalStrength, skip it.
				if (_iconMappings.Any(x => x.signalStrength == signalStrength))
				{
					if (_iconMappings.Count(x => x.signalStrength == signalStrength) > 1)
					{
						Debug.LogWarningFormat(this, IconMappingIsDuplicated, signalStrength);
					}

					continue;
				}

				// Where we do not find a mapping for this SignalStrength, add one.
				_iconMappings.Add(new IconMapping { signalStrength = signalStrength });
			}

			// Ensure all icon mappings have a sprite assigned.
			for (var i = 0; i < _iconMappings.Count; i++)
			{
				if (_iconMappings[i].sprite != null)
				{
					continue;
				}

				Debug.LogWarningFormat(this, IconUnassignedWarningFormat, _iconMappings[i].signalStrength);
			}
		}

		private void Reset()
		{
			_iconMappings = new List<IconMapping>();
			for (var i = 0; i < WearableConstants.SignalStrengths.Length; i++)
			{
				_iconMappings.Add(new IconMapping { signalStrength = WearableConstants.SignalStrengths[i] });
			}
		}

		#endif
	}
}
