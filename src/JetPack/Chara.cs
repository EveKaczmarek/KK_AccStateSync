using System;

using HarmonyLib;

namespace JetPack
{
	public partial class Chara
	{
		internal static void Init()
		{
			Core._hookInstance.PatchAll(typeof(Hooks));

			OnChangeCoordinateType += (_sender, _args) =>
			{
				Core.DebugLog($"[OnChangeCoordinateType][{_args.CoordinateType}]");
				CharaStudio.RefreshCharaStatePanel();
			};
		}

		public static event EventHandler<ChangeCoordinateTypeEventArgs> OnChangeCoordinateType;
		public class ChangeCoordinateTypeEventArgs : EventArgs
		{
			public ChangeCoordinateTypeEventArgs(ChaControl _chaCtrl, int _coordinateIndex)
			{
				ChaControl = _chaCtrl;
				CoordinateType = _coordinateIndex;
			}

			public ChaControl ChaControl { get; }
			public int CoordinateType { get; }
		}

		internal class Hooks
		{
			[HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCoordinateType), typeof(ChaFileDefine.CoordinateType), typeof(bool))]
			private static void ChaControl_ChangeCoordinateType_Prefix(ChaControl __instance, ChaFileDefine.CoordinateType type)
			{
				OnChangeCoordinateType?.Invoke(null, new ChangeCoordinateTypeEventArgs(__instance, (int) type));
			}
		}
	}
}
