using System;
using System.Collections.Generic;
using System.Linq;

using Studio;

using HarmonyLib;

namespace JetPack
{
	public partial class CharaStudio
	{
		public static OCIChar CurOCIChar;
		public static int CurTreeNodeObjID = -1;

		internal static void TreeNodesInit()
		{
			OnSelectNodeChange += (_sender, _args) =>
			{
				Core.DebugLog($"[OnSelectNodeChange][{_args.OldNodeID}][{_args.NewNodeID}]");
			};

			//Core.DebugLog($"[TreeNodesInit][(OnSelectSingle: {OnSelectSingle?.GetInvocationList()?.Length}]");
			OnSelectSingle += (_sender, _args) =>
			{
				int _old = CurTreeNodeObjID;

				if (_args.SelectNode == null || ListSelectNodes.Count == 0)
				{
					if (_old > -1)
					{
						CurOCIChar = null;
						CurTreeNodeObjID = -1;

						OnSelectNodeChange?.Invoke(Instance.treeNodeCtrl, new TreeNodesSelectChangeEventArgs(_old, CurTreeNodeObjID));
					}
					return;
				}

				if (Instance.dicInfo.TryGetValue(_args.SelectNode, out ObjectCtrlInfo _info))
				{
					CurTreeNodeObjID = GetSceneId(_info);
					if (_old != CurTreeNodeObjID)
					{
						OCIChar _selected = _info as OCIChar;
						if (_selected?.GetType() != null)
							CurOCIChar = _selected;
						else
							CurOCIChar = null;

						OnSelectNodeChange?.Invoke(Instance.treeNodeCtrl, new TreeNodesSelectChangeEventArgs(_old, CurTreeNodeObjID));
					}
				}
			};

			_hookInstance.PatchAll(typeof(HooksTreeNodes));
		}

		//public static List<TreeNodeObject> ListSelectNodes => Traverse.Create(StudioInstance.treeNodeCtrl).Property("selectNodes").GetValue<TreeNodeObject[]>().ToList();
		public static List<TreeNodeObject> ListSelectNodes => Instance.treeNodeCtrl.selectNodes?.ToList();

		public static int GetSceneId(ObjectCtrlInfo _info)
		{
			if (_info == null) throw new ArgumentNullException(nameof(_info));
			if (Instance == null) throw new InvalidOperationException("Studio is not initialized yet!");

			foreach (KeyValuePair<int, ObjectCtrlInfo> x in Instance.dicObjectCtrl)
			{
				if (x.Value == _info)
					return x.Key;
			}
			return -1;
		}

		public static event EventHandler<TreeNodesSelectChangeEventArgs> OnSelectNodeChange;

		public class TreeNodesSelectChangeEventArgs : EventArgs
		{
			public TreeNodesSelectChangeEventArgs(int _oldNodeID, int _newNodeID)
			{
				OldNodeID = _oldNodeID;
				NewNodeID = _newNodeID;
			}

			public int OldNodeID { get; }
			public int NewNodeID { get; }
		}

		public static event EventHandler<TreeNodesEventArgs> OnSelectMultiple;

		public class TreeNodesEventArgs : EventArgs
		{
			public TreeNodesEventArgs()
			{
				SelectNodes = ListSelectNodes;
			}

			public List<TreeNodeObject> SelectNodes { get; }
		}

		public static event EventHandler<TreeNodeEventArgs> OnSelectSingle;

		public class TreeNodeEventArgs : EventArgs
		{
			public TreeNodeEventArgs(TreeNodeObject _node)
			{
				SelectNode = _node;
			}

			public TreeNodeObject SelectNode { get; }
		}

		internal partial class HooksTreeNodes
		{
			[HarmonyPriority(Priority.Last)]
			[HarmonyPostfix, HarmonyPatch(typeof(TreeNodeCtrl), nameof(TreeNodeCtrl.SelectSingle))]
			private static void TreeNodeCtrl_SelectSingle_Postfix(TreeNodeCtrl __instance, TreeNodeObject _node, bool _deselect)
			{
				OnSelectSingle?.Invoke(__instance, new TreeNodeEventArgs(_node));
			}

			[HarmonyPriority(Priority.Last)]
			[HarmonyPostfix, HarmonyPatch(typeof(TreeNodeCtrl), nameof(TreeNodeCtrl.SelectMultiple))]
			private static void TreeNodeCtrl_SelectMultiple_Postfix(TreeNodeCtrl __instance, TreeNodeObject _start, TreeNodeObject _end)
			{
				OnSelectSingle?.Invoke(__instance, new TreeNodeEventArgs(_start));
				OnSelectMultiple?.Invoke(__instance, null);
			}

			[HarmonyPriority(Priority.First)]
			[HarmonyPrefix, HarmonyPatch(typeof(Studio.Studio), nameof(Studio.Studio.InitScene))]
			private static void Studio_InitScene_Prefix(bool _close)
			{
				Core.DebugLog($"Studio_InitScene_Prefix");
				OnSelectSingle?.Invoke(null, new TreeNodeEventArgs(null));
			}
		}
	}
}
