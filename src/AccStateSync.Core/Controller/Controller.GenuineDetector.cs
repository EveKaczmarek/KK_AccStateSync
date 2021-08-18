using BepInEx.Logging;
using ExtensibleSaveFormat;

using KKAPI;
using KKAPI.Chara;

namespace AccStateSync
{
	public partial class AccStateSync
	{
		internal static GenuineDetector GetGenuineDetector(ChaControl _chaCtrl) => _chaCtrl?.gameObject?.GetComponent<GenuineDetector>();

		internal static void GenuineDetectorInit() => CharacterApi.RegisterExtraBehaviour<GenuineDetector>("Accessory_States");

		public partial class GenuineDetector : CharaCustomFunctionController
		{
			internal bool IsGenuine = true;
			internal string CharaFullName => ChaControl.chaFile.parameter?.fullname?.Trim();

			protected override void OnCardBeingSaved(GameMode currentGameMode) { }

			protected override void OnCoordinateBeingLoaded(ChaFileCoordinate coordinate)
			{
				PluginData _pluginData = GetCoordinateExtendedData(coordinate);
				IsGenuine = _pluginData == null;
				_logger.LogDebug($"[OnCoordinateBeingLoaded][{CharaFullName}][_isGenuine: {IsGenuine}]");
				base.OnCoordinateBeingLoaded(coordinate);
			}

			protected override void OnReload(GameMode currentGameMode)
			{
				PluginData _pluginData = GetExtendedData();
				IsGenuine = _pluginData == null;
				_logger.LogDebug($"[OnReload][{CharaFullName}][_isGenuine: {IsGenuine}]");
				base.OnReload(currentGameMode);
			}
		}
	}
}
