using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using ChaCustom;
using UniRx;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		internal static AccStateSyncUI _charaConfigWindow;

		internal partial class AccStateSyncUI : MonoBehaviour
		{
			private ChaControl _chaCtrl
			{
				get
				{
					if (JetPack.CharaStudio.Running)
						return CharaStudio._curOCIChar?.charInfo;
					else
						return CustomBase.Instance?.chaCtrl;
				}
			}
			private AccStateSyncController _pluginCtrl => _chaCtrl?.gameObject?.GetComponent<AccStateSyncController>();
			private int _currentCoordinateIndex => (int) _chaCtrl?.fileStatus?.coordinateType;

			private int _windowRectID;
			private Rect _windowRect, _dragWindowRect;

			private Vector2 _kindScrollPos = Vector2.zero;
			private Vector2 _windowSize = new Vector2(575, 425);
			internal Vector2 _windowPos = new Vector2(525, 460);
			private Texture2D _windowBGtex = null;
			private bool _hasFocus = false;
			private bool _passThrough = false;
			internal bool _onAccTab = false;
			internal int _slotIndex = -1;

			private Vector2 _ScreenRes = Vector2.zero;
			internal float _cfgScaleFactor = 1f;
			private Vector2 _resScaleFactor = Vector2.one;
			private Matrix4x4 _resScaleMatrix;
#if KK
			private readonly Color _windowBG = new Color(0.5f, 0.5f, 0.5f, 1f);
#elif KKS
			private readonly Color _windowBG = new Color(0.2f, 0.2f, 0.2f, 1f);
#endif
			private List<float> _scaleFactorList;

			private readonly GUILayoutOption _buttonElem = GUILayout.Width(50);
			private readonly GUILayoutOption _toggleElem = GUILayout.Width(15);
			private readonly GUILayoutOption _priorityElem = GUILayout.Width(20);

			private const string _priorityTooltipUp = "Rise priority, only used when multiple settings trying to apply on the same acc";
			private const string _priorityTooltipDown = "Lower priority, only used when multiple settings trying to apply on the same acc";
			private const string _toggleBindAcc = "Bind current accessory to the state of this part";
			private const string _toggleUnBindAcc = "Unbind current accessory to the state of this part";

			private void Awake()
			{
				DontDestroyOnLoad(this);
				enabled = false;

				_windowRectID = GUIUtility.GetControlID(FocusType.Passive);

				if (JetPack.CharaStudio.Running)
				{
					_windowSize = new Vector2(325, 425);
					_windowBGtex = JetPack.UI.MakePlainTex((int) _windowSize.x, (int) _windowSize.y + 10, _windowBG);
					_windowPos.x = _cfgStudioWinX.Value;
					_windowPos.y = _cfgStudioWinY.Value;
					_scaleFactorList = (_cfgStudioWinScale.Description.AcceptableValues as BepInEx.Configuration.AcceptableValueList<float>).AcceptableValues.ToList();
				}
				else
				{
					if (_cfgMakerWinEnable.Value)
						enabled = true;
					_windowPos.x = _cfgMakerWinX.Value;
					_windowPos.y = _cfgMakerWinY.Value;
					_windowBGtex = JetPack.UI.MakePlainTex((int) _windowSize.x, (int) _windowSize.y, _windowBG);
					_scaleFactorList = (_cfgMakerWinScale.Description.AcceptableValues as BepInEx.Configuration.AcceptableValueList<float>).AcceptableValues.ToList();
				}
#if KK && !DEBUG
				if (JetPack.MoreAccessories.BuggyBootleg)
				{
					_windowSize = new Vector2(350, 225);
					_windowBGtex = JetPack.UI.MakePlainTex((int) _windowSize.x, (int) _windowSize.y, _windowBG);
				}
#endif
				_passThrough = _cfgDragPass.Value;
				_windowRect = new Rect(_windowPos.x, _windowPos.y, _windowSize.x, _windowSize.y);
				ChangeRes();
			}

			private void OnGUI()
			{
				if (JetPack.CharaStudio.Running)
				{
					if (CharaStudio._curOCIChar == null) return;
				}
				else
				{
					if (CustomBase.Instance?.chaCtrl == null) return;
					if (CustomBase.Instance.customCtrl.hideFrontUI) return;
#if KK
					if (!Manager.Scene.Instance.AddSceneName.IsNullOrEmpty() && Manager.Scene.Instance.AddSceneName != "CustomScene") return;
#endif
					if (JetPack.CharaMaker.CvsMainMenu != 4) return;
					if (_pluginCtrl == null || _pluginCtrl._curPartsInfo == null || _pluginCtrl._curPartsInfo.type == 120) return;

					_slotIndex = CharaMaker._currentSlotIndex;
					if (_slotIndex < 0) return;
				}

				if (_ScreenRes.x != Screen.width || _ScreenRes.y != Screen.height)
					ChangeRes();

				if (_initStyle)
				{
					ChangeRes();
					InitStyle();
				}
				GUI.matrix = _resScaleMatrix;
#if KK && !DEBUG
				if (JetPack.MoreAccessories.BuggyBootleg)
					_dragWindowRect = GUILayout.Window(_windowRectID, _windowRect, DrawWarningWindow, "", _windowSolid);
				else
#endif
				{
					if (JetPack.CharaStudio.Running)
						_dragWindowRect = GUILayout.Window(_windowRectID, _windowRect, DrawStudioWindow, "", _windowSolid);
					else
						_dragWindowRect = GUILayout.Window(_windowRectID, _windowRect, DrawMakerWindow, "", _windowSolid);
				}

				_windowRect.x = _dragWindowRect.x;
				_windowRect.y = _dragWindowRect.y;

				Event _windowEvent = Event.current;
				if (EventType.MouseDown == _windowEvent.type || EventType.MouseUp == _windowEvent.type || EventType.MouseDrag == _windowEvent.type || EventType.MouseMove == _windowEvent.type)
					_hasFocus = false;

				//if (_hasFocus && GetResizedRect(_windowRect).Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)))
				if ((!_passThrough || _hasFocus) && JetPack.UI.GetResizedRect(_windowRect).Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)))
					Input.ResetInputAxes();
			}

			private void DrawWarningWindow(int _windowID)
			{
				GUI.Box(new Rect(0, 0, _windowSize.x, _windowSize.y), _windowBGtex);
				if (GUI.Button(new Rect(_windowSize.x - 27, 4, 23, 23), new GUIContent("X", "Close this window")))
				{
					CloseWindow();
				}
				GUILayout.BeginVertical();
				{
					GUILayout.Space(10);
					GUILayout.BeginHorizontal(GUI.skin.box);
					GUILayout.TextArea("AccStateSync plugin support disabled\n\n" + "MoreAccessories experimental build detected\n" + "This version is not meant for productive use\n\n" + "Please rollback to current stable build\n\n" + "Which could be found at " + "https://www.patreon.com/posts/kk-ec-1-1-0-39203275", GUILayout.ExpandHeight(true));
					GUILayout.EndHorizontal();
				}
				GUILayout.EndVertical();
				GUI.DragWindow();
			}

		private void OnEnable()
			{
				_hasFocus = true;
			}

			private void OnDisable()
			{
				_initStyle = true;
				_hasFocus = false;

				_curRenameGroupKind = -1;
				_curRenameGroupLabel = "";
				_curRenameGroupState = -1;
				_curRenameStateLabel = "";
			}

			// https://answers.unity.com/questions/840756/how-to-scale-unity-gui-to-fit-different-screen-siz.html
			internal void ChangeRes()
			{
				//_cfgScaleFactor = _cfgMakerWinScale.Value;
				_ScreenRes.x = Screen.width;
				_ScreenRes.y = Screen.height;
				_resScaleFactor.x = _ScreenRes.x / 1600;
				_resScaleFactor.y = _ScreenRes.y / 900;

				if (_cfgMakerWinResScale.Value || _cfgStudioWinResScale.Value)
					_resScaleMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(_resScaleFactor.x * _cfgScaleFactor, _resScaleFactor.y * _cfgScaleFactor, 1));
				else
					_resScaleMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(_cfgScaleFactor, _cfgScaleFactor, 1));
				ResetPos();
			}

			internal void ResetPos()
			{
				if (JetPack.CharaStudio.Running)
				{
					_windowPos.x = _cfgStudioWinX.Value;
					_windowPos.y = _cfgStudioWinY.Value;
				}
				else
				{
					_windowPos.x = _cfgMakerWinX.Value;
					_windowPos.y = _cfgMakerWinY.Value;
				}
				
				if (_cfgMakerWinResScale.Value || _cfgStudioWinResScale.Value)
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
				if (JetPack.CharaStudio.Running)
					CharaStudio._ttConfigWindow.SetValue(false);
				else
					enabled = false;
			}
		}
	}
}
