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
	public partial class Studio
	{
		internal static void SceneInit()
		{
			HarmonyInstance.PatchAll(typeof(HooksScene));
		}

		public static event EventHandler<SceneLoadEventArgs> OnSceneLoad;
		public enum SceneLoadMode { Save, Load, Import }
		public enum SceneLoadState { Pre, Post, Coroutine }
		public class SceneLoadEventArgs : EventArgs
		{
			public SceneLoadEventArgs(string path, SceneLoadMode mode, SceneLoadState state)
			{
				Path = path;
				Mode = mode;
				State = state;
			}

			public string Path { get; }
			public SceneLoadMode Mode { get; }
			public SceneLoadState State { get; }
		}

		internal partial class HooksScene
		{
			[HarmonyPriority(Priority.First)]
			[HarmonyPrefix, HarmonyPatch(typeof(global::Studio.Studio), nameof(global::Studio.Studio.SaveScene))]
			private static void Studio_SaveScene_Prefix()
			{
				Core.DebugLog($"Studio_SaveScene_Prefix");
				OnSceneLoad?.Invoke(null, new SceneLoadEventArgs(null, SceneLoadMode.Save, SceneLoadState.Pre));
			}

			[HarmonyPriority(Priority.Last)]
			[HarmonyPostfix, HarmonyPatch(typeof(global::Studio.Studio), nameof(global::Studio.Studio.SaveScene))]
			private static void Studio_SaveScene_Postfix()
			{
				Core.DebugLog($"Studio_SaveScene_Postfix");
				OnSceneLoad?.Invoke(null, new SceneLoadEventArgs(null, SceneLoadMode.Save, SceneLoadState.Post));
				StudioInstance.StartCoroutine(Studio_LoadSceneCoroutine_Postfix_Coroutine(null, SceneLoadMode.Save));
			}

			[HarmonyPriority(Priority.First)]
			[HarmonyPrefix, HarmonyPatch(typeof(global::Studio.Studio), nameof(global::Studio.Studio.ImportScene))]
			private static void Studio_ImportScene_Prefix(string _path)
			{
				Core.DebugLog($"Studio_ImportScene_Prefix");
				OnSceneLoad?.Invoke(null, new SceneLoadEventArgs(_path, SceneLoadMode.Import, SceneLoadState.Pre));
			}

			[HarmonyPriority(Priority.Last)]
			[HarmonyPostfix, HarmonyPatch(typeof(global::Studio.Studio), nameof(global::Studio.Studio.ImportScene))]
			private static void Studio_ImportScene_Postfix(string _path)
			{
				Core.DebugLog($"Studio_ImportScene_Postfix");
				OnSceneLoad?.Invoke(null, new SceneLoadEventArgs(_path, SceneLoadMode.Import, SceneLoadState.Post));
				StudioInstance.StartCoroutine(Studio_LoadSceneCoroutine_Postfix_Coroutine(_path, SceneLoadMode.Import));
			}

			[HarmonyPriority(Priority.Last)]
			[HarmonyPostfix, HarmonyPatch(typeof(global::Studio.Studio), nameof(global::Studio.Studio.LoadSceneCoroutine))]
			private static void Studio_LoadSceneCoroutine_Postfix(string _path)
			{
				Core.DebugLog($"Studio_LoadSceneCoroutine_Postfix");
				OnSceneLoad?.Invoke(null, new SceneLoadEventArgs(_path, SceneLoadMode.Load, SceneLoadState.Post));
				StudioInstance.StartCoroutine(Studio_LoadSceneCoroutine_Postfix_Coroutine(_path, SceneLoadMode.Load));
			}

			internal static IEnumerator Studio_LoadSceneCoroutine_Postfix_Coroutine(string path, SceneLoadMode mode)
			{
				yield return new WaitForEndOfFrame();
				yield return new WaitForEndOfFrame();

				Core.DebugLog($"Studio_LoadSceneCoroutine_Postfix_Coroutine [mode: {mode}]");
				OnSceneLoad?.Invoke(null, new SceneLoadEventArgs(path, SceneLoadMode.Load, SceneLoadState.Coroutine));
			}
		}
	}
}
