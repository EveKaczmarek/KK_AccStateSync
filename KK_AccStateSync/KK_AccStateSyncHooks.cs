using HarmonyLib;
using System.Collections.Generic;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		internal class Hooks
		{
			[HarmonyPostfix, HarmonyPatch(typeof(ChaControl), "SetClothesState")]
			internal static void SetClothesStatePostfix(ChaControl __instance, int clothesKind)
			{
				AccStateSyncController controller = GetController(__instance);
				if (controller != null)
				{
					int state = __instance.fileStatus.clothesState[clothesKind];
					controller.ToggleByClothesState(__instance, clothesKind, state);
				}
			}

			[HarmonyPostfix, HarmonyPatch(typeof(HSceneProc), "SetClothStateStartMotion")]
			internal static void SetClothStateStartMotionPostfix(HSceneProc __instance)
			{
				foreach (var heroine in __instance.flags.lstHeroine)
				{
					ChaControl chaCtrl = heroine.chaCtrl;
					Logger.Log(DebugLogLevel, $"[Harmony][HSceneProc][SetClothStateStartMotion][Postfix][{chaCtrl.chaFile.parameter?.fullname}]");
					AccStateSyncController controller = GetController(chaCtrl);
					if (controller != null)
						controller.SyncAllAccToggle();
				}
			}

			[HarmonyPostfix, HarmonyPatch(typeof(HSceneProc), "Start")]
			internal static void HSceneProcStartPostfix(List<ChaControl> ___lstFemale, HSprite ___sprite)
			{
				HSceneHeroine = ___lstFemale;
				HSprites.Add(___sprite);
			}

			[HarmonyPriority(Priority.Last)]
			[HarmonyPostfix, HarmonyPatch(typeof(ChaControl), "SetAccessoryStateCategory")]
			internal static void SetAccessoryStateCategoryPostfix(ChaControl __instance, int cateNo, bool show)
			{
				AccStateSyncController controller = GetController(__instance);
				if (controller != null)
				{
					if (controller.CoroutineCounter <= CoroutineCounterMax.Value)
						controller.CoroutineCounter ++;
				}
			}
		}
	}
}
