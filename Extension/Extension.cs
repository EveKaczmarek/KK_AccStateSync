using BepInEx;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Extension
{
	public static class Extension
	{
		private struct FieldKey
		{
			public readonly Type type;
			public readonly string name;
			private readonly int _hashCode;

			public FieldKey(Type inType, string inName)
			{
				this.type = inType;
				this.name = inName;
				this._hashCode = this.type.GetHashCode() ^ this.name.GetHashCode();
			}

			public override int GetHashCode()
			{
				return this._hashCode;
			}
		}

		private static readonly Dictionary<FieldKey, FieldInfo> _fieldCache = new Dictionary<FieldKey, FieldInfo>();

		public static object GetField(this object self, string name, Type type = null)
		{
			if (null == type)
			{
				type = self.GetType();
			}
			if (!self.SearchForFields(name))
			{
				Console.WriteLine("[KK_Extension] Field Not Found: " + name);
				return false;
			}
			FieldKey key = new FieldKey(type, name);
			if (_fieldCache.TryGetValue(key, out FieldInfo info) == false)
			{
				info = key.type.GetField(key.name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
				_fieldCache.Add(key, info);
			}
			return info.GetValue(self);
		}

		public static bool SetField(this object self, string name, object value, Type type = null)
		{
			if (null == type)
			{
				type = self.GetType();
			}
			if (!self.SearchForFields(name))
			{
				Console.WriteLine("[KK_Extension] Field Not Found: " + name);
				return false;
			}
			FieldKey fieldKey = new FieldKey(type, name);
			if (_fieldCache.TryGetValue(fieldKey, out FieldInfo field) == false) {
				field = fieldKey.type.GetField(fieldKey.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
				if (null != field)
				{
					_fieldCache.Add(fieldKey, field);
				}
				else
				{
					Console.WriteLine("[KK_Extension] Set Field Not Found: " + name);
					return false;
				}
			}
			try
			{
				field.SetValue(self, value);
				return true;
			}
			catch (ArgumentException ae)
			{
				Console.WriteLine("[KK_Extension] Set Field is not the same type as input: " + name);
				Console.WriteLine("[KK_Extension] " + ae.Message);
				return false;
			}
		}

		public static bool SetProperty(this object self, string name, object value)
		{
			if (!self.SearchForProperties(name))
			{
				Console.WriteLine("[KK_Extension] Field Not Found: " + name);
				return false;
			}
			PropertyInfo propertyInfo;
			propertyInfo = self.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetProperty);
			if (null != propertyInfo)
			{
				propertyInfo.SetValue(self, value, null);
				return true;
			}
			else
			{
				Console.WriteLine("[KK_Extension] Set Property Not Found: " + name);
				return false;
			}
		}

		public static object GetProperty(this object self, string name)
		{
			if (!self.SearchForProperties(name))
			{
				Console.WriteLine("[KK_Extension] Property Not Found: " + name);
				return false;
			}
			PropertyInfo propertyInfo;
			propertyInfo = self.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetProperty);
			return propertyInfo.GetValue(self, null);
		}

		public static object Invoke(this object self, string name, object[] p = null)
		{
			try
			{
				return self?.GetType().InvokeMember(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.InvokeMethod, null, self, p);
			}
			catch (MissingMethodException e)
			{
				Console.WriteLine(e.Message);
				Console.WriteLine(e.InnerException);
				MemberInfo[] members = self?.GetType().GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.InvokeMethod);
				List<string> printArray = new List<string>();
				foreach (MemberInfo me in members)
				{
					if (me.Name == name)
					{
						return true;
					}
					printArray.Add("[KK_Extension] Member Name/Type: " + me.Name + " / " + me.MemberType);
				}
				foreach (string st in printArray)
				{
					Console.WriteLine(st);
				}
				Console.WriteLine("[KK_Extension] Get " + members.Length + " Members.");
				return false;
			}
		}

		//List all the fields inside the object if name not found.
		public static bool SearchForFields(this object self, string name)
		{
			FieldInfo[] fieldInfos = self.GetType().GetFields(AccessTools.all);
			List<string> printArray = new List<string>();
			foreach (FieldInfo fi in fieldInfos)
			{
				if (fi.Name == name)
				{
					return true;
				}
				printArray.Add("[KK_Extension] Field Name/Type: " + fi.Name + " / " + fi.FieldType);
			}
			Console.WriteLine("[KK_Extension] Get " + fieldInfos.Length + " Fields.");

			foreach (string st in printArray)
			{
				Console.WriteLine(st);
			}
			return false;
		}

		//List all the fields inside the object if name not found.
		public static bool SearchForProperties(this object self, string name)
		{
			PropertyInfo[] propertyInfos = self.GetType().GetProperties(AccessTools.all);
			List<string> printArray = new List<string>();
			foreach (PropertyInfo pi in propertyInfos)
			{
				if (pi.Name == name)
				{
					return true;
				}
				printArray.Add("[KK_Extension] Property Name/Type: " + pi.Name + " / " + pi.PropertyType);
			}
			Console.WriteLine("[KK_Extension] Get " + propertyInfos.Length + " Properties.");

			foreach (string st in printArray)
			{
				Console.WriteLine(st);
			}
			return false;
		}

		public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this object self)
		{
			if (!(self is IDictionary dictionary))
			{
				Console.WriteLine("[KK_Extension] Faild to cast to Dictionary!");
				return null;
			}
			Dictionary<TKey, TValue> newDictionary = CastDict(dictionary).ToDictionary(entry => (TKey)entry.Key, entry => (TValue)entry.Value);
			return newDictionary;

			IEnumerable<DictionaryEntry> CastDict(IDictionary dic)
			{
				foreach (DictionaryEntry entry in dic)
				{
					yield return entry;
				}
			}
		}

		public static List<T> ToList<T>(this object self)
		{
			if (!(self is IEnumerable<T> iEnumerable))
			{
				Console.WriteLine("[KK_Extension] Faild to cast to List!");
				return null;
			}
			List<T> newList = new List<T>(iEnumerable);
			return newList;
		}
	}
}