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
			public List<string> VirtualGroupNames = new List<string>();

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
				SetExtendedData(ExtendedData);
			}

			protected override void OnCoordinateBeingLoaded(ChaFileCoordinate coordinate)
			{
				Logger.Log(DebugLogLevel, $"[OnCoordinateBeingLoaded][{ChaControl.chaFile.parameter?.fullname}] Fired!!");
				CurOutfitTriggerInfo = null;
				PluginData ExtendedData = GetCoordinateExtendedData(coordinate);
				if (ExtendedData != null && ExtendedData.data.TryGetValue("OutfitTriggerInfo", out var loadedOutfitTriggerInfo) && loadedOutfitTriggerInfo != null)
				{
					bool LoadFlag = true;
					if (MakerAPI.InsideMaker)
					{
						CoordinateLoadFlags LoadFlags = MakerAPI.GetCoordinateLoadFlags();
						if ((!(bool)LoadFlags?.Accessories) && (LoadFlags != null))
							LoadFlag = false;
					}
					if (LoadFlag)
					{
						CharaTriggerInfo[CurrentCoordinateIndex] = MessagePackSerializer.Deserialize<OutfitTriggerInfo>((byte[])loadedOutfitTriggerInfo);
						Logger.Log(DebugLogLevel, $"[OnCoordinateBeingLoaded][{ChaControl.chaFile.parameter?.fullname}] CharaTriggerInfo[{CurrentCoordinateIndex}] loaded from extdata");
					}

					CurOutfitTriggerInfo = CharaTriggerInfo[CurrentCoordinateIndex];
					Logger.Log(DebugLogLevel, $"[OnCoordinateBeingLoaded] CurOutfitTriggerInfo.Parts count: {CurOutfitTriggerInfo.Parts.Count()}");
				}
				InitCurOutfitTriggerInfo();

				base.OnCoordinateBeingLoaded(coordinate);
			}

			protected override void OnCoordinateBeingSaved(ChaFileCoordinate coordinate)
			{
				Logger.Log(DebugLogLevel, $"[OnCoordinateBeingSaved][{ChaControl.chaFile.parameter?.fullname}] Fired!!");
				CurOutfitTriggerInfo = CharaTriggerInfo.ElementAtOrDefault(CurrentCoordinateIndex);
				Logger.Log(DebugLogLevel, $"[OnCoordinateBeingSaved] CurOutfitTriggerInfo.Parts count: {CurOutfitTriggerInfo.Parts.Count()}");
				SyncOutfitTriggerInfo(CurrentCoordinateIndex);
				PluginData ExtendedData = new PluginData();
				ExtendedData.data.Add("OutfitTriggerInfo", MessagePackSerializer.Serialize(CurOutfitTriggerInfo));
				SetCoordinateExtendedData(coordinate, ExtendedData);
			}

			protected override void OnReload(GameMode currentGameMode)
			{
				Logger.Log(DebugLogLevel, "[OnReload] Fired!!");
				TriggerEnabled = false;
				ResetCharaTriggerInfo();

				PluginData ExtendedData = GetExtendedData();
				if (ExtendedData != null && ExtendedData.data.TryGetValue("CharaTriggerInfo", out var loadedCharaTriggerInfo) && loadedCharaTriggerInfo != null)
				{
					if (MakerAPI.InsideMaker)
					{
						CharacterLoadFlags LoadFlags = MakerAPI.GetCharacterLoadFlags();
						if (((bool)LoadFlags?.Clothes) || (LoadFlags == null))
							CharaTriggerInfo = MessagePackSerializer.Deserialize<List<OutfitTriggerInfo>>((byte[])loadedCharaTriggerInfo);
					}
					else
						CharaTriggerInfo = MessagePackSerializer.Deserialize<List<OutfitTriggerInfo>>((byte[])loadedCharaTriggerInfo);

					Logger.Log(DebugLogLevel, $"[OnReload][{ChaControl.chaFile.parameter?.fullname}] CharaTriggerInfo loaded from extdata");
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
//				CurSlotTriggerInfo = new AccTriggerInfo(0);

				FillVirtualGroupStates();

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
