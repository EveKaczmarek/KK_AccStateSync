using HarmonyLib;
using System.Collections.Generic;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		internal class HooksVR
		{
			[HarmonyPostfix, HarmonyPatch(typeof(VRHScene), "SetClothStateStartMotion")]
			internal static void SetClothStateStartMotionPostfix(VRHScene __instance)
			{
				foreach (var heroine in __instance.flags.lstHeroine)
				{
					ChaControl chaCtrl = heroine.chaCtrl;
					Logger.Log(DebugLogLevel, $"[Harmony][VRHScene][SetClothStateStartMotion][Postfix][{chaCtrl.chaFile.parameter?.fullname}]");
					AccStateSyncController controller = GetController(chaCtrl);
					if (controller != null)
						controller.SyncAllAccToggle();
				}
			}

			[HarmonyPostfix, HarmonyPatch(typeof(VRHScene), "Start")]
			internal static void VRHSceneStartPostfix(List<ChaControl> ___lstFemale, HSprite[] ___sprites)
			{
				HSceneHeroine = ___lstFemale;
				foreach (HSprite sprite in ___sprites)
					HSprites.Add(sprite);
			}

			[HarmonyPostfix, HarmonyPatch(typeof(VRHScene), "MapSameObjectDisable")]
			internal static void VRHSceneLoadPostFix()
			{
				InsideHScene = true;
				UpdateHUI();
			}

			[HarmonyPostfix, HarmonyPatch(typeof(VRHScene), "OnDestroy")]
			internal static void VRHSceneDestroyPostFix()
			{
				InsideHScene = false;
				HSprites.Clear();
			}
		}
	}
}
