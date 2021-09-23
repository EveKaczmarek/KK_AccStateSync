using System.Linq;

using UnityEngine;
using UnityEngine.UI;
using UniRx;
using ChaCustom;

using BepInEx;
using HarmonyLib;

using KKAPI.Maker;
using KKAPI.Maker.UI;
using KKAPI.Maker.UI.Sidebar;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		internal static SidebarToggle _sidebarTogglePreview;
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
			internal static int _currentSlotIndex => JetPack.CharaMaker.CurrentAccssoryIndex;
			internal static string _savePath = Paths.ConfigPath;

			internal static void RegisterControls()
			{
				_cfgCharaMakerPreview.SettingChanged += (sender, args) =>
				{
					if (JetPack.CharaMaker.Loaded)
					{
						if (_sidebarTogglePreview.Value != _cfgCharaMakerPreview.Value)
							_sidebarTogglePreview.Value = _cfgCharaMakerPreview.Value;
						if (_sidebarTogglePreview.Value)
						{
							_pluginCtrl.RefreshPreview("ToggleForcePreview");
						}
						/*
						else
						{
							_pluginCtrl.SetAccessoryStateCategory(0, _imgTglCol01.isOn);
							_pluginCtrl.SetAccessoryStateCategory(1, _imgTglCol02.isOn);
						}
						*/
					}
				};

				AccessoriesApi.AccessoryTransferred += (_sender, _args) => _pluginCtrl.AccessoryTransferredHandler(_args.SourceSlotIndex, _args.DestinationSlotIndex);
				AccessoriesApi.AccessoriesCopied += (_sender, _args) => _pluginCtrl.AccessoriesCopiedHandler((int) _args.CopySource, (int) _args.CopyDestination, _args.CopiedSlotIndexes.ToList());

				MakerAPI.RegisterCustomSubCategories += (_sender, _args) =>
				{
					_makerLoadToggle = _args.AddLoadToggle(new MakerLoadToggle("AccStateSync"));
					_makerCoordinateLoadToggle = _args.AddCoordinateLoadToggle(new MakerCoordinateLoadToggle("AccStateSync"));
					_charaConfigWindow = _instance.gameObject.AddComponent<AccStateSyncUI>();
					_sidebarTogglePreview = _args.AddSidebarControl(new SidebarToggle("A.S.S. Preview", _cfgCharaMakerPreview.Value, _instance));
					_sidebarTogglePreview.ValueChanged.Subscribe(_value =>
					{
						if (_cfgCharaMakerPreview.Value != _value)
							_cfgCharaMakerPreview.Value = _value;
					});
					_accWinCtrlEnable = MakerAPI.AddAccessoryWindowControl(new MakerButton("AccStateSync", null, _instance));
					_accWinCtrlEnable.OnClick.AddListener(() => _charaConfigWindow.enabled = true);
					_accWinCtrlEnable.Visible.OnNext(false);
				};
				MakerAPI.MakerFinishedLoading += (_sender, _args) =>
				{
					PatchMakerToggles();
					if (MoreAccessories._installed)
						_accMenuTree = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMenuTree/04_AccessoryTop/Slots/Viewport/Content").transform;
					else
						_accMenuTree = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMenuTree/04_AccessoryTop").transform;
					MoreAccessories.HarmonyPatch();
				};

				JetPack.CharaMaker.OnMakerExiting += (_sender, _args) =>
				{
					Destroy(_charaConfigWindow);

					MoreAccessories.HarmonyUnpatch();

					_sidebarTogglePreview = null;
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
							_charaConfigWindow._onAccTab = false;
							_pluginCtrl._curPartsInfo = null;
							_pluginCtrl._cachedSlotPropertyList.Clear();
							_chaCtrl.StartCoroutine(_pluginCtrl.AccSlotChangedHandlerCoroutine());
							return;
						}

						_chaCtrl.StartCoroutine(_pluginCtrl.AccSlotChangedHandlerCoroutine());
						_charaConfigWindow._onAccTab = true;
					}
					else
					{
						if (_args.TopIndex == 3)
							_chaCtrl.StartCoroutine(_pluginCtrl.AccSlotChangedHandlerCoroutine());
						_charaConfigWindow._onAccTab = false;
					}
				};
				JetPack.CharaMaker.OnAccessoryTypeChanged += (_sender, _args) =>
				{
					_pluginCtrl.AccessoryTypeChanged(_args);
				};
			}

			internal static void PatchMakerToggles()
			{
				Toggle[] _toggle = CustomBase.Instance.customCtrl.cmpDrawCtrl.tglShowAccessory;

				_imgTglCol01 = _toggle[0];
				_imgTglCol02 = _toggle[1];

				_toggle[0].onValueChanged.RemoveAllListeners();
				_toggle[0].onValueChanged.AddListener(_value =>
				{
					if (_cfgCharaMakerPreview.Value)
						MoreAccessories.Hooks.SetAccessoryStateCategory(_chaCtrl, 0, _value);
					else
						_pluginCtrl.SetAccessoryStateCategory(0, _value);
				});

				_toggle[1].onValueChanged.RemoveAllListeners();
				_toggle[1].onValueChanged.AddListener(_value =>
				{
					if (_cfgCharaMakerPreview.Value)
						MoreAccessories.Hooks.SetAccessoryStateCategory(_chaCtrl, 1, _value);
					else
						_pluginCtrl.SetAccessoryStateCategory(1, _value);
				});
			}
		}
	}
}
