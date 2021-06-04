using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace JetPack
{
	public partial class CharaStudio
	{
		public static Button CreateCharaButton(string _name, string _label, string _path, UnityAction _onClick) => CreateCharaButton(_name, _label, SetupList(_path), _onClick);
		public static Button CreateCharaButton(string _name, string _label, ScrollRect _scrollRect, UnityAction _onClick)
		{
			return CreateButton(_name, _label, _scrollRect, _onClick, "StudioScene/Canvas Main Menu/02_Manipulate/00_Chara/00_Root/Viewport/Content/State");
		}

		public static ScrollRect SetupList(string _path)
		{
			ScrollRect _scrollRect = GameObject.Find(_path).GetComponent<ScrollRect>();
			_scrollRect.content.gameObject.GetOrAddComponent<VerticalLayoutGroup>();
			_scrollRect.content.gameObject.GetOrAddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
			_scrollRect.scrollSensitivity = 25;

			foreach (Transform _child in _scrollRect.content.transform)
			{
				LayoutElement _layoutElement = _child.gameObject.GetOrAddComponent<LayoutElement>();
				_layoutElement.preferredHeight = 40;
			}

			return _scrollRect;
		}

		internal static Button CreateButton(string _name, string _label, ScrollRect _scrollRect, UnityAction _onClick, string _path)
		{
			GameObject _origin = GameObject.Find(_path);
			GameObject _copy = Object.Instantiate(_origin, _scrollRect.content.transform);
			_copy.name = _name;
			_copy.GetComponentInChildren<Text>().text = _label;
			Button _button = _copy.GetComponent<Button>();
			for (int i = 0; i < _button.onClick.GetPersistentEventCount(); i++)
				_button.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
			_button.onClick.RemoveAllListeners();
			_button.onClick.AddListener(_onClick);
			return _button;
		}
	}
}
