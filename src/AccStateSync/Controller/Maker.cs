using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
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
				yield return new WaitForEndOfFrame();
				yield return new WaitForEndOfFrame();

				AccSlotChangedHandler(_slotIndex);
			}

			internal void AccSlotChangedHandler(int _slotIndex)
			{
				if (!JetPack.CharaMaker.Loaded) return;
				SkipAutoSave = true;

				DebugMsg(LogLevel.Info, $"[AccSlotChangedHandler][{CharaFullName}] Fired!!");

				DebugMsg(LogLevel.Info, $"[AccSlotChangedHandler][{CharaFullName}][SlotIndex]: {_slotIndex}");
#if DEBUG
				if (_slotIndex < 0)
				{
					_logger.LogError($"[AccSlotChangedHandler][{CharaFullName}] calling when SlotIndex = -1");
					CurSlotTriggerInfo = null;
					return;
				}
#endif
				_curPartsInfo = ChaControl.GetPartsInfo(_slotIndex);
				if (_curPartsInfo == null)
				{
					_logger.LogError($"[AccSlotChangedHandler][{CharaFullName}] Cannot retrive info for Slot{_slotIndex + 1:00}");
					CurSlotTriggerInfo = null;
					return;
				}

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

				SyncOutfitVirtualGroupInfo(_currentCoordinateIndex);

				DebugMsg(LogLevel.Info, $"[AccSlotChangedHandler][{CharaFullName}][Slot: {CurSlotTriggerInfo.Slot}][Kind: {CurSlotTriggerInfo.Kind}][State: {CurSlotTriggerInfo.State[0]}|{CurSlotTriggerInfo.State[1]}|{CurSlotTriggerInfo.State[2]}|{CurSlotTriggerInfo.State[3]}]");

				if (_cfgCharaMakerPreview.Value)
					_instance.StartCoroutine(SyncAllAccToggleCoroutine());

				_instance.StartCoroutine(SkipAutoSaveCoroutine());

				IEnumerator SkipAutoSaveCoroutine()
				{
					yield return new WaitForEndOfFrame();
					yield return new WaitForEndOfFrame();
#if DEBUG
					_logger.LogError("SkipAutoSave off");
#endif
					SkipAutoSave = false;
				}
			}

			internal void AccessoriesCopiedHandler(int _srcCoordinateIndex, int _dstCoordinateIndex, List<int> CopiedSlotIndexes)
			{
				DebugMsg(LogLevel.Info, $"[AccessoriesCopiedHandler][{CharaFullName}][Soruce: {_srcCoordinateIndex}][Destination: {_dstCoordinateIndex}][CopiedSlotIndexes: {string.Join(",", CopiedSlotIndexes.Select(x => x.ToString()).ToArray())}]");

				NullCheckOutfitTriggerInfo(_dstCoordinateIndex);

				int j = -1;
				if (CharaTriggerInfo[_dstCoordinateIndex]?.Parts?.Count() > 0)
					j = CharaTriggerInfo[_dstCoordinateIndex].Parts.Values.Max(x => x.Kind);

				int i = 9;
				foreach (int _slot in CopiedSlotIndexes)
				{
					if (CharaTriggerInfo[_dstCoordinateIndex].Parts.ContainsKey(_slot))
						CharaTriggerInfo[_dstCoordinateIndex].Parts.Remove(_slot);

					if (CharaTriggerInfo[_srcCoordinateIndex].Parts.ContainsKey(_slot))
					{
						CharaTriggerInfo[_dstCoordinateIndex].Parts[_slot] = new AccTriggerInfo(_slot);
						CopySlotTriggerInfo(CharaTriggerInfo[_srcCoordinateIndex].Parts[_slot], CharaTriggerInfo[_dstCoordinateIndex].Parts[_slot]);
						i = CharaTriggerInfo[_srcCoordinateIndex].Parts[_slot].Kind > i ? CharaTriggerInfo[_srcCoordinateIndex].Parts[_slot].Kind : i;
					}
				}

				if (i > j)
				{
					for (int _kind = 10; _kind < (i + 1); _kind++)
					{
						string _group = $"custom_{_kind - 9}";
						if (!CharaVirtualGroupInfo[_dstCoordinateIndex].ContainsKey(_group))
						{
							CharaVirtualGroupInfo[_dstCoordinateIndex][_group] = CharaVirtualGroupInfo[_srcCoordinateIndex][_group];
							DebugMsg(LogLevel.Info, $"[AccessoriesCopiedHandler][{CharaFullName}][Group: {_group}] created");
						}
					}
				}

				DebugMsg(LogLevel.Info, $"[AccessoriesCopiedHandler][{CharaFullName}] CharaVirtualGroupNames[{_dstCoordinateIndex}].Count(): {CharaVirtualGroupInfo[_dstCoordinateIndex].Count()}");

				if (_dstCoordinateIndex == _currentCoordinateIndex)
				{
					SyncOutfitVirtualGroupInfo(_currentCoordinateIndex);
				}
			}

			internal void AccessoryTransferredHandler(int _srcSlotIndex, int _dstSlotIndex) => AccessoryTransferredHandler(_srcSlotIndex, _dstSlotIndex, _currentCoordinateIndex);
			internal void AccessoryTransferredHandler(int _srcSlotIndex, int _dstSlotIndex, int _coordinateIndex)
			{
				DebugMsg(LogLevel.Info, $"[AccessoryTransferredHandler][{CharaFullName}] Fired!!");

				NullCheckOutfitTriggerInfo(_coordinateIndex);

				if (CharaTriggerInfo[_coordinateIndex].Parts.ContainsKey(_dstSlotIndex))
					CharaTriggerInfo[_coordinateIndex].Parts.Remove(_dstSlotIndex);
				if (CharaTriggerInfo[_coordinateIndex].Parts.ContainsKey(_srcSlotIndex))
				{
					CharaTriggerInfo[_coordinateIndex].Parts[_dstSlotIndex] = new AccTriggerInfo(_dstSlotIndex);
					CopySlotTriggerInfo(CharaTriggerInfo[_coordinateIndex].Parts[_srcSlotIndex], CharaTriggerInfo[_coordinateIndex].Parts[_dstSlotIndex]);
					CharaTriggerInfo[_coordinateIndex].Parts[_dstSlotIndex].Slot = _dstSlotIndex;
				}
				SyncOutfitVirtualGroupInfo(_currentCoordinateIndex);
			}

			internal void RenameGroup(string _group, string _label)
			{
				if (!CharaVirtualGroupInfo[_currentCoordinateIndex].ContainsKey(_group))
				{
					_logger.LogMessage($"Invalid group {_group}");
					return;
				}
				CharaVirtualGroupInfo[_currentCoordinateIndex][_group].Label = _label;
				_logger.LogMessage($"[{_group}] renamed into {_label}");
			}

			internal void PushGroup()
			{
				int n = CharaVirtualGroupInfo[_currentCoordinateIndex]?.Values?.Where(x => x.Kind > 9)?.Count() > 0 ? CharaVirtualGroupInfo[_currentCoordinateIndex].Values.Max(x => x.Kind) - 9 : 0;
				n++;
				string _group = $"custom_{n}";

				CharaVirtualGroupInfo[_currentCoordinateIndex][$"custom_{n}"] = new VirtualGroupInfo(_group, n + 9);
				_logger.LogMessage($"[custom_{n}][Custom {n}] added");
			}

			internal void PopGroup()
			{
				if (CharaVirtualGroupInfo[_currentCoordinateIndex]?.Values?.Where(x => x.Kind > 9)?.Count() == 0)
				{
					_logger.LogMessage($"Cannot go below 0 custom group");
					return;
				}

				VirtualGroupInfo _group = CharaVirtualGroupInfo[_currentCoordinateIndex]?.Values?.OrderByDescending(x => x.Kind)?.FirstOrDefault();
				if (GetPartsOfKind(_group.Kind).Count() > 0)
				{
					_logger.LogMessage($"Cannot remove [{_group.Group}][{_group.Label}] because it's being assigned by slots");
					return;
				}
				_logger.LogMessage($"[{_group.Group}][{_group.Label}] removed");
				CharaVirtualGroupInfo[_currentCoordinateIndex].Remove(_group.Group);
			}

			internal void SetSecondaryGroup(string _key, bool _secondary = true)
			{
				if (!(bool) CharaVirtualGroupInfo[_currentCoordinateIndex]?.ContainsKey(_key))
				{
					_logger.LogMessage($"Invalid group {_key}");
					return;
				}

				VirtualGroupInfo _group = CharaVirtualGroupInfo[_currentCoordinateIndex][_key];
				_group.Secondary = _secondary;
				_logger.LogMessage($"[{_group.Group}][{_group.Label}] set as {(_secondary ? "Secondary" : "Primary")}");
			}

			internal void CvsAccessory_UpdateSelectAccessoryType_Postfix(int _slotIndex)
			{
				if (SkipSlotChangePartTypeCheck)
				{
#if DEBUG
					_logger.LogError($"[CvsAccessory_UpdateSelectAccessoryType_Postfix][AutoSave: {SkipAutoSave}][PartTypeCheck: {SkipSlotChangePartTypeCheck}]");
#endif
					return;
				}

				AccSlotChangedHandler(_slotIndex);
			}

			internal void CvsAccessory_UpdateSelectAccessoryParent_Postfix(int _slotIndex)
			{
				if (SkipSlotChangePartTypeCheck)
				{
#if DEBUG
					_logger.LogError($"[CvsAccessory_UpdateSelectAccessoryParent_Postfix][AutoSave: {SkipAutoSave}][PartTypeCheck: {SkipSlotChangePartTypeCheck}]");
#endif
					return;
				}

				if (!CharaTriggerInfo.ContainsKey(_currentCoordinateIndex) || !CharaTriggerInfo[_currentCoordinateIndex].Parts.ContainsKey(_slotIndex))
					return;

				if (CurSlotTriggerInfo?.Kind == 9)
					SetCurSlotTriggerInfo(9);
			}

			internal void CreateGroupIfNotExist()
			{
				if (CharaVirtualGroupInfo[_currentCoordinateIndex].ContainsKey(CurSlotTriggerInfo.Group)) return;

				CharaVirtualGroupInfo[_currentCoordinateIndex][CurSlotTriggerInfo.Group] = new VirtualGroupInfo(CurSlotTriggerInfo.Group, CurSlotTriggerInfo.Kind);

				VirtualGroupInfo _group = CharaVirtualGroupInfo[_currentCoordinateIndex][CurSlotTriggerInfo.Group];
				_logger.LogMessage($"Group: {_group.Group} ({_group.Label}) created");
			}

			internal void PreviewChange()
			{
				if (MathfEx.RangeEqualOn(0, CurSlotTriggerInfo.Kind, 6))
				{
					int _state = ChaControl.fileStatus.clothesState[CurSlotTriggerInfo.Kind];
					bool _show = CurSlotTriggerInfo.State[_state];
					ShowAccessory(CurSlotTriggerInfo.Slot, _show);
				}
				else if ((CurSlotTriggerInfo.Kind == 7) || (CurSlotTriggerInfo.Kind == 8))
				{
					int _kind = (ChaControl.fileStatus.shoesType == 0) ? 7 : 8;
					bool _show = false;
					if (_kind == CurSlotTriggerInfo.Kind)
						_show = CurSlotTriggerInfo.State[ChaControl.fileStatus.clothesState[_kind]];
					ShowAccessory(CurSlotTriggerInfo.Slot, _show);
				}
				else if (CurSlotTriggerInfo.Kind >= 9)
				{
					CreateGroupIfNotExist();
					bool _show = true;
					if (CharaVirtualGroupInfo[_currentCoordinateIndex].ContainsKey(CurSlotTriggerInfo.Group))
						_show = CharaVirtualGroupInfo[_currentCoordinateIndex][CurSlotTriggerInfo.Group].State ? CurSlotTriggerInfo.State[0] : CurSlotTriggerInfo.State[3];
					else
						_logger.LogError($"[PreviewChange] Group {CurSlotTriggerInfo.Group} not found in CurOutfitVirtualGroupInfo");
					ShowAccessory(CurSlotTriggerInfo.Slot, _show);
				}
			}

			internal void SetCurSlotTriggerInfo(int _kind)
			{
				if (!JetPack.CharaMaker.Loaded) return;
				if (CharaMaker._currentSlotIndex < 0) return;
				if (CurSlotTriggerInfo == null || _curPartsInfo == null) return;

				string _oldGroup = CurSlotTriggerInfo.Group;

				CurSlotTriggerInfo.Kind = _kind;

				if (MathfEx.RangeEqualOn(-1, _kind, 8))
					CurSlotTriggerInfo.Group = "";
				else if (_kind == 9)
					CurSlotTriggerInfo.Group = _curPartsInfo.parentKey;
				else
					CurSlotTriggerInfo.Group = $"custom_{_kind - 9}";

				if (!CharaTriggerInfo[_currentCoordinateIndex].Parts.ContainsKey(CurSlotTriggerInfo.Slot))
					CharaTriggerInfo[_currentCoordinateIndex].Parts[CurSlotTriggerInfo.Slot] = new AccTriggerInfo(CurSlotTriggerInfo.Slot);
				CopySlotTriggerInfo(CurSlotTriggerInfo, CharaTriggerInfo[_currentCoordinateIndex].Parts[CurSlotTriggerInfo.Slot]);

				if (CharaVirtualGroupInfo[_currentCoordinateIndex].ContainsKey(_oldGroup))
				{
					if (CharaTriggerInfo[_currentCoordinateIndex].Parts.Values.FirstOrDefault(x => x.Group == _oldGroup) == null)
						CharaVirtualGroupInfo[_currentCoordinateIndex].Remove(_oldGroup);
				}
			}
		}
	}
}
