using System.Collections.Generic;
using System.Linq;

using ChaCustom;
using Studio;

using HarmonyLib;

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

				if (MathfEx.RangeEqualOn(0, clothesKind, 6))
					_pluginCtrl.ToggleByClothesState(clothesKind);
				else
					_pluginCtrl.ToggleByShoesType(clothesKind);
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
				_pluginCtrl.ToggleByShoesType(_slotIndex);
			}
		}

		internal class HooksCharaMaker
		{
			[HarmonyPriority(Priority.Last)]
			[HarmonyPostfix, HarmonyPatch(typeof(CvsAccessory), nameof(CvsAccessory.UpdateSelectAccessoryType), new[] { typeof(int) })]
			private static void CvsAccessory_UpdateSelectAccessoryType_Postfix(CvsAccessory __instance, int index)
			{
				if (CharaMaker._pluginCtrl == null) return;
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
	}
}
