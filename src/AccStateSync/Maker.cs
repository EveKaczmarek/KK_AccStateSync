using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;
using UniRx;
using ChaCustom;

using HarmonyLib;

using KKAPI.Maker;
using KKAPI.Maker.UI;
using KKAPI.Maker.UI.Sidebar;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		internal static SidebarToggle _sidebarTogglePreview;
		internal static SidebarToggle _sidebarToggleEnable;
		internal static MakerLoadToggle _makerLoadToggle;
		internal static MakerCoordinateLoadToggle _makerCoordinateLoadToggle;
		internal static MakerButton _accWinCtrlEnable;
		internal static bool _loadCharaExtdata => _makerLoadToggle == null || _makerLoadToggle.Value;
		internal static bool _loadCoordinateExtdata => _makerCoordinateLoadToggle == null || _makerCoordinateLoadToggle.Value;

		internal static class CharaMaker
		{
			internal static ChaControl _chaCtrl => CustomBase.Instance?.chaCtrl;
			internal static AccStateSyncController _pluginCtrl => GetController(CustomBase.Instance?.chaCtrl);
			internal static Toggle _imgTglCol01, _imgTglCol02;
			internal static Transform _accMenuTree;
			internal static int _currentSlotIndex => GetCurrentAccSlot();

			internal static void RegisterControls()
			{
				_cfgCharaMakerPreview.SettingChanged += (sender, args) =>
				{
					if (JetPack.CharaMaker.Loaded)
					{
						if (_sidebarTogglePreview.Value != _cfgCharaMakerPreview.Value)
							_sidebarTogglePreview.Value = _cfgCharaMakerPreview.Value;
						if (_sidebarTogglePreview.Value)
							_pluginCtrl.SyncAllAccToggle();
						else
						{
							_pluginCtrl.SetAccessoryStateCategory(0, _imgTglCol01.isOn);
							_pluginCtrl.SetAccessoryStateCategory(1, _imgTglCol02.isOn);
						}
					}
				};

				AccessoriesApi.AccessoryTransferred += (object _sender, AccessoryTransferEventArgs _args) => _pluginCtrl.AccessoryTransferredHandler(_args.SourceSlotIndex, _args.DestinationSlotIndex);
				AccessoriesApi.AccessoriesCopied += (object _sender, AccessoryCopyEventArgs _args) => _pluginCtrl.AccessoriesCopiedHandler((int) _args.CopySource, (int) _args.CopyDestination, _args.CopiedSlotIndexes.ToList());

				MakerAPI.MakerBaseLoaded += (_sender, _args) =>
				{
					MoreAccessories.HarmonyPatch();

					_makerLoadToggle = _args.AddLoadToggle(new MakerLoadToggle("AccStateSync"));
					_makerCoordinateLoadToggle = _args.AddCoordinateLoadToggle(new MakerCoordinateLoadToggle("AccStateSync"));
					PatchMakerToggles();
				};
				MakerAPI.RegisterCustomSubCategories += (_sender, _args) =>
				{
					_makerConfigWindow = _instance.gameObject.AddComponent<AccStateSyncUI>();

					_sidebarToggleEnable = _args.AddSidebarControl(new SidebarToggle("AccStateSync", _cfgMakerWinEnable.Value, _instance));
					_sidebarToggleEnable.ValueChanged.Subscribe(_value =>
					{
						if (_makerConfigWindow.enabled != _value)
							_makerConfigWindow.enabled = _value;
					});
					_sidebarTogglePreview = _args.AddSidebarControl(new SidebarToggle("Force Preview", _cfgCharaMakerPreview.Value, _instance));
					_sidebarTogglePreview.ValueChanged.Subscribe(_value =>
					{
						if (_cfgCharaMakerPreview.Value != _value)
							_cfgCharaMakerPreview.Value = _value;
					});
					_accWinCtrlEnable = MakerAPI.AddAccessoryWindowControl(new MakerButton("AccStateSync", null, _instance));
					_accWinCtrlEnable.OnClick.AddListener(() => _sidebarToggleEnable.SetValue(true));
				};
				MakerAPI.MakerFinishedLoading += (_sender, _args) =>
				{
					_accMenuTree = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMenuTree/04_AccessoryTop/Slots/Viewport/Content").transform;
				};
				MakerAPI.MakerExiting += (_sender, _args) =>
				{
					Destroy(_makerConfigWindow);

					MoreAccessories.HarmonyUnpatch();

					_sidebarTogglePreview = null;
					_sidebarToggleEnable = null;
					_makerLoadToggle = null;
					_makerCoordinateLoadToggle = null;
					_accWinCtrlEnable = null;
					_imgTglCol01 = null;
					_imgTglCol02 = null;
				};

				JetPack.CharaMaker.OnCvsNavMenuClick += (_sender, _args) =>
				{
					if (_args.TopIndex == 4)
					{
						if (_args.SideToggle?.GetComponentInChildren<CvsAccessory>(true) == null)
						{
							_makerConfigWindow._onAccTab = false;
							_pluginCtrl._curPartsInfo = null;
							_pluginCtrl.CurSlotTriggerInfo = null;
							return;
						}

						int _slotIndex = (int) _args.SideToggle.GetComponentInChildren<CvsAccessory>(true)?.slotNo;
						_chaCtrl.StartCoroutine(_pluginCtrl.AccSlotChangedHandlerCoroutine(_slotIndex));
						_makerConfigWindow._onAccTab = true;
					}
					else
					{
						_makerConfigWindow._onAccTab = false;
					}
				};
				JetPack.CharaMaker.OnAccessoryTypeChanged += (_sender, _args) =>
				{
					_pluginCtrl.AccessoryTypeChanged(_args);
				};
				JetPack.CharaMaker.OnAccessoryParentChanged += (_sender, _args) =>
				{
					_pluginCtrl.AccessoryParentChanged(_args.SlotIndex);
				};
			}

			internal static void PatchMakerToggles()
			{
				Toggle[] _toggle = Traverse.Create(CustomBase.Instance.customCtrl.cmpDrawCtrl).Field("tglShowAccessory").GetValue<Toggle[]>();

				_imgTglCol01 = _toggle[0];
				_imgTglCol02 = _toggle[1];

				_toggle[0].onValueChanged.RemoveAllListeners();
				_toggle[0].onValueChanged.AddListener(_value =>
				{
					_pluginCtrl.SetAccessoryStateCategory(0, _value);
				});

				_toggle[1].onValueChanged.RemoveAllListeners();
				_toggle[1].onValueChanged.AddListener(_value =>
				{
					_pluginCtrl.SetAccessoryStateCategory(1, _value);
				});
			}

			internal static int GetCurrentAccSlot()
			{
				int _slotIndex = -1;
				foreach (Transform _child in _accMenuTree)
				{
					if (!_child.gameObject.activeSelf)
						break;

					Toggle _toggle = _child.GetComponent<Toggle>();
					if (_toggle == null)
						break;

					if (_toggle.isOn)
					{
						CvsAccessory _cmp = _toggle.GetComponentInChildren<CvsAccessory>(true);
						if (_cmp != null)
							_slotIndex = (int) _cmp.slotNo;
						break;
					}
				}
				return _slotIndex;
			}
		}
	}
}
