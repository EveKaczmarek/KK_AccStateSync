using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using KKAPI.Maker.UI.Sidebar;
using KKAPI.Studio;

namespace AccStateSync
{
	[BepInPlugin(GUID, Name, Version)]
	[BepInDependency("marco.kkapi")]
	[BepInDependency("com.joan6694.illusionplugins.moreaccessories")]
	public partial class AccStateSync : BaseUnityPlugin
	{
		public const string Name = "KK_AccStateSync";
		public const string GUID = "madevil.kk.ass";
		public const string Version = "2.9.2.0";

		internal static new ManualLogSource Logger;
		internal static LogLevel DebugLogLevel;
		internal static UnityEngine.MonoBehaviour instance;

		internal static ConfigEntry<bool> CharaMakerPreview { get; set; }
		internal static ConfigEntry<bool> StudioUseMoreAccBtn { get; set; }
		internal static ConfigEntry<bool> LogLevelInfo { get; set; }
		internal static ConfigEntry<bool> AutoSaveSetting { get; set; }

		internal static SidebarToggle CharaMakerPreviewSidebarToggle;
		internal static MakerLoadToggle LoadCharaExtdataToggle;
		internal static bool LoadCharaExtdata => LoadCharaExtdataToggle == null || LoadCharaExtdataToggle.Value;
		internal static MakerCoordinateLoadToggle LoadCoordinateExtdataToggle;
		internal static bool LoadCoordinateExtdata => LoadCoordinateExtdataToggle == null || LoadCoordinateExtdataToggle.Value;

		internal static int MenuitemHeightOffsetY = 0;
		internal static int AnchorOffsetMinY = 0;
		internal static int ContainerOffsetMinY = 0;

		internal static int DefaultCustomGroupCount = 0;

		internal void Start()
		{
			Logger = base.Logger;
			instance = this;

			CharacterApi.RegisterExtraBehaviour<AccStateSyncController>(GUID);
			MoreAccessories_Support.LoadAssembly();

			AutoSaveSetting = Config.Bind("Maker", "Auto Save Setting", false, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 20 }));
			CharaMakerPreview = Config.Bind("Maker", "CharaMaker Force Preview", true, new ConfigDescription("", null, new ConfigurationManagerAttributes { Order = 10 }));
			CharaMakerPreview.SettingChanged += (sender, args) =>
			{
				if (MakerAPI.InsideMaker)
				{
					CharaMakerPreviewSidebarToggle.Value = CharaMakerPreview.Value;
					if (CharaMakerPreviewSidebarToggle.Value)
						GetController(MakerAPI.GetCharacterControl()).SyncAllAccToggle();
				}
			};
			LogLevelInfo = Config.Bind("Debug", "LogLevel Info", false, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true, Order = 10 }));
			LogLevelInfo.SettingChanged += (sender, args) =>
			{
				DebugLogLevel = LogLevelInfo.Value ? LogLevel.Info : LogLevel.Debug;
			};
			DebugLogLevel = LogLevelInfo.Value ? LogLevel.Info : LogLevel.Debug;

			AccessoriesApi.SelectedMakerAccSlotChanged += (object sender, AccessorySlotEventArgs eventArgs) => GetController(MakerAPI.GetCharacterControl()).AccSlotChangedHandler(eventArgs.SlotIndex);
			AccessoriesApi.AccessoryTransferred += (object sender, AccessoryTransferEventArgs eventArgs) => GetController(MakerAPI.GetCharacterControl()).AccessoryTransferredHandler(eventArgs.SourceSlotIndex, eventArgs.DestinationSlotIndex);
			AccessoriesApi.AccessoriesCopied += (object sender, AccessoryCopyEventArgs eventArgs) => GetController(MakerAPI.GetCharacterControl()).AccessoriesCopiedHandler((int)eventArgs.CopySource, (int)eventArgs.CopyDestination, eventArgs.CopiedSlotIndexes.ToList());

			MakerAPI.MakerBaseLoaded += MakerAPI_MakerBaseLoaded;
			MakerAPI.MakerFinishedLoading += (sender, e) => CreateMakerInterface();
			MakerAPI.RegisterCustomSubCategories += (sender, e) =>
			{
				CharaMakerPreviewSidebarToggle = e.AddSidebarControl(new SidebarToggle("Force Preview", CharaMakerPreview.Value, this));
				CharaMakerPreviewSidebarToggle.ValueChanged.Subscribe(b => CharaMakerPreview.Value = b);
			};
			MakerAPI.MakerExiting += (sender, e) =>
			{
				HooksInstanceCharaMaker.UnpatchAll(HooksInstanceCharaMaker.Id);
				HooksInstanceCharaMaker = null;
				CharaMakerPreviewSidebarToggle = null;
                LoadCharaExtdataToggle = null;
				LoadCoordinateExtdataToggle = null;
            };

			Harmony.CreateAndPatchAll(typeof(Hooks));

			if (UnityEngine.Application.dataPath.EndsWith("KoikatuVR_Data"))
				Harmony.CreateAndPatchAll(typeof(HooksVR));
			else if (UnityEngine.Application.dataPath.EndsWith("Koikatu_Data"))
			{
				UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, lsm) =>
				{
					if (s.name == "HProc")
						HooksInstanceHScene = Harmony.CreateAndPatchAll(typeof(HooksHScene));
				};
			}
			else if (UnityEngine.Application.dataPath.EndsWith("CharaStudio_Data"))
				StudioAPI.StudioLoadedChanged += (sender, e) => RegisterStudioControls();

			foreach (var key in Enum.GetValues(typeof(ChaAccessoryDefine.AccessoryParentKey)))
				AccParentNames[key.ToString()] = ChaAccessoryDefine.dictAccessoryParent[(int) key];
		}

		internal static void MakerAPI_MakerBaseLoaded(object sender, RegisterCustomControlsEvent e)
		{
			HooksInstanceCharaMaker = Harmony.CreateAndPatchAll(typeof(HooksCharaMaker));
			LoadCharaExtdataToggle = e.AddLoadToggle(new MakerLoadToggle("AccStateSync"));
			LoadCoordinateExtdataToggle = e.AddCoordinateLoadToggle(new MakerCoordinateLoadToggle("AccStateSync"));
		}

		internal static void RegisterStudioControls()
		{
			if (!StudioAPI.InsideStudio) return;

			InsideCharaStudio = true;
			CreateStudioUIPanel();
		}

		internal static Dictionary<string, string> AccParentNames = new Dictionary<string, string>();
		internal static AccStateSyncController GetController(ChaControl chaCtrl) => chaCtrl?.gameObject?.GetComponent<AccStateSyncController>();
	}
}
