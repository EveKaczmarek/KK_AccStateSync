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
			public Dictionary<int, OutfitTriggerInfo> CharaTriggerInfo = new Dictionary<int, OutfitTriggerInfo>();
			public OutfitTriggerInfo CurOutfitTriggerInfo;
			public AccTriggerInfo CurSlotTriggerInfo = new AccTriggerInfo(0);

			public bool TriggerEnabled = false;
			public bool SkipAutoSave = false;

			public Dictionary<string, bool> VirtualGroupStates = new Dictionary<string, bool>();

			public Dictionary<int, Dictionary<string, string>> CharaVirtualGroupNames = new Dictionary<int, Dictionary<string, string>>();
			public Dictionary<string, string> CurOutfitVirtualGroupNames = new Dictionary<string, string>();

			public float CoroutineCounter = 0;

			public List<ChaFileAccessory.PartsInfo> CharaAccInfo => MoreAccessories_Support.GetAccessoriesInfos(ChaControl);

			protected override void Start()
			{
				Logger.Log(DebugLogLevel, "[Start] Fired!!");
				ResetCharaTriggerInfo();
				ResetCharaVirtualGroupNames();
				CurrentCoordinate.Subscribe( value => { OnCoordinateChanged(); } );
				base.Start();
			}

			internal void OnCoordinateChanged()
			{
				Logger.Log(DebugLogLevel, $"[OnCoordinateChanged][{ChaControl.chaFile.parameter?.fullname}] CurrentCoordinateIndex: {CurrentCoordinateIndex}");
				SkipAutoSave = true;
				InitCurOutfitTriggerInfo();
			}

			protected override void OnCoordinateBeingSaved(ChaFileCoordinate coordinate)
			{
				Logger.Log(DebugLogLevel, $"[OnCoordinateBeingSaved][{ChaControl.chaFile.parameter?.fullname}] Fired!!");
				PluginData ExtendedData = new PluginData();
				SyncOutfitTriggerInfo(CurrentCoordinateIndex);
				ExtendedData.data.Add("OutfitTriggerInfo", MessagePackSerializer.Serialize(CharaTriggerInfo[CurrentCoordinateIndex]));
				ExtendedData.data.Add("OutfitVirtualGroupNames", MessagePackSerializer.Serialize(CharaVirtualGroupNames[CurrentCoordinateIndex]));
				ExtendedData.version = 2;
				SetCoordinateExtendedData(coordinate, ExtendedData);
			}

			protected override void OnCardBeingSaved(GameMode currentGameMode)
			{
				Logger.Log(DebugLogLevel, $"[OnCardBeingSaved][{ChaControl.chaFile.parameter?.fullname}] Fired!!");
				SyncCharaTriggerInfo();
				PluginData ExtendedData = new PluginData();
				ExtendedData.data.Add("CharaTriggerInfo", MessagePackSerializer.Serialize(CharaTriggerInfo));
				ExtendedData.data.Add("CharaVirtualGroupNames", MessagePackSerializer.Serialize(CharaVirtualGroupNames));
				ExtendedData.version = 2;
				SetExtendedData(ExtendedData);
			}

			protected override void OnCoordinateBeingLoaded(ChaFileCoordinate coordinate)
			{
				Logger.Log(DebugLogLevel, $"[OnCoordinateBeingLoaded][{ChaControl.chaFile.parameter?.fullname}] Fired!!");
				if (!MakerAPI.InsideMaker || (MakerAPI.InsideMaker && LoadCoordinateExtdata))
				{
					CharaTriggerInfo[CurrentCoordinateIndex] = new OutfitTriggerInfo(CurrentCoordinateIndex);
					CharaVirtualGroupNames[CurrentCoordinateIndex] = new Dictionary<string, string>();
					PluginData ExtendedData = GetCoordinateExtendedData(coordinate);
					if (ExtendedData != null && ExtendedData.data.TryGetValue("OutfitTriggerInfo", out var loadedOutfitTriggerInfo) && loadedOutfitTriggerInfo != null)
					{
						if (ExtendedData.version < 2)
						{
							OutfitTriggerInfoV1 OldCharaTriggerInfo = MessagePackSerializer.Deserialize<OutfitTriggerInfoV1>((byte[])loadedOutfitTriggerInfo);
							CharaTriggerInfo[CurrentCoordinateIndex] = UpgradeOutfitTriggerInfoV1(OldCharaTriggerInfo);
						}
						else
							CharaTriggerInfo[CurrentCoordinateIndex] = MessagePackSerializer.Deserialize<OutfitTriggerInfo>((byte[])loadedOutfitTriggerInfo);
						Logger.Log(DebugLogLevel, $"[OnCoordinateBeingLoaded][{ChaControl.chaFile.parameter?.fullname}] CharaTriggerInfo[{CurrentCoordinateIndex}] loaded from extdata");

						if (ExtendedData.data.TryGetValue("OutfitVirtualGroupNames", out var loadedOutfitVirtualGroupNames) && loadedOutfitVirtualGroupNames != null)
						{
							CharaVirtualGroupNames[CurrentCoordinateIndex] = MessagePackSerializer.Deserialize<Dictionary<string, string>>((byte[])loadedOutfitVirtualGroupNames);
							Logger.Log(DebugLogLevel, $"[OnCoordinateBeingLoaded][{ChaControl.chaFile.parameter?.fullname}] CharaVirtualGroupNames[{CurrentCoordinateIndex}] loaded from extdata");
						}
					}
				}
				InitCurOutfitTriggerInfo();

				base.OnCoordinateBeingLoaded(coordinate);
			}

			protected override void OnReload(GameMode currentGameMode)
			{
				Logger.Log(DebugLogLevel, "[OnReload] Fired!!");
				TriggerEnabled = false;
				if (!MakerAPI.InsideMaker || (MakerAPI.InsideMaker && LoadCharaExtdata))
				{
					ResetCharaTriggerInfo();
					ResetCharaVirtualGroupNames();
					PluginData ExtendedData = GetExtendedData();
					if (ExtendedData != null && ExtendedData.data.TryGetValue("CharaTriggerInfo", out var loadedCharaTriggerInfo) && loadedCharaTriggerInfo != null)
					{
						Logger.Log(DebugLogLevel, $"[OnReload][{ChaControl.chaFile.parameter?.fullname}] ExtendedData.version: {ExtendedData.version}");

						if (ExtendedData.version < 2)
						{
							List<OutfitTriggerInfoV1> OldCharaTriggerInfo = MessagePackSerializer.Deserialize<List<OutfitTriggerInfoV1>>((byte[])loadedCharaTriggerInfo);
							for (int i = 0; i < 7; i++)
								CharaTriggerInfo[i] = UpgradeOutfitTriggerInfoV1(OldCharaTriggerInfo[i]);
						}
						else
							CharaTriggerInfo = MessagePackSerializer.Deserialize<Dictionary<int, OutfitTriggerInfo>>((byte[])loadedCharaTriggerInfo);
						Logger.Log(DebugLogLevel, $"[OnReload][{ChaControl.chaFile.parameter?.fullname}] CharaTriggerInfo loaded from extdata");

						if (ExtendedData.data.TryGetValue("CharaVirtualGroupNames", out var loadedCharaVirtualGroupNames) && loadedCharaVirtualGroupNames != null)
						{
							if (ExtendedData.version < 2)
							{
								var OldCharaVirtualGroupNames = MessagePackSerializer.Deserialize<List<Dictionary<string, string>>>((byte[])loadedCharaVirtualGroupNames);
								if (OldCharaVirtualGroupNames.Count() != 7)
									Logger.LogError($"[OnReload][{ChaControl.chaFile.parameter?.fullname}] OldCharaVirtualGroupNames.Count(): {OldCharaVirtualGroupNames.Count()}");
								else
								{
									for (int i = 0; i < 7; i++)
										CharaVirtualGroupNames[i] = UpgradeVirtualGroupNamesV1(OldCharaVirtualGroupNames[i]);
								}
							}
							else
								CharaVirtualGroupNames = MessagePackSerializer.Deserialize<Dictionary<int, Dictionary<string, string>>>((byte[])loadedCharaVirtualGroupNames);
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
				CurOutfitTriggerInfo = new OutfitTriggerInfo(CurrentCoordinateIndex);
				CurOutfitVirtualGroupNames = new Dictionary<string, string>();
				VirtualGroupStates?.Clear();

				if (MakerAPI.InsideMaker)
				{
					if (!CharaTriggerInfo.ContainsKey(CurrentCoordinateIndex))
						CharaTriggerInfo[CurrentCoordinateIndex] = new OutfitTriggerInfo(CurrentCoordinateIndex);

					if (CharaTriggerInfo[CurrentCoordinateIndex] == null)
						CharaTriggerInfo[CurrentCoordinateIndex] = new OutfitTriggerInfo(CurrentCoordinateIndex);
				}
				CurOutfitTriggerInfo = CharaTriggerInfo[CurrentCoordinateIndex];
				Logger.Log(DebugLogLevel, $"[InitCurOutfitTriggerInfo] CurOutfitTriggerInfo.Parts.Count() {CurOutfitTriggerInfo.Parts.Count()}");

				if ((!MakerAPI.InsideMaker) && (CurOutfitTriggerInfo.Parts.Count() == 0))
				{
					Logger.Log(DebugLogLevel, $"[InitOutfitTriggerInfo][{ChaControl.chaFile.parameter?.fullname}] TriggerEnabled false");
					if (KKAPI.Studio.StudioAPI.InsideStudio)
						ClearStudioUI();
					return;
				}

				TriggerEnabled = true;

				FillVirtualGroupStates();
				FillVirtualGroupNames();
				Logger.Log(DebugLogLevel, $"[InitCurOutfitTriggerInfo] CurOutfitVirtualGroupNames.Count() {CurOutfitVirtualGroupNames.Count()}");

				if (MakerAPI.InsideMaker)
				{
					AccSlotChangedHandler(AccessoriesApi.SelectedMakerAccSlot, true);
				}
				else if (KKAPI.Studio.StudioAPI.InsideStudio)
				{
					UpdateStudioUI();
					ChaControl.SetAccessoryStateAll(true);
					SyncAllAccToggle();
				}
				else
				{
					UpdateHUI();
					ChaControl.SetAccessoryStateAll(true);
					SyncAllAccToggle();
				}
			}
		}
	}
}
