using System.Collections.Generic;
using System.Linq;

using Studio;

using HarmonyLib;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		internal static Dictionary<string, Harmony> _hooksInstance = new Dictionary<string, Harmony>();

		internal class Hooks
		{
			internal static bool Return_False() => false;

			[HarmonyPriority(Priority.Last)]
			[HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetClothesState), new[] { typeof(int), typeof(byte), typeof(bool) })]
			internal static void ChaControl_SetClothesState_Postfix(ChaControl __instance, int clothesKind)
			{
				AccStateSyncController _pluginCtrl = GetController(__instance);
				if (_pluginCtrl != null)
					_pluginCtrl.ToggleByRefKind(clothesKind);
			}

			[HarmonyPriority(Priority.Last)]
			[HarmonyPostfix, HarmonyPatch(typeof(ChaFileStatus), nameof(ChaFileStatus.shoesType), MethodType.Setter)]
			internal static void ChaFileStatus_ShoesType_Postfix(ChaFileStatus __instance)
			{
				ChaControl _chaCtrl = FindObjectsOfType<ChaControl>().FirstOrDefault(x => x?.chaFile?.status == __instance);
				if (_chaCtrl != null)
				{
					AccStateSyncController _pluginCtrl = GetController(_chaCtrl);
					if (_pluginCtrl != null)
					{
						int clothesKind = __instance.shoesType == 0 ? 7 : 8;
						_pluginCtrl.ToggleByRefKind(clothesKind);
					}
				}
			}
		}

		internal class HooksCharaStudio
		{
			[HarmonyPostfix, HarmonyPatch(typeof(MPCharCtrl), nameof(MPCharCtrl.OnClickRoot))]
			private static void MPCharCtrl_OnClickRoot_Postfix(int _idx)
			{
				if (_idx == 0)
					CharaStudio.UpdateUI();
			}
		}
	}
}
