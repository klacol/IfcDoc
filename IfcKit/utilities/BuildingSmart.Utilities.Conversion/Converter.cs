// Name:        Converter.cs
// Description: Convert between IFC schema versions
// Author:      Tim Chipman
// Origination: Work performed for BuildingSmart by Constructivity.com LLC.
// Copyright:   (c) 2018 BuildingSmart International Ltd.
// License:     http://www.buildingsmart-tech.org/legal

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

using BuildingSmart.Serialization;

namespace BuildingSmart.Utilities.Conversion
{
	/// <summary>
	/// Interface supporting updating or converting an object to conform to schema
	/// </summary>
	public interface IConvert
	{
		/// <summary>
		/// Either updates the source object or returns a new object compatible with the target schema.
		/// </summary>
		/// <param name="source">The source object to process.</param>
		/// <param name="type">The type of new object to convert, which may be within the same or different assembly as the source object's type.</param>
		/// <returns>Either a new object of the specified type if converted, the same object if updated, or null if no conversion is available to remove the object.</returns>
		object Convert(object source, Type type);
	}

	public class Converter
	{
		Type m_typeSource;
		Type m_typeTarget;
		Dictionary<Type, Type> m_mapTypes;
		Dictionary<Type, IConvert> m_mapConverters;
		Dictionary<Type, IConvert> m_mapHealers;
		Adapter m_adapterSource;
		Adapter m_adapterTarget;

		/// <summary>
		/// Converts between versions of the IFC schema, upwards or downwards.
		/// </summary>
		/// <param name="sourceType">The source type (e.g. BuildingSmart.IFC4X1.IfcProject to capture everything)</param>
		/// <param name="targetType">The target type (e.g. BuildingSmart.IFC2X3_FINAL.IfcProject) -- must be equivalent to sourceType.</param>
		public Converter(Type sourceType, Type targetType) : this(sourceType, targetType, null, null)
		{
		}

		/// <summary>
		/// Converts between versions of the IFC schema, upwards or downwards.
		/// </summary>
		/// <param name="sourceType">The source type (e.g. BuildingSmart.IFC4X1.IfcProject to capture everything)</param>
		/// <param name="targetType">The target type (e.g. BuildingSmart.IFC2X3_FINAL.IfcProject) -- must be equivalent to sourceType.</param>
		/// <param name="mapTypes">Mapping of conversions; if not found then inheritance is used to match the closest base type. Types may be of the same assembly or different assemblies.</param>
		/// <param name="mapConverters">Custom conversion called for performing conversions of specific types.</param>
		public Converter(Type sourceType, Type targetType, Dictionary<Type, Type> mapTypes, Dictionary<Type, IConvert> mapConverters)
		{
			this.m_typeSource = sourceType;
			this.m_typeTarget = targetType;
			this.m_adapterSource = new Adapter(sourceType);
			this.m_adapterTarget = new Adapter(targetType);
			this.m_mapTypes = mapTypes;
			this.m_mapConverters = mapConverters;

			// separate abstract types into healers
			this.m_mapHealers = new Dictionary<Type, IConvert>();
			if (mapConverters != null)
			{
				foreach (Type t in mapConverters.Keys)
				{
					if (t.IsAbstract)
					{
						this.m_mapHealers.Add(t, mapConverters[t]);
					}
				}
			}
		}


		/// <summary>
		/// Converts between versions of the IFC schema, upwards or downwards.
		/// </summary>
		/// <param name="sourceObject">Ths source object (use IfcProject to capture everything), which must be of the sourceType indicated in the constructor.</param>
		/// <returns>The new object that has been converted to the targetType indicated in the constructor.</typeparam></returns>
		public object Convert(object sourceObject)
		{
			// copy over fields and trace dependencies
			ObjectIDGenerator gen = new ObjectIDGenerator();
			Dictionary<object, object> mapInstances = new Dictionary<object, object>(); // map from object from source schema to object in target schema
			Dictionary<object, object> mapChanges = new Dictionary<object, object>(); // map from object from source schema to converted object in source schema
			Queue<object> qRoot = new Queue<object>();
			qRoot.Enqueue(sourceObject);

			while (qRoot.Count > 0)
			{
				// process the queue
				object o = qRoot.Dequeue();
				object oReplacement = o;

				// allocate target object
				Type typeSource = o.GetType();
				Type typeTarget = this.m_adapterTarget.GetType(typeSource.Name);

				// heal any objects first (before transforming)
				Type t = typeSource.BaseType;
				while (t != typeof(object))
				{
					IConvert healer = null;
					if (this.m_mapHealers.TryGetValue(t, out healer))
					{
						healer.Convert(o, t);
					}

					t = t.BaseType;
				}

				Type typeOverride = null;
				if (this.m_mapTypes != null && this.m_mapTypes.TryGetValue(typeSource, out typeOverride))
				{
					typeTarget = typeOverride;

					// pre-convert, such as for geometry (may or may not convert to within the same IFC version)
					IConvert typeconverter = null;
					if (this.m_mapConverters != null && this.m_mapConverters.TryGetValue(typeSource, out typeconverter))
					{
						oReplacement = typeconverter.Convert(o, typeTarget);
						if (oReplacement != null)
						{
							mapChanges.Add(o, oReplacement);

							typeSource = oReplacement.GetType();
							typeTarget = this.m_adapterTarget.GetType(typeSource.Name);
						}
					}
					else
					{
						typeTarget = this.m_adapterTarget.GetType(typeOverride.Name);
					}
				}

				if (typeTarget == null || typeTarget.IsAbstract || oReplacement == null)
				{
					mapInstances.Add(o, null); // record null to void future conversion, report what didn't convert
				}
				else
				{
					object target = Activator.CreateInstance(typeTarget);

					// map it immediately
					mapInstances.Add(o, target);

					bool firstTime;
					long id = gen.GetId(o, out firstTime);

					IEnumerable<PropertyInfo> fieldsDirect = this.m_adapterSource.GetDirectFields(oReplacement.GetType());
					foreach (PropertyInfo field in fieldsDirect)
					{
						if (field != null && !field.PropertyType.IsValueType)
						{
							object inval = field.GetValue(oReplacement);
							if (inval is IEnumerable)
							{
								IEnumerable list = (IEnumerable)inval;
								foreach (object oival in list)
								{
									if (oival != null && !oival.GetType().IsValueType && !(oival is string))
									{
										gen.GetId(oival, out firstTime);
										if (firstTime)
										{
											qRoot.Enqueue(oival);
										}
									}
								}
							}
							else if (inval is object)
							{
								if (inval != null && !inval.GetType().IsValueType && !(inval is string))
								{
									gen.GetId(inval, out firstTime);
									if (firstTime)
									{
										qRoot.Enqueue(inval);
									}
								}
							}
						}
					}

					// capture inverse fields -- don't use properties, as those will allocate superflously
					IList<PropertyInfo> fields = this.m_adapterSource.GetInverseFields(oReplacement.GetType());
					foreach (PropertyInfo field in fields)
					{
						object inval = field.GetValue(oReplacement);
						if (inval is IEnumerable)
						{
							IEnumerable list = (IEnumerable)inval;
							foreach (object oival in list)
							{
								gen.GetId(oival, out firstTime);
								if (firstTime)
								{
									qRoot.Enqueue(oival);
								}
							}
						}
						else if (inval is object)
						{
							gen.GetId(inval, out firstTime);
							if (firstTime)
							{
								qRoot.Enqueue(inval);
							}
						}
					}
				}
			}

			// populate fields
			foreach (object o in mapInstances.Keys)
			{
				object target = mapInstances[o];
				if (target != null)
				{
					object source = null;
					if (!mapChanges.TryGetValue(o, out source))
					{
						source = o;
					}

					IList<PropertyInfo> fieldsSource = this.m_adapterSource.GetDirectFields(source.GetType()).ToList();
					IList<PropertyInfo> fieldsTarget = this.m_adapterTarget.GetDirectFields(target.GetType()).ToList();
					for (int iField = 0; iField < fieldsSource.Count && iField < fieldsTarget.Count; iField++)
					{
						PropertyInfo fieldSource = fieldsSource[iField];
						PropertyInfo fieldTarget = fieldsTarget[iField];
						if (fieldSource != null && fieldTarget != null) // null if derived
						{
							object valueSource = fieldSource.GetValue(source);
							if (valueSource != null)
							{
								object valueTarget = ConvertValue(valueSource, fieldTarget.PropertyType, mapInstances);
								if (valueTarget != null)
								{
									fieldTarget.SetValue(target, valueTarget);
									this.m_adapterTarget.UpdateInverseReferences(target, fieldTarget, valueTarget);
								}
							}
							else if (fieldTarget.PropertyType.IsEnum && !fieldTarget.PropertyType.IsGenericType) // if non-nullable enum, must populate
							{
								object valueTarget = null;

								// use NOTDEFINED if provided, otherwise pick first constant 
								// (e.g. IfcSpatialStructureElement.ElementCompositionType is required in IFC2x3, optional in IFC4, and there's no NOTDEFINED value) --> ELEMENT should be used in such case
								FieldInfo[] fieldConstants = fieldTarget.PropertyType.GetFields(BindingFlags.Public | BindingFlags.Static);
								foreach (FieldInfo fieldConst in fieldConstants)
								{
									Enum enumvalue = (Enum)fieldConst.GetValue(null);
									int intvalue = (int)System.Convert.ChangeType(enumvalue, enumvalue.GetTypeCode());
									if (intvalue == 0)
									{
										valueTarget = enumvalue;
										break;
									}
								}

								valueTarget = fieldConstants[0].GetValue(null);
								fieldTarget.SetValue(target, valueTarget);
							}
						}
					}

					// populate any required fields on target object
					//for (int iField = 0; iField < fieldsTarget.Count; iField++)
					//{

					//}
				}
			}

			return mapInstances[sourceObject];
		}

		private object ConvertValue(object valueSource, Type declaredType, Dictionary<object, object> mapInstances)
		{
			if (valueSource == null)
				return null;

			if (declaredType == typeof(string) && valueSource is string)
				return (string)valueSource;

			Type targetType = declaredType;

			if (typeof(IEnumerable).IsInstanceOfType(valueSource) && !(valueSource is string))
			{
				Type typeCollection = this.m_adapterTarget.GetCollectionType(targetType);
				IEnumerable list = (IEnumerable)Activator.CreateInstance(typeCollection);

				// perf: customize...
				MethodInfo methodAdd = typeCollection.GetMethod("Add");
				if (methodAdd != null)
				{
					foreach (object elementSource in ((IEnumerable)valueSource))
					{
						try
						{
							Type elemType = targetType.GetGenericArguments()[0];
							object elementTarget = ConvertValue(elementSource, elemType, mapInstances);
							if (elementTarget != null)
							{
								methodAdd.Invoke(list, new object[] { elementTarget }); // perf!!
							}
							else
							{
								elemType.ToString();
							}
						}
						catch (Exception)
						{
						}
					}
				}

				return list;
			}
			else if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
			{
				targetType = targetType.GetGenericArguments()[0];
			}

			Type typeValueSource = valueSource.GetType();
			if (typeValueSource.IsValueType || typeValueSource == typeof(string)) //... also byte[]
			{
				// target field could be a direct long/double/boolean/string -- or wrapped as IfcLengthMeasure, ...
				// specific field types could have changed between schemas so long as underlying value type is similar

				// get the underlying value
				object primSource = null;
				if (typeValueSource.IsPrimitive || typeValueSource.IsEnum)
				{
					primSource = valueSource;
				}
				else
				{
					FieldInfo fieldValueSource = typeValueSource.GetField("Value", BindingFlags.Public | BindingFlags.Instance);
					if (fieldValueSource != null)
					{
						primSource = fieldValueSource.GetValue(valueSource);
						if (primSource == null)
							return null;
					}
				}

				if (targetType.IsPrimitive)
				{
					return primSource;
				}
				else
				{
					Type typeFieldTarget = this.m_adapterTarget.GetType(typeValueSource.Name); // if select
					if (typeFieldTarget == null)
					{
						// fall back on declared type //... need to deal with case of SELECT where IfcNonNegativeLengthMeasure --> IfcLengthMeasure
						typeFieldTarget = targetType;
					}

					if (typeFieldTarget != null)
					{
						if (typeFieldTarget.IsGenericType && typeFieldTarget.GetGenericTypeDefinition() == typeof(Nullable<>))
						{
							typeFieldTarget = typeFieldTarget.GetGenericArguments()[0];
						}

						if (typeFieldTarget.IsEnum)
						{
							FieldInfo fieldConstant = typeFieldTarget.GetField(primSource.ToString(), BindingFlags.Static | BindingFlags.Public);
							if (fieldConstant != null)
							{
								return fieldConstant.GetValue(null);
							}
							else if (targetType == declaredType)
							{
								// if enum was provided, but it doesn't exist in other schema, then map it to whatever is 0 (NOTDEFINED)
								return Activator.CreateInstance(targetType);
							}
						}
						else
						{
							object valueTarget = Activator.CreateInstance(typeFieldTarget);
							FieldInfo fieldValueTarget = typeFieldTarget.GetField("Value", BindingFlags.Public | BindingFlags.Instance);
							if (fieldValueTarget != null)
							{
								// resolve derived type indirections recursively
								// e.g. IfcPositiveLengthMeasure -> IfcLengthMeasure -> double
								object convertedvalue = ConvertValue(primSource, fieldValueTarget.FieldType, mapInstances);

								fieldValueTarget.SetValue(valueTarget, convertedvalue);
								return valueTarget;
							}
						}
					}
				}
			}
			else
			{
				// object reference
				object valueTarget = null;
				if (mapInstances.TryGetValue(valueSource, out valueTarget))
				{
					return valueTarget;
				}
			}

			return null;
		}

	}

}
