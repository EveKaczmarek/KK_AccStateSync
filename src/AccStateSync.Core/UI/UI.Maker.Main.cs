using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		internal partial class AccStateSyncUI
		{
			private int _curRenameGroupKind = -1;
			private string _curRenameGroupLabel = "";
			private int _curRenameGroupState = -1;
			private string _curRenameStateLabel = "";
			private string _rightPane = "preview";

			private void DrawMakerWindow(int _windowID)
			{
#if KKS
				GUI.backgroundColor = Color.grey;
#endif
				Event _windowEvent = Event.current;
				if (EventType.MouseDown == _windowEvent.type || EventType.MouseUp == _windowEvent.type || EventType.MouseDrag == _windowEvent.type || EventType.MouseMove == _windowEvent.type)
					_hasFocus = true;

				GUI.Box(new Rect(0, 0, _windowSize.x, _windowSize.y), _windowBGtex);
				GUI.Box(new Rect(0, 0, _windowSize.x, 30), $"AccStateSync - Slot{_slotIndex + 1:00}", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });

				if (GUI.Button(new Rect(_windowSize.x - 27, 4, 23, 23), new GUIContent("X", "Close this window")))
				{
					CloseWindow();
				}

				if (GUI.Button(new Rect(_windowSize.x - 100, 4, 50, 23), new GUIContent("ON", $"Turn on/off the preview of the settings"), (_sidebarTogglePreview.Value ? _buttonActive : GUI.skin.button)))
				{
					_sidebarTogglePreview.Value = !_sidebarTogglePreview.Value;
				}

				if (GUI.Button(new Rect(_windowSize.x - 50, 4, 23, 23), new GUIContent("0", "Config window will not block mouse drag from outside (experemental)"), (_passThrough ? _buttonActive : new GUIStyle(GUI.skin.button))))
				{
					_passThrough = !_passThrough;
					_cfgDragPass.Value = _passThrough;
					_logger.LogMessage($"Pass through mode: {(_passThrough ? "ON" : "OFF")}");
				}

				if (GUI.Button(new Rect(4, 4, 23, 23), new GUIContent("<", "Reset window position")))
				{
					ChangeRes();
				}

				if (GUI.Button(new Rect(27, 4, 23, 23), new GUIContent("T", "Use current window position when reset")))
				{
					if (_cfgMakerWinResScale.Value)
					{
						_windowPos.x = _windowRect.x * _cfgScaleFactor;
						_windowPos.y = _windowRect.y * _cfgScaleFactor;
					}
					else
					{
						_windowPos.x = _windowRect.x / _resScaleFactor.x * _cfgScaleFactor;
						_windowPos.y = _windowRect.y / _resScaleFactor.y * _cfgScaleFactor;
					}
					_cfgMakerWinX.Value = _windowPos.x;
					_cfgMakerWinY.Value = _windowPos.y;
				}

				if (GUI.Button(new Rect(50, 4, 23, 23), new GUIContent("-", "")))
				{
					int _index = _scaleFactorList.IndexOf(_cfgMakerWinScale.Value);
					if (_index > 0)
						_cfgMakerWinScale.Value = _scaleFactorList.ElementAt(_index - 1);
				}

				if (GUI.Button(new Rect(73, 4, 23, 23), new GUIContent("+", "")))
				{
					int _index = _scaleFactorList.IndexOf(_cfgMakerWinScale.Value);
					if (_index < (_scaleFactorList.Count - 1))
						_cfgMakerWinScale.Value = _scaleFactorList.ElementAt(_index + 1);
				}

				GUILayout.BeginVertical();
				{
					GUILayout.Space(10);
					GUILayout.BeginHorizontal();
					{
						GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(300));
						{
							_kindScrollPos = GUILayout.BeginScrollView(_kindScrollPos);
							{
								for (int i = 0; i <= 8; i++)
								{
									Dictionary<byte, string> _keys = _chaCtrl.GetClothesStateKind(i);
									if (_keys != null)
									{
										List<TriggerProperty> _triggersRefSlot = _pluginCtrl._cachedSlotPropertyList.Where(x => x.RefKind == i).ToList();
										GUILayout.BeginVertical(GUI.skin.box);
										{
											bool _bind = _triggersRefSlot.Count > 0;
											GUILayout.BeginHorizontal(GUI.skin.box, GUILayout.ExpandWidth(false));
											{
												if (_bind)
												{
													if (GUILayout.Button(new GUIContent("bind", _toggleUnBindAcc), _buttonActive, _buttonElem))
													{
														_pluginCtrl.TriggerPropertyList.RemoveAll(x => x.Coordinate == _currentCoordinateIndex && x.Slot == _slotIndex && x.RefKind == i);
														RefreshCache();
													}
												}
												else
												{
													if (GUILayout.Button(new GUIContent("bind", _toggleBindAcc), _buttonElem))
													{
														for (int j = 0; j <= 3; j++)
															_pluginCtrl.TriggerPropertyList.Add(new TriggerProperty(_currentCoordinateIndex, _slotIndex, i, j, j < 3, 0));
														RefreshCache();
													}
												}
												GUILayout.Label(_clothesNames[i], (_bind ? GUI.skin.label : _labelDisabled));
												GUILayout.FlexibleSpace();
											}
											GUILayout.EndHorizontal();

											if (_bind)
											{
												for (int j = 0; j <= 3; j++)
												{
													if (_keys.ContainsKey((byte) j))
													{
														TriggerProperty _triggerState = _triggersRefSlot.FirstOrDefault(x => x.RefState == j);
														GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
														GUILayout.Label(_statesNames[j], GUILayout.ExpandWidth(false));
														GUILayout.FlexibleSpace();

														if (_triggerState.Visible)
														{
															GUILayout.Button("show", _buttonActive, _buttonElem);
															if (GUILayout.Button("hide", _buttonElem))
															{
																_triggerState.Visible = false;
																RefreshCache();
															}
														}
														else
														{
															if (GUILayout.Button("show", _buttonElem))
															{
																_triggerState.Visible = true;
																RefreshCache();
															}
															GUILayout.Button("hide", _buttonActive, _buttonElem);
														}

														if (GUILayout.Button(new GUIContent("-", _priorityTooltipDown), _priorityElem))
														{
															_triggerState.Priority = Math.Max(_triggerState.Priority - 1, 0);
															RefreshCache();
														}
														GUILayout.Label(_triggerState.Priority.ToString(), _labelAlignCenter, _priorityElem);
														if (GUILayout.Button(new GUIContent("+", _priorityTooltipUp), _priorityElem))
														{
															_triggerState.Priority = Math.Min(_triggerState.Priority + 1, 99);
															RefreshCache();
														}
														GUILayout.EndHorizontal();
													}
												}
											}
											GUI.enabled = true;

											GUILayout.EndHorizontal();
										}
									}
								}

								foreach (TriggerGroup _group in _pluginCtrl._cachedCoordinateGroupList.ToList())
								{
									List<TriggerProperty> _triggersRefSlot = _pluginCtrl._cachedSlotPropertyList.Where(x => x.RefKind == _group.Kind).ToList();
									GUILayout.BeginVertical(GUI.skin.box);
									{
										bool _bind = _triggersRefSlot.Count > 0;
										GUILayout.BeginHorizontal(GUI.skin.box, GUILayout.ExpandWidth(false));
										{
											if (_bind)
											{
												if (GUILayout.Button(new GUIContent("bind", _toggleUnBindAcc), _buttonActive, _buttonElem))
												{
													_pluginCtrl.TriggerPropertyList.RemoveAll(x => x.Coordinate == _currentCoordinateIndex && x.Slot == _slotIndex && x.RefKind == _group.Kind);
													RefreshCache();
												}
											}
											else
											{
												if (GUILayout.Button(new GUIContent("bind", _toggleBindAcc), _buttonElem))
												{
													foreach (int _state in _group.States.Keys)
														_pluginCtrl.NewOrGetTriggerProperty(_currentCoordinateIndex, _slotIndex, _group.Kind, _state);
													_pluginCtrl.GetTriggerProperty(_currentCoordinateIndex, _slotIndex, _group.Kind, 1).Visible = false;
													RefreshCache();
												}
											}
											GUILayout.Label(_group.Label, (_bind ? GUI.skin.label : _labelDisabled));
											GUILayout.FlexibleSpace();

											if (!_bind)
												GUI.enabled = false;
											if (GUILayout.Button(new GUIContent("+", $"Add a new state to virtual group: {_group.Label}"), _priorityElem))
											{
												int _state = _group.AddNewState();
												HashSet<int> _slots = new HashSet<int>(_pluginCtrl.TriggerPropertyList.Where(x => x.Coordinate == _currentCoordinateIndex && x.RefKind == _group.Kind).Select(x => x.Slot));
												foreach (int _slot in _slots)
													_pluginCtrl.NewOrGetTriggerProperty(_currentCoordinateIndex, _slot, _group.Kind, _state);
												RefreshCache();
											}
											GUI.enabled = true;
										}
										GUILayout.EndHorizontal();

										Dictionary<int, string> _states = _group.States.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);

										if (_bind)
										{
											foreach (KeyValuePair<int, string> _state in _states)
											{
												TriggerProperty _trigger = _triggersRefSlot.FirstOrDefault(x => x.RefState == _state.Key);
												if (_trigger == null) continue;

												GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
												GUILayout.Label(_state.Value, GUILayout.ExpandWidth(false));
												GUILayout.FlexibleSpace();

												if (_trigger.Visible)
												{
													GUILayout.Button("show", _buttonActive, _buttonElem);
													if (GUILayout.Button("hide", _buttonElem))
													{
														_trigger.Visible = false;
														RefreshCache();
													}
												}
												else
												{
													if (GUILayout.Button("show", _buttonElem))
													{
														_trigger.Visible = true;
														RefreshCache();
													}
													GUILayout.Button("hide", _buttonActive, _buttonElem);
												}

												if (GUILayout.Button(new GUIContent("-", _priorityTooltipDown), _priorityElem))
												{
													_trigger.Priority = Math.Max(_trigger.Priority - 1, 0);
													RefreshCache();
												}
												GUILayout.Label(_trigger.Priority.ToString(), _labelAlignCenter, _priorityElem);
												if (GUILayout.Button(new GUIContent("+", _priorityTooltipUp), _priorityElem))
												{
													_trigger.Priority = Math.Min(_trigger.Priority + 1, 99);
													RefreshCache();
												}
												GUILayout.EndHorizontal();
											}
										}
										GUILayout.EndHorizontal();
									}
								}

								if (GUILayout.Button(new GUIContent("+", "Add a new virtual group")))
								{
									_pluginCtrl.CreateTriggerGroup();
									RefreshCache();
								}
							}
							GUILayout.EndScrollView();
						}
						GUILayout.EndVertical();

						GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(250));
						{
							GUILayout.BeginHorizontal(GUI.skin.box);
							{
								GUILayout.FlexibleSpace();
								if (GUILayout.Button(new GUIContent("preview", "Switch to group preview panel"), (_rightPane == "preview" ? _buttonActive : GUI.skin.button), GUILayout.Width(70)))
									_rightPane = "preview";
								if (GUILayout.Button(new GUIContent("edit", "Switch to group setting edit panel"), (_rightPane == "edit" ? _buttonActive : GUI.skin.button), GUILayout.Width(70)))
									_rightPane = "edit";
								GUILayout.FlexibleSpace();
							}
							GUILayout.EndHorizontal();

							if (_rightPane == "edit")
								DrawEditBlock();
							else
								DrawPreviewBlock();
						}
						GUILayout.EndVertical();
					}
					GUILayout.EndHorizontal();

					GUILayout.BeginHorizontal(GUI.skin.box);
					GUILayout.Label(GUI.tooltip);
					GUILayout.EndHorizontal();

					if (JetPack.MoreAccessories.BuggyBootleg)
                    {
						GUILayout.BeginHorizontal(GUI.skin.box);
						GUILayout.Label("MoreAccessories experimental build detected, which is not meant for production use", _labelBoldOrange);
						GUILayout.EndHorizontal();
					}
				}
				GUILayout.EndVertical();
				GUI.DragWindow();
			}
		}
	}
}
