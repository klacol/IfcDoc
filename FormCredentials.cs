using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IfcDoc
{
	public partial class FormCredentials : Form
	{
		public FormCredentials()
		{
			InitializeComponent();

			this.checkBoxRemember.Checked = !String.IsNullOrEmpty(Properties.Settings.Default.Username);
			this.textBoxUsername.Text = Properties.Settings.Default.Username;
			this.textBoxPassword.Text = Properties.Settings.Default.Password;
		}

		public string Username
		{
			get
			{
				return this.textBoxUsername.Text;
			}
		}

		// no sense in using SecureString as it will be sent in the clear anyways
		public string Password
		{
			get
			{
				return this.textBoxPassword.Text;
			}
		}

		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);

			if (this.checkBoxRemember.Checked)
			{
				Properties.Settings.Default.Username = this.textBoxUsername.Text;
				Properties.Settings.Default.Password = this.textBoxPassword.Text;
			}
			else
			{
				Properties.Settings.Default.Username = String.Empty;
				Properties.Settings.Default.Password = String.Empty;
			}
			Properties.Settings.Default.Save();
		}
	}
}
