using System;
using System.Collections.Generic;
using System.Linq;

using BepInEx;
using HarmonyLib;

using JetPack;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		class MoreAccessories
		{
			internal static BaseUnityPlugin _instance;
			internal static bool _installed = false;

			internal static Type _type = null;

			internal static void Init()
			{
				if (!JetPack.MoreAccessories.Installed) return;

				_instance = JetPack.MoreAccessories.Instance;
				_installed = true;
				_type = _instance.GetType();
			}

			internal static void HarmonyPatch()
			{
				_hooksInstance["MoreAccessories"] = Harmony.CreateAndPatchAll(typeof(Hooks));

				if (_installed)
				{
					if (JetPack.MoreAccessories.BuggyBootleg)
					{
						_hooksInstance["MoreAccessories"].Patch(_type.Assembly.GetType("MoreAccessoriesKOI.Patches.MainGame.ChaControl_Patches+SetAccessoryStateAll_Patch").GetMethod("Prefix", AccessTools.all, null, new[] { typeof(ChaControl), typeof(bool) }, null), prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.CharaMakerForcePreview)));
						_hooksInstance["MoreAccessories"].Patch(_type.Assembly.GetType("MoreAccessoriesKOI.Patches.MainGame.ChaControl_Patches+SetAccessoryStateCategoryPatch").GetMethod("Prefix", AccessTools.all, null, new[] { typeof(ChaControl), typeof(int), typeof(bool) }, null), prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.CharaMakerForcePreview)));
					}
					else
					{
						_hooksInstance["MoreAccessories"].Patch(_type.Assembly.GetType("MoreAccessoriesKOI.ChaControl_SetAccessoryStateAll_Patches").GetMethod("Postfix", AccessTools.all, null, new[] { typeof(ChaControl), typeof(bool) }, null), prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.CharaMakerForcePreview)));
						_hooksInstance["MoreAccessories"].Patch(_type.Assembly.GetType("MoreAccessoriesKOI.ChaControl_SetAccessoryStateCategory_Patches").GetMethod("Postfix", AccessTools.all, null, new[] { typeof(ChaControl), typeof(int), typeof(bool) }, null), prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.CharaMakerForcePreview)));
					}
				}

				_hooksInstance["MoreAccessories"].Patch(typeof(ChaControl).GetMethod("SetAccessoryStateAll", AccessTools.all, null, new[] { typeof(bool) }, null), prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.ChaControl_SetAccessoryStateAll_Prefix)));
				_hooksInstance["MoreAccessories"].Patch(typeof(ChaControl).GetMethod("SetAccessoryStateCategory", AccessTools.all, null, new[] { typeof(int), typeof(bool) }, null), prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.ChaControl_SetAccessoryStateCategory_Prefix)));
			}

			internal static void HarmonyUnpatch()
			{
				if (_hooksInstance["MoreAccessories"] == null) return;

				_hooksInstance["MoreAccessories"].UnpatchAll(_hooksInstance["MoreAccessories"].Id);
				_hooksInstance["MoreAccessories"] = null;
			}

			internal static void UpdateUI()
			{
				if (_installed)
					AccessTools.Method(_type, "UpdateUI").Invoke(_instance, null);
			}

			internal static class Hooks
			{
				internal static bool ChaControl_SetAccessoryStateAll_Prefix(ChaControl __instance, bool show)
				{
					if (JetPack.CharaMaker.Inside && _cfgCharaMakerPreview.Value)
					{
						SetAccessoryStateAll(__instance, show);
						return false;
					}
					return true;
				}

				internal static bool ChaControl_SetAccessoryStateCategory_Prefix(ChaControl __instance, int cateNo, bool show)
				{
					if (JetPack.CharaMaker.Inside && _cfgCharaMakerPreview.Value)
					{
						SetAccessoryStateCategory(__instance, cateNo, show);
						return false;
					}
					return true;
				}

				internal static void SetAccessoryStateAll(ChaControl _chaCtrl, bool _show)
				{
					int _currentCoordinateIndex = _chaCtrl.fileStatus.coordinateType;
					List<ChaFileAccessory.PartsInfo> _partsInfo = _chaCtrl.ListPartsInfo(_currentCoordinateIndex);

					AccStateSyncController _pluginCtrl = GetController(_chaCtrl);
					if (_pluginCtrl == null)
					{
						for (int _slotIndex = 0; _slotIndex < _partsInfo.Count; _slotIndex++)
							_chaCtrl.SetAccessoryState(_slotIndex, _show);
						return;
					}

					HashSet<int>  _triggers = new HashSet<int>(_pluginCtrl.TriggerPropertyList.Where(x => x.Coordinate == _currentCoordinateIndex).Select(x => x.Slot));
					for (int _slotIndex = 0; _slotIndex < _partsInfo.Count; _slotIndex++)
					{
						if (_triggers.Contains(_slotIndex)) continue;
						_chaCtrl.SetAccessoryState(_slotIndex, _show);
					}
				}

				internal static void SetAccessoryStateCategory(ChaControl _chaCtrl, int _cateNo, bool _show)
				{
					int _currentCoordinateIndex = _chaCtrl.fileStatus.coordinateType;
					List<ChaFileAccessory.PartsInfo> _partsInfo = _chaCtrl.ListPartsInfo(_currentCoordinateIndex);

					AccStateSyncController _pluginCtrl = GetController(_chaCtrl);
					if (_pluginCtrl == null)
					{
						for (int _slotIndex = 0; _slotIndex < _partsInfo.Count; _slotIndex++)
						{
							if (_partsInfo.ElementAtOrDefault(_slotIndex) != null && _partsInfo[_slotIndex].hideCategory == _cateNo)
								_chaCtrl.SetAccessoryState(_slotIndex, _show);
						}
						return;
					}

					HashSet<int> _triggers = new HashSet<int>(_pluginCtrl.TriggerPropertyList.Where(x => x.Coordinate == _currentCoordinateIndex).Select(x => x.Slot));
					for (int _slotIndex = 0; _slotIndex < _partsInfo.Count; _slotIndex++)
					{
						if (_triggers.Contains(_slotIndex)) continue;
						if (_partsInfo.ElementAtOrDefault(_slotIndex) != null && _partsInfo[_slotIndex].hideCategory == _cateNo)
							_chaCtrl.SetAccessoryState(_slotIndex, _show);
					}
				}

				internal static bool CharaMakerForcePreview()
				{
					return !(JetPack.CharaMaker.Inside && _cfgCharaMakerPreview.Value);
				}
			}
		}
	}
}
