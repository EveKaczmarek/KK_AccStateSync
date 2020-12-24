using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.SceneManagement;
using UniRx;
using ChaCustom;

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

using KKAPI.Maker;

namespace JetPack
{
	[BepInPlugin(GUID, Name, Version)]
	[BepInDependency("marco.kkapi")]
	[BepInDependency("com.deathweasel.bepinex.materialeditor")]
	[BepInDependency("com.joan6694.illusionplugins.moreaccessories")]
	public partial class Core : BaseUnityPlugin
	{
		public const string GUID = "madevil.JetPack";
		public const string Name = "JetPack";
		public const string Version = "1.0.0.0";

		internal static new ManualLogSource Logger { get; private set; }
		internal static Core PluginInstance { get; private set; }
		internal static Harmony HarmonyInstance { get; private set; }

		internal static ConfigEntry<bool> CfgDebugMsg { get; set; }

		private void Awake()
		{
			Logger = base.Logger;
			PluginInstance = this;

			CfgDebugMsg = Config.Bind("Debug", "Display debug message", false);
		}

		private void Start()
		{
			Game.HasDarkness = typeof(ChaControl).GetProperties(AccessTools.all).Any(x => x.Name == "exType");

			HarmonyInstance = Harmony.CreateAndPatchAll(typeof(Hooks));
			Accessory.Init();

			if (Application.dataPath.EndsWith("CharaStudio_Data"))
			{
				Studio.Running = true;
				Chara.Init();
				KKAPI.Studio.StudioAPI.StudioLoadedChanged += Studio.RegisterControls;
			}
			else
			{
				Maker.Init();
				MakerAPI.MakerFinishedLoading += Maker.MakerFinishedLoading;
				MakerAPI.MakerExiting += Maker.MakerExiting;
			}
		}

		internal class Hooks { }

		internal static void DebugLog(object data) => DebugLog(LogLevel.Warning, data);
		internal static void DebugLog(LogLevel level, object data)
		{
			if (CfgDebugMsg.Value)
				Logger.Log(level, data);
		}
	}

	public partial class Maker
	{
		internal static void MakerFinishedLoading(object sender, EventArgs args)
		{
			HarmonyInstance = Harmony.CreateAndPatchAll(typeof(Hooks));

			int OnCustomSelectListClickCount = OnCustomSelectListClick?.GetInvocationList()?.Length ?? 0;
			if (OnCustomSelectListClickCount > 0)
			{
				Core.DebugLog($"[MakerFinishedLoading][(OnCustomSelectListClick: {OnCustomSelectListClickCount}]");
				HarmonyInstance.PatchAll(typeof(HooksCustomSelectListCtrl));
			}

			int OnPointerEnterCount = OnPointerEnter?.GetInvocationList()?.Length ?? 0;
			int OnPointerExitCount = OnPointerExit?.GetInvocationList()?.Length ?? 0;
			if (OnPointerEnterCount + OnPointerExitCount > 0)
			{
				Core.DebugLog($"[MakerFinishedLoading][(OnPointerEnter + OnPointerExit: {OnPointerEnterCount + OnPointerExitCount}]");
				HarmonyInstance.PatchAll(typeof(HooksSelectable));
			}

			//CustomChangeMainMenu CustomChangeMainMenu = GameObject.FindObjectsOfType<CustomChangeMainMenu>().FirstOrDefault();
			CustomChangeMainMenu CustomChangeMainMenu = Singleton<CustomChangeMainMenu>.Instance;
			CvsNavMenuInit(CustomChangeMainMenu);
		}

		internal static void MakerExiting(object sender, EventArgs args)
		{
			HarmonyInstance.UnpatchAll(HarmonyInstance.Id);
			HarmonyInstance = null;
		}
	}

	public class Game
	{
		public static bool HasDarkness = false;
	}

	public partial class Toolbox
	{
		public static T MessagepackClone<T>(T sourceObj)
		{
			byte[] bytes = MessagePack.MessagePackSerializer.Serialize(sourceObj);
			return MessagePack.MessagePackSerializer.Deserialize<T>(bytes);
		}
	}
}
