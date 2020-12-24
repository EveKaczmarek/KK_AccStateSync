using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UniRx;
using TMPro;
using ChaCustom;

using ExtensibleSaveFormat;
using HarmonyLib;
using Sideloader.AutoResolver;

namespace JetPack
{
	public partial class Chara
	{
		internal static void Init()
		{
			//Core.HarmonyInstance.PatchAll(typeof(Hooks));
		}

		public static event EventHandler<CharaCreateEventArgs> OnCharaCreate;

		public class CharaCreateEventArgs : EventArgs
		{
			public CharaCreateEventArgs(ChaControl chaCtrl, ChaFileControl chaFile)
			{
				ChaControl = chaCtrl;
				ChaFileControl = chaFile;
			}

			public ChaControl ChaControl { get; }
			public ChaFileControl ChaFileControl { get; }
		}

		internal class Hooks
		{
			[HarmonyPostfix, HarmonyPatch(typeof(Manager.Character), nameof(Manager.Character.CreateChara))]
			private static void CreateChara(ChaControl __result, ChaFileControl _chaFile)
			{
				OnCharaCreate?.Invoke(null, new CharaCreateEventArgs(__result, _chaFile));
			}
		}
	}
}
