using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UniRx;
using TMPro;
using ChaCustom;

using HarmonyLib;

using KKAPI.Maker;
using KKAPI.Maker.UI;
using KKAPI.Maker.UI.Sidebar;
using JetPack;

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
			internal static Dictionary<string, GameObject> tglASSobj = new Dictionary<string, GameObject>();
			internal static Dictionary<string, GameObject> tglASSgroup = new Dictionary<string, GameObject>();
			internal static GameObject btnASSsave;
			internal static GameObject ddASSList;
			internal static Dictionary<int, UI.DropdownASSList> ddASSListDic = new Dictionary<int, UI.DropdownASSList>();

			internal static ChaControl chaCtrl => CustomBase.Instance?.chaCtrl;
			internal static AccStateSyncController pluginCtrl => GetController(chaCtrl);

			internal static void RegisterControls()
			{
				CharaMakerPreview.SettingChanged += (sender, args) =>
				{
					if (MakerAPI.InsideMaker)
					{
						CharaMakerPreviewSidebarToggle.Value = CharaMakerPreview.Value;
						if (CharaMakerPreviewSidebarToggle.Value)
							pluginCtrl.SyncAllAccToggle();
						else
						{
							pluginCtrl.SetAccessoryStateCategory(0, GameObject.Find("tglAcsGrp/imgTglCol01").GetComponent<Toggle>().isOn);
							pluginCtrl.SetAccessoryStateCategory(1, GameObject.Find("tglAcsGrp/imgTglCol02").GetComponent<Toggle>().isOn);
						}
					}
				};

				//AccessoriesApi.SelectedMakerAccSlotChanged += (object sender, AccessorySlotEventArgs eventArgs) => Instance.StartCoroutine(pluginCtrl.AccSlotChangedHandlerCoroutine(eventArgs.SlotIndex));
				AccessoriesApi.AccessoryTransferred += (object sender, AccessoryTransferEventArgs eventArgs) => pluginCtrl.AccessoryTransferredHandler(eventArgs.SourceSlotIndex, eventArgs.DestinationSlotIndex);
				AccessoriesApi.AccessoriesCopied += (object sender, AccessoryCopyEventArgs eventArgs) => pluginCtrl.AccessoriesCopiedHandler((int) eventArgs.CopySource, (int) eventArgs.CopyDestination, eventArgs.CopiedSlotIndexes.ToList());

				MakerAPI.MakerBaseLoaded += (sender, args) =>
				{
					HooksInstance["CharaMaker"] = Harmony.CreateAndPatchAll(typeof(HooksCharaMaker));
					MoreAccessories.HarmonyPatch();

					LoadCharaExtdataToggle = args.AddLoadToggle(new MakerLoadToggle("AccStateSync"));
					LoadCoordinateExtdataToggle = args.AddCoordinateLoadToggle(new MakerCoordinateLoadToggle("AccStateSync"));
					PatchMakerToggles();
				};
				MakerAPI.RegisterCustomSubCategories += (sender, args) =>
				{
					CharaMakerPreviewSidebarToggle = args.AddSidebarControl(new SidebarToggle("Force Preview", CharaMakerPreview.Value, Instance));
					CharaMakerPreviewSidebarToggle.ValueChanged.Subscribe(value => CharaMakerPreview.Value = value);
				};
				MakerAPI.MakerExiting += (sender, args) =>
				{
					HooksInstance["CharaMaker"].UnpatchAll(HooksInstance["CharaMaker"].Id);
					HooksInstance["CharaMaker"] = null;
					MoreAccessories.HarmonyUnpatch();

					CharaMakerPreviewSidebarToggle = null;
					LoadCharaExtdataToggle = null;
					LoadCoordinateExtdataToggle = null;
				};
				MakerAPI.MakerFinishedLoading += (sender, args) => InitInterface();

				Maker.OnCvsNavMenuClick += (sender, args) =>
				{
					if (args.TopIndex == 3)
					{
						if (CharaMakerPreview.Value)
							Instance.StartCoroutine(pluginCtrl.SyncAllAccToggleCoroutine());
					}
					else if (args.TopIndex == 4)
					{
						if (!args.Changed)
							return;
#if DEBUG
						Logger.LogError($"[OnCvsNavMenuClick][{args.Changed}]");
#endif
						if (args.SideToggle?.GetComponentInChildren<CvsAccessory>(true) == null)
							return;
						int Slot = (int) args.SideToggle?.GetComponentInChildren<CvsAccessory>(true)?.slotNo;
						Instance.StartCoroutine(pluginCtrl.AccSlotChangedHandlerCoroutine(Slot));
					}
				};
			}

			internal static void InitInterface()
			{
				AcsMoveWindow = GameObject.Find("04_AccessoryTop/AcsMoveWindow01").transform;
				imgWindowBack = AcsMoveWindow.Find("BasePanel/imgWindowBack").GetComponent<RectTransform>();
				grpParent = AcsMoveWindow.Find("grpParent");
				tglParent = GameObject.Find("tglSlot01/Slot01Top/tglResetColor")?.transform;
				if (tglParent == null)
					tglParent = GameObject.Find("tglSlot01/Slot01Top/tglSlot01ScrollView/Viewport/Content/tglResetColor")?.transform;
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
				sep.transform.name = "imgSeparete";
				sep.gameObject.SetActiveIfDifferent(true);

				// state toggles
				UI.AnchorOffsetMinY -= UI.MenuitemHeightOffsetY;
				for (int i = 0; i < 4; i++)
					CreateStateToggle(i);

				// dropdown
				UI.AnchorOffsetMinY -= UI.MenuitemHeightOffsetY;
				{
					Transform origin = GameObject.Find("06_SystemTop/tglConfig/ConfigTop/ddRamp").transform;
					Transform copy = Instantiate(origin, grpParent, false);
					copy.name = "ddASSList";
					copy.GetComponentInChildren<TextMeshProUGUI>().text = " ";
					RectTransform copyRt = copy.GetComponent<RectTransform>();
					copyRt.offsetMin = new Vector2(0, UI.AnchorOffsetMinY);
					copyRt.offsetMax = new Vector2(280, UI.AnchorOffsetMinY + UI.MenuitemHeightOffsetY);
					ddASSList = copy.gameObject;
					CreateDropdownList();
					CreateDropdownItems(-1);
				}

				// save button
				UI.AnchorOffsetMinY -= UI.MenuitemHeightOffsetY;
				{
					Transform origin = grpParent.Find("grpMove/X/btnReset");
					Transform copy = Instantiate(origin, grpParent, true);
					copy.transform.name = "btnASSsave";
					copy.GetComponentInChildren<TextMeshProUGUI>().text = "Save";
					RectTransform dropdownRt = grpParent.Find("ddASSList").GetComponent<RectTransform>();
					RectTransform copyRt = copy.GetComponent<RectTransform>();
					copyRt.offsetMin = new Vector2(copyRt.offsetMin.x, dropdownRt.offsetMin.y + 412); // -76
					copyRt.offsetMax = new Vector2(copyRt.offsetMax.x, dropdownRt.offsetMin.y + 412 - 380); // -456
					Button button = copy.GetComponentInChildren<Button>();
					button.targetGraphic.raycastTarget = true;
					button.onClick.RemoveAllListeners();
					button.onClick.AddListener(delegate ()
					{
						if (pluginCtrl.CurSlotTriggerInfo == null)
						{
							Logger.LogError($"[btnASSsave][{pluginCtrl.CharaFullName}] CurSlotTriggerInfo is Null");
							return;
						}

						int Slot = pluginCtrl.CurSlotTriggerInfo.Slot;
						ChaFileAccessory.PartsInfo PartInfo = chaCtrl.GetPartsInfo(Slot);
						if ((PartInfo == null) || (PartInfo.type == 120) || (pluginCtrl.CurSlotTriggerInfo.Kind == -1))
						{
							if (pluginCtrl.CurOutfitTriggerInfo.Parts.ContainsKey(Slot))
							{
								pluginCtrl.CurOutfitTriggerInfo.Parts.Remove(Slot);
								Logger.LogMessage($"AccTriggerInfo for Coordinate {pluginCtrl.CurrentCoordinateIndex} Slot{Slot + 1:00} has been reset");
							}
						}
						else
						{
							if (!pluginCtrl.CurOutfitTriggerInfo.Parts.ContainsKey(Slot))
								pluginCtrl.CurOutfitTriggerInfo.Parts[Slot] = new AccTriggerInfo(Slot);
							CopySlotTriggerInfo(pluginCtrl.CurSlotTriggerInfo, pluginCtrl.CurOutfitTriggerInfo.Parts[Slot]);

							if (pluginCtrl.CurSlotTriggerInfo.Kind >= 9)
							{
								if (pluginCtrl.CurSlotTriggerInfo.Group.IsNullOrEmpty())
									Logger.LogError($"[btnASSsave][{pluginCtrl.CharaFullName}] Group is Null");
								else
									pluginCtrl.CreateGroupIfNotExist(pluginCtrl.CurSlotTriggerInfo.Group);
							}
							Logger.LogMessage($"Slot{Slot + 1:00} updated");
						}

						pluginCtrl.SkipSlotChangePartTypeCheck = true;
						pluginCtrl.AccSlotChangedHandler(AccessoriesApi.SelectedMakerAccSlot);
					});
					copy.gameObject.SetActiveIfDifferent(true);
					btnASSsave = copy.gameObject;
				}
			}

			internal static void CreateDropdownList()
			{
				ddASSListDic.Clear();

				foreach (KeyValuePair<int, UI.DropdownASSList> item in Constants.DropdownASSList)
					ddASSListDic[item.Key] = new UI.DropdownASSList(item.Value.Index, item.Value.Kind, item.Value.Label, item.Value.States);

				int i = Constants.DropdownASSList.Count();
				List<VirtualGroupInfo> Filtered = pluginCtrl.CurOutfitVirtualGroupInfo.Values?.Where(x => x.Kind > 9)?.OrderBy(x => x.Kind)?.ToList();
				if (Filtered?.Count() > 0)
				{
					foreach (VirtualGroupInfo group in Filtered)
					{
						ddASSListDic[i] = new UI.DropdownASSList(i, group.Kind, group.Label, new List<bool>() { true, false, false, true });
						i++;
					}
				}
			}

			internal static void CreateDropdownItems(int i)
			{
				ddASSList.SetActiveIfDifferent(false);
				TMP_Dropdown dropdown = ddASSList.GetComponentInChildren<TMP_Dropdown>();
				dropdown.GetComponent<Image>().raycastTarget = true;

				dropdown.onValueChanged.RemoveAllListeners();
				dropdown.ClearOptions();
				dropdown.options.AddRange(ddASSListDic.Select(x => new TMP_Dropdown.OptionData(x.Value.Label)));
				dropdown.options.AddRange((new List<string>() { "+", "-" }).Select(x => new TMP_Dropdown.OptionData(x)));
				dropdown.value = i;
				dropdown.onValueChanged.AddListener(delegate (int value)
				{
#if DEBUG
					Logger.LogError($"Dropdown onValueChanged");
#endif
					AccTriggerInfo CurSlotTriggerInfo = pluginCtrl.CurSlotTriggerInfo;

					if (CurSlotTriggerInfo == null)
					{
						Logger.LogError($"[CreateDropdownItems][{pluginCtrl.CharaFullName}] AccessoriesApi.SelectedMakerAccSlot {AccessoriesApi.SelectedMakerAccSlot} out of range!!");
						return;
					}

					int j = dropdown.options.Count();
					if ((value == (j - 1)) || (value == (j - 2)))
					{
						if (value == (j - 2))
							pluginCtrl.PushGroup();
						else if (value == (j - 1))
							pluginCtrl.PopGroup();
						pluginCtrl.SkipSlotChangePartTypeCheck = true;
						pluginCtrl.AccSlotChangedHandler(CurSlotTriggerInfo.Slot);
						return;
					}

					UI.DropdownASSList item = ddASSListDic.Values.Where(x => x.Index == value).FirstOrDefault();
					if (item == null) return;
					CurSlotTriggerInfo.Kind = item.Kind;

					if (CurSlotTriggerInfo.Kind < 9)
						CurSlotTriggerInfo.Group = "";
					else if (CurSlotTriggerInfo.Kind == 9)
					{
						ChaFileAccessory.PartsInfo PartInfo = chaCtrl.GetPartsInfo(CurSlotTriggerInfo.Slot);
						CurSlotTriggerInfo.Group = PartInfo.parentKey;
					}
					else
						CurSlotTriggerInfo.Group = "custom_" + (CurSlotTriggerInfo.Kind - 9).ToString();

					pluginCtrl.OnCurSlotTriggerInfoChange();
				});
				ddASSList.SetActiveIfDifferent(true);
			}

			internal class StateToggleEventHandler : MonoBehaviour, IPointerClickHandler
			{
				internal int State;
				public void OnPointerClick(PointerEventData pointerEventData)
				{
					bool value = gameObject.GetComponentInChildren<Toggle>().isOn;
					pluginCtrl.CurSlotTriggerInfo.State[State] = value;
					pluginCtrl.OnCurSlotTriggerInfoChange();
				}
			}

			internal static void CreateStateToggle(int i)
			{
				Transform copy = Instantiate(tglParent, grpParent, false);
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

				toggle.gameObject.AddComponent<StateToggleEventHandler>().State = i;

				copy.gameObject.SetActiveIfDifferent(true);
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

			internal class VirtualGroupToggleEventHandler : MonoBehaviour, IPointerClickHandler
			{
				internal string Group;
				public void OnPointerClick(PointerEventData pointerEventData)
				{
					bool value = gameObject.GetComponentInChildren<Toggle>().isOn;
					pluginCtrl.OnVirtualGroupStateChange(Group, value);
				}
			}

			internal static void CreateVirtualGroupToggle(string group)
			{
				Transform copy = Instantiate(tglParent, grpParent, false);
				copy.name = "tglASS_" + group;
				RectTransform copyRt = copy.GetComponent<RectTransform>();
				copyRt.offsetMin = new Vector2(0, UI.AnchorOffsetMinY);
				copyRt.offsetMax = new Vector2(0, UI.AnchorOffsetMinY + UI.MenuitemHeightOffsetY);

				string label = pluginCtrl.CurOutfitVirtualGroupInfo[group].Label;
				if (VirtualGroupShowName.Value)
					label = label + " (" + group +  ")";
				copy.GetComponentInChildren<TextMeshProUGUI>().text = label;

				Toggle toggle = copy.GetComponentInChildren<Toggle>();
				toggle.onValueChanged.RemoveAllListeners();
				toggle.image.raycastTarget = true;
				toggle.graphic.raycastTarget = true;
				toggle.isOn = pluginCtrl.CurOutfitVirtualGroupInfo[group].State;

				toggle.gameObject.AddComponent<VirtualGroupToggleEventHandler>().Group = group;

				copy.gameObject.SetActiveIfDifferent(true);
				tglASSgroup["tglASS_" + group] = copy.gameObject;
				UI.AnchorOffsetMinY -= UI.MenuitemHeightOffsetY;
			}

			internal static void PatchMakerToggles()
			{
				Toggle[] toggles = CustomBase.Instance.customCtrl.cmpDrawCtrl.Field<Toggle[]>("tglShowAccessory");
				{
					toggles[0].onValueChanged.RemoveAllListeners();
					toggles[0].onValueChanged.AddListener(value =>
					{
						pluginCtrl.SetAccessoryStateCategory(0, value);
					});
				}
				{
					toggles[1].onValueChanged.RemoveAllListeners();
					toggles[1].onValueChanged.AddListener(value =>
					{
						pluginCtrl.SetAccessoryStateCategory(1, value);
					});
				}
			}
		}
	}
}
