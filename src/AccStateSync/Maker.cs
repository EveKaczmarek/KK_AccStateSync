using System.Collections.Generic;
using System.Linq;

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
		internal static bool _loadCharaExtdata => _makerLoadToggle == null || _makerLoadToggle.Value;
		internal static bool _loadCoordinateExtdata => _makerCoordinateLoadToggle == null || _makerCoordinateLoadToggle.Value;

		internal static class CharaMaker
		{
			internal static ChaControl _chaCtrl => CustomBase.Instance?.chaCtrl;
			internal static AccStateSyncController _pluginCtrl => GetController(CustomBase.Instance?.chaCtrl);
			internal static Toggle _imgTglCol01, _imgTglCol02;
			internal static int _currentSlotIndex => AccessoriesApi.SelectedMakerAccSlot;

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
				AccessoriesApi.SelectedMakerAccSlotChanged += (object _sender, AccessorySlotEventArgs _args) => _chaCtrl.StartCoroutine(_pluginCtrl.AccSlotChangedHandlerCoroutine(_args.SlotIndex));

				MakerAPI.MakerBaseLoaded += (_sender, _args) =>
				{
					_hooksInstance["CharaMaker"] = Harmony.CreateAndPatchAll(typeof(HooksCharaMaker));
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
				};
				MakerAPI.MakerExiting += (sender, args) =>
				{
					Destroy(_makerConfigWindow);

					_hooksInstance["CharaMaker"].UnpatchAll(_hooksInstance["CharaMaker"].Id);
					_hooksInstance["CharaMaker"] = null;
					MoreAccessories.HarmonyUnpatch();

					_sidebarTogglePreview = null;
					_sidebarToggleEnable = null;
					_makerLoadToggle = null;
					_makerCoordinateLoadToggle = null;
					_imgTglCol01 = null;
					_imgTglCol02 = null;
				};

				JetPack.CharaMaker.OnCvsNavMenuClick += (_sender, _args) =>
				{
					if (_args.TopIndex == 3)
					{
						if (!_args.Changed)
							return;
						if (_cfgCharaMakerPreview.Value)
							_instance.StartCoroutine(_pluginCtrl.SyncAllAccToggleCoroutine());
					}
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
		}
	}
}
