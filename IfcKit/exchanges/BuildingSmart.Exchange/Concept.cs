using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildingSmart.Exchange
{
	public abstract class Concept
	{
		object _target;

		protected Concept(object target)
		{
			this._target = target;
		}

		public object Target
		{
			get
			{
				return this._target;
			}
		}

		protected object GetValue(string path)
		{
			ValuePath valpath = ValuePath.Parse(this._target.GetType().Assembly, path);
			return valpath.GetValue(this._target, null);
		}

		protected void SetValue(string path, object value)
		{
			ValuePath valpath = ValuePath.Parse(this._target.GetType().Assembly, path);
			valpath.SetValue(this._target, value, null);
		}
	}
}
