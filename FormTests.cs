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
	public partial class FormTests : Form
	{
		public FormTests()
		{
			InitializeComponent();

			for (int i = 0; i < 10; i++)
			{
				label1.Text += i.ToString() + "\n";
			}
		}
	}
}
