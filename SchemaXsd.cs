// Name:        SchemaXsd.cs
// Description: Schema for ISO-10303-28 configuraiton
// Author:      Tim Chipman
// Origination: Work performed for BuildingSmart by Constructivity.com LLC.
// Copyright:   (c) 2014 BuildingSmart International Ltd.
// License:     http://www.buildingsmart-tech.org/legal

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace IfcDoc.Schema.XSD
{
	public static class SchemaXsd
	{
		public const string DefaultNamespace = "http://www.w3.org/2001/XMLSchema";
	}

	public class schema
	{
		[XmlAttribute]
		public string id;

		[XmlAttribute]
		public string version;

		[XmlElement]
		public List<simpleType> simpleType = new List<simpleType>();

		[XmlElement]
		public List<complexType> complexType = new List<complexType>();

		[XmlElement]
		public List<element> element = new List<element>();
	}

	public class element
	{
		[XmlAttribute]
		public string name;

		[XmlAttribute]
		public string type;

		[XmlAttribute("ref")]
		public string reftype;

		[XmlAttribute("minOccurs")]
		public string minOccurs;

		[XmlAttribute("maxOccurs")]
		public string maxOccurs;

		[XmlElement]
		public annotation annotation;

		[XmlElement]
		public complexType complexType;
	}

	public class simpleType
	{
		[XmlAttribute]
		public string name;

		[XmlElement]
		public annotation annotation;

		[XmlElement]
		public restriction restriction;
	}

	public class complexType
	{
		[XmlAttribute]
		public string name;

		[XmlElement]
		public annotation annotation;

		[XmlElement]
		public sequence sequence;

		[XmlElement]
		public choice choice;

		[XmlElement]
		public all all;

		[XmlElement]
		public List<attribute> attribute = new List<attribute>();

		[XmlElement]
		public complexContent complexContent;
	}

	public class complexContent
	{
		[XmlElement] public extension extension;
	}

	public class extension
	{
		[XmlAttribute("base")] public string basetype;

		[XmlElement]
		public List<attribute> attribute = new List<attribute>();

		[XmlElement]
		public choice choice;
	}

	public class choice
	{
		[XmlAttribute("minOccurs")]
		public string minOccurs;

		[XmlAttribute("maxOccurs")]
		public string maxOccurs;

		[XmlElement]
		public List<element> element = new List<element>();
	}

	public class sequence
	{
		[XmlElement]
		public List<element> element = new List<element>();
	}

	public class all
	{
		[XmlElement]
		public List<element> element = new List<element>();
	}

	public class annotation
	{
		[XmlElement]
		public List<string> documentation;
	}

	public class restriction
	{
		[XmlAttribute("base")]
		public string basetype;

		[XmlElement]
		public List<enumeration> enumeration;
	}

	public class enumeration
	{
		[XmlAttribute]
		public string value;

		[XmlElement]
		public annotation annotation;
	}

	public class attribute
	{
		[XmlAttribute]
		public string name;

		[XmlAttribute]
		public string type;

		[XmlAttribute]
		public use use;

		[XmlElement]
		public simpleType simpleType;
	}

	public enum use
	{
		optional = 0,
		required = 1,
		prohibited = 2,
	}
}
