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
		internal static void CreateMakerInterface()
		{
			GameObject accw = GameObject.Find("04_AccessoryTop/AcsMoveWindow01");

			RectTransform windRt = accw.transform.Find("BasePanel/imgWindowBack").GetComponent<RectTransform>();
			ContainerOffsetMinY = (int) windRt.offsetMin.y - 90; // -90
			windRt.offsetMin = new Vector2(0, ContainerOffsetMinY);

			Transform toggleParent = accw.transform.Find("grpParent");

			Transform button = accw.transform.Find("button");
			RectTransform buttonRt = button.GetComponent<RectTransform>();

			AnchorOffsetMinY = (int) buttonRt.offsetMin.y;

// sep
			AnchorOffsetMinY += 40;
			Transform sep = Object.Instantiate(accw.transform.Find("grpParent/imgSeparete"), toggleParent, false);
			RectTransform sepRt = sep.GetComponent<RectTransform>();
			sepRt.offsetMin = new Vector2(0, AnchorOffsetMinY); // -408
			sepRt.offsetMax = new Vector2(0, AnchorOffsetMinY + 4); // -404
			sepRt.transform.name = "imgSeparete";
			sep.gameObject.SetActive(true);

// state toggles
			AnchorOffsetMinY -= 40;
			for (int i = 0; i < 4; i++)
				CreateMakerStateToggle(i);

// dropdown
			AnchorOffsetMinY -= 40;
			CreateMakerDropdown();

// save button
			AnchorOffsetMinY -= 40;
			Transform btnSave = Object.Instantiate(accw.transform.Find("grpParent/grpMove/X/btnReset"), toggleParent, true);
			btnSave.GetComponentInChildren<TextMeshProUGUI>().text = "Save";
			RectTransform dropdownRt = GameObject.Find("ddASSList").GetComponent<RectTransform>();
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
				pluginCtrl.CopySlotTriggerInfo(pluginCtrl.CurSlotTriggerInfo, pluginCtrl.CurOutfitTriggerInfo.Parts[SlotNo]);

				if ((pluginCtrl.CurSlotTriggerInfo.Kind >= 9) && (!pluginCtrl.CurSlotTriggerInfo.Group.IsNullOrEmpty()))
				{
					if (!pluginCtrl.VirtualGroupStates.ContainsKey(pluginCtrl.CurSlotTriggerInfo.Group))
						pluginCtrl.VirtualGroupStates[pluginCtrl.CurSlotTriggerInfo.Group] = true;
				}

				Logger.LogMessage($"SlotNo: {SlotNo + 1:00} updated");
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

		internal static void CreateMakerDropdownItems(List<string> labels)
		{
			Transform copy = GameObject.Find("ddASSList").transform;
			copy.gameObject.SetActive(false);
			TMP_Dropdown dropdown = copy.GetComponentInChildren<TMP_Dropdown>();
			dropdown.onValueChanged.RemoveAllListeners();
			dropdown.ClearOptions();
			dropdown.GetComponent<Image>().raycastTarget = true;
			dropdown.options.AddRange(ddASSListLabels.Select(x => new TMP_Dropdown.OptionData(x)));
			dropdown.options.AddRange(labels.Select(x => new TMP_Dropdown.OptionData(x)));
			dropdown.value = 0;
			dropdown.onValueChanged.AddListener( _ =>
			{
				ChaControl chaCtrl = MakerAPI.GetCharacterControl();
				AccStateSyncController pluginCtrl = GetController(chaCtrl);
				AccTriggerInfo Part = pluginCtrl.CurSlotTriggerInfo;
				if (Part == null)
				{
					Logger.LogError($"[ddASSList][{chaCtrl.chaFile.parameter?.fullname}] AccessoriesApi.SelectedMakerAccSlot {AccessoriesApi.SelectedMakerAccSlot} out of range!!");
					return;
				}

				int refIndex = dropdown.value < 10 ? dropdown.value : 9;
				GameObject.Find("tglASS0").GetComponentInChildren<TextMeshProUGUI>().alpha = clothesStates[refIndex][0] ? 1f : 0.2f;
				GameObject.Find("tglASS1").GetComponentInChildren<TextMeshProUGUI>().alpha = clothesStates[refIndex][1] ? 1f : 0.2f;
				GameObject.Find("tglASS2").GetComponentInChildren<TextMeshProUGUI>().alpha = clothesStates[refIndex][2] ? 1f : 0.2f;
				GameObject.Find("tglASS3").GetComponentInChildren<TextMeshProUGUI>().alpha = clothesStates[refIndex][3] ? 1f : 0.2f;
				Part.Kind = dropdown.value < 10 ? ddASSListVals[dropdown.value] : dropdown.value;

				MakerSettingChangePreview(chaCtrl, Part);
				Logger.Log(DebugLogLevel, $"[ddASSList][{chaCtrl.chaFile.parameter?.fullname}][Slot: {Part.Slot}][Kind: {Part.Kind}][State: {Part.State[0]}|{Part.State[1]}|{Part.State[2]}|{Part.State[3]}]");
			});
			copy.gameObject.SetActive(true);
		}

		internal static void CreateMakerDropdown()
		{
			Transform origin = GameObject.Find("06_SystemTop/tglConfig/ConfigTop/ddRamp").transform;
			Transform copy = Object.Instantiate(origin, GameObject.Find("04_AccessoryTop/AcsMoveWindow01/grpParent").transform, false);
			copy.name = "ddASSList";
			copy.GetComponentInChildren<TextMeshProUGUI>().text = " ";
			RectTransform copyRt = copy.GetComponent<RectTransform>();
			copyRt.offsetMin = new Vector2(0, AnchorOffsetMinY);
			copyRt.offsetMax = new Vector2(280, AnchorOffsetMinY + 40);

			List<string> extra = new List<string>();
			for (int i = 0; i < CustomGroupCount; i++)
				extra.Add($"Custom {(i + 1)}");
			CreateMakerDropdownItems(extra);
		}

		internal static void CreateMakerStateToggle(int i)
		{
			Transform origin = GameObject.Find("tglSlot01/Slot01Top/tglNoShake").transform;
			Transform copy = Object.Instantiate(origin, GameObject.Find("04_AccessoryTop/AcsMoveWindow01/grpParent").transform, false);
			copy.GetComponentInChildren<TextMeshProUGUI>().text = clothesStateText[i];

			RectTransform copyRt = copy.GetComponent<RectTransform>();
			copyRt.offsetMin = new Vector2(i * 100, AnchorOffsetMinY);
			copyRt.offsetMax = new Vector2(i * 100, AnchorOffsetMinY + 40);
			copyRt.transform.name = "tglASS" + i;

			Toggle toggle = copy.GetComponentInChildren<Toggle>();
			toggle.onValueChanged.RemoveAllListeners();
			toggle.image.raycastTarget = true;
			toggle.graphic.raycastTarget = true;
			toggle.isOn = clothesStates[0][i];

			toggle.onValueChanged.AddListener( _ =>
			{
				ChaControl chaCtrl = MakerAPI.GetCharacterControl();
				AccStateSyncController pluginCtrl = GetController(chaCtrl);
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
		}

		internal static void CreateMakerVirtualGroupToggle(string group)
		{
			Transform origin = GameObject.Find("tglSlot01/Slot01Top/tglNoShake").transform;
			Transform copy = Object.Instantiate(origin, GameObject.Find("04_AccessoryTop/AcsMoveWindow01/grpParent").transform, false);

			copy.GetComponentInChildren<TextMeshProUGUI>().text = (AccParentNames.ContainsKey(group)) ? AccParentNames[group] : group;

			RectTransform copyRt = copy.GetComponent<RectTransform>();
			copyRt.offsetMin = new Vector2(0, AnchorOffsetMinY);
			copyRt.offsetMax = new Vector2(0, AnchorOffsetMinY + 40);
			copyRt.transform.name = "tglASS_" + group;

			Toggle toggle = copy.GetComponentInChildren<Toggle>();
			toggle.onValueChanged.RemoveAllListeners();
			toggle.image.raycastTarget = true;
			toggle.graphic.raycastTarget = true;

			ChaControl chaCtrl = MakerAPI.GetCharacterControl();
			AccStateSyncController pluginCtrl = GetController(chaCtrl);
			pluginCtrl.VirtualGroupNames.Add("tglASS_" + group);
			toggle.isOn = pluginCtrl.VirtualGroupStates[group];

			toggle.onValueChanged.AddListener( _ =>
			{
				bool show = toggle.isOn;
				pluginCtrl.ToggleByVirtualGroup(group, show);
			});

			copy.gameObject.SetActive(true);
			AnchorOffsetMinY -= 40;
		}
	}
}
