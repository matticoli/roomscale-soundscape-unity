using Bose.Wearable.Extensions;
using UnityEngine;

namespace Bose.Wearable
{
	/// <summary>
	/// <see cref="GestureDebugUIWindow"/> setups up 1:1 <see cref="GestureToggleDebugUIControl"/> for all
	/// available <see cref="GestureId"/>(s).
	/// </summary>
	internal sealed class GestureDebugUIWindow : MonoBehaviour
	{
		[Header("UI Refs")]
		[SerializeField]
		private Transform _gestureRootTransform;

		[Header("Prefabs"), Space(5)]
		[SerializeField]
		private GestureToggleDebugUIControl _gestureToggleDebugUIControl;

		private void Awake()
		{
			// Clear out any items on the root transforms prior to setting it up with new prefabs
			_gestureRootTransform.DeleteAllChildren();

			// Instantiate 1:1 prefab for distinct gesture ids.
			for (var i = 0; i < WearableConstants.GestureIds.Length; i++)
			{
				if (WearableConstants.GestureIds[i] == GestureId.None)
				{
					continue;
				}

				var gestureToggle = Instantiate(
					_gestureToggleDebugUIControl,
					_gestureRootTransform,
					false);

				gestureToggle.SetGestureId(WearableConstants.GestureIds[i]);
			}
		}
	}
}
