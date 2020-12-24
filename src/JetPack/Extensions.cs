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
		private struct MemberKey
		{
			public readonly Type type;
			public readonly string name;
			private readonly int _hashCode;

			public MemberKey(Type inType, string inName)
			{
				type = inType;
				name = inName;
				_hashCode = type.GetHashCode() ^ name.GetHashCode();
			}

			public override int GetHashCode()
			{
				return _hashCode;
			}
		}

		private static readonly Dictionary<MemberKey, FieldInfo> _fieldCache = new Dictionary<MemberKey, FieldInfo>();

		public static T Field<T>(this Traverse self, string name) where T : class
		{
			return self.Field(name).GetValue<T>() ?? null;
		}

		public static T Field<T>(this object self, string name) where T : class
		{
			MemberKey key = new MemberKey(self.GetType(), name);
			if (_fieldCache.TryGetValue(key, out FieldInfo info) == false)
			{
				info = key.type.GetField(key.name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
				_fieldCache.Add(key, info);
			}
			return info.GetValue(self) as T ?? null;
		}

		public static bool SetActiveIfDifferent(this GameObject self, bool active)
		{
			if (self.activeSelf == active)
				return false;

			self.SetActive(active);
			return true;
		}

		public static RectTransform rectTransform(this GameObject self) => self.GetComponent<RectTransform>();
		public static RectTransform rectTransform(this Transform self) => self.GetComponent<RectTransform>();

		public static void SetRect(this Transform self, Vector2 offsetMin, Vector2 offsetMax)
		{
			RectTransform RT = self.GetComponent<RectTransform>();
			RT.offsetMin = offsetMin;
			RT.offsetMax = offsetMax;
		}
		public static void SetRect(this Transform self, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
		{
			RectTransform RT = self.GetComponent<RectTransform>();
			RT.anchorMin = anchorMin;
			RT.anchorMax = anchorMax;
			RT.offsetMin = offsetMin;
			RT.offsetMax = offsetMax;
		}
		public static void SetRect(this Transform self, float anchorLeft, float anchorBottom, float anchorRight, float anchorTop, float offsetLeft, float offsetBottom, float offsetRight, float offsetTop)
		{
			RectTransform RT = self.GetComponent<RectTransform>();
			RT.anchorMin = new Vector2(anchorLeft, anchorBottom);
			RT.anchorMax = new Vector2(anchorRight, anchorTop);
			RT.offsetMin = new Vector2(offsetLeft, offsetBottom);
			RT.offsetMax = new Vector2(offsetRight, offsetTop);
		}

		public static List<ChaFileAccessory.PartsInfo> ListPartsInfo(this ChaControl self, int CoordinateIndex) => Accessory.ListPartsInfo(self, CoordinateIndex);
		public static ChaFileAccessory.PartsInfo GetPartsInfo(this ChaControl self, int SlotIndex) => Accessory.GetPartsInfo(self, SlotIndex);
		public static ChaFileAccessory.PartsInfo GetPartsInfo(this ChaControl self, int CoordinateIndex, int SlotIndex) => Accessory.GetPartsInfo(self, CoordinateIndex, SlotIndex);
		public static ChaAccessoryComponent GetChaAccessoryComponent(this ChaControl self, int SlotIndex) => Accessory.GetChaAccessoryComponent(self, SlotIndex);
		public static bool GetAccessoryVisibility(this ChaControl self, int SlotIndex) => Accessory.GetAccessoryVisibility(self, SlotIndex);
		public static int GetAccessoryStateCategory(this ChaControl self, int SlotIndex) => Accessory.GetAccessoryStateCategory(self, SlotIndex);
		public static string GetFullName(this ChaControl self) => self.chaFile.parameter?.fullname.Trim();
		public static List<bool> GetClothesStates(this ChaControl self, int SlotIndex) => Chara.Clothes.GetClothesStates(self, SlotIndex);
	}
}
