// Name:        SchemaDoc.cs
// Description: IFC documentation schema
// Author:      Tim Chipman
// Origination: Work performed for BuildingSmart by Constructivity.com LLC.
// Copyright:   (c) 2010 BuildingSmart International Ltd.
// License:     http://www.buildingsmart-tech.org/legal

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Text;

using System.Xml.Serialization;

using System.Text.RegularExpressions;
using BuildingSmart.Utilities.Conversion;
using BuildingSmart.Serialization;

using HtmlAgilityPack;

namespace IfcDoc.Schema.DOC
{
	public static class SchemaDOC
	{
		public static Type[] Types
		{
			get
			{
				List<Type> listTypes = new List<Type>();
				Type[] types = typeof(SchemaDOC).Assembly.GetTypes();
				foreach (Type t in types)
				{
					if (typeof(SEntity).IsAssignableFrom(t) && t.Namespace.Equals("IfcDoc.Schema.DOC"))
					{
						string name = t.Name.ToUpper();
						listTypes.Add(t);
					}
				}

				return listTypes.ToArray();
			}
		}
	}

	public interface IDocumentation
	{
		string Name
		{
			get;
			set;
		}

		string Documentation
		{
			get;
			set;
		}

		string DocumentationHtml();
		string DocumentationHtmlNoParagraphs();
	}

	/// <summary>
	/// Localization of a definition for a particular language and region, or an identifier on a remote system (e.g. bsDD)
	/// </summary>
	public class DocLocalization : SEntity,
		IDocumentation,
		IComparable
	{
		// language code, e.g. "en-US", "de-CH"; or blank if system identifier (e.g. bsDD)
		[DataMember(Order = 0)] [XmlAttribute] public string Locale { get; set; } 
		[DataMember(Order = 1)] [XmlAttribute] public DocCategoryEnum Category { get; set; }
		// localized name if locale provided, or system identifier (e.g. guid) used for bsDD
		[DataMember(Order = 2), XmlAttribute] public string Name { get; set; }
		[DataMember(Order = 3)] [XmlElement] [FileExtensions(Extensions = ".md"), DataType(DataType.MultilineText)] public string Documentation { get; set; }
		[DataMember(Order = 4)] [XmlAttribute] public string URL { get; set; } // URL of remote system, e.g. http://bsdd.buildingsmart.org or http://test.bsdd.buildingsmart.org

		public int CompareTo(object obj)
		{
			if (!(obj is DocLocalization))
				return -1;

			if (this.Locale == null) // used for bsDD guid mapping
				return -1;

			DocLocalization other = (DocLocalization)obj;
			return this.Locale.CompareTo(other.Locale);
		}

		public CustomAttributeBuilder ToCustomAttributeBuilder()
		{
			if (!String.IsNullOrEmpty(this.Locale))
			{
				ConstructorInfo conDisplay = typeof(DisplayAttribute).GetConstructor(new Type[] { });
				PropertyInfo propLcid = typeof(DisplayAttribute).GetProperty("ShortName"); // hack
				PropertyInfo propName = typeof(DisplayAttribute).GetProperty("Name");
				PropertyInfo propDesc = typeof(DisplayAttribute).GetProperty("Description");
				CustomAttributeBuilder cabDisplay = new CustomAttributeBuilder(conDisplay, new object[] { }, new PropertyInfo[]
						{
							propLcid,
							propName,
							propDesc,
						}, new object[]
						{
							this.Locale,
							this.Name,
							this.Documentation
						});
				return cabDisplay;
			}

			return null;
		}

		public DocLocalization() { }
		public DocLocalization(DocLocalization localization)
		{
			Locale = localization.Locale;
			Category = localization.Category;
			Name = localization.Name;
			Documentation = localization.Documentation;
			URL = localization.URL;
		}

		public string DocumentationHtmlNoParagraphs()
		{
			HtmlNode node = Format.HTM.FormatHTM.MarkdownToHtml(Documentation, true);
			if (node == null)
				return "";
			return node.OuterHtml;
		}
		public string DocumentationHtml()
		{
			HtmlNode node = Format.HTM.FormatHTM.MarkdownToHtml(Documentation, false);
			if (node == null)
				return "";
			return node.OuterHtml;
		}
	}

	public enum DocCategoryEnum
	{
		Definition = 0,
		Agreement = 1,
		Diagram = 2,
		Instantiation = 3,
		Example = 4,
	}

	public enum DocXsdFormatEnum
	{
		Default = IfcDoc.Schema.CNF.exp_attribute.unspecified,
		Hidden = IfcDoc.Schema.CNF.exp_attribute.no_tag,    // for direct attribute, don't include as inverse is defined instead
		Attribute = IfcDoc.Schema.CNF.exp_attribute.attribute_tag, // represent as attribute
		Element = IfcDoc.Schema.CNF.exp_attribute.double_tag,   // represent as element
		Content = IfcDoc.Schema.CNF.exp_attribute.attribute_content,   // represent as content
		Type = IfcDoc.Schema.CNF.exp_attribute.type_tag,
		Simple = IfcDoc.Schema.CNF.exp_attribute.no_tag_simple
	}

	/// <summary>
	/// Abstract base class for any documentation item, having identifier, html documentation, and version history
	/// </summary>
	public abstract class DocObject : SEntity,
		IDocumentation,
		IComparable
	{
		[DataMember(Order = 0)] [XmlAttribute] public string Name { get; set; } // the identifier (shows in tree)
		[DataMember(Order = 1)] [XmlElement] [FileExtensions(Extensions = ".md"), DataType(DataType.MultilineText)] public string Documentation { get; set; } // the documentation (synchronized with Visual Express)
		[DataMember(Order = 2)] [XmlAttribute] public string UniqueId { get; set; } = Guid.NewGuid().ToString(); // V1.8 inserted
		[DataMember(Order = 3)] [XmlAttribute] public string Code { get; set; } // V1.8 inserted // e.g. 'bsi-100' 
		[DataMember(Order = 4)] [XmlAttribute] public string Version { get; set; } // V1.8 inserted
		[DataMember(Order = 5)] [XmlAttribute] public string Status { get; set; } // V1.8 inserted // e.g. 'draft'
		[DataMember(Order = 6)] [XmlAttribute] public string Author { get; set; } // V1.8 inserted 
		[DataMember(Order = 7)] [XmlAttribute] public string Owner { get; set; } // V1.8 inserted // e.g. 'vg vghbuildingSMART international'
		[DataMember(Order = 8)] [XmlAttribute] public string Copyright { get; set; } // V1.8 inserted
		[DataMember(Order = 9)] [XmlArray] public List<DocLocalization> Localization { get; protected set; } // definitions

		public object Tag; // for holding UI state, e.g. tree node

		[IgnoreDataMember] public string id { get { return xmlid(); } }
		[IgnoreDataMember] public string _folderName { get { return folderName(); } }
		// v1.8: inserted fields Code, Version, Status, Author, Owner, Copyright to support MVD-XML

		protected DocObject()
		{
			this.UniqueId = Guid.NewGuid().ToString();
			this.Localization = new List<DocLocalization>();
		}

		public override string ToString()
		{
			return this.Name;
		}

		public void CopyFrom(DocObject source)
		{
			this.Name = source.Name;
			this.Documentation = source.Documentation;
			this.Uuid = source.Uuid;
			this.Code = source.Code;
			this.Version = source.Version;
			this.Status = source.Status;
			this.Author = source.Author;
			this.Owner = source.Owner;
			this.Copyright = source.Copyright;
			this.Localization.Clear();
			foreach (DocLocalization docLocalSource in source.Localization)
			{
				DocLocalization docLocalTarget = new DocLocalization();
				docLocalTarget.Locale = docLocalSource.Locale;
				docLocalTarget.Name = docLocalSource.Name;
				docLocalTarget.Documentation = docLocalSource.Documentation;
				docLocalTarget.Category = docLocalSource.Category;
				docLocalTarget.URL = docLocalSource.URL;
				this.Localization.Add(docLocalTarget);
			}
		}

		internal protected virtual void FindQuery(string query, bool searchtext, List<DocFindResult> results)
		{
			if (!searchtext && this.Name != null && this.Name.ToLower().Contains(query.ToLower()))
			{
				results.Add(new DocFindResult(this, null, -1, 0));
			}

			if (searchtext && this.Documentation != null && this.Documentation.ToLower().Contains(query.ToLower()))
			{
				// only find first result
				int offset = this.Documentation.IndexOf(query);
				results.Add(new DocFindResult(this, null, offset, query.Length));
			}
		}

		public DocLocalization GetLocalization(string locale)
		{
			DocLocalization docLocal = null;
			foreach (DocLocalization docEach in this.Localization)
			{
				if (docEach.Locale != null && docEach.Locale.Equals(locale, StringComparison.OrdinalIgnoreCase))
				{
					docLocal = docEach;
					break;
				}
			}

			return docLocal;
		}

		/// <summary>
		/// Creates or replaces a dictionary reference.
		/// </summary>
		/// <param name="url"></param>
		/// <param name="identifier"></param>
		/// <returns></returns>
		public DocLocalization RegisterDictionary(string url, string identifier)
		{
			// find existing
			DocLocalization docLocal = null;
			foreach (DocLocalization docEach in this.Localization)
			{
				if (docEach.Locale == null && docEach.URL == url && docEach.Name == identifier)
				{
					docLocal = docEach;
					break;
				}
			}

			if (docLocal == null)
			{
				docLocal = new DocLocalization();
				docLocal.URL = url;
				this.Localization.Add(docLocal);
				this.Localization.Sort();
			}

			docLocal.Name = identifier;

			return docLocal;
		}

		/// <summary>
		/// Creates or replaces a translation
		/// </summary>
		/// <param name="locale"></param>
		/// <param name="name"></param>
		/// <param name="desc"></param>
		/// <returns></returns>
		public DocLocalization RegisterLocalization(string locale, string name, string desc)
		{
			// find existing
			DocLocalization docLocal = null;
			foreach (DocLocalization docEach in this.Localization)
			{
				if (docEach.Locale != null && docEach.Locale.Equals(locale, StringComparison.OrdinalIgnoreCase))
				{
					docLocal = docEach;
					break;
				}
			}

			if (docLocal == null)
			{
				docLocal = new DocLocalization();
				docLocal.Locale = locale;
				this.Localization.Add(docLocal);
				this.Localization.Sort();
			}

			docLocal.Name = name;
			docLocal.Documentation = desc;

			return docLocal;
		}
		public void RegisterLocalization(DocLocalization localization)
		{
			for(int lcounter = this.Localization.Count-1; lcounter >= 0; lcounter--)
			{
				DocLocalization existing = this.Localization[lcounter];
				if (existing.Locale != null && existing.Locale.Equals(localization.Locale, StringComparison.OrdinalIgnoreCase))
					this.Localization.RemoveAt(lcounter);
			}
			this.Localization.Add(localization);
			this.Localization.Sort();
		}

		[Category("Misc")]
		public Guid Uuid
		{
			get
			{
				// don't generate guid from name, as it is often duplicated
				return GlobalId.Parse(this.UniqueId); // should be MS format, however there's mix of encodings in existing files
			}
			set
			{
				this.UniqueId = value.ToString();
			}
		}

		// GlobalId reduces chances of long path
		protected virtual string xmlid() { return (string.IsNullOrEmpty(Name) ? "" : Name + "_") + GlobalId.Format(Uuid); }
		protected virtual string folderName() { return (string.IsNullOrEmpty(Name) ? GlobalId.Format(Uuid) : Name);  }
		/*
        [Category("Misc")]
        public string Status
        {
            get
            {
                if (this._Status != null)
                {
                    return this._Status.TrimStart('_');
                }

                return null;
            }
            set
            {
                if (this._Status != null && this._Status.StartsWith("_"))
                {
                    this._Status = "_" + value;
                }
                else if(!String.IsNullOrEmpty(value))
                {
                    this._Status = value;
                }
                else
                {
                    this._Status = null;
                }
            }
        }
        */


		public int CompareTo(object obj)
		{
			if (!(obj is DocObject))
				return -1;

			DocObject that = (DocObject)obj;

			return string.Compare(this.UniqueId, that.UniqueId);
		}

		public override int GetHashCode()
		{
			return UniqueId.GetHashCode();
		}
		public bool IsDeprecated()
		{
			return this.Status != null && this.Status.Equals("Deprecated");
		}

		public class ComparerDocObject : IComparer<DocObject>
		{
			public int Compare(DocObject templateA, DocObject templateB)
			{
				return string.Compare(templateA.id, templateB.id);
			}

		}
		public string DocumentationHtmlNoParagraphs()
		{
			HtmlNode node = Format.HTM.FormatHTM.MarkdownToHtml(Documentation, true);
			if (node == null)
				return "";
			return node.OuterHtml;
		}
		public string DocumentationHtml()
		{
			HtmlNode node = Format.HTM.FormatHTM.MarkdownToHtml(Documentation, false);
			if (node == null)
				return "";
			return node.OuterHtml;
		}
	}

	public enum DocFormatSchemaEnum
	{
		//[DisplayName("XML")]
		[Description("IFC XSD long form schema")]
		XML = 1,

		//[DisplayName("STEP")]
		[Description("IFC EXPRESS long form schema")]
		STEP = 2,

		//[DisplayName("SQL")]
		[Description("Structured Query Language (SQL)")]
		SQL = 3,

		//[DisplayName("TTL")]
		[Description("ifcOWL Web Ontology Language (OWL)")]
		OWL = 4, // for transition, keep identifier as OWL for now so existing .ifcdoc files still work

		//[DisplayName("Java")]
		[Description("Java Programming Language")]
		JSON = 5,

		//[DisplayName("C#")]
		[Description("C# Programming Language")]
		CS = 6, // CShart
	}

	public enum DocFormatOptionEnum
	{
		None = 0,
		Schema = 1,
		Examples = 2,
		Markup = 3,
	}

#if false // future
    public enum DocFormatDataEnum
    {
        [Description("IFC-XML")]
        XML = 1,

        [Description("IFC-SPF")]
        SPF = 2,

        [Description("IFC-RDF")]
        RDF = 4,

        [Description("IFC-JSN")]
        JSON = 5,
    }

    public class DocFormatData : SEntity
    {
        [DataMember(Order = 0)] private DocFormatDataEnum _FormatType;

        public DocFormatData()
        {

        }

        public DocFormatData(DocFormatDataEnum type)
        {
            this._FormatType = type;
        }

        public DocFormatDataEnum FormatType
        {
            get
            {
                return this._FormatType;
            }
            set
            {
                this._FormatType = value;
            }
        }
    }
#endif

	/// <summary>
	/// Configuration settings for generating output format, such as CSharp, Express, 
	/// </summary>
	public class DocFormat : SEntity // new in IfcDoc 9.6
	{
		[DataMember(Order = 0)] [XmlAttribute] public DocFormatSchemaEnum FormatType { get; set; }
		[DataMember(Order = 1)] [XmlAttribute] public DocFormatOptionEnum FormatOptions { get; set; } /// V10.2 Deprecated

		public DocFormat()
		{

		}

		public DocFormat(DocFormatSchemaEnum type, DocFormatOptionEnum options)
		{
			this.FormatType = type;
			this.FormatOptions = options;
		}

		public string ExtensionSchema
		{
			get
			{
				switch (this.FormatType)
				{
					case DocFormatSchemaEnum.XML:
						return "xsd";

					case DocFormatSchemaEnum.STEP:
						return "exp";

					case DocFormatSchemaEnum.JSON:
						return "java";

					case DocFormatSchemaEnum.CS:
						return "cs";

					case DocFormatSchemaEnum.SQL:
						return "sql";

					case DocFormatSchemaEnum.OWL:
						return "owl";
				}
				return "txt"; // fallback if unknown
			}
		}

		public string ExtensionInstances
		{
			get
			{
				switch (this.FormatType)
				{
					case DocFormatSchemaEnum.XML:
						return "ifcxml";

					case DocFormatSchemaEnum.STEP:
						return "ifc";

					case DocFormatSchemaEnum.JSON:
						return "json";

					case DocFormatSchemaEnum.CS:
						return null;

					case DocFormatSchemaEnum.SQL:
						return "csv";

					case DocFormatSchemaEnum.OWL:
						return "ttl";

				}
				return "txt"; // fallback if unknown
			}
		}
	}

	/// <summary>
	/// Settings for an overall publication, which defines introductory pages and may include 0-many views
	/// </summary>
	public class DocPublication : DocObject
	{
		[DataMember(Order = 0)] [XmlArray] public List<DocModelView> Views { get; set; }
		[DataMember(Order = 1)] [XmlArray] public List<DocFormat> Formats { get; set; }
		[DataMember(Order = 2)] [XmlArray] public List<DocChangeSet> ChangeSets { get; set; } // IfcDoc 11.2; was List<string> locales
		[DataMember(Order = 3)] [XmlArray] public List<DocAnnotation> Annotations { get; set; } // Forward + Introduction
		[DataMember(Order = 4)] [XmlAttribute] public string Header { get; set; }
		[DataMember(Order = 5)] [XmlAttribute] public string Footer { get; set; }
		[DataMember(Order = 6)] [XmlAttribute] public bool HideHistory { get; set; } // hide version history
		[DataMember(Order = 7)] [XmlAttribute] public bool ISO { get; set; } // ISO format -- comply to strict formatting, such as using comments for EXPRESS language
		[DataMember(Order = 8)] [XmlAttribute] public bool UML { get; set; } // IfcDoc 9.8: UML diagrams instead of Express-G
		[DataMember(Order = 9)] [XmlAttribute] public bool Comparison { get; set; } // IfcDoc 9.9: compare mappings between tabular exchanges, e.g. GSA
		[DataMember(Order = 10)] [XmlAttribute] public bool Exchanges { get; set; } // IfcDoc 9.9: show exchange tables
		[DataMember(Order = 11)] [XmlAttribute] public bool HtmlExamples { get; set; } // IfcDoc 10.7: include examples with HTML markup
		[DataMember(Order = 12)] [XmlAttribute] public bool ReportIssues { get; set; } // IfcDoc 11.5: link to Jira database specific to each page

		// unserialized
		private List<string> m_errorlog; // list of filenames missing for images

		public DocPublication()
		{
			this.Views = new List<DocModelView>();
			this.Formats = new List<DocFormat>();
			this.ChangeSets = new List<DocChangeSet>();
			this.Annotations = new List<DocAnnotation>();

			this.Formats.Add(new DocFormat(DocFormatSchemaEnum.XML, DocFormatOptionEnum.Examples));
			this.Formats.Add(new DocFormat(DocFormatSchemaEnum.STEP, DocFormatOptionEnum.Examples));
			this.Formats.Add(new DocFormat(DocFormatSchemaEnum.SQL, DocFormatOptionEnum.None));
			this.Formats.Add(new DocFormat(DocFormatSchemaEnum.OWL, DocFormatOptionEnum.None));
			this.Formats.Add(new DocFormat(DocFormatSchemaEnum.JSON, DocFormatOptionEnum.None));
			this.Formats.Add(new DocFormat(DocFormatSchemaEnum.CS, DocFormatOptionEnum.None));
		}

		public List<string> ErrorLog
		{
			get
			{
				if (this.m_errorlog == null)
				{
					this.m_errorlog = new List<string>();
				}

				return this.m_errorlog;
			}
		}

		public DocFormatOptionEnum GetFormatOption(DocFormatSchemaEnum docFormatTypeEnum)
		{
			foreach (DocFormat docFormat in this.Formats)
			{
				if (docFormat.FormatType == docFormatTypeEnum)
				{
					return docFormat.FormatOptions;
				}
			}

			return DocFormatOptionEnum.None;
		}

		/// <summary>
		/// Returns the release identifier, which may be combined with the version identifier
		/// Example: "ADD1", "RC3"
		/// </summary>
		/// <returns></returns>
		public string GetReleaseIdentifier()
		{
			if (!String.IsNullOrEmpty(this.Code))
			{
				return this.Code;
			}

			return "UNSPECIFIEDRELEASE";
		}

	}

	/// <summary>
	/// The single root of the documentation having sections in order of ISO documentation
	/// </summary>
	public class DocProject : SEntity
	{ 
		[DataMember(Order = 0)] [XmlArray(Order = 5)] [XmlArrayItem(NestingLevel = 1)] public List<DocSection> Sections { get; protected set; }
		[DataMember(Order = 1)] [XmlArray(Order = 6)] public List<DocAnnex> Annexes { get; protected set; } // inserted in 1.2
		[DataMember(Order = 2)] [XmlArray(Order = 7)] [XmlArrayItem(NestingLevel = 1)] public List<DocTemplateDefinition> Templates { get; protected set; }
		[DataMember(Order = 3)] [XmlArray(Order = 8)] [XmlArrayItem(NestingLevel = 1)] public List<DocModelView> ModelViews { get; protected set; } // new in 2.7
		[DataMember(Order = 4)] [XmlArray(Order = 9)] [XmlArrayItem(NestingLevel = 1)] public List<DocChangeSet> ChangeSets { get; protected set; } // new in 2.7
		[DataMember(Order = 5)] [XmlArray(Order = 10)] [XmlArrayItem(NestingLevel = 1)] public List<DocExample> Examples { get; protected set; } // new in 4.2
		[DataMember(Order = 6)] [XmlArray(Order = 11)] public List<DocReference> NormativeReferences { get; protected set; } // new in 4.3
		[DataMember(Order = 7)] [XmlArray(Order = 12)] public List<DocReference> InformativeReferences { get; protected set; }// new in 4.3
		[DataMember(Order = 8)] [XmlArray(Order = 13)] public List<DocTerm> Terms { get; protected set; } // new in 4.3
		[DataMember(Order = 9)] [XmlArray(Order = 14)] public List<DocAbbreviation> Abbreviations { get; protected set; } // new in 4.3
		[DataMember(Order = 10)] [XmlArray(Order = 15)] public List<DocAnnotation> Annotations { get; protected set; } // new in 8.7: Cover | Foreword | Introduction; Deprecated in 9.6
		[DataMember(Order = 11)] [XmlArray(Order = 16)] [XmlArrayItem(NestingLevel = 1)] public List<DocPublication> Publications { get; protected set; } // new in 9.6
		[DataMember(Order = 12)] [XmlArray(Order = 0)] [XmlArrayItem(NestingLevel = 2, DataType ="_leaf_")] public List<DocConstant> Constants { get; protected set; } // 12.1 
		[DataMember(Order = 13)] [XmlArray(Order = 1)] [XmlArrayItem(NestingLevel = 2, DataType = "_leaf_")] public List<DocPropertyConstant> PropertyConstants { get; protected set; } // 12.1 
		[DataMember(Order = 14)] [XmlArray(Order = 2)] [XmlArrayItem(NestingLevel = 2, DataType = "_leaf_")] public List<DocPropertyEnumeration> PropertyEnumerations { get; protected set; } // 12.1
		[DataMember(Order = 15)] [XmlArray(Order = 3)] [XmlArrayItem(NestingLevel = 2)] public List<DocProperty> Properties { get; protected set; } // 12.1 
		[DataMember(Order = 16)] [XmlArray(Order = 4)] [XmlArrayItem(NestingLevel = 2)] public List<DocQuantity> Quantities { get; protected set; } // 12.1

		public DocProject()
		{
			this.Sections = new List<DocSection>();
			this.Annexes = new List<DocAnnex>();
			this.Templates = new List<DocTemplateDefinition>();
			this.ModelViews = new List<DocModelView>();
			this.ChangeSets = new List<DocChangeSet>();
			this.Examples = new List<DocExample>();
			this.NormativeReferences = new List<DocReference>();
			this.InformativeReferences = new List<DocReference>();
			this.Terms = new List<DocTerm>();
			this.Abbreviations = new List<DocAbbreviation>();
			this.Annotations = new List<DocAnnotation>();
			this.Publications = new List<DocPublication>();
			this.Constants = new List<DocConstant>();
			this.PropertyConstants = new List<DocPropertyConstant>();
			this.PropertyEnumerations = new List<DocPropertyEnumeration>();
			this.Properties = new List<DocProperty>();
			this.Quantities = new List<DocQuantity>();

			this.Sections.Add(new DocSection("Scope"));
			this.Sections.Add(new DocSection("Normative references"));
			this.Sections.Add(new DocSection("Terms, definitions, and abbreviated terms"));
			this.Sections.Add(new DocSection("Fundamental concepts and assumptions"));
			this.Sections.Add(new DocSection("Core data schemas"));
			this.Sections.Add(new DocSection("Shared element data schemas"));
			this.Sections.Add(new DocSection("Domain specific data schemas"));
			this.Sections.Add(new DocSection("Resource definition data schemas"));

			this.Annexes.Add(new DocAnnex("Computer interpretable listings"));
			this.Annexes.Add(new DocAnnex("Alphabetical listings"));
			this.Annexes.Add(new DocAnnex("Inheritance listings"));
			this.Annexes.Add(new DocAnnex("Diagrams"));
			this.Annexes.Add(new DocAnnex("Examples"));
			this.Annexes.Add(new DocAnnex("Change logs"));
		}

		/// <summary>
		/// Returns an identifier for the schema and major/minor version, applicable to the overall .ifcdoc file -- which is the same, independent of model view or publication (addendum, release candidate, etc.).
		/// Certain encodings -- such as EXPRESS -- will use this by itself and force it to uppercase.
		/// Other encodings -- such as XML or OWL -- will use this along with the release identifier using case as defined.
		/// Examples: "IFC2x3", "IFC4", "IFC4x1".
		/// It is stored as the Code of the first Section.
		/// </summary>
		/// <returns></returns>
		public string GetSchemaIdentifier()
		{
			if (this.Sections.Count > 0 && !String.IsNullOrEmpty(this.Sections[0].Code))
			{
				return this.Sections[0].Code;
			}

			return "UNSPECIFIEDSCHEMA";
		}

		/// <summary>
		/// Returns the schema version in form Major.Minor.Addendum.Corrigendum
		/// </summary>
		/// <returns></returns>
		public string GetSchemaVersion()
		{
			if (this.Sections.Count > 0 && !String.IsNullOrEmpty(this.Sections[0].Version))
			{
				return this.Sections[0].Version;
			}

			return null;
		}

		/// <summary>
		/// Returns a URI to the buildingsmart-tech.org website.
		/// </summary>
		/// <param name="docPub">Optional publication to qualify; if no publication defined, then uses the first in the list; if none, then uses "FINAL".</param>
		/// <returns></returns>
		public string GetSchemaURI(DocPublication docPub)
		{
			string release = "FINAL";
			if (docPub != null)
			{
				release = docPub.GetReleaseIdentifier();
			}
			else if (this.Publications.Count > 0)
			{
				release = this.Publications[0].GetReleaseIdentifier();
			}

			string draft = "";
			if (docPub != null && string.Compare(docPub.Status, "Official",true) != 0)
			{
				draft = "review/";
			}

			return "http://www.buildingsmart-tech.org/ifc/" + draft + this.GetSchemaIdentifier() + "/" + release.ToLower();

			// for now...temp hack...
			//return "http://www.buildingsmart-tech.org/ifc/IFC4/Add2TC1";
		}

		public DocTemplateDefinition GetTemplate(Guid guid)
		{
			foreach (DocTemplateDefinition docTemplate in this.Templates)
			{
				DocTemplateDefinition docEach = docTemplate.GetTemplate(guid);
				if (docEach != null)
				{
					return docEach;
				}
			}

			return null;
		}

		/// <summary>
		/// Returns an entity or type by name, or null if none
		/// </summary>
		/// <param name="def"></param>
		/// <returns></returns>
		public DocDefinition GetDefinition(string def)
		{
			if (def == null)
				return null;

			foreach (DocSection docSection in this.Sections)
			{
				foreach (DocSchema docSchema in docSection.Schemas)
				{
					foreach (DocType docType in docSchema.Types)
					{
						if (docType.Name != null && docType.Name.Equals(def))
							return docType;
					}

					foreach (DocEntity docType in docSchema.Entities)
					{
						if (docType.Name != null && docType.Name.Equals(def))
							return docType;
					}
				}
			}

			return null;
		}

		public DocFunction GetFunction(string def)
		{
			if (def == null)
				return null;

			foreach (DocSection docSection in this.Sections)
			{
				foreach (DocSchema docSchema in docSection.Schemas)
				{
					foreach (DocFunction docFunc in docSchema.Functions)
					{
						if (docFunc.Name != null && docFunc.Name.Equals(def))
							return docFunc;
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Returns property at highest level (most abstract) of given name. Used for resolving inherited properties (e.g. Pset_ElementCommon.Reference)
		/// </summary>
		/// <param name="def">Name of property</param>
		/// <param name="docApplicableEntity">Entity</param>
		/// <returns></returns>
		public DocProperty FindProperty(string def, DocEntity docApplicableEntity)
		{
			if (def == null || docApplicableEntity == null)
				return null;

			// try supertype first
			if (!String.IsNullOrEmpty(docApplicableEntity.BaseDefinition))
			{
				DocEntity docSuper = this.GetDefinition(docApplicableEntity.BaseDefinition) as DocEntity;
				if (docSuper != null)
				{
					DocProperty docProp = this.FindProperty(def, docSuper);
					if (docProp != null)
						return docProp;
				}
			}

			// find matching property sets
			foreach (DocSection docSection in this.Sections)
			{
				foreach (DocSchema docSchema in docSection.Schemas)
				{
					foreach (DocPropertySet docPset in docSchema.PropertySets)
					{
						if (docPset.IsVisible() && docPset.ApplicableType != null && docPset.ApplicableType.Equals(docApplicableEntity.Name))
						{
							// search properties
							foreach (DocProperty docProp in docPset.Properties)
							{
								if (def.Equals(docProp.Name))
								{
									return docProp;
								}
							}
						}
					}

				}
			}

			return null;
		}

		public DocPropertySet FindPropertySet(string def, out DocSchema schema)
		{
			foreach (DocSection docSection in this.Sections)
			{
				foreach (DocSchema docSchema in docSection.Schemas)
				{
					foreach (DocPropertySet docType in docSchema.PropertySets)
					{
						if (docType.Name != null && docType.Name.Equals(def))
						{
							schema = docSchema;
							return docType;
						}
					}
				}
			}

			schema = null;
			return null;
		}

		public DocPropertyEnumeration FindPropertyEnumeration(string def)
		{
			foreach (DocPropertyEnumeration docType in PropertyEnumerations)
			{
				if (docType.Name != null && docType.Name.Equals(def))
				{
					return docType;
				}
			}

			return null;
		}

		public DocQuantitySet FindQuantitySet(string def, out DocSchema schema)
		{
			foreach (DocSection docSection in this.Sections)
			{
				foreach (DocSchema docSchema in docSection.Schemas)
				{
					foreach (DocQuantitySet docType in docSchema.QuantitySets)
					{
						if (docType.Name != null && docType.Name.Equals(def))
						{
							schema = docSchema;
							return docType;
						}
					}
				}
			}

			schema = null;
			return null;
		}

		public DocSchema GetSchemaOfDefinition(DocDefinition def)
		{
			if (def is DocEntity)
			{
				DocEntity de = (DocEntity)def;
				foreach (DocSection docSection in this.Sections)
				{
					foreach (DocSchema docSchema in docSection.Schemas)
					{
						if (docSchema.Entities.Contains(de))
							return docSchema;
					}
				}
			}
			else if (def is DocType)
			{
				DocType dt = (DocType)def;
				foreach (DocSection docSection in this.Sections)
				{
					foreach (DocSchema docSchema in docSection.Schemas)
					{
						if (docSchema.Types.Contains(dt))
							return docSchema;
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Returns a flat list of all templates sorted by order in template hierarchy.
		/// Used for generating tables that preserve order of templates.
		/// </summary>
		/// <returns></returns>
		public List<DocTemplateDefinition> GetTemplateList()
		{
			List<DocTemplateDefinition> listTemplate = new List<DocTemplateDefinition>();
			BuildTemplateList(this.Templates, listTemplate);
			return listTemplate;
		}

		private static void BuildTemplateList(List<DocTemplateDefinition> source, List<DocTemplateDefinition> target)
		{
			foreach (DocTemplateDefinition docTemplate in source)
			{
				target.Add(docTemplate);
				BuildTemplateList(docTemplate.Templates, target);
			}
		}

		/// <summary>
		/// Returns a flat list of all entities sorted by order in schema hierarchy.
		/// Used for generating tables that preserve order of entities.
		/// </summary>
		/// <returns></returns>
		public List<DocEntity> GetEntityList()
		{
			List<DocEntity> listTemplate = new List<DocEntity>();
			foreach (DocSection docSection in this.Sections)
			{
				foreach (DocSchema docSchema in docSection.Schemas)
				{
					foreach (DocEntity docEntity in docSchema.Entities)
					{
						listTemplate.Add(docEntity);
					}
				}
			}
			return listTemplate;
		}

		public DocModelView GetView(Guid guid)
		{
			foreach (DocModelView docEach in this.ModelViews)
			{
				if (docEach.Uuid == guid)
				{
					return docEach;
				}
			}

			return null;
		}

		/// <summary>
		/// Buidls list of inherited views, in order starting from leaf view to base view
		/// </summary>
		/// <param name="docModelView"></param>
		/// <returns></returns>
		public DocModelView[] GetViewInheritance(DocModelView docModelView)
		{
			if (docModelView == null)
				return null;

			// build list of inherited views
			List<DocModelView> inheritviews = new List<DocModelView>();
			inheritviews.Add(docModelView);
			DocModelView docSuperView = docModelView;
			while (docSuperView != null && !String.IsNullOrEmpty(docSuperView.BaseView))
			{
				Guid guid = new Guid(docSuperView.BaseView);
				docSuperView = this.GetView(guid);
				if (docSuperView != null && !inheritviews.Contains(docSuperView))
				{
					inheritviews.Add(docSuperView);
				}
				else
				{
					break;
				}
			}
			DocModelView[] modelviews = inheritviews.ToArray();

			return modelviews;
		}


		/// <summary>
		/// Creates or returns existing schema of specified name, within particular section
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public DocSchema RegisterSchema(string name)
		{
			// map uppercase to mixed case (don't do in vex for now to preserve compatibility when merging)
			string[] schemas = new string[]
			{
				"IfcKernel",
				"IfcControlExtension",
				"IfcProcessExtension",
				"IfcProductExtension",

				"IfcSharedBldgElements",
				"IfcSharedBldgServiceElements",
				"IfcSharedComponentElements",
				"IfcSharedFacilitiesElements",
				"IfcSharedMgmtElements",

				"IfcArchitectureDomain",
				"IfcBuildingControlsDomain",
				"IfcConstructionMgmtDomain",
				"IfcElectricalDomain",
				"IfcHvacDomain",
				"IfcPlumbingFireProtectionDomain",
				"IfcStructuralAnalysisDomain",
				"IfcStructuralElementsDomain",

				"IfcActorResource",
				"IfcApprovalResource",
				"IfcConstraintResource",
				"IfcCostResource",
				"IfcDateTimeResource",
				"IfcExternalReferenceResource",
				"IfcGeometricConstraintResource",
				"IfcGeometricModelResource",
				"IfcGeometryResource",
				"IfcMaterialResource",
				"IfcMeasureResource",
				"IfcPresentationAppearanceResource",
				"IfcPresentationDefinitionResource",
				"IfcPresentationOrganizationResource",
				"IfcPresentationResource",
				"IfcProfileResource",
				"IfcPropertyResource",
				"IfcQuantityResource",
				"IfcRepresentationResource",
				"IfcStructuralLoadResource",
				"IfcTopologyResource",
				"IfcUtilityResource",
			};

			// normalize name
			foreach (string s in schemas)
			{
				if (s.Equals(name, StringComparison.OrdinalIgnoreCase))
				{
					name = s;
					break;
				}
			}

			// hard-coded categorization
			string sectionname;
			if (name.EndsWith("Resource", StringComparison.OrdinalIgnoreCase))
			{
				sectionname = "Resource definition data schemas";
			}
			else if (name.EndsWith("Domain", StringComparison.OrdinalIgnoreCase))
			{
				sectionname = "Domain specific data schemas";
			}
			else if (name.StartsWith("IfcShared", StringComparison.OrdinalIgnoreCase))
			{
				sectionname = "Shared element data schemas";
			}
			else
			{
				sectionname = "Core data schemas";
			}

			DocSchema docSchema = null;

			foreach (DocSection section in this.Sections)
			{
				if (sectionname.Equals(section.Name))
				{
					// if there is an existing schema of same name, replace it
					for (int i = section.Schemas.Count - 1; i >= 0; i--)
					{
						DocSchema existingschema = section.Schemas[i];
						if (existingschema.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
						{
							docSchema = existingschema;
							break;
						}
					}

					// create schema object if it doesn't already exist
					if (docSchema == null)
					{
						docSchema = new DocSchema();
						docSchema.Name = name;
						section.Schemas.Add(docSchema);

						// sort
						DocSchema kernel = null;
						SortedList<string, DocSchema> sortSchema = new SortedList<string, DocSchema>();
						foreach (DocSchema s in section.Schemas)
						{
							if (s.Name.Equals("IfcKernel", StringComparison.OrdinalIgnoreCase))
							{
								kernel = s;
							}
							else
							{
								sortSchema.Add(s.Name, s);
							}
						}
						section.Schemas.Clear();

						// special case for kernel; always comes first
						if (kernel != null)
						{
							section.Schemas.Add(kernel);
						}

						foreach (DocSchema s in sortSchema.Values)
						{
							section.Schemas.Add(s);
						}
					}

					break;
				}
			}

			return docSchema;
		}

		private bool RegisterSelect(Dictionary<DocObject,bool> included, DocSelect select)
		{
			foreach (DocSelectItem item in select.Selects)
			{
				DocDefinition docDefinition = this.GetDefinition(item.Name);
				DocEntity entity = docDefinition as DocEntity;
				if(entity != null)
					RegisterEntity(included, entity);
				else
				{
					DocSelect itemSelect = docDefinition as DocSelect;
					if (itemSelect != null)
						RegisterSelect(included, itemSelect);
				}
			}
			return true;
		}
		/// <summary>
		/// Registers any select objects that are already in scope
		/// </summary>
		/// <param name="included"></param>
		/// <param name="entity"></param>
		private bool RegisterSelects(Dictionary<DocObject, bool> included, DocDefinition entity)
		{
			bool outerchain = false;

			foreach (DocSection docSection in this.Sections)
			{
				foreach (DocSchema docSchema in docSection.Schemas)
				{
					foreach (DocType docType in docSchema.Types)
					{
						if (docType is DocSelect)
						{
							DocSelect docSelect = (DocSelect)docType;
							foreach (DocSelectItem docItem in docSelect.Selects)
							{
								if (docItem.Name == entity.Name)
								{
									if (!included.ContainsKey(docType))
									{
										// recurse
										bool innerchain = RegisterSelects(included, docType);

										if (innerchain)
										{
											included[docSelect] = true;
										}
									}

									outerchain = true;
									break;
								}
							}
						}
					}
				}
			}

			return outerchain;
		}

		private void RegisterDefined(Dictionary<DocObject, bool> included, DocDefined entity)
		{
			try
			{
				if (included.ContainsKey(entity))
					return;

				included[entity] = true;

				if (entity.DefinedType != null)
				{
					DocDefinition docBase = this.GetDefinition(entity.DefinedType);
					if (docBase is DocDefined)
					{
						RegisterDefined(included, (DocDefined)docBase);
					}
				}

				// register any selects that reference the defined type
				RegisterSelects(included, entity);
			}
			catch (Exception xx)
			{
				xx.ToString();
			}
		}

		private void RegisterFunction(Dictionary<DocObject, bool> included, DocFunction docFunc)
		{
			if (included.ContainsKey(docFunc))
				return;

			included[docFunc] = true;

			RegisterExpression(included, docFunc.Expression);
		}

		private void RegisterExpression(Dictionary<DocObject, bool> included, string escaped)
		{
			if (escaped == null)
				return;

			int iStart = -1;
			for (int i = 0; i < escaped.Length; i++)
			{
				char ch = escaped[i];
				if (Char.IsLetterOrDigit(ch))
				{
					if (iStart == -1)
					{
						iStart = i;
					}
				}
				else
				{
					if (iStart != -1)
					{
						// end: write buffer
						string identifier = escaped.Substring(iStart, i - iStart);
						DocFunction docFunc = this.GetFunction(identifier);
						if (docFunc != null)
						{
							RegisterFunction(included, docFunc);
						}
						iStart = -1;
					}
				}
			}
		}

		private void RegisterEntity(Dictionary<DocObject, bool> included, DocEntity entity)
		{
			try
			{
				if (included.ContainsKey(entity))
					return;

				included[entity] = true;

				if (entity.BaseDefinition != null)
				{
					DocDefinition docBase = this.GetDefinition(entity.BaseDefinition);
					if (docBase is DocEntity)
					{
						RegisterEntity(included, (DocEntity)docBase);
					}
				}

				// register any selects that reference the entity
				RegisterSelects(included, entity);

				// traverse attributes
				foreach (DocAttribute docAttribute in entity.Attributes)
				{
					DocDefinition docAttrType = this.GetDefinition(docAttribute.DefinedType);
					if (!docAttribute.IsOptional && docAttribute.Inverse == null) // new (4.0): only pull in mandatory attributes
					{
						included[docAttribute] = true;

						if (docAttrType is DocEntity)
						{
							DocEntity docRefEntity = (DocEntity)docAttrType;
							RegisterEntity(included, docRefEntity);
						}
						else if (docAttrType is DocType) // otherwise native EXPRESS type
						{
							included[docAttrType] = true;

							if (docAttrType is DocDefined)
							{
								DocDefined docDefined = (DocDefined)docAttrType;
								DocDefinition docDefRef = this.GetDefinition(docDefined.DefinedType);
								if (docDefRef != null)
								{
									included[docDefRef] = true;
								}
							}
							else
							{
								DocSelect select = docAttrType as DocSelect;
								if(select != null)
								{
									RegisterSelect(included, select);
								}
							}
						}
					}
				}
			}
			catch (Exception xx)
			{
				xx.ToString();
			}
		}

		private DocAttribute GetEntityAttribute(DocEntity entity, string attr)
		{
			foreach (DocAttribute docAttr in entity.Attributes)
			{
				if (docAttr.Name != null && docAttr.Name.Equals(attr))
				{
					return docAttr;
				}
			}

			if (entity.BaseDefinition != null)
			{
				DocEntity docBase = this.GetDefinition(entity.BaseDefinition) as DocEntity;
				if (docBase != null)
				{
					return GetEntityAttribute(docBase, attr);
				}
			}

			return null;
		}

		private void RegisterRule(Dictionary<DocObject, bool> included, DocEntity docEntity, DocModelRuleAttribute docRuleAttr, Dictionary<DocModelRuleAttribute, DocEntity> mapVirtualAttributes)
		{
			// cardinality of 0:0 indicates excluded
#if false
            if (docRuleAttr.CardinalityMin == -1 && docRuleAttr.CardinalityMax == -1)
            {
                // excluded
            }
            else
#endif
			{
				DocAttribute docAttr = GetEntityAttribute(docEntity, docRuleAttr.Name);
				if (docAttr != null)
				{
					included[docAttr] = true;

					DocDefinition docDeclared = this.GetDefinition(docAttr.DefinedType);
					if (docDeclared is DocEntity)
					{
						RegisterEntity(included, (DocEntity)docDeclared);
					}
					else if (docDeclared is DocSelect)
					{
						included[docDeclared] = true;
					}

					// include inverse attribute if any
					if (!String.IsNullOrEmpty(docAttr.Inverse))
					{
						DocEntity docEntInverse = GetDefinition(docAttr.DefinedType) as DocEntity;
						if (docEntInverse != null)
						{
							DocAttribute docAttrInverse = GetEntityAttribute(docEntInverse, docAttr.Inverse);
							if (docAttrInverse != null)
							{
								included[docAttrInverse] = true;
							}
						}
					}

					if (docRuleAttr.Rules != null)
					{
						foreach (DocModelRuleEntity docRuleEntity in docRuleAttr.Rules)
						{
							DocDefinition docInner = this.GetDefinition(docRuleEntity.Name);

							if (docInner != null)
							{
								if (docInner is DocEntity)
								{
									RegisterEntity(included, (DocEntity)docInner);
									if (docRuleEntity.Rules != null)
									{
										foreach (DocModelRule docRuleInner in docRuleEntity.Rules)
										{
											if (docRuleInner is DocModelRuleAttribute)
											{
												RegisterRule(included, (DocEntity)docInner, (DocModelRuleAttribute)docRuleInner, mapVirtualAttributes);
											}
										}
									}

									// if attribute type is different than specified type, also include attribute type
								}
								else if (docInner is DocDefined)
								{
									RegisterDefined(included, (DocDefined)docInner);
								}
								else
								{
									included[docInner] = true;
								}
							}

							foreach (DocTemplateDefinition docRef in docRuleEntity.References)
							{
								RegisterTemplate(included, docRef, mapVirtualAttributes);
							}
						}
					}
				}
				else
				{
					// may be defined on subtype, e.g. PredefinedType
					mapVirtualAttributes.Add(docRuleAttr, docEntity);
				}
			}
		}

		private void RegisterTemplate(Dictionary<DocObject, bool> included, DocTemplateDefinition template, Dictionary<DocModelRuleAttribute, DocEntity> mapVirtualAttributes)
		{
			if (included.ContainsKey(template))
				return;

			included[template] = true;

			if (template.Rules != null)
			{
				DocEntity docEnt = this.GetDefinition(template.Type) as DocEntity;
				if (docEnt != null)
				{
					RegisterEntity(included, docEnt);

					foreach (DocModelRuleAttribute docRuleAttr in template.Rules)
					{
						RegisterRule(included, docEnt, docRuleAttr, mapVirtualAttributes);
					}
				}
			}
		}

		private bool RegisterBaseTemplates(Dictionary<DocObject, bool> included, DocTemplateDefinition basetemplate, Dictionary<DocModelRuleAttribute, DocEntity> mapVirtualAttributes)
		{
			//if (included.ContainsKey(basetemplate))
			//    return true;

			if (basetemplate.Templates != null)
			{
				foreach (DocTemplateDefinition docTemplate in basetemplate.Templates)
				{
					bool check = RegisterBaseTemplates(included, docTemplate, mapVirtualAttributes);
					if (check)
					{
						RegisterTemplate(included, basetemplate, mapVirtualAttributes);
						//included[basetemplate] = true;
					}
				}
			}

			if (included.ContainsKey(basetemplate))
				return true;

			return false;
		}

		private static void RegisterExample(DocModelView docView, Dictionary<DocObject, bool> included, DocExample docExample)
		{
			if (docExample.Views.Contains(docView))
			{
				included[docExample] = true;
			}

			// register example if referenced from any sub-views
			foreach (DocModelView docSub in docView.ModelViews)
			{
				RegisterExample(docSub, included, docExample);
			}

			foreach (DocExample docSub in docExample.Examples)
			{
				RegisterExample(docView, included, docSub);
			}
		}

		public void RegisterObjectsInScope(DocModelView docView, Dictionary<DocObject, bool> included)
		{
			if (!included.ContainsKey(docView))
			{
				included[docView] = true;
			}

			// special case -- if view references self, then include everything (no filtering)
			if (docView.IncludeAllDefinitions)
			{
				foreach (DocSection docSection in this.Sections)
				{
					foreach (DocSchema docSchema in docSection.Schemas)
					{
						included[docSchema] = true;
						foreach (DocType docType in docSchema.Types)
						{
							included[docType] = true;
						}
						foreach (DocEntity docEntity in docSchema.Entities)
						{
							included[docEntity] = true;
							foreach (DocAttribute docAttr in docEntity.Attributes)
							{
								included[docAttr] = true;
							}
						}
						foreach (DocFunction docFunc in docSchema.Functions)
						{
							included[docFunc] = true;
						}
						foreach (DocGlobalRule docRule in docSchema.GlobalRules)
						{
							included[docRule] = true;
						}
						foreach (DocPropertySet docProp in docSchema.PropertySets)
						{
							included[docProp] = true;
							foreach (DocProperty docProperty in docProp.Properties)
								docProperty.RegisterObjectsInScope(included);
						}
						
						foreach (DocQuantitySet docQuan in docSchema.QuantitySets)
						{
							included[docQuan] = true;
						}
					}
				}
			}

			Guid guidBase;
			if (!String.IsNullOrEmpty(docView.BaseView) && Guid.TryParse(docView.BaseView, out guidBase))
			{
				// register base view
				DocModelView docViewBase = this.GetView(guidBase);
				if (docViewBase != null && docViewBase != docView)
				{
					RegisterObjectsInScope(docViewBase, included);
				}
			}

			if (this.Examples != null)
			{
				foreach (DocExample docExample in this.Examples)
				{
					RegisterExample(docView, included, docExample);
				}
			}

			// track referenced attributes that apply to subtypes, e.g. PredefinedType
			Dictionary<DocModelRuleAttribute, DocEntity> mapVirtualAttributes = new Dictionary<DocModelRuleAttribute, DocEntity>();

			foreach (DocConceptRoot docRoot in docView.ConceptRoots)
			{
				if (docRoot.ApplicableEntity != null)
				{
					RegisterEntity(included, docRoot.ApplicableEntity);

					foreach (DocTemplateUsage docUsage in docRoot.Concepts)
					{
						RegisterConcept(docUsage, included, mapVirtualAttributes, docRoot.ApplicableEntity);
					}
				}
			}

			// now include any templates that have subtemplate included
			foreach (DocTemplateDefinition docTemplate in this.Templates)
			{
				RegisterBaseTemplates(included, docTemplate, mapVirtualAttributes);
			}

			// now include any attributes on subtypes referenced by name (e.g. PredefinedType)
			foreach (DocModelRuleAttribute docVirtualRule in mapVirtualAttributes.Keys)
			{
				DocEntity docVirtualEntity = mapVirtualAttributes[docVirtualRule];

				foreach (DocSection docSection in this.Sections)
				{
					foreach (DocSchema docSchema in docSection.Schemas)
					{
						foreach (DocEntity docEntity in docSchema.Entities)
						{
							if (included.ContainsKey(docEntity))
							{
								foreach (DocAttribute docAttr in docEntity.Attributes)
								{
									if (docAttr.Name != null && docAttr.Name.Equals(docVirtualRule.Name))
									{
										// check if it inherits from entity (innermost check as it's expensive)
										bool inherits = false;
										DocEntity docSuper = docEntity;
										while (docSuper != null && !String.IsNullOrEmpty(docSuper.BaseDefinition))
										{
											docSuper = this.GetDefinition(docSuper.BaseDefinition) as DocEntity;
											if (docSuper == docVirtualEntity)
											{
												inherits = true;
												break;
											}
										}

										if (inherits)
										{
											included[docAttr] = true;

											DocDefinition docAttrType = this.GetDefinition(docAttr.DefinedType);
											if (docAttrType != null)
											{
												included[docAttrType] = true;
												if (docAttrType is DocEntity)
												{
													RegisterEntity(included, (DocEntity)docAttrType);
												}
												else if (docAttrType is DocDefined)
												{
													RegisterDefined(included, (DocDefined)docAttrType);
												}
											}
										}
										break;
									}
								}

								foreach (DocWhereRule docWhere in docEntity.WhereRules)
								{
									// extract function references from expression ...docWhere.Expression
									RegisterExpression(included, docWhere.Expression);
								}
							}
						}
					}
				}
			}

			// now include any schemas that have entities or types included
			foreach (DocSection docSection in this.Sections)
			{
				foreach (DocSchema docSchema in docSection.Schemas)
				{
					foreach (DocEntity docEntity in docSchema.Entities)
					{
						if (included.ContainsKey(docEntity))
						{
							included[docSchema] = true; ;
							break;
						}
					}

					foreach (DocType docType in docSchema.Types)
					{
						if (included.ContainsKey(docType))
						{
							included[docSchema] = true; ;
							break;
						}
					}

					foreach (DocPropertySet docPset in docSchema.PropertySets)
					{
						if (included.ContainsKey(docPset))
						{
							included[docSchema] = true;

							// include any types used for properties
							foreach (DocProperty docProp in docPset.Properties)
							{
								string propclass = docProp.GetEntityName();
								DocEntity docPropType = this.GetDefinition(propclass) as DocEntity;
								if (docPropType != null)
								{
									RegisterEntity(included, docPropType);
								}

								DocDefinition docPropData = this.GetDefinition(docProp.PrimaryDataType) as DocDefinition;
								if (docPropData != null)
								{
									if (docPropData is DocEntity)
									{
										RegisterEntity(included, (DocEntity)docPropData);
									}
									else
									{
										included[docPropData] = true;
									}
								}
								if (docProp.PropertyType == DocPropertyTemplateTypeEnum.P_ENUMERATEDVALUE)
								{
									docProp.RegisterObjectsInScope(included);
								}
								else if (!String.IsNullOrEmpty(docProp.SecondaryDataType))
								{
									DocDefinition docPropTime = this.GetDefinition(docProp.SecondaryDataType) as DocDefinition;
									if (docPropTime != null)
									{
										if (docPropTime is DocEntity)
										{
											RegisterEntity(included, (DocEntity)docPropTime);
										}
										else
										{
											included[docPropTime] = true;
										}
									}
								}
							}
						}
					}

					foreach (DocQuantitySet docQset in docSchema.QuantitySets)
					{
						if (included.ContainsKey(docQset))
						{
							included[docSchema] = true;

							// include any types used for quantities
							foreach (DocQuantity docProp in docQset.Quantities)
							{
								string propclass = docProp.GetEntityName();

								DocEntity docPropType = this.GetDefinition(propclass) as DocEntity;
								if (docPropType != null)
								{
									RegisterEntity(included, docPropType);
								}
							}
						}
					}
				}
			}

			// for now, also include rules
			foreach (DocSection docSection in this.Sections)
			{
				foreach (DocSchema docSchema in docSection.Schemas)
				{
					if (included.ContainsKey(docSchema))
					{
						foreach (DocGlobalRule docRule in docSchema.GlobalRules)
						{
							included[docRule] = true;
						}
					}
				}
			}

			// register sub-views
			foreach (DocModelView docSub in docView.ModelViews)
			{
				RegisterObjectsInScope(docSub, included);
			}
		}

		private void RegisterConcept(DocTemplateUsage docUsage, Dictionary<DocObject, bool> included, Dictionary<DocModelRuleAttribute, DocEntity> mapVirtualAttributes, DocEntity applicableEntity)
		{
			if (docUsage.Definition != null)
			{
				RegisterTemplate(included, docUsage.Definition, mapVirtualAttributes);

				// include types referenced at concepts
				string[] parameters = docUsage.Definition.GetParameterNames();
				foreach (DocTemplateItem docItem in docUsage.Items)
				{
					foreach (string param in parameters)
					{
						string val = docItem.GetParameterValue(param);
						if (val != null && (val.StartsWith("Ifc") || val.Contains("_")))//val.StartsWith("Pset_") || val.StartsWith("Qto_"))) // perf shortcut
						{
							DocDefinition docRef = this.GetDefinition(val);
							if (docRef is DocEntity)
							{
								RegisterEntity(included, (DocEntity)docRef);
							}
							else if (docRef is DocDefined)
							{
								RegisterDefined(included, (DocDefined)docRef);
							}
							else if (docRef != null)
							{
								included[docRef] = true;
							}
							else
							{
								DocSchema docSchemaRef = null;
								DocPropertySet docPset = FindPropertySet(val, out docSchemaRef);
								if (docPset != null)
								{
									if (!included.ContainsKey(docPset))
									{
										included[docPset] = true;

										// include any referenced enumerations
										foreach (DocProperty docProp in docPset.Properties)
										{
											if (docProp.PropertyType == DocPropertyTemplateTypeEnum.P_ENUMERATEDVALUE && docProp.PrimaryDataType != null)
											{
												DocPropertyEnumeration docEnum = FindPropertyEnumeration(docProp.PrimaryDataType);
												if (docEnum != null)
												{
													included[docEnum] = true;
												}
											}
										}
									}
								}
								else
								{
									DocQuantitySet docQset = FindQuantitySet(val, out docSchemaRef);
									if (docQset != null)
									{
										included[docQset] = true;
									}
								}
							}
						}
						else
						{
							DocModelRule[] paramRules = docUsage.Definition.GetParameterRules();
							string parameterName = "";

							for (int i = 0; i < paramRules.Length - 1; i++)
							{
								if (paramRules[i].Identification == param)
								{
									parameterName = paramRules[i].Name;
								}
							}
							DocAttribute attributeParameter = GetEntityAttribute(applicableEntity, parameterName);
							if (attributeParameter != null)
							{
								if (!included.ContainsKey(attributeParameter)) included[attributeParameter] = true;

								DocDefinition docAttrType = this.GetDefinition(attributeParameter.DefinedType);

								if (docAttrType is DocEntity)
								{
									DocEntity docRefEntity = (DocEntity)docAttrType;
									RegisterEntity(included, docRefEntity);
								}
								else if (docAttrType is DocType) // otherwise native EXPRESS type
								{
									included[docAttrType] = true;

									if (docAttrType is DocDefined)
									{
										DocDefined docDefined = (DocDefined)docAttrType;
										DocDefinition docDefRef = this.GetDefinition(docDefined.DefinedType);
										if (docDefRef != null)
										{
											included[docDefRef] = true;
										}
									}
								}
							}
						}
					}

					// recurse
					foreach (DocTemplateUsage docSub in docItem.Concepts)
					{
						RegisterConcept(docSub, included, mapVirtualAttributes, applicableEntity);
					}

				}
			}

			// recurse through nested concepts
			foreach (DocTemplateUsage docChild in docUsage.Concepts)
			{
				RegisterConcept(docChild, included, mapVirtualAttributes, applicableEntity);
			}
		}

		/// <summary>
		/// Finds all occurrences of text within project.
		/// </summary>
		/// <param name="query">The text to query.</param>
		/// <param name="searchtext">Whether to search within descriptions or just names</param>
		/// <returns></returns>
		public List<DocFindResult> Find(string query, bool searchtext)
		{
			List<DocFindResult> results = new List<DocFindResult>();
			foreach (DocSection docSection in this.Sections)
			{
				docSection.FindQuery(query, searchtext, results);

				if (docSection == this.Sections[0])
				{
					foreach (DocTemplateDefinition docTemp in this.Templates)
					{
						docTemp.FindQuery(query, searchtext, results);
					}
				}
				else if (docSection == this.Sections[1])
				{
					// norm refs
					foreach (DocReference docRef in this.NormativeReferences)
					{
						docRef.FindQuery(query, searchtext, results);
					}
				}
				else if (docSection == this.Sections[2])
				{
					// terms
					foreach (DocTerm docTerm in this.Terms)
					{
						docTerm.FindQuery(query, searchtext, results);
					}
					foreach (DocAbbreviation docAbbr in this.Abbreviations)
					{
						docAbbr.FindQuery(query, searchtext, results);
					}
				}
				else if (docSection == this.Sections[3])
				{
					foreach (DocModelView docView in this.ModelViews)
					{
						docView.FindQuery(query, searchtext, results);
						foreach (DocExchangeDefinition docExchange in docView.Exchanges)
						{
							docExchange.FindQuery(query, searchtext, results);
						}

						foreach (DocConceptRoot docRoot in docView.ConceptRoots)
						{
							docRoot.FindQuery(query, searchtext, results);
							foreach (DocTemplateUsage docConc in docRoot.Concepts)
							{
								docConc.FindQuery(query, searchtext, results);
							}
						}
					}
				}
				else
				{
					foreach (DocSchema docSchema in docSection.Schemas)
					{
						docSchema.FindQuery(query, searchtext, results);
						foreach (DocType docType in docSchema.Types)
						{
							docType.FindQuery(query, searchtext, results);
							if (docType is DocEnumeration)
							{
								DocEnumeration docEnum = (DocEnumeration)docType;
								foreach (DocConstant docConst in docEnum.Constants)
								{
									docConst.FindQuery(query, searchtext, results);
								}
							}
						}
						foreach (DocEntity docEntity in docSchema.Entities)
						{
							docEntity.FindQuery(query, searchtext, results);
							foreach (DocAttribute docAttr in docEntity.Attributes)
							{
								docAttr.FindQuery(query, searchtext, results);
							}
						}
						foreach (DocFunction docFunc in docSchema.Functions)
						{
							docFunc.FindQuery(query, searchtext, results);
						}
						foreach (DocGlobalRule docGlob in docSchema.GlobalRules)
						{
							docGlob.FindQuery(query, searchtext, results);
						}
						foreach (DocPropertySet docPset in docSchema.PropertySets)
						{
							docPset.FindQuery(query, searchtext, results);
							foreach (DocProperty docProp in docPset.Properties)
							{
								docProp.FindQuery(query, searchtext, results);
							}
						}
						foreach (DocQuantitySet docPset in docSchema.QuantitySets)
						{
							docPset.FindQuery(query, searchtext, results);
							foreach (DocQuantity docProp in docPset.Quantities)
							{
								docProp.FindQuery(query, searchtext, results);
							}
						}
					}
				}
			}
			foreach (DocAnnex docSection in this.Annexes)
			{
				docSection.FindQuery(query, searchtext, results);

				if (docSection == this.Annexes[4])
				{
					foreach (DocExample docExample in this.Examples)
					{
						docExample.FindQuery(query, searchtext, results);
					}
				}
			}

			foreach (DocPropertyEnumeration docPset in PropertyEnumerations)
			{
				docPset.FindQuery(query, searchtext, results);
				foreach (DocPropertyConstant docProp in docPset.Constants)
				{
					docProp.FindQuery(query, searchtext, results);
				}
			}
			return results;
		}

		public List<DocXsdFormat> BuildXsdFormatList()
		{
			List<DocXsdFormat> xsdFormatBase = new List<DocXsdFormat>();
			foreach (DocSection docSection in this.Sections)
			{
				foreach (DocSchema docSchema in docSection.Schemas)
				{
					foreach (DocEntity docEntity in docSchema.Entities)
					{
						foreach (DocAttribute docAttr in docEntity.Attributes)
						{
							if (docAttr.XsdFormat != DocXsdFormatEnum.Default || docAttr.XsdTagless != null)
							{
								DocXsdFormat xsdformat = new DocXsdFormat();
								xsdformat.Entity = docEntity.Name;
								xsdformat.Attribute = docAttr.Name;
								xsdformat.XsdFormat = docAttr.XsdFormat;
								xsdformat.XsdTagless = docAttr.XsdTagless;
								xsdFormatBase.Add(xsdformat);
							}
						}
					}
				}
			}

			return xsdFormatBase;
		}

		/// <summary>
		/// Renames an object and updates all dependencies
		/// </summary>
		/// <param name="docSchema">The schema containing definition, or to be renamed.</param>
		/// <param name="docDefinition">Optional definition containing attribute, or to be renamed (if NULL, then schema is renamed)</param>
		/// <param name="docAttribute">Optional attribute to rename (if NULL, then definition is renamed)</param>
		/// <param name="newname">New name of object</param>
		public void Rename(DocSchema docSchema, DocDefinition docDefinition, DocAttribute docAttribute, string newname)
		{
			// for now, this assumes definition names are unique globally (not just within schema)

			if (docDefinition != null)
			{
				foreach (DocTemplateDefinition docTemplate in this.Templates)
				{
					docTemplate.Rename(docSchema, docDefinition, docAttribute, newname);
				}
			}

			foreach (DocSection docSection in this.Sections)
			{
				foreach (DocSchema docSectionSchema in docSection.Schemas)
				{
					foreach (DocSchemaRef docSchemaRef in docSectionSchema.SchemaRefs)
					{
						if (docSchemaRef.Name.Equals(docSchema.Name))
						{
							if (docDefinition == null)
							{
								docSchemaRef.Name = newname;
								break;
							}
							else
							{
								foreach (DocDefinitionRef docDefRef in docSchemaRef.Definitions)
								{
									if (docAttribute == null && docDefRef.Name.Equals(docDefinition.Name))
									{
										docDefRef.Name = newname;
										break;
									}
								}
							}
						}
					}
				}
			}


			//... rename schema...

			if (docAttribute != null)
			{
				docAttribute.Name = newname;
			}
			else if (docDefinition != null)
			{
				docDefinition.Name = newname;
			}
			else if (docSchema != null)
			{
				docSchema.Name = newname;
			}
		}

		public void SortProject()
		{
			SortSections();
			SortAnnexes();
			DocObject.ComparerDocObject comparer = new DocObject.ComparerDocObject();
			Templates.Sort(comparer);
			foreach(DocTemplateDefinition template in Templates)
			{
				template.SortTemplate();
			}
			ModelViews.Sort(comparer);
			foreach(DocModelView mv in ModelViews)
			{
				mv.SortModelView();
			}
			Examples.Sort(comparer);
			foreach (DocExample example in Examples)
				example.SortExample();
			SortNormativeReferences();
			SortInformativeReferences();
			SortTerms();
			SortAbbreviations();
			Constants.Sort(comparer);
			PropertyConstants.Sort(comparer);
			PropertyEnumerations.Sort(comparer);
			Properties.Sort(comparer);
			Quantities.Sort(comparer);
		}
		public void SortSections()
		{
			DocSection scope = null, normRefs = null, terms = null, fundamentals = null;
			DocSection core = null, shared = null, domain = null, resource = null;
			
			List<DocSection> notSorted = new List<DocSection>();
			foreach (DocSection docSection in this.Sections)
			{
				if (string.Compare(docSection.Name, "Scope", true) == 0)
					scope = docSection;
				else if (string.Compare(docSection.Name, "Normative references", true) == 0)
					normRefs = docSection;
				else if (string.Compare(docSection.Name, "Terms, definitions, and abbreviated terms", true) == 0)
					terms = docSection;
				else if (string.Compare(docSection.Name, "Fundamental concepts and assumptions", true) == 0)
					fundamentals = docSection;
				else if (string.Compare(docSection.Name, "Core data schemas", true) == 0)
					core = docSection;
				else if (string.Compare(docSection.Name, "Shared element data schemas", true) == 0)
					shared = docSection;
				else if (string.Compare(docSection.Name, "Domain specific data schemas", true) == 0)
					domain = docSection;
				else if (string.Compare(docSection.Name, "Resource definition data schemas", true) == 0)
					resource = docSection;
				else
					notSorted.Add(docSection);
				docSection.SortSection();
			}

			this.Sections.Clear();
			this.Sections.Add(scope);
			this.Sections.Add(normRefs);
			this.Sections.Add(terms);
			this.Sections.Add(fundamentals);
			this.Sections.Add(core);
			this.Sections.Add(shared);
			this.Sections.Add(domain);
			this.Sections.Add(resource);

			this.Sections.AddRange(notSorted);
		}
		public void SortAnnexes()
		{
			DocAnnex computer = null, alphabetical = null, inheritance = null;
			DocAnnex diagrams = null, examples = null, changeLogs = null;

			List<DocAnnex> notSorted = new List<DocAnnex>();
			foreach (DocAnnex docAnnex in this.Annexes)
			{
				if (docAnnex == null)
					continue;
				if (string.Compare(docAnnex.Name, "Computer interpretable listings", true) == 0)
					computer = docAnnex;
				else if (string.Compare(docAnnex.Name, "Alphabetical listings", true) == 0)
					alphabetical = docAnnex;
				else if (string.Compare(docAnnex.Name, "Inheritance listings", true) == 0)
					inheritance = docAnnex;
				else if (string.Compare(docAnnex.Name, "Diagrams", true) == 0)
					diagrams = docAnnex;
				else if (string.Compare(docAnnex.Name, "Examples", true) == 0)
					examples = docAnnex;
				else if (string.Compare(docAnnex.Name, "Change logs", true) == 0)
					changeLogs = docAnnex;
				else
					notSorted.Add(docAnnex);
			}

			this.Annexes.Clear();
			this.Annexes.Add(computer);
			this.Annexes.Add(alphabetical);
			this.Annexes.Add(inheritance);
			this.Annexes.Add(diagrams);
			this.Annexes.Add(examples);
			this.Annexes.Add(changeLogs);

			this.Annexes.AddRange(notSorted);
		}

		public void SortTerms()
		{
			SortedList<string, DocTerm> sortEntity = new SortedList<string, DocTerm>();

			foreach (DocTerm docType in this.Terms)
			{
				sortEntity.Add(docType.Name, docType);
			}

			this.Terms.Clear();
			this.Terms.AddRange(sortEntity.Values);
		}

		public void SortAbbreviations()
		{
			SortedList<string, DocAbbreviation> sortEntity = new SortedList<string, DocAbbreviation>();

			foreach (DocAbbreviation docType in this.Abbreviations)
			{
				sortEntity.Add(docType.Name, docType);
			}

			this.Abbreviations.Clear();
			this.Abbreviations.AddRange(sortEntity.Values);
		}

		public void SortNormativeReferences()
		{
			SortedList<string, DocReference> sortEntity = new SortedList<string, DocReference>();

			foreach (DocReference docType in this.NormativeReferences)
			{
				sortEntity.Add(docType.Name, docType);
			}

			this.NormativeReferences.Clear();
			this.NormativeReferences.AddRange(sortEntity.Values);
		}

		public void SortInformativeReferences()
		{
			SortedList<string, DocReference> sortEntity = new SortedList<string, DocReference>();

			foreach (DocReference docType in this.InformativeReferences)
			{
				sortEntity.Add(docType.Name, docType);
			}

			this.InformativeReferences.Clear();
			this.InformativeReferences.AddRange(sortEntity.Values);
		}

		public void SortPropertyEnums()
		{
			SortedList<string, DocPropertyEnumeration> sortEntity = new SortedList<string, DocPropertyEnumeration>();

			foreach (DocPropertyEnumeration docType in this.PropertyEnumerations)
			{
				sortEntity.Add(docType.Name, docType);
			}

			this.PropertyEnumerations.Clear();
			this.PropertyEnumerations.AddRange(sortEntity.Values);
		}
		internal void UpgradeExample(DocExample docExample, Dictionary<DocEntity, DocEntity> migration)
		{
			// load example

			if (docExample.File != null)
			{
				string source = Encoding.ASCII.GetString(docExample.File);
				string target = source;

				foreach (DocEntity docSource in migration.Keys)
				{
					DocEntity docTarget = migration[docSource];

					target = target.Replace(docSource.Name.ToUpper(), docTarget.Name.ToUpper()); // brute force -- this could also falsely impact descriptions (not just definitions), but pragmatic for now, and we want to preserve comments and layout of data the same
				}

				if (String.Compare(source, target) != 0)
				{
					docExample.File = Encoding.ASCII.GetBytes(target);
				}
			}

			// recurse
			foreach (DocExample sub in docExample.Examples)
			{
				UpgradeExample(sub, migration);
			}
		}

		public DocSchema GetSchema(string schema)
		{
			if (schema == null)
				return null;

			foreach (DocSection docSection in this.Sections)
			{
				foreach (DocSchema docSchema in docSection.Schemas)
				{
					if (docSchema.Name != null && docSchema.Name.Equals(schema, StringComparison.InvariantCultureIgnoreCase))
					{
						return docSchema;
					}
				}
			}

			return null;
		}

		private static void AddGuid(Dictionary<Guid, DocObject> mapGuid, DocObject docObj)
		{
			try
			{
				mapGuid.Add(docObj.Uuid, docObj);
			}
			catch
			{
				System.Diagnostics.Debug.WriteLine("Duplicate Guid: " + docObj.Uuid.ToString() + " - " + docObj.GetType().ToString() + " - " + docObj.Name);
			}
		}

		public Dictionary<Guid, DocObject> GenerateGuidMap()
		{
			Dictionary<Guid, DocObject> map = new Dictionary<Guid, DocObject>();
			foreach (DocSection docSection in this.Sections)
			{
				foreach (DocSchema docSchema in docSection.Schemas)
				{
					AddGuid(map, docSchema);
					foreach (DocType docType in docSchema.Types)
					{
						AddGuid(map, docType);
						if (docType is DocEnumeration)
						{
							DocEnumeration docEnum = (DocEnumeration)docType;
							foreach (DocConstant docConst in docEnum.Constants)
							{
								AddGuid(map, docConst);
							}
						}
					}

					foreach (DocEntity docEntity in docSchema.Entities)
					{
						AddGuid(map, docEntity);
						foreach (DocAttribute docAttr in docEntity.Attributes)
						{
							AddGuid(map, docAttr);
						}

						foreach (DocWhereRule docRule in docEntity.WhereRules)
						{
							AddGuid(map, docRule);
						}
					}

					foreach (DocFunction docFunc in docSchema.Functions)
					{
						AddGuid(map, docFunc);
					}

					foreach (DocGlobalRule docRule in docSchema.GlobalRules)
					{
						AddGuid(map, docRule);
					}

					foreach (DocPropertySet docPset in docSchema.PropertySets)
					{
						AddGuid(map, docPset);
						foreach (DocProperty docProp in docPset.Properties)
						{
							AddGuid(map, docProp);
						}
					}

					foreach (DocQuantitySet docQset in docSchema.QuantitySets)
					{
						AddGuid(map, docQset);
						foreach (DocQuantity docProp in docQset.Quantities)
						{
							AddGuid(map, docProp);
						}
					}
				}
			}

			foreach (DocPropertyEnumeration docEnum in PropertyEnumerations)
			{
				AddGuid(map, docEnum);
				foreach (DocPropertyConstant docConst in docEnum.Constants)
				{
					AddGuid(map, docConst);
				}
			}
			return map;
		}

		/// <summary>
		/// Converts a constraint mapping concept into a concept root.
		/// </summary>
		/// <param name="docConceptMapping"></param>
		/// <returns></returns>
		public DocConceptRoot ConvertMappingToConceptRoot(DocTemplateUsage docConceptMapping)
		{
			return null;
		}

		/// <summary>
		/// Converts a concept root into a constraint mapping.
		/// </summary>
		/// <param name="docRoot"></param>
		/// <returns></returns>
		public DocTemplateUsage ConvertConceptRootToMapping(DocConceptRoot docRoot)
		{
			return null;
		}

		public void BuildMaps(Dictionary<string, DocObject> mapEntity, Dictionary<string, string> mapSchema)
		{
			foreach (DocPropertyEnumeration def in PropertyEnumerations)
			{
				if (!mapEntity.ContainsKey(def.Name))
				{
					mapEntity.Add(def.Name, def);
				}
			}
			foreach (DocSection docSection in Sections)
			{
				foreach (DocSchema docSchema in docSection.Schemas)
				{
					foreach (DocEntity def in docSchema.Entities)
					{
						if (def.Name != null)
						{
							if (!mapSchema.ContainsKey(def.Name))
							{
								mapSchema.Add(def.Name, docSchema.Name);
							}

							if (!mapEntity.ContainsKey(def.Name))
							{
								mapEntity.Add(def.Name, def);
							}
						}

					}
					foreach (DocType def in docSchema.Types)
					{
						// bug in vex file: IfcNullStyle included twice (?)
						if (!mapSchema.ContainsKey(def.Name))
						{
							mapSchema.Add(def.Name, docSchema.Name);
						}

						if (!mapEntity.ContainsKey(def.Name))
						{
							mapEntity.Add(def.Name, def);
						}
					}
					foreach (DocFunction def in docSchema.Functions)
					{
						// e.g. IfcDotProduct defined in multiple schemas!!!
						if (!mapSchema.ContainsKey(def.Name))
						{
							mapSchema.Add(def.Name, docSchema.Name);
						}
						if (!mapEntity.ContainsKey(def.Name))
						{
							mapEntity.Add(def.Name, def);
						}
					}
					foreach (DocGlobalRule def in docSchema.GlobalRules)
					{
						mapSchema.Add(def.Name, docSchema.Name);
						if (!mapEntity.ContainsKey(def.Name))
						{
							mapEntity.Add(def.Name, def);
						}
					}
					foreach (DocPropertySet def in docSchema.PropertySets)
					{
						if (def.Name != null)
						{
							mapSchema.Add(def.Name, docSchema.Name);
						}
						if (!mapEntity.ContainsKey(def.Name))
						{
							mapEntity.Add(def.Name, def);
						}
					}

					foreach (DocQuantitySet def in docSchema.QuantitySets)
					{
						mapSchema.Add(def.Name, docSchema.Name);
						if (!mapEntity.ContainsKey(def.Name))
						{
							mapEntity.Add(def.Name, def);
						}
					}
				}
			}

		}
	}

	/// <summary>
	/// A definition of a template which provides boilerplate text for Use Definitions, and is applicable to a particular IFC entity and its descendents.
	/// </summary>
	public class DocTemplateDefinition : DocObject // now inherits from DocObject
	{
		[DataMember(Order = 0)] [XmlAttribute] public string Type { get; set; } // applicable entity base type for which this template may be used, e.g. "IfcElement"
		[DataMember(Order = 1), Obsolete] internal string _Description { get; set; } // text at top of section, e.g. "Materials are defined on the @Type using IfcRelAssociatesMaterial"
		[DataMember(Order = 2), Obsolete] private string _ContentListHead { get; set; } // text at top of list, if items are present, e.g. "<ul>"
		[DataMember(Order = 3), Obsolete] private string _ContentListItem { get; set; } // text for each item within list (repeated), e.g. "<li><b>@1</b>: @2</li>"
		[DataMember(Order = 4), Obsolete] private string _ContentListTail { get; set; } // text at bottom of list, e.g. "</ul>"
		[DataMember(Order = 5), Obsolete] private string _FieldType1 { get; set; } // type of custom field #1, e.g. "IfcLabel"
		[DataMember(Order = 6), Obsolete] private string _FieldType2 { get; set; } // type of custom field #2, e.g. "IfcText"
		[DataMember(Order = 7), Obsolete] private string _FieldType3 { get; set; } // type of custom field #3, e.g. "IfcDistributionSystemTypeEnum"
		[DataMember(Order = 8), Obsolete] private string _FieldType4 { get; set; } // type of custom field #4, e.g. "IfcFlowDirectionEnum"        
		[DataMember(Order = 9)] [XmlArray] public List<DocModelRule> Rules { get; protected set; } //NEW IN 2.5
		[DataMember(Order = 10)] [XmlArray] public List<DocTemplateDefinition> Templates { get; protected set; } // NEW IN 2.7 sub-templates
		[DataMember(Order = 11)] [XmlAttribute] public bool IsDisabled { get; set; }

		private bool? _validation; // unserialized; null: no applicable instances; false: one or more failures; true: all pass


		public static readonly Guid guidTemplateQset = new Guid("6652398e-6579-4460-8cb4-26295acfacc7");
		public static readonly Guid guidTemplatePsetObject = new Guid("f74255a6-0c0e-4f31-84ad-24981db62461");
		public static readonly Guid guidTemplatePsetMaterial = new Guid("f3269e50-59bd-4660-a1df-68e93c8ba30f");
		public static readonly Guid guidTemplatePsetProfile = new Guid("34ff8134-b8da-4670-9839-f24b362d6ecf");
		public static readonly Guid guidTemplatePropertySingle = new Guid("6655f6d0-29a8-47b8-8f3d-c9fce9c9a620");
		public static readonly Guid guidTemplatePropertyBounded = new Guid("3d67a2d2-761d-44d9-a09e-b7fbb1fa5632");
		public static readonly Guid guidTemplatePropertyEnumerated = new Guid("c148a099-c351-43a8-9266-5f3de0b45a95");
		public static readonly Guid guidTemplatePropertyList = new Guid("8e10b688-9179-4e3a-8db2-6abcaafe952d");
		public static readonly Guid guidTemplatePropertyTable = new Guid("35c947b0-6abc-4b13-8ec7-696ef2041721");
		public static readonly Guid guidTemplatePropertyReference = new Guid("e20bc116-889b-46cc-b193-31b3e2376a8e");
		public static readonly Guid guidTemplateMapping = new Guid("daf77f62-7ec3-4ff1-bec2-b3175f51f14b");
		public static readonly Guid guidPortNesting = new Guid("bafc93b7-d0e2-42d8-84cf-5da20ee1480a");



		// Note: for file compatibility, above fields must remain

		public DocTemplateDefinition()
		{
			this.Rules = new List<DocModelRule>();
			this.Templates = new List<DocTemplateDefinition>();
		}

		/// <summary>
		/// Indicates whether latest test passes (true), has one or more failures (false), or no applicable instances (null). Not serialized.
		/// </summary>
		public bool? Validation
		{
			get
			{
				return this._validation;
			}
			set
			{
				this._validation = value;
			}
		}


		protected internal override void FindQuery(string query, bool searchtext, List<DocFindResult> results)
		{
			base.FindQuery(query, searchtext, results);

			foreach (DocTemplateDefinition docSub in this.Templates)
			{
				docSub.FindQuery(query, searchtext, results);
			}
		}

		public DocTemplateDefinition GetTemplate(Guid guid)
		{
			if (this.Uuid == guid)
				return this;

			if (this.Templates == null)
				return null;

			foreach (DocTemplateDefinition docTemplate in this.Templates)
			{
				DocTemplateDefinition docEach = docTemplate.GetTemplate(guid);
				if (docEach != null)
				{
					return docEach;
				}
			}

			return null;
		}

		/// <summary>
		/// Returns array of parameters available according to rules.
		/// Used to populate use definition tables.
		/// </summary>
		/// <returns></returns>
		public string[] GetParameterNames()
		{
			List<DocModelRule> list = new List<DocModelRule>();

			if (this.Rules != null)
			{
				foreach (DocModelRule rule in this.Rules)
				{
					rule.BuildParameterList(list);
				}
			}

			string[] array = new string[list.Count];
			for (int i = 0; i < list.Count; i++)
			{
				array[i] = list[i].Identification;
			}
			return array;
		}

		public DocModelRule[] GetParameterRules()
		{
			List<DocModelRule> list = new List<DocModelRule>();

			if (this.Rules != null)
			{
				foreach (DocModelRule rule in this.Rules)
				{
					rule.BuildParameterList(list);
				}
			}

			return list.ToArray();
		}

		/// <summary>
		/// Resolves a template parameter type
		/// </summary>
		/// <param name="docTemplate"></param>
		/// <param name="parameter"></param>
		/// <returns></returns>
		public DocDefinition GetParameterType(string parameter, Dictionary<string, DocObject> map)
		{
			DocObject docEnt = null;
			if (!map.TryGetValue(this.Type, out docEnt) || !(docEnt is DocEntity))
				return null;

			DocEntity docEntity = (DocEntity)docEnt;

			foreach (DocModelRule rule in this.Rules)
			{
				if (rule is DocModelRuleAttribute)
				{
					DocDefinition docAttr = docEntity.ResolveParameterType((DocModelRuleAttribute)rule, parameter, map);
					if (docAttr != null)
					{
						return docAttr;
					}
				}
			}

			return null;
		}

		internal bool Includes(DocTemplateDefinition docTemplateDefinition)
		{
			if (this == docTemplateDefinition)
				return true;

			foreach (DocTemplateDefinition sub in this.Templates)
			{
				if (sub.Includes(docTemplateDefinition))
					return true;
			}

			return false;
		}

		public DocModelRule[] BuildRulePath(DocModelRule docRule)
		{
			List<DocModelRule> list = new List<DocModelRule>();

			foreach (DocModelRule docEach in this.Rules)
			{
				bool result = docEach.BuildRulePath(list, docRule);
				if (result)
					break;
			}

			return list.ToArray();
		}

		/// <summary>
		/// Finds rule according to path delimited by backslash (as returned from a TreeView)
		/// </summary>
		/// <param name="rulepath"></param>
		/// <returns></returns>
		public DocModelRule[] GetRulePath(string rulepath)
		{
			string[] parts = rulepath.Split('\\'); // ignore first component which is the applicable entity

			DocModelRule[] objpath = new DocModelRule[parts.Length - 1];

			for (int i = 0; i < objpath.Length; i++)
			{
				List<DocModelRule> list;
				if (i == 0)
				{
					list = this.Rules;
				}
				else
				{
					list = objpath[i - 1].Rules;
				}

				if (list == null)
					return objpath;

				foreach (DocModelRule docModelRule in list)
				{
					if (docModelRule.Name != null && docModelRule.Name.Equals(parts[i + 1]))
					{
						objpath[i] = docModelRule;
						break;
					}
				}

				if (objpath[i] == null)
					break;
			}

			return objpath;
		}

		/// <summary>
		/// Finds rule and propagates it to all child items
		/// </summary>
		/// <param name="rulepath"></param>
		public void PropagateRule(string rulepath)
		{
			if (this.Templates == null)
				return;

			DocModelRule[] objpath = GetRulePath(rulepath);

			foreach (DocTemplateDefinition docTemplate in this.Templates)
			{
				DocModelRule[] childpath = docTemplate.GetRulePath(rulepath);

				for (int i = 0; i < objpath.Length; i++)
				{
					if (childpath[i] == null && objpath[i] != null)
					{
						// must add rule
						childpath[i] = (DocModelRule)objpath[i].Clone();
						if (i > 0)
						{
							childpath[i - 1].Rules.Add(childpath[i]);
							childpath[i].ParentRule = childpath[i - 1];
						}
						else
						{
							docTemplate.Rules.Add(childpath[i]);
							childpath[i].ParentRule = null;
						}
					}
					else if (objpath[i] == null && childpath[i] != null)
					{
						// must delete rule
						if (i > 0)
						{
							childpath[i - 1].Rules.Remove(childpath[i]);
						}
						else
						{
							docTemplate.Rules.Remove(childpath[i]);
						}
						childpath[i].Delete();
					}
					else if (objpath[i] != null && childpath[i] != null)
					{
						// exists -- must update expression
						childpath[i].CardinalityMin = objpath[i].CardinalityMin;
						childpath[i].CardinalityMax = objpath[i].CardinalityMax;
						childpath[i].Description = objpath[i].Description;
						childpath[i].Identification = objpath[i].Identification;

						if (childpath[i] is DocModelRuleEntity)
						{
							DocModelRuleEntity dmeChild = (DocModelRuleEntity)childpath[i];
							DocModelRuleEntity dmeObj = (DocModelRuleEntity)objpath[i];
							dmeChild.References.Clear();
							foreach (DocTemplateDefinition dtdRef in dmeObj.References)
							{
								dmeChild.References.Add(dtdRef);
							}
						}
					}
				}

				// cascade downwards
				docTemplate.PropagateRule(rulepath);
			}
		}

		internal bool IsTemplateReferenced(DocTemplateDefinition docTemplateDefinition)
		{
			foreach (DocModelRule docRule in this.Rules)
			{
				if (docRule.IsTemplateReferenced(docTemplateDefinition))
					return true;
			}

			return false;
		}

		internal void Rename(DocSchema docSchema, DocDefinition docDefinition, DocAttribute docAttribute, string newname)
		{
			foreach (DocModelRule docRule in this.Rules)
			{
				docRule.RenameDefinition(docSchema, docDefinition, docAttribute, newname);
			}

			foreach (DocTemplateDefinition docSub in this.Templates)
			{
				docSub.Rename(docSchema, docDefinition, docAttribute, newname);
			}
		}
		public void SortTemplate()
		{
			DocObject.ComparerDocObject comparer = new DocObject.ComparerDocObject();
			Templates.Sort(comparer);
			foreach (DocTemplateDefinition t in Templates)
				t.SortTemplate();
		}
		internal void setRuleIds()
		{
			foreach (DocModelRule rule in Rules)
				rule.setIdNew();
			foreach (DocTemplateDefinition templateDefinition in Templates)
				templateDefinition.setRuleIds();
		}
	}

	// this is kept as single structure (rather than on ConceptRoot) such that all formatting info can be easily accessed in one place, and not comingle usage of concepts
	/// <summary>
	/// Indicates how attributes should be formatted for an XML schema, for a specific MVD
	/// </summary>
	public class DocXsdFormat : SEntity // new in 5.7
	{
		[DataMember(Order = 0)] [XmlAttribute] public string Entity { get; set; } // string to avoid referential dependencies
		[DataMember(Order = 1)] [XmlAttribute] public string Attribute { get; set; } // string to avoid referential dependencies
		[DataMember(Order = 2)] [XmlAttribute] public DocXsdFormatEnum XsdFormat { get; set; }
		[DataMember(Order = 3)] [XmlAttribute] public bool? XsdTagless { get; set; }
	}

	// custom field types may be IFC Types (defined types, enumerations) to indicate that a *value* should be specified of the particular type.
	// custom field types may be IFC Entities (e.g. IfcElement) to indicate that an entity *type* should be specified deriving from the particular type.    

	public class DocProcess : DocObject
	{
		[DataMember(Order = 0)] [XmlArray] public List<DocExchangeItem> Inputs { get; protected set; }
		[DataMember(Order = 1)] [XmlArray] public List<DocExchangeItem> Outputs { get; protected set; }

		public DocProcess()
		{
			this.Inputs = new List<DocExchangeItem>();
			this.Outputs = new List<DocExchangeItem>();
		}
	}

	// new in IfcDoc 2.7
	public class DocModelView : DocObject
	{
		[DataMember(Order = 0)] [XmlArray] public List<DocExchangeDefinition> Exchanges { get; protected set; }
		[DataMember(Order = 1)] [XmlArray] public List<DocConceptRoot> ConceptRoots { get; protected set; } // new in 3.5
		[DataMember(Order = 2)] [XmlAttribute] public string BaseView { get; set; } // new in 3.9
		[DataMember(Order = 3)] [XmlAttribute] public string XsdUri { get; set; } // new in 5.4
		[DataMember(Order = 4)] [XmlArray] public List<DocXsdFormat> XsdFormats { get; protected set; } // new in 5.7
		[DataMember(Order = 5)] [XmlAttribute] public bool IncludeAllDefinitions { get; set; } // new in 8.9: if true, then don't filter out unreferenced entities/attributes
		[DataMember(Order = 6)] [XmlAttribute] public string RootEntity { get; set; } // new in 8.9: indicates root entity of schema, as shown in inheritance diagram
		[DataMember(Order = 7)] [XmlAttribute] public byte[] Icon { get; set; } // embedded PNG file of 16x16 icon // added in IfcDoc 9.6
		[DataMember(Order = 8)] [XmlArray] public List<DocProcess> Processes { get; protected set; } // new in V11.5
		[DataMember(Order = 9)] [XmlArray] [XmlArrayItem(NestingLevel = 1)] public List<DocModelView> ModelViews { get; protected set; } // new in V11.6 -- organize sub-views

		private Dictionary<DocObject, bool> m_filtercache; // for performance, remember items within scope of model view; built on demand, cleared whenever there's a change that could impact

		public DocModelView()
		{
			this.Exchanges = new List<DocExchangeDefinition>();
			this.ConceptRoots = new List<DocConceptRoot>();
			this.XsdFormats = new List<DocXsdFormat>();
			this.Processes = new List<DocProcess>();
			this.ModelViews = new List<DocModelView>();
		}

		public DocConceptRoot GetConceptRoot(Guid guid)
		{
			if (this.ConceptRoots != null)
			{
				foreach (DocConceptRoot docConcept in this.ConceptRoots)
				{
					if (docConcept.Uuid == guid)
					{
						return docConcept;
					}
				}
			}

			return null;
		}


		/// <summary>
		/// Sorts concept root list according to alphabetical name
		/// </summary>
		public void SortConceptRoots()
		{
			SortedList<string, DocConceptRoot> sortEntity = new SortedList<string, DocConceptRoot>();

			try
			{
				foreach (DocConceptRoot docType in this.ConceptRoots)
				{
					string key = docType.ToString() + docType.Uuid.ToString();
					if (sortEntity.ContainsKey(key)) //duplicates
						return;
					sortEntity[key] = docType;
				}
			}
			catch
			{
				return;
			}

			this.ConceptRoots.Clear();
			this.ConceptRoots.AddRange(sortEntity.Values);
		}
		public void SortModelView()
		{
			DocObject.ComparerDocObject comparer = new DocObject.ComparerDocObject();
			Exchanges.Sort(comparer);
			ConceptRoots.Sort(comparer);
			Processes.Sort(comparer);
			ModelViews.Sort(comparer);
		}
		/// <summary>
		/// Returns filter of objects within model view, caching as necessary
		/// </summary>
		/// <param name="project">The project to use, or null to clear cache for future rebuild</param>
		/// <returns>Dictionary of objects within scope, or null if cleared.</returns>
		public Dictionary<DocObject, bool> Filter(DocProject docProject)
		{
			if (docProject == null)
			{
				this.m_filtercache = null;
			}
			else if (this.m_filtercache == null)
			{
				this.m_filtercache = new Dictionary<DocObject, bool>();
				docProject.RegisterObjectsInScope(this, this.m_filtercache);
			}

			return this.m_filtercache;
		}
	}

	// new in IfcDoc 3.5 -- organizes concepts according to MVD
	public class DocConceptRoot : DocObject
	{
		[DataMember(Order = 0)] [XmlElement] public DocEntity ApplicableEntity { get; set; }
		[DataMember(Order = 1)] [XmlArray] [XmlArrayItem(NestingLevel = 1)] public List<DocTemplateUsage> Concepts { get; protected set; }
		[DataMember(Order = 2)] [XmlElement] public DocTemplateDefinition ApplicableTemplate { get; set; } // V9.3: optional template definition to be used for determining applicability
		[DataMember(Order = 3)] [XmlArray] public List<DocTemplateItem> ApplicableItems { get; protected set; } // V9.3: items used for template definition
		[DataMember(Order = 4)] [XmlElement] public DocTemplateOperator ApplicableOperator { get; set; } // V9.3: operator used for items

		protected override string xmlid() { return (ApplicableEntity == null ? "" : ApplicableEntity.Name + "_") + GlobalId.Format(Uuid); } 

		public DocConceptRoot()
		{
			this.Concepts = new List<DocTemplateUsage>();
			this.ApplicableItems = new List<DocTemplateItem>();
		}


		/// <summary>
		/// Returns display name that also captures definition if different.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			string name = this.Name;
			if (this.ApplicableEntity != null)
			{
				if ((String.IsNullOrEmpty(this.Name) || this.Name.Equals(this.ApplicableEntity.Name)))
				{
					name = this.ApplicableEntity.Name;
				}
				else
				{
					//  concept name differs
					name = this.Name + " [" + this.ApplicableEntity.Name + "]";
				}
			}

			return name;
		}
	}

	// new in IfcDoc 2.7
	public class DocExchangeDefinition : DocObject
	{
		[DataMember(Order = 0), Obsolete] [XmlAttribute] internal string _Description { get; set; } // added in IfcDoc 3.4, obsolete in IfcDoc 4.9 -- description for formatting purposes
		[DataMember(Order = 1)] [XmlElement] public byte[] Icon { get; set; } // embedded PNG file of 16x16 icon // added in IfcDoc 4.9
		[DataMember(Order = 2)] [XmlAttribute] public DocExchangeApplicabilityEnum Applicability { get; set; }            // added in IfcDoc 4.9
		[DataMember(Order = 3)] [XmlAttribute] public string ExchangeClass { get; set; } // added in IfcDoc 5.3
		[DataMember(Order = 4)] [XmlAttribute] public string SenderClass { get; set; } // added in IfcDoc 5.3
		[DataMember(Order = 5)] [XmlAttribute] public string ReceiverClass { get; set; } // added in IfcDoc 5.3
	}

	// new in IfcDoc 2.5
	public abstract class DocModelRule : SEntity,
		ICloneable// abstract in IfcDoc 2.7
	{
		[DataMember(Order = 0)] [XmlAttribute] public string Name { get; set; } // the attribute or entity name, case-sensitive
		[DataMember(Order = 1)] [XmlAttribute] public string Description { get; set; } // used as human description on template rules; otherwise holds special encodings
		[DataMember(Order = 2)] [XmlAttribute] public string Identification { get; set; } // the template parameter ID
		[DataMember(Order = 3)] [XmlElement] public List<DocModelRule> Rules { get; protected set; } // subrules
		//[DataMember(Order = 4)] public DocModelRuleTypeEnum Type; // deleted in IfcDoc 2.7        
		[DataMember(Order = 4), Obsolete] public int CardinalityMin { get; set; } // -1 means undefined // added in IfcDoc 3.3 ; DEPRECATED
		[DataMember(Order = 5), Obsolete] public int CardinalityMax { get; set; } // -1 means unbounded // added in IfcDoc 3.3 ; DEPRECATED

		[IgnoreDataMember] [XmlIgnore] [InverseProperty("Rules")] public DocModelRule ParentRule { get; set; }

		public DocModelRule()
		{
			this.Rules = new List<DocModelRule>();
		}

		public override string ToString()
		{
			return this.Name;
		}

		public bool IsCondition()
		{
			// special encoding to indicate rule represents a condition instead of a constraint.
			return (this.Description != null && this.Description.Equals("*"));
		}

		public virtual void BuildParameterList(IList<DocModelRule> list)
		{
			// base implementation recurses
			foreach (DocModelRule sub in this.Rules)
			{
				sub.BuildParameterList(list);
			}
		}

#if false
        public string GetCardinalityExpression()
        {
            if (this.CardinalityMin == 0 && this.CardinalityMax == 0)
                return null;

            if(this.CardinalityMin == -1 && this.CardinalityMax == -1)
                return " [0:0]";

            string min = "?";
            string max = "?";
            if (this.CardinalityMin >= 0)
            {
                min = this.CardinalityMin.ToString();
            }
            if (this.CardinalityMax >= 0)
            {
                max = this.CardinalityMax.ToString();
            }

            return " [" + min + ":" + max + "]";
        }
#endif
		public abstract bool? Validate(object target, DocTemplateItem docItem, Dictionary<string, Type> typemap, List<DocModelRule> trace,
			object root, DocTemplateUsage docOuterConcept, Dictionary<DocModelRuleAttribute, bool> conditions);

		public virtual bool IsTemplateReferenced(DocTemplateDefinition docTemplate)
		{
			return false;
		}

		/// <summary>
		/// Makes deep copy of rule and all child rules
		/// </summary>
		/// <returns></returns>
		public override object Clone()
		{
			DocModelRule modelrule = (DocModelRule)Activator.CreateInstance(this.GetType()); // force constructor to get called to hook up events MemberwiseClone();
			modelrule.Name = this.Name;
			modelrule.Description = this.Description;
			modelrule.Identification = this.Identification;
			modelrule.CardinalityMin = this.CardinalityMin;
			modelrule.CardinalityMax = this.CardinalityMax;

			foreach (DocModelRule sub in this.Rules)
			{
				DocModelRule clone = (DocModelRule)sub.Clone();
				modelrule.Rules.Add(clone);
				clone.ParentRule = modelrule;
			}

			return modelrule;
		}

		public bool BuildRulePath(List<DocModelRule> path, DocModelRule target)
		{
			path.Add(this);

			if (target == this)
				return true;

			if (this.Rules != null)
			{
				foreach (DocModelRule docSub in this.Rules)
				{
					bool find = docSub.BuildRulePath(path, target);
					if (find)
						return true;
				}
			}

			path.Remove(this);

			return false;
		}

		internal virtual void EmitInstructions(
			Compiler context,
			ILGenerator generator,
			DocTemplateDefinition dtd)
		{
			if (this.Rules != null)
			{
				foreach (DocModelRule rule in this.Rules)
				{
					rule.EmitInstructions(context, generator, dtd);
				}
			}
		}

		public void RenameParameter(string identification, DocProject docProject, DocTemplateDefinition docTemplateDefinition)
		{

			if (String.IsNullOrEmpty(this.Identification))
			{
				// setting, nothing to rename
				this.Identification = identification;
				return;
			}

			string oldvalue = this.Identification;

			// otherwise renaming or clearing
			foreach (DocModelView docView in docProject.ModelViews)
			{
				foreach (DocConceptRoot docRoot in docView.ConceptRoots)
				{
					foreach (DocTemplateUsage docConcept in docRoot.Concepts)
					{
						docConcept.RenameParameter(docTemplateDefinition, oldvalue, identification);

						// also try sub-templates (one-level deep for now)
						foreach (DocTemplateDefinition sub in docTemplateDefinition.Templates)
						{
							docConcept.RenameParameter(sub, oldvalue, identification);
						}
					}
				}
			}

			// now update
			this.Identification = identification;

		}

		internal void setIdNew()
		{
			foreach (DocModelRule rule in Rules)
			{
				DocModelRuleConstraint constraint = rule as DocModelRuleConstraint;
				if (constraint != null)
				{
					DocOpStatement statement = constraint.Expression as DocOpStatement;
					if (statement != null)
					{
						DocModelRuleEntity docModelRuleEntity = statement.Reference.EntityRule as DocModelRuleEntity;
						if (docModelRuleEntity != null && string.IsNullOrEmpty(docModelRuleEntity.UniqueId))
						{
							docModelRuleEntity.UniqueId = Guid.NewGuid().ToString();
						}
					}
				}
				rule.setIdNew();
			}
		}
		internal void setId(string uniqueStr)
		{
			string str = uniqueStr + "_" +( string.IsNullOrEmpty(Name) ? "" : Name);
			foreach(DocModelRule rule in Rules)
			{
				DocModelRuleConstraint constraint = rule as DocModelRuleConstraint;
				if(constraint != null)
				{
					DocOpStatement statement = constraint.Expression as DocOpStatement;
					if(statement != null)
					{
						DocModelRuleEntity docModelRuleEntity = statement.Reference.EntityRule as DocModelRuleEntity;
						if(docModelRuleEntity != null && string.IsNullOrEmpty(docModelRuleEntity.UniqueId))
						{
							docModelRuleEntity.UniqueId = GlobalId.HashGuid(str + "_" + constraint.Name).ToString();
						}
					}
				}
				rule.setId(str);
			}
		}
		internal virtual void RenameDefinition(DocSchema docSchema, DocDefinition docDefinition, DocAttribute docAttribute, string newname)
		{
			// recurse
			foreach (DocModelRule docRule in this.Rules)
			{
				docRule.RenameDefinition(docSchema, docDefinition, docAttribute, newname);
			}
		}
	}

	public class DocModelRuleAttribute : DocModelRule
	{
		public override void BuildParameterList(IList<DocModelRule> list)
		{
			// add ourselves if marked as parameter
			if (!String.IsNullOrEmpty(this.Identification))
			{
				list.Add(this);
			}

			base.BuildParameterList(list);
		}

		/// <summary>
		/// Checks a value to see if it matches the parameter value.
		/// </summary>
		/// <param name="value">The object to check.</param>
		/// <param name="docItem">Template item to test against the object.</param>
		/// <param name="typemap">Map identifiers to compiled types.</param>
		/// <param name="trace">Sequence of rules leading up to this check, used for reporting any failures.</param>
		/// <param name="root">The root object, used for recording pass/fail status for nested rules.</param>
		/// <param name="docOuterConcept">Outer concept, used for recording objects that failed.</param>
		/// <returns>True if passing, False if failing, Null if inapplicable.</returns>
		private bool? ValidateItem(object value, DocTemplateItem docItem, Dictionary<string, Type> typemap, List<DocModelRule> trace,
			object root, DocTemplateUsage docOuterConcept, Dictionary<DocModelRuleAttribute, bool> conditions)
		{
			// (3) if parameter is defined, check for match
			if (!String.IsNullOrEmpty(this.Identification))
			{
				if (docItem == null)
					return true; // parameter must be specified in order to check this rule

				DocTemplateUsage docInnerConcept = docItem.GetParameterConcept(this.Identification, null);
				if (docInnerConcept != null && docInnerConcept.Definition != null)
				{
					if (docInnerConcept.Items.Count == 0)
						return true; // no items to check

					// V9.0: new behavior: if something isn't listed as a rule, then it fails
					foreach (DocTemplateItem docInnerItem in docInnerConcept.Items)
					{
						{
							bool applies = true;
							bool alltrue = true;
							foreach (DocModelRule docInnerRule in docInnerConcept.Definition.Rules)
							{
								// all inner rules must return true for the template item
								bool? innerResult = docInnerRule.Validate(value, docInnerItem, typemap, trace, root, docInnerConcept, conditions);
								if (innerResult != null)
								{
									if (!innerResult.Value)
									{
										if (value is object)
										{
											List<object> list = docInnerConcept.GetValidationMismatches(root, docOuterConcept);
											list.Add((object)value);
										}

										alltrue = false;
										break;
									}
#if false
                                    if (innerResult.Value)
                                    {
                                        if(docInnerItem.RuleParameters.Contains("Flame"))
                                        {
                                            this.ToString();
                                        }

                                        // passes, regardless if optional or required
                                        docInnerItem.ValidationStructure[root] = true;

                                        if (!docInnerConcept.ValidationStructure.ContainsKey(root))
                                        {
                                            docInnerConcept.ValidationStructure[root] = true;

                                            if (docInnerConcept.Validation == null)
                                            {
                                                docInnerConcept.Validation = true;
                                            }
                                        }
                                        itemresult = true;
                                        break;
                                    }
                                    else
                                    {
                                        // fails -- if required
#if true
                                        if (!docInnerItem.ValidationStructure.ContainsKey(root))
                                        {
                                            docInnerItem.ValidationStructure[root] = false;
                                            if (!docInnerItem.Optional)
                                            {
                                                docInnerConcept.ValidationStructure[root] = false;
                                                docInnerConcept.Validation = false;
                                                itemresult = false;
                                            }
                                        }
#endif
                                    }
                                    //break;
#endif
								}
								else
								{
									// doesn't apply -- a conditional parameter didn't match
									applies = false;
									break;
								}
							}

							if (applies)
							{
								if (alltrue)
								{
									docInnerItem.ValidationStructure[root] = alltrue;
									if (!docInnerConcept.ValidationStructure.ContainsKey(root))
									{
										docInnerConcept.ValidationStructure[root] = true;
										docInnerConcept.Validation = true;
									}
									return true;
								}
								else
								{
									//docInnerConcept.ValidationStructure[root] = false;
									//docInnerConcept.Validation = false;
								}
							}
							else
							{

							}
						}

					}

#if false
                    if (itemresult == null || itemresult == false)
                    {
                        docInnerConcept.ValidationStructure[root] = false;
                        docInnerConcept.Validation = false;
                        return false;
                    }
#endif
					// item found that was unexpected
					docInnerConcept.ValidationStructure[root] = false;
					docInnerConcept.Validation = false;

					if (docItem != null)
					{
						List<object> listMismatch = docInnerConcept.GetValidationMismatches(root, docItem);
						listMismatch.Add((object)value);
					}
					else
					{
						this.ToString();
					}

					return false;
				}
				else
				{
					string match = docItem.GetParameterValue(this.Identification); // TODO: extend such that parameter value refers to an inner DocTemplateUsage (list of DocTemplateItem's).
					if (value == null && String.IsNullOrEmpty(match))
					{
						//return true;
					}
					else if (value is SEntity)
					{
						if (match == null) // for now, if list of doc template items, then return ok
						{
							return true;
						}
						else if (match != null)// && value.GetType().Name.Equals(match))
						{
							// check for type inheritance -- support example filtering such as for COBie instances using ConceptRoot class filtering
							Type tcheck = null;
							if (typemap.TryGetValue(match, out tcheck) && tcheck.IsInstanceOfType(value))
							{
								return true;
							}
							else
							{
								return false;
							}

							// this was commented out before; added the above
							////return true;
						}
						else
						{
							return false;
						}
					}
					else// if (value != null)
					{
						// pull out internal value type
						object innervalue = null;

						if (value != null)
						{
							innervalue = value.ToString();

							FieldInfo fieldinfo = value.GetType().GetField("Value");
							if (fieldinfo != null)
							{
								innervalue = fieldinfo.GetValue(value);
							}
						}

						Type typeCheck = null;
						if (match != null && typemap.TryGetValue(match, out typeCheck))
						{
							// type comparison
							return typeCheck.IsInstanceOfType(value);
						}
						else
						{
							if (match != null && innervalue != null && innervalue.ToString().Equals(match.ToString(), StringComparison.Ordinal))
							{
								// value comparison
								return true;
							}
							else if (this.IsCondition())
							{
								// condition didn't match, so chain of rules does not apply -- return null.
								return null;
							}
							else
							{
								if (value is object)
								{
									List<object> listMismatch = docOuterConcept.GetValidationMismatches(root, docItem);
									listMismatch.Add((object)value);
								}

								// constraint evaluated to false and conditioned applied.
								return false;
							}
						}
					}
				}
			}

			// (4) recurse through constraints or entity rules
			if (this.Rules != null && this.Rules.Count > 0)
			{
				int tracelen = trace.Count;

				bool skip = false;
				foreach (DocModelRule rule in this.Rules)
				{
					while (trace.Count > tracelen)
					{
						trace.RemoveAt(tracelen);
					}

					// attribute rule is true if at least one entity filter matches or one constraint filter matches
					bool? result = rule.Validate(value, docItem, typemap, trace, root, docOuterConcept, conditions);
					if (result != null && result.Value)
					{
						return result;
					}
					else if (result == null)
					{
						skip = true;
						// keep going
						//return null;
					}
				}

				if (skip)
				{
					return null;
				}

				// keep last failure path intact
				return false;
			}

			return true;
		}

		/// <summary>
		/// Validates an object to meet rule.
		/// </summary>
		/// <param name="target">Required instance to validate.</param>
		/// <param name="docItem">Optional template parameters to use for validation.</param>
		/// <param name="typemap">Map of types to resolve.</param>
		/// <param name="trace"></param>
		/// <param name="root">Root object used for associating status of rules at referenced templates</param>
		/// <returns></returns>
		public override bool? Validate(object target, DocTemplateItem docItem, Dictionary<string, Type> typemap, List<DocModelRule> trace,
			object root, DocTemplateUsage docOuterConcept, Dictionary<DocModelRuleAttribute, bool> conditions)
		{
			trace.Add(this);

			if (target == null)
				return false;

			object value = null;
			Type targetType = target.GetType();
			// (1) check if property/field is defined on target object; if not, then this rule does not apply.
			PropertyInfo propertyInfo = targetType.GetProperty(this.Name);
			if (propertyInfo == null)
			{
				FieldInfo fieldinfo = target.GetType().GetField(this.Name);
				if (fieldinfo == null)
					return null; // return NULL, not FALSE -- field is not applicable to object, e.g. PredefinedType may not exist on a given subtype (e.g. IFC2x3 IfcColumn)
								 // (2) extract the value
				value = fieldinfo.GetValue(target); // may be null
			}
			else
				value = propertyInfo.GetValue(target);

			if (docItem != null && value == null)
				return false; // structure required to exist

			bool? checkcard = null;
			if (value is System.Collections.IEnumerable)
			{
				System.Collections.IEnumerable list = (System.Collections.IEnumerable)value;
				int pass = 0;
				int fail = 0;

				int tracelen = trace.Count;

				foreach (object o in list)
				{
					// reset
					while (trace.Count > tracelen)
					{
						trace.RemoveAt(tracelen);
					}

					bool? result = ValidateItem(o, docItem, typemap, trace, root, docOuterConcept, conditions);
					if (result != null)
					{
						if (result.Value)
						{
							pass++;
						}
						else
						{
							fail++;
						}
					}
				}

#if true // ???Georgia Tech PCI-103 per V9.4 Review -- change to allow
				if (docItem != null)
				{
					// previous behavior in V9.4 was:
					checkcard = (pass > 0);// (fail == 0);
				}
				else
#endif
				if (pass > 0)
				{
					checkcard = true;
				}
				else if (fail > 0)
				{
					checkcard = false;
				}
				else
				{
					checkcard = null;
				}

				if (checkcard == null || checkcard.Value)
				{
					while (trace.Count > tracelen)
					{
						trace.RemoveAt(tracelen);
					}

					trace.Remove(this);
				}
			}
			else
			{
				// validate single
				checkcard = ValidateItem(value, docItem, typemap, trace, root, docOuterConcept, conditions);
				if (checkcard == null || !checkcard.Value)
				{
					trace.Remove(this);
				}

			}

			// if parameter, mark it as matched
			if (checkcard != null && this.IsCondition())
			{
				conditions.Add(this, checkcard.Value);
			}

			return checkcard;
		}
	}

	public class DocModelRuleEntity : DocModelRule
	{
		[DataMember(Order = 0)] [XmlArray] public List<DocTemplateDefinition> References { get; protected set; } // IfcDoc 6.3: references to chained templates
		[DataMember(Order = 1)] [XmlAttribute] public string Prefix { get; set; }
		[DataMember(Order = 2)] [XmlAttribute] public string UniqueId { get; set; } // V12.2 inserted

		[IgnoreDataMember] [XmlIgnore] public string id { get { return (string.IsNullOrEmpty(Name) ? "" : Name + "_") + GlobalId.Format(GlobalId.Parse(UniqueId));  } }

		public DocModelRuleEntity()
		{
			this.References = new List<DocTemplateDefinition>();
		}

		public override bool IsTemplateReferenced(DocTemplateDefinition docTemplate)
		{
			return this.References.Contains(docTemplate);
		}

		internal override void RenameDefinition(DocSchema docSchema, DocDefinition docDefinition, DocAttribute docAttribute, string newname)
		{
			if (this.Name.Equals(docDefinition.Name))
			{
				if (docAttribute == null)
				{
					this.Name = newname;
				}
				else
				{
					foreach (DocModelRule docRule in this.Rules)
					{
						if (docRule.Name.Equals(docAttribute.Name))
						{
							docRule.Name = newname;
						}
					}
				}
			}

			base.RenameDefinition(docSchema, docDefinition, docAttribute, newname);
		}

		/// <summary>
		/// Validates rules for an entity.
		/// </summary>
		/// <param name="target">Required object to validate.</param>
		/// <param name="docItem">Template item to validate.</param>
		/// <param name="typemap">Map of type names to type definitions.</param>
		/// <returns>True if passing, False if failing, or null if inapplicable.</returns>
		public override bool? Validate(object target, DocTemplateItem docItem, Dictionary<string, Type> typemap, List<DocModelRule> trace,
			object root, DocTemplateUsage docOuterConcept, Dictionary<DocModelRuleAttribute, bool> conditions)
		{
			trace.Add(this);

			// checking for matching cast
			Type t = null;
			if (typemap == null || !typemap.TryGetValue(this.Name, out t))
				return false;

			if (target == null)
				return false;

			if (!t.IsInstanceOfType(target))
				return null; // if instance doesn't match, return null (not failure) to skip it, e.g. IfcObject.IsDefinedBy\IfcRelDefinesByType encountered while looking for IfcObject.IsDefinedBy\IfcRelDefinesByProperties

			bool canpass = true; // if false, then can only be null (if not applicable due to parameter filtered out), or false (failure)

			// first pass: catch any conditional parameters
			List<DocModelRule> nonConditionalRules = new List<DocModelRule>();
			foreach (DocModelRule rule in this.Rules)
			{
				if (rule.IsCondition())
				{
					bool? result = rule.Validate(target, docItem, typemap, trace, root, docOuterConcept, conditions);

					// entity rule is inapplicable if any attribute rules are inapplicable
					if (result == null || !result.Value)
					{
						trace.Remove(this);
						return null;
					}

#if false
					// entity rule fails if any attribute rules fail
					if (!result.Value)
					{
						return null;
						canpass = false;
					}
#endif
				}
				else
					nonConditionalRules.Add(rule);
			}

			// second pass: catch any non-conditional parameters
			foreach (DocModelRule rule in nonConditionalRules)
			{
				bool? result = rule.Validate(target, docItem, typemap, trace, root, docOuterConcept, conditions);

					// entity rule is inapplicable if any attribute rules are inapplicable
				if (result == null)
				{
					trace.Remove(this);
					return null;
				}

				// entity rule fails if any attribute rules fail
				if (!result.Value)
				{
					canpass = false;
				}
			}

			foreach(DocTemplateDefinition docTemplateDefinition in References)
			{
				foreach(DocModelRule rule in docTemplateDefinition.Rules)
				{
					bool? result = rule.Validate(target, docItem, typemap, trace, root, docOuterConcept, conditions);

					// entity rule is inapplicable if any attribute rules are inapplicable
					if (result == null)
					{
						trace.Remove(this);
						return null;
					}

					// entity rule fails if any attribute rules fail
					if (!result.Value)
					{
						canpass = false;
					}
				}
			}
			if (canpass)
			{
				trace.Remove(this);
			}

			return canpass;

		}

	}

	public class DocModelRuleConstraint : DocModelRule
	{
		[DataMember(Order = 0)] [XmlElement] public DocOpExpression Expression { get; set; } // new in IfcDoc 6.1

		public override bool? Validate(object target, DocTemplateItem docItem, Dictionary<string, Type> typemap, List<DocModelRule> trace,
			object root, DocTemplateUsage docOuterConcept, Dictionary<DocModelRuleAttribute, bool> conditions)
		{
			// constraint validation is now done in compiled code -- indicate pass to keep going
			return true;
		}

		internal override void EmitInstructions(
			Compiler context,
			ILGenerator generator,
			DocTemplateDefinition dtd)
		{
			if (this.Expression != null)
			{
				this.Expression.Emit(context, generator, dtd, null);

				// if successful, keep going; if not, then return false
				generator.Emit(OpCodes.Brtrue_S, (byte)2);
				generator.Emit(OpCodes.Ldc_I4_0);
				generator.Emit(OpCodes.Ret);
			}
		}

		/// <summary>
		/// Formats expression according to mvdXML syntax.
		/// </summary>
		/// <returns></returns>
		public string FormatExpression(DocTemplateDefinition template)
		{
			if (this.Expression == null)
				return null;

			return this.Expression.ToString(template);
		}
	}

	// Operators are a strict subset of .NET MSIL operators
	// MSIL is used to enable compilation to machine language for the fastest possible validation when dealing with huge files


	/// <summary>
	/// High-level construct of constraint to enable editing, parsing, and translation to common languages
	/// </summary>
	public abstract class DocOp : SEntity
	{
		[DataMember(Order = 0)] [XmlAttribute] public DocOpCode Operation { get; set; }

		/// <summary>
		/// Generates CIL code for operation
		/// </summary>
		/// <param name="writer"></param>
		internal virtual LocalBuilder Emit(
			Compiler context,
			ILGenerator generator,
			DocTemplateDefinition dtd,
			Type valuetype)
		{
			return null;
		}

		/// <summary>
		/// Evaluates the operation and returns the result for debugging purposes.
		/// </summary>
		/// <param name="o"></param>
		/// <returns></returns>
		internal virtual object Eval(object o, Hashtable population, DocTemplateDefinition template, Type valuetype, List<int> indexpath)
		{
			return null;
		}

		public override string ToString()
		{
			return this.Operation.ToString().ToUpper();
		}

		public virtual string ToString(DocTemplateDefinition template)
		{
			return this.ToString();
		}
	}

	public abstract class DocOpExpression : DocOp
	{
	}

	/// <summary>
	/// Compares a variable to a value
	/// </summary>
	public class DocOpStatement : DocOpExpression // ceq|cle|clt|cge|cgt|cne
	{
		[DataMember(Order = 0)] [XmlElement] public DocOpReference Reference { get; set; }  // statement
		[DataMember(Order = 1)] [XmlElement] public DocOpValue Value { get; set; } // statement or constant
		[DataMember(Order = 2)] [XmlAttribute] public DocOpCode Metric { get; set; } // qualifier for additional operation on reference

		public override string ToString()
		{
			return this.ToString(null);
		}

		public override string ToString(DocTemplateDefinition dtd)
		{
			string opname = "=";
			switch (this.Operation)
			{
				case DocOpCode.CompareEqual:
					opname = "=";
					break;

				case DocOpCode.CompareNotEqual:
					opname = "<>";
					break;

				case DocOpCode.CompareLessThanOrEqual:
					opname = "<=";
					break;

				case DocOpCode.CompareLessThan:
					opname = "<";
					break;

				case DocOpCode.CompareGreaterThanOrEqual:
					opname = ">=";
					break;

				case DocOpCode.CompareGreaterThan:
					opname = ">";
					break;

				case DocOpCode.IsIncluded:
					opname = "{";
					break;
			}

			if (this.Reference != null && this.Value != null)
			{
				string metricname = "Value";
				string metrichead = "";
				string metrictail = "";
				string suffix = " " + opname + " " + this.Value.ToString(dtd);
				switch (this.Metric)
				{
					case DocOpCode.LoadLength:
						metrichead = "SIZEOF(";
						metrictail = ")";
						metricname = "Length";
						break;

					case DocOpCode.IsInstance:
						metrichead = "TYPEOF(";
						metrictail = ")";
						metricname = "Type";
						break;

					case DocOpCode.IsUnique:
						metrichead = "UNIQUE(";
						metrictail = ")";
						suffix = "";
						metricname = "Unique";
						break;
				}
				//return metrichead + this.Reference.ToString(dtd) + metrictail + suffix;

				// new: mvdXML syntax
				DocModelRuleEntity docModelRuleEntity = Reference.EntityRule;
				if (docModelRuleEntity != null)
				{
					DocModelRule docModelRule = Reference.EntityRule.ParentRule;
					if (docModelRule != null && !string.IsNullOrEmpty(docModelRule.Identification))
					{
						return Reference.ToString(dtd) + "[" + metricname + "]" + suffix;
					}
					else
					{
						DocModelRule parentRule = docModelRuleEntity.ParentRule;
						if(parentRule != null)
							return parentRule.Identification + "[" + metricname + "]" + suffix;
					}
				}

				//return this.Reference.ToString(dtd) + "[" + metricname + "]" + suffix;
			}

			return null;
		}

		internal override object Eval(object o, Hashtable population, DocTemplateDefinition template, Type valuetype, List<int> indexpath)
		{
			if (this.Reference.EntityRule == null)
				return null;

			// find the type
			Type[] types = o.GetType().Assembly.GetTypes();
			foreach (Type t in types)
			{
				if (t.Name == this.Reference.EntityRule.Name)
				{
					valuetype = t;
					break;
				}
			}

			// apply additional operation on reference according to metric
			List<int> listindex = new List<int>();
			object lvalue = this.Reference.Eval(o, population, template, null, listindex);
			if (lvalue == null)
				return null;

			object rvalue = this.Value.Eval(o, population, template, valuetype, null);
			switch (this.Metric)
			{
				case DocOpCode.NoOperation:
					break;

				case DocOpCode.LoadLength:
					if (rvalue is string)
					{
						rvalue = ((string)rvalue).Length;
					}
					else
					{
						rvalue = 0;
					}
					break;

				case DocOpCode.IsInstance:
					//...
					break;

				case DocOpCode.IsUnique:
					if (lvalue == null)
					{
						return null;
					}
					else if (population.ContainsKey(lvalue))
					{
						return false;
					}
					else
					{
						population.Add(lvalue, lvalue);
						return true;
					}
			}

			bool result = false;
			switch (this.Operation)
			{
				case DocOpCode.IsIncluded:
					if (rvalue is string)
					{
						string srv = (string)rvalue;
						string[] options = srv.Split(',');
						foreach (string option in options)
						{
							if (option.Equals(lvalue))
							{
								result = true;
								break;
							}
						}
					}
					break;

				case DocOpCode.NoOperation: // backcompat
				case DocOpCode.CompareEqual:
					result = Object.Equals(lvalue, rvalue);
					break;

				case DocOpCode.CompareNotEqual:
					result = !Object.Equals(lvalue, rvalue);
					break;

				case DocOpCode.CompareGreaterThan:
					result = ((IComparable)lvalue).CompareTo(rvalue) > 0;
					break;

				case DocOpCode.CompareLessThan:
					result = ((IComparable)lvalue).CompareTo(rvalue) < 0;
					break;

				case DocOpCode.CompareGreaterThanOrEqual:
					result = ((IComparable)lvalue).CompareTo(rvalue) >= 0;
					break;

				case DocOpCode.CompareLessThanOrEqual:
					result = ((IComparable)lvalue).CompareTo(rvalue) <= 0;
					break;
			}

			return result;
		}

		internal override LocalBuilder Emit(Compiler context, ILGenerator generator, DocTemplateDefinition dtd, Type valuetype)
		{
			if (this.Reference.EntityRule == null)
			{
				System.Diagnostics.Debug.WriteLine("Invalid statement reference: " + dtd.Name);
				generator.Emit(OpCodes.Ldc_I4_0); // return false;
				return null;
			}

			Type typeCompare = context.RegisterType(this.Reference.EntityRule.Name);

			// apply additional operation on reference according to metric
			switch (this.Metric)
			{
				case DocOpCode.NoOperation:
					{
						// push the referenced value onto the stack
						LocalBuilder local = this.Reference.Emit(context, generator, dtd, null);

						// cast to IComparable if required
						if (this.Operation == DocOpCode.CompareGreaterThan ||
							this.Operation == DocOpCode.CompareGreaterThanOrEqual ||
							this.Operation == DocOpCode.CompareLessThan ||
							this.Operation == DocOpCode.CompareLessThanOrEqual)
						{
							generator.Emit(OpCodes.Castclass, typeof(IComparable));
						}

						// push the value to be compared onto the stack
						if (typeCompare.IsEnum && this.Value is DocOpLiteral)
						{
							generator.Emit(OpCodes.Ldtoken, typeCompare);

							MethodInfo methodTypeFromHandle = typeof(Type).GetMethod("GetTypeFromHandle");
							generator.Emit(OpCodes.Call, methodTypeFromHandle);

							FieldInfo fieldEnum = null;
							string litval = ((DocOpLiteral)this.Value).Literal;
							if (litval != null)
							{
								fieldEnum = typeCompare.GetField(litval, BindingFlags.Static | BindingFlags.Public);
							}

							if (fieldEnum != null)
							{
								int fieldVal = (int)fieldEnum.GetValue(null);
								generator.Emit(OpCodes.Ldc_I4, fieldVal);
							}
							else
							{
								System.Diagnostics.Debug.WriteLine("Invalid enumeration: " + litval + " - " + dtd.Name);
								generator.Emit(OpCodes.Ldc_I4_0);
							}

							// convert it to object
							MethodInfo methodToObject = typeof(Enum).GetMethod("ToObject", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(Type), typeof(Int32) }, null);
							generator.Emit(OpCodes.Call, methodToObject);
						}
						else
						{
							// string
							this.Value.Emit(context, generator, dtd, typeCompare);
						}
					}
					break;

				case DocOpCode.LoadLength:
					{
						// push the referenced value onto the stack
						LocalBuilder local = this.Reference.Emit(context, generator, dtd, null);

						generator.Emit(OpCodes.Ldlen);

						// push the value to be compared onto the stack
						this.Value.Emit(context, generator, dtd, null);
					}
					break;

				case DocOpCode.IsInstance:
					if (this.Value is DocOpLiteral)
					{
						// push the referenced value onto the stack
						LocalBuilder local = this.Reference.Emit(context, generator, dtd, null);

						Type typeInstance = context.RegisterType(((DocOpLiteral)this.Value).Literal);
						generator.Emit(OpCodes.Isinst, typeInstance);
					}
					return null;

				case DocOpCode.IsUnique: // unique
					{
						string uniquecheck = "_Unique" + this.GetHashCode();
						Type t = context.Module.GetType(uniquecheck);
						if (t == null)
						{
							// create a type with static field to track uniqueness for the particular rule
							TypeBuilder tb = context.Module.DefineType("_Unique" + this.GetHashCode()); // give it a unique name based on OID corresponding to rule
							FieldBuilder fb = tb.DefineField("Hash", typeof(Hashtable), FieldAttributes.Static | FieldAttributes.Public);
							ConstructorBuilder cb = tb.DefineTypeInitializer();
							ILGenerator il = cb.GetILGenerator();
							ConstructorInfo constructorHashtable = typeof(Hashtable).GetConstructor(new Type[] { });
							il.Emit(OpCodes.Newobj, constructorHashtable);
							il.Emit(OpCodes.Stsfld, fb);
							il.Emit(OpCodes.Ret);

							t = tb.CreateType();
						}
						FieldInfo f = t.GetFields()[0];

						// track value in local varaible
						LocalBuilder local = generator.DeclareLocal(typeof(string));
						LocalBuilder localObject = generator.DeclareLocal(typeof(object));

						Label labelPass = generator.DefineLabel();
						Label labelFail = generator.DefineLabel();
						Label labelNext = generator.DefineLabel();

						// store the referenced value
						this.Reference.Emit(context, generator, dtd, null);
						generator.Emit(OpCodes.Stloc, (short)local.LocalIndex);

						// if null, then not considered unique -- fail, jump to end
						generator.Emit(OpCodes.Ldloc, (short)local.LocalIndex);
						generator.Emit(OpCodes.Ldnull);
						generator.Emit(OpCodes.Beq_S, labelFail);

						// check if hash table for rule already contains value
						MethodInfo methodGetItem = typeof(Hashtable).GetMethod("get_Item");
						generator.Emit(OpCodes.Ldsfld, f);
						generator.Emit(OpCodes.Ldloc, (short)local.LocalIndex);
						generator.Emit(OpCodes.Callvirt, methodGetItem);
						generator.Emit(OpCodes.Stloc, (short)localObject.LocalIndex);

						// if it equals ourselves, then it matches so pass
						generator.Emit(OpCodes.Ldloc, (short)localObject.LocalIndex);
						generator.Emit(OpCodes.Ldarg_0);
						generator.Emit(OpCodes.Beq, labelPass);

						// if non-null (and doesn't match), then it fails
						generator.Emit(OpCodes.Ldloc, (short)localObject.LocalIndex);
						generator.Emit(OpCodes.Brtrue_S, labelFail); // skip over to last instruction 

						// otherwise if null, then add to hash table
						MethodInfo methodAdd = typeof(Hashtable).GetMethod("Add");
						generator.Emit(OpCodes.Ldsfld, f); // 5
						generator.Emit(OpCodes.Ldloc, (short)local.LocalIndex); // 3
						generator.Emit(OpCodes.Ldarg_0); // 1 // this pointer
						generator.Emit(OpCodes.Callvirt, methodAdd); // 5

						// not a duplicate (yet) -- first duplicate instance will push True
						generator.MarkLabel(labelPass);
						generator.Emit(OpCodes.Ldc_I4_1); // 1
						generator.Emit(OpCodes.Br_S, labelNext); // 2 - skip over

						// is a duplicate, so not unique: return false
						generator.MarkLabel(labelFail);
						generator.Emit(OpCodes.Ldc_I4_0);

						generator.MarkLabel(labelNext);

						return null;
					}
			}

			switch (this.Operation)
			{
				case DocOpCode.IsIncluded:
					{
						MethodInfo methodSplit = typeof(String).GetMethod("Split", new Type[] { typeof(Char[]) });

						// Stack: V|S

#if false
                        generator.Emit(OpCodes.Ldc_I4_1);             // V|S|I 
                        generator.Emit(OpCodes.Newarr, typeof(Char)); // V|S|A
                        generator.Emit(OpCodes.Dup);                  // V|S|A|A
                        generator.Emit(OpCodes.Ldc_I4_0);             // V|S|A|A|I
                        generator.Emit(OpCodes.Ldc_I4_S, (Byte)',');  // V|S|A|A|I|B
                        generator.Emit(OpCodes.Stelem_I2);            // V|S|A
                        generator.Emit(OpCodes.Call, methodSplit);    // V|A
#endif
						// replacement to accept comma and return characters
						generator.Emit(OpCodes.Ldc_I4_3);             // V|S|I 
						generator.Emit(OpCodes.Newarr, typeof(Char)); // V|S|A
						generator.Emit(OpCodes.Dup);                  // V|S|A|A
						generator.Emit(OpCodes.Ldc_I4_0);             // V|S|A|A|I
						generator.Emit(OpCodes.Ldc_I4_S, (Byte)',');  // V|S|A|A|I|B
						generator.Emit(OpCodes.Stelem_I2);            // V|S|A
						generator.Emit(OpCodes.Dup);                  // V|S|A|A
						generator.Emit(OpCodes.Ldc_I4_1);             // V|S|A|A|I
						generator.Emit(OpCodes.Ldc_I4_S, (Byte)'\r');  // V|S|A|A|I|B
						generator.Emit(OpCodes.Stelem_I2);            // V|S|A
						generator.Emit(OpCodes.Dup);                  // V|S|A|A
						generator.Emit(OpCodes.Ldc_I4_2);             // V|S|A|A|I
						generator.Emit(OpCodes.Ldc_I4_S, (Byte)'\n');  // V|S|A|A|I|B
						generator.Emit(OpCodes.Stelem_I2);            // V|S|A
						generator.Emit(OpCodes.Call, methodSplit);    // V|A

						MethodInfo methodCompare = typeof(Object).GetMethod("Equals", new Type[] { typeof(Object), typeof(Object) });

						// Left argument:  String
						// Right argument: Array of String: MUST have at least one element

						LocalBuilder localValue = generator.DeclareLocal(typeof(object));
						LocalBuilder localArray = generator.DeclareLocal(typeof(string[]));
						LocalBuilder localIndex = generator.DeclareLocal(typeof(int));
						LocalBuilder localCheck = generator.DeclareLocal(typeof(bool)); // false until included

						// save values
						generator.Emit(OpCodes.Stloc, (short)localArray.LocalIndex);
						generator.Emit(OpCodes.Stloc, (short)localValue.LocalIndex);

						// LOOP STARTS HERE

						// load the array element and compare
						generator.Emit(OpCodes.Ldloc, (short)localValue.LocalIndex); // 3
						generator.Emit(OpCodes.Ldloc, (short)localArray.LocalIndex); // 3
						generator.Emit(OpCodes.Ldloc, (short)localIndex.LocalIndex); // 3
						generator.Emit(OpCodes.Ldelem_Ref); // 1
						generator.Emit(OpCodes.Call, methodCompare); // 5

						// store True if equal (True)
						generator.Emit(OpCodes.Brfalse_S, (byte)5); // 2
						generator.Emit(OpCodes.Ldc_I4_1); // 1
						generator.Emit(OpCodes.Stloc, (short)localCheck.LocalIndex); // 3

						// increase the position
						generator.Emit(OpCodes.Ldloc, (short)localIndex.LocalIndex); // 3
						generator.Emit(OpCodes.Ldc_I4_1); // 1
						generator.Emit(OpCodes.Add); // 1
						generator.Emit(OpCodes.Stloc, (short)localIndex.LocalIndex); // 3

						// go to start of loop 
						generator.Emit(OpCodes.Ldloc, (short)localIndex.LocalIndex); // 3
						generator.Emit(OpCodes.Ldloc, (short)localArray.LocalIndex); // 3
						generator.Emit(OpCodes.Ldlen); // 1
						generator.Emit(OpCodes.Blt_S, (sbyte)-46);

						// return the flag
						generator.Emit(OpCodes.Ldloc, (short)localCheck.LocalIndex);
					}
					break;

				case DocOpCode.NoOperation: // backcompat
				case DocOpCode.CompareEqual:
					{
						// strings may not necessarily be interned, so can't use comparison opcode -- must call Object.Compare (which checks for null conditions, calls virtual Compare specific to string)
						MethodInfo methodCompare = typeof(Object).GetMethod("Equals", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(Object), typeof(Object) }, null);
						generator.Emit(OpCodes.Call, methodCompare);
					}
					break;

				case DocOpCode.CompareNotEqual:
					{
						MethodInfo methodCompare = typeof(Object).GetMethod("Equals", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(Object), typeof(Object) }, null);
						generator.Emit(OpCodes.Call, methodCompare); // 0 means they are equal, so non-zero means not equal
						generator.Emit(OpCodes.Not); // 0 means they are equal, so flip result
					}
					break;

				case DocOpCode.CompareGreaterThan:
					{
						MethodInfo methodCompare = typeof(IComparable).GetMethod("CompareTo", new Type[] { typeof(Object) });
						generator.Emit(OpCodes.Castclass, typeof(IComparable));
						generator.Emit(OpCodes.Callvirt, methodCompare);
						generator.Emit(OpCodes.Ldc_I4_0);
						generator.Emit(OpCodes.Cgt);
					}
					break;

				case DocOpCode.CompareLessThan:
					{
						MethodInfo methodCompare = typeof(IComparable).GetMethod("CompareTo", new Type[] { typeof(Object) });
						generator.Emit(OpCodes.Castclass, typeof(IComparable));
						generator.Emit(OpCodes.Callvirt, methodCompare);
						generator.Emit(OpCodes.Ldc_I4_0);
						generator.Emit(OpCodes.Clt);
					}
					break;

				case DocOpCode.CompareGreaterThanOrEqual:
					{
						MethodInfo methodCompare = typeof(IComparable).GetMethod("CompareTo", new Type[] { typeof(Object) });
						generator.Emit(OpCodes.Castclass, typeof(IComparable));
						generator.Emit(OpCodes.Callvirt, methodCompare);
						generator.Emit(OpCodes.Ldc_I4_0);
					}
					generator.Emit(OpCodes.Bge_S, (byte)3);
					generator.Emit(OpCodes.Ldc_I4_0);
					generator.Emit(OpCodes.Br_S, (byte)1);
					generator.Emit(OpCodes.Ldc_I4_1);
					break;

				case DocOpCode.CompareLessThanOrEqual:
					{
						MethodInfo methodCompare = typeof(IComparable).GetMethod("CompareTo", new Type[] { typeof(Object) });
						generator.Emit(OpCodes.Castclass, typeof(IComparable));
						generator.Emit(OpCodes.Callvirt, methodCompare);
						generator.Emit(OpCodes.Ldc_I4_0);
					}
					generator.Emit(OpCodes.Ble_S, (byte)3);
					generator.Emit(OpCodes.Ldc_I4_0);
					generator.Emit(OpCodes.Br_S, (byte)1);
					generator.Emit(OpCodes.Ldc_I4_1);
					break;
			}

			return null;
		}
	}

	public class DocOpLogical : DocOpExpression // and|or|xor
	{
		[DataMember(Order = 0)] [XmlElement] public DocOpExpression ExpressionA { get; set; }
		[DataMember(Order = 1)] [XmlElement] public DocOpExpression ExpressionB { get; set; }

		internal override object Eval(object o, Hashtable population, DocTemplateDefinition template, Type valuetype, List<int> indexpath)
		{
			object valueA = this.ExpressionA.Eval(o, population, template, null, null);
			object valueB = this.ExpressionB.Eval(o, population, template, null, null);
			if (valueA is bool && valueB is bool)
			{
				bool resultA = (bool)valueA;
				bool resultB = (bool)valueB;
				bool result = false;

				switch (this.Operation)
				{
					case DocOpCode.And:
						result = (resultA && resultB);
						break;

					case DocOpCode.Or:
						result = (resultA || resultB);
						break;

					case DocOpCode.Xor:
						result = (resultA ^ resultB);
						break;
				}
				return result;
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Generates CIL code for operation
		/// </summary>
		/// <param name="writer"></param>
		internal override LocalBuilder Emit(
			Compiler context,
			ILGenerator generator,
			DocTemplateDefinition dtd,
			Type valuetype)
		{
			this.ExpressionA.Emit(context, generator, dtd, null);
			this.ExpressionB.Emit(context, generator, dtd, null);

			switch (this.Operation)
			{
				case DocOpCode.And:
					generator.Emit(OpCodes.And);
					break;

				case DocOpCode.Or:
					generator.Emit(OpCodes.Or);
					break;

				case DocOpCode.Xor:
					generator.Emit(OpCodes.Xor);
					break;

			}

			return null;
		}

		public override string ToString(DocTemplateDefinition template)
		{
			if (this.ExpressionA == null || this.ExpressionB == null)
				return String.Empty;

			string ea = this.ExpressionA.ToString(template);
			string eb = this.ExpressionB.ToString(template);
			string op = this.Operation.ToString().ToUpper();

			string expr = "";

			expr = "(" + AssignRuleIDToExpression(this.ExpressionA, template) + " " + this.Operation.ToString().ToUpper() + " " + AssignRuleIDToExpression(this.ExpressionB, template) + ")";

			//if (this.ExpressionA is DocOpLogical)
			//{
			//    expr += "(" + this.ExpressionA.ToString(template) + " " + this.Operation.ToString().ToUpper() + " " + AssignRuleIDToExpression(this.ExpressionB, template) + ")";
			//}
			//else if (this.ExpressionB is DocOpLogical)
			//{
			//    expr += "(" + AssignRuleIDToExpression(this.ExpressionA, template) + " " + this.Operation.ToString().ToUpper() + " " + this.ExpressionB.ToString(template) + ")";
			//}
			//else
			//{
			//    string exprA = AssignRuleIDToExpression(this.ExpressionA);
			//    int bracketA = exprA.IndexOf('[');

			//    string exprB = AssignRuleIDToExpression(this.ExpressionB);
			//    int bracketB = exprB.IndexOf('[');

			//    expr += "(" + exprA + " " + this.Operation.ToString().ToUpper() + " " + exprB + ")";
			//}

			return expr;
		}

		private static string AssignRuleIDToExpression(DocOpExpression expr, DocTemplateDefinition template)
		{
			string exprRuleID = expr.ToString(template);
			int bracket = exprRuleID.IndexOf('[');

			if (expr is DocOpStatement)
			{
				DocOpStatement statement = (DocOpStatement)expr;
				exprRuleID = statement.Reference.EntityRule.ParentRule.Identification + exprRuleID.Substring(bracket);
			}

			return exprRuleID;
		}

		private string NestedString(DocTemplateDefinition template, string opExpression)
		{

			return base.ToString();
		}
	}
	/// <summary>
	/// A value which is either a literal or a variable
	/// </summary>
	public abstract class DocOpValue : DocOp
	{
	}

	/// <summary>
	/// A parameter passed to the template
	/// </summary>
	public class DocOpParameter : DocOpValue // ldarg
	{
		[DataMember(Order = 0)] [XmlElement] public DocModelRuleAttribute AttributeRule { get; set; }

		public override string ToString()
		{
			return "#" + this.AttributeRule.Identification;
		}

		internal override LocalBuilder Emit(Compiler context, ILGenerator generator, DocTemplateDefinition dtd, Type valuetype)
		{
			// determine the index of the parameter
			DocModelRule[] rules = dtd.GetParameterRules();
			for (int i = 0; i < rules.Length; i++)
			{
				if (rules[i] == this.AttributeRule)
				{
					// found it
					switch (i)
					{
						case 0:
							generator.Emit(OpCodes.Ldarg_1);
							break;

						case 1:
							generator.Emit(OpCodes.Ldarg_2);
							break;

						case 2:
							generator.Emit(OpCodes.Ldarg_3);
							break;

						default:
							if (i < 254)
							{
								generator.Emit(OpCodes.Ldarg_S, (byte)(i + 1));
							}
							else
							{
								generator.Emit(OpCodes.Ldarg, i + 1);
							}
							break;
					}
					return null;
				}
			}

			generator.Emit(OpCodes.Ldnull); // should never get here
			return null;
		}
	}

	/// <summary>
	/// A variable -- currently limited to a field on an object (no local variables or arguments currently)
	/// </summary>
	public class DocOpReference : DocOpValue // ldfld|ldlen
	{
		[DataMember(Order = 0)] [XmlElement] public DocModelRuleEntity EntityRule { get; set; }

		internal override object Eval(object o, Hashtable population, DocTemplateDefinition template, Type valuetype, List<int> indexpath)
		{
			DocModelRule[] rulepath = template.BuildRulePath(this.EntityRule);
			object[] valuepath = new object[rulepath.Length];

			if (indexpath == null)
			{
				// local copy only; otherwise the caller's list is modified
				indexpath = new List<int>();
			}

			// ensure count matches
			while (indexpath.Count < rulepath.Length)
			{
				indexpath.Add(0);
			}

			object value = o;
			if (value == null)
				return null;

			for (int iRuleLevel = 0; iRuleLevel < rulepath.Length; iRuleLevel++)
			{
				DocModelRule rule = rulepath[iRuleLevel];
				if (valuepath[iRuleLevel] != null)
				{
					value = valuepath[iRuleLevel];
				}
				else
				{
					valuepath[iRuleLevel] = value;
				}

				Type type = value.GetType();
				if (rule is DocModelRuleAttribute)
				{
					FieldInfo field = type.GetField(rule.Name);
					if (field == null)
					{
						// bail
						return null;
					}

					value = field.GetValue(value);
					if (value == null)
						return null;

					// if field is collection, then get first element of the specified type
					int listindex = indexpath[iRuleLevel];
					if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(List<>))
					{
						IList list = (IList)value;
						if (list.Count == 0)
							return null;

						if (listindex >= list.Count)
							return null; // no matches along this path

						// dereference at position of list
						value = list[listindex];
					}
					else
					{
						if (listindex > 0)
							return null; // only one position for scalar reference

						if (field.FieldType.IsValueType && field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(Nullable<>))
						{
							// drill in to underlying value
							MethodInfo methodGetValue = field.FieldType.GetMethod("get_Value");
							value = methodGetValue.Invoke(value, null);
						}
					}
					indexpath[iRuleLevel]++;
				}
				else if (rule is DocModelRuleEntity)
				{
					bool isinstance = false;
					Type typeCompare = type;
					while (typeCompare != null)
					{
						if (typeCompare.Name == rule.Name)
						{
							isinstance = true;
							break;
						}

						typeCompare = typeCompare.BaseType;
					}

					if (!isinstance)
					{
						// back up -- if list, then perhaps next instance may match
						valuepath[iRuleLevel] = null;
						iRuleLevel--;
						iRuleLevel--;
					}
					else if (type.IsEnum)
					{
						FieldInfo fieldValue = type.GetField(value.ToString(), BindingFlags.Static | BindingFlags.Public);
						value = fieldValue.GetRawConstantValue();
					}
					else if (!type.IsPrimitive && type.IsValueType) // EXPRESS ENUM or TYPE
					{
						// structure - all compiled structures for EXPRESS TYPE's have single field called Value holding the underlying type
						//FieldInfo fieldValue = type.GetField("Value");
						FieldInfo fieldValue = type.GetFields()[0];
						value = fieldValue.GetValue(value);
					}
				}
			}

			return value;
		}

		internal override LocalBuilder Emit(
			Compiler compiler,
			ILGenerator generator,
			DocTemplateDefinition template,
			Type valuetype)
		{
			// ldarg.0 (this)
			// ldfld + token (calculated from attribute reference) -- recursively until final attribute
			// {leelem} -- for specific indices 

			Type type = compiler.RegisterType(template.Type);
			generator.Emit(OpCodes.Ldarg_0); // this

			Label labelBail = generator.DefineLabel();

			DocModelRule[] rulepath = template.BuildRulePath(this.EntityRule);
			foreach (DocModelRule rule in rulepath)
			{
				if (rule is DocModelRuleAttribute)
				{
					FieldInfo field = compiler.RegisterField(type, rule.Name);
					if (field == null)
					{
						// bail
						return null;
					}
					generator.Emit(OpCodes.Castclass, type);

					if (field.FieldType.IsValueType && field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(Nullable<>))
					{
						MethodInfo methodHasValue = field.FieldType.GetMethod("get_HasValue");
						MethodInfo methodGetValue = field.FieldType.GetMethod("get_Value");

						generator.Emit(OpCodes.Dup); // 2

						generator.Emit(OpCodes.Ldflda, field); // field address
						generator.Emit(OpCodes.Call, methodHasValue);
						generator.Emit(OpCodes.Brfalse, labelBail);

						generator.Emit(OpCodes.Ldflda, field); // field address
						generator.Emit(OpCodes.Call, methodGetValue);
					}
					else
					{
						generator.Emit(OpCodes.Ldfld, field); // field
						if (!field.FieldType.IsValueType)
						{
							generator.Emit(OpCodes.Dup);
							generator.Emit(OpCodes.Brfalse, labelBail);
						}


						// if field is collection, then get last element
						if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(List<>))
						{
							// iterate
							LocalBuilder localList = generator.DeclareLocal(field.FieldType);
							LocalBuilder localElem = generator.DeclareLocal(field.FieldType.GetGenericArguments()[0]);
							generator.Emit(OpCodes.Stloc, (short)localList.LocalIndex);

							// get the length of the list -- go in reverse order to avoid having to check or store length each iteration
							MethodInfo methodListLength = typeof(ICollection).GetProperty("Count").GetGetMethod();
							generator.Emit(OpCodes.Ldloc, (short)localList.LocalIndex);
							generator.Emit(OpCodes.Callvirt, methodListLength);

							// if 0 length, then return
							Label labelAfter = generator.DefineLabel();
							generator.Emit(OpCodes.Brtrue, labelAfter); // if length != 0 then keep going
							generator.Emit(OpCodes.Ldnull); // push null onto stack
							generator.Emit(OpCodes.Br, labelBail); // skip any more indirections
							generator.MarkLabel(labelAfter);

							// get the first element of the collection -- FUTURE: allow index to be specified
							generator.Emit(OpCodes.Ldloc, (short)localList.LocalIndex);
							generator.Emit(OpCodes.Ldc_I4_0); // first element

							// get the element
							MethodInfo methodListGet = ((PropertyInfo)typeof(IList).GetDefaultMembers()[0]).GetGetMethod();
							generator.Emit(OpCodes.Callvirt, methodListGet);
							if (localElem.LocalType.IsValueType)
							{
								// e.g. IfcCartesianPoint.Coordinates[1] : IfcLengthMeasure
								generator.Emit(OpCodes.Unbox, localElem.LocalType);
								generator.Emit(OpCodes.Ldobj, localElem.LocalType);//Ldind_R8); // assume 64-bit floating point (float) .. TBD: check for integer
							}
							else
							{
								generator.Emit(OpCodes.Castclass, localElem.LocalType);
							}
							generator.Emit(OpCodes.Stloc, (short)localElem.LocalIndex);

							// then load the element again
							generator.Emit(OpCodes.Ldloc, (short)localElem.LocalIndex);
						}
					}
				}
				else if (rule is DocModelRuleEntity)
				{
					type = compiler.RegisterType(rule.Name);

					if (type.IsEnum || type.IsPrimitive)
					{
						// box it
						generator.Emit(OpCodes.Box, type); // needed for Object.Compare
					}
					else if (type.IsValueType)
					{
						// duplicate it to retain object on stack after instance check (otherwise null)
						//generator.Emit(OpCodes.Dup);
						//generator.Emit(OpCodes.Brfalse, labelBail);

						FieldInfo fieldValue = compiler.RegisterField(type, "Value");
						generator.Emit(OpCodes.Ldfld, fieldValue);
						generator.Emit(OpCodes.Box, fieldValue.FieldType); // needed for Object.Compare
					}
					else
					{
						// check if instance of expected type
						generator.Emit(OpCodes.Isinst, type); // object -> object (if instance) or null if not

						// duplicate it to retain object on stack after instance check (otherwise null)
						generator.Emit(OpCodes.Dup);
						generator.Emit(OpCodes.Brfalse, labelBail);

						// otherwise, stack contains object at this point
					}
				}
			}

			generator.MarkLabel(labelBail); // stack should have exactly one element here -- either the resolved value or NULL if any attribute/entity didn't resolve along the way

			return null;
		}

		public override string ToString(DocTemplateDefinition template)
		{
			if (template == null)
				return this.ToString();

			DocModelRule[] rulepath = template.BuildRulePath(this.EntityRule);
			StringBuilder sb = new StringBuilder();
			sb.Append(template.Type);
			foreach (DocModelRule rule in rulepath)
			{
				if (rule is DocModelRuleAttribute)
				{
					sb.Append(".");
				}
				else if (rule is DocModelRuleEntity)
				{
					sb.Append("\\");
				}
				sb.Append(rule.Name);
			}

			return sb.ToString();
		}

		public override string ToString()
		{
			if (this.EntityRule != null)
			{
				return this.EntityRule.ToString();//... convert to path...
			}
			else
			{
				return "Value";
			}
		}
	}

	public class DocOpLiteral : DocOpValue // ldstr|ldc.i8|ldc.r8
	{
		[DataMember(Order = 0)] [XmlAttribute] public string Literal { get; set; }

		internal override object Eval(object o, Hashtable population, DocTemplateDefinition template, Type valuetype, List<int> indexpath)
		{
			if (valuetype != null && valuetype.IsEnum)
			{
				FieldInfo field = valuetype.GetField(this.Literal, BindingFlags.Public | BindingFlags.Static);
				if (field != null)
				{
					return field.GetRawConstantValue();
					//return field.GetValue(null); // underlying integer value
				}
				else
				{
					return null;
				}
			}
			else if (valuetype != null && valuetype.IsValueType && valuetype.GetFields()[0].FieldType == typeof(double))
			{
				Double literal = 0.0;
				Double.TryParse(this.Literal, out literal);
				return literal;
			}
			else if (valuetype != null && valuetype.IsValueType && valuetype.GetFields()[0].FieldType == typeof(long))
			{
				Int64 literal = 0L;
				Int64.TryParse(this.Literal, out literal);
				return literal;
			}
			else if (valuetype != null && valuetype.IsValueType && valuetype.GetFields()[0].FieldType == typeof(bool))
			{
				Boolean literal = false;
				Boolean.TryParse(this.Literal, out literal);
				return literal;
			}
			else
			{
				return this.Literal;
			}
		}

		internal override LocalBuilder Emit(
			Compiler compiler,
			ILGenerator generator,
			DocTemplateDefinition dtd,
			Type valuetype)
		{
			// + constant or string metadata token

			if (valuetype != null && valuetype.IsValueType && valuetype.GetFields()[0].FieldType == typeof(double))
			{
				Double literal = 0.0;
				Double.TryParse(this.Literal, out literal);
				generator.Emit(OpCodes.Ldc_R8, literal);
				generator.Emit(OpCodes.Box, typeof(Double)); // needed for Object.Compare
			}
			else if (valuetype != null && valuetype.IsValueType && valuetype.GetFields()[0].FieldType == typeof(long))
			{
				Int64 literal = 0L;
				Int64.TryParse(this.Literal, out literal);
				generator.Emit(OpCodes.Ldc_I8, literal);
				generator.Emit(OpCodes.Box, typeof(Int64)); // needed for Object.Compare
			}
			else if (valuetype != null && valuetype.IsValueType && valuetype.GetFields()[0].FieldType == typeof(bool))
			{
				Boolean literal = false;
				Boolean.TryParse(this.Literal, out literal);
				generator.Emit(OpCodes.Ldc_I4, literal ? 1 : 0);
				generator.Emit(OpCodes.Box, typeof(Boolean)); // needed for Object.Compare
			}
			else if (this.Literal != null)
			{
				generator.Emit(OpCodes.Ldstr, this.Literal);
			}
			else
			{
				generator.Emit(OpCodes.Ldnull);
			}

			return null;
		}

		public override string ToString()
		{
			if (String.IsNullOrEmpty(this.Literal))
				return "NULL";

			return "'" + this.Literal + "'";
		}
	}

	/// <summary>
	/// Low-level operation corresponding to .NET MSIL code -- enables compilation to machine language for the fastest possible validation for large building files.
	/// </summary>
	public enum DocOpCode
	{
		NoOperation = 0, // no operation (use value on stack)

		CompareEqual = 0xFE01,    // compare equal
		CompareGreaterThan = 0xFE02,
		CompareGreaterThanOrEqual = 0xFE03,
		CompareLessThan = 0xFE04,
		CompareLessThanOrEqual = 0xFE05,
		CompareNotEqual = 0x66FE01,

		And = 0x5F,    // conditional and
		Or = 0x60,
		Xor = 0x61,

		LoadString = 0x72,  // string
		LoadLength = 0x8e, // load array length
		IsInstance = 0x75, // check type

		LoadArgument = 0x0E, // load argument (parameter)
		LoadField = 0x7B, // load field

		IsIncluded = 0xFF0001, // custom op for checking containment in array
		IsUnique = 0xFF0002, // custom op for uniqueness
	}

	public enum DocTemplateOperator
	{
		And = 0, // every row must be true
		Or = 1,  // at least one row must be true
		Not = 2, // at least one row must be false
		Nand = 3,// reverse of AND: every row must be false 
		Nor = 4, // reverse of OR:
		Xor = 5, // only one row must be true
		Nxor = 6,// reverse of XOR: ??
	}

	/// <summary>
	/// Concept (usage of a template)
	/// </summary>
	public class DocTemplateUsage : DocObject // now inherits from DocObject
	{
		[DataMember(Order = 0)] [XmlElement] public DocTemplateDefinition Definition { get; set; } // the template definition to be used for formatting text.
		[DataMember(Order = 1)] [XmlArray] public List<DocTemplateItem> Items { get; protected set; } // items to be listed within use definition (rules)
		[DataMember(Order = 2)] [XmlArray] public List<DocExchangeItem> Exchanges { get; protected set; } // new in 2.5
		//[DataMember(Order = 3)] private DocModelView _ModelView; // new in 2.7, removed on 3.5; determine from ModelView.ConceptRoot.Concepts hierarchy
		[DataMember(Order = 3)] [XmlAttribute] public bool Override { get; set; } // new in 5.0; if true, then any concepts of same template from supertypes are not inherited
		[DataMember(Order = 4)] [XmlAttribute] public bool Suppress { get; set; } // new in 8.2; if true, then concept is disallowed
		[DataMember(Order = 5)] [XmlArray] public List<DocTemplateUsage> Concepts { get; protected set; } // new in 8.6: nested concepts, where only one is required to pass
		[DataMember(Order = 6)] [XmlAttribute] public DocTemplateOperator Operator { get; set; } // new in 9.3

		private bool? _validation; // unserialized; null: no applicable instances; false: one or more failures; true: all pass
		private Dictionary<object, bool> _validateStructure; // 
		private Dictionary<object, bool> _validateConstraints; // 
		private Dictionary<object, Dictionary<DocObject, List<object>>> _validateMismatches;

		public DocTemplateUsage()
		{
			this.Items = new List<DocTemplateItem>();
			this.Exchanges = new List<DocExchangeItem>();
			this.Concepts = new List<DocTemplateUsage>();
		}

		/// <summary>
		/// Returns display name that also captures definition if different.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			string name = this.Name;
			if (this.Definition != null && !String.IsNullOrEmpty(this.Definition.Name))
			{
				if ((String.IsNullOrEmpty(this.Name) || this.Name.Equals(this.Definition.Name)))
				{
					name = this.Definition.Name;
				}
				else
				{
					//  concept name differs
					name = this.Name + " [" + this.Definition.Name + "]";
				}
			}

			return name;
		}

		/// <summary>
		/// Indicates whether latest test passes (true), has one or more failures (false), or no applicable instances (null). Not serialized.
		/// </summary>
		public bool? Validation
		{
			get
			{
				return this._validation;
			}
			set
			{
				this._validation = value;
			}
		}

		public Dictionary<object, bool> ValidationStructure
		{
			get
			{
				if (this._validateStructure == null)
				{
					this._validateStructure = new Dictionary<object, bool>();
				}
				return this._validateStructure;
			}
		}

		public Dictionary<object, bool> ValidationConstraints
		{
			get
			{
				if (this._validateConstraints == null)
				{
					this._validateConstraints = new Dictionary<object, bool>();
				}
				return this._validateConstraints;
			}
		}

		protected internal override void FindQuery(string query, bool searchtext, List<DocFindResult> results)
		{
			base.FindQuery(query, searchtext, results);

			foreach (DocTemplateUsage docSub in this.Concepts)
			{
				docSub.FindQuery(query, searchtext, results);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="o">The root entity being tested</param>
		/// <param name="outer">outer template item or concept root to qualify</param>
		/// <returns></returns>
		public List<object> GetValidationMismatches(object o, DocObject outer)
		{
			if (o == null)
				return null;

			if (this._validateMismatches == null)
			{
				this._validateMismatches = new Dictionary<object, Dictionary<DocObject, List<object>>>();
			}

			Dictionary<DocObject, List<object>> dictionaryItem = null;
			if (!this._validateMismatches.TryGetValue(o, out dictionaryItem))
			{
				dictionaryItem = new Dictionary<DocObject, List<object>>();
				this._validateMismatches.Add(o, dictionaryItem);
			}

			List<object> list = null;
			if (!dictionaryItem.TryGetValue(outer, out list))
			{
				list = new List<object>();
				dictionaryItem.Add(outer, list);
			}

			return list;
		}

		public DocExchangeItem GetExchange(DocExchangeDefinition definition, DocExchangeApplicabilityEnum applicability)
		{
			foreach (DocExchangeItem docExchange in this.Exchanges)
			{
				if (docExchange.Exchange == definition && docExchange.Applicability == applicability)
				{
					return docExchange;
				}
			}

			return null;
		}

		public void RegisterExchange(DocExchangeDefinition docExchange, DocExchangeRequirementEnum requirement)
		{
			DocExchangeItem docIm = null;
			DocExchangeItem docEx = null;
			foreach (DocExchangeItem eachEx in this.Exchanges)
			{
				if (eachEx.Exchange == docExchange)
				{
					if (eachEx.Applicability == DocExchangeApplicabilityEnum.Export)
					{
						docEx = eachEx;
					}
					else if (eachEx.Applicability == DocExchangeApplicabilityEnum.Import)
					{
						docIm = eachEx;
					}
				}
			}

			if (docEx == null)
			{
				docEx = new DocExchangeItem();
				this.Exchanges.Add(docEx);
				docEx.Exchange = docExchange;
				docEx.Applicability = DocExchangeApplicabilityEnum.Export;
			}

			if (docIm == null)
			{
				docIm = new DocExchangeItem();
				this.Exchanges.Add(docIm);
				docIm.Exchange = docExchange;
				docIm.Applicability = DocExchangeApplicabilityEnum.Import;
			}

			docEx.Requirement = requirement;
			docIm.Requirement = requirement;
		}

		public void RenameParameter(DocTemplateDefinition template, string oldid, string newid)
		{
			if (this.Definition == template)
			{
				foreach (DocTemplateItem docItem in this.Items)
				{
					DocExpression[] expressions = docItem.GetParameterExpressions();
					if (expressions != null)
					{
						for (int i = 0; i < expressions.Length; i++)
						{
							DocExpression expr = expressions[i];
							if (expr.Name == oldid)
							{
								// rename it
								expr.Name = newid;
								docItem.SetParameterExpressions(expressions);
								break;
							}
						}
					}
				}
			}

			// subitems
			foreach (DocTemplateUsage docSub in this.Concepts)
			{
				docSub.RenameParameter(template, oldid, newid);
			}

			foreach (DocTemplateItem docItem in this.Items)
			{
				foreach (DocTemplateUsage docUsage in docItem.Concepts)
				{
					docUsage.RenameParameter(template, oldid, newid);
				}
			}
		}

		public void ResetValidation()
		{
			this.Validation = null;
			this.ValidationStructure.Clear();
			this.ValidationConstraints.Clear();
			this._validateMismatches = null;

			foreach (DocTemplateItem docItem in this.Items)
			{
				docItem.ValidationStructure.Clear();
				docItem.ValidationConstraints.Clear();

				foreach (DocTemplateUsage innerusage in docItem.Concepts)
				{
					innerusage.ResetValidation();
				}
			}

			foreach (DocTemplateUsage docNest in this.Concepts)
			{
				docNest.ResetValidation();
			}
		}

		/// <summary>
		/// Returns test result for object
		/// </summary>
		/// <param name="?"></param>
		/// <returns></returns>
		public bool? GetResultForObject(object o)
		{
			if (o == null)
			{
				return false;
			}

			bool hasstructure = false;
			bool structure = false;
			hasstructure = ValidationStructure.TryGetValue(o, out structure);

			bool hasconstraint = false;
			bool constraint = false;
			hasconstraint = ValidationConstraints.TryGetValue(o, out constraint);

			if ((hasstructure && !structure) || (hasconstraint && !constraint))
				return false;

			int total = 0;
			int pass = 0;
			int fail = 0;

			foreach (DocTemplateItem docItem in this.Items)
			{
				//if (!docItem.Optional)
				{
					bool? innerresult = docItem.GetResultForObject(o);
					if (innerresult != null)
					{
						total++;

						if (innerresult.Value)
						{
							pass++;
						}
						else
						{
							if (!docItem.Optional)
							{
								fail++;
							}
						}
					}
				}
			}

			if (this.Items.Count > 0 && total == 0) // no items meet conditions
			{
				return null; // not applicable
			}

			bool result = false;
			switch (this.Operator)
			{
				case DocTemplateOperator.And:
					result = (pass == total);
					break;

				case DocTemplateOperator.Or:
					result = (pass >= 1 && total >= 1);
					break;

				case DocTemplateOperator.Not:
					result = (pass != total);
					break;

				case DocTemplateOperator.Nand:
					result = (pass != total);
					break;

				case DocTemplateOperator.Nor:
					result = !(pass >= 1 && total >= 1);
					break;

				case DocTemplateOperator.Xor:
					result = (pass == 1 && total > 1);
					break;

				case DocTemplateOperator.Nxor:
					result = !(pass == 1 && total > 1);
					break;
			}

			return result;
		}
	}

	public class DocExchangeItem : SEntity
	{
		[DataMember(Order = 0)] [XmlElement] public DocExchangeDefinition Exchange { get; set; } // 2.7: type changed from DocAnnotation
		[DataMember(Order = 1)] [XmlAttribute] public DocExchangeApplicabilityEnum Applicability { get; set; }
		[DataMember(Order = 2)] [XmlAttribute] public DocExchangeRequirementEnum Requirement { get; set; }
	}

	public enum DocExchangeApplicabilityEnum
	{
		Export = 1,
		Import = 2,
	}

	public enum DocExchangeRequirementEnum
	{
		Mandatory = 1,
		Optional = 2, // "Recommended"
		NotRelevant = 3,
		NotRecommended = 4,
		Excluded = 5,
	}

	public enum DocMetricEnum
	{
		Value = 0,
		Size = 1,
		Type = 2,
		Unique = 3,
		Exists = 4,
	}

	public enum DocOperatorEnum
	{
		EQUAL = 0,
		NOT_EQUAL = 1,
		GREATER_THAN = 2,
		GREATER_THAN_OR_EQUAL = 3,
		LESS_THAN = 4,
		LESS_THAN_OR_EQUAL = 5,
	}

	public class DocExpression //: SEntity
	{
		[DataMember(Order = 0)] [XmlAttribute] public string Name { get; set; } 
		[DataMember(Order = 1)] [XmlAttribute] public DocMetricEnum Metric { get; set; }
		[DataMember(Order = 2)] [XmlAttribute] public DocOperatorEnum Operator { get; set; }
		[DataMember(Order = 3)] [XmlAttribute] public string Value { get; set; }

		/// <summary>
		/// Returns string compatible with mvdXML grammar
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(this.Name);
			sb.Append("[");
			sb.Append(Enum.GetName(typeof(DocMetricEnum), this.Metric));
			sb.Append("]");
			switch (this.Operator)
			{
				case DocOperatorEnum.EQUAL:
					sb.Append("=");
					break;
				case DocOperatorEnum.NOT_EQUAL:
					sb.Append("!=");
					break;
				case DocOperatorEnum.GREATER_THAN:
					sb.Append("?");
					break;
				case DocOperatorEnum.GREATER_THAN_OR_EQUAL:
					sb.Append(">=");
					break;
				case DocOperatorEnum.LESS_THAN:
					sb.Append("<");
					break;
				case DocOperatorEnum.LESS_THAN_OR_EQUAL:
					sb.Append("<=");
					break;
			}
			sb.Append("'");
			sb.Append(this.Value);
			sb.Append("'");

			return sb.ToString();
		}
	}

	// color-coding: 
	// Color    | Name    | Optional  Key______ Reference_ System___
	// 0xFF0000 | Red     | ?         T         F          F          // public key for uniquely identifying (separate from any system keys)
	// 0x       | Orange  | ?         ?         T          F          // required input from constrained list
	// 0x       | Yellow  | F         F         F          F          // required input
	// 0x       | Green   | T         F         F          F          // optional input
	// 0x       | Blue    | T         ?         ?          T          // system calculation (e.g. Energy Analysis)
	// 0x       | Purple  | F         ?         ?          T          // system mapping     (e.g. GUID)
	public class DocTemplateItem : DocObject // now inherits from DocObject
	{
		[DataMember(Order = 0)] [XmlArray] public List<DocTemplateUsage> Concepts { get; protected set; }// IfcDoc 6.3: for parameters consisting of lists of objects -- translates to nested concepts in mvdXML
		[DataMember(Order = 1)] [XmlAttribute] public bool Optional { get; set; } // IfcDoc 8.7: indicate whether item is optional
		[DataMember(Order = 2)] [XmlAttribute] public bool Reference { get; set; } // IfcDoc 11.2 (changed from obsolete string): item is constrained by referenced objects or value list
		[DataMember(Order = 3)] [XmlAttribute] public bool Key { get; set; } // IfcDoc 11.2 (changed from obsolete string): item is used as primary key
		[DataMember(Order = 4)] [XmlAttribute] public bool Calculated { get; set; } // IfcDoc 11.2 (changed from obsolete string): item is managed by system - should not be touched by user
		[DataMember(Order = 5)] [XmlAttribute] public string RuleInstanceID { get; set; } // IfcDoc 2.5: id of the entity rule to instantiate for each item
		[DataMember(Order = 6)] [XmlAttribute] public string RuleParameters { get; set; } // IfcDoc 2.5: parameters and constraints to substitute into the rule
		[DataMember(Order = 7), XmlAttribute, Required] public int Order { get; set; } // IfcDoc 11.6
		[DataMember(Order = 8)] [XmlArray] public List<DocExchangeItem> Exchanges { get; protected set; } // IfcDoc 11.6: override requirements for individual item 
		// new in 8.5
		private Dictionary<object, bool> _validateStructure; // 
		private Dictionary<object, bool> _validateConstraints; // 

		public DocTemplateItem()
		{
			this.Concepts = new List<DocTemplateUsage>();
			this.Exchanges = new List<DocExchangeItem>();
		}

		public System.Drawing.Color GetColor()
		{
			if (this.Calculated)
			{
				if (this.Optional)
				{
					return System.Drawing.Color.FromArgb(0xFF, 0xCC, 0xCC, 0xFF); //"Calculated";
				}
				else
				{
					return System.Drawing.Color.FromArgb(0xFF, 0xCC, 0x99, 0xFF); //"System";
				}
			}
			else
			{
				if (this.Key)
				{
					return System.Drawing.Color.FromArgb(0xFF, 0xFF, 0x00, 0x00); //"Key";
				}
				else if (this.Reference)
				{
					return System.Drawing.Color.FromArgb(0xFF, 0xFF, 0xCC, 0x99); //"Reference";
				}
				else if (this.Optional)
				{
					return System.Drawing.Color.FromArgb(0xFF, 0xCC, 0xFF, 0xCC); //"Optional";
				}
			}

			return System.Drawing.Color.FromArgb(0xFF, 0xFF, 0xFF, 0x99); //"Required";
		}

		/// <summary>
		/// Returns usage according to flags using human-readable subset
		/// </summary>
		/// <returns></returns>
		public string GetUsage()
		{
			if (this.Calculated)
			{
				if (this.Optional)
				{
					return "Calculated";
				}
				else
				{
					return "System";
				}
			}
			else
			{
				if (this.Key)
				{
					return "Key";
				}
				else if (this.Reference)
				{
					return "Reference";
				}
				else if (this.Optional)
				{
					return "Optional";
				}
			}

			return "Required";
		}

		public void SetUsage(string usage)
		{
			switch (usage)
			{
				case "Key":
					this.Optional = false;
					this.Key = true;
					this.Reference = false;
					this.Calculated = false;
					break;

				case "Reference":
					this.Optional = false;
					this.Key = false;
					this.Reference = true;
					this.Calculated = false;
					break;

				case "Required":
					this.Optional = false;
					this.Key = false;
					this.Reference = false;
					this.Calculated = false;
					break;

				case "Optional":
					this.Optional = true;
					this.Key = false;
					this.Reference = false;
					this.Calculated = false;
					break;

				case "Calculated":
					this.Optional = true;
					this.Key = false;
					this.Reference = false;
					this.Calculated = true;
					break;

				case "System":
					this.Optional = false;
					this.Key = false;
					this.Reference = false;
					this.Calculated = true;
					break;
			}
		}

		public DocTemplateUsage GetParameterConcept(string parameter, DocTemplateDefinition def)
		{
			foreach (DocTemplateUsage docEachUsage in this.Concepts)
			{
				if (docEachUsage.Name.Equals(parameter) && (def == null || docEachUsage.Definition == def))
				{
					return docEachUsage;
				}
			}

			return null;
		}

		public List<DocTemplateUsage> GetParameterConcepts(DocTemplateDefinition def)
		{
			List<DocTemplateUsage> docEachUsage = new List<DocTemplateUsage>();
			foreach (DocTemplateUsage docUsage in this.Concepts)
			{
				if (def == null || docUsage.Definition == def)
				{
					docEachUsage.Add(docUsage);
				}
			}

			return docEachUsage;
		}

		public DocTemplateUsage RegisterParameterConcept(string parameter, DocTemplateDefinition def)
		{
			DocTemplateUsage docUsage = this.GetParameterConcept(parameter, def);
			if (docUsage == null)
			{
				docUsage = new DocTemplateUsage();
				docUsage.Name = parameter;
				docUsage.Definition = def;
				this.Concepts.Add(docUsage);
			}
			return docUsage;
		}

		public List<DocTemplateUsage> RegisterParameterConcepts(DocTemplateDefinition def)
		{
			List<DocTemplateUsage> docUsages = this.GetParameterConcepts(def);
			if (docUsages == null)
			{
				return null;
			}
			else
			{
				return docUsages;
			}
		}

		/// <summary>
		/// Returns parameter string as array of data structures
		/// </summary>
		/// <returns></returns>
		public DocExpression[] GetParameterExpressions()
		{
			if (this.RuleParameters == null)
				return null;

			string[] parms = this.RuleParameters.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries); // FUTURE: need more complex parsing such as to support embedding semicolons into values
			DocExpression[] expr = new DocExpression[parms.Length];
			for (int i = 0; i < parms.Length; i++)
			{
				expr[i] = new DocExpression();

				// for now, only equals supported
				string[] args = parms[i].Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
				if (args.Length == 2)
				{
					string[] nameparts = args[0].Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
					if (nameparts.Length == 2)
					{
						expr[i].Name = nameparts[1];
					}
					else
					{
						expr[i].Name = args[0];
					}

					//...
					if (args[1].Length > 3 && args[1].Substring(0, 3) == "Ifc")
					{
						expr[i].Metric = DocMetricEnum.Type;
					}
					else
					{
						expr[i].Metric = DocMetricEnum.Value;
					}

					expr[i].Operator = DocOperatorEnum.EQUAL;
					expr[i].Value = args[1];
				}
			}

			return expr;
		}

		public void SetParameterExpressions(DocExpression[] expressions)
		{
			StringBuilder sb = new StringBuilder();
			foreach (DocExpression exp in expressions)
			{
				if (!String.IsNullOrEmpty(exp.Name)) // null if deleted
				{
					sb.Append(exp.Name);
					sb.Append("=");
					sb.Append(exp.Value);
					sb.Append(";");
				}
			}
			this.RuleParameters = sb.ToString();
		}

		/// <summary>
		/// Returns mvdXML-formatted expression for all parameters
		/// </summary>
		/// <returns></returns>
		public string FormatParameterExpressions(DocTemplateDefinition template, DocProject docProject, Dictionary<string, DocObject> map)
		{
			StringBuilder sb = new StringBuilder();
			DocExpression[] exprs = this.GetParameterExpressions();
			if (exprs == null)
				return null;

			string[] parmnames = template.GetParameterNames();

			for (int i = 0; i < exprs.Length; i++)
			{
				DocExpression exp = exprs[i];
				for (int j = 0; j < parmnames.Length; j++)
				{
					if (parmnames[j].Equals(exp.Name))
					{
						if (sb.Length > 0)
						{
							sb.Append(" AND ");
						}

						DocDefinition docAttrType = template.GetParameterType(exp.Name, map);
						if (docAttrType is DocEntity)
						{
							exp.Metric = DocMetricEnum.Type;
						}
						/*
                        DocDefinition docDef = docProject.GetDefinition(template.Type);
                        if (docDef is DocEntity)
                        {
                            DocEntity docEnt = (DocEntity)docDef;
                            //... get attribute...
                            DocAttribute docAttr = docEnt.ResolveAttribute(exp., docProject);
                            if(docAttr  != null)
                            {
                                DocDefinition docAttrType = docProject.GetDefinition(docAttr.DefinedType);
                                if(docAttrType != null)
                                {
                                    exp.Metric = DocMetricEnum.Type;
                                }
                            }
                        }*/
						// force 
						//template.GetParameterType(paramnames[j], )

						sb.Append(exp.ToString());
					}
				}
			}
			return sb.ToString();
		}

		/// <summary>
		/// Parses mvdXML-formatted expresion for all parameters
		/// </summary>
		/// <param name="encoding"></param>
		public void ParseParameterExpressions(string encoding)
		{
			string[] rules = encoding.Split(new string[] { "AND" }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string rule in  rules)
			{
				string parsedRule = Regex.Replace(rule, @"\s+", "");
				parsedRule = Regex.Replace(parsedRule, @"\[.*\]", "");
				parsedRule = parsedRule.Replace("\'", "");

				this.RuleParameters += parsedRule + ";";
			}
		}

		public string GetParameterValue(string key)
		{
			if (this.RuleParameters == null)
				return null;

			string[] parms = this.RuleParameters.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string parm in parms)
			{
				// for now, only equals supported
				string[] args = parm.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
				if (args.Length == 2)
				{
					string[] nameparts = args[0].Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
					string fieldname = null;
					if (nameparts.Length == 2)
					{
						fieldname = nameparts[1];
					}
					else
					{
						fieldname = args[0];
					}

					if (fieldname.Equals(key))
					{
						return args[1];
					}
				}
			}

			return null; // no such parameters
		}

		public Dictionary<object, bool> ValidationStructure
		{
			get
			{
				if (this._validateStructure == null)
				{
					this._validateStructure = new Dictionary<object, bool>();
				}
				return this._validateStructure;
			}
		}

		public Dictionary<object, bool> ValidationConstraints
		{
			get
			{
				if (this._validateConstraints == null)
				{
					this._validateConstraints = new Dictionary<object, bool>();
				}
				return this._validateConstraints;
			}

		}

		/// <summary>
		/// Returns test result for object
		/// </summary>
		/// <param name="?"></param>
		/// <returns></returns>
		public bool? GetResultForObject(object o)
		{
			//if (this.Optional)
			//    return null;

			bool hasstructure = false;
			bool structure = false;
			hasstructure = ValidationStructure.TryGetValue(o, out structure);
			if (!hasstructure)
				return null; // condition not met (V9.5)

			bool hasconstraint = false;
			bool constraint = false;
			hasconstraint = ValidationConstraints.TryGetValue(o, out constraint);

			if ((hasstructure && !structure) || (hasconstraint && !constraint))
			{
				if (this.Optional)
					return null;

				return false;
			}

			// check nested
			bool alltrue = true;
			foreach (DocTemplateUsage docUsage in this.Concepts)
			{
				bool? innerresult = docUsage.GetResultForObject(o);
				if (innerresult == null || !innerresult.Value)
					alltrue = false;
			}

			if (alltrue && hasstructure)
				return true;

			if (this.Optional)
				return null;

			return false;
		}



	}

	/// <summary>
	/// Represents a top-level documentation clause such as "Core Layer", "Extension Layer", "Domain Layer", or "Resource Layer"
	/// </summary>
	public class DocSection : DocObject
	{
		[DataMember(Order = 0)] [XmlArray] public List<DocAnnotation> Annotations { get; protected set; } // v1.8 inserted  TBD - use MVD-XML concept instead
		[DataMember(Order = 1)] [XmlArray] [XmlArrayItem(NestingLevel = 1)] public List<DocSchema> Schemas { get; protected set; }

		public DocSection() : this(null)
		{
		}

		public DocSection(string name)
		{
			this.Name = name;
			this.Annotations = new List<DocAnnotation>();
			this.Schemas = new List<DocSchema>();
		}

		/// <summary>
		/// Sorts entity list according to alphabetical name
		/// </summary>
		public void SortSection()
		{
			SortedList<string, DocSchema> sortEntity = new SortedList<string, DocSchema>();

			foreach (DocSchema docSchema in this.Schemas)
			{
				sortEntity.Add(docSchema.Name, docSchema);
				docSchema.SortSchema();
			}

			this.Schemas.Clear();
			this.Schemas.AddRange(sortEntity.Values);
		}
	}

	/// <summary>
	/// Represents a top-level documentation annex
	/// </summary>
	public class DocAnnex : DocObject
	{
		public DocAnnex()
		{
		}

		public DocAnnex(string name)
		{
			this.Name = name;
		}
	}

	/// <summary>
	/// Represents a generic definition
	/// </summary>
	public class DocAnnotation : DocObject
	{
		[DataMember(Order = 0)] [XmlArray] public List<DocAnnotation> Annotations { get; protected set; }

		public DocAnnotation() : this(null)
		{
		}

		public DocAnnotation(string name)
		{
			this.Name = name;
			this.Annotations = new List<DocAnnotation>();
		}
	}

	/// <summary>
	/// A reference which may be normative or non-normative (in bibliography)
	/// </summary>
	public class DocReference : DocObject
	{
	}
	
	public class DocTerm : DocObject
	{
		[DataMember(Order = 0)] [XmlArray] public List<DocTerm> Terms { get; protected set; } // added in V7.3  // sub-terms
	}

	public class DocAbbreviation : DocObject
	{
	}

	public abstract class DocGeometry : SEntity
	{
	}

	// new in IfcDoc 3.5 for capturing Express-G diagrams
	public class DocPoint : DocGeometry
	{
		[DataMember(Order = 0)] [XmlAttribute] public double X { get; set; }
		[DataMember(Order = 1)] [XmlAttribute] public double Y { get; set; }

		public DocPoint()
		{
		}

		public DocPoint(double x, double y)
		{
			this.X = x;
			this.Y = y;
		}
	}

	// new in IFcDoc 5.8 for capturing nested tree structures of lines
	public class DocLine : SEntity
	{
		[DataMember(Order = 0)] [XmlArray] public List<DocPoint> DiagramLine { get; protected set; } // required points
		[DataMember(Order = 1)] [XmlArray] public List<DocLine> Tree { get; protected set; } // optional set of nested lines
		[DataMember(Order = 2)] [XmlIgnore] public DocDefinition Definition { get; set; } // optional target that the line points to

		public DocLine()
		{
			this.DiagramLine = new List<DocPoint>();
			this.Tree = new List<DocLine>();
		}
	}

	// new in IfcDoc 3.5 for capturing Express-G diagrams
	public class DocRectangle : DocGeometry
	{
		[DataMember(Order = 0)] [XmlAttribute] public double X { get; set; }
		[DataMember(Order = 1)] [XmlAttribute] public double Y { get; set; }
		[DataMember(Order = 2)] [XmlAttribute] public double Width { get; set; } 
		[DataMember(Order = 3)] [XmlAttribute] public double Height { get; set; }
	}

	/// <summary>
	/// Reference to another schema
	/// </summary>
	public class DocSchemaRef : DocObject // new in v4.9
	{
		[DataMember(Order = 0)] [XmlArray] public List<DocDefinitionRef> Definitions { get; set; }

		public DocSchemaRef()
		{
			this.Definitions = new List<DocDefinitionRef>();
		}
	}

	/// <summary>
	/// Reference to an attribute on a referenced definition -- used for UML diagrams
	/// </summary>
	public class DocAttributeRef : DocObject // new in IfcDoc V11.6
	{
		[DataMember(Order = 0)] [XmlElement] public DocAttribute Attribute { get; set; } // originating attribute
		[DataMember(Order = 1)] [XmlElement] public DocDefinitionRef DefinitionRef { get; set; } // target definition reference
		[DataMember(Order = 2)] [XmlArray] public List<DocPoint> DiagramLine { get; protected set; } // line connecting target definition reference

		public DocAttributeRef()
		{
			this.DiagramLine = new List<DocPoint>();
		}
	}

	/// <summary>
	/// Reference to a definition within another schema.
	/// </summary>
	public class DocDefinitionRef : DocDefinition, // new in v4.9
		IDocTreeHost
	{
		[DataMember(Order = 0)] [XmlIgnore] public List<DocLine> Tree { get; protected set; } // new in 5.8 -- tree for subclasses
		[DataMember(Order = 1)] [XmlArray] public List<DocAttributeRef> AttributeRefs { get; protected set; } // new in V11.6: attribute on referenced entity

		public DocDefinitionRef()
		{
			this.Tree = new List<DocLine>();
			this.AttributeRefs = new List<DocAttributeRef>();
		}
		public void InitializeTree() { Tree = new List<DocLine>(); }
	}

	/// <summary>
	/// Comment
	/// </summary>
	public class DocComment : DocDefinition
	{
	}

	/// <summary>
	/// Represents a Schema
	/// </summary>
	public class DocSchema : DocObject
	{
		// ORDER CHANGED in V1.8
		[DataMember(Order = 0)] [XmlArray(Order = 2)] [XmlArrayItem(NestingLevel = 1)] public List<DocAnnotation> Annotations { get; protected set; }   // 5.1.1 Definitions     // inserted in 1.8      
		[DataMember(Order = 1)] [XmlArray(Order = 3)] [XmlArrayItem(NestingLevel = 1)] public List<DocType> Types { get; protected set; }               // 5.1.2 Types           // moved up in 1.8
		[DataMember(Order = 2)] [XmlArray(Order = 4)] [XmlArrayItem(NestingLevel = 1)] public List<DocEntity> Entities { get; protected set; }          // 5.1.3 Entities        // moved down in 1.8
		[DataMember(Order = 3)] [XmlArray(Order = 5)] [XmlArrayItem(NestingLevel = 1)] public List<DocFunction> Functions { get; protected set; }       // 5.1.4 Functions
		[DataMember(Order = 4)] [XmlArray(Order = 6)] [XmlArrayItem(NestingLevel = 1)] public List<DocGlobalRule> GlobalRules { get; protected set; }   // 5.1.5 Global Rules    // inserted in 1.2
		[DataMember(Order = 5)] [XmlArray(Order = 7)] [XmlArrayItem(NestingLevel = 1)] public List<DocPropertySet> PropertySets { get; protected set; } // 5.1.6 Property Sets
		[DataMember(Order = 6)] [XmlArray(Order = 8)] [XmlArrayItem(NestingLevel = 1)] public List<DocQuantitySet> QuantitySets { get; protected set; } // 5.1.7 Quantity Sets
		[DataMember(Order = 7)] [XmlIgnore] public List<DocPageTarget> PageTargets { get; protected set; }  // inserted in 3.5, renamed to DocPageTarget in 4.9
		[DataMember(Order = 8)] [XmlArray(Order = 0)] public List<DocSchemaRef> SchemaRefs { get; protected set; }     // inserted in 4.9
		[DataMember(Order = 9)] [XmlIgnore] public List<DocComment> Comments { get; protected set; } = new List<DocComment>();         // inserted in 4.9
		[DataMember(Order = 10)] [Obsolete] [XmlIgnore] public List<DocPropertyEnumeration> PropertyEnumerations { get; protected set; } // inserted in 5.8, relocated to DocProject 12.1
		[DataMember(Order = 11)] [XmlArray(Order = 1)] [XmlArrayItem(NestingLevel = 1)] public List<DocPrimitive> Primitives { get; protected set; }    // inserted in 5.8
		[DataMember(Order = 12)] [XmlIgnore] public int DiagramPagesHorz { get; set; } // inserted in 5.8
		[DataMember(Order = 13)] [XmlIgnore] public int DiagramPagesVert { get; set; } // inserted in 5.8
		
		public DocSchema()
		{
			this.Annotations = new List<DocAnnotation>();
			this.Types = new List<DocType>();
			this.Entities = new List<DocEntity>();
			this.Functions = new List<DocFunction>();
			this.GlobalRules = new List<DocGlobalRule>();
			this.PropertySets = new List<DocPropertySet>();
			this.QuantitySets = new List<DocQuantitySet>();
			this.PageTargets = new List<DocPageTarget>();
			this.SchemaRefs = new List<DocSchemaRef>();
			this.Comments = new List<DocComment>();
			this.PropertyEnumerations = new List<DocPropertyEnumeration>();
			this.Primitives = new List<DocPrimitive>();
		}

		/// <summary>
		/// Creates or returns existing entity by name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public DocEntity RegisterEntity(string name)
		{
			// find existing
			foreach (DocEntity entity in this.Entities)
			{
				if (entity.Name.Equals(name))
					return entity;
			}

			// create new
			DocEntity docEntity = new DocEntity();
			docEntity.Name = name;
			this.Entities.Add(docEntity);

			// sort alphabetically
			SortedList<string, DocEntity> sortEntity = new SortedList<string, DocEntity>();
			foreach (DocEntity s in this.Entities)
			{
				sortEntity.Add(s.Name, s);
			}
			this.Entities.Clear();

			foreach (DocEntity s in sortEntity.Values)
			{
				this.Entities.Add(s);
			}

			return docEntity;
		}

		public T RegisterType<T>(string name) where T : DocType, new()
		{
			// find existing
			foreach (DocType type in this.Types)
			{
				if (typeof(T).IsInstanceOfType(type) && type.Name.Equals(name))
					return (T)type;
			}

			// create new
			T docType = new T();
			docType.Name = name;
			this.Types.Add(docType);

			// sort alphabetically
			SortedList<string, DocType> sortType = new SortedList<string, DocType>();
			foreach (DocType s in this.Types)
			{
				sortType.Add(s.Name, s);
			}
			this.Types.Clear();

			// order specifically

			foreach (DocType s in sortType.Values)
			{
				if (s is DocDefined)
				{
					this.Types.Add(s);
				}
			}
			foreach (DocType s in sortType.Values)
			{
				if (s is DocEnumeration)
				{
					this.Types.Add(s);
				}
			}
			foreach (DocType s in sortType.Values)
			{
				if (s is DocSelect)
				{
					this.Types.Add(s);
				}
			}

			return docType;
		}

		/// <summary>
		/// Creates or returns existing function by name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public DocFunction RegisterFunction(string name)
		{
			// find existing
			foreach (DocFunction entity in this.Functions)
			{
				if (entity.Name.Equals(name))
					return entity;
			}

			// create new
			DocFunction docFunction = new DocFunction();
			docFunction.Name = name;
			this.Functions.Add(docFunction);

			// sort alphabetically
			SortedList<string, DocFunction> sort = new SortedList<string, DocFunction>();
			foreach (DocFunction s in this.Functions)
			{
				sort.Add(s.Name, s);
			}
			this.Functions.Clear();

			foreach (DocFunction s in sort.Values)
			{
				this.Functions.Add(s);
			}

			return docFunction;
		}

		/// <summary>
		/// Creates or returns existing rule by name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public DocGlobalRule RegisterRule(string name)
		{
			// find existing
			foreach (DocGlobalRule entity in this.GlobalRules)
			{
				if (entity.Name.Equals(name))
					return entity;
			}

			// create new
			DocGlobalRule docFunction = new DocGlobalRule();
			docFunction.Name = name;
			this.GlobalRules.Add(docFunction);

			// sort alphabetically
			SortedList<string, DocGlobalRule> sort = new SortedList<string, DocGlobalRule>();
			foreach (DocGlobalRule s in this.GlobalRules)
			{
				sort.Add(s.Name, s);
			}
			this.GlobalRules.Clear();

			foreach (DocGlobalRule s in sort.Values)
			{
				this.GlobalRules.Add(s);
			}

			return docFunction;
		}

		public DocPropertySet RegisterPset(string name)
		{
			// find existing
			foreach (DocPropertySet entity in this.PropertySets)
			{
				if (entity.Name.Equals(name))
					return entity;
			}

			// create new
			DocPropertySet docFunction = new DocPropertySet();
			docFunction.Name = name;
			this.PropertySets.Add(docFunction);

			// sort alphabetically
			SortedList<string, DocPropertySet> sort = new SortedList<string, DocPropertySet>();
			foreach (DocPropertySet s in this.PropertySets)
			{
				sort.Add(s.Name, s);
			}
			this.PropertySets.Clear();

			foreach (DocPropertySet s in sort.Values)
			{
				this.PropertySets.Add(s);
			}

			return docFunction;
		}

		public DocQuantitySet RegisterQset(string name)
		{
			// find existing
			foreach (DocQuantitySet entity in this.QuantitySets)
			{
				if (entity.Name.Equals(name))
					return entity;
			}

			// create new
			DocQuantitySet docFunction = new DocQuantitySet();
			docFunction.Name = name;
			this.QuantitySets.Add(docFunction);

			// sort alphabetically
			SortedList<string, DocQuantitySet> sort = new SortedList<string, DocQuantitySet>();
			foreach (DocQuantitySet s in this.QuantitySets)
			{
				sort.Add(s.Name, s);
			}
			this.QuantitySets.Clear();

			foreach (DocQuantitySet s in sort.Values)
			{
				this.QuantitySets.Add(s);
			}

			return docFunction;
		}

		internal void initialize()
		{
			if (PageTargets == null)
				PageTargets = new List<DocPageTarget>();
			if (Comments == null)
				Comments = new List<DocComment>();
		}
		/// <summary>
		/// Updates page numbers for all definitions within schema
		/// </summary>
		/// <returns>Returns the number of diagrams</returns>
		public int UpdateDiagramPageNumbers()
		{
			int iLastDiagram = 0;


			foreach (DocEntity docEnt in this.Entities)
			{
				if (docEnt.DiagramRectangle != null) // older files didn't have this, e.g. IFC2x3 baseline
				{
					// temp: force update
					int px = (int)(docEnt.DiagramRectangle.X * CtlExpressG.Factor / CtlExpressG.PageX);
					int py = (int)(docEnt.DiagramRectangle.Y * CtlExpressG.Factor / CtlExpressG.PageY);
					int page = 1 + py * this.DiagramPagesHorz + px;
					docEnt.DiagramNumber = page;

					if (docEnt.DiagramNumber > iLastDiagram)
					{
						iLastDiagram = docEnt.DiagramNumber;
					}
				}
			}
			foreach (DocType docType in this.Types)
			{
				if (docType.DiagramRectangle != null)
				{
					// temp: force update
					int px = (int)(docType.DiagramRectangle.X * CtlExpressG.Factor / CtlExpressG.PageX);
					int py = (int)(docType.DiagramRectangle.Y * CtlExpressG.Factor / CtlExpressG.PageY);
					int page = 1 + py * this.DiagramPagesHorz + px;
					docType.DiagramNumber = page;

					if (docType.DiagramNumber > iLastDiagram)
					{
						iLastDiagram = docType.DiagramNumber;
					}
				}
			}

			if (this.SchemaRefs != null)
			{
				foreach (DocSchemaRef docRef in this.SchemaRefs)
				{
					foreach (DocDefinitionRef docDef in docRef.Definitions)
					{
						if (docDef.DiagramRectangle != null)
						{
							int px = (int)(docDef.DiagramRectangle.X * CtlExpressG.Factor / CtlExpressG.PageX);
							int py = (int)(docDef.DiagramRectangle.Y * CtlExpressG.Factor / CtlExpressG.PageY);
							int page = 1 + py * this.DiagramPagesHorz + px;
							docDef.DiagramNumber = page;

							if (docDef.DiagramNumber > iLastDiagram)
							{
								iLastDiagram = docDef.DiagramNumber;
							}
						}
						else
						{
							this.ToString();
						}
					}
				}
			}

			if (this.PageTargets != null)
			{
				foreach (DocPageTarget docPageTarget in this.PageTargets)
				{
					if (docPageTarget.DiagramRectangle != null)
					{
						int px = (int)(docPageTarget.DiagramRectangle.X * CtlExpressG.Factor / CtlExpressG.PageX);
						int py = (int)(docPageTarget.DiagramRectangle.Y * CtlExpressG.Factor / CtlExpressG.PageY);
						int page = 1 + py * this.DiagramPagesHorz + px;
						docPageTarget.DiagramNumber = page;

						foreach (DocPageSource docPageSource in docPageTarget.Sources)
						{
							px = (int)(docPageSource.DiagramRectangle.X * CtlExpressG.Factor / CtlExpressG.PageX);
							py = (int)(docPageSource.DiagramRectangle.Y * CtlExpressG.Factor / CtlExpressG.PageY);
							page = 1 + py * this.DiagramPagesHorz + px;
							docPageSource.DiagramNumber = page;
						}
					}
				}
			}


			return iLastDiagram;
		}

		/// <summary>
		/// Returns Entity, Type, or Reference to entity/type, or null if no such definition.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public DocDefinition GetDefinition(string name)
		{
			foreach (DocType docType in this.Types)
			{
				if (docType.Name == name)
					return docType;
			}

			foreach (DocEntity docEnt in this.Entities)
			{
				if (docEnt.Name == name)
					return docEnt;
			}

			foreach (DocSchemaRef docSchemaRef in this.SchemaRefs)
			{
				foreach (DocDefinitionRef docDefRef in docSchemaRef.Definitions)
				{
					if (docDefRef.Name == name)
						return docDefRef;
				}
			}

			return null;
		}

		public void SortSchema()
		{
			DocObject.ComparerDocObject comparer = new DocObject.ComparerDocObject();
			Annotations.Sort(comparer);
			SortTypes();
			SortEntities();
			Functions.Sort(comparer);
			GlobalRules.Sort(comparer);
			PropertySets.Sort(comparer);
			QuantitySets.Sort(comparer);
			PageTargets.Sort(comparer);
			SchemaRefs.Sort(comparer);
			Comments.Sort(comparer);
			Primitives.Sort(comparer);
		}
	/// <summary>
	/// Sorts type list according to definition type and alphabetical name
	/// </summary>
		public void SortTypes()
		{
			SortedList<string, DocDefined> sortDefined = new SortedList<string, DocDefined>();
			SortedList<string, DocEnumeration> sortEnum = new SortedList<string, DocEnumeration>();
			SortedList<string, DocSelect> sortSelect = new SortedList<string, DocSelect>();

			foreach (DocType docType in this.Types)
			{
				if (docType is DocDefined)
				{
					sortDefined.Add(docType.Name, (DocDefined)docType);
				}
				else if (docType is DocEnumeration)
				{
					sortEnum.Add(docType.Name, (DocEnumeration)docType);
				}
				else if (docType is DocSelect)
				{
					sortSelect.Add(docType.Name, (DocSelect)docType);
				}
			}

			this.Types.Clear();
			this.Types.AddRange(sortDefined.Values);
			this.Types.AddRange(sortEnum.Values);
			this.Types.AddRange(sortSelect.Values);
		}

		/// <summary>
		/// Sorts entity list according to alphabetical name
		/// </summary>
		public void SortEntities()
		{
			SortedList<string, DocEntity> sortEntity = new SortedList<string, DocEntity>();

			foreach (DocEntity docType in this.Entities)
			{
				sortEntity.Add(docType.Name, docType);
			}

			this.Entities.Clear();
			this.Entities.AddRange(sortEntity.Values);
		}

		
		/// <summary>
		/// Sorts function list according to alphabetical name
		/// </summary>
		public void SortFunctions()
		{
			SortedList<string, DocFunction> sortEntity = new SortedList<string, DocFunction>();

			foreach (DocFunction docType in this.Functions)
			{
				sortEntity.Add(docType.Name, docType);
			}

			this.Functions.Clear();
			this.Functions.AddRange(sortEntity.Values);
		}

		/// <summary>
		/// Sorts function list according to alphabetical name
		/// </summary>
		public void SortGlobalRules()
		{
			SortedList<string, DocGlobalRule> sortEntity = new SortedList<string, DocGlobalRule>();

			foreach (DocGlobalRule docType in this.GlobalRules)
			{
				sortEntity.Add(docType.Name, docType);
			}

			this.GlobalRules.Clear();
			this.GlobalRules.AddRange(sortEntity.Values);
		}

		/// <summary>
		/// Sorts property sets list according to alphabetical name
		/// </summary>
		public void SortPropertySets()
		{
			SortedList<string, DocPropertySet> sortEntity = new SortedList<string, DocPropertySet>();

			foreach (DocPropertySet docType in this.PropertySets)
			{
				sortEntity.Add(docType.Name, docType);
			}

			this.PropertySets.Clear();
			this.PropertySets.AddRange(sortEntity.Values);
		}

		/// <summary>
		/// Sorts quantity sets list according to alphabetical name
		/// </summary>
		public void SortQuantitySets()
		{
			SortedList<string, DocQuantitySet> sortEntity = new SortedList<string, DocQuantitySet>();

			foreach (DocQuantitySet docType in this.QuantitySets)
			{
				sortEntity.Add(docType.Name, docType);
			}

			this.QuantitySets.Clear();
			this.QuantitySets.AddRange(sortEntity.Values);
		}

		public int GetDefinitionPageNumber(DocDefinition docEntity)
		{
			if (docEntity.DiagramRectangle == null)
				return 0;

			int px = (int)(docEntity.DiagramRectangle.X * CtlExpressG.Factor / CtlExpressG.PageX);
			int py = (int)(docEntity.DiagramRectangle.Y * CtlExpressG.Factor / CtlExpressG.PageY);
			int page = 1 + py * this.DiagramPagesHorz + px;
			return page;
		}

		public int GetPageTargetItemNumber(DocPageTarget docTarget)
		{
			int page = GetDefinitionPageNumber(docTarget);
			int item = 0;
			foreach (DocPageTarget docEachTarget in this.PageTargets)
			{
				int eachpage = GetDefinitionPageNumber(docEachTarget);
				if (eachpage == page)
				{
					item++;
				}

				if (docEachTarget == docTarget)
					return item;
			}

			return 0; // don't know
		}
	}

	/// <summary>
	/// Abstract type definition (base of Identity and Type)
	/// </summary>
	public abstract class DocDefinition : DocObject
	{
		[DataMember(Order = 0)] [XmlIgnore] public DocRectangle DiagramRectangle { get; set; } // replaces template status (Integer) in v3.5
		[DataMember(Order = 1)] [XmlIgnore] public int DiagramNumber { get; set; } // used to determine hyperlink to EXPRESS-G diagram [inserted in v1.2]        

		private Type m_runtimetype; // corresponding compiled type

		public Type RuntimeType
		{
			get
			{
				return this.m_runtimetype;
			}
			set
			{
				this.m_runtimetype = value;
			}
		}

		public bool IsInstanceOfType(object target)
		{
			if (this.m_runtimetype == null)
				return false;

			return this.m_runtimetype.IsInstanceOfType(target);
		}

	}

	/// <summary>
	/// EXPRESS-G page reference targets (has one or more sources that point to it)
	/// The name identifies the entity or type to be referenced across pages.
	/// </summary>
	public class DocPageTarget : DocDefinition // 4.9
	{
		[DataMember(Order = 0)] [XmlArray] public List<DocPoint> DiagramLine { get; protected set; }
		[DataMember(Order = 1)] [XmlArray] public List<DocPageSource> Sources { get; protected set; }
		[DataMember(Order = 2)] [XmlArray] public DocDefinition Definition { get; set; } // 5.8

		public DocPageTarget()
		{
			this.DiagramLine = new List<DocPoint>();
			this.Sources = new List<DocPageSource>();
		}

	}

	/// <summary>
	/// Express-G page reference sources (link to targets)
	/// </summary>
	public class DocPageSource : DocDefinition // 4.9
	{
		// OBSOLETE - REMOVED 8.6 // [DataMember(Order = 0)] public DocPageTarget Target; // new in 5.8 -- link to associated target
	}

	/// <summary>
	/// Primitive declaration (e.g. BOOLEAN, LOGICAL, INTEGER, REAL, STRING, BINARY)
	/// </summary>
	public class DocPrimitive : DocDefinition // 5.8
	{
		protected override string folderName() { return xmlid(); }
	}

	/// <summary>
	/// Represents an Identity
	/// </summary>
	public class DocEntity : DocDefinition,
		IDocTreeHost
	{
		[DataMember(Order = 0)] [XmlAttribute] public string BaseDefinition { get; set; } // string base type
		[DataMember(Order = 1), XmlAttribute, Required] public int EntityFlags { get; set; }
		[DataMember(Order = 2)] [XmlArray(Order = 0)] public List<DocSubtype> Subtypes { get; protected set; } // flat list of subtypes (regardless of diagram tree)
		[DataMember(Order = 3)] [XmlArray(Order = 1)] public List<DocAttribute> Attributes { get; protected set; }
		[DataMember(Order = 4)] [XmlArray(Order = 2)] public List<DocUniqueRule> UniqueRules { get; protected set; }
		[DataMember(Order = 5)] [XmlArray(Order = 3)] public List<DocWhereRule> WhereRules { get; protected set; }
		[DataMember(Order = 6), Obsolete, XmlIgnore] private List<DocTemplateUsage> _Templates { get; set; } // to be deprecated -- use ModelView.ConceptRoots[].Concepts
		//[DataMember(Order = 7), Obsolete]
		//private string _Description { get; set; } // 2.7 -- holds Body description from MVD-XML for which documentation is generated; 5.3 deprecated
		[DataMember(Order = 7)] [XmlAttribute] private string DefaultMember { get; set; } // 12.0: identifies attribute used for identifying object, e.g. IfcRepresentation.RepresentationIdentifier -- DefaultMember in C#
		[DataMember(Order = 8), Obsolete, XmlIgnore] private List<DocPoint> _DiagramLine { get; set; } // 3.5 -- line to tree of subtypes - removed in V5.8
		[DataMember(Order = 9)] [XmlIgnore] public List<DocLine> Tree { get; protected set; } // 5.8 -- tree of lines and subtypes for diagram rendering

		internal bool _InheritanceDiagramFlag;

		public DocEntity()
		{
			this.Subtypes = new List<DocSubtype>();
			this.Attributes = new List<DocAttribute>();
			this.UniqueRules = new List<DocUniqueRule>();
			this.WhereRules = new List<DocWhereRule>();
			this.Tree = new List<DocLine>();
			this.EntityFlags = 0x20; // non-abstract
		}

		[Category("Template Fields"), DisplayName("TEXT")]
		public string Text
		{
			get
			{
				return MakeDisplayName(this.Name);
			}
		}

		private string MakeDisplayName(string content)
		{
			if (content == null)
				return null;

			if (content.StartsWith("IfcRelAssociates"))
				return content.Substring(16);

			if (content.StartsWith("IfcRel"))
				return null;

			StringBuilder sb = new StringBuilder();
			for (int i = 3; i < content.Length; i++)
			{
				if (Char.IsUpper(content[i]))
				{
					// insert space before capital letter
					if (i > 3)
					{
						sb.Append(" ");
					}
					sb.Append(Char.ToLower(content[i]));
				}
				else
				{
					sb.Append(content[i]);
				}
			}

			return sb.ToString();
		}
		public void InitializeTree() { Tree = new List<DocLine>(); }
		public bool IsAbstract
		{
			get
			{
				return ((this.EntityFlags & 0x20) == 0);
			}
			set
			{
				if (value)
				{
					this.EntityFlags &= ~0x20;
				}
				else
				{
					this.EntityFlags |= 0x20;
				}
			}
		}

		public DocDefinition ResolveParameterType(DocModelRuleAttribute docRuleAttr, string parmname, Dictionary<string, DocObject> map)
		{
			DocAttribute docAttribute = this.ResolveParameterAttribute(docRuleAttr, parmname, map);
			if (docAttribute != null)
			{
				DocObject docdef = null;
				if (map.TryGetValue(docAttribute.DefinedType, out docdef) && docdef is DocDefinition)
					return (DocDefinition)docdef;
			}

			return null;
		}

		public DocAttribute ResolveParameterAttribute(DocModelRuleAttribute docRuleAttr, string parmname, Dictionary<string, DocObject> map)
		{
			DocAttribute docAttribute = this.ResolveAttribute(docRuleAttr.Name, map);
			if (docAttribute == null)
				return null;

			if (docRuleAttr.Identification != null && docRuleAttr.Identification.Equals(parmname))
			{
				return docAttribute;
				/*
                // resolve type
                DocObject docdef = null;
                if (map.TryGetValue(docAttribute.DefinedType, out docdef) && docdef is DocDefinition)
                    return (DocDefinition)docdef;*/
			}

			// keep drilling
			foreach (DocModelRuleEntity docRuleEntity in docRuleAttr.Rules)
			{
				DocObject docObjSub = null;
				if (map.TryGetValue(docRuleEntity.Name, out docObjSub))
				{
					if (docObjSub is DocEntity)
					{
						DocEntity docEntitySub = (DocEntity)docObjSub;
						foreach (DocModelRule docRuleSub in docRuleEntity.Rules)
						{
							if (docRuleSub is DocModelRuleAttribute)
							{
								DocAttribute docDefSub = docEntitySub.ResolveParameterAttribute((DocModelRuleAttribute)docRuleSub, parmname, map);
								if (docDefSub != null)
								{
									return docDefSub;
								}
							}
						}
					}
				}
			}

			return null;
		}

		// new version
		internal DocAttribute ResolveAttribute(string attrname, DocProject docProject)
		{
			foreach (DocAttribute docAttr in this.Attributes)
			{
				if (docAttr.Name.Equals(attrname))
					return docAttr;
			}

			// super
			if (!String.IsNullOrEmpty(this.BaseDefinition))// && map.TryGetValue(this.BaseDefinition, out docObj))
			{
				DocDefinition docObj = docProject.GetDefinition(this.BaseDefinition);
				if (docObj is DocEntity)
				{
					DocEntity docSuper = (DocEntity)docObj;
					return docSuper.ResolveAttribute(attrname, docProject);
				}
			}

			return null;
		}

		// old version - deprecate
		internal DocAttribute ResolveAttribute(string attrname, Dictionary<string, DocObject> map)
		{
			foreach (DocAttribute docAttr in this.Attributes)
			{
				if (docAttr.Name.Equals(attrname))
					return docAttr;
			}

			// super
			DocObject docObj = null;
			if (!String.IsNullOrEmpty(this.BaseDefinition) && map.TryGetValue(this.BaseDefinition, out docObj))
			{
				DocEntity docSuper = (DocEntity)docObj;
				return docSuper.ResolveAttribute(attrname, map);
			}

			return null;
		}

		public DocAttribute RegisterAttribute(string name)
		{
			foreach (DocAttribute docExist in this.Attributes)
			{
				if (docExist.Name.Equals(name))
				{
					return docExist;
				}
			}

			DocAttribute docAttr = new DocAttribute();
			docAttr.Name = name;
			this.Attributes.Add(docAttr);
			return docAttr;
		}

		public DocWhereRule RegisterWhereRule(string name)
		{
			foreach (DocWhereRule docExist in this.WhereRules)
			{
				if (docExist.Name.Equals(name))
				{
					return docExist;
				}
			}

			DocWhereRule docAttr = new DocWhereRule();
			docAttr.Name = name;
			this.WhereRules.Add(docAttr);
			return docAttr;
		}
		internal void ClearDefaultMember()
		{
			DefaultMember = null;
		}

		protected override string xmlid() { return Name; } 
	}
	public class DocSubtype : DocObject
	{
		[DataMember(Order = 0)] [XmlAttribute] public string DefinedType { get; set; }

		public DocSubtype()
		{
		}
	}

	/*
    [Flags]
    public enum DocAttributeFlags
    {
        None = 0,
        Optional = 1, // Visual Express
        Unique = 2,   // Visual Express

        Strong = 0x10000, // UML: strong reference: delete if 
    }
    */

	/// <summary>
	/// Represents an Attribute
	/// </summary>
	public class DocAttribute : DocObject
	{
		[DataMember(Order = 0)] [XmlAttribute] public string DefinedType { get; set; } // the EXPRESS type (bypassing any indirection from page references, etc.)
		[DataMember(Order = 1)] [XmlIgnore] public DocDefinition Definition { get; set; } // the EXPRESS-G link -- never used until 5.8 -- holds EXPRESS-G target; renamed from "ReferencedType"
		[DataMember(Order = 2), XmlAttribute, Required] public int AttributeFlags { get; set; }
		[DataMember(Order = 3), XmlAttribute, Required] public int AggregationType { get; set; } 
		[DataMember(Order = 4), XmlAttribute, Required] public int AggregationFlag { get; set; } // inserted
		[DataMember(Order = 5)] [XmlAttribute] public string AggregationLower { get; set; } // was int (changed for VEX in v2.0)
		[DataMember(Order = 6)] [XmlAttribute] public string AggregationUpper { get; set; } // was int (changed for VEX in v2.0)
		[DataMember(Order = 7)] [XmlAttribute] public string Inverse { get; set; }
		[DataMember(Order = 8)] [XmlElement] public string Derived { get; set; }
		[DataMember(Order = 9)] [XmlElement] public DocAttribute AggregationAttribute { get; set; } // nested aggregations
		[DataMember(Order = 10)] [XmlIgnore] public List<DocPoint> DiagramLine { get; protected set; } // line coordinates
		[DataMember(Order = 11)] [XmlIgnore] public DocRectangle DiagramLabel { get; set; } // position of label
		[DataMember(Order = 12, IsRequired = false)] [XmlAttribute] public DocXsdFormatEnum XsdFormat { get; set; }  // NEW in IfcDoc 4.9f: tag behavior
		[DataMember(Order = 13)] [XmlAttribute] public bool? XsdTagless { get; set; } // NEW in IfcDoc 5.0b: tagless; 8.7: NULLABLE

		private PropertyInfo m_runtimefield; // holds compiled property definition

		public DocAttribute()
		{
			this.DiagramLine = new List<DocPoint>();
		}

		public DocAggregationEnum GetAggregation()
		{
			return (DocAggregationEnum)this.AggregationType;
		}

		public bool IsOptional
		{
			get
			{
				return ((this.AttributeFlags & 1) != 0);
			}
			set
			{
				if (value)
				{
					this.AttributeFlags |= 1;
				}
				else
				{
					this.AttributeFlags &= ~1;
				}
			}
		}

		public bool IsUnique
		{
			get
			{
				return ((this.AggregationFlag & 2) != 0);
			}
			set
			{
				if (value)
				{
					this.AggregationFlag |= 2;
				}
				else
				{
					this.AggregationFlag &= ~2;
				}
			}
		}

		/// <summary>
		/// Returns aggregation expression suitable for use in diagram, e.g. "S[1:?]"
		/// </summary>
		/// <returns></returns>
		public string GetAggregationExpression()
		{
			string display = "";
			string lower = "0";
			string upper = "?";
			if (this.AggregationLower != null)
			{
				lower = this.AggregationLower;
			}
			if (this.AggregationUpper != null && this.AggregationUpper != "0")
			{
				upper = this.AggregationUpper;
			}
			DocAggregationEnum docAggr = this.GetAggregation();
			switch (docAggr)
			{
				case DocAggregationEnum.SET:
					display += "S[" + lower + ":" + upper + "]";
					break;

				case DocAggregationEnum.LIST:
					display += "L[" + lower + ":" + upper + "]";
					break;
			}

			return display;
		}

		public int GetAggregationNestingLower()
		{
			if (String.IsNullOrEmpty(this.AggregationLower))
				return 0; //??? or -1???

			int iLower = Int32.Parse(this.AggregationLower);
			DocAttribute docAggregate = this.AggregationAttribute;
			while (docAggregate != null && docAggregate.AggregationLower != null)
			{
				int iInner = Int32.Parse(docAggregate.AggregationLower);
				iLower = iLower * iInner;
				docAggregate = docAggregate.AggregationAttribute;
			}

			return iLower;
		}

		public int GetAggregationNestingUpper()
		{
			int iUpper = 0;

			if (String.IsNullOrEmpty(this.AggregationUpper) || !Int32.TryParse(this.AggregationUpper, out iUpper))
				return 0;

			DocAttribute docAggregate = this.AggregationAttribute;
			while (docAggregate != null)
			{
				int iInner = Int32.Parse(docAggregate.AggregationUpper);
				iUpper = iUpper * iInner;
				docAggregate = docAggregate.AggregationAttribute;
			}

			return iUpper;
		}

		public PropertyInfo RuntimeField
		{
			get
			{
				return this.m_runtimefield;
			}
			set
			{
				this.m_runtimefield = value;
			}
		}

		/// <summary>
		/// Gets value on runtime instance
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="index"></param>
		/// <returns></returns>
		public object GetValue(object obj, object[] index)
		{
			if (this.m_runtimefield == null)
				return null;

			return this.m_runtimefield.GetValue(obj);
		}

		public Type PropertyType
		{
			get
			{
				if (this.m_runtimefield == null)
					return null;

				return this.m_runtimefield.PropertyType;
			}
		}
	}

	public enum DocAggregationEnum
	{
		NONE = 0,
		LIST = 1,
		ARRAY = 2,
		SET = 3,
		BAG = 4,
	}

	public class DocUniqueRule : DocConstraint
	{
		[DataMember(Order = 0)] [XmlArray] public List<DocUniqueRuleItem> Items { get; set; }

		public DocUniqueRule()
		{
			this.Items = new List<DocUniqueRuleItem>();
		}
	}

	public class DocUniqueRuleItem : DocObject
	{
	}

	public class DocWhereRule : DocConstraint
	{
	}

	/// <summary>
	/// Abstract base class of types (non-entities)
	/// </summary>
	public abstract class DocType : DocDefinition
	{
		protected override string xmlid() { return Name; } 
	}

	/// <summary>
	/// A defined type
	/// </summary>
	public class DocDefined : DocType
	{
		[DataMember(Order = 0)] [XmlAttribute] public string DefinedType { get; set; }
		[DataMember(Order = 1)] [XmlElement] public DocDefinition Definition { get; set; } // never used until V5.8
		[DataMember(Order = 2)] [XmlArray] public List<DocWhereRule> WhereRules { get; protected set; }
		[DataMember(Order = 3)] [XmlAttribute] public int Length { get; set; } // e.g. length of string        
		[DataMember(Order = 4)] [XmlElement] public DocAttribute Aggregation { get; set; } // added V1.8, 2011-02-22
		[DataMember(Order = 5)] [XmlIgnore] public List<DocPoint> DiagramLine { get; protected set; } // added V5.8

		public DocDefined()
		{
			this.WhereRules = new List<DocWhereRule>();
			this.DiagramLine = new List<DocPoint>();
		}
	}

	/// <summary>
	/// A select type
	/// </summary>
	public class DocSelect : DocType,
		IDocTreeHost
	{
		[DataMember(Order = 0)] [XmlArray] public List<DocSelectItem> Selects { get; protected set; }
		[DataMember(Order = 1)] [XmlIgnore] public List<DocLine> Tree { get; protected set; } // V5.8, optional tree for EXPRESS-G diagram..... todo: replace this

		public DocSelect()
		{
			this.Selects = new List<DocSelectItem>();
			this.Tree = new List<DocLine>();
		}
		public void InitializeTree() { Tree = new List<DocLine>(); }
	}

	public class DocSelectItem : DocObject
	{
		//[DataMember(Order = 0), Obsolete]
		//private List<DocPoint> _DiagramLine { get; set; } // 3.8  -- deprecated in 5.8 (use DocLine instead to capture tree structure) 

		public DocSelectItem()
		{
		}
	}

	/// <summary>
	/// An enumeration type
	/// </summary>
	public class DocEnumeration : DocType
	{
		[DataMember(Order = 0)] [XmlArray] public List<DocConstant> Constants { get; protected set; }

		public DocEnumeration()
		{
			this.Constants = new List<DocConstant>();
		}
	}

	/// <summary>
	/// Constant of an enumeration
	/// </summary>
	public class DocConstant : DocObject
	{
		[IgnoreDataMember] [XmlIgnore] [InverseProperty("Constants")] public HashSet<DocEnumeration> PartOfEnumeration { get; protected set; } = new HashSet<DocEnumeration>();
		protected override string folderName() { return xmlid(); }
	}

	public abstract class DocConstraint : DocObject
	{
		[DataMember(Order = 0)] [XmlElement] [DataType(DataType.MultilineText)] public string Expression { get; set; }

	}

	/// <summary>
	/// Global function
	/// </summary>
	public class DocFunction : DocConstraint
	{
		[DataMember(Order = 0)] [XmlArray] public List<DocParameter> Parameters { get; protected set; }
		[DataMember(Order = 1)] [XmlAttribute] public string ReturnValue { get; set; }

		public DocFunction()
		{
			this.Parameters = new List<DocParameter>();
		}
	}

	public class DocParameter : DocObject
	{
		[DataMember(Order = 0)] [XmlAttribute] public string DefinedType { get; set; }
	}

	public class DocGlobalRule : DocConstraint
	{
		[DataMember(Order = 0)] [XmlArray] public List<DocWhereRule> WhereRules { get; set; }
		[DataMember(Order = 1)] [XmlAttribute] public string ApplicableEntity { get; set; } // really list, but IFC only has single item

		public DocGlobalRule()
		{
			this.WhereRules = new List<DocWhereRule>();
		}
	}

	public abstract class DocVariableSet : DocObject
	{
		[DataMember(Order = 0)] [XmlAttribute] public string ApplicableType { get; set; } // e.g. IfcSensor/TEMPERATURESENSOR

		public DocEntity[] GetApplicableTypeDefinitions(DocProject docProject)
		{
			if (this.ApplicableType == null)
				return null;

			string[] parts = this.ApplicableType.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			DocEntity[] list = new DocEntity[parts.Length];
			for (int i = 0; i < list.Length; i++)
			{
				string[] entry = parts[i].Split('/');
				list[i] = docProject.GetDefinition(entry[0]) as DocEntity;
			}

			return list;
		}
	}

	/// <summary>
	/// Property set definition
	/// </summary>
	public class DocPropertySet : DocVariableSet
	{
		[DataMember(Order = 0)] [XmlAttribute] public string PropertySetType { get; set; } // PSET_OCCURRENCEDRIVEN, PSET_TYPEDRIVENOVERRIDE, PSET_PERFORMANCEDRIVEN
		[DataMember(Order = 1)] [XmlArray] public List<DocProperty> Properties { get; protected set; }

		public DocPropertySet()
		{
			this.Properties = new List<DocProperty>();
		}

		internal DocProperty RegisterProperty(string p, DocProject project)
		{
			foreach (DocProperty docProperty in this.Properties)
			{
				if (docProperty.Name.Equals(p))
					return docProperty;
			}

			DocProperty q = new DocProperty();
			q.Name = p;
			this.Properties.Add(q);
			q.PartOfPset.Add(this);
			project.Properties.Add(q);
			return q;
		}


		internal DocProperty GetProperty(string p)
		{
			foreach (DocProperty docProperty in this.Properties)
			{
				if (docProperty.Name.Equals(p))
					return docProperty;
			}

			return null;
		}

		/// <summary>
		/// Indicates whether property should be shown in documentation, according to its type.
		/// </summary>
		/// <returns></returns>
		public bool IsVisible()
		{
			return (this.PropertySetType != "NOTDEFINED");
		}

		/// <summary>
		/// Converts a property set to an entity
		/// </summary>
		/// <returns></returns>
		public DocEntity ToEntity(Dictionary<string, DocObject> map)
		{
			DocEntity docEnt = new DocEntity();
			docEnt.CopyFrom(this);

			DocObject docApp = null;
			if (this.ApplicableType != null && map.TryGetValue(this.ApplicableType, out docApp) && docApp is DocEntity)
			{
				docEnt.BaseDefinition = docApp.Name; // link through base type
				if (!this.IsVisible())
				{
					docEnt.EntityFlags |= 0x20; // abstract
				}

				docEnt.Code = "Pset"; // mark it
			}

			foreach (DocProperty docProp in this.Properties)
			{
				DocAttribute docAttr = docProp.ToAttribute(map);
				if (docAttr != null)
				{
					docEnt.Attributes.Add(docAttr);
				}
			}

			return docEnt;
		}

		protected override string xmlid() { return Name; } 
	}

	public enum DocStateEnum // matches IfcStateEnum
	{
		READWRITE = 0,
		READONLY = 1,
		LOCKED = 2,
		READWRITELOCKED = 3,
		READONLYLOCKED = 4,
	}


	/// <summary>
	/// Property definition
	/// </summary>
	public class DocProperty : DocObject
	{
		[DataMember(Order = 0)] [XmlAttribute] public DocPropertyTemplateTypeEnum PropertyType { get; set; } // IfcPropertySingleValue, IfcPropertyBoundedValue, ...
		[DataMember(Order = 1)] [XmlAttribute] public string PrimaryDataType { get; set; }
		[DataMember(Order = 2)] [XmlAttribute] public string SecondaryDataType { get; set; }
		[DataMember(Order = 3)] [XmlArray] public List<DocProperty> Elements { get; set; } // enumerated or complex properties
		[DataMember(Order = 4)] [XmlAttribute] public DocStateEnum AccessState { get; set; } // V10.5
		[DataMember(Order = 5)] [XmlElement] public DocPropertyEnumeration Enumeration { get; set; } // 12.1

		[IgnoreDataMember] [XmlIgnore] [InverseProperty("Properties")] public HashSet<DocPropertySet> PartOfPset { get; protected set; }
		[IgnoreDataMember] [XmlIgnore] [InverseProperty("Elements")] public HashSet<DocProperty> PartOfComplex { get; protected set; }


		public DocProperty()
		{
			this.Elements = new List<DocProperty>();
			this.PartOfPset = new HashSet<DocPropertySet>();
			this.PartOfComplex = new HashSet<DocProperty>();
		}

		protected internal override void FindQuery(string query, bool searchtext, List<DocFindResult> results)
		{
			base.FindQuery(query, searchtext, results);

			foreach (DocProperty docProp in this.Elements)
			{
				docProp.FindQuery(query, searchtext, results);
			}
		}

		internal DocProperty RegisterProperty(string p)
		{
			foreach (DocProperty docQuantity in this.Elements)
			{
				if (docQuantity.Name.Equals(p))
					return docQuantity;
			}

			DocProperty q = new DocProperty();
			q.Name = p;
			this.Elements.Add(q);
			q.PartOfComplex.Add(this);
			return q;
		}

		public void RegisterObjectsInScope(Dictionary<DocObject, bool> included)
		{
			if (this.Enumeration != null)
				included[this.Enumeration] = true;
			foreach (DocProperty docProperty in Elements)
				docProperty.RegisterObjectsInScope(included);

		}

		public string GetEntityName()
		{
			string propclass = "IfcPropertySingleValue";
			switch (this.PropertyType)
			{
				case DocPropertyTemplateTypeEnum.P_SINGLEVALUE:
					propclass = "IfcPropertySingleValue";
					break;

				case DocPropertyTemplateTypeEnum.P_BOUNDEDVALUE:
					propclass = "IfcPropertyBoundedValue";
					break;

				case DocPropertyTemplateTypeEnum.P_ENUMERATEDVALUE:
					propclass = "IfcPropertyEnumeratedValue";
					break;

				case DocPropertyTemplateTypeEnum.P_LISTVALUE:
					propclass = "IfcPropertyListValue";
					break;

				case DocPropertyTemplateTypeEnum.P_TABLEVALUE:
					propclass = "IfcPropertyTableValue";
					break;

				case DocPropertyTemplateTypeEnum.P_REFERENCEVALUE:
					propclass = "IfcPropertyReferenceValue";
					break;

				case DocPropertyTemplateTypeEnum.COMPLEX:
					propclass = "IfcComplexProperty";
					break;
			}

			return propclass;
		}

		public DocAttribute ToAttribute(Dictionary<string, DocObject> mapDefs)
		{
			DocAttribute docAttr = new DocAttribute();
			docAttr.CopyFrom(this);

			docAttr.DefinedType = this.PrimaryDataType;
			if (String.IsNullOrEmpty(docAttr.DefinedType))
			{
				docAttr.DefinedType = "IfcLabel";
			}

			DocObject docDef = null;
			if (docAttr.DefinedType != null && mapDefs.TryGetValue(docAttr.DefinedType, out docDef))
			{
				docAttr.Definition = docDef as DocDefinition;
			}

			DocObject docRef = null;
			switch (this.PropertyType)
			{
				case DocPropertyTemplateTypeEnum.P_SINGLEVALUE:
					break;

				case DocPropertyTemplateTypeEnum.P_BOUNDEDVALUE:
					docAttr.AggregationType = (int)DocAggregationEnum.ARRAY;
					docAttr.AggregationLower = "1";
					docAttr.AggregationUpper = "3";
					break;

				case DocPropertyTemplateTypeEnum.P_LISTVALUE:
					docAttr.AggregationType = (int)DocAggregationEnum.LIST;
					docAttr.AggregationLower = "1";
					docAttr.AggregationUpper = "?";
					break;

				case DocPropertyTemplateTypeEnum.P_TABLEVALUE:
					docAttr.AggregationType = (int)DocAggregationEnum.LIST;
					docAttr.AggregationLower = "2";
					docAttr.AggregationUpper = "?";
					break;

				case DocPropertyTemplateTypeEnum.P_ENUMERATEDVALUE:
					if (this.Enumeration != null)
					{
						if (mapDefs.TryGetValue(this.Enumeration.Name, out docRef))
						{
							docAttr.DefinedType = this.Enumeration.Name;
							docAttr.Definition = docRef as DocDefinition;
						}
					}
					break;

				case DocPropertyTemplateTypeEnum.P_REFERENCEVALUE:
					if (!String.IsNullOrEmpty(this.SecondaryDataType))
					{
						// e.g. time series
						if (mapDefs.TryGetValue(this.SecondaryDataType, out docRef))
						{
							docAttr.DefinedType = this.SecondaryDataType;
							docAttr.Definition = docRef as DocDefinition;
							docAttr.AggregationType = (int)DocAggregationEnum.BAG; // marker for time series
							docAttr.AggregationLower = "0";
							docAttr.AggregationUpper = "?";
						}
					}
					break;

				case DocPropertyTemplateTypeEnum.COMPLEX:
					if (!String.IsNullOrEmpty(this.PrimaryDataType))
					{
						//...
					}
					break;
			}

			return docAttr;
		}
		protected override string xmlid() { return (PropertyType == DocPropertyTemplateTypeEnum.COMPLEX ? "zz" : "") + base.xmlid(); }
		protected override string folderName() { return xmlid(); }

		internal DocChangeAction DetectChanges(DocProperty baseProperty)
		{
			DocChangeAction changeAction = new DocChangeAction() { Name = Name };
			if (PropertyType != baseProperty.PropertyType)
			{
				DocChangeAspect docAspect = new DocChangeAspect(DocChangeAspectEnum.INSTANTIATION, baseProperty.PropertyType.ToString(), PropertyType.ToString());
				changeAction.Aspects.Add(docAspect);
				changeAction.Action = DocChangeActionEnum.MODIFIED;
			}

			if (PrimaryDataType != null && baseProperty.PrimaryDataType != null &&
				!PrimaryDataType.Trim().Equals(baseProperty.PrimaryDataType.Trim()))
			{
				DocChangeAspect docAspect = new DocChangeAspect(DocChangeAspectEnum.TYPE, baseProperty.PrimaryDataType, PrimaryDataType);
				changeAction.Aspects.Add(docAspect);
			}

			if (changeAction.Aspects.Count > 0)
				changeAction.Action = DocChangeActionEnum.MODIFIED;
			if (changeAction.Action != DocChangeActionEnum.NOCHANGE)
				return changeAction;

			foreach (DocProperty element in Elements)
			{
				//	docpropert
				//	DocChangeAction elementChange = elemen
			}
			return null;
		}
	}

	public enum DocPropertyTemplateTypeEnum
	{
		P_SINGLEVALUE = 0,
		P_ENUMERATEDVALUE = 1,
		P_BOUNDEDVALUE = 2,
		P_LISTVALUE = 3,
		P_TABLEVALUE = 4,
		P_REFERENCEVALUE = 5,

		COMPLEX = 6,
	}

	// new in IFCDOC 5.8
	public class DocPropertyEnumeration : DocObject
	{
		[DataMember(Order = 0)] [XmlArray] public List<DocPropertyConstant> Constants { get; protected set; }

		public DocPropertyEnumeration()
		{
			this.Constants = new List<DocPropertyConstant>();
		}

		internal DocPropertyConstant GetConstant(string p)
		{
			foreach (DocPropertyConstant docQuantity in this.Constants)
			{
				if (docQuantity.Name.Equals(p))
					return docQuantity;
			}

			return null;
		}

		public DocEnumeration ToEnumeration()
		{
			DocEnumeration docEnum = new DocEnumeration();
			docEnum.CopyFrom(this);

			foreach (DocPropertyConstant docPropConst in this.Constants)
			{
				DocConstant docConst = new DocConstant();
				docConst.CopyFrom(docPropConst);
				docEnum.Constants.Add(docConst);
			}

			return docEnum;
		}
		protected override string xmlid() { return Name; }
	}

	// new in IFCDOC 5.8
	public class DocPropertyConstant : DocObject
	{
		[IgnoreDataMember] [XmlIgnore] [InverseProperty("Constants")] public HashSet<DocPropertyEnumeration> PartOfEnumeration { get; protected set; } = new HashSet<DocPropertyEnumeration>();
		protected override string folderName() { return xmlid(); }
	}

	/// <summary>
	/// Quantity set definition
	/// </summary>
	public class DocQuantitySet : DocVariableSet
	{
		[DataMember(Order = 0)] [XmlArray] public List<DocQuantity> Quantities { get; protected set; }

		public DocQuantitySet()
		{
			this.Quantities = new List<DocQuantity>();
		}

		internal DocQuantity RegisterQuantity(string p, DocProject project)
		{
			foreach (DocQuantity docQuantity in this.Quantities)
			{
				if (docQuantity.Name.Equals(p))
					return docQuantity;
			}

			DocQuantity q = new DocQuantity();
			this.Quantities.Add(q);
			q.Name = p;
			project.Quantities.Add(q);
			return q;
		}

		internal DocQuantity GetQuantity(string p)
		{
			foreach (DocQuantity docQuantity in this.Quantities)
			{
				if (docQuantity.Name.Equals(p))
					return docQuantity;
			}

			return null;
		}

		public DocEntity ToEntity(Dictionary<string, DocObject> mapDefs)
		{
			DocEntity docEnt = new DocEntity();
			docEnt.CopyFrom(this);

			DocObject docApp = null;
			if (mapDefs.TryGetValue(this.ApplicableType, out docApp) && docApp is DocEntity)
			{
				docEnt.BaseDefinition = docApp.Name; // link through base type

				docEnt.Code = "Qset"; // mark it
			}

			foreach (DocQuantity docProp in this.Quantities)
			{
				DocAttribute docAttr = docProp.ToAttribute(mapDefs);
				if (docAttr != null)
				{
					docEnt.Attributes.Add(docAttr);
				}
			}

			return docEnt;
		}

		protected override string xmlid() { return Name; } 
	}

	/// <summary>
	/// Quantity definition
	/// </summary>
	public class DocQuantity : DocObject
	{
		[DataMember(Order = 0)] [XmlAttribute] public DocQuantityTemplateTypeEnum QuantityType { get; set; } // IfcQuantityWeight, IfcQuantityLength, etc.
		[DataMember(Order = 1)] [XmlAttribute] public DocStateEnum AccessState { get; set; } // V10.5

		[IgnoreDataMember] [XmlIgnore] [InverseProperty("Quantities")] public HashSet<DocQuantitySet> PartOfQset { get; protected set; } = new HashSet<DocQuantitySet>();

		public string GetEntityName()
		{
			string propclass = "IfcQuantityCount";
			switch (this.QuantityType)
			{
				case DocQuantityTemplateTypeEnum.Q_AREA:
					propclass = "IfcQuantityArea";
					break;

				case DocQuantityTemplateTypeEnum.Q_COUNT:
					propclass = "IfcQuantityCount";
					break;

				case DocQuantityTemplateTypeEnum.Q_LENGTH:
					propclass = "IfcQuantityLength";
					break;

				case DocQuantityTemplateTypeEnum.Q_TIME:
					propclass = "IfcQuantityTime";
					break;

				case DocQuantityTemplateTypeEnum.Q_VOLUME:
					propclass = "IfcQuantityVolume";
					break;

				case DocQuantityTemplateTypeEnum.Q_WEIGHT:
					propclass = "IfcQuantityWeight";
					break;
			}

			return propclass;
		}

		public DocAttribute ToAttribute(Dictionary<string, DocObject> mapDefs)
		{
			DocAttribute docAttr = new DocAttribute();
			docAttr.CopyFrom(this);

			string quantityentity = this.GetEntityName();

			// get type of nominal value, such that it is compatible with dictionary, C#
			DocObject docDef = null;
			if (mapDefs.TryGetValue(quantityentity, out docDef) && docDef is DocEntity)
			{
				DocEntity docEnt = (DocEntity)docDef;

				DocObject docVal = null;
				if (docEnt.Attributes.Count > 0 && mapDefs.TryGetValue(docEnt.Attributes[0].DefinedType, out docVal) && docVal is DocDefined)
				{
					docAttr.Definition = docVal as DocDefinition;
					docAttr.DefinedType = docVal.Name;
				}
			}

			return docAttr;
		}
		protected override string folderName() { return xmlid(); }
	}

	public enum DocQuantityTemplateTypeEnum
	{
		Q_LENGTH = 11,
		Q_AREA = 12,
		Q_VOLUME = 13,
		Q_COUNT = 14,
		Q_WEIGHT = 15,
		Q_TIME = 16
	}

	public class DocChangeSet : DocObject
	{
		[DataMember(Order = 0)] [XmlArray] public List<DocChangeAction> ChangesEntities { get; protected set; } // nested hierarchy: section / schema / entity / attribute
		[DataMember(Order = 1)] [XmlAttribute] public string VersionCompared { get; set; } // null means same version as project
		[DataMember(Order = 2)] [XmlAttribute] public string VersionBaseline { get; set; } // identifer of the baseline (takes on file name of compared ifcdoc file)
		[DataMember(Order = 3)] [XmlArray] public List<DocChangeAction> ChangesProperties { get; protected set; } // IFCDOC v5.2
		[DataMember(Order = 4)] [XmlArray] public List<DocChangeAction> ChangesQuantities { get; protected set; } // IFCDOC v5.2
		[DataMember(Order = 5)] [XmlArray] public List<DocChangeAction> ChangesViews { get; protected set; } // IFCDOC V11.5

		public DocChangeSet()
		{
			this.ChangesEntities = new List<DocChangeAction>();
			this.ChangesProperties = new List<DocChangeAction>();
			this.ChangesQuantities = new List<DocChangeAction>();
			this.ChangesViews = new List<DocChangeAction>();
		}
	}

	// Name identifies item
	public class DocChangeAction : DocObject
	{
		[DataMember(Order = 0, IsRequired = false)] [XmlAttribute] public DocChangeActionEnum Action { get; set; } 
		[DataMember(Order = 1)] [XmlArray] public List<DocChangeAspect> Aspects { get; protected set; } // modifications
		[DataMember(Order = 2)] [XmlArray] public List<DocChangeAction> Changes { get; protected set; } // nested changes
		[DataMember(Order = 3, IsRequired = false)] [XmlAttribute] public bool ImpactSPF { get; set; } // not upward compatible with SPF   
		[DataMember(Order = 4, IsRequired = false)] [XmlAttribute] public bool ImpactXML { get; set; } // not upward compatible with XML

		public DocChangeAction()
		{
			this.Aspects = new List<DocChangeAspect>();
			this.Changes = new List<DocChangeAction>();
		}

		public DocChangeAction Copy()
		{
			DocChangeAction clone = new DocChangeAction();
			clone.Name = this.Name;
			clone.Documentation = this.Documentation;
			clone.Code = this.Code;
			clone.Action = this.Action;
			clone.ImpactSPF = this.ImpactSPF;
			clone.ImpactXML = this.ImpactXML;

			foreach (DocChangeAspect aspect in this.Aspects)
			{
				DocChangeAspect newaspect = new DocChangeAspect(aspect.Aspect, aspect.OldValue, aspect.NewValue);
				clone.Aspects.Add(newaspect);
			}

			foreach (DocChangeAction change in this.Changes)
			{
				DocChangeAction subaction = change.Copy();
				clone.Changes.Add(subaction);
			}

			return clone;
		}

		/// <summary>
		/// Indicates if this action or any subactions have changes.
		/// Used to hide records that don't contain any changes
		/// </summary>
		/// <returns></returns>
		public bool HasChanges()
		{
			if (this.Action != DocChangeActionEnum.NOCHANGE)
				return true;

			foreach (DocChangeAction sub in this.Changes)
			{
				if (sub.HasChanges())
				{
					return true;
				}
			}

			return false;
		}
	}

	public enum DocChangeActionEnum
	{
		NOCHANGE = 0, // no direct change, however subitems may have changes
		ADDED = 1,
		DELETED = 2,
		MODIFIED = 4,
		MOVED = 5, // moved from another schema
	}

	public class DocChangeAspect : SEntity
	{
		[DataMember(Order = 0)] [XmlAttribute] public DocChangeAspectEnum Aspect { get; set; }
		[DataMember(Order = 1)] [XmlAttribute] public string OldValue { get; set; }
		[DataMember(Order = 2)] [XmlAttribute] public string NewValue { get; set; }

		public DocChangeAspect()
		{
		}

		public DocChangeAspect(DocChangeAspectEnum aspect, string oldval, string newval)
		{
			this.Aspect = aspect;
			this.OldValue = oldval;
			this.NewValue = newval;
		}

		public override string ToString()
		{
			string display = this.Aspect.ToString().Substring(0, 1) + this.Aspect.ToString().ToLower().Substring(1);

			if (!String.IsNullOrEmpty(NewValue) && !String.IsNullOrEmpty(OldValue))
			{
				return display + " changed from <i>" + OldValue + "</i> to <i>" + NewValue + "</i>. ";
			}
			else if (NewValue != null)
			{
				return display + " changed to <i>" + NewValue + "</i>. ";
			}
			else if (OldValue != null)
			{
				return display + " changed from <i>" + OldValue + "</i>. ";
			}

			return this.Aspect.ToString();
		}
	}

	public enum DocChangeAspectEnum
	{
		NAME = 1,
		TYPE = 2,
		INSTANTIATION = 3,
		AGGREGATION = 4,
		SCHEMA = 5,
		XSDFORMAT = 6, // New in IfcDoc V10.6
		XSDTAGLESS = 7, // New in IfcDoc V10.6
		STATUS = 8, // New in IfcDoc V11.4
	}

	public class DocExample : DocVariableSet // inherited in 4.2 to contain ApplicableType (was DocObject)
	{
		[DataMember(Order = 0)] [XmlArray] [XmlArrayItem(NestingLevel = 1)] public List<DocExample> Examples { get; protected set; } // added in 4.3
		[DataMember(Order = 1)] [XmlArray] public List<DocTemplateDefinition> ApplicableTemplates { get; protected set; } // added in 4.9
		[DataMember(Order = 2)] [XmlElement] public DocModelView ModelView { get; set; }// added in 5.3; deprecated in 7.8 (replaced with collection that follows)
		[DataMember(Order = 3)] [XmlElement] [FileExtensions(Extensions = ".ifc"), DataType(DataType.MultilineText)] public byte[] File { get; set; } // added in 7.2 - encoded data of file in IFC format -- if stored internally
		[DataMember(Order = 4)] [XmlArray] public List<DocModelView> Views { get; protected set; } // added in 7.8
		[DataMember(Order = 5)] [XmlAttribute] public string Path { get; set; }  // path to external file which may be IFC or some other format to be converted -- added in V11.2

		public DocExample()
		{
			this.Examples = new List<DocExample>();
			this.ApplicableTemplates = new List<DocTemplateDefinition>();
			this.Views = new List<DocModelView>();
		}

		protected internal override void FindQuery(string query, bool searchtext, List<DocFindResult> results)
		{
			base.FindQuery(query, searchtext, results);

			foreach (DocExample docEx in this.Examples)
			{
				docEx.FindQuery(query, searchtext, results);
			}
		}


		public void SortExample()
		{
			DocObject.ComparerDocObject comparer = new DocObject.ComparerDocObject();
			Examples.Sort(comparer);
			foreach (DocExample example in Examples)
				example.SortExample();
			ApplicableTemplates.Sort(comparer);
			Views.Sort(comparer);
		}
	}

	/// <summary>
	/// A definition that can be formatted as a tree
	/// </summary>
	public interface IDocTreeHost
	{
		List<DocLine> Tree { get; }
		void InitializeTree();
	}

	public enum DocExpressType
	{
		OBJECT = 0,
		BOOLEAN = 1,
		LOGICAL = 2,
		INTEGER = 3,
		REAL = 4,
		NUMBER = 5,
		STRING = 6,
		BINARY = 7,
	}

	/// <summary>
	/// Used for searching objects; not persistent
	/// </summary>
	public class DocFindResult
	{
		DocObject _Target;
		string _Locale; // null for default
		int _Offset; // -1 if object
		int _Length; // 0 of object

		public DocFindResult(DocObject target, string locale, int offset, int length)
		{
			this._Target = target;
			this._Locale = locale;
			this._Offset = offset;
			this._Length = length;
		}

		public DocObject Target
		{
			get
			{
				return this._Target;
			}
		}

		public string Locale
		{
			get
			{
				return this._Locale;
			}
		}

		public int Offset
		{
			get
			{
				return this._Offset;
			}
		}

		public int Length
		{
			get
			{
				return this._Length;
			}
		}
	}

	[Flags]
	public enum DocDefinitionScopeEnum
	{
		None = 0,

		Type = 0x10,
		TypeConstant = 0x01,

		Entity = 0x20,
		EntityAttribute = 0x02,

		Rule = 0x40,
		RuleWhere = 0x04,

		Pset = 0x1000,
		PsetProperty = 0x0100,

		PEnum = 0x2000,
		PEnumConstant = 0x0200,

		Qset = 0x4000,
		QsetQuantity = 0x0400,

		//... complex property definitions...

		Default = 0xFFFF,
	}

	/// <summary>
	/// Formats schema information
	/// </summary>
	public interface IFormatExtension
	{
		/// <summary>
		/// Formats an entity data type.
		/// </summary>
		/// <param name="docEntity"></param>
		/// <returns></returns>
		string FormatEntity(DocEntity docEntity, Dictionary<string, DocObject> map, Dictionary<DocObject, bool> included);

		/// <summary>
		/// Formats an enumeration data type.
		/// </summary>
		/// <param name="docEnumeration"></param>
		/// <returns></returns>
		string FormatEnumeration(DocEnumeration docEnumeration, Dictionary<string, DocObject> map, Dictionary<DocObject, bool> included);

		/// <summary>
		/// Formats a select data type
		/// </summary>
		/// <param name="docSelect"></param>
		/// <returns></returns>
		string FormatSelect(DocSelect docSelect, Dictionary<string, DocObject> map, Dictionary<DocObject, bool> included);

		/// <summary>
		/// Formats a defined data type
		/// </summary>
		/// <param name="docDefined"></param>
		/// <returns></returns>
		string FormatDefined(DocDefined docDefined, Dictionary<string, DocObject> map, Dictionary<DocObject, bool> included);

		/// <summary>
		/// Formats all schema definitions within project
		/// </summary>
		/// <param name="docProject"></param>
		/// <returns></returns>
		string FormatDefinitions(DocProject docProject, DocPublication docPublication, Dictionary<string, DocObject> map, Dictionary<DocObject, bool> included);
	}

	[Flags]
	public enum DocCodeEnum
	{
		Default = 0,

		Entities = 1,
		Attributes = 2,
		Rules = 4,
		Functions = 8,

		Documentation = 0x10,
		Localization = 0x20,
		Diagrams = 0x40,

		Views = 0x100,

		Examples = 0x1000,

		All = 0x7FFFFFFF,
	}

	public enum DiagramFormat
	{
		ExpressG = 0,
		UML = 1,
	}
}
