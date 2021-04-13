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
			public List<AccTriggerInfo> GetPartsOfKind(int _kind, string _group = "")
			{
				if (!CharaTriggerInfo.ContainsKey(_currentCoordinateIndex))
					return new List<AccTriggerInfo>();
				List<AccTriggerInfo> _filtered = CharaTriggerInfo[_currentCoordinateIndex].Parts.Values.Where(x => x.Kind == _kind).ToList();
				if (!_group.IsNullOrEmpty())
					_filtered = _filtered.Where(x => x.Group == _group).ToList();
				return _filtered;
			}

			public List<AccTriggerInfo> GetPartsOfGroup(string _group)
			{
				if (_group.IsNullOrEmpty())
					return new List<AccTriggerInfo>();
				if (!CharaTriggerInfo.ContainsKey(_currentCoordinateIndex))
					return new List<AccTriggerInfo>();
				return CharaTriggerInfo[_currentCoordinateIndex].Parts.Values.Where(x => x.Group == _group).ToList();
			}

			public void CloneSlotTriggerInfo(int _srcSlotIndex, int _dstSlotIndex) => CloneSlotTriggerInfo(_srcSlotIndex, _dstSlotIndex, _currentCoordinateIndex, _currentCoordinateIndex);
			public void CloneSlotTriggerInfo(int _srcSlotIndex, int _dstSlotIndex, int _coordinateIndex) => CloneSlotTriggerInfo(_srcSlotIndex, _dstSlotIndex, _coordinateIndex, _coordinateIndex);
			public void CloneSlotTriggerInfo(int _srcSlotIndex, int _dstSlotIndex, int _srcCoordinateIndex, int _dstCoordinateIndex)
			{
				if (CharaTriggerInfo[_dstCoordinateIndex].Parts.ContainsKey(_dstSlotIndex))
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
				int _count = ChaControl.ListPartsInfo(_currentCoordinateIndex).Count;
				if (_slot < _count)
				{
					if (ChaControl.GetAccessoryVisibility(_slot) != _show)
						ChaControl.SetAccessoryState(_slot, _show);
				}
#if DEBUG
				else
					_logger.LogError($"[ShowAccessory][{CharaFullName}][slot: {_slot}] out of range of {_count}");
#endif
			}
		}
	}
}
