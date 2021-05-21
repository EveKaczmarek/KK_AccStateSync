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
			public AccTriggerInfo CurSlotTriggerInfo = null;
			internal ChaFileAccessory.PartsInfo _curPartsInfo = null;

			internal IEnumerator AccSlotChangedHandlerCoroutine(int _slotIndex)
			{
				yield return JetPack.Toolbox.WaitForEndOfFrame;
				yield return JetPack.Toolbox.WaitForEndOfFrame;

				AccSlotChangedHandler(_slotIndex);
			}

			internal void AccSlotChangedHandler(int _slotIndex)
			{
				if (!JetPack.CharaMaker.Loaded) return;
				SkipAutoSave = true;

				DebugMsg(LogLevel.Info, $"[AccSlotChangedHandler][{CharaFullName}] Fired!!");

				DebugMsg(LogLevel.Info, $"[AccSlotChangedHandler][{CharaFullName}][SlotIndex]: {_slotIndex}");
				if (_slotIndex < 0)
				{
#if DEBUG
					_logger.LogError($"[AccSlotChangedHandler][{CharaFullName}] calling when SlotIndex = -1");
#endif
					CurSlotTriggerInfo = null;
					return;
				}
				_curPartsInfo = ChaControl.GetPartsInfo(_slotIndex);
				if (_curPartsInfo == null)
				{
					DebugMsg(LogLevel.Warning, $"[AccSlotChangedHandler][{CharaFullName}] Cannot retrive info for Slot{_slotIndex + 1:00}");
					CurSlotTriggerInfo = null;
					_accWinCtrlEnable.Visible.OnNext(false);
					return;
				}

				_accWinCtrlEnable.Visible.OnNext(_curPartsInfo.type > 120);

				CurSlotTriggerInfo = new AccTriggerInfo(_slotIndex);
				if (CharaTriggerInfo[_currentCoordinateIndex].Parts.ContainsKey(_slotIndex))
					CopySlotTriggerInfo(CharaTriggerInfo[_currentCoordinateIndex].Parts[_slotIndex], CurSlotTriggerInfo);

				if (!SkipSlotChangePartTypeCheck)
				{
					if ((CurSlotTriggerInfo.Kind > -1) && (_curPartsInfo.type == 120))
					{
						CharaTriggerInfo[_currentCoordinateIndex].Parts.Remove(_slotIndex);
						CurSlotTriggerInfo = new AccTriggerInfo(_slotIndex);
#if DEBUG
						_logger.Log(LogLevel.Error | LogLevel.Message, $"AccTriggerInfo for Coordinate {_currentCoordinateIndex} Slot{CurSlotTriggerInfo.Slot + 1:00} has been reset");
#else
						_logger.LogMessage($"AccTriggerInfo for Coordinate {_currentCoordinateIndex} Slot{CurSlotTriggerInfo.Slot + 1:00} has been reset");
#endif
					}
				}
				SkipSlotChangePartTypeCheck = false;

				DebugMsg(LogLevel.Info, $"[AccSlotChangedHandler][{CharaFullName}][Slot: {CurSlotTriggerInfo.Slot}][Kind: {CurSlotTriggerInfo.Kind}][State: {CurSlotTriggerInfo.State[0]}|{CurSlotTriggerInfo.State[1]}|{CurSlotTriggerInfo.State[2]}|{CurSlotTriggerInfo.State[3]}]");

				if (_cfgCharaMakerPreview.Value)
					_instance.StartCoroutine(SyncAllAccToggleCoroutine());

				_instance.StartCoroutine(SkipAutoSaveCoroutine());

				IEnumerator SkipAutoSaveCoroutine()
				{
					yield return JetPack.Toolbox.WaitForEndOfFrame;
					yield return JetPack.Toolbox.WaitForEndOfFrame;
#if DEBUG
					_logger.LogInfo("SkipAutoSave off");
#endif
					SkipAutoSave = false;
				}
			}

			internal void AccessoriesCopiedHandler(int _srcCoordinateIndex, int _dstCoordinateIndex, List<int> _copiedSlotIndexes)
			{
				DebugMsg(LogLevel.Info, $"[AccessoriesCopiedHandler][{CharaFullName}][Soruce: {_srcCoordinateIndex}][Destination: {_dstCoordinateIndex}][CopiedSlotIndexes: {string.Join(",", _copiedSlotIndexes.Select(x => x.ToString()).ToArray())}]");

				NullCheckOutfitTriggerInfo(_dstCoordinateIndex);
				foreach (int _slotIndex in _copiedSlotIndexes)
					CloneSlotTriggerInfo(_slotIndex, _slotIndex, _srcCoordinateIndex, _dstCoordinateIndex);

				if (_dstCoordinateIndex == _currentCoordinateIndex)
					SyncOutfitVirtualGroupInfo(_dstCoordinateIndex);
			}

			internal void AccessoryTransferredHandler(int _srcSlotIndex, int _dstSlotIndex) => AccessoryTransferredHandler(_srcSlotIndex, _dstSlotIndex, _currentCoordinateIndex);
			internal void AccessoryTransferredHandler(int _srcSlotIndex, int _dstSlotIndex, int _coordinateIndex)
			{
				DebugMsg(LogLevel.Info, $"[AccessoryTransferredHandler][{CharaFullName}] Fired!!");

				NullCheckOutfitTriggerInfo(_coordinateIndex);

				CloneSlotTriggerInfo(_srcSlotIndex, _dstSlotIndex, _coordinateIndex);

				if (_coordinateIndex == _currentCoordinateIndex)
					SyncOutfitVirtualGroupInfo(_coordinateIndex);
			}

			internal void SetKindCurSlotTriggerInfo(int _kind)
			{
				if (!JetPack.CharaMaker.Loaded) return;
				if (CharaMaker._currentSlotIndex < 0) return;
				if (CurSlotTriggerInfo == null) return;

				int _oldKind = CurSlotTriggerInfo.Kind;
				string _oldGroup = CurSlotTriggerInfo.Group;

				string _group = "";

				if (_kind == -1)
				{
					CurSlotTriggerInfo.Kind = _kind;
					CurSlotTriggerInfo.Group = _group;
					CharaTriggerInfo[_currentCoordinateIndex].Parts.Remove(CurSlotTriggerInfo.Slot);
				}
				else
				{
					if (_kind == 9)
						_group = _curPartsInfo.parentKey;
					else if (_kind > 9)
						_group = $"custom_{_kind - 9}";

					CurSlotTriggerInfo.Kind = _kind;
					CurSlotTriggerInfo.Group = _group;

					CharaTriggerInfo[_currentCoordinateIndex].Parts[CurSlotTriggerInfo.Slot] = new AccTriggerInfo(CurSlotTriggerInfo.Slot);
					CopySlotTriggerInfo(CurSlotTriggerInfo, CharaTriggerInfo[_currentCoordinateIndex].Parts[CurSlotTriggerInfo.Slot]);

					if (_kind >= 9)
						CreateGroup(_currentCoordinateIndex, _kind, CurSlotTriggerInfo.Group);
				}

				if (_oldKind == 9 && CharaVirtualGroupInfo[_currentCoordinateIndex].ContainsKey(_oldGroup))
				{
					if (!CharaTriggerInfo[_currentCoordinateIndex].Parts.Values.Any(x => x.Group == _oldGroup))
						RemoveGroup(_currentCoordinateIndex, _oldGroup);
				}
			}

			internal void OnCurSlotTriggerInfoChange()
			{
				if (!JetPack.CharaMaker.Loaded) return;
				ToggleByAccTriggerInfo(CurSlotTriggerInfo);
			}

			internal void AccessoryTypeChanged(JetPack.CharaMaker.AccessoryTypeChangedEventArgs _args)
			{
				if (SkipSlotChangePartTypeCheck)
					return;

				if (CharaMaker._currentSlotIndex != _args.SlotIndex)
					return;

				_curPartsInfo = _args.PartsInfo;
				_accWinCtrlEnable.Visible.OnNext(_curPartsInfo.type > 120);

				if (_curPartsInfo.type == 120)
				{
					if (!CharaTriggerInfo.ContainsKey(_currentCoordinateIndex) || !CharaTriggerInfo[_currentCoordinateIndex].Parts.ContainsKey(_args.SlotIndex))
						return;
					SetKindCurSlotTriggerInfo(-1);
				}
			}

			internal void AccessoryParentChanged(int _slotIndex)
			{
				if (SkipSlotChangePartTypeCheck)
					return;

				if (CharaMaker._currentSlotIndex != _slotIndex || !CharaTriggerInfo.ContainsKey(_currentCoordinateIndex) || !CharaTriggerInfo[_currentCoordinateIndex].Parts.ContainsKey(_slotIndex))
					return;

				if (CurSlotTriggerInfo?.Kind == 9)
					SetKindCurSlotTriggerInfo(9);
			}
		}
	}
}
