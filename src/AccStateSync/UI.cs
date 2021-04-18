using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using ChaCustom;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		internal static AccStateSyncUI _makerConfigWindow;

		internal class AccStateSyncUI : MonoBehaviour
		{
			private List<string> _clothesNames = new List<string>() { "None", "Top", "Bottom", "Bra", "Underwear", "Gloves", "Pantyhose", "Legwear", "Indoors", "Outdoors", "Parent" };
			private List<string> _statesNames = new List<string>() { "Full", "Half 1", "Half 2", "Undressed" };

			private AccStateSyncController _pluginCtrl => CustomBase.Instance?.chaCtrl?.gameObject?.GetComponent<AccStateSyncController>();
			private int _currentCoordinateIndex => CustomBase.Instance.chaCtrl.fileStatus.coordinateType;

			private int _windowRectID;
			private Rect _windowRect, _dragWindowRect;

			private Vector2 _kindScrollPos = Vector2.zero;
			private Vector2 _groupScrollPos = Vector2.zero;
			private Vector2 _windowSize = new Vector2(425, 405);
			internal Vector2 _windowPos = new Vector2(525, 460);
			private Texture2D _windowBGtex = null;
			private bool _hasFocus = false;

			private Vector2 _ScreenRes = Vector2.zero;
			internal float _cfgScaleFactor = 1f;
			private Vector2 _resScaleFactor = Vector2.one;
			private Matrix4x4 _resScaleMatrix;

			private bool _initStyle = true;

			private GUIStyle _windowSolid;
			private GUIStyle _labelDisabledAlignCenter;
			private GUIStyle _labelDisabled;
			private GUIStyle _buttonActive;

			private List<float> _scaleFactorList;

			private readonly GUILayoutOption _buttonElem = GUILayout.Width(50);
			private readonly GUILayoutOption _toggleElem = GUILayout.Width(15);

			private List<bool> _states = new List<bool>() { true, false, false, false };
			private string _curRenameGroup = "", _curRenameLabel = "";

			private void Awake()
			{
				DontDestroyOnLoad(this);
				enabled = false;

				_scaleFactorList = (_cfgMakerWinScale.Description.AcceptableValues as BepInEx.Configuration.AcceptableValueList<float>).AcceptableValues.ToList();

				_windowRectID = GUIUtility.GetControlID(FocusType.Passive);
				_windowPos.x = _cfgMakerWinX.Value;
				_windowPos.y = _cfgMakerWinY.Value;
				_windowRect = new Rect(_windowPos.x, _windowPos.y, _windowSize.x, _windowSize.y);
				_windowBGtex = MakeTex((int) _windowSize.x, (int) _windowSize.y, new Color(0.5f, 0.5f, 0.5f, 1f));
				ChangeRes();
			}

			private void OnGUI()
			{
				if (CustomBase.Instance?.chaCtrl == null) return;
				if (CustomBase.Instance.customCtrl.hideFrontUI) return;
				if (!Manager.Scene.Instance.AddSceneName.IsNullOrEmpty() && Manager.Scene.Instance.AddSceneName != "CustomScene") return;
				if (JetPack.CharaMaker.CvsMainMenu != 4) return;
				if (_pluginCtrl == null || _pluginCtrl.CurSlotTriggerInfo == null || CharaMaker._currentSlotIndex < 0 || _pluginCtrl._curPartsInfo == null || _pluginCtrl._curPartsInfo.type == 120) return;

				if (_ScreenRes.x != Screen.width || _ScreenRes.y != Screen.height)
					ChangeRes();

				if (_initStyle)
				{
					ChangeRes();

					_windowSolid = new GUIStyle(GUI.skin.window);
					Texture2D _onNormalBG = _windowSolid.onNormal.background;
					_windowSolid.normal.background = _onNormalBG;

					_labelDisabledAlignCenter = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
					_labelDisabledAlignCenter.normal.textColor = Color.grey;

					_labelDisabled = new GUIStyle(GUI.skin.label);
					_labelDisabled.normal.textColor = Color.grey;

					_buttonActive = new GUIStyle(GUI.skin.button);
					_buttonActive.normal.textColor = Color.cyan;
					_buttonActive.hover.textColor = Color.cyan;
					_buttonActive.fontStyle = FontStyle.Bold;

					_initStyle = false;
				}
				GUI.matrix = _resScaleMatrix;

				_dragWindowRect = GUILayout.Window(_windowRectID, _windowRect, DrawWindowContents, "", _windowSolid);
				_windowRect.x = _dragWindowRect.x;
				_windowRect.y = _dragWindowRect.y;

				Event _windowEvent = Event.current;
				if (EventType.MouseDown == _windowEvent.type || EventType.MouseUp == _windowEvent.type || EventType.MouseDrag == _windowEvent.type || EventType.MouseMove == _windowEvent.type)
					_hasFocus = false;

				if (_hasFocus && GetResizedRect(_windowRect).Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)))
					Input.ResetInputAxes();
			}

			// https://bensilvis.com/unity3d-auto-scale-gui/
			private Rect GetResizedRect(Rect _rect)
			{
				Vector2 _position = GUI.matrix.MultiplyVector(new Vector2(_rect.x, _rect.y));
				Vector2 _size = GUI.matrix.MultiplyVector(new Vector2(_rect.width, _rect.height));

				return new Rect(_position.x, _position.y, _size.x, _size.y);
			}

			private void OnEnable()
			{
				_hasFocus = true;
			}

			private void OnDisable()
			{
				_initStyle = true;
				_hasFocus = false;
			}

			// https://answers.unity.com/questions/840756/how-to-scale-unity-gui-to-fit-different-screen-siz.html
			internal void ChangeRes()
			{
				//_cfgScaleFactor = _cfgMakerWinScale.Value;
				_ScreenRes.x = Screen.width;
				_ScreenRes.y = Screen.height;
				_resScaleFactor.x = _ScreenRes.x / 1600;
				_resScaleFactor.y = _ScreenRes.y / 900;

				if (_cfgMakerWinResScale.Value)
					_resScaleMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(_resScaleFactor.x * _cfgScaleFactor, _resScaleFactor.y * _cfgScaleFactor, 1));
				else
					_resScaleMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(_cfgScaleFactor, _cfgScaleFactor, 1));
				ResetPos();
			}

			internal void ResetPos()
			{
				_windowPos.x = _cfgMakerWinX.Value;
				_windowPos.y = _cfgMakerWinY.Value;
				if (_cfgMakerWinResScale.Value)
				{
					_windowRect.x = _windowPos.x / _cfgScaleFactor;
					_windowRect.y = _windowPos.y / _cfgScaleFactor;
				}
				else
				{
					_windowRect.x = _windowPos.x * _resScaleFactor.x / _cfgScaleFactor;
					_windowRect.y = _windowPos.y * _resScaleFactor.y / _cfgScaleFactor;
				}
			}

			private void CloseWindow()
			{
				_sidebarToggleEnable.Value = false;
				//enabled = false;
			}

			private void DrawWindowContents(int _windowID)
			{
				Event _windowEvent = Event.current;
				if (EventType.MouseDown == _windowEvent.type || EventType.MouseUp == _windowEvent.type || EventType.MouseDrag == _windowEvent.type || EventType.MouseMove == _windowEvent.type)
					_hasFocus = true;

				GUI.Box(new Rect(0, 0, _windowSize.x, _windowSize.y), _windowBGtex);
				GUI.Box(new Rect(0, 0, _windowSize.x, 30), $"AccStateSync - Slot{CharaMaker._currentSlotIndex + 1:00}", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });

				if (GUI.Button(new Rect(_windowSize.x - 27, 4, 23, 23), new GUIContent("X", "Clothes this window")))
				{
					CloseWindow();
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
						GUILayout.BeginVertical(GUILayout.Width(200));
						{
							_kindScrollPos = GUILayout.BeginScrollView(_kindScrollPos, GUI.skin.box);
							{
								for (int i = 0; i < _clothesNames.Count; i++)
								{
									int _kind = i - 1;
									string _label = _clothesNames[i];

									if (_kind == 9)
										_label += $" ({_pluginCtrl._curPartsInfo.parentKey})";

									if (_kind == _pluginCtrl.CurSlotTriggerInfo.Kind)
										GUILayout.Label(_label, _buttonActive);
									else
									{
										if (GUILayout.Button(new GUIContent(_label, $"Bind this accessory to {_label}")))
										{
											_pluginCtrl.SetCurSlotTriggerInfo(_kind);
											_pluginCtrl.OnCurSlotTriggerInfoChange();
										}
									}
								}

								if (_pluginCtrl.CharaVirtualGroupInfo[_currentCoordinateIndex]?.Count > 0)
								{
									foreach (VirtualGroupInfo _item in _pluginCtrl.CharaVirtualGroupInfo[_currentCoordinateIndex].Values.ToList())
									{
										if (_item.Kind == 9) continue;

										string _label = $"{_item.Label} ({_pluginCtrl.CharaVirtualGroupInfo[_currentCoordinateIndex][$"custom_{_item.Kind - 9}"].Group})";

										if (_item.Kind == _pluginCtrl.CurSlotTriggerInfo.Kind)
											GUILayout.Label(_label, _buttonActive);
										else
										{
											if (GUILayout.Button(new GUIContent(_label, $"Bind this accessory to {_label}")))
											{
												_pluginCtrl.SetCurSlotTriggerInfo(_item.Kind);
												_pluginCtrl.OnCurSlotTriggerInfoChange();
											}
										}
									}
								}

								if (GUILayout.Button(new GUIContent("-", "Remove last virtual group")))
								{
									_pluginCtrl.PopGroup();
								}

								if (GUILayout.Button(new GUIContent("+", "Add a new virtual group")))
								{
									_pluginCtrl.PushGroup();
								}
							}
							GUILayout.EndScrollView();
						}
						GUILayout.EndVertical();

						if (MathfEx.RangeEqualOn(0, _pluginCtrl.CurSlotTriggerInfo.Kind, 8))
						{
							_states = JetPack.Chara.Clothes.GetClothesStates(CustomBase.Instance.chaCtrl, _pluginCtrl.CurSlotTriggerInfo.Kind);
							if (_states.Count < 4)
								_states = new List<bool>() { false, false, false, false };
						}
						else if (_pluginCtrl.CurSlotTriggerInfo.Kind < 0)
							_states = new List<bool>() { false, false, false, false };
						else
							_states = new List<bool>() { true, false, false, true };

						GUILayout.BeginVertical(GUILayout.Width(200));
						{
							GUILayout.BeginVertical();
							{
								for (int i = 0; i < 4; i++)
								{
									GUILayout.BeginHorizontal(GUI.skin.box);
									{
										if (!_states[i])
										{
											GUILayout.Label(_statesNames[i], _labelDisabled);
											GUILayout.FlexibleSpace();
											GUILayout.Label("Show", _labelDisabledAlignCenter, _buttonElem);
											GUILayout.Label("Hide", _labelDisabledAlignCenter, _buttonElem);
										}
										else
										{
											GUILayout.Label(_statesNames[i]);
											GUILayout.FlexibleSpace();
											if (_pluginCtrl.CurSlotTriggerInfo.State[i])
											{
												GUILayout.Label("Show", _buttonActive, _buttonElem);
												if (GUILayout.Button("Hide", _buttonElem))
												{
													_pluginCtrl.CurSlotTriggerInfo.State[i] = false;
													_pluginCtrl.OnCurSlotTriggerInfoChange();
												}
											}
											else
											{
												if (GUILayout.Button("Show", _buttonElem))
												{
													_pluginCtrl.CurSlotTriggerInfo.State[i] = true;
													_pluginCtrl.OnCurSlotTriggerInfoChange();
												}
												GUILayout.Label("Hide", _buttonActive, _buttonElem);
											}
										}
									}
									GUILayout.EndHorizontal();
								}
							}
							GUILayout.EndVertical();

							GUILayout.BeginVertical();
							{
								_groupScrollPos = GUILayout.BeginScrollView(_groupScrollPos, GUI.skin.box);
								foreach (var _item in _pluginCtrl.CharaVirtualGroupInfo[_currentCoordinateIndex].Values.ToList())
								{
									GUILayout.BeginHorizontal(GUI.skin.box);
									{
										if (_curRenameGroup == _item.Group)
											_curRenameLabel = GUILayout.TextField(_curRenameLabel, new GUIStyle(GUI.skin.textField) { fixedWidth = 90 });
										else
										{
											if (_item.Kind == 9)
												GUILayout.Label(_item.Label);
											else
											{
												if (GUILayout.Button(new GUIContent(_item.Label, "Rename group label"), new GUIStyle(GUI.skin.label)))
												{
													_curRenameGroup = _item.Group;
													_curRenameLabel = _item.Label;
												}
											}
										}
										GUILayout.FlexibleSpace();

										if (_item.Kind > 9)
										{
											if (_curRenameGroup == _item.Group)
											{
												if (GUILayout.Button(new GUIContent("Save", "Save renamed label, leave empty will reset to default"), _buttonElem))
												{
													if (_curRenameLabel.IsNullOrEmpty())
														_curRenameLabel = "Custom " + _curRenameGroup.Replace("custom_", "");
													if (_curRenameLabel != _item.Label)
														_pluginCtrl.RenameGroup(_curRenameGroup, _curRenameLabel);
													_curRenameGroup = "";
													_curRenameLabel = "";
												}
											}
										}

										// https://answers.unity.com/questions/360635/strange-guitoggle-behavior.html
										if (GUILayout.Toggle(_item.State, new GUIContent("", "Toggle grouped accessories visibility"), _toggleElem) != _item.State)
										{
											_item.State = !_item.State;
											_pluginCtrl.OnVirtualGroupStateChange(_item.Group, _item.State);
										}
										if (GUILayout.Toggle(_item.Secondary, new GUIContent("", "Set group as secondary (not show in H start)"), _toggleElem) != _item.Secondary)
										{
											_pluginCtrl.SetSecondaryGroup(_item.Group, !_item.Secondary);
										}
									}
									GUILayout.EndHorizontal();
								}
								GUILayout.EndScrollView();
							}
							GUILayout.EndVertical();
						}
						GUILayout.EndVertical();
					}
					GUILayout.EndHorizontal();

					GUILayout.BeginHorizontal(GUI.skin.box);
					GUILayout.Label(GUI.tooltip);
					GUILayout.EndHorizontal();
				}
				GUILayout.EndVertical();
				GUI.DragWindow();
			}

			private Texture2D MakeTex(int _width, int _height, Color _color)
			{
				Color[] pix = new Color[_width * _height];

				for (int i = 0; i < pix.Length; i++)
					pix[i] = _color;

				Texture2D result = new Texture2D(_width, _height);
				result.SetPixels(pix);
				result.Apply();

				return result;
			}
		}
	}
}
