using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

using UnityEngine;

using KKAPI.Chara;
using KKAPI.Studio;

namespace AccStateSync
{
	[BepInPlugin(GUID, Name, Version)]
	[BepInDependency("marco.kkapi", "1.17")]
	[BepInDependency("madevil.JetPack", JetPack.Core.Version)]
	[BepInDependency("com.joan6694.illusionplugins.moreaccessories", "1.0.9")]
	public partial class AccStateSync : BaseUnityPlugin
	{
		public const string GUID = "madevil.kk.ass";
		public const string Name = "AccStateSync (JetPack)";
		public const string Version = "4.0.0.0";

		internal static ManualLogSource _logger;
		internal static AccStateSync _instance;

		private void Start()
		{
			_logger = base.Logger;
			_instance = this;

			InitConfig();
			InitConstants();

			MoreAccessories.Init();

			CharacterApi.RegisterExtraBehaviour<AccStateSyncController>(GUID);

			Harmony.CreateAndPatchAll(typeof(Hooks));

			if (Application.dataPath.EndsWith("CharaStudio_Data"))
				StudioAPI.StudioLoadedChanged += (_sender, _args) => CharaStudio.RegisterControls();
			else
			{
				{
					BaseUnityPlugin _instance = JetPack.Toolbox.GetPluginInstance("madevil.kk.MovUrAcc");
					if (_instance != null && !JetPack.Toolbox.PluginVersionCompare(_instance, "1.7.0.0"))
						_logger.LogError($"MovUrAcc 1.7+ is required to work properly, version {_instance.Info.Metadata.Version} detected");
				}
				{
					BaseUnityPlugin _instance = JetPack.Toolbox.GetPluginInstance("madevil.kk.ca");
					if (_instance != null && !JetPack.Toolbox.PluginVersionCompare(_instance, "1.2.0.0"))
						_logger.LogError($"Character Accessory 1.2+ is required to work properly, version {_instance.Info.Metadata.Version} detected");
				}
				CharaHscene.RegisterEvents();
				CharaMaker.RegisterControls();
			}
		}

		internal static void DebugMsg(LogLevel _level, string _meg)
		{
			if (_cfgDebugMode.Value)
				_logger.Log(_level, _meg);
			else
				_logger.Log(LogLevel.Debug, _meg);
		}
	}
}
