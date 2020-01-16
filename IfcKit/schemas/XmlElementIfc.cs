using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;

using BuildingSmart.IFC.IfcKernel;

namespace BuildingSmart.IFC
{
	[XmlType("ifcXML")]
	public class XmlElementIfc : List<object>
	{
		[DataMember(Name = "header", Order = 0)] [XmlElement] public XmlHeader Header { get; set; }

		public XmlElementIfc(XmlHeader header)
		{
			Header = header;
		}

		public XmlElementIfc(XmlHeader header, IfcContext context)
			: this(header)
		{
			base.Add(context);
		}
		
		public const string NameSpace = "http://www.buildingsmart-tech.org/ifc/IFC4x1/final";
		public const string SchemaLocation = "http://standards.buildingsmart.org/IFC/RELEASE/IFC4_1/FINAL/XML/IFC4x1.xsd"; 
	}
}
