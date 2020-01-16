// Name:        SchemaCNF.cs
// Description: Schema for ISO-10303-28 configuraiton
// Author:      Tim Chipman
// Origination: Work performed for BuildingSmart by Constructivity.com LLC.
// Copyright:   (c) 2013 BuildingSmart International Ltd.
// License:     http://www.buildingsmart-tech.org/legal

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace IfcDoc.Schema.CNF
{
	public static class SchemaCNF
	{
		static Dictionary<string, Type> s_types;

		public static Dictionary<string, Type> Types
		{
			get
			{
				if (s_types == null)
				{
					s_types = new Dictionary<string, Type>();

					Type[] types = typeof(SchemaCNF).Assembly.GetTypes();
					foreach (Type t in types)
					{
						if (typeof(SEntity).IsAssignableFrom(t) && !t.IsAbstract && t.Namespace.Equals("IfcDoc.Schema.CNF"))
						{
							string name = t.Name.ToUpper();
							s_types.Add(name, t);
						}
					}
				}

				return s_types;
			}
		}

		public const string DefaultNamespace = "urn:iso:std:iso:10303:-28:ed-2:tech:XMLschema:configuration_language";

		private static XmlSerializerNamespaces _Prefixes;
		public static XmlSerializerNamespaces Prefixes
		{
			get
			{
				if (_Prefixes == null)
				{
					_Prefixes = new XmlSerializerNamespaces();
					_Prefixes.Add("cnf", DefaultNamespace);
					_Prefixes.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");
				}

				return _Prefixes;
			}
		}
	}

	[XmlRoot(Namespace = SchemaCNF.DefaultNamespace)]
	public class configuration
	{
		[XmlAttribute]
		public string id;

		//[XmlAttribute("schemaLocation", Namespace="xsi")]
		[XmlAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, Namespace = "http://www.w3.org/2001/XMLSchema-instance")]
		public string schemaLocation = "urn:iso:std:iso:10303:-28:ed-2:tech:XMLschema:configuration_language http://www.buildingsmart-tech.org/ifcXML/IFC4/P28/cnf.xsd";

		[XmlElement]
		public List<option> option = new List<option>();

		[XmlElement]
		public List<schema> schema = new List<schema>();

		[XmlElement]
		public List<uosElement> uosElement = new List<uosElement>();

		[XmlElement]
		public List<type> type = new List<type>();

		[XmlElement]
		public List<entity> entity = new List<entity>();
	}

	public enum exp_type
	{
		root = 1,
		value = 2,
		unspecified = 0,
	}

	public enum exp_attribute_global
	{
		[XmlEnum("double-tag")] double_tag = 1,
		[XmlEnum("attribute-tag")] attribute_tag = 2,
		[XmlEnum("attribute-content")] attribute_content = 3,
	}

	public enum exp_attribute
	{
		[XmlEnum("double-tag")] double_tag = 1,
		[XmlEnum("attribute-tag")] attribute_tag = 2,
		[XmlEnum("type-tag")] type_tag = 3,
		[XmlEnum("no-tag")] no_tag = 4,
		[XmlEnum("no-tag-simple")] no_tag_simple = 5,
		[XmlEnum("attribute-content")] attribute_content = 6,
		[XmlEnum("unspecified")] unspecified = 0,
	}

	public enum attributeType
	{
		inverse = 1,
		derive = 2,
		[XmlEnum("derive-inverse")] derive_inverse = 3,
	}

	public enum boolean_or_unspecified
	{
		[XmlEnum("unspecified")] unspecified = 0,
		[XmlEnum("true")] boolean_true = 1,
		[XmlEnum("false")] boolean_false = 2,
	}

	public enum qual
	{
		qualified = 1,
		unqualified = 2,
	}

	public enum naming_convention
	{
		[XmlEnum("initial-upper")] initial_upper = 0,
		[XmlEnum("camel-case")] camel_case = 1,
		[XmlEnum("preserve-case")] preserve_case = 2,
	}

	public class option
	{
		[XmlAttribute]
		public bool inheritance;

		[XmlAttribute("exp-type"), DefaultValue(exp_type.unspecified)]
		public exp_type exp_type;

		[XmlAttribute("entity-attribute"), DefaultValue(exp_attribute_global.double_tag)]
		public exp_attribute_global entity_attribute;

		[XmlAttribute("concrete-attribute"), DefaultValue(exp_attribute_global.attribute_tag)]
		public exp_attribute_global concrete_attribute;

		[XmlAttribute, DefaultValue(boolean_or_unspecified.unspecified)]
		public boolean_or_unspecified tagless;

		[XmlAttribute("naming-convention"), DefaultValue(naming_convention.initial_upper)]
		public naming_convention naming_convention;

		//[XmlAttribute("keep-all")]
		//public attributeType keep_all;

		[XmlAttribute("generate-keys"), DefaultValue(true)]
		public bool generate_keys;
	}

	public class uosElement
	{
		[XmlAttribute]
		public string name;
	}

	public class schema
	{
		[XmlAttribute]
		public string targetNamespace;

		[XmlAttribute]
		public qual elementFormDefault;

		[XmlAttribute]
		public qual attributeFormDefault;

		[XmlAttribute("embed-schema-items")]
		public bool embed_schema_items;

		[XmlElement("namespace")]
		public _namespace _namespace;

		[XmlElement("include")]
		public include include;
	}

	public class _namespace
	{
		[XmlAttribute]
		public string prefix;

		[XmlAttribute]
		public string alias;
	}

	public class include
	{
		[XmlAttribute]
		public string urn;

		[XmlAttribute("schema-location")]
		public string schema_location;
	}

	public class type
	{
		[XmlAttribute]
		public string select;

		[XmlAttribute]
		public string map;

		[XmlAttribute("exp-type"), DefaultValue(exp_type.unspecified)]
		public exp_type exp_type;

		[XmlAttribute, DefaultValue(boolean_or_unspecified.unspecified)]
		public boolean_or_unspecified tagless;

		[XmlAttribute]
		public string notation;

		[XmlAttribute, DefaultValue(true)]
		public bool keep = true;

		[XmlAttribute, DefaultValue(false)]
		public bool flatten;
	}

	public class entity
	{
		[XmlAttribute]
		public string select;

		[XmlElement]
		public List<attribute> attribute = new List<attribute>();

		[XmlElement]
		public List<inverse> inverse = new List<inverse>();
	}

	public class attribute
	{
		[XmlAttribute]
		public string select;

		[XmlAttribute("exp-attribute"), DefaultValue(exp_attribute.unspecified)]
		public exp_attribute exp_attribute;

		[XmlAttribute, DefaultValue(true)]
		public bool keep = true;

		[XmlAttribute, DefaultValue(boolean_or_unspecified.unspecified)]
		public boolean_or_unspecified tagless;
	}

	public class inverse
	{
		[XmlAttribute]
		public string select;

		[XmlAttribute("exp-attribute"), DefaultValue(exp_attribute.unspecified)]
		public exp_attribute exp_attribute;
	}
}
