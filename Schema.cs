// Name:        Schema.cs
// Description: Base classes for schema support
// Author:      Tim Chipman
// Origination: This is based on prior work of Constructivity donated to BuildingSmart at no charge.
// Copyright:   (c) 2010 BuildingSmart International Ltd., (c) 2006-2010 Constructivity.com LLC.
// Note:        This specific file has dual copyright such that both organizations maintain all rights to its use.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Text;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace IfcDoc.Schema
{
	[Flags]
	public enum FieldScope
	{
		Direct = 1,
		Inverse = 2,
		All = 3,
	}

	/// <summary>
	/// A context-bound object that can query relationships or pull content dynamically from broker
	/// </summary>
	[DataContract]
	public abstract class SEntity :
		ICloneable
	{
		static Dictionary<Type, IList<FieldInfo>> s_fieldmap = new Dictionary<Type, IList<FieldInfo>>(); // cached field lists in declaration order
		static Dictionary<Type, IList<FieldInfo>> s_inversemap = new Dictionary<Type, IList<FieldInfo>>();
		static Dictionary<Type, IList<FieldInfo>> s_fieldallmap = new Dictionary<Type, IList<FieldInfo>>();
		static Dictionary<Type, IList<PropertyInfo>> s_propertymapdeclared = new Dictionary<Type, IList<PropertyInfo>>(); // cached properties per type
		static Dictionary<Type, IList<PropertyInfo>> s_propertymapinverse = new Dictionary<Type, IList<PropertyInfo>>(); // cached properties per type

		public static IList<FieldInfo> GetFieldsOrdered(Type type)
		{
			IList<FieldInfo> fields = null;
			if (s_fieldmap.TryGetValue(type, out fields))
			{
				return fields;
			}

			fields = new List<FieldInfo>();
			BuildFieldList(type, fields, FieldScope.Direct);
			s_fieldmap.Add(type, fields);

			return fields;
		}

		public static IList<FieldInfo> GetFieldsInverse(Type type)
		{
			IList<FieldInfo> fields = null;
			if (s_inversemap.TryGetValue(type, out fields))
			{
				return fields;
			}

			fields = new List<FieldInfo>();
			BuildFieldList(type, fields, FieldScope.Inverse);
			s_inversemap.Add(type, fields);

			return fields;
		}

		private static void BuildFieldList(Type type, IList<FieldInfo> list, FieldScope scope)
		{
			if (type.IsValueType && ((scope & FieldScope.Direct) != 0))
			{
				FieldInfo fieldinfo = type.GetField("Value");
				if (fieldinfo != null)
				{
					list.Add(fieldinfo);
				}
				return;
			}

			if (type.BaseType != null && type.BaseType != typeof(object) && type.BaseType != typeof(SEntity))
			{
				BuildFieldList(type.BaseType, list, scope);
			}

			FieldInfo[] fields = type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			FieldInfo[] sorted = new FieldInfo[fields.Length];

			if ((scope & FieldScope.Direct) != 0)
			{
				foreach (FieldInfo field in fields)
				{
					if (field.IsDefined(typeof(DataMemberAttribute), false))
					{
						DataMemberAttribute attr = (DataMemberAttribute)field.GetCustomAttributes(typeof(DataMemberAttribute), false)[0];
						sorted[attr.Order] = field;
					}
				}

				foreach (FieldInfo sort in sorted)
				{
					if (sort != null)
					{
						list.Add(sort);
					}
				}
			}

			if ((scope & FieldScope.Inverse) != 0)
			{
				foreach (FieldInfo field in fields)
				{
					if (field.IsDefined(typeof(InversePropertyAttribute), false))
					{
						// sort order...
						list.Add(field);
					}
				}
			}
		}


		// INSTANCE methods

		public virtual void Delete()
		{
			// no longer does anything inherently -- derived classes may override to remove references pointing to object
			// todo: clean up code so this is no longer called
		}

		/// <summary>
		/// Gets user-friendly caption describing the type of object.
		/// </summary>
		/// <returns></returns>
		public virtual string GetTypeCaption()
		{
			// use display name if one exists
			if (this.GetType().IsDefined(typeof(DisplayNameAttribute), false))
			{
				DisplayNameAttribute attr = (DisplayNameAttribute)this.GetType().GetCustomAttributes(typeof(DisplayNameAttribute), false)[0];
				return attr.DisplayName;
			}

			// try base class (i.e. BeamStandardCase -> Beam)
			if (this.GetType().BaseType.IsDefined(typeof(DisplayNameAttribute), false))
			{
				DisplayNameAttribute attr = (DisplayNameAttribute)this.GetType().BaseType.GetCustomAttributes(typeof(DisplayNameAttribute), false)[0];
				return attr.DisplayName;
			}

			// fall back on internal type
			return this.GetType().Name;
		}

		public virtual object Clone()
		{
			Type t = this.GetType();

			// make a copy, attached to broker
			SEntity clone = (SEntity)Activator.CreateInstance(t);

			// reference all registered fields
			IList<FieldInfo> fields = SEntity.GetFieldsOrdered(t);
			foreach (FieldInfo field in fields)
			{
				if (field.FieldType.IsValueType || field.FieldType == typeof(string))
				{
					// copy over value types
					object val = field.GetValue(this);
					field.SetValue(clone, val);
				}
				else if (field.FieldType.IsInterface || typeof(SEntity).IsAssignableFrom(field.FieldType))
				{
					// make unique copy of referenced type except for owner history!!!
					object val = field.GetValue(this);
					if (val is SEntity)
					{
						SEntity sentity = (SEntity)val;

						SEntity valclone = (SEntity)sentity.Clone();
						field.SetValue(clone, valclone);
					}
					else
					{
						field.SetValue(clone, val);
					}
				}
				else if (typeof(IList).IsAssignableFrom(field.FieldType))
				{
					IList listSource = (IList)field.GetValue(this);
					if (listSource != null)
					{
						// don't copy collections, but initialize new collection
						System.Collections.IList listClone = (System.Collections.IList)Activator.CreateInstance(field.FieldType);
						field.SetValue(clone, listClone);

						Type[] genericargs = field.FieldType.GetGenericArguments();
						if (genericargs.Length == 1)
						{
							foreach (object element in listSource)
							{
								object elemClone = null;

								// clone resources -- don't carry over rooted objects
								if (element is ICloneable)
								{
									// clone resources, list of list, e.g. IfcBSplineSurface
									elemClone = ((ICloneable)element).Clone();
								}
								else
								{
									// i.e. length coordinate
									elemClone = element;
								}

								// now add to list, INCLUDING IF NULL such as blank entries of table
								listClone.Add(elemClone);
							}
						}
					}
				}
			}

			return clone;
		}
		
	}

}
