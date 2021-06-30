using System;
using System.Collections.Generic;

using HarmonyLib;

namespace JetPack
{
	public partial class CharaHscene
	{
		public static bool Inside { get; internal set; }
		public static bool Loaded { get; internal set; }
		public static bool VR { get; internal set; }
		public static List<ChaControl> Heroine { get; internal set; }
		public static List<HSprite> Sprites { get; internal set; } = new List<HSprite>();

		internal static Harmony _hookInstance;
		public static Type HSceneProcType;

		public static event EventHandler OnHSceneStartLoading;
		public static event EventHandler<HSceneFinishedLoadingEventArgs> OnHSceneFinishedLoading;
		public class HSceneFinishedLoadingEventArgs : EventArgs
		{
			public HSceneFinishedLoadingEventArgs(List<ChaControl> _lstFemale)
			{
				Female = _lstFemale;
			}

			public List<ChaControl> Female { get; }
		}
		public static event EventHandler OnHSceneExiting;
		public static event EventHandler<HSceneSetClothStateStartMotionEventArgs> OnHSceneSetClothStateStartMotion;
		public class HSceneSetClothStateStartMotionEventArgs : EventArgs
		{
			public HSceneSetClothStateStartMotionEventArgs(List<ChaControl> _lstFemale)
			{
				Female = _lstFemale;
			}

			public List<ChaControl> Female { get; }
		}

		internal static void InvokeOnHSceneStartLoading(object _sender, EventArgs _args) => OnHSceneStartLoading?.Invoke(_sender, _args);

		internal static void Init()
		{
			OnHSceneStartLoading += (_sender, _args) =>
			{
				Core.DebugLog($"[OnHSceneStartLoading]");
			};

			OnHSceneFinishedLoading += (_sender, _args) =>
			{
				Core.DebugLog($"[OnHSceneFinishedLoading]");
			};

			OnHSceneExiting += (_sender, _args) =>
			{
				Core.DebugLog($"[OnHSceneExiting]");
			};

			OnHSceneSetClothStateStartMotion += (_sender, _args) =>
			{
				Core.DebugLog($"[OnHSceneSetClothStateStartMotion]");
			};
		}

		internal class Hooks
		{
			internal static void Init()
			{
				_hookInstance = Harmony.CreateAndPatchAll(typeof(Hooks));
				if (VR)
				{
					HSceneProcType = Type.GetType("VRHScene, Assembly-CSharp");
					_hookInstance.Patch(HSceneProcType.GetMethod("MapSameObjectDisable", AccessTools.all), postfix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.VRHScene_MapSameObjectDisable_PostFix)));
				}
				else
				{
					HSceneProcType = Type.GetType("HSceneProc, Assembly-CSharp");
					_hookInstance.Patch(HSceneProcType.GetMethod("MapSameObjectDisable", AccessTools.all), postfix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.HSceneProc_MapSameObjectDisable_PostFix)));
				}
				_hookInstance.Patch(HSceneProcType.GetMethod("OnDestroy", AccessTools.all), prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.HSceneProc_OnDestroy_Prefix)));
				_hookInstance.Patch(HSceneProcType.GetMethod("SetClothStateStartMotion", AccessTools.all), postfix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.HSceneProc_SetClothStateStartMotion_Postfix)));
			}

			private static void HSceneProc_OnDestroy_Prefix()
			{
				OnHSceneExiting?.Invoke(null, null);
				Inside = false;
				Loaded = false;
				Heroine = null;
				Sprites.Clear();
				_hookInstance.UnpatchAll(_hookInstance.Id);
				_hookInstance = null;
			}

			private static void HSceneProc_SetClothStateStartMotion_Postfix(List<ChaControl> ___lstFemale)
			{
				OnHSceneSetClothStateStartMotion?.Invoke(null, new HSceneSetClothStateStartMotionEventArgs(___lstFemale));
			}

			private static void HSceneProc_MapSameObjectDisable_PostFix(List<ChaControl> ___lstFemale, HSprite ___sprite)
			{
				if (Loaded) return;

				Loaded = true;
				Heroine = ___lstFemale;
				Sprites.Add(___sprite);
				OnHSceneFinishedLoading?.Invoke(null, new HSceneFinishedLoadingEventArgs(___lstFemale));
			}

			private static void VRHScene_MapSameObjectDisable_PostFix(List<ChaControl> ___lstFemale, HSprite[] ___sprites)
			{
				if (Loaded) return;

				Loaded = true;
				Heroine = ___lstFemale;
				foreach (HSprite _sprite in ___sprites)
					Sprites.Add(_sprite);
				OnHSceneFinishedLoading?.Invoke(null, new HSceneFinishedLoadingEventArgs(___lstFemale));
			}
		}
	}
}
