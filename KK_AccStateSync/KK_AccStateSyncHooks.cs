using HarmonyLib;
using System.Collections.Generic;
using ChaCustom;

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

			[HarmonyPriority(Priority.Last)]
			[HarmonyPostfix, HarmonyPatch(typeof(ChaControl), "SetAccessoryStateCategory")]
			internal static void SetAccessoryStateCategoryPostfix(ChaControl __instance, int cateNo, bool show)
			{
				AccStateSyncController controller = GetController(__instance);
				if (controller != null)
				{
					if (controller.CoroutineCounter <= CoroutineCounterMax.Value)
						controller.CoroutineCounter++;
				}
			}

			[HarmonyPostfix, HarmonyPatch(typeof(CvsAccessory), "UpdateSelectAccessoryType", new[] {typeof(int)})]
			internal static void CvsAccessoryUpdateSelectAccessoryTypePostfix(CvsAccessory __instance, int index)
			{
				AccStateSyncController controller = GetController(KKAPI.Maker.MakerAPI.GetCharacterControl());
				if (controller != null)
				{
					if (index == 0)
						controller.ResetSlot((int)__instance.slotNo);
				}
			}
		}

		internal class HooksHScene
		{
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

			[HarmonyPostfix, HarmonyPatch(typeof(HSceneProc), "MapSameObjectDisable")]
			internal static void HSceneProcMapSameObjectDisablePostFix()
			{
				InsideHScene = true;
				UpdateHUI();
			}

			[HarmonyPostfix, HarmonyPatch(typeof(HSceneProc), "OnDestroy")]
			internal static void HSceneProcOnDestroyPostFix()
			{
				InsideHScene = false;
				HSprites.Clear();
			}
		}
	}
}
