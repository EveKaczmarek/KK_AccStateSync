using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

using UnityEngine;

using KKAPI.Chara;
using KKAPI.Studio;
using KKAPI.Utilities;

namespace AccStateSync
{
	[BepInPlugin(GUID, Name, Version)]
	[BepInDependency("marco.kkapi")]
	[BepInDependency("madevil.JetPack")]
	[BepInDependency("com.joan6694.illusionplugins.moreaccessories")]
	public partial class AccStateSync : BaseUnityPlugin
	{
		public const string GUID = "madevil.kk.ass";
		public const string Name = "AccStateSync (JetPack)";
		public const string Version = "3.4.2.0";

		internal static ManualLogSource _logger;
		internal static AccStateSync _instance;

		internal static ConfigEntry<bool> _cfgCharaMakerPreview;
		internal static ConfigEntry<bool> _cfgDebugMode;
		internal static ConfigEntry<bool> _cfgPreserveVirtualGroupState;
		internal static ConfigEntry<bool> _cfgAutoHideSecondary;
		internal static ConfigEntry<bool> _cfgLegacySaveFormat;
		internal static ConfigEntry<bool> _cfgVirtualGroupShowName;

		internal static ConfigEntry<bool> _cfgMakerWinEnable;
		internal static ConfigEntry<float> _cfgMakerWinX;
		internal static ConfigEntry<float> _cfgMakerWinY;
		internal static ConfigEntry<float> _cfgMakerWinScale;
		internal static ConfigEntry<bool> _cfgMakerWinResScale;

		private void Start()
		{
			_logger = base.Logger;
			_instance = this;

			MoreAccessories.Init();

			_cfgMakerWinEnable = Config.Bind("Maker", "Config Window Startup Enable", false, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 20 }));
			_cfgMakerWinX = Config.Bind("Maker", "Config Window Startup X", 525f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 19 }));
			_cfgMakerWinX.SettingChanged += (_sender, _args) =>
			{
				if (_makerConfigWindow == null) return;
				if (_makerConfigWindow._windowPos.x != _cfgMakerWinX.Value)
				{
					_makerConfigWindow._windowPos.x = _cfgMakerWinX.Value;
				}
			};
			_cfgMakerWinY = Config.Bind("Maker", "Config Window Startup Y", 460f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 18 }));
			_cfgMakerWinY.SettingChanged += (_sender, _args) =>
			{
				if (_makerConfigWindow == null) return;
				if (_makerConfigWindow._windowPos.y != _cfgMakerWinY.Value)
				{
					_makerConfigWindow._windowPos.y = _cfgMakerWinY.Value;
				}
			};
			_cfgMakerWinResScale = Config.Bind("Maker", "Config Window Resolution Adjust", false, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 17 }));
			_cfgMakerWinResScale.SettingChanged += (_sender, _args) =>
			{
				if (_makerConfigWindow == null) return;
				_makerConfigWindow.ChangeRes();
			};
			_cfgMakerWinScale = Config.Bind("Maker", "Config Window Scale", 1f, new ConfigDescription("", new AcceptableValueList<float>(0.5f, 0.75f, 1f, 1.25f, 1.75f, 2f), new ConfigurationManagerAttributes { Order = 16 }));
			_cfgMakerWinScale.SettingChanged += (_sender, _args) =>
			{
				if (_makerConfigWindow == null) return;
				_makerConfigWindow._cfgScaleFactor = _cfgMakerWinScale.Value;
				_makerConfigWindow.ChangeRes();
			};

			_cfgCharaMakerPreview = Config.Bind("Maker", "CharaMaker Force Preview", true, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 10 }));
			_cfgPreserveVirtualGroupState = Config.Bind("Maker", "Preserve Virtual Group State", false, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 0 }));

			_cfgDebugMode = Config.Bind("Debug", "Debug Mode", false, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true, Order = 10 }));
			_cfgLegacySaveFormat = Config.Bind("Debug", "Legacy Save Format", true, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true, Order = 5 }));
			_cfgAutoHideSecondary = Config.Bind("Debug", "Auto Hide Secondary", true, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true, Order = 0 }));
			_cfgVirtualGroupShowName = Config.Bind("Debug", "Virtual Group Show Variable Name", false, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true, Order = 0 }));

			_cfgVirtualGroupShowName.SettingChanged += (_sender, _args) =>
			{
				if (JetPack.CharaMaker.Inside)
					GetController(CharaMaker._chaCtrl).AccSlotChangedHandler(CharaMaker._currentSlotIndex);
			};

			CharacterApi.RegisterExtraBehaviour<AccStateSyncController>(GUID);

			Harmony.CreateAndPatchAll(typeof(Hooks));

			if (Application.dataPath.EndsWith("CharaStudio_Data"))
				StudioAPI.StudioLoadedChanged += (_sender, _args) => CharaStudio.RegisterControls();
			else
			{
				{
					BaseUnityPlugin _instance = JetPack.Toolbox.GetPluginInstance("madevil.kk.MovUrAcc");
					if (_instance != null && !JetPack.Toolbox.PluginVersionCompare(_instance, "1.6.0.0"))
						_logger.LogError($"MovUrAcc 1.6+ is required to work properly, version {_instance.Info.Metadata.Version} detected");
				}
				CharaHscene.RegisterEvents();
				CharaMaker.RegisterControls();
			}

			Constants.ParseIntoStruct();
		}

		internal static void DebugMsg(LogLevel _level, string _msg)
		{
			if (_cfgDebugMode.Value)
				_logger.Log(_level, _msg);
			else
				_logger.Log(LogLevel.Debug, _msg);
		}
	}
}
