// Name:        SchemaSch.cs
// Description: Schematron schema
// Author:      Tim Chipman
// Origination: Work performed for BuildingSmart by Constructivity.com LLC.
// Copyright:   (c) 2013 BuildingSmart International Ltd.
// License:     http://www.buildingsmart-tech.org/legal

using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace IfcDoc.Schema.SCH
{
	public class schema // MVD View
	{
		[XmlAttribute] public string icon;
		[XmlAttribute] public string defaultphase;
		public string p;
		[XmlElement("phase")] public List<phase> Phases = new List<phase>();
		[XmlElement("pattern")] public List<pattern> Patterns = new List<pattern>();
	}

	public class phase // MVD Exchange
	{
		[XmlAttribute] public string id;

		[XmlElement("active")] public List<active> Actives = new List<active>();
	}

	public class active // MVD Exchange Requirement
	{
		[XmlAttribute] public string pattern;
	}

	public class pattern // MVD Concept
	{
		[XmlAttribute] public string id;
		[XmlAttribute] public string name;
		public string p;
		[XmlElement("rule")] public List<rule> Rules = new List<rule>();
	}

	public class rule // MVD Rule
	{
		[XmlAttribute("abstract")] public bool is_abstract;
		[XmlAttribute] public string context;
		[XmlElement("assert")] public List<assert> Asserts = new List<assert>();
	}

	public class assert // MVD Constraint
	{
		[XmlAttribute] public string test;
	}
}
