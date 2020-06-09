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
				Logger.Log(DebugLogLevel, $"[AccSlotChangedHandler][{ChaControl.chaFile.parameter?.fullname}] Fired!!");

				if (!MakerAPI.InsideAndLoaded) return;

				if (!TriggerEnabled)
				{
					Logger.Log(DebugLogLevel, $"[AccSlotChangedHandler][{ChaControl.chaFile.parameter?.fullname}] TriggerEnabled false");
					return;
				}
				CurOutfitTriggerInfo = CharaTriggerInfo.ElementAtOrDefault(CurrentCoordinateIndex);
				if (CurOutfitTriggerInfo == null) // basically won't happen
				{
					Logger.Log(DebugLogLevel, $"[AccSlotChangedHandler][{ChaControl.chaFile.parameter?.fullname}] CurOutfitTriggerInfo is null");
					return;
				}

				CheckOutfitTriggerInfoCount(CurrentCoordinateIndex);

				Logger.Log(DebugLogLevel, $"[AccSlotChangedHandler][{ChaControl.chaFile.parameter?.fullname}][SlotIndex]: {SlotIndex}");

				List<ChaFileAccessory.PartsInfo> PartsInfo = CharaAccInfo;
				ChaFileAccessory.PartsInfo PartInfo = PartsInfo.ElementAtOrDefault(SlotIndex);
				if ((PartInfo == null) || (CurOutfitTriggerInfo.Parts.ElementAtOrDefault(SlotIndex) == null))
				{
					Logger.LogError($"[AccSlotChangedHandler][{ChaControl.chaFile.parameter?.fullname}][PartInfo: {PartInfo}][CurSlotTriggerInfo: {CurSlotTriggerInfo}]");
					return;
				}

				CopySlotTriggerInfo(CurOutfitTriggerInfo.Parts[SlotIndex], CurSlotTriggerInfo);

				if (!Skip)
				{
					if ((CurSlotTriggerInfo.Kind > -1) && (PartInfo.type == 120))
					{
						CurOutfitTriggerInfo.Parts[SlotIndex] = new AccTriggerInfo(SlotIndex);
						Logger.LogMessage($"AccTriggerInfo for Coordinate {CurrentCoordinateIndex} Slot {CurSlotTriggerInfo.Slot + 1:00} has been reset");
					}
				}

				List<string> extra = CurOutfitVirtualGroupNames.Select(x => x.Value).ToList();
				CreateMakerDropdownItems(extra);

				int ddASSListVal = CurSlotTriggerInfo.Kind < 10 ? ddASSListVals.IndexOf(CurSlotTriggerInfo.Kind) : CurSlotTriggerInfo.Kind;
				GameObject.Find("ddASSList").GetComponentInChildren<TMP_Dropdown>().value = ddASSListVal;
				GameObject.Find("tglASS0").GetComponentInChildren<Toggle>().isOn = CurSlotTriggerInfo.State[0];
				GameObject.Find("tglASS1").GetComponentInChildren<Toggle>().isOn = CurSlotTriggerInfo.State[1];
				GameObject.Find("tglASS2").GetComponentInChildren<Toggle>().isOn = CurSlotTriggerInfo.State[2];
				GameObject.Find("tglASS3").GetComponentInChildren<Toggle>().isOn = CurSlotTriggerInfo.State[3];

				int refIndex = ddASSListVal < 10 ? ddASSListVal : 9;
				GameObject.Find("tglASS0").GetComponentInChildren<TextMeshProUGUI>().alpha = clothesStates[refIndex][0] ? 1f : 0.2f;
				GameObject.Find("tglASS1").GetComponentInChildren<TextMeshProUGUI>().alpha = clothesStates[refIndex][1] ? 1f : 0.2f;
				GameObject.Find("tglASS2").GetComponentInChildren<TextMeshProUGUI>().alpha = clothesStates[refIndex][2] ? 1f : 0.2f;
				GameObject.Find("tglASS3").GetComponentInChildren<TextMeshProUGUI>().alpha = clothesStates[refIndex][3] ? 1f : 0.2f;

				foreach (string group in GameObjectNames)
					Object.Destroy(GameObject.Find(group));
				GameObjectNames.Clear();

				FillVirtualGroupStates();

				AnchorOffsetMinY = (int) GameObject.Find("tglASS0").GetComponent<RectTransform>().offsetMin.y - 80;

				List<string> names = VirtualGroupStates.OrderBy(x => x.Key).Select(x => x.Key).ToList<string>();
				foreach (string group in names)
					CreateMakerVirtualGroupToggle(group);

				GameObject accw = GameObject.Find("04_AccessoryTop/AcsMoveWindow01");
				RectTransform windRt = accw.transform.Find("BasePanel/imgWindowBack").GetComponent<RectTransform>();
				windRt.offsetMin = new Vector2(0, ContainerOffsetMinY - 40 * names.Count());

				Logger.Log(DebugLogLevel, $"[AccSlotChangedHandler][{ChaControl.chaFile.parameter?.fullname}][Slot: {CurSlotTriggerInfo.Slot}][Kind: {CurSlotTriggerInfo.Kind}][State: {CurSlotTriggerInfo.State[0]}|{CurSlotTriggerInfo.State[1]}|{CurSlotTriggerInfo.State[2]}|{CurSlotTriggerInfo.State[3]}]");

				ChaControl.StartCoroutine(CoroutineSyncAllAccToggle(CoroutineSlotChangeDelay.Value));
			}

			internal void CheckOutfitTriggerInfoCount(int CoordinateIndex)
			{
				if (!MakerAPI.InsideMaker) return;

				bool Current = (CoordinateIndex == CurrentCoordinateIndex);
				int InfoCount = CharaTriggerInfo[CoordinateIndex].Parts.Count();
				int PartCount = MoreAccessories_Support.GetCoordinatePartsInfo(ChaControl, CoordinateIndex).Count();

				if (InfoCount > PartCount) // shit happend
				{
					CharaTriggerInfo[CoordinateIndex].Parts = CharaTriggerInfo[CoordinateIndex].Parts.Take(PartCount).ToList();
					Logger.LogError($"[CheckOutfitTriggerInfoCount][{ChaControl.chaFile.parameter?.fullname}] CharaTriggerInfo[{CoordinateIndex}].Parts removed: {InfoCount} -> {PartCount}");
				}

				if (InfoCount < PartCount)
				{
					for (int i = InfoCount; i < PartCount; i++)
						CharaTriggerInfo[CoordinateIndex].Parts.Add( new AccTriggerInfo(i) );
					Logger.Log(DebugLogLevel, $"[CheckOutfitTriggerInfoCount][{ChaControl.chaFile.parameter?.fullname}] CharaTriggerInfo[{CoordinateIndex}].Parts {InfoCount} -> {PartCount}");
				}

				if (Current)
					CurOutfitTriggerInfo = CharaTriggerInfo[CoordinateIndex];
			}

			internal void AccessoriesCopiedHandler(int CopySource, int CopyDestination, List<int> CopiedSlotIndexes)
			{
				if (!MakerAPI.InsideAndLoaded) return;

				Logger.Log(DebugLogLevel, $"[AccessoriesCopiedHandler][{ChaControl.chaFile.parameter?.fullname}][Soruce: {CopySource}][Destination: {CopyDestination}][CopiedSlotIndexes: {string.Join(",", CopiedSlotIndexes.Select(i => i.ToString()).ToArray())}]");

				CheckOutfitTriggerInfoCount(CopySource);
				CheckOutfitTriggerInfoCount(CopyDestination);

				foreach (int Slot in CopiedSlotIndexes)
					CopySlotTriggerInfo(CharaTriggerInfo[CopySource].Parts[Slot], CharaTriggerInfo[CopyDestination].Parts[Slot]);

				if (CopyDestination == CurrentCoordinateIndex)
					AccSlotChangedHandler(AccessoriesApi.SelectedMakerAccSlot, true);
			}

			internal void AccessoryTransferredHandler(int SourceSlotIndex, int DestinationSlotIndex) => AccessoryTransferredHandler(SourceSlotIndex, DestinationSlotIndex, CurrentCoordinateIndex);
			internal void AccessoryTransferredHandler(int SourceSlotIndex, int DestinationSlotIndex, int CoordinateIndex)
			{
				if (!MakerAPI.InsideAndLoaded) return;

				Logger.Log(DebugLogLevel, $"[AccessoryTransferredHandler][{ChaControl.chaFile.parameter?.fullname}] Fired!!");

				CheckOutfitTriggerInfoCount(CoordinateIndex);

				if (!TriggerEnabled)
				{
					Logger.Log(DebugLogLevel, $"[AccessoryTransferredHandler][{ChaControl.chaFile.parameter?.fullname}] TriggerEnabled false");
					return;
				}
				OutfitTriggerInfo Todo = CharaTriggerInfo.ElementAtOrDefault(CoordinateIndex);
				CopySlotTriggerInfo(Todo.Parts[SourceSlotIndex], Todo.Parts[DestinationSlotIndex]);
				Todo.Parts[DestinationSlotIndex].Slot = DestinationSlotIndex;
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
			}

			internal void RenameGroup(int kind, string label)
			{
				if (kind <= 9)
				{
					Logger.LogMessage($"Invalid kind {kind}");
					return;
				}
				string group = $"custom_{kind - 9}";
				CurOutfitVirtualGroupNames[group] = label;
				Logger.LogMessage($"[{group}] renamed into {label}");
			}

			internal void PushGroup()
			{
				int n = CurOutfitVirtualGroupNames.Count() + 1;
				CurOutfitVirtualGroupNames[$"custom_{n}"] = $"Custom {n}";
				Logger.LogMessage($"[custom_{n}] added");
			}

			internal void PopGroup()
			{
				int n = CurOutfitVirtualGroupNames.Count();
				if (n <= DefaultCustomGroupCount)
				{
					Logger.LogMessage($"Cannot go below {DefaultCustomGroupCount} custom group");
					return;
				}
				CurOutfitVirtualGroupNames.Remove($"custom_{n}");
				Logger.LogMessage($"[custom_{n}] removed");
			}
		}
	}
}
