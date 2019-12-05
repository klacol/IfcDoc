// Name:        FormatZIP.cs
// Description: Generates zip files containing property sets and quantity sets
// Author:      Tim Chipman
// Origination: Work performed for BuildingSmart by Constructivity.com LLC.
// Copyright:   (c) 2016 BuildingSmart International Ltd.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Packaging;

using IfcDoc.Format.XML;
using IfcDoc.Schema.DOC;
using IfcDoc.Schema.PSD;

namespace IfcDoc
{
	/// <summary>
	/// Zip file for storing property sets and quantity sets
	/// </summary>
	class FormatZIP : IDisposable
	{
		Stream m_stream;
		DocProject m_project;
		Dictionary<DocObject, bool> m_included;
		Type m_type;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="stream">Stream for the zip file.</param>
		/// <param name="project">Project.</param>
		/// <param name="included">Map of included definitions</param>
		/// <param name="type">Optional type of data to export, or null for all; DocPropertySet, DocQuantitySet are valid</param>
		public FormatZIP(Stream stream, DocProject project, Dictionary<DocObject, bool> included, Type type)
		{
			this.m_stream = stream;
			this.m_project = project;
			this.m_included = included;
			this.m_type = type;
		}

		public void Save()
		{
			// build map of enumerations
			Dictionary<string, DocPropertyEnumeration> mapPropEnum = new Dictionary<string, DocPropertyEnumeration>();
			foreach (DocPropertyEnumeration docEnum in this.m_project.PropertyEnumerations)
			{
				if (!mapPropEnum.ContainsKey(docEnum.Name))
				{
					mapPropEnum.Add(docEnum.Name, docEnum);
				}
			}

			using (Package zip = ZipPackage.Open(this.m_stream, FileMode.Create))
			{
				foreach (DocSection docSection in this.m_project.Sections)
				{
					foreach (DocSchema docSchema in docSection.Schemas)
					{
						if (this.m_type == null || this.m_type == typeof(DocPropertySet))
						{
							foreach (DocPropertySet docPset in docSchema.PropertySets)
							{
								if (m_included == null || this.m_included.ContainsKey(docPset))
								{
									if (docPset.IsVisible())
									{
										Uri uri = PackUriHelper.CreatePartUri(new Uri(docPset.Name + ".xml", UriKind.Relative));
										PackagePart part = zip.CreatePart(uri, "", CompressionOption.Normal);
										using (Stream refstream = part.GetStream())
										{
											refstream.SetLength(0);
											PropertySetDef psd = Program.ExportPsd(docPset, mapPropEnum, this.m_project);
											using (FormatXML format = new FormatXML(refstream, typeof(PropertySetDef), PropertySetDef.DefaultNamespace, null))
											{
												format.Instance = psd;
												format.Save();
											}
										}
									}
								}
							}
						}

						if (this.m_type == null || this.m_type == typeof(DocQuantitySet))
						{
							foreach (DocQuantitySet docQset in docSchema.QuantitySets)
							{
								if (m_included == null || this.m_included.ContainsKey(docQset))
								{
									Uri uri = PackUriHelper.CreatePartUri(new Uri(docQset.Name + ".xml", UriKind.Relative));
									PackagePart part = zip.CreatePart(uri, "", CompressionOption.Normal);
									using (Stream refstream = part.GetStream())
									{
										refstream.SetLength(0);
										QtoSetDef psd = Program.ExportQto(docQset, this.m_project);
										using (FormatXML format = new FormatXML(refstream, typeof(QtoSetDef), PropertySetDef.DefaultNamespace, null))
										{
											format.Instance = psd;
											format.Save();
										}
									}
								}
							}
						}
					}
				}
			}

		}

		public void Dispose()
		{
			this.m_stream.Close();
		}
	}
}
