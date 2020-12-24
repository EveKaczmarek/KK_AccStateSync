using System;

using HarmonyLib;

namespace AccStateSync
{
	class MoreAccessories
	{
		internal static readonly BepInEx.Logging.ManualLogSource Logger = AccStateSync.Logger;
		internal static Type MoreAccType = null;
		internal static object MoreAccObj;
		internal static bool NewVer = false;
		internal static Harmony HarmonyInstance;

		internal static void LoadAssembly()
		{
			MoreAccType = Type.GetType("MoreAccessoriesKOI.MoreAccessories, MoreAccessories");
			MoreAccObj = Traverse.Create(MoreAccType).Field("_self").GetValue();

			BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue("com.joan6694.illusionplugins.moreaccessories", out BepInEx.PluginInfo target);
			NewVer = !(target.Metadata.Version.CompareTo(new Version("1.0.10")) < 0);
		}

		internal static void HarmonyPatch()
		{
			HarmonyInstance = Harmony.CreateAndPatchAll(typeof(Hooks));

			HarmonyInstance.Patch(MoreAccType.Assembly.GetType("MoreAccessoriesKOI.ChaControl_SetAccessoryStateAll_Patches").GetMethod("Postfix", AccessTools.all), prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.CharaMakerPreview_Block_Prefix)));
			HarmonyInstance.Patch(MoreAccType.Assembly.GetType("MoreAccessoriesKOI.ChaControl_SetAccessoryStateCategory_Patches").GetMethod("Postfix", AccessTools.all), prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.CharaMakerPreview_Block_Prefix)));

			HarmonyInstance.Patch(typeof(ChaControl).GetMethod("SetAccessoryStateAll", AccessTools.all), prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.CharaMakerPreview_Block_Prefix)));
			HarmonyInstance.Patch(typeof(ChaControl).GetMethod("SetAccessoryStateCategory", AccessTools.all), prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.CharaMakerPreview_Block_Prefix)));
		}

		internal static void HarmonyUnpatch()
		{
			if (HarmonyInstance == null) return;

			HarmonyInstance.UnpatchAll(HarmonyInstance.Id);
			HarmonyInstance = null;
		}

		internal static void UpdateUI()
		{
			AccessTools.Method(MoreAccType, "UpdateUI").Invoke(MoreAccObj, null);
		}

		internal class Hooks
		{
			internal static bool CharaMakerPreview_Block_Prefix()
			{
				if (AccStateSync.CharaMakerPreview.Value)
					return false;
				return true;
			}
		}
	}
}
