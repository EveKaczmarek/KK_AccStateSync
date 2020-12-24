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
			internal int CurrentCoordinateIndex => ChaControl.fileStatus.coordinateType;
			internal int TreeNodeObjID = -1000;
			internal string CharaFullName => ChaControl.chaFile.parameter?.fullname.Trim();
			public Dictionary<int, OutfitTriggerInfo> CharaTriggerInfo = new Dictionary<int, OutfitTriggerInfo>();
			public OutfitTriggerInfo CurOutfitTriggerInfo;
			public AccTriggerInfo CurSlotTriggerInfo = new AccTriggerInfo(0);

			internal bool TriggerEnabled = false;
			internal bool SkipAutoSave = false;
			internal bool SkipSlotChangePartTypeCheck = false;

			public Dictionary<int, Dictionary<string, VirtualGroupInfo>> CharaVirtualGroupInfo = new Dictionary<int, Dictionary<string, VirtualGroupInfo>>();
			public Dictionary<string, VirtualGroupInfo> CurOutfitVirtualGroupInfo = new Dictionary<string, VirtualGroupInfo>();

			public Dictionary<int, Dictionary<string, bool>> CharaVirtualGroupStates = new Dictionary<int, Dictionary<string, bool>>();

			protected override void Start()
			{
				Logger.Log(DebugLogLevel, "[Start] Fired!!");
				ResetCharaTriggerInfo();
				ResetCharaVirtualGroupInfo();
				if (CharaStudio.Inside)
				{
					for (int i = 0; i < 7; i++)
						CharaVirtualGroupStates[i] = new Dictionary<string, bool>();
				}
				CurrentCoordinate.Subscribe( value => OnCoordinateChanged() );

				base.Start();
			}

			private void OnCoordinateChanged()
			{
				Logger.Log(DebugLogLevel, $"[OnCoordinateChanged][{CharaFullName}] CurrentCoordinateIndex: {CurrentCoordinateIndex}");
				SkipAutoSave = true;
				InitCurOutfitTriggerInfo("OnCoordinateChanged");
			}

			protected override void OnCoordinateBeingSaved(ChaFileCoordinate coordinate)
			{
				Logger.Log(DebugLogLevel, $"[OnCoordinateBeingSaved][{CharaFullName}] Fired!!");
				PluginData ExtendedData = new PluginData();
				SyncOutfitTriggerInfo(CurrentCoordinateIndex);
				ExtendedData.data.Add("OutfitTriggerInfo", MessagePackSerializer.Serialize(CharaTriggerInfo[CurrentCoordinateIndex]));
				ExtendedData.data.Add("OutfitVirtualGroupInfo", MessagePackSerializer.Serialize(CharaVirtualGroupInfo[CurrentCoordinateIndex]));
				ExtendedData.version = Constants.ExtDataVersion;
				if (LegacySaveFormat.Value)
				{
					Dictionary<string, string> VirtualGroupNames = new Dictionary<string, string>();
					foreach (KeyValuePair<string, VirtualGroupInfo> group in CharaVirtualGroupInfo[CurrentCoordinateIndex])
					{
						if (group.Value.Kind > 9)
							VirtualGroupNames[group.Key] = group.Value.Label;
					}
					ExtendedData.data.Add("OutfitVirtualGroupNames", MessagePackSerializer.Serialize(VirtualGroupNames));
				}
				SetCoordinateExtendedData(coordinate, ExtendedData);
			}

			protected override void OnCardBeingSaved(GameMode currentGameMode)
			{
				Logger.Log(DebugLogLevel, $"[OnCardBeingSaved][{CharaFullName}] Fired!!");
				SyncCharaTriggerInfo();
				PluginData ExtendedData = new PluginData();
				ExtendedData.data.Add("CharaTriggerInfo", MessagePackSerializer.Serialize(CharaTriggerInfo));
				ExtendedData.data.Add("CharaVirtualGroupInfo", MessagePackSerializer.Serialize(CharaVirtualGroupInfo));
				ExtendedData.version = Constants.ExtDataVersion;
				if (LegacySaveFormat.Value)
				{
					Dictionary<int, Dictionary<string, string>> CharaVirtualGroupNames = new Dictionary<int, Dictionary<string, string>>();
					for (int i = 0; i < 7; i++)
					{
						CharaVirtualGroupNames[i] = new Dictionary<string, string>();
						foreach (KeyValuePair<string, VirtualGroupInfo> group in CharaVirtualGroupInfo[i])
						{
							if (group.Value.Kind > 9)
								CharaVirtualGroupNames[i][group.Key] = group.Value.Label;
						}
					}
					ExtendedData.data.Add("CharaVirtualGroupNames", MessagePackSerializer.Serialize(CharaVirtualGroupNames));
				}
				SetExtendedData(ExtendedData);
			}

			protected override void OnCoordinateBeingLoaded(ChaFileCoordinate coordinate)
			{
				Logger.Log(DebugLogLevel, $"[OnCoordinateBeingLoaded][{CharaFullName}] Fired!!");
				SkipAutoSave = true;
				if (!MakerAPI.InsideMaker || (MakerAPI.InsideMaker && LoadCoordinateExtdata))
				{
					if (CharaStudio.Inside)
						StoreOutfitVirtualGroupStates(CurrentCoordinateIndex);

					CharaTriggerInfo[CurrentCoordinateIndex] = new OutfitTriggerInfo(CurrentCoordinateIndex);
					CharaVirtualGroupInfo[CurrentCoordinateIndex] = new Dictionary<string, VirtualGroupInfo>();

					PluginData ExtendedData = GetCoordinateExtendedData(coordinate);
					if (ExtendedData?.version > Constants.ExtDataVersion)
						Logger.Log(BepInEx.Logging.LogLevel.Error | BepInEx.Logging.LogLevel.Message, $"[OnReload][{CharaFullName}] ExtendedData.version: {ExtendedData.version} is newer than your plugin");
					if (ExtendedData != null && ExtendedData.data.TryGetValue("OutfitTriggerInfo", out var loadedOutfitTriggerInfo) && loadedOutfitTriggerInfo != null)
					{
						if (ExtendedData.version < 2)
						{
							OutfitTriggerInfoV1 OldCharaTriggerInfo = MessagePackSerializer.Deserialize<OutfitTriggerInfoV1>((byte[]) loadedOutfitTriggerInfo);
							CharaTriggerInfo[CurrentCoordinateIndex] = UpgradeOutfitTriggerInfoV1(OldCharaTriggerInfo);
						}
						else
							CharaTriggerInfo[CurrentCoordinateIndex] = MessagePackSerializer.Deserialize<OutfitTriggerInfo>((byte[]) loadedOutfitTriggerInfo);
						Logger.Log(DebugLogLevel, $"[OnCoordinateBeingLoaded][{CharaFullName}] CharaTriggerInfo[{CurrentCoordinateIndex}] loaded from extdata");

						if (ExtendedData.version < 5)
						{
							if (ExtendedData.data.TryGetValue("OutfitVirtualGroupNames", out var loadedOutfitVirtualGroupNames) && loadedOutfitVirtualGroupNames != null)
							{
								Dictionary<string, string> OutfitVirtualGroupNames = MessagePackSerializer.Deserialize<Dictionary<string, string>>((byte[]) loadedOutfitVirtualGroupNames);
								CharaVirtualGroupInfo[CurrentCoordinateIndex] = UpgradeVirtualGroupNamesV2(OutfitVirtualGroupNames);
							}
						}
						else
						{
							if (ExtendedData.data.TryGetValue("OutfitVirtualGroupInfo", out var loadedOutfitVirtualGroupInfo) && loadedOutfitVirtualGroupInfo != null)
								CharaVirtualGroupInfo[CurrentCoordinateIndex] = MessagePackSerializer.Deserialize<Dictionary<string, VirtualGroupInfo>>((byte[]) loadedOutfitVirtualGroupInfo);
						}
						Logger.Log(DebugLogLevel, $"[OnCoordinateBeingLoaded][{CharaFullName}] CharaVirtualGroupInfo[{CurrentCoordinateIndex}] loaded from extdata");
					}

					if (CharaStudio.Inside)
						RestoreOutfitVirtualGroupStates(CurrentCoordinateIndex);
				}

				NullCheckOutfitTriggerInfo(CurrentCoordinateIndex);
				NullCheckOutfitVirtualGroupInfo(CurrentCoordinateIndex);
				NullCheckOnePieceTriggerInfo(CurrentCoordinateIndex);

				if (!CharaStudio.Inside)
					ResetOutfitVirtualGroupState(CurrentCoordinateIndex);
				if (HScene.Inside)
				{
					if (AutoHideSecondary.Value)
					{
						List<string> secondary = CharaVirtualGroupInfo[CurrentCoordinateIndex].Values.Where(x => x.Secondary).Select(x => x.Group).ToList();
						foreach (string group in secondary)
							CharaVirtualGroupInfo[CurrentCoordinateIndex][group].State = false;
					}
				}

				InitCurOutfitTriggerInfo("OnCoordinateBeingLoaded");

				base.OnCoordinateBeingLoaded(coordinate);
			}

			protected override void OnReload(GameMode currentGameMode)
			{
				Logger.Log(DebugLogLevel, "[OnReload] Fired!!");
				TriggerEnabled = false;
				SkipAutoSave = true;
				if (!MakerAPI.InsideMaker || (MakerAPI.InsideMaker && LoadCharaExtdata))
				{
					if (CharaStudio.Inside)
						StoreCharaVirtualGroupStates();

					ResetCharaTriggerInfo();
					ResetCharaVirtualGroupInfo();
					PluginData ExtendedData = GetExtendedData();
					if (ExtendedData?.version > Constants.ExtDataVersion)
						Logger.Log(BepInEx.Logging.LogLevel.Error | BepInEx.Logging.LogLevel.Message, $"[OnReload][{CharaFullName}] ExtendedData.version: {ExtendedData.version} is newer than your plugin");
					if (ExtendedData != null && ExtendedData.data.TryGetValue("CharaTriggerInfo", out var loadedCharaTriggerInfo) && loadedCharaTriggerInfo != null)
					{
						Logger.Log(DebugLogLevel, $"[OnReload][{CharaFullName}] ExtendedData.version: {ExtendedData.version}");

						if (ExtendedData.version < 2)
						{
							List<OutfitTriggerInfoV1> OldCharaTriggerInfo = MessagePackSerializer.Deserialize<List<OutfitTriggerInfoV1>>((byte[]) loadedCharaTriggerInfo);
							for (int i = 0; i < 7; i++)
								CharaTriggerInfo[i] = UpgradeOutfitTriggerInfoV1(OldCharaTriggerInfo[i]);
						}
						else
							CharaTriggerInfo = MessagePackSerializer.Deserialize<Dictionary<int, OutfitTriggerInfo>>((byte[]) loadedCharaTriggerInfo);
						Logger.Log(DebugLogLevel, $"[OnReload][{CharaFullName}] CharaTriggerInfo loaded from extdata");

						if (ExtendedData.version < 5)
						{
							if (ExtendedData.data.TryGetValue("CharaVirtualGroupNames", out var loadedCharaVirtualGroupNames) && loadedCharaVirtualGroupNames != null)
							{
								if (ExtendedData.version < 2)
								{
									var OldCharaVirtualGroupNames = MessagePackSerializer.Deserialize<List<Dictionary<string, string>>>((byte[]) loadedCharaVirtualGroupNames);
									if (OldCharaVirtualGroupNames.Count() != 7)
										Logger.LogError($"[OnReload][{CharaFullName}] OldCharaVirtualGroupNames.Count(): {OldCharaVirtualGroupNames.Count()}");
									else
									{
										for (int i = 0; i < 7; i++)
										{
											Dictionary<string, string> VirtualGroupNames = UpgradeVirtualGroupNamesV1(OldCharaVirtualGroupNames[i]);
											CharaVirtualGroupInfo[i] = UpgradeVirtualGroupNamesV2(VirtualGroupNames);
										}
									}
								}
								else
								{
									Dictionary<int, Dictionary<string, string>> CharaVirtualGroupNames = MessagePackSerializer.Deserialize<Dictionary<int, Dictionary<string, string>>>((byte[]) loadedCharaVirtualGroupNames);
									for (int i = 0; i < 7; i++)
										CharaVirtualGroupInfo[i] = UpgradeVirtualGroupNamesV2(CharaVirtualGroupNames[i]);
								}
							}
						}
						else
						{
							if (ExtendedData.data.TryGetValue("CharaVirtualGroupInfo", out var loadedCharaVirtualGroupInfo) && loadedCharaVirtualGroupInfo != null)
								CharaVirtualGroupInfo = MessagePackSerializer.Deserialize<Dictionary<int, Dictionary<string, VirtualGroupInfo>>>((byte[]) loadedCharaVirtualGroupInfo);
						}

						Logger.Log(DebugLogLevel, $"[OnReload][{CharaFullName}] CharaVirtualGroupNames loaded from extdata");

						if (CharaStudio.Inside)
							RestoreCharaVirtualGroupStates();
					}
				}

				for (int i = 0; i < 7; i++)
				{
					NullCheckOutfitTriggerInfo(i);
					NullCheckOutfitVirtualGroupInfo(i);
					NullCheckOnePieceTriggerInfo(i);

					if (!CharaStudio.Inside)
						ResetOutfitVirtualGroupState(i);
				}

				InitCurOutfitTriggerInfo("OnReload");

				base.OnReload(currentGameMode);
			}
		}

		internal static AccStateSyncController GetController(ChaControl chaCtrl) => chaCtrl?.gameObject?.GetComponent<AccStateSyncController>();
	}
}
