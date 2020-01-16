// Name:        FormatIFD.cs
// Description: Text file import/export for IFD guids
// Author:      Tim Chipman
// Origination: Work performed for BuildingSmart by Constructivity.com LLC.
// Copyright:   (c) 2012 BuildingSmart International Ltd.
// License:     http://www.buildingsmart-tech.org/legal

using System;
using System.Collections.Generic;
using System.Text;

using IfcDoc.Schema;
using IfcDoc.Schema.DOC;

using BuildingSmart.Utilities.Conversion;

namespace IfcDoc
{
	internal class FormatIFD : IDisposable
	{
		string m_filename;
		DocProject m_project;

		public FormatIFD(string filename)
		{
			this.m_filename = filename;
		}

		public DocProject Instance
		{
			get
			{
				return this.m_project;
			}
			set
			{
				this.m_project = value;
			}
		}

		public void Load()
		{
			// prepare map
			Dictionary<string, DocPropertySet> map = new Dictionary<string, DocPropertySet>();
			foreach (DocSection docSection in this.m_project.Sections)
			{
				foreach (DocSchema docSchema in docSection.Schemas)
				{
					foreach (DocPropertySet docEntity in docSchema.PropertySets)
					{
						map.Add(docEntity.Name, docEntity);
					}
				}
			}

			// use commas for simplicity
			using (System.IO.StreamReader reader = new System.IO.StreamReader(this.m_filename))
			{
				string headerline = reader.ReadLine(); // blank

				// now rows
				while (!reader.EndOfStream)
				{
					string rowdata = reader.ReadLine();

					string[] rowcells = rowdata.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

					if (rowcells.Length > 1)
					{
						DocPropertySet docObj = null;
						string ifdguid = rowcells[0];
						string ifdname = rowcells[1];

						Guid guid = GlobalId.Parse(ifdguid);

						string[] nameparts = ifdname.Split('.');
						string psetname = nameparts[0].Trim();
						string propname = null;
						if (nameparts.Length == 2)
						{
							propname = nameparts[1];
						}

						if (map.TryGetValue(psetname, out docObj))
						{
							if (propname != null)
							{
								foreach (DocProperty docprop in docObj.Properties)
								{
									if (propname.Equals(docprop.Name))
									{
										// found it
										docprop.Uuid = guid;
										break;
									}
								}
							}
							else
							{
								docObj.Uuid = guid;
							}
						}
						else
						{
							System.Diagnostics.Debug.WriteLine("IFD (not found): " + psetname);
						}
					}
				}
			}
		}

		public void Save()
		{
			using (System.IO.StreamWriter writer = new System.IO.StreamWriter(this.m_filename))
			{
				// header
				writer.WriteLine();

				SortedList<string, DocObject> sortlist = new SortedList<string, DocObject>();

				// rows
				//...
			}
		}


		#region IDisposable Members

		public void Dispose()
		{
		}

		#endregion
	}
}
