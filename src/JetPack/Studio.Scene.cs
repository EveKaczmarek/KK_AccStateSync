using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

using HarmonyLib;

namespace JetPack
{
	public partial class CharaStudio
	{
		internal static void SceneInit()
		{
			_hookInstance.PatchAll(typeof(HooksScene));
		}

		public static event EventHandler<SceneLoadEventArgs> OnSceneLoad;
		public enum SceneLoadMode { Save, Load, Import }
		public enum SceneLoadState { Pre, Post, Coroutine }
		public class SceneLoadEventArgs : EventArgs
		{
			public SceneLoadEventArgs(string _path, SceneLoadMode _mode, SceneLoadState _state)
			{
				Path = _path;
				Mode = _mode;
				State = _state;
			}

			public string Path { get; }
			public SceneLoadMode Mode { get; }
			public SceneLoadState State { get; }
		}

		internal partial class HooksScene
		{
			[HarmonyPriority(Priority.First)]
			[HarmonyPrefix, HarmonyPatch(typeof(Studio.Studio), nameof(Studio.Studio.SaveScene))]
			private static void Studio_SaveScene_Prefix()
			{
				Core.DebugLog($"Studio_SaveScene_Prefix");
				OnSceneLoad?.Invoke(null, new SceneLoadEventArgs(null, SceneLoadMode.Save, SceneLoadState.Pre));
			}

			[HarmonyPriority(Priority.Last)]
			[HarmonyPostfix, HarmonyPatch(typeof(Studio.Studio), nameof(Studio.Studio.SaveScene))]
			private static void Studio_SaveScene_Postfix()
			{
				Core.DebugLog($"Studio_SaveScene_Postfix");
				OnSceneLoad?.Invoke(null, new SceneLoadEventArgs(null, SceneLoadMode.Save, SceneLoadState.Post));
				Instance.StartCoroutine(Studio_SaveScene_Postfix_Coroutine(null, SceneLoadMode.Save));
			}

			internal static IEnumerator Studio_SaveScene_Postfix_Coroutine(string _path, SceneLoadMode _mode)
			{
				yield return Toolbox.WaitForEndOfFrame;
				yield return Toolbox.WaitForEndOfFrame;
				Core.DebugLog($"Studio_SaveScene_Postfix_Coroutine [mode: {_mode}]");
				OnSceneLoad?.Invoke(null, new SceneLoadEventArgs(_path, SceneLoadMode.Save, SceneLoadState.Coroutine));
			}

			[HarmonyPriority(Priority.First)]
			[HarmonyPrefix, HarmonyPatch(typeof(Studio.Studio), nameof(Studio.Studio.ImportScene))]
			private static void Studio_ImportScene_Prefix(string _path)
			{
				Core.DebugLog($"Studio_ImportScene_Prefix");
				OnSceneLoad?.Invoke(null, new SceneLoadEventArgs(_path, SceneLoadMode.Import, SceneLoadState.Pre));
			}

			[HarmonyPriority(Priority.Last)]
			[HarmonyPostfix, HarmonyPatch(typeof(Studio.Studio), nameof(Studio.Studio.ImportScene))]
			private static void Studio_ImportScene_Postfix(string _path)
			{
				Core.DebugLog($"Studio_ImportScene_Postfix");
				OnSceneLoad?.Invoke(null, new SceneLoadEventArgs(_path, SceneLoadMode.Import, SceneLoadState.Post));
				Instance.StartCoroutine(Studio_ImportScene_Postfix_Coroutine(_path, SceneLoadMode.Import));
			}

			internal static IEnumerator Studio_ImportScene_Postfix_Coroutine(string _path, SceneLoadMode _mode)
			{
				yield return Toolbox.WaitForEndOfFrame;
				yield return Toolbox.WaitForEndOfFrame;
				Core.DebugLog($"Studio_ImportScene_Postfix_Coroutine [mode: {_mode}]");
				OnSceneLoad?.Invoke(null, new SceneLoadEventArgs(_path, SceneLoadMode.Import, SceneLoadState.Coroutine));
			}

			[HarmonyPriority(Priority.Last)]
			[HarmonyPrefix, HarmonyPatch(typeof(Studio.Studio), nameof(Studio.Studio.LoadSceneCoroutine))]
			private static void Studio_LoadSceneCoroutine_Prefix(string _path)
			{
				Core.DebugLog($"Studio_LoadSceneCoroutine_Prefix");
				OnSceneLoad?.Invoke(null, new SceneLoadEventArgs(_path, SceneLoadMode.Load, SceneLoadState.Pre));
			}

			[HarmonyPriority(Priority.Last)]
			[HarmonyPostfix, HarmonyPatch(typeof(Studio.Studio), nameof(Studio.Studio.LoadSceneCoroutine))]
			private static void Studio_LoadSceneCoroutine_Postfix(string _path, ref IEnumerator __result)
			{
				IEnumerator original = __result;
				__result = new[] { original, Studio_LoadSceneCoroutine_Postfix_Coroutine(_path) }.GetEnumerator();
			}

			private static IEnumerator Studio_LoadSceneCoroutine_Postfix_Coroutine(string _path)
			{
				Core.DebugLog($"Studio_LoadSceneCoroutine_Postfix_Coroutine");
				OnSceneLoad?.Invoke(null, new SceneLoadEventArgs(_path, SceneLoadMode.Load, SceneLoadState.Post));
				yield break;
			}
		}
	}
}
