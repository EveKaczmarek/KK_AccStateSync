using System;

using BepInEx;
using HarmonyLib;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		class MoreAccessories
		{
			internal static BaseUnityPlugin _instance;

			internal static Type _type = null;
			internal static bool _newVer = false;

			internal static void Init()
			{
				_instance = JetPack.MoreAccessories.Instance;
				_type = _instance.GetType();

				_newVer = JetPack.MoreAccessories.NewVer;
			}

			internal static void HarmonyPatch()
			{
				_hooksInstance["MoreAccessories"] = Harmony.CreateAndPatchAll(typeof(Hooks));

				_hooksInstance["MoreAccessories"].Patch(_type.Assembly.GetType("MoreAccessoriesKOI.ChaControl_SetAccessoryStateAll_Patches").GetMethod("Postfix", AccessTools.all), prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.CharaMakerPreview_Block_Prefix)));
				_hooksInstance["MoreAccessories"].Patch(_type.Assembly.GetType("MoreAccessoriesKOI.ChaControl_SetAccessoryStateCategory_Patches").GetMethod("Postfix", AccessTools.all), prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.CharaMakerPreview_Block_Prefix)));

				_hooksInstance["MoreAccessories"].Patch(typeof(ChaControl).GetMethod("SetAccessoryStateAll", AccessTools.all), prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.CharaMakerPreview_Block_Prefix)));
				_hooksInstance["MoreAccessories"].Patch(typeof(ChaControl).GetMethod("SetAccessoryStateCategory", AccessTools.all), prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.CharaMakerPreview_Block_Prefix)));
			}

			internal static void HarmonyUnpatch()
			{
				if (_hooksInstance["MoreAccessories"] == null) return;

				_hooksInstance["MoreAccessories"].UnpatchAll(_hooksInstance["MoreAccessories"].Id);
				_hooksInstance["MoreAccessories"] = null;
			}

			internal static void UpdateUI()
			{
				AccessTools.Method(_type, "UpdateUI").Invoke(_instance, null);
			}

			internal class Hooks
			{
				internal static bool CharaMakerPreview_Block_Prefix()
				{
					return !(JetPack.CharaMaker.Inside && _cfgCharaMakerPreview.Value);
				}
			}
		}
	}
}
