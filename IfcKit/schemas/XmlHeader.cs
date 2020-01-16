using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;


namespace BuildingSmart.IFC
{
	[XmlType(TypeName = "header")]
	public class XmlHeader  
	{
		[DataMember(Order = 0)] [XmlElement] public string name { get; set; }
		[DataMember(Order = 1)] [XmlElement] public DateTime time_stamp { get; set; }
		[DataMember(Order = 2)] [XmlElement] public string author { get; set; }
		[DataMember(Order = 3)] [XmlElement] public string organization { get; set; }
		[DataMember(Order = 4)] [XmlElement] public string preprocessor_version { get; set; }
		[DataMember(Order = 5)] [XmlElement] public string originating_system { get; set; }
		[DataMember(Order = 6)] [XmlElement] public string authorization { get; set; }
		[DataMember(Order = 7)] [XmlElement] public string documentation { get; set; }

		public XmlHeader()
			: this(null, null, null, null, null)
		{
		}

		public XmlHeader(string thename, string theauthor, string theorganization, string preprocessor, string system)
		{
			this.name = thename;
			this.time_stamp = DateTime.UtcNow;
			this.author = theauthor;
			this.organization = theorganization;
			this.preprocessor_version = preprocessor;
			this.originating_system = system;
			this.authorization = null;
			this.documentation = null;
		}
	}
}
