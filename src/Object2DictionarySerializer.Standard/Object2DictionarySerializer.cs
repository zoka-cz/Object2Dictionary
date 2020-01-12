using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Zoka.Object2Dictionary.Serializer
{
	/// <summary>
	///		Serializer of any C# object into and from Dictionary&lt;string, string&gt; into form
	///		"property_of_object"-"value_of_property"
	/// </summary>
	public class Object2DictionarySerializer
	{
		#region Static methods

		/// <summary>Will serialize the object into the string key-value dictionary including all underlaying objects</summary>
		public static Dictionary<string, string>			Serialize<T>(T _object) where T : class, new()
		{
			var dictionary = new Dictionary<string, string>();
			var serializer = new Object2DictionarySerializer();
			serializer.GetValuesFromComplexTypeIntoDictionary(dictionary, _object, "");
			return dictionary;
		}

		/// <summary>Will deserialize the dictionary string key-value pairs into the object</summary>
		public static T										Deserialize<T>(Dictionary<string, string> _dictionary) where T : class, new()
		{
			var serializer = new Object2DictionarySerializer();
			var obj = new T();
			serializer.SetValuesFromDictionaryIntoComplexType(_dictionary, obj, "");
			return obj;
		}

		/// <summary>Will deserialize the dictionary string key-value pairs into passed existing object</summary>
		public static void									Populate(Dictionary<string, string> _dictionary, object _target_object)
		{
			var serializer = new Object2DictionarySerializer();
			if (serializer.IsComplexType(_target_object.GetType()))
				serializer.SetValuesFromDictionaryIntoComplexType(_dictionary, _target_object, "");
		}

		#endregion // Static methods

		#region Getter helpers

		private void GetValuesFromListTypeIntoDictionary(Dictionary<string, string> _settings_dictionary, IEnumerable _from_object, Type _item_type, string _dictionary_prefix)
		{
			int idx = 0;
			foreach (var item in _from_object)
			{
				Type sublist_type, subitem_type;
				if (IsValueType(_item_type) || Nullable.GetUnderlyingType(_item_type) != null)
				{
					var converter = GetTypeConverter(null, _item_type);
					if (item != null)
					{
						var str_val = converter.ConvertToString(item);
						_settings_dictionary.Add($"{_dictionary_prefix}[{idx}]", str_val);
					}
				}
				else if (IsListType(_item_type, out sublist_type, out subitem_type))
				{
					var enumerable = item as IEnumerable;
					if (enumerable != null)
						GetValuesFromListTypeIntoDictionary(_settings_dictionary, enumerable, subitem_type, $"{_dictionary_prefix}[{idx}]");
				}
				else if (IsComplexType(_item_type))
				{
					if (item != null)
						GetValuesFromComplexTypeIntoDictionary(_settings_dictionary, item, $"{_dictionary_prefix}[{idx}].");
				}

				idx++;
			}
		}

		private void GetValuesFromComplexTypeIntoDictionary(Dictionary<string, string> _settings_dictionary, object _from_object, string _dictionary_prefix)
		{
			var properties = _from_object.GetType().GetProperties().Where(p => !p.GetCustomAttributes(typeof(DontSetFromDictionaryAttribute), false).Any());

			foreach (var property in properties)
			{
				Type list_type, item_type;

				if (IsValueType(property.PropertyType) || Nullable.GetUnderlyingType(property.PropertyType) != null)
				{
					var converter = GetTypeConverter(property, property.PropertyType);
					var val = property.GetValue(_from_object, null);
					if (val != null)
					{
						var str_val = converter.ConvertToString(val);
						_settings_dictionary.Add($"{_dictionary_prefix}{property.Name}", str_val);
					}
				}
				else if (IsListType(property.PropertyType, out list_type, out item_type)) // may be list
				{
					var enumerable = property.GetValue(_from_object, null) as IEnumerable;
					if (enumerable != null)
						GetValuesFromListTypeIntoDictionary(_settings_dictionary, enumerable, item_type, $"{_dictionary_prefix}{property.Name}");
				}
				else if (IsComplexType(property.PropertyType))
				{
					var inst = property.GetValue(_from_object, null);
					if (inst != null)
						GetValuesFromComplexTypeIntoDictionary(_settings_dictionary, inst, $"{_dictionary_prefix}{property.Name}.");
				}
			}

		}

		private static TypeConverter GetTypeConverter(PropertyInfo _property, Type _for_type)
		{
			System.ComponentModel.TypeConverter converter = null;
			if (_property != null)
			{
				_for_type = _property.PropertyType;
				var type_converter_attributes = _property.GetCustomAttributes(typeof(System.ComponentModel.TypeConverterAttribute), false);
				if (type_converter_attributes != null && type_converter_attributes.Any())
				{
					var type_converter_type = Type.GetType((type_converter_attributes.First() as System.ComponentModel.TypeConverterAttribute).ConverterTypeName);
					if (type_converter_type != null)
						converter = Activator.CreateInstance(type_converter_type) as System.ComponentModel.TypeConverter;
				}
			}

			if (converter == null)
				converter = System.ComponentModel.TypeDescriptor.GetConverter(_for_type);
			return converter;
		}

		#endregion // Getter helpers

		#region Setter helpers

		private bool IsValueType(Type _type)
		{
			return _type.IsValueType || _type == typeof(string);
		}
		private void SetPropertyValueType(PropertyInfo _property_info, object _target_instance, string _value)
		{
			var nullable_type = Nullable.GetUnderlyingType(_property_info.PropertyType);
			if (!IsValueType(_property_info.PropertyType) && nullable_type == null)
				throw new Exception("Not a value type");
			var tgt_type = _property_info.PropertyType;
			var converter = GetTypeConverter(_property_info, tgt_type);
			var val = converter.ConvertFromString(_value);
			_property_info.SetValue(_target_instance, val, null);
		}

		private bool IsListType(Type _type, out Type _list_type, out Type _item_type)
		{
			if (_type.IsGenericType && _type.GetGenericArguments().Length == 1)
			{
				_item_type = _type.GetGenericArguments()[0];
				_list_type = typeof(List<>).MakeGenericType(_item_type);
				if (_list_type.IsAssignableFrom(_type))
					return true;
			}

			_item_type = null;
			_list_type = null;
			return false;
		}
		private void SetValuesFromDictionaryIntoListType(Dictionary<string, string> _settings_dictionary, object _list, string _dictionary_prefix)
		{
			Type list_type, item_type, sublist_type, sublist_item_type;
			if (!IsListType(_list.GetType(), out list_type, out item_type))
				throw new ArgumentException("Not an List<T> type", nameof(_list));

			var add_method = _list.GetType().GetMethod("Add");
			if (add_method == null)
				throw new Exception("Add method not found");

			if (IsValueType(item_type) || Nullable.GetUnderlyingType(item_type) != null)
			{
				int idx = 0;
				string key = $"{_dictionary_prefix}[{idx++}]";
				while (_settings_dictionary.ContainsKey(key))
				{
					add_method.Invoke(_list, new[] { GetTypeConverter(null, item_type).ConvertFromString(_settings_dictionary[key]) });
					key = $"{_dictionary_prefix}[{idx++}]";
				}
			}
			else if (IsListType(item_type, out sublist_type, out sublist_item_type))
			{
				int idx = 0;
				string key = $"{_dictionary_prefix}[{idx++}]";
				while (_settings_dictionary.Keys.Any(k => k.StartsWith(key)))
				{
					var sublist = Activator.CreateInstance(sublist_item_type);
					add_method.Invoke(_list, new[] { sublist });
					SetValuesFromDictionaryIntoListType(_settings_dictionary, sublist, key);
					key = $"{_dictionary_prefix}[{idx++}]";
				}
			}
			else if (IsComplexType(item_type))
			{
				int idx = 0;
				string key = $"{_dictionary_prefix}[{idx++}]";
				while (_settings_dictionary.Keys.Any(k => k.StartsWith(key)))
				{
					var inst = Activator.CreateInstance(item_type);
					add_method.Invoke(_list, new[] { inst });
					SetValuesFromDictionaryIntoComplexType(_settings_dictionary, inst, key + ".");
					key = $"{_dictionary_prefix}[{idx++}]";
				}
			}
		}

		private bool IsComplexType(Type _type)
		{
			return _type.IsClass && !_type.IsAbstract;
		}

		private void SetValuesFromDictionaryIntoComplexType(Dictionary<string, string> _settings_dictionary, object _target_object, string _dictionary_prefix)
		{
			var properties = _target_object.GetType().GetProperties().Where(p => !p.GetCustomAttributes(typeof(DontSetFromDictionaryAttribute), false).Any());

			foreach (var property in properties)
			{
				Type list_type, item_type;

				if (IsValueType(property.PropertyType) || Nullable.GetUnderlyingType(property.PropertyType) != null)
				{
					if (_settings_dictionary.Keys.Contains(_dictionary_prefix + property.Name))
					{
						SetPropertyValueType(property, _target_object, _settings_dictionary[_dictionary_prefix + property.Name]);
					}
					else
					{
						var default_val_attr = property.GetCustomAttributes(typeof(DictionaryDefaultValueAttribute), false).Cast<DictionaryDefaultValueAttribute>();
						if (default_val_attr != null && default_val_attr.Any() && default_val_attr.First().DefaultValue != null)
						{
							SetPropertyValueType(property, _target_object, default_val_attr.First().DefaultValue);
						}
					}
				}
				else if (IsListType(property.PropertyType, out list_type, out item_type)) // may be list
				{
					var list = Activator.CreateInstance(list_type);
					property.SetValue(_target_object, list, null);
					SetValuesFromDictionaryIntoListType(_settings_dictionary, list, _dictionary_prefix + property.Name);
				}
				else if (IsComplexType(property.PropertyType))
				{
					var inst = Activator.CreateInstance(property.PropertyType);
					property.SetValue(_target_object, inst, null);
					SetValuesFromDictionaryIntoComplexType(_settings_dictionary, inst, property.Name + ".");
				}
				else
					throw new NotSupportedException("Not supportted property type");
			}

		}

		#endregion // Setter helpers

	}
}
