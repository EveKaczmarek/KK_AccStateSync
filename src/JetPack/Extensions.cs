using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using HarmonyLib;

namespace JetPack
{
	public static partial class Extensions
	{
		public static List<ChaFileAccessory.PartsInfo> ListPartsInfo(this ChaControl _self, int _coordinateIndex) => Accessory.ListPartsInfo(_self, _coordinateIndex);
		public static ChaFileAccessory.PartsInfo GetPartsInfo(this ChaControl _self, int _slotIndex) => Accessory.GetPartsInfo(_self, _slotIndex);
		public static ChaFileAccessory.PartsInfo GetPartsInfo(this ChaControl _self, int _coordinateIndex, int _slotIndex) => Accessory.GetPartsInfo(_self, _coordinateIndex, _slotIndex);
		public static bool GetAccessoryVisibility(this ChaControl _self, int _slotIndex) => Accessory.GetAccessoryVisibility(_self, _slotIndex);

		public static bool SetActiveIfDifferent(this GameObject _self, bool _active)
		{
			if (_self.activeSelf == _active)
				return false;

			_self.SetActive(_active);
			return true;
		}

		public static string GetFullName(this ChaControl _self) => _self.chaFile.parameter?.fullname.Trim();

		public static List<bool> GetClothesStates(this ChaControl _self, int _slotIndex) => Chara.Clothes.GetClothesStates(_self, _slotIndex);

		public static object RefTryGetValue(this object _self, object _key)
		{
			if (_self == null) return null;

			MethodInfo _tryMethod = AccessTools.Method(_self.GetType(), "TryGetValue");
			object[] _parameters = new object[] { _key, null };
			_tryMethod.Invoke(_self, _parameters);
			return _parameters[1];
		}

		public static object RefElementAt(this object _self, int _key)
		{
			if (_self == null)
				return null;
			if (_key > (Traverse.Create(_self).Property("Count").GetValue<int>() - 1))
				return null;

			return Traverse.Create(_self).Method("get_Item", new object[] { _key }).GetValue();
		}

		public static T RefElementAt<T>(this object _self, int _key)
		{
			if (_self == null)
				return default(T);
			if (_key > (Traverse.Create(_self).Property("Count").GetValue<int>() - 1))
				return default(T);

			return Traverse.Create(_self).Method("get_Item", new object[] { _key }).GetValue<T>();
		}
	}
}
