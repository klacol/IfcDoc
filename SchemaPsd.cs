// Name:        SchemaPsd.cs
// Description: IFC property set schema
// Author:      Tim Chipman
// Origination: Work performed for BuildingSmart by Constructivity.com LLC.
// Copyright:   (c) 2010 BuildingSmart International Ltd.
// License:     http://www.buildingsmart-tech.org/legal

using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace IfcDoc.Schema.PSD
{
	public abstract class DefinitionSet
	{
		[XmlElement("IfcVersion", typeof(IfcVersion))]
		public List<IfcVersion> Versions;

		public string Name;
		public string Definition;
		public string Applicability = String.Empty; // obsolete

		public List<ClassName> ApplicableClasses;
		public string ApplicableTypeValue;
	}

	// Quantities
	public class QtoSetDef : DefinitionSet
	{
		public string MethodOfMeasurement;
		public List<QtoDef> QtoDefs;
		public List<QtoDefinitionAlias> QtoDefinitionAliases;

		public const string DefaultNamespace = "http://www.buildingsmart-tech.org/xml/qto/QTO_IFC4.xsd";
	}

	public class QtoDefinitionAlias
	{
		[XmlAttribute("lang")]
		public string lang;

		[XmlText]
		public string Value;
	}

	public class QtoDef
	{
		public string Name;
		public string Definition;
		public string QtoType;

		public List<NameAlias> NameAliases;
		public List<DefinitionAlias> DefinitionAliases;
	}

	public class PropertySetDef : DefinitionSet
	{
		public List<PropertyDef> PropertyDefs;
		public List<PsetDefinitionAlias> PsetDefinitionAliases;

		[XmlAttribute("ifdguid")]
		public string IfdGuid;

		[XmlAttribute("templatetype")]
		public string TemplateType;

		public const string DefaultNamespace = "http://buildingSMART-tech.org/xml/psd/PSD_IFC4.xsd";
		public const string DefaultFile = "PSD_IFC4.xsd";

		[XmlAttribute(Namespace = "http://www.w3.org/2001/XMLSchema-instance", AttributeName = "noNamespaceSchemaLocation")]
		public string NoNamespaceSchemaLocation = PropertySetDef.DefaultNamespace;

		//[XmlAttribute(Namespace = "http://www.w3.org/2001/XMLSchema-instance", AttributeName = "schemaLocation")]
		//public string SchemaLocation = PropertySetDef.DefaultNamespace + " " + DefaultFile;
	}

	public class PsetDefinitionAlias
	{
		[XmlAttribute("lang")]
		public string lang;

		[XmlText]
		public string Value;
	}

	public class ClassName
	{
		[XmlText]
		public string Value;
	}

	public class PropertyDef
	{
		[XmlAttribute("ifdguid")]
		public string IfdGuid;

		public string Name;
		public string Definition;
		public PropertyType PropertyType;

		public List<NameAlias> NameAliases;
		public List<DefinitionAlias> DefinitionAliases;
	}

	public class ConstantDef
	{
		public string Name;
		public string Definition;
		public List<NameAlias> NameAliases;
		public List<DefinitionAlias> DefinitionAliases;
	}

	public class NameAlias
	{
		[XmlAttribute("lang")]
		public string lang;

		[XmlText]
		public string Value;
	}

	public class DefinitionAlias
	{
		[XmlAttribute("lang")]
		public string lang;

		[XmlText]
		public string Value;
	}

	public class PropertyType
	{
		public TypePropertySingleValue TypePropertySingleValue;
		public TypePropertyBoundedValue TypePropertyBoundedValue;
		public TypePropertyEnumeratedValue TypePropertyEnumeratedValue;
		public TypePropertyReferenceValue TypePropertyReferenceValue;
		public TypePropertyListValue TypePropertyListValue;
		public TypePropertyTableValue TypePropertyTableValue;
		public TypeComplexProperty TypeComplexProperty;
	}

	public class TypeComplexProperty
	{
		[XmlAttribute("name")]
		public string name;

		[XmlElement("PropertyDef", typeof(PropertyDef))]
		public List<PropertyDef> PropertyDefs;
	}

	public class TypeSimpleProperty
	{
	}

	public class TypePropertySingleValue : TypeSimpleProperty
	{
		public DataType DataType;
		public UnitType UnitType;
	}

	public class TypePropertyBoundedValue : TypeSimpleProperty
	{
		public DataType DataType;
		public UnitType UnitType;
	}

	public class TypePropertyEnumeratedValue : TypeSimpleProperty
	{
		public EnumList EnumList;
		public ConstantList ConstantList;
	}

	public class TypePropertyReferenceValue : TypeSimpleProperty
	{
		[XmlAttribute("reftype")]
		public string reftype;
		public DataType DataType;
		public UnitType UnitType;
	}

	public class TypePropertyListValue : TypeSimpleProperty
	{
		public ListValue ListValue;
	}

	public class TypePropertyTableValue : TypeSimpleProperty
	{
		public string Expression; // obsolete
		public DefiningValue DefiningValue;
		public DefinedValue DefinedValue;
	}

	public class DataType
	{
		[XmlAttribute("type")]
		public string type;
	}

	public class UnitType
	{
		[XmlAttribute("type")]
		public string type;
	}

	public class EnumList
	{
		[XmlAttribute("name")]
		public string name;

		[XmlElement("EnumItem", typeof(EnumItem))]
		public List<EnumItem> Items;
	}

	public class EnumItem
	{
		[XmlText]
		public string Value;
	}

	public class ConstantList
	{
		[XmlElement("ConstantDef", typeof(ConstantDef))]
		public List<ConstantDef> Items;
	}

	public class IfcVersion
	{
		[XmlAttribute("version")]
		public string version;
		public string projectcode;
		public string date;// was datetime
		public string author;
		public string domain;
		[XmlAttribute("schema")]
		public string schema;
	}

	public class DefiningValue
	{
		public DataType DataType;
		public UnitType UnitType;
		public List<ValueItem> Values;
	}

	public class DefinedValue
	{
		public DataType DataType;
		public UnitType UnitType;
		public List<ValueItem> Values;
	}

	public class ListValue
	{
		public DataType DataType;
		public UnitType UnitType;
		public List<ValueItem> Values;
	}

	public class ValueItem
	{
		[XmlText]
		public string Value;
	}

}
