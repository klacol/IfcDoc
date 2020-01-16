using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using IfcDoc.Schema.DOC;

namespace IfcDoc
{
	public partial class FormFilterDefinitions : Form
	{
		public FormFilterDefinitions()
		{
			InitializeComponent();
		}

		public DocDefinitionScopeEnum DefinitionScope
		{
			get
			{
				DocDefinitionScopeEnum scope = DocDefinitionScopeEnum.None;
				if (this.checkBoxType.Checked)
				{
					scope |= DocDefinitionScopeEnum.Type;
				}
				if (this.checkBoxTypeConstant.Checked)
				{
					scope |= DocDefinitionScopeEnum.TypeConstant;
				}
				if (this.checkBoxEntity.Checked)
				{
					scope |= DocDefinitionScopeEnum.Entity;
				}
				if (this.checkBoxEntityAttribute.Checked)
				{
					scope |= DocDefinitionScopeEnum.EntityAttribute;
				}
				if (this.checkBoxRules.Checked)
				{
					scope |= DocDefinitionScopeEnum.RuleWhere;
				}
				if (this.checkBoxPset.Checked)
				{
					scope |= DocDefinitionScopeEnum.Pset;
				}
				if (this.checkBoxPsetProperty.Checked)
				{
					scope |= DocDefinitionScopeEnum.PsetProperty;
				}
				if (this.checkBoxPEnum.Checked)
				{
					scope |= DocDefinitionScopeEnum.PEnum;
				}
				if (this.checkBoxPEnumConstant.Checked)
				{
					scope |= DocDefinitionScopeEnum.PEnumConstant;
				}
				if (this.checkBoxQset.Checked)
				{
					scope |= DocDefinitionScopeEnum.Qset;
				}
				if (this.checkBoxQsetQuantity.Checked)
				{
					scope |= DocDefinitionScopeEnum.QsetQuantity;
				}

				return scope;
			}
		}
	}
}
