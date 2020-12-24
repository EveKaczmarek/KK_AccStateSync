using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;
using Studio;
using UniRx;

using KKAPI.Studio;
using JetPack;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		internal static class CharaStudio
		{
			internal static bool Inside = false;
			internal static OCIChar CurOCIChar = null;
			internal static int CurTreeNodeObjID = -1;
			internal static GameObject ASSPanel;
			internal static GameObject OriginalCopy;
			internal static CanvasGroup ASSPanelCanvasGroup;
			internal static Button StateButton;

			internal static void RegisterControls()
			{
				if (!StudioAPI.InsideStudio) return;

				Inside = true;
				CreatePanel();
				ExtensibleSaveFormat.ExtendedSave.SceneBeingLoaded += (path) => CurTreeNodeObjID = -1;
				ExtensibleSaveFormat.ExtendedSave.SceneBeingImported += (path) => CurTreeNodeObjID = -1;

				JetPack.Studio.OnSelectNodeChange += (sender, args) =>
				{
					CurTreeNodeObjID = JetPack.Studio.CurTreeNodeObjID;
					CurOCIChar = JetPack.Studio.CurOCIChar;
					UpdateUI();
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

				if ((CurOCIChar == null) || (CurOCIChar.charInfo == null))
					return;
				AccStateSyncController pluginCtrl = CurOCIChar.charInfo.GetComponent<AccStateSyncController>();
				if (pluginCtrl == null)
					return;
				pluginCtrl.TreeNodeObjID = CurTreeNodeObjID;
				if (!pluginCtrl.TriggerEnabled)
					return;
				if (pluginCtrl.CurOutfitVirtualGroupInfo.Count() == 0)
					return;
				int i = 0;

				Dictionary<string, VirtualGroupInfo> VirtualGroupInfo = pluginCtrl.CurOutfitVirtualGroupInfo;
				foreach (KeyValuePair<string, VirtualGroupInfo> group in VirtualGroupInfo)
				{
					if (pluginCtrl.GetPartsOfGroup(group.Key).Count() > 0)
					{
						CreateUIText(group.Key, i, group.Value.Label);
						CreateUIToggle(group.Key, i, group.Value.State);
						i++;
					}
				}
				SetVisibility(i > 0);
			}

			internal static void CreatePanel()
			{
				UI.ContainerOffsetMinY = -45;
				UI.MenuitemHeightOffsetY = -25;

				OriginalCopy = GameObject.Find("StudioScene/Canvas Main Menu/02_Manipulate/00_Chara/02_Kinematic/00_FK");
				GameObject parent = GameObject.Find("StudioScene/Canvas Main Menu/02_Manipulate/00_Chara/01_State");

				Transform copy = Instantiate(OriginalCopy.transform, parent.transform, true);
				copy.transform.name = "AccStateSync";

				int shiftX = 124;

				RectTransform copyRt = copy.GetComponent<RectTransform>();
				copyRt.offsetMin = new Vector2(copyRt.offsetMin.x + shiftX, copyRt.offsetMin.y);
				copyRt.offsetMax = new Vector2(copyRt.offsetMax.x + shiftX, copyRt.offsetMax.y);

				ASSPanel = copy.gameObject;
				ASSPanel.SetActiveIfDifferent(true);
				ASSPanelCanvasGroup = ASSPanel.GetOrAddComponent<CanvasGroup>();

				StateButton = GameObject.Find("StudioScene/Canvas Main Menu/02_Manipulate/00_Chara/00_Root/Viewport/Content/State").GetComponentInChildren<Button>();

				ClearUI();
			}

			internal static T GetPanelObject<T>(string name) => OriginalCopy.transform.GetComponentsInChildren<RectTransform>(true).First(x => x.name == name).GetComponent<T>();

			internal static void CreateUIText(string name, int i, string text)
			{
				Text txt = Instantiate(GetPanelObject<Text>("Text Function"), ASSPanel.transform);
				txt.name = name;
				txt.text = text;
				txt.transform.localPosition = new Vector3(txt.transform.localPosition.x + 40, UI.ContainerOffsetMinY + (UI.MenuitemHeightOffsetY * i), txt.transform.localPosition.z);
			}

			internal static void CreateUIToggle(string name, int i, bool show)
			{
				Toggle toggle = Instantiate(GetPanelObject<Toggle>("Toggle Function"), ASSPanel.transform);
				toggle.name = name;
				toggle.isOn = show;
				toggle.transform.localPosition = new Vector3(toggle.transform.localPosition.x - 75, UI.ContainerOffsetMinY + (UI.MenuitemHeightOffsetY * i), toggle.transform.localPosition.z);
				toggle.onValueChanged.RemoveAllListeners();
				toggle.onValueChanged.AddListener(value =>
				{
					AccStateSyncController pluginCtrl = CurOCIChar.charInfo.GetComponent<AccStateSyncController>();
					if (pluginCtrl == null) return;
					pluginCtrl.OnVirtualGroupStateChange(name, value);
					StateButton.onClick.Invoke();
					MoreAccessories.UpdateUI();
				});
			}
		}
	}
}
