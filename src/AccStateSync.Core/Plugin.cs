using UnityEngine;

using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

using ExtensibleSaveFormat;

using KKAPI;
using KKAPI.Chara;
using KKAPI.Studio;

namespace AccStateSync
{
	[BepInPlugin(GUID, Name, Version)]
	[BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
	[BepInDependency(ExtendedSave.GUID, ExtendedSave.Version)]
	[BepInDependency("madevil.JetPack", JetPack.Core.Version)]
#if KK
	[BepInDependency("com.joan6694.illusionplugins.moreaccessories", "1.1.0")]
#endif
	[BepInIncompatibility("KK_ClothesLoadOption")]
#if !DEBUG
	[BepInIncompatibility("com.jim60105.kk.studiocoordinateloadoption")]
	[BepInIncompatibility("com.jim60105.kk.coordinateloadoption")]
#endif
	public partial class AccStateSync : BaseUnityPlugin
	{
		public const string GUID = "madevil.kk.ass";
		public const string Name = "AccStateSync";
		public const string Version = "4.4.0.0";

		internal static ManualLogSource _logger;
		internal static AccStateSync _instance;

		private void Start()
		{
			_logger = base.Logger;
			_instance = this;

			InitConfig();
			InitConstants();

			MoreAccessories.Init();
			GenuineDetectorInit();
			CharacterApi.RegisterExtraBehaviour<AccStateSyncController>(GUID);

			_hooksInstance["General"] = Harmony.CreateAndPatchAll(typeof(Hooks));
#if KK
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
#elif KKS
			if (Application.dataPath.EndsWith("CharaStudio_Data"))
				StudioAPI.StudioLoadedChanged += (_sender, _args) => CharaStudio.RegisterControls();
			else
			{
				Migration.InitCardImport();
				CharaHscene.RegisterEvents();
				CharaMaker.RegisterControls();
			}
#endif
			_hooksInstance["General"].Patch(JetPack.MaterialEditor.Type["MaterialEditorCharaController"].GetMethod("ClothesStateChangeEvent", AccessTools.all), prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.Return_False)));

			JetPack.Chara.OnChangeCoordinateType += (_sender, _args) =>
			{
				AccStateSyncController _pluginCtrl = GetController(_args.ChaControl);
				if (_pluginCtrl == null) return;

				if (_args.State == "Coroutine")
					_pluginCtrl._duringCordChange = false;
				else
					_pluginCtrl._duringCordChange = true;
			};

			JetPack.MaterialEditor.OnDataApply += (_sender, _args) =>
			{
				if (_args.State != "Postfix") return;

				AccStateSyncController _pluginCtrl = GetController((_args.Controller as CharaCustomFunctionController).ChaControl);
				if (_pluginCtrl == null) return;

				_pluginCtrl.InitCurOutfitTriggerInfo("OnDataApply");
			};
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
