using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

using BepInEx.Logging;
using HarmonyLib;

using JetPack;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		internal static class CharaHscene
		{
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
							if (_cfgAutoHideSecondary.Value)
							{
								for (int i = 0; i < 7; i++)
								{
									List<string> _secondary = _pluginCtrl.CharaVirtualGroupInfo[i].Values?.Where(x => x.Secondary)?.Select(x => x.Group)?.ToList();
									foreach (string _group in _secondary)
										_pluginCtrl.CharaVirtualGroupInfo[i][_group].State = false;
								}
							}
							_pluginCtrl.SyncAllAccToggle();
						}
					}
				};
			}

			internal static void UpdateUI()
			{
				if (!JetPack.CharaHscene.Loaded) return;
				DebugMsg(LogLevel.Info, $"[UpdateUI] Fired!!");

				UI.ContainerOffsetMinY = -144;
				UI.MenuitemHeightOffsetY = -24;

				int i = 0, _counter = 0;
				foreach (ChaControl _chaCtrl in JetPack.CharaHscene.Heroine)
				{
					AccStateSyncController _pluginCtrl = GetController(_chaCtrl);
					if (_pluginCtrl.TriggerEnabled && _pluginCtrl.CharaVirtualGroupInfo[_chaCtrl.fileStatus.coordinateType].Count() > 0)
					{
						foreach (KeyValuePair<string, VirtualGroupInfo> _group in _pluginCtrl.CharaVirtualGroupInfo[_chaCtrl.fileStatus.coordinateType])
						{
							if (_pluginCtrl.GetPartsOfKind(_group.Value.Kind).Count() > 0)
							{
								CreateButton(_chaCtrl, _counter, _group.Key, i);
								i++;
							}
						}
						i = 0;
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

			internal static void CreateButton(ChaControl _chaCtrl, int _counter, string _group, int i)
			{
				int _currentCoordinateIndex = _chaCtrl.fileStatus.coordinateType;
				foreach (HSprite _sprite in JetPack.CharaHscene.Sprites)
				{
					Transform _parent;
					if (JetPack.CharaHscene.Heroine.Count > 1)
						_parent = _sprite.lstMultipleFemaleDressButton[_counter].accessoryAll.transform;
					else
						_parent = _sprite.categoryAccessoryAll.transform;

					Transform _origin = _sprite.categoryAccessory.lstButton[0].transform;
					Transform _copy = Instantiate(_origin.transform, _parent, false);
					_copy.name = $"btnASS_{_counter}_{_group}";

					AccStateSyncController _pluginCtrl = GetController(_chaCtrl);
					string _label = _pluginCtrl.CharaVirtualGroupInfo[_currentCoordinateIndex][_group].Label;
					_copy.GetComponentInChildren<TextMeshProUGUI>().text = _label;

					RectTransform _copyRt = _copy.GetComponent<RectTransform>();
					_copyRt.offsetMin = new Vector2(0, UI.ContainerOffsetMinY + (UI.MenuitemHeightOffsetY * (i + 1))); // -168 
					_copyRt.offsetMax = new Vector2(112, UI.ContainerOffsetMinY + (UI.MenuitemHeightOffsetY * i)); // -144 

					Button _button = _copy.GetComponentInChildren<Button>();
					for (int n = 0; n < _button.onClick.GetPersistentEventCount(); n++)
						_button.onClick.SetPersistentListenerState(n, UnityEventCallState.Off);
					_button.onClick.RemoveAllListeners();
					_button.onClick = new Button.ButtonClickedEvent();
					_button.image.raycastTarget = true;

					_button.onClick.AddListener(delegate ()
					{
						bool _show = !_pluginCtrl.CharaVirtualGroupInfo[_currentCoordinateIndex][_group].State;
						_pluginCtrl.OnVirtualGroupStateChange(_group, _show);
						Illusion.Game.Utils.Sound.Play(Illusion.Game.SystemSE.sel);
					});

					_copy.gameObject.SetActiveIfDifferent(true);
				}
			}
		}
	}
}
