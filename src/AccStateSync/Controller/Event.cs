namespace AccStateSync
{
	public partial class AccStateSync
	{
		public partial class AccStateSyncController
		{
			internal void OnVirtualGroupStateChange(string _group, bool _state)
			{
				CharaVirtualGroupInfo[_currentCoordinateIndex][_group].State = _state;
				ToggleByVirtualGroup(_group, _state);
			}

			internal void OnCurSlotTriggerInfoChange()
			{
				if (!JetPack.CharaMaker.Loaded) return;
				PreviewChange();
			}
		}
	}
}
