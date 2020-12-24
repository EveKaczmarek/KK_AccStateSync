using System;
using System.Collections.Generic;
using System.Linq;

using BepInEx;
using HarmonyLib;
using MessagePack;

using KKAPI.Maker;
using KK_Plugins.MaterialEditor;
using static KK_Plugins.MaterialEditor.MaterialEditorCharaController;

namespace JetPack
{
	public static partial class MaterialEditorExtension
	{
		public static void MEAddRange(this object self, object obj)
		{
			if (self is List<RendererProperty>)
				(self as List<RendererProperty>).AddRange(obj as List<RendererProperty>);
			else if (self is List<MaterialFloatProperty>)
				(self as List<MaterialFloatProperty>).AddRange(obj as List<MaterialFloatProperty>);
			else if (self is List<MaterialColorProperty>)
				(self as List<MaterialColorProperty>).AddRange(obj as List<MaterialColorProperty>);
			else if (self is List<MaterialTextureProperty>)
				(self as List<MaterialTextureProperty>).AddRange(obj as List<MaterialTextureProperty>);
			else if (self is List<MaterialShader>)
				(self as List<MaterialShader>).AddRange(obj as List<MaterialShader>);
		}

		public static int MERemoveAll(this object self, Func<object, bool> match)
		{
			if (self is List<RendererProperty>)
				return (self as List<RendererProperty>).RemoveAll(new Predicate<RendererProperty>(match));
			else if (self is List<MaterialFloatProperty>)
				return (self as List<MaterialFloatProperty>).RemoveAll(new Predicate<MaterialFloatProperty>(match));
			else if (self is List<MaterialColorProperty>)
				return (self as List<MaterialColorProperty>).RemoveAll(new Predicate<MaterialColorProperty>(match));
			else if (self is List<MaterialTextureProperty>)
				return (self as List<MaterialTextureProperty>).RemoveAll(new Predicate<MaterialTextureProperty>(match));
			else if (self is List<MaterialShader>)
				return (self as List<MaterialShader>).RemoveAll(new Predicate<MaterialShader>(match));
			return 0;
		}

		public static object MEWhere(this object self, Func<object, bool> match)
		{
			if (self is List<RendererProperty>)
				return (self as List<RendererProperty>).Where(new Func<RendererProperty, bool>(match)).ToList();
			else if (self is List<MaterialFloatProperty>)
				return (self as List<MaterialFloatProperty>).Where(new Func<MaterialFloatProperty, bool>(match)).ToList();
			else if (self is List<MaterialColorProperty>)
				return (self as List<MaterialColorProperty>).Where(new Func<MaterialColorProperty, bool>(match)).ToList();
			else if (self is List<MaterialTextureProperty>)
				return (self as List<MaterialTextureProperty>).Where(new Func<MaterialTextureProperty, bool>(match)).ToList();
			else if (self is List<MaterialShader>)
				return (self as List<MaterialShader>).Where(new Func<MaterialShader, bool>(match)).ToList();
			return null;
		}

		public static void MEForEach(this object self, Action<object> action)
		{
			if (self is List<RendererProperty>)
				(self as List<RendererProperty>).ForEach(new Action<RendererProperty>(action));
			else if (self is List<MaterialFloatProperty>)
				(self as List<MaterialFloatProperty>).ForEach(new Action<MaterialFloatProperty>(action));
			else if (self is List<MaterialColorProperty>)
				(self as List<MaterialColorProperty>).ForEach(new Action<MaterialColorProperty>(action));
			else if (self is List<RendererProperty>)
				(self as List<RendererProperty>).ForEach(new Action<RendererProperty>(action));
			else if (self is List<MaterialTextureProperty>)
				(self as List<MaterialTextureProperty>).ForEach(new Action<MaterialTextureProperty>(action));
			else if (self is List<MaterialShader>)
				(self as List<MaterialShader>).ForEach(new Action<MaterialShader>(action));
		}

		public static byte[] MessagePackBackup(this object self)
		{
			if (self is List<RendererProperty>)
				return MessagePackSerializer.Serialize(self as List<RendererProperty>);
			else if (self is List<MaterialFloatProperty>)
				return MessagePackSerializer.Serialize(self as List<MaterialFloatProperty>);
			else if (self is List<MaterialColorProperty>)
				return MessagePackSerializer.Serialize(self as List<MaterialColorProperty>);
			else if (self is List<MaterialTextureProperty>)
				return MessagePackSerializer.Serialize(self as List<MaterialTextureProperty>);
			else if (self is List<MaterialShader>)
				return MessagePackSerializer.Serialize(self as List<MaterialShader>);
			return null;
		}

		public static object MessagePackRestore(this byte[] self, string TypeName)
		{
			if (TypeName == "RendererPropertyList")
				return MessagePackSerializer.Deserialize<List<RendererProperty>>(self);
			else if (TypeName == "MaterialFloatPropertyList")
				return MessagePackSerializer.Deserialize<List<MaterialFloatProperty>>(self);
			else if (TypeName == "MaterialColorPropertyList")
				return MessagePackSerializer.Deserialize<List<MaterialColorProperty>>(self);
			else if (TypeName == "MaterialTexturePropertyList")
				return MessagePackSerializer.Deserialize<List<MaterialTextureProperty>>(self);
			else if (TypeName == "MaterialShaderList")
				return MessagePackSerializer.Deserialize<List<MaterialShader>>(self);
			return null;
		}
	}
}
