using System.Collections.Generic;
using System.Linq;

using UniRx;

using BepInEx.Logging;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		public partial class AccStateSyncController
		{
			internal bool GroupExist(string _group) => GroupExist(_currentCoordinateIndex, _group);
			internal bool GroupExist(int _coordinateIndex, string _group) => CharaVirtualGroupInfo[_coordinateIndex].ContainsKey(_group);

			internal void RenameGroup(string _group, string _label) => RenameGroup(_currentCoordinateIndex, _group, _label);
			internal void RenameGroup(int _coordinateIndex, string _group, string _label)
			{
				if (!GroupExist(_coordinateIndex, _group))
				{
					_logger.LogMessage($"Invalid group {_group}");
					return;
				}
				CharaVirtualGroupInfo[_coordinateIndex][_group].Label = _label;
				_logger.LogMessage($"[{_group}] renamed into {_label}");
			}

			internal void RemoveGroup(string _group) => RemoveGroup(_currentCoordinateIndex, _group);
			internal void RemoveGroup(int _coordinateIndex, string _group)
			{
				if (!GroupExist(_coordinateIndex, _group))
				{
					_logger.LogMessage($"Invalid group {_group}");
					return;
				}
				VirtualGroupInfo _info = CharaVirtualGroupInfo[_coordinateIndex][_group];
				CharaVirtualGroupInfo[_coordinateIndex].Remove(_group);
				_logger.LogMessage($"[{_info.Group}][{_info.Label}] removed");
			}

			internal void CreateGroup(int _kind, string _group) => CreateGroup(_currentCoordinateIndex, _kind, _group);
			internal void CreateGroup(int _coordinateIndex, int _kind, string _group)
			{
				if (CharaVirtualGroupInfo[_coordinateIndex].ContainsKey(_group)) return;

				CharaVirtualGroupInfo[_coordinateIndex][_group] = new VirtualGroupInfo(_group, _kind);
				VirtualGroupInfo _info = CharaVirtualGroupInfo[_coordinateIndex][_group];
				_logger.LogMessage($"Group: {_info.Group} ({_info.Label}) created");
			}

			internal void SetSecondaryGroup(string _group, bool _secondary = true) => SetSecondaryGroup(_currentCoordinateIndex, _group, _secondary);
			internal void SetSecondaryGroup(int _coordinateIndex, string _group, bool _secondary = true)
			{
				if (!GroupExist(_coordinateIndex, _group))
				{
					_logger.LogMessage($"Invalid group {_group}");
					return;
				}
				VirtualGroupInfo _info = CharaVirtualGroupInfo[_coordinateIndex][_group];
				_info.Secondary = _secondary;
				_logger.LogMessage($"[{_info.Group}][{_info.Label}] set as {(_secondary ? "Secondary" : "Primary")}");
			}

			internal void PushGroup() => PushGroup(_currentCoordinateIndex);
			internal void PushGroup(int _coordinateIndex)
			{
				int _kind = CharaVirtualGroupInfo[_coordinateIndex].Values.Any(x => x.Kind > 9) ? CharaVirtualGroupInfo[_coordinateIndex].Values.Max(x => x.Kind) + 1 : 10;
				string _group = $"custom_{_kind - 9}";
				CreateGroup(_coordinateIndex, _kind, _group);
			}

			internal void PopGroup() => PopGroup(_currentCoordinateIndex);
			internal void PopGroup(int _coordinateIndex)
			{
				VirtualGroupInfo _info = CharaVirtualGroupInfo[_coordinateIndex].Values.OrderByDescending(x => x.Kind).FirstOrDefault();
				if (_info.Kind <= 9)
				{
					_logger.LogMessage($"Cannot go below 0 custom group");
					return;
				}
				if (GetPartsOfKind(_info.Kind).Count() > 0)
				{
					_logger.LogMessage($"Cannot remove [{_info.Group}][{_info.Label}] because it's being assigned by slots");
					return;
				}
				RemoveGroup(_coordinateIndex, _info.Group);
			}

			internal void PurgeGroup(string _group) => PurgeGroup(_currentCoordinateIndex, _group);
			internal void PurgeGroup(int _coordinateIndex, string _group)
			{
				if (!GroupExist(_coordinateIndex, _group))
				{
					_logger.LogMessage($"Invalid group {_group}");
					return;
				}
				VirtualGroupInfo _info = CharaVirtualGroupInfo[_coordinateIndex][_group];
				CharaVirtualGroupInfo[_coordinateIndex].Remove(_group);
				_logger.LogMessage($"[{_info.Group}][{_info.Label}] removed");

				List<AccTriggerInfo> _filtered = GetPartsOfGroup(_group);
				List<int> _slots = new List<int>();
				foreach (AccTriggerInfo _trigger in _filtered)
				{
					JetPack.Accessory.SetPartsInfo(ChaControl, _coordinateIndex, _trigger.Slot, new ChaFileAccessory.PartsInfo());
					_slots.Add(_trigger.Slot);
				}
				foreach (int _slot in _slots)
					CharaTriggerInfo[_coordinateIndex].Parts.Remove(_slot);
			}
		}
	}
}
