// This file may be edited manually or auto-generated using IfcKit at www.buildingsmart-tech.org.
// IFC content is copyright (C) 1996-2018 BuildingSMART International Ltd.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Xml.Serialization;


namespace BuildingSmart.IFC.IfcMeasureResource
{
	public partial struct IfcText :
		BuildingSmart.IFC.IfcMeasureResource.IfcSimpleValue
	{
		[XmlText]
		public String Value { get; private set; }
	
		public IfcText(String value) : this()
		{
			this.Value = value;
		}

		public static implicit operator string(IfcText value)
		{
			return value.Value;
		}

		public override string ToString()
		{
			return Value.ToString();
		}
	}
	
}
