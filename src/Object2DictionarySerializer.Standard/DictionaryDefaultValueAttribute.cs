using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zoka.Object2Dictionary.Serializer
{
	/// <summary>
	///		Attribute marking the property which should be deserialized from Dictionary, which says,
	///		that, when the value is not found in dictionary, the DefaultValue should be used instead.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
	public class DictionaryDefaultValueAttribute : Attribute
	{
		/// <summary>The default value of the property in case, its definition is not found in the dictionary</summary>
		public string										DefaultValue { get; set; }

		/// <summary>Empty constructor</summary>
		public DictionaryDefaultValueAttribute() : base()
		{ }

		/// <summary>Constructor taking the default value of the property</summary>
		public DictionaryDefaultValueAttribute(string _default_value) : base()
		{
			DefaultValue = _default_value;
		}
	}
}
