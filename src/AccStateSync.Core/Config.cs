using BepInEx;
using BepInEx.Configuration;

using KKAPI.Utilities;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		internal static ConfigEntry<bool> _cfgDebugMode;
		internal static ConfigEntry<string> _cfgExportPath;

		internal static ConfigEntry<bool> _cfgCharaMakerPreview;
		internal static ConfigEntry<bool> _cfgMakerWinEnable;
		internal static ConfigEntry<bool> _cfgDragPass;
		internal static ConfigEntry<float> _cfgMakerWinX;
		internal static ConfigEntry<float> _cfgMakerWinY;
		internal static ConfigEntry<float> _cfgMakerWinScale;
		internal static ConfigEntry<bool> _cfgMakerWinResScale;

		internal static ConfigEntry<float> _cfgStudioWinX;
		internal static ConfigEntry<float> _cfgStudioWinY;
		internal static ConfigEntry<float> _cfgStudioWinScale;
		internal static ConfigEntry<bool> _cfgStudioWinResScale;
		internal static ConfigEntry<bool> _cfgStudioAutoEnable;

		internal void InitConfig()
		{
			_cfgMakerWinEnable = Config.Bind("Maker", "Config Window Startup Enable", false, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 20 }));
			_cfgDragPass = Config.Bind("Maker", "Drag Pass Mode", false, new ConfigDescription("Setting window will not block mouse dragging", null, new ConfigurationManagerAttributes { Order = 15 }));

			_cfgMakerWinX = Config.Bind("Maker", "Config Window Startup X", 525f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 19 }));
			_cfgMakerWinX.SettingChanged += (_sender, _args) =>
			{
				if (_charaConfigWindow == null) return;
				if (_charaConfigWindow._windowPos.x != _cfgMakerWinX.Value)
				{
					_charaConfigWindow._windowPos.x = _cfgMakerWinX.Value;
				}
			};
			_cfgMakerWinY = Config.Bind("Maker", "Config Window Startup Y", 460f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 18 }));
			_cfgMakerWinY.SettingChanged += (_sender, _args) =>
			{
				if (_charaConfigWindow == null) return;
				if (_charaConfigWindow._windowPos.y != _cfgMakerWinY.Value)
				{
					_charaConfigWindow._windowPos.y = _cfgMakerWinY.Value;
				}
			};
			_cfgMakerWinResScale = Config.Bind("Maker", "Config Window Resolution Adjust", false, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 17 }));
			_cfgMakerWinResScale.SettingChanged += (_sender, _args) =>
			{
				if (_charaConfigWindow == null) return;
				_charaConfigWindow.ChangeRes();
			};
			_cfgMakerWinScale = Config.Bind("Maker", "Config Window Scale", 1f, new ConfigDescription("", new AcceptableValueList<float>(0.5f, 0.75f, 1f, 1.25f, 1.75f, 2f), new ConfigurationManagerAttributes { Order = 16 }));
			_cfgMakerWinScale.SettingChanged += (_sender, _args) =>
			{
				if (_charaConfigWindow == null) return;
				_charaConfigWindow._cfgScaleFactor = _cfgMakerWinScale.Value;
				_charaConfigWindow.ChangeRes();
			};

			_cfgCharaMakerPreview = Config.Bind("Maker", "CharaMaker Force Preview", true, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 10 }));

			_cfgExportPath = Config.Bind("Debug", "Export Path", Paths.ConfigPath, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 9 }));

			_cfgDebugMode = Config.Bind("Debug", "Debug Mode", false, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true, Order = 20 }));

			_cfgStudioWinX = Config.Bind("Studio", "Config Window Startup X", 525f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 19 }));
			_cfgStudioWinX.SettingChanged += (_sender, _args) =>
			{
				if (_charaConfigWindow == null) return;
				if (_charaConfigWindow._windowPos.x != _cfgStudioWinX.Value)
				{
					_charaConfigWindow._windowPos.x = _cfgStudioWinX.Value;
				}
			};
			_cfgStudioWinY = Config.Bind("Studio", "Config Window Startup Y", 460f, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 18 }));
			_cfgStudioWinY.SettingChanged += (_sender, _args) =>
			{
				if (_charaConfigWindow == null) return;
				if (_charaConfigWindow._windowPos.y != _cfgStudioWinY.Value)
				{
					_charaConfigWindow._windowPos.y = _cfgStudioWinY.Value;
				}
			};
			_cfgStudioWinResScale = Config.Bind("Studio", "Config Window Resolution Adjust", false, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 17 }));
			_cfgStudioWinResScale.SettingChanged += (_sender, _args) =>
			{
				if (_charaConfigWindow == null) return;
				_charaConfigWindow.ChangeRes();
			};
			_cfgStudioWinScale = Config.Bind("Studio", "Config Window Scale", 1f, new ConfigDescription("", new AcceptableValueList<float>(0.5f, 0.75f, 1f, 1.25f, 1.75f, 2f), new ConfigurationManagerAttributes { Order = 16 }));
			_cfgStudioWinScale.SettingChanged += (_sender, _args) =>
			{
				if (_charaConfigWindow == null) return;
				_charaConfigWindow._cfgScaleFactor = _cfgStudioWinScale.Value;
				_charaConfigWindow.ChangeRes();
			};

			_cfgStudioAutoEnable = Config.Bind("Studio", "Auto Enable After Load", false, new ConfigDescription("Automatically enable after scene or character load", null, new ConfigurationManagerAttributes { Order = 1 }));
		}
	}
}
