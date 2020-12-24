using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Studio;

using HarmonyLib;

namespace JetPack
{
	public partial class Studio
	{
		public static OCIChar CurOCIChar;
		public static int CurTreeNodeObjID = -1;

		internal static void TreeNodesInit()
		{
			OnSelectNodeChange += (sender, args) =>
			{
				Core.DebugLog($"[OnSelectNodeChange][{args.OldNodeID}][{args.NewNodeID}]");
			};

			//Core.DebugLog($"[TreeNodesInit][(OnSelectSingle: {OnSelectSingle?.GetInvocationList()?.Length}]");
			OnSelectSingle += (sender, args) =>
			{
				int OldTreeNodeObjID = CurTreeNodeObjID;

				if (args.SelectNode == null || ListSelectNodes.Count == 0)
				{
					if (OldTreeNodeObjID > -1)
					{
						CurOCIChar = null;
						CurTreeNodeObjID = -1;

						OnSelectNodeChange?.Invoke(StudioInstance.treeNodeCtrl, new TreeNodesSelectChangeEventArgs(OldTreeNodeObjID, CurTreeNodeObjID));
					}
					return;
				}

				if (StudioInstance.dicInfo.TryGetValue(args.SelectNode, out ObjectCtrlInfo info))
				{
					CurTreeNodeObjID = GetSceneId(info);
					if (OldTreeNodeObjID != CurTreeNodeObjID)
					{
						OCIChar selected = info as OCIChar;
						if (selected?.GetType() != null)
							CurOCIChar = selected;
						else
							CurOCIChar = null;

						OnSelectNodeChange?.Invoke(StudioInstance.treeNodeCtrl, new TreeNodesSelectChangeEventArgs(OldTreeNodeObjID, CurTreeNodeObjID));
					}
				}
			};

			HarmonyInstance.PatchAll(typeof(HooksTreeNodes));
		}

		public static List<TreeNodeObject> ListSelectNodes => Traverse.Create(StudioInstance.treeNodeCtrl).Property("selectNodes").GetValue<TreeNodeObject[]>().ToList();

		public static int GetSceneId(ObjectCtrlInfo obj)
		{
			if (obj == null) throw new ArgumentNullException(nameof(obj));
			if (StudioInstance == null) throw new InvalidOperationException("Studio is not initialized yet!");

			foreach (KeyValuePair<int, ObjectCtrlInfo> info in StudioInstance.dicObjectCtrl)
			{
				if (info.Value == obj)
					return info.Key;
			}
			return -1;
		}

		public static event EventHandler<TreeNodesSelectChangeEventArgs> OnSelectNodeChange;

		public class TreeNodesSelectChangeEventArgs : EventArgs
		{
			public TreeNodesSelectChangeEventArgs(int oldNodeID, int newNodeID)
			{
				OldNodeID = oldNodeID;
				NewNodeID = newNodeID;
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
			public TreeNodeEventArgs(TreeNodeObject selectNode)
			{
				SelectNode = selectNode;
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
			[HarmonyPrefix, HarmonyPatch(typeof(global::Studio.Studio), nameof(global::Studio.Studio.InitScene))]
			private static void Studio_InitScene_Prefix(bool _close)
			{
				Core.DebugLog($"Studio_InitScene_Prefix");
				OnSelectSingle?.Invoke(null, new TreeNodeEventArgs(null));
			}
		}
	}
}
