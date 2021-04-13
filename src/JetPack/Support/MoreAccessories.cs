using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using BepInEx;
using HarmonyLib;

namespace JetPack
{
	public partial class MoreAccessories
	{
		public static BaseUnityPlugin Instance = null;
		public static bool NewVer = false;

		private static Type _type = null;
		private static object _accessoriesByChar;

		internal static void Init()
		{
			Instance = Toolbox.GetPluginInstance("com.joan6694.illusionplugins.moreaccessories");
			NewVer = Toolbox.PluginVersionCompare(Instance, "1.1.0");
			Core.DebugLog($"MoreAccessories {Instance.Info.Metadata.Version} found, NewVer: {NewVer}");
			_type = Instance.GetType();
			_accessoriesByChar = Traverse.Create(Instance).Field("_accessoriesByChar").GetValue();
		}

		public static Type GetCvsPatchType(string _methodName) => _type.Assembly.GetType($"MoreAccessoriesKOI.CvsAccessory_Patches+CvsAccessory_{_methodName}_Patches");

		public static void CheckAndPadPartInfo(ChaControl _chaCtrl, int _coordinateIndex, int _slotIndex)
		{
			List<ChaFileAccessory.PartsInfo> _parts = ListMorePartsInfo(_chaCtrl, _coordinateIndex);
			if (_parts == null) return;

			for (int i = _parts.Count; i < _slotIndex + 1; i++)
			{
				if (_parts.ElementAtOrDefault(i) == null)
					_parts.Add(new ChaFileAccessory.PartsInfo());
			}
		}

		public static List<ChaFileAccessory.PartsInfo> ListMorePartsInfo(ChaControl _chaCtrl, int _coordinateIndex)
		{
			List<ChaFileAccessory.PartsInfo> _parts = null; // ?? new List<ChaFileAccessory.PartsInfo>();

			object _charAdditionalData = GetCharAdditionalData(_chaCtrl);
			if (_charAdditionalData == null) return _parts;
			object _rawAccessoriesInfos = Traverse.Create(_charAdditionalData).Field("rawAccessoriesInfos").GetValue();
			if (_rawAccessoriesInfos == null) return _parts;
			if (NewVer)
				(_rawAccessoriesInfos as Dictionary<int, List<ChaFileAccessory.PartsInfo>>).TryGetValue(_coordinateIndex, out _parts);
			else
				(_rawAccessoriesInfos as Dictionary<ChaFileDefine.CoordinateType, List<ChaFileAccessory.PartsInfo>>).TryGetValue((ChaFileDefine.CoordinateType) _coordinateIndex, out _parts);
			return _parts;
		}
		/*
		public static MoreAccessoriesKOI.MoreAccessories.CharAdditionalData GetCharAdditionalData(ChaControl _chaCtrl)
		{
			return _accessoriesByChar.RefTryGetValue(_chaCtrl.chaFile) as MoreAccessoriesKOI.MoreAccessories.CharAdditionalData;
		}
		*/
		public static object GetCharAdditionalData(ChaControl _chaCtrl)
		{
			return _accessoriesByChar.RefTryGetValue(_chaCtrl.chaFile);
		}
	}
}
