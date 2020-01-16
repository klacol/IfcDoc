// Name:        Compiler.cs
// Description: .NET Assembly Generator
// Author:      Tim Chipman
// Origination: Work performed for BuildingSmart International Ltd. and Georgia Tech by Constructivity.com LLC.
// Copyright:   (c) 2012-2014 BuildingSmart International Ltd., (c) 2014 Georgia Tech
// License:     http://www.buildingsmart-tech.org/legal

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using System.Resources;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using IfcDoc.Schema;
using IfcDoc.Schema.DOC;

namespace IfcDoc
{
	public class Compiler
	{
		private DocProject m_project;
		private Dictionary<string, DocModelView> m_views;
		private DocExchangeDefinition m_exchange;
		private AssemblyBuilder m_assembly;
		private ModuleBuilder m_module;
		private Dictionary<string, DocObject> m_definitions;
		private Dictionary<string, Type> m_types;
		private Dictionary<string, string> m_namespaces;
		private Dictionary<Type, Dictionary<string, FieldInfo>> m_fields;
		private Dictionary<DocTemplateDefinition, MethodInfo> m_templates;
		private bool m_psets;
		private string m_rootnamespace;

		public static Type CompileProject(DocProject docProject)
		{
			return CompileProject(docProject, false);
		}

		public static Type CompileProject(DocProject docProject, bool psets)
		{
			return CompileProject(docProject, null, null, psets);
		}
		public static Type CompileProject(DocProject docProject, DocModelView[] views, DocExchangeDefinition exchangeDefinition,  bool psets)
		{
			Compiler compiler = new Compiler(docProject, views, exchangeDefinition, psets);
			System.Reflection.Emit.AssemblyBuilder assembly = compiler.Assembly;
			Type[] types = null;
			try
			{
				types = assembly.GetTypes();
			}
			catch (System.Reflection.ReflectionTypeLoadException)
			{
				// schema could not be compiled according to definition
			}

			foreach (Type t in types)
			{
				// todo: make root type configurable with schema
				if (t.Name.Equals("IfcProject"))
					return t;
			}

			return null; // no root type
		}

		public Compiler(DocProject project, DocModelView[] views, DocExchangeDefinition exchange, bool psets)
		{
			this.m_project = project;
			this.m_views = new Dictionary<string, DocModelView>();
			if (views != null)
			{
				foreach (DocModelView view in views)
					addModelView(view);
			}
			this.m_exchange = exchange;
			this.m_psets = psets;

			// version needs to be included for extracting XML namespace (Major.Minor.Addendum.Corrigendum)
			string version = project.GetSchemaVersion();
			ConstructorInfo conContract = (typeof(AssemblyVersionAttribute).GetConstructor(new Type[] { typeof(string) }));
			CustomAttributeBuilder cabAssemblyVersion = new CustomAttributeBuilder(conContract, new object[] { version });

			string schemaid = project.GetSchemaIdentifier();
			string assembly = "buildingSmart." + schemaid;
			string module = assembly + ".dll";

			this.m_rootnamespace = assembly + ".";

			this.m_assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(assembly), AssemblyBuilderAccess.RunAndSave, new CustomAttributeBuilder[] { cabAssemblyVersion });
			this.m_module = this.m_assembly.DefineDynamicModule(module, module);
			this.m_definitions = new Dictionary<string, DocObject>();
			this.m_types = new Dictionary<string, Type>();
			this.m_fields = new Dictionary<Type, Dictionary<string, FieldInfo>>();
			this.m_templates = new Dictionary<DocTemplateDefinition, MethodInfo>();
			this.m_namespaces = new Dictionary<string, string>();

			Dictionary<DocObject, bool> included = null;
			if (this.m_views != null)
			{
				included = new Dictionary<DocObject, bool>();
				foreach (DocModelView docView in this.m_views.Values)
				{
					this.m_project.RegisterObjectsInScope(docView, included);
				}
			}
			if (psets)
			{
				foreach (DocPropertyEnumeration docPropEnum in project.PropertyEnumerations)
				{
					DocEnumeration docType = docPropEnum.ToEnumeration();
					if (!this.m_definitions.ContainsKey(docType.Name))
					{
						this.m_definitions.Add(docType.Name, docType);
					}
				}
			}

			foreach (DocSection docSection in project.Sections)
			{
				foreach (DocSchema docSchema in docSection.Schemas)
				{
					foreach (DocEntity docEntity in docSchema.Entities)
					{
						if (included == null || included.ContainsKey(docEntity))
						{
							if (!this.m_definitions.ContainsKey(docEntity.Name))
							{
								this.m_definitions.Add(docEntity.Name, docEntity);
								this.m_namespaces.Add(docEntity.Name, docSchema.Name);
							}
						}
					}

					foreach (DocType docType in docSchema.Types)
					{
						if (included == null || included.ContainsKey(docType))
						{
							if (!this.m_definitions.ContainsKey(docType.Name))
							{
								this.m_definitions.Add(docType.Name, docType);
								this.m_namespaces.Add(docType.Name, docSchema.Name);
							}
						}
					}

				}
			}

			// second pass: 
			if (psets)
			{
				foreach (DocSection docSection in project.Sections)
				{
					foreach (DocSchema docSchema in docSection.Schemas)
					{
						foreach (DocPropertySet docPset in docSchema.PropertySets)
						{
							DocEntity docType = docPset.ToEntity(this.m_definitions);
							if (!this.m_definitions.ContainsKey(docType.Name))
							{
								this.m_definitions.Add(docType.Name, docType);
								this.m_namespaces.Add(docType.Name, docSchema.Name);
							}
						}

						foreach (DocQuantitySet docQset in docSchema.QuantitySets)
						{
							DocEntity docType = docQset.ToEntity(this.m_definitions);
							if (!this.m_definitions.ContainsKey(docType.Name))
							{
								this.m_definitions.Add(docType.Name, docType);
								this.m_namespaces.Add(docType.Name, docSchema.Name);
							}
						}
					}
				}
			}

			// first register types and fields
			foreach (string key in this.m_definitions.Keys)
			{
				Type typereg = RegisterType(key);

				// localization -- use custom attributes for now -- ideal way would be to use assembly resources, though has bugs in .net 4.0
				if (typereg is TypeBuilder)
				{
					TypeBuilder tb = (TypeBuilder)typereg;

					DocObject docObj = this.m_definitions[key];
					foreach (DocLocalization docLocal in docObj.Localization)
					{
						CustomAttributeBuilder cab = docLocal.ToCustomAttributeBuilder();
						if (cab != null)
						{
							tb.SetCustomAttribute(cab);
						}
					}
				}
			}

			// now register template functions (may depend on fields existing)

			// find associated ConceptRoot for model view, define validation function
			if (this.m_views != null)
			{
				foreach (DocModelView view in this.m_views.Values)
				{
					string viewname = view.Code;
					foreach (DocConceptRoot root in view.ConceptRoots)
					{
						Type tOpen = null;

						if (this.m_types.TryGetValue(root.ApplicableEntity.Name, out tOpen) && tOpen is TypeBuilder)
						{
							TypeBuilder tb = (TypeBuilder)tOpen;


							// new: generate type for concept root
							//if (view.Name != null && root.Name != null)
							{
								/*
                                string typename = this.m_rootnamespace + "." + view.Name.Replace(" ", "_") + "." + root.Name.Replace(" ", "_");
                                //Type tbConceptRoot = RegisterType(typename);
                                TypeBuilder tbRoot = this.m_module.DefineType(typename, attr, typebase);
                                */

								// add typebuilder to map temporarily in case referenced by an attribute within same class or base class
								//this.m_types.Add(typename, tb);

								foreach (DocTemplateUsage concept in root.Concepts)
								{
									CompileConcept(concept, view, tb);
								}

							}
						}
					}
				}
			}

			//Dictionary<string, Stream> mapResStreams = new Dictionary<string, Stream>();
			//Dictionary<string, ResXResourceWriter> mapResources = new Dictionary<string, ResXResourceWriter>();

			// seal types once all are built
			List<TypeBuilder> listBase = new List<TypeBuilder>();
			foreach (string key in this.m_definitions.Keys)
			{
				Type tOpen = this.m_types[key];
				while (tOpen is TypeBuilder)
				{
					listBase.Add((TypeBuilder)tOpen);
					tOpen = tOpen.BaseType;
				}

				// seal in base class order
				for (int i = listBase.Count - 1; i >= 0; i--)
				{
					Type tClosed = listBase[i].CreateType();
					this.m_types[tClosed.Name] = tClosed;
				}
				listBase.Clear();


				// record bindings
				DocDefinition docDef = this.m_definitions[key] as DocDefinition;
				if (docDef != null)
				{
					docDef.RuntimeType = this.m_types[key];

					if (docDef is DocEntity)
					{
						DocEntity docEnt = (DocEntity)docDef;
						foreach (DocAttribute docAttr in docEnt.Attributes)
						{
							docAttr.RuntimeField = docDef.RuntimeType.GetProperty(docAttr.Name);
						}
					}

#if false // bug in .net framework 4.0+ -- can't read resources dynamically defined
                    // capture localization
                    foreach (DocLocalization docLocal in docDef.Localization)
                    {
                        if (!String.IsNullOrEmpty(docLocal.Locale))
                        {
                            string major = docLocal.Locale.Substring(0, 2).ToLower();

                            ResXResourceWriter reswriter = null;
                            if (!mapResources.TryGetValue(major, out reswriter))
                            {
                                MemoryStream stream = new MemoryStream();
                                mapResStreams.Add(major, stream);

                                reswriter = new ResXResourceWriter(stream);
                                mapResources.Add(major, reswriter);
                            }

                            ResXDataNode node = new ResXDataNode(docDef.Name, docLocal.Name);
                            node.Comment = docLocal.Documentation;
                            reswriter.AddResource(node);
                        }
                    }
#endif
				}
			}

#if false
            foreach (string locale in mapResStreams.Keys)
            {
                ResXResourceWriter writer = mapResources[locale];
                writer.Generate();
                Stream stream = mapResStreams[locale];
                stream.Seek(0, SeekOrigin.Begin);
                m_module.DefineManifestResource("Resources." + locale + ".resx", stream, ResourceAttributes.Public);
            }
#endif
		}
		private void addModelView(DocModelView view)
		{
			if(view != null && !m_views.ContainsKey(view.UniqueId))
			{
				m_views[view.UniqueId] = view;
				if (!string.IsNullOrEmpty(view.BaseView))
					addModelView(m_project.ModelViews.Where(x => string.Compare(view.BaseView, x.UniqueId) == 0).FirstOrDefault());
			}
		}
		private void CompileConcept(DocTemplateUsage concept, DocModelView view, TypeBuilder tb)
		{
			bool includeconcept = true;
			if (this.m_exchange != null)
			{
				includeconcept = false;
				foreach (DocExchangeItem ei in concept.Exchanges)
				{
					if (ei.Exchange == this.m_exchange && ei.Applicability == DocExchangeApplicabilityEnum.Export &&
						(ei.Requirement == DocExchangeRequirementEnum.Mandatory || ei.Requirement == DocExchangeRequirementEnum.Optional))
					{
						includeconcept = true;
					}
				}
			}

			// bool ConceptTemplateA([Parameter1, ...]);
			// {
			//    // for loading reference value:
			//    .ldfld [AttributeRule]
			//    .ldelem [Index] // for collection, get element by index
			//    .castclass [EntityRule] for entity, cast to expected type; 
			//    // for object graphs, repeat the above instructions to load value
			//    
			//    for loading constant:
			//    .ldstr 'value'
			//
			//    for comparison functions:
			//    .cge
			//
			//    for logical aggregations, repeat each item, pushing 2 elements on stack, then run comparison
			//    .or
			//
			//    return the boolean value on the stack
			//    .ret;
			// }

			// bool[] ConceptA()
			// {
			//    bool[] result = new bool[2];
			// 
			//    if parameters are specified, call for each template rule; otherwise call just once
			//    result[0] = ConceptTemplateA([Parameter1, ...]); // TemplateRule#1
			//    result[1] = ConceptTemplateA([Parameter1, ...]); // TemplateRule#2
			// 
			//    return result;
			// }

			// compile a method for the template definition, where parameters are passed to the template


#if true
			if (includeconcept && concept.Definition != null)
			{
				MethodInfo methodTemplate = this.RegisterTemplate(concept.Definition);

				// verify that definition is compatible with entity (user error)
				if (methodTemplate != null && methodTemplate.DeclaringType.IsAssignableFrom(tb))
				{
					string methodname = DocumentationISO.MakeLinkName(view) + "_" + DocumentationISO.MakeLinkName(concept.Definition);
					MethodBuilder method = tb.DefineMethod(methodname, MethodAttributes.Public, CallingConventions.HasThis, typeof(bool[]), null);
					ILGenerator generator = method.GetILGenerator();

					DocModelRule[] parameters = concept.Definition.GetParameterRules();

					if (parameters != null && parameters.Length > 0)
					{
						// allocate array of booleans, store as local variable
						generator.DeclareLocal(typeof(bool[]));
						generator.Emit(OpCodes.Ldc_I4, concept.Items.Count);
						generator.Emit(OpCodes.Newarr, typeof(bool));
						generator.Emit(OpCodes.Stloc_0);

						// call for each item with specific parameters
						for (int row = 0; row < concept.Items.Count; row++)
						{
							DocTemplateItem docItem = concept.Items[row];

							generator.Emit(OpCodes.Ldloc_0);   // push the array object onto the stack, for storage later
							generator.Emit(OpCodes.Ldc_I4, row); // push the array index onto the stack for storage later

							generator.Emit(OpCodes.Ldarg_0);   // push the *this* pointer for the IFC object instance

							// push parameters onto stack
							for (int col = 0; col < parameters.Length; col++)
							{
								DocModelRule docParam = parameters[col];
								string paramvalue = docItem.GetParameterValue(docParam.Identification);
								if (paramvalue != null)
								{
									DocDefinition docParamType = concept.Definition.GetParameterType(docParam.Identification, this.m_definitions);
									if (docParamType is DocDefined)
									{
										DocDefined docDefined = (DocDefined)docParamType;
										switch (docDefined.DefinedType)
										{
											case "INTEGER":
												{
													Int64 ival = 0;
													Int64.TryParse(paramvalue, out ival);
													generator.Emit(OpCodes.Ldc_I8, ival);
													generator.Emit(OpCodes.Box);
												}
												break;

											case "REAL":
												{
													Double dval = 0.0;
													Double.TryParse(paramvalue, out dval);
													generator.Emit(OpCodes.Ldc_R8, dval);
													generator.Emit(OpCodes.Box);
												}
												break;

											case "STRING":
												generator.Emit(OpCodes.Ldstr, paramvalue);
												break;

											default:
												generator.Emit(OpCodes.Ldstr, paramvalue);
												break;
										}
									}
									else
									{
										// assume string
										generator.Emit(OpCodes.Ldstr, paramvalue);
									}
								}
								else
								{
									generator.Emit(OpCodes.Ldnull);
								}
							}

							generator.Emit(OpCodes.Call, methodTemplate); // call the validation function for the concept template
							generator.Emit(OpCodes.Stelem_I1); // store the result (bool) into an array slot 
						}

						// return the array of boolean results
						generator.Emit(OpCodes.Ldloc_0);
						generator.Emit(OpCodes.Ret);
					}
					else
					{
						// allocate array of booleans, store as local variable
						generator.DeclareLocal(typeof(bool[]));
						generator.Emit(OpCodes.Ldc_I4, 1);
						generator.Emit(OpCodes.Newarr, typeof(bool));
						generator.Emit(OpCodes.Stloc_0);

						generator.Emit(OpCodes.Ldloc_0);   // push the array object onto the stack, for storage later
						generator.Emit(OpCodes.Ldc_I4, 0); // push the array index onto the stack for storage later

						// call once
						generator.Emit(OpCodes.Ldarg_0);   // push the *this* pointer for the IFC object instance
						generator.Emit(OpCodes.Call, methodTemplate); // call the validation function for the concept template
						generator.Emit(OpCodes.Stelem_I1); // store the result (bool) into an array slot 

						// return the array of boolean results
						generator.Emit(OpCodes.Ldloc_0);
						generator.Emit(OpCodes.Ret);
					}
				}
				else
				{
					System.Diagnostics.Debug.WriteLine("Incompatible template: " + tb.Name + " - " + concept.Definition.Name);
				}
			}
#endif
			// recurse
			foreach (DocTemplateUsage docChild in concept.Concepts)
			{
				CompileConcept(docChild, view, tb);
			}
		}

		public AssemblyBuilder Assembly
		{
			get
			{
				return this.m_assembly;
			}
		}

		public ModuleBuilder Module
		{
			get
			{
				return this.m_module;
			}
		}

		public FieldInfo RegisterField(Type type, string field)
		{
			while (type != null && type != typeof(object))
			{
				Dictionary<string, FieldInfo> map = this.m_fields[type];
				FieldInfo fieldinfo = null;
				if (map.TryGetValue(field, out fieldinfo))
				{
					return fieldinfo;
				}

				type = type.BaseType;
			}

			return null;
		}

		private PropertyBuilder RegisterProperty(string name, Type typefield, TypeBuilder tb, FieldBuilder fb)
		{

			// The property set and property get methods require a special
			// set of attributes.
			MethodAttributes getSetAttr = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;

			// Define the "get" accessor method for CustomerName.
			MethodBuilder custNameGetPropMthdBldr = tb.DefineMethod("get_" + name, getSetAttr, typefield, Type.EmptyTypes);

			ILGenerator custNameGetIL = custNameGetPropMthdBldr.GetILGenerator();

			custNameGetIL.Emit(OpCodes.Ldarg_0);
			custNameGetIL.Emit(OpCodes.Ldfld, fb);
			custNameGetIL.Emit(OpCodes.Ret);

			// Define the "set" accessor method for CustomerName.
			MethodBuilder custNameSetPropMthdBldr = tb.DefineMethod("set_" + name, getSetAttr, null, new Type[] { typefield });

			ILGenerator custNameSetIL = custNameSetPropMthdBldr.GetILGenerator();

			custNameSetIL.Emit(OpCodes.Ldarg_0);
			custNameSetIL.Emit(OpCodes.Ldarg_1);
			custNameSetIL.Emit(OpCodes.Stfld, fb);
			custNameSetIL.Emit(OpCodes.Ret);

			// Last, we must map the two methods created above to our PropertyBuilder to 
			// their corresponding behaviors, "get" and "set" respectively. 
			PropertyBuilder pb = tb.DefineProperty(name, PropertyAttributes.HasDefault, typefield, null);
			pb.SetGetMethod(custNameGetPropMthdBldr);
			pb.SetSetMethod(custNameSetPropMthdBldr);

			return pb;
		}

		/// <summary>
		/// Creates or returns emitted type, or NULL if no such type.
		/// </summary>
		/// <param name="map"></param>
		/// <param name="typename"></param>
		/// <returns></returns>
		public Type RegisterType(string strtype)
		{
			// this implementation maps direct and inverse attributes to fields for brevity; a production implementation would use properties as well

			if (strtype == null)
				return typeof(object);

			Type type = null;

			// resolve standard types
			switch (strtype)
			{
				case "INTEGER":
					type = typeof(long);
					break;

				case "REAL":
				case "NUMBER":
					type = typeof(double);
					break;

				case "BOOLEAN":
				case "LOGICAL":
					type = typeof(bool);
					break;

				case "STRING":
					type = typeof(string);
					break;

				case "BINARY":
				case "BINARY (32)":
					type = typeof(byte[]);
					break;
			}

			if (type != null)
				return type;

			// check for existing mapped type
			if (this.m_types.TryGetValue(strtype, out type))
			{
				return type;
			}

			// look up
			DocObject docType = null;
			if (!this.m_definitions.TryGetValue(strtype, out docType))
				return null;

			string schema = this.m_namespaces[docType.Name];

			// not yet exist: create it
			TypeAttributes attr = TypeAttributes.Public;
			if (docType is DocEntity)
			{
				attr |= TypeAttributes.Class;

				DocEntity docEntity = (DocEntity)docType;
				if (docEntity.IsAbstract)
				{
					attr |= TypeAttributes.Abstract;
				}

				Type typebase = RegisterType(docEntity.BaseDefinition);

				// calling base class may result in this class getting defined (IFC2x3 schema with IfcBuildingElement), so check again
				if (this.m_types.TryGetValue(strtype, out type))
				{
					return type;
				}

				TypeBuilder tb = this.m_module.DefineType(this.m_rootnamespace + schema + "." + docType.Name, attr, typebase);

				// add typebuilder to map temporarily in case referenced by an attribute within same class or base class
				this.m_types.Add(strtype, tb);

				// custom attributes (required for JSON serialization)
				ConstructorInfo conContract = (typeof(DataContractAttribute).GetConstructor(new Type[] { }));
				PropertyInfo propContractReference = typeof(DataContractAttribute).GetProperty("IsReference");
				CustomAttributeBuilder cbContract = new CustomAttributeBuilder(conContract, new object[] { }, new PropertyInfo[] { propContractReference }, new object[] { false }); // consider setting IsReference to true if/when serializers like JSON support such referencing
				tb.SetCustomAttribute(cbContract);


				string displayname = docType.Name;
				if (displayname != null)
				{
					ConstructorInfo conReq = typeof(DisplayNameAttribute).GetConstructor(new Type[] { typeof(string) });
					CustomAttributeBuilder cabReq = new CustomAttributeBuilder(conReq, new object[] { displayname });
					tb.SetCustomAttribute(cabReq);
				}

				string description = docType.Documentation;
				if (description != null)
				{
					ConstructorInfo conReq = typeof(DescriptionAttribute).GetConstructor(new Type[] { typeof(string) });
					CustomAttributeBuilder cabReq = new CustomAttributeBuilder(conReq, new object[] { description });
					tb.SetCustomAttribute(cabReq);
				}


				// interfaces implemented by type (SELECTS)
				foreach (DocDefinition docdef in this.m_definitions.Values)
				{
					if (docdef is DocSelect)
					{
						DocSelect docsel = (DocSelect)docdef;
						foreach (DocSelectItem dsi in docsel.Selects)
						{
							if (strtype.Equals(dsi.Name))
							{
								// register
								Type typeinterface = this.RegisterType(docdef.Name);
								tb.AddInterfaceImplementation(typeinterface);
							}
						}
					}
				}

				Dictionary<string, FieldInfo> mapField = new Dictionary<string, FieldInfo>();
				this.m_fields.Add(tb, mapField);

				ConstructorInfo conMember = typeof(DataMemberAttribute).GetConstructor(new Type[] { /*typeof(int)*/ });
				ConstructorInfo conInverse = typeof(InversePropertyAttribute).GetConstructor(new Type[] { typeof(string) });

				PropertyInfo propMemberOrder = typeof(DataMemberAttribute).GetProperty("Order");

				int order = 0;
				foreach (DocAttribute docAttribute in docEntity.Attributes)
				{
					DocObject docRef = null;
					if (docAttribute.DefinedType != null)
					{
						this.m_definitions.TryGetValue(docAttribute.DefinedType, out docRef);
					}

					// exclude derived attributes
					if (String.IsNullOrEmpty(docAttribute.Derived))
					{
						Type typefield = RegisterType(docAttribute.DefinedType);
						if (typefield == null)
							typefield = typeof(object); // excluded from scope

						if (docAttribute.AggregationType != 0)
						{
							if (docAttribute.AggregationAttribute != null)
							{
								// list of list
								switch (docAttribute.AggregationAttribute.GetAggregation())
								{
									case DocAggregationEnum.SET:
										typefield = typeof(ISet<>).MakeGenericType(new Type[] { typefield });
										break;

									case DocAggregationEnum.LIST:
									default:
										typefield = typeof(IList<>).MakeGenericType(new Type[] { typefield });
										break;
								}
							}

							switch (docAttribute.GetAggregation())
							{
								case DocAggregationEnum.SET:
									typefield = typeof(ISet<>).MakeGenericType(new Type[] { typefield });
									break;

								case DocAggregationEnum.LIST:
								default:
									typefield = typeof(IList<>).MakeGenericType(new Type[] { typefield });
									break;
							}
						}
						else if (typefield.IsValueType && docAttribute.IsOptional)
						{
							typefield = typeof(Nullable<>).MakeGenericType(new Type[] { typefield });
						}

						FieldBuilder fb = tb.DefineField("_" + docAttribute.Name, typefield, FieldAttributes.Private);
						mapField.Add(docAttribute.Name, fb);


						PropertyBuilder pb = RegisterProperty(docAttribute.Name, typefield, tb, fb);


						if (String.IsNullOrEmpty(docAttribute.Inverse))
						{
							// direct attributes are fields marked for serialization
							CustomAttributeBuilder cb = new CustomAttributeBuilder(conMember, new object[] { }, new PropertyInfo[] { propMemberOrder }, new object[] { order });
							pb.SetCustomAttribute(cb);
							order++;

							// mark if required
							if (!docAttribute.IsOptional)
							{
								ConstructorInfo conReq = typeof(RequiredAttribute).GetConstructor(new Type[] { });
								CustomAttributeBuilder cabReq = new CustomAttributeBuilder(conReq, new object[] { });
								pb.SetCustomAttribute(cabReq);
							}

						}
						else
						{
							// inverse attributes are fields marked for lookup
							CustomAttributeBuilder cb = new CustomAttributeBuilder(conInverse, new object[] { docAttribute.Inverse });
							pb.SetCustomAttribute(cb);
						}

						// XML
						ConstructorInfo conXSD;
						CustomAttributeBuilder cabXSD;
						if (docAttribute.AggregationAttribute == null && (docRef is DocDefined || docRef is DocEnumeration))
						{
							conXSD = typeof(XmlAttributeAttribute).GetConstructor(new Type[] { });
							cabXSD = new CustomAttributeBuilder(conXSD, new object[] { });
							pb.SetCustomAttribute(cabXSD);
						}
						else
						{
							switch (docAttribute.XsdFormat)
							{
								case DocXsdFormatEnum.Element:
									conXSD = typeof(XmlElementAttribute).GetConstructor(new Type[] { typeof(string) });
									cabXSD = new CustomAttributeBuilder(conXSD, new object[] { docAttribute.DefinedType });
									pb.SetCustomAttribute(cabXSD);
									break;

								case DocXsdFormatEnum.Attribute:
									conXSD = typeof(XmlElementAttribute).GetConstructor(new Type[] { });
									cabXSD = new CustomAttributeBuilder(conXSD, new object[] { });
									pb.SetCustomAttribute(cabXSD);
									break;

								case DocXsdFormatEnum.Hidden:
									conXSD = typeof(XmlIgnoreAttribute).GetConstructor(new Type[] { });
									cabXSD = new CustomAttributeBuilder(conXSD, new object[] { });
									pb.SetCustomAttribute(cabXSD);
									break;
							}
						}

						// documentation
						string fielddisplayname = docAttribute.Name;
						if (displayname != null)
						{
							ConstructorInfo conReq = typeof(DisplayNameAttribute).GetConstructor(new Type[] { typeof(string) });
							CustomAttributeBuilder cabReq = new CustomAttributeBuilder(conReq, new object[] { fielddisplayname });
							pb.SetCustomAttribute(cabReq);
						}

						string fielddescription = docAttribute.Documentation;
						if (description != null)
						{
							ConstructorInfo conReq = typeof(DescriptionAttribute).GetConstructor(new Type[] { typeof(string) });
							CustomAttributeBuilder cabReq = new CustomAttributeBuilder(conReq, new object[] { fielddescription });
							pb.SetCustomAttribute(cabReq);
						}

					}
				}


				// remove from typebuilder
				this.m_types.Remove(strtype);

				type = tb; // avoid circular conditions -- generate type afterwords
			}
			else if (docType is DocSelect)
			{
				attr |= TypeAttributes.Interface | TypeAttributes.Abstract;
				TypeBuilder tb = this.m_module.DefineType(this.m_rootnamespace + schema + "." + docType.Name, attr);

				// interfaces implemented by type (SELECTS)
				foreach (DocDefinition docdef in this.m_definitions.Values)
				{
					if (docdef is DocSelect)
					{
						DocSelect docsel = (DocSelect)docdef;
						foreach (DocSelectItem dsi in docsel.Selects)
						{
							if (strtype.Equals(dsi.Name))
							{
								// register
								Type typeinterface = this.RegisterType(docdef.Name);
								tb.AddInterfaceImplementation(typeinterface);
							}
						}
					}
				}

				type = tb.CreateType();
			}
			else if (docType is DocEnumeration)
			{
				DocEnumeration docEnum = (DocEnumeration)docType;
				EnumBuilder eb = this.m_module.DefineEnum(schema + "." + docType.Name, TypeAttributes.Public, typeof(int));

				for (int i = 0; i < docEnum.Constants.Count; i++)
				{
					DocConstant docConst = docEnum.Constants[i];
					FieldBuilder fb = eb.DefineLiteral(docConst.Name, (int)i);

					foreach (DocLocalization docLocal in docConst.Localization)
					{
						CustomAttributeBuilder cab = docLocal.ToCustomAttributeBuilder();
						if (cab != null)
						{
							fb.SetCustomAttribute(cab);
						}
					}
				}

				type = eb.CreateType();
			}
			else if (docType is DocDefined)
			{
				DocDefined docDef = (DocDefined)docType;
				attr |= TypeAttributes.Sealed;

				if (docDef.DefinedType == docDef.Name)
					return null;

				TypeBuilder tb = this.m_module.DefineType(this.m_rootnamespace + schema + "." + docType.Name, attr, typeof(ValueType));

				// interfaces implemented by type (SELECTS)
				foreach (DocDefinition docdef in this.m_definitions.Values)
				{
					if (docdef is DocSelect)
					{
						DocSelect docsel = (DocSelect)docdef;
						foreach (DocSelectItem dsi in docsel.Selects)
						{
							if (strtype.Equals(dsi.Name))
							{
								// register
								Type typeinterface = RegisterType(docdef.Name);
								tb.AddInterfaceImplementation(typeinterface);
							}
						}
					}
				}

				Type typeliteral = RegisterType(docDef.DefinedType);
				if (typeliteral != null)
				{
					if (docDef.Aggregation != null && docDef.Aggregation.AggregationType != 0)
					{
						switch (docDef.Aggregation.GetAggregation())
						{
							case DocAggregationEnum.SET:
								typeliteral = typeof(ISet<>).MakeGenericType(new Type[] { typeliteral });
								break;

							case DocAggregationEnum.LIST:
							default:
								typeliteral = typeof(IList<>).MakeGenericType(new Type[] { typeliteral });
								break;
						}
					}
					else
					{
#if false // now use direct type -- don't recurse
                        FieldInfo fieldval = typeliteral.GetField("Value");
                        while (fieldval != null)
                        {
                            typeliteral = fieldval.FieldType;
                            fieldval = typeliteral.GetField("Value");
                        }
#endif
					}

					FieldBuilder fieldValue = tb.DefineField("Value", typeliteral, FieldAttributes.Public);

					RegisterProperty("Value", typeliteral, tb, fieldValue);



					type = tb.CreateType();

					Dictionary<string, FieldInfo> mapField = new Dictionary<string, FieldInfo>();
					mapField.Add("Value", fieldValue);
					this.m_fields.Add(type, mapField);
				}
			}

			this.m_types.Add(strtype, type);
			return type;
		}

		private MethodInfo RegisterTemplate(DocTemplateDefinition dtd)
		{
			if (dtd == null || dtd.Rules == null)
				return null;

			MethodInfo methodexist = null;
			if (this.m_templates.TryGetValue(dtd, out methodexist))
				return methodexist;

			Type[] paramtypes = null;
			DocModelRule[] parameters = dtd.GetParameterRules();
			if (parameters != null && parameters.Length > 0)
			{
				paramtypes = new Type[parameters.Length];
				for (int iParam = 0; iParam < parameters.Length; iParam++)
				{
#if false
                    DocModelRule param = parameters[iParam];
                    if (param is DocModelRuleAttribute)
                    {
                        DocDefinition paramtype = dtd.GetParameterType(param.Name, this.m_definitions);
                        if (paramtype != null)
                        {
                            paramtypes[iParam] = RegisterType(paramtype.Name);
                        }
                    }
                    else if(param is DocModelRuleEntity)
                    {
                        paramtypes[iParam] = RegisterType(param.Name);
                    }
#endif

					if (paramtypes[iParam] == null)
					{
						paramtypes[iParam] = typeof(string); // fallback
					}
				}
			}

			if (dtd.Type == null)
				return null;

			TypeBuilder tb = (System.Reflection.Emit.TypeBuilder)this.RegisterType(dtd.Type);
			if (tb == null)
				return null;

			string methodname;
			if (dtd.Name != null)
			{
				methodname = dtd.Name;
			}
			else
			{
				methodname = dtd.UniqueId;
			}
			methodname = methodname.Replace(' ', '_').Replace(':', '_').Replace('-', '_');
			MethodBuilder method = tb.DefineMethod(methodname, MethodAttributes.Public, CallingConventions.HasThis, typeof(bool), paramtypes);
			ILGenerator generator = method.GetILGenerator();
			foreach (DocModelRule docRule in dtd.Rules)
			{
				docRule.EmitInstructions(this, generator, dtd);
			}

			// if made it to the end, then successful, so return true
			generator.Emit(OpCodes.Ldc_I4_1);
			generator.Emit(OpCodes.Ret);

			return method;
		}
	}
}
