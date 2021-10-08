using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;
using Studio;
using UniRx;

using KKAPI.Studio.UI;
using KKAPI.Utilities;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		internal static class CharaStudio
		{
			internal static OCIChar _curOCIChar = null;
			internal static int _curTreeNodeObjID = -1;
			internal static ToolbarToggle _ttConfigWindow;
			internal static bool _duringSceneLoad = false;

			internal static AccStateSyncController GetController(OCIChar _chara) => _chara?.charInfo?.gameObject?.GetComponent<AccStateSyncController>();

			internal static void RegisterControls()
			{
				if (!JetPack.CharaStudio.Running) return;

				_charaConfigWindow = _instance.gameObject.AddComponent<AccStateSyncUI>();

				Texture2D _iconTex = TextureUtils.LoadTexture(ResourceUtils.GetEmbeddedResource("toolbar_icon.png"));
				_ttConfigWindow = CustomToolbarButtons.AddLeftToolbarToggle(_iconTex, false, _value => _charaConfigWindow.enabled = _value);

				if (MoreAccessories._installed)
				{
					List<Button> _buttons = GameObject.Find("StudioScene/Canvas Main Menu/02_Manipulate/00_Chara/01_State/Viewport/Content/Clothing Details").GetComponentsInChildren<Button>().ToList();
					_buttons.Add(GameObject.Find($"StudioScene/Canvas Main Menu/02_Manipulate/00_Chara/01_State/Viewport/Content/Cos/Button Shoes 1").GetComponent<Button>());
					_buttons.Add(GameObject.Find($"StudioScene/Canvas Main Menu/02_Manipulate/00_Chara/01_State/Viewport/Content/Cos/Button Shoes 2").GetComponent<Button>());
					foreach (Button _button in _buttons)
					{
						_button.onClick.AddListener(delegate ()
						{
							_instance.StartCoroutine(StatusPanelUpdateCoroutine());
						});
					}
				}

				JetPack.CharaStudio.OnSelectNodeChange += (_sender, _args) =>
				{
					_curTreeNodeObjID = JetPack.CharaStudio.CurTreeNodeObjID;
					_curOCIChar = JetPack.CharaStudio.CurOCIChar;
					UpdateUI();
				};

				JetPack.CharaStudio.OnSceneLoad += (_sender, _args) =>
				{
					if (_args.Mode == JetPack.CharaStudio.SceneLoadMode.Save) return;

					if (_args.State == JetPack.CharaStudio.SceneLoadState.Pre)
						_duringSceneLoad = true;

					if (_args.Mode == JetPack.CharaStudio.SceneLoadMode.Load && _args.State == JetPack.CharaStudio.SceneLoadState.Post)
						_duringSceneLoad = false;

					if (_args.Mode == JetPack.CharaStudio.SceneLoadMode.Import && _args.State == JetPack.CharaStudio.SceneLoadState.Coroutine)
						_duringSceneLoad = false;
				};

				HarmonyLib.Harmony.CreateAndPatchAll(typeof(HooksCharaStudio));
			}

			internal static IEnumerator StatusPanelUpdateCoroutine()
			{
				yield return JetPack.Toolbox.WaitForEndOfFrame;
				yield return JetPack.Toolbox.WaitForEndOfFrame;

				if (MoreAccessories._installed)
				{
					AccStateSyncController _pluginCtrl = GetController(_curOCIChar);
					if (_pluginCtrl != null && _pluginCtrl.TriggerEnabled)
					{
						if (JetPack.CharaStudio.RefreshCharaStatePanel())
							MoreAccessories.UpdateUI();
					}
				}
			}

			internal static bool IsCharaSelected(ChaControl _chaCtrl)
			{
				List<TreeNodeObject> _selectNodes = JetPack.CharaStudio.ListSelectNodes;
				if (_selectNodes?.Count == 0)
					return false;
				for (int i = 0; i < _selectNodes.Count; i++)
				{
					if (Studio.Studio.Instance.dicInfo.TryGetValue(_selectNodes[i], out ObjectCtrlInfo _info))
					{
						OCIChar _selected = _info as OCIChar;
						if (_selected != null && _selected.GetType() != null)
						{
							if (_selected.charInfo == _chaCtrl)
								return true;
						}
					}
				}
				return false;
			}

			internal static void UpdateUI()
			{
				if ((_curOCIChar == null) || (_curOCIChar.charInfo == null))
					return;
				AccStateSyncController _pluginCtrl = GetController(_curOCIChar);
				if (_pluginCtrl == null)
					return;

				_pluginCtrl._treeNodeObjID = _curTreeNodeObjID;
			}
		}
	}
}
