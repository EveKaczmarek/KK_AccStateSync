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
		public static global::Studio.Studio StudioInstance => global::Studio.Studio.Instance;

		public static bool Running { get; internal set; }
		public static bool Loaded { get; internal set; }

		internal static Harmony HarmonyInstance;

		internal static void RegisterControls(object sender, EventArgs args)
		{
			Loaded = true;
			HarmonyInstance = Harmony.CreateAndPatchAll(typeof(Hooks));
			CharaInit();
			TreeNodesInit();
			SceneInit();
		}

		internal class Hooks { }
	}
}
