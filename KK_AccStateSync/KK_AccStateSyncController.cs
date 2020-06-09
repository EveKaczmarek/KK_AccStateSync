using System.Collections.Generic;
using System.Linq;
using UniRx;
using ExtensibleSaveFormat;
using MessagePack;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		public partial class AccStateSyncController : CharaCustomFunctionController
		{
			public int CurrentCoordinateIndex => ChaControl.fileStatus.coordinateType;
			public List<OutfitTriggerInfo> CharaTriggerInfo = null;
			public OutfitTriggerInfo CurOutfitTriggerInfo;
			public AccTriggerInfo CurSlotTriggerInfo = new AccTriggerInfo(0);

			public bool TriggerEnabled = false;

			public Dictionary<string, bool> VirtualGroupStates = new Dictionary<string, bool>();

			public List<Dictionary<string, string>> CharaVirtualGroupNames = new List<Dictionary<string, string>>();
			public Dictionary<string, string> CurOutfitVirtualGroupNames = new Dictionary<string, string>();

			public List<string> GameObjectNames = new List<string>();

			public float CoroutineCounter = 0;

			public List<ChaFileAccessory.PartsInfo> CharaAccInfo => MoreAccessories_Support.GetAccessoriesInfos(ChaControl);

			protected override void Start()
			{
				Logger.Log(DebugLogLevel, "[Start] Fired!!");
				ResetCharaTriggerInfo();
				CurrentCoordinate.Subscribe( value => { OnCoordinateChanged(); } );
				base.Start();
			}

			internal void OnCoordinateChanged()
			{
				Logger.Log(DebugLogLevel, $"[OnCoordinateChanged][{ChaControl.chaFile.parameter?.fullname}] CurrentCoordinateIndex: {CurrentCoordinateIndex}");
				InitCurOutfitTriggerInfo();
			}

			protected override void OnCardBeingSaved(GameMode currentGameMode)
			{
				Logger.Log(DebugLogLevel, $"[OnCardBeingSaved][{ChaControl.chaFile.parameter?.fullname}] Fired!!");
				SyncCharaTriggerInfo();
				PluginData ExtendedData = new PluginData();
				ExtendedData.data.Add("CharaTriggerInfo", MessagePackSerializer.Serialize(CharaTriggerInfo));
				ExtendedData.data.Add("CharaVirtualGroupNames", MessagePackSerializer.Serialize(CharaVirtualGroupNames));
				SetExtendedData(ExtendedData);
			}

			protected override void OnCoordinateBeingLoaded(ChaFileCoordinate coordinate)
			{
				Logger.Log(DebugLogLevel, $"[OnCoordinateBeingLoaded][{ChaControl.chaFile.parameter?.fullname}] Fired!!");
				CharaTriggerInfo[CurrentCoordinateIndex] = new OutfitTriggerInfo(CurrentCoordinateIndex);
				CharaVirtualGroupNames[CurrentCoordinateIndex] = new Dictionary<string, string>();
				PluginData ExtendedData = GetCoordinateExtendedData(coordinate);
				if (ExtendedData != null && ExtendedData.data.TryGetValue("OutfitTriggerInfo", out var loadedOutfitTriggerInfo) && loadedOutfitTriggerInfo != null)
				{
					if (!MakerAPI.InsideMaker || (MakerAPI.InsideMaker && LoadCoordinateExtdata))
					{
						CharaTriggerInfo[CurrentCoordinateIndex] = MessagePackSerializer.Deserialize<OutfitTriggerInfo>((byte[])loadedOutfitTriggerInfo);
						Logger.Log(DebugLogLevel, $"[OnCoordinateBeingLoaded][{ChaControl.chaFile.parameter?.fullname}] CharaTriggerInfo[{CurrentCoordinateIndex}] loaded from extdata");

						if (ExtendedData.data.TryGetValue("OutfitVirtualGroupNames", out var loadedOutfitVirtualGroupNames) && loadedOutfitVirtualGroupNames != null)
						{
							CharaVirtualGroupNames[CurrentCoordinateIndex] = MessagePackSerializer.Deserialize<Dictionary<string, string>>((byte[])loadedOutfitVirtualGroupNames);
							Logger.Log(DebugLogLevel, $"[OnCoordinateBeingLoaded][{ChaControl.chaFile.parameter?.fullname}] CharaVirtualGroupNames[{CurrentCoordinateIndex}] loaded from extdata");
						}
					}

					Logger.Log(DebugLogLevel, $"[OnCoordinateBeingLoaded] CurOutfitTriggerInfo.Parts count: {CurOutfitTriggerInfo.Parts.Count()}");
				}
				CurOutfitTriggerInfo = CharaTriggerInfo[CurrentCoordinateIndex];
				CurOutfitVirtualGroupNames = CharaVirtualGroupNames[CurrentCoordinateIndex];
				InitCurOutfitTriggerInfo();

				base.OnCoordinateBeingLoaded(coordinate);
			}

			protected override void OnCoordinateBeingSaved(ChaFileCoordinate coordinate)
			{
				Logger.Log(DebugLogLevel, $"[OnCoordinateBeingSaved][{ChaControl.chaFile.parameter?.fullname}] Fired!!");
				PluginData ExtendedData = new PluginData();

				CurOutfitTriggerInfo = CharaTriggerInfo.ElementAtOrDefault(CurrentCoordinateIndex);
				Logger.Log(DebugLogLevel, $"[OnCoordinateBeingSaved] CurOutfitTriggerInfo.Parts count: {CurOutfitTriggerInfo.Parts.Count()}");
				SyncOutfitTriggerInfo(CurrentCoordinateIndex);
				ExtendedData.data.Add("OutfitTriggerInfo", MessagePackSerializer.Serialize(CurOutfitTriggerInfo));

				CurOutfitVirtualGroupNames = CharaVirtualGroupNames.ElementAtOrDefault(CurrentCoordinateIndex);
				ExtendedData.data.Add("OutfitVirtualGroupNames", MessagePackSerializer.Serialize(CurOutfitVirtualGroupNames));

				SetCoordinateExtendedData(coordinate, ExtendedData);
			}

			protected override void OnReload(GameMode currentGameMode)
			{
				Logger.Log(DebugLogLevel, "[OnReload] Fired!!");
				TriggerEnabled = false;
				ResetCharaTriggerInfo();
				ResetCharaVirtualGroupNames();

				PluginData ExtendedData = GetExtendedData();
				if (ExtendedData != null && ExtendedData.data.TryGetValue("CharaTriggerInfo", out var loadedCharaTriggerInfo) && loadedCharaTriggerInfo != null)
				{
					if (!MakerAPI.InsideMaker || (MakerAPI.InsideMaker && LoadCharaExtdata))
					{
						CharaTriggerInfo = MessagePackSerializer.Deserialize<List<OutfitTriggerInfo>>((byte[])loadedCharaTriggerInfo);
						Logger.Log(DebugLogLevel, $"[OnReload][{ChaControl.chaFile.parameter?.fullname}] CharaTriggerInfo loaded from extdata");

						if (ExtendedData.data.TryGetValue("CharaVirtualGroupNames", out var loadedCharaVirtualGroupNames) && loadedCharaVirtualGroupNames != null)
						{
							CharaVirtualGroupNames = MessagePackSerializer.Deserialize<List<Dictionary<string, string>>>((byte[])loadedCharaVirtualGroupNames);
							Logger.Log(DebugLogLevel, $"[OnReload][{ChaControl.chaFile.parameter?.fullname}] CharaVirtualGroupNames loaded from extdata");
						}
					}
				}
				InitCurOutfitTriggerInfo();

				base.OnReload(currentGameMode);
			}

			internal void InitCurOutfitTriggerInfo()
			{
				Logger.Log(DebugLogLevel, $"[InitCurOutfitTriggerInfo][{ChaControl.chaFile.parameter?.fullname}] Fired!!");

				TriggerEnabled = false;

				CurOutfitTriggerInfo = CharaTriggerInfo?.ElementAtOrDefault(CurrentCoordinateIndex);
				if (CurOutfitTriggerInfo == null)
				{
					Logger.Log(DebugLogLevel, "[InitCurOutfitTriggerInfo] CurOutfitTriggerInfo is Null");
					return;
				}

				if (MakerAPI.InsideMaker)
					CheckOutfitTriggerInfoCount(CurrentCoordinateIndex);

				if (CurOutfitTriggerInfo.Parts.Count() < 20)
				{
					Logger.Log(DebugLogLevel, $"[InitOutfitTriggerInfo][{ChaControl.chaFile.parameter?.fullname}] TriggerEnabled false");
					return;
				}

				TriggerEnabled = true;
				VirtualGroupStates.Clear();

				FillVirtualGroupStates();

				CurOutfitVirtualGroupNames = CharaVirtualGroupNames?.ElementAtOrDefault(CurrentCoordinateIndex);
				FillVirtualGroupNames();

				if (MakerAPI.InsideMaker)
				{
					if (AccessoriesApi.SelectedMakerAccSlot > CurOutfitTriggerInfo.Parts.Count())
						AccSlotChangedHandler(0, true);
					else
						AccSlotChangedHandler(AccessoriesApi.SelectedMakerAccSlot, true);
				}
				else if (KKAPI.Studio.StudioAPI.InsideStudio)
				{
					UpdateStudioUI();
					SyncAllAccToggle();
				}
				else
				{
					UpdateHUI();
					SyncAllAccToggle();
				}
			}
		}
	}
}
