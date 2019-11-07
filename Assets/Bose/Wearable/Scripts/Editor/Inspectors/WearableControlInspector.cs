using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bose.Wearable.Extensions;
using UnityEditor;
using UnityEngine;

namespace Bose.Wearable.Editor.Inspectors
{
	[CustomEditor(typeof(WearableControl))]
	public sealed class WearableControlInspector : UnityEditor.Editor
	{
		private SerializedProperty _editorProvider;
		private SerializedProperty _runtimeProvider;

		private Dictionary<string, ProviderId> _editorProviderMap;
		private string[] _editorProviderOptions;
		private int _editorProviderIndex;

		private Dictionary<string, ProviderId> _runtimeProviderMap;
		private string[] _runtimeProviderOptions;
		private int _runtimeProviderIndex;

		private const string OverrideConfigPresentWarning =
			"The device config is currently overridden and does not reflect the normal device config present " +
			"during normal runtime usage.";
		private const string SpecifyIntentWarning =
			"Specify an intent profile to enable intent validation.";
		private const string IntentValidationInProgress = "App intent validation in progress...";
		private const string IntentValidationSucceeded = "Current app intent is valid.";
		private const string IntentValidationFailed =
			"Current app intent is invalid! Some sensors, gestures, or update intervals might not be available on " +
			"this hardware or firmware version.";
		private const string IntentProfileChanged =
			"The active intent profile has changed since last validation. Consider re-validating.";

		private const string EditorDefaultTitle = "Editor Default";
		private const string RuntimeDefaultTitle = "Runtime Default";
		private const string ResolvedDeviceConfigTitle = "Resolved Device Config";
		private const string OverrideDeviceConfigTitle = "Override Device Config";
		private const string ValidateIntentsTitle = "Validate Intents";
		private const string TitleSeparator = " - ";

		private const string EditorDefaultProviderField = "_editorDefaultProvider";
		private const string RuntimeDefaultProviderField = "_runtimeDefaultProvider";
		private const string UpdateModeField = "_updateMode";
		private const string DebugProviderField = "_debugProvider";
		private const string DeviceProviderField = "_deviceProvider";
		private const string USBProviderField = "_usbProvider";
		private const string ProxyProviderField = "_proxyProvider";
		private const string IntentProfileField = "_activeAppIntentProfile";

		private const string FinalWearableDeviceConfigField = "_finalWearableDeviceConfig";
		private const string OverrideWearableDeviceConfigField = "_overrideDeviceConfig";

		private WearableControl _wearableControl;

		private void OnEnable()
		{
			_editorProvider = serializedObject.FindProperty(EditorDefaultProviderField);
			_runtimeProvider = serializedObject.FindProperty(RuntimeDefaultProviderField);

			_editorProviderMap = GetProviderMap(WearableConstants.DisallowedEditorProviders);
			_editorProviderOptions = _editorProviderMap.Keys.ToArray();

			_runtimeProviderMap = GetProviderMap(WearableConstants.DisallowedRuntimeProviders);
			_runtimeProviderOptions = _runtimeProviderMap.Keys.ToArray();

			_wearableControl = (WearableControl)target;
		}

		private void DrawProviderBox(string field, ProviderId provider)
		{
			bool isEditorDefault = _editorProvider.enumValueIndex == (int) provider;
			bool isRuntimeDefault = _runtimeProvider.enumValueIndex == (int) provider;

			if (isEditorDefault || isRuntimeDefault)
			{
				GUILayoutTools.LineSeparator();

				StringBuilder titleBuilder = new StringBuilder();
				titleBuilder.Append(Enum.GetName(typeof(ProviderId), provider));

				if (isEditorDefault)
				{
					titleBuilder.Append(TitleSeparator);
					titleBuilder.Append(EditorDefaultTitle);
				}

				if (isRuntimeDefault)
				{
					titleBuilder.Append(TitleSeparator);
					titleBuilder.Append(RuntimeDefaultTitle);
				}

				EditorGUILayout.LabelField(titleBuilder.ToString(), WearableConstants.EmptyLayoutOptions);

				EditorGUILayout.PropertyField(serializedObject.FindProperty(field), WearableConstants.EmptyLayoutOptions);
			}
		}

		private void DrawIntentsSection()
		{
			bool isDeviceConnected = _wearableControl.ConnectedDevice.HasValue;

			var status = _wearableControl.GetIntentValidationStatus();

			var profileProperty = serializedObject.FindProperty(IntentProfileField);
			AppIntentProfile oldProfile = profileProperty.objectReferenceValue as AppIntentProfile;
			EditorGUILayout.ObjectField(profileProperty, WearableConstants.EmptyLayoutOptions);
			AppIntentProfile newProfile = profileProperty.objectReferenceValue as AppIntentProfile;
			if (newProfile == null)
			{
				EditorGUILayout.HelpBox(SpecifyIntentWarning, MessageType.Warning);
				return;
			}

			// If the profile changed at runtime, and there's a device connected, check intents again.
			if (oldProfile != newProfile && isDeviceConnected)
			{
				_wearableControl.SetIntentProfile(newProfile);
			}

			// Profile description
			EditorGUILayout.HelpBox(newProfile.ToString(), MessageType.None);

			if (!Application.isPlaying || !isDeviceConnected)
			{
				return;
			}

			// Re-validate warning
			if (status == IntentValidationStatus.Unknown)
			{
				EditorGUILayout.HelpBox(IntentProfileChanged, MessageType.Warning);

				bool validateAgain = GUILayout.Button(ValidateIntentsTitle, WearableConstants.EmptyLayoutOptions);
				if (validateAgain)
				{
					_wearableControl.SetIntentProfile(newProfile);
					_wearableControl.ValidateIntentProfile();
				}
			}
			else
			{
				// Status box
				switch (status)
				{
					// "Unknown" is checked above, so no need to check it here.
					case IntentValidationStatus.Validating:
						EditorGUILayout.HelpBox(IntentValidationInProgress, MessageType.Info);
						break;
					case IntentValidationStatus.Success:
						EditorGUILayout.HelpBox(IntentValidationSucceeded, MessageType.Info);
						break;
					case IntentValidationStatus.Failure:
						EditorGUILayout.HelpBox(IntentValidationFailed, MessageType.Error);
						break;
					case IntentValidationStatus.Disabled:
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			// Update mode
			EditorGUILayout.PropertyField(serializedObject.FindProperty(UpdateModeField), WearableConstants.EmptyLayoutOptions);

			// Provider defaults
			using (new EditorGUI.DisabledScope(Application.isPlaying))
			{
				// Editor Default Provider
				string editorProviderName = GetNameForProvider((ProviderId)_editorProvider.intValue);
				int optionIndex = Array.IndexOf(_editorProviderOptions, editorProviderName);
				_editorProviderIndex = EditorGUILayout.Popup(
					ObjectNames.NicifyVariableName(EditorDefaultProviderField),
					optionIndex >= 0 ? optionIndex : (int)WearableConstants.EditorDefaultProvider,
					_editorProviderOptions
				);

				_editorProvider.intValue = (int)_editorProviderMap[_editorProviderOptions[_editorProviderIndex]];

				// Runtime Default Provider
				string runtimeProviderName = GetNameForProvider((ProviderId)_runtimeProvider.intValue);
				optionIndex = Array.IndexOf(_runtimeProviderOptions, runtimeProviderName);
				_runtimeProviderIndex = EditorGUILayout.Popup(
					ObjectNames.NicifyVariableName(RuntimeDefaultProviderField),
					optionIndex >= 0 ? optionIndex : (int)WearableConstants.RuntimeDefaultProvider,
					_runtimeProviderOptions
				);

				_runtimeProvider.intValue = (int)_runtimeProviderMap[_runtimeProviderOptions[_runtimeProviderIndex]];
			}

			// Intent profile
			GUILayoutTools.LineSeparator();
			DrawIntentsSection();

			// Providers
			DrawProviderBox(DebugProviderField, ProviderId.DebugProvider);
			DrawProviderBox(ProxyProviderField, ProviderId.WearableProxy);
			DrawProviderBox(USBProviderField, ProviderId.USBProvider);
			DrawProviderBox(DeviceProviderField, ProviderId.WearableDevice);

			if (Application.isPlaying)
			{
				GUILayoutTools.LineSeparator();

				if (_wearableControl.IsOverridingDeviceConfig)
				{
					EditorGUILayout.LabelField(OverrideDeviceConfigTitle, WearableConstants.EmptyLayoutOptions);
					EditorGUILayout.HelpBox(OverrideConfigPresentWarning, MessageType.Warning);
					using (new EditorGUI.DisabledScope(true))
					{
						EditorGUILayout.PropertyField(
							serializedObject.FindProperty(OverrideWearableDeviceConfigField),
							WearableConstants.EmptyLayoutOptions);
					}
				}
				else
				{
					EditorGUILayout.LabelField(ResolvedDeviceConfigTitle, WearableConstants.EmptyLayoutOptions);
					using (new EditorGUI.DisabledScope(true))
					{
						EditorGUILayout.PropertyField(
							serializedObject.FindProperty(FinalWearableDeviceConfigField),
							WearableConstants.EmptyLayoutOptions);
					}
				}

			}

			serializedObject.ApplyModifiedProperties();
		}

		public override bool RequiresConstantRepaint()
		{
			return Application.isPlaying;
		}

		private Dictionary<string, ProviderId> GetProviderMap(ProviderId[] disallowedProviders)
		{
			var providerNames = new Dictionary<string, ProviderId>();
			var providers = (ProviderId[])Enum.GetValues(typeof(ProviderId));
			for (int i = 0; i < providers.Length; ++i)
			{
				var providerId = providers[i];
				if (disallowedProviders.Contains(providerId))
				{
					continue;
				}

				var providerName = GetNameForProvider(providerId);
				providerNames.Add(providerName, providerId);
			}

			return providerNames;
		}

		private string GetNameForProvider(ProviderId providerId)
		{
			var result = Enum.GetName(typeof(ProviderId), providerId);
			result = result == null ? string.Empty : result.Nicify();

			return result;
		}
	}
}
