using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;
using Studio;
using UniRx;

using BepInEx.Logging;

using JetPack;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		internal static class CharaStudio
		{
			internal static OCIChar _curOCIChar = null;
			internal static int _curTreeNodeObjID = -1;
			internal static GameObject _original;
			internal static GameObject ASSPanel;
			internal static CanvasGroup ASSPanelCanvasGroup;
			internal static bool _duringSceneLoad = false;

			internal static void RegisterControls()
			{
				if (!JetPack.CharaStudio.Running) return;

				CreatePanel();

				JetPack.CharaStudio.OnSceneLoad += (_sender, _args) =>
				{
					if (_args.Mode == JetPack.CharaStudio.SceneLoadMode.Load)
					{
						if (_args.State == JetPack.CharaStudio.SceneLoadState.Pre)
						{
							_duringSceneLoad = true;
							DebugMsg(LogLevel.Warning, $"[OnSceneLoad][Pre][_curTreeNodeObjID: {_curTreeNodeObjID}][_duringSceneLoad: {_duringSceneLoad}]");
						}
						if (_args.State == JetPack.CharaStudio.SceneLoadState.Post)
						{
							_duringSceneLoad = false;
							DebugMsg(LogLevel.Warning, $"[OnSceneLoad][Post][_curTreeNodeObjID: {_curTreeNodeObjID}][_duringSceneLoad: {_duringSceneLoad}]");
						}
					}
				};

				List<Button> _buttons = GameObject.Find("StudioScene/Canvas Main Menu/02_Manipulate/00_Chara/01_State/Viewport/Content/Clothing Details").GetComponentsInChildren<Button>().ToList();
				_buttons.Add(GameObject.Find($"StudioScene/Canvas Main Menu/02_Manipulate/00_Chara/01_State/Viewport/Content/Cos/Button Shoes 1").GetComponent<Button>());
				_buttons.Add(GameObject.Find($"StudioScene/Canvas Main Menu/02_Manipulate/00_Chara/01_State/Viewport/Content/Cos/Button Shoes 2").GetComponent<Button>());
				foreach (Button _button in _buttons)
				{
					_button.onClick.AddListener(delegate()
					{
						_instance.StartCoroutine(StatusPanelUpdate_Coroutine());
					});
				}

				JetPack.CharaStudio.OnSelectNodeChange += (_sender, _args) =>
				{
					_curTreeNodeObjID = JetPack.CharaStudio.CurTreeNodeObjID;
					_curOCIChar = JetPack.CharaStudio.CurOCIChar;
					UpdateUI();
				};

				HarmonyLib.Harmony.CreateAndPatchAll(typeof(HooksCharaStudio));
			}

			internal static IEnumerator StatusPanelUpdate_Coroutine()
            {
				yield return new WaitForEndOfFrame();
				yield return new WaitForEndOfFrame();
				DebugMsg(LogLevel.Info, $"[StatusPanelUpdate_Coroutine]");
				if (JetPack.CharaStudio.RefreshCharaStatePanel())
					MoreAccessories.UpdateUI();
			}

			internal static void SetVisibility(bool _show)
			{
				if (ASSPanelCanvasGroup == null) return;
				ASSPanelCanvasGroup.alpha = _show ? 1 : 0;
				ASSPanelCanvasGroup.blocksRaycasts = _show;
			}

			internal static void ClearUI()
			{
				foreach (Transform _child in ASSPanel.transform)
				{
					if (_child.gameObject != null)
						Destroy(_child.gameObject);
				}
				SetVisibility(false);
			}

			internal static void UpdateUI()
			{
				ClearUI();

				if ((_curOCIChar == null) || (_curOCIChar.charInfo == null))
					return;
				AccStateSyncController _pluginCtrl = GetController(_curOCIChar);
				if (_pluginCtrl == null)
					return;
				_pluginCtrl._treeNodeObjID = _curTreeNodeObjID;
				if (!_pluginCtrl.TriggerEnabled)
					return;
				if (_pluginCtrl.CharaVirtualGroupInfo[_curOCIChar.charInfo.fileStatus.coordinateType].Count() == 0)
					return;
				int i = 0;

				foreach (KeyValuePair<string, VirtualGroupInfo> _group in _pluginCtrl.CharaVirtualGroupInfo[_curOCIChar.charInfo.fileStatus.coordinateType])
				{
					if (_pluginCtrl.GetPartsOfGroup(_group.Key).Count() > 0)
					{
						CreateUIText(_group.Key, i, _group.Value.Label);
						CreateUIToggle(_group.Key, i, _group.Value.State);
						i++;
					}
				}
				SetVisibility(i > 0);
			}

			internal static void CreatePanel()
			{
				UI.ContainerOffsetMinY = -20;
				UI.MenuitemHeightOffsetY = -25;

				_original = GameObject.Find("StudioScene/Canvas Main Menu/02_Manipulate/00_Chara/02_Kinematic/00_FK");

				Transform _copy = Instantiate(_original.transform, GameObject.Find("StudioScene/Canvas Main Menu/02_Manipulate/00_Chara/01_State").transform, true);
				_copy.transform.name = "AccStateSync";

				Image _image = _copy.GetComponentInChildren<Image>();
				_image.sprite = null;
				_image.overrideSprite = null;
				_image.color = new Color(0, 0, 0, 0.5f);

				int _shiftX = 124;

				RectTransform _copyRt = _copy.GetComponent<RectTransform>();
				_copyRt.offsetMin = new Vector2(_copyRt.offsetMin.x + _shiftX, _copyRt.offsetMin.y);
				_copyRt.offsetMax = new Vector2(_copyRt.offsetMax.x + _shiftX, _copyRt.offsetMax.y);

				ASSPanel = _copy.gameObject;
				ASSPanel.SetActiveIfDifferent(true);
				ASSPanelCanvasGroup = ASSPanel.GetOrAddComponent<CanvasGroup>();

				ClearUI();
			}

			internal static T GetPanelObject<T>(string _name) => _original.transform.GetComponentsInChildren<RectTransform>(true).First(x => x.name == _name).GetComponent<T>();

			internal static void CreateUIText(string _name, int i, string _text)
			{
				Text _cmp = Instantiate(GetPanelObject<Text>("Text Function"), ASSPanel.transform);
				_cmp.name = _name;
				_cmp.text = _text;
				_cmp.transform.localPosition = new Vector3(_cmp.transform.localPosition.x + 40, UI.ContainerOffsetMinY + (UI.MenuitemHeightOffsetY * i), _cmp.transform.localPosition.z);
			}

			internal static void CreateUIToggle(string _name, int i, bool _show)
			{
				Toggle _toggle = Instantiate(GetPanelObject<Toggle>("Toggle Function"), ASSPanel.transform);
				_toggle.name = _name;
				_toggle.isOn = _show;
				_toggle.transform.localPosition = new Vector3(_toggle.transform.localPosition.x - 75, UI.ContainerOffsetMinY + (UI.MenuitemHeightOffsetY * i), _toggle.transform.localPosition.z);
				_toggle.onValueChanged.RemoveAllListeners();
				_toggle.onValueChanged.AddListener(value =>
				{
					AccStateSyncController _pluginCtrl = GetController(_curOCIChar);
					if (_pluginCtrl == null) return;

					_pluginCtrl.OnVirtualGroupStateChange(_name, value);

					if (JetPack.CharaStudio.RefreshCharaStatePanel())
						MoreAccessories.UpdateUI();
				});
			}
		}
	}
}
