
// Name:        DocumentationISO.cs
// Description: Generates documentation
// Author:      Tim Chipman
// Origination: Work performed for BuildingSmart by Constructivity.com LLC.
// Copyright:   (c) 2010 BuildingSmart International Ltd.
// License:     https://standards.buildingsmart.org/legal

using System;
using System.Text;
//using System.Xml.Serialization;

//using BuildingSmart.IFC;

using Ifc2Json.Schema.DOC;

namespace Ifc2Json
{
    public static class DocumentationISO
    {
        /// <summary>
        /// Capture link to table or figure
        /// </summary>
        public struct ContentRef
        {
            public string Caption; // caption to be displayed in table of contents
            public DocObject Page; // relative link to reference

            public ContentRef(string caption, DocObject page)
            {
                this.Caption = caption;
                this.Page = page;
            }
        }

        public static string MakeLinkName(DocObject docobj)
        {
            if (docobj == null)
                return null;

            if (docobj.Name == null)
                return docobj.Uuid.ToString();

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < docobj.Name.Length; i++)
            {
                Char ch = docobj.Name[i];
                if ((ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9') || ch == '-' || ch == '_')
                {
                    sb.Append(ch);
                }
                else if (ch == ' ')
                {
                    sb.Append('-');
                }
            }

            return sb.ToString().ToLower();
        }

    }
}