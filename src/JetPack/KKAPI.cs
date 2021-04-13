using System;
using System.Reflection;

using UnityEngine;

using BepInEx;
using HarmonyLib;

namespace JetPack
{
	public class KKAPI
	{
		private static BaseUnityPlugin _instance = null;
		private static Type _makerAPI = null;

		internal static void Init()
		{
			_instance = Toolbox.GetPluginInstance("marco.kkapi");
			_makerAPI = _instance.GetType().Assembly.GetType("KKAPI.Maker.MakerAPI");
			Hooks.Init();
		}

		internal class Hooks
		{
			private static Harmony _hookInstance;

			internal static void Init() => _hookInstance = Harmony.CreateAndPatchAll(typeof(Hooks));

			internal static void OnMakerStartLoadingPatch()
			{
				Core.DebugLog($"[KKAPI.Hooks.OnMakerStartLoadingPatch]");
				_hookInstance.Patch(_makerAPI.GetMethod("OnMakerBaseLoaded", AccessTools.all), postfix: new HarmonyMethod(typeof(Hooks), nameof(KKAPI_MakerAPI_OnMakerBaseLoaded_Postfix)));
				_hookInstance.Patch(_makerAPI.GetMethod("OnMakerFinishedLoading", AccessTools.all), postfix: new HarmonyMethod(typeof(Hooks), nameof(KKAPI_MakerAPI_OnMakerFinishedLoading_Postfix)));
			}

			private static void KKAPI_MakerAPI_OnMakerBaseLoaded_Postfix()
			{
				CharaMaker.InvokeOnMakerBaseLoaded(null, null);
				_hookInstance.Unpatch(_makerAPI.GetMethod("OnMakerBaseLoaded", AccessTools.all), HarmonyPatchType.Postfix, _hookInstance.Id);
			}

			private static void KKAPI_MakerAPI_OnMakerFinishedLoading_Postfix()
			{
				CharaMaker.InvokeOnMakerFinishedLoading(null, null);
				_hookInstance.Unpatch(_makerAPI.GetMethod("OnMakerFinishedLoading", AccessTools.all), HarmonyPatchType.Postfix, _hookInstance.Id);
			}
		}
	}
}
