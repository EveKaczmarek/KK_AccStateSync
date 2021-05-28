using System.Linq;

using UnityEngine;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		internal partial class AccStateSyncUI
		{
			private void DrawStudioWindow(int _windowID)
			{
				if (CharaStudio._curOCIChar?.charInfo == null) return;

				Event _windowEvent = Event.current;
				if (EventType.MouseDown == _windowEvent.type || EventType.MouseUp == _windowEvent.type || EventType.MouseDrag == _windowEvent.type || EventType.MouseMove == _windowEvent.type)
					_hasFocus = true;

				GUI.Box(new Rect(0, 0, _windowSize.x, _windowSize.y), _windowBGtex);
				GUI.Box(new Rect(0, 0, _windowSize.x, 30), "ASS", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });

				if (GUI.Button(new Rect(_windowSize.x - 27, 4, 23, 23), new GUIContent("X", "Close this window")))
				{
					CloseWindow();
				}

				if (GUI.Button(new Rect(_windowSize.x - 77, 4, 50, 23), new GUIContent("ON", $"{(_pluginCtrl.TriggerEnabled ? "Disable" : "Enable")} triggers on this character"), (_pluginCtrl.TriggerEnabled ? _buttonActive : GUI.skin.button)))
				{
					_pluginCtrl.TriggerEnabled = !_pluginCtrl.TriggerEnabled;
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
					_cfgStudioWinX.Value = _windowPos.x;
					_cfgStudioWinY.Value = _windowPos.y;
				}

				if (GUI.Button(new Rect(50, 4, 23, 23), new GUIContent("-", "")))
				{
					int _index = _scaleFactorList.IndexOf(_cfgStudioWinScale.Value);
					if (_index > 0)
						_cfgStudioWinScale.Value = _scaleFactorList.ElementAt(_index - 1);
				}

				if (GUI.Button(new Rect(73, 4, 23, 23), new GUIContent("+", "")))
				{
					int _index = _scaleFactorList.IndexOf(_cfgStudioWinScale.Value);
					if (_index < (_scaleFactorList.Count - 1))
						_cfgStudioWinScale.Value = _scaleFactorList.ElementAt(_index + 1);
				}

				GUILayout.Space(10);
				GUILayout.BeginVertical();
				{
					GUILayout.BeginHorizontal();
					{
						GUILayout.BeginVertical(GUI.skin.box);
						{
							DrawStudioPreviewBlock();
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
		}
	}
}
