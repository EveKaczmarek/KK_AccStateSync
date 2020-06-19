using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using TMPro;
using KKAPI.Maker;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		public static Transform AcsMoveWindow;
		public static RectTransform imgWindowBack;
		public static Transform grpParent;
		public static Transform tglParent;
		public static Dictionary<string, GameObject> tglASSobj;
		public static Dictionary<string, GameObject> tglASSgroup;

		internal static void CreateMakerInterface()
		{
			AcsMoveWindow = GameObject.Find("04_AccessoryTop/AcsMoveWindow01").transform;
			imgWindowBack = AcsMoveWindow.Find("BasePanel/imgWindowBack").GetComponent<RectTransform>();
			grpParent = AcsMoveWindow.Find("grpParent");
			tglParent = GameObject.Find("tglSlot01/Slot01Top/tglNoShake").transform;
			tglASSobj = new Dictionary<string, GameObject>();
			tglASSgroup = new Dictionary<string, GameObject>();

			ContainerOffsetMinY = (int) imgWindowBack.offsetMin.y - 90; // -90
			imgWindowBack.offsetMin = new Vector2(0, ContainerOffsetMinY);

			AnchorOffsetMinY = (int) AcsMoveWindow.Find("button").GetComponent<RectTransform>().offsetMin.y;
			MenuitemHeightOffsetY = 40;

// sep
			AnchorOffsetMinY += MenuitemHeightOffsetY;
			Transform sep = Instantiate(grpParent.Find("imgSeparete"), grpParent, false);
			RectTransform sepRt = sep.GetComponent<RectTransform>();
			sepRt.offsetMin = new Vector2(0, AnchorOffsetMinY); // -408
			sepRt.offsetMax = new Vector2(0, AnchorOffsetMinY + 4); // -404
			sepRt.transform.name = "imgSeparete";
			sep.gameObject.SetActive(true);

// state toggles
			AnchorOffsetMinY -= MenuitemHeightOffsetY;
			for (int i = 0; i < 4; i++)
				CreateMakerStateToggle(i);

// dropdown
			AnchorOffsetMinY -= MenuitemHeightOffsetY;
			CreateMakerDropdown();

// save button
			AnchorOffsetMinY -= MenuitemHeightOffsetY;
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
			btnSaveButton.onClick.AddListener(() =>
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
						pluginCtrl.CurOutfitTriggerInfo.Parts.Remove(SlotNo);
				}
				else
				{
					if (!pluginCtrl.CurOutfitTriggerInfo.Parts.ContainsKey(SlotNo))
						pluginCtrl.CurOutfitTriggerInfo.Parts[SlotNo] = new AccTriggerInfo(SlotNo);
					CopySlotTriggerInfo(pluginCtrl.CurSlotTriggerInfo, pluginCtrl.CurOutfitTriggerInfo.Parts[SlotNo]);

					if ((pluginCtrl.CurSlotTriggerInfo.Kind >= 9) && (!pluginCtrl.CurSlotTriggerInfo.Group.IsNullOrEmpty()))
					{
						if (!pluginCtrl.VirtualGroupStates.ContainsKey(pluginCtrl.CurSlotTriggerInfo.Group))
							pluginCtrl.VirtualGroupStates[pluginCtrl.CurSlotTriggerInfo.Group] = true;
					}
				}

				Logger.LogMessage($"Slot{SlotNo + 1:00} updated");
				pluginCtrl.AccSlotChangedHandler(AccessoriesApi.SelectedMakerAccSlot, true);
			});
			btnSave.gameObject.SetActive(true);
		}

		internal static void MakerSettingChangePreview(ChaControl chaCtrl, AccTriggerInfo Part)
		{
			AccStateSyncController pluginCtrl = GetController(chaCtrl);
			if (MathfEx.RangeEqualOn(0, Part.Kind, 7))
			{
				int state = chaCtrl.fileStatus.clothesState[Part.Kind];
				bool vis = Part.State[state];
				Part.Group = "";
				chaCtrl.SetAccessoryState(Part.Slot, vis);
			}
			else if (Part.Kind >= 9)
			{
				if (Part.Kind == 9)
				{
					List<ChaFileAccessory.PartsInfo> PartsInfo = MoreAccessories_Support.GetAccessoriesInfos(chaCtrl);
					Part.Group = PartsInfo[Part.Slot].parentKey;
				}
				else
					Part.Group = "custom_" + (Part.Kind - 9).ToString();

				bool vis = true;
				if (pluginCtrl.VirtualGroupStates.ContainsKey(Part.Group))
					vis = pluginCtrl.VirtualGroupStates[Part.Group] ? Part.State[0] : Part.State[3];
				chaCtrl.SetAccessoryState(Part.Slot, vis);
			}
		}

		internal static void CreateMakerDropdownItems(List<string> labels, int i = -1)
		{
			Transform copy = grpParent.Find("ddASSList").transform;
			copy.gameObject.SetActive(false);
			TMP_Dropdown dropdown = copy.GetComponentInChildren<TMP_Dropdown>();
			dropdown.GetComponent<Image>().raycastTarget = true;

			dropdown.onValueChanged.RemoveAllListeners();
			dropdown.ClearOptions();
			dropdown.options.AddRange(ddASSListLabels.Select(x => new TMP_Dropdown.OptionData(x)));
			dropdown.options.AddRange(labels.Select(x => new TMP_Dropdown.OptionData(x)));
			dropdown.options.AddRange((new List<string>() {"+", "-"}).Select(x => new TMP_Dropdown.OptionData(x)));
			dropdown.value = i;
			dropdown.onValueChanged.AddListener( _ =>
			{
				ChaControl chaCtrl = MakerAPI.GetCharacterControl();
				AccStateSyncController pluginCtrl = GetController(chaCtrl);
				AccTriggerInfo Part = pluginCtrl.CurSlotTriggerInfo;
				int j = dropdown.options.Count();
				if (dropdown.value == (j - 2))
				{
					pluginCtrl.PushGroup();
					List<string> extra = pluginCtrl.CurOutfitVirtualGroupNames.Select(x => x.Value).ToList();
					CreateMakerDropdownItems(extra, Part.Kind);
					return;
				}
				else if (dropdown.value == (j - 1))
				{
					pluginCtrl.PopGroup();
					List<string> extra = pluginCtrl.CurOutfitVirtualGroupNames.Select(x => x.Value).ToList();
					CreateMakerDropdownItems(extra, Part.Kind);
					return;
				}

				if (Part == null)
				{
					Logger.LogError($"[ddASSList][{chaCtrl.chaFile.parameter?.fullname}] AccessoriesApi.SelectedMakerAccSlot {AccessoriesApi.SelectedMakerAccSlot} out of range!!");
					return;
				}

				int refIndex = dropdown.value <= 9 ? dropdown.value : 9;
				for (int x = 0; x < 4; x++)
					tglASSobj[$"tglASS{x}"].GetComponentInChildren<TextMeshProUGUI>().alpha = clothesStates[refIndex][x] ? 1f : 0.2f;
				Part.Kind = dropdown.value <= 9 ? ddASSListVals[dropdown.value] : dropdown.value;

				MakerSettingChangePreview(chaCtrl, Part);
				Logger.Log(DebugLogLevel, $"[ddASSList][{chaCtrl.chaFile.parameter?.fullname}][Slot: {Part.Slot}][Kind: {Part.Kind}][State: {Part.State[0]}|{Part.State[1]}|{Part.State[2]}|{Part.State[3]}]");
			});
			copy.gameObject.SetActive(true);
		}

		internal static void CreateMakerDropdown()
		{
			Transform origin = GameObject.Find("06_SystemTop/tglConfig/ConfigTop/ddRamp").transform;
			Transform copy = Instantiate(origin, grpParent, false);
			copy.name = "ddASSList";
			copy.GetComponentInChildren<TextMeshProUGUI>().text = " ";
			RectTransform copyRt = copy.GetComponent<RectTransform>();
			copyRt.offsetMin = new Vector2(0, AnchorOffsetMinY);
			copyRt.offsetMax = new Vector2(280, AnchorOffsetMinY + MenuitemHeightOffsetY);

			List<string> extra = new List<string>();
			for (int i = 0; i < DefaultCustomGroupCount; i++)
				extra.Add($"Custom {(i + 1)}");
			CreateMakerDropdownItems(extra);
		}

		internal static void CreateMakerStateToggle(int i)
		{
			Transform copy = Instantiate(tglParent, grpParent, false);

			ChaControl chaCtrl = MakerAPI.GetCharacterControl();
			AccStateSyncController pluginCtrl = GetController(chaCtrl);

			RectTransform copyRt = copy.GetComponent<RectTransform>();
			copyRt.offsetMin = new Vector2(i * 100, AnchorOffsetMinY);
			copyRt.offsetMax = new Vector2(i * 100, AnchorOffsetMinY + MenuitemHeightOffsetY);
			copyRt.transform.name = "tglASS" + i;
			copyRt.GetComponentInChildren<TextMeshProUGUI>().text = clothesStateText[i];

			Toggle toggle = copy.GetComponentInChildren<Toggle>();
			toggle.onValueChanged.RemoveAllListeners();
			toggle.image.raycastTarget = true;
			toggle.graphic.raycastTarget = true;
			toggle.isOn = clothesStates[0][i];

			toggle.onValueChanged.AddListener( _ =>
			{
				AccTriggerInfo Part = pluginCtrl.CurSlotTriggerInfo;
				if (Part == null)
				{
					Logger.LogError($"[btnASSsave][{chaCtrl.chaFile.parameter?.fullname}] CurSlotTriggerInfo is Null");
					return;
				}

				Part.State[i] = toggle.isOn;
				MakerSettingChangePreview(chaCtrl, Part);
				Logger.Log(DebugLogLevel, $"[tglASS{i}][{chaCtrl.chaFile.parameter?.fullname}][Slot: {Part.Slot}][Kind: {Part.Kind}][State: {Part.State[0]}|{Part.State[1]}|{Part.State[2]}|{Part.State[3]}]");
			});

			copy.gameObject.SetActive(true);
			tglASSobj["tglASS" + i] = copy.gameObject;
		}

		internal static void CreateMakerVirtualGroupToggle(string group)
		{
			Transform copy = Instantiate(tglParent, grpParent, false);

			ChaControl chaCtrl = MakerAPI.GetCharacterControl();
			AccStateSyncController pluginCtrl = GetController(chaCtrl);

			RectTransform copyRt = copy.GetComponent<RectTransform>();
			copyRt.offsetMin = new Vector2(0, AnchorOffsetMinY);
			copyRt.offsetMax = new Vector2(0, AnchorOffsetMinY + MenuitemHeightOffsetY);
			copyRt.transform.name = "tglASS_" + group;

			string label = group;
			if (AccParentNames.ContainsKey(group))
				label = AccParentNames[group];
			else if (pluginCtrl.CurOutfitVirtualGroupNames.ContainsKey(group))
				label = pluginCtrl.CurOutfitVirtualGroupNames[group];
			copy.GetComponentInChildren<TextMeshProUGUI>().text = label;

			Toggle toggle = copy.GetComponentInChildren<Toggle>();
			toggle.onValueChanged.RemoveAllListeners();
			toggle.image.raycastTarget = true;
			toggle.graphic.raycastTarget = true;
			toggle.isOn = pluginCtrl.VirtualGroupStates[group];

			toggle.onValueChanged.AddListener( _ =>
			{
				bool show = toggle.isOn;
				pluginCtrl.ToggleByVirtualGroup(group, show);
			});

			copy.gameObject.SetActive(true);
			tglASSgroup["tglASS_" + group] = copy.gameObject;
			AnchorOffsetMinY -= MenuitemHeightOffsetY;
		}
	}
}
