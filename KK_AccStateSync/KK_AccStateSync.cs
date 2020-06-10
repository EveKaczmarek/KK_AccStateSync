using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Harmony;
using BepInEx.Logging;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using KKAPI.Maker.UI.Sidebar;
using KKAPI.Studio;

namespace AccStateSync
{
	[BepInPlugin(GUID, Name, Version)]
	[BepInDependency("marco.kkapi")]
	public partial class AccStateSync : BaseUnityPlugin
	{
		public const string Name = "KK_AccStateSync";
		public const string GUID = "madevil.kk.ass";
		public const string Version = "0.0.0.13";

		internal static new ManualLogSource Logger;
#if DEBUG
		internal const LogLevel DebugLogLevel = LogLevel.Info;
#else
		internal const LogLevel DebugLogLevel = LogLevel.Debug;
#endif
		internal static UnityEngine.MonoBehaviour instance;

		internal static ConfigEntry<float> CoroutineSlotChangeDelay { get; set; }
		internal static ConfigEntry<float> CoroutineCounterMax { get; set; }
		internal static ConfigEntry<bool> CharaMakerPreview { get; set; }
		internal static ConfigEntry<bool> StudioUseMoreAccBtn { get; set; }

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

			CoroutineSlotChangeDelay = Config.Bind("Maker", "Slot Change Delay", 1f, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true, Order = 2 }));
			CoroutineCounterMax = Config.Bind("Maker", "Maximun Coroutine Counter", 30f, new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true, Order = 1 }));
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

			MakerAPI.MakerBaseLoaded += MakerAPI_MakerBaseLoaded;
			MakerAPI.MakerFinishedLoading += (object sender, EventArgs e) => CreateMakerInterface();

			AccessoriesApi.SelectedMakerAccSlotChanged += (object sender, AccessorySlotEventArgs eventArgs) => GetController(MakerAPI.GetCharacterControl()).AccSlotChangedHandler(eventArgs.SlotIndex);
			AccessoriesApi.AccessoryTransferred += (object sender, AccessoryTransferEventArgs eventArgs) => GetController(MakerAPI.GetCharacterControl()).AccessoryTransferredHandler(eventArgs.SourceSlotIndex, eventArgs.DestinationSlotIndex);
			AccessoriesApi.AccessoriesCopied += (object sender, AccessoryCopyEventArgs eventArgs) => GetController(MakerAPI.GetCharacterControl()).AccessoriesCopiedHandler((int)eventArgs.CopySource, (int)eventArgs.CopyDestination, eventArgs.CopiedSlotIndexes.ToList());

			MakerAPI.RegisterCustomSubCategories += (sender, e) =>
			{
				CharaMakerPreviewSidebarToggle = e.AddSidebarControl(new SidebarToggle("Force Preview", CharaMakerPreview.Value, this));
				CharaMakerPreviewSidebarToggle.ValueChanged.Subscribe(b => CharaMakerPreview.Value = b);
			};
			MakerAPI.MakerExiting += (sender, e) =>
			{
				CharaMakerPreviewSidebarToggle = null;
			};

			StudioAPI.StudioLoadedChanged += (sender, e) => RegisterStudioControls();

			HarmonyWrapper.PatchAll(typeof(Hooks));

			if (UnityEngine.Application.dataPath.EndsWith("Koikatu_Data"))
				HarmonyWrapper.PatchAll(typeof(HooksHScene));

			if (UnityEngine.Application.dataPath.EndsWith("KoikatuVR_Data"))
				HarmonyWrapper.PatchAll(typeof(HooksVR));

			foreach (var key in Enum.GetValues(typeof(ChaAccessoryDefine.AccessoryParentKey)))
				AccParentNames[key.ToString()] = ChaAccessoryDefine.dictAccessoryParent[(int) key];
		}

		internal static void MakerAPI_MakerBaseLoaded(object sender, RegisterCustomControlsEvent e)
		{
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
