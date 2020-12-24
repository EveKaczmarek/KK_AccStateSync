using System.Collections.Generic;
using System.Linq;
using UniRx;

using KKAPI.Chara;
using JetPack;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		public partial class AccStateSyncController : CharaCustomFunctionController
		{
			public List<AccTriggerInfo> GetPartsOfKind(int kind, string group = "")
			{
				if (!CharaTriggerInfo.ContainsKey(CurrentCoordinateIndex))
					return new List<AccTriggerInfo>();
				List<AccTriggerInfo> filtered = CharaTriggerInfo[CurrentCoordinateIndex].Parts.Values.Where(x => x.Kind == kind).ToList();
				if (!group.IsNullOrEmpty())
					filtered = filtered.Where(x => x.Group == group).ToList();
				return filtered;
			}

			public List<AccTriggerInfo> GetPartsOfGroup(string group)
			{
				if (group.IsNullOrEmpty())
					return new List<AccTriggerInfo>();
				if (!CharaTriggerInfo.ContainsKey(CurrentCoordinateIndex))
					return new List<AccTriggerInfo>();
				return CharaTriggerInfo[CurrentCoordinateIndex].Parts.Values.Where(x => x.Group == group).ToList();
			}

			internal void SetAccessoryStateAll(bool show)
			{
				int Count = ChaControl.ListPartsInfo(CurrentCoordinateIndex).Count;
				for (int i = 0; i < Count; i++)
					ShowAccessory(i, show);
			}

			internal void SetAccessoryStateCategory(int cateNo, bool show)
			{
				List<ChaFileAccessory.PartsInfo> PartsInfo = ChaControl.ListPartsInfo(CurrentCoordinateIndex);
				for (int i = 0; i < PartsInfo.Count; i++)
				{
					if (PartsInfo[i].hideCategory == cateNo)
						ShowAccessory(i, show);
				}
			}

			internal void ShowAccessory(int slot, bool show)
			{
				int Count = ChaControl.ListPartsInfo(CurrentCoordinateIndex).Count;
				if (slot < Count)
				{
					if (ChaControl.GetAccessoryVisibility(slot) != show)
						ChaControl.SetAccessoryState(slot, show);
				}
#if DEBUG
				else
					Logger.LogError($"[ShowAccessory][{CharaFullName}][slot: {slot}] out of range of {Count}");
#endif
			}
		}
	}
}
