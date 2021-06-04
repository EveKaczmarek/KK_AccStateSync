using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		internal partial class AccStateSyncUI
		{
			private readonly GUILayoutOption _previewLabelStudio = GUILayout.Width(110);

			private void DrawStudioPreviewBlock()
			{
				GUILayout.BeginHorizontal(GUI.skin.box);
				{
					GUILayout.Label(_pluginCtrl.CharaFullName, (_pluginCtrl.TriggerEnabled ? _labelAlignCenterBoldActive : _labelAlignCenterDisabled));
				}
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal(GUI.skin.box);
				{
					for (int i = 0; i < _cordNames.Count; i++)
					{
						if (GUILayout.Button(new GUIContent($"{i + 1}", $"Switch to {_cordNames[i]}"), (i == _currentCoordinateIndex ? _buttonActive : GUI.skin.button), _priorityElem))
						{
							if (i == _currentCoordinateIndex)
								_chaCtrl.ChangeCoordinateTypeAndReload(false);
							else
								_chaCtrl.ChangeCoordinateTypeAndReload((ChaFileDefine.CoordinateType) i);
							StartCoroutine(CharaStudio.StatusPanelUpdateCoroutine());
						}
					}

					GUILayout.FlexibleSpace();

					int _shoesType = _chaCtrl.fileStatus.shoesType;

					if (GUILayout.Button(new GUIContent("I", "Switch to indoors"), (_shoesType == 0 ? _buttonActive : GUI.skin.button), _priorityElem))
					{
						_chaCtrl.fileStatus.shoesType = 0;
						StartCoroutine(CharaStudio.StatusPanelUpdateCoroutine());
					}
					if (GUILayout.Button(new GUIContent("O", "Switch to outdoors"), (_shoesType == 1 ? _buttonActive : GUI.skin.button), _priorityElem))
					{
						_chaCtrl.fileStatus.shoesType = 1;
						StartCoroutine(CharaStudio.StatusPanelUpdateCoroutine());
					}
				}
				GUILayout.EndHorizontal();

				_previewScrollPos = GUILayout.BeginScrollView(_previewScrollPos);
				{
					HashSet<int> _kinds = new HashSet<int>(_pluginCtrl._cachedCoordinatePropertyList.OrderBy(x => x.RefKind).Select(x => x.RefKind));
					Dictionary<int, int> _triggerCount = new Dictionary<int, int>();
					foreach (int _kind in _kinds)
						_triggerCount[_kind] = _pluginCtrl._cachedCoordinatePropertyList.Where(x => x.RefKind == _kind).GroupBy(x => x.Slot).Count();

					for (int i = 0; i < _clothesNames.Count; i++)
					{
						if (i == 7 && _chaCtrl.fileStatus.shoesType != 0) continue;
						if (i == 8 && _chaCtrl.fileStatus.shoesType != 1) continue;

						List<int> _states = _chaCtrl.GetClothesStateKind(i)?.Select(x => (int) x.Key).ToList();
						if (_states?.Count > 0)
						{
							GUILayout.BeginHorizontal(GUI.skin.box);
							{
								int _state = _chaCtrl.fileStatus.clothesState[i];
								int _count = _triggerCount.ContainsKey(i) ? _triggerCount[i] : 0;
								GUILayout.Label($"({_count}) {_clothesNames[i]}", _label, _previewLabelStudio, GUILayout.ExpandWidth(false));
								GUILayout.FlexibleSpace();

								if (_states.Count > 3)
								{
									if (_state == 0)
										GUI.enabled = false;
									if (GUILayout.Button(new GUIContent("<", "Switch to previous state"), _priorityElem))
									{
										int _index = _states.IndexOf(_state) - 1;
										if (i == 7 || i == 8)
										{
											_chaCtrl.SetClothesState(7, (byte) _states[_index]);
											_chaCtrl.SetClothesState(8, (byte) _states[_index]);
										}
										else
											_chaCtrl.SetClothesState(i, (byte) _states[_index]);
										StartCoroutine(CharaStudio.StatusPanelUpdateCoroutine());
									}
									GUI.enabled = true;
									GUILayout.Label(_state.ToString(), _labelAlignCenter, _priorityElem);
									if (_state == _states[_states.Count - 1])
										GUI.enabled = false;
									if (GUILayout.Button(new GUIContent(">", "Switch to next state"), _priorityElem))
									{
										int _index = _states.IndexOf(_state) + 1;
										if (i == 7 || i == 8)
										{
											_chaCtrl.SetClothesState(7, (byte) _states[_index]);
											_chaCtrl.SetClothesState(8, (byte) _states[_index]);
										}
										else
											_chaCtrl.SetClothesState(i, (byte) _states[_index]);
										StartCoroutine(CharaStudio.StatusPanelUpdateCoroutine());
									}
									GUI.enabled = true;
								}
								else
								{
									foreach (int j in _states)
									{
										if (GUILayout.Button(new GUIContent(j.ToString(), $"Switch {_statesNames[j]} state"), (_state == j ? _buttonActive : GUI.skin.button), _priorityElem))
										{
											if (i == 7 || i == 8)
											{
												_chaCtrl.SetClothesState(7, (byte) j);
												_chaCtrl.SetClothesState(8, (byte) j);
											}
											else
												_chaCtrl.SetClothesState(i, (byte) j);
											StartCoroutine(CharaStudio.StatusPanelUpdateCoroutine());
										}
									}
								}
							}
							GUILayout.EndHorizontal();
						}
					}

					foreach (TriggerGroup _group in _pluginCtrl._cachedCoordinateGroupList)
					{
						GUILayout.BeginHorizontal(GUI.skin.box);
						{
							int _count = _triggerCount.ContainsKey(_group.Kind) ? _triggerCount[_group.Kind] : 0;
							GUILayout.Label($"({_count}) {_group.Label}", _label, _previewLabelStudio, GUILayout.ExpandWidth(false));
							GUILayout.FlexibleSpace();

							if (_group.States.Count == 0) continue;

							List<int> _states = _group.States.OrderBy(x => x.Key).Select(x => x.Key).ToList();

							if (_states.Count > 3)
							{
								int _state = _group.State;
								if (_state == _states[0])
									GUI.enabled = false;
								if (GUILayout.Button(new GUIContent("<", "Switch to previous state"), _priorityElem))
								{
									int _index = _states.IndexOf(_state) - 1;
									_pluginCtrl.SetGroupState(_group.Kind, _states[_index]);
									StartCoroutine(CharaStudio.StatusPanelUpdateCoroutine());
								}
								GUI.enabled = true;
								GUILayout.Label(_state.ToString(), _labelAlignCenter, _priorityElem);
								if (_state == _states[_states.Count - 1])
									GUI.enabled = false;
								if (GUILayout.Button(new GUIContent(">", "Switch to next state"), _priorityElem))
								{
									int _index = _states.IndexOf(_state) + 1;
									_pluginCtrl.SetGroupState(_group.Kind, _states[_index]);
									StartCoroutine(CharaStudio.StatusPanelUpdateCoroutine());
								}
								GUI.enabled = true;
							}
							else
							{
								foreach (int _state in _states)
								{
									if (GUILayout.Button(new GUIContent(_state.ToString(), $"Switch to {_group.States[_state]} state"), (_state == _group.State ? _buttonActive : GUI.skin.button), _priorityElem))
									{
										_pluginCtrl.SetGroupState(_group.Kind, _state);
										StartCoroutine(CharaStudio.StatusPanelUpdateCoroutine());
									}
								}
							}
						}
						GUILayout.EndHorizontal();
					}
				}
				GUILayout.EndScrollView();
			}
		}
	}
}
