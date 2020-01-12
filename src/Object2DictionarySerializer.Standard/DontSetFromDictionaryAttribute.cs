using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zoka.Object2Dictionary.Serializer
{
	/// <summary>Attribute marking the property, which says, that this property is not to be set and serialized into/from the Dictionary</summary>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
	public class DontSetFromDictionaryAttribute : Attribute
	{
	}
}
