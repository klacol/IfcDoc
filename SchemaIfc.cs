// Name:        SchemaIfc.cs
// Description: IFC partial schema for property set templates
// Author:      Tim Chipman
// Origination: Work performed for BuildingSmart by Constructivity.com LLC.
// Copyright:   (c) 2010 BuildingSmart International Ltd.
// License:     http://www.buildingsmart-tech.org/legal

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using System.Runtime.Serialization;

using IfcDoc.Schema;
// Contains subset of IFC schema used for property set templates

namespace IfcDoc.Schema.IFC
{
	public static class SchemaIfc
	{
		static Dictionary<string, Type> s_types;

		public static Dictionary<string, Type> Types
		{
			get
			{
				if (s_types == null)
				{
					s_types = new Dictionary<string, Type>();

					Type[] types = typeof(SchemaIfc).Assembly.GetTypes();
					foreach (Type t in types)
					{
						if (typeof(SEntity).IsAssignableFrom(t) && !t.IsAbstract && t.Namespace.Equals("IfcDoc.Schema.IFC"))
						{
							string name = t.Name.ToUpper();
							s_types.Add(name, t);
						}
					}
				}

				return s_types;
			}
		}
	}

	public abstract class IfcRoot : SEntity
	{
		[DataMember(Order = 0)] private string _GlobalId; // IfcGloballyUniqueId : simplify
		[DataMember(Order = 1)] private SEntity _OwnerHistory; // IfcOwnerHistory : exclude
		[DataMember(Order = 2)] private string _Name; // IfcLabel? : simplify
		[DataMember(Order = 3)] private string _Description; // IfcText? : simplify

		public IfcRoot()
		{
			this._GlobalId = SGuid.New().ToString();
			this._OwnerHistory = null;
		}

		[XmlAttribute("GlobalId")]
		public string GlobalId
		{
			get
			{
				return this._GlobalId;// SGuid.Parse(this._GlobalId);
			}
			set
			{
				this._GlobalId = value;// new SGuid(value).ToString();
			}
		}

		public SEntity OwnerHistory
		{
			get
			{
				return this._OwnerHistory;
			}
		}

		[XmlAttribute("Name")]
		public string Name
		{
			get
			{
				return this._Name;
			}
			set
			{
				this._Name = value;
			}
		}

		public string Description
		{
			get
			{
				return this._Description;
			}
			set
			{
				this._Description = value;
			}
		}
	}


	public abstract class IfcPropertyTemplateDefinition : IfcRoot
	{
		public List<IfcRelAssociatesLibrary> HasAssociations = new List<IfcRelAssociatesLibrary>();
	}

	[XmlType("IfcPropertySetTemplate")]
	public class IfcPropertySetTemplate : IfcPropertyTemplateDefinition // don't care about intermediate entities
	{
		[DataMember(Order = 0)] public IfcPropertySetTemplateTypeEnum? PredefinedType;
		[DataMember(Order = 1), XmlAttribute("ApplicableEntity")] public string ApplicableEntity;
		[DataMember(Order = 2)] public List<IfcPropertyTemplate> HasPropertyTemplates = new List<IfcPropertyTemplate>();
	}

	public enum IfcPropertySetTemplateTypeEnum
	{
		NOTDEFINED = 0, // regular property set
		PSET_TYPEDRIVENONLY = 1,
		PSET_TYPEDRIVENOVERRIDE = 2,
		PSET_OCCURRENCEDRIVEN = 3,
		PSET_PERFORMANCEDRIVEN = 4, // performance history

		QTO_TYPEDRIVENONLY = 5,
		QTO_TYPEDRIVENOVERRIDE = 6,
		QTO_OCCURRENCEDRIVEN = 7,
	}

	[
	XmlInclude(typeof(IfcSimplePropertyTemplate)),
	XmlInclude(typeof(IfcComplexPropertyTemplate))
	]
	public abstract class IfcPropertyTemplate : IfcPropertyTemplateDefinition
	{
	}

	[XmlType("IfcSimplePropertyTemplate")]
	public class IfcSimplePropertyTemplate : IfcPropertyTemplate
	{
		[DataMember(Order = 0)] public IfcSimplePropertyTemplateTypeEnum TemplateType;
		[DataMember(Order = 1), XmlAttribute("PrimaryMeasureType")] public string PrimaryMeasureType; // IfcIdentifier?
		[DataMember(Order = 2), XmlAttribute("SecondaryMeasureType")] public string SecondaryMeasureType; // IfcIdentifier?
		[DataMember(Order = 3)] public IfcPropertyEnumeration Enumerators;
		[DataMember(Order = 4)] public object PrimaryUnit; // 
		[DataMember(Order = 5)] public object SecondaryUnit; // 
		[DataMember(Order = 6)] public string Expression; // IfcText
		[DataMember(Order = 7), XmlAttribute("AccessState")] public IfcStateEnum AccessState; // 2x4 Beta 3
	}

	[XmlType("IfcComplexPropertyTemplate")]
	public class IfcComplexPropertyTemplate : IfcPropertyTemplate
	{
		[DataMember(Order = 0), XmlAttribute("UsageName")] public string UsageName;
		[DataMember(Order = 1)] public IfcComplexPropertyTemplateTypeEnum? TemplateType;
		[DataMember(Order = 2)] public List<IfcPropertyTemplate> HasPropertyTemplates = new List<IfcPropertyTemplate>();
	}

	public enum IfcComplexPropertyTemplateTypeEnum
	{
		P_COMPLEX = 1,
		Q_COMPLEX = 2,
	}

	public enum IfcSimplePropertyTemplateTypeEnum
	{
		P_SINGLEVALUE = 0,
		P_ENUMERATEDVALUE = 1,
		P_BOUNDEDVALUE = 2,
		P_LISTVALUE = 3,
		P_TABLEVALUE = 4,
		P_REFERENCEVALUE = 5,

		Q_LENGTH = 11,
		Q_AREA = 12,
		Q_VOLUME = 13,
		Q_COUNT = 14,
		Q_WEIGHT = 15,
		Q_TIME = 16,
	}

	public enum IfcStateEnum
	{
		READWRITE = 0,
		READONLY = 1,
		LOCKED = 2,
		READWRITELOCKED = 3,
		READONLYLOCKED = 4,
	}

	[XmlType("IfcPropertyEnumeration")]
	public class IfcPropertyEnumeration : SEntity
	{
		[DataMember(Order = 0), XmlAttribute("Name")] public string Name;
		[DataMember(Order = 1)] public List<IfcValue> EnumerationValues;
		[DataMember(Order = 2)] public object Unit;
	}

	public interface IfcValue
	{
	}

	public struct IfcLabel : IfcValue
	{
		public string Value;

		public override string ToString()
		{
			return this.Value;
		}
	}

	// entities for multilingual description

	[XmlType("IfcLibraryReference")]
	public class IfcLibraryReference : SEntity,
		IfcLibrarySelect
	{
		[DataMember(Order = 0), XmlAttribute("Location")] public string Location;
		[DataMember(Order = 1), XmlAttribute("Identification")] public string Identification;
		[DataMember(Order = 2), XmlAttribute("Name")] public string Name;
		[DataMember(Order = 3)] public string Description;
		[DataMember(Order = 4), XmlAttribute()] public string Language;
		[DataMember(Order = 5)] public IfcLibraryInformation ReferencedLibrary;
	}

	public class IfcLibraryInformation : SEntity,
		IfcLibrarySelect
	{
		[DataMember(Order = 0), XmlAttribute()] public string Name;
		[DataMember(Order = 1), XmlAttribute()] public string Version;
		[DataMember(Order = 2)] public IfcPersonAndOrganization Publisher; // IfcActorSelect; XML serializer doesn't support interfaces
		[DataMember(Order = 3), XmlAttribute()] public DateTime VersionDate;
		[DataMember(Order = 4), XmlAttribute()] public string Location;
		[DataMember(Order = 5)] public string Description;
	}

	public interface IfcActorSelect
	{
	}

	public class IfcPersonAndOrganization : SEntity
	{
		[DataMember(Order = 0)] public IfcPerson ThePerson;
		[DataMember(Order = 1)] public IfcOrganization TheOrganization;
		[DataMember(Order = 3)] public List<IfcActorRole> Roles;
	}

	public class IfcPerson : SEntity
	{
		// not yet used
	}

	public class IfcOrganization : SEntity
	{
		// not yet used
	}

	public class IfcActorRole : SEntity
	{
		// not yet used
	}

	public interface IfcLibrarySelect
	{
	}

	[XmlType("IfcRelAssociatesLibrary")]
	public class IfcRelAssociatesLibrary : IfcRoot
	{
		[DataMember(Order = 0), XmlIgnore] public List<IfcRoot> RelatedObjects = new List<IfcRoot>();
		[DataMember(Order = 1)] public IfcLibraryReference RelatingLibrary; // XML serializer doesn't support interfaces, so only support IfcLibraryReference for holding localizations
	}

	// entities for project structure

	[XmlType("IfcProject")]
	public class IfcProject : IfcRoot
	{
		[DataMember(Order = 0), XmlAttribute("ObjectType")] public string ObjectType;
		[DataMember(Order = 1), XmlAttribute("LongName")] public string LongName;
		[DataMember(Order = 2), XmlAttribute("Phase")] public string Phase;
		[DataMember(Order = 3)] public List<SEntity> RepresentationContexts; // omitted
		[DataMember(Order = 4)] public SEntity UnitAssignment; // omitted

		public List<IfcRelDeclares> Declares = new List<IfcRelDeclares>();
	}

	[XmlType("IfcRelDeclares")]
	public class IfcRelDeclares : IfcRoot
	{
		[DataMember(Order = 0), XmlIgnore] public IfcProject RelatingContext;
		[DataMember(Order = 1)] public List<IfcPropertySetTemplate> RelatedDefinitions = new List<IfcPropertySetTemplate>();
	}

}
