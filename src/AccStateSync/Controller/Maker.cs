using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;
using UniRx;
using TMPro;

using KKAPI.Chara;
using KKAPI.Maker;
using JetPack;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		public partial class AccStateSyncController : CharaCustomFunctionController
		{
			internal IEnumerator AccSlotChangedHandlerCoroutine(int SlotIndex)
			{
				yield return new WaitForEndOfFrame();
				yield return new WaitForEndOfFrame();

				AccSlotChangedHandler(SlotIndex);
			}

			internal void AccSlotChangedHandler(int SlotIndex)
			{
				if (!MakerAPI.InsideAndLoaded) return;
				SkipAutoSave = true;

				Logger.Log(DebugLogLevel, $"[AccSlotChangedHandler][{CharaFullName}] Fired!!");

				Logger.Log(DebugLogLevel, $"[AccSlotChangedHandler][{CharaFullName}][SlotIndex]: {SlotIndex}");

				ChaFileAccessory.PartsInfo PartInfo = ChaControl.GetPartsInfo(SlotIndex);
				if (PartInfo == null)
				{
					Logger.LogError($"[AccSlotChangedHandler][{CharaFullName}] Cannot retrive info for Slot{SlotIndex + 1:00}");
					return;
				}

				if (!CurOutfitTriggerInfo.Parts.ContainsKey(SlotIndex))
					CurSlotTriggerInfo = new AccTriggerInfo(SlotIndex);
				else
					CopySlotTriggerInfo(CurOutfitTriggerInfo.Parts[SlotIndex], CurSlotTriggerInfo);

				if (!SkipSlotChangePartTypeCheck)
				{
					if ((CurSlotTriggerInfo.Kind > -1) && (PartInfo.type == 120))
					{
						CharaTriggerInfo[CurrentCoordinateIndex].Parts.Remove(SlotIndex);
						CurSlotTriggerInfo = new AccTriggerInfo(SlotIndex);
#if DEBUG
						Logger.Log(BepInEx.Logging.LogLevel.Error | BepInEx.Logging.LogLevel.Message, $"AccTriggerInfo for Coordinate {CurrentCoordinateIndex} Slot{CurSlotTriggerInfo.Slot + 1:00} has been reset");
#else
						Logger.LogMessage($"AccTriggerInfo for Coordinate {CurrentCoordinateIndex} Slot{CurSlotTriggerInfo.Slot + 1:00} has been reset");
#endif
					}
				}
				SkipSlotChangePartTypeCheck = false;

				SyncOutfitVirtualGroupInfo(CurrentCoordinateIndex);
				CurOutfitVirtualGroupInfo = CharaVirtualGroupInfo[CurrentCoordinateIndex];

				CharaMaker.ClearVirtualGroupToggle();
				UI.AnchorOffsetMinY = (int) CharaMaker.tglASSobj["tglASS0"].GetComponent<RectTransform>().offsetMin.y - 80;
				CharaMaker.CreateDropdownList();
				UI.DropdownASSList item = CharaMaker.ddASSListDic.Values.Where(x => x.Kind == CurSlotTriggerInfo.Kind).FirstOrDefault();
				CharaMaker.CreateDropdownItems(item.Index);
				CharaMaker.ddASSList.GetComponentInChildren<TMP_Dropdown>().RefreshShownValue();

				UpdateStatesToggle();
#if DEBUG
				Logger.LogWarning($"[AccSlotChangedHandler][CurOutfitVirtualGroupInfo.Count: {CurOutfitVirtualGroupInfo.Count}]");
#endif
				int i = 0;
				foreach (KeyValuePair<string, VirtualGroupInfo> group in CurOutfitVirtualGroupInfo)
				{
					if (GetPartsOfKind(group.Value.Kind).Count() > 0)
					{
						CharaMaker.CreateVirtualGroupToggle(group.Key);
						i++;
					}
				}
				CharaMaker.imgWindowBack.offsetMin = new Vector2(0, UI.ContainerOffsetMinY - UI.MenuitemHeightOffsetY * i);

				Logger.Log(DebugLogLevel, $"[AccSlotChangedHandler][{CharaFullName}][Slot: {CurSlotTriggerInfo.Slot}][Kind: {CurSlotTriggerInfo.Kind}][State: {CurSlotTriggerInfo.State[0]}|{CurSlotTriggerInfo.State[1]}|{CurSlotTriggerInfo.State[2]}|{CurSlotTriggerInfo.State[3]}]");

				if (CharaMakerPreview.Value)
					Instance.StartCoroutine(SyncAllAccToggleCoroutine());

				Instance.StartCoroutine(SkipAutoSaveCoroutine());
				//SkipAutoSave = false;
			}

			internal IEnumerator SkipAutoSaveCoroutine()
			{
				yield return new WaitForEndOfFrame();
				yield return new WaitForEndOfFrame();
#if DEBUG
				Logger.LogError("SkipAutoSave off");
#endif
				SkipAutoSave = false;
			}

			internal void AccessoriesCopiedHandler(int CopySource, int CopyDestination, List<int> CopiedSlotIndexes)
			{
				if (!MakerAPI.InsideAndLoaded) return;

				Logger.Log(DebugLogLevel, $"[AccessoriesCopiedHandler][{CharaFullName}][Soruce: {CopySource}][Destination: {CopyDestination}][CopiedSlotIndexes: {string.Join(",", CopiedSlotIndexes.Select(Slot => Slot.ToString()).ToArray())}]");

				NullCheckOutfitTriggerInfo(CopyDestination);

				int j = -1;
				if (CharaTriggerInfo[CopyDestination]?.Parts?.Count() > 0)
					j = CharaTriggerInfo[CopyDestination].Parts.Values.Max(x => x.Kind);

				int i = 9;
				foreach (int Slot in CopiedSlotIndexes)
				{
					if (CharaTriggerInfo[CopyDestination].Parts.ContainsKey(Slot))
						CharaTriggerInfo[CopyDestination].Parts.Remove(Slot);

					if (CharaTriggerInfo[CopySource].Parts.ContainsKey(Slot))
					{
						CharaTriggerInfo[CopyDestination].Parts[Slot] = new AccTriggerInfo(Slot);
						CopySlotTriggerInfo(CharaTriggerInfo[CopySource].Parts[Slot], CharaTriggerInfo[CopyDestination].Parts[Slot]);
						i = CharaTriggerInfo[CopySource].Parts[Slot].Kind > i ? CharaTriggerInfo[CopySource].Parts[Slot].Kind : i;
					}
				}

				if (i > j)
				{
					for (int Kind = 10; Kind < (i + 1); Kind++)
					{
						string group = $"custom_{Kind - 9}";
						if (!CharaVirtualGroupInfo[CopyDestination].ContainsKey(group))
						{
							CharaVirtualGroupInfo[CopyDestination][group] = CharaVirtualGroupInfo[CopySource][group];
							Logger.Log(DebugLogLevel, $"[AccessoriesCopiedHandler][{CharaFullName}][Group: {group}] created");
						}
					}
				}

				Logger.Log(DebugLogLevel, $"[AccessoriesCopiedHandler][{CharaFullName}] CharaVirtualGroupNames[{CopyDestination}].Count(): {CharaVirtualGroupInfo[CopyDestination].Count()}");

				if (CopyDestination == CurrentCoordinateIndex)
				{
					SyncOutfitVirtualGroupInfo(CurrentCoordinateIndex);
					CurOutfitVirtualGroupInfo = CharaVirtualGroupInfo[CurrentCoordinateIndex];
				}
			}

			internal void AccessoryTransferredHandler(int SourceSlotIndex, int DestinationSlotIndex) => AccessoryTransferredHandler(SourceSlotIndex, DestinationSlotIndex, CurrentCoordinateIndex);
			internal void AccessoryTransferredHandler(int SourceSlotIndex, int DestinationSlotIndex, int CoordinateIndex)
			{
				if (!MakerAPI.InsideAndLoaded) return;

				Logger.Log(DebugLogLevel, $"[AccessoryTransferredHandler][{CharaFullName}] Fired!!");

				NullCheckOutfitTriggerInfo(CoordinateIndex);

				if (CharaTriggerInfo[CoordinateIndex].Parts.ContainsKey(DestinationSlotIndex))
					CharaTriggerInfo[CoordinateIndex].Parts.Remove(DestinationSlotIndex);
				if (CharaTriggerInfo[CoordinateIndex].Parts.ContainsKey(SourceSlotIndex))
				{
					CharaTriggerInfo[CoordinateIndex].Parts[DestinationSlotIndex] = new AccTriggerInfo(DestinationSlotIndex);
					CopySlotTriggerInfo(CharaTriggerInfo[CoordinateIndex].Parts[SourceSlotIndex], CharaTriggerInfo[CoordinateIndex].Parts[DestinationSlotIndex]);
					CharaTriggerInfo[CoordinateIndex].Parts[DestinationSlotIndex].Slot = DestinationSlotIndex;
				}
				SyncOutfitVirtualGroupInfo(CurrentCoordinateIndex);
			}

			internal void RenameGroup(string group, string label)
			{
				if (!CurOutfitVirtualGroupInfo.ContainsKey(group))
				{
					Logger.LogMessage($"Invalid group {group}");
					return;
				}
				CurOutfitVirtualGroupInfo[group].Label = label;
				Logger.LogMessage($"[{group}] renamed into {label}");

				TMP_Dropdown dropdown = CharaMaker.ddASSList.GetComponentInChildren<TMP_Dropdown>();
				UI.DropdownASSList item = CharaMaker.ddASSListDic.Values.Where(x => x.Kind == CurOutfitVirtualGroupInfo[group].Kind).FirstOrDefault();
				dropdown.options[item.Index].text = label;
				dropdown.RefreshShownValue();
				if (CharaMaker.tglASSgroup.ContainsKey("tglASS_" + group))
				{
					GameObject toggle = CharaMaker.tglASSgroup["tglASS_" + group];
					if (toggle != null)
					{
						if (VirtualGroupShowName.Value)
							toggle.GetComponentInChildren<TextMeshProUGUI>().text = label + " (" + group + ")";
						else
							toggle.GetComponentInChildren<TextMeshProUGUI>().text = label;
					}
				}
			}

			internal void RenameGroup(int kind, string label)
			{
				if (kind <= 9)
				{
					Logger.LogMessage($"Invalid kind {kind}");
					return;
				}
				string group = $"custom_{kind - 9}";
				RenameGroup(group, label);
			}

			internal void PushGroup()
			{
				int n = CurOutfitVirtualGroupInfo?.Values?.Where(x => x.Kind > 9)?.Count() > 0 ? CurOutfitVirtualGroupInfo.Values.Max(x => x.Kind) - 9 : 0;
				n++;
				string Group = $"custom_{n}";

				CharaVirtualGroupInfo[CurrentCoordinateIndex][$"custom_{n}"] = new VirtualGroupInfo(Group, n + 9);
				Logger.LogMessage($"[custom_{n}][Custom {n}] added");
			}

			internal void PopGroup()
			{
				if (CurOutfitVirtualGroupInfo?.Values?.Where(x => x.Kind > 9)?.Count() == 0)
				{
					Logger.LogMessage($"Cannot go below 0 custom group");
					return;
				}

				VirtualGroupInfo group = CurOutfitVirtualGroupInfo?.Values?.OrderByDescending(x => x.Kind)?.FirstOrDefault();
				if (GetPartsOfKind(group.Kind).Count() > 0)
				{
					Logger.LogMessage($"Cannot remove [{group.Group}][{group.Label}] because it's being assigned by slots");
					return;
				}
				Logger.LogMessage($"[{group.Group}][{group.Label}] removed");
				CharaVirtualGroupInfo[CurrentCoordinateIndex].Remove(group.Group);
			}

			internal void CreateGroupIfNotExist(string group)
            {
				if (CurOutfitVirtualGroupInfo.ContainsKey(CurSlotTriggerInfo.Group)) return;

				CharaVirtualGroupInfo[CurrentCoordinateIndex][CurSlotTriggerInfo.Group] = new VirtualGroupInfo(CurSlotTriggerInfo.Group, CurSlotTriggerInfo.Kind);
				CurOutfitVirtualGroupInfo = CharaVirtualGroupInfo[CurrentCoordinateIndex];

				VirtualGroupInfo info = CurOutfitVirtualGroupInfo[CurSlotTriggerInfo.Group];
				Logger.LogMessage($"Group: {info.Group} ({info.Label}) created");
			}

			internal void SetSecondaryGroup(int kind, bool secondary = true)
			{
				if (CurOutfitVirtualGroupInfo?.Values?.Where(x => x.Kind == kind)?.Count() == 0)
				{
					Logger.LogMessage($"Invalid kind {kind}");
					return;
				}

				VirtualGroupInfo group = CurOutfitVirtualGroupInfo.Values.Where(x => x.Kind == kind).FirstOrDefault();
				CharaVirtualGroupInfo[CurrentCoordinateIndex][group.Group].Secondary = secondary;
				string MsgText = secondary ? "Secondary" : "Primary";
				Logger.LogMessage($"[{group.Group}][{group.Label}] set as {MsgText}");
			}

			internal void SetSecondaryGroup(string key, bool secondary = true)
			{
				if (!(bool) CurOutfitVirtualGroupInfo?.ContainsKey(key))
				{
					Logger.LogMessage($"Invalid group {key}");
					return;
				}

				VirtualGroupInfo group = CharaVirtualGroupInfo[CurrentCoordinateIndex][key];
				group.Secondary = secondary;
				string MsgText = secondary ? "Secondary" : "Primary";
				Logger.LogMessage($"[{group.Group}][{group.Label}] set as {MsgText}");
			}

			internal void CvsAccessory_UpdateSelectAccessoryType_Postfix(int SlotIndex)
			{
				if (SkipAutoSave)
				{
#if DEBUG
					Logger.LogError($"[CvsAccessory_UpdateSelectAccessoryType_Postfix] skip autosave");
#endif
					return;
				}

				if (!(bool) CharaTriggerInfo[CurrentCoordinateIndex]?.Parts?.ContainsKey(SlotIndex))
					return;

				AccTriggerInfo Part = CharaTriggerInfo[CurrentCoordinateIndex].Parts[SlotIndex];
				ChaFileAccessory.PartsInfo PartInfo = ChaControl.GetPartsInfo(CurrentCoordinateIndex, SlotIndex);

				if ((PartInfo.type == 120) && (Part.Kind > -1))
				{
					CharaTriggerInfo[CurrentCoordinateIndex].Parts.Remove(SlotIndex);
					Logger.LogMessage($"AccTriggerInfo for Coordinate {CurrentCoordinateIndex} Slot{SlotIndex + 1:00} has been reset");
					AccSlotChangedHandler(SlotIndex);
				}
			}

			internal void CvsAccessory_UpdateSelectAccessoryParent_Postfix(int SlotIndex)
			{
				if (SkipAutoSave)
				{
#if DEBUG
					Logger.LogError($"[CvsAccessory_UpdateSelectAccessoryParent_Postfix] skip autosave");
#endif
					return;
				}

				if (!CharaTriggerInfo[CurrentCoordinateIndex].Parts.ContainsKey(SlotIndex))
					return;

				AccTriggerInfo Part = CharaTriggerInfo[CurrentCoordinateIndex].Parts[SlotIndex];
				if (Part.Kind == 9)
				{
					ChaFileAccessory.PartsInfo PartInfo = ChaControl.GetPartsInfo(CurrentCoordinateIndex, SlotIndex);
					Part.Group = PartInfo.parentKey;
					Logger.LogMessage($"Slot{SlotIndex + 1:00} has been assigned to {Part.Group}");
					AccSlotChangedHandler(SlotIndex);
				}
			}

			internal void VerifyOnePiece(int Category, int Coordinate)
			{
				if (!MakerAPI.InsideAndLoaded) return;

				if (Category == 105)
				{
					Logger.Log(DebugLogLevel, $"[VerifyOnePiece][{CharaFullName}][Category: {Category}][Coordinate: {Coordinate}]");
					CharaTriggerInfo[CurrentCoordinateIndex].OnePiece["top"] = false;
					if (Coordinate == 2)
					{
						if (ChaControl.nowCoordinate.clothes.parts[1].id == 0)
							CharaTriggerInfo[CurrentCoordinateIndex].OnePiece["top"] = true;
					}
				}
				else if (Category == 107)
				{
					Logger.Log(DebugLogLevel, $"[VerifyOnePiece][{CharaFullName}][Category: {Category}][Coordinate: {Coordinate}]");
					CharaTriggerInfo[CurrentCoordinateIndex].OnePiece["bra"] = false;
					if (Coordinate == 2)
					{
						if (ChaControl.nowCoordinate.clothes.parts[3].id == 0)
							CharaTriggerInfo[CurrentCoordinateIndex].OnePiece["bra"] = true;
					}
				}
			}

			internal void UpdateStatesToggle()
			{
				UI.DropdownASSList item = CharaMaker.ddASSListDic.Values.Where(x => x.Kind == CurSlotTriggerInfo.Kind).FirstOrDefault();
				List<bool> states = new List<bool>();
				if (MathfEx.RangeEqualOn(0, CurSlotTriggerInfo.Kind, 8))
					states = ChaControl.GetClothesStates(CurSlotTriggerInfo.Kind);
				if (states.Count < 4)
					states = item.States;

				bool clickable = CurSlotTriggerInfo.Kind != -1;
				for (int x = 0; x < 4; x++)
				{
					CharaMaker.tglASSobj[$"tglASS{x}"].GetComponentInChildren<Toggle>().isOn = CurSlotTriggerInfo.State[x];
					CharaMaker.tglASSobj[$"tglASS{x}"].GetComponentInChildren<Toggle>().interactable = clickable;
					CharaMaker.tglASSobj[$"tglASS{x}"].GetComponentInChildren<TextMeshProUGUI>().alpha = states[x] ? 1f : 0.2f;
				}
			}

			internal void PreviewChange()
			{
				if (MathfEx.RangeEqualOn(0, CurSlotTriggerInfo.Kind, 6))
				{
					int state = ChaControl.fileStatus.clothesState[CurSlotTriggerInfo.Kind];
					bool vis = CurSlotTriggerInfo.State[state];
					ShowAccessory(CurSlotTriggerInfo.Slot, vis);
				}
				else if ((CurSlotTriggerInfo.Kind == 7) || (CurSlotTriggerInfo.Kind == 8))
				{
					int clothesKind = (ChaControl.fileStatus.shoesType == 0) ? 7 : 8;
					bool vis = false;
					if (clothesKind == CurSlotTriggerInfo.Kind)
						vis = CurSlotTriggerInfo.State[ChaControl.fileStatus.clothesState[CurSlotTriggerInfo.Kind]];
					ShowAccessory(CurSlotTriggerInfo.Slot, vis);
				}
				else if (CurSlotTriggerInfo.Kind >= 9)
				{
					CreateGroupIfNotExist(CurSlotTriggerInfo.Group);
					bool vis = true;
					if (CurOutfitVirtualGroupInfo.ContainsKey(CurSlotTriggerInfo.Group))
						vis = CurOutfitVirtualGroupInfo[CurSlotTriggerInfo.Group].State ? CurSlotTriggerInfo.State[0] : CurSlotTriggerInfo.State[3];
					else
						Logger.LogError($"[PreviewChange] Group {CurSlotTriggerInfo.Group} not found in CurOutfitVirtualGroupInfo");
					ShowAccessory(CurSlotTriggerInfo.Slot, vis);
				}
			}

			internal void AutoSaveTrigger()
			{
				if (!MakerAPI.InsideAndLoaded) return;
				if (!AutoSaveSetting.Value) return;
				if (SkipAutoSave)
				{
#if DEBUG
					Logger.LogError("AutoSaveTrigger skip autosave");
#endif
					return;
				}

				Logger.LogMessage($"AutoSaveTrigger for Coordinate {CurrentCoordinateIndex} Slot{CurSlotTriggerInfo.Slot + 1:00}");
				CharaMaker.btnASSsave.GetComponentInChildren<Button>().onClick.Invoke();
			}
		}
	}
}
