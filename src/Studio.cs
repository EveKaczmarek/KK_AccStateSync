using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Studio;
using UniRx;
using KKAPI.Studio;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		private void Update()
		{
			CharaStudio.CheckSelectionChange();
		}

		internal static class CharaStudio
		{
			internal static bool Inside = false;
			internal static OCIChar CurOCIChar;
			internal static int CurTreeNodeObjID = -1;
			internal static GameObject ASSPanel;
			internal static CanvasGroup ASSPanelCanvasGroup;

			internal static void CheckSelectionChange()
			{
				if (Inside)
				{
					TreeNodeObject treeNodeObject = Studio.Studio.Instance.treeNodeCtrl.selectNode;
					if (treeNodeObject != null)
					{
						if (Studio.Studio.Instance.dicInfo.TryGetValue(treeNodeObject, out ObjectCtrlInfo info))
						{
							OCIChar selected = info as OCIChar;
							if (selected != CurOCIChar)
							{
								CurOCIChar = selected;
								CurTreeNodeObjID = StudioObjectExtensions.GetSceneId(info);
#if DEBUG
								Logger.LogWarning($"[CheckSelectionChange][CurTreeNodeObjID: {CurTreeNodeObjID}]");
#endif
								if (CurOCIChar?.GetType().ToString() != null)
								{
									AccStateSyncController controller = CurOCIChar?.charInfo?.GetComponent<AccStateSyncController>();
									if (controller != null)
										controller.TreeNodeObjID = CurTreeNodeObjID;

									UpdateUI();
								}
							}
						}
					}
				}
			}

			internal static void RegisterControls()
			{
				if (!StudioAPI.InsideStudio) return;

				Inside = true;
				CreatePanel();
				ExtensibleSaveFormat.ExtendedSave.SceneBeingLoaded += (path) =>
				{
					CurTreeNodeObjID = -1;
				};
				ExtensibleSaveFormat.ExtendedSave.SceneBeingImported += (path) =>
				{
					CurTreeNodeObjID = -1;
				};
			}

			internal static void SetVisibility(bool visible)
			{
				if (ASSPanelCanvasGroup == null) return;
				ASSPanelCanvasGroup.alpha = visible ? 1 : 0;
				ASSPanelCanvasGroup.blocksRaycasts = visible;
			}

			internal static void ClearUI()
			{
				foreach (Transform child in ASSPanel.transform)
				{
					if (child.gameObject != null)
						Destroy(child.gameObject);
				}
				SetVisibility(false);
			}

			internal static void UpdateUI()
			{
				ClearUI();

				AccStateSyncController controller = CurOCIChar?.charInfo?.GetComponent<AccStateSyncController>();
				if (controller == null)
					return;
				if (!controller.TriggerEnabled)
					return;
				if (controller.CurOutfitVirtualGroupInfo.Count() == 0)
					return;
				int i = 0;
				OutfitTriggerInfo CurOutfitTriggerInfo = controller.CurOutfitTriggerInfo;
				Dictionary<string, VirtualGroupInfo> VirtualGroupInfo = controller.CurOutfitVirtualGroupInfo;
				foreach (KeyValuePair<string, VirtualGroupInfo> group in VirtualGroupInfo)
				{
					if (CurOutfitTriggerInfo?.Parts?.Values?.Where(x => x.Kind == group.Value.Kind)?.Count() > 0)
					{
						CreateUIText(group.Key, i, group.Value.Label);
						CreateUIToggle(group.Key, i, group.Value.State);
						i++;
					}
				}
				if (i > 0)
					SetVisibility(true);
			}

			internal static void CreatePanel()
			{
				UI.ContainerOffsetMinY = -45;
				UI.MenuitemHeightOffsetY = -25;

				GameObject origin = GameObject.Find("StudioScene/Canvas Main Menu/02_Manipulate/00_Chara/02_Kinematic/00_FK");
				GameObject parent = GameObject.Find("StudioScene/Canvas Main Menu/02_Manipulate/00_Chara/01_State");

				Transform copy = Instantiate(origin.transform, parent.transform, true);
				RectTransform copyRt = copy.GetComponent<RectTransform>();
				copyRt.transform.name = "AccStateSync";

				int shiftX = 124;

				copyRt.offsetMin = new Vector2(copyRt.offsetMin.x + shiftX, copyRt.offsetMin.y);
				copyRt.offsetMax = new Vector2(copyRt.offsetMax.x + shiftX, copyRt.offsetMax.y);

				ASSPanel = copy.gameObject;
				ASSPanel.SetActive(true);
				ASSPanelCanvasGroup = ASSPanel.GetOrAddComponent<CanvasGroup>();

				ClearUI();
			}

			public static T GetPanelObject<T>(string name) => GameObject.Find("StudioScene/Canvas Main Menu/02_Manipulate/00_Chara/02_Kinematic/00_FK").transform.GetComponentsInChildren<RectTransform>(true).First(x => x.name == name).GetComponent<T>();

			internal static void CreateUIText(string name, int i, string text)
			{
				Text txt = Instantiate(GetPanelObject<Text>("Text Function"), ASSPanel.transform);
				txt.name = name;
				txt.text = text;
				txt.transform.localPosition = new Vector3(txt.transform.localPosition.x + 40, UI.ContainerOffsetMinY + (UI.MenuitemHeightOffsetY * i), txt.transform.localPosition.z);
			}

			internal static void CreateUIToggle(string name, int i, bool show)
			{
				Toggle tglNew = Instantiate(GetPanelObject<Toggle>("Toggle Function"), ASSPanel.transform);
				tglNew.name = name;
				tglNew.isOn = show;
				tglNew.transform.localPosition = new Vector3(tglNew.transform.localPosition.x - 75, UI.ContainerOffsetMinY + (UI.MenuitemHeightOffsetY * i), tglNew.transform.localPosition.z);
				tglNew.onValueChanged.RemoveAllListeners();
				tglNew.onValueChanged.AddListener(delegate (bool value)
				{
					AccStateSyncController controller = CurOCIChar.charInfo.GetComponent<AccStateSyncController>();
					if (controller == null)
						return;
					controller.ToggleByVirtualGroup(name, value);
					GameObject.Find("StudioScene/Canvas Main Menu/02_Manipulate/00_Chara/00_Root/Viewport/Content/State").GetComponentInChildren<Button>().onClick.Invoke();
					MoreAccessories_Support.UpdateUI();
				});
			}
		}
	}
}
