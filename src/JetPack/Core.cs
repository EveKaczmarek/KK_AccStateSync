using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using UnityEngine;
using UnityEngine.SceneManagement;
using ParadoxNotion.Serialization;
using MessagePack;

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace JetPack
{
	[BepInPlugin(GUID, Name, Version)]
	[BepInDependency("marco.kkapi")]
	[BepInDependency("com.deathweasel.bepinex.materialeditor", "3.0")]
	[BepInDependency("com.joan6694.illusionplugins.moreaccessories", "1.0.9")]
	public partial class Core : BaseUnityPlugin
	{
		public const string GUID = "madevil.JetPack";
		public const string Name = "JetPack";
		public const string Version = "2.0.6.0";

		internal static ManualLogSource _logger;
		internal static Harmony _hookInstance;

		private static ConfigEntry<bool> _cfgDebugMsg;

		private void Awake()
		{
			_logger = base.Logger;

			_cfgDebugMsg = Config.Bind("Debug", "Display debug message", false);

			if (Application.dataPath.EndsWith("CharaStudio_Data"))
				CharaStudio.Running = true;
			if (Application.dataPath.EndsWith("KoikatuVR_Data"))
			{
				CharaHscene.Inside = true;
				CharaHscene.VR = true;
			}
		}

		private void Start()
		{
			Game.HasDarkness = typeof(ChaControl).GetProperties(AccessTools.all).Any(x => x.Name == "exType");

			_hookInstance = Harmony.CreateAndPatchAll(typeof(Hooks));

			Chara.Init();
			KKAPI.Init();
			MoreAccessories.Init();
			MaterialEditor.Init();

			if (CharaStudio.Running)
			{
				CharaStudio.OnStudioLoaded += CharaStudio.RegisterControls;
				SceneManager.sceneLoaded += CharaStudio.SceneLoaded;
			}
			else
			{
				SceneManager.sceneLoaded += SceneLoaded;
				CharaMaker.Init();
				CharaHscene.Init();
			}
		}

		private static void SceneLoaded(Scene _scene, LoadSceneMode _loadSceneMode)
		{
			DebugLog($"[SceneLoaded][name: {_scene.name}][mode: {_loadSceneMode}]");
			if (_scene.name == "CustomScene")
				CharaMaker.InvokeOnMakerStartLoading(null, null);
			else if (_scene.name == "HProc" || _scene.name == "VRHScene")
			{
				CharaHscene.Inside = true;
				CharaHscene.Hooks.Init();
				CharaHscene.InvokeOnHSceneStartLoading(null, null);
			}
		}

		private static class Hooks { }

		internal static void DebugLog(object _msg) => DebugLog(LogLevel.Warning, _msg);
		internal static void DebugLog(LogLevel _level, object _msg)
		{
			if (_cfgDebugMsg.Value)
				_logger.Log(_level, _msg);
			else
				_logger.Log(LogLevel.Debug, _msg);
		}
	}

	public class Game
	{
		public static bool HasDarkness = false;
	}

	public static partial class Toolbox
	{
		public static T MessagepackClone<T>(T _object)
		{
			byte[] _byte = MessagePackSerializer.Serialize(_object);
			return MessagePackSerializer.Deserialize<T>(_byte);
		}

		public static object JsonClone(this object _self)
		{
			if (_self == null)
				return null;
			string _json = JSONSerializer.Serialize(_self.GetType(), _self);
			return JSONSerializer.Deserialize(_self.GetType(), _json);
		}

		public static BaseUnityPlugin GetPluginInstance(string _guid)
		{
			BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(_guid, out PluginInfo _pluginInfo);
			return _pluginInfo?.Instance;
		}

		public static bool PluginVersionCompare(string _guid, string _version)
		{
			BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(_guid, out PluginInfo _pluginInfo);
			if (_pluginInfo == null) return false;
			return _pluginInfo.Metadata.Version.CompareTo(new Version(_version)) > -1;
		}

		public static bool PluginVersionCompare(BaseUnityPlugin _instance, string _version)
		{
			return _instance.Info.Metadata.Version.CompareTo(new Version(_version)) > -1;
		}

		public static T[] AddToArray<T>(this T[] _self, T _item)
		{
			List<T> _list = _self.ToList();
			_list.Add(_item);
			return _list.ToArray();
		}

		public static Texture2D LoadTexture(byte[] _byte)
		{
			Texture2D _texture = new Texture2D(2, 2, TextureFormat.ARGB32, false);
			_texture.LoadImage(_byte);
			return _texture;
		}

		public static byte[] ReadAllBytes(this Stream _self)
		{
			byte[] _byte = new byte[16 * 1024];
			using (var ms = new MemoryStream())
			{
				int _pointer;
				while ((_pointer = _self.Read(_byte, 0, _byte.Length)) > 0)
					ms.Write(_byte, 0, _pointer);
				return ms.ToArray();
			}
		}

		// https://stackoverflow.com/questions/8477664/how-can-i-generate-uuid-in-c-sharp
		// https://stackoverflow.com/questions/1700361/how-to-convert-a-guid-to-a-string-in-c
		public static string GUID(string _format = "D")
		{
			return Guid.NewGuid().ToString(_format).ToUpper();
		}

		public static readonly WaitForEndOfFrame WaitForEndOfFrame = new WaitForEndOfFrame();
	}

	public partial class Storage
	{
		public static int _focusWindowID = -1;
	}
}
