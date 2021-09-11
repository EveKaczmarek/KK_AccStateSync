using System.Collections.Generic;
using System.Linq;

using UniRx;

using BepInEx.Logging;

using JetPack;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		public partial class AccStateSyncController
		{
			internal List<TriggerProperty> _cachedCoordinatePropertyList = new List<TriggerProperty>();
			internal List<TriggerProperty> _cachedCoordinateGroupPropertyList = new List<TriggerProperty>();
			internal List<TriggerGroup> _cachedCoordinateGroupList = new List<TriggerGroup>();

			internal HashSet<int> _cachedSlotIndex = new HashSet<int>(); // acc slots with triggers
			internal HashSet<int> _cachedGroupKind = new HashSet<int>();

			internal List<TriggerProperty> _cachedSlotPropertyList = new List<TriggerProperty>();

			internal void ClearCache()
			{
				_cachedCoordinatePropertyList.Clear();
				_cachedCoordinateGroupPropertyList.Clear();
				_cachedCoordinateGroupList.Clear();
				_cachedSlotIndex.Clear();
				_cachedGroupKind.Clear();
			}

			internal void RefreshCache()
			{
				ClearCache();

				_cachedCoordinatePropertyList = TriggerPropertyList.Where(x => x.Coordinate == _currentCoordinateIndex).OrderBy(x => x.Slot).ToList();
				_cachedSlotIndex = new HashSet<int>(_cachedCoordinatePropertyList.Select(x => x.Slot));
				_cachedCoordinateGroupPropertyList = _cachedCoordinatePropertyList.Where(x => x.RefKind >= 9).ToList();
				_cachedCoordinateGroupList = TriggerGroupList.Where(x => x.Coordinate == _currentCoordinateIndex).OrderBy(x => x.Kind).ToList();
				_cachedGroupKind = new HashSet<int>(_cachedCoordinateGroupList.Select(x => x.Kind));
			}

			internal Dictionary<int, int> _ListClothesStates()
			{
				Dictionary<int, int> _clothesStates = new Dictionary<int, int>();
				for (int i = 0; i < _clothesNames.Count; i++)
				{
					if (ChaControl.GetClothesStateKind(i) == null) continue;

					_clothesStates[i] = ChaControl.fileStatus.clothesState[i];

					if (i == 0 && ChaControl.notBot && ChaControl.fileStatus.clothesState[1] == 3)
						_clothesStates[i] = 3;
					if (i == 1 && ChaControl.notBot && ChaControl.fileStatus.clothesState[0] == 3)
						_clothesStates[i] = 3;

					if (i == 2 && ChaControl.notShorts && ChaControl.fileStatus.clothesState[3] == 3)
						_clothesStates[i] = 3;
					if (i == 3 && ChaControl.notShorts && ChaControl.fileStatus.clothesState[2] == 3)
						_clothesStates[i] = 3;

					if (i == 7 && _shoesType != 0)
						_clothesStates[i] = 3;
					if (i == 8 && _shoesType != 1)
						_clothesStates[i] = 3;
				}

				if (_shoesType == 0 && ChaControl.GetClothesStateKind(7) == null)
					_clothesStates[7] = 3;
				if (_shoesType == 1 && ChaControl.GetClothesStateKind(8) == null)
					_clothesStates[8] = 3;

				return _clothesStates;
			}

			internal void ToggleByRefKind(int _refKind)
			{
				if (_duringLoadChange) return;
				if (JetPack.CharaMaker.Loaded && !_cfgCharaMakerPreview.Value) return;
				if (JetPack.CharaStudio.Loaded && !TriggerEnabled) return;

				Dictionary<int, TriggerProperty> _effectingPropertList = new Dictionary<int, TriggerProperty>();

				Dictionary<int, int> _clothesStates = _ListClothesStates();

				HashSet<int> _filtered = new HashSet<int>(_cachedCoordinatePropertyList.Where(x => x.RefKind == _refKind).Select(x => x.Slot));
				if (ChaControl.notBot && (_refKind == 0 || _refKind == 1))
					_filtered = new HashSet<int>(_cachedCoordinatePropertyList.Where(x => (x.RefKind == 0 || x.RefKind == 1)).Select(x => x.Slot));
				if (ChaControl.notShorts && (_refKind == 2 || _refKind == 3))
					_filtered = new HashSet<int>(_cachedCoordinatePropertyList.Where(x => (x.RefKind == 2 || x.RefKind == 3)).Select(x => x.Slot));
				if (_refKind == 7 || _refKind == 8)
					_filtered = new HashSet<int>(_cachedCoordinatePropertyList.Where(x => (x.RefKind == 7 || x.RefKind == 8)).Select(x => x.Slot));

				foreach (int _slot in _filtered)
				{
					TriggerProperty _effectingPropert = null;
					List<TriggerProperty> _slotPropertyList = _cachedCoordinatePropertyList.Where(x => x.Slot == _slot).OrderByDescending(x => x.Priority).ThenBy(x => x.RefKind).ThenBy(x => x.RefState).ToList();
					foreach (TriggerProperty x in _slotPropertyList)
					{
						DebugMsg(LogLevel.Info, $"[ToggleByRefKind][Slot: {x.Slot}][Priority: {x.Priority}][RefKind: {x.RefKind}][RefState: {x.RefState}][Visible: {x.Visible}]");

						if (MathfEx.RangeEqualOn(0, x.RefKind, 8))
						{
							if (!_clothesStates.ContainsKey(x.RefKind)) continue;
							if (_shoesType == 0 && x.RefKind == 8 && ChaControl.GetClothesStateKind(7) != null) continue;
							if (_shoesType == 1 && x.RefKind == 7 && ChaControl.GetClothesStateKind(8) != null) continue;

							if (_clothesStates[x.RefKind] == x.RefState)
							{
								_effectingPropert = x;
								break;
							}
						}
						else if (x.RefKind >= 9)
						{
							TriggerGroup _group = _cachedCoordinateGroupList.FirstOrDefault(y => y.Kind == x.RefKind);
							if (_group != null && _group.State == x.RefState)
							{
								_effectingPropert = x;
								break;
							}
						}
					}

					if (_effectingPropert != null)
						_effectingPropertList[_slot] = _effectingPropert;
				}

				foreach (TriggerProperty _trigger in _effectingPropertList.Values)
					ChaControl.SetAccessoryState(_trigger.Slot, _trigger.Visible);
			}

			internal void SyncAllAccToggle(string _caller)
			{
				if (_duringLoadChange) return;
				if (JetPack.CharaMaker.Loaded && !_cfgCharaMakerPreview.Value) return;
				if (JetPack.CharaStudio.Loaded && !TriggerEnabled) return;

				Dictionary<int, TriggerProperty> _effectingPropertList = new Dictionary<int, TriggerProperty>();

				Dictionary<int, int> _clothesStates = _ListClothesStates();

				foreach (int _slot in _cachedSlotIndex)
				{
					TriggerProperty _effectingPropert = null;
					List<TriggerProperty> _slotPropertyList = _cachedCoordinatePropertyList.Where(x => x.Slot == _slot).OrderByDescending(x => x.Priority).ThenBy(x => x.RefKind).ThenBy(x => x.RefState).ToList();
					foreach (TriggerProperty x in _slotPropertyList)
					{
						DebugMsg(LogLevel.Info, $"[SyncAllAccToggle][Slot: {x.Slot}][Priority: {x.Priority}][RefKind: {x.RefKind}][RefState: {x.RefState}][Visible: {x.Visible}]");

						if (MathfEx.RangeEqualOn(0, x.RefKind, 8))
						{
							if (!_clothesStates.ContainsKey(x.RefKind)) continue;
							if (_shoesType == 0 && x.RefKind == 8 && ChaControl.GetClothesStateKind(7) != null) continue;
							if (_shoesType == 1 && x.RefKind == 7 && ChaControl.GetClothesStateKind(8) != null) continue;

							if (_clothesStates[x.RefKind] == x.RefState)
							{
								_effectingPropert = x;
								break;
							}
						}
						else if (x.RefKind >= 9)
						{
							TriggerGroup _group = _cachedCoordinateGroupList.FirstOrDefault(y => y.Kind == x.RefKind);
							if (_group != null && _group.State == x.RefState)
							{
								_effectingPropert = x;
								break;
							}
						}
					}

					if (_effectingPropert != null)
						_effectingPropertList[_slot] = _effectingPropert;
				}

				foreach (TriggerProperty _trigger in _effectingPropertList.Values)
					ChaControl.SetAccessoryState(_trigger.Slot, _trigger.Visible);
			}

			internal void RefreshPreview(string _caller)
			{
				if (JetPack.CharaMaker.Inside && !_cfgCharaMakerPreview.Value) return;
				if (JetPack.CharaStudio.Loaded && !TriggerEnabled) return;

				SetAccessoryStateAll(true);
				SyncAllAccToggle(_caller);
			}

			internal TriggerProperty GetTriggerProperty(int _coordinate, int _slot, int _refKind, int _refState)
			{
				return TriggerPropertyList.FirstOrDefault(x => x.Coordinate == _coordinate && x.Slot == _slot && x.RefKind == _refKind && x.RefState == _refState);
			}

			internal TriggerProperty NewOrGetTriggerProperty(int _coordinate, int _slot, int _refKind, int _refState)
			{
				TriggerProperty _trigger = GetTriggerProperty(_coordinate, _slot, _refKind, _refState);
				if (_trigger == null)
				{
					_trigger = new TriggerProperty(_coordinate, _slot, _refKind, _refState);
					TriggerPropertyList.Add(_trigger);
				}
				return _trigger;
			}

			internal void RemoveKindTriggerProperty(int _coordinate, int _refKind)
			{
				TriggerPropertyList.RemoveAll(x => x.Coordinate == _coordinate && x.RefKind == _refKind);
			}

			internal void RemoveSlotTriggerProperty(int _coordinate, int _slot)
			{
				TriggerPropertyList.RemoveAll(x => x.Coordinate == _coordinate && x.Slot == _slot);
			}

			public void CloneSlotTriggerProperty(int _srcSlotIndex, int _dstSlotIndex) => CloneSlotTriggerProperty(_srcSlotIndex, _dstSlotIndex, _currentCoordinateIndex, _currentCoordinateIndex);
			public void CloneSlotTriggerProperty(int _srcSlotIndex, int _dstSlotIndex, int _coordinateIndex) => CloneSlotTriggerProperty(_srcSlotIndex, _dstSlotIndex, _coordinateIndex, _coordinateIndex);
			public void CloneSlotTriggerProperty(int _srcSlotIndex, int _dstSlotIndex, int _srcCoordinateIndex, int _dstCoordinateIndex)
			{
				RemoveSlotTriggerProperty(_dstCoordinateIndex, _dstSlotIndex);
				List<TriggerProperty> _triggers = TriggerPropertyList.Where(x => x.Coordinate == _srcCoordinateIndex && x.Slot == _srcSlotIndex).ToList();
				if (_triggers?.Count > 0)
				{
					foreach (TriggerProperty _trigger in _triggers)
					{
						TriggerProperty _copy = _trigger.JsonClone() as TriggerProperty;
						_copy.Coordinate = _dstCoordinateIndex;
						_copy.Slot = _dstSlotIndex;

						if (_srcCoordinateIndex != _dstCoordinateIndex && _copy.RefKind >= 9)
						{
							string _guid = TriggerGroupList.FirstOrDefault(x => x.Coordinate == _srcCoordinateIndex && x.Kind == _copy.RefKind)?.GUID;
							if (_guid.IsNullOrEmpty())
							{
								_logger.LogMessage($"Something seriously fucked up, don't save your card");
								continue;
							}

							TriggerGroup _dstGroup = TriggerGroupList.FirstOrDefault(x => x.Coordinate == _dstCoordinateIndex && x.GUID == _guid);
							if (_dstGroup == null)
							{
								TriggerGroup _clone = GetTriggerGroup(_srcCoordinateIndex, _trigger.RefKind).JsonClone() as TriggerGroup;
								_clone.Coordinate = _dstCoordinateIndex;

								if (TriggerGroupList.Any(x => x.Coordinate == _dstCoordinateIndex && x.Kind == _copy.RefKind))
								{
									int _kind = GetNextGroupID(_dstCoordinateIndex);
									_clone.Kind = _kind;
									_copy.RefKind = _kind;
									_clone.State = _clone.Startup;
									TriggerPropertyList.RemoveAll(x => x.Coordinate == _dstCoordinateIndex && x.RefKind == _kind);
								}

								TriggerGroupList.Add(_clone);
							}
							else
							{
								int _kind = _dstGroup.Kind;
								_copy.RefKind = _kind;
								int _state = _trigger.RefState;
								TriggerGroup _srcGroup = GetTriggerGroup(_srcCoordinateIndex, _kind);
								if (!_dstGroup.States.ContainsKey(_state))
								{
									_dstGroup.States.Add(_state, _srcGroup.States[_state]);
									TriggerPropertyList.RemoveAll(x => x.Coordinate == _dstCoordinateIndex && x.RefKind == _kind && x.RefState == _state);
									HashSet<int> _slots = new HashSet<int>(TriggerPropertyList.Where(x => x.Coordinate == _dstCoordinateIndex && x.RefKind == _kind && x.Slot != _dstSlotIndex).Select(x => x.Slot));
									if (_slots.Count > 0)
									{
										List<TriggerProperty> _tempTriggerProperty = new List<TriggerProperty>();
										foreach (int _slot in _slots)
											_tempTriggerProperty.Add(new TriggerProperty(_dstCoordinateIndex, _slot, _kind, _state));
										TriggerPropertyList.AddRange(_tempTriggerProperty);
									}
								}
							}
						}

						TriggerPropertyList.Add(_copy);
					}
				}
			}

			internal void SetAccessoryStateAll(bool _show)
			{
				if (JetPack.CharaMaker.Loaded)
				{
					CharaMaker._imgTglCol01.isOn = _show;
					CharaMaker._imgTglCol02.isOn = _show;
				}
				int _count = ChaControl.ListPartsInfo(_currentCoordinateIndex).Count;
				for (int i = 0; i < _count; i++)
					ChaControl.SetAccessoryState(i, _show);
			}

			internal void SetAccessoryStateCategory(int _cateNo, bool _show)
			{
				List<ChaFileAccessory.PartsInfo> _partsInfo = ChaControl.ListPartsInfo(_currentCoordinateIndex);
				for (int i = 0; i < _partsInfo.Count; i++)
				{
					if (_partsInfo[i].hideCategory == _cateNo)
						ChaControl.SetAccessoryState(i, _show);
				}
			}
		}
	}
}
