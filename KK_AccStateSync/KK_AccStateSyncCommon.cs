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
				CharaTriggerInfo = new List<OutfitTriggerInfo>();
				for (int i = 0; i < 7; i++)
					CharaTriggerInfo.Add(new OutfitTriggerInfo(i));
			}

			internal void ResetCharaVirtualGroupNames()
			{
				CharaVirtualGroupNames = new List<Dictionary<string, string>>();
				for (int i = 0; i < 7; i++)
					CharaVirtualGroupNames.Add(new Dictionary<string, string>());
			}

			internal void CopySlotTriggerInfo(AccTriggerInfo CopySource, AccTriggerInfo CopyDestination)
			{
				CopyDestination.Slot = CopySource.Slot;
				CopyDestination.Kind = CopySource.Kind;
				CopyDestination.Group = CopySource.Group;
				CopyDestination.State = CopySource.State.ToList();
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
				if (!TriggerEnabled)
				{
					Logger.Log(DebugLogLevel, $"[SyncOutfitTriggerInfo][{ChaControl.chaFile.parameter?.fullname}] TriggerEnabled false");
					return;
				}

				List<ChaFileAccessory.PartsInfo> PartsInfo = MoreAccessories_Support.GetCoordinatePartsInfo(ChaControl, CoordinateIndex);
				OutfitTriggerInfo OutfitTriggerInfo = CharaTriggerInfo.ElementAtOrDefault(CoordinateIndex);

				Logger.Log(DebugLogLevel, $"[SyncOutfitTriggerInfo][{ChaControl.chaFile.parameter?.fullname}]Process OutfitTriggerInfo for Coordinate {CoordinateIndex}");

				List<AccTriggerInfo> TriggerParts = OutfitTriggerInfo.Parts;
				int PartsInfoCount = PartsInfo.Count();
				int TriggerPartsCount = TriggerParts.Count();

				Logger.Log(DebugLogLevel, $"[SyncOutfitTriggerInfo][{ChaControl.chaFile.parameter?.fullname}][{CoordinateIndex}] Count {TriggerPartsCount} : {PartsInfoCount}");

				if (TriggerPartsCount > PartsInfoCount)
				{
					CharaTriggerInfo[CoordinateIndex].Parts = CharaTriggerInfo[CoordinateIndex].Parts.Take(PartsInfoCount).ToList();
					Logger.LogWarning($"[SyncOutfitTriggerInfo][{ChaControl.chaFile.parameter?.fullname}] AccTriggerInfo for Coordinate {CoordinateIndex} Slot {TriggerPartsCount} -> {PartsInfoCount}");
				}
				else if (TriggerPartsCount < PartsInfoCount)
				{
					for (int j = TriggerPartsCount; j < PartsInfoCount; j++)
						CharaTriggerInfo[CoordinateIndex].Parts.Add(new AccTriggerInfo(j) );
					Logger.Log(DebugLogLevel, $"[SyncOutfitTriggerInfo][{ChaControl.chaFile.parameter?.fullname}] AccTriggerInfo for Coordinate {CoordinateIndex} Slot {TriggerPartsCount} -> {PartsInfoCount - 1}");
				}
				for (int i = 0; i < PartsInfoCount; i++)
				{
					AccTriggerInfo TriggerPart = TriggerParts.ElementAtOrDefault(i);
					if ((TriggerPart.Kind != -1) && (PartsInfo[i].type == 120))
					{
						CharaTriggerInfo[CoordinateIndex].Parts[i] = new AccTriggerInfo(i);
						Logger.LogMessage($"AccTriggerInfo for Coordinate {CoordinateIndex} Slot {i + 1:00} has been reset");
					}
					else
					{
						if (TriggerPart.Kind == -1)
							CharaTriggerInfo[CoordinateIndex].Parts[i] = new AccTriggerInfo(i);
						else if (MathfEx.RangeEqualOn(0, TriggerPart.Kind, 7))
							CharaTriggerInfo[CoordinateIndex].Parts[i].Group = "";
						else if (TriggerPart.Kind == 9)
							CharaTriggerInfo[CoordinateIndex].Parts[i].Group = PartsInfo[i].parentKey;
						else if (TriggerPart.Kind > 9)
							CharaTriggerInfo[CoordinateIndex].Parts[i].Group = "custom_" + (TriggerPart.Kind - 9).ToString();
					}
				}
			}

			internal void FillVirtualGroupStates()
			{
				Dictionary<string, bool> tmpVGS = new Dictionary<string, bool>();
				List<string> Groups = CurOutfitTriggerInfo.Parts.Where(x => x.Kind >= 9).OrderBy(x => x.Kind).OrderBy(x => x.Group).GroupBy(x => x.Group).Select(x => x.First().Group).ToList();
				foreach (string Group in Groups)
				{
					if (VirtualGroupStates.ContainsKey(Group))
						tmpVGS[Group] = VirtualGroupStates[Group];
					else
						tmpVGS[Group] = true;
				}
				VirtualGroupStates = tmpVGS.ToDictionary(entry => entry.Key, entry => entry.Value);
			}

			internal void FillVirtualGroupNames()
			{
				int max = CurOutfitTriggerInfo.Parts.Max(x => x.Kind);
				max = max > (9 + DefaultCustomGroupCount) ? (max - 9) : DefaultCustomGroupCount;
				int i = CurOutfitVirtualGroupNames.Count();
				if (i < max)
				{
					Logger.Log(DebugLogLevel, $"[FillVirtualGroupNames][{ChaControl.chaFile.parameter?.fullname}] Filled with {max - i} entries");

					for (; i < max; i++)
						CurOutfitVirtualGroupNames[$"custom_{i + 1}"] = $"Custom {i + 1}";
				}
			}

			internal void ToggleByClothesState(ChaControl chaCtrl, int kind, int state)
			{
				if (TriggerEnabled)
				{
					if (!MathfEx.RangeEqualOn(0, kind, 7))
						return;

					List<AccTriggerInfo> Parts = CurOutfitTriggerInfo.Parts.Where(x => x.Kind == kind).ToList();
					foreach (AccTriggerInfo Part in Parts)
						chaCtrl.SetAccessoryState(Part.Slot, Part.State[state]);
				}
			}

			private IEnumerator CoroutineSyncAllAccToggle(float InitCounter)
			{
				CoroutineCounter = InitCounter;
				for (int i = 0; i < CoroutineCounter; ++i)
					yield return null;
				SyncAllAccToggle();
			}

			internal void SyncAllAccToggle()
			{
				Logger.Log(DebugLogLevel, $"[SyncAllAccToggle][{ChaControl.chaFile.parameter?.fullname}] Fired!!");

				if ((MakerAPI.InsideMaker) && (!CharaMakerPreview.Value))
				{
					Logger.Log(DebugLogLevel, $"[SyncAllAccToggle][{ChaControl.chaFile.parameter?.fullname}] Disabled by config");
					return;
				}
				if (!TriggerEnabled)
				{
					Logger.Log(DebugLogLevel, $"[SyncAllAccToggle][{ChaControl.chaFile.parameter?.fullname}] TriggerEnabled false");
					return;
				}

				foreach (AccTriggerInfo Part in CurOutfitTriggerInfo.Parts)
				{
					if (MathfEx.RangeEqualOn(0, Part.Kind, 7))
					{
						int state = ChaControl.fileStatus.clothesState[Part.Kind];
						bool vis = Part.State[state];
						Logger.Log(DebugLogLevel, $"[SyncAllAccToggle][{ChaControl.chaFile.parameter?.fullname}][slot: {Part.Slot}][kind: {Part.Kind}][state: {state}][vis: {vis}]");
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
				{
					Logger.Log(DebugLogLevel, $"[ToggleByVirtualGroup][{ChaControl.chaFile.parameter?.fullname}] TriggerEnabled false");
					return;
				}
				Logger.Log(DebugLogLevel, $"[ToggleByVirtualGroup][{ChaControl.chaFile.parameter?.fullname}][group: {group}][show: {show}]");

				VirtualGroupStates[group] = show;

				foreach (AccTriggerInfo Part in CurOutfitTriggerInfo.Parts)
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
				if (InsideCharaStudio)
					MoreAccessories_Support.UpdateUI();
			}
		}
	}
}
