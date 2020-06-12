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
		public static List<ChaControl> HSceneHeroine;
		public static List<HSprite> HSprites = new List<HSprite>();

		internal static void UpdateHUI()
		{
			if (!InsideHScene) return;
			Logger.Log(DebugLogLevel, $"[UpdateHUI] Fired!!");

			ContainerOffsetMinY = -144;
			MenuitemHeightOffsetY = -24;

			foreach(HSprite sprite in HSprites)
			{
				if (HSceneHeroine.Count() == 2)
				{
					ClearFreeHbutton(sprite.lstMultipleFemaleDressButton[0].accessoryAll.gameObject);
					ClearFreeHbutton(sprite.lstMultipleFemaleDressButton[1].accessoryAll.gameObject);
				}
				else
					ClearFreeHbutton(sprite.categoryAccessoryAll.gameObject);
			}

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
					Destroy(child.gameObject);
			}
		}

		internal static void CreateFreeHbutton(ChaControl chaCtrl, int Heroine, string group, int i)
		{
			foreach (HSprite sprite in HSprites)
			{
				Transform parent;
				if (HSceneHeroine.Count() == 2)
					parent = (Heroine == 0) ? sprite.lstMultipleFemaleDressButton[0].accessoryAll.transform : sprite.lstMultipleFemaleDressButton[1].accessoryAll.transform;
				else
					parent = sprite.categoryAccessoryAll.transform;

				Transform origin = sprite.categoryAccessory.lstButton[0].transform;
				Transform copy = Instantiate(origin.transform, parent, false);

				AccStateSyncController pluginCtrl = GetController(chaCtrl);
				string label = group;
				if (AccParentNames.ContainsKey(group))
					label = AccParentNames[group];
				else if (pluginCtrl.CurOutfitVirtualGroupNames.ContainsKey(group))
					label = pluginCtrl.CurOutfitVirtualGroupNames[group];
				copy.GetComponentInChildren<TextMeshProUGUI>().text = label;

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
					bool show = !pluginCtrl.VirtualGroupStates[group];
					pluginCtrl.ToggleByVirtualGroup(group, show);
					Illusion.Game.Utils.Sound.Play(Illusion.Game.SystemSE.sel);
				});

				copy.gameObject.SetActive(true);
			}
		}
	}
}
