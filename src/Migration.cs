using MessagePack;
using System;
using System.Linq;
using System.Collections.Generic;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		[Serializable]
		[MessagePackObject]
		public class OutfitTriggerInfoV1
		{
			[Key("Index")]
			public int Index { get; set; }
			[Key("Parts")]
			public List<AccTriggerInfo> Parts { get; set; } = new List<AccTriggerInfo>();

			public OutfitTriggerInfoV1(int index) { Index = index; }
		}

		internal static OutfitTriggerInfo UpgradeOutfitTriggerInfoV1(OutfitTriggerInfoV1 OldOutfitTriggerInfo)
		{
			Logger.Log(DebugLogLevel, $"[UpgradeOutfitTriggerInfoV1] Fired!!");
			OutfitTriggerInfo OutfitTriggerInfo = new OutfitTriggerInfo(OldOutfitTriggerInfo.Index);
			if (OldOutfitTriggerInfo.Parts.Count() > 0)
			{
				for (int j = 0; j < OldOutfitTriggerInfo.Parts.Count(); j++)
				{
					AccTriggerInfo TriggerPart = OldOutfitTriggerInfo.Parts[j];
					if (TriggerPart.Kind > -1)
					{
						OutfitTriggerInfo.Parts[j] = new AccTriggerInfo(j);
						CopySlotTriggerInfo(TriggerPart, OutfitTriggerInfo.Parts[j]);
					}
				}
			}
			return OutfitTriggerInfo;
		}

		internal static Dictionary<string, string> UpgradeVirtualGroupNamesV1(Dictionary<string, string> OldVirtualGroupNames)
		{
			Logger.Log(DebugLogLevel, $"[UpgradeVirtualGroupNamesV1] Fired!!");
			Dictionary<string, string> VirtualGroupNames = new Dictionary<string, string>();
			if (OldVirtualGroupNames?.Count() > 0)
			{
				foreach (KeyValuePair<string, string> VirtualGroupName in OldVirtualGroupNames)
					VirtualGroupNames[VirtualGroupName.Key] = VirtualGroupName.Value;
			}
			return VirtualGroupNames;
		}

		internal static Dictionary<string, VirtualGroupInfo> UpgradeVirtualGroupNamesV2(Dictionary<string, string> OldVirtualGroupNames)
		{
			Logger.Log(DebugLogLevel, $"[UpgradeVirtualGroupNamesV2] Fired!!");
			Dictionary<string, VirtualGroupInfo> OutfitVirtualGroupInfo = new Dictionary<string, VirtualGroupInfo>();
			if (OldVirtualGroupNames?.Count() > 0)
			{
				foreach (KeyValuePair<string, string> VirtualGroupName in OldVirtualGroupNames)
				{
					if (VirtualGroupName.Key.StartsWith("custom_"))
					{
						string Group = VirtualGroupName.Key;
						int Kind = System.Int32.Parse(Group.Replace("custom_", "")) + 9;
						string Label = VirtualGroupName.Value;

						OutfitVirtualGroupInfo[VirtualGroupName.Key] = new VirtualGroupInfo(Group, Kind, Label);
					}
				}
			}
			return OutfitVirtualGroupInfo;
		}
	}
}
