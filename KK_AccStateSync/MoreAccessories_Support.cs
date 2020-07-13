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
		public static bool NewVer = false;

		public static bool LoadAssembly()
		{
			try
			{
//				MoreAccessories = UnityEngine.Object.FindObjectOfType<MoreAccessoriesKOI.MoreAccessories>().GetType();
				MoreAccessories = UnityEngine.GameObject.Find("BepInEx_Manager").GetComponent<MoreAccessoriesKOI.MoreAccessories>().GetType();
				if (null == MoreAccessories)
				{
					throw new Exception("Load assembly FAILED: MoreAccessories");
				}
				MoreAccObj = MoreAccessories.GetField("_self", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.Instance)?.GetValue(null);

				BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue("com.joan6694.illusionplugins.moreaccessories", out BepInEx.PluginInfo target);
				if (Int32.Parse((target.Metadata.Version.ToString().Split('.'))[1]) > 0)
					NewVer = true;

				return true;
			}
			catch (Exception ex)
			{
				Logger.LogDebug(ex.Message);
				return false;
			}
		}

		public static List<ChaFileAccessory.PartsInfo> GetCoordinatePartsInfo(ChaControl chaCtrl, int CoordinateIndex)
		{
			List<ChaFileAccessory.PartsInfo> PartsInfo = new List<ChaFileAccessory.PartsInfo>();
			PartsInfo.AddRange(chaCtrl.chaFile.coordinate[CoordinateIndex].accessory.parts.ToList<ChaFileAccessory.PartsInfo>());
			PartsInfo.AddRange(GetMoreAccessoriesInfos(chaCtrl, CoordinateIndex));
			return PartsInfo;
		}

		public static List<ChaFileAccessory.PartsInfo> GetMoreAccessoriesInfos(ChaControl chaCtrl, int CoordinateIndex)
		{
			if (NewVer)
			{
				object charAdditionalData = TryGetValueFromWeakKeyDict(MoreAccObj.GetField("_accessoriesByChar"), chaCtrl.chaFile);
				charAdditionalData.GetField("rawAccessoriesInfos").ToDictionary<ChaFileDefine.CoordinateType, List<ChaFileAccessory.PartsInfo>>()
					.TryGetValue((ChaFileDefine.CoordinateType) CoordinateIndex, out List<ChaFileAccessory.PartsInfo> parts);
				return parts ?? new List<ChaFileAccessory.PartsInfo>();
			}
			else
			{
				Dictionary<ChaFile, object> _accessoriesByChar = MoreAccObj.GetField("_accessoriesByChar").ToDictionary<ChaFile, object>();
				_accessoriesByChar.TryGetValue(chaCtrl.chaFile, out object charAdditionalData);
				charAdditionalData.GetField("rawAccessoriesInfos").ToDictionary<ChaFileDefine.CoordinateType, List<ChaFileAccessory.PartsInfo>>()
					.TryGetValue((ChaFileDefine.CoordinateType) CoordinateIndex, out List<ChaFileAccessory.PartsInfo> parts);
				return parts ?? new List<ChaFileAccessory.PartsInfo>();
			}
		}

		public static object TryGetValueFromWeakKeyDict(object weakDict, object Key)
		{
			object enumerator = weakDict.Invoke("GetEnumerator");
			if ((bool) weakDict.Invoke("ContainsKey", new object[] { Key }))
			{
				while ((bool) enumerator.Invoke("MoveNext"))
				{
					object current = enumerator.GetProperty("Current");
					if (current?.GetProperty("Key") == Key)
						return current?.GetProperty("Value");
				}
			}
			return null;
		}

		public static void UpdateUI()
		{
			MoreAccessories.InvokeMember("UpdateUI", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance, null, MoreAccObj, null);
		}
	}
}
