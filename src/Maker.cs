using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using TMPro;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using KKAPI.Maker.UI.Sidebar;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		internal static SidebarToggle CharaMakerPreviewSidebarToggle;
		internal static MakerLoadToggle LoadCharaExtdataToggle;
		internal static bool LoadCharaExtdata => LoadCharaExtdataToggle == null || LoadCharaExtdataToggle.Value;
		internal static MakerCoordinateLoadToggle LoadCoordinateExtdataToggle;
		internal static bool LoadCoordinateExtdata => LoadCoordinateExtdataToggle == null || LoadCoordinateExtdataToggle.Value;

		internal static class CharaMaker
		{
			internal static Transform AcsMoveWindow;
			internal static RectTransform imgWindowBack;
			internal static Transform grpParent;
			internal static Transform tglParent;
			internal static Dictionary<string, GameObject> tglASSobj;
			internal static Dictionary<string, GameObject> tglASSgroup;
			internal static Dictionary<int, UI.DropdownASSList> ddASSList = new Dictionary<int, UI.DropdownASSList>();

			internal static void RegisterControls()
			{
				CharaMakerPreview.SettingChanged += (sender, args) =>
				{
					if (MakerAPI.InsideMaker)
					{
						CharaMakerPreviewSidebarToggle.Value = CharaMakerPreview.Value;
						if (CharaMakerPreviewSidebarToggle.Value)
							GetController(MakerAPI.GetCharacterControl()).SyncAllAccToggle();
					}
				};

				AccessoriesApi.SelectedMakerAccSlotChanged += (object sender, AccessorySlotEventArgs eventArgs) => GetController(MakerAPI.GetCharacterControl()).AccSlotChangedHandler(eventArgs.SlotIndex);
				AccessoriesApi.AccessoryTransferred += (object sender, AccessoryTransferEventArgs eventArgs) => GetController(MakerAPI.GetCharacterControl()).AccessoryTransferredHandler(eventArgs.SourceSlotIndex, eventArgs.DestinationSlotIndex);
				AccessoriesApi.AccessoriesCopied += (object sender, AccessoryCopyEventArgs eventArgs) => GetController(MakerAPI.GetCharacterControl()).AccessoriesCopiedHandler((int)eventArgs.CopySource, (int)eventArgs.CopyDestination, eventArgs.CopiedSlotIndexes.ToList());

				MakerAPI.MakerBaseLoaded += (sender, e) =>
				{
					HooksInstance["CharaMaker"] = HarmonyLib.Harmony.CreateAndPatchAll(typeof(HooksCharaMaker));
					LoadCharaExtdataToggle = e.AddLoadToggle(new MakerLoadToggle("AccStateSync"));
					LoadCoordinateExtdataToggle = e.AddCoordinateLoadToggle(new MakerCoordinateLoadToggle("AccStateSync"));
				};
				MakerAPI.RegisterCustomSubCategories += (sender, e) =>
				{
					CharaMakerPreviewSidebarToggle = e.AddSidebarControl(new SidebarToggle("Force Preview", CharaMakerPreview.Value, Instance));
					CharaMakerPreviewSidebarToggle.ValueChanged.Subscribe(b => CharaMakerPreview.Value = b);
				};
				MakerAPI.MakerExiting += (sender, e) =>
				{
					HooksInstance["CharaMaker"].UnpatchAll(HooksInstance["CharaMaker"].Id);
					HooksInstance["CharaMaker"] = null;
					CharaMakerPreviewSidebarToggle = null;
					LoadCharaExtdataToggle = null;
					LoadCoordinateExtdataToggle = null;
				};
				MakerAPI.MakerFinishedLoading += (sender, e) => InitInterface();
			}

			internal static void InitInterface()
			{
				AcsMoveWindow = GameObject.Find("04_AccessoryTop/AcsMoveWindow01").transform;
				imgWindowBack = AcsMoveWindow.Find("BasePanel/imgWindowBack").GetComponent<RectTransform>();
				grpParent = AcsMoveWindow.Find("grpParent");
				tglParent = GameObject.Find("tglSlot01/Slot01Top/tglResetColor").transform;
				tglASSobj = new Dictionary<string, GameObject>();
				tglASSgroup = new Dictionary<string, GameObject>();

				UI.ContainerOffsetMinY = (int) imgWindowBack.offsetMin.y - 90; // -90
				imgWindowBack.offsetMin = new Vector2(0, UI.ContainerOffsetMinY);

				UI.AnchorOffsetMinY = (int) AcsMoveWindow.Find("button").GetComponent<RectTransform>().offsetMin.y;
				UI.MenuitemHeightOffsetY = 40;

				// sep
				UI.AnchorOffsetMinY += UI.MenuitemHeightOffsetY;
				Transform sep = Instantiate(grpParent.Find("imgSeparete"), grpParent, false);
				RectTransform sepRt = sep.GetComponent<RectTransform>();
				sepRt.offsetMin = new Vector2(0, UI.AnchorOffsetMinY); // -408
				sepRt.offsetMax = new Vector2(0, UI.AnchorOffsetMinY + 4); // -404
				sepRt.transform.name = "imgSeparete";
				sep.gameObject.SetActive(true);

				// state toggles
				UI.AnchorOffsetMinY -= UI.MenuitemHeightOffsetY;
				for (int i = 0; i < 4; i++)
					CreateStateToggle(i);

				// dropdown
				UI.AnchorOffsetMinY -= UI.MenuitemHeightOffsetY;
				CreateDropdown();

				// save button
				UI.AnchorOffsetMinY -= UI.MenuitemHeightOffsetY;
				Transform btnSave = Instantiate(grpParent.Find("grpMove/X/btnReset"), grpParent, true);
				btnSave.GetComponentInChildren<TextMeshProUGUI>().text = "Save";
				RectTransform dropdownRt = grpParent.Find("ddASSList").GetComponent<RectTransform>();
				RectTransform btnSaveRt = btnSave.GetComponent<RectTransform>();
				btnSaveRt.offsetMin = new Vector2(btnSaveRt.offsetMin.x, dropdownRt.offsetMin.y + 412); // -76
				btnSaveRt.offsetMax = new Vector2(btnSaveRt.offsetMax.x, dropdownRt.offsetMin.y + 412 - 380); // -456
				btnSaveRt.transform.name = "btnASSsave";
				Button btnSaveButton = btnSave.GetComponentInChildren<Button>();
				btnSaveButton.onClick.RemoveAllListeners();
				btnSaveButton.targetGraphic.raycastTarget = true;
				btnSaveButton.onClick.AddListener(delegate ()
				{
					ChaControl chaCtrl = MakerAPI.GetCharacterControl();
					AccStateSyncController pluginCtrl = GetController(chaCtrl);

					if (pluginCtrl.CurSlotTriggerInfo == null)
					{
						Logger.LogError($"[btnASSsave][{chaCtrl.chaFile.parameter?.fullname}] CurSlotTriggerInfo is Null");
						return;
					}

					int SlotNo = pluginCtrl.CurSlotTriggerInfo.Slot;
					if (pluginCtrl.CurSlotTriggerInfo.Kind == -1)
					{
						if (pluginCtrl.CurOutfitTriggerInfo.Parts.ContainsKey(SlotNo))
						{
							pluginCtrl.CurOutfitTriggerInfo.Parts.Remove(SlotNo);
							Logger.LogMessage($"AccTriggerInfo for Coordinate {pluginCtrl.CurrentCoordinateIndex} Slot{SlotNo + 1:00} has been reset");
						}
					}
					else
					{
						if (!pluginCtrl.CurOutfitTriggerInfo.Parts.ContainsKey(SlotNo))
							pluginCtrl.CurOutfitTriggerInfo.Parts[SlotNo] = new AccTriggerInfo(SlotNo);
						CopySlotTriggerInfo(pluginCtrl.CurSlotTriggerInfo, pluginCtrl.CurOutfitTriggerInfo.Parts[SlotNo]);

						if ((pluginCtrl.CurSlotTriggerInfo.Kind >= 9) && (!pluginCtrl.CurSlotTriggerInfo.Group.IsNullOrEmpty()))
						{
							if (!pluginCtrl.CurOutfitVirtualGroupInfo.ContainsKey(pluginCtrl.CurSlotTriggerInfo.Group))
								pluginCtrl.CurOutfitVirtualGroupInfo[pluginCtrl.CurSlotTriggerInfo.Group] = new VirtualGroupInfo(pluginCtrl.CurSlotTriggerInfo.Group, pluginCtrl.CurSlotTriggerInfo.Kind);
						}
						Logger.LogMessage($"Slot{SlotNo + 1:00} updated");
					}

					pluginCtrl.AccSlotChangedHandler(AccessoriesApi.SelectedMakerAccSlot, true);
				});
				btnSave.gameObject.SetActive(true);

				GameObject parent = GameObject.Find("CustomScene/CustomRoot/FrontUIGroup/CustomUIGroup/CvsMenuTree/03_ClothesTop");
				foreach (Transform child in parent.transform)
				{
					child.GetComponent<Toggle>().onValueChanged.AddListener(delegate (bool value)
					{
						ChaControl chaCtrl = MakerAPI.GetCharacterControl();
						AccStateSyncController pluginCtrl = GetController(chaCtrl);
						if (value)
							Instance.StartCoroutine(pluginCtrl.WaitForEndOfFrameSyncAllAccToggle());
					});
				}
			}

			internal static void CreateDropdownList(ChaControl chaCtrl)
			{
				AccStateSyncController pluginCtrl = GetController(chaCtrl);
				ddASSList.Clear();

				foreach (KeyValuePair<int, UI.DropdownASSList> item in Constants.DropdownASSList)
					ddASSList[item.Key] = new UI.DropdownASSList(item.Value.Index, item.Value.Kind, item.Value.Label, item.Value.States);

				int i = Constants.DropdownASSList.Count();
				List<VirtualGroupInfo> Filtered = pluginCtrl.CurOutfitVirtualGroupInfo.Values?.Where(x => x.Kind > 9)?.OrderBy(x => x.Kind)?.ToList();
				if (Filtered?.Count() > 0)
				{
					foreach (VirtualGroupInfo group in Filtered)
					{
						ddASSList[i] = new UI.DropdownASSList(i, group.Kind, group.Label, new List<bool>() { true, false, false, true });
						i++;
					}
				}
			}

			internal static void CreateDropdownItems(int i)
			{
				Transform copy = grpParent.Find("ddASSList").transform;
				copy.gameObject.SetActive(false);
				TMP_Dropdown dropdown = copy.GetComponentInChildren<TMP_Dropdown>();
				dropdown.GetComponent<Image>().raycastTarget = true;

				dropdown.onValueChanged.RemoveAllListeners();
				dropdown.ClearOptions();
				dropdown.options.AddRange(ddASSList.Select(x => new TMP_Dropdown.OptionData(x.Value.Label)));
				dropdown.options.AddRange((new List<string>() { "+", "-" }).Select(x => new TMP_Dropdown.OptionData(x)));
				dropdown.value = i;
				dropdown.onValueChanged.AddListener(delegate (int value)
				{
					ChaControl chaCtrl = MakerAPI.GetCharacterControl();
					AccStateSyncController pluginCtrl = GetController(chaCtrl);
					AccTriggerInfo Part = pluginCtrl.CurSlotTriggerInfo;

					if (Part == null)
					{
						Logger.LogError($"[ddASSList][{chaCtrl.chaFile.parameter?.fullname}] AccessoriesApi.SelectedMakerAccSlot {AccessoriesApi.SelectedMakerAccSlot} out of range!!");
						return;
					}

					int j = dropdown.options.Count();
					if ((value == (j - 1)) || (value == (j - 2)))
					{
						if (value == (j - 2))
							pluginCtrl.PushGroup();
						else if (value == (j - 1))
							pluginCtrl.PopGroup();
						pluginCtrl.AccSlotChangedHandler(AccessoriesApi.SelectedMakerAccSlot, true);
						return;
					}

					Logger.LogWarning($"[CreateDropdownItems][dropdown.value: {value}]");
					UI.DropdownASSList item = ddASSList.Values.Where(x => x.Index == value).FirstOrDefault();
					if (item == null) return;
					Part.Kind = item.Kind;

					bool clickable = Part.Kind != -1;
					for (int x = 0; x < 4; x++)
					{
						tglASSobj[$"tglASS{x}"].GetComponentInChildren<TextMeshProUGUI>().alpha = item.States[x] ? 1f : 0.2f;
						tglASSobj[$"tglASS{x}"].GetComponentInChildren<Toggle>().interactable = clickable;
					}

					Logger.Log(DebugLogLevel, $"[ddASSList][{chaCtrl.chaFile.parameter?.fullname}][Slot: {Part.Slot}][Kind: {Part.Kind}][State: {Part.State[0]}|{Part.State[1]}|{Part.State[2]}|{Part.State[3]}]");
					pluginCtrl.PreviewChange();
				});
				copy.gameObject.SetActive(true);
			}

			internal static void CreateDropdown()
			{
				Transform origin = GameObject.Find("06_SystemTop/tglConfig/ConfigTop/ddRamp").transform;
				Transform copy = Instantiate(origin, grpParent, false);
				copy.name = "ddASSList";
				copy.GetComponentInChildren<TextMeshProUGUI>().text = " ";
				RectTransform copyRt = copy.GetComponent<RectTransform>();
				copyRt.offsetMin = new Vector2(0, UI.AnchorOffsetMinY);
				copyRt.offsetMax = new Vector2(280, UI.AnchorOffsetMinY + UI.MenuitemHeightOffsetY);

				CreateDropdownList(MakerAPI.GetCharacterControl());
				CreateDropdownItems(-1);
			}

			internal static void CreateStateToggle(int i)
			{
				Transform copy = Instantiate(tglParent, grpParent, false);

				ChaControl chaCtrl = MakerAPI.GetCharacterControl();
				AccStateSyncController pluginCtrl = GetController(chaCtrl);

				RectTransform copyRt = copy.GetComponent<RectTransform>();
				copyRt.offsetMin = new Vector2(i * 100, UI.AnchorOffsetMinY);
				copyRt.offsetMax = new Vector2(i * 100, UI.AnchorOffsetMinY + UI.MenuitemHeightOffsetY);
				copyRt.transform.name = "tglASS" + i;
				copyRt.GetComponentInChildren<TextMeshProUGUI>().text = UI.clothesStateText[i];

				Toggle toggle = copy.GetComponentInChildren<Toggle>();
				toggle.onValueChanged.RemoveAllListeners();
				toggle.image.raycastTarget = true;
				toggle.graphic.raycastTarget = true;
				toggle.isOn = Constants.DropdownASSList[0].States[i];

				toggle.onValueChanged.AddListener(delegate (bool value)
				{
					AccTriggerInfo Part = pluginCtrl.CurSlotTriggerInfo;
					if (Part == null)
					{
						Logger.LogError($"[btnASSsave][{chaCtrl.chaFile.parameter?.fullname}] CurSlotTriggerInfo is Null");
						return;
					}

					Part.State[i] = value;
					pluginCtrl.PreviewChange();
					Logger.Log(DebugLogLevel, $"[tglASS{i}][{chaCtrl.chaFile.parameter?.fullname}][Slot: {Part.Slot}][Kind: {Part.Kind}][State: {Part.State[0]}|{Part.State[1]}|{Part.State[2]}|{Part.State[3]}]");
				});

				copy.gameObject.SetActive(true);
				tglASSobj["tglASS" + i] = copy.gameObject;
			}

			internal static void ClearVirtualGroupToggle()
            {
				foreach (KeyValuePair<string, GameObject> group in tglASSgroup)
				{
					if (group.Value.gameObject != null)
						Destroy(group.Value);
				}
				tglASSgroup.Clear();
			}

			internal static void CreateVirtualGroupToggle(string group)
			{
				Transform copy = Instantiate(tglParent, grpParent, false);

				ChaControl chaCtrl = MakerAPI.GetCharacterControl();
				AccStateSyncController pluginCtrl = GetController(chaCtrl);

				RectTransform copyRt = copy.GetComponent<RectTransform>();
				copyRt.offsetMin = new Vector2(0, UI.AnchorOffsetMinY);
				copyRt.offsetMax = new Vector2(0, UI.AnchorOffsetMinY + UI.MenuitemHeightOffsetY);
				copyRt.transform.name = "tglASS_" + group;

				string label = pluginCtrl.CurOutfitVirtualGroupInfo[group].Label;
				if (VirtualGroupShowName.Value)
					label = label + " (" + pluginCtrl.CurOutfitVirtualGroupInfo[group].Group +  ")";
				copy.GetComponentInChildren<TextMeshProUGUI>().text = label;

				Toggle toggle = copy.GetComponentInChildren<Toggle>();
				toggle.onValueChanged.RemoveAllListeners();
				toggle.image.raycastTarget = true;
				toggle.graphic.raycastTarget = true;
				toggle.isOn = pluginCtrl.CurOutfitVirtualGroupInfo[group].State;

				toggle.onValueChanged.AddListener(value =>
				{
					pluginCtrl.ToggleByVirtualGroup(group, value);
				});

				copy.gameObject.SetActive(true);
				tglASSgroup["tglASS_" + group] = copy.gameObject;
				UI.AnchorOffsetMinY -= UI.MenuitemHeightOffsetY;
			}
		}
	}
}
