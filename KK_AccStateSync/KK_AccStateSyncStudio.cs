using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Studio;
using UniRx;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		public static bool InsideCharaStudio = false;
		public static OCIChar CurOCIChar;
		public static GameObject ASSPanel = null;

		void Update()
		{
			if (InsideCharaStudio)
			{
				TreeNodeObject treeNodeObject = Studio.Studio.Instance.treeNodeCtrl.selectNode;
				if (treeNodeObject != null)
				{
					ObjectCtrlInfo info;
					if (Studio.Studio.Instance.dicInfo.TryGetValue(treeNodeObject, out info))
					{
						OCIChar selected = info as OCIChar;
						if (selected != CurOCIChar)
						{
							CurOCIChar = selected;

//							Logger.LogWarning($"{StudioObjectExtensions.GetSceneId(info)}");
							if (CurOCIChar?.GetType().ToString() != null)
								UpdateStudioUI();
						}
					}
				}
			}
		}

		internal static void ClearStudioUI()
		{
			foreach (Transform child in ASSPanel.transform)
				Object.Destroy(child.gameObject);
		}

		internal static void UpdateStudioUI()
		{
			ClearStudioUI();

			AccStateSyncController controller = CurOCIChar.charInfo.GetComponent<AccStateSyncController>();
			if (controller == null)
				return;

			int i = 0;
			Dictionary<string, bool> VirtualGroupStates = controller.VirtualGroupStates;
			foreach (KeyValuePair<string, bool> group in VirtualGroupStates)
			{
				string label = (AccParentNames.ContainsKey(group.Key)) ? AccParentNames[group.Key] : group.Key;
				CreateStudioUIText(group.Key, i, label);
				CreateStudioUIToggle(group.Key, i, group.Value);
				i++;
			}
		}

		internal static void CreateStudioUIPanel()
		{
			ContainerOffsetMinY = -45;
			MenuitemHeightOffsetY = -25;

			GameObject origin = GameObject.Find("StudioScene/Canvas Main Menu/02_Manipulate/00_Chara/02_Kinematic/00_FK");
			GameObject parent = GameObject.Find("StudioScene/Canvas Main Menu/02_Manipulate/00_Chara/01_State");

			Transform copy = Object.Instantiate(origin.transform, parent.transform, true);
			RectTransform copyRt = copy.GetComponent<RectTransform>();
			copyRt.transform.name = "AccStateSync";

			int shiftX = 124;

			copyRt.offsetMin = new Vector2(copyRt.offsetMin.x + shiftX, copyRt.offsetMin.y);
			copyRt.offsetMax = new Vector2(copyRt.offsetMax.x + shiftX, copyRt.offsetMax.y);

			ASSPanel = copy.gameObject;
			ASSPanel.SetActive(true);
			ClearStudioUI();
		}

		public static T GetPanelObject<T>(string name) => GameObject.Find("StudioScene/Canvas Main Menu/02_Manipulate/00_Chara/02_Kinematic/00_FK").transform.GetComponentsInChildren<RectTransform>(true).First(x => x.name == name).GetComponent<T>();

		internal static void CreateStudioUIText(string name, int i, string text)
		{
			Text txt = Instantiate(GetPanelObject<Text>("Text Function"), ASSPanel.transform);
			txt.name = name;
			txt.text = text;
			txt.transform.localPosition = new Vector3(txt.transform.localPosition.x + 40, ContainerOffsetMinY + (MenuitemHeightOffsetY * i), txt.transform.localPosition.z);
		}

		internal static void CreateStudioUIToggle(string name, int i, bool show)
		{
			Toggle tglNew = Instantiate(GetPanelObject<Toggle>("Toggle Function"), ASSPanel.transform);
			tglNew.name = name;
			tglNew.isOn = show;
			tglNew.transform.localPosition = new Vector3(tglNew.transform.localPosition.x - 75, ContainerOffsetMinY + (MenuitemHeightOffsetY * i), tglNew.transform.localPosition.z);
			tglNew.onValueChanged.RemoveAllListeners();
			tglNew.onValueChanged.AddListener(delegate (bool value)
			{
				AccStateSyncController controller = CurOCIChar.charInfo.GetComponent<AccStateSyncController>();
				if (controller == null)
					return;
				controller.ToggleByVirtualGroup(name, value);
			});
		}
	}
}
