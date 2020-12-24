using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UniRx;

using KKAPI.Chara;
using KKAPI.Maker;
using JetPack;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		public partial class AccStateSyncController : CharaCustomFunctionController
		{
			internal void ResetCharaTriggerInfo()
			{
				Logger.Log(DebugLogLevel, $"[ResetCharaTriggerInfo][{CharaFullName}] Fired!!");
				CharaTriggerInfo.Clear();
				for (int i = 0; i < 7; i++)
					CharaTriggerInfo[i] = new OutfitTriggerInfo(i);
			}

			internal void ResetCharaVirtualGroupInfo()
			{
				Logger.Log(DebugLogLevel, $"[ResetCharaVirtualGroupNames][{CharaFullName}] Fired!!");
				CharaVirtualGroupInfo.Clear();
				for (int i = 0; i < 7; i++)
					CharaVirtualGroupInfo[i] = new Dictionary<string, VirtualGroupInfo>();
			}

			internal void NullCheckOutfitTriggerInfo(int CoordinateIndex)
			{
				if (!CharaTriggerInfo.ContainsKey(CoordinateIndex))
					CharaTriggerInfo[CoordinateIndex] = new OutfitTriggerInfo(CoordinateIndex);
				if (CharaTriggerInfo[CoordinateIndex] == null)
					CharaTriggerInfo[CoordinateIndex] = new OutfitTriggerInfo(CoordinateIndex);
			}

			internal void NullCheckOutfitVirtualGroupInfo(int CoordinateIndex)
			{
				if (!CharaVirtualGroupInfo.ContainsKey(CoordinateIndex))
					CharaVirtualGroupInfo[CoordinateIndex] = new Dictionary<string, VirtualGroupInfo>();
				if (CharaVirtualGroupInfo[CoordinateIndex] == null)
					CharaVirtualGroupInfo[CoordinateIndex] = new Dictionary<string, VirtualGroupInfo>();
			}

			internal void NullCheckOnePieceTriggerInfo(int CoordinateIndex)
			{
				if ((CharaTriggerInfo[CoordinateIndex].OnePiece?.ContainsKey("top") != true) || (CharaTriggerInfo[CoordinateIndex].OnePiece?.ContainsKey("bra") != true))
				{
					Logger.Log(DebugLogLevel, $"[NullCheckOnePieceTriggerInfo][{CharaFullName}] Init OnePiece info for outfit[{CoordinateIndex}]");
					CharaTriggerInfo[CoordinateIndex].OnePiece = new Dictionary<string, bool>() { ["top"] = false, ["bra"] = false };
				}
			}

			internal void RestoreOutfitVirtualGroupStates(int CoordinateIndex)
			{
				if (CharaVirtualGroupStates[CoordinateIndex].Count() > 0)
				{
					foreach (KeyValuePair<string, bool> group in CharaVirtualGroupStates[CoordinateIndex])
					{
						if (CharaVirtualGroupInfo[CoordinateIndex].ContainsKey(group.Key))
							CharaVirtualGroupInfo[CoordinateIndex][group.Key].State = group.Value;
					}
				}
				CharaVirtualGroupStates[CoordinateIndex].Clear();
			}

			internal void RestoreCharaVirtualGroupStates()
			{
				for (int i = 0; i < 7; i++)
					RestoreOutfitVirtualGroupStates(i);
			}

			internal void StoreOutfitVirtualGroupStates(int CoordinateIndex)
			{
				CharaVirtualGroupStates[CoordinateIndex].Clear();
				if (CharaVirtualGroupInfo[CoordinateIndex].Count() > 0)
				{
					foreach (KeyValuePair<string, VirtualGroupInfo> group in CharaVirtualGroupInfo[CoordinateIndex])
						CharaVirtualGroupStates[CoordinateIndex][group.Key] = group.Value.State;
				}
			}

			internal void StoreCharaVirtualGroupStates()
			{
				for (int i = 0; i < 7; i++)
					StoreOutfitVirtualGroupStates(i);
			}

			internal void SyncCharaTriggerInfo()
			{
				for (int CoordinateIndex = 0; CoordinateIndex < 7; CoordinateIndex++)
					SyncOutfitTriggerInfo(CoordinateIndex);
			}

			internal void SyncOutfitTriggerInfo(int CoordinateIndex)
			{
				if (!MakerAPI.InsideAndLoaded) return;
				if (!TriggerEnabled) return;
				Logger.Log(DebugLogLevel, $"[SyncOutfitTriggerInfo][{CharaFullName}] Process OutfitTriggerInfo for Coordinate {CoordinateIndex}");

				List<ChaFileAccessory.PartsInfo> PartsInfo = ChaControl.ListPartsInfo(CoordinateIndex);

				NullCheckOutfitTriggerInfo(CoordinateIndex);
				OutfitTriggerInfo OutfitTriggerInfo = CharaTriggerInfo[CoordinateIndex];

				Logger.Log(DebugLogLevel, $"[SyncCharaTriggerInfo][{CharaFullName}] OutfitTriggerInfo.Parts.Count: {OutfitTriggerInfo.Parts.Count()} before sync");
				List<int> Keys = OutfitTriggerInfo.Parts.Keys.ToList();
				foreach (int SlotIndex in Keys)
				{
					AccTriggerInfo TriggerPart = OutfitTriggerInfo.Parts[SlotIndex];
					Logger.Log(DebugLogLevel, $"[SyncCharaTriggerInfo][{CharaFullName}][Slot: {SlotIndex}][Kind: {TriggerPart.Kind}]");
					ChaFileAccessory.PartsInfo PartInfo = PartsInfo.ElementAtOrDefault(SlotIndex);

					if (PartInfo == null)
					{
						CharaTriggerInfo[CoordinateIndex].Parts.Remove(SlotIndex);
						Logger.LogMessage($"AccTriggerInfo for Coordinate {CoordinateIndex} Slot{SlotIndex + 1:00} has been reset");
						continue;
					}
					if ((PartInfo.type == 120) || (TriggerPart.Kind == -1))
					{
						CharaTriggerInfo[CoordinateIndex].Parts.Remove(SlotIndex);
						Logger.LogMessage($"AccTriggerInfo for Coordinate {CoordinateIndex} Slot{SlotIndex + 1:00} has been reset");
						continue;
					}

					if (MathfEx.RangeEqualOn(0, TriggerPart.Kind, 8))
						CharaTriggerInfo[CoordinateIndex].Parts[SlotIndex].Group = "";
					else if (TriggerPart.Kind == 9)
						CharaTriggerInfo[CoordinateIndex].Parts[SlotIndex].Group = PartInfo.parentKey;
					else if (TriggerPart.Kind > 9)
						CharaTriggerInfo[CoordinateIndex].Parts[SlotIndex].Group = "custom_" + (TriggerPart.Kind - 9).ToString();
				}
				Logger.Log(DebugLogLevel, $"[SyncCharaTriggerInfo][{CharaFullName}] OutfitTriggerInfo.Parts.Count: {OutfitTriggerInfo.Parts.Count()} after sync");

				SyncOutfitVirtualGroupInfo(CoordinateIndex);
				TrimOutfitVirtualGroupInfo(CoordinateIndex);
				NullCheckOnePieceTriggerInfo(CoordinateIndex);
			}

			internal void PadOutfitVirtualGroupInfo(int CoordinateIndex)
			{
				int max = -1;
				if (CharaTriggerInfo[CoordinateIndex]?.Parts?.Count() > 0)
				{
					max = CharaTriggerInfo[CoordinateIndex].Parts.Values.Max(x => x.Kind);
					for (int i = 10; i <= max; i++)
					{
						string Group = $"custom_{i - 9}";
						if (CharaVirtualGroupInfo[CoordinateIndex].ContainsKey(Group))
							continue;
						int Kind = i;
						CharaVirtualGroupInfo[CoordinateIndex][Group] = new VirtualGroupInfo(Group, Kind);
#if DEBUG
						Logger.LogError($"[PadOutfitVirtualGroupInfo][{CharaFullName}][CoordinateIndex: {CoordinateIndex}] \"{Group}\" added missing");
#endif
					}
				}
			}

			internal void TrimOutfitVirtualGroupInfo(int CoordinateIndex)
			{
				int max = -1;
				if (CharaTriggerInfo[CoordinateIndex]?.Parts?.Count() > 0)
					max = CharaTriggerInfo[CoordinateIndex].Parts.Values.Max(x => x.Kind);

				int check = -1;
				if (CharaVirtualGroupInfo[CoordinateIndex].Count() > 0)
					check = CharaVirtualGroupInfo[CoordinateIndex].Values.Max(x => x.Kind);

				if ((check > -1) && (check > max))
				{
					for (int i = max + 1; i <= check; i++)
					{
						string Group = $"custom_{i - 9}";
						CharaVirtualGroupInfo[CoordinateIndex].Remove(Group);
#if DEBUG
						Logger.LogError($"[TrimOutfitVirtualGroupInfo][{CharaFullName}][CoordinateIndex: {CoordinateIndex}] \"{Group}\" unused and removed");
#endif
					}
				}
			}

			internal void ResetOutfitVirtualGroupState(int CoordinateIndex)
			{
				if (CharaVirtualGroupInfo[CoordinateIndex].Count() > 0)
				{
					foreach (KeyValuePair<string, VirtualGroupInfo> group in CharaVirtualGroupInfo[CoordinateIndex])
						CharaVirtualGroupInfo[CoordinateIndex][group.Key].State = true;
				}
			}

			internal void ResetCharaVirtualGroupState()
			{
				for (int i = 0; i < 7; i++)
					ResetOutfitVirtualGroupState(i);
			}

			internal void SortOutfitVirtualGroupInfo(int CoordinateIndex)
			{
				if (CharaTriggerInfo[CoordinateIndex]?.Parts?.Count() > 0)
					CharaVirtualGroupInfo[CoordinateIndex] = CharaVirtualGroupInfo[CoordinateIndex].Values.OrderBy(x => x.Kind).ThenBy(x => x.Group).ToDictionary(x => x.Group, x => x);
			}

			internal void SyncOutfitVirtualGroupInfo(int CoordinateIndex)
			{
				if (!TriggerEnabled)
					return;

				OutfitTriggerInfo OutfitTriggerInfo = CharaTriggerInfo[CoordinateIndex];
				foreach (AccTriggerInfo TriggerPart in OutfitTriggerInfo.Parts.Values)
				{
					if (TriggerPart.Kind >= 9)
					{
						if (CharaVirtualGroupInfo[CoordinateIndex].ContainsKey(TriggerPart.Group)) continue;

						string Group = TriggerPart.Group;
						int Kind = TriggerPart.Kind;
						CharaVirtualGroupInfo[CoordinateIndex][Group] = new VirtualGroupInfo(Group, Kind);
						Logger.LogError($"[SyncOutfitVirtualGroupInfo][{CharaFullName}][CoordinateIndex: {CoordinateIndex}] \"{Group}\" added missing");
					}
				}

				List<string> Filtered = CharaVirtualGroupInfo[CoordinateIndex]?.Values?.Where(x => x.Kind == 9)?.GroupBy(x => x.Group)?.Select(x => x.First().Group)?.ToList();

				if (Filtered?.Count() > 0)
				{
					foreach (string Group in Filtered)
					{
						if (CharaTriggerInfo[CoordinateIndex]?.Parts?.Values?.FirstOrDefault(x => x.Group == Group) == null)
						{
							CharaVirtualGroupInfo[CoordinateIndex].Remove(Group);
							Logger.LogError($"[SyncOutfitVirtualGroupNames][{CharaFullName}][CoordinateIndex: {CoordinateIndex}] \"{Group}\" unused and removed");
						}
					}
				}

				PadOutfitVirtualGroupInfo(CoordinateIndex);
				SortOutfitVirtualGroupInfo(CoordinateIndex);
			}

			internal void ToggleByClothesState(int kind, int state)
			{
				if (!TriggerEnabled)
					return;
				if (CurOutfitTriggerInfo?.Parts?.Count() == 0)
					return;

				Logger.Log(DebugLogLevel, $"[ToggleByClothesState][{CharaFullName}] Fired!!");

				if (!MathfEx.RangeEqualOn(0, kind, 6))
					return;
				List<AccTriggerInfo> Parts = GetPartsOfKind(kind);
				if (Parts.Count() > 0)
				{
					foreach (AccTriggerInfo Part in Parts)
						ShowAccessory(Part.Slot, Part.State[state]);
				}

				int relKind = -1;
				int relState = -1;

				if ((kind == 0) && (CharaTriggerInfo[CurrentCoordinateIndex].OnePiece["top"]))
				{
					relKind = 1;
					relState = (state == 3) ? 3 : ChaControl.fileStatus.clothesState[relKind];
				}
				else if ((kind == 1) && (CharaTriggerInfo[CurrentCoordinateIndex].OnePiece["top"]))
				{
					relKind = 0;
					relState = (state == 3) ? 3 : ChaControl.fileStatus.clothesState[relKind];
				}
				else if ((kind == 2) && (CharaTriggerInfo[CurrentCoordinateIndex].OnePiece["bra"]))
				{
					relKind = 3;
					relState = (state == 3) ? 3 : ChaControl.fileStatus.clothesState[relKind];
				}
				else if ((kind == 3) && (CharaTriggerInfo[CurrentCoordinateIndex].OnePiece["bra"]))
				{
					relKind = 2;
					relState = (state == 3) ? 3 : ChaControl.fileStatus.clothesState[relKind];
				}

				if ((relKind > -1) && (relState > -1))
				{
					Parts = GetPartsOfKind(relKind);
					if (Parts.Count() > 0)
					{
						foreach (AccTriggerInfo Part in Parts)
							ShowAccessory(Part.Slot, Part.State[relState]);
					}
				}
			}

			internal void ToggleByShoesType(int kind, int state)
			{
				if (!TriggerEnabled)
					return;
				if (CurOutfitTriggerInfo?.Parts?.Count() == 0)
					return;

				Logger.Log(DebugLogLevel, $"[ToggleByShoesType][{CharaFullName}] Fired!!");

				if ((kind != 7) && (kind != 8))
					return;
				int cur = (ChaControl.fileStatus.shoesType == 0) ? 7 : 8;
				int off = (cur == 7) ? 8 : 7;
				List<AccTriggerInfo> Parts = GetPartsOfKind(cur);
				if (Parts.Count() > 0)
				{
					foreach (AccTriggerInfo Part in Parts)
						ShowAccessory(Part.Slot, Part.State[state]);
				}
				Parts = GetPartsOfKind(off);
				if (Parts.Count() > 0)
				{
					foreach (AccTriggerInfo Part in Parts)
						ShowAccessory(Part.Slot, false);
				}
			}

			internal IEnumerator SyncAllAccToggleCoroutine()
			{
				yield return new WaitForEndOfFrame();
				yield return new WaitForEndOfFrame();

				SyncAllAccToggle();
			}

			internal void SyncAllAccToggle()
			{
				if (!TriggerEnabled)
					return;
				if (CurOutfitTriggerInfo?.Parts?.Count() == 0)
					return;

				Logger.Log(DebugLogLevel, $"[SyncAllAccToggle][{CharaFullName}] Fired!!");

				if ((MakerAPI.InsideMaker) && (!CharaMakerPreview.Value))
				{
					Logger.Log(DebugLogLevel, $"[SyncAllAccToggle][{CharaFullName}] Disabled by config");
					return;
				}
				// https://stackoverflow.com/questions/19406242/select-distinct-using-linq
				List<int> Kinds = CurOutfitTriggerInfo.Parts.Values.OrderBy(x => x.Kind)?.GroupBy(x => x.Kind)?.Select(x => x.First().Kind)?.ToList();
				if (Kinds?.Count() == 0)
				{
					Logger.Log(DebugLogLevel, $"[SyncAllAccToggle][{CharaFullName}] Nothing to trigger");
					return;
				}
				// https://stackoverflow.com/questions/1528724/converting-a-listint-to-a-comma-separated-string
				Logger.Log(DebugLogLevel, $"[SyncAllAccToggle][{CharaFullName}][Kinds: {string.Join(",", Kinds.Select(Kind => Kind.ToString()).ToArray())}]");

				byte[] ClothesState = ChaControl.fileStatus.clothesState;
				byte ShoesType = ChaControl.fileStatus.shoesType;
				foreach (AccTriggerInfo Part in CurOutfitTriggerInfo.Parts.Values)
				{
					if (MathfEx.RangeEqualOn(0, Part.Kind, 6))
					{
						int state = ClothesState[Part.Kind];
						bool vis = Part.State[state];
						Logger.Log(DebugLogLevel, $"[SyncAllAccToggle][{CharaFullName}][slot: {Part.Slot}][kind: {Part.Kind}][state: {state}][vis: {vis}]");
						ShowAccessory(Part.Slot, vis);
					}
					else if ((Part.Kind == 7) || (Part.Kind == 8))
					{
						int clothesKind = (ShoesType == 0) ? 7 : 8;
						bool vis = false;
						if (clothesKind == Part.Kind)
							vis = Part.State[ClothesState[Part.Kind]];
						ShowAccessory(Part.Slot, vis);
					}
					else if ((Part.Kind >= 9) && (!Part.Group.IsNullOrEmpty()))
					{
						bool vis = true;
						if (CurOutfitVirtualGroupInfo.ContainsKey(Part.Group))
							vis = CurOutfitVirtualGroupInfo[Part.Group].State ? Part.State[0] : Part.State[3];
						ShowAccessory(Part.Slot, vis);
					}
				}
			}

			internal void ToggleByVirtualGroup(string group, bool show)
			{
				List<AccTriggerInfo> filtered = GetPartsOfGroup(group);
				Logger.Log(DebugLogLevel, $"[ToggleByVirtualGroup][{CharaFullName}][group: {group}][show: {show}][count: {filtered.Count}]");
				foreach (AccTriggerInfo Part in filtered)
				{
					bool vis = show ? Part.State[0] : Part.State[3];
					ShowAccessory(Part.Slot, vis);
					Logger.Log(DebugLogLevel, $"[ToggleByVirtualGroup][{CharaFullName}][Part.Slot: {Part.Slot}][show: {show}]");
				}
			}
			/*
			internal IEnumerator WaitForEndOfFrameInitCurOutfitTriggerInfo(string caller)
			{
				yield return new WaitForEndOfFrame();
				yield return new WaitForEndOfFrame();

				InitCurOutfitTriggerInfo(caller);
			}
			*/
			internal void InitCurOutfitTriggerInfo(string caller)
			{
				Logger.Log(DebugLogLevel, $"[InitCurOutfitTriggerInfo][{CharaFullName}] Fired!!");

				TriggerEnabled = false;

				NullCheckOutfitTriggerInfo(CurrentCoordinateIndex);
				CurOutfitTriggerInfo = CharaTriggerInfo[CurrentCoordinateIndex];
				Logger.Log(DebugLogLevel, $"[InitCurOutfitTriggerInfo] CurOutfitTriggerInfo.Parts.Count() {CurOutfitTriggerInfo.Parts.Count()}");

				if ((!MakerAPI.InsideMaker) && (CurOutfitTriggerInfo.Parts.Count() == 0))
				{
					Logger.Log(DebugLogLevel, $"[InitOutfitTriggerInfo][{CharaFullName}] TriggerEnabled false");
					if (CharaStudio.Inside)
					{
						Logger.Log(DebugLogLevel, $"[InitCurOutfitTriggerInfo][CurTreeNodeObjID: {CharaStudio.CurTreeNodeObjID}][TreeNodeObjID: {TreeNodeObjID}]");
						CharaStudio.ClearUI();
					}
					else if (HScene.Inside)
					{
						HScene.ClearUI();
						HScene.UpdateUI();
					}

					return;
				}

				TriggerEnabled = true;

				SyncOutfitVirtualGroupInfo(CurrentCoordinateIndex);
				CurOutfitVirtualGroupInfo = CharaVirtualGroupInfo[CurrentCoordinateIndex];

				if (MakerAPI.InsideMaker)
				{
					if (!PreserveVirtualGroupState.Value)
						ResetOutfitVirtualGroupState(CurrentCoordinateIndex);
					SetAccessoryStateAll(true);
					SkipSlotChangePartTypeCheck = true;
					StartCoroutine(AccSlotChangedHandlerCoroutine(AccessoriesApi.SelectedMakerAccSlot));
				}
				else if (CharaStudio.Inside)
				{
					Logger.Log(DebugLogLevel, $"[InitCurOutfitTriggerInfo][CurTreeNodeObjID: {CharaStudio.CurTreeNodeObjID}][TreeNodeObjID: {TreeNodeObjID}]");
					if (TreeNodeObjID == CharaStudio.CurTreeNodeObjID)
						CharaStudio.UpdateUI();
					if (caller != "OnReload")
					{
						SetAccessoryStateAll(true);
						SyncAllAccToggle();
					}
				}
				else if (HScene.Inside)
				{
					HScene.ClearUI();
					HScene.UpdateUI();
					SetAccessoryStateAll(true);
					SyncAllAccToggle();
				}
				else
				{
					SetAccessoryStateAll(true);
					SyncAllAccToggle();
				}
			}
		}
	}
}
