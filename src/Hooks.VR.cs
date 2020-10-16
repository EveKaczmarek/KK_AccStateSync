using HarmonyLib;
using System.Collections.Generic;
using System.Linq;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		internal class HooksVR
		{
			[HarmonyPostfix, HarmonyPatch(typeof(VRHScene), "SetClothStateStartMotion")]
			private static void SetClothStateStartMotionPostfix(VRHScene __instance)
			{
				foreach (var heroine in __instance.flags.lstHeroine)
				{
					ChaControl chaCtrl = heroine.chaCtrl;
					Logger.Log(DebugLogLevel, $"[Harmony][VRHScene][SetClothStateStartMotion][Postfix][{chaCtrl.chaFile.parameter?.fullname}]");
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

			[HarmonyPostfix, HarmonyPatch(typeof(VRHScene), "Start")]
			private static void VRHSceneStartPostfix(List<ChaControl> ___lstFemale, HSprite[] ___sprites)
			{
				HScene.Heroine = ___lstFemale;
				foreach (HSprite sprite in ___sprites)
					HScene.Sprites.Add(sprite);
			}

			[HarmonyPostfix, HarmonyPatch(typeof(VRHScene), "MapSameObjectDisable")]
			private static void VRHSceneLoadPostFix()
			{
				HScene.Inside = true;
				HScene.UpdateUI();
			}

			[HarmonyPostfix, HarmonyPatch(typeof(VRHScene), "OnDestroy")]
			private static void VRHSceneOnDestroyPostFix()
			{
				HScene.Inside = false;
				HScene.Sprites.Clear();
			}
		}
	}
}
