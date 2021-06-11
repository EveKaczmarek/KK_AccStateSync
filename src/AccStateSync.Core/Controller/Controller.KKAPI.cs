using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UniRx;
using MessagePack;

using BepInEx.Logging;
using ExtensibleSaveFormat;

using KKAPI;
using KKAPI.Chara;

using JetPack;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		internal static AccStateSyncController GetController(ChaControl _chaCtrl) => _chaCtrl?.gameObject?.GetComponent<AccStateSyncController>();

		public partial class AccStateSyncController : CharaCustomFunctionController
		{
			internal string CharaFullName => ChaControl.chaFile.parameter?.fullname?.Trim();
			internal int _currentCoordinateIndex => ChaControl.fileStatus.coordinateType;
			internal int _shoesType => ChaControl.fileStatus.shoesType;

			public List<TriggerProperty> TriggerPropertyList = new List<TriggerProperty>();
			public List<TriggerGroup> TriggerGroupList = new List<TriggerGroup>();
			public bool TriggerEnabled = true;
			internal bool _duringLoadChange = false;
			internal int _treeNodeObjID = -1000;

			protected override void OnCoordinateBeingSaved(ChaFileCoordinate coordinate)
			{
				MissingKindCheck(_currentCoordinateIndex);
				MissingPartCheck(_currentCoordinateIndex);
				MissingGroupCheck(_currentCoordinateIndex);
				MissingPropertyCheck(_currentCoordinateIndex);

				RefreshCache();
				DebugMsg(LogLevel.Info, $"[OnCoordinateBeingSaved][{CharaFullName}][PropertyList.Count: {_cachedCoordinatePropertyList?.Count}]");

				if (_cachedCoordinatePropertyList?.Count == 0)
				{
					SetCoordinateExtendedData(coordinate, null);
					return;
				}

				List<TriggerProperty> _tempTriggerProperty = _cachedCoordinatePropertyList.JsonClone() as List<TriggerProperty>;
				List<TriggerGroup> _tempTriggerGroup = _cachedCoordinateGroupList.JsonClone() as List<TriggerGroup>;
				_tempTriggerProperty.ForEach(x => x.Coordinate = -1);
				_tempTriggerGroup.ForEach(x => x.Coordinate = -1);
				_tempTriggerGroup.ForEach(x => x.State = 0);
				PluginData _pluginData = new PluginData() { version = _pluginDataVersion };
				_pluginData.data.Add("TriggerPropertyList", MessagePackSerializer.Serialize(_tempTriggerProperty));
				_pluginData.data.Add("TriggerGroupList", MessagePackSerializer.Serialize(_tempTriggerGroup));
				SetCoordinateExtendedData(coordinate, _pluginData);
			}

			protected override void OnCardBeingSaved(GameMode currentGameMode)
			{
				DebugMsg(LogLevel.Info, $"[OnCardBeingSaved][{CharaFullName}][PropertyList.Count: {TriggerPropertyList?.Count}]");
				if (TriggerPropertyList?.Count == 0)
				{
					SetExtendedData(null);
					return;
				}

				PluginData _pluginData = new PluginData() { version = _pluginDataVersion };
				_pluginData.data.Add("TriggerPropertyList", MessagePackSerializer.Serialize(TriggerPropertyList));
				List<TriggerGroup> _tempTriggerGroup = TriggerGroupList.JsonClone() as List<TriggerGroup>;
				if (JetPack.CharaMaker.Inside)
					_tempTriggerGroup.ForEach(x => x.State = x.Startup);
				_pluginData.data.Add("TriggerGroupList", MessagePackSerializer.Serialize(_tempTriggerGroup));
				SetExtendedData(_pluginData);
			}

			protected override void OnCoordinateBeingLoaded(ChaFileCoordinate coordinate)
			{
				/*
				if (JetPack.CharaStudio.Running)
					TriggerEnabled = false;
				*/
				if (!JetPack.CharaMaker.Inside || (JetPack.CharaMaker.Inside && _loadCoordinateExtdata))
				{
					TriggerPropertyList.RemoveAll(x => x.Coordinate == _currentCoordinateIndex);
					TriggerGroupList.RemoveAll(x => x.Coordinate == _currentCoordinateIndex);

					PluginData _pluginData = GetCoordinateExtendedData(coordinate);
					if (_pluginData != null)
					{
						if (_pluginData.version > _pluginDataVersion)
							_logger.Log(LogLevel.Error | LogLevel.Message, $"[OnCoordinateBeingLoaded][{CharaFullName}] ExtendedData.version: {_pluginData.version} is newer than your plugin");
						else if (_pluginData.version < _pluginDataVersion)
						{
							_logger.Log(LogLevel.Info, $"[OnCoordinateBeingLoaded][{CharaFullName}][Migrating from ver. {_pluginData.version}]");
							Migration.ConvertOutfitPluginData(_currentCoordinateIndex, _pluginData, ref TriggerPropertyList, ref TriggerGroupList);
						}
						else
						{
							if (_pluginData.data.TryGetValue("TriggerPropertyList", out object _loadedTriggerProperty) && _loadedTriggerProperty != null)
							{
								List<TriggerProperty> _tempTriggerProperty = MessagePackSerializer.Deserialize<List<TriggerProperty>>((byte[]) _loadedTriggerProperty);
								if (_tempTriggerProperty?.Count > 0)
								{
									_tempTriggerProperty.ForEach(x => x.Coordinate = _currentCoordinateIndex);
									TriggerPropertyList.AddRange(_tempTriggerProperty);
								}

								if (_pluginData.data.TryGetValue("TriggerGroupList", out object _loadedTriggerGroup) && _loadedTriggerGroup != null)
								{
									List<TriggerGroup> _tempTriggerGroup = MessagePackSerializer.Deserialize<List<TriggerGroup>>((byte[]) _loadedTriggerGroup);
									if (_tempTriggerGroup?.Count > 0)
									{
										foreach (TriggerGroup _group in _tempTriggerGroup)
										{
											_group.Coordinate = _currentCoordinateIndex;

											if (_group.GUID.IsNullOrEmpty())
												_group.GUID = JetPack.Toolbox.GUID("D");

											if (!JetPack.CharaStudio.Running)
												_group.State = _group.Startup;
										}
										TriggerGroupList.AddRange(_tempTriggerGroup);
									}
								}
							}
						}

						/*
						MissingKindCheck(_currentCoordinateIndex);
						MissingPartCheck(_currentCoordinateIndex);
						*/
						MissingGroupCheck(_currentCoordinateIndex);
						MissingPropertyCheck(_currentCoordinateIndex);
					}

					//StartCoroutine(InitCurOutfitTriggerInfoCoroutine("OnCoordinateBeingLoaded"));
					InitCurOutfitTriggerInfo("OnCoordinateBeingLoaded");
				}
				base.OnCoordinateBeingLoaded(coordinate);
			}

			protected override void OnReload(GameMode currentGameMode)
			{
				if (JetPack.CharaStudio.Running)
					TriggerEnabled = false;

				if (!JetPack.CharaMaker.Inside || (JetPack.CharaMaker.Inside && _loadCharaExtdata))
				{
					TriggerPropertyList.Clear();
					TriggerGroupList.Clear();

					PluginData _pluginData = GetExtendedData();
					if (_pluginData != null)
					{
						if (_pluginData.version > _pluginDataVersion)
							_logger.Log(LogLevel.Error | LogLevel.Message, $"[OnReload][{CharaFullName}] ExtendedData.version: {_pluginData.version} is newer than your plugin");
						else if (_pluginData.version < _pluginDataVersion)
						{
							_logger.Log(LogLevel.Info, $"[OnReload][{CharaFullName}][Migrating from ver. {_pluginData.version}]");
							Migration.ConvertCharaPluginData(_pluginData, ref TriggerPropertyList, ref TriggerGroupList);
						}
						else
						{
							if (_pluginData.data.TryGetValue("TriggerPropertyList", out object _loadedTriggerProperty) && _loadedTriggerProperty != null)
							{
								List<TriggerProperty> _tempTriggerProperty = MessagePackSerializer.Deserialize<List<TriggerProperty>>((byte[]) _loadedTriggerProperty);
								if (_tempTriggerProperty?.Count > 0)
									TriggerPropertyList.AddRange(_tempTriggerProperty);

								if (_pluginData.data.TryGetValue("TriggerGroupList", out object _loadedTriggerGroup) && _loadedTriggerGroup != null)
								{
									List<TriggerGroup> _tempTriggerGroup = MessagePackSerializer.Deserialize<List<TriggerGroup>>((byte[]) _loadedTriggerGroup);
									if (_tempTriggerGroup?.Count > 0)
									{
										foreach (TriggerGroup _group in _tempTriggerGroup)
										{
											if (_group.GUID.IsNullOrEmpty())
												_group.GUID = JetPack.Toolbox.GUID("D");

											if (!JetPack.CharaStudio.Running)
												_group.State = _group.Startup;
										}
										TriggerGroupList.AddRange(_tempTriggerGroup);
									}
								}
							}
						}
					}

					//ChaControl.StartCoroutine(InitCurOutfitTriggerInfoCoroutine("OnReload"));
					InitCurOutfitTriggerInfo("OnReload");
				}
				base.OnReload(currentGameMode);
			}

			internal IEnumerator LoadChangeCoroutine()
			{
				yield return JetPack.Toolbox.WaitForEndOfFrame;
				yield return JetPack.Toolbox.WaitForEndOfFrame;

				_duringLoadChange = false;
			}

			internal IEnumerator InitCurOutfitTriggerInfoCoroutine(string _caller)
			{
				yield return JetPack.Toolbox.WaitForEndOfFrame;
				yield return JetPack.Toolbox.WaitForEndOfFrame;

				InitCurOutfitTriggerInfo(_caller);
			}

			internal void InitCurOutfitTriggerInfo(string _caller)
			{
				RefreshCache();

				if (JetPack.CharaStudio.Loaded && !TriggerEnabled)
					return;

				int _count = _cachedCoordinatePropertyList.Count;
				DebugMsg(LogLevel.Info, $"[InitCurOutfitTriggerInfo][{CharaFullName}][{_caller}][CoordinatePropertyList.Count: {_count}]");

				if (!JetPack.CharaMaker.Inside && _count == 0)
				{
					if (JetPack.CharaStudio.Loaded)
					{
						DebugMsg(LogLevel.Info, $"[InitCurOutfitTriggerInfo][CurTreeNodeObjID: {CharaStudio._curTreeNodeObjID}][TreeNodeObjID: {_treeNodeObjID}]");
						if (CharaStudio._curTreeNodeObjID == _treeNodeObjID)
							StartCoroutine(CharaStudio.StatusPanelUpdateCoroutine());
					}
					else if (JetPack.CharaHscene.Loaded)
					{
						CharaHscene.ClearUI();
					}
					return;
				}

				if (JetPack.CharaMaker.Loaded)
				{
					if (_duringLoadChange)
						StartCoroutine(LoadChangeCoroutine());
					StartCoroutine(AccSlotChangedHandlerCoroutine());
				}
				else if (JetPack.CharaStudio.Loaded)
				{
					DebugMsg(LogLevel.Info, $"[InitCurOutfitTriggerInfo][CurTreeNodeObjID: {CharaStudio._curTreeNodeObjID}][TreeNodeObjID: {_treeNodeObjID}]");
					RefreshPreview(_caller);
					if (CharaStudio._curTreeNodeObjID == _treeNodeObjID)
					{
						CharaStudio.UpdateUI();
						StartCoroutine(CharaStudio.StatusPanelUpdateCoroutine());
					}
				}
				else if (JetPack.CharaHscene.Loaded)
				{
					RefreshPreview(_caller);
					CharaHscene.ClearUI();
					CharaHscene.UpdateUI();
				}
				else
				{
					RefreshPreview(_caller);
				}
			}
		}
	}
}
