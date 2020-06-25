using System.Collections.Generic;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		internal static List<string> clothesKindText = new List<string>() {"トップス", "ボトムス", "ブラ", "ショーツ", "手袋", "パンスト", "靴下", "靴"};
		internal static List<string> clothesStateText = new List<string>() {"On", "Shift", "Hang", "Off"};

		internal static List<List<bool>> clothesStates = new List<List<bool>>()
		{
			new List<bool>() { false, false, false, false }, // None
			new List<bool>() { true, true, false, true }, // Top
			new List<bool>() { true, true, false, true }, // Bottom
			new List<bool>() { true, true, false, true }, // Bra
			new List<bool>() { true, true, true, true }, // Underwear
			new List<bool>() { true, false, false, true }, // Gloves
			new List<bool>() { true, true, false, true }, // Pantyhose
			new List<bool>() { true, false, false, true }, // Legwear
			new List<bool>() { true, false, false, true }, // Shoes
			new List<bool>() { true, false, false, true }, // Parent
		};

		internal static List<string> ddASSListLabels = new List<string>() {"無", "トップス", "ボトムス", "ブラ", "ショーツ", "手袋", "パンスト", "靴下", "靴", "親"};
		internal static List<int> ddASSListVals = new List<int>() {-1, 0, 1, 2, 3, 4, 5, 6, 7, 9};
	}
}
