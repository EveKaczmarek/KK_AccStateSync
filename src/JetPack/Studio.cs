using System;

using UnityEngine.SceneManagement;
using Studio;

using HarmonyLib;

namespace JetPack
{
	public partial class CharaStudio
	{
		public static Studio.Studio Instance => Studio.Studio.Instance;

		public static bool Running { get; internal set; }
		public static bool Loaded { get; internal set; }

		internal static Harmony _hookInstance;

		internal static void SceneLoaded(Scene _scene, LoadSceneMode _loadSceneMode)
		{
			Core.DebugLog($"[SceneLoaded][name: {_scene.name}][mode: {_loadSceneMode}]");
			if (!Loaded && _scene.name == "Studio")
			{
				Loaded = true;
				OnStudioLoaded?.Invoke(null, null);
			}
		}

		public static event EventHandler OnStudioLoaded;

		internal static void RegisterControls(object _sender, EventArgs _args)
		{
			_hookInstance = Harmony.CreateAndPatchAll(typeof(Hooks));
			CharaInit();
			TreeNodesInit();
			SceneInit();
		}

		internal class Hooks { }
	}

	public static partial class Extensions
	{
		public static ChaControl GetChaControl(this OICharInfo _self) => CharaStudio.GetChaControl(_self);
		public static ChaControl GetChaControl(this OCIChar _self) => CharaStudio.GetChaControl(_self);
	}
}
