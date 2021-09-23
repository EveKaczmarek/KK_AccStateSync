using System.Collections;
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
			internal ChaFileAccessory.PartsInfo _curPartsInfo = null;

			internal IEnumerator AccSlotChangedHandlerCoroutine()
			{
				yield return JetPack.Toolbox.WaitForEndOfFrame;
				yield return JetPack.Toolbox.WaitForEndOfFrame;

				AccSlotChangedHandler();
			}

			internal void AccSlotChangedHandler()
			{
				if (!JetPack.CharaMaker.Loaded) return;
				int _slotIndex = CharaMaker._currentSlotIndex;
				_curPartsInfo = null;
				_cachedSlotPropertyList.Clear();

				DebugMsg(LogLevel.Info, $"[AccSlotChangedHandler][{CharaFullName}] Fired!!");

				DebugMsg(LogLevel.Info, $"[AccSlotChangedHandler][{CharaFullName}][SlotIndex]: {_slotIndex}");
				if (_slotIndex < 0)
				{
#if DEBUG
					_logger.LogError($"[AccSlotChangedHandler][{CharaFullName}] calling when SlotIndex = -1");
#endif
					RefreshPreview("AccSlotChangedHandler");
					return;
				}
				_curPartsInfo = ChaControl.GetPartsInfo(_slotIndex);
				if (_curPartsInfo == null)
				{
					DebugMsg(LogLevel.Warning, $"[AccSlotChangedHandler][{CharaFullName}] Cannot retrive info for Slot{_slotIndex + 1:00}");
					_accWinCtrlEnable.Visible.OnNext(false);
					RefreshPreview("AccSlotChangedHandler");
					return;
				}

				_accWinCtrlEnable.Visible.OnNext(_curPartsInfo.type > 120);

				_cachedSlotPropertyList = TriggerPropertyList.Where(x => x.Coordinate == _currentCoordinateIndex && x.Slot == _slotIndex).OrderBy(x => x.RefKind).ThenBy(x => x.RefState).ToList();

				RefreshPreview("AccSlotChangedHandler");
			}

			internal void AccessoriesCopiedHandler(int _srcCoordinateIndex, int _dstCoordinateIndex, List<int> _copiedSlotIndexes)
			{
				DebugMsg(LogLevel.Info, $"[AccessoriesCopiedHandler][{CharaFullName}][Soruce: {_srcCoordinateIndex}][Destination: {_dstCoordinateIndex}][CopiedSlotIndexes: {string.Join(",", _copiedSlotIndexes.Select(x => x.ToString()).ToArray())}]");

				foreach (int _slotIndex in _copiedSlotIndexes)
					CloneSlotTriggerProperty(_slotIndex, _slotIndex, _srcCoordinateIndex, _dstCoordinateIndex);

				if (_dstCoordinateIndex == _currentCoordinateIndex)
				{
					_duringLoadChange = false;
					RefreshCache();
					RefreshPreview("AccessoriesCopiedHandler");
				}
			}

			internal void AccessoryTransferredHandler(int _srcSlotIndex, int _dstSlotIndex) => AccessoryTransferredHandler(_srcSlotIndex, _dstSlotIndex, _currentCoordinateIndex);
			internal void AccessoryTransferredHandler(int _srcSlotIndex, int _dstSlotIndex, int _coordinateIndex)
			{
				DebugMsg(LogLevel.Info, $"[AccessoryTransferredHandler][{CharaFullName}] Fired!!");

				CloneSlotTriggerProperty(_srcSlotIndex, _dstSlotIndex, _coordinateIndex);

				if (_coordinateIndex == _currentCoordinateIndex)
				{
					_duringLoadChange = false;
					RefreshCache();
					RefreshPreview("AccessoryTransferredHandler");
				}
			}

			internal void AccessoryTypeChanged(JetPack.CharaMaker.AccessoryTypeChangedEventArgs _args)
			{
				if (_duringLoadChange)
				{
#if DEBUG
					//_logger.LogError($"[AccessoryTypeChanged] during load");
#endif
					return;
				}

				if (CharaMaker._currentSlotIndex != _args.SlotIndex)
					return;

				_curPartsInfo = _args.PartsInfo;
				_accWinCtrlEnable.Visible.OnNext(_curPartsInfo.type > 120);

				if (_curPartsInfo.type == 120)
				{
					RemoveSlotTriggerProperty(_currentCoordinateIndex, _args.SlotIndex);
					RefreshCache();
				}
			}
		}
	}
}
