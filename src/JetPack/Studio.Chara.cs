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
	public partial class Studio
	{
		public static int OnLoadingChara { get; internal set; } = 0;

		internal static void CharaInit()
		{
			OnCharaLoad += (sender, args) =>
			{
				if (args.State == CharaLoadState.Pre)
					OnLoadingChara++;
				else if (args.State == CharaLoadState.Coroutine)
					OnLoadingChara--;
				Core.DebugLog($"[OnCharaLoad][CharaLoadState: {args.Mode} {args.State}][Count: {OnLoadingChara}]");
			};

			HarmonyInstance.PatchAll(typeof(HooksChara));
		}

		public static ChaControl GetChaControl(OICharInfo chara)
		{
			if (StudioInstance == null) throw new InvalidOperationException("Studio is not initialized yet!");
			return StudioInstance.dicInfo.Values.OfType<OCIChar>().Select(x => x.charInfo).FirstOrDefault(x => x.chaFile == chara.charFile);
		}

		public static OCIChar GetOCIChar(OICharInfo chara)
		{
			if (StudioInstance == null) throw new InvalidOperationException("Studio is not initialized yet!");
			return StudioInstance.dicInfo.Values.OfType<OCIChar>().FirstOrDefault(x => x.charInfo.chaFile == chara.charFile);
		}

		public static OCIChar GetOCIChar(ChaControl chara)
		{
			if (StudioInstance == null) throw new InvalidOperationException("Studio is not initialized yet!");
			return StudioInstance.dicInfo.Values.OfType<OCIChar>().FirstOrDefault(x => x.charInfo == chara);
		}

		public static MPCharCtrl GetMPCharCtrl(OCIChar chara)
		{
			MPCharCtrl[] charas = GameObject.FindObjectsOfType<MPCharCtrl>();
			foreach (MPCharCtrl charCtrl in charas)
			{
				if (charCtrl.ociChar == chara)
					return charCtrl;
			}
			return null;
		}

		public static event EventHandler<CharaLoadEventArgs> OnCharaLoad;
		public enum CharaLoadMode { Add, Change, Load }
		public enum CharaLoadState { Pre, Post, Coroutine }
		public class CharaLoadEventArgs : EventArgs
		{
			public CharaLoadEventArgs(OCIChar chara, CharaLoadMode mode, CharaLoadState state)
			{
				ChaControl = chara?.charInfo;
				OCIChar = chara;
				Mode = mode;
				State = state;
			}

			public CharaLoadEventArgs(ChaControl chara, CharaLoadMode mode, CharaLoadState state)
			{
				ChaControl = chara;
				OCIChar = GetOCIChar(chara);
				Mode = mode;
				State = state;
			}

			public CharaLoadEventArgs(OICharInfo chara, CharaLoadMode mode, CharaLoadState state)
			{
				ChaControl = GetChaControl(chara);
				OCIChar = GetOCIChar(chara);
				Mode = mode;
				State = state;
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
				StudioInstance.StartCoroutine(OCIChar_ChangeChara_Coroutine(__instance, CharaLoadState.Coroutine));
			}

			internal static IEnumerator OCIChar_ChangeChara_Coroutine(OCIChar chara, CharaLoadState state)
			{
				yield return new WaitForEndOfFrame();
				yield return new WaitForEndOfFrame();

				yield return new WaitForEndOfFrame();
				yield return new WaitForEndOfFrame();

				Core.DebugLog($"OCIChar_ChangeChara_Postfix_Coroutine [state: {state}]");
				/*
				if (state == CharaLoadState.Post)
					StudioInstance.StartCoroutine(OCIChar_ChangeChara_Coroutine(chara, CharaLoadState.Coroutine));
				else
				*/
					OnCharaLoad?.Invoke(null, new CharaLoadEventArgs(chara, CharaLoadMode.Change, state));
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
				StudioInstance.StartCoroutine(AddObjectChara_Coroutine(null, CharaLoadMode.Add));
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
				StudioInstance.StartCoroutine(AddObjectChara_Coroutine(GetOCIChar(_info), CharaLoadMode.Load));
			}

			internal static IEnumerator AddObjectChara_Coroutine(OCIChar chara, CharaLoadMode mode)
			{
				yield return new WaitForEndOfFrame();
				yield return new WaitForEndOfFrame();

				OnCharaLoad?.Invoke(null, new CharaLoadEventArgs(chara, mode, CharaLoadState.Coroutine));
				Core.DebugLog($"AddObjectChara_Coroutine [mode: {mode}]");
			}
		}
	}
}
