// Name:        FormatHtm.cs
// Description: HTML document generation
// Author:      Tim Chipman
// Origination: Work performed for BuildingSmart by Constructivity.com LLC.
// Copyright:   (c) 2010 BuildingSmart International Ltd.
// License:     http://www.buildingsmart-tech.org/legal

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

using HtmlAgilityPack;

using IfcDoc.Schema.DOC;

namespace IfcDoc.Format.HTM
{
	public class FormatHTM :
		IDisposable,
		IComparer<string>
	{
		Stream m_stream;
		StreamWriter m_writer;
		Dictionary<string, DocObject> m_mapEntity;
		Dictionary<string, string> m_mapSchema;
		bool m_anchors; // if true, hyperlinks are anchors within same page; if false, hyperlinks go to documentation
		Dictionary<DocObject, bool> m_included;

		const string BEGIN_KEYWORD = "<span class=\"keyword\">";
		const string END_KEYWORD = "</span>";

		public FormatHTM(string path, Dictionary<string, DocObject> mapEntity, Dictionary<string, string> mapSchema, Dictionary<DocObject, bool> included)
		{
			string dirpath = System.IO.Path.GetDirectoryName(path);
			if (!Directory.Exists(dirpath))
			{
				Directory.CreateDirectory(dirpath);
			}

			this.m_stream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
			this.m_writer = new StreamWriter(this.m_stream, Encoding.UTF8);

			this.m_mapEntity = mapEntity;
			this.m_mapSchema = mapSchema;
			this.m_included = included;
		}

		public Stream Stream
		{
			get
			{
				return this.m_stream;
			}
		}

		public bool UseAnchors
		{
			get
			{
				return this.m_anchors;
			}
			set
			{
				this.m_anchors = value;
			}
		}

		public Dictionary<DocObject, bool> Included
		{
			get
			{
				return this.m_included;
			}
		}

		#region IDisposable Members

		public void Dispose()
		{
			if (this.m_writer != null)
			{
				this.m_writer.Flush();
				this.m_writer.Close();
			}

			if (this.m_stream != null)
			{
				this.m_stream.Close();
				this.m_stream = null;
			}
		}

		#endregion

		public void WriteHeader(string title, int level, string pageheader)
		{
			WriteHeader(title, level, 0, 0, 0, 0, pageheader);
		}

		/// <summary>
		/// Writes opening HTML
		/// </summary>
		/// <param name="title">Caption</param>
		/// <param name="level">Number of levels deep (for referencing style sheet)</param>
		public void WriteHeader(string title, int level, int section, int schema, int category, int definition, string pageheader)
		{
			string style = "";

			for (int i = 0; i < level; i++)
			{
				style += "../";
			}

			this.m_writer.WriteLine("<!DOCTYPE HTML>\r\n");
			this.m_writer.WriteLine("<html>");
			this.m_writer.WriteLine("  <head>");
			this.m_writer.WriteLine("    <meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\">\r\n");
			this.m_writer.WriteLine("    <link rel=\"stylesheet\" type=\"text/css\" href=\"" + style + "ifc-styles.css\">\r\n");
			this.m_writer.WriteLine("    <link rel=\"stylesheet\" type=\"text/css\" href=\"" + style + "details-shim.css\">");
			this.m_writer.WriteLine("    <script type=\"text/javascript\" src=\"" + style + "details-shim.js\"></script>");
			this.m_writer.WriteLine("    <title>" + title + "</title>\r\n");

			this.m_writer.WriteLine("    <style type=\"text/css\">");
			this.m_writer.WriteLine("      body { counter-reset: index1 " + section + " index2 " + schema + " index3 " + category + " index4 " + (definition - 1) + " index5 0; }");
			this.m_writer.WriteLine("    </style>");

			this.m_writer.WriteLine("  </head>\r\n");
			this.m_writer.WriteLine("  <body>\r\n");

			if (!String.IsNullOrEmpty(pageheader))
			{
				this.m_writer.WriteLine("<p>" + pageheader + "</p>");
			}
		}


		/// <summary>
		/// Old function for compatibility
		/// </summary>
		/// <param name="title"></param>
		/// <param name="section"></param>
		/// <param name="schema"></param>
		/// <param name="definition"></param>
		/// <param name="header"></param>
		public void WriteHeader(string title, int section, int schema, int category, int definition, string header)
		{
			int level = 0;

			if (schema == 0)
			{
				// section page
				level = 1;
			}
			else if (section < 0)
			{
				// annex
				if (section == -2 || section == -3)
				{
					level = 2;
				}
				else
				{
					level = 3;
				}
			}
			else if (section == 1)
			{
				// views
				level = 3;
			}
			else if (section == 4)
			{
				level = 2;
			}
			else if (definition > 0)
			{
				level = 3;
			}
			else if (schema > 0)
			{
				level = 2;
			}
			else if (section > 0)
			{
			}
			else if (title != "Table of Contents" && title != "Index")
			{
				level = 2;
			}

			WriteHeader(title, level, section, schema, category, definition, header);
		}

		/// <summary>
		/// Writes closing tags
		/// </summary>
		public void WriteFooter(string footer)
		{
			if (footer != null)
			{
				this.m_writer.WriteLine("<p>" + footer + "</p>");
			}

			this.m_writer.WriteLine("  </body>");
			this.m_writer.WriteLine("</html>");
		}

		public void Write(string content)
		{
			this.m_writer.Write(content);
		}

		public void WriteLine(string content)
		{
			this.m_writer.WriteLine(content);
		}
		public void WriteDefinition(string definition)
		{
			WriteDefinition(definition, "../../");
		}
		public void WriteDefinition(string definition, string urlprefix)
		{
			string format = FormatDefinition(definition, urlprefix);
			WriteLine(format);
		}

#if false // unused???
        /// <summary>
        /// Encodes HTML, preserves tabs and new lines, and generates hyperlinks for IFC references
        /// </summary>
        /// <param name="rawtext"></param>
        public void WriteFormatted(string rawtext)
        {
            if (rawtext == null)
                return;

            string html = System.Web.HttpUtility.HtmlEncode(rawtext.ToString());
            html = html.Replace("\r\n", "<br/>\r\n");
            html = html.Replace("\t", "&nbsp;");

            StringBuilder sb = new StringBuilder();
            int p = 0; // previous
            int i = html.IndexOf(":Ifc", StringComparison.Ordinal);
            while (i != -1)
            {
                int j = html.IndexOf('&', i + 1); // end-quote converted to &quot
                // j should always be valid here

                string def = html.Substring(i + 1, j - i - 1);
                string fmt = FormatDefinition(def);

                sb.Append(html, p, i - p + 1);
                sb.Append(fmt);

                p = j;
                i = html.IndexOf(":Ifc", i + 1, StringComparison.Ordinal);
            }
            sb.Append(html, p, html.Length - p);

            this.Write(sb.ToString());
        }
#endif

		/// <summary>
		/// Writes text containing definition identifiers -- e.g. schema languages, instance data
		/// </summary>
		/// <param name="rawtext">The text to markup.</param>
		/// <param name="urlprefix">Prefix of URLs to use that points to schema directory of documentation -- may be relative or absolute.</param>
		public void WriteExpression(string rawtext, string urlprefix)
		{
			if (rawtext == null)
				return;

			string html = System.Web.HttpUtility.HtmlEncode(rawtext.ToString());
			html = html.Replace("\r\n", "<br/>\r\n");
			html = html.Replace("\t", "&nbsp;");

			html = FormatExpression(html, urlprefix);
			this.Write(html);
		}

		/// <summary>
		/// Formats a string that may contain identifiers of entities, types, or functions
		/// </summary>
		/// <param name="expression"></param>
		/// <returns></returns>
		public string FormatExpression(string expression, string urlprefix)
		{
			string escaped = expression;// System.Security.SecurityElement.Escape(expression);
			if (escaped != null)
			{
				escaped = escaped.Replace("&apos;", "'");
			}

			int iStart = -1;
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < escaped.Length; i++)
			{
				char ch = escaped[i];
				if (Char.IsLetterOrDigit(ch) && i < escaped.Length - 1)
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

						// uppercase for expressions? hack
						if (identifier.StartsWith("IFC", StringComparison.OrdinalIgnoreCase))
						{
							foreach (string s in this.m_mapEntity.Keys) // slow
							{
								if (s.Equals(identifier, StringComparison.InvariantCultureIgnoreCase))
								{
									identifier = s;
									break;
								}
							}
						}

						if (this.m_mapEntity.ContainsKey(identifier))
						{
							string fmt = FormatDefinition(identifier, urlprefix);
							sb.Append(fmt);
						}
						else
						{
							sb.Append(identifier);
						}
						iStart = -1;
					}

					sb.Append(ch);
				}
			}

			string html = sb.ToString();
			return html;
		}

		private string FormatDefinition(string definition)
		{
			return FormatDefinition(definition, "../../");
		}

		/// <summary>
		/// Writes definition, using hyperlink for IFC-defined type (e.g. IfcCartesianPoint) or bold for primitive type (e.g. INTEGER)
		/// </summary>
		/// <param name="definition"></param>
		private string FormatDefinition(string definition, string urlprefix)
		{
			DocObject ent = null;
			string schema = null;
			if (definition != null && //definition.StartsWith("Ifc") && 
				this.m_mapEntity.TryGetValue(definition, out ent) &&
				this.m_mapSchema.TryGetValue(definition, out schema))
			{
				if (this.m_included == null || this.m_included.ContainsKey(ent))
				{
					if (this.m_anchors)
					{
						return "<a href=\"#" + definition.ToLower() + "\">" + definition + "</a>";
					}
					else
					{
						if (this.m_stream is FileStream && ((FileStream)this.m_stream).Name.Contains("\\annex-a\\"))//EndsWith(".xsd.htm"))
						{
							string hyperlink = @"../../../schema/" + schema.ToLower() + @"/lexical/" + definition.ToLower() + ".htm";
							return "<a href=\"" + hyperlink + "\">" + definition + "</a>";
						}
						else if (this.m_stream is FileStream && ((FileStream)this.m_stream).Name.Contains("\\schema\\templates\\"))
						{
							// hack to force relative directory
							string hyperlink = @"../" + schema.ToLower() + @"/lexical/" + definition.ToLower() + ".htm";
							return "<a href=\"" + hyperlink + "\">" + definition + "</a>";
						}
						else if (ent is DocPropertyEnumeration)
						{
							string hyperlink = urlprefix + @"/penum/" + definition.ToLower() + ".htm";
							return "<a href=\"" + hyperlink + "\">" + definition + "</a>";
						}
						else if (ent is DocPropertySet)
						{
							string hyperlink = urlprefix + schema.ToLower() + @"/pset/" + definition.ToLower() + ".htm";
							return "<a href=\"" + hyperlink + "\">" + definition + "</a>";
						}
						else if (ent is DocQuantitySet)
						{
							string hyperlink = urlprefix + schema.ToLower() + @"/qset/" + definition.ToLower() + ".htm";
							return "<a href=\"" + hyperlink + "\">" + definition + "</a>";
						}
						else //if (ent is DocDefinition)
						{
							string hyperlink = urlprefix + schema.ToLower() + @"/lexical/" + definition.ToLower() + ".htm";
							return "<a href=\"" + hyperlink + "\">" + definition + "</a>";
						}
					}
				}
				else
				{
					return "<span class=\"self-ref\">IfcStrippedOptional</span>";
				}
			}
			else
			{
				return "<span class=\"self-ref\">" + definition + "</span>";
			}
		}

		public void WriteTOC(int indent, string content)
		{
			for (int i = 0; i < indent; i++)
			{
				// ISO document is incorrect - there is no such "&sp5;" symbol
				this.m_writer.Write("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;");
			}

			this.m_writer.Write(content);
			this.m_writer.Write("<br />");
			this.m_writer.WriteLine();
		}

		private void WriteExpressHeader(int indent)
		{
#if false
            this.m_writer.WriteLine("<table width=\"100%\" cellspacing=\"0\" cellpadding=\"0\" class=\"express\">");
            this.m_writer.WriteLine(" <tr valign=\"top\">");
            if (indent > 0)
            {
                this.m_writer.WriteLine("  <td width=\"" + indent.ToString() + "%\">");
            }
            this.m_writer.WriteLine("  <td>");
#endif
		}

		private void WriteExpressFooter()
		{
#if false
            this.m_writer.WriteLine("  </td>");
            this.m_writer.WriteLine(" </tr>");
            this.m_writer.WriteLine("</table>");
#endif
		}

		public void WriteExpressLine(int indent, string content)
		{
			for (int i = 0; i < indent; i++)
			{
				this.m_writer.Write("&nbsp;");
			}

			this.m_writer.Write(content);
			this.m_writer.WriteLine("<br/>");
			/*
            WriteExpressHeader(indent);
            WriteLine(content);
            WriteExpressFooter();
            */
		}

		/// <summary>
		/// Writes EXPRESS attributes for entity definition or as part of inheritance graph
		/// </summary>
		/// <param name="entity">The entity to reflect attributes.</param>
		/// <param name="treeleaf">Optional leaf entity for inheritance diagram; any overridden attributes will be suppressed.</param>
		public void WriteExpressAttributes(DocEntity entity, DocEntity treeleaf)
		{
			bool bInverse = false;
			bool bDerived = false;
			bool bExplicit = false;

			// count attributes first to avoid generating tables unnecessarily (W3C validation)
			if (entity.Attributes != null && entity.Attributes.Count > 0)
			{
				foreach (DocAttribute attr in entity.Attributes)
				{
					if (attr.Derived != null)
					{
						// inverse may also be indicated to hold class
						bDerived = true;
					}
					else if (attr.Inverse != null)
					{
						bInverse = true;
					}
					else
					{
						bExplicit = true;
					}
				}
			}


			// explicit attributes, plus detect any inverse or derived
			if (bExplicit)
			{
				this.WriteExpressHeader(2);
				foreach (DocAttribute attr in entity.Attributes)
				{
					bool bInclude = true;

					// suppress any attribute that is overridden on leaf class
					if (treeleaf != null && treeleaf != entity)
					{
						foreach (DocAttribute derivedattr in treeleaf.Attributes)
						{
							if (derivedattr.Name.Equals(attr.Name))
							{
								bInclude = false;
							}
						}
					}

					if (bInclude)
					{
						if (attr.Inverse == null && attr.Derived == null)
						{
							this.m_writer.Write("&nbsp;&nbsp;");
							this.m_writer.Write(attr.Name);
							this.m_writer.Write(" : ");

							if ((attr.AttributeFlags & 1) != 0)
							{
								this.m_writer.Write(BEGIN_KEYWORD + "OPTIONAL" + END_KEYWORD + " ");
							}

							this.WriteExpressAggregation(attr);

							if (this.m_included == null || this.m_included.ContainsKey(attr))
							{
								this.m_writer.Write(FormatDefinition(attr.DefinedType));
							}
							else
							{
								this.m_writer.Write("<span class=\"self-ref\">IfcStrippedOptional</span>");
							}
							this.m_writer.WriteLine(";<br/>");

						}
					}
				}
				this.WriteExpressFooter();
			}

			// inverse attributes
			if (bInverse)
			{
				this.WriteExpressLine(1, BEGIN_KEYWORD + "INVERSE" + END_KEYWORD);
				this.WriteExpressHeader(2);
				foreach (DocAttribute attr in entity.Attributes)
				{
					DocObject docinvtype = null;
					if (attr.Inverse != null && attr.Derived == null && this.m_mapEntity.TryGetValue(attr.DefinedType, out docinvtype))
					{
						if (this.m_included == null || this.m_included.ContainsKey(docinvtype))
						{
							this.m_writer.Write("&nbsp;&nbsp;");
							this.m_writer.Write(attr.Name);
							this.m_writer.Write(" : ");

							this.WriteExpressAggregation(attr);

							this.m_writer.Write(FormatDefinition(attr.DefinedType));
							this.m_writer.Write(" " + BEGIN_KEYWORD + "FOR" + END_KEYWORD + " ");
							this.m_writer.Write(attr.Inverse);
							this.m_writer.WriteLine(";<br/>");
						}
					}
				}
				this.WriteExpressFooter();
			}

			// derived attributes
			if (bDerived)
			{
				this.WriteExpressLine(1, BEGIN_KEYWORD + "DERIVE" + END_KEYWORD);
				this.WriteExpressHeader(2);

				foreach (DocAttribute attr in entity.Attributes)
				{
					if (attr.Derived != null)
					{
						// determine the superclass having the attribute                        
						DocEntity found = null;
						if (treeleaf == null)
						{
							DocEntity super = entity;
							while (super != null && found == null && super.BaseDefinition != null)
							{
								super = this.m_mapEntity[super.BaseDefinition] as DocEntity;
								if (super != null)
								{
									foreach (DocAttribute docattr in super.Attributes)
									{
										if (docattr.Name.Equals(attr.Name))
										{
											// found class
											found = super;
											break;
										}
									}
								}
							}
						}

						if (found != null)
						{
							// overridden attribute
							this.m_writer.Write("&nbsp;&nbsp;" + BEGIN_KEYWORD + "SELF" + END_KEYWORD + "\\" + found.Name + "." + attr.Name + " : ");
						}
						else
						{
							// non-overridden
							this.m_writer.Write("&nbsp;&nbsp;" + attr.Name + " : ");
						}

						this.WriteExpressAggregation(attr);

						this.m_writer.Write(FormatDefinition(attr.DefinedType));

						this.m_writer.Write(" := ");
						this.m_writer.Write(attr.Derived);
						this.m_writer.WriteLine(";<br/>");

					}
				}
				this.WriteExpressFooter();
			}
		}

		private void WriteAttributeAggregation(DocAttribute attr)
		{
			if (attr.AggregationType == 0)
				return;

			DocAttribute docAggregation = attr;
			while (docAggregation != null)
			{
				if ((docAggregation.AggregationFlag & 2) != 0)
				{
					// unique
					// this.m_writer.Write("~"); // don't show anymore - per email discussion
				}

				switch (docAggregation.AggregationType)
				{
					case 1: // list
						this.m_writer.Write("L[");
						break;

					case 2: // array
						this.m_writer.Write("A[");
						break;

					case 3: // set
						this.m_writer.Write("S[");
						break;

					case 4: // bag
						this.m_writer.Write("B[");
						break;
				}

				if (docAggregation.AggregationUpper != null && docAggregation.AggregationUpper != "0")
				{
					this.m_writer.Write(docAggregation.AggregationLower + ":" + docAggregation.AggregationUpper);
				}
				else if (docAggregation.AggregationLower != null)
				{
					this.m_writer.Write(docAggregation.AggregationLower + ":?");
				}
				else
				{
					this.m_writer.Write("0:?");
				}

				switch (docAggregation.AggregationType)
				{
					case 1: // list
						this.m_writer.Write("]");
						break;

					case 2: // array
						this.m_writer.Write("]");
						break;

					case 3: // set
						this.m_writer.Write("]");
						break;

					case 4: // bag
						this.m_writer.Write("]");
						break;
				}

				// drill in
				docAggregation = docAggregation.AggregationAttribute;

				if (docAggregation != null)
				{
					this.m_writer.Write(" ");
				}
			}
		}


		private void WriteExpressAggregation(DocAttribute attr)
		{
			if (attr.AggregationType == 0)
				return;

			DocAttribute docAggregation = attr;
			while (docAggregation != null)
			{
				switch (docAggregation.AggregationType)
				{
					case 1:
						this.m_writer.Write(BEGIN_KEYWORD + "LIST" + END_KEYWORD + " ");
						break;

					case 2:
						this.m_writer.Write(BEGIN_KEYWORD + "ARRAY" + END_KEYWORD + " ");
						break;

					case 3:
						this.m_writer.Write(BEGIN_KEYWORD + "SET" + END_KEYWORD + " ");
						break;

					case 4:
						this.m_writer.Write(BEGIN_KEYWORD + "BAG" + END_KEYWORD + " ");
						break;
				}

				if (docAggregation.AggregationUpper != null && docAggregation.AggregationUpper != "0")
				{
					this.m_writer.Write("[" + docAggregation.AggregationLower + ":" + docAggregation.AggregationUpper + "] OF ");
				}
				else if (docAggregation.AggregationLower != null)
				{
					this.m_writer.Write("[" + docAggregation.AggregationLower + ":?] " + BEGIN_KEYWORD + "OF" + END_KEYWORD + " ");
				}
				else
				{
					this.m_writer.Write("[0:?] OF ");
					//this.m_writer.Write(BEGIN_KEYWORD + "OF" + END_KEYWORD + " ");
				}

				if ((docAggregation.AggregationFlag & 2) != 0)
				{
					// unique
					this.m_writer.Write(BEGIN_KEYWORD + "UNIQUE" + END_KEYWORD + " ");
				}

				// drill in
				docAggregation = docAggregation.AggregationAttribute;
			}
		}

		public void WriteSummaryHeader(string caption, bool expanded, DocPublication docPublication)
		{
			if (docPublication.ISO)
			{
				this.m_writer.WriteLine("<p><b><u>" + caption + "</u></b></p>");
				return;
			}

			//this.m_writer.Write("<hr />");
			this.m_writer.Write("<details");
			this.m_writer.Write(expanded ? " open=\"open\"" : "");
			this.m_writer.Write("><summary>");
			this.m_writer.Write(caption);
			this.m_writer.Write("</summary>");
			//this.m_writer.WriteLine("<br/>");
		}

		public void WriteSummaryFooter(DocPublication docPublication)
		{
			if (docPublication != null && docPublication.ISO)
			{
				this.m_writer.WriteLine("<p></p>");
				return;
			}

			this.m_writer.WriteLine("</details>");
		}

		public void WriteExpressEntitySpecification(DocEntity entity, bool suppresshistory, DocPublication docPublication)
		{
			this.WriteSummaryHeader("EXPRESS Specification", false, docPublication);
			this.m_writer.WriteLine("<div class=\"express\"><code class=\"express\">");

			// new: comment tag
			if (docPublication != null && docPublication.ISO)
			{
				this.WriteExpressLine(0, "*)");
			}

			this.WriteExpressEntity(entity);

			// Comment tag for ISO STEP documentation requirements
			if (docPublication != null && docPublication.ISO)
			{
				this.WriteExpressLine(0, "(*");
			}

			// link to EXPRESS-G
			WriteExpressDiagram(entity);

			this.m_writer.WriteLine("</code></div>");


#if false // no longer included
            if (isoformat)
            {
                // inheritance
                this.m_writer.WriteLine();
                this.m_writer.WriteLine("<p class=\"spec-head\">Inheritance Graph:</p>");

                this.m_writer.WriteLine("<span class=\"express\">");

                this.WriteExpressLine(0, BEGIN_KEYWORD + "ENTITY" + END_KEYWORD + " " + entity.Name);

                WriteExpressInheritance(entity, entity);

                this.WriteExpressLine(0, BEGIN_KEYWORD + "END_ENTITY;" + END_KEYWORD);

                this.m_writer.WriteLine("</span>");
            }
#endif

			this.WriteSummaryFooter(docPublication);
		}

		public void WriteExpressEntity(DocEntity entity)
		{
			// build up list of subtypes from other schemas
			SortedList<string, DocEntity> subtypes = new SortedList<string, DocEntity>(this); // use custom comparer to match with Visual Express ordering

			// include from other schemas
			foreach (DocObject eachdoc in this.m_mapEntity.Values)
			{
				if (eachdoc is DocEntity)
				{
					DocEntity eachent = (DocEntity)eachdoc;
					if (eachent.BaseDefinition != null && eachent.BaseDefinition.Equals(entity.Name))
					{
						subtypes.Add(eachent.Name, eachent);
					}
				}
			}

			string noteAbstract = null;
			string termEntity = "<b>;</b>";
			string termSuper = "<b>;</b>";
			if ((entity.EntityFlags & 0x20) == 0)
			{
				noteAbstract = BEGIN_KEYWORD + "ABSTRACT" + END_KEYWORD + " ";
				termEntity = null;
			}
			if (subtypes.Count > 0 || (entity.Subtypes != null && entity.Subtypes.Count > 0))
			{
				termEntity = null;
			}
			if (entity.BaseDefinition != null)
			{
				termEntity = null;
				termSuper = null;
			}

			this.WriteExpressLine(0, BEGIN_KEYWORD + "ENTITY" + END_KEYWORD + " " + entity.Name + termEntity);
			if ((entity.EntityFlags & 0x20) == 0 || subtypes.Count > 0 || (entity.Subtypes != null && entity.Subtypes.Count > 0))
			{
				if (subtypes.Count > 0)
				{
					StringBuilder sb = new StringBuilder();

					// Capture all subtypes, not just those within schema
					int countsub = 0;
					foreach (string ds in subtypes.Keys)
					{
						DocEntity refent = subtypes[ds];
						if (this.m_included == null || this.m_included.ContainsKey(refent))
						{
							countsub++;

							if (sb.Length != 0)
							{
								sb.Append(", ");
							}

							sb.Append(this.FormatDefinition(ds));
						}
					}

					if (countsub > 1)
					{
						this.WriteExpressLine(1, noteAbstract + BEGIN_KEYWORD + "SUPERTYPE OF" + END_KEYWORD + "(" + BEGIN_KEYWORD + "ONEOF" + END_KEYWORD + "(" + sb.ToString() + "))" + termSuper);
					}
					else if (countsub == 1)
					{
						this.WriteExpressLine(1, noteAbstract + BEGIN_KEYWORD + "SUPERTYPE OF" + END_KEYWORD + "(" + sb.ToString() + ")" + termSuper);
					}
				}
				else
				{
					this.WriteExpressLine(1, noteAbstract + BEGIN_KEYWORD + "SUPERTYPE" + END_KEYWORD + termSuper);
				}
			}

			if (entity.BaseDefinition != null)
			{
				this.WriteExpressLine(1, BEGIN_KEYWORD + "SUBTYPE OF" + END_KEYWORD + " (" + FormatDefinition(entity.BaseDefinition) + ")<b>;</b>");
			}

			WriteExpressAttributes(entity, null);

			if (entity.UniqueRules != null && entity.UniqueRules.Count > 0)
			{
				this.WriteExpressLine(1, BEGIN_KEYWORD + "UNIQUE" + END_KEYWORD);

				this.WriteExpressHeader(2);
				foreach (DocUniqueRule docWhere in entity.UniqueRules)
				{
					this.m_writer.Write("&nbsp;&nbsp;");
					this.m_writer.Write(docWhere.Name);

					this.m_writer.Write(" : ");
					foreach (DocUniqueRuleItem item in docWhere.Items)
					{
						if (item != docWhere.Items[0])
						{
							this.m_writer.Write(", ");
						}
						this.m_writer.Write(item.Name);
					}
					this.m_writer.Write(";<br/>");
				}

				this.WriteExpressFooter();
			}

			if (entity.WhereRules != null && entity.WhereRules.Count > 0)
			{
				this.WriteExpressLine(1, BEGIN_KEYWORD + "WHERE" + END_KEYWORD);

				this.WriteExpressHeader(2);
				foreach (DocWhereRule docWhere in entity.WhereRules)
				{
					if (docWhere.Name.Equals("AllowedRelatedElements"))
					{
						this.ToString();
					}

					// markup any references to functions...
					//string html = FormatExpression(docWhere.Expression);

					this.m_writer.Write("&nbsp;&nbsp;");
					this.m_writer.Write(docWhere.Name);
					this.m_writer.Write(" : ");
					//this.m_writer.Write(html);
					this.WriteExpression(docWhere.Expression, "../../");
					//this.WriteFormatted(docWhere.Expression);
					this.m_writer.Write(";<br/>");
				}

				this.WriteExpressFooter();
			}

			WriteExpressLine(0, BEGIN_KEYWORD + "END_ENTITY;" + END_KEYWORD);

		}

		public void WriteExpressDiagram(DocDefinition docDef)
		{
			int diagramnumber = docDef.DiagramNumber;
			string schema = null;
			if (this.m_mapSchema.TryGetValue(docDef.Name, out schema))
			{

				// Per ISO doc requirement, use icon to link to diagram
				this.m_writer.WriteLine(
					"<p class=\"std\">" +
					"<a href=\"../../../annex/annex-d/" + schema.ToLower() + "/diagram_" + diagramnumber.ToString("D4") +
					".htm\" ><img src=\"../../../img/diagram.png\" style=\"border: 0px\" title=\"Link to EXPRESS-G diagram\" alt=\"Link to EXPRESS-G diagram\">&nbsp;EXPRESS-G diagram</a>" +
					"</p>");
			}
		}

		// recurses up inheritance chain
		private void WriteExpressInheritance(DocEntity entity, DocEntity treeleaf)
		{
			if (entity.BaseDefinition != null)
			{
				if (this.m_mapEntity.ContainsKey(entity.BaseDefinition))
				{
					DocEntity baseEntity = (DocEntity)this.m_mapEntity[entity.BaseDefinition];
					WriteExpressInheritance(baseEntity, treeleaf);
				}
			}

			WriteExpressLine(1, BEGIN_KEYWORD + "ENTITY" + END_KEYWORD + " " + FormatDefinition(entity.Name));
			WriteExpressAttributes(entity, treeleaf);
		}

		public void WriteExpressTypeAndDocumentation(DocType type, bool suppresshistory, DocPublication docPublication)
		{
			this.WriteSummaryHeader("EXPRESS Specification", false, docPublication);
			this.m_writer.WriteLine("<div class=\"express\"><code class=\"express\">");

			this.WriteExpressHeader(2);

			// Per ISO doc requirement, comment tag
			if (docPublication.ISO)
			{
				this.WriteExpressLine(0, "*)");
			}

			WriteExpressType(type);

			if (docPublication.ISO)
			{
				this.WriteExpressLine(0, "(*");
			}
			WriteExpressFooter();

			this.m_writer.WriteLine("</p>");

			// link to EXPRESS-G
			WriteExpressDiagram(type);

			this.m_writer.WriteLine("</code></div>");
			this.WriteSummaryFooter(docPublication);

		}

		public void WriteExpressType(DocType type)
		{
			if (type is DocEnumeration)
			{
				DocEnumeration docenum = (DocEnumeration)type;

				this.WriteExpressLine(0, BEGIN_KEYWORD + "TYPE" + END_KEYWORD + " " + docenum.Name + " = " + BEGIN_KEYWORD + "ENUMERATION OF" + END_KEYWORD + " (");
				this.WriteExpressHeader(2);

				if (docenum.Constants != null)
				{
					foreach (DocConstant docconst in docenum.Constants)
					{
						this.m_writer.Write("&nbsp;");
						this.m_writer.Write(docconst.Name);

						if (docconst == docenum.Constants[docenum.Constants.Count - 1])
						{
							this.m_writer.Write(");<br/>");
						}
						else
						{
							this.m_writer.Write(", <br/>");
						}
					}
				}

				this.WriteExpressFooter();
			}
			else if (type is DocSelect)
			{
				DocSelect docenum = (DocSelect)type;

				this.WriteExpressLine(0, BEGIN_KEYWORD + "TYPE" + END_KEYWORD + " " + docenum.Name + " = " + BEGIN_KEYWORD + "SELECT" + END_KEYWORD + " (");
				this.WriteExpressHeader(2);

				if (docenum.Selects != null)
				{
					bool previtem = false;
					foreach (DocSelectItem docconst in docenum.Selects)
					{
						DocObject docref = null;
						if (this.m_mapEntity.TryGetValue(docconst.Name, out docref))
						{
							if (this.m_included == null || this.m_included.ContainsKey(docref))
							{
								if (previtem)
								{
									this.m_writer.Write(", <br/>");
								}

								this.m_writer.Write("&nbsp;");
								this.m_writer.Write(this.FormatDefinition(docconst.Name));

								previtem = true;
							}
						}
					}

					if (previtem)
					{
						this.m_writer.Write(");<br/>");
					}
				}

				this.WriteExpressFooter();
			}
			else if (type is DocDefined)
			{
				DocDefined docenum = (DocDefined)type;

				string length = "";
				if (docenum.Length > 0)
				{
					length = " (" + docenum.Length.ToString() + ")";
				}
				else if (docenum.Length < 0)
				{
					int len = -docenum.Length;
					length = " (" + len.ToString() + ") FIXED";
				}

				WriteExpressHeader(0);
				this.m_writer.Write(BEGIN_KEYWORD + "TYPE" + END_KEYWORD + " " + docenum.Name + " = ");

				if (docenum.Aggregation != null)
				{
					this.WriteExpressAggregation(docenum.Aggregation);
				}

				this.m_writer.Write(FormatDefinition(docenum.DefinedType) + length + ";<br/>");
				WriteExpressFooter();

				this.WriteExpressHeader(2);
				if (docenum.WhereRules != null && docenum.WhereRules.Count > 0)
				{
					this.WriteExpressLine(1, BEGIN_KEYWORD + "WHERE" + END_KEYWORD);

					this.WriteExpressHeader(2);
					foreach (DocWhereRule docWhere in docenum.WhereRules)
					{
						string escaped = System.Security.SecurityElement.Escape(docWhere.Expression);
						escaped = escaped.Replace("&apos;", "'");

						this.m_writer.Write("&nbsp;&nbsp;");
						this.m_writer.Write(docWhere.Name);
						this.m_writer.Write(" : " + escaped + "<br/>");
					}

					this.WriteExpressFooter();
				}

				this.WriteExpressFooter();
			}

			WriteExpressLine(0, BEGIN_KEYWORD + "END_TYPE;" + END_KEYWORD);
		}

		public void WriteExpressFunction(DocFunction entity)
		{
			this.WriteLine("<p>");
			this.WriteLine(BEGIN_KEYWORD + "FUNCTION" + END_KEYWORD + " " + entity.Name + "<br/>\r\n");

			string escaped = entity.Expression;

			escaped = System.Security.SecurityElement.Escape(escaped);
			escaped = escaped.Replace("&apos;", "'");
			escaped = FormatExpression(escaped, "../../");

			escaped = escaped.Replace("\r\n", "<br/>");
			escaped = escaped.Replace("    ", "&nbsp;&nbsp;&nbsp;&nbsp;");
			escaped = escaped.Replace("   ", "&nbsp;&nbsp;&nbsp;");
			escaped = escaped.Replace("  ", "&nbsp;&nbsp;");

			this.WriteLine(escaped);

			this.WriteLine("<br/>\r\n");
			this.WriteLine(BEGIN_KEYWORD + "END_FUNCTION" + END_KEYWORD + ";\r\n");
			this.WriteLine("</p>\r\n");
		}

		public void WriteExpressGlobalRule(DocGlobalRule entity)
		{
			this.WriteLine("<p>");
			this.WriteLine(BEGIN_KEYWORD + "RULE" + END_KEYWORD + " " + entity.Name + " <b>FOR</b> (");
			this.WriteDefinition(entity.ApplicableEntity);
			this.WriteLine(");<br/>\r\n");

			if (!String.IsNullOrEmpty(entity.Expression))
			{
				string escaped = entity.Expression;
				escaped = System.Security.SecurityElement.Escape(escaped);
				escaped = escaped.Replace("&apos;", "'");

				escaped = escaped.Replace("\r\n", "<br/>");
				escaped = escaped.Replace("    ", "&nbsp;&nbsp;&nbsp;&nbsp;");
				escaped = escaped.Replace("   ", "&nbsp;&nbsp;&nbsp;");
				escaped = escaped.Replace("  ", "&nbsp;&nbsp;");

				this.WriteLine(escaped);

				this.WriteLine("<br/>");
			}

			if (entity.WhereRules != null && entity.WhereRules.Count > 0)
			{
				this.WriteLine("&nbsp;&nbsp;&nbsp;&nbsp;" + BEGIN_KEYWORD + "WHERE" + END_KEYWORD + "<br/>\r\n");
				foreach (DocWhereRule docWhere in entity.WhereRules)
				{
					string escaped = System.Security.SecurityElement.Escape(docWhere.Expression);
					escaped = escaped.Replace("&apos;", "'");// must unescape for HTML (instead of XML)
					this.WriteLine("&nbsp;&nbsp;&nbsp;&nbsp;");
					this.WriteLine(docWhere.Name);
					this.WriteLine(" : " + escaped + "<br/>\r\n");
				}
			}

			this.WriteLine(BEGIN_KEYWORD + "END_RULE" + END_KEYWORD + ";");

			this.WriteLine("</p>");
		}

		/// <summary>
		/// Writes alphabetical listing according to a specific locale
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="caption">The caption to use, e.g. "Entities"</param>
		/// <param name="locale">The locale for which to generate listings or NULL for default locale.</param>
		public void WriteLocalizedListing<T>(string caption, string locale, string path, string name, DocPublication docPublication) where T : DocObject
		{
			SortedList<string, T> alphaMissing = new SortedList<string, T>();

			int count = 0;
			SortedList<string, T> alphaEntity = new SortedList<string, T>();
			foreach (string s in this.m_mapEntity.Keys)
			{
				DocObject obj = this.m_mapEntity[s];
				if (obj is T && (this.m_included == null || this.m_included.ContainsKey(obj)))
				{
					count++;

					if (locale == null)
					{
						// default locale (IFC4 uses British English as default locale)
						alphaEntity.Add(obj.Name, (T)obj);
					}
					else if (obj.Localization != null)
					{
						bool exists = false;

						// find specific locale
						foreach (DocLocalization docLocal in obj.Localization)
						{
							if (docLocal.Locale != null && docLocal.Locale.Equals(locale, StringComparison.OrdinalIgnoreCase) && docLocal.Name != null && !alphaEntity.ContainsKey(docLocal.Name))
							{
								// found it
								alphaEntity.Add(docLocal.Name, (T)obj);
								exists = true;
								break;
							}
						}

						if (!exists)
						{
							//System.Diagnostics.Debug.WriteLine("[" + locale + "] Missing transation: " + obj.Name);
							alphaMissing.Add(obj.Name, (T)obj);
						}
					}
				}
			}

			if (locale == null)
			{
				locale = "en"; // default
			}

			string localeheader = locale.ToUpper();
			System.Globalization.CultureInfo cultureinfo = System.Globalization.CultureInfo.GetCultureInfo(locale);
			if (cultureinfo != null)
			{
				localeheader += " [" + cultureinfo.EnglishName + "]";
			}

			this.WriteHeader(localeheader, 3, docPublication.Header);
			this.WriteLine("<h2 class=\"annex\">" + caption + " (" + alphaEntity.Count.ToString() + " translations out of " + count + ")</h2>");
			this.WriteLine("<ul class=\"std\">");

			foreach (string key in alphaEntity.Keys)
			{
				T entity = alphaEntity[key];

				string schema = this.m_mapSchema[entity.Name];

				string hyperlink = null;
				if (entity is DocPropertySet)
				{
					hyperlink = @"../../../schema/" + schema.ToLower() + @"/pset/" + entity.Name.ToLower() + ".htm";
				}
				else if (entity is DocQuantitySet)
				{
					hyperlink = @"../../../schema/" + schema.ToLower() + @"/qset/" + entity.Name.ToLower() + ".htm";
				}
				else
				{
					hyperlink = @"../../../schema/" + schema.ToLower() + @"/lexical/" + entity.Name.ToLower() + ".htm";
				}
				this.WriteLine("<li><a class=\"listing-link\" href=\"" + hyperlink + "\">" + key + "</a></li>\r\n");
			}

			this.WriteLine("</ul>");

			if (alphaMissing.Count > 0)
			{
				this.WriteLine("<p><i>The following definitions do not have translations for this locale: ");
				foreach (string key in alphaMissing.Keys)
				{
					this.WriteLine(key + "; ");
				}
				this.WriteLine("</i></p>");
			}

			string linkid = locale.Substring(0, 2) + "-alphabeticalorder-" + caption.ToLower().Replace(' ', '-');
			this.WriteLinkTo(docPublication, linkid, 3);


			this.WriteFooter(docPublication.Footer);

			using (FormatHTM htmLink = new FormatHTM(path + "/link/" + linkid + ".htm", this.m_mapEntity, this.m_mapSchema, this.m_included))
			{
				htmLink.WriteLinkPage("../annex/annex-b/" + locale.Substring(0, 2).ToLower() + "/alphabeticalorder_" + name + ".htm", docPublication);
			}
		}

		public void WriteAlphabeticalListing<T>(string caption, string path, string name, DocPublication docPublication) where T : DocObject
		{
			SortedList<string, T> alphaEntity = new SortedList<string, T>();
			foreach (string s in this.m_mapEntity.Keys)
			{
				DocObject obj = this.m_mapEntity[s];
				if (obj is T && (this.m_included == null || this.m_included.ContainsKey(obj)))
				{
					// don't add hidden psets
					bool add = true;
					if (obj is DocPropertySet)
					{
						DocPropertySet docPset = (DocPropertySet)obj;
						if (!docPset.IsVisible())
						{
							add = false;
						}
					}

					if (add)
					{
						alphaEntity.Add(s, (T)obj);
					}
				}
			}

			this.WriteHeader("Alphabetical Listing", -2, -1, -1, -1, docPublication.Header);
			this.WriteLine("<h2 class=\"annex\">" + caption + " (" + alphaEntity.Count.ToString() + ")</h2>");
			this.WriteLine("<ul class=\"std\">");

			foreach (string key in alphaEntity.Keys)
			{
				T entity = alphaEntity[key];

				string hyperlink = null;
				if (entity is DocPropertyEnumeration)
				{
					hyperlink = @"../../penum/" + entity.Name.ToLower() + ".htm";
				}
				else
				{
					string schema = this.m_mapSchema[entity.Name];

					if (entity is DocPropertySet)
					{
						hyperlink = @"../../schema/" + schema.ToLower() + @"/pset/" + entity.Name.ToLower() + ".htm";
					}
					else if (entity is DocQuantitySet)
					{
						hyperlink = @"../../schema/" + schema.ToLower() + @"/qset/" + entity.Name.ToLower() + ".htm";
					}
					else
					{
						hyperlink = @"../../schema/" + schema.ToLower() + @"/lexical/" + entity.Name.ToLower() + ".htm";
					}
				}
				this.WriteLine("<li><a class=\"listing-link\" href=\"" + hyperlink + "\">" + entity.Name + "</a></li>\r\n");
			}

			this.WriteLine("</ul>");

			string linkid = "alphabeticalorder-" + caption.ToLower().Replace(' ', '-');
			this.WriteLinkTo(docPublication, linkid, 2);
			this.WriteFooter(docPublication.Footer);

			using (FormatHTM htmLink = new FormatHTM(path + "/link/" + linkid + ".htm", this.m_mapEntity, this.m_mapSchema, this.m_included))
			{
				htmLink.WriteLinkPage("../annex/annex-b/alphabeticalorder_" + name.ToLower() + ".htm", docPublication);
			}
		}

		public void WriteInheritanceMapping(DocProject docProject, DocModelView[] views, DocPublication docPublication)
		{
			SortedList<string, DocEntity> alphaEntity = new SortedList<string, DocEntity>();
			foreach (string s in this.m_mapEntity.Keys)
			{
				DocObject obj = this.m_mapEntity[s];
				if (obj is DocEntity)
				{
					alphaEntity.Add(s, (DocEntity)obj);
				}
			}


			this.WriteHeader("Inheritance Listing", 3, docPublication.Header);
			this.WriteLine("<h2 class=\"annex\">" + "Mappings" + "</h2>");
			this.WriteLine("<table class=\"gridtable\">");

			this.WriteLine("<tr>");
			this.WriteLine("<th>Entity</th>");

			if (views != null)
			{
				Dictionary<DocObject, bool>[] maps = new Dictionary<DocObject, bool>[views.Length];
				for (int iView = 0; iView < views.Length; iView++)
				{
					maps[iView] = new Dictionary<DocObject, bool>();
					DocModelView docView = views[iView];
					docProject.RegisterObjectsInScope(docView, maps[iView]);

					this.WriteLine("<th>" + docView.Code + "</th>");
				}
				this.WriteLine("</tr>");

				WriteInheritanceMappingLevel(null, alphaEntity.Values, maps, 0);
			}
			this.WriteLine("</table>");

			this.WriteFooter(docPublication.Footer);
		}

		public void WriteTemplateTable(DocProject docProject, DocTemplateDefinition docTemplateDefinition, int level, Dictionary<DocObject, bool>[] dictionaryViews)
		{
			bool isTemplateUsed = false;

			string templateUrl = "../schema/templates/" + DocumentationISO.MakeLinkName(docTemplateDefinition) + ".htm";
			string templateAnchor = "<a href=\"" + templateUrl + "\">" + docTemplateDefinition.Name + "</a>";

			StringBuilder sB = new StringBuilder();

			for (int j = 0; j < dictionaryViews.Length; j++)
			{
				Dictionary<DocObject, bool> dictionary = dictionaryViews[j];
				if (dictionary != null)
				{
					sB.Append("<td>");
					if (dictionary.ContainsKey(docTemplateDefinition))
					{
						sB.Append("X");
						isTemplateUsed = true;
					}
					sB.Append("</td>");
				}
			}

			if (dictionaryViews != null && (!isTemplateUsed || !String.IsNullOrEmpty(docTemplateDefinition.Code))) // new: don't display unused templates - only adds clutter
				return;

			this.WriteLine("<tr><td>");
			for (int i = 0; i < level; i++)
			{
				this.WriteLine("&nbsp;");
			}

			if (isTemplateUsed)
			{
				this.WriteLine(templateAnchor);
			}
			else
			{
				this.WriteLine(docTemplateDefinition.Name);
			}
			this.WriteLine("</td>");
			this.WriteLine(sB.ToString());

			this.WriteLine("</tr>");
			foreach (DocTemplateDefinition childTemplateDefinition in docTemplateDefinition.Templates)
			{
				WriteTemplateTable(docProject, childTemplateDefinition, level + 1, dictionaryViews);
			}
		}

		private void WriteInheritanceMappingLevel(string baseclass, IList<DocEntity> list, Dictionary<DocObject, bool>[] maps, int indent)
		{
			bool include;
			foreach (DocEntity entity in list)
			{
				if (entity.BaseDefinition == baseclass)
				{
					string schema = this.m_mapSchema[entity.Name];
					string hyperlink = @"../schema/" + schema.ToLower() + @"/lexical/" + entity.Name.ToLower() + ".htm";

					this.Write("<tr><td>");

					for (int i = 0; i < indent; i++)
					{
						this.Write("&nbsp;&nbsp;");
					}

					bool includeitem = false;
#if false // temp
                    for (int iView = 0; iView < maps.Length; iView++)
                    {
                        if(maps[iView].TryGetValue(entity, out include) && include)
                        {
                            includeitem = true;
                            break;
                        }
                    }
#endif

					if (includeitem)
					{
						this.Write("<a class=\"listing-link\" href=\"" + hyperlink + "\">");
					}
					this.Write(entity.Name);
					if (includeitem)
					{
						this.Write("</a>");
					}
					this.Write("</td>");

					for (int iView = 0; iView < maps.Length; iView++)
					{
						this.Write("<td>");
						if (maps[iView].TryGetValue(entity, out include) && include)
						{
							this.Write("X");
						}
						this.Write("</td>");
					}

					this.WriteLine("</tr>");

					// recurse
					WriteInheritanceMappingLevel(entity.Name, list, maps, indent + 1);

					this.WriteLine("</ul></li>\r\n");
				}
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="baseclass">Name of base class or null for all entities</param>
		public void WriteInheritanceListing(string baseclass, bool predefined, string caption, DocModelView docView, string path, string filename, DocPublication docPublication)
		{
			SortedList<string, DocEntity> alphaEntity = new SortedList<string, DocEntity>();
			foreach (string s in this.m_mapEntity.Keys)
			{
				DocObject obj = this.m_mapEntity[s];
				if (obj is DocEntity && (this.m_included == null || this.m_included.ContainsKey(obj)))
				{
					alphaEntity.Add(s, (DocEntity)obj);
				}
			}

			this.WriteHeader("Inheritance Listing", 3, docPublication.Header);
			this.WriteLine("<h2 class=\"annex\">" + caption + "</h2>");
			this.WriteLine("<ul class=\"std\">");

			WriteInheritanceLevel(baseclass, alphaEntity.Values, predefined);

			this.WriteLine("</ul>");

			string linkid = "inheritance-" + DocumentationISO.MakeLinkName(docView) + "-" + caption.ToLower();
			this.WriteLinkTo(docPublication, linkid, 3);
			this.WriteFooter(docPublication.Footer);

			using (FormatHTM htmLink = new FormatHTM(path + "/link/" + linkid + ".htm", this.m_mapEntity, this.m_mapSchema, this.m_included))
			{
				htmLink.WriteLinkPage("../annex/annex-c/" + DocumentationISO.MakeLinkName(docView) + "/" + filename + ".htm", docPublication);
			}
		}

		private void WriteInheritanceLevel(string baseclass, IList<DocEntity> list, bool predefined)
		{
			foreach (DocEntity entity in list)
			{
				if (entity.BaseDefinition == baseclass)
				{
					string schema = this.m_mapSchema[entity.Name];
					string hyperlink = @"../../../schema/" + schema.ToLower() + @"/lexical/" + entity.Name.ToLower() + ".htm";

					this.Write("<li><a class=\"listing-link\" href=\"" + hyperlink + "\">" + entity.Name + "</a>");

					if (predefined && !baseclass.EndsWith("Type") && this.m_mapEntity.ContainsKey(entity.Name + "Type"))
					{
						DocEntity entType = this.m_mapEntity[entity.Name + "Type"] as DocEntity;
						if (entType != null && list.Contains(entType))
						{
							this.Write(" - <a class=\"listing-link\" href=\"../../../schema/" + schema.ToLower() + @"/lexical/" + entity.Name.ToLower() + "type.htm\">" + entity.Name + "Type</a>");
						}
					}

					this.WriteLine("<ul>");

					if (predefined)
					{
						foreach (DocAttribute attr in entity.Attributes)
						{
							if (attr.Name == "PredefinedType")
							{
								DocEnumeration docEnum = (DocEnumeration)this.m_mapEntity[attr.DefinedType];
								this.WriteLine("<table class=\"inheritanceenum\">");
								foreach (DocConstant docConst in docEnum.Constants)
								{
									this.WriteLine("<tr><td>" + docConst.Name + "</td></tr>");
								}
								this.WriteLine("</table>");
								break;
							}
						}
					}

					// recurse
					WriteInheritanceLevel(entity.Name, list, predefined);

					this.WriteLine("</ul></li>\r\n");
				}
			}
		}

		/// <summary>
		/// Writes ISO documentation by formatting hyperlinks and removing blocks starting with "HISTORY" and "IFC2X4 CHANGE"
		/// </summary>
		/// <param name="content"></param>
		public void WriteDocumentationMarkup(string content, DocObject current, DocPublication docPublication)
		{
			if (content == null)
				return;

			// target="SOURCE" -> target="info" (for transition; need to update vex)
			content = content.Replace("target=\"SOURCE\"", "target=\"info\"");

			int index = 0;

			// force pset and qset links to lowercase (for Linux servers)
			while (index >= 0)
			{
				index = content.IndexOf("/psd/", index, StringComparison.OrdinalIgnoreCase);
				if (index >= 0)
				{
					int tail = content.IndexOf("\"", index);
					if (tail >= 0)
					{
						string url = content.Substring(index, tail - index);
						url = url.Replace("/psd/", "/");
						url = url.Replace("/Pset_", "/pset/pset_");
						url = url.Replace(".xml", ".htm");
						url = url.ToLower();

						content = content.Substring(0, index) + url + content.Substring(tail);
					}

					index++;
				}
			}

			index = 0;
			while (index >= 0)
			{
				index = content.IndexOf("/qto/", index, StringComparison.OrdinalIgnoreCase);
				if (index >= 0)
				{
					int tail = content.IndexOf("\"", index);
					if (tail >= 0)
					{
						string url = content.Substring(index, tail - index);
						url = url.Replace("/qto/", "/");
						url = url.Replace("/Qto_", "/qset/qto_");
						url = url.Replace(".xml", ".htm");
						url = url.ToLower();

						content = content.Substring(0, index) + url + content.Substring(tail);
					}

					index++;
				}
			}

			if (docPublication.HideHistory)
			{
				// remove history and deprecation info
				index = 0;
				while (index >= 0)
				{
					index = content.IndexOf("<blockquote", index, StringComparison.OrdinalIgnoreCase);
					if (index >= 0)
					{
						int end = content.IndexOf("</blockquote>", index, StringComparison.OrdinalIgnoreCase);
						if (end > index)
						{
							string block = content.Substring(index, end - index + 13);
							if (block.Contains("HISTORY") ||
								block.Contains("IFC2X") ||
								block.Contains("IFC2x") ||
								block.Contains("IFC 2X") ||
								block.Contains("IFC 2x") ||
								block.Contains("IFC4"))
							{
								// exclude the block
								content = content.Substring(0, index) + content.Substring(end + 13, content.Length - end - 13);
							}
							else
							{
								index = end;
							}
						}
						else
						{
							index++; // badly formatted documentation; no end block
						}
					}
				}

				// remove excluded sections for ISO (may include nested blocks)
				index = 0;
				while (index >= 0 && index < content.Length)
				{
					index = content.IndexOf("<blockquote class=\"extDef\">", index, StringComparison.OrdinalIgnoreCase);
					if (index >= 0)
					{
						int end = FindEndOfTag(content, index);
						if (end >= 0)
						{
							content = content.Substring(0, index) + content.Substring(end);
						}
						index++;
					}
				}
			}

			// convert <em> to <i> -- prepare for links
			content = content.Replace("<em>", "<i>");
			content = content.Replace("</em>", "</i>");

			// links
			index = 0;
			while (index >= 0)
			{
				string prefix = "";// was "Ifc";
				int prelen = prefix.Length;

				index = content.IndexOf("<i>" + prefix, index, StringComparison.OrdinalIgnoreCase); // capture text, but not any hyperlink surrounding it
				if (index >= 0)
				{
					int end = content.IndexOf("</i>", index, StringComparison.OrdinalIgnoreCase);
					if (end > index)
					{
						string block = content.Substring(index, end - index + prelen + 4);
						string def = content.Substring(index + prelen + 3, end - index - prelen - 3);

						DocObject docDef = null;
						if (this.m_mapEntity.TryGetValue(def, out docDef) && (this.m_included == null || this.m_included.ContainsKey(docDef)))
						{
							// IFC definition exists; 
							if (current is DocSchema || current is DocTemplateDefinition || current is DocSection)
							{
								string schema = null;
								if (def.StartsWith(prefix) && this.m_mapSchema.TryGetValue(def, out schema))
								{
									// only 1 level up
									string hyperlink = schema.ToLower() + @"/lexical/" + def.ToLower() + ".htm";
									if (!(current is DocSection))
									{
										hyperlink = "../" + hyperlink;
									}
									string format = "<a href=\"" + hyperlink + "\">" + def + "</a>";
									content = content.Substring(0, index) + format + content.Substring(end + 4);
								}
								else if (docDef is DocTemplateDefinition)
								{
									string hyperlink = def.ToLower().Replace(' ', '-') + ".htm";
									if (current is DocSection)
									{
										hyperlink = "templates/" + hyperlink;
									}
									string format = "<a href=\"" + hyperlink + "\">" + def + "</a>";
									content = content.Substring(0, index) + format + content.Substring(end + 4);
								}
								else
								{
									// leave as italics
									index++;
								}
							}
							else if (current is DocChangeSet)
							{
								string schema = null;
								if (def.StartsWith(prefix) && this.m_mapSchema.TryGetValue(def, out schema))
								{
									string hyperlink = @"../../../schema/" + schema.ToLower() + @"/lexical/" + def.ToLower() + ".htm";
									string format = "<a href=\"" + hyperlink + "\">" + def + "</a>";
									content = content.Substring(0, index) + format + content.Substring(end + 4);
								}
							}
							else if (current is DocExample)
							{
								string schema = null;
								if (def.StartsWith(prefix) && this.m_mapSchema.TryGetValue(def, out schema))
								{
									string hyperlink = @"../../schema/" + schema.ToLower() + @"/lexical/" + def.ToLower() + ".htm";
									string format = "<a href=\"" + hyperlink + "\">" + def + "</a>";
									content = content.Substring(0, index) + format + content.Substring(end + 4);
								}
							}
							else if (docDef != current)
							{
								// replace it with hyperlink
								string format = this.FormatDefinition(def);
								content = content.Substring(0, index) + format + content.Substring(end + 4);
							}
							else
							{
								// new: use self-ref
								string format = "<span class=\"self-ref\">" + docDef + "</span>";
								content = content.Substring(0, index) + format + content.Substring(end + 4);

								// leave as italics
								index += format.Length;
							}
						}
						else
						{
							index++; // non-existant or misspelled IFC reference
						}
					}
					else
					{
						index++; // bad format
					}

				}

			}

			if (current is DocTemplateDefinition || current is DocExchangeDefinition)
			{
				// skip diagrams
			}
			else
			{
				string relativePath = "../../../";
				if (current is DocPropertyEnumeration)
					relativePath = "../";
				content = Regex.Replace(content, "../(../)+figures", relativePath + "figures");
				int i = content.Length - 1;
				while (i > 0)
				{
					i = content.LastIndexOf("src=", i - 1);
					if (i >= 0)
					{
						int s = content.IndexOf("\"", i);
						int t = content.IndexOf("\"", s + 1);
						if (s >= 0 && t >= 0 && s < t)
						{
							string imgold = content.Substring(s + 1, t - s - 1);
							imgold = imgold.Substring(imgold.LastIndexOf('/') + 1);
							string source = Properties.Settings.Default.InputPathGeneral + "\\" + imgold;
							string target = Properties.Settings.Default.OutputPath + "\\" + DocumentationISO.MakeLinkName(docPublication) + "\\html\\figures\\" + imgold;
							if (current is DocExample)
							{
								source = Properties.Settings.Default.InputPathGeneral + "\\examples\\" + imgold;
								target = Properties.Settings.Default.OutputPath + "\\" + DocumentationISO.MakeLinkName(docPublication) + "\\html\\figures\\examples\\" + imgold;
							}

							target = target.ToLower();

							if (!System.IO.File.Exists(target) &&
								imgold != "diagram.png" &&
								!imgold.StartsWith("mvd-"))
							{
								if (System.IO.File.Exists(source))
								{
									string dirpath = System.IO.Path.GetDirectoryName(target);
									if (!System.IO.Directory.Exists(dirpath))
									{
										System.IO.Directory.CreateDirectory(dirpath);
									}

									// copy it over
									try
									{
										System.IO.File.Copy(source, target);
									}
									catch (Exception xx)
									{
										docPublication.ErrorLog.Add(xx.Message);
									}
								}
								else
								{
									// log error
									string errortext = "Referenced file not found: " + source;
									if (!docPublication.ErrorLog.Contains(errortext))
									{
										docPublication.ErrorLog.Add(errortext);
									}
								}
							}

							//content = content.Substring(0, i + 10) + imgnew + content.Substring(t);
						}
					}
				}
			}

			this.m_writer.Write(content);
		}

		/// <summary>
		/// Returns the position after the end of a tag; expects separate opening and closing tags; deals with nested tags.
		/// Expects less-than and greater-then symbols to be ONLY used for tags (escaped properly if used in strings)
		/// </summary>
		/// <param name="content">String to search</param>
		/// <param name="start">Starting index at the beginning of the tag; the opening bracket.</param>
		/// <returns>Index after the closing bracket, or -1 if no closing bracket (bad html)</returns>
		private static int FindEndOfTag(string content, int start)
		{
			bool parsetag = false;
			int directive = 0; // 0 = inline tag; +1 = open tag; -1 = close tag

			int nest = 0;
			for (int index = start; index < content.Length; index++)
			{
				char ch = content[index];
				if (!parsetag && ch == '<')
				{
					parsetag = true;
					directive = 1; // assume inner tags unless slash exists otherwise
				}
				else if (parsetag && ch == '/')
				{
					if (content[index - 1] == '<')
					{
						directive = -1; // close previous tag
					}
					else
					{
						directive = 0; // same tag
					}
				}
				else if (parsetag && ch == '>')
				{
					// implicit closing for special tags such as <br>
					if (index >= 3 &&
						content[index - 3] == '<' &&
						content[index - 2] == 'b' &&
						content[index - 1] == 'r')
					{
						directive = 0;
					}

					parsetag = false;

					nest += directive;
					if (nest == 0)
					{
						return index + 1;
					}
				}
			}

			return -1; // no closing tag
		}
		internal void WriteScriptToBlank(string path)
		{
			this.Write(
				"\r\n" +
				"<script type=\"text/javascript\">\r\n" +
				"<!--\r\n" +
				"    parent.index.location.replace(\"" + path + "blank.htm\");\r\n" +
				"//-->\r\n" +
				"</script>\r\n");
		}
		internal void WriteScript(int iSection, int iSchema, int iType, int iItem)
		{
			if (iSection < 0)
			{
				// annex
				char chAnnex = (char)('A' - iSection - 1);

				if (iSchema == 0)
				{
					this.WriteLine(
						"\r\n" +
						"<script type=\"text/javascript\">\r\n" +
						"<!--\r\n" +
						"    parent.index.location.replace(\"toc-" + chAnnex.ToString().ToLower() + ".htm\");\r\n" +
						"//-->\r\n" +
						"</script>\r\n");
				}
				else if (chAnnex == 'A' || (chAnnex == 'D' && iSchema == -1) || (chAnnex == 'D' && iSchema == 1 && iType > 0) || (chAnnex == 'D' && iSchema == 2) || chAnnex == 'F')
				{
					// 2 levels up
					this.WriteLine(
						"\r\n" +
						"<script type=\"text/javascript\">\r\n" +
						"<!--\r\n" +
						"    parent.index.location.replace(\"../../toc-" + chAnnex.ToString().ToLower() + ".htm\");\r\n" +
						"//-->\r\n" +
						"</script>\r\n");
				}
				else if (chAnnex == 'E')
				{
					if (iType > 0)
					{
						// 2 levels up (tables)
						this.WriteLine(
							"\r\n" +
							"<script type=\"text/javascript\">\r\n" +
							"<!--\r\n" +
							"    parent.index.location.replace(\"../../toc-" + chAnnex.ToString().ToLower() + ".htm#" + iSchema.ToString() + "\");\r\n" +
							"//-->\r\n" +
							"</script>\r\n");
					}
					else
					{
						// 1 level up
						this.WriteLine(
							"\r\n" +
							"<script type=\"text/javascript\">\r\n" +
							"<!--\r\n" +
							"    parent.index.location.replace(\"../toc-" + chAnnex.ToString().ToLower() + ".htm#" + iSchema.ToString() + "\");\r\n" +
							"//-->\r\n" +
							"</script>\r\n");
					}
				}
				else
				{
					// 1 level up
					this.WriteLine(
						"\r\n" +
						"<script type=\"text/javascript\">\r\n" +
						"<!--\r\n" +
						"    parent.index.location.replace(\"../toc-" + chAnnex.ToString().ToLower() + ".htm\");\r\n" +
						"//-->\r\n" +
						"</script>\r\n");
				}
			}
			else if (iSection == 1 && iSchema >= 1 && iType == 0)
			{
				// top-level section
				this.WriteLine(
					"\r\n" +
					"<script type=\"text/javascript\">\r\n" +
					"<!--\r\n" +
					"    parent.index.location.replace(\"../../toc-1.htm#\");\r\n" +
					"//-->\r\n" +
					"</script>\r\n");
			}
			else if (iSchema == 0)
			{
				// top-level section
				this.WriteLine(
					"\r\n" +
					"<script type=\"text/javascript\">\r\n" +
					"<!--\r\n" +
					"    parent.index.location.replace(\"toc-" + iSection.ToString() + ".htm#\");\r\n" +
					"//-->\r\n" +
					"</script>\r\n");
			}
			else if ((iSection != 4 && iType > 0) ||
				(iSection == 1 && iSchema == 1))// || (iSection == 1 && iSchema == 1))
			{
				string linkprefix = "../";
				if (iSection == 4 && iSchema == 1)
				{
					linkprefix = "";
				}

				this.WriteLine(
					"\r\n" +
					"<script type=\"text/javascript\">\r\n" +
					"<!--\r\n" +
					"    parent.index.location.replace(\"" + linkprefix + "../toc-" + iSection.ToString() + ".htm#" + iSection.ToString() + "." + iSchema.ToString() + "." + iType.ToString() + "." + iItem.ToString() + "\");\r\n" +
					"//-->\r\n" +
					"</script>\r\n");
			}
			else
			{
				string linkprefix = "";
				this.WriteLine(
					"\r\n" +
					"<script type=\"text/javascript\">\r\n" +
					"<!--\r\n" +
					"    parent.index.location.replace(\"" + linkprefix + "../toc-" + iSection.ToString() + ".htm#" + iSection.ToString() + "." + iSchema.ToString() + "\");\r\n" +
					"//-->\r\n" +
					"</script>\r\n");
			}
		}

		internal void WriteContentRefs(List<IfcDoc.DocumentationISO.ContentRef> listFigures, string prefix)
		{
			for (int iFigure = 0; iFigure < listFigures.Count; iFigure++)
			{
				DocObject target = listFigures[iFigure].Page;
				string figurename = listFigures[iFigure].Caption;

				string link = "";
				if (target is DocTemplateDefinition)
				{
					link = "schema/templates/" + DocumentationISO.MakeLinkName(target) + ".htm";
				}
				else if (target is DocEntity || target is DocType)
				{
					string schema = this.m_mapSchema[target.Name];
					link = "schema/" + schema.ToLower() + "/lexical/" + DocumentationISO.MakeLinkName(target) + ".htm";
				}
				else if (target is DocSchema)
				{
					link = "schema/" + target.Name.ToLower() + "/content.htm";
				}
				else if (target is DocExample)
				{
					link = "annex/annex-e/" + DocumentationISO.MakeLinkName(target) + ".htm";
				}

				this.m_writer.WriteLine("<a class=\"listing-link\" href=\"" + link + "\">" + prefix + " " + (iFigure + 1).ToString() + " &mdash; " + figurename + "</a><br />");
			}
		}

		public void WriteChangeItem(DocChangeAction docChange, int level)
		{
			// don't output if no change, and no sub-items have changed
			if (!docChange.HasChanges())
				return;

			bool inverse = (docChange.Status == "INVERSE");

			StringBuilder sb = new StringBuilder();
			sb.Append("<tr>");


			if (level == 0)
			{
				// section
				sb.Append("<td colspan=\"5\"><b>");
				sb.Append(docChange.Name.ToUpper());
				sb.Append("</b></td>");
			}
			else if (level == 1)
			{
				// schema
				sb.Append("<td>&nbsp;&nbsp;<b>");
				sb.Append(docChange.Name.ToUpper());
				sb.Append("</b></td>");
			}
			else if (level == 2)
			{
				sb.Append("<td>&nbsp;&nbsp;&nbsp;&nbsp;");

				// entity or type                
				string schema = null;
				DocObject docobj = null;
				if (this.m_mapSchema.TryGetValue(docChange.Name, out schema) && this.m_mapEntity.TryGetValue(docChange.Name, out docobj) && (this.m_included == null || this.m_included.ContainsKey(docobj)))
				{
					if (docChange.Name.StartsWith("Pset_") || docChange.Name.StartsWith("PEnum_"))
					{
						string hyperlink = @"../../../schema/" + schema.ToLower() + @"/pset/" + docChange.Name.ToLower() + ".htm";
						sb.Append("<a href=\"" + hyperlink + "\">" + docChange.Name + "</a>");
					}
					else if (docChange.Name.StartsWith("Qto_"))
					{
						string hyperlink = @"../../../schema/" + schema.ToLower() + @"/qset/" + docChange.Name.ToLower() + ".htm";
						sb.Append("<a href=\"" + hyperlink + "\">" + docChange.Name + "</a>");
					}
					else
					{
						string hyperlink = @"../../../schema/" + schema.ToLower() + @"/lexical/" + docChange.Name.ToLower() + ".htm";
						sb.Append("<a href=\"" + hyperlink + "\">" + docChange.Name + "</a>");
					}
				}
				else if (docChange.Action == DocChangeActionEnum.DELETED)
				{
					sb.Append(docChange.Name);
				}
				else
				{
					sb.Append(docChange.Name);
					////return;
				}

				sb.Append("</td>");
			}
			else
			{
				sb.Append("<td>");
				for (int i = 0; i < level; i++)
				{
					sb.Append("&nbsp;&nbsp;");
				}

				if (inverse)
				{
					sb.Append("<i>");
				}

				sb.Append(docChange.Name);
				if (inverse)
				{
					sb.Append("</i>");
				}

				sb.Append("</td>");
			}

			if (level > 0)
			{
				// IFC-SPF
				sb.Append("<td>");
				if (docChange.ImpactSPF && !inverse)
				{
					sb.Append("X");
				}
				sb.Append("</td>");

				// IFC-XML
				sb.Append("<td>");
				if (docChange.ImpactXML)
				{
					sb.Append("X");
				}
				sb.Append("</td>");

				// change
				sb.Append("<td>");
				if (docChange.Action != DocChangeActionEnum.NOCHANGE)
				{
					sb.Append(docChange.Action.ToString());
				}
				sb.Append("</td>");

				// description
				sb.Append("<td>");
				foreach (DocChangeAspect docAspect in docChange.Aspects)
				{
					sb.Append(docAspect.ToString());
					sb.Append("<br/>");
				}
				sb.Append("</td>");

				sb.Append("</tr>");
			}

			this.WriteLine(sb.ToString());

			// recurse
			foreach (DocChangeAction docSub in docChange.Changes)
			{
				this.WriteChangeItem(docSub, level + 1);
			}
		}

		public static void WriteTOCforTemplates(List<DocTemplateDefinition> list, int level, string prefix, FormatHTM htmTOC, FormatHTM htmSectionTOC, Dictionary<DocObject, bool> included)
		{
			if (list == null)
				return;

			int iTemplateDef = 0;
			foreach (DocTemplateDefinition docTemplateDef in list)
			{
				if (included == null || included.ContainsKey(docTemplateDef))
				{
					iTemplateDef++;

					string rellink = DocumentationISO.MakeLinkName(docTemplateDef) + ".htm";
					htmTOC.WriteTOC(level, "<a href=\"schema/templates/" + rellink + "\">" + prefix + "." + iTemplateDef.ToString() + " " + docTemplateDef.Name + "</a>");

					if (level == 1)
					{
						htmSectionTOC.WriteLine("<tr><td>&nbsp;</td></tr>");
					}
					htmSectionTOC.WriteLine("<tr class=\"std\"><td class=\"menu\">" + prefix + "." + iTemplateDef.ToString() + " <a name=\"" + prefix + "." + iTemplateDef.ToString() + "\" /><a class=\"listing-link\" target=\"info\" href=\"templates/" + rellink + "\" >" + docTemplateDef.Name + "</a></td></tr>");

					// recurse                    
					WriteTOCforTemplates(docTemplateDef.Templates, level + 1, prefix + "." + iTemplateDef.ToString(), htmTOC, htmSectionTOC, included);
				}
			}
		}

		public void WriteAnchor(DocObject docobj)
		{
			this.WriteLine("<a name=\"" + docobj.Name.ToLower() + "\" />");
		}

		/// <summary>
		/// Writes entire EXPRESS schema as HTML with hyperlinks.
		/// </summary>
		public void WriteExpressSchema(DocProject docProject)
		{
			SortedList<string, DocDefined> mapDefined = new SortedList<string, DocDefined>(this);
			SortedList<string, DocEnumeration> mapEnum = new SortedList<string, DocEnumeration>(this);
			SortedList<string, DocSelect> mapSelect = new SortedList<string, DocSelect>(this);
			SortedList<string, DocEntity> mapEntity = new SortedList<string, DocEntity>(this);
			SortedList<string, DocFunction> mapFunction = new SortedList<string, DocFunction>(this);
			SortedList<string, DocGlobalRule> mapRule = new SortedList<string, DocGlobalRule>(this);

			SortedList<string, DocObject> mapGeneral = new SortedList<string, DocObject>();

			foreach (DocSection docSection in docProject.Sections)
			{
				foreach (DocSchema docSchema in docSection.Schemas)
				{
					if (this.m_included == null || this.m_included.ContainsKey(docSchema))
					{
						foreach (DocType docType in docSchema.Types)
						{
							if (this.m_included == null || this.m_included.ContainsKey(docType))
							{
								if (docType is DocDefined)
								{
									if (!mapDefined.ContainsKey(docType.Name))
									{
										mapDefined.Add(docType.Name, (DocDefined)docType);
									}
								}
								else if (docType is DocEnumeration)
								{
									mapEnum.Add(docType.Name, (DocEnumeration)docType);
								}
								else if (docType is DocSelect)
								{
									mapSelect.Add(docType.Name, (DocSelect)docType);
								}

								if (!mapGeneral.ContainsKey(docType.Name))
								{
									mapGeneral.Add(docType.Name, docType);
								}
							}
						}

						foreach (DocEntity docEnt in docSchema.Entities)
						{
							if (this.m_included == null || this.m_included.ContainsKey(docEnt))
							{
								if (!mapEntity.ContainsKey(docEnt.Name))
								{
									mapEntity.Add(docEnt.Name, docEnt);
								}
								if (!mapGeneral.ContainsKey(docEnt.Name))
								{
									mapGeneral.Add(docEnt.Name, docEnt);
								}
							}
						}

						foreach (DocFunction docFunc in docSchema.Functions)
						{
							if ((this.m_included == null || this.m_included.ContainsKey(docFunc)) && !mapFunction.ContainsKey(docFunc.Name))
							{
								mapFunction.Add(docFunc.Name, docFunc);
							}
						}

						foreach (DocGlobalRule docRule in docSchema.GlobalRules)
						{
							if (this.m_included == null || this.m_included.ContainsKey(docRule))
							{
								mapRule.Add(docRule.Name, docRule);
							}
						}
					}
				}
			}

			string schemaid = docProject.Annexes[1].Code; // Computer interpretable listings                

			this.m_writer.WriteLine("<span class=\"express\">");

			this.m_writer.WriteLine("SCHEMA " + schemaid + ";");
			this.m_writer.WriteLine("<br/>");
			this.m_writer.WriteLine("<br/>");

			// if MVD, export IfcStrippedOptional
#if false
            bool mvd = false;
            if (docProject.ModelViews != null)
            {
                foreach (DocModelView docView in docProject.ModelViews)
                {
                    if (docView.Visible && docView.Exchanges.Count > 0)
                    {
                        mvd = true;
                    }
                }
            }

            if (mvd)
            {
                this.m_writer.WriteLine("TYPE IfcStrippedOptional = BOOLEAN;");
                this.m_writer.WriteLine("<br/>");
                this.m_writer.WriteLine("END_TYPE;");
                this.m_writer.WriteLine("<br/>");
                this.m_writer.WriteLine("<br/>");
            }
#endif

			foreach (DocDefined docDef in mapDefined.Values)
			{
				this.WriteAnchor(docDef);
				this.WriteExpressType(docDef);
				this.WriteLine("<br/>");
			}

			foreach (DocEnumeration docEnum in mapEnum.Values)
			{
				this.WriteAnchor(docEnum);
				this.WriteExpressType(docEnum);
				this.WriteLine("<br/>");
			}

			foreach (DocSelect docSelect in mapSelect.Values)
			{
				this.WriteAnchor(docSelect);
				this.WriteExpressType(docSelect);
				this.WriteLine("<br/>");
			}

			foreach (DocEntity docEntity in mapEntity.Values)
			{
				this.WriteAnchor(docEntity);
				this.WriteExpressEntity(docEntity);
				this.WriteLine("<br/>");
			}

			foreach (DocFunction docFunction in mapFunction.Values)
			{
				this.WriteAnchor(docFunction);
				this.WriteExpressFunction(docFunction);
				this.WriteLine("<br/>");
			}

			foreach (DocGlobalRule docRule in mapRule.Values)
			{
				this.WriteAnchor(docRule);
				this.WriteExpressGlobalRule(docRule);
				this.WriteLine("<br/>");
			}

			this.m_writer.WriteLine("END_SCHEMA;");
			this.m_writer.WriteLine("<br/>");

			this.m_writer.WriteLine("</span>");
		}

		#region IComparer Members

		public int Compare(string x, string y)
		{
			return String.CompareOrdinal((string)x, (string)y);
		}

		#endregion


		internal void WriteProperties(List<DocProperty> list, DocProject docProject, DocEntity docEntity, IList<string> locales)
		{
			if (list.Count == 0)
				return;

			this.WriteLine("<table class=\"gridtable\">");
			this.WriteLine("<tr><th>Name</th><th>Type</th><th>Description</th></tr>");

			foreach (DocProperty docpropdecl in list)
			{
				// find property defined at supertype (e.g. Pset_ElementCommon.Reference)
				DocProperty docprop = docProject.FindProperty(docpropdecl.Name, docEntity);
				string suffix = "";
				if (docprop != null && docprop != docpropdecl)
				{
					suffix = "*";
					//System.Diagnostics.Debug.WriteLine("FormatHTM.WriteProperties() - overridden property: " + docEntity.Name + " - " + docprop.Name);
				}
				else
				{
					docprop = docpropdecl;
				}

				string datatype = docprop.PrimaryDataType;
				if (datatype == null)
				{
					datatype = "IfcLabel";
				}

				this.WriteLine("<tr><td>" + docprop.Name + "</td><td>");
				this.WriteDefinition(docprop.PropertyType.ToString());
				this.WriteLine("/");
				this.WriteDefinition(datatype.Trim());
				if (docprop.Enumeration != null)
				{
					this.WriteLine("/");

					this.Write(" <a href=\"../../../penum/" + docprop.Enumeration.Name.ToLower() + ".htm\">" + docprop.Enumeration.Name + "</a>");
				}
				else if (!String.IsNullOrEmpty(docprop.SecondaryDataType))
				{
					this.WriteLine("/");
					this.WriteDefinition(docprop.SecondaryDataType.Trim().Replace(",", ", ").Replace(":", ": "));
				}
				if (suffix != null)
				{
					this.Write(suffix);
				}
				this.WriteLine("</td><td>");

				this.WriteLocalizationTable(docprop, locales); // up 3 levels for images

				// complex properties
				if (docprop.Elements != null)
				{
					WriteProperties(docprop.Elements, docProject, docEntity, locales);
				}

				this.WriteLine("</td></tr>");
			}

			this.WriteLine("</table>");

		}

		internal void WriteLinkTo(DocProject docProject, DocPublication docPublication, DocObject type)
		{
			int levels = 3;
			if (type is DocExample || type is DocTemplateDefinition || type is DocSchema)
			{
				levels = 2;
			}
			else if (type is DocPropertyEnumeration)
			{
				levels = 1;
			}

			string up = "";
			for (int i = 0; i < levels; i++)
			{
				up += "../";
			}

			// write references
			List<DocObject> listReferences = new List<DocObject>();
			foreach (DocSection docSection in docProject.Sections)
			{
				foreach (DocSchema docSchema in docSection.Schemas)
				{
					if (type is DocPropertyEnumeration)
					{
						foreach (DocPropertySet docPset in docSchema.PropertySets)
						{
							foreach (DocProperty docProp in docPset.Properties)
							{
								if (docProp.PropertyType == DocPropertyTemplateTypeEnum.P_ENUMERATEDVALUE &&
									type == docProp.Enumeration)
								{
									listReferences.Add(docPset);
									break;
								}
							}
						}
					}
					else if (type is DocEntity || type is DocType)
					{
						foreach (DocEntity docEntity in docSchema.Entities)
						{
							foreach (DocAttribute docAttr in docEntity.Attributes)
							{
								if (docAttr.DefinedType.Equals(type.Name) ||
									(docAttr.AggregationAttribute != null &&
									docAttr.AggregationAttribute.DefinedType != null &&
									docAttr.AggregationAttribute.DefinedType.Equals(type.Name)))
								{
									listReferences.Add(docEntity);
									break;
								}
							}
						}

						foreach (DocType docType in docSchema.Types)
						{
							if (docType is DocSelect)
							{
								DocSelect docSelect = (DocSelect)docType;
								foreach (DocSelectItem docItem in docSelect.Selects)
								{
									if (docItem.Name.Equals(type.Name))
									{
										listReferences.Add(docType);
										break;
									}
								}
							}
						}
					}
					else if (type is DocFunction)
					{
						foreach (DocEntity docEntity in docSchema.Entities)
						{
							foreach (DocWhereRule docRule in docEntity.WhereRules)
							{
								if (docRule.Expression.Contains(type.Name)) // todo: refine to bound it in case of sub-string match
								{
									listReferences.Add(docEntity);
									break;
								}
							}
						}
					}
				}
			}

			if (type is DocTemplateDefinition)
			{
#if false
                foreach (DocModelView docView in docProject.ModelViews)
                {
                    foreach(DocConceptRoot docRoot in docView.ConceptRoots)
                    {
                        foreach(DocTemplateUsage docUsage in docRoot.Concepts)
                        {
                            if(docUsage.Definition == type)
                            {
                                listReferences.Add(doc);
                                break;
                            }
                        }
                    }
                }
#endif

				// find partial template references
				foreach (DocTemplateDefinition docTemplate in docProject.Templates)
				{
					//...
				}
			}

			// scrub anything out of scope
			for (int i = listReferences.Count - 1; i >= 0; i--)
			{
				DocObject docObj = listReferences[i];
				if (this.m_included != null && !this.m_included.ContainsKey(docObj))
				{
					listReferences.RemoveAt(i);
				}
			}

			if (listReferences.Count > 0)
			{
				this.Write("<p><img src=\"" + up + "img/external.png\" style=\"border: 0px\" title=\"References\" alt=\"References\"/>&nbsp; References: ");
				foreach (DocObject docObj in listReferences)
				{
#if false
                    if (docObj != listReferences[0])
                    {
                        this.Write(", ");
                    }
#endif
					if(type is DocPropertyEnumeration)
						this.WriteDefinition(docObj.Name, up + "schema/");
					else
						this.WriteDefinition(docObj.Name, "../../"); // link...
				}
			}
			this.WriteLine("</p>");

			this.WriteLinkTo(docPublication, DocumentationISO.MakeLinkName(type), levels);
		}

		internal void WriteLinkTo(DocPublication docPublication, string identifier, int levels)
		{
			string up = "";
			for (int i = 0; i < levels; i++)
			{
				up += "../";
			}

			if (docPublication.ReportIssues)
			{
				this.WriteLine("<p><a href=\"http://jira.buildingsmart.org/issues/?jql=project%20%3D%20IFR%20AND%20text%20~%20%22" + identifier + "%22\" target=\"_blank\" ><img src=\"" + up + "img/external.png\" style=\"border: 0px\" title=\"Report issue\" alt=\"Report issue\"/>&nbsp; Report an issue</a></p>");
			}

			this.WriteLine("<p><a href=\"" + up + "link/" + identifier + ".htm\" target=\"_top\" ><img src=\"" + up + "img/permlink.png\" style=\"border: 0px\" title=\"Link to this page\" alt=\"Link to this page\"/>&nbsp; Link to this page</a></p>");
		}

		internal void WriteViewIcons(DocDefinition type, DocProject docProject, Dictionary<DocObject, bool>[] dictionaryViews, string path)
		{
			if (dictionaryViews != null && dictionaryViews.Length > 0)
			{
				for (int iMapView = 0; iMapView < dictionaryViews.Length; iMapView++)
				{
					bool mapinclude = false;
					Dictionary<DocObject, bool> mapthis = dictionaryViews[iMapView];
					if (mapthis != null && mapthis.TryGetValue(type, out mapinclude) && mapinclude)
					{
						DocModelView docViewMap = docProject.ModelViews[iMapView];
						if (docViewMap.Icon != null)
						{
							this.WriteLine("<a href=\"../../views/" + DocumentationISO.MakeLinkName(docViewMap) + "/index.htm\" ><img src=\"../../../img/view-" + DocumentationISO.MakeLinkName(docViewMap) + ".png\" title=\"" + docViewMap.Name + "\"/></a>");
						}
					}
				}
			}
		}

		internal void WriteLinkPage(string linkurl, DocPublication docPublication)
		{
			this.WriteLine(
			"<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01 Frameset//EN\"" +
			   "\"http://www.w3.org/TR/html4/frameset.dtd\">" +
			"<html lang=\"en\">" +
			"<head>" +
			"	<meta content=\"text/html; charset=utf-8\" http-equiv=\"content-type\">" +
			"	<link rel=\"STYLESHEET\" href=\"../ifc-styles.css\">" +
			"	<title>" + docPublication.Header + "</title>" +
			"	<style type=\"text/css\">" +
			"	<!--" +
			"	frameset {" +
			"	  border-width: 1px;" +
			"	  border-color: rgb(255,255,255);" +
			"	  border-style: solid;" +
			"	}" +
			"	-->" +
			"   </style>" +
			"</head>" +

			"<frameset rows=\"110px,*\">" +
			"    <frame src=\"../content.htm\" name=\"menu\" frameborder=\"0\">" +
			"	<frameset cols=\"15%,*\">" +
			"		<frame src=\"../blank.htm\" name=\"index\" frameborder=\"0\">" +
			"		<frame src=\"" + linkurl + "\" name=\"info\" frameborder=\"0\">" +
			"	</frameset>" +
			"	<noframes>" +
			"		<body>" +
			"			<p>Industry Foundation Classes (IFC) for Data Sharing in the Construction and Facility Management Industries </p>" +
			"			<p>&nbsp;</p>" +
			"		</body>" +
			"	</noframes>" +
			"</frameset>" +

			"</html>");

		}

		internal void WriteComputerListing(string name, string code, int[] indexpath, DocPublication docPublication)
		{
			int iAnnex = -1;

			string linkprefix = code;

			string indexer = "";
			foreach (int part in indexpath)
			{
				if (indexer.Length != 0)
				{
					indexer += ".";
				}
				indexer += part.ToString();
			}


			this.WriteHeader(name, iAnnex, indexpath[0], 0, 0, docPublication.Header);
			this.WriteScript(iAnnex, indexpath[0], 0, 0);
			this.WriteLine("<h3 class=\"std\">A." + indexer + " " + name + "</h3>");

			if (!String.IsNullOrEmpty(code))
			{
				/*
                this.WriteLine("<p>Schema files are provided that are filtered according to this view definition. " +
                    "Data exported by conforming applications shall conform to this schema subset (i.e. not include any definitions excluded from this schema subset). " +
                    "Applications importing data for this model view shall be able to read all data conforming to the full schema (i.e. allow for definitions outside of this schema subset without processing them).</p>");
                */
				this.WriteLine("<p>Computer intepretable files are provided specific to this view definition.</p>");

				string key1 = "";// "A." + iCodeView + ".1";
				string key2 = "";// "A." + iCodeView + ".2";
				string key3 = "";// "A." + iCodeView + ".3";

#if false // 2018-04-23: no longer publish filtered schema listings for model view -- use parent listing
                // write table linking formatted listings
                this.Write(
                    "<h4 class=\"annex\"><a>" + key1 + " Schema definitions</a></h4>" +
                    "<p class=\"std\">This schema is defined according to formats as follows.</p>" +
                    "<table class=\"gridtable\" summary=\"listings\" width=\"80%\">" +
                    "<col width=\"60%\">" +
                    "<col width=\"20%\">" +
                    "<col width=\"20%\">" +
                    "<tr style=\"border: 1px grey solid;\">" +
                    "<th>Description</td>" +
                    "<th>ASCII file</td>" +
                    "<th>HTML file</td>" +
                    "</tr>");

                foreach (DocFormat format in docPublication.Formats)
                {
                    if (format.FormatOptions != DocFormatOptionEnum.None)
                    {
                        System.Reflection.FieldInfo fieldEnum = typeof(DocFormatSchemaEnum).GetField(format.FormatType.ToString(), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                        if (fieldEnum != null)
                        {
                            System.ComponentModel.DescriptionAttribute[] descattrs = (System.ComponentModel.DescriptionAttribute[])fieldEnum.GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false);

                            string desc = format.FormatType.ToString();
                            if (descattrs.Length == 1)
                            {
                                desc = descattrs[0].Description;
                            }

                            string ext = format.ExtensionSchema;

                            this.Write(
                                "<tr>" +
                                "<td>" + desc + "</td>" +
                                "<td><a href=\"" + linkprefix + "." + ext + "\" target=\"_blank\">" + code + "." + ext + "</a></td>" +
                                "<td><a href=\"" + linkprefix + "." + ext + ".htm\" >" + code + "." + ext + ".htm</a></td>" +
                                "</tr>");
                        }
                    }
                }


                this.Write("</table>");
#endif

				this.Write(
					"<h4 class=\"annex\"><a>" + key2 + " Property and quantity templates</a></h4>" +
					"<p>Property sets and quantity sets are defined according to formats as follows.</p>" +
					"<table class=\"gridtable\" summary=\"listings\" width=\"80%\">" +
					"<col width=\"60%\">" +
					"<col width=\"20%\">" +
					"<col width=\"20%\">" +
					"<tr style=\"border: 1px grey solid;\">" +
					"<th>Description</td>" +
					"<th>ASCII file</td>" +
					"<th>HTML file</td>" +
					"</tr>" +

					"<tr>" +
					"<td>IFC-SPF property and quantity templates</td>" +
					"<td><a href=\"" + linkprefix + ".ifc\">" + code + ".ifc</a></td>" +
					"<td>&nbsp;</td>" +
					"</tr>" +

					"<tr>" +
					"<td>IFC-XML property and quantity templates</td>" +
					"<td><a href=\"" + linkprefix + ".ifcxml\">" + code + ".ifcxml</a></td>" +
					"<td>&nbsp;</td>" +
					"</tr>");

				if (!docPublication.ISO)
				{
					this.Write("<tr>" +
						"<td>PSD-XML property templates in ZIP file</td>" +
						"<td><a href=\"" + linkprefix + "-psd.zip_\">" + code + "-psd.zip</a></td>" +
						"<td>&nbsp;</td>" +
						"</tr>" +

						"<tr>" +
						"<td>QTO-XML quantity templates in ZIP file</td>" +
						"<td><a href=\"" + linkprefix + "-qto.zip_\">" + code + "-qto.zip</a></td>" +
						"<td>&nbsp;</td>" +
						"</tr>");
				}

				this.Write("</table>");


				if (!docPublication.ISO) // don't provide mvdXML for ISO
				{
					this.Write(
		"<h4 class=\"annex\"><a>" + key3 + " Model view definition</a></h4>" +
		"<p>Model view definitions are defined according to formats as follows.</p>" +
		"<table class=\"gridtable\" summary=\"listings\" width=\"80%\">" +
		"<col width=\"60%\">" +
		"<col width=\"20%\">" +
		"<col width=\"20%\">" +
		"<tr style=\"border: 1px grey solid;\">" +
		"<th>Description</td>" +
		"<th>ASCII file</td>" +
		"<th>HTML file</td>" +
		"</tr>" +
		"<tr>" +
		"<td>MVD-XML model view definitions</td>" +
		"<td><a href=\"" + linkprefix + ".mvdxml\" target=\"_blank\">" + code + ".mvdxml</a></td>" +
		"<td>&nbsp;</td>" +
		"</tr>" +
		"<tr>" +
		"<td>EXPRESS XSD configuration</td>" +
		"<td><a href=\"" + linkprefix + ".xml\" target=\"_blank\">" + code + ".xml</a></td>" +
		"<td>&nbsp;</td>" +
		"</tr>" +
		"</table>");
				}
			}

			if (code != null)
			{
				WriteLinkTo(docPublication, "listing-" + code.ToLower(), 3);
			}

			this.WriteFooter(docPublication.Footer);
		}

		/// <summary>
		/// Writes page for index into EXPRESS-G diagrams
		/// </summary>
		/// <param name="docSection"></param>
		/// <param name="iSection">Section number (1-based)</param>
		internal void WriteDiagramListing(DocSection docSection, int iSection, DocPublication docPublication)
		{
			int iAnnex = -4;
			int iSub = 1;

			this.WriteHeader(docSection.Name, 2, docPublication.Header);
			this.WriteScript(iAnnex, iSub, iSection, 0);
			this.WriteLine("<h3 class=\"std\">D.1." + iSection.ToString() + " " + docSection.Name + "</h3>");

			int iSchema = 0;
			foreach (DocSchema docSchema in docSection.Schemas)
			{
				iSchema++;
				this.WriteLine("<h4 class=\"std\">D.1." + iSection + "." + iSchema + " " + docSchema.Name + "</h4>");

				this.WriteLine("<p>");

				// determine number of diagrams
				int iLastDiagram = docSchema.UpdateDiagramPageNumbers();

				// write thumbnail links for each diagram
				for (int iDiagram = 1; iDiagram <= iLastDiagram; iDiagram++)
				{
					string formatnumber = iDiagram.ToString("D4"); // 0001
					this.WriteLine("<a href=\"" + docSchema.Name.ToLower() + "/diagram_" + formatnumber + ".htm\">" +
						"<img src=\"" + docSchema.Name.ToLower() + "/small_diagram_" + formatnumber + ".png\" width=\"100\" height=\"148\" /></a>"); // width=\"150\" height=\"222\"> 
				}

				this.WriteLine("</p>");
			}

			this.WriteFooter(docPublication.Footer);
		}

		public void WriteLocalizationSection(DocObject entity, IList<string> locales, DocPublication docPublication)
		{
			if (locales == null)
				return;

			// localization
			this.WriteSummaryHeader("Natural language names", true, docPublication);

			this.WriteLine("<table>");

			if (entity.Localization.Count > 0)
			{
				this.WriteLine("<table class=\"gridtable\">");
				entity.Localization.Sort();
				foreach (DocLocalization doclocal in entity.Localization)
				{
					if (doclocal.Locale != null)
					{
						string localname = doclocal.Name;

						string localid = doclocal.Locale.Substring(0, 2).ToLower();
						if (locales.Contains(localid))
						{
							this.WriteLine("<tr><td><img src=\"../../../img/locale-" + localid + ".png\" /></td><td><b>" + localname + "</b></td></tr>");
						}
					}
				}
				this.WriteLine("</table>");
			}

			this.WriteSummaryFooter(docPublication);
		}
		public void WriteLocalizationTable(DocObject entity, IList<string> locales)
		{
			WriteLocalizationTable(entity, locales, "../../../");
		}
		public void WriteLocalizationTable(DocObject entity, IList<string> locales, string path)
		{
			string defaultdesc = entity.DocumentationHtmlNoParagraphs();
			bool tableopen = false;
			if (entity.Localization.Count > 0)
			{
				entity.Localization.Sort();
				foreach (DocLocalization doclocal in entity.Localization)
				{
					string localname = doclocal.Name;
					string localdesc = doclocal.DocumentationHtmlNoParagraphs();

					string localid = doclocal.Locale.Substring(0, 2).ToLower();
					if (localid.Equals("en", StringComparison.InvariantCultureIgnoreCase) && localdesc == null)
					{
						localdesc = entity.DocumentationHtmlNoParagraphs();
						defaultdesc = entity.DocumentationHtmlNoParagraphs();
					}

					if (locales != null && locales.Contains(localid))
					{
						if (!tableopen)
						{
							this.WriteLine("<table class=\"gridtable\">");
							tableopen = true;
						}

						this.WriteLine("<tr><td><img src=\"" + path + "img/locale-" + localid + ".png\" /></td><td><b>" + localname + "</b></td><td>" + localdesc + "</td></tr>");
						defaultdesc = null;
					}
				}

				if (tableopen)
				{
					this.WriteLine("</table>");
				}
			}

			if (defaultdesc != null)
			{
				this.WriteLine(defaultdesc);
			}
#if false
            entity.Localization.Sort(); // ensure sorted
            foreach (DocLocalization doclocal in entity.Localization)
            {
                if (doclocal.Locale != null && doclocal.Locale.Length > 2)
                {
                    string localname = doclocal.Name;
                    string localdesc = doclocal.Documentation;

                    string localid = doclocal.Locale.Substring(0, 2).ToLower();
                    if (locales.Contains(localid))
                    {
                        if (localid.Equals("en", StringComparison.InvariantCultureIgnoreCase) && localdesc == null)
                        {
                            localdesc = entity.Documentation;
                        }

                        this.WriteLine("<tr><td><img src=\"../../../img/locale-" + localid + ".png\" /></td><td><b> " + localname + ":</b> " + localdesc + "</td></tr>");
                    }
                }
            }

            this.WriteLine("</table>");
#endif
			/* old */
			/*
            this.WriteLine("<table>");
            entity.Localization.Sort();
            foreach (DocLocalization doclocal in entity.Localization)
            {
                string localid = doclocal.Locale.Substring(0, 2).ToLower();
                if (locales.Contains(localid))
                {
                    string localname = doclocal.Name;
                    string localdesc = doclocal.Documentation;

                    if (!String.IsNullOrEmpty(localdesc))
                    {
                        localdesc = ": " + localdesc;
                    }

                    this.WriteLine("<tr><td><img alt=\"" + localid + "\" src=\"../../../img/locale-" + localid + ".png\" /></td><td><b>" + localname + "</b>" + localdesc + "</td></tr>");
                }
            }
            this.WriteLine("</table>");
             */
		}

		public void WriteEntityInheritance(DocEntity entity, DocEntity treeleaf, DocModelView[] views, Dictionary<DocObject, bool>[] viewmap, DocPublication docPublication, ref int sequence)
		{
			if (entity.BaseDefinition != null)
			{
				if (this.m_mapEntity.ContainsKey(entity.BaseDefinition))
				{
					DocEntity baseEntity = (DocEntity)this.m_mapEntity[entity.BaseDefinition];
					WriteEntityInheritance(baseEntity, treeleaf, views, viewmap, docPublication, ref sequence);
				}
			}

			int colspan = 5;

			if (views != null)
			{
				colspan += views.Length;
			}

			this.Write("<tr><td colspan=\"" + colspan + "\">");
			if (entity.IsAbstract)
			{
				this.Write("<i>");
			}
			this.Write(FormatDefinition(entity.Name));
			if (entity.IsAbstract)
			{
				this.Write("</i>");
			}
			this.WriteLine("</td></tr>");

			WriteEntityAttributes(entity, treeleaf, views, viewmap, docPublication, ref sequence);
		}

		private void WriteEntityAttributeViews(DocAttribute docAttr, DocModelView[] views, Dictionary<DocObject, bool>[] viewmap)
		{
			if (views == null || viewmap == null)
				return;

			for (int iView = 0; iView < viewmap.Length; iView++)
			{
				if (viewmap[iView] != null)
				{
					bool isViewPublishing = false;
					for (int i = 0; i < views.Length; i++)
					{
						if (views[i] != null)
						{
							if (viewmap[iView].TryGetValue(views[i], out isViewPublishing)) break;
						}
					}

					if (isViewPublishing)
					{
						this.m_writer.Write("<td>");
						bool included = false;
						if (viewmap[iView].TryGetValue(docAttr, out included))
						{
							this.m_writer.Write("X");
						}
						this.m_writer.Write("</td>");
					}
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="treeleaf"></param>
		/// <param name="sequence">Last sequence number used (0 initially)</param>
		public void WriteEntityAttributes(DocEntity entity, DocEntity treeleaf, DocModelView[] views, Dictionary<DocObject, bool>[] viewmap, DocPublication docPublication, ref int sequence)
		{
			bool bInverse = false;
			bool bDerived = false;
			bool bExplicit = false;

			bool suppresshistory = true;

			// count attributes first to avoid generating tables unnecessarily (W3C validation)
			if (entity.Attributes != null && entity.Attributes.Count > 0)
			{
				foreach (DocAttribute attr in entity.Attributes)
				{
					if (attr.Derived != null)
					{
						// inverse may also be indicated to hold class
						bDerived = true;
					}
					else if (attr.Inverse != null)
					{
						bInverse = true;
					}
					else
					{
						bExplicit = true;
					}
				}
			}

			// explicit attributes, plus detect any inverse or derived
			if (bExplicit)
			{
				foreach (DocAttribute attr in entity.Attributes)
				{
					bool bInclude = true;

					// suppress any attribute that is overridden on leaf class
					if (treeleaf != null && treeleaf != entity)
					{
						foreach (DocAttribute derivedattr in treeleaf.Attributes)
						{
							if (derivedattr.Name.Equals(attr.Name))
							{
								bInclude = false;
								sequence++;
							}
						}
					}

					if (bInclude)
					{
						if (attr.Inverse == null && attr.Derived == null)
						{
							sequence++;

							this.m_writer.Write("<tr><td>");
							this.m_writer.Write(sequence.ToString());
							this.m_writer.Write("</td><td>");
							this.m_writer.Write(attr.Name);
							this.m_writer.Write("</td><td>");

							if (this.m_included == null || this.m_included.ContainsKey(attr))
							{
								this.m_writer.Write(FormatDefinition(attr.DefinedType));
							}
							else
							{
								this.m_writer.Write("<span class=\"self-ref\">-</span>");
							}

							this.m_writer.Write("</td><td>");

							if (this.m_included == null || this.m_included.ContainsKey(attr))
							{
								if (attr.IsOptional)
								{
									this.m_writer.Write("? ");
								}

								if (attr.GetAggregation() != DocAggregationEnum.NONE)
								{
									this.WriteAttributeAggregation(attr);
								}
							}

							this.m_writer.WriteLine("</td><td>");
							if (this.m_included == null || this.m_included.ContainsKey(attr))
							{
								this.WriteDocumentationMarkup(attr.DocumentationHtmlNoParagraphs(), entity, docPublication);
							}
							else
							{
								this.m_writer.Write("<i>This attribute is out of scope for this model view definition and shall not be set.</i>");
							}
							this.m_writer.Write("</td>");
							this.WriteEntityAttributeViews(attr, views, viewmap);
							this.m_writer.WriteLine("</tr>");

						}
					}
				}
			}

			// inverse attributes
			if (bInverse)
			{
				foreach (DocAttribute attr in entity.Attributes)
				{
					DocObject docinvtype = null;
					if (attr.Inverse != null && attr.Derived == null && this.m_mapEntity.TryGetValue(attr.DefinedType, out docinvtype))
					{
						if (this.m_included == null || this.m_included.ContainsKey(docinvtype))
						{
							this.m_writer.Write("<tr><td></td><td><i>");
							this.m_writer.Write(attr.Name);
							this.m_writer.Write("</i>");
							this.m_writer.Write("</td><td>");

							this.m_writer.Write(FormatDefinition(attr.DefinedType));
							this.m_writer.Write("<br/>@" + attr.Inverse);

							this.m_writer.Write("</td><td>");

							if (attr.IsOptional)
							{
								this.m_writer.Write("? ");
							}

							if (attr.GetAggregation() != DocAggregationEnum.NONE)
							{
								this.WriteAttributeAggregation(attr);
							}

							this.m_writer.Write("</td><td>");
							this.WriteDocumentationMarkup(attr.DocumentationHtmlNoParagraphs(), entity, docPublication);
							this.m_writer.Write("</td>");

							this.WriteEntityAttributeViews(attr, views, viewmap);

							this.m_writer.WriteLine("</tr>");
						}
					}
				}
			}

			// derived attributes
			if (bDerived)
			{
				foreach (DocAttribute attr in entity.Attributes)
				{
					if (attr.Derived != null)
					{
						// determine the superclass having the attribute                        
						DocEntity found = null;
						if (treeleaf == null)
						{
							DocEntity super = entity;
							while (super != null && found == null && super.BaseDefinition != null)
							{
								super = this.m_mapEntity[super.BaseDefinition] as DocEntity;
								if (super != null)
								{
									foreach (DocAttribute docattr in super.Attributes)
									{
										if (docattr.Name.Equals(attr.Name))
										{
											// found class
											found = super;
											break;
										}
									}
								}
							}
						}

						if (found != null)
						{
							// overridden attribute
							this.m_writer.Write("<tr><td></td><td>" + "\\" + found.Name + "." + attr.Name);
						}
						else
						{
							// non-overridden
							this.m_writer.Write("<tr><td></td><td>" + attr.Name);
						}
						this.m_writer.Write("<br/>:=" + attr.Derived);

						this.m_writer.Write("</td><td>");
						this.m_writer.Write(FormatDefinition(attr.DefinedType));
						this.m_writer.Write("</td><td>");
						//this.m_writer.Write(" := ");

						if (attr.IsOptional)
						{
							this.m_writer.Write("? ");
						}

						if (attr.GetAggregation() != DocAggregationEnum.NONE)
						{
							this.WriteAttributeAggregation(attr);
						}

						this.m_writer.Write("</td><td>");
						this.WriteDocumentationMarkup(attr.DocumentationHtmlNoParagraphs(), entity, docPublication);
						this.m_writer.WriteLine("</td>");

						this.WriteEntityAttributeViews(attr, views, viewmap);

						this.m_writer.WriteLine("</tr>");

					}
				}
			}


		}

		/// <summary>
		/// Writes term to documentation
		/// </summary>
		/// <param name="docRef">The term to write</param>
		/// <param name="indexpath">The 1-based index path of the term</param>
		internal void WriteTerm(DocTerm docRef, int[] indexpath)
		{
			StringBuilder sbIndex = new StringBuilder();
			for (int i = 0; i < indexpath.Length; i++)
			{
				if (i > 0)
				{
					sbIndex.Append(".");
				}
				sbIndex.Append(indexpath[i]);
			}

			this.WriteLine("<dt class=\"term\"><strong name=\"" + DocumentationISO.MakeLinkName(docRef) + "\" id=\"" + DocumentationISO.MakeLinkName(docRef) + "\">" +
				sbIndex.ToString() + " " + docRef.Name + "</strong></dt>");
			this.WriteLine("<dd class=\"term\">" + docRef.DocumentationHtml());

			if (docRef.Terms.Count > 0)
			{
				int[] subindexpath = new int[indexpath.Length + 1];
				for (int i = 0; i < indexpath.Length; i++)
				{
					subindexpath[i] = indexpath[i];
				}

				this.WriteLine("<dl>");
				foreach (DocTerm sub in docRef.Terms)
				{
					subindexpath[subindexpath.Length - 1]++;
					WriteTerm(sub, subindexpath);
				}
				this.WriteLine("</dl>");
			}

			this.WriteLine("</dd>");
		}

		internal void WriteChangeLog(DocObject entity, List<DocChangeSet> listChangeSets, DocPublication docPublication)
		{
			Dictionary<DocChangeSet, DocChangeAction> mapChange = new Dictionary<DocChangeSet, DocChangeAction>();
			foreach (DocChangeSet docChangeSet in listChangeSets)
			{
				List<DocChangeAction> listActions = docChangeSet.ChangesEntities;
				if (entity is DocPropertySet || entity is DocPropertyEnumeration)
				{
					listActions = docChangeSet.ChangesProperties;
				}
				else if (entity is DocQuantitySet)
				{
					listActions = docChangeSet.ChangesQuantities;
				}

				foreach (DocChangeAction docChangeSection in listActions)
				{
					foreach (DocChangeAction docChangeSchema in docChangeSection.Changes)
					{
						foreach (DocChangeAction docChangeEntity in docChangeSchema.Changes)
						{
							if (docChangeEntity.Name.Equals(entity.Name) && docChangeEntity.HasChanges())
							{
								try
								{
									// could show up twice, for case of an entity moving between schemas
									if (!mapChange.ContainsKey(docChangeSet))
									{
										mapChange.Add(docChangeSet, docChangeEntity);
									}
									else
									{
										// overwrite with new schema
										DocChangeAction docPrev = mapChange[docChangeSet];
										if (docChangeEntity.Changes.Count > docPrev.Changes.Count)
										{
											mapChange[docChangeSet] = docChangeEntity;
										}
									}
								}
								catch
								{
									mapChange.ToString();
								}
							}
						}
					}
				}
			}

			if (mapChange.Count > 0 || entity.IsDeprecated())
			{
				this.WriteSummaryHeader("Change log", true, docPublication);

				if (entity.IsDeprecated())
				{
					//this.WriteLine("<blockquote class=\"deprecated\">DEPRECATED&nbsp; This definition may be imported, but shall not be exported by applications.</blockquote>");
					this.WriteLine("<table class=\"gridtable\"><tr><td style=\"background-color:red;\">DEPRECATED</td><td>This definition may be imported, but shall not be exported by applications.</td></tr></table>");
				}

				this.WriteLine("<table class=\"gridtable\">");
				this.WriteLine("<tr>" +
					"<th>Item</th>" +
					"<th>SPF</th>" +
					"<th>XML</th>" +
					"<th>Change</th>" +
					"<th>Description</th>" +
					"</tr>");

				foreach (DocChangeSet docChangeSet in mapChange.Keys)
				{
					this.WriteLine("<td colspan=5><b><a href=\"../../../annex/annex-f/" + DocumentationISO.MakeLinkName(docChangeSet) + "/index.htm\">" + docChangeSet.Name + "</a></b></td>");
					DocChangeAction docChangeAction = mapChange[docChangeSet];
					this.WriteChangeItem(docChangeAction, 2);
				}

				this.WriteLine("</table>");

				this.WriteSummaryFooter(docPublication);
			}


		}

		public static HtmlNode MarkdownToHtml(string markdown, bool removeParagraphs)
		{
			if (string.IsNullOrEmpty(markdown))
				return null;
			Markdig.MarkdownPipelineBuilder builder = new Markdig.MarkdownPipelineBuilder();
			builder.Extensions.Add(new Markdig.Extensions.GenericAttributes.GenericAttributesExtension());
			builder.Extensions.Add(new Markdig.Extensions.Tables.PipeTableExtension());
			builder.Extensions.Add(new Markdig.Extensions.EmphasisExtras.EmphasisExtraExtension());
			Markdig.MarkdownPipeline pipeline = builder.Build();
			return MarkdownToHtml(markdown, removeParagraphs, pipeline);

		}
		private static HtmlNode MarkdownToHtml(string markdown, bool removeParagraphs, Markdig.MarkdownPipeline pipeline)
		{ 
			string html = Markdig.Markdown.ToHtml(markdown, pipeline);
			HtmlDocument document = new HtmlDocument();
			document.LoadHtml(html);
			if(removeParagraphs)
			{
				List<HtmlNode> nodes = new List<HtmlNode>(document.DocumentNode.ChildNodes), processed = new List<HtmlNode>();
				foreach(HtmlNode node in nodes)
				{
					processed.AddRange(RemoveParagraphs(node));
				}
				document.DocumentNode.ChildNodes.Clear();
				foreach (HtmlNode n in processed)
					document.DocumentNode.ChildNodes.Add(n);
			}
			AdjustFromMarkdown(document.DocumentNode, pipeline);
			return document.DocumentNode;
		}

		private static void AdjustFromMarkdown(HtmlNode node, Markdig.MarkdownPipeline pipeline)
		{
			if(string.Compare(node.Name, "blockquote", true) == 0)
			{
				Dictionary<string, HtmlAttribute> attributes = new Dictionary<string, HtmlAttribute>();
				foreach (HtmlAttribute att in node.Attributes)
					attributes[att.Name] = att;
				string innerText = node.InnerText.Trim();
				if (innerText.StartsWith("EXAMPLE", StringComparison.CurrentCultureIgnoreCase))
				{
					if (!attributes.ContainsKey("class"))
						node.SetAttributeValue("class", "example");
				}
				else if (innerText.StartsWith("NOTE",	StringComparison.CurrentCultureIgnoreCase))
				{
					if (!attributes.ContainsKey("class"))
						node.SetAttributeValue("class", "note");
				}
				else if (innerText.StartsWith("HISTORY", StringComparison.CurrentCultureIgnoreCase))
				{
					if (!attributes.ContainsKey("class"))
						node.SetAttributeValue("class", "history");
				}
			}
			else if (string.Compare(node.Name, "img",true) == 0)
			{
				foreach (HtmlAttribute att in node.Attributes)
				{
					if (string.Compare(att.Name, "title", true) == 0)
					{
						string title = att.Value;
						if (title.StartsWith("figure", StringComparison.CurrentCultureIgnoreCase))
						{
							HtmlNode table = node.OwnerDocument.CreateElement("table");
							node.ParentNode.ReplaceChild(table, node);
							HtmlNode tableRow = node.OwnerDocument.CreateElement("tr");
							table.ChildNodes.Add(tableRow);
							HtmlNode tableData = node.OwnerDocument.CreateElement("td");
							tableRow.ChildNodes.Add(tableData);
							tableData.Attributes.Add("style", "width: 600px");
							tableData.ChildNodes.Add(node);
							node.SetAttributeValue("title", title.Replace("_", ""));
							tableRow = node.OwnerDocument.CreateElement("tr");
							table.ChildNodes.Add(tableRow);
							tableData = node.OwnerDocument.CreateElement("td");
							tableRow.ChildNodes.Add(tableData);
							HtmlNode paragraph = node.OwnerDocument.CreateElement("p");
							tableData.ChildNodes.Add(paragraph);
							HtmlNode text = node.OwnerDocument.CreateTextNode(MarkdownToHtml(title, true, pipeline).InnerHtml.Trim());
							paragraph.ChildNodes.Add(text);
							paragraph.SetAttributeValue("class", "figure");
						}
					}
				}
			}
			List<HtmlNode> nodes = new List<HtmlNode>(node.ChildNodes);
			foreach (HtmlNode n in nodes)
				AdjustFromMarkdown(n, pipeline);
		}

		public static List<HtmlNode> RemoveParagraphs(HtmlNode node)
		{
			if (string.Compare(node.Name, "p", true) == 0)
			{
				List<HtmlNode> result = new List<HtmlNode>();
				foreach (HtmlNode n in node.ChildNodes)
					result.AddRange(RemoveParagraphs(n));
				return result;
			}
			List<HtmlNode> nodes = new List<HtmlNode>();
			foreach (HtmlNode n in node.ChildNodes)
				nodes.AddRange(RemoveParagraphs(n));

			node.ChildNodes.Clear();
			foreach (HtmlNode n in nodes)
				node.ChildNodes.Add(n);

			return new List<HtmlNode>() { node };
		}
	}

}
