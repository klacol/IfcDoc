// Name:        SchemaMvd.cs
// Description: MVD-XML schema
// Author:      Tim Chipman
// Origination: Work performed for BuildingSmart by Constructivity.com LLC.
// Copyright:   (c) 2010 BuildingSmart International Ltd.
// License:     http://www.buildingsmart-tech.org/legal

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Runtime.Serialization;

using IfcDoc.Format.XML;

namespace IfcDoc.Schema.MVD
{
	public static class SchemaMVD
	{
		static Dictionary<string, Type> s_types;

		public static Dictionary<string, Type> Types
		{
			get
			{
				if (s_types == null)
				{
					s_types = new Dictionary<string, Type>();

					Type[] types = typeof(SchemaMVD).Assembly.GetTypes();
					foreach (Type t in types)
					{
						if (typeof(SEntity).IsAssignableFrom(t) && !t.IsAbstract && t.Namespace.Equals("IfcDoc.Schema.MVD"))
						{
							string name = t.Name.ToUpper();
							s_types.Add(name, t);
						}
					}
				}

				return s_types;
			}
		}

		internal static void Load(DOC.DocProject docProject, string filename)
		{
			for (int iNS = 0; iNS < mvdXML.Namespaces.Length; iNS++)
			{
				string xmlns = mvdXML.Namespaces[iNS];
				mvdXML mvd = null;
				BuildingSmart.Serialization.Xml.XmlSerializer xmlSerializer = new BuildingSmart.Serialization.Xml.XmlSerializer(typeof(mvdXML));
				using (System.IO.FileStream streamSource = new System.IO.FileStream(filename, System.IO.FileMode.Open))
				{
					mvd = xmlSerializer.ReadObject(streamSource) as mvdXML;
				}
				if(mvd != null)
				{ 
					try
					{
						Program.ImportMvd(mvd, docProject, filename);
						break;
					}
					catch (InvalidOperationException xx)
					{
						// keep going until successful

						if (iNS == mvdXML.Namespaces.Length - 1)
						{
							throw new InvalidOperationException("The file is not of a supported format (mvdXML 1.0 or mvdXML 1.1).");
						}
					}
					catch (Exception xx)
					{
						xmlns = null;
					}
				}
			}
		}
	}

	public enum StatusEnum
	{
		[XmlEnum("sample")] Sample = 0, // default
		[XmlEnum("proposal")] Proposal = 1,
		[XmlEnum("draft")] Draft = 2,
		[XmlEnum("candidate")] Candidate = 3,
		[XmlEnum("final")] Final = 4,
		[XmlEnum("deprecated")] Deprecated = -1,
	}

	public abstract class Identity : SEntity
	{
		[DataMember(Order = 0), XmlAttribute("uuid")] public Guid Uuid { get; set; }
		[DataMember(Order = 1), XmlAttribute("name")] public string Name { get; set; }
		[DataMember(Order = 2), XmlAttribute("code")] public string Code { get; set; } // e.g. 'bsi-100'
		[DataMember(Order = 3), XmlAttribute("version")] public string Version { get; set; }
		[DataMember(Order = 4), XmlAttribute("status")] public StatusEnum Status { get; set; } // e.g. 'draft'
		[DataMember(Order = 5), XmlAttribute("author")] public string Author { get; set; }
		[DataMember(Order = 6), XmlAttribute("owner")] public string Owner { get; set; } // e.g. 'buildingSMART international'
		[DataMember(Order = 7), XmlAttribute("copyright")] public string Copyright { get; set; }

		public bool ShouldSerializeUuid()
		{
			return Uuid != Guid.Empty;
		}
		public bool ShouldSerializeStatus()
		{
			return Status != StatusEnum.Sample;
		}
	}

	public abstract class Element : Identity
	{
		[DataMember(Order = 0)] [XmlArray(Order = 0)] public List<Definition> Definitions { get; set; }

		public Element()
		{
			Definitions = new List<Definition>();
		}

		public bool ShouldSerializeDefinitions()
		{
			return null != this.Definitions && this.Definitions.Any();
		}
	}

	[XmlType("mvdXML")]
	public class mvdXML : Identity
	{
		[DataMember(Order = 0)] [XmlArray(Order = 0)] public List<ConceptTemplate> Templates { get; set; }
		[DataMember(Order = 1)] [XmlArray(Order = 1)] public List<ModelView> Views { get; set; }

		public mvdXML()
		{
			Templates = new List<ConceptTemplate>();
			Views = new List<ModelView>();
		}
		[XmlAttribute("schemaLocation", Namespace = "http://www.w3.org/2001/XMLSchema-instance")]
		public string schemaLocation = LocationV11;

		// namespaces in order of attempts to load
		public static readonly string[] Namespaces = new string[]
		{
			NamespaceV12,
			"http://buildingsmart-tech.org/mvdXML/mvdXML1-0",
			"http://buildingsmart-tech.org/mvdXML/mvdXML_V1-0",
			"http://buildingsmart-tech.org/mvdXML/mvdXML1-1",
			"http://buildingsmart-tech.org/mvd/XML/1.1"
		};

		public const string LocationV11 = "http://standards.buildingsmart.org/MVD/RELEASE/mvdXML/v1-1/mvdXML_V1.1.xsd";
		public const string LocationV12 = "http://www.buildingsmart-tech.org/mvd/XML/1.2 http://www.buildingsmart-tech.org/mvd/XML/1.2/mvdXML_V1.2.xsd";

		public const string NamespaceV11 = "http://buildingsmart-tech.org/mvd/XML/1.1";
		public const string NamespaceV12 = "http://buildingsmart-tech.org/mvd/XML/1.2";
	}

	[XmlType("ConceptTemplate")]
	public class ConceptTemplate : Element
	{
		[DataMember(Order = 0), XmlAttribute("applicableSchema")] public string ApplicableSchema { get; set; } = "IFC4";
		[DataMember(Order = 1), XmlAttribute("applicableEntity")] public string ApplicableEntity { get; set; }// was ApplicableEntities before final
		[DataMember(Order = 2)] [XmlArray(Order = 0)] public List<AttributeRule> Rules { get; set; }// new in 2.5
		[DataMember(Order = 3)] [XmlArray(Order = 1)] public List<ConceptTemplate> SubTemplates { get; set; }

		public ConceptTemplate()
		{
			Rules = new List<AttributeRule>();
			SubTemplates = new List<ConceptTemplate>();
		}

		public bool ShouldSerializeRules()
		{
			return null != this.Rules &&  this.Rules.Any();
		}
		public bool ShouldSerializeSubTemplates()
		{
			return null != this.SubTemplates && this.SubTemplates.Any();
		}
	}

	// used to map xpath
	[XmlType("Template")]
	public class TemplateRef : SEntity
	{
		[DataMember(Order = 0), XmlAttribute("ref")] public Guid Ref { get; set; }
	}

	[XmlType("BaseConcept")]
	public class BaseConcept : SEntity
	{
		[DataMember(Order = 0), XmlAttribute("ref")] public Guid Ref { get; set; }
	}

	[XmlType("Concept")]
	public class Concept : Element
	{
		[DataMember(Order = 0), XmlElement(Order = 0)] public TemplateRef Template { get; set; }// links to ConceptTemplate 
		[DataMember(Order = 1), XmlArray(Order = 1)] public List<ConceptRequirement> Requirements { get; set; }
		[DataMember(Order = 2), XmlElement(Order = 2)] public TemplateRules TemplateRules { get; set; }
		[DataMember(Order = 3), XmlArray(Order = 3)] public List<Concept> SubConcepts { get; set; }// added v3.8
		[DataMember(Order = 4), XmlAttribute("override")] public bool Override { get; set; }// added in v5.6
		[DataMember(Order = 5), XmlElement("baseConcept", Order = 4)] public BaseConcept BaseConcept { get; set; }
		
		public Concept()
		{
			Requirements = new List<ConceptRequirement>();
			SubConcepts = new List<Concept>();
		}
		public bool ShouldSerializeSubConcepts()
		{
			return null != this.SubConcepts && this.SubConcepts.Any();
		}
		public bool ShouldSerializeRequirements()
		{
			return null != this.Requirements && this.Requirements.Any();
		}
	}

	// used to map xpath
	[XmlType("Concept")]
	public class ConceptRef : SEntity
	{
		[DataMember(Order = 0), XmlAttribute("ref")] public Guid Ref { get; set; }
	}

	[XmlType("Requirement")]
	public class ConceptRequirement : SEntity
	{
		[DataMember(Order = 0), XmlAttribute("applicability")] public ApplicabilityEnum Applicability { get; set; }
		[DataMember(Order = 1), XmlAttribute("requirement")] public RequirementEnum Requirement { get; set; }
		[DataMember(Order = 2), XmlAttribute("exchangeRequirement")] public Guid ExchangeRequirement { get; set; }// Uuid
	}

	public enum ApplicabilityEnum
	{
		[XmlEnum("both")] Both = 0,
		[XmlEnum("export")] Export = 1,
		[XmlEnum("import")] Import = 2,
	}

	public enum RequirementEnum
	{
		[XmlEnum("mandatory")] Mandatory = 1,
		[XmlEnum("recommended")] Recommended = 2,
		[XmlEnum("not-relevant")] NotRelevant = 3,
		[XmlEnum("not-recommended")] NotRecommended = 4,
		[XmlEnum("excluded")] Excluded = 5,
	}

	[XmlType("ModelView")]
	public class ModelView : Element
	{
		[DataMember(Order = 0), XmlElement(Order = 0)] public String BaseView { get; set; }
		[DataMember(Order = 1), XmlArray(Order = 1)] public List<ExchangeRequirement> ExchangeRequirements { get; set; }
		[DataMember(Order = 2), XmlArray(Order = 2)] public List<ConceptRoot> Roots { get; set; }
		[DataMember(Order = 3), XmlIgnore] public List<ModelView> Views { get; set; }//Ignore mvdxml1.1
		[DataMember(Order = 4), XmlAttribute("applicableSchema")] public string ApplicableSchema { get; set; }

		public ModelView()
		{
			ExchangeRequirements = new List<ExchangeRequirement>();
			Roots = new List<ConceptRoot>();
			Views = new List<ModelView>();
		}
	}

	[XmlType("Definition")]
	public class Definition : SEntity
	{
		[DataMember(Order = 0), XmlElement("Body")] public Body Body { get; set; }
		[DataMember(Order = 1), XmlElement("Link")] public List<Link> Links { get; set; }
		[DataMember(Order = 2), XmlAttribute("tags")] public string Tags { get; set; }
		
		public Definition()
		{
			Links = new List<Link>();
		}
	}

	public class Body : SEntity,
		IXmlSerializable
	{
		[XmlIgnore] public string Content { get; set; }
		[DataMember(Order = 1), XmlAttribute("lang")] public string Lang { get; set; }
		[DataMember(Order = 2), XmlAttribute("tags")] public string Tags { get; set; }

		[XmlText]
		public string ContentCDATA
		{
			get
			{
				return new System.Xml.XmlDocument().CreateCDataSection(Content).ToString();
			}
			set
			{
				Content = value;
			}
		}
		#region IXmlSerializable Members

		public System.Xml.Schema.XmlSchema GetSchema()
		{
			return null;
		}

		public void ReadXml(System.Xml.XmlReader reader)
		{
			reader.ReadStartElement();
			this.Content = reader.ReadString();
			reader.ReadEndElement();
		}

		public void WriteXml(System.Xml.XmlWriter writer)
		{
			if (!String.IsNullOrEmpty(this.Lang))
			{
				writer.WriteAttributeString("lang", this.Lang);
			}
			if (!String.IsNullOrEmpty(this.Tags))
			{
				writer.WriteAttributeString("tags", this.Tags);
			}

			writer.WriteCData(this.Content);
		}

		#endregion
	}

	public class Link : SEntity
	{
		[DataMember(Order = 0), XmlAttribute("lang")] public string Lang { get; set; }
		[DataMember(Order = 1), XmlAttribute("category")] public CategoryEnum Category { get; set; }
		[DataMember(Order = 2), XmlAttribute("title")] public string Title { get; set; }
		[DataMember(Order = 3), XmlAttribute("href")] public string Href { get; set; }
		[DataMember(Order = 4), XmlIgnore] public string Content { get; set; }
	}

	public enum CategoryEnum
	{
		[XmlEnum("definition")] definition = 0,
		[XmlEnum("agreement")] agreement = 1,
		[XmlEnum("diagram")] diagram = 2,
		[XmlEnum("instantiation")] instantiation = 3,
		[XmlEnum("example")] example = 4,
	}

	[XmlType("ExchangeRequirement")]
	public class ExchangeRequirement : Element
	{
		[DataMember(Order = 0), XmlAttribute("applicability")] public ApplicabilityEnum Applicability { get; set; }
	}

	[XmlType("ConceptRoot")]
	public class ConceptRoot : Element
	{
		[DataMember(Order = 0), XmlElement(Order = 0)] public ApplicabilityRules Applicability { get; set; }
		[DataMember(Order = 1), XmlAttribute("applicableRootEntity")] public string ApplicableRootEntity { get; set; }// e.g. 'IfcBeam'
		[DataMember(Order = 2), XmlArray(Order = 1)] public List<Concept> Concepts { get; set; }// = new List<Concept>(); // really Concept but fixed according to sample data to get xml serializer working

		public ConceptRoot()
		{
			Concepts = new List<Concept>();
		}
	}

	[XmlType("ApplicabilityRules")]
	public class ApplicabilityRules : Element
	{
		[DataMember(Order = 0), XmlElement(Order = 0)] public TemplateRef Template { get; set; }
		[DataMember(Order = 1), XmlElement(Order = 1)] public TemplateRules TemplateRules { get; set; }
	}

	[XmlType("AbstractRule")]
	public abstract class AbstractRule : SEntity
	{
		[DataMember(Order = 0), XmlAttribute("RuleID")] public string RuleID { get; set; }
		[DataMember(Order = 1), XmlAttribute("Description")] public string Description { get; set; }
	}

	[XmlType("AttributeRule")]
	public class AttributeRule : AbstractRule
	{
		[DataMember(Order = 0), XmlAttribute("AttributeName")] public string AttributeName { get; set; }
		[DataMember(Order = 1), XmlArray(Order = 0)] public List<EntityRule> EntityRules { get; set; }
		[DataMember(Order = 2), XmlArray(Order = 1)] public List<Constraint> Constraints { get; set; }

		public AttributeRule()
		{
			EntityRules = new List<EntityRule>();
			Constraints = new List<Constraint>();
		}

		public bool ShouldSerializeEntityRules()
		{
			return null != this.EntityRules && this.EntityRules.Any();
		}
		public bool ShouldSerializeConstraints()
		{
			return null != this.Constraints && this.Constraints.Any();
		}
	}

	[XmlType("EntityRule")]
	public class EntityRule : AbstractRule
	{
		[DataMember(Order = 0), XmlAttribute("EntityName")] public string EntityName { get; set; }
		[DataMember(Order = 1)] public References References { get; set; }
		[DataMember(Order = 2)] public List<AttributeRule> AttributeRules { get; set; }
		[DataMember(Order = 3)] public List<Constraint> Constraints { get; set; }

		public EntityRule()
		{
			AttributeRules = new List<AttributeRule>();
			Constraints = new List<Constraint>();
		}

		public bool ShouldSerializeAttributeRules()
		{
			return null != this.AttributeRules && this.AttributeRules.Any();
		}
		public bool ShouldSerializeConstraints()
		{
			return null != this.Constraints && this.Constraints.Any();
		}
	}

	[XmlType("References")]
	public class References
	{
		[DataMember(Order = 0), XmlAttribute("IdPrefix")] public string IdPrefix { get; set; }
		[DataMember(Order = 1), XmlElement(typeof(TemplateRef))] public List<TemplateRef> Template { get; set; }// MVDXML 1.1 -- links to concept templates defined on referenced entity

		public References()
		{
			Template = new List<TemplateRef>();
		}
	}

	[XmlType("Constraint")]
	public class Constraint : SEntity
	{
		[DataMember(Order = 0), XmlAttribute("Expression")] public string Expression { get; set; }
	}

	[XmlType("TemplateRule"), XmlInclude(typeof(TemplateItem))]
	public class TemplateRule : AbstractRule
	{
		[DataMember(Order = 0), XmlAttribute("Parameters")] public string Parameters { get; set; }
	}

	[XmlType("TemplateItem")] // todo: update XSD
	public class TemplateItem : TemplateRule
	{
		//[DataMember(Order = 0), XmlAttribute("Order")] public int Order; // mvdXML 1.2
		//[DataMember(Order = 1), XmlAttribute("Usage")] public TemplateRuleUsage Usage; // mvdxml 1.2
		//[DataMember(Order = 2)] public List<ConceptRequirement> Requirements; // proposed for mvdxml 1.2
		[DataMember(Order = 0), XmlIgnore] public List<Concept> References { get; set; }// proposed for mvdxml 1.2

		public TemplateItem()
		{
			References = new List<Concept>();
		}
	}

	public enum TemplateRuleUsage
	{
		[XmlEnum("required")] Required = 0,       // COBie: yellow
		[XmlEnum("optional")] Optional = 1,       // COBie: green
		[XmlEnum("key")] Key = 2,                 // COBie: red
		[XmlEnum("reference")] Reference = 3,     // COBie: orange
		[XmlEnum("calculation")] Calculation = 4, // COBie: blue
		[XmlEnum("system")] System = 5,           // COBie: purple
	}

	[XmlType("TemplateRules")] // added in mvdXML 1.1d
	public class TemplateRules
	{
		[DataMember(Order = 0), XmlAttribute("operator")] public TemplateOperator Operator { get; set; }
		[DataMember(Order = 1), XmlElement(typeof(TemplateRule))] public List<TemplateRule> TemplateRule { get; set; }
		[DataMember(Order = 2), XmlElement(typeof(TemplateRules))] public TemplateRules InnerRules { get; set; }

		public TemplateRules()
		{
			TemplateRule = new List<TemplateRule>();
		}
	}

	public enum TemplateOperator // added in mvdXML 1.1d
	{
		[XmlEnum("and")] And = 0,
		[XmlEnum("or")] Or = 1,
		[XmlEnum("not")] Not = 2,
		[XmlEnum("nand")] Nand = 3,
		[XmlEnum("nor")] Nor = 4,
		[XmlEnum("xor")] Xor = 5,
		[XmlEnum("nxor")] Nxor = 6,
	}
}