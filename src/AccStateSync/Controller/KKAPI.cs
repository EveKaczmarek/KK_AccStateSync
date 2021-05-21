using System.Collections.Generic;
using System.Linq;

using MessagePack;
using UniRx;
using Studio;

using BepInEx.Logging;
using ExtensibleSaveFormat;

using KKAPI;
using KKAPI.Chara;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		public partial class AccStateSyncController : CharaCustomFunctionController
		{
			internal int _currentCoordinateIndex => ChaControl.fileStatus.coordinateType;
			internal string CharaFullName => ChaControl.chaFile.parameter?.fullname.Trim();
			internal int _treeNodeObjID = -1000;

			public Dictionary<int, OutfitTriggerInfo> CharaTriggerInfo = new Dictionary<int, OutfitTriggerInfo>();

			internal bool TriggerEnabled = false;
			internal bool SkipAutoSave = false;
			internal bool SkipSlotChangePartTypeCheck = false;

			public Dictionary<int, Dictionary<string, VirtualGroupInfo>> CharaVirtualGroupInfo = new Dictionary<int, Dictionary<string, VirtualGroupInfo>>();
			public Dictionary<int, Dictionary<string, bool>> CharaVirtualGroupStates = new Dictionary<int, Dictionary<string, bool>>();

			protected override void Start()
			{
				DebugMsg(LogLevel.Info, "[Start] Fired!!");
				ResetCharaTriggerInfo();
				ResetCharaVirtualGroupInfo();
				if (JetPack.CharaStudio.Loaded)
					ResetCharaVirtualGroupStates();
				CurrentCoordinate.Subscribe( _ => OnCoordinateChanged() );
				base.Start();
			}

			private void OnCoordinateChanged()
			{
				DebugMsg(LogLevel.Info, $"[OnCoordinateChanged][{CharaFullName}] _currentCoordinateIndex: {_currentCoordinateIndex}");
				SkipAutoSave = true;
				StartCoroutine(InitCurOutfitTriggerInfoCoroutine("OnCoordinateChanged"));
			}

			protected override void OnCoordinateBeingSaved(ChaFileCoordinate coordinate)
			{
				DebugMsg(LogLevel.Info, $"[OnCoordinateBeingSaved][{CharaFullName}] Fired!!");
				PluginData _pluginData = new PluginData();
				SyncOutfitTriggerInfo(_currentCoordinateIndex);
				_pluginData.data.Add("OutfitTriggerInfo", MessagePackSerializer.Serialize(CharaTriggerInfo[_currentCoordinateIndex]));
				_pluginData.data.Add("OutfitVirtualGroupInfo", MessagePackSerializer.Serialize(CharaVirtualGroupInfo[_currentCoordinateIndex]));
				_pluginData.version = Constants._pluginDataVersion;
				if (_cfgLegacySaveFormat.Value)
				{
					Dictionary<string, string> OutfitVirtualGroupNames = new Dictionary<string, string>();
					foreach (KeyValuePair<string, VirtualGroupInfo> _group in CharaVirtualGroupInfo[_currentCoordinateIndex])
					{
						if (_group.Value.Kind > 9)
							OutfitVirtualGroupNames[_group.Key] = _group.Value.Label;
					}
					_pluginData.data.Add("OutfitVirtualGroupNames", MessagePackSerializer.Serialize(OutfitVirtualGroupNames));
				}
				SetCoordinateExtendedData(coordinate, _pluginData);
			}

			protected override void OnCardBeingSaved(GameMode currentGameMode)
			{
				DebugMsg(LogLevel.Info, $"[OnCardBeingSaved][{CharaFullName}] Fired!!");
				SyncCharaTriggerInfo();
				PluginData _pluginData = new PluginData();
				_pluginData.data.Add("CharaTriggerInfo", MessagePackSerializer.Serialize(CharaTriggerInfo));
				_pluginData.data.Add("CharaVirtualGroupInfo", MessagePackSerializer.Serialize(CharaVirtualGroupInfo));
				_pluginData.version = Constants._pluginDataVersion;
				if (_cfgLegacySaveFormat.Value)
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
					_pluginData.data.Add("CharaVirtualGroupNames", MessagePackSerializer.Serialize(CharaVirtualGroupNames));
				}
				SetExtendedData(_pluginData);
			}

			protected override void OnCoordinateBeingLoaded(ChaFileCoordinate coordinate)
			{
				DebugMsg(LogLevel.Info, $"[OnCoordinateBeingLoaded][{CharaFullName}] Fired!!");
				SkipAutoSave = true;
				if (!JetPack.CharaMaker.Inside || (JetPack.CharaMaker.Inside && _loadCoordinateExtdata))
				{
					if (JetPack.CharaStudio.Loaded)
						StoreOutfitVirtualGroupStates(_currentCoordinateIndex);

					CharaTriggerInfo[_currentCoordinateIndex] = new OutfitTriggerInfo(_currentCoordinateIndex);
					CharaVirtualGroupInfo[_currentCoordinateIndex] = new Dictionary<string, VirtualGroupInfo>();

					PluginData _pluginData = GetCoordinateExtendedData(coordinate);
					if (_pluginData?.version > Constants._pluginDataVersion)
						_logger.Log(LogLevel.Error | LogLevel.Message, $"[OnCoordinateBeingLoaded][{CharaFullName}] ExtendedData.version: {_pluginData.version} is newer than your plugin");
					if (_pluginData != null && _pluginData.data.TryGetValue("OutfitTriggerInfo", out object _loadedOutfitTriggerInfo) && _loadedOutfitTriggerInfo != null)
					{
						DebugMsg(LogLevel.Info, $"[OnCoordinateBeingLoaded][{CharaFullName}][Version: {_pluginData.version}]");

						if (_pluginData.version < 2)
						{
							OutfitTriggerInfoV1 OldCharaTriggerInfo = MessagePackSerializer.Deserialize<OutfitTriggerInfoV1>((byte[]) _loadedOutfitTriggerInfo);
							CharaTriggerInfo[_currentCoordinateIndex] = UpgradeOutfitTriggerInfoV1(OldCharaTriggerInfo);
						}
						else
							CharaTriggerInfo[_currentCoordinateIndex] = MessagePackSerializer.Deserialize<OutfitTriggerInfo>((byte[]) _loadedOutfitTriggerInfo);
						CharaTriggerInfo[_currentCoordinateIndex].Index = _currentCoordinateIndex;
						DebugMsg(LogLevel.Info, $"[OnCoordinateBeingLoaded][{CharaFullName}][CharaTriggerInfo][Coordinate: {_currentCoordinateIndex}][Count: {CharaTriggerInfo[_currentCoordinateIndex].Parts.Count}]");

						if (_pluginData.version < 5)
						{
							if (_pluginData.data.TryGetValue("OutfitVirtualGroupNames", out object _loadedOutfitVirtualGroupNames) && _loadedOutfitVirtualGroupNames != null)
							{
								Dictionary<string, string> OutfitVirtualGroupNames = MessagePackSerializer.Deserialize<Dictionary<string, string>>((byte[]) _loadedOutfitVirtualGroupNames);
								CharaVirtualGroupInfo[_currentCoordinateIndex] = UpgradeVirtualGroupNamesV2(OutfitVirtualGroupNames);
							}
						}
						else
						{
							if (_pluginData.data.TryGetValue("OutfitVirtualGroupInfo", out object _loadedOutfitVirtualGroupInfo) && _loadedOutfitVirtualGroupInfo != null)
								CharaVirtualGroupInfo[_currentCoordinateIndex] = MessagePackSerializer.Deserialize<Dictionary<string, VirtualGroupInfo>>((byte[]) _loadedOutfitVirtualGroupInfo);
						}
						DebugMsg(LogLevel.Info, $"[OnCoordinateBeingLoaded][{CharaFullName}][CharaVirtualGroupInfo][Coordinate: {_currentCoordinateIndex}][Count: {CharaVirtualGroupInfo[_currentCoordinateIndex].Count}]");
					}

					if (JetPack.CharaStudio.Loaded)
						RestoreOutfitVirtualGroupStates(_currentCoordinateIndex);
				}

				NullCheckOutfitTriggerInfo(_currentCoordinateIndex);
				NullCheckOutfitVirtualGroupInfo(_currentCoordinateIndex);

				if (!JetPack.CharaStudio.Running)
					ResetOutfitVirtualGroupState(_currentCoordinateIndex);
				if (JetPack.CharaHscene.Loaded)
				{
					if (_cfgAutoHideSecondary.Value)
					{
						List<string> _secondary = CharaVirtualGroupInfo[_currentCoordinateIndex].Values.Where(x => x.Secondary).Select(x => x.Group).ToList();
						foreach (string _group in _secondary)
							CharaVirtualGroupInfo[_currentCoordinateIndex][_group].State = false;
					}
				}

				StartCoroutine(InitCurOutfitTriggerInfoCoroutine("OnCoordinateBeingLoaded"));

				base.OnCoordinateBeingLoaded(coordinate);
			}

			protected override void OnReload(GameMode currentGameMode)
			{
				DebugMsg(LogLevel.Info, "[OnReload] Fired!!");
				TriggerEnabled = false;
				SkipAutoSave = true;
				if (!JetPack.CharaMaker.Inside || (JetPack.CharaMaker.Inside && _loadCharaExtdata))
				{
					if (JetPack.CharaStudio.Loaded)
						StoreCharaVirtualGroupStates();

					ResetCharaTriggerInfo();
					ResetCharaVirtualGroupInfo();
					PluginData _pluginData = GetExtendedData();
					if (_pluginData?.version > Constants._pluginDataVersion)
						_logger.Log(LogLevel.Error | LogLevel.Message, $"[OnReload][{CharaFullName}] ExtendedData.version: {_pluginData.version} is newer than your plugin");
					if (_pluginData != null && _pluginData.data.TryGetValue("CharaTriggerInfo", out object _loadedCharaTriggerInfo) && _loadedCharaTriggerInfo != null)
					{
						DebugMsg(LogLevel.Info, $"[OnReload][{CharaFullName}][Version: {_pluginData.version}]");

						if (_pluginData.version < 2)
						{
							List<OutfitTriggerInfoV1> OldCharaTriggerInfo = MessagePackSerializer.Deserialize<List<OutfitTriggerInfoV1>>((byte[])_loadedCharaTriggerInfo);
							for (int i = 0; i < 7; i++)
								CharaTriggerInfo[i] = UpgradeOutfitTriggerInfoV1(OldCharaTriggerInfo[i]);
						}
						else
							CharaTriggerInfo = MessagePackSerializer.Deserialize<Dictionary<int, OutfitTriggerInfo>>((byte[]) _loadedCharaTriggerInfo);

						for (int i = 0; i < 7; i++)
							DebugMsg(LogLevel.Info, $"[OnReload][{CharaFullName}][CharaTriggerInfo][Coordinate: {i}][Count: {CharaTriggerInfo[i].Parts.Count}]");

						if (_pluginData.version < 5)
						{
							if (_pluginData.data.TryGetValue("CharaVirtualGroupNames", out object _loadedCharaVirtualGroupNames) && _loadedCharaVirtualGroupNames != null)
							{
								if (_pluginData.version < 2)
								{
									List<Dictionary<string, string>> OldCharaVirtualGroupNames = MessagePackSerializer.Deserialize<List<Dictionary<string, string>>>((byte[]) _loadedCharaVirtualGroupNames);
									if (OldCharaVirtualGroupNames?.Count != 7)
										_logger.LogError($"[OnReload][{CharaFullName}][OldCharaVirtualGroupNames][Count: {OldCharaVirtualGroupNames?.Count}]");
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
									Dictionary<int, Dictionary<string, string>> CharaVirtualGroupNames = MessagePackSerializer.Deserialize<Dictionary<int, Dictionary<string, string>>>((byte[]) _loadedCharaVirtualGroupNames);
									for (int i = 0; i < 7; i++)
										CharaVirtualGroupInfo[i] = UpgradeVirtualGroupNamesV2(CharaVirtualGroupNames[i]);
								}
							}
						}
						else
						{
							if (_pluginData.data.TryGetValue("CharaVirtualGroupInfo", out object _loadedCharaVirtualGroupInfo) && _loadedCharaVirtualGroupInfo != null)
								CharaVirtualGroupInfo = MessagePackSerializer.Deserialize<Dictionary<int, Dictionary<string, VirtualGroupInfo>>>((byte[]) _loadedCharaVirtualGroupInfo);
						}

						for (int i = 0; i < 7; i++)
							DebugMsg(LogLevel.Info, $"[OnReload][{CharaFullName}][CharaVirtualGroupInfo][Coordinate: {i}][Count: {CharaVirtualGroupInfo[i].Count}]");

						if (JetPack.CharaStudio.Loaded)
							RestoreCharaVirtualGroupStates();
					}
				}

				for (int i = 0; i < 7; i++)
				{
					NullCheckOutfitTriggerInfo(i);
					NullCheckOutfitVirtualGroupInfo(i);

					if (!JetPack.CharaStudio.Running)
						ResetOutfitVirtualGroupState(i);
				}

				//InitCurOutfitTriggerInfo("OnReload");
				StartCoroutine(InitCurOutfitTriggerInfoCoroutine("OnReload"));

				base.OnReload(currentGameMode);
			}
		}

		internal static AccStateSyncController GetController(ChaControl _chara) => _chara?.gameObject?.GetComponent<AccStateSyncController>();
		internal static AccStateSyncController GetController(OCIChar _chara) => _chara?.charInfo?.gameObject?.GetComponent<AccStateSyncController>();
	}
}
