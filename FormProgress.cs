// Name:        FormProgress.cs
// Description: Dialog box for showing progress while performing background operations.
// Author:      Tim Chipman
// Origination: Work performed for BuildingSmart by Constructivity.com LLC.
// Copyright:   (c) 2012 BuildingSmart International Ltd.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace IfcDoc
{
	public partial class FormProgress : Form
	{
		int m_progressmax = 100;

		public FormProgress()
		{
			InitializeComponent();
		}

		public string Description
		{
			get
			{
				return this.label1.Text;
			}
			set
			{
				this.label1.Text = value;
			}
		}

		public void SetProgressTotal(int total)
		{
			this.m_progressmax = total;
		}

		public void ReportProgress(int step, object state)
		{
			if (step >= 0)
			{
				this.progressBar.Maximum = this.m_progressmax;
				if (step <= this.progressBar.Maximum)
				{
					this.progressBar.Value = step;
				}
			}
			else
			{
				this.Text += " - Error";
				this.buttonCancel.Text = "Close";
			}

			if (state != null)
			{
				this.textBoxStatus.Text = this.textBoxStatus.Text + "\r\n" + state.ToString();
				this.textBoxStatus.Select(this.textBoxStatus.Text.Length, 0);
				this.textBoxStatus.ScrollToCaret();
			}
		}
	}
}
