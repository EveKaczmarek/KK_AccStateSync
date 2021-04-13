using System.Collections;
using System.Collections.Generic;
using System.Linq;

using ChaCustom;
using Studio;

using BepInEx.Logging;
using HarmonyLib;

using JetPack;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		internal static Dictionary<string, Harmony> _hooksInstance = new Dictionary<string, Harmony>();

		internal class Hooks
		{
			[HarmonyPriority(Priority.Last)]
			[HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetClothesState))]
			private static void ChaControl_SetClothesState_Postfix(ChaControl __instance, int clothesKind)
			{
				AccStateSyncController _pluginCtrl = GetController(__instance);
				if (_pluginCtrl == null) return;

				int _state = __instance.fileStatus.clothesState[clothesKind];
				if (MathfEx.RangeEqualOn(0, clothesKind, 6))
					_pluginCtrl.ToggleByClothesState(clothesKind, _state);
				else
					_pluginCtrl.ToggleByShoesType(clothesKind, _state);
			}

			[HarmonyPriority(Priority.Last)]
			[HarmonyPostfix, HarmonyPatch(typeof(ChaFileStatus), nameof(ChaFileStatus.shoesType), MethodType.Setter)]
			private static void ChaFileStatus_shoesType_Postfix(ChaFileStatus __instance)
			{
				ChaControl _chaCtrl = FindObjectsOfType<ChaControl>().Where(x => x.chaFile?.status == __instance).FirstOrDefault();
				if (_chaCtrl == null) return;
				AccStateSyncController _pluginCtrl = GetController(_chaCtrl);
				if (_pluginCtrl == null) return;

				int _slotIndex = __instance.shoesType == 0 ? 7 : 8;
				int _state = __instance.clothesState[_slotIndex];
				_pluginCtrl.ToggleByShoesType(_slotIndex, _state);
			}
		}

		internal class HooksCharaMaker
		{
			[HarmonyPriority(Priority.Last)]
			[HarmonyPostfix, HarmonyPatch(typeof(CvsAccessory), nameof(CvsAccessory.UpdateSelectAccessoryType), new[] { typeof(int) })]
			private static void CvsAccessory_UpdateSelectAccessoryType_Postfix(CvsAccessory __instance, int index)
			{
				if (CharaMaker._pluginCtrl == null) return;
				if (index == 0)
					CharaMaker._pluginCtrl.CvsAccessory_UpdateSelectAccessoryType_Postfix((int) __instance.slotNo);
			}

			[HarmonyPriority(Priority.Last)]
			[HarmonyPostfix, HarmonyPatch(typeof(CvsAccessory), nameof(CvsAccessory.UpdateSelectAccessoryParent), new[] { typeof(int) })]
			private static void CvsAccessory_UpdateSelectAccessoryParent_Postfix(CvsAccessory __instance)
			{
				if (CharaMaker._pluginCtrl == null) return;
				CharaMaker._pluginCtrl.CvsAccessory_UpdateSelectAccessoryParent_Postfix((int) __instance.slotNo);
			}
		}

		internal class HooksCharaStudio
		{
			[HarmonyPostfix, HarmonyPatch(typeof(MPCharCtrl), nameof(MPCharCtrl.OnClickRoot))]
			private static void MPCharCtrl_OnClickRoot(int _idx)
			{
				if (_idx == 0)
					CharaStudio.UpdateUI();
				else
					CharaStudio.ClearUI();
			}
		}

		internal class HooksHScene
		{
			internal static void HSceneProc_SetClothStateStartMotion_Postfix(HSceneProc __instance)
			{
				foreach (SaveData.Heroine _heroine in __instance.flags.lstHeroine)
				{
					ChaControl _chaCtrl = _heroine.chaCtrl;
					DebugMsg(LogLevel.Info, $"[HSceneProc_SetClothStateStartMotion_Postfix][{_chaCtrl.GetFullName()}]");
					AccStateSyncController _pluginCtrl = GetController(_chaCtrl);
					if (_pluginCtrl != null)
					{
						if (_cfgAutoHideSecondary.Value)
						{
							for (int i = 0; i < 7; i++)
							{
								List<string> _secondary = _pluginCtrl.CharaVirtualGroupInfo[i].Values?.Where(x => x.Secondary)?.Select(x => x.Group)?.ToList();
								foreach (string _group in _secondary)
									_pluginCtrl.CharaVirtualGroupInfo[i][_group].State = false;
							}
						}
						_pluginCtrl.SyncAllAccToggle();
					}
				}
			}
		}
	}
}
