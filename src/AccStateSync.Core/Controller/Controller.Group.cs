using System;
using System.Collections.Generic;
using System.Linq;

using UniRx;

using JetPack;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		public partial class AccStateSyncController
		{
			internal TriggerGroup GetTriggerGroup(int _kind) => GetTriggerGroup(_currentCoordinateIndex, _kind);
			internal TriggerGroup GetTriggerGroup(int _coordinate, int _kind)
			{
				return TriggerGroupList.FirstOrDefault(x => x.Coordinate == _coordinate && x.Kind == _kind);
			}

			internal TriggerGroup GetTriggerGroupByGUID(int _coordinate, string _guid)
			{
				return TriggerGroupList.FirstOrDefault(x => x.Coordinate == _coordinate && x.GUID == _guid);
			}

			internal TriggerGroup NewOrGetTriggerGroup(int _coordinate, int _kind)
			{
				TriggerGroup _group = GetTriggerGroup(_coordinate, _kind);
				if (_group == null)
				{
					_group = new TriggerGroup(_coordinate, _kind);
					TriggerGroupList.Add(_group);
				}
				return _group;
			}

			internal void SetGroupState(int _kind, int _state)
			{
				if (!_cachedGroupKind.Contains(_kind))
					return;
				TriggerGroup _group = _cachedCoordinateGroupList.Where(x => x.Kind == _kind).First();
				_group.State = _state;
				ToggleByRefKind(_kind);
			}

			internal void SetGroupStateNext(int _kind)
			{
				if (!_cachedGroupKind.Contains(_kind))
					return;
				TriggerGroup _group = _cachedCoordinateGroupList.Where(x => x.Kind == _kind).First();
				int _state = _group.State;
				List<int> _states = _group.States.OrderBy(x => x.Key).Select(x => x.Key).ToList();
				int _index = _states.IndexOf(_state);
				if (_index == _states.Count - 1)
					_group.State = _states[0];
				else
					_group.State = _states[_index + 1];
				ToggleByRefKind(_kind);
			}

			internal void SetSecondaryGroup(int _kind, int _secondary) => SetSecondaryGroup(_currentCoordinateIndex, _kind, _secondary);
			internal void SetSecondaryGroup(int _coordinate, int _kind, int _secondary)
			{
				TriggerGroup _group = GetTriggerGroup(_coordinate, _kind);
				if (_group == null) return;
				_group.Secondary = _secondary;
			}

			internal TriggerGroup CreateTriggerGroup() => CreateTriggerGroup(_currentCoordinateIndex);
			internal TriggerGroup CreateTriggerGroup(int _coordinate)
			{
				TriggerGroup _group = new TriggerGroup(_coordinate, GetNextGroupID(_coordinate));
				TriggerGroupList.Add(_group);
				return _group;
			}

			internal void RenameTriggerGroup(int _kind, string _label) => RenameTriggerGroup(_currentCoordinateIndex, _kind, _label);
			internal void RenameTriggerGroup(int _coordinate, int _kind, string _label)
			{
				TriggerGroup _group = GetTriggerGroup(_coordinate, _kind);
				if (_group == null) return;
				_group.Rename(_label);
			}

			internal void RenameTriggerGroupState(int _kind, int _state, string _label) => RenameTriggerGroupState(_currentCoordinateIndex, _kind, _state, _label);
			internal void RenameTriggerGroupState(int _coordinate, int _kind, int _state, string _label)
			{
				TriggerGroup _group = GetTriggerGroup(_coordinate, _kind);
				if (_group == null) return;
				_group.RenameState(_state, _label);
			}

			internal void RemoveTriggerGroup(int _coordinate, int _kind)
			{
				TriggerPropertyList.RemoveAll(x => x.Coordinate == _coordinate && x.RefKind == _kind);
				TriggerGroupList.RemoveAll(x => x.Coordinate == _coordinate && x.Kind == _kind);
			}

			internal void RemoveTriggerGroupState(int _coordinate, int _kind, int _state)
			{
				TriggerGroup _group = GetTriggerGroup(_coordinate, _kind);
				if (_group == null) return;
				if (!_group.States.ContainsKey(_state)) return;

				if (_state == 0 || _state == 1)
				{
					_logger.LogMessage($"Removing the first two states is restricted");
					return;
				}

				TriggerPropertyList.RemoveAll(x => x.Coordinate == _coordinate && x.RefKind == _kind && x.RefState == _state);
				_group.States.Remove(_state);

				if (_group.Startup == _state)
					_group.Startup = 0;

				if (_group.State == _state)
					_group.State = _group.Startup;
			}

			internal void CloneAsNewTriggerGroupState(int _coordinate, int _kind, int _state)
			{
				TriggerGroup _group = GetTriggerGroup(_coordinate, _kind);
				if (_group == null) return;
				if (!_group.States.ContainsKey(_state)) return;

				int _newState = _group.AddNewState();

				TriggerPropertyList.RemoveAll(x => x.Coordinate == _coordinate && x.RefKind == _kind && x.RefState == _newState);
				List<TriggerProperty> _tempTriggerProperty = TriggerPropertyList.Where(x => x.Coordinate == _coordinate && x.RefKind == _kind && x.RefState == _state).ToList().JsonClone() as List<TriggerProperty>;
				if (_tempTriggerProperty?.Count == 0) return;
				foreach (TriggerProperty x in _tempTriggerProperty)
					x.RefState = _newState;
				TriggerPropertyList.AddRange(_tempTriggerProperty);
			}

			internal int GetNextGroupID(int _coordinate)
			{
				if (!TriggerGroupList.Any(x => x.Coordinate == _coordinate))
					return 9;
				return TriggerGroupList.Where(x => x.Coordinate == _coordinate).OrderByDescending(x => x.Kind).First().Kind + 1;
			}

			internal void PackGroupID(int _coordinate)
			{
				Dictionary<int, int> _mapping = new Dictionary<int, int>();
				List<TriggerProperty> _tempTriggerProperty = TriggerPropertyList.Where(x => x.Coordinate == _coordinate && x.RefKind >= 9).OrderBy(x => x.RefKind).ToList();
				List<int> _tempGroupKind = new HashSet<int>(_tempTriggerProperty.Select(x => x.RefKind)).ToList();

				for (int i = 9; i < _tempGroupKind.Count; i++)
				{
					if (_tempGroupKind[i] != i)
						_mapping[i] = _tempGroupKind[i];
				}

				if (_mapping.Count == 0) return;

				foreach (TriggerProperty x in TriggerPropertyList)
				{
					if (x.Coordinate != _coordinate) continue;
					if (x.RefKind < 9 || !_mapping.ContainsKey(x.RefKind)) continue;
					x.RefKind = _mapping[x.RefKind];
				}

				foreach (TriggerGroup x in TriggerGroupList)
				{
					if (x.Coordinate != _coordinate) continue;
					if (!_mapping.ContainsKey(x.Kind)) continue;
					x.Kind = _mapping[x.Kind];
				}
			}

			internal void MissingPartCheck(int _coordinate)
			{
				List<ChaFileAccessory.PartsInfo> _partsInfo = ChaControl.ListPartsInfo(_coordinate);
				HashSet<int> _slots = new HashSet<int>(TriggerPropertyList.Where(x => x.Coordinate == _coordinate).OrderBy(x => x.Slot).Select(x => x.Slot));
				string _name = Enum.GetNames(typeof(ChaFileDefine.CoordinateType)).ElementAtOrDefault(_coordinate);

				foreach (int _slot in _slots)
				{
					if (_slot >= _partsInfo.Count || _partsInfo[_slot].type == 120)
					{
						TriggerPropertyList.RemoveAll(x => x.Coordinate == _coordinate && x.Slot == _slot);
						_logger.LogMessage($"Triggers on {_name} slot {_slot + 1:00} removed because no accessory assigned for that slot");
					}
				}
			}

			internal void MissingKindCheck(int _coordinate)
			{
				List<TriggerProperty> _list = TriggerPropertyList.Where(x => x.Coordinate == _coordinate && x.RefKind < 9).OrderBy(x => x.RefKind).ToList();
				for (int i = 0; i <= 8; i++)
				{
					if (ChaControl.GetClothesStateKind(i) == null && _list.Where(x => x.RefKind == i).ToList().Count > 0)
					{
						RemoveKindTriggerProperty(_coordinate, i);
						_logger.LogMessage($"Triggers removed because no clothes assigned for {_clothesNames[i]}");
					}
				}
			}

			internal void MissingPropertyCheck(int _coordinate)
			{
				string _name = GetCordName(ChaControl, _coordinate);
				HashSet<int> _kinds = new HashSet<int>(TriggerPropertyList.Where(x => x.Coordinate == _coordinate).OrderBy(x => x.RefKind).Select(x => x.RefKind));
				foreach (int _kind in _kinds)
				{
					HashSet<int> _slots = new HashSet<int>(TriggerPropertyList.Where(x => x.Coordinate == _coordinate && x.RefKind == _kind).OrderBy(x => x.Slot).Select(x => x.Slot));
					foreach (int _slot in _slots)
					{
						List<TriggerProperty> _list = TriggerPropertyList.Where(x => x.Coordinate == _coordinate && x.RefKind == _kind && x.Slot == _slot).OrderBy(x => x.RefState).ToList();
						if (_kind < 9 && _list.Count < 4)
						{
							for (int _state = 0; _state <= 3; _state++)
							{
								if (_list.Any(x => x.RefState == _state))
									continue;
								TriggerPropertyList.Add(new TriggerProperty(_coordinate, _slot, _kind, _state));
								_logger.LogMessage($"Triggers on {_name} slot {_slot + 1:00} for group {_clothesNames[_kind]} state {_statesNames[_state]} added because of absent");
							}
						}
						else if (_kind >= 9)
						{
							TriggerGroup _group = GetTriggerGroup(_coordinate, _kind);
							foreach (int _state in _group.States.Keys)
							{
								if (_list.Any(x => x.RefState == _state))
									continue;
								TriggerPropertyList.Add(new TriggerProperty(_coordinate, _slot, _kind, _state));
								_logger.LogMessage($"Triggers on {_name} slot {_slot + 1:00} for group {_group.Label} state {_group.States[_state]} added because of absent");
							}
						}
					}
				}
			}

			internal void MissingGroupCheck(int _coordinate)
			{
				string _name = GetCordName(ChaControl, _coordinate);
				HashSet<int> _kinds = new HashSet<int>(TriggerPropertyList.Where(x => x.Coordinate == _coordinate && x.RefKind >= 9).OrderBy(x => x.RefKind).Select(x => x.RefKind));
				foreach (int _kind in _kinds)
				{
					HashSet<int> _states = new HashSet<int>(TriggerPropertyList.Where(x => x.Coordinate == _coordinate && x.RefKind == _kind).OrderBy(x => x.RefState).Select(x => x.RefState));
					TriggerGroup _group = GetTriggerGroup(_coordinate, _kind);
					if (_group == null)
					{
						_logger.LogMessage($"Group info on {_name} for group {_group.Label} added because of absent");
						_group = new TriggerGroup(_coordinate, _kind);
					}
					foreach (int _state in _states)
					{
						if (_group.States.ContainsKey(_state)) continue;
						_group.AddNewState(_state);
						_logger.LogMessage($"State info on {_name} for group {_group.Label} state {_group.States[_state]} added because of absent");
					}
				}
			}
		}
	}
}
