using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using ChaCustom;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		internal static Dictionary<string, Harmony> HooksInstance = new Dictionary<string, Harmony>();

		internal class Hooks
		{
			[HarmonyPostfix, HarmonyPatch(typeof(ChaControl), "SetClothesState")]
			private static void SetClothesStatePostfix(ChaControl __instance, int clothesKind)
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
			private static void ShoesTypePostfix(ChaFileStatus __instance)
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
			private static void CvsAccessoryUpdateSelectAccessoryTypePostfix(CvsAccessory __instance, int index)
			{
				AccStateSyncController controller = GetController(KKAPI.Maker.MakerAPI.GetCharacterControl());
				if (controller != null)
				{
					if (index == 0)
						controller.CvsAccessoryUpdateSelectAccessoryTypePostfix((int)__instance.slotNo);
				}
			}

			[HarmonyPostfix, HarmonyPatch(typeof(CvsAccessory), "UpdateSelectAccessoryParent", new[] {typeof(int)})]
			private static void CvsAccessoryUpdateSelectAccessoryParentPostfix(CvsAccessory __instance, int index)
			{
				AccStateSyncController controller = GetController(KKAPI.Maker.MakerAPI.GetCharacterControl());
				if (controller != null)
					controller.CvsAccessoryUpdateSelectAccessoryParentPostfix((int)__instance.slotNo);
			}

			[HarmonyPriority(Priority.Last)]
			[HarmonyPostfix, HarmonyPatch(typeof(ListInfoBase), nameof(ListInfoBase.GetInfo))]
			private static void ListInfoBaseGetInfoPostfix(ListInfoBase __instance, ChaListDefine.KeyType keyType)
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
			[HarmonyPriority(Priority.Last)]
			[HarmonyPostfix, HarmonyPatch(typeof(HSceneProc), "SetClothStateStartMotion")]
			private static void SetClothStateStartMotionPostfix(HSceneProc __instance)
			{
				foreach (var heroine in __instance.flags.lstHeroine)
				{
					ChaControl chaCtrl = heroine.chaCtrl;
					Logger.Log(DebugLogLevel, $"[Harmony][HSceneProc][SetClothStateStartMotion][Postfix][{chaCtrl.chaFile.parameter?.fullname}]");
					AccStateSyncController controller = GetController(chaCtrl);
					if (controller != null)
					{
						if (AutoHideSecondary.Value)
						{
							for (int i = 0; i < 7; i++)
							{
								List<string> secondary = controller.CharaVirtualGroupInfo[i].Values?.Where(x => x.Secondary)?.Select(x => x.Group)?.ToList();
								foreach (string group in secondary)
									controller.CharaVirtualGroupInfo[i][group].State = false;
							}
						}
						controller.SyncAllAccToggle();
					}
				}
			}

			[HarmonyPostfix, HarmonyPatch(typeof(HSceneProc), "Start")]
			private static void HSceneProcStartPostfix(List<ChaControl> ___lstFemale, HSprite ___sprite)
			{
				Logger.Log(DebugLogLevel, "HSceneProcStartPostfix");
				HScene.Heroine = ___lstFemale;
				HScene.Sprites.Add(___sprite);
			}

			[HarmonyPostfix, HarmonyPatch(typeof(HSceneProc), "MapSameObjectDisable")]
			private static void HSceneProcMapSameObjectDisablePostFix()
			{
				HScene.Inside = true;
				HScene.UpdateUI();
			}

			[HarmonyPostfix, HarmonyPatch(typeof(HSceneProc), "OnDestroy")]
			private static void HSceneProcOnDestroyPostFix()
			{
				HScene.Inside = false;
				HScene.Sprites.Clear();
				HooksInstance["HScene"].UnpatchAll(HooksInstance["HScene"].Id);
				HooksInstance["HScene"] = null;
				Logger.Log(DebugLogLevel, "HSceneProcOnDestroyPostFix");
			}
		}
	}
}
