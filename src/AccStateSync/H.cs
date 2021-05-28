using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using ParadoxNotion.Serialization;

using BepInEx.Logging;

using JetPack;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		internal static class CharaHscene
		{
			internal static int MenuitemHeightOffsetY = 0;
			internal static int AnchorOffsetMinY = 0;
			internal static int ContainerOffsetMinY = 0;

			internal static void RegisterEvents()
			{
				JetPack.CharaHscene.OnHSceneFinishedLoading += (_sender, _args) =>
				{
					ClearUI();
					UpdateUI();
				};
				JetPack.CharaHscene.OnHSceneSetClothStateStartMotion += (_sender, _args) =>
				{
					foreach (ChaControl _chaCtrl in _args.Female)
					{
						DebugMsg(LogLevel.Info, $"[OnHSceneSetClothStateStartMotion][{_chaCtrl.GetFullName()}]");
						AccStateSyncController _pluginCtrl = GetController(_chaCtrl);
						if (_pluginCtrl != null)
						{
							foreach (TriggerGroup _group in _pluginCtrl.TriggerGroupList)
								_group.State = (_group.Secondary > -1) ? _group.Secondary : _group.Startup;
							_pluginCtrl.RefreshCache();
							_pluginCtrl.SyncAllAccToggle("OnHSceneSetClothStateStartMotion");

							var data = _pluginCtrl.TriggerGroupList.ToList();
							string json = JSONSerializer.Serialize(data.GetType(), data, true);
							_logger.LogWarning("\n" + json);
						}
					}
				};
			}

			internal static void UpdateUI()
			{
				if (!JetPack.CharaHscene.Loaded) return;
				DebugMsg(LogLevel.Info, $"[UpdateUI] Fired!!");

				ContainerOffsetMinY = -144;
				MenuitemHeightOffsetY = -24;

				int i = 0, _counter = 0;
				foreach (ChaControl _chaCtrl in JetPack.CharaHscene.Heroine)
				{
					AccStateSyncController _pluginCtrl = GetController(_chaCtrl);
					if (_pluginCtrl != null)
					{
						_pluginCtrl.RefreshCache();
						if (_pluginCtrl._cachedCoordinateGroupList.Count > 0)
						{
							foreach (TriggerGroup _group in _pluginCtrl._cachedCoordinateGroupList)
							{
								if (_pluginCtrl._cachedCoordinateGroupPropertyList.Where(x => x.RefKind == _group.Kind).Count() > 0)
								{
									CreateButton(_chaCtrl, _counter, i, _group);
									i++;
								}
							}
							i = 0;
						}
					}
					_counter++;
				}
			}

			internal static void ClearUI()
			{
				foreach (HSprite _sprite in JetPack.CharaHscene.Sprites)
				{
					if (JetPack.CharaHscene.Heroine.Count > 1)
					{
						ClearButton(_sprite.lstMultipleFemaleDressButton[0].accessoryAll.gameObject);
						ClearButton(_sprite.lstMultipleFemaleDressButton[1].accessoryAll.gameObject);
					}
					else
						ClearButton(_sprite.categoryAccessoryAll.gameObject);
				}
			}

			internal static readonly List<string> _whitelist = new List<string>() { "Clothing", "Undressing", "Category1Clothing", "Category1Undressing", "Category2Clothing", "Category2Undressing" };

			internal static void ClearButton(GameObject _parent)
			{
				foreach (Transform _child in _parent.transform)
				{
					if (_whitelist.IndexOf(_child.name) < 0)
						Destroy(_child.gameObject);
				}
			}

			internal static void CreateButton(ChaControl _chaCtrl, int _counter, int i, TriggerGroup _group)
			{
				foreach (HSprite _sprite in JetPack.CharaHscene.Sprites)
				{
					Transform _parent;
					if (JetPack.CharaHscene.Heroine.Count > 1)
						_parent = _sprite.lstMultipleFemaleDressButton[_counter].accessoryAll.transform;
					else
						_parent = _sprite.categoryAccessoryAll.transform;

					Transform _origin = _sprite.categoryAccessory.lstButton[0].transform;
					Transform _copy = Instantiate(_origin.transform, _parent, false);
					_copy.name = $"btnASS_{_counter}_{_group.Kind}";

					AccStateSyncController _pluginCtrl = GetController(_chaCtrl);
					_copy.GetComponentInChildren<TextMeshProUGUI>().text = _group.Label;

					RectTransform _copyRt = _copy.GetComponent<RectTransform>();
					_copyRt.offsetMin = new Vector2(0, ContainerOffsetMinY + (MenuitemHeightOffsetY * (i + 1))); // -168 
					_copyRt.offsetMax = new Vector2(112, ContainerOffsetMinY + (MenuitemHeightOffsetY * i)); // -144 

					Button _button = _copy.GetComponentInChildren<Button>();
					for (int n = 0; n < _button.onClick.GetPersistentEventCount(); n++)
						_button.onClick.SetPersistentListenerState(n, UnityEventCallState.Off);
					_button.onClick.RemoveAllListeners();
					_button.onClick = new Button.ButtonClickedEvent();
					_button.image.raycastTarget = true;

					_button.onClick.AddListener(delegate ()
					{
						_pluginCtrl.SetGroupStateNext(_group.Kind);
						Illusion.Game.Utils.Sound.Play(Illusion.Game.SystemSE.sel);
					});

					_copy.gameObject.SetActiveIfDifferent(true);
				}
			}
		}
	}
}
