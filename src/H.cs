using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		internal static class HScene
		{
			internal static bool Inside = false;
			internal static List<ChaControl> Heroine;
			internal static List<HSprite> Sprites = new List<HSprite>();

			internal static void UpdateUI()
			{
				if (!Inside) return;
				Logger.Log(DebugLogLevel, $"[UpdateHUI] Fired!!");

				UI.ContainerOffsetMinY = -144;
				UI.MenuitemHeightOffsetY = -24;

				int i = 0, Counter = 0;
				foreach (ChaControl chaCtrl in Heroine)
				{
					AccStateSyncController controller = GetController(chaCtrl);
					if (!controller.TriggerEnabled)
						continue;
					if (controller.CurOutfitVirtualGroupInfo.Count() == 0)
						continue;
					OutfitTriggerInfo CurOutfitTriggerInfo = controller.CurOutfitTriggerInfo;
					Dictionary<string, VirtualGroupInfo> VirtualGroupInfo = controller.CurOutfitVirtualGroupInfo;
					foreach (KeyValuePair<string, VirtualGroupInfo> group in VirtualGroupInfo)
					{
						if (CurOutfitTriggerInfo?.Parts?.Values?.Where(x => x.Kind == group.Value.Kind)?.Count() > 0)
						{
							CreateButton(chaCtrl, Counter, group.Key, i);
							i++;
						}
					}
					i = 0;
					Counter++;
				}
			}

			internal static void ClearUI()
			{
				foreach (HSprite sprite in Sprites)
				{
					if (Heroine.Count() == 2)
					{
						ClearButton(sprite.lstMultipleFemaleDressButton[0].accessoryAll.gameObject);
						ClearButton(sprite.lstMultipleFemaleDressButton[1].accessoryAll.gameObject);
					}
					else
						ClearButton(sprite.categoryAccessoryAll.gameObject);
				}
			}

			internal static void ClearButton(GameObject parent)
			{
				List<string> whitelist = new List<string>() { "Clothing", "Undressing", "Category1Clothing", "Category1Undressing", "Category2Clothing", "Category2Undressing" };
				foreach (Transform child in parent.transform)
				{
					if (whitelist.IndexOf(child.name) == -1)
						Destroy(child.gameObject);
				}
			}

			internal static void CreateButton(ChaControl chaCtrl, int Counter, string group, int i)
			{
				foreach (HSprite sprite in Sprites)
				{
					Transform parent;
					if (Heroine.Count() == 2)
						parent = (Counter == 0) ? sprite.lstMultipleFemaleDressButton[0].accessoryAll.transform : sprite.lstMultipleFemaleDressButton[1].accessoryAll.transform;
					else
						parent = sprite.categoryAccessoryAll.transform;

					Transform origin = sprite.categoryAccessory.lstButton[0].transform;
					Transform copy = Instantiate(origin.transform, parent, false);

					AccStateSyncController pluginCtrl = GetController(chaCtrl);
					string label = pluginCtrl.CurOutfitVirtualGroupInfo[group].Label;
					copy.GetComponentInChildren<TextMeshProUGUI>().text = label;

					RectTransform copyRt = copy.GetComponent<RectTransform>();
					copyRt.offsetMin = new Vector2(0, UI.ContainerOffsetMinY + (UI.MenuitemHeightOffsetY * (i + 1))); // -168
					copyRt.offsetMax = new Vector2(112, UI.ContainerOffsetMinY + (UI.MenuitemHeightOffsetY * i)); // -144
					copyRt.transform.name = $"btnASS_{Counter}_{group}";

					Button button = copy.GetComponentInChildren<Button>();
					button.onClick.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.Off);
					button.onClick.RemoveAllListeners();
					button.onClick = new Button.ButtonClickedEvent();
					button.image.raycastTarget = true;

					button.onClick.AddListener(delegate ()
					{
						bool show = !pluginCtrl.CurOutfitVirtualGroupInfo[group].State;
						pluginCtrl.ToggleByVirtualGroup(group, show);
						Illusion.Game.Utils.Sound.Play(Illusion.Game.SystemSE.sel);
					});

					copy.gameObject.SetActive(true);
				}
			}
		}
	}
}
