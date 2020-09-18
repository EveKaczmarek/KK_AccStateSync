using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using ChaCustom;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		public static HarmonyLib.Harmony HooksInstanceCharaMaker = null;
		public static HarmonyLib.Harmony HooksInstanceHScene = null;

		internal class Hooks
		{
			[HarmonyPostfix, HarmonyPatch(typeof(ChaControl), "SetClothesState")]
			internal static void SetClothesStatePostfix(ChaControl __instance, int clothesKind)
			{
				AccStateSyncController controller = GetController(__instance);
				if (controller != null)
				{
					int state = __instance.fileStatus.clothesState[clothesKind];
					if (MathfEx.RangeEqualOn(0, clothesKind, 6))
						controller.ToggleByClothesState(__instance, clothesKind, state);
					else
						controller.ToggleByShoesType(__instance, clothesKind, state);
				}
			}

			[HarmonyPostfix, HarmonyPatch(typeof(ChaFileStatus), nameof(ChaFileStatus.shoesType), MethodType.Setter)]
			internal static void ShoesTypePostfix(ChaFileStatus __instance)
			{
				ChaControl chaCtrl = FindObjectsOfType<ChaControl>().Where(x => x?.chaFile?.status == __instance).FirstOrDefault();
				if (chaCtrl != null)
				{
					int clothesKind = chaCtrl.fileStatus.shoesType == 0 ? 7 : 8;
					int state = chaCtrl.fileStatus.clothesState[clothesKind];
					AccStateSyncController controller = GetController(chaCtrl);
					if (controller != null)
						controller.ToggleByShoesType(chaCtrl, clothesKind, state);
				}
			}
		}

		internal class HooksCharaMaker
		{
			[HarmonyPostfix, HarmonyPatch(typeof(CvsAccessory), "UpdateSelectAccessoryType", new[] {typeof(int)})]
			internal static void CvsAccessoryUpdateSelectAccessoryTypePostfix(CvsAccessory __instance, int index)
			{
				AccStateSyncController controller = GetController(KKAPI.Maker.MakerAPI.GetCharacterControl());
				if (controller != null)
				{
					if (index == 0)
						controller.CvsAccessoryUpdateSelectAccessoryTypePostfix((int)__instance.slotNo);
				}
			}

			[HarmonyPostfix, HarmonyPatch(typeof(CvsAccessory), "UpdateSelectAccessoryParent", new[] {typeof(int)})]
			internal static void CvsAccessoryUpdateSelectAccessoryParentPostfix(CvsAccessory __instance, int index)
			{
				AccStateSyncController controller = GetController(KKAPI.Maker.MakerAPI.GetCharacterControl());
				if (controller != null)
					controller.CvsAccessoryUpdateSelectAccessoryParentPostfix((int)__instance.slotNo);
			}

			[HarmonyPriority(Priority.Last)]
			[HarmonyPostfix, HarmonyPatch(typeof(ListInfoBase), nameof(ListInfoBase.GetInfo))]
			internal static void ListInfoBaseGetInfoPostfix(ListInfoBase __instance, ChaListDefine.KeyType keyType)
			{
				if (keyType == ChaListDefine.KeyType.Coordinate)
				{
					int Category = System.Int32.Parse(__instance.dictInfo[(int) ChaListDefine.KeyType.Category]);
					if ((Category == 105) || (Category == 107))
					{
						AccStateSyncController controller = GetController(KKAPI.Maker.MakerAPI.GetCharacterControl());
						if (controller != null)
						{
							int KeyTypeCoordinat = (int) ChaListDefine.KeyType.Coordinate;
							string Coordinate = __instance.dictInfo.ContainsKey(KeyTypeCoordinat) ? __instance.dictInfo[KeyTypeCoordinat] : "0";
							controller.VerifyOnePiece(Category, System.Int32.Parse(Coordinate));
						}
					}
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
				Logger.Log(DebugLogLevel, "HSceneProcStartPostfix");
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
				HooksInstanceHScene.UnpatchAll(HooksInstanceHScene.Id);
				HooksInstanceHScene = null;
				Logger.Log(DebugLogLevel, "HSceneProcOnDestroyPostFix");
			}
		}
	}
}
