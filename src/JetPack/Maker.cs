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
	public partial class Maker
	{
		public static CustomBase CustomBase => CustomBase.Instance;
		public static ChaControl ChaControl => CustomBase?.chaCtrl;
		public int CurrentCoordinateIndex => ChaControl.fileStatus.coordinateType;

		internal static Harmony HarmonyInstance;

		internal static int CvsMainMenu = 0;
		internal static Dictionary<int, Toggle> CvsMenuTree = new Dictionary<int, Toggle>();

		public static class Instance
		{
			public static CvsDrawCtrl CvsDrawCtrl => CustomBase.Instance.customCtrl.cmpDrawCtrl;
			public static CvsAccessoryCopy CvsAccessoryCopy => Singleton<CvsAccessoryCopy>.Instance;
		}

		internal static void Init()
		{
			Accessory._moreAccessoriesInstance.onCharaMakerSlotAdded += new Action<int, Transform>((i, transform) => OnCharaMakerSlotAdded(Core.PluginInstance, new MakerSlotAddedEventArgs(i, transform)));
			OnCharaMakerSlotAdded += (sender, args) =>
			{
				//Core.DebugLog($"[OnCharaMakerSlotAdded][GameObject: {args.SlotTemplate.gameObject.name}]");
				Toggle tglItem = args.SlotTemplate.GetComponent<Toggle>();
				tglItem.onValueChanged.AddListener(value =>
				{
					if (value)
					{
						bool changed = CvsMenuTree[4] != tglItem;
						CvsMenuTree[4] = tglItem;
						OnCvsNavMenuClick?.Invoke(null, new CvsNavMenuEventArgs(4, tglItem, changed));
					}
				});
			};

			OnCvsNavMenuClick += (sender, args) =>
			{
				Core.DebugLog($"[OnCvsNavMenuClick][{args.TopIndex}][{args.SideToggle.name}][{args.Changed}]");
			};

			Chara.Clothes.OnClothesCopy += (sender, args) =>
			{
				Core.DebugLog($"[OnClothesCopy][{args.SourceCoordinateIndex}][{args.DestinationCoordinateIndex}][{args.DestinationSlotIndex}]");
			};
		}

		internal class Hooks
		{
			[HarmonyPostfix, HarmonyPatch(typeof(CvsClothesCopy), "CopyClothes")]
			private static void CvsClothesCopy_CopyClothes_Postfix(TMP_Dropdown[] ___ddCoordeType, Toggle[] ___tglKind)
			{
				for (int i = 0; i < Enum.GetNames(typeof(ChaFileDefine.ClothesKind)).Length; i++)
				{
					if (___tglKind[i].isOn)
						Chara.Clothes.InvokeOnClothesCopy(___ddCoordeType[1].value, ___ddCoordeType[0].value, i);
				}
			}
		}

		public static event EventHandler<CvsNavMenuEventArgs> OnCvsNavMenuClick;

		public class CvsNavMenuEventArgs : EventArgs
		{
			public CvsNavMenuEventArgs(int top, Toggle side, bool changed)
			{
				TopIndex = top;
				SideToggle = side;
				Changed = changed;
			}

			public int TopIndex { get; }
			public Toggle SideToggle { get; }
			public bool Changed { get; }
		}

		internal static void CvsNavMenuInit(CustomChangeMainMenu instance)
		{
			for (int i = 0; i < instance.items.Length; i++)
			{
				int topIdx = i;
				Toggle tglItem = instance.items[i].tglItem;
				if (tglItem != null)
				{
					tglItem.onValueChanged.AddListener(value =>
					{
						if (value)
						{
							bool changed = CvsMainMenu != topIdx;
							CvsMainMenu = topIdx;
							OnCvsNavMenuClick?.Invoke(null, new CvsNavMenuEventArgs(topIdx, CvsMenuTree[topIdx], changed));
						}
					});
				}
			}

			foreach (Transform child in GameObject.Find("CvsMenuTree").transform)
			{
				int topIdx = child.GetSiblingIndex();

				UI_ToggleGroupCtrl.ItemInfo[] items = child.GetComponent<UI_ToggleGroupCtrl>().items;
				CvsMenuTree[topIdx] = items[0].tglItem;
				for (int i = 0; i < items.Length; i++)
				{
					Toggle tglItem = items[i].tglItem;
					if (tglItem != null)
					{
						tglItem.onValueChanged.AddListener(value =>
						{
							if (value)
							{
								bool changed = CvsMenuTree[topIdx] != tglItem;
								CvsMenuTree[topIdx] = tglItem;
								OnCvsNavMenuClick?.Invoke(null, new CvsNavMenuEventArgs(topIdx, tglItem, changed));
							}
						});
					}
				}
			}
		}

		public static event EventHandler<CustomSelectListCtrlEventArgs> OnCustomSelectListClick;

		public class CustomSelectListCtrlEventArgs : EventArgs
		{
			public CustomSelectListCtrlEventArgs(GameObject obj)
			{
				CustomSelectInfoComponent component = obj.GetComponent<CustomSelectInfoComponent>();
				if (component == null || !component.tgl.interactable) return;

				if (component.info.index >= UniversalAutoResolver.BaseSlotID)
				{
					ResolveInfo info = UniversalAutoResolver.TryGetResolutionInfo((ChaListDefine.CategoryNo) component.info.category, component.info.index);
					if (info != null)
					{
						CategoryNo = (int) info.CategoryNo;
						GUID = info.GUID;
						ItemID = info.LocalSlot;
						LocalItemID = info.Slot;
					}
				}
				else
				{
					CategoryNo = component.info.category;
					ItemID = component.info.index;
					LocalItemID = -1;
				}
			}

			public int CategoryNo { get; }
			public string GUID { get; }
			public int ItemID { get; }
			public int LocalItemID { get; }
		}

		internal partial class HooksCustomSelectListCtrl
		{
			[HarmonyPriority(Priority.Last)]
			[HarmonyPostfix, HarmonyPatch(typeof(CustomSelectListCtrl), nameof(CustomSelectListCtrl.OnPointerClick))]
			internal static void CustomSelectListCtrl_OnPointerClick_Postfix(CustomSelectListCtrl __instance, GameObject obj)
			{
				if (__instance.onChangeItemFunc == null || obj == null) return;
				OnCustomSelectListClick?.Invoke(__instance, new CustomSelectListCtrlEventArgs(obj));
			}
		}

		public static event EventHandler<HoverEventArgs> OnPointerEnter;
		public static event EventHandler<HoverEventArgs> OnPointerExit;

		public class HoverEventArgs : EventArgs
		{
			public HoverEventArgs(Selectable selectable, PointerEventData eventData)
			{
				Selectable = selectable;
				EventData = eventData;
			}

			public Selectable Selectable { get; }
			public PointerEventData EventData { get; }
		}

		internal partial class HooksSelectable
		{
			[HarmonyPriority(Priority.Last)]
			[HarmonyPostfix, HarmonyPatch(typeof(Selectable), nameof(Selectable.OnPointerEnter))]
			private static void Selectable_OnPointerEnter(Selectable __instance, PointerEventData eventData)
			{
				OnPointerEnter?.Invoke(__instance, new HoverEventArgs(__instance, eventData));
			}

			[HarmonyPriority(Priority.Last)]
			[HarmonyPostfix, HarmonyPatch(typeof(Selectable), nameof(Selectable.OnPointerExit))]
			private static void Selectable_OnPointerExit(Selectable __instance, PointerEventData eventData)
			{
				OnPointerExit?.Invoke(__instance, new HoverEventArgs(__instance, eventData));
			}
		}
	}
}
