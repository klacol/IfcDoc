// Name:        StepSerializer.cs
// Description: STEP serializer
// Author:      Tim Chipman
// Origination: Work performed for BuildingSmart by Constructivity.com LLC.
// Copyright:   (c) 2017 BuildingSmart International Ltd.
// License:     http://www.buildingsmart-tech.org/legal

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Reflection;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace BuildingSmart.Serialization.Step
{
	public class StepSerializer : Serializer
	{
		/// <summary>
		/// Creates an instance of a STEP Physical Format (SPF) serializer for all types within an assembly.
		/// </summary>
		/// <param name="roottype">The type of object to serialize (typically IfcProject)</param>
		public StepSerializer(Type roottype)
			: base(roottype)
		{
		}

		/// <summary>
		/// Creates an instance of a STEP Physical Format (SPF) serializer for specified types.
		/// </summary>
		/// <param name="roottype"></param>
		/// <param name="loadtypes"></param>
		public StepSerializer(Type roottype, Type[] loadtypes)
			: base(roottype, loadtypes)
		{
		}

		/// <summary>
		/// Creates an instance of a STEP Physical Format (SPF) serializer for specified types.
		/// </summary>
		/// <param name="roottype"></param>
		/// <param name="loadtypes"></param>
		public StepSerializer(Type roottype, Type[] loadtypes, string schema, string release, string application)
			: base(roottype, loadtypes, schema, release, application)
		{
		}

		/// <summary>
		/// Reads an object graph from a stream in STEP Physical File format (SPF) according to ISO 10303-21.
		/// </summary>
		/// <param name="stream">The stream to read.</param>
		/// <param name="type">The type of the object to read (typically IfcProject)</param>
		/// <returns>The root object.</returns>
		public override object ReadObject(Stream stream)
		{
			if (stream == null)
				throw new ArgumentNullException("stream");

			// pull it into a memory stream so we can make multiple passes (can't assume it's a file; could be web service)
			//...MemoryStream memstream = new MemoryStream();

			Dictionary<long, object> instances = null;
			return ReadObject(stream, out instances);
		}

		/// <summary>
		/// Reads an object graph and provides access to instance identifiers from file.
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="instances"></param>
		/// <returns></returns>
		public object ReadObject(Stream stream, out Dictionary<long, object> instances)
		{
			instances = new Dictionary<long, object>();

			// first pass: read header to verify schema and model view definitions...

			// second pass: read instances
			ReadContent(stream, instances, ParseScope.DataInstances);

			// third pass: read fields
			ReadContent(stream, instances, ParseScope.DataFields);

			object root = null;
			instances.TryGetValue(0, out root);
			return root;
		}

		/// <summary>
		/// Writes an object graph to a stream in Step Physical File (SPF) format according to ISO 10303-21.
		/// </summary>
		/// <param name="stream">The stream to write.</param>
		/// <param name="root">The root object to write (typically IfcProject)</param>
		public override void WriteObject(Stream stream, object root)
		{
			if (stream == null)
				throw new ArgumentNullException("stream");

			if (root == null)
				throw new ArgumentNullException("root");

			List<object> headertags = new List<object>();

			// generate a new header
			// specific order is required per 10303.11: Description, Name, Schema

			FILE_DESCRIPTION tagFileDesc = new FILE_DESCRIPTION(new string[] { "" });
			headertags.Add(tagFileDesc);

			FILE_NAME tagFileName = new FILE_NAME("", "", "", this.Preprocessor, this.Application);
			headertags.Add(tagFileName);

			FILE_SCHEMA tagFileSchema = new FILE_SCHEMA(new string[] { this.Schema });
			headertags.Add(tagFileSchema);

			// write file
			StreamWriter writer = new StreamWriter(stream);

			// write ISO file identifier
			writer.WriteLine("ISO-10303-21;");

			// HEADERS
			writer.WriteLine("HEADER;");
			foreach (object tag in headertags)
			{
				string strheader = this.FormatConstructor(tag, null, null, null);
				writer.Write(strheader);
				writer.WriteLine(";");
			}
			writer.WriteLine("ENDSEC;");
			writer.WriteLine();

			// DATA
			writer.WriteLine("DATA;");

			if (root != null)
			{
				ObjectIDGenerator gen = new ObjectIDGenerator();

				// for formatted output, we split the queue into two: resources and roots
				Queue<object> qRoot = new Queue<object>();
				Queue<object> qResource = new Queue<object>();
				qRoot.Enqueue(root);

				while (qRoot.Count > 0 || qResource.Count > 0)
				{
					// process the queue
					object o;

					if (qResource.Count > 0)
					{
						o = qResource.Dequeue();
					}
					else
					{
						// write space to separate
						writer.WriteLine();
						o = qRoot.Dequeue();
					}

					bool firstTime;
					long id = gen.GetId(o, out firstTime);

					writer.Write("#");
					writer.Write(id);
					writer.Write("= ");

					string format = this.FormatConstructor(o, qRoot, qResource, gen);
					writer.Write(format);

					writer.WriteLine(";");

					// capture inverse fields -- don't use properties, as those will allocate superflously
					IList<PropertyInfo> fields = this.GetFieldsInverseAll(o.GetType());
					foreach (PropertyInfo field in fields)
					{
						object inval = field.GetValue(o);
						if (inval is IEnumerable)
						{
							IEnumerable list = (IEnumerable)inval;
							foreach (object oival in list)
							{
								gen.GetId(oival, out firstTime);
								if (firstTime)
								{
									if (this.RootType.IsInstanceOfType(oival))
									{
										qRoot.Enqueue(oival);
									}
									else if (oival != null)
									{
										qResource.Enqueue(oival);
									}
								}
							}
						}
						else if (inval is object)
						{
							gen.GetId(inval, out firstTime);
							if (firstTime)
							{
								if (this.RootType.IsInstanceOfType(inval))
								{
									qRoot.Enqueue(inval);
								}
								else if (inval != null)
								{
									qResource.Enqueue(inval);
								}
							}
						}
					}
				}
			}

			writer.WriteLine("ENDSEC;");
			writer.WriteLine();
			writer.WriteLine("END-ISO-10303-21;");

			writer.Flush();
		}

		private string FormatConstructor(object o, Queue<object> qRoot, Queue<object> qResource, ObjectIDGenerator idgen)
		{
			StringBuilder sb = new StringBuilder();

			Type t = o.GetType();
			string strType = t.Name.ToUpper();
			sb.Append(strType);
			sb.Append("(");

			if (t.IsValueType)
			{
				PropertyInfo[] fields = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
				if (fields.Length == 1)
				{
					object val = fields[0].GetValue(o);
					string strValue = FormatValue(val, qRoot, qResource, idgen);
					sb.Append(strValue);
				}
			}
			else
			{

				IList<PropertyInfo> fields = this.GetFieldsOrdered(t).Select(x=>x.Value).ToList();
				for (int iField = 0; iField < fields.Count; iField++)
				{
					if (iField != 0)
					{
						sb.Append(",");
					}

					PropertyInfo field = fields[iField];

					if (field == null)
					{
						// special case if field is overridden
						sb.Append("*");
					}
					else
					{
						object val = field.GetValue(o);
						if (field.PropertyType.IsInterface && !field.PropertyType.IsGenericType && val is ValueType)
						{
							// may need to qualify constructor
							if (val != null)
							{
								// value type
								string strValue = FormatConstructor(val, qRoot, qResource, idgen);
								sb.Append(strValue);
							}
							else
							{
								sb.Append("$");
							}
						}
						else
						{
							string strValue = FormatValue(val, qRoot, qResource, idgen);
							sb.Append(strValue);
						}
					}
				}
			}

			sb.Append(")");

			return sb.ToString();
		}

		private string FormatValue(object o, Queue<object> qRoot, Queue<object> qResource, ObjectIDGenerator idgen)
		{
			if (o == null)
			{
				return "$";
			}

			Type t = o.GetType();

			if (t == typeof(Boolean))
			{
				bool bVal = (bool)o;
				if (bVal)
				{
					return ".T.";
				}
				else
				{
					return ".F.";
				}
			}
			else if (t == typeof(Int64))
			{
				return ((Int64)o).ToString(CultureInfo.InvariantCulture);
			}
			else if (t == typeof(Int32))
			{
				return ((Int32)o).ToString(CultureInfo.InvariantCulture);
			}
			else if (t == typeof(Double))
			{
				return FormatDouble((double)o);
			}
			else if (t == typeof(Single))
			{
				return FormatDouble((float)o);
			}
			else if (t == typeof(String))
			{
				return "'" + FormatString((string)o) + "'";
			}
			else if (t == typeof(DateTime))
			{
				DateTime date = (DateTime)o;

				// '2006-07-28T15:17:15'
				return "'" + date.ToString("yyyy-MM-ddTHH:mm:ss") + "'";
			}
			else if (t == typeof(byte[]))
			{
				return SerializeBytes((byte[])o);
			}
			else if (t.IsEnum)
			{
				// if converting enumeration that doesn't exist in target schema, then revert to default (0) value, which is typically NOTDEFINED for IFC
				if (!Enum.IsDefined(t, o))
				{
					o = Enum.ToObject(t, 0);
				}

				return "." + o.ToString() + ".";
			}
			else if (t.IsValueType)
			{
				PropertyInfo[] fields = t.GetProperties();
				if (fields.Length == 1)
				{
					// drill into the field -- may be multiple steps, e.g. IfcPositiveInteger -> IfcInteger -> long
					o = fields[0].GetValue(o);
					return FormatValue(o, qRoot, qResource, idgen);
				}
				else if (fields.Length > 1)
				{
					// wrap it, e.g. IfcCompositePlaneAngleMeasure for latitude/longitude
					StringBuilder sb = new StringBuilder();
					sb.Append("(");
					foreach (PropertyInfo f in fields) //todo: verify order...
					{
						object v = f.GetValue(o);
						string s = FormatValue(v, qRoot, qResource, idgen);
						sb.Append(s);
					}
					sb.Append(")");
					return sb.ToString();
				}
				else
				{
					return ""; // should not be possible to have a defined type without field
				}
			}
			else if (typeof(IEnumerable).IsAssignableFrom(t))
			{
				return FormatList((IEnumerable)o, qRoot, qResource, idgen);
			}
			else if (o is object)
			{
				if (idgen != null)
				{
					// reference another object
					bool firstTime = false;
					long oid = 0;
					try
					{
						oid = idgen.GetId(o, out firstTime);
					}
					catch (SerializationException xx)
					{
						throw new InvalidOperationException(
							"This data set contains more than 13.2 million objects (which is reasonable for detailed models), however cannot be stored using Microsoft .NET technologies. " +
							"a future version will address this by removing such dependencies, however feel free to direct complaints to Microsoft here: https://connect.microsoft.com/VisualStudio/feedback/details/303278/binary-serialization-fails-for-moderately-large-object-graphs", xx);
					}

					if (firstTime)
					{
						if (qRoot != null && this.RootType.IsInstanceOfType(o))
						{
							qRoot.Enqueue(o);
						}
						else if (qResource != null)
						{
							qResource.Enqueue(o);
						}
					}

					return "#" + oid.ToString(CultureInfo.InvariantCulture);
				}
				else
				{
					return o.ToString();
				}
			}

			return null; // should not get here
		}

		private string FormatList(IEnumerable list, Queue<object> qRoot, Queue<object> qResource, ObjectIDGenerator idgen)
		{
			Type typeElement = list.GetType().GetGenericArguments()[0];

			StringBuilder sb = new StringBuilder();
			sb.Append("(");

			int i = 0;
			foreach (object o in list)
			{
				if (i != 0)
				{
					sb.Append(",");
				}
				i++;

				// if value type for interface, must pre-qualify with constructor
				if (typeElement.IsInterface && o != null && o.GetType().IsValueType)
				{
					string strConstructor = FormatConstructor(o, qRoot, qResource, idgen);
					sb.Append(strConstructor);
				}
				else
				{
					string strElement = FormatValue(o, qRoot, qResource, idgen);
					sb.Append(strElement);
				}
			}

			if (i == 0)
			{
				return "$"; // don't export empty collections; use null
			}

			sb.Append(")");
			return sb.ToString();
		}

		private static string FormatString(string value)
		{
			if (value == null)
				return "";

			// check if encoding is required
			bool bRecode = false;
			for (int i = 0; i < value.Length; i++)
			{
				Char ch = value[i];
				if ((ch & 0xFF80) != 0 || ch == '\\' || ch == '\'' ||
					ch == '\r' || ch == '\n' || ch == '\t')
				{
					bRecode = true;
					break;
				}
			}

			// return original if no recoding is required
			if (!bRecode)
			{
				return value;
			}

			bool unicode = false; // flag for indicating whether to store as unicode

			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < value.Length; i++)
			{
				Char ch = value[i];

				// handle unicode
				if (ch > 255)
				{
					// extended encoding
					if (!unicode)
					{
						sb.Append(@"\X2\");
						unicode = true;
					}

					sb.Append(String.Format("{0:X4}", (int)ch));
				}
				else if (unicode)
				{
					// end of unicode; terminate
					sb.Append(@"\X0\");
					unicode = false;
				}

				// then all other modes
				if (ch == '\\')
				{
					// back-slash escaping
					sb.Append(@"\\");
				}
				else if (ch == '\'')
				{
					// single-quote escaping
					sb.Append(@"''"); // single-quote repeated
				}
				else if (ch >= 32 && ch < 126)
				{
					// direct encoding
					sb.Append(ch);
				}
				else if (ch >= 128 + 32 && ch <= 128 + 126)
				{
					// shifted encoding
					Char chMod = (Char)(ch & 0x007F);
					sb.Append(@"\S\");
					sb.Append(chMod);
				}
				else if (ch < 255)
				{
					// other character
					sb.Append(@"\X\");
					sb.Append(String.Format("{0:X2}", (int)ch));
				}
			}

			if (unicode)
			{
				// end of unicode; terminate
				sb.Append(@"\X0\");
				unicode = false;
			}

			return sb.ToString();
		}

		private static string FormatDouble(double value)
		{
			// catch error before encoding
			if (Double.IsNaN(value) || Double.IsInfinity(value))
			{
				value = 0.0;
			}

			string strval = value.ToString(CultureInfo.InvariantCulture);
			if (!strval.Contains("."))
			{
				int index = strval.IndexOf("E");
				if (index >= 0)
				{
					strval = strval.Insert(index, ".0");
				}
				else
				{
					strval = strval + ".";
				}
			}

			return strval;
		}


		private static string ParseString(string encoded)
		{
			// performance optimization - return if no escaping
			if (!encoded.Contains(@"\") && !encoded.Contains("'"))
			{
				return encoded;
			}

			bool bQuote = false;
			bool bEscape = false;
			bool bParse8bit = false;
			bool bParsePage = false;
			bool bUnicode = false;

			Encoding encoding = null; // ISO 8859 encoding -- page lookup may be expensive so avoid until needed

			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < encoded.Length; i++)
			{
				Char ch = encoded[i];

				if (bParse8bit)
				{
					if (bEscape)
					{
						if (encoding == null)
						{
							encoding = Encoding.GetEncoding("iso-8859-1");
							//encoding = Encoding.GetEncoding("iso-8859-9"); //test
						}

						// NEW: get code page
						if (encoding != null)
						{
							//Char ch8 = (Char)(0x80 | ch);
							//byte[] isoBytes = encoding.GetBytes(new char[] { ch8 });
							//byte[] uniBytes = Encoding.Convert(encoding, Encoding.UTF8, isoBytes);

							byte val = (byte)(0x80 | ch);
							Char[] chars = encoding.GetChars(new byte[] { val });
							sb.Append(chars[0]);
						}
						else
						{
							// OLD: assume unicode
							Char ch8 = (Char)(0x80 | ch);
							sb.Append(ch8);
						}

						bParse8bit = false;
						bEscape = false;
					}
					else if (ch == '\\')
					{
						bEscape = true;
					}
				}
				else if (bParsePage)
				{
					if (!bEscape)
					{
						string page = "iso-8859-1";
						switch (ch)
						{
							case 'A':
								page = "iso-8859-1";
								break;
							case 'B':
								page = "iso-8859-2";
								break;
							case 'C':
								page = "iso-8859-3";
								break;
							case 'D':
								page = "iso-8859-4";
								break;
							case 'E':
								page = "iso-8859-5";
								break;
							case 'F':
								page = "iso-8859-6";
								break;
							case 'G':
								page = "iso-8859-7";
								break;
							case 'H':
								page = "iso-8859-8";
								break;
							case 'I':
								page = "iso-8859-9";
								break;
						}

						encoding = Encoding.GetEncoding(page);
					}
					else if (ch == '\\')
					{
						bEscape = false;
					}
				}
				else if (bEscape)
				{
					if (ch == 'S')
					{
						bParse8bit = true;
						bEscape = false;
					}
					else if (ch == 'P')
					{
						bParsePage = true;
						bEscape = false;
					}
					else if (ch == 'X')
					{
						bEscape = false;

						if (i + 6 < encoded.Length && encoded[i + 1] == '2' && encoded[i + 2] == '\\')
						{
							bUnicode = true;
							i += 2;
						}
						else if (i + 2 < encoded.Length && encoded[i + 1] == '0' && encoded[i + 2] == '\\')
						{
							// end of unicode
							bUnicode = false;
							i += 2;
						}
						else if (i + 10 < encoded.Length && encoded[i + 1] == '4' && encoded[i + 2] == '\\')
						{
							// 4 bytes
							// \X4\00000000
							string strHex = encoded.Substring(i + 3, 8);
							int hexval = Int32.Parse(strHex, System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture);
							Char chHex = (Char)hexval;
							sb.Append(chHex);
							i += 10;
						}
						else if (i + 3 < encoded.Length && encoded[i + 1] == '\\')
						{
							// 1 byte
							// \X\00
							string strHex = encoded.Substring(i + 2, 2);
							int hexval = Int32.Parse(strHex, System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture);
							Char chHex = (Char)hexval;
							sb.Append(chHex);
							i += 3;
						}
						else
						{
							throw new InvalidDataException("Invalid string.");
						}
					}
					else if (ch == '\\')
					{
						// double-escape-sequence means back-slash
						bEscape = false;
						sb.Append(ch);
					}
				}
				else if (ch == '\\')
				{
					bEscape = true;
				}
				else if (ch == '\'')
				{
					// quotes doubled up
					if (bQuote)
					{
						sb.Append(ch);
						bQuote = false;
					}
					else
					{
						bQuote = true;
					}
				}
				else if (bUnicode)
				{
					string strHex = encoded.Substring(i, 4);
					int hexval = Int32.Parse(strHex, System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture);
					Char chHex = (Char)hexval;
					sb.Append(chHex);
					i += 3; // account for 3 additional characters
				}
				else
				{
					sb.Append(ch);
				}
			}

			return sb.ToString();
		}

		private static bool ParseBoolean(string strval)
		{
			switch (strval)
			{
				case ".T.":
					return true;

				case ".F.":
				default:
					return false;
			}
		}

		private static long ParseInteger(string strval)
		{
			long iValue = 0;
			if (Int64.TryParse(strval, NumberStyles.Integer, CultureInfo.InvariantCulture, out iValue))
			{
				return iValue;
			}
			else
			{
				// could have dot
				double dValue = 0.0;
				if (Double.TryParse(strval, NumberStyles.Float, CultureInfo.InvariantCulture, out dValue))
				{
					return (long)dValue;
				}
			}

			return iValue;
		}

		private static double ParseReal(string strval)
		{
			double dValue = 0.0;
			Double.TryParse(strval, NumberStyles.Float, CultureInfo.InvariantCulture, out dValue);
			return dValue;
		}

		/// <summary>
		/// Parses primitive if defined, otherwise null if type is not a primitive.
		/// </summary>
		/// <param name="strval"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		private static object ParsePrimitive(string strval, Type type)
		{
			object value = null;
			if (typeof(Int64) == type)
			{
				// INTEGER
				value = ParseInteger(strval);
			}
			else if (typeof(Int32) == type)
			{
				value = (Int32)ParseInteger(strval);
			}
			else if (typeof(Double) == type)
			{
				// REAL
				value = ParseReal(strval);
			}
			else if (typeof(Single) == type)
			{
				value = (Single)ParseReal(strval);
			}
			else if (typeof(Boolean) == type)
			{
				// BOOLEAN
				value = ParseBoolean(strval);
			}
			else if (typeof(String) == type)
			{
				// STRING
				strval = strval.Substring(1, strval.Length - 1); // remove quotes - don't use String.Trim because it may start or end with escape quotes!
				value = ParseString(strval);
			}
			else if (typeof(byte[]) == type)
			{
				// BINARY
				value = ParseBinary(strval);
			}

			return value;
		}

		private object ParseValue(Type type, string strval, Dictionary<long, object> idmap)
		{
			object value = null;

			if (strval == "$" || strval == "*")
			{
				// special case if list - create it
				if (type != typeof(string) && type != typeof(byte[]) &&
					typeof(IEnumerable).IsAssignableFrom(type))
				{
					Type typeCollection = this.GetCollectionInstanceType(type);
					value = Activator.CreateInstance(typeCollection);
				}

				return value;
			}

			if (type.IsGenericType && type.IsValueType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
			{
				// special case for Nullable types
				type = type.GetGenericArguments()[0];
			}

			// primitive types -- typical only for header data
			value = ParsePrimitive(strval, type);
			if (value != null)
				return value;

			if (type.IsEnum)
			{
				// enumeration
				strval = strval.Trim('.');
				System.Reflection.FieldInfo enumfield = type.GetField(strval, BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase);
				if (enumfield != null)
				{
					value = enumfield.GetValue(null);
				}
			}
			else if (type.IsValueType)
			{
				// defined type -- get the underlying field -- may recurse multiple times, e.g. IfcPositiveLengthMeasure
				PropertyInfo[] fields = type.GetProperties(BindingFlags.Instance | BindingFlags.Public); //perf: cache this
				if (fields.Length == 1)
				{
					PropertyInfo fieldValue = fields[0];

					object primval = ParsePrimitive(strval, fieldValue.PropertyType);
					if (primval != null)
					{
						value = Activator.CreateInstance(type);
						fieldValue.SetValue(value, primval);
					}
					else
					{
						object innerval = ParseValue(fieldValue.PropertyType, strval, idmap);
						if (innerval != null)
						{
							value = Activator.CreateInstance(type);
							fieldValue.SetValue(value, innerval);
						}
					}
				}
			}
			else if (typeof(IEnumerable).IsAssignableFrom(type))
			{
				value = ParseList(type, strval, idmap);
			}
			else
			{
				value = ParseObject(strval, idmap); // must not be null!!! -- if so, then referenced instance does not exist
			}

			return value;
		}

		private IEnumerable ParseList(Type t, string line, Dictionary<long, object> idmap)
		{
			// use instantiable collection type
			Type typeCollection = this.GetCollectionInstanceType(t);
			IEnumerable list = (IEnumerable)Activator.CreateInstance(typeCollection);

			// perf: customize...
			MethodInfo methodAdd = typeCollection.GetMethod("Add");
			if (methodAdd == null)
			{
				return null;
			}

			bool bQuote = false;
			bool bValue = false;
			int nNest = 0;
			int x = 1;
			int x0 = x;

			while (x < line.Length - 1)
			{
				char ch = line[x];
				switch (ch)
				{
					case '\'': // entering or existing string
						bQuote = !bQuote;
						break;

					case '(':
						if (!bQuote)
						{
							nNest++;
						}
						break;

					case ')':
						if (!bQuote)
						{
							nNest--;
						}
						break;

					case ',': // end of parameter
						if (!bQuote && nNest == 0)
						{
							bValue = true;
						}
						break;
				}

				if (x == line.Length - 2)
				{
					bValue = true;
					x++;
				}

				if (bValue)
				{
					// parse it out
					string strval = line.Substring(x0, x - x0);

					Type elemtype = t.GetGenericArguments()[0];
					object value = ParseValue(elemtype, strval, idmap);

					// assign the value -- could be null, add it anyways
					try
					{
						//list.Add(value);
						methodAdd.Invoke(list, new object[] { value }); // perf!!
					}
					catch (Exception e)
					{
						// could be type that changed and is no longer compatible with schema -- try to keep going
						System.Diagnostics.Debug.WriteLine("StepSerializer: " + e.Message);
					}

					// reset to next
					x0 = x + 1;

					bValue = false;
				}

				x++;
			}

			return list;
		}

		private object ParseObject(string strval, Dictionary<long, object> idmap)
		{
			object value = null;
			if (strval[0] == '#')
			{
				// reference to another object
				long iIndex = Int64.Parse(strval.Substring(1), CultureInfo.InvariantCulture);
				object entity = null;
				if (idmap.TryGetValue(iIndex, out entity))
				{
					value = entity;
				}
			}
			else
			{
				// inline
				value = ParseConstructor(strval, idmap);
			}

			return value;
		}

		private object ParseConstructor(
			 string line,
			 Dictionary<long, object> idmap)
		{
			int iParam = line.IndexOf('(');
			if (iParam == -1)
			{
				throw new InvalidDataException("Invalid Data: " + line);
			}

			string strType = line.Substring(0, iParam);
			strType = strType.Trim();

			Type t = this.GetNonAbstractTypeByName(strType);
			if (t == null)
			{
				throw new FormatException("Unknown type: " + line);
			}

			object instance = Activator.CreateInstance(t);// FormatterServices.GetUninitializedObject(t);
			ParseFields(instance, line, idmap);
			return instance;
		}

		private void ParseFields(object instance, string line, Dictionary<long, object> idmap)
		{
			Type t = instance.GetType();

			IList<PropertyInfo> fields = this.GetFieldsOrdered(t).Select(x=>x.Value).ToList();
			if (fields.Count == 0)
			{
				PropertyInfo f = t.GetProperty("Value"); //!!!!!REFACTOR: MUST USE FIELD!!!! -- no property encoding....
				fields = new List<PropertyInfo>(new PropertyInfo[] { f });
			}

			int iParam = line.IndexOf('(');
			try
			{
				int n = 0;
				int x = iParam + 1;
				int x0 = x;
				bool bQuote = false;
				bool bEscape = false; // \ inside string
				int nNest = 0;
				bool bValue = false;

				while (x < line.Length - 1 && n < fields.Count)
				{
					char ch = line[x];
					switch (ch)
					{
						case '\'': // entering or existing string
							if (!bEscape)
							{
								bQuote = !bQuote;
							}
							break;

						case '(':
							if (!bQuote)
							{
								nNest++;
							}
							break;

						case ')':
							if (!bQuote)
							{
								nNest--;
							}
							break;

						case ',': // end of parameter
							if (!bQuote && nNest == 0)
							{
								bValue = true;
							}
							break;
					}

					if (bEscape)
					{
						bEscape = false;
					}

					if (x == line.Length - 2)
					{
						bValue = true;
						x++;
					}

					if (bValue)
					{
						// parse it out
						PropertyInfo field = fields[n];

						// if field is null, means derived
						if (field != null)
						{
							string strval = line.Substring(x0, x - x0);
							ParseField(field, instance, strval, idmap);
						}

						// reset to next
						x0 = x + 1;
						n++;

						bValue = false;
					}

					x++;
				}
			}
			catch (Exception e)
			{
				// invalid data!
				System.Diagnostics.Debug.WriteLine("StepSerializer: " + e.Message);
			}
		}

		private void ParseField(
			   PropertyInfo field,
			   object instance,
			   string strval,
			   Dictionary<long, object> idmap)
		{
			// "*" means accept existing/derived value
			if (strval.Length == 0 ||
				(strval.Length == 1 && strval[0] == '*'))
			{
				return;
			}

			// assign the value
			object value = null;
			if (field.PropertyType == typeof(object))
				value = strval;
			else
			{
				try
				{
					value = ParseValue(field.PropertyType, strval, idmap);
				}
				catch (Exception e)
				{
					// e.g. null value for list
					System.Diagnostics.Debug.WriteLine("StepSerializer: " + e.Message);
				}
			}

			if (value != null && field.PropertyType.IsInstanceOfType(value))
			{
				LoadEntityValue(instance, field, value);

				//... todo: use generated code for specialized collections to keep inverse properties in sync...
				this.UpdateInverse(instance, field, value);
			}
		}

		private void ReadContent(Stream stream, Dictionary<long, object> idmap, ParseScope parsescope)
		{
			stream.Position = 0;
			StreamReader reader = new StreamReader(stream, Encoding.ASCII);
			ParseSection parse = ParseSection.Unknown;
			string commandline = ReadNext(reader);

			while (commandline != null)
			{
				switch (parse)
				{
					case ParseSection.Unknown:
						if (commandline.Equals("ISO-10303-21"))
						{
							parse = ParseSection.IsoStep;
						}
						else
						{
							// invalid format
							throw new NotSupportedException("Format is not ISO-10303-21");
						}
						break;

					case ParseSection.IsoStep:
						if (commandline.Equals("HEADER"))
						{
							parse = ParseSection.Header;
						}
						else if (commandline.Equals("DATA"))
						{
							parse = ParseSection.Data;
						}
						else if (commandline.Equals("END-ISO-10303-21"))
						{
							parse = ParseSection.Unknown;
						}
						break;

					case ParseSection.Header:
						if (commandline.Equals("ENDSEC"))
						{
							if (parsescope == ParseScope.Header)
							{
								// got everything we need from header, so return
								return;
							}

							parse = ParseSection.IsoStep;
						}
						else if (parsescope == ParseScope.Header ||	parsescope == ParseScope.DataInstances)
						{
							// process header
							int iParam = commandline.IndexOf('(');
							if (iParam == -1)
							{
								throw new InvalidDataException("Invalid Data: " + commandline);
							}

							string strType = commandline.Substring(0, iParam);
							strType = strType.Trim();

							switch (strType)
							{
								case "FILE_DESCRIPTION":
									FILE_DESCRIPTION fileDescription = new FILE_DESCRIPTION(new string[] { });
									this.ParseFields(fileDescription, commandline, idmap);
									break;

								case "FILE_SCHEMA":
									FILE_SCHEMA fileSchema = new FILE_SCHEMA(new string[] { });
									this.ParseFields(fileSchema, commandline, idmap);
									if(fileSchema.schema.Count > 0)
									this.Schema = fileSchema.schema[0];
									break;

								case "FILE_NAME":
									FILE_NAME fileName = new FILE_NAME("", "", "", "", "");
									this.ParseFields(fileName, commandline, idmap);
									break;
							}

						}
						break;

					case ParseSection.Data:
						if (commandline.Equals("ENDSEC"))
						{
							parse = ParseSection.IsoStep;
						}
						else
						{
							// process data
							ReadCommand(commandline, idmap, parsescope);
						}
						break;
				}

				string commandnext = ReadNext(reader);

				if (commandnext == null && parse != ParseSection.Unknown)
				{
					// invalid file -- not terminated -- e.g. quote not escaped properly
					throw new InvalidDataException(
						"This file contains format errors and cannot be loaded - it may not be terminated properly, " +
						"or may contain fields with invalid encodings such as quotes that are not escaped properly.\r\n\r\n" +
						"The last command successfully parsed was the following:\r\n" +
					commandline);//this.LogError(ExtensionErrorType.Unknown, null, null, null);
				}

				commandline = commandnext;
			}
		}

		/// <summary>
		/// Reads an ISO-STEP command and populates broker according to scope
		/// </summary>
		/// <param name="line">The processed STEP line to parse</param>
		private void ReadCommand(string line, Dictionary<long, object> idmap, ParseScope parsescope)
		{
			if (line[0] != '#')
			{
				// invalid
				throw new FormatException("Bad format: command must start with '#'");
			}

			if (line.Contains("ENTITIES("))
			{
				this.ToString();
			}

			int iIdTail = line.IndexOf('=');
			if (iIdTail == -1)
			{
				throw new FormatException("Bad format: object identifier must be followed by '='");
			}
			string strId = line.Substring(1, iIdTail - 1);

			long id;
			if (!Int64.TryParse(strId, NumberStyles.Integer, CultureInfo.InvariantCulture, out id))
			{
				throw new FormatException("Bad format: object identifier must be 32-bit signed integer");
			}

			string strConstructor = line.Substring(iIdTail + 1);

			switch (parsescope)
			{
				case ParseScope.DataInstances:
					{
						int iParam = strConstructor.IndexOf('(');
						if (iParam == -1)
						{
							throw new InvalidDataException("Invalid Data: " + line);
						}

						string strType = strConstructor.Substring(0, iParam);
						strType = strType.Trim();

						Type t = this.GetNonAbstractTypeByName(strType);
						if (t == null)
						{
							System.Diagnostics.Debug.WriteLine("Unrecognized entity " + strType);
						}
						else
						{
							object o = FormatterServices.GetUninitializedObject(t); // works if no parameterless constructor is defined
																					// capture project
							if (this.ProjectType != null && this.ProjectType.IsInstanceOfType(o) && !idmap.ContainsKey(0))
								idmap.Add(0, o);
							idmap.Add(id, o);
							// populate collections (catch case of older version where field may not be asserted)
							IEnumerable<PropertyInfo> listProp = GetFieldsAll(t).Select(x=>x.Value);
							foreach (PropertyInfo prop in listProp)
							{
								Type type = prop.PropertyType;
								if (type != typeof(string) && type != typeof(byte[]) &&
									typeof(IEnumerable).IsAssignableFrom(type))
								{
									Type typeCollection = this.GetCollectionInstanceType(type);
									object colval = Activator.CreateInstance(typeCollection);
									prop.SetValue(o, colval);
								}

							}
						}
					}
					break;

				case ParseScope.DataFields:
					{
						object instance = null;
						if (idmap.TryGetValue(id, out instance))
						{
							this.ParseFields(instance, strConstructor, idmap);
						}
					}
					break;
			}
		}

		private string ReadNext(StreamReader reader)
		{
			// reads the next expression
			// skips whitespace, goes until ";" (ignores comments and string literals)

			StringBuilder sb = new StringBuilder();

			ParseCommand parse = ParseCommand.Open;
			while (parse != ParseCommand.End)
			{
				int iChar = reader.Read();
				if (iChar == -1)
				{
					// end of file
					return null;
				}

				char ch = (char)iChar;
				bool bAppend = true;
				bool bEscape = false;

				// case of "/**** " -> back in comment mode
				if (parse == ParseCommand.CommentLeave && ch != '/')
				{
					parse = ParseCommand.Comment;
				}

				if (parse == ParseCommand.Comment)
				{
					bAppend = false;
				}

				switch (ch)
				{
					case ';': // done
						if (parse == ParseCommand.Open)
						{
							return sb.ToString();
						}
						break;

					case '/': // about to enter a command or about to leave a comment
						if (parse == ParseCommand.Open)
						{
							parse = ParseCommand.CommentEnter;
							bAppend = false;
						}
						else if (parse == ParseCommand.CommentLeave)
						{
							bAppend = false;
							parse = ParseCommand.Open;
						}
						break;

					case '*':
						if (parse == ParseCommand.CommentEnter)
						{
							bAppend = false;
							parse = ParseCommand.Comment;
						}
						else if (parse == ParseCommand.Comment)
						{
							parse = ParseCommand.CommentLeave;
						}
						break;

					case ' ': // empty space
					case '\r':
					case '\n':
					case '\t':
						if (parse == ParseCommand.Open)
						{
							bAppend = false;
						}
						break;

					case '\'':
						if (parse == ParseCommand.Open)
						{
							parse = ParseCommand.String;
						}
						else if (parse == ParseCommand.String)
						{
							parse = ParseCommand.Open;
						}
						break;
				}

				// leaving escape mode
				if (parse == ParseCommand.StringEscape && !bEscape)
				{
					parse = ParseCommand.String;
				}

				if (bAppend)
				{
					sb.Append(ch);
				}
			}

			return null; // end of file!
		}

		private enum ParseSection
		{
			Unknown = 0, // waiting for ISO-STEP directive
			IsoStep = 1, // ISO-STEP; waiting for header or data
			Header = 2, // header; receiving header elements until END_HEADER
			Data = 3, // receiving data until end-data
		}

		private enum ParseScope
		{
			None = 0,
			Header = 1,
			DataInstances = 2,
			DataFields = 3,
		}

		public enum ParseCommand
		{
			Open = 0, // normal parsing
			End = 1, // end of 
			String = 2, // inside a string
			Comment = 3, // inside a comment
			CommentEnter = 4, // possibly entering a comment (/)
			CommentLeave = 5, // possibly leaving a comment (*)
			StringEscape = 6, // inside escape sequence of string
		}
	}


}
