using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;
using ChaCustom;

using HarmonyLib;

using MoreAccessoriesKOI;

namespace JetPack
{
	public partial class Maker
	{
		public static event EventHandler<MakerSlotAddedEventArgs> OnCharaMakerSlotAdded;
		public class MakerSlotAddedEventArgs : EventArgs
		{
			public MakerSlotAddedEventArgs(int index, Transform transform)
			{
				SlotIndex = index;
				SlotTemplate = transform;
			}

			public int SlotIndex { get; }
			public Transform SlotTemplate { get; }
		}

		public static Toggle CopyToggle(int SlotIndex)
		{
			if (SlotIndex < 20)
				return Instance.CvsAccessoryCopy.Field<Toggle[]>("tglKind")[SlotIndex];

			IList additionalCharaMakerSlots = Accessory._moreAccessoriesInstance.Field<IList>("_additionalCharaMakerSlots");
			if (SlotIndex - 20 >= additionalCharaMakerSlots.Count)
				return null;
			return additionalCharaMakerSlots[SlotIndex - 20].Field<Toggle>("copyToggle");
		}

		public static GameObject GetObjAcsMove(int SlotIndex)
		{
			if (SlotIndex < 20)
				return CustomBase.Instance.chaCtrl.objAcsMove[SlotIndex, 1];
			else
				return Accessory._moreAccessoriesInstance._charaMakerData.objAcsMove?.ElementAtOrDefault(SlotIndex - 20)?.ElementAtOrDefault(1);
		}
	}

	public partial class Accessory
	{
		public static bool GetAccessoryVisibility(ChaControl chaCtrl, int SlotIndex)
		{
			if (SlotIndex < 20)
				return chaCtrl.fileStatus.showAccessory[SlotIndex];
			else
				return (bool) GetCharAdditionalData(chaCtrl).showAccessories?.ElementAtOrDefault(SlotIndex - 20);
		}

		public static int GetAccessoryStateCategory(ChaControl chaCtrl, int SlotIndex)
		{
			if (SlotIndex < 20)
				return chaCtrl.nowCoordinate.accessory.parts[SlotIndex].hideCategory;
			else
				return GetCharAdditionalData(chaCtrl).GetPartsInfo(SlotIndex - 20).hideCategory;
		}

		public static ChaFileAccessory.PartsInfo GetPartsInfo(ChaControl chaCtrl, int SlotIndex)
		{
			if (SlotIndex < 20)
				return chaCtrl.nowCoordinate.accessory.parts[SlotIndex];
			else
				return GetCharAdditionalData(chaCtrl).GetPartsInfo(SlotIndex - 20);
		}

		public static ChaFileAccessory.PartsInfo GetPartsInfo(ChaControl chaCtrl, int CoordinateIndex, int SlotIndex)
		{
			List<ChaFileAccessory.PartsInfo> parts = ListPartsInfo(chaCtrl, CoordinateIndex);
			return parts.ElementAtOrDefault(SlotIndex) ?? new ChaFileAccessory.PartsInfo();
		}

		public static List<ChaFileAccessory.PartsInfo> ListPartsInfo(ChaControl chaCtrl, int CoordinateIndex)
		{
			List<ChaFileAccessory.PartsInfo> parts = new List<ChaFileAccessory.PartsInfo>();
			parts.AddRange(chaCtrl.chaFile.coordinate[CoordinateIndex].accessory.parts.ToList());
			parts.AddRange(GetCharAdditionalData(chaCtrl).ListPartsInfo(CoordinateIndex));
			return parts;
		}

		public static List<ChaAccessoryComponent> ListChaAccessoryComponent(ChaControl chaCtrl)
		{
			List<ChaAccessoryComponent> parts = new List<ChaAccessoryComponent>();
			parts.AddRange(chaCtrl.cusAcsCmp.ToList());
			parts.AddRange(GetCharAdditionalData(chaCtrl).CusAcsCmp);
			return parts;
		}

		public static bool IsHairAccessory(ChaControl chaCtrl, int SlotIndex)
		{
			ChaAccessoryComponent accessory = GetChaAccessoryComponent(chaCtrl, SlotIndex);
			if (accessory == null)
				return false;
			return accessory.gameObject?.GetComponent<ChaCustomHairComponent>() != null;
		}

		public static CvsAccessory GetCvsAccessory(int SlotIndex)
		{
			return Traverse.Create(_moreAccessoriesInstance).Method("GetCvsAccessory", new object[] { SlotIndex } ).GetValue<CvsAccessory>();
		}

		public static ChaAccessoryComponent GetChaAccessoryComponent(ChaControl chaCtrl, int SlotIndex) => _moreAccessoriesInstance.GetChaAccessoryComponent(chaCtrl, SlotIndex);

		public static MoreAccessories MoreAccessoriesInstance => _moreAccessoriesInstance;
		//public static MoreAccessories.CharAdditionalData charaMakerData => _moreAccessoriesInstance._charaMakerData;
		/*
		public static GameObject GetObjAcsMove(int SlotIndex)
		{
			if (SlotIndex < 20)
				return CustomBase.Instance.chaCtrl.objAcsMove[SlotIndex, 1];
			else
				return _moreAccessoriesInstance._charaMakerData.objAcsMove?.ElementAtOrDefault(SlotIndex - 20)?.ElementAtOrDefault(1);
		}
		*/
		internal static bool _legacy = false;
		internal static Type _moreAccessoriesType;
		internal static MoreAccessories _moreAccessoriesInstance;
		internal static Dictionary<ChaFile, MoreAccessories.CharAdditionalData> _accessoriesByChar;

		internal static void Init()
		{
			_moreAccessoriesType = Type.GetType("MoreAccessoriesKOI.MoreAccessories, MoreAccessories");
			_moreAccessoriesInstance = MoreAccessories._self;

			BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue("com.joan6694.illusionplugins.moreaccessories", out BepInEx.PluginInfo _pluginInfo);
			_legacy = _pluginInfo.Metadata.Version.CompareTo(new Version("1.0.10")) < 0;
			_accessoriesByChar = _moreAccessoriesInstance.Field<Dictionary<ChaFile, MoreAccessories.CharAdditionalData>>("_accessoriesByChar");
		}

		public static Type GetCvsPatchType(string name) => _moreAccessoriesType.Assembly.GetType($"MoreAccessoriesKOI.CvsAccessory_Patches+CvsAccessory_{name}_Patches");

		//internal static Toggle GetCopyToggle(int SlotIndex) => (SlotIndex < 20) ? null : _moreAccessoriesType.Field<IList>("_additionalCharaMakerSlots")[SlotIndex - 20].Field<Toggle>("copyToggle");

		public class CharAdditionalData
		{
			internal MoreAccessories.CharAdditionalData _charAdditionalData;
			public CharAdditionalData(ChaControl chaCtrl)
			{
				this.chaCtrl = chaCtrl;
				_accessoriesByChar.TryGetValue(chaCtrl.chaFile, out _charAdditionalData);
			}
			public object rawAccessoriesInfos => _charAdditionalData?.Field<object>("rawAccessoriesInfos");
			public List<bool> showAccessories => _charAdditionalData?.showAccessories;
			public List<ChaFileAccessory.PartsInfo> nowAccessories => _charAdditionalData?.nowAccessories;
			public List<ListInfoBase> infoAccessory => _charAdditionalData?.infoAccessory;
			public List<GameObject> objAccessory => _charAdditionalData?.objAccessory;
			public List<ChaAccessoryComponent> CusAcsCmp => _charAdditionalData?.cusAcsCmp;

			internal ChaControl chaCtrl;
			public List<ChaFileAccessory.PartsInfo> ListPartsInfo() => ListPartsInfo(chaCtrl.fileStatus.coordinateType);
			public List<ChaFileAccessory.PartsInfo> ListPartsInfo(int CoordinateIndex)
			{
				if (rawAccessoriesInfos == null)
					return new List<ChaFileAccessory.PartsInfo>();
				List<ChaFileAccessory.PartsInfo> parts;
				if (_legacy)
					(rawAccessoriesInfos as Dictionary<ChaFileDefine.CoordinateType, List<ChaFileAccessory.PartsInfo>>).TryGetValue((ChaFileDefine.CoordinateType) CoordinateIndex, out parts);
				else
					(rawAccessoriesInfos as Dictionary<int, List<ChaFileAccessory.PartsInfo>>).TryGetValue(CoordinateIndex, out parts);
				return parts ?? new List<ChaFileAccessory.PartsInfo>();
			}
			public ChaFileAccessory.PartsInfo GetPartsInfo(int SlotIndex) => ListPartsInfo().ElementAtOrDefault(SlotIndex) ?? new ChaFileAccessory.PartsInfo();
		}

		public static CharAdditionalData GetCharAdditionalData(ChaControl chaCtrl) => new CharAdditionalData(chaCtrl);
	}
}
