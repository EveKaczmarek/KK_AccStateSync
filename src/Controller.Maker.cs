using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using TMPro;
using KKAPI.Chara;
using KKAPI.Maker;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		public partial class AccStateSyncController : CharaCustomFunctionController
		{
			internal void AccSlotChangedHandler(int SlotIndex, bool Skip = false)
			{
				if (!MakerAPI.InsideAndLoaded) return;
				SkipAutoSave = true;

				Logger.Log(DebugLogLevel, $"[AccSlotChangedHandler][{ChaControl.chaFile.parameter?.fullname}] Fired!!");

				Logger.Log(DebugLogLevel, $"[AccSlotChangedHandler][{ChaControl.chaFile.parameter?.fullname}][SlotIndex]: {SlotIndex}");

				ChaFileAccessory.PartsInfo PartInfo = AccessoriesApi.GetPartsInfo(SlotIndex);
				if (PartInfo == null)
				{
					Logger.LogError($"[AccSlotChangedHandler][{ChaControl.chaFile.parameter?.fullname}] Cannot retrive info for Slot{CurSlotTriggerInfo.Slot + 1:00}");
					return;
				}

				if (!CurOutfitTriggerInfo.Parts.ContainsKey(SlotIndex))
					CurSlotTriggerInfo = new AccTriggerInfo(SlotIndex);
				else
					CopySlotTriggerInfo(CurOutfitTriggerInfo.Parts[SlotIndex], CurSlotTriggerInfo);

				if (!Skip)
				{
					if ((CurSlotTriggerInfo.Kind > -1) && (PartInfo.type == 120))
					{
						CharaTriggerInfo[CurrentCoordinateIndex].Parts.Remove(SlotIndex);
						CurSlotTriggerInfo = new AccTriggerInfo(SlotIndex);
						Logger.LogMessage($"AccTriggerInfo for Coordinate {CurrentCoordinateIndex} Slot{CurSlotTriggerInfo.Slot + 1:00} has been reset");
					}
				}

				SyncOutfitVirtualGroupInfo(CurrentCoordinateIndex);
				CurOutfitVirtualGroupInfo = CharaVirtualGroupInfo[CurrentCoordinateIndex];

				CharaMaker.ClearVirtualGroupToggle();
				UI.AnchorOffsetMinY = (int) CharaMaker.tglASSobj["tglASS0"].GetComponent<RectTransform>().offsetMin.y - 80;
				CharaMaker.CreateDropdownList(ChaControl);
				UI.DropdownASSList item = CharaMaker.ddASSList.Values.Where(x => x.Kind == CurSlotTriggerInfo.Kind).FirstOrDefault();
				CharaMaker.CreateDropdownItems(item.Index);
				CharaMaker.grpParent.Find("ddASSList").GetComponentInChildren<TMP_Dropdown>().RefreshShownValue();
				bool clickable = CurSlotTriggerInfo.Kind != -1;
				for (int x = 0; x < 4; x++)
				{
					CharaMaker.tglASSobj[$"tglASS{x}"].GetComponentInChildren<Toggle>().isOn = CurSlotTriggerInfo.State[x];
					CharaMaker.tglASSobj[$"tglASS{x}"].GetComponentInChildren<Toggle>().interactable = clickable;
					CharaMaker.tglASSobj[$"tglASS{x}"].GetComponentInChildren<TextMeshProUGUI>().alpha = item.States[x] ? 1f : 0.2f;
				}

				int i = 0;
				foreach (KeyValuePair<string, VirtualGroupInfo> group in CurOutfitVirtualGroupInfo)
				{
					if (CurOutfitTriggerInfo?.Parts?.Values?.Where(x => x.Kind == group.Value.Kind)?.Count() > 0)
					{
						CharaMaker.CreateVirtualGroupToggle(group.Key);
						i++;
					}
				}
				CharaMaker.imgWindowBack.offsetMin = new Vector2(0, UI.ContainerOffsetMinY - UI.MenuitemHeightOffsetY * i);

				Logger.Log(DebugLogLevel, $"[AccSlotChangedHandler][{ChaControl.chaFile.parameter?.fullname}][Slot: {CurSlotTriggerInfo.Slot}][Kind: {CurSlotTriggerInfo.Kind}][State: {CurSlotTriggerInfo.State[0]}|{CurSlotTriggerInfo.State[1]}|{CurSlotTriggerInfo.State[2]}|{CurSlotTriggerInfo.State[3]}]");

				Instance.StartCoroutine(WaitForEndOfFrameSyncAllAccToggle());
			}

			internal void AccessoriesCopiedHandler(int CopySource, int CopyDestination, List<int> CopiedSlotIndexes)
			{
				if (!MakerAPI.InsideAndLoaded) return;

				Logger.Log(DebugLogLevel, $"[AccessoriesCopiedHandler][{ChaControl.chaFile.parameter?.fullname}][Soruce: {CopySource}][Destination: {CopyDestination}][CopiedSlotIndexes: {string.Join(",", CopiedSlotIndexes.Select(Slot => Slot.ToString()).ToArray())}]");

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
							Logger.Log(DebugLogLevel, $"[AccessoriesCopiedHandler][{ChaControl.chaFile.parameter?.fullname}][Group: {group}] created");
						}
					}
				}

				Logger.Log(DebugLogLevel, $"[AccessoriesCopiedHandler][{ChaControl.chaFile.parameter?.fullname}] CharaVirtualGroupNames[{CopyDestination}].Count(): {CharaVirtualGroupInfo[CopyDestination].Count()}");

				if (CopyDestination == CurrentCoordinateIndex)
				{
					CurOutfitVirtualGroupInfo = CharaVirtualGroupInfo[CurrentCoordinateIndex];
					AccSlotChangedHandler(AccessoriesApi.SelectedMakerAccSlot, true);
				}
			}

			internal void AccessoryTransferredHandler(int SourceSlotIndex, int DestinationSlotIndex) => AccessoryTransferredHandler(SourceSlotIndex, DestinationSlotIndex, CurrentCoordinateIndex);
			internal void AccessoryTransferredHandler(int SourceSlotIndex, int DestinationSlotIndex, int CoordinateIndex)
			{
				if (!MakerAPI.InsideAndLoaded) return;

				Logger.Log(DebugLogLevel, $"[AccessoryTransferredHandler][{ChaControl.chaFile.parameter?.fullname}] Fired!!");

				NullCheckOutfitTriggerInfo(CoordinateIndex);

				if (CharaTriggerInfo[CoordinateIndex].Parts.ContainsKey(DestinationSlotIndex))
					CharaTriggerInfo[CoordinateIndex].Parts.Remove(DestinationSlotIndex);
				if (CharaTriggerInfo[CoordinateIndex].Parts.ContainsKey(SourceSlotIndex))
				{
					CharaTriggerInfo[CoordinateIndex].Parts[DestinationSlotIndex] = new AccTriggerInfo(DestinationSlotIndex);
					CopySlotTriggerInfo(CharaTriggerInfo[CoordinateIndex].Parts[SourceSlotIndex], CharaTriggerInfo[CoordinateIndex].Parts[DestinationSlotIndex]);
					CharaTriggerInfo[CoordinateIndex].Parts[DestinationSlotIndex].Slot = DestinationSlotIndex;
				}
			}

			internal void PreviewChange()
			{
				AccTriggerInfo Part = CurSlotTriggerInfo;

				if (MathfEx.RangeEqualOn(0, Part.Kind, 6))
				{
					int state = ChaControl.fileStatus.clothesState[Part.Kind];
					bool vis = Part.State[state];
					Part.Group = "";
					ChaControl.SetAccessoryState(Part.Slot, vis);
				}
				else if ((Part.Kind == 7) || (Part.Kind == 8))
				{
					int clothesKind = (ChaControl.fileStatus.shoesType == 0) ? 7 : 8;
					bool vis = false;
					Part.Group = "";
					if (clothesKind == Part.Kind)
						vis = Part.State[ChaControl.fileStatus.clothesState[Part.Kind]];
					ChaControl.SetAccessoryState(Part.Slot, vis);
				}
				else if (Part.Kind >= 9)
				{
					if (Part.Kind == 9)
					{
						ChaFileAccessory.PartsInfo PartInfo = AccessoriesApi.GetPartsInfo(Part.Slot);
						Part.Group = PartInfo.parentKey;
					}
					else
						Part.Group = "custom_" + (Part.Kind - 9).ToString();

					bool vis = true;
					if (CurOutfitVirtualGroupInfo.ContainsKey(Part.Group))
						vis = CurOutfitVirtualGroupInfo[Part.Group].State ? Part.State[0] : Part.State[3];
					ChaControl.SetAccessoryState(Part.Slot, vis);
				}
				AutoSaveTrigger(Part.Slot);
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

				TMP_Dropdown dropdown = CharaMaker.grpParent.Find("ddASSList").GetComponentInChildren<TMP_Dropdown>();
				UI.DropdownASSList item = CharaMaker.ddASSList.Values.Where(x => x.Kind == CurOutfitVirtualGroupInfo[group].Kind).FirstOrDefault();
				dropdown.options[item.Index].text = label;
				dropdown.RefreshShownValue();
				if (CharaMaker.tglASSgroup.ContainsKey("tglASS_" + group))
				{
					GameObject toggle = CharaMaker.tglASSgroup["tglASS_" + group];
					if (toggle != null)
						toggle.GetComponentInChildren<TextMeshProUGUI>().text = label;
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
				if (CurOutfitTriggerInfo?.Parts?.Values?.Where(x => x.Kind == group.Kind)?.Count() > 0)
				{
					Logger.LogMessage($"Cannot remove [{group.Group}][{group.Label}] because it's being assigned by slots");
					return;
				}
				Logger.LogMessage($"[{group.Group}][{group.Label}] removed");
				CharaVirtualGroupInfo[CurrentCoordinateIndex].Remove(group.Group);
			}

			internal void CvsAccessoryUpdateSelectAccessoryTypePostfix(int SlotIndex)
			{
				if (!(bool) CharaTriggerInfo[CurrentCoordinateIndex]?.Parts?.ContainsKey(SlotIndex))
					return;

				AccTriggerInfo Part = CharaTriggerInfo[CurrentCoordinateIndex].Parts[SlotIndex];

				if (Part.Kind > -1)
				{
					CharaTriggerInfo[CurrentCoordinateIndex].Parts.Remove(SlotIndex);
					Logger.LogMessage($"AccTriggerInfo for Coordinate {CurrentCoordinateIndex} Slot{SlotIndex + 1:00} has been reset");
					AccSlotChangedHandler(SlotIndex);
				}
			}

			internal void CvsAccessoryUpdateSelectAccessoryParentPostfix(int SlotIndex)
			{
				if (!(bool) CharaTriggerInfo[CurrentCoordinateIndex]?.Parts?.ContainsKey(SlotIndex))
					return;

				Logger.Log(DebugLogLevel, $"[CvsAccessoryUpdateSelectAccessoryParentPostfix][{ChaControl.chaFile.parameter?.fullname}] Fired!!");
				AccTriggerInfo Part = CharaTriggerInfo[CurrentCoordinateIndex].Parts[SlotIndex];

				if (Part.Kind == 9)
				{
					ChaFileAccessory.PartsInfo PartInfo = AccessoriesApi.GetPartsInfo(Part.Slot);
					Part.Group = PartInfo.parentKey;
					Logger.LogMessage($"Slot{SlotIndex + 1:00} has been assigned to {Part.Group}");

					AccSlotChangedHandler(SlotIndex);
				}
			}

			internal void AutoSaveTrigger(int SlotIndex)
			{
				if (!MakerAPI.InsideAndLoaded) return;
				if (!AutoSaveSetting.Value) return;
				if (SkipAutoSave) return;

				ChaFileAccessory.PartsInfo PartInfo = AccessoriesApi.GetPartsInfo(SlotIndex);
				if ((PartInfo == null) || (PartInfo.type == 120))
				{
					if ((bool) CharaTriggerInfo[CurrentCoordinateIndex]?.Parts?.ContainsKey(SlotIndex))
					{
						CharaTriggerInfo[CurrentCoordinateIndex].Parts.Remove(SlotIndex);
						Logger.LogMessage($"AccTriggerInfo for Coordinate {CurrentCoordinateIndex} Slot{SlotIndex + 1:00} has been reset");
					}
					return;
				}

				Logger.LogMessage($"AutoSaveTrigger for Coordinate {CurrentCoordinateIndex} Slot{SlotIndex + 1:00}");
				CharaMaker.grpParent.Find("btnASSsave").GetComponentInChildren<Button>().onClick.Invoke();
			}

			internal void VerifyOnePiece(int Category, int Coordinate)
			{
				if (!MakerAPI.InsideAndLoaded) return;

				if (Category == 105)
				{
					Logger.Log(DebugLogLevel, $"[VerifyOnePiece][{ChaControl.chaFile.parameter?.fullname}][Category: {Category}][Coordinate: {Coordinate}]");
					CharaTriggerInfo[CurrentCoordinateIndex].OnePiece["top"] = false;
					if (Coordinate == 2)
					{
						if (ChaControl.nowCoordinate.clothes.parts[1].id == 0)
							CharaTriggerInfo[CurrentCoordinateIndex].OnePiece["top"] = true;
					}
				}
				else if (Category == 107)
				{
					Logger.Log(DebugLogLevel, $"[VerifyOnePiece][{ChaControl.chaFile.parameter?.fullname}][Category: {Category}][Coordinate: {Coordinate}]");
					CharaTriggerInfo[CurrentCoordinateIndex].OnePiece["bra"] = false;
					if (Coordinate == 2)
					{
						if (ChaControl.nowCoordinate.clothes.parts[3].id == 0)
							CharaTriggerInfo[CurrentCoordinateIndex].OnePiece["bra"] = true;
					}
				}
			}
		}
	}
}
