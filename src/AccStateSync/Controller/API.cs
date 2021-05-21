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
			public List<AccTriggerInfo> GetPartsOfKind(int _kind, string _group = "") => GetPartsOfKind(_currentCoordinateIndex, _kind, _group);
			public List<AccTriggerInfo> GetPartsOfKind(int _coordinateIndex, int _kind, string _group = "")
			{
				List<AccTriggerInfo> _filtered = CharaTriggerInfo[_coordinateIndex].Parts.Values.Where(x => x.Kind == _kind).OrderBy(x => x.Slot).ToList();
				if (!_group.IsNullOrEmpty())
					_filtered = _filtered.Where(x => x.Group == _group).ToList();
				DebugMsg(LogLevel.Info, $"[GetPartsOfKind][{CharaFullName}][Coordinate: {_coordinateIndex}][Kind: {_kind}][Group: {_group}][Count: {_filtered.Count}][Slot: {string.Join(",", _filtered.Select(x => x.Slot.ToString()).ToArray())}]");
				return _filtered ?? new List<AccTriggerInfo>();
			}

			public List<AccTriggerInfo> GetPartsOfGroup(string _group) => GetPartsOfGroup(_currentCoordinateIndex, _group);
			public List<AccTriggerInfo> GetPartsOfGroup(int _coordinateIndex, string _group)
			{
				if (_group.IsNullOrEmpty())
					return new List<AccTriggerInfo>();

				List<AccTriggerInfo> _filtered = CharaTriggerInfo[_coordinateIndex].Parts.Values.Where(x => x.Group == _group).OrderBy(x => x.Slot).ToList();
				DebugMsg(LogLevel.Info, $"[GetPartsOfGroup][{CharaFullName}][Coordinate: {_coordinateIndex}][Group: {_group}][Count: {_filtered.Count}][Slot: {string.Join(",", _filtered.Select(x => x.Slot.ToString()).ToArray())}]");
				return _filtered ?? new List<AccTriggerInfo>();
			}

			public void CloneSlotTriggerInfo(int _srcSlotIndex, int _dstSlotIndex) => CloneSlotTriggerInfo(_srcSlotIndex, _dstSlotIndex, _currentCoordinateIndex, _currentCoordinateIndex);
			public void CloneSlotTriggerInfo(int _srcSlotIndex, int _dstSlotIndex, int _coordinateIndex) => CloneSlotTriggerInfo(_srcSlotIndex, _dstSlotIndex, _coordinateIndex, _coordinateIndex);
			public void CloneSlotTriggerInfo(int _srcSlotIndex, int _dstSlotIndex, int _srcCoordinateIndex, int _dstCoordinateIndex)
			{
				CharaTriggerInfo[_dstCoordinateIndex].Parts.Remove(_dstSlotIndex);
				if (CharaTriggerInfo[_srcCoordinateIndex].Parts.ContainsKey(_srcSlotIndex))
				{
					CharaTriggerInfo[_dstCoordinateIndex].Parts[_dstSlotIndex] = new AccTriggerInfo(_dstSlotIndex);
					CopySlotTriggerInfo(CharaTriggerInfo[_srcCoordinateIndex].Parts[_srcSlotIndex], CharaTriggerInfo[_dstCoordinateIndex].Parts[_dstSlotIndex]);
					CharaTriggerInfo[_dstCoordinateIndex].Parts[_dstSlotIndex].Slot = _dstSlotIndex;
				}
			}

			internal void SetAccessoryStateAll(bool _show)
			{
				int _count = ChaControl.ListPartsInfo(_currentCoordinateIndex).Count;
				for (int i = 0; i < _count; i++)
					ShowAccessory(i, _show);
			}

			internal void SetAccessoryStateCategory(int _cateNo, bool _show)
			{
				List<ChaFileAccessory.PartsInfo> _partsInfo = ChaControl.ListPartsInfo(_currentCoordinateIndex);
				for (int i = 0; i < _partsInfo.Count; i++)
				{
					if (_partsInfo[i].hideCategory == _cateNo)
						ShowAccessory(i, _show);
				}
			}

			internal void ShowAccessory(int _slot, bool _show)
			{
				ChaControl.SetAccessoryState(_slot, _show);
			}

			internal void ToggleByAccTriggerInfo(int _slotIndex)
			{
				if (CharaTriggerInfo[_currentCoordinateIndex].Parts.ContainsKey(_slotIndex))
					ToggleByAccTriggerInfo(CharaTriggerInfo[_currentCoordinateIndex].Parts[_slotIndex]);
			}
			internal void ToggleByAccTriggerInfo(AccTriggerInfo _trigger)
			{
				if (MathfEx.RangeEqualOn(0, _trigger.Kind, 6))
				{
					int _state = 0;
					if (_trigger.Kind == 0 && ChaControl.notBot)
						_state = (ChaControl.fileStatus.clothesState[1] == 3) ? 3 : ChaControl.fileStatus.clothesState[0];
					else if (_trigger.Kind == 1 && ChaControl.notBot)
						_state = (ChaControl.fileStatus.clothesState[0] == 3) ? 3 : ChaControl.fileStatus.clothesState[1];
					else if (_trigger.Kind == 2 && ChaControl.notShorts)
						_state = (ChaControl.fileStatus.clothesState[3] == 3) ? 3 : ChaControl.fileStatus.clothesState[2];
					else if (_trigger.Kind == 3 && ChaControl.notShorts)
						_state = (ChaControl.fileStatus.clothesState[2] == 3) ? 3 : ChaControl.fileStatus.clothesState[3];
					else
						_state = ChaControl.fileStatus.clothesState[_trigger.Kind];

					bool _show = _trigger.State[_state];
					ShowAccessory(_trigger.Slot, _show);
				}
				else if ((_trigger.Kind == 7) || (_trigger.Kind == 8))
				{
					int _kind = (ChaControl.fileStatus.shoesType == 0) ? 7 : 8;
					bool _show = false;
					if (_kind == _trigger.Kind)
						_show = _trigger.State[ChaControl.fileStatus.clothesState[_kind]];
					ShowAccessory(_trigger.Slot, _show);
				}
				else if (_trigger.Kind >= 9)
				{
					bool _show = true;
					if (!_trigger.Group.IsNullOrEmpty() && CharaVirtualGroupInfo[_currentCoordinateIndex].ContainsKey(_trigger.Group))
						_show = CharaVirtualGroupInfo[_currentCoordinateIndex][_trigger.Group].State ? _trigger.State[0] : _trigger.State[3];
					ShowAccessory(_trigger.Slot, _show);
				}
			}
		}
	}
}
