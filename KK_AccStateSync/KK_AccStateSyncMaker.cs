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

				Logger.Log(DebugLogLevel, $"[AccSlotChangedHandler][{ChaControl.chaFile.parameter?.fullname}] Fired!!");

				Logger.Log(DebugLogLevel, $"[AccSlotChangedHandler][{ChaControl.chaFile.parameter?.fullname}][SlotIndex]: {SlotIndex}");

				List<ChaFileAccessory.PartsInfo> PartsInfo = CharaAccInfo;
				ChaFileAccessory.PartsInfo PartInfo = PartsInfo.ElementAtOrDefault(SlotIndex);
				if ((PartInfo == null))
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

				foreach (KeyValuePair<string, GameObject> group in tglASSgroup)
				{
					if (group.Value.gameObject != null)
						Destroy(group.Value);
				}
				tglASSgroup.Clear();

				AnchorOffsetMinY = (int) tglASSobj["tglASS0"].GetComponent<RectTransform>().offsetMin.y - 80;

				int ddASSListVal = CurSlotTriggerInfo.Kind <= 9 ? ddASSListVals.IndexOf(CurSlotTriggerInfo.Kind) : CurSlotTriggerInfo.Kind;
				int refIndex = ddASSListVal <= 9 ? ddASSListVal : 9;

				List<string> extra = new List<string>();
				if (CurOutfitVirtualGroupNames?.Count() > 0)
					extra = CurOutfitVirtualGroupNames.Select(x => x.Value).ToList();
				CreateMakerDropdownItems(extra, ddASSListVal);

//				grpParent.Find("ddASSList").GetComponentInChildren<TMP_Dropdown>().value = ddASSListVal;
				grpParent.Find("ddASSList").GetComponentInChildren<TMP_Dropdown>().RefreshShownValue();

				for (int x = 0; x < 4; x++)
				{
					tglASSobj[$"tglASS{x}"].GetComponentInChildren<Toggle>().isOn = CurSlotTriggerInfo.State[x];
					tglASSobj[$"tglASS{x}"].GetComponentInChildren<TextMeshProUGUI>().alpha = clothesStates[refIndex][x] ? 1f : 0.2f;
				}

				FillVirtualGroupStates();

				List<string> names = new List<string>();
				if (VirtualGroupStates?.Count() > 0)
					names = VirtualGroupStates.OrderBy(x => x.Key).Select(x => x.Key).ToList<string>();
				foreach (string group in names)
					CreateMakerVirtualGroupToggle(group);

				imgWindowBack.offsetMin = new Vector2(0, ContainerOffsetMinY - MenuitemHeightOffsetY * names.Count());

				Logger.Log(DebugLogLevel, $"[AccSlotChangedHandler][{ChaControl.chaFile.parameter?.fullname}][Slot: {CurSlotTriggerInfo.Slot}][Kind: {CurSlotTriggerInfo.Kind}][State: {CurSlotTriggerInfo.State[0]}|{CurSlotTriggerInfo.State[1]}|{CurSlotTriggerInfo.State[2]}|{CurSlotTriggerInfo.State[3]}]");

				ChaControl.StartCoroutine(CoroutineSyncAllAccToggle(CoroutineSlotChangeDelay.Value));
			}

			internal void AccessoriesCopiedHandler(int CopySource, int CopyDestination, List<int> CopiedSlotIndexes)
			{
				if (!MakerAPI.InsideAndLoaded) return;

				Logger.Log(DebugLogLevel, $"[AccessoriesCopiedHandler][{ChaControl.chaFile.parameter?.fullname}][Soruce: {CopySource}][Destination: {CopyDestination}][CopiedSlotIndexes: {string.Join(",", CopiedSlotIndexes.Select(Slot => Slot.ToString()).ToArray())}]");

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

				int j = -1;
				if (CharaTriggerInfo[CopyDestination].Parts.Count() > 0)
					j = CharaTriggerInfo[CopyDestination].Parts.Values.Max(x => x.Kind);
				if (i > j)
				{
					for (int Kind = 10; Kind < (i + 1); Kind++)
					{
						string group = $"custom_{Kind - 9}";
						if (!CharaVirtualGroupNames[CopyDestination].ContainsKey(group))
						{
							CharaVirtualGroupNames[CopyDestination][group] = $"Custom {Kind - 9}";
							Logger.Log(DebugLogLevel, $"[AccessoriesCopiedHandler][{ChaControl.chaFile.parameter?.fullname}][Group: {group}] created");
						}
					}
				}
				Logger.Log(DebugLogLevel, $"[AccessoriesCopiedHandler][{ChaControl.chaFile.parameter?.fullname}] CharaVirtualGroupNames[{CopyDestination}].Count(): {CharaVirtualGroupNames[CopyDestination].Count()}");

				if (CopyDestination == CurrentCoordinateIndex)
					AccSlotChangedHandler(AccessoriesApi.SelectedMakerAccSlot, true);
			}

			internal void AccessoryTransferredHandler(int SourceSlotIndex, int DestinationSlotIndex) => AccessoryTransferredHandler(SourceSlotIndex, DestinationSlotIndex, CurrentCoordinateIndex);
			internal void AccessoryTransferredHandler(int SourceSlotIndex, int DestinationSlotIndex, int CoordinateIndex)
			{
				if (!MakerAPI.InsideAndLoaded) return;

				Logger.Log(DebugLogLevel, $"[AccessoryTransferredHandler][{ChaControl.chaFile.parameter?.fullname}] Fired!!");
				if (CharaTriggerInfo[CoordinateIndex].Parts.ContainsKey(SourceSlotIndex))
				{
					CharaTriggerInfo[CoordinateIndex].Parts[DestinationSlotIndex] = new AccTriggerInfo(DestinationSlotIndex);
					CopySlotTriggerInfo(CharaTriggerInfo[CoordinateIndex].Parts[SourceSlotIndex], CharaTriggerInfo[CoordinateIndex].Parts[DestinationSlotIndex]);
					CharaTriggerInfo[CoordinateIndex].Parts[DestinationSlotIndex].Slot = DestinationSlotIndex;
				}
			}

			internal void RenameGroup(string group, string label)
			{
				if (!CurOutfitVirtualGroupNames.ContainsKey(group))
				{
					Logger.LogMessage($"Invalid group {group}");
					return;
				}
				CurOutfitVirtualGroupNames[group] = label;
				Logger.LogMessage($"[{group}] renamed into {label}");

				int kind = System.Int32.Parse(group.Replace("custom_", "")) + 9;
				TMP_Dropdown dropdown = grpParent.Find("ddASSList").GetComponentInChildren<TMP_Dropdown>();
				dropdown.options[kind].text = label;
				dropdown.RefreshShownValue();
				GameObject toggle = tglASSgroup["tglASS_" + group];
				if (toggle != null)
					toggle.GetComponentInChildren<TextMeshProUGUI>().text = label;
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
				int n = CurOutfitVirtualGroupNames.Count() + 1;
				CurOutfitVirtualGroupNames[$"custom_{n}"] = $"Custom {n}";
				Logger.LogMessage($"[custom_{n}][Custom {n}] added");
			}

			internal void PopGroup()
			{
				int n = CurOutfitVirtualGroupNames.Count();
				string group = $"custom_{n}";
				if (n <= DefaultCustomGroupCount)
				{
					Logger.LogMessage($"Cannot go below {DefaultCustomGroupCount} custom group");
					return;
				}
				if (CurOutfitTriggerInfo.Parts.Values?.Where(x => x.Group == group)?.ToList()?.Count() > 0)
				{
					Logger.LogMessage($"Cannot remove [{group}][{CurOutfitVirtualGroupNames[group]}] because it's being assigned by slots");
					return;
				}
				Logger.LogMessage($"[{group}][{CurOutfitVirtualGroupNames[group]}] removed");
				CurOutfitVirtualGroupNames.Remove(group);
			}

			internal void CvsAccessoryUpdateSelectAccessoryTypePostfix(int SlotIndex)
			{
				if (!CharaTriggerInfo[CurrentCoordinateIndex].Parts.ContainsKey(SlotIndex))
					return;

				AccTriggerInfo Part = CharaTriggerInfo[CurrentCoordinateIndex].Parts[SlotIndex];

				if (Part.Kind > -1)
				{
					CharaTriggerInfo[CurrentCoordinateIndex].Parts.Remove(SlotIndex);
					Logger.LogMessage($"AccTriggerInfo for Coordinate {CurrentCoordinateIndex} Slot{SlotIndex + 1:00} has been reset");
					AccSlotChangedHandler(SlotIndex);
				}
			}
		}
	}
}
