using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace AccStateSync
{
	class MoreAccessories_Support
	{
		internal static readonly BepInEx.Logging.ManualLogSource Logger = AccStateSync.Logger;
		internal static Type MoreAccessories = null;
		internal static object MoreAccObj;
		internal static bool NewVer = false;

		internal static bool LoadAssembly()
		{
			try
			{
				MoreAccessories = Type.GetType("MoreAccessoriesKOI.MoreAccessories, MoreAccessories");
				if (MoreAccessories == null)
				{
					throw new Exception("Load assembly FAILED: MoreAccessories");
				}
				MoreAccObj = Traverse.Create(MoreAccessories).Field("_self").GetValue();

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

		internal static List<ChaFileAccessory.PartsInfo> GetCoordinatePartsInfo(ChaControl chaCtrl, int CoordinateIndex)
		{
			List<ChaFileAccessory.PartsInfo> parts = new List<ChaFileAccessory.PartsInfo>();
			parts.AddRange(chaCtrl.chaFile.coordinate[CoordinateIndex].accessory.parts.ToList());
			parts.AddRange(GetMoreAccessoriesInfos(chaCtrl, CoordinateIndex));
			return parts;
		}

		internal static List<ChaFileAccessory.PartsInfo> GetMoreAccessoriesInfos(ChaControl chaCtrl, int CoordinateIndex)
		{
			object accessoriesByChar = Traverse.Create(MoreAccObj).Field("_accessoriesByChar").GetValue();
			MethodInfo tryMethod = AccessTools.Method(accessoriesByChar.GetType(), "TryGetValue");
			object[] parameters = new object[] { chaCtrl.chaFile, null };
			tryMethod.Invoke(accessoriesByChar, parameters);
			if (NewVer)
			{
				Dictionary<int, List<ChaFileAccessory.PartsInfo>> rawAccessoriesInfos = Traverse.Create(parameters[1]).Field("rawAccessoriesInfos").GetValue<Dictionary<int, List<ChaFileAccessory.PartsInfo>>>();
				rawAccessoriesInfos.TryGetValue(CoordinateIndex, out List<ChaFileAccessory.PartsInfo> parts);
				return parts ?? new List<ChaFileAccessory.PartsInfo>();
			}
			else
			{
				Dictionary<ChaFileDefine.CoordinateType, List<ChaFileAccessory.PartsInfo>> rawAccessoriesInfos = Traverse.Create(parameters[1]).Field("rawAccessoriesInfos").GetValue<Dictionary<ChaFileDefine.CoordinateType, List<ChaFileAccessory.PartsInfo>>>();
				rawAccessoriesInfos.TryGetValue((ChaFileDefine.CoordinateType) CoordinateIndex, out List<ChaFileAccessory.PartsInfo> parts);
				return parts ?? new List<ChaFileAccessory.PartsInfo>();
			}
		}

		internal static void UpdateUI()
		{
			AccessTools.Method(MoreAccessories, "UpdateUI").Invoke(MoreAccObj, null);
		}
	}
}
