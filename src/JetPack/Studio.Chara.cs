using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;
using Studio;

using HarmonyLib;

namespace JetPack
{
	public partial class CharaStudio
	{
		public static int OnLoadingChara { get; internal set; } = 0;

		internal static void CharaInit()
		{
			OnCharaLoad += (_sender, _args) =>
			{
				if (_args.State == CharaLoadState.Pre)
					OnLoadingChara++;
				else if (_args.State == CharaLoadState.Coroutine)
					OnLoadingChara--;
				Core.DebugLog($"[OnCharaLoad][CharaLoadState: {_args.Mode} {_args.State}][Count: {OnLoadingChara}]");
			};

			_hookInstance.PatchAll(typeof(HooksChara));
		}

		public static IEnumerable<OCIChar> GetSelectedOCIChar()
		{
			return GetSelectedObjects().OfType<OCIChar>();
		}

		public static IEnumerable<ObjectCtrlInfo> GetSelectedObjects()
		{
			if (!Loaded)
				yield break;

			for (int i = 0; i < ListSelectNodes.Count; i++)
				if (Instance.dicInfo.TryGetValue(ListSelectNodes[i], out ObjectCtrlInfo _info))
					yield return _info;
		}

		public static ChaControl GetChaControl(OICharInfo _chara)
		{
			if (Instance == null) throw new InvalidOperationException("Studio is not initialized yet!");
			return Instance.dicInfo.Values.OfType<OCIChar>().Select(x => x.charInfo).FirstOrDefault(x => x.chaFile == _chara.charFile);
		}

		public static ChaControl GetChaControl(OCIChar _chara)
		{
			if (Instance == null) throw new InvalidOperationException("Studio is not initialized yet!");
			return _chara?.charInfo;
		}

		public static OCIChar GetOCIChar(OICharInfo _chara)
		{
			if (Instance == null) throw new InvalidOperationException("Studio is not initialized yet!");
			return Instance.dicInfo.Values.OfType<OCIChar>().FirstOrDefault(x => x.charInfo.chaFile == _chara.charFile);
		}

		public static OCIChar GetOCIChar(ChaControl _chara)
		{
			if (Instance == null) throw new InvalidOperationException("Studio is not initialized yet!");
			return Instance.dicInfo.Values.OfType<OCIChar>().FirstOrDefault(x => x.charInfo == _chara);
		}

		public static MPCharCtrl GetMPCharCtrl(OCIChar _chara)
		{
			MPCharCtrl[] _charas = GameObject.FindObjectsOfType<MPCharCtrl>();
			foreach (MPCharCtrl _charCtrl in _charas)
			{
				if (_charCtrl.ociChar == _chara)
					return _charCtrl;
			}
			return null;
		}

		public static bool RefreshCharaStatePanel()
		{
			if (!Loaded) return false;
			if (CurOCIChar == null) return false;
			MPCharCtrl _chara = GetMPCharCtrl(CurOCIChar);
			if (_chara == null) return false;
			int _select = Traverse.Create(_chara).Field<int>("select").Value;
			if (_select != 0) return false;
			_chara.OnClickRoot(0);
			return true;
		}

		public static event EventHandler<CharaLoadEventArgs> OnCharaLoad;
		public enum CharaLoadMode { Add, Change, Load }
		public enum CharaLoadState { Pre, Post, Coroutine }
		public class CharaLoadEventArgs : EventArgs
		{
			public CharaLoadEventArgs(OCIChar _chara, CharaLoadMode _mode, CharaLoadState _state)
			{
				ChaControl = _chara?.charInfo;
				OCIChar = _chara;
				Mode = _mode;
				State = _state;
			}

			public CharaLoadEventArgs(ChaControl _chara, CharaLoadMode _mode, CharaLoadState _state)
			{
				ChaControl = _chara;
				OCIChar = GetOCIChar(_chara);
				Mode = _mode;
				State = _state;
			}

			public CharaLoadEventArgs(OICharInfo _chara, CharaLoadMode _mode, CharaLoadState _state)
			{
				ChaControl = GetChaControl(_chara);
				OCIChar = GetOCIChar(_chara);
				Mode = _mode;
				State = _state;
			}

			public ChaControl ChaControl { get; }
			public OCIChar OCIChar { get; }
			public CharaLoadMode Mode { get; }
			public CharaLoadState State { get; }
		}

		internal partial class HooksChara
		{
			[HarmonyPriority(Priority.First)]
			[HarmonyPrefix, HarmonyPatch(typeof(OCIChar), nameof(OCIChar.ChangeChara), new[] { typeof(string) })]
			private static void OCIChar_ChangeChara_Prefix(OCIChar __instance, string _path)
			{
				// only fires when switching chara
				Core.DebugLog($"OCIChar_ChangeChara_Prefix");
				OnCharaLoad?.Invoke(null, new CharaLoadEventArgs(__instance, CharaLoadMode.Change, CharaLoadState.Pre));
			}

			[HarmonyPriority(Priority.Last)]
			[HarmonyPostfix, HarmonyPatch(typeof(OCIChar), nameof(OCIChar.ChangeChara), new[] { typeof(string) })]
			private static void OCIChar_ChangeChara_Postfix(OCIChar __instance, string _path)
			{
				// only fires when switching chara
				Core.DebugLog($"OCIChar_ChangeChara_Postfix");
				OnCharaLoad?.Invoke(null, new CharaLoadEventArgs(__instance, CharaLoadMode.Change, CharaLoadState.Post));
				//StudioInstance.StartCoroutine(OCIChar_ChangeChara_Coroutine(__instance, CharaLoadState.Post));
				Instance.StartCoroutine(OCIChar_ChangeChara_Coroutine(__instance, CharaLoadState.Coroutine));
			}

			internal static IEnumerator OCIChar_ChangeChara_Coroutine(OCIChar _chara, CharaLoadState _state)
			{
				yield return new WaitForEndOfFrame();
				yield return new WaitForEndOfFrame();

				yield return new WaitForEndOfFrame();
				yield return new WaitForEndOfFrame();

				Core.DebugLog($"OCIChar_ChangeChara_Postfix_Coroutine [state: {_state}]");
				/*
				if (state == CharaLoadState.Post)
					StudioInstance.StartCoroutine(OCIChar_ChangeChara_Coroutine(chara, CharaLoadState.Coroutine));
				else
				*/
					OnCharaLoad?.Invoke(null, new CharaLoadEventArgs(_chara, CharaLoadMode.Change, _state));
			}

			[HarmonyPriority(Priority.First)]
			[HarmonyPrefix]
			[HarmonyPatch(typeof(AddObjectFemale), nameof(AddObjectFemale.Add), new[] { typeof(string) })]
			[HarmonyPatch(typeof(AddObjectMale), nameof(AddObjectMale.Add), new[] { typeof(string) })]
			private static void AddObjectChara_Add_Prefix()
			{
				// only fires when adding chara
				Core.DebugLog($"AddObjectChara_Add_Prefix");
				OnCharaLoad?.Invoke(null, new CharaLoadEventArgs(new OCIChar(), CharaLoadMode.Add, CharaLoadState.Pre));
			}

			[HarmonyPriority(Priority.Last)]
			[HarmonyPostfix]
			[HarmonyPatch(typeof(AddObjectFemale), nameof(AddObjectFemale.Add), new[] { typeof(string) })]
			[HarmonyPatch(typeof(AddObjectMale), nameof(AddObjectMale.Add), new[] { typeof(string) })]
			private static void AddObjectChara_Add_Postfix()
			{
				// only fires when adding chara
				Core.DebugLog($"AddObjectChara_Add_Postfix");
				OnCharaLoad?.Invoke(null, new CharaLoadEventArgs(new OCIChar(), CharaLoadMode.Add, CharaLoadState.Post));
				Instance.StartCoroutine(AddObjectChara_Coroutine(null, CharaLoadMode.Add));
			}
			/*
			[HarmonyPriority(Priority.First)]
			[HarmonyPrefix]
			[HarmonyPatch(typeof(AddObjectFemale), nameof(AddObjectFemale.Add), new[] { typeof(ChaControl), typeof(OICharInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject), typeof(bool), typeof(int) })]
			[HarmonyPatch(typeof(AddObjectMale), nameof(AddObjectMale.Add), new[] { typeof(ChaControl), typeof(OICharInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject), typeof(bool), typeof(int) })]
			private static void AddObjectChara_Add_Prefix_x6()
			{
				// fires on adding chara and loading scene
				Core.DebugLog($"AddObjectChara_Add_Prefix_x6");
			}

			[HarmonyPriority(Priority.Last)]
			[HarmonyPostfix]
			[HarmonyPatch(typeof(AddObjectFemale), nameof(AddObjectFemale.Add), new[] { typeof(ChaControl), typeof(OICharInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject), typeof(bool), typeof(int) })]
			[HarmonyPatch(typeof(AddObjectMale), nameof(AddObjectMale.Add), new[] { typeof(ChaControl), typeof(OICharInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject), typeof(bool), typeof(int) })]
			private static void AddObjectChara_Add_Postfix_x6()
			{
				// fires on adding chara and loading scene
				Core.DebugLog($"AddObjectChara_Add_Postfix_x6");
			}
			*/
			[HarmonyPriority(Priority.First)]
			[HarmonyPrefix]
			[HarmonyPatch(typeof(AddObjectFemale), nameof(AddObjectFemale.Load), new[] { typeof(OICharInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject) })]
			[HarmonyPatch(typeof(AddObjectMale), nameof(AddObjectMale.Load), new[] { typeof(OICharInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject) })]
			private static void AddObjectChara_Load_Prefix(OICharInfo _info)
			{
				// only fires when loading scene
				Core.DebugLog($"AddObjectChara_Load_Prefix");
				OnCharaLoad?.Invoke(null, new CharaLoadEventArgs(_info, CharaLoadMode.Load, CharaLoadState.Pre));
			}

			[HarmonyPriority(Priority.Last)]
			[HarmonyPostfix]
			[HarmonyPatch(typeof(AddObjectFemale), nameof(AddObjectFemale.Load), new[] { typeof(OICharInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject) })]
			[HarmonyPatch(typeof(AddObjectMale), nameof(AddObjectMale.Load), new[] { typeof(OICharInfo), typeof(ObjectCtrlInfo), typeof(TreeNodeObject) })]
			private static void AddObjectChara_Load_Postfix(OICharInfo _info)
			{
				// only fires when loading scene
				Core.DebugLog($"AddObjectChara_Load_Postfix");
				OnCharaLoad?.Invoke(null, new CharaLoadEventArgs(_info, CharaLoadMode.Load, CharaLoadState.Post));
				Instance.StartCoroutine(AddObjectChara_Coroutine(GetOCIChar(_info), CharaLoadMode.Load));
			}

			internal static IEnumerator AddObjectChara_Coroutine(OCIChar _chara, CharaLoadMode _mode)
			{
				yield return new WaitForEndOfFrame();
				yield return new WaitForEndOfFrame();

				OnCharaLoad?.Invoke(null, new CharaLoadEventArgs(_chara, _mode, CharaLoadState.Coroutine));
				Core.DebugLog($"AddObjectChara_Coroutine [mode: {_mode}]");
			}
		}
	}
}
