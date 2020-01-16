// Name:        ThemedTreeView.cs
// Description: Windows TreeView configured to use styles from OS
// Author:      Tim Chipman
// Origination: Work performed for BuildingSmart by Constructivity.com LLC.
// Copyright:   (c) 2013 BuildingSmart International Ltd.
// License:     http://www.buildingsmart-tech.org/legal

using System;
using System.Collections.Generic;
using System.Text;

using System.Runtime.InteropServices;

namespace IfcDoc
{
	public class ThemedTreeView : System.Windows.Forms.TreeView
	{
		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);

			if (!this.DesignMode && Environment.OSVersion.Platform ==
			  PlatformID.Win32NT && Environment.OSVersion.Version.Major >= 6)
			{
				SetWindowTheme(this.Handle, "explorer", null);
				this.ShowLines = false;
			}
		}

		[DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
		public extern static int SetWindowTheme
			(IntPtr hWnd, string pszSubAppName, string pszSubIdList);
	}
}
