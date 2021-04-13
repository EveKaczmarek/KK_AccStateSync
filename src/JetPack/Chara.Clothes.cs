using System;
using System.Collections.Generic;
using System.Linq;

namespace JetPack
{
	public partial class Chara
	{
		public partial class Clothes
		{
			public static int GetClothesState(ChaControl _chaCtrl, int _slotIndex) => _chaCtrl.fileStatus.clothesState.ElementAtOrDefault(_slotIndex);
			public static ChaFileClothes.PartsInfo GetPartsInfo(ChaControl _chaCtrl, int _slotIndex) => _chaCtrl.nowCoordinate.clothes.parts.ElementAtOrDefault(_slotIndex);
			public static ListInfoBase GetListInfo(ChaControl _chaCtrl, int _slotIndex) => _chaCtrl.infoClothes.ElementAtOrDefault(_slotIndex);

			public static List<bool> GetClothesStates(ChaControl _chaCtrl, int _slotIndex)
			{
				List<bool> _states = new List<bool>();
				Dictionary<byte, string> _keys = _chaCtrl.GetClothesStateKind(_slotIndex);
				if (_keys != null)
				{
					for (int i = 0; i < 4; i++)
						_states.Add(_keys.ContainsKey((byte) i));
				}
				return _states;
			}

			public static event EventHandler<ShoesCopyEventArgs> OnShoesCopy;

			public class ShoesCopyEventArgs : EventArgs
			{
				public ShoesCopyEventArgs(int _coordinateIndex, int _sourceSlotIndex, int _destinationSlotIndex)
				{
					CoordinateIndex = _coordinateIndex;
					SourceSlotIndex = _sourceSlotIndex;
					DestinationSlotIndex = _destinationSlotIndex;
				}

				public int CoordinateIndex { get; }
				public int SourceSlotIndex { get; }
				public int DestinationSlotIndex { get; }
			}

			public static void CopyShoesPartsInfo(ChaControl _chaCtrl, int _coordinateIndex, int _sourceSlotIndex)
			{
				int _destinationSlotIndex = _sourceSlotIndex == 7 ? 8 : 7;
				_chaCtrl.chaFile.coordinate[_coordinateIndex].clothes.parts[_destinationSlotIndex] = Toolbox.MessagepackClone(_chaCtrl.chaFile.coordinate[_coordinateIndex].clothes.parts[_sourceSlotIndex]);

				OnShoesCopy?.Invoke(_chaCtrl, new ShoesCopyEventArgs(_coordinateIndex, _sourceSlotIndex, _destinationSlotIndex));
			}
		}
	}
}
