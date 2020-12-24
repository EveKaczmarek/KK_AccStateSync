using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;
using TMPro;

using JetPack;

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
				Logger.Log(DebugLogLevel, $"[UpdateUI] Fired!!");

				UI.ContainerOffsetMinY = -144;
				UI.MenuitemHeightOffsetY = -24;

				int i = 0, Counter = 0;
				foreach (ChaControl chaCtrl in Heroine)
				{
					AccStateSyncController pluginCtrl = GetController(chaCtrl);
					if ((pluginCtrl.TriggerEnabled) &&  (pluginCtrl.CurOutfitVirtualGroupInfo.Count() > 0))
					{
						foreach (KeyValuePair<string, VirtualGroupInfo> group in pluginCtrl.CurOutfitVirtualGroupInfo)
						{
							if (pluginCtrl.GetPartsOfKind(group.Value.Kind).Count() > 0)
							{
								CreateButton(chaCtrl, Counter, group.Key, i);
								i++;
							}
						}
						i = 0;
					}
					Counter++;
				}
			}

			internal static void ClearUI()
			{
				foreach (HSprite sprite in Sprites)
				{
					if (Heroine.Count > 1)
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
					if (whitelist.IndexOf(child.name) < 0)
						Destroy(child.gameObject);
				}
			}

			internal static void CreateButton(ChaControl chaCtrl, int Counter, string group, int i)
			{
				foreach (HSprite sprite in Sprites)
				{
					Transform parent;
					if (Heroine.Count > 1)
						parent = sprite.lstMultipleFemaleDressButton[Counter].accessoryAll.transform;
					else
						parent = sprite.categoryAccessoryAll.transform;

					Transform origin = sprite.categoryAccessory.lstButton[0].transform;
					Transform copy = Instantiate(origin.transform, parent, false);
					copy.name = $"btnASS_{Counter}_{group}";

					AccStateSyncController pluginCtrl = GetController(chaCtrl);
					string label = pluginCtrl.CurOutfitVirtualGroupInfo[group].Label;
					copy.GetComponentInChildren<TextMeshProUGUI>().text = label;

					RectTransform copyRt = copy.GetComponent<RectTransform>();
					copyRt.offsetMin = new Vector2(0, UI.ContainerOffsetMinY + (UI.MenuitemHeightOffsetY * (i + 1))); // -168 
					copyRt.offsetMax = new Vector2(112, UI.ContainerOffsetMinY + (UI.MenuitemHeightOffsetY * i)); // -144 

					Button button = copy.GetComponentInChildren<Button>();
					button.onClick.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.Off);
					button.onClick.RemoveAllListeners();
					button.onClick = new Button.ButtonClickedEvent();
					button.image.raycastTarget = true;

					button.onClick.AddListener(delegate ()
					{
						bool show = !pluginCtrl.CurOutfitVirtualGroupInfo[group].State;
						pluginCtrl.OnVirtualGroupStateChange(group, show);
						Illusion.Game.Utils.Sound.Play(Illusion.Game.SystemSE.sel);
					});

					copy.gameObject.SetActiveIfDifferent(true);
				}
			}
		}
	}
}
