using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using KKAPI.Chara;
using KKAPI.Maker;
using JetPack;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		public partial class AccStateSyncController : CharaCustomFunctionController
		{
			internal void OnVirtualGroupStateChange(string group, bool state)
			{
				CurOutfitVirtualGroupInfo[group].State = state;
				ToggleByVirtualGroup(group, state);
			}

			internal IEnumerator OnCurSlotTriggerInfoChangeCoroutine()
			{
				yield return new WaitForEndOfFrame();
				yield return new WaitForEndOfFrame();

				OnCurSlotTriggerInfoChange();
			}

			internal void OnCurSlotTriggerInfoChange()
			{
				if (!MakerAPI.InsideAndLoaded) return;

				UpdateStatesToggle();
				PreviewChange();
				AutoSaveTrigger();
			}
		}
	}
}
