using Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AccStateSync
{
	class MoreAccessories_Support
	{
		private static readonly BepInEx.Logging.ManualLogSource Logger = AccStateSync.Logger;
		public static Type MoreAccessories = null;
		public static object MoreAccObj;

		public static bool LoadAssembly()
		{
			try
			{
				string path = Extension.Extension.TryGetPluginInstance("com.joan6694.illusionplugins.moreaccessories")?.Info.Location;
				Assembly ass = Assembly.LoadFrom(path);
				MoreAccessories = ass.GetType("MoreAccessoriesKOI.MoreAccessories");
				if (null == MoreAccessories)
				{
					throw new Exception("Load assembly FAILED: MoreAccessories");
				}
				MoreAccObj = MoreAccessories.GetField("_self", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.Instance)?.GetValue(null);
				return true;
			}
			catch (Exception ex)
			{
				Logger.LogDebug(ex.Message);
				return false;
			}
		}

		public static List<ChaFileAccessory.PartsInfo> GetAccessoriesInfos(ChaControl chaCtrl)
		{
			List<ChaFileAccessory.PartsInfo> PartsInfo = new List<ChaFileAccessory.PartsInfo>() {};
			PartsInfo.AddRange( chaCtrl.nowCoordinate.accessory.parts.ToList<ChaFileAccessory.PartsInfo>() );
			PartsInfo.AddRange(GetMoreAccessoriesInfos(chaCtrl, chaCtrl.fileStatus.coordinateType));
			return PartsInfo;
		}

		public static Dictionary<ChaFileDefine.CoordinateType, List<ChaFileAccessory.PartsInfo>> GetRawAccessoriesInfos(ChaControl chaCtrl)
		{
			Dictionary<ChaFile, object> _accessoriesByChar = MoreAccObj.GetField("_accessoriesByChar").ToDictionary<ChaFile, object>();
			_accessoriesByChar.TryGetValue(chaCtrl.chaFile, out object charAdditionalData);
			Dictionary<ChaFileDefine.CoordinateType, List<ChaFileAccessory.PartsInfo>> rawAccInfos = charAdditionalData.GetField("rawAccessoriesInfos").ToDictionary<ChaFileDefine.CoordinateType, List<ChaFileAccessory.PartsInfo>>();

			return rawAccInfos;
		}

		public static List<ChaFileAccessory.PartsInfo> GetCoordinatePartsInfo(ChaControl chaCtrl, int CoordinateIndex)
		{
			List<ChaFileAccessory.PartsInfo> PartsInfo = new List<ChaFileAccessory.PartsInfo>() {};
			PartsInfo.AddRange(chaCtrl.chaFile.coordinate[CoordinateIndex].accessory.parts.ToList<ChaFileAccessory.PartsInfo>());
			PartsInfo.AddRange(GetMoreAccessoriesInfos(chaCtrl, CoordinateIndex));
			return PartsInfo;
		}

		public static List<ChaFileAccessory.PartsInfo> GetMoreAccessoriesInfos(ChaControl chaCtrl, int CoordinateIndex)
		{
			Dictionary<ChaFile, object> _accessoriesByChar = MoreAccObj.GetField("_accessoriesByChar").ToDictionary<ChaFile, object>();
			_accessoriesByChar.TryGetValue(chaCtrl.chaFile, out object charAdditionalData);
			charAdditionalData.GetField("rawAccessoriesInfos").ToDictionary<ChaFileDefine.CoordinateType, List<ChaFileAccessory.PartsInfo>>()
				.TryGetValue((ChaFileDefine.CoordinateType)CoordinateIndex, out List<ChaFileAccessory.PartsInfo> parts);

			return parts ?? new List<ChaFileAccessory.PartsInfo>(){};
		}

		public static ChaAccessoryComponent GetChaAccessoryComponent(ChaControl chaCtrl, int index)
		{
			return (ChaAccessoryComponent)MoreAccessories.InvokeMember("GetChaAccessoryComponent", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance, null, MoreAccObj, new object[] { chaCtrl, index });
		}

		public static int GetAccessoriesAmount(ChaFile chaFile)
		{
			MoreAccObj.GetField("_accessoriesByChar").ToDictionary<ChaFile, object>().TryGetValue(chaFile, out object charAdditionalData);
			return charAdditionalData?.GetField("nowAccessories").ToList<ChaFileAccessory.PartsInfo>().Count + 20 ?? 20;
		}

		public static void UpdateUI()
		{
			MoreAccessories.InvokeMember("UpdateUI", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance, null, MoreAccObj, null);
		}
	}
}
