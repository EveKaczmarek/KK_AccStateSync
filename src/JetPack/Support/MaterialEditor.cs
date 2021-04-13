using System;
using System.Collections.Generic;
using System.Linq;

using MessagePack;
using ParadoxNotion.Serialization;

using BepInEx;
using HarmonyLib;

namespace JetPack
{
	public class MaterialEditor
	{
		public static BaseUnityPlugin Instance = null;
		public static Type MaterialAPI = null;

		public static readonly List<string> ContainerKeys = new List<string>() { "RendererPropertyList", "MaterialShaderList", "MaterialFloatPropertyList", "MaterialColorPropertyList", "MaterialTexturePropertyList", "MaterialCopyList" };

		internal static void Init()
		{
			Instance = Toolbox.GetPluginInstance("com.deathweasel.bepinex.materialeditor");
			MaterialAPI = Instance.GetType().Assembly.GetType("MaterialEditorAPI.MaterialAPI");
		}

		public static object GetController(ChaControl _chaCtrl)
		{
			return Traverse.Create(Instance).Method("GetCharaController", new object[] { _chaCtrl }).GetValue();
		}
	}
}
