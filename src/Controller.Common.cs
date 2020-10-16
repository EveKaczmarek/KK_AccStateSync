using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using KKAPI.Chara;
using KKAPI.Maker;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		public partial class AccStateSyncController : CharaCustomFunctionController
		{
			internal void ResetCharaTriggerInfo()
			{
				Logger.Log(DebugLogLevel, $"[ResetCharaTriggerInfo][{ChaControl.chaFile.parameter?.fullname}] Fired!!");
				CharaTriggerInfo.Clear();
				for (int i = 0; i < 7; i++)
					CharaTriggerInfo[i] = new OutfitTriggerInfo(i);
			}

			internal void ResetCharaVirtualGroupInfo()
			{
				Logger.Log(DebugLogLevel, $"[ResetCharaVirtualGroupNames][{ChaControl.chaFile.parameter?.fullname}] Fired!!");
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
					Logger.Log(DebugLogLevel, $"[NullCheckOnePieceTriggerInfo][{ChaControl.chaFile.parameter?.fullname}] Init OnePiece info for outfit[{CoordinateIndex}]");
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
				Logger.Log(DebugLogLevel, $"[SyncOutfitTriggerInfo][{ChaControl.chaFile.parameter?.fullname}] Process OutfitTriggerInfo for Coordinate {CoordinateIndex}");

				List<ChaFileAccessory.PartsInfo> PartsInfo = MoreAccessories_Support.GetCoordinatePartsInfo(ChaControl, CoordinateIndex);

				NullCheckOutfitTriggerInfo(CoordinateIndex);
				OutfitTriggerInfo OutfitTriggerInfo = CharaTriggerInfo[CoordinateIndex];

				Logger.Log(DebugLogLevel, $"[SyncCharaTriggerInfo][{ChaControl.chaFile.parameter?.fullname}] OutfitTriggerInfo.Parts.Count: {OutfitTriggerInfo.Parts.Count()} before sync");
				List<int> Keys = OutfitTriggerInfo.Parts.Keys.ToList<int>();
				foreach (int SlotIndex in Keys)
				{
					AccTriggerInfo TriggerPart = OutfitTriggerInfo.Parts[SlotIndex];
					Logger.Log(DebugLogLevel, $"[SyncCharaTriggerInfo][{ChaControl.chaFile.parameter?.fullname}][Slot: {SlotIndex}][Kind: {TriggerPart.Kind}]");
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
				Logger.Log(DebugLogLevel, $"[SyncCharaTriggerInfo][{ChaControl.chaFile.parameter?.fullname}] OutfitTriggerInfo.Parts.Count: {OutfitTriggerInfo.Parts.Count()} after sync");

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
						Logger.LogError($"[PadOutfitVirtualGroupInfo][{ChaControl.chaFile.parameter?.fullname}][CoordinateIndex: {CoordinateIndex}] \"{Group}\" added missing");
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
						Logger.LogError($"[TrimOutfitVirtualGroupInfo][{ChaControl.chaFile.parameter?.fullname}][CoordinateIndex: {CoordinateIndex}] \"{Group}\" unused and removed");
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
						Logger.LogError($"[SyncOutfitVirtualGroupInfo][{ChaControl.chaFile.parameter?.fullname}][CoordinateIndex: {CoordinateIndex}] \"{Group}\" added missing");
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
							Logger.LogError($"[SyncOutfitVirtualGroupNames][{ChaControl.chaFile.parameter?.fullname}][CoordinateIndex: {CoordinateIndex}] \"{Group}\" unused and removed");
						}
					}
				}

				PadOutfitVirtualGroupInfo(CoordinateIndex);
				SortOutfitVirtualGroupInfo(CoordinateIndex);
			}

			internal void ToggleByClothesState(ChaControl chaCtrl, int kind, int state)
			{
				if (!TriggerEnabled)
					return;
				if (CurOutfitTriggerInfo?.Parts?.Count() == 0)
					return;

				Logger.Log(DebugLogLevel, $"[ToggleByClothesState][{chaCtrl.chaFile.parameter?.fullname}] Fired!!");

				if (!MathfEx.RangeEqualOn(0, kind, 6))
					return;
				List<AccTriggerInfo> Parts = CurOutfitTriggerInfo?.Parts?.Values?.Where(x => x.Kind == kind)?.ToList();
				if (Parts?.Count() > 0)
				{
					foreach (AccTriggerInfo Part in Parts)
						chaCtrl.SetAccessoryState(Part.Slot, Part.State[state]);
				}

				int relKind = -1;
				int relState = -1;

				if ((kind == 0) && (CharaTriggerInfo[CurrentCoordinateIndex].OnePiece["top"]))
				{
					relKind = 1;
					relState = (state == 3) ? 3 : chaCtrl.fileStatus.clothesState[relKind];
				}
				else if ((kind == 1) && (CharaTriggerInfo[CurrentCoordinateIndex].OnePiece["top"]))
				{
					relKind = 0;
					relState = (state == 3) ? 3 : chaCtrl.fileStatus.clothesState[relKind];
				}
				else if ((kind == 2) && (CharaTriggerInfo[CurrentCoordinateIndex].OnePiece["bra"]))
				{
					relKind = 3;
					relState = (state == 3) ? 3 : chaCtrl.fileStatus.clothesState[relKind];
				}
				else if ((kind == 3) && (CharaTriggerInfo[CurrentCoordinateIndex].OnePiece["bra"]))
				{
					relKind = 2;
					relState = (state == 3) ? 3 : chaCtrl.fileStatus.clothesState[relKind];
				}

				if ((relKind > -1) && (relState > -1))
				{
					Parts = CurOutfitTriggerInfo?.Parts?.Values?.Where(x => x.Kind == relKind)?.ToList();
					if (Parts?.Count() > 0)
					{
						foreach (AccTriggerInfo Part in Parts)
							chaCtrl.SetAccessoryState(Part.Slot, Part.State[relState]);
					}
				}
			}

			internal void ToggleByShoesType(ChaControl chaCtrl, int kind, int state)
			{
				if (!TriggerEnabled)
					return;
				if (CurOutfitTriggerInfo?.Parts?.Count() == 0)
					return;

				Logger.Log(DebugLogLevel, $"[ToggleByShoesType][{chaCtrl.chaFile.parameter?.fullname}] Fired!!");

				if ((kind != 7) && (kind != 8))
					return;
				int cur = (chaCtrl.fileStatus.shoesType == 0) ? 7 : 8;
				int off = (cur == 7) ? 8 : 7;
				List<AccTriggerInfo> Parts = CurOutfitTriggerInfo?.Parts?.Values?.Where(x => x.Kind == cur)?.ToList();
				if (Parts?.Count() > 0)
				{
					foreach (AccTriggerInfo Part in Parts)
						chaCtrl.SetAccessoryState(Part.Slot, Part.State[state]);
				}
				Parts = CurOutfitTriggerInfo?.Parts?.Values?.Where(x => x.Kind == off)?.ToList();
				if (Parts?.Count() > 0)
				{
					foreach (AccTriggerInfo Part in Parts)
						chaCtrl.SetAccessoryState(Part.Slot, false);
				}
			}

			internal IEnumerator WaitForEndOfFrameSyncAllAccToggle()
			{
				// trick from MakerOptimizations, seems only works with 2 lines
				yield return new UnityEngine.WaitForEndOfFrame();
				yield return new UnityEngine.WaitForEndOfFrame();

				SkipAutoSave = false;
				SyncAllAccToggle();
			}

			internal void SyncAllAccToggle()
			{
				if (!TriggerEnabled)
					return;
				if (CurOutfitTriggerInfo?.Parts?.Count() == 0)
					return;

				Logger.Log(DebugLogLevel, $"[SyncAllAccToggle][{ChaControl.chaFile.parameter?.fullname}] Fired!!");

				if ((MakerAPI.InsideMaker) && (!CharaMakerPreview.Value))
				{
					Logger.Log(DebugLogLevel, $"[SyncAllAccToggle][{ChaControl.chaFile.parameter?.fullname}] Disabled by config");
					return;
				}

				List<int> Kinds = CurOutfitTriggerInfo?.Parts?.Values?.OrderBy(x => x.Kind)?.GroupBy(x => x.Kind)?.Select(x => x.First().Kind)?.ToList();
				if (Kinds?.Count() == 0)
				{
					Logger.Log(DebugLogLevel, $"[SyncAllAccToggle][{ChaControl.chaFile.parameter?.fullname}] Nothing to trigger");
					return;
				}
				Logger.Log(DebugLogLevel, $"[SyncAllAccToggle][{ChaControl.chaFile.parameter?.fullname}][Kinds: {string.Join(",", Kinds.Select(Kind => Kind.ToString()).ToArray())}]");

				byte[] ClothesState = ChaControl.fileStatus.clothesState;
				byte ShoesType = ChaControl.fileStatus.shoesType;
				foreach (AccTriggerInfo Part in CurOutfitTriggerInfo.Parts.Values)
				{
					if (MathfEx.RangeEqualOn(0, Part.Kind, 6))
					{
						int state = ClothesState[Part.Kind];
						bool vis = Part.State[state];
						Logger.Log(DebugLogLevel, $"[SyncAllAccToggle][{ChaControl.chaFile.parameter?.fullname}][slot: {Part.Slot}][kind: {Part.Kind}][state: {state}][vis: {vis}]");
						ChaControl.SetAccessoryState(Part.Slot, vis);
					}
					else if ((Part.Kind == 7) || (Part.Kind == 8))
					{
						int clothesKind = (ShoesType == 0) ? 7 : 8;
						bool vis = false;
						if (clothesKind == Part.Kind)
							vis = Part.State[ClothesState[Part.Kind]];
						ChaControl.SetAccessoryState(Part.Slot, vis);
					}
					else if ((Part.Kind >= 9) && (!Part.Group.IsNullOrEmpty()))
					{
						bool vis = true;
						if (CurOutfitVirtualGroupInfo.ContainsKey(Part.Group))
							vis = CurOutfitVirtualGroupInfo[Part.Group].State ? Part.State[0] : Part.State[3];
						ChaControl.SetAccessoryState(Part.Slot, vis);
					}
				}
			}

			internal void ToggleByVirtualGroup(string group, bool show)
			{
				if (!TriggerEnabled)
					return;
				if (CurOutfitTriggerInfo?.Parts?.Count() == 0)
					return;

				Logger.Log(DebugLogLevel, $"[ToggleByVirtualGroup][{ChaControl.chaFile.parameter?.fullname}][group: {group}][show: {show}]");

				CurOutfitVirtualGroupInfo[group].State = show;

				foreach (AccTriggerInfo Part in CurOutfitTriggerInfo.Parts.Values)
				{
					if ((Part.Kind < 9) || (Part.Group.IsNullOrEmpty()))
						continue;

					if (Part.Group == group)
					{
						bool vis = show ? Part.State[0] : Part.State[3];
						ChaControl.SetAccessoryState(Part.Slot, vis);
						Logger.Log(DebugLogLevel, $"[ToggleByVirtualGroup][{ChaControl.chaFile.parameter?.fullname}][Part.Slot: {Part.Slot}][show: {show}]");
					}
				}
			}

			internal void InitCurOutfitTriggerInfo(string caller)
			{
				Logger.Log(DebugLogLevel, $"[InitCurOutfitTriggerInfo][{ChaControl.chaFile.parameter?.fullname}] Fired!!");

				TriggerEnabled = false;

				CurOutfitTriggerInfo = CharaTriggerInfo[CurrentCoordinateIndex];
				Logger.Log(DebugLogLevel, $"[InitCurOutfitTriggerInfo] CurOutfitTriggerInfo.Parts.Count() {CurOutfitTriggerInfo.Parts.Count()}");

				if ((!MakerAPI.InsideMaker) && (CurOutfitTriggerInfo.Parts.Count() == 0))
				{
					Logger.Log(DebugLogLevel, $"[InitOutfitTriggerInfo][{ChaControl.chaFile.parameter?.fullname}] TriggerEnabled false");
					if (CharaStudio.Inside)
					{
						Logger.Log(DebugLogLevel, $"[InitCurOutfitTriggerInfo][CurTreeNodeObjID: {CharaStudio.CurTreeNodeObjID}][TreeNodeObjID: {TreeNodeObjID}]");
						CharaStudio.ClearUI();
					}
					else if (HScene.Inside)
						HScene.ClearUI();
					return;
				}

				TriggerEnabled = true;

				SyncOutfitVirtualGroupInfo(CurrentCoordinateIndex);
				CurOutfitVirtualGroupInfo = CharaVirtualGroupInfo[CurrentCoordinateIndex];

				if (MakerAPI.InsideMaker)
				{
					if (!PreserveVirtualGroupState.Value)
						ResetOutfitVirtualGroupState(CurrentCoordinateIndex);
					ChaControl.SetAccessoryStateAll(true);
					AccSlotChangedHandler(AccessoriesApi.SelectedMakerAccSlot, true);
				}
				else if (CharaStudio.Inside)
				{
					Logger.Log(DebugLogLevel, $"[InitCurOutfitTriggerInfo][CurTreeNodeObjID: {CharaStudio.CurTreeNodeObjID}][TreeNodeObjID: {TreeNodeObjID}]");
					if (TreeNodeObjID == CharaStudio.CurTreeNodeObjID)
						CharaStudio.UpdateUI();
					if (caller != "OnReload")
					{
						ChaControl.SetAccessoryStateAll(true);
						SyncAllAccToggle();
					}
				}
				else if (HScene.Inside)
				{
					HScene.UpdateUI();
					ChaControl.SetAccessoryStateAll(true);
					SyncAllAccToggle();
				}
				else
				{
					ChaControl.SetAccessoryStateAll(true);
					SyncAllAccToggle();
				}
			}
		}
	}
}
