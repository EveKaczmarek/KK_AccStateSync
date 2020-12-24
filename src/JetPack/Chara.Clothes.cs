using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UniRx;

using HarmonyLib;

namespace JetPack
{
	public partial class Chara
	{
		public partial class Clothes
		{
			public static int GetClothesState(ChaControl chaCtrl, int clothesKind) => chaCtrl.fileStatus.clothesState.ElementAtOrDefault(clothesKind);
			public static ChaFileClothes.PartsInfo GetPartsInfo(ChaControl chaCtrl, int clothesKind) => chaCtrl.nowCoordinate.clothes.parts.ElementAtOrDefault(clothesKind);
			public static ListInfoBase GetListInfo(ChaControl chaCtrl, int clothesKind) => chaCtrl.infoClothes.ElementAtOrDefault(clothesKind);

			public static List<bool> GetClothesStates(ChaControl chaCtrl, int clothesKind)
			{
				List<bool> states = new List<bool>();
				Dictionary<byte, string> keys = chaCtrl.GetClothesStateKind(clothesKind);
				if (keys != null)
				{
					for (int i = 0; i < 4; i++)
						states.Add(keys.ContainsKey((byte) i));
				}
				/*
				if (states.Count < 4)
					states = new List<bool>(4);
				*/
				return states;
			}

			public static event EventHandler<ShoesCopyEventArgs> OnShoesCopy;

			public class ShoesCopyEventArgs : EventArgs
			{
				public ShoesCopyEventArgs(int coordinateIndex, int sourceSlotIndex, int destinationSlotIndex)
				{
					CoordinateIndex = coordinateIndex;
					SourceSlotIndex = sourceSlotIndex;
					DestinationSlotIndex = destinationSlotIndex;
				}

				public int CoordinateIndex { get; }
				public int SourceSlotIndex { get; }
				public int DestinationSlotIndex { get; }
			}

			public static void CopyShoesPartsInfo(ChaControl chaCtrl, int coordinateIndex, int sourceSlotIndex)
			{
				int destinationSlotIndex = sourceSlotIndex == 7 ? 8 : 7;
				chaCtrl.chaFile.coordinate[coordinateIndex].clothes.parts[destinationSlotIndex] = Toolbox.MessagepackClone(chaCtrl.chaFile.coordinate[coordinateIndex].clothes.parts[sourceSlotIndex]);

				OnShoesCopy?.Invoke(chaCtrl, new ShoesCopyEventArgs(coordinateIndex, sourceSlotIndex, destinationSlotIndex));
			}

			public static event EventHandler<ClothesCopyEventArgs> OnClothesCopy;

			public class ClothesCopyEventArgs : EventArgs
			{
				public ClothesCopyEventArgs(int srcIndex, int dstIndex, int dstSlot)
				{
					DestinationSlotIndex = dstSlot;
					SourceCoordinateIndex = srcIndex;
					DestinationCoordinateIndex = dstIndex;
				}

				public int DestinationSlotIndex { get; }
				public int SourceCoordinateIndex { get; }
				public int DestinationCoordinateIndex { get; }
			}

			internal static void InvokeOnClothesCopy(int srcIndex, int dstIndex, int dstSlot)
			{
				OnClothesCopy?.Invoke(null, new ClothesCopyEventArgs(srcIndex, dstIndex, dstSlot));
			}
		}
	}
}
