using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace BuildingSmart.Serialization.Xml
{
	public class headerData
	{
		[DataMember(Order = 0)]
		[XmlElement(ElementName ="name")]
		public string name { get; set; }

		[DataMember(Order = 1)]
		[XmlElement(ElementName = "time_stamp")]
		public DateTime time_stamp { get; set; }

		[DataMember(Order = 2)]
		[XmlElement(ElementName = "author")]
		public string author { get; set; }

		[DataMember(Order = 3)]
		[XmlElement(ElementName = "organization")]
		public string organization { get; set; }

		[DataMember(Order = 4)]
		[XmlElement(ElementName = "preprocessor_version")]
		public string preprocessor_version { get; set; }

		[DataMember(Order = 5)]
		[XmlElement(ElementName = "originating_system")]
		public string originating_system { get; set; }

		[DataMember(Order = 6)]
		[XmlElement(ElementName = "authorization")]
		public string authorization { get; set; }
	}
}
