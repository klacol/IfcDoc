// Name:        FormCode.cs
// Description: Dialog box for downloading content
// Author:      Tim Chipman
// Origination: Work performed for BuildingSmart by Constructivity.com LLC.
// Copyright:   (c) 2013 BuildingSmart International Ltd.
// License:     http://www.buildingsmart-tech.org/legal

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace IfcDoc
{
	public partial class FormDownload : Form
	{
		Exception m_exception;
		string m_localpath;

		public FormDownload()
		{
			InitializeComponent();
		}

		public string Url
		{
			get
			{
				return this.textBoxUrl.Text;
			}
		}

		public string LocalPath
		{
			get
			{
				return this.m_localpath;
			}
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			this.textBoxUrl.Enabled = false;
			this.progressBar.Value = 0;
			this.buttonOK.Enabled = false;
			this.errorProvider.Clear();

			// start download to local file
			this.backgroundWorkerDownload.RunWorkerAsync();
		}

		private void backgroundWorkerDownload_DoWork(object sender, DoWorkEventArgs e)
		{
			// connect to server
			this.m_exception = null;
			try
			{
				HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(this.textBoxUrl.Text);
				request.Accept = "text/plain";// "application/step";
				using (WebResponse response = request.GetResponse())
				{
					using (Stream streamWeb = response.GetResponseStream())
					{
						this.m_localpath = Path.GetTempFileName();
						this.m_localpath = Path.ChangeExtension(this.m_localpath, ".ifcdoc");
						using (FileStream streamFile = new FileStream(this.m_localpath, FileMode.CreateNew))
						{
							streamFile.SetLength(response.ContentLength);
							byte[] buffer = new byte[8192];
							int len = 0;
							do
							{
								len = streamWeb.Read(buffer, 0, buffer.Length);
								if (len > 0)
								{
									streamFile.Write(buffer, 0, len);
									this.backgroundWorkerDownload.ReportProgress((int)(100L * streamFile.Position / streamFile.Length));
								}
							} while (len > 0);
						}
					}
				}
			}
			catch (Exception x)
			{
				this.m_exception = x;
			}
		}

		private void backgroundWorkerDownload_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			this.progressBar.Value = e.ProgressPercentage;
		}

		private void backgroundWorkerDownload_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (m_exception != null)
			{
				this.textBoxUrl.Enabled = true;
				this.buttonOK.Enabled = true;
				this.errorProvider.SetError(this.labelError, this.m_exception.Message);
			}
			else
			{
				this.DialogResult = DialogResult.OK;
				this.Close();
			}
		}
	}
}
