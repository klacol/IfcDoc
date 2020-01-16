// Name:        Adapter.cs
// Description: Base class for serializers
// Author:      Tim Chipman
// Origination: Work performed for BuildingSmart by Constructivity.com LLC.
// Copyright:   (c) 2017 BuildingSmart International Ltd.
// License:     http://www.buildingsmart-tech.org/legal

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace BuildingSmart.Serialization
{
	public class Adapter : Inspector
	{
		public Adapter(Type typeProject)
			: base(typeProject, null, null, null, null)
		{
		}

		public Type GetType(string type)
		{
			return this.GetNonAbstractTypeByName(type);
		}

		public IEnumerable<PropertyInfo> GetDirectFields(Type t)
		{
			return this.GetFieldsOrdered(t).Select(x=>x.Value);
		}

		public IList<PropertyInfo> GetInverseFields(Type t)
		{
			return this.GetFieldsInverseAll(t);
		}

		public Type GetCollectionType(Type t)
		{
			return this.GetCollectionInstanceType(t);
		}

		public void UpdateInverseReferences(object instance, PropertyInfo field, object value)
		{
			if (value == null)
				return;

			Type t = value.GetType();
			if (t.IsValueType || t.IsEnum || t == typeof(string))
				return;

			if (value is IEnumerable)
			{
				foreach (object elem in ((IEnumerable)value))
				{
					this.UpdateInverse(instance, field, elem);
				}
			}
			else
			{
				this.UpdateInverse(instance, field, value);
			}
		}

	}
}
