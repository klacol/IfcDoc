// Name:        SchemaVex.cs
// Description: Schema for Visual Express (.vex) files.
// Author:      Tim Chipman
// Origination: Work performed for BuildingSmart by Constructivity.com LLC.
// Copyright:   (c) 2010 BuildingSmart International Ltd.
// License:     http://www.buildingsmart-tech.org/legal

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;

namespace IfcDoc.Schema.VEX
{
	// Entities for Visual Express

	public static class SchemaVEX
	{
		public static Type[] Types
		{
			get
			{
				List<Type> listTypes = new List<Type>();
				Type[] types = typeof(SchemaVEX).Assembly.GetTypes();
				foreach (Type t in types)
				{
					if (typeof(SEntity).IsAssignableFrom(t) && !t.IsAbstract && t.Namespace.Equals("IfcDoc.Schema.VEX"))
					{
						string name = t.Name.ToUpper();
						listTypes.Add(t);
					}
				}

				return listTypes.ToArray();
			}
		}
	}

	public class AGGREGATES : SEntity
	{
		[DataMember(Order = 0)] public AGGREGATES next;
		[DataMember(Order = 1)] public int aggrtype;
		[DataMember(Order = 2)] public string lower; // was int (changed in Visual Express 5.00.0310.b01 : 16 Mar 2011
		[DataMember(Order = 3)] public string upper; // was int (changed in Visual Express 5.00.0310.b01 : 16 Mar 2011
		[DataMember(Order = 4)] public int flag;
	}

	public class ATTRIBUTE_DEF : DEFINITION
	{
		[DataMember(Order = 0)] public AGGREGATES aggregates;
		[DataMember(Order = 1)] public string is_derived;
		[DataMember(Order = 2)] public int attributeflag;
		[DataMember(Order = 3)] public OBJECT is_inverse;
		[DataMember(Order = 4)] public IList<OBJECT> inversefor;
		[DataMember(Order = 5)] public OBJECT retyped;
		[DataMember(Order = 6)] public OBJECT the_attribute;
		[DataMember(Order = 7)] public OBJECT_LINE_LAYOUT layout;
		[DataMember(Order = 8)] public COMMENT comment;

		[DataMember(Order = 9)] public string user_redeclaration;
		[DataMember(Order = 10)] public string name_before_redeclaration;
	}

	public class BACKGROUND_HTML : SEntity
	{
		[DataMember(Order = 0)] public int _A0;
		[DataMember(Order = 1)] public string image;
		[DataMember(Order = 2)] public int color;
	}

	public class BACKGROUND_SET : SEntity
	{
		[DataMember(Order = 0)] public COLOR color;
		[DataMember(Order = 1)] public int width;
	}

	public class CIRCLE_SET : SEntity
	{
		[DataMember(Order = 0)] public LINE_SET lineset;
		[DataMember(Order = 1)] public BACKGROUND_SET backgroundset;
		[DataMember(Order = 2)] public double radius;
		[DataMember(Order = 3)] public int _A3;
	}

	public class COLOR : SEntity
	{
		[DataMember(Order = 0)] public int red;
		[DataMember(Order = 1)] public int green;
		[DataMember(Order = 2)] public int blue;
	}

	public class COMMENT : OBJECT
	{
		[DataMember(Order = 0)] public TEXT text;
		[DataMember(Order = 1)] public TYPE_LAYOUT layout;
	}

	public abstract class CONSTRAINT : SEntity
	{
		[DataMember(Order = 0)] public string name;
		[DataMember(Order = 1)] public string rule_context;
	}

	public class DEFINED_DEF : OBJECT
	{
		[DataMember(Order = 0)] public AGGREGATES aggregates;
		[DataMember(Order = 1)] public object defined;
		[DataMember(Order = 2)] public OBJECT_LINE_LAYOUT object_line_layout;
		[DataMember(Order = 3)] public TEXT name;
	}

	public class DEFINED_TYPE : DEFINITION
	{
		[DataMember(Order = 0)] public DEFINED_DEF defined;
		[DataMember(Order = 1)] public List<WHERE_RULE> whererules;
		[DataMember(Order = 2)] public TYPE_LAYOUT layout;
		[DataMember(Order = 3)] public COMMENT comment;
		[DataMember(Order = 4)] public INTERFACE_TO interfaceto;
	}

	public class DESCRIPTION_CONTENT_HTML : SEntity
	{
		[DataMember(Order = 0)] public FONT_HTML font;
		[DataMember(Order = 1)] public OUTLOOK_HTML nameoutlook;
		[DataMember(Order = 2)] public int namewidth;
	}

	public class DIAGRAM_HTML : SEntity
	{
		[DataMember(Order = 0)] public int flag;
		[DataMember(Order = 1)] public int imagetype;
		[DataMember(Order = 2)] public int jpegquality;
		[DataMember(Order = 3)] public int pngtype;
		[DataMember(Order = 4)] public int border;
		[DataMember(Order = 5)] public int color;
		[DataMember(Order = 6)] public int width;
	}

	public class ENTITIES : DEFINITION
	{
		[DataMember(Order = 0)] public IList<SUPERTYPE_DEF> supertypes;
		[DataMember(Order = 1)] public IList<SUBTYPE_DEF> subtypes;
		[DataMember(Order = 2)] public IList<ATTRIBUTE_DEF> attributes;
		[DataMember(Order = 3)] public IList<UNIQUE_RULE> uniquenes;
		[DataMember(Order = 4)] public IList<WHERE_RULE> wheres;
		[DataMember(Order = 5)] public int inglobalrules;
		[DataMember(Order = 6)] public TYPE_LAYOUT layout;
		[DataMember(Order = 7)] public COMMENT comment;
		[DataMember(Order = 8)] public INTERFACE_TO interfaceto;

		public override string ToString()
		{
			return name.text;
		}
	}

	public class ENUMERATIONS : DEFINITION
	{
		[DataMember(Order = 0)] public IList<string> enums;
		[DataMember(Order = 1)] public IList<WHERE_RULE> wheres;
		[DataMember(Order = 2)] public TYPE_LAYOUT typelayout;
		[DataMember(Order = 3)] public COMMENT comment;
		[DataMember(Order = 4)] public INTERFACE_TO interfaceto;
		[DataMember(Order = 5)] public object basedon;
		[DataMember(Order = 6)] public List<object> extendedby;
		[DataMember(Order = 7)] public bool isextensible;

		public override string ToString()
		{
			return name.text;
		}
	}

	public class EXPRESS_HTML : SEntity
	{
		[DataMember(Order = 0)] public int flag;
		[DataMember(Order = 1)] public OUTLOOK_HTML keyword;
		[DataMember(Order = 2)] public int textwidth;
		[DataMember(Order = 3)] public int indent0;
		[DataMember(Order = 4)] public int indent1;
		[DataMember(Order = 5)] public int indent2;
		[DataMember(Order = 6)] public int attributewidth;
		[DataMember(Order = 7)] public int constraintwidth;
	}

	public class EXPRESS_SET : SEntity
	{
		[DataMember(Order = 0)] public int compileroptions;
	}

	public class FONT_HTML : SEntity
	{
		[DataMember(Order = 0)] public string fontname;
		[DataMember(Order = 1)] public int size;
		[DataMember(Order = 2)] public OUTLOOK_HTML outlook;
	}

	public class GLOBAL_RULE : OBJECT
	{
		[DataMember(Order = 0)] public string name;
		[DataMember(Order = 1)] public string rule_context;
		[DataMember(Order = 2)] public IList<OBJECT> for_entities;
		[DataMember(Order = 3)] public IList<WHERE_RULE> where_rule;
		[DataMember(Order = 4)] public COMMENT comment;
		[DataMember(Order = 5)] public INTERFACE_TO interfaceto;
	}

	public class IMAGE_MAP_HTML : SEntity
	{
		[DataMember(Order = 0)] public string hyperlink;
		[DataMember(Order = 1)] public int left;
		[DataMember(Order = 2)] public int top;
		[DataMember(Order = 3)] public int right;
		[DataMember(Order = 4)] public int bottom;
	}

	public class INTERFACE_SCHEMA : OBJECT
	{
		[DataMember(Order = 0)] public int ref_or_use;
		[DataMember(Order = 1)] public string schema_name;
		[DataMember(Order = 2)] public IList<object> item;
		[DataMember(Order = 3)] public int express_g_model_id;
		[DataMember(Order = 4)] public OBJECT default_item;
	}

	public class INTERFACE_TO : SEntity
	{
		[DataMember(Order = 0)] public INTERFACE_SCHEMA theschema;
		[DataMember(Order = 1)] public TEXT aliasname;
	}

	public class LINE_SET : SEntity
	{
		[DataMember(Order = 0)] public COLOR color;
		[DataMember(Order = 1)] public double thickness;
		[DataMember(Order = 2)] public int dash;
	}

	public class NAVBAR_HTML : SEntity
	{
		[DataMember(Order = 0)] public string name;
		[DataMember(Order = 1)] public string image;
		[DataMember(Order = 2)] public int dx;
		[DataMember(Order = 3)] public int dy;
		[DataMember(Order = 4)] public IList<IMAGE_MAP_HTML> map;
	}

	public abstract class OBJECT : SEntity
	{
		[DataMember(Order = 0)] public int objecttype;
		[DataMember(Order = 1)] public int flag;
		[DataMember(Order = 2)] public OBJECT objectpageref;
		[DataMember(Order = 3)] public OBJECT objectpagerefto;
	}

	/// <summary>
	/// Intermediate type, not defined in VEX schema
	/// </summary>
	public abstract class DEFINITION : OBJECT
	{
		[DataMember(Order = 0)] public TEXT name;
	}

	public class OBJECT_LINE_LAYOUT : SEntity
	{
		[DataMember(Order = 0)] public TEXT_PLACEMENT textplacement;
		[DataMember(Order = 1)] public WORLD_PLINE pline;
	}

	public class OUTLOOK_HTML : SEntity
	{
		[DataMember(Order = 0)] public int flag;
		[DataMember(Order = 1)] public int color;
	}

	public class PAGE_REF : OBJECT
	{
		[DataMember(Order = 0)] public TEXT text;
		[DataMember(Order = 1)] public int pagenr;
		[DataMember(Order = 2)] public int id;
		[DataMember(Order = 3)] public int showrefpages;
		[DataMember(Order = 4)] public PAGE_REF_LINE pageline;
		[DataMember(Order = 5)] public List<PAGE_REF_TO> pagerefto;
		[DataMember(Order = 6)] public TYPE_LAYOUT layout;
	}

	public class PAGE_REF_LINE : OBJECT
	{
		[DataMember(Order = 0)] public OBJECT pageref;
		[DataMember(Order = 1)] public OBJECT_LINE_LAYOUT layout;
	}

	public class PAGE_REF_TO : OBJECT
	{
		[DataMember(Order = 0)] public TEXT text;
		[DataMember(Order = 1)] public int pagenr;
		[DataMember(Order = 2)] public DEFINITION pageref;//OBJECT pageref;
		[DataMember(Order = 3)] public int index;
		[DataMember(Order = 4)] public TYPE_LAYOUT layout;
		[DataMember(Order = 5)] public int primitivetype;
	}

	public class PAGE_SET : SEntity
	{
		[DataMember(Order = 0)] public LINE_SET lineset;
		[DataMember(Order = 1)] public double leftmargin;
		[DataMember(Order = 2)] public double topmargin;
		[DataMember(Order = 3)] public double rightmargin;
		[DataMember(Order = 4)] public double bottommargin;
		[DataMember(Order = 5)] public double toptextsize;
		[DataMember(Order = 6)] public double bottomtextsize;
		[DataMember(Order = 7)] public double width;
		[DataMember(Order = 8)] public double height;
		[DataMember(Order = 9)] public int nhorizontalpages;
		[DataMember(Order = 10)] public int nverticalpages;
		[DataMember(Order = 11)] public double paperwidth;
		[DataMember(Order = 12)] public double paperheight;
		[DataMember(Order = 13)] public int papersizeindex;
		[DataMember(Order = 14)] public int flag;
	}

	public class PARAMETER : SEntity
	{
		[DataMember(Order = 0)] public string name;
		[DataMember(Order = 1)] public object parameter_type;
	}

	public class PLINE_SET : SEntity
	{
		[DataMember(Order = 0)] public LINE_SET lineset;
		[DataMember(Order = 1)] public TEXT_SET textset;
		[DataMember(Order = 2)] public CIRCLE_SET circleset;
	}

	public class PRIMITIVE_TYPE : DEFINITION
	{
		[DataMember(Order = 0)] public int the_primitive;
		[DataMember(Order = 1)] public TYPE_LAYOUT layout;
		[DataMember(Order = 2)] public int constraints;
	}

	public class RECT_SET : SEntity
	{
		[DataMember(Order = 0)] public LINE_SET lineset;
		[DataMember(Order = 1)] public TEXT_SET textset;
		[DataMember(Order = 2)] public VLINE vline;
		[DataMember(Order = 3)] public int size;
		[DataMember(Order = 4)] public BACKGROUND_SET backgroundset;
		[DataMember(Order = 5)] public WORLD_POINT worldpoint;
	}

	public class RECTANGLE : SEntity
	{
		[DataMember(Order = 0)] public double x;
		[DataMember(Order = 1)] public double y;
		[DataMember(Order = 2)] public double dx;
		[DataMember(Order = 3)] public double dy;
	}

	public class REPORT_HTML : SEntity
	{
		[DataMember(Order = 0)] public string homepage;
		[DataMember(Order = 1)] public IList<FONT_HTML> styletoplevel;
		[DataMember(Order = 2)] public IList<DESCRIPTION_CONTENT_HTML> stylesecondlevel;
		[DataMember(Order = 3)] public BACKGROUND_HTML background;
		[DataMember(Order = 4)] public EXPRESS_HTML express;
		[DataMember(Order = 5)] public DIAGRAM_HTML diagram;
		[DataMember(Order = 6)] public NAVBAR_HTML navbar;
	}

	public class REPORT_SET : SEntity
	{
		[DataMember(Order = 0)] public double expgscale;
		[DataMember(Order = 1)] public int _A1;
		[DataMember(Order = 2)] public IList<int> sortlevel;
		[DataMember(Order = 3)] public IList<STYLE_SET> style;
		[DataMember(Order = 4)] public string headertext;
		[DataMember(Order = 5)] public string footertext;
		[DataMember(Order = 6)] public List<string> specificationname;
		[DataMember(Order = 7)] public REPORT_HTML html;
	}

	public class SAVE_SET : SEntity
	{
		[DataMember(Order = 0)] public int autosave;
		[DataMember(Order = 1)] public int autosaveinterval;
		[DataMember(Order = 2)] public int savesafe;
		[DataMember(Order = 3)] public int nversions;
	}

	public class SCHEMA_REF : DEFINITION
	{
		//[DataMember(Order = 0)] public TEXT aliasname; // name
		[DataMember(Order = 0)] public object _A1;     // aliasname
		[DataMember(Order = 1)] public object _A2;     // interface
		[DataMember(Order = 2)] public TYPE_LAYOUT layout;
		[DataMember(Order = 3)] public INTERFACE_TO interfaceto;
	}

	public class SCHEMATA : SEntity
	{
		[DataMember(Order = 0)] public int id;
		[DataMember(Order = 1)] public string name;
		[DataMember(Order = 2)] public object layoutname;
		[DataMember(Order = 3)] public int flag;
		[DataMember(Order = 4)] public object showstate;
		[DataMember(Order = 5)] public object selectstate;
		[DataMember(Order = 6)] public int time;
		[DataMember(Order = 7)] public SETTINGS settings;
		[DataMember(Order = 8)] public double scale;
		[DataMember(Order = 9)] public double xpos;
		[DataMember(Order = 10)] public double ypos;
		[DataMember(Order = 11)] public IList<object> objects;
		[DataMember(Order = 12)] public IList<GLOBAL_RULE> global_rules;
		[DataMember(Order = 13)] public IList<USER_FUNCTION> user_functions;
		[DataMember(Order = 14)] public COMMENT comment;
		[DataMember(Order = 15)] public int version;
		[DataMember(Order = 16)] public IList<USER_CONSTANT> user_constant;
		[DataMember(Order = 17)] public SECTION_LIST section;
		[DataMember(Order = 18)] public int? enabled_express_version;
		[DataMember(Order = 19)] public string language_id;
		[DataMember(Order = 20)] public string version_id;
	}

	public class SECTION_LIST : SEntity
	{
		[DataMember(Order = 0)] public int numberofpages; // flag
		[DataMember(Order = 1)] public int _A2;           // number_of_pages
		[DataMember(Order = 2)] public IList<object> section;
	}

	public class SELECTS : DEFINITION
	{
		//[DataMember(Order = 0)] public TEXT name;
		[DataMember(Order = 0)] public IList<SELECT_DEF> selects;
		[DataMember(Order = 1)] public IList<WHERE_RULE> wheres;
		[DataMember(Order = 2)] public TYPE_LAYOUT typelayout;
		[DataMember(Order = 3)] public COMMENT comment;
		[DataMember(Order = 4)] public INTERFACE_TO interfaceto;
		[DataMember(Order = 5)] public object BasedOn;
		[DataMember(Order = 6)] public List<object> ExtendedBy;
		[DataMember(Order = 7)] public bool _A8; // Is_Extensible
		[DataMember(Order = 8)] public bool _A9; // ???
	}

	public class SELECT_DEF : OBJECT
	{
		[DataMember(Order = 0)] public OBJECT def;
		[DataMember(Order = 1)] public OBJECT_LINE_LAYOUT layout;
	}

	public class SELECTION_SET : SEntity
	{
		[DataMember(Order = 0)] public int size;
	}

	public class SETTINGS : SEntity
	{
		[DataMember(Order = 0)] public int flag;
		[DataMember(Order = 1)] public int showstate;
		[DataMember(Order = 2)] public int selectstate;
		[DataMember(Order = 3)] public BACKGROUND_SET background;
		[DataMember(Order = 4)] public object grid;
		[DataMember(Order = 5)] public SELECTION_SET selection;
		[DataMember(Order = 6)] public RECT_SET entities;
		[DataMember(Order = 7)] public PLINE_SET supertypes;
		[DataMember(Order = 8)] public PLINE_SET attributes;
		[DataMember(Order = 9)] public PLINE_SET optattributes;
		[DataMember(Order = 10)] public PLINE_SET definedef;
		[DataMember(Order = 11)] public PLINE_SET selectdef;
		[DataMember(Order = 12)] public PLINE_SET pagerefline;
		[DataMember(Order = 13)] public RECT_SET deftype;
		[DataMember(Order = 14)] public RECT_SET selecttype;
		[DataMember(Order = 15)] public RECT_SET enumerationtype;
		[DataMember(Order = 16)] public RECT_SET primtype;
		[DataMember(Order = 17)] public RECT_SET pageref;
		[DataMember(Order = 18)] public RECT_SET pagerefto;
		[DataMember(Order = 19)] public VISIBLE_SET visibleset;
		[DataMember(Order = 20)] public PAGE_SET page;
		[DataMember(Order = 21)] public double dxtext;
		[DataMember(Order = 22)] public double dytext;
		[DataMember(Order = 23)] public SAVE_SET save;
		[DataMember(Order = 24)] public REPORT_SET report;
		[DataMember(Order = 25)] public RECT_SET comment;
		[DataMember(Order = 26)] public EXPRESS_SET express;
		[DataMember(Order = 27)] public PLINE_SET basedonline;
	}

	public class STYLE_SET : SEntity
	{
		[DataMember(Order = 0)] public string name;
		[DataMember(Order = 1)] public string fontname;
		[DataMember(Order = 2)] public double fontsize;
		[DataMember(Order = 3)] public int fontorientation;
		[DataMember(Order = 4)] public int fontcolor;
		[DataMember(Order = 5)] public int fontstyle;
		[DataMember(Order = 6)] public double indent;
		[DataMember(Order = 7)] public double deltafirstline;
		[DataMember(Order = 8)] public double spacebefore;
		[DataMember(Order = 9)] public double spaceafter;
		[DataMember(Order = 10)] public List<double> tab;
	}

	public class SUBTYPE_DEF : OBJECT
	{
		[DataMember(Order = 0)] public OBJECT the_subtype;
		[DataMember(Order = 1)] public OBJECT_LINE_LAYOUT layout;
	}

	public class SUPERTYPE_DEF : OBJECT
	{
		[DataMember(Order = 0)] public OBJECT the_supertype;
	}

	public class TEXT : OBJECT
	{
		[DataMember(Order = 0)] public string text;
		[DataMember(Order = 1)] public string vtext;
		[DataMember(Order = 2)] public TEXT_LAYOUT layout;
	}

	public class TEXT_LAYOUT : SEntity
	{
		[DataMember(Order = 0)] public double x; // X position relative to origin of line segment (+ is right, - is left)
		[DataMember(Order = 1)] public double y; // Y position relative to origin of line segment (+ is down, - is up)
		[DataMember(Order = 2)] public double width;
		[DataMember(Order = 3)] public double height;
		[DataMember(Order = 4)] public TEXT_TYPE style;
	}

	public class TEXT_PLACEMENT : SEntity
	{
		[DataMember(Order = 0)] public int npos; // 0-based index of line segment where text is placed
		[DataMember(Order = 1)] public int placement;
	}

	public class TEXT_SET : SEntity
	{
		[DataMember(Order = 0)] public int orientation;
		[DataMember(Order = 1)] public int style;
		[DataMember(Order = 2)] public double size;
		[DataMember(Order = 3)] public string fontname;
		[DataMember(Order = 4)] public COLOR color;
	}

	public class TEXT_TYPE : SEntity
	{
		[DataMember(Order = 0)] public int orientation;
		[DataMember(Order = 1)] public int style;
	}

	public class TREE : OBJECT
	{
		[DataMember(Order = 0)] public int operate;
		[DataMember(Order = 1)] public IList<OBJECT> list;
	}

	public class TYPE_LAYOUT : SEntity
	{
		[DataMember(Order = 0)] public RECTANGLE rectangle;
	}

	public class UNIQUE_RULE : SEntity
	{
		[DataMember(Order = 0)] public string name;
		[DataMember(Order = 1)] public IList<ATTRIBUTE_DEF> for_attribute;
	}

	public class UNRESOLVED_OBJECT : OBJECT
	{
		[DataMember(Order = 0)] public TEXT name;
		[DataMember(Order = 1)] public TYPE_LAYOUT layout;
	}

	public class USER_CONSTANT : SEntity
	{
		[DataMember(Order = 0)] public string name;
		[DataMember(Order = 1)] public string value_type;
		[DataMember(Order = 2)] public string value_content;
		[DataMember(Order = 3)] public COMMENT comment;
	}

	public class USER_FUNCTION : CONSTRAINT // multiple inheritance from OBJECT too
	{
		[DataMember(Order = 0)] public int objecttype;
		[DataMember(Order = 1)] public int flag;
		[DataMember(Order = 2)] public OBJECT objectpageref;
		[DataMember(Order = 3)] public OBJECT objectpagerefto;
		[DataMember(Order = 4)] public object return_value;
		[DataMember(Order = 5)] public IList<PARAMETER> parameter_list;
		[DataMember(Order = 6)] public COMMENT comment;
		[DataMember(Order = 7)] public INTERFACE_TO interfaceto;
	}

	public class USER_PROCEDURE : CONSTRAINT // multiple inheritance from OBJECT too
	{
		[DataMember(Order = 0)] public int objecttype;
		[DataMember(Order = 1)] public int flag;
		[DataMember(Order = 2)] public OBJECT objectpageref;
		[DataMember(Order = 3)] public OBJECT objectpagerefto;
		[DataMember(Order = 5)] public IList<PARAMETER> parameter_list;
		[DataMember(Order = 6)] public COMMENT comment;
		[DataMember(Order = 7)] public INTERFACE_TO interfaceto;
	}

	public class VISIBLE_SET : SEntity
	{
		[DataMember(Order = 0)] public IList<bool> list;
	}

	public class VLINE : SEntity
	{
		[DataMember(Order = 0)] public int reffrom;
		[DataMember(Order = 1)] public double dx;
	}

	public class WHERE_RULE : CONSTRAINT
	{
		[DataMember(Order = 0)] public COMMENT comment;
	}

	public class WORLD_POINT : SEntity
	{
		[DataMember(Order = 0)] public double wx; // X position of start, absolute coordinates
		[DataMember(Order = 1)] public double wy; // Y position of start, absolute coordinates
	}

	public class WORLD_PLINE : SEntity
	{
		[DataMember(Order = 0)] public WORLD_POINT startpoint; // position of start, absolute coordinates
		[DataMember(Order = 1)] public int startdirection; // 0 is Y, 1 is X
		[DataMember(Order = 2)] public object radius; // unused
		[DataMember(Order = 3)] public IList<double> rpoint; // list of line lengths in alternating directions (if startdirection is 0, then - is up, + is down)
		[DataMember(Order = 4)] public double endlinepos;
		[DataMember(Order = 5)] public double endlinelength;
	}


}
