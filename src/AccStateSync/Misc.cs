using System.Collections.Generic;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		internal static class Constants
		{
			internal static int ExtDataVersion = 5;

			internal static Dictionary<string, string> AccessoryParentNames = new Dictionary<string, string>();
			internal static Dictionary<int, UI.DropdownASSList> DropdownASSList = new Dictionary<int, UI.DropdownASSList>();

			internal static void ParseIntoStruct()
			{
				foreach (var key in System.Enum.GetValues(typeof(ChaAccessoryDefine.AccessoryParentKey)))
					AccessoryParentNames[key.ToString()] = ChaAccessoryDefine.dictAccessoryParent[(int) key];

				DropdownASSList[0] = new UI.DropdownASSList(0, -1, "無", new List<bool>() { false, false, false, false }); // None
				DropdownASSList[1] = new UI.DropdownASSList(1, 0, "トップス", new List<bool>() { true, true, false, true }); // Top
				DropdownASSList[2] = new UI.DropdownASSList(2, 1, "ボトムス", new List<bool>() { true, true, false, true }); // Bottom
				DropdownASSList[3] = new UI.DropdownASSList(3, 2, "ブラ", new List<bool>() { true, true, false, true }); // Bra
				DropdownASSList[4] = new UI.DropdownASSList(4, 3, "ショーツ", new List<bool>() { true, true, true, true }); // Underwear
				DropdownASSList[5] = new UI.DropdownASSList(5, 4, "手袋", new List<bool>() { true, false, false, true }); // Gloves
				DropdownASSList[6] = new UI.DropdownASSList(6, 5, "パンスト", new List<bool>() { true, true, false, true }); // Pantyhose
				DropdownASSList[7] = new UI.DropdownASSList(7, 6, "靴下", new List<bool>() { true, false, false, true }); // Legwear
				DropdownASSList[8] = new UI.DropdownASSList(8, 7, "内履き", new List<bool>() { true, false, false, true }); // Inner Shoes
				DropdownASSList[9] = new UI.DropdownASSList(9, 8, "外履き", new List<bool>() { true, false, false, true }); // Outer Shoes
				DropdownASSList[10] = new UI.DropdownASSList(10, 9, "親", new List<bool>() { true, false, false, true }); // Parent
			}
		}

		internal static class UI
		{
			internal static int MenuitemHeightOffsetY = 0;
			internal static int AnchorOffsetMinY = 0;
			internal static int ContainerOffsetMinY = 0;

			internal static List<string> clothesStateText = new List<string>() { "着衣", "半脱１", "半脱２", "脱衣" };

			internal class DropdownASSList
			{
				public int Index { get; set; }
				public int Kind { get; set; }
				public string Label { get; set; }
				public List<bool> States { get; set; } = new List<bool>() { false, false, false, false };

				public DropdownASSList(int index, int kind, string label, List<bool> states)
				{
					Index = index;
					Kind = kind;
					Label = label;
					States = states;
				}
			}
		}
	}
}
