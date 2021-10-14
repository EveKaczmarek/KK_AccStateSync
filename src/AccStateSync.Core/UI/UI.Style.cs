using UnityEngine;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		internal partial class AccStateSyncUI
		{
			private bool _initStyle = true;

			private GUIStyle _windowSolid;

			private GUIStyle _label;
			private GUIStyle _labelDisabled;
			private GUIStyle _labelActive;
			private GUIStyle _labelBoldOrange;

			private GUIStyle _labelAlignCenter;
			private GUIStyle _labelAlignCenterDisabled;
			private GUIStyle _labelAlignCenterActive;
			private GUIStyle _labelAlignCenterBoldDisabled;
			private GUIStyle _labelAlignCenterBoldActive;

			private GUIStyle _buttonActive;
			private GUIStyle _textArea;

			private void InitStyle()
			{
				_windowSolid = new GUIStyle(GUI.skin.window);
				_windowSolid.normal.background = _windowSolid.onNormal.background;

				_labelAlignCenter = new GUIStyle(GUI.skin.label);
				_labelAlignCenter.clipping = TextClipping.Clip;
				_labelAlignCenter.wordWrap = false;
				_labelAlignCenter.alignment = TextAnchor.MiddleCenter;
				_labelAlignCenter.normal.textColor = Color.white;

				_labelAlignCenterDisabled = new GUIStyle(_labelAlignCenter);
				_labelAlignCenterDisabled.normal.textColor = Color.grey;

				_labelAlignCenterActive = new GUIStyle(_labelAlignCenter);
				_labelAlignCenterActive.normal.textColor = Color.cyan;

				_labelAlignCenterBoldDisabled = new GUIStyle(_labelAlignCenterDisabled) { fontStyle = FontStyle.Bold };
				_labelAlignCenterBoldActive = new GUIStyle(_labelAlignCenterActive) { fontStyle = FontStyle.Bold };

				_label = new GUIStyle(GUI.skin.label);
				_label.clipping = TextClipping.Clip;
				_label.wordWrap = false;
				_label.normal.textColor = Color.white;

				_labelDisabled = new GUIStyle(_label);
				_labelDisabled.normal.textColor = Color.grey;

				_labelActive = new GUIStyle(_label);
				_labelActive.normal.textColor = Color.cyan;

				_buttonActive = new GUIStyle(GUI.skin.button);
				_buttonActive.normal.textColor = Color.cyan;
				_buttonActive.hover.textColor = Color.cyan;
				_buttonActive.fontStyle = FontStyle.Bold;

				_labelBoldOrange = new GUIStyle(_label);
				_labelBoldOrange.normal.textColor = new Color(1, 0.7f, 0, 1);
				_labelBoldOrange.fontStyle = FontStyle.Bold;

				_textArea = new GUIStyle(GUI.skin.textArea);
				_textArea.richText = true;

				_initStyle = false;
			}
		}
	}
}
