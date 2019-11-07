using UnityEngine;

namespace Bose.Wearable
{
	/// <summary>
	/// Constant fields for the debug panel.
	/// </summary>
	internal static class DebuggingConstants
	{
		// Format strings
		public const string FramesPerSecondFormat = "{0:0.0}";
		public const string DataComponentFormatPositive = "<color=#999999>{0}</color>{1: 000.000}";
		public const string DataComponentFormatNegative = "<color=#999999>{0}</color>{1:-000.000}";
		public const string EulerDataComponentFormatPositive = "<color=#999999>{0}</color>{1: 000.00°}";
		public const string EulerDataComponentFormatNegative = "<color=#999999>{0}</color>{1:-000.00°}";
		public const string QuaternionComponentFormatPositive = "<color=#999999>{0}</color>{1: 0.00}";
		public const string QuaternionComponentFormatNegative = "<color=#999999>{0}</color>{1:-0.00}";
		public const string UncertaintyFormat = "± {0:00.00}°";
		public const string RotationSourceFormat = "{0} {1}";
		public const string SecondsFormat = "{0:0.000}s";
		public const string EulerUnits = "| deg";

		public const string XField = "X";
		public const string YField = "Y";
		public const string ZField = "Z";
		public const string WField = "W";

		// UI
		public const string ResetOverrideButtonText = "RESET OVERRIDE";
		public const string OverrideConfigButtonText = "OVERRIDE CONFIG";

		public const string ResetOverrideConfigOnHideTooltip =
			"When this is true, closing the DebugUIPanel will automatically stop overriding the devie config. " +
			"Otherwise the user will need to click the \"" + ResetOverrideButtonText + "\" button to do " +
			"so.";

		public static readonly WearableUIColorPalette.Style EmptyStyle;

		static DebuggingConstants()
		{
			EmptyStyle = new WearableUIColorPalette.Style()
			{
				textColor = Color.magenta,
				elementColor = Color.magenta
			};
		}
	}
}
