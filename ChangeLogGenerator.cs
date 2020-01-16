using System;
using System.Collections.Generic;
using System.Text;

using IfcDoc.Schema.DOC;

namespace IfcDoc
{
	public class ChangeLogGenerator
	{
		/// <summary>
		/// Creates change log entries in current project based on a previous project.
		/// </summary>
		/// <param name="projectPrev">The previous project for which definitions are compared.</param>
		/// <param name="docProject">The current project where change log entries are generated.</param>
		/// <param name="docPublication">The publication where to record change logs</param>
		public static void Generate(DocProject projectPrev, DocProject docProject, DocPublication docPublication)
		{
			List<DocChangeSet> listChangeSets = docProject.ChangeSets;
			if (docPublication != null)
			{
				listChangeSets = docPublication.ChangeSets;
			}

			// copy over any previous change logs
			foreach (DocChangeSet prevCS in projectPrev.ChangeSets)
			{
				DocChangeSet currCS = new DocChangeSet();
				currCS.Name = prevCS.Name;
				currCS.Documentation = prevCS.Documentation;
				currCS.VersionBaseline = prevCS.VersionBaseline;
				currCS.VersionCompared = prevCS.VersionCompared;
				listChangeSets.Add(currCS);

				foreach (DocChangeAction prevCA in prevCS.ChangesEntities)
				{
					DocChangeAction currCA = prevCA.Copy();
					currCS.ChangesEntities.Add(currCA);
				}

				if (prevCS.ChangesProperties != null)
				{
					foreach (DocChangeAction prevCA in prevCS.ChangesProperties)
					{
						DocChangeAction currCA = prevCA.Copy();
						currCS.ChangesProperties.Add(currCA);
					}
				}

				if (prevCS.ChangesQuantities != null)
				{
					foreach (DocChangeAction prevCA in prevCS.ChangesQuantities)
					{
						DocChangeAction currCA = prevCA.Copy();
						currCS.ChangesQuantities.Add(currCA);
					}
				}
			}

			// build maps            
			Dictionary<string, DocObject> mapNew = new Dictionary<string, DocObject>();
			foreach (DocSection docSection in docProject.Sections)
			{
				foreach (DocSchema docSchema in docSection.Schemas)
				{
					foreach (DocEntity docEntity in docSchema.Entities)
					{
						mapNew.Add(docEntity.Name, docEntity);
					}
					foreach (DocType docType in docSchema.Types)
					{
						mapNew.Add(docType.Name, docType);
					}
				}
			}
			Dictionary<string, DocObject> mapOld = new Dictionary<string, DocObject>();
			foreach (DocSection docSection in projectPrev.Sections)
			{
				foreach (DocSchema docSchema in docSection.Schemas)
				{
					foreach (DocEntity docEntity in docSchema.Entities)
					{
						mapOld.Add(docEntity.Name, docEntity);
					}
					foreach (DocType docType in docSchema.Types)
					{
						mapOld.Add(docType.Name, docType);
					}
				}
			}

			DocChangeSet docChangeSet = new DocChangeSet();
			listChangeSets.Add(docChangeSet);
			docChangeSet.Name = projectPrev.GetSchemaIdentifier();
			docChangeSet.VersionBaseline = projectPrev.Sections[0].Version;

			// property enums
			foreach (DocPropertyEnumeration docPset in docProject.PropertyEnumerations)
			{
				DocChangeAction docChangePset = new DocChangeAction();
				docChangeSet.ChangesProperties.Add(docChangePset);
				docChangePset.Name = docPset.Name;

				// find equivalent pset
				DocPropertyEnumeration docPsetBase = null;
				foreach (DocPropertyEnumeration docEntityEach in projectPrev.PropertyEnumerations)
				{
					if (docEntityEach.Name.Equals(docPset.Name))
					{
						docPsetBase = docEntityEach;
						break;
					}
				}

				if (docPsetBase == null)
				{
					// new entity
					docChangePset.Action = DocChangeActionEnum.ADDED;
				}

				if (docPsetBase != null)
				{
					foreach (DocPropertyConstant docAttribute in docPset.Constants)
					{
						DocChangeAction docChangeAttribute = new DocChangeAction();
						docChangePset.Changes.Add(docChangeAttribute);
						docChangeAttribute.Name = docAttribute.Name;

						DocPropertyConstant docAttributeBase = null;
						foreach (DocPropertyConstant docEachProperty in docPsetBase.Constants)
						{
							if (docEachProperty.Name.Equals(docAttribute.Name))
							{
								docAttributeBase = docEachProperty;
								break;
							}
						}

						if (docAttributeBase == null)
						{
							// new constant added
							docChangeAttribute.Action = DocChangeActionEnum.ADDED;
						}
						else
						{
							// compare for changes -- no state captured for constants
						}
					}

					// report deleted properties
					foreach (DocPropertyConstant docAttributeBase in docPsetBase.Constants)
					{
						DocPropertyConstant docAttribute = null;
						foreach (DocPropertyConstant docEachProperty in docPset.Constants)
						{
							if (docEachProperty.Name.Equals(docAttributeBase.Name))
							{
								docAttribute = docEachProperty;
								break;
							}
						}

						if (docAttribute == null)
						{
							DocChangeAction docChangeAttribute = new DocChangeAction();
							docChangePset.Changes.Add(docChangeAttribute);
							docChangeAttribute.Name = docAttributeBase.Name;
							docChangeAttribute.Action = DocChangeActionEnum.DELETED;
						}
					}
				}

			}


			// now find deleted property enums
			foreach (DocPropertyEnumeration docEntityBase in projectPrev.PropertyEnumerations)
			{
				// find equivalent
				DocPropertyEnumeration docEntity = null;
				foreach (DocPropertyEnumeration docEntityEach in docProject.PropertyEnumerations)
				{
					if (docEntityEach.Name.Equals(docEntityBase.Name))
					{
						docEntity = docEntityEach;
						break;
					}
				}

				if (docEntity == null)
				{
					DocChangeAction docChangeEntity = new DocChangeAction();
					docChangeSet.ChangesProperties.Add(docChangeEntity);
					docChangeEntity.Name = docEntityBase.Name;

					docChangeEntity.Action = DocChangeActionEnum.DELETED;
				}
			}
			// end property enums

			// iterate through each schema (new and old)
			for (int iSection = 4; iSection < 8; iSection++)
			{
				DocSection docSection = docProject.Sections[iSection];
				DocSection docSectionBase = projectPrev.Sections[iSection];

				DocChangeAction docChangeSection = new DocChangeAction();
				docChangeSet.ChangesEntities.Add(docChangeSection);
				docChangeSection.Name = docSection.Name;

				DocChangeAction docChangeSectionProperties = new DocChangeAction();
				docChangeSet.ChangesProperties.Add(docChangeSectionProperties);
				docChangeSectionProperties.Name = docSection.Name;

				DocChangeAction docChangeSectionQuantities = new DocChangeAction();
				docChangeSet.ChangesQuantities.Add(docChangeSectionQuantities);
				docChangeSectionQuantities.Name = docSection.Name;

				foreach (DocSchema docSchema in docSection.Schemas)
				{
					// find equivalent schema
					DocSchema docSchemaBase = null;
					foreach (DocSchema docSchemaEach in docSectionBase.Schemas)
					{
						if (docSchemaEach.Name.Equals(docSchema.Name))
						{
							docSchemaBase = docSchemaEach;
							break;
						}
					}

					DocChangeAction docChangeSchema = new DocChangeAction();
					docChangeSection.Changes.Add(docChangeSchema);
					docChangeSchema.Name = docSchema.Name;

					DocChangeAction docChangeSchemaProperties = new DocChangeAction();
					docChangeSectionProperties.Changes.Add(docChangeSchemaProperties);
					docChangeSchemaProperties.Name = docSchema.Name;

					DocChangeAction docChangeSchemaQuantities = new DocChangeAction();
					docChangeSectionQuantities.Changes.Add(docChangeSchemaQuantities);
					docChangeSchemaQuantities.Name = docSchema.Name;

					if (docSchemaBase == null)
					{
						// new schema
						docChangeSchema.Action = DocChangeActionEnum.ADDED;
					}
					else
					{
						// existing schema


						// compare types
						foreach (DocType docType in docSchema.Types)
						{
							DocChangeAction docChangeType = new DocChangeAction();
							docChangeSchema.Changes.Add(docChangeType);
							docChangeType.Name = docType.Name;

							// find equivalent type
							DocType docTypeBase = null;
							foreach (DocType docTypeEach in docSchemaBase.Types)
							{
								if (docTypeEach.Name.Equals(docType.Name))
								{
									docTypeBase = docTypeEach;
									break;
								}
							}

							if (docTypeBase == null)
							{
								// new type
								docChangeType.Action = DocChangeActionEnum.ADDED;

								// check if it was moved from another schema                                
								foreach (DocSection docOtherSection in projectPrev.Sections)
								{
									foreach (DocSchema docOtherSchema in docOtherSection.Schemas)
									{
										foreach (DocType docOtherType in docOtherSchema.Types)
										{
											if (docOtherType.Name.Equals(docType.Name))
											{
												docChangeType.Action = DocChangeActionEnum.MOVED;
												docChangeType.Aspects.Add(new DocChangeAspect(DocChangeAspectEnum.SCHEMA, docOtherSchema.Name.ToUpper(), docSchema.Name.ToUpper()));
												docTypeBase = docOtherType;
												break;
											}
										}
									}
								}
							}

							if (docTypeBase != null)
							{
								// existing type

								CheckObjectChanges(docChangeType, docTypeBase, docType);

								// if enumeration, check enums
								if (docType is DocEnumeration)
								{
									DocEnumeration docEnumeration = (DocEnumeration)docType;
									DocEnumeration docEnumerationBase = (DocEnumeration)docTypeBase;

									// find constants added
									foreach (DocConstant docConstant in docEnumeration.Constants)
									{
										// find equivalent constant
										DocConstant docConstantBase = null;
										foreach (DocConstant docConstantEach in docEnumerationBase.Constants)
										{
											if (docConstantEach.Name.Equals(docConstant.Name))
											{
												docConstantBase = docConstantEach;
												break;
											}
										}

										// for constants, only generate additions or deletions for brevity
										if (docConstantBase == null)
										{
											// new entity
											DocChangeAction docChangeConstant = new DocChangeAction();
											docChangeType.Changes.Add(docChangeConstant);
											docChangeConstant.Name = docConstant.Name;
											docChangeConstant.Action = DocChangeActionEnum.ADDED;
										}
									}

									// find constants removed
									foreach (DocConstant docConstantBase in docEnumerationBase.Constants)
									{
										// find equivalent constant
										DocConstant docConstant = null;
										foreach (DocConstant docConstantEach in docEnumeration.Constants)
										{
											if (docConstantEach.Name.Equals(docConstantBase.Name))
											{
												docConstant = docConstantEach;
												break;
											}
										}

										// for constants, only generate additions or deletions for brevity
										if (docConstant == null)
										{
											// deleted
											DocChangeAction docChangeConstant = new DocChangeAction();
											docChangeType.Changes.Add(docChangeConstant);
											docChangeConstant.Name = docConstantBase.Name;
											docChangeConstant.Action = DocChangeActionEnum.DELETED;
											docChangeConstant.ImpactSPF = true;
											docChangeConstant.ImpactXML = true;
										}
									}

								}
								else if (docType is DocSelect)
								{
									DocSelect docEnumeration = (DocSelect)docType;
									DocSelect docEnumerationBase = (DocSelect)docTypeBase;

									// find select items added
									foreach (DocSelectItem docConstant in docEnumeration.Selects)
									{
										// find equivalent constant
										DocSelectItem docConstantBase = null;
										foreach (DocSelectItem docConstantEach in docEnumerationBase.Selects)
										{
											if (docConstantEach.Name.Equals(docConstant.Name))
											{
												docConstantBase = docConstantEach;
												break;
											}
										}

										// for constants, only generate additions or deletions for brevity
										if (docConstantBase == null)
										{
											// new entity
											DocChangeAction docChangeConstant = new DocChangeAction();
											docChangeType.Changes.Add(docChangeConstant);
											docChangeConstant.Name = docConstant.Name;
											docChangeConstant.Action = DocChangeActionEnum.ADDED;
										}
									}

									// find select items removed
									foreach (DocSelectItem docConstantBase in docEnumerationBase.Selects)
									{
										// find equivalent constant
										DocSelectItem docConstant = null;
										foreach (DocSelectItem docConstantEach in docEnumeration.Selects)
										{
											if (docConstantEach.Name.Equals(docConstantBase.Name))
											{
												docConstant = docConstantEach;
												break;
											}
										}

										// for selects, only generate additions or deletions for brevity
										if (docConstant == null)
										{
											// deleted select
											DocChangeAction docChangeConstant = new DocChangeAction();
											docChangeType.Changes.Add(docChangeConstant);
											docChangeConstant.Name = docConstantBase.Name;
											docChangeConstant.Action = DocChangeActionEnum.DELETED;

											// if a supertype of the deleted select has been added, then it's compatible (e.g. IfcMetricValueSelect: +IfcValue, -IfcText)                                            
											if (!SelectIncludes(docEnumeration, docConstantBase.Name, mapNew))
											{
												docChangeConstant.ImpactSPF = true;
												docChangeConstant.ImpactXML = true;
											}
										}
									}
								}
							}
						}


						// compare entities
						foreach (DocEntity docEntity in docSchema.Entities)
						{
							DocChangeAction docChangeEntity = new DocChangeAction();
							docChangeSchema.Changes.Add(docChangeEntity);
							docChangeEntity.Name = docEntity.Name;

							// find equivalent entity
							DocEntity docEntityBase = null;
							foreach (DocEntity docEntityEach in docSchemaBase.Entities)
							{
								if (docEntityEach.Name.Equals(docEntity.Name))
								{
									docEntityBase = docEntityEach;
									break;
								}
							}

							if (docEntityBase == null)
							{
								// new entity
								docChangeEntity.Action = DocChangeActionEnum.ADDED;

								// check if it was moved from another schema                                
								foreach (DocSection docOtherSection in projectPrev.Sections)
								{
									foreach (DocSchema docOtherSchema in docOtherSection.Schemas)
									{
										foreach (DocEntity docOtherEntity in docOtherSchema.Entities)
										{
											if (docOtherEntity.Name.Equals(docEntity.Name))
											{
												docEntityBase = docOtherEntity; // still compare attributes if moved (e.g. IfcRelSequence)

												docChangeEntity.Action = DocChangeActionEnum.MOVED;
												docChangeEntity.Aspects.Add(new DocChangeAspect(DocChangeAspectEnum.SCHEMA, docOtherSchema.Name.ToUpper(), docSchema.Name.ToUpper()));
												break;
											}
										}
									}
								}

							}

							if (docEntityBase != null)
							{
								// existing entity

								// compare abstract vs. non-abstract
								if (docEntity.IsAbstract != docEntityBase.IsAbstract)
								{
									docChangeEntity.Action = DocChangeActionEnum.MODIFIED;

									if (docEntityBase.IsAbstract)
									{
										docChangeEntity.Aspects.Add(new DocChangeAspect(DocChangeAspectEnum.INSTANTIATION, "ABSTRACT", null));
									}
									else
									{
										docChangeEntity.Aspects.Add(new DocChangeAspect(DocChangeAspectEnum.INSTANTIATION, null, "ABSTRACT"));
									}
								}

								CheckObjectChanges(docChangeEntity, docEntityBase, docEntity);

								// compare attributes by index

								// only report non-abstract entities; e.g. attributes may be demoted without file impact
								if (!docEntity.IsAbstract)
								{
									List<DocAttribute> listAttributeNew = new List<DocAttribute>();
									List<DocAttribute> listAttributeOld = new List<DocAttribute>();
									BuildAttributeListDirect(listAttributeNew, mapNew, docEntity);
									BuildAttributeListDirect(listAttributeOld, mapOld, docEntityBase);

									for (int iAttribute = 0; iAttribute < listAttributeNew.Count; iAttribute++)
									{
										DocAttribute docAttribute = listAttributeNew[iAttribute];

										// we only care about direct attributes
										DocChangeAction docChangeAttribute = new DocChangeAction();
										docChangeEntity.Changes.Add(docChangeAttribute);
										docChangeAttribute.Name = docAttribute.Name;

										if (iAttribute >= listAttributeOld.Count)
										{
											// new attribute added
											docChangeAttribute.Action = DocChangeActionEnum.ADDED;
										}
										else
										{
											DocAttribute docAttributeBase = listAttributeOld[iAttribute];

											// compare for changes
											if (!docAttribute.Name.Equals(docAttributeBase.Name))
											{
												docChangeAttribute.Action = DocChangeActionEnum.MODIFIED;
												docChangeAttribute.Aspects.Add(new DocChangeAspect(DocChangeAspectEnum.NAME, docAttributeBase.Name, docAttribute.Name));

												docChangeAttribute.ImpactXML = true; // no impact to SPF though
											}

											CheckObjectChanges(docChangeAttribute, docAttributeBase, docAttribute);

											if (!docAttribute.DefinedType.Equals(docAttributeBase.DefinedType))
											{
												DocChangeAspect docAspect = new DocChangeAspect(DocChangeAspectEnum.TYPE, docAttributeBase.DefinedType, docAttribute.DefinedType);
												docChangeAttribute.Aspects.Add(docAspect);
												docChangeAttribute.Action = DocChangeActionEnum.MODIFIED;

												// check for compatibility
												// assume incompatible unless we can prove types are compatible
												bool impact = true;

												// ok if new type is a supertype of the old type
												DocObject docNew = null;
												if (mapNew.TryGetValue(docAspect.NewValue, out docNew))
												{
													DocObject docOld = null;
													if (mapOld.TryGetValue(docAspect.OldValue, out docOld))
													{
														if (docNew is DocEntity)
														{
															DocEntity docNewEnt = (DocEntity)docNew;

															if (docOld is DocEntity)
															{
																DocEntity docOldEnt = (DocEntity)docOld;

																while (docNewEnt != null)
																{
																	if (docNewEnt.Name.Equals(docOldEnt.Name))
																	{
																		impact = false; // subtype
																	}

																	if (docNewEnt.BaseDefinition != null)
																	{
																		docNewEnt = mapNew[docNewEnt.BaseDefinition] as DocEntity;
																	}
																	else
																	{
																		docNewEnt = null;
																	}
																}
															}
														}
														else if (docNew is DocSelect)
														{
															DocSelect docNewSelect = (DocSelect)docNew;
															foreach (DocSelectItem docNewSelectItem in docNewSelect.Selects)
															{
																if (docNewSelectItem.Name.Equals(docOld.Name))
																{
																	impact = false; // included in select
																}
															}
														}
														else if (docNew is DocEnumeration)
														{
															// ok if new enumeration contains all of old enumerations (e.g. IfcInternalOrExternalEnum -> IfcSpaceTypeEnum)                                                            
															DocEnumeration docNewEnum = (DocEnumeration)docNew;
															if (docOld is DocEnumeration)
															{
																impact = false;

																DocEnumeration docOldEnum = (DocEnumeration)docOld;
																foreach (DocConstant docOldConstant in docOldEnum.Constants)
																{
																	bool match = false;
																	foreach (DocConstant docNewConstant in docNewEnum.Constants)
																	{
																		if (docOldConstant.Name.Equals(docNewConstant.Name))
																		{
																			match = true;
																			break;
																		}
																	}

																	if (!match)
																	{
																		impact = true;
																		break;
																	}
																}
															}
														}
														else if (docNew is DocDefined)
														{
															DocDefined docNewDefined = (DocDefined)docNew;

															// compare underlying types
															if (docOld is DocDefined)
															{
																DocDefined docOldDefined = (DocDefined)docOld;

																if (docNewDefined.DefinedType.Equals(docOldDefined.DefinedType))
																{
																	// e.g. IfcLabel -> IfcIdentifier
																	impact = false;
																}
															}
														}
													}
												}

												if (docNew is DocDefined && ((DocDefined)docNew).DefinedType == docAspect.OldValue)
												{
													impact = false;
												}

												//todo: add generic check that traverses multiple levels of defined types; in the interest of hitting deadline, hard-coded hack for now
												if (docNew != null && docNew.Name == "IfcPositiveInteger" && docAspect.OldValue == "INTEGER")
												{
													impact = false;
												}
												if (docNew != null && docNew.Name == "IfcBinary" && docAspect.OldValue == "BINARY (32)")
												{
													impact = false;
												}

												if (impact)
												{
													impact.ToString();
												}

												docChangeAttribute.ImpactSPF = impact;
												docChangeAttribute.ImpactXML = impact;
											}

											if (docAttribute.AttributeFlags != docAttributeBase.AttributeFlags)
											{
												docChangeAttribute.Action = DocChangeActionEnum.MODIFIED;

												if ((docAttributeBase.AttributeFlags & 1) != 0)
												{
													docChangeAttribute.Aspects.Add(new DocChangeAspect(DocChangeAspectEnum.INSTANTIATION, "OPTIONAL", null));
												}
												else
												{
													docChangeAttribute.Aspects.Add(new DocChangeAspect(DocChangeAspectEnum.INSTANTIATION, null, "OPTIONAL"));
												}
											}

											if (docAttribute.AggregationType != docAttributeBase.AggregationType)
											{
												docChangeAttribute.Action = DocChangeActionEnum.MODIFIED;
												docChangeAttribute.Aspects.Add(new DocChangeAspect(DocChangeAspectEnum.AGGREGATION, docAttributeBase.GetAggregation().ToString(), docAttribute.GetAggregation().ToString()));
											}

											if (docAttribute.XsdFormat != docAttributeBase.XsdFormat)
											{
												docChangeAttribute.Action = DocChangeActionEnum.MODIFIED;
												docChangeAttribute.Aspects.Add(new DocChangeAspect(DocChangeAspectEnum.XSDFORMAT, docAttributeBase.XsdFormat.ToString(), docAttribute.XsdFormat.ToString()));
												docChangeAttribute.ImpactXML = true;
											}
											if (docAttribute.XsdTagless.GetValueOrDefault() != docAttributeBase.XsdTagless.GetValueOrDefault())
											{
												docChangeAttribute.Action = DocChangeActionEnum.MODIFIED;
												docChangeAttribute.Aspects.Add(new DocChangeAspect(DocChangeAspectEnum.XSDTAGLESS, docAttributeBase.XsdTagless.GetValueOrDefault().ToString(), docAttribute.XsdTagless.GetValueOrDefault().ToString()));
												docChangeAttribute.ImpactXML = true;
											}

										}
									}

									// report deleted attributes
									for (int iAttribute = listAttributeNew.Count; iAttribute < listAttributeOld.Count; iAttribute++)
									{
										DocAttribute docAttributeBase = listAttributeOld[iAttribute];

										DocChangeAction docChangeAttribute = new DocChangeAction();
										docChangeEntity.Changes.Add(docChangeAttribute);
										docChangeAttribute.Name = docAttributeBase.Name;
										docChangeAttribute.Action = DocChangeActionEnum.DELETED;
										docChangeAttribute.ImpactSPF = true;

										// deleted attributes don't affect XML
									}



									// now inverse - order doesn't matter - compare by name

									listAttributeNew.Clear();
									listAttributeOld.Clear();
									BuildAttributeListInverse(listAttributeNew, mapNew, docEntity);
									BuildAttributeListInverse(listAttributeOld, mapOld, docEntityBase);

									for (int iAttribute = 0; iAttribute < listAttributeNew.Count; iAttribute++)
									{
										DocAttribute docAttribute = listAttributeNew[iAttribute];

										DocChangeAction docChangeAttribute = new DocChangeAction();
										docChangeEntity.Changes.Add(docChangeAttribute);
										docChangeAttribute.Name = docAttribute.Name;
										docChangeAttribute.Status = "INVERSE";

										DocAttribute docAttributeBase = null;
										foreach (DocAttribute docAttrOld in listAttributeOld)
										{
											if (docAttrOld.Name == docAttribute.Name)
											{
												docAttributeBase = docAttrOld;
												break;
											}
										}

										if (docAttributeBase == null)
										{
											// new attribute added
											docChangeAttribute.Action = DocChangeActionEnum.ADDED;
										}
										else
										{
											if (!docAttribute.DefinedType.Equals(docAttributeBase.DefinedType))
											{
												DocChangeAspect docAspect = new DocChangeAspect(DocChangeAspectEnum.TYPE, docAttributeBase.DefinedType, docAttribute.DefinedType);
												docChangeAttribute.Aspects.Add(docAspect);
												docChangeAttribute.Action = DocChangeActionEnum.MODIFIED;
												docChangeAttribute.ImpactSPF = false;
												docChangeAttribute.ImpactXML = false;
											}

											if (docAttribute.AggregationType != docAttributeBase.AggregationType)
											{
												docChangeAttribute.Action = DocChangeActionEnum.MODIFIED;
												docChangeAttribute.Aspects.Add(new DocChangeAspect(DocChangeAspectEnum.AGGREGATION, docAttributeBase.GetAggregation().ToString(), docAttribute.GetAggregation().ToString()));
											}
										}
									}

									// report deleted attributes
									for (int iAttribute = 0; iAttribute < listAttributeOld.Count; iAttribute++)
									{
										DocAttribute docAttributeBase = listAttributeOld[iAttribute];

										DocAttribute docAttributeCurr = null;
										foreach (DocAttribute docAttrNew in listAttributeNew)
										{
											if (docAttrNew.Name == docAttributeBase.Name)
											{
												docAttributeCurr = docAttrNew;
												break;
											}
										}

										if (docAttributeCurr == null)
										{
											DocChangeAction docChangeAttribute = new DocChangeAction();
											docChangeEntity.Changes.Add(docChangeAttribute);
											docChangeAttribute.Name = docAttributeBase.Name;
											docChangeAttribute.Action = DocChangeActionEnum.DELETED;
											docChangeAttribute.Status = "INVERSE";
										}
									}

								}
							}
						}

						// now find deleted entities
						foreach (DocEntity docEntityBase in docSchemaBase.Entities)
						{
							// find equivalent
							DocEntity docEntity = null;
							foreach (DocEntity docEntityEach in docSchema.Entities)
							{
								if (docEntityEach.Name.Equals(docEntityBase.Name))
								{
									docEntity = docEntityEach;
									break;
								}
							}

							if (docEntity == null)
							{
								// entity may have moved to other schema; check other schemas
								DocSchema docThatSchema = null;
								foreach (DocSection docOtherSection in docProject.Sections)
								{
									foreach (DocSchema docOtherSchema in docOtherSection.Schemas)
									{
										foreach (DocEntity docOtherEntity in docOtherSchema.Entities)
										{
											if (docOtherEntity.Name.Equals(docEntityBase.Name))
											{
												docEntity = docOtherEntity;
												docThatSchema = docOtherSchema;
											}
										}
									}
								}

								DocChangeAction docChangeEntity = new DocChangeAction();
								docChangeSchema.Changes.Add(docChangeEntity);
								docChangeEntity.Name = docEntityBase.Name;

								if (docEntity != null)
								{
									// moved from another schema
									docChangeEntity.Action = DocChangeActionEnum.MOVED;
									docChangeEntity.Aspects.Add(new DocChangeAspect(DocChangeAspectEnum.SCHEMA, docSchema.Name.ToUpper(), docThatSchema.Name.ToUpper()));
								}
								else
								{
									// otherwise, deleted
									docChangeEntity.Action = DocChangeActionEnum.DELETED;

									// if non-abstract, it impacts file
									if (!docEntityBase.IsAbstract)
									{
										docChangeEntity.ImpactSPF = true;
										docChangeEntity.ImpactXML = true;
									}
								}
							}
						}

						// property sets
						foreach (DocPropertySet docPset in docSchema.PropertySets)
						{
							if (docPset.IsVisible())
							{
								DocChangeAction docChangePset = new DocChangeAction();
								docChangeSchemaProperties.Changes.Add(docChangePset);
								docChangePset.Name = docPset.Name;

								// find equivalent pset
								DocPropertySet docPsetBase = null;
								foreach (DocPropertySet docEntityEach in docSchemaBase.PropertySets)
								{
									if (docEntityEach.Name.Equals(docPset.Name))
									{
										docPsetBase = docEntityEach;
										break;
									}
								}

								if (docPsetBase == null)
								{
									// new entity
									docChangePset.Action = DocChangeActionEnum.ADDED;

									// check if it was moved from another schema                                
									foreach (DocSection docOtherSection in projectPrev.Sections)
									{
										foreach (DocSchema docOtherSchema in docOtherSection.Schemas)
										{
											foreach (DocPropertySet docOtherPset in docOtherSchema.PropertySets)
											{
												if (docOtherPset.Name.Equals(docPset.Name))
												{
													docPsetBase = docOtherPset; // still compare attributes if moved (e.g. IfcRelSequence)

													docChangePset.Action = DocChangeActionEnum.MOVED;
													docChangePset.Aspects.Add(new DocChangeAspect(DocChangeAspectEnum.SCHEMA, docOtherSchema.Name.ToUpper(), docSchema.Name.ToUpper()));
												}
											}
										}
									}

								}

								if (docPsetBase != null)
								{
									// existing entity

									// compare abstract vs. non-abstract
									if (docPset.ApplicableType != docPsetBase.ApplicableType)
									{
										docChangePset.Action = DocChangeActionEnum.MODIFIED;
										docChangePset.Aspects.Add(new DocChangeAspect(DocChangeAspectEnum.INSTANTIATION, docPsetBase.ApplicableType, docPset.ApplicableType));
									}

									// compare attributes by index

									// only report non-abstract entities; e.g. attributes may be demoted without file impact


									foreach (DocProperty docAttribute in docPset.Properties)
									{
										// we only care about direct attributes
										DocChangeAction docChangeAttribute = new DocChangeAction();
										docChangePset.Changes.Add(docChangeAttribute);
										docChangeAttribute.Name = docAttribute.Name;

										DocProperty docAttributeBase = null;
										foreach (DocProperty docEachProperty in docPsetBase.Properties)
										{
											if (docEachProperty.Name.Equals(docAttribute.Name))
											{
												docAttributeBase = docEachProperty;
												break;
											}
										}

										if (docAttributeBase == null)
										{
											// new attribute added
											docChangeAttribute.Action = DocChangeActionEnum.ADDED;
										}
										else
										{
											// compare for changes
											if (!docAttribute.PropertyType.Equals(docAttributeBase.PropertyType))
											{
												DocChangeAspect docAspect = new DocChangeAspect(DocChangeAspectEnum.INSTANTIATION, docAttributeBase.PropertyType.ToString(), docAttribute.PropertyType.ToString());
												docChangeAttribute.Aspects.Add(docAspect);
												docChangeAttribute.Action = DocChangeActionEnum.MODIFIED;
											}

											if (docAttribute.PrimaryDataType != null && docAttributeBase.PrimaryDataType != null &&
												!docAttribute.PrimaryDataType.Trim().Equals(docAttributeBase.PrimaryDataType.Trim()))
											{
												DocChangeAspect docAspect = new DocChangeAspect(DocChangeAspectEnum.TYPE, docAttributeBase.PrimaryDataType, docAttribute.PrimaryDataType);
												docChangeAttribute.Aspects.Add(docAspect);
												docChangeAttribute.Action = DocChangeActionEnum.MODIFIED;
											}
										}
									}

									// report deleted properties
									foreach (DocProperty docAttributeBase in docPsetBase.Properties)
									{
										DocProperty docAttribute = null;
										foreach (DocProperty docEachProperty in docPset.Properties)
										{
											if (docEachProperty.Name.Equals(docAttributeBase.Name))
											{
												docAttribute = docEachProperty;
												break;
											}
										}

										if (docAttribute == null)
										{
											DocChangeAction docChangeAttribute = new DocChangeAction();
											docChangePset.Changes.Add(docChangeAttribute);
											docChangeAttribute.Name = docAttributeBase.Name;
											docChangeAttribute.Action = DocChangeActionEnum.DELETED;
										}
									}
								}
							}
						}

						// now find deleted psets
						foreach (DocPropertySet docEntityBase in docSchemaBase.PropertySets)
						{
							// find equivalent
							DocPropertySet docEntity = null;
							foreach (DocPropertySet docEntityEach in docSchema.PropertySets)
							{
								if (docEntityEach.Name.Equals(docEntityBase.Name))
								{
									docEntity = docEntityEach;
									break;
								}
							}

							if (docEntity == null)
							{
								// entity may have moved to other schema; check other schemas
								DocSchema docThatSchema = null;
								foreach (DocSection docOtherSection in docProject.Sections)
								{
									foreach (DocSchema docOtherSchema in docOtherSection.Schemas)
									{
										foreach (DocPropertySet docOtherEntity in docOtherSchema.PropertySets)
										{
											if (docOtherEntity.Name.Equals(docEntityBase.Name))
											{
												docEntity = docOtherEntity;
												docThatSchema = docOtherSchema;
											}
										}
									}
								}

								DocChangeAction docChangeEntity = new DocChangeAction();
								docChangeSchemaProperties.Changes.Add(docChangeEntity);
								docChangeEntity.Name = docEntityBase.Name;

								if (docEntity != null)
								{
									// moved from another schema
									docChangeEntity.Action = DocChangeActionEnum.MOVED;
									docChangeEntity.Aspects.Add(new DocChangeAspect(DocChangeAspectEnum.SCHEMA, docSchema.Name.ToUpper(), docThatSchema.Name.ToUpper()));
								}
								else
								{
									// otherwise, deleted
									docChangeEntity.Action = DocChangeActionEnum.DELETED;
								}
							}
						}
						// end property sets


						// quantity sets
						foreach (DocQuantitySet docQset in docSchema.QuantitySets)
						{
							DocChangeAction docChangeQset = new DocChangeAction();
							docChangeSchemaQuantities.Changes.Add(docChangeQset);
							docChangeQset.Name = docQset.Name;

							// find equivalent pset
							DocQuantitySet docQsetBase = null;
							foreach (DocQuantitySet docEntityEach in docSchemaBase.QuantitySets)
							{
								if (docEntityEach.Name.Equals(docQset.Name))
								{
									docQsetBase = docEntityEach;
									break;
								}
							}

							if (docQsetBase == null)
							{
								// new entity
								docChangeQset.Action = DocChangeActionEnum.ADDED;

								// check if it was moved from another schema                                
								foreach (DocSection docOtherSection in projectPrev.Sections)
								{
									foreach (DocSchema docOtherSchema in docOtherSection.Schemas)
									{
										foreach (DocQuantitySet docOtherQset in docOtherSchema.QuantitySets)
										{
											if (docOtherQset.Name.Equals(docQset.Name))
											{
												docQsetBase = docOtherQset; // still compare attributes if moved (e.g. IfcRelSequence)

												docChangeQset.Action = DocChangeActionEnum.MOVED;
												docChangeQset.Aspects.Add(new DocChangeAspect(DocChangeAspectEnum.SCHEMA, docOtherSchema.Name.ToUpper(), docSchema.Name.ToUpper()));
											}
										}
									}
								}

							}

							if (docQsetBase != null)
							{
								// existing entity

								// compare abstract vs. non-abstract
								if (docQset.ApplicableType != docQsetBase.ApplicableType)
								{
									docChangeQset.Action = DocChangeActionEnum.MODIFIED;
									docChangeQset.Aspects.Add(new DocChangeAspect(DocChangeAspectEnum.INSTANTIATION, docQsetBase.ApplicableType, docQset.ApplicableType));
								}

								// compare attributes by index

								// only report non-abstract entities; e.g. attributes may be demoted without file impact


								foreach (DocQuantity docAttribute in docQset.Quantities)
								{
									// we only care about direct attributes
									DocChangeAction docChangeAttribute = new DocChangeAction();
									docChangeQset.Changes.Add(docChangeAttribute);
									docChangeAttribute.Name = docAttribute.Name;

									DocQuantity docAttributeBase = null;
									foreach (DocQuantity docEachProperty in docQsetBase.Quantities)
									{
										if (docEachProperty.Name.Equals(docAttribute.Name))
										{
											docAttributeBase = docEachProperty;
											break;
										}
									}

									if (docAttributeBase == null)
									{
										// new attribute added
										docChangeAttribute.Action = DocChangeActionEnum.ADDED;
									}
									else
									{
										// compare for changes
										if (!docAttribute.QuantityType.Equals(docAttributeBase.QuantityType))
										{
											DocChangeAspect docAspect = new DocChangeAspect(DocChangeAspectEnum.INSTANTIATION, docAttributeBase.QuantityType.ToString(), docAttribute.QuantityType.ToString());
											docChangeAttribute.Aspects.Add(docAspect);
											docChangeAttribute.Action = DocChangeActionEnum.MODIFIED;
										}
									}
								}

								// report deleted quantities
								foreach (DocQuantity docAttributeBase in docQsetBase.Quantities)
								{
									DocQuantity docAttribute = null;
									foreach (DocQuantity docEachQuantity in docQset.Quantities)
									{
										if (docEachQuantity.Name.Equals(docAttributeBase.Name))
										{
											docAttribute = docEachQuantity;
											break;
										}
									}

									if (docAttribute == null)
									{
										DocChangeAction docChangeAttribute = new DocChangeAction();
										docChangeQset.Changes.Add(docChangeAttribute);
										docChangeAttribute.Name = docAttributeBase.Name;
										docChangeAttribute.Action = DocChangeActionEnum.DELETED;
									}
								}
							}

						}

						// now find deleted qsets
						foreach (DocQuantitySet docEntityBase in docSchemaBase.QuantitySets)
						{
							// find equivalent
							DocQuantitySet docEntity = null;
							foreach (DocQuantitySet docEntityEach in docSchema.QuantitySets)
							{
								if (docEntityEach.Name.Equals(docEntityBase.Name))
								{
									docEntity = docEntityEach;
									break;
								}
							}

							if (docEntity == null)
							{
								// entity may have moved to other schema; check other schemas
								DocSchema docThatSchema = null;
								foreach (DocSection docOtherSection in docProject.Sections)
								{
									foreach (DocSchema docOtherSchema in docOtherSection.Schemas)
									{
										foreach (DocQuantitySet docOtherEntity in docOtherSchema.QuantitySets)
										{
											if (docOtherEntity.Name.Equals(docEntityBase.Name))
											{
												docEntity = docOtherEntity;
												docThatSchema = docOtherSchema;
											}
										}
									}
								}

								DocChangeAction docChangeEntity = new DocChangeAction();
								docChangeSchemaQuantities.Changes.Add(docChangeEntity);
								docChangeEntity.Name = docEntityBase.Name;

								if (docEntity != null)
								{
									// moved from another schema
									docChangeEntity.Action = DocChangeActionEnum.MOVED;
									docChangeEntity.Aspects.Add(new DocChangeAspect(DocChangeAspectEnum.SCHEMA, docSchema.Name.ToUpper(), docThatSchema.Name.ToUpper()));
								}
								else
								{
									// otherwise, deleted
									docChangeEntity.Action = DocChangeActionEnum.DELETED;
								}
							}
						}
						// end quantity sets

					}
				}

				foreach (DocSchema docSchemaBase in docSectionBase.Schemas)
				{
					// find equivalent schema
					DocSchema docSchema = null;
					foreach (DocSchema docSchemaEach in docSection.Schemas)
					{
						if (docSchemaEach.Name.Equals(docSchemaBase.Name))
						{
							docSchema = docSchemaEach;
							break;
						}
					}

					if (docSchema == null)
					{
						DocChangeAction docChangeSchema = new DocChangeAction();
						docChangeSchema.Name = docSchemaBase.Name;
						docChangeSchema.Action = DocChangeActionEnum.DELETED;
						docChangeSection.Changes.Add(docChangeSchema);

						// list all deleted types
						foreach (DocType docTypeBase in docSchemaBase.Types)
						{
							DocChangeAction docChangeType = new DocChangeAction();
							docChangeSchema.Changes.Add(docChangeType);
							docChangeType.Name = docTypeBase.Name;

							// each entity either moved or deleted

							// entity may have moved to other schema; check other schemas
							DocSchema docThatSchema = null;
							foreach (DocSection docOtherSection in docProject.Sections)
							{
								foreach (DocSchema docOtherSchema in docOtherSection.Schemas)
								{
									foreach (DocType docOtherType in docOtherSchema.Types)
									{
										if (docOtherType.Name.Equals(docTypeBase.Name))
										{
											docThatSchema = docOtherSchema;
										}
									}
								}
							}

							if (docThatSchema != null)
							{
								docChangeType.Action = DocChangeActionEnum.MOVED;
								docChangeType.Aspects.Add(new DocChangeAspect(DocChangeAspectEnum.SCHEMA, docSchemaBase.Name, docThatSchema.Name));
							}
							else
							{
								docChangeType.Action = DocChangeActionEnum.DELETED;

								// deleting a type does not impact file
							}
						}


						// list all deleted entities
						foreach (DocEntity docEntityBase in docSchemaBase.Entities)
						{
							DocChangeAction docChangeEntity = new DocChangeAction();
							docChangeSchema.Changes.Add(docChangeEntity);
							docChangeEntity.Name = docEntityBase.Name;

							// each entity either moved or deleted

							// entity may have moved to other schema; check other schemas
							DocSchema docThatSchema = null;
							foreach (DocSection docOtherSection in docProject.Sections)
							{
								foreach (DocSchema docOtherSchema in docOtherSection.Schemas)
								{
									foreach (DocEntity docOtherEntity in docOtherSchema.Entities)
									{
										if (docOtherEntity.Name.Equals(docEntityBase.Name))
										{
											docThatSchema = docOtherSchema;
										}
									}
								}
							}

							if (docThatSchema != null)
							{
								docChangeEntity.Action = DocChangeActionEnum.MOVED;
								docChangeEntity.Aspects.Add(new DocChangeAspect(DocChangeAspectEnum.SCHEMA, docSchemaBase.Name, docThatSchema.Name));
							}
							else
							{
								docChangeEntity.Action = DocChangeActionEnum.DELETED;
								docChangeEntity.ImpactSPF = true;
								docChangeEntity.ImpactXML = true;
							}
						}
					}
				}

			}


			// check changes for model views
			foreach (DocModelView docView in docProject.ModelViews)
			{
				DocChangeAction docChangeView = new DocChangeAction();
				docChangeView.Name = docView.Name;
				docChangeSet.ChangesViews.Add(docChangeView);

				// find equivalent schema
				DocModelView docViewBase = null;
				foreach (DocModelView docViewEach in projectPrev.ModelViews)
				{
					if (docViewEach.Name.Equals(docView.Name))
					{
						docViewBase = docViewEach;
						break;
					}
				}

				if (docViewBase == null)
				{
					// new schema
					docChangeView.Action = DocChangeActionEnum.ADDED;
				}
				else
				{
					// existing schema


					// compare types
					foreach (DocConceptRoot docType in docView.ConceptRoots)
					{
						if (docType.ApplicableEntity != null)
						{
							DocChangeAction docChangeType = new DocChangeAction();
							docChangeView.Changes.Add(docChangeType);
							docChangeType.Name = docType.ApplicableEntity.Name;

							// find equivalent type
							DocConceptRoot docTypeBase = null;
							foreach (DocConceptRoot docTypeEach in docViewBase.ConceptRoots)
							{
								if (docTypeEach.ApplicableEntity != null &&
									docTypeEach.ApplicableEntity.Name.Equals(docType.ApplicableEntity.Name))
								{
									docTypeBase = docTypeEach;
									break;
								}
							}

							if (docTypeBase == null)
							{
								// new type
								docChangeType.Action = DocChangeActionEnum.ADDED;
							}

							if (docTypeBase != null)
							{
								// find concepts added
								foreach (DocTemplateUsage docConstant in docType.Concepts)
								{
									// find equivalent constant
									DocTemplateUsage docConstantBase = null;
									foreach (DocTemplateUsage docConstantEach in docTypeBase.Concepts)
									{
										if (docConstant.Definition != null &&
											docConstantEach.Definition != null &&
											docConstantEach.Definition.Uuid.Equals(docConstant.Definition.Uuid))
										{
											docConstantBase = docConstantEach;
											break;
										}
									}

									// for constants, only generate additions or deletions for brevity
									if (docConstantBase == null)
									{
										// new entity
										DocChangeAction docChangeConstant = new DocChangeAction();
										docChangeType.Changes.Add(docChangeConstant);
										docChangeConstant.Name = docConstant.Definition.Name;
										docChangeConstant.Action = DocChangeActionEnum.ADDED;
									}
								}

								// find constants removed
								foreach (DocTemplateUsage docConstantBase in docTypeBase.Concepts)
								{
									// find equivalent constant
									DocTemplateUsage docConstant = null;
									foreach (DocTemplateUsage docConstantEach in docType.Concepts)
									{
										if (docConstantBase.Definition != null &&
											docConstantEach.Definition != null &&
											docConstantEach.Definition.Name.Equals(docConstantBase.Definition.Name))
										{
											docConstant = docConstantEach;
											break;
										}
									}

									// for constants, only generate additions or deletions for brevity
									if (docConstant == null)
									{
										// deleted
										DocChangeAction docChangeConstant = new DocChangeAction();
										docChangeType.Changes.Add(docChangeConstant);
										docChangeConstant.Name = docConstantBase.Definition.Name;
										docChangeConstant.Action = DocChangeActionEnum.DELETED;
									}
								}

							}

						}
					}

				}
			}


		}

		private static void CheckObjectChanges(DocChangeAction docChangeType, DocObject source, DocObject target)
		{
			bool depSource = source.IsDeprecated();
			bool depTarget = target.IsDeprecated();

			// only check for deprecation
			if (depSource != depTarget)
			{
				docChangeType.Action = DocChangeActionEnum.MODIFIED;
				if (depTarget)
				{
					docChangeType.Aspects.Add(new DocChangeAspect(DocChangeAspectEnum.STATUS, String.Empty, target.Status));
				}
				else
				{
					docChangeType.Aspects.Add(new DocChangeAspect(DocChangeAspectEnum.STATUS, source.Status, String.Empty));
				}
			}
		}

		/// <summary>
		/// Builds list of inherited direct attributes in order (excludes INVERSE attributes)
		/// </summary>
		/// <param name="list">list to populate</param>
		/// <param name="map">map to lookup for base types</param>
		/// <param name="docEntity">entity to traverse</param>
		private static void BuildAttributeListDirect(List<DocAttribute> list, Dictionary<string, DocObject> map, DocEntity docEntity)
		{
			// recurse to base type first
			if (docEntity.BaseDefinition != null)
			{
				DocEntity docSuper = map[docEntity.BaseDefinition] as DocEntity;
				BuildAttributeListDirect(list, map, docSuper);
			}

			// then add direct attributes
			foreach (DocAttribute docAttribute in docEntity.Attributes)
			{
				if (docAttribute.Inverse == null && docAttribute.Derived == null)
				{
					list.Add(docAttribute);
				}
			}
		}

		public static void BuildAttributeListInverse(List<DocAttribute> list, Dictionary<string, DocObject> map, DocEntity docEntity)
		{
			// recurse to base type first
			if (docEntity.BaseDefinition != null)
			{
				DocEntity docSuper = map[docEntity.BaseDefinition] as DocEntity;
				BuildAttributeListInverse(list, map, docSuper);
			}

			// then add direct attributes
			foreach (DocAttribute docAttribute in docEntity.Attributes)
			{
				if (docAttribute.Inverse != null)
				{
					list.Add(docAttribute);
				}
			}
		}

		/// <summary>
		/// Indicates whether select includes another select, entity, or value type
		/// </summary>
		/// <param name="docSelect"></param>
		/// <param name="docDefinition"></param>        
		/// <returns></returns>
		private static bool SelectIncludes(DocSelect docSelect, string defname, Dictionary<string, DocObject> map)
		{
			foreach (DocSelectItem docSelectItem in docSelect.Selects)
			{
				if (docSelectItem.Name == defname)
					return true;

				DocObject docObj = null;
				if (map.TryGetValue(docSelectItem.Name, out docObj))
				{
					if (docObj is DocSelect)
					{
						bool result = SelectIncludes((DocSelect)docObj, defname, map);
						if (result)
							return true;
					}
					else if (docObj is DocEntity)
					{
						bool result = EntityIncludes((DocEntity)docObj, defname, map);
						if (result)
							return true;
					}
				}
			}

			return false;
		}


		private static bool EntityIncludes(DocEntity docEntity, string defname, Dictionary<string, DocObject> map)
		{
			// traverse subtypes
			DocObject docTest = null;
			if (!map.TryGetValue(defname, out docTest))
				return false;

			if (!(docTest is DocEntity))
				return false;

			DocEntity docTestEntity = (DocEntity)docTest;
			if (docTestEntity.BaseDefinition == null)
				return false;

			if (docTestEntity.BaseDefinition == docEntity.Name)
				return true;

			// recurse upwards
			return EntityIncludes(docEntity, docTestEntity.BaseDefinition, map);
		}

	}
}
