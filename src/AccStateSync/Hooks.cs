using System.Collections.Generic;
using System.Linq;

using ChaCustom;

using HarmonyLib;

using JetPack;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		internal static Dictionary<string, Harmony> HooksInstance = new Dictionary<string, Harmony>();

		internal class Hooks
		{
			[HarmonyPriority(Priority.Last)]
			[HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetClothesState))]
			private static void ChaControl_SetClothesState_Postfix(ChaControl __instance, int clothesKind)
			{
				AccStateSyncController pluginCtrl = GetController(__instance);
				if (pluginCtrl == null) return;

				int state = __instance.fileStatus.clothesState[clothesKind];
				if (MathfEx.RangeEqualOn(0, clothesKind, 6))
					pluginCtrl.ToggleByClothesState(clothesKind, state);
				else
					pluginCtrl.ToggleByShoesType(clothesKind, state);
			}

			[HarmonyPriority(Priority.Last)]
			[HarmonyPostfix, HarmonyPatch(typeof(ChaFileStatus), nameof(ChaFileStatus.shoesType), MethodType.Setter)]
			private static void ChaFileStatus_shoesType_Postfix(ChaFileStatus __instance)
			{
				ChaControl chaCtrl = FindObjectsOfType<ChaControl>().Where(x => x.chaFile?.status == __instance).FirstOrDefault();
				if (chaCtrl == null) return;

				int clothesKind = __instance.shoesType == 0 ? 7 : 8;
				int state = __instance.clothesState[clothesKind];
				AccStateSyncController pluginCtrl = GetController(chaCtrl);
				if (pluginCtrl != null)
					pluginCtrl.ToggleByShoesType(clothesKind, state);
			}
		}

		internal class HooksCharaMaker
		{
			[HarmonyPriority(Priority.Last)]
			[HarmonyPostfix, HarmonyPatch(typeof(CvsAccessory), nameof(CvsAccessory.UpdateSelectAccessoryType), new[] { typeof(int) })]
			private static void CvsAccessory_UpdateSelectAccessoryType_Postfix(CvsAccessory __instance, int index)
			{
				if (CharaMaker.pluginCtrl == null) return;
				if (index == 0)
					CharaMaker.pluginCtrl.CvsAccessory_UpdateSelectAccessoryType_Postfix((int) __instance.slotNo);
			}

			[HarmonyPriority(Priority.Last)]
			[HarmonyPostfix, HarmonyPatch(typeof(CvsAccessory), nameof(CvsAccessory.UpdateSelectAccessoryParent), new[] { typeof(int) })]
			private static void CvsAccessory_UpdateSelectAccessoryParent_Postfix(CvsAccessory __instance)
			{
				if (CharaMaker.pluginCtrl == null) return;
				CharaMaker.pluginCtrl.CvsAccessory_UpdateSelectAccessoryParent_Postfix((int) __instance.slotNo);
			}

			[HarmonyPriority(Priority.Last)]
			[HarmonyPostfix, HarmonyPatch(typeof(ListInfoBase), nameof(ListInfoBase.GetInfo))]
			private static void ListInfoBase_GetInfo_Postfix(ListInfoBase __instance, ChaListDefine.KeyType keyType)
			{
				if (keyType == ChaListDefine.KeyType.Coordinate)
				{
					int Category = int.Parse(__instance.dictInfo[(int) ChaListDefine.KeyType.Category]);
					if ((Category == 105) || (Category == 107))
					{
						if (CharaMaker.pluginCtrl != null)
						{
							int KeyTypeCoordinat = (int) ChaListDefine.KeyType.Coordinate;
							string Coordinate = __instance.dictInfo.ContainsKey(KeyTypeCoordinat) ? __instance.dictInfo[KeyTypeCoordinat] : "0";
							CharaMaker.pluginCtrl.VerifyOnePiece(Category, int.Parse(Coordinate));
						}
					}
				}
			}
		}

		internal class HooksHScene
		{
			[HarmonyPriority(Priority.Last)]
			[HarmonyPostfix, HarmonyPatch(typeof(HSceneProc), "SetClothStateStartMotion")]
			private static void HSceneProc_SetClothStateStartMotion_Postfix(HSceneProc __instance)
			{
				foreach (var heroine in __instance.flags.lstHeroine)
				{
					ChaControl chaCtrl = heroine.chaCtrl;
					Logger.Log(DebugLogLevel, $"[HSceneProc_SetClothStateStartMotion_Postfix][{chaCtrl.GetFullName()}]");
					AccStateSyncController pluginCtrl = GetController(chaCtrl);
					if (pluginCtrl != null)
					{
						if (AutoHideSecondary.Value)
						{
							for (int i = 0; i < 7; i++)
							{
								List<string> secondary = pluginCtrl.CharaVirtualGroupInfo[i].Values?.Where(x => x.Secondary)?.Select(x => x.Group)?.ToList();
								foreach (string group in secondary)
									pluginCtrl.CharaVirtualGroupInfo[i][group].State = false;
							}
						}
						pluginCtrl.SyncAllAccToggle();
					}
				}
			}

			[HarmonyPostfix, HarmonyPatch(typeof(HSceneProc), "MapSameObjectDisable")]
			private static void HSceneProc_MapSameObjectDisable_PostFix(List<ChaControl> ___lstFemale, HSprite ___sprite)
			{
				HScene.Inside = true;
				HScene.Heroine = ___lstFemale;
				HScene.Sprites.Add(___sprite);
				HScene.ClearUI();
				HScene.UpdateUI();
			}

			[HarmonyPostfix, HarmonyPatch(typeof(HSceneProc), "OnDestroy")]
			private static void HSceneProc_OnDestroy_PostFix()
			{
				HScene.Inside = false;
				HScene.Sprites.Clear();
				HooksInstance["HScene"].UnpatchAll(HooksInstance["HScene"].Id);
				HooksInstance["HScene"] = null;
			}
		}
	}
}
