using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEngine;
using TMPro;
using ChaCustom;

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
		private static Harmony _hooksInstance = null;

		internal static void Init()
		{
			Instance = Toolbox.GetPluginInstance("com.joan6694.illusionplugins.moreaccessories");
			NewVer = Toolbox.PluginVersionCompare(Instance, "1.1.0");
			Core.DebugLog($"MoreAccessories {Instance.Info.Metadata.Version} found, NewVer: {NewVer}");
			_type = Instance.GetType();
			_accessoriesByChar = Traverse.Create(Instance).Field("_accessoriesByChar").GetValue();
		}

		internal static void OnMakerBaseLoaded()
		{
			_hooksInstance = Harmony.CreateAndPatchAll(typeof(Hooks));
		}

		internal static void OnMakerFinishedLoading()
		{
			_hooksInstance.Patch(typeof(CvsAccessory).GetMethod("UpdateCustomUI", AccessTools.all), prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.CvsAccessory_UpdateCustomUI_Prefix)));
			_hooksInstance.Patch(GetCvsPatchType("UpdateCustomUI").GetMethod("Prefix", AccessTools.all), prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.ReturnFalse)));
		}

		internal static void OnMakerExiting()
		{
			_hooksInstance.UnpatchAll(_hooksInstance.Id);
			_hooksInstance = null;
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
			List<ChaFileAccessory.PartsInfo> _parts = new List<ChaFileAccessory.PartsInfo>();

			object _charAdditionalData = GetCharAdditionalData(_chaCtrl);
			if (_charAdditionalData == null) return _parts;
			object _rawAccessoriesInfos = Traverse.Create(_charAdditionalData).Field("rawAccessoriesInfos").GetValue();
			if (_rawAccessoriesInfos == null) return _parts;
			if (NewVer)
				(_rawAccessoriesInfos as Dictionary<int, List<ChaFileAccessory.PartsInfo>>).TryGetValue(_coordinateIndex, out _parts);
			else
				(_rawAccessoriesInfos as Dictionary<ChaFileDefine.CoordinateType, List<ChaFileAccessory.PartsInfo>>).TryGetValue((ChaFileDefine.CoordinateType) _coordinateIndex, out _parts);
			return _parts ?? new List<ChaFileAccessory.PartsInfo>();
		}

		public static List<GameObject> ListMoreObjAccessory(ChaControl _chaCtrl)
		{
			List<GameObject> _parts = new List<GameObject>();
			object _charAdditionalData = GetCharAdditionalData(_chaCtrl);
			if (_charAdditionalData == null) return _parts;
			_parts = Traverse.Create(_charAdditionalData).Field("objAccessory").GetValue<List<GameObject>>();
			return _parts ?? new List<GameObject>();
		}

		public static object GetCharAdditionalData(ChaControl _chaCtrl)
		{
			return _accessoriesByChar.RefTryGetValue(_chaCtrl.chaFile);
		}

		internal static class Hooks
		{
			internal static bool ReturnFalse() => false;

			internal static bool CvsAccessory_UpdateCustomUI_Prefix(CvsAccessory __instance)
			{
				int _slotIndex = (int) __instance.slotNo;
				if (_slotIndex < 0)
					return false;

				ChaFileAccessory.PartsInfo _part = CustomBase.Instance.chaCtrl.GetPartsInfo(_slotIndex);

				__instance.CalculateUI();
				//__instance.Field<CvsDrawCtrl>("cmpDrawCtrl").UpdateAccessoryDraw();
				int _value = 0;
				if (_part != null)
					_value = _part.type - 120;
				Traverse _traverse = Traverse.Create(__instance);
				_traverse.Field("ddAcsType").GetValue<TMP_Dropdown>().value = _value;

				__instance.UpdateAccessoryKindInfo();
				__instance.UpdateAccessoryParentInfo();
				__instance.UpdateAccessoryMoveInfo();
				__instance.ChangeSettingVisible(_value != 0);

				_traverse.Field("separateColor").GetValue<GameObject>().SetActiveIfDifferent(false);
				_traverse.Field("separateCorrect").GetValue<GameObject>().SetActiveIfDifferent(false);
				Transform _parent = CharaMaker.CvsScrollable ? __instance.transform.GetChild(0).GetChild(0).GetChild(0) : __instance.transform;
				_parent.Find("objController01/Controller/imgSeparete").gameObject.SetActiveIfDifferent(_traverse.Field("objControllerTop02").GetValue<GameObject>().activeSelf);

				return false;
			}

		}
	}
}
