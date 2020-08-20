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

			internal void ResetCharaVirtualGroupNames()
			{
				Logger.Log(DebugLogLevel, $"[ResetCharaVirtualGroupNames][{ChaControl.chaFile.parameter?.fullname}] Fired!!");
				CharaVirtualGroupNames.Clear();
				for (int i = 0; i < 7; i++)
					CharaVirtualGroupNames[i] = new Dictionary<string, string>();
			}

			internal void SyncCharaTriggerInfo()
			{
				if (!TriggerEnabled)
				{
					Logger.Log(DebugLogLevel, $"[SyncCharaTriggerInfo][{ChaControl.chaFile.parameter?.fullname}] TriggerEnabled false");
					return;
				}
				for (int CoordinateIndex = 0; CoordinateIndex < 7; CoordinateIndex++)
					SyncOutfitTriggerInfo(CoordinateIndex);
			}

			internal void SyncOutfitTriggerInfo(int CoordinateIndex)
			{
				Logger.Log(DebugLogLevel, $"[SyncOutfitTriggerInfo][{ChaControl.chaFile.parameter?.fullname}] Process OutfitTriggerInfo for Coordinate {CoordinateIndex}");
				if (!MakerAPI.InsideAndLoaded) return;

				List<ChaFileAccessory.PartsInfo> PartsInfo = MoreAccessories_Support.GetCoordinatePartsInfo(ChaControl, CoordinateIndex);

				if (!CharaTriggerInfo.ContainsKey(CoordinateIndex))
					CharaTriggerInfo[CoordinateIndex] = new OutfitTriggerInfo(CoordinateIndex);
				OutfitTriggerInfo OutfitTriggerInfo = CharaTriggerInfo[CoordinateIndex];

				Logger.Log(DebugLogLevel, $"[SyncCharaTriggerInfo][{ChaControl.chaFile.parameter?.fullname}] OutfitTriggerInfo.Parts.Count: {OutfitTriggerInfo.Parts.Count()}");
				List<int> Keys = OutfitTriggerInfo.Parts.Keys.ToList<int>();
				foreach (int SlotIndex in Keys)
				{
					AccTriggerInfo TriggerPart = OutfitTriggerInfo.Parts[SlotIndex];
					Logger.Log(DebugLogLevel, $"[SyncCharaTriggerInfo][{ChaControl.chaFile.parameter?.fullname}][Slot: {SlotIndex}][Kind: {TriggerPart.Kind}]");
					ChaFileAccessory.PartsInfo PartInfo = PartsInfo.ElementAtOrDefault(SlotIndex);

					if (PartInfo == null)
						CharaTriggerInfo[CoordinateIndex].Parts.Remove(SlotIndex);
					else
					{
						if (TriggerPart.Kind == -1)
						{
							CharaTriggerInfo[CoordinateIndex].Parts.Remove(SlotIndex);
							Logger.LogMessage($"AccTriggerInfo for Coordinate {CoordinateIndex} Slot{SlotIndex + 1:00} has been reset");
						}
						else
						{
							if (PartInfo.type == 120)
							{
								CharaTriggerInfo[CoordinateIndex].Parts.Remove(SlotIndex);
								Logger.LogMessage($"AccTriggerInfo for Coordinate {CoordinateIndex} Slot{SlotIndex + 1:00} has been reset");
							}
							else
							{
								if (MathfEx.RangeEqualOn(0, TriggerPart.Kind, 8))
									CharaTriggerInfo[CoordinateIndex].Parts[SlotIndex].Group = "";
								else if (TriggerPart.Kind == 9)
									CharaTriggerInfo[CoordinateIndex].Parts[SlotIndex].Group = PartInfo.parentKey;
								else if (TriggerPart.Kind > 9)
									CharaTriggerInfo[CoordinateIndex].Parts[SlotIndex].Group = "custom_" + (TriggerPart.Kind - 9).ToString();
							}
						}
					}
				}
			}

			internal void FillVirtualGroupStates()
			{
				if (CurOutfitTriggerInfo?.Parts?.Count() == 0)
				{
					VirtualGroupStates?.Clear();
					return;
				}

				Dictionary<string, bool> tmpVGS = new Dictionary<string, bool>();
				List<AccTriggerInfo> Filtered = CurOutfitTriggerInfo.Parts.Values.Where(x => x.Kind >= 9).Where(x => !string.IsNullOrEmpty(x.Group))?.ToList() ?? new List<AccTriggerInfo>();
				List<string> Groups = new List<string>();
				if (Filtered.Count() > 0)
					Groups = Filtered.OrderBy(x => x.Kind).OrderBy(x => x.Group).GroupBy(x => x.Group).Select(x => x.First().Group).ToList();
				foreach (string Group in Groups)
				{
					Logger.Log(DebugLogLevel, $"[FillVirtualGroupStates][{ChaControl.chaFile.parameter?.fullname}] Group: {Group}");
					if ((bool) VirtualGroupStates?.ContainsKey(Group))
						tmpVGS[Group] = VirtualGroupStates[Group];
					else
						tmpVGS[Group] = true;
				}
				VirtualGroupStates = tmpVGS.ToDictionary(entry => entry.Key, entry => entry.Value);
			}

			internal void FillVirtualGroupNames()
			{
				if (CurOutfitTriggerInfo?.Parts?.Count() == 0)
				{
					CurOutfitVirtualGroupNames?.Clear();
					return;
				}

				if (!CharaVirtualGroupNames.ContainsKey(CurrentCoordinateIndex))
					CharaVirtualGroupNames[CurrentCoordinateIndex] = new Dictionary<string, string>();
				if (CharaVirtualGroupNames[CurrentCoordinateIndex] == null)
					CharaVirtualGroupNames[CurrentCoordinateIndex] = new Dictionary<string, string>();
				CurOutfitVirtualGroupNames = CharaVirtualGroupNames[CurrentCoordinateIndex];

				int max = CurOutfitTriggerInfo.Parts.Values.Max(x => x.Kind);
				max = max > (9 + DefaultCustomGroupCount) ? (max - 9) : DefaultCustomGroupCount;
				int i = (int) CurOutfitVirtualGroupNames?.Count();
				if (i < max)
				{
					Logger.Log(DebugLogLevel, $"[FillVirtualGroupNames][{ChaControl.chaFile.parameter?.fullname}] Filled with {max - i} entries");

					for (; i < max; i++)
						CurOutfitVirtualGroupNames[$"custom_{i + 1}"] = $"Custom {i + 1}";
				}
			}

			internal void ToggleByClothesState(ChaControl chaCtrl, int kind, int state)
			{
				if (!TriggerEnabled)
					return;

				Logger.Log(DebugLogLevel, $"[ToggleByClothesState][{chaCtrl.chaFile.parameter?.fullname}] Fired!!");

				if (CurOutfitTriggerInfo?.Parts?.Count() == 0)
					return;
				if (!MathfEx.RangeEqualOn(0, kind, 6))
					return;
				List<AccTriggerInfo> Parts = CurOutfitTriggerInfo?.Parts?.Values?.Where(x => x.Kind == kind)?.ToList() ?? new List<AccTriggerInfo>();
				if (Parts.Count() > 0)
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
					Parts = CurOutfitTriggerInfo?.Parts?.Values?.Where(x => x.Kind == relKind)?.ToList() ?? new List<AccTriggerInfo>();
					if (Parts.Count() > 0)
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

				Logger.Log(DebugLogLevel, $"[ToggleByShoesType][{chaCtrl.chaFile.parameter?.fullname}] Fired!!");

				if (CurOutfitTriggerInfo?.Parts?.Count() == 0)
					return;
				if ((kind != 7) && (kind != 8))
					return;

				int cur = (chaCtrl.fileStatus.shoesType == 0) ? 7 : 8;
				int off = (cur == 7) ? 8 : 7;
				List<AccTriggerInfo> Parts = CurOutfitTriggerInfo?.Parts?.Values?.Where(x => x.Kind == cur)?.ToList() ?? new List<AccTriggerInfo>();
				if (Parts.Count() > 0)
				{
					foreach (AccTriggerInfo Part in Parts)
						chaCtrl.SetAccessoryState(Part.Slot, Part.State[state]);
				}
				Parts = CurOutfitTriggerInfo?.Parts?.Values?.Where(x => x.Kind == off)?.ToList() ?? new List<AccTriggerInfo>();
				if (Parts.Count() > 0)
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

				Logger.Log(DebugLogLevel, $"[SyncAllAccToggle][{ChaControl.chaFile.parameter?.fullname}] Fired!!");

				if (CurOutfitTriggerInfo?.Parts?.Count() == 0)
					return;
				if ((MakerAPI.InsideMaker) && (!CharaMakerPreview.Value))
				{
					Logger.Log(DebugLogLevel, $"[SyncAllAccToggle][{ChaControl.chaFile.parameter?.fullname}] Disabled by config");
					return;
				}

				List<int> Kinds = CurOutfitTriggerInfo.Parts.Values.OrderBy(x => x.Kind).GroupBy(x => x.Kind).Select(x => x.First().Kind).ToList() ?? new List<int>();
				if (Kinds.Count() == 0)
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
						if (VirtualGroupStates.ContainsKey(Part.Group))
							vis = VirtualGroupStates[Part.Group] ? Part.State[0] : Part.State[3];
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

				VirtualGroupStates[group] = show;

				foreach (AccTriggerInfo Part in CurOutfitTriggerInfo.Parts.Values)
				{
					if ((Part.Kind >= 9) && (!Part.Group.IsNullOrEmpty()))
					{
						if (Part.Group == group)
						{
							bool vis = show ? Part.State[0] : Part.State[3];
							ChaControl.SetAccessoryState(Part.Slot, vis);
							Logger.Log(DebugLogLevel, $"[ToggleByVirtualGroup][{ChaControl.chaFile.parameter?.fullname}][Part.Slot: {Part.Slot}][show: {show}]");
						}
					}
				}
			}
		}
	}
}
