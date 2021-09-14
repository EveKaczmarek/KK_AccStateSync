#if KK
using UnityEngine;
#endif
using ParadoxNotion.Serialization;

using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

using ExtensibleSaveFormat;

using KKAPI;
using KKAPI.Chara;
#if KK
using KKAPI.Studio;
#endif

namespace AccStateSync
{
	[BepInPlugin(GUID, Name, Version)]
	[BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
	[BepInDependency(ExtendedSave.GUID, ExtendedSave.Version)]
	[BepInDependency("madevil.JetPack", JetPack.Core.Version)]
#if KK
	[BepInDependency("com.joan6694.illusionplugins.moreaccessories", "1.1.0")]
#endif
	public partial class AccStateSync : BaseUnityPlugin
	{
		public const string GUID = "madevil.kk.ass";
		public const string Name = "AccStateSync";
		public const string Version = "4.3.3.0";

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
			CharaMaker.RegisterControls();
			Migration.InitCardImport();
#endif
			_hooksInstance["General"].Patch(JetPack.MaterialEditor.Type["MaterialEditorCharaController"].GetMethod("ClothesStateChangeEvent", AccessTools.all), prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.Return_False)));


			JetPack.Chara.OnChangeCoordinateType += (_sender, _args) =>
			{
				AccStateSyncController _pluginCtrl = GetController(_args.ChaControl);
				if (_pluginCtrl == null) return;

				if (_args.State == "Coroutine")
					_pluginCtrl._duringLoadChange = false;
				else
					_pluginCtrl._duringLoadChange = true;
			};

			JetPack.MaterialEditor.OnDataApply += (_sender, _args) =>
			{
				if (_args.State != "Postfix") return;

				AccStateSyncController _pluginCtrl = GetController((_args.Controller as CharaCustomFunctionController).ChaControl);
				if (_pluginCtrl == null) return;

				_pluginCtrl.InitCurOutfitTriggerInfo("OnDataApply");
				if (_pluginCtrl._studioAutoEnable)
				{
					_pluginCtrl._studioAutoEnable = false;
					_pluginCtrl.TriggerEnabled = true;
				}
			};
		}

		internal static void DebugMsg(LogLevel _level, string _meg)
		{
			if (_cfgDebugMode.Value)
				_logger.Log(_level, _meg);
			else
				_logger.Log(LogLevel.Debug, _meg);
		}

		internal static string JsonEncode(object _obj, bool _format = false)
		{
			return JSONSerializer.Serialize(_obj.GetType(), _obj, _format);
		}
	}
}
