using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UniRx;

using BepInEx.Logging;

using KKAPI.Maker;
using JetPack;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		public partial class AccStateSyncController
		{
			internal void ResetCharaTriggerInfo()
			{
				DebugMsg(LogLevel.Info, $"[ResetCharaTriggerInfo][{CharaFullName}] Fired!!");
				if (CharaTriggerInfo == null)
					CharaTriggerInfo = new Dictionary<int, OutfitTriggerInfo>();
				else
					CharaTriggerInfo.Clear();
				for (int i = 0; i < 7; i++)
					CharaTriggerInfo[i] = new OutfitTriggerInfo(i);
			}

			internal void ResetCharaVirtualGroupInfo()
			{
				DebugMsg(LogLevel.Info, $"[ResetCharaVirtualGroupNames][{CharaFullName}] Fired!!");
				if (CharaVirtualGroupInfo == null)
					CharaVirtualGroupInfo = new Dictionary<int, Dictionary<string, VirtualGroupInfo>>();
				else
					CharaVirtualGroupInfo.Clear();
				for (int i = 0; i < 7; i++)
					CharaVirtualGroupInfo[i] = new Dictionary<string, VirtualGroupInfo>();
			}

			internal void ResetCharaVirtualGroupStates()
			{
				if (CharaVirtualGroupStates == null)
					CharaVirtualGroupStates = new Dictionary<int, Dictionary<string, bool>>();
				for (int i = 0; i < 7; i++)
					CharaVirtualGroupStates[i] = new Dictionary<string, bool>();
			}

			internal void NullCheckOutfitTriggerInfo(int _coordinateIndex)
			{
				if (CharaTriggerInfo == null)
					CharaTriggerInfo = new Dictionary<int, OutfitTriggerInfo>();
				if (!CharaTriggerInfo.ContainsKey(_coordinateIndex) || CharaTriggerInfo[_coordinateIndex] == null)
					CharaTriggerInfo[_coordinateIndex] = new OutfitTriggerInfo(_coordinateIndex);
			}

			internal void NullCheckOutfitVirtualGroupInfo(int _coordinateIndex)
			{
				if (CharaVirtualGroupInfo == null)
					CharaVirtualGroupInfo = new Dictionary<int, Dictionary<string, VirtualGroupInfo>>();
				if (!CharaVirtualGroupInfo.ContainsKey(_coordinateIndex) || CharaVirtualGroupInfo[_coordinateIndex] == null)
					CharaVirtualGroupInfo[_coordinateIndex] = new Dictionary<string, VirtualGroupInfo>();
			}

			internal void RestoreOutfitVirtualGroupStates(int _coordinateIndex)
			{
				if (CharaVirtualGroupStates[_coordinateIndex].Count() > 0)
				{
					foreach (KeyValuePair<string, bool> _group in CharaVirtualGroupStates[_coordinateIndex])
					{
						if (CharaVirtualGroupInfo[_coordinateIndex].ContainsKey(_group.Key))
							CharaVirtualGroupInfo[_coordinateIndex][_group.Key].State = _group.Value;
					}
				}
				CharaVirtualGroupStates[_coordinateIndex].Clear();
			}

			internal void RestoreCharaVirtualGroupStates()
			{
				for (int i = 0; i < 7; i++)
					RestoreOutfitVirtualGroupStates(i);
			}

			internal void StoreOutfitVirtualGroupStates(int _coordinateIndex)
			{
				CharaVirtualGroupStates[_coordinateIndex].Clear();
				if (CharaVirtualGroupInfo[_coordinateIndex].Count() > 0)
				{
					foreach (KeyValuePair<string, VirtualGroupInfo> _group in CharaVirtualGroupInfo[_coordinateIndex])
						CharaVirtualGroupStates[_coordinateIndex][_group.Key] = _group.Value.State;
				}
			}

			internal void StoreCharaVirtualGroupStates()
			{
				for (int i = 0; i < 7; i++)
					StoreOutfitVirtualGroupStates(i);
			}

			internal void SyncCharaTriggerInfo()
			{
				for (int i = 0; i < 7; i++)
					SyncOutfitTriggerInfo(i);
			}

			internal void SyncOutfitTriggerInfo(int _coordinateIndex)
			{
				//if (!MakerAPI.InsideAndLoaded) return;
				//if (!TriggerEnabled) return;
				DebugMsg(LogLevel.Info, $"[SyncOutfitTriggerInfo][{CharaFullName}] Process OutfitTriggerInfo for Coordinate {_coordinateIndex}");

				NullCheckOutfitTriggerInfo(_coordinateIndex);
				if (CharaTriggerInfo[_coordinateIndex].Parts.Count == 0) return;

				OutfitTriggerInfo OutfitTriggerInfo = CharaTriggerInfo[_coordinateIndex];
				List<ChaFileAccessory.PartsInfo> _partsInfo = ChaControl.ListPartsInfo(_coordinateIndex);

				DebugMsg(LogLevel.Info, $"[SyncCharaTriggerInfo][{CharaFullName}] OutfitTriggerInfo.Parts.Count: {OutfitTriggerInfo.Parts.Count()} before sync");
				List<int> _keys = OutfitTriggerInfo.Parts.Keys.ToList();
				foreach (int _slotIndex in _keys)
				{
					AccTriggerInfo _trigger = OutfitTriggerInfo.Parts[_slotIndex];
					DebugMsg(LogLevel.Info, $"[SyncCharaTriggerInfo][{CharaFullName}][Slot: {_slotIndex}][Kind: {_trigger.Kind}]");
					ChaFileAccessory.PartsInfo _partInfo = _partsInfo.ElementAtOrDefault(_slotIndex);

					if (_partInfo == null)
					{
						CharaTriggerInfo[_coordinateIndex].Parts.Remove(_slotIndex);
						_logger.LogMessage($"AccTriggerInfo for Coordinate {_coordinateIndex} Slot{_slotIndex + 1:00} has been reset");
						continue;
					}
					if ((_partInfo.type == 120) || (_trigger.Kind == -1))
					{
						CharaTriggerInfo[_coordinateIndex].Parts.Remove(_slotIndex);
						_logger.LogMessage($"AccTriggerInfo for Coordinate {_coordinateIndex} Slot{_slotIndex + 1:00} has been reset");
						continue;
					}

					if (MathfEx.RangeEqualOn(0, _trigger.Kind, 8))
						CharaTriggerInfo[_coordinateIndex].Parts[_slotIndex].Group = "";
					else if (_trigger.Kind == 9)
						CharaTriggerInfo[_coordinateIndex].Parts[_slotIndex].Group = _partInfo.parentKey;
					else if (_trigger.Kind > 9)
						CharaTriggerInfo[_coordinateIndex].Parts[_slotIndex].Group = "custom_" + (_trigger.Kind - 9).ToString();
				}
				DebugMsg(LogLevel.Info, $"[SyncCharaTriggerInfo][{CharaFullName}] OutfitTriggerInfo.Parts.Count: {OutfitTriggerInfo.Parts.Count()} after sync");

				SyncOutfitVirtualGroupInfo(_coordinateIndex);
				TrimOutfitVirtualGroupInfo(_coordinateIndex);
			}

			internal void PadOutfitVirtualGroupInfo(int _coordinateIndex)
			{
				int _max = -1;
				if (CharaTriggerInfo[_coordinateIndex]?.Parts?.Count() > 0)
				{
					_max = CharaTriggerInfo[_coordinateIndex].Parts.Values.Max(x => x.Kind);
					for (int i = 10; i <= _max; i++)
					{
						string _group = $"custom_{i - 9}";
						if (CharaVirtualGroupInfo[_coordinateIndex].ContainsKey(_group))
							continue;
						CharaVirtualGroupInfo[_coordinateIndex][_group] = new VirtualGroupInfo(_group, i);
#if DEBUG
						_logger.LogError($"[PadOutfitVirtualGroupInfo][{CharaFullName}][CoordinateIndex: {_coordinateIndex}] \"{_group}\" added missing");
#endif
					}
				}
			}

			internal void TrimOutfitVirtualGroupInfo(int _coordinateIndex)
			{
				int _max = -1;
				if (CharaTriggerInfo[_coordinateIndex]?.Parts?.Count() > 0)
					_max = CharaTriggerInfo[_coordinateIndex].Parts.Values.Max(x => x.Kind);

				int _check = -1;
				if (CharaVirtualGroupInfo[_coordinateIndex].Count() > 0)
					_check = CharaVirtualGroupInfo[_coordinateIndex].Values.Max(x => x.Kind);

				if ((_check > -1) && (_check > _max) && (_max >= 9))
				{
					for (int i = _max + 1; i <= _check; i++)
					{
						string _group = $"custom_{i - 9}";
						CharaVirtualGroupInfo[_coordinateIndex].Remove(_group);
#if DEBUG
						_logger.LogError($"[TrimOutfitVirtualGroupInfo][{CharaFullName}][CoordinateIndex: {_coordinateIndex}] \"{_group}\" unused and removed");
#endif
					}
				}
			}

			internal void ResetOutfitVirtualGroupState(int _coordinateIndex)
			{
				if (CharaVirtualGroupInfo[_coordinateIndex].Count() > 0)
				{
					foreach (KeyValuePair<string, VirtualGroupInfo> _group in CharaVirtualGroupInfo[_coordinateIndex])
						CharaVirtualGroupInfo[_coordinateIndex][_group.Key].State = true;
				}
			}

			internal void ResetCharaVirtualGroupState()
			{
				for (int i = 0; i < 7; i++)
					ResetOutfitVirtualGroupState(i);
			}

			internal void SortOutfitVirtualGroupInfo(int _coordinateIndex)
			{
				if (CharaTriggerInfo[_coordinateIndex]?.Parts?.Count() > 0)
					CharaVirtualGroupInfo[_coordinateIndex] = CharaVirtualGroupInfo[_coordinateIndex].Values.OrderBy(x => x.Kind).ThenBy(x => x.Group).ToDictionary(x => x.Group, x => x);
			}

			internal void SyncOutfitVirtualGroupInfo(int _coordinateIndex)
			{
				if (!TriggerEnabled)
					return;

				OutfitTriggerInfo OutfitTriggerInfo = CharaTriggerInfo[_coordinateIndex];
				foreach (AccTriggerInfo _trigger in OutfitTriggerInfo.Parts.Values)
				{
					if (_trigger.Kind >= 9)
					{
						if (CharaVirtualGroupInfo[_coordinateIndex].ContainsKey(_trigger.Group)) continue;

						string _group = _trigger.Group;
						int Kind = _trigger.Kind;
						CharaVirtualGroupInfo[_coordinateIndex][_group] = new VirtualGroupInfo(_group, Kind);
						_logger.LogError($"[SyncOutfitVirtualGroupInfo][{CharaFullName}][CoordinateIndex: {_coordinateIndex}] \"{_group}\" added missing");
					}
				}

				List<string> _filtered = CharaVirtualGroupInfo[_coordinateIndex]?.Values?.Where(x => x.Kind == 9)?.GroupBy(x => x.Group)?.Select(x => x.First().Group)?.ToList();

				if (_filtered?.Count() > 0)
				{
					foreach (string _group in _filtered)
					{
						if (CharaTriggerInfo[_coordinateIndex]?.Parts?.Values?.FirstOrDefault(x => x.Group == _group) == null)
						{
							CharaVirtualGroupInfo[_coordinateIndex].Remove(_group);
							_logger.LogError($"[SyncOutfitVirtualGroupNames][{CharaFullName}][CoordinateIndex: {_coordinateIndex}] \"{_group}\" unused and removed");
						}
					}
				}

				PadOutfitVirtualGroupInfo(_coordinateIndex);
				SortOutfitVirtualGroupInfo(_coordinateIndex);
			}

			internal void ToggleByClothesState(int _kind, int _state)
			{
				if (!TriggerEnabled)
					return;
				if (CharaTriggerInfo[_currentCoordinateIndex]?.Parts?.Count() == 0)
					return;

				DebugMsg(LogLevel.Info, $"[ToggleByClothesState][{CharaFullName}] Fired!!");

				if (!MathfEx.RangeEqualOn(0, _kind, 6))
					return;
				List<AccTriggerInfo> _triggers = GetPartsOfKind(_kind);
				if (_triggers.Count() > 0)
				{
					foreach (AccTriggerInfo _trigger in _triggers)
						ShowAccessory(_trigger.Slot, _trigger.State[_state]);
				}

				int _relKind = -1;
				int _relState = -1;

				DebugMsg(LogLevel.Info, $"[ToggleByClothesState][{CharaFullName}][kind: {_kind}][notBot: {ChaControl.notBot}][notShorts: {ChaControl.notShorts}]");

				if ((_kind == 0) && (ChaControl.notBot))
				{
					_relKind = 1;
					_relState = (_state == 3) ? 3 : ChaControl.fileStatus.clothesState[_relKind];
				}
				else if ((_kind == 1) && (ChaControl.notBot))
				{
					_relKind = 0;
					_relState = (_state == 3) ? 3 : ChaControl.fileStatus.clothesState[_relKind];
				}
				else if ((_kind == 2) && (ChaControl.notShorts))
				{
					_relKind = 3;
					_relState = (_state == 3) ? 3 : ChaControl.fileStatus.clothesState[_relKind];
				}
				else if ((_kind == 3) && (ChaControl.notShorts))
				{
					_relKind = 2;
					_relState = (_state == 3) ? 3 : ChaControl.fileStatus.clothesState[_relKind];
				}

				if ((_relKind > -1) && (_relState > -1))
				{
					_triggers = GetPartsOfKind(_relKind);
					if (_triggers.Count() > 0)
					{
						foreach (AccTriggerInfo _trigger in _triggers)
							ShowAccessory(_trigger.Slot, _trigger.State[_relState]);
					}
				}
			}

			internal void ToggleByShoesType(int _kind, int _state)
			{
				if (!TriggerEnabled)
					return;
				if (CharaTriggerInfo[_currentCoordinateIndex]?.Parts?.Count() == 0)
					return;

				DebugMsg(LogLevel.Info, $"[ToggleByShoesType][{CharaFullName}] Fired!!");

				if ((_kind != 7) && (_kind != 8))
					return;
				int _cur = (ChaControl.fileStatus.shoesType == 0) ? 7 : 8;
				int _off = (_cur == 7) ? 8 : 7;
				List<AccTriggerInfo> _triggers = GetPartsOfKind(_cur);
				if (_triggers.Count() > 0)
				{
					foreach (AccTriggerInfo _trigger in _triggers)
						ShowAccessory(_trigger.Slot, _trigger.State[_state]);
				}
				_triggers = GetPartsOfKind(_off);
				if (_triggers.Count() > 0)
				{
					foreach (AccTriggerInfo _trigger in _triggers)
						ShowAccessory(_trigger.Slot, false);
				}
			}

			internal IEnumerator SyncAllAccToggleCoroutine()
			{
				yield return new WaitForEndOfFrame();
				yield return new WaitForEndOfFrame();

				SyncAllAccToggle();
			}

			internal void SyncAllAccToggle()
			{
				if (!TriggerEnabled)
					return;
				if (CharaTriggerInfo[_currentCoordinateIndex]?.Parts?.Count() == 0)
					return;

				DebugMsg(LogLevel.Info, $"[SyncAllAccToggle][{CharaFullName}] Fired!!");

				if (JetPack.CharaMaker.Inside && !_cfgCharaMakerPreview.Value)
				{
					DebugMsg(LogLevel.Info, $"[SyncAllAccToggle][{CharaFullName}] Disabled by config");
					return;
				}
				// https://stackoverflow.com/questions/19406242/select-distinct-using-linq
				List<int> _kinds = CharaTriggerInfo[_currentCoordinateIndex].Parts.Values.OrderBy(x => x.Kind)?.GroupBy(x => x.Kind)?.Select(x => x.First().Kind)?.ToList();
				if (_kinds?.Count() == 0)
				{
					DebugMsg(LogLevel.Info, $"[SyncAllAccToggle][{CharaFullName}] Nothing to trigger");
					return;
				}
				// https://stackoverflow.com/questions/1528724/converting-a-listint-to-a-comma-separated-string
				DebugMsg(LogLevel.Info, $"[SyncAllAccToggle][{CharaFullName}][Kinds: {string.Join(",", _kinds.Select(x => x.ToString()).ToArray())}]");

				foreach (AccTriggerInfo _trigger in CharaTriggerInfo[_currentCoordinateIndex].Parts.Values)
				{
					if (MathfEx.RangeEqualOn(0, _trigger.Kind, 6))
					{
						int _state = ChaControl.fileStatus.clothesState[_trigger.Kind];
						bool _show = _trigger.State[_state];
						DebugMsg(LogLevel.Info, $"[SyncAllAccToggle][{CharaFullName}][slot: {_trigger.Slot}][kind: {_trigger.Kind}][state: {_state}][vis: {_show}]");
						ShowAccessory(_trigger.Slot, _show);
					}
					else if ((_trigger.Kind == 7) || (_trigger.Kind == 8))
					{
						int _kind = (ChaControl.fileStatus.shoesType == 0) ? 7 : 8;
						bool _show = false;
						if (_kind == _trigger.Kind)
							_show = _trigger.State[ChaControl.fileStatus.clothesState[_trigger.Kind]];
						ShowAccessory(_trigger.Slot, _show);
					}
					else if ((_trigger.Kind >= 9) && (!_trigger.Group.IsNullOrEmpty()))
					{
						bool _show = true;
						if (CharaVirtualGroupInfo[_currentCoordinateIndex].ContainsKey(_trigger.Group))
							_show = CharaVirtualGroupInfo[_currentCoordinateIndex][_trigger.Group].State ? _trigger.State[0] : _trigger.State[3];
						ShowAccessory(_trigger.Slot, _show);
					}
				}
			}

			internal void ToggleByVirtualGroup(string _group, bool _state)
			{
				List<AccTriggerInfo> _filtered = GetPartsOfGroup(_group);
				DebugMsg(LogLevel.Info, $"[ToggleByVirtualGroup][{CharaFullName}][group: {_group}][show: {_state}][count: {_filtered.Count}]");
				foreach (AccTriggerInfo _trigger in _filtered)
				{
					bool _show = _state ? _trigger.State[0] : _trigger.State[3];
					ShowAccessory(_trigger.Slot, _show);
					DebugMsg(LogLevel.Info, $"[ToggleByVirtualGroup][{CharaFullName}][Part.Slot: {_trigger.Slot}][show: {_state}]");
				}
			}

			internal IEnumerator InitCurOutfitTriggerInfoCoroutine(string _caller)
			{
				yield return new WaitForEndOfFrame();
				yield return new WaitForEndOfFrame();

				InitCurOutfitTriggerInfo(_caller);
			}

			internal void InitCurOutfitTriggerInfo(string _caller)
			{
				DebugMsg(LogLevel.Info, $"[InitCurOutfitTriggerInfo][{CharaFullName}] Fired!!");

				TriggerEnabled = false;

				NullCheckOutfitTriggerInfo(_currentCoordinateIndex);
				DebugMsg(LogLevel.Info, $"[InitCurOutfitTriggerInfo] CurOutfitTriggerInfo.Parts.Count() {CharaTriggerInfo[_currentCoordinateIndex].Parts.Count()}");

				if ((!JetPack.CharaMaker.Inside) && (CharaTriggerInfo[_currentCoordinateIndex].Parts.Count() == 0))
				{
					DebugMsg(LogLevel.Info, $"[InitOutfitTriggerInfo][{CharaFullName}] TriggerEnabled false");
					if (JetPack.CharaStudio.Loaded)
					{
						DebugMsg(LogLevel.Info, $"[InitCurOutfitTriggerInfo][CurTreeNodeObjID: {CharaStudio._curTreeNodeObjID}][TreeNodeObjID: {_treeNodeObjID}]");
						if (CharaStudio._curTreeNodeObjID == _treeNodeObjID)
							CharaStudio.ClearUI();
					}
					else if (JetPack.CharaHscene.Loaded)
					{
						CharaHscene.ClearUI();
						CharaHscene.UpdateUI();
					}
					return;
				}

				TriggerEnabled = true;

				SyncOutfitVirtualGroupInfo(_currentCoordinateIndex);

				if (JetPack.CharaMaker.Loaded)
				{
					if (!_cfgPreserveVirtualGroupState.Value)
						ResetOutfitVirtualGroupState(_currentCoordinateIndex);
					SetAccessoryStateAll(true);
					CharaMaker._imgTglCol01.isOn = true;
					CharaMaker._imgTglCol02.isOn = true;
					SkipSlotChangePartTypeCheck = true;
					StartCoroutine(AccSlotChangedHandlerCoroutine(AccessoriesApi.SelectedMakerAccSlot));
				}
				else if (JetPack.CharaStudio.Loaded)
				{
					DebugMsg(LogLevel.Info, $"[InitCurOutfitTriggerInfo][CurTreeNodeObjID: {CharaStudio._curTreeNodeObjID}][TreeNodeObjID: {_treeNodeObjID}]");
					if (_caller != "OnReload")
					{
						SetAccessoryStateAll(true);
						SyncAllAccToggle();
					}

					if (CharaStudio._curTreeNodeObjID == _treeNodeObjID)
						CharaStudio.UpdateUI();
				}
				else if (JetPack.CharaHscene.Loaded)
				{
					CharaHscene.ClearUI();
					CharaHscene.UpdateUI();
					SetAccessoryStateAll(true);
					SyncAllAccToggle();
				}
				else
				{
					SetAccessoryStateAll(true);
					SyncAllAccToggle();
				}
			}
		}
	}
}
