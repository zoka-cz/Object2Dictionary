using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Object2DictionarySerializer.Tests
{
	[TestClass]
	public class Object2DictionarySerializerTests
	{
		public class B
		{
			[Zoka.Object2Dictionary.Serializer.DictionaryDefaultValue("DefaultValueX")]
			public string X { get; set; }
			public TimeSpan? Y { get; set; }
			[Zoka.Object2Dictionary.Serializer.DontSetFromDictionary]
			public string Z { get; set; }
		}

		public class A 		
		{
			public int R { get; set; }
			public string S { get; set; }
			public List<B> T { get; set; }
		}

		public class Tp
		{
			public Type TpX { get; set; }
		}


		[TestMethod]
		public void											SerializeWorks()
		{
			var a = new A() {
				R = 17,
				S = "TestStringS",
				T = new List<B>() {
					new B() { X = "TestStringX", Y = TimeSpan.FromMinutes(77), Z = null },
					new B() { X = "TestStringX2", Y = null, Z = "TestStringZ1" }
				}
			};

			var dict = Zoka.Object2Dictionary.Serializer.Object2DictionarySerializer.Serialize(a);

			Assert.IsNotNull(dict);
			Assert.AreEqual("17", dict["R"]);
			Assert.AreEqual("TestStringS", dict["S"]);
			Assert.AreEqual("TestStringX", dict["T[0].X"]);
			Assert.AreEqual(TimeSpan.FromMinutes(77), TimeSpan.Parse(dict["T[0].Y"]));
			Assert.IsFalse(dict.ContainsKey("T[0].Z"));
		}

		[TestMethod]
		public void											DeserializeWorks()
		{
			var test_data = new Dictionary<string, string>() {
				{ "R", "17" },
				{ "S", "TestStringS" },
				{ "T[0].X", "TestStringX" },
				{ "T[1].Y", "01:17:00" },
				{ "T[1].Z", "TestStringZ" }
			};

			var obj = Zoka.Object2Dictionary.Serializer.Object2DictionarySerializer.Deserialize<A>(test_data);

			Assert.IsNotNull(obj);
			Assert.AreEqual(17, obj.R);
			Assert.AreEqual("TestStringS", obj.S);
			Assert.IsNotNull(obj.T);
			Assert.AreEqual(2, obj.T.Count);
			Assert.AreEqual("TestStringX", obj.T[0].X);
			Assert.IsNull(obj.T[0].Y);
			Assert.IsNull(obj.T[0].Z);
			Assert.AreEqual("DefaultValueX", obj.T[1].X);
			Assert.AreEqual(TimeSpan.FromMinutes(77), obj.T[1].Y);
			Assert.IsNull(obj.T[1].Z);
		}

		[TestMethod]
		public void											PopulateWorks()
		{
			var test_data = new Dictionary<string, string>() {
				{ "R", "17" },
				{ "S", "TestStringS" },
				{ "T[0].X", "TestStringX" },
				{ "T[1].Y", "01:17:00" },
				{ "T[1].Z", "TestStringZ" }
			};

			var obj = new A();
			Zoka.Object2Dictionary.Serializer.Object2DictionarySerializer.Populate(test_data, obj);

			Assert.IsNotNull(obj);
			Assert.AreEqual(17, obj.R);
			Assert.AreEqual("TestStringS", obj.S);
			Assert.IsNotNull(obj.T);
			Assert.AreEqual(2, obj.T.Count);
			Assert.AreEqual("TestStringX", obj.T[0].X);
			Assert.IsNull(obj.T[0].Y);
			Assert.IsNull(obj.T[0].Z);
			Assert.AreEqual("DefaultValueX", obj.T[1].X);
			Assert.AreEqual(TimeSpan.FromMinutes(77), obj.T[1].Y);
			Assert.IsNull(obj.T[1].Z);
		}

		[TestMethod]
		public void											SupportsTypeSerialization()
		{
			var t = new Tp() { TpX = typeof(Object2DictionarySerializerTests) };

			var dict = Zoka.Object2Dictionary.Serializer.Object2DictionarySerializer.Serialize(t);

			var t1 = Zoka.Object2Dictionary.Serializer.Object2DictionarySerializer.Deserialize(typeof(Tp), dict) as Tp;

			Assert.AreEqual(t.TpX, t1.TpX);
		}

	}
}
