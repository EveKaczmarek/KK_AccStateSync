using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEngine;
using UnityEngine.UI;
using ChaCustom;

using BepInEx;
using HarmonyLib;

namespace JetPack
{
	public partial class CharaMaker
	{
		public static event EventHandler<SelectedMakerSlotChangedEventArgs> OnSelectedMakerSlotChanged;
		public class SelectedMakerSlotChangedEventArgs : EventArgs
		{
			public SelectedMakerSlotChangedEventArgs(int _oldSlotIndex, int _newSlotIndex)
			{
				CurrentAccssoryIndex = _newSlotIndex;
				OldSlotIndex = _oldSlotIndex;
				NewSlotIndex = _newSlotIndex;
			}

			public int OldSlotIndex { get; }
			public int NewSlotIndex { get; }
		}

		public static event EventHandler<AccessoryKindChangedEventArgs> OnAccessoryKindChanged;
		public class AccessoryKindChangedEventArgs : EventArgs
		{
			public AccessoryKindChangedEventArgs(int _slotIndex)
			{
				SlotIndex = _slotIndex;
			}

			public int SlotIndex { get; }
		}

		public static event EventHandler<AccessoryTypeChangedEventArgs> OnAccessoryTypeChanged;
		public class AccessoryTypeChangedEventArgs : EventArgs
		{
			public AccessoryTypeChangedEventArgs(int _slotIndex, int _oldType, int _newType, ChaFileAccessory.PartsInfo _part)
			{
				SlotIndex = _slotIndex;
				OldType = _oldType + 120;
				NewType = _newType + 120;
				PartsInfo = _part;
			}

			public int SlotIndex { get; }
			public int OldType { get; }
			public int NewType { get; }
			public ChaFileAccessory.PartsInfo PartsInfo { get; }
		}

		public static event EventHandler<AccessoryParentChangedEventArgs> OnAccessoryParentChanged;
		public class AccessoryParentChangedEventArgs : EventArgs
		{
			public AccessoryParentChangedEventArgs(int _slotIndex, string _oldParent, string _newParent, ChaFileAccessory.PartsInfo _part)
			{
				SlotIndex = _slotIndex;
				OldParent = _oldParent;
				NewParent = _newParent;
				PartsInfo = _part;
			}

			public int SlotIndex { get; }
			public string OldParent { get; }
			public string NewParent { get; }
			public ChaFileAccessory.PartsInfo PartsInfo { get; }
		}

		public static event EventHandler<SlotAddedEventArgs> OnSlotAdded;
		public class SlotAddedEventArgs : EventArgs
		{
			public SlotAddedEventArgs(int _slotIndex, Transform _transform)
			{
				SlotIndex = _slotIndex;
				SlotTemplate = _transform;
			}

			public int SlotIndex { get; }
			public Transform SlotTemplate { get; }
		}
	}
}
