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
		public class AccTriggerInfo
		{
			[Key("Slot")]
			public int Slot { get; set; }
			[Key("Kind")]
			public int Kind { get; set; } = -1;
			[Key("Group")]
			public string Group { get; set; } = "";
			[Key("State")]
			public List<bool> State { get; set; } = new List<bool>() { true, false, false, false };

			public AccTriggerInfo(int slot) { Slot = slot; }
		}

		[Serializable]
		[MessagePackObject]
		public class OutfitTriggerInfo
		{
			[Key("Index")]
			public int Index { get; set; }
			[Key("Parts")]
			public Dictionary<int, AccTriggerInfo> Parts { get; set; } = new Dictionary<int, AccTriggerInfo>();

			public OutfitTriggerInfo(int index) { Index = index; }
		}

		public static void CopySlotTriggerInfo(AccTriggerInfo CopySource, AccTriggerInfo CopyDestination)
		{
			CopyDestination.Slot = CopySource.Slot;
			CopyDestination.Kind = CopySource.Kind;
			CopyDestination.Group = CopySource.Group;
			CopyDestination.State = CopySource.State.ToList();
		}
	}
}
