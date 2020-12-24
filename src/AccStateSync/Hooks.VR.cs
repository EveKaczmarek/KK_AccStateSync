using HarmonyLib;
using System.Collections.Generic;
using System.Linq;

using JetPack;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		internal class HooksVR
		{
			[HarmonyPostfix, HarmonyPatch(typeof(VRHScene), "SetClothStateStartMotion")]
			private static void VRHScene_SetClothStateStartMotion_Postfix(VRHScene __instance)
			{
				foreach (var heroine in __instance.flags.lstHeroine)
				{
					ChaControl chaCtrl = heroine.chaCtrl;
					Logger.Log(DebugLogLevel, $"[VRHScene_SetClothStateStartMotion_Postfix][{chaCtrl.GetFullName()}]");
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

			[HarmonyPostfix, HarmonyPatch(typeof(VRHScene), "MapSameObjectDisable")]
			private static void VRHScene_MapSameObjectDisable_PostFix(List<ChaControl> ___lstFemale, HSprite[] ___sprites)
			{
				HScene.Inside = true;
				HScene.Heroine = ___lstFemale;
				foreach (HSprite sprite in ___sprites)
					HScene.Sprites.Add(sprite);
				HScene.ClearUI();
				HScene.UpdateUI();
			}

			[HarmonyPostfix, HarmonyPatch(typeof(VRHScene), "OnDestroy")]
			private static void VRHScene_OnDestroy_PostFix()
			{
				HScene.Inside = false;
				HScene.Sprites.Clear();
			}
		}
	}
}
