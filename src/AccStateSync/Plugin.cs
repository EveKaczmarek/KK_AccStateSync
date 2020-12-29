using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

using UnityEngine;

using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Studio;
using JetPack;

namespace AccStateSync
{
	[BepInPlugin(GUID, Name, Version)]
	[BepInDependency("marco.kkapi")]
	[BepInDependency("madevil.JetPack", "1.0.1")]
	[BepInDependency("com.joan6694.illusionplugins.moreaccessories")]
	public partial class AccStateSync : BaseUnityPlugin
	{
		public const string Name = "AccStateSync";
		public const string GUID = "madevil.kk.ass";
		public const string Version = "3.1.1.0";

		internal static new ManualLogSource Logger;
		internal static LogLevel DebugLogLevel;
		internal static AccStateSync Instance;

		internal static ConfigEntry<bool> CharaMakerPreview { get; set; }
		internal static ConfigEntry<bool> LogLevelInfo { get; set; }
		internal static ConfigEntry<bool> AutoSaveSetting { get; set; }
		internal static ConfigEntry<bool> PreserveVirtualGroupState { get; set; }
		internal static ConfigEntry<bool> AutoHideSecondary { get; set; }
		internal static ConfigEntry<bool> LegacySaveFormat { get; set; }
		internal static ConfigEntry<bool> VirtualGroupShowName { get; set; }

		private void Start()
		{
			Logger = base.Logger;
			Instance = this;

			MoreAccessories.LoadAssembly();

			AutoSaveSetting = Config.Bind("Maker", "Auto Save Setting", false, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 20 }));
			CharaMakerPreview = Config.Bind("Maker", "CharaMaker Force Preview", true, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 10 }));
			PreserveVirtualGroupState = Config.Bind("Maker", "Preserve Virtual Group State", false, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 0 }));
			LogLevelInfo = Config.Bind("Debug", "LogLevel Info", false, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true, Order = 10 }));
			LegacySaveFormat = Config.Bind("Debug", "Legacy Save Format", true, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true, Order = 5 }));
			AutoHideSecondary = Config.Bind("Debug", "Auto Hide Secondary", true, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true, Order = 0 }));
			VirtualGroupShowName = Config.Bind("Debug", "Virtual Group Show Variable Name", false, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true, Order = 0 }));

			LogLevelInfo.SettingChanged += (sender, args) =>
			{
				DebugLogLevel = LogLevelInfo.Value ? LogLevel.Info : LogLevel.Debug;
			};
			DebugLogLevel = LogLevelInfo.Value ? LogLevel.Info : LogLevel.Debug;

			VirtualGroupShowName.SettingChanged += (sender, args) =>
			{
				if (MakerAPI.InsideMaker)
					GetController(Maker.ChaControl).AccSlotChangedHandler(AccessoriesApi.SelectedMakerAccSlot);
			};

			CharacterApi.RegisterExtraBehaviour<AccStateSyncController>(GUID);

			Harmony.CreateAndPatchAll(typeof(Hooks));

			if (Application.dataPath.EndsWith("KoikatuVR_Data"))
				Harmony.CreateAndPatchAll(typeof(HooksVR));
			else if (Application.dataPath.EndsWith("CharaStudio_Data"))
				StudioAPI.StudioLoadedChanged += (sender, args) => CharaStudio.RegisterControls();
			else
			{
				UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, lsm) =>
				{
					if (s.name == "HProc")
						HooksInstance["HScene"] = Harmony.CreateAndPatchAll(typeof(HooksHScene));
				};
				CharaMaker.RegisterControls();
			}

			Constants.ParseIntoStruct();
		}

		internal sealed class ConfigurationManagerAttributes
		{
			public int? Order;
			public bool? IsAdvanced;
		}
	}
}
