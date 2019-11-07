using UnityEngine;

namespace Bose.Wearable.Extensions
{
	/// <summary>
	/// Extension methods for <see cref="RectTransform"/>.
	/// </summary>
	internal static class RectTransformExtensions
	{
		private readonly static Vector2 PointLeftMiddle = new Vector2(0f, 0.5f);
		private readonly static Vector2 PointCenterMiddle = new Vector2(0.5f, 0.5f);
		private readonly static Vector2 PointRightMiddle = new Vector2(1f, 0.5f);
		
		/// <summary>
		/// Sets the anchor to the left-middle of its parent <see cref="RectTransform"/>.
		/// </summary>
		/// <param name="rectTransform"></param>
		/// <param name="worldPositionStays">Keep or reset the anchorPosition after moving the anchor.</param>
		public static void SetAnchorLeftMiddle(this RectTransform rectTransform, bool worldPositionStays = true)
		{
			SetAnchorPoint(rectTransform, PointLeftMiddle, worldPositionStays);
		}
		
		/// <summary>
		/// Sets the pivot to the left, middle of the <see cref="RectTransform"/>.
		/// </summary>
		/// <param name="rectTransform"></param>
		/// <param name="worldPositionStays">Keep or reset the anchorPosition after moving the pivot.</param>
		public static void SetPivotLeftMiddle(this RectTransform rectTransform, bool worldPositionStays = true)
		{
			SetPivotPoint(rectTransform, PointLeftMiddle, worldPositionStays);
		}
		
		/// <summary>
		/// Sets the anchor to the center of its parent <see cref="RectTransform"/>.
		/// </summary>
		/// <param name="rectTransform"></param>
		/// <param name="worldPositionStays">Keep or reset the anchorPosition after moving the anchor.</param>
		public static void SetAnchorCenterMiddle(this RectTransform rectTransform, bool worldPositionStays = true)
		{
			SetAnchorPoint(rectTransform, PointCenterMiddle, worldPositionStays);
		}

		/// <summary>
		/// Sets the pivot to the center, middle of the <see cref="RectTransform"/>.
		/// </summary>
		/// <param name="rectTransform"></param>
		/// <param name="worldPositionStays">Keep or reset the anchorPosition after moving the pivot.</param>
		public static void SetPivotCenterMiddle(this RectTransform rectTransform, bool worldPositionStays = true)
		{
			SetPivotPoint(rectTransform, PointCenterMiddle, worldPositionStays);
		}

		/// <summary>
		/// Sets the anchor to the right-middle of its parent <see cref="RectTransform"/>.
		/// </summary>
		/// <param name="rectTransform"></param>
		/// <param name="worldPositionStays">Keep or reset the anchorPosition after moving the anchor.</param>
		public static void SetAnchorRightMiddle(this RectTransform rectTransform, bool worldPositionStays = true)
		{
			SetAnchorPoint(rectTransform, PointRightMiddle, worldPositionStays);
		}
		
		/// <summary>
		/// Sets the pivot to the right, middle of the <see cref="RectTransform"/>.
		/// </summary>
		/// <param name="rectTransform"></param>
		/// <param name="worldPositionStays">Keep or reset the anchorPosition after moving the pivot.</param>
		public static void SetPivotRightMiddle(this RectTransform rectTransform, bool worldPositionStays = true)
		{
			SetPivotPoint(rectTransform, PointRightMiddle, worldPositionStays);
		}

		public static void SetAnchorPoint(RectTransform rectTransform, Vector2 point, bool worldPositionStays = true)
		{
			var position = rectTransform.position;
			
			rectTransform.anchorMin = point;
			rectTransform.anchorMax = point;

			if (worldPositionStays)
			{
				rectTransform.position = position;
			}
			else
			{
				rectTransform.ResetAnchorPosition();
			}
		}
		
		public static void SetPivotPoint(RectTransform rectTransform, Vector2 point, bool worldPositionStays = true)
		{
			var pivotDelta = point - rectTransform.pivot;

			rectTransform.pivot = point;
			
			if (worldPositionStays)
			{
				rectTransform.anchoredPosition += Vector2.Scale(rectTransform.sizeDelta, pivotDelta);
			}
			else
			{
				rectTransform.ResetAnchorPosition();
			}
		}

		public static void ResetAnchorPosition(this RectTransform rectTransform)
		{
			rectTransform.anchoredPosition = Vector2.zero;
		}
	}
}
