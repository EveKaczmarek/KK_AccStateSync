using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		public static bool InsideHScene = false;

		private const string objPathSingle = "Canvas/SubMenu/DressCategory/Accessory/AccessoryGroup/AccessoryAllCategory";
		private const string objPathDouble1st = "Canvas/SubMenu/SecondDressCategory/First/Group/Accessory/AccessoryGroup/AccessoryAllCategory";
		private const string objPathDouble2nd = "Canvas/SubMenu/SecondDressCategory/Second/Group/Accessory/AccessoryGroup/AccessoryAllCategory";
		private const string objPathHorigin = "Canvas/SubMenu/DressCategory/Accessory/AccessoryGroup/AccessoryCategory/1";

		internal static List<ChaControl> HSceneHeroine;

		internal static void UpdateHUI()
		{
			if (!InsideHScene) return;
			Logger.Log(DebugLogLevel, $"[UpdateHUI] Fired!!");

			ContainerOffsetMinY = -144;
			MenuitemHeightOffsetY = -24;

			if (HSceneHeroine.Count() == 2)
			{
				ClearFreeHbutton(GameObject.Find(objPathDouble1st));
				ClearFreeHbutton(GameObject.Find(objPathDouble2nd));
			}
			else
				ClearFreeHbutton(GameObject.Find(objPathSingle));

			int i = 0, Heroine = 0;
			foreach (ChaControl chaCtrl in HSceneHeroine)
			{
				AccStateSyncController controller = GetController(chaCtrl);
				foreach (KeyValuePair<string, bool> group in controller.VirtualGroupStates)
				{
					CreateFreeHbutton(chaCtrl, Heroine, group.Key, i);
					i ++;
				}
				i = 0;
				Heroine ++;
			}
		}

		internal static void ClearFreeHbutton(GameObject parent)
		{
			List<string> whitelist = new List<string>() { "Clothing", "Undressing", "Category1Clothing", "Category1Undressing", "Category2Clothing", "Category2Undressing" };
			foreach (Transform child in parent.transform)
			{
				if (whitelist.IndexOf(child.name) == -1)
					Object.Destroy(child.gameObject);
			}
		}

		internal static void CreateFreeHbutton(ChaControl chaCtrl, int Heroine, string group, int i)
		{
			GameObject parent;
			if (HSceneHeroine.Count() == 2)
				parent = (Heroine == 0) ? GameObject.Find(objPathDouble1st) : GameObject.Find(objPathDouble2nd);
			else
				parent = GameObject.Find(objPathSingle);

			Transform origin = GameObject.Find(objPathHorigin).transform;
			Transform copy = Object.Instantiate(origin.transform, parent.transform, false);
			copy.GetComponentInChildren<TextMeshProUGUI>().text = (AccParentNames.ContainsKey(group)) ? AccParentNames[group] : group;

			RectTransform copyRt = copy.GetComponent<RectTransform>();
			copyRt.offsetMin = new Vector2(0, ContainerOffsetMinY + (MenuitemHeightOffsetY * (i + 1))); // -168
			copyRt.offsetMax = new Vector2(112, ContainerOffsetMinY + (MenuitemHeightOffsetY * i)); // -144
			copyRt.transform.name = $"btnASS_{Heroine}_{group}";

			Button button = copy.GetComponentInChildren<Button>();
			button.onClick.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.Off);
			button.onClick.RemoveAllListeners();
			button.onClick = new Button.ButtonClickedEvent();
			button.image.raycastTarget = true;

			button.onClick.AddListener(() =>
			{
				AccStateSyncController pluginCtrl = GetController(chaCtrl);
				bool show = !pluginCtrl.VirtualGroupStates[group];
				pluginCtrl.ToggleByVirtualGroup(group, show);
				Illusion.Game.Utils.Sound.Play(Illusion.Game.SystemSE.sel);
			});

			copy.gameObject.SetActive(true);
		}
	}
}
