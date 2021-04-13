using System.Collections.Generic;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		internal static class Constants
		{
			internal static int _pluginDataVersion = 5;

			internal static Dictionary<string, string> AccessoryParentNames = new Dictionary<string, string>();

			internal static void ParseIntoStruct()
			{
				foreach (var key in System.Enum.GetValues(typeof(ChaAccessoryDefine.AccessoryParentKey)))
					AccessoryParentNames[key.ToString()] = ChaAccessoryDefine.dictAccessoryParent[(int) key];
			}
		}

		internal static class UI
		{
			internal static int MenuitemHeightOffsetY = 0;
			internal static int AnchorOffsetMinY = 0;
			internal static int ContainerOffsetMinY = 0;
		}
	}
}
