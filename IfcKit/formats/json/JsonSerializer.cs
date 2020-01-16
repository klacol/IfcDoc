// Name:        JsonSerializer.cs
// Description: JSON serializer
// Author:      Tim Chipman
// Origination: Work performed for BuildingSmart by Constructivity.com LLC.
// Copyright:   (c) 2017 BuildingSmart International Ltd.
// License:     http://www.buildingsmart-tech.org/legal

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BuildingSmart.Serialization.Xml;

namespace BuildingSmart.Serialization.Json
{
	public class JsonSerializer : XmlSerializer
	{
		public JsonSerializer(Type type) : base(type)
		{
		}

		/// <summary>
		/// Terminates the opening tag, to allow for sub-elements to be written
		/// </summary>
		protected override void WriteOpenElement(StreamWriter writer)
		{
			// do nothing
		}

		/// <summary>
		/// Terminates the opening tag, with no subelements
		/// </summary>
		protected override void WriteCloseElementEntity(StreamWriter writer, ref int indent)
		{
			indent--;
			this.WriteIndent(writer, indent);
			writer.WriteLine("}");
		}

		protected override void WriteCloseElementAttribute(StreamWriter writer, ref int indent)
		{
			// do nothing
		}

		protected override void WriteEntityStart(StreamWriter writer, ref int indent)
		{
			this.WriteIndent(writer, indent);
			writer.WriteLine("{");
			indent++;
		}

		protected override void WriteEntityEnd(StreamWriter writer, ref int indent)
		{
			indent--;
			this.WriteIndent(writer, indent);
			writer.Write("}");
		}

		protected override void WriteStartElementEntity(StreamWriter writer, ref int indent, string name)
		{
			this.WriteIndent(writer, indent);
			writer.WriteLine("{");
			indent++;
			this.WriteType(writer, indent, name);
		}

		protected override void WriteStartElementAttribute(StreamWriter writer, ref int indent, string name)
		{
			this.WriteIndent(writer, indent);
			writer.WriteLine("\"" + name + "\": ");
		}

		protected override void WriteEndElementEntity(StreamWriter writer, ref int indent, string name)
		{
			indent--;
			this.WriteIndent(writer, indent);
			writer.Write("}");
		}

		protected override void WriteEndElementAttribute(StreamWriter writer, ref int indent, string name)
		{
			// do nothing
		}

		protected override void WriteIdentifier(StreamWriter writer, int indent, string id)
		{
			this.WriteIndent(writer, indent);
			writer.Write("\"id\": ");
			writer.Write(id);
			writer.WriteLine(",");
		}

		protected override void WriteReference(StreamWriter writer, int indent, string id)
		{
			this.WriteIndent(writer, indent);
			writer.Write("\"href\": ");
			writer.Write(id);
			writer.WriteLine();
		}

		protected override void WriteType(StreamWriter writer, int indent, string type)
		{
			this.WriteIndent(writer, indent);
			writer.Write("\"type\": \"");
			writer.Write(type);
			writer.WriteLine("\",");
		}

		protected override void WriteTypedValue(StreamWriter writer, ref int indent, string type, string value)
		{
			this.WriteEntityStart(writer, ref indent);
			this.WriteType(writer, indent, type);
			this.WriteIndent(writer, indent);
			writer.WriteLine("\"value\": \"" + value + "\"");
			this.WriteEntityEnd(writer, ref indent);
		}

		protected override void WriteStartAttribute(StreamWriter writer, int indent, string name)
		{
			this.WriteIndent(writer, indent);
			writer.Write("\"" + name + "\": \"");
		}

		protected override void WriteEndAttribute(StreamWriter writer)
		{
			writer.Write("\"");
		}

		protected override void WriteHeader(StreamWriter writer)
		{
			writer.WriteLine("{");
			writer.WriteLine("  \"ifc\": [");
			writer.WriteLine("  {");
		}

		protected override void WriteFooter(StreamWriter writer)
		{
			writer.WriteLine("  }");
			writer.WriteLine("  ]");
			writer.WriteLine("}");
		}

		protected override void WriteAttributeDelimiter(StreamWriter writer)
		{
			writer.WriteLine(",");
		}

		protected override void WriteAttributeTerminator(StreamWriter writer)
		{
			writer.WriteLine(); // ensure closing bracket is on next line
		}

		protected override void WriteCollectionDelimiter(StreamWriter writer, int indent)
		{
			this.WriteIndent(writer, indent);
			writer.WriteLine(",");
		}

		protected override void WriteCollectionStart(StreamWriter writer, ref int indent)
		{
			this.WriteIndent(writer, indent);
			writer.WriteLine("[");
			indent++;
		}

		protected override void WriteCollectionEnd(StreamWriter writer, ref int indent)
		{
			writer.WriteLine();
			indent--;
			this.WriteIndent(writer, indent);
			writer.WriteLine("]");
		}

		protected override void WriteRootDelimeter(StreamWriter writer)
		{
			writer.WriteLine(",");
		}

	}
}
