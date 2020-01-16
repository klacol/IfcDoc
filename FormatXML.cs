// Name:        FormatXML.cs
// Description: XML (.xml) file loading
// Author:      Tim Chipman
// Origination: Work performed for BuildingSmart by Constructivity.com LLC.
// Copyright:   (c) 2010 BuildingSmart International Ltd.
// License:     http://www.buildingsmart-tech.org/legal

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Serialization;

namespace IfcDoc.Format.XML
{
	public class FormatXML : IDisposable
	{
		string m_url;
		Stream m_stream;
		Type m_type;
		object m_instance;
		string m_namespace;
		XmlSerializerNamespaces m_prefixes;

		public FormatXML(string file, Type type) : this(file, type, null, null)
		{
		}

		public FormatXML(string file, Type type, string defaultnamespace)
			: this(file, type, defaultnamespace, null)
		{
		}

		public FormatXML(string file, Type type, string defaultnamespace, XmlSerializerNamespaces prefixes)
		{
			string dirpath = System.IO.Path.GetDirectoryName(file);
			if (!Directory.Exists(dirpath))
			{
				Directory.CreateDirectory(dirpath);
			}

			this.m_url = file;
			this.m_stream = new System.IO.FileStream(file, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
			this.m_type = type;
			this.m_instance = null;
			this.m_namespace = defaultnamespace;
			this.m_prefixes = prefixes;
		}

		public FormatXML(Stream stream, Type type, string defaultnamespace, XmlSerializerNamespaces prefixes)
		{
			this.m_url = null;
			this.m_stream = stream;
			this.m_type = type;
			this.m_namespace = defaultnamespace;
			this.m_prefixes = prefixes;
		}

		public void Dispose()
		{
			if (this.m_stream != null)
			{
				this.m_stream.Close();
				this.m_stream = null;
			}
		}

		public object Instance
		{
			get
			{
				return this.m_instance;
			}
			set
			{
				this.m_instance = value;
			}
		}

		public void Load()
		{
			this.m_stream.Position = 0;
			XmlSerializer ser = new XmlSerializer(this.m_type, this.m_namespace);
			this.m_instance = ser.Deserialize(this.m_stream);
		}

		public void Save()
		{
			this.m_stream.SetLength(0);
			this.m_stream.Position = 0;

			if (this.m_prefixes != null)
			{
				XmlSerializer ser = new XmlSerializer(this.m_type);
				ser.Serialize(this.m_stream, this.m_instance, this.m_prefixes);
			}
			else
			{
				XmlSerializer ser = new XmlSerializer(this.m_type, this.m_namespace);
				ser.Serialize(this.m_stream, this.m_instance);
			}
		}
	}
}
