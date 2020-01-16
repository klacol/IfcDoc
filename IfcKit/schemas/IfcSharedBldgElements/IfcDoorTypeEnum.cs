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


namespace BuildingSmart.IFC.IfcSharedBldgElements
{
	public enum IfcDoorTypeEnum
	{
		[Description("A standard door usually within a wall opening, as a door panel in a curtain wall," +
	    " or as a \"free standing\" door.")]
		DOOR = 1,
	
		[Description("A gate is a point of entry to a property usually within an opening in a fence. Or" +
	    " as a \"free standing\" gate.")]
		GATE = 2,
	
		[Description("A special door that lies horizonally in a slab opening. Often used for accessing " +
	    "cellar or attic.")]
		TRAPDOOR = 3,
	
		[Description("User-defined door element.")]
		USERDEFINED = -1,
	
		[Description("Undefined door element.")]
		NOTDEFINED = 0,
	
	}
}
