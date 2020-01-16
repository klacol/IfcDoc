using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

using System.ComponentModel;
using System.Net;
using System.Web;

using IfcDoc.Format.XML;
using IfcDoc.Schema;
using IfcDoc.Schema.DOC;
using IfcDoc.Schema.PSD;

namespace IfcDoc
{
	public class DataDictionary
	{
		/// <summary>
		/// Connects to Data Dictionary and returns session ID.
		/// </summary>1
		/// <param name="project"></param>
		/// <param name="worker"></param>
		/// <param name="baseurl"></param>
		/// <param name="username"></param>
		/// <param name="password"></param>
		/// <returns></returns>
		public static string Connect(DocProject project, BackgroundWorker worker, string baseurl, string username, string password)
		{
			string url = baseurl + "api/4.0/session/login?email=" + HttpUtility.UrlEncode(username) + "&password=" + HttpUtility.UrlEncode(password);

			HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
			request.Method = "POST";
			request.ContentLength = 0;
			request.ContentType = "application/x-www-form-urlencoded";
			request.Accept = "application/json";
			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			System.IO.Stream stream = response.GetResponseStream();
			System.IO.StreamReader reader = new System.IO.StreamReader(stream);
			string body = reader.ReadToEnd();

			string cookie = response.Headers.Get("Set-Cookie");

			string[] parts = cookie.Split(new char[] { ';', ',' }); // bug? comma separates session ID
			string match = "peregrineapisessionid=";
			string sessionid = null;
			foreach (string part in parts)
			{
				if (part.StartsWith(match))
				{
					sessionid = part.Substring(match.Length);
					break;
				}
			}

			return sessionid;
		}

		public static ResponseContext GetContexts(DocProject docProject, BackgroundWorker worker, string baseurl, string username, string password)
		{
			string sessionid = Connect(docProject, worker, baseurl, username, password);
			string url = baseurl + "api/4.0/IfdContext/currentUserHasReadAccess";
			HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
			request.Accept = "application/json";
			request.Headers.Add("cookie", "peregrineapisessionid=" + sessionid);
			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			Stream stream = response.GetResponseStream();

			ResponseContext ifdRoot = null;
			try
			{
				DataContractJsonSerializerSettings settings = new DataContractJsonSerializerSettings();
				DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(ResponseContext));
				ifdRoot = (ResponseContext)ser.ReadObject(stream);
			}
			catch
			{
			}

			return ifdRoot;
		}

		/// <summary>
		/// Gets child concepts
		/// </summary>
		/// <param name="docProject"></param>
		/// <param name="worker"></param>
		/// <param name="baseurl"></param>
		/// <param name="username"></param>
		/// <param name="password"></param>
		/// <param name="ifdParent"></param>
		/// <param name="direction">True for children, False for parents</param>
		/// <returns></returns>
		public static IList<IfdConceptInRelationship> GetConcepts(DocProject docProject, BackgroundWorker worker, string baseurl, string username, string password, IfdConceptInRelationship ifdParent, bool direction)
		{
			SortedList<string, IfdConceptInRelationship> list = new SortedList<string, IfdConceptInRelationship>();

			string sessionid = Connect(docProject, worker, baseurl, username, password);

			// get children
			string nextpage = null;
			do
			{
				string url = baseurl + "api/4.0/IfdConcept/" + ifdParent.guid;
				if (direction)
				{
					url += "/children";//filter/SUBJECT"; // ifc-2X4
				}
				else
				{
					url += "/parents";
				}

				if (nextpage != null)
				{
					url += "?page=" + nextpage;
				}

				HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
				request.Accept = "application/json";
				request.Headers.Add("cookie", "peregrineapisessionid=" + sessionid);
				HttpWebResponse response = (HttpWebResponse)request.GetResponse();
				nextpage = response.Headers.Get("Next-Page");
				Stream stream = response.GetResponseStream();

				ResponseConceptInRelationship responseSearch = null;
				try
				{
					DataContractJsonSerializerSettings settings = new DataContractJsonSerializerSettings();
					DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(ResponseConceptInRelationship));
					responseSearch = (ResponseConceptInRelationship)ser.ReadObject(stream);
					if (responseSearch != null)
					{
						foreach (IfdConceptInRelationship ifdRel in responseSearch.IfdConceptInRelationship)
						{
							list.Add(ifdRel.ToString(), ifdRel);
						}
					}
				}
				catch
				{
					break;
				}
			}
			while (!String.IsNullOrEmpty(nextpage));

			return list.Values;
		}

		public static void Download(DocProject project, BackgroundWorker worker, string baseurl, string username, string password, DocModelView[] docViews)
		{
			string sessionid = Connect(project, worker, baseurl, username, password);
			if (sessionid == null)
				return;

			//foreach (DocModelView docView in docViews)
			string page = null;
			do
			{
				//string url = baseurl + "api/4.0/IfdContext/" + SGuid.Format(docView.Uuid);
				string url = baseurl + "api/4.0/IfdConcept/filter/SUBJECT"; // ifc-2X4
																			//string url = baseurl + "api/4.0/IfdConcept/search/filter/language/1ASQw0qJqHuO00025QrE$V/*";//type/SUBJECT/*";
				if (page != null)
				{
					url += "?page=" + page;
				}

				HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
				request.Accept = "application/json";
				request.Headers.Add("cookie", "peregrineapisessionid=" + sessionid);
				HttpWebResponse response = (HttpWebResponse)request.GetResponse();
				Stream stream = response.GetResponseStream();

				page = response.Headers.Get("Next-Page");
				System.Diagnostics.Debug.WriteLine(page);

				ResponseSearch responseSearch = null;
				try
				{
					DataContractJsonSerializerSettings settings = new DataContractJsonSerializerSettings();
					DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(ResponseSearch));
					responseSearch = (ResponseSearch)ser.ReadObject(stream);
					if (responseSearch != null)
					{
						responseSearch.ToString();

						foreach (IfdConcept ifdConcept in responseSearch.IfdConcept)
						{
							DocModelView docView = new DocModelView();

							if (ifdConcept.shortNames != null)
							{
								foreach (IfdName ifdName in ifdConcept.shortNames)
								{
									DocLocalization docLoc = new DocLocalization();
									docLoc.Locale = ifdName.language.languageCode;
									docLoc.Name = ifdName.name;
									docView.Localization.Add(docLoc);

									if (ifdName.language.languageCode == "en")
									{
										docView.Name = ifdName.name;
									}
								}
							}
							else if (ifdConcept.fullNames != null)
							{
								ifdConcept.ToString();
							}

							docView.Uuid = SGuid.Parse(ifdConcept.guid);
							docView.Version = ifdConcept.versionId;
							docView.Copyright = ifdConcept.versionDate;
							docView.Status = ifdConcept.status;
							//docView.Owner = ifdConcept.owner;
							//docView.Documentation = ifdConcept.comments

							project.ModelViews.Add(docView);

							ifdConcept.ToString();
						}
						//foreach (IfdDescription ifcDesc in ifdContext.definitions)
						{
							// create/update concept root
							//...
						}
					}
				}
				catch (Exception xx)
				{
					xx.ToString();
				}

			}
			while (!String.IsNullOrEmpty(page));
		}

		public static void Upload(DocProject docProject, BackgroundWorker worker, string baseurl, string username, string password, string parentid, DocModelView[] docViews)
		{
			string sessionid = Connect(docProject, worker, baseurl, username, password);

#if false
            if (docViews != null && docViews.Length > 0)
            {
                foreach (DocModelView docView in docViews)//docProject.ModelViews)
                {
                    // hack: only bridge view for now
                    if (docView.Name.Contains("Bridge"))
                    {
                        string codename = docView.Name;
                        if (!String.IsNullOrEmpty(docView.Code))
                        {
                            codename = docView.Code;
                        }

                        IfdBase ifdView = CreateConcept(baseurl, sessionid, docView, codename, docView.Name, IfdConceptTypeEnum.BAG);
                        //CreateRelationship(baseurl, sessionid, parentid, ifdView.guid, "COLLECTS"); // no top-level item for now

                        foreach (DocConceptRoot docRoot in docView.ConceptRoots)
                        {
                            if (docRoot.Name != null)
                            {
                                System.Diagnostics.Debug.WriteLine(docRoot.ToString());

                                IfdBase ifdRoot = CreateConcept(baseurl, sessionid, docRoot, docRoot.ApplicableEntity.Name, docRoot.Name, IfdConceptTypeEnum.SUBJECT);
                                CreateRelationship(baseurl, sessionid, ifdView.guid, ifdRoot.guid, IfdRelationshipTypeEnum.COLLECTS);

                                foreach (DocTemplateUsage docConc in docRoot.Concepts)
                                {
                                    UploadTemplateUsage(docProject, baseurl, sessionid, ifdRoot.guid, docConc);
                                }
                            }
                        }
                    }
                }
            }
            else
#endif
			{
				// build list of types referenced by property sets
				Dictionary<string, IfdBase> mapEntities = new Dictionary<string, IfdBase>();


				// core schema
				foreach (DocSection docSection in docProject.Sections)
				{
					foreach (DocSchema docSchema in docSection.Schemas)
					{
						// only export objects that have associated property sets
						foreach (DocPropertySet docPset in docSchema.PropertySets)
						{
							IfdBase ifdPset = CreateConcept(baseurl, sessionid, docPset, docPset.Name, docPset.Name, docPset.PropertySetType.ToString(), IfdConceptTypeEnum.BAG);
							foreach (DocProperty docProp in docPset.Properties)
							{
								IfdBase ifdProp = CreateConcept(baseurl, sessionid, docProp, docProp.Name, docProp.Name, docProp.PropertyType.ToString(), IfdConceptTypeEnum.PROPERTY);
								if (ifdProp != null)
								{
									CreateRelationship(baseurl, sessionid, ifdPset.guid, ifdProp.guid, IfdRelationshipTypeEnum.COLLECTS);

									string paramval = docProp.PrimaryDataType;
									if (!String.IsNullOrEmpty(paramval))
									{
										DocDefinition docDef = docProject.GetDefinition(paramval);
										if (docDef != null)
										{
											// get the measure type
											IfdBase ifdType = SearchConcept(baseurl, sessionid, docDef.Name, IfdConceptTypeEnum.MEASURE);
											if (ifdType == null)
											{
												// create concept
												ifdType = CreateConcept(baseurl, sessionid, docDef, docDef.Name, docDef.Name, null, IfdConceptTypeEnum.MEASURE);

												// for enums, get enumerated type
												if (docProp.PropertyType == DocPropertyTemplateTypeEnum.P_ENUMERATEDVALUE)
												{
													DocSchema docPropSchema = null;
													DocPropertyEnumeration docPropEnum = docProject.FindPropertyEnumeration(docProp.SecondaryDataType, out docPropSchema);
													if (docPropEnum != null)
													{
														foreach (DocPropertyConstant docPropConst in docPropEnum.Constants)
														{
															IfdBase ifdConst = CreateConcept(baseurl, sessionid, docPropConst, docPropConst.Name, docPropConst.Name, null, IfdConceptTypeEnum.VALUE);
															CreateRelationship(baseurl, sessionid, ifdType.guid, ifdConst.guid, IfdRelationshipTypeEnum.ASSIGNS_VALUES);
														}
													}
												}
											}
											CreateRelationship(baseurl, sessionid, ifdProp.guid, ifdType.guid, IfdRelationshipTypeEnum.ASSIGNS_MEASURES); // ??? fails for Pset_BuildingUse.NarrativeText / IfcText
										}
									}
								}
							}

							// now link the property set to applicable type
							DocEntity[] docEntities = docPset.GetApplicableTypeDefinitions(docProject);
							if (docEntities != null && docEntities.Length > 0)
							{
								// only the first one matters
								DocEntity docEnt = docEntities[0];
								IfdBase ifdEnt = null;
								if (!mapEntities.TryGetValue(docEnt.Name, out ifdEnt))
								{
									ifdEnt = CreateConcept(baseurl, sessionid, docEnt, docEnt.Name, docEnt.Name, null, IfdConceptTypeEnum.SUBJECT);
									mapEntities.Add(docEnt.Name, ifdEnt);

									// subtypes (predefined type)
									foreach (DocAttribute docAttr in docEnt.Attributes)
									{
										if (docAttr.Name.Equals("PredefinedType"))
										{
											DocEnumeration docEnum = docProject.GetDefinition(docAttr.DefinedType) as DocEnumeration;
											if (docEnum != null)
											{
												foreach (DocConstant docConst in docEnum.Constants)
												{
													IfdBase ifdConst = CreateConcept(baseurl, sessionid, docConst, docConst.Name, docConst.Name, null, IfdConceptTypeEnum.SUBJECT);
													CreateRelationship(baseurl, sessionid, ifdEnt.guid, ifdConst.guid, IfdRelationshipTypeEnum.SPECIALIZES);
												}
											}
										}
									}
								}
								CreateRelationship(baseurl, sessionid, ifdEnt.guid, ifdPset.guid, IfdRelationshipTypeEnum.ASSIGNS_COLLECTIONS); //!!! Fails with Forbidden!!! why???
																																				// http://test.bsdd.buildingsmart.org/api/4.0/IfdRelationship/validrelations/SUBJECT/BAG indicates 
																																				// <ifdRelationshipTypeEnums>
																																				//   <IfdRelationshipType xmlns="http://peregrine.catenda.no/objects">ASSIGNS_COLLECTIONS</IfdRelationshipType>
																																				// </ifdRelationshipTypeEnums>
							}
						}

						foreach (DocQuantitySet docPset in docSchema.QuantitySets)
						{
							IfdBase ifdPset = CreateConcept(baseurl, sessionid, docPset, docPset.Name, docPset.Name, "QTO_OCCURRENCEDRIVEN", IfdConceptTypeEnum.BAG);
							foreach (DocQuantity docProp in docPset.Quantities)
							{
								IfdBase ifdProp = CreateConcept(baseurl, sessionid, docProp, docProp.Name, docProp.Name, docProp.QuantityType.ToString(), IfdConceptTypeEnum.PROPERTY);
								if (ifdProp != null)
								{
									CreateRelationship(baseurl, sessionid, ifdPset.guid, ifdProp.guid, IfdRelationshipTypeEnum.COLLECTS);

									string propclass = "IfcQuantityCount";
									switch (docProp.QuantityType)
									{
										case DocQuantityTemplateTypeEnum.Q_AREA:
											propclass = "IfcQuantityArea";
											break;

										case DocQuantityTemplateTypeEnum.Q_COUNT:
											propclass = "IfcQuantityCount";
											break;

										case DocQuantityTemplateTypeEnum.Q_LENGTH:
											propclass = "IfcQuantityLength";
											break;

										case DocQuantityTemplateTypeEnum.Q_TIME:
											propclass = "IfcQuantityTime";
											break;

										case DocQuantityTemplateTypeEnum.Q_VOLUME:
											propclass = "IfcQuantityVolume";
											break;

										case DocQuantityTemplateTypeEnum.Q_WEIGHT:
											propclass = "IfcQuantityWeight";
											break;
									}

									string paramval = propclass;
									if (!String.IsNullOrEmpty(paramval))
									{
										DocDefinition docDef = docProject.GetDefinition(paramval);
										if (docDef != null)
										{
											// get the measure type
											IfdBase ifdType = SearchConcept(baseurl, sessionid, docDef.Name, IfdConceptTypeEnum.MEASURE);
											if (ifdType == null)
											{
												// create concept
												ifdType = CreateConcept(baseurl, sessionid, docDef, docDef.Name, docDef.Name, null, IfdConceptTypeEnum.MEASURE);
											}
											CreateRelationship(baseurl, sessionid, ifdProp.guid, ifdType.guid, IfdRelationshipTypeEnum.ASSIGNS_MEASURES); // ??? fails for Pset_BuildingUse.NarrativeText / IfcText
										}
									}
								}
							}

							// now link the property set to applicable type
							DocEntity[] docEntities = docPset.GetApplicableTypeDefinitions(docProject);
							if (docEntities != null && docEntities.Length > 0)
							{
								// only the first one matters
								DocEntity docEnt = docEntities[0];
								IfdBase ifdEnt = null;
								if (mapEntities.TryGetValue(docEnt.Name, out ifdEnt))
								{
									CreateRelationship(baseurl, sessionid, ifdEnt.guid, ifdPset.guid, IfdRelationshipTypeEnum.ASSIGNS_COLLECTIONS);
								}
							}

						}
					}
				}
			}
		}

		public static void UploadTemplateUsage(DocProject docProject, string baseurl, string sessionid, string parentid, DocTemplateUsage docConc)
		{
			// MVD Concept entry is a relationship in BSDD; contexts are exchanges
			foreach (DocTemplateItem docItem in docConc.Items)
			{
				if (docItem.Concepts.Count == 0)
				{
					string code = docItem.GetParameterValue("Reference");
					if (String.IsNullOrEmpty(code))
					{
						code = docItem.Name;
					}
					code = HttpUtility.UrlEncode(code);

					string name = docItem.GetParameterValue("Name");
					if (code != null && name != null)
					{
						// if no sub-concepts, then its a property
						IfdBase ifdItem = CreateConcept(baseurl, sessionid, docItem, code, name, null, IfdConceptTypeEnum.PROPERTY);
						CreateRelationship(baseurl, sessionid, parentid, ifdItem.guid, IfdRelationshipTypeEnum.ASSIGNS_PROPERTIES);

						string paramval = docItem.GetParameterValue("Value");
						if (!String.IsNullOrEmpty(paramval))
						{
							DocDefinition docDef = docProject.GetDefinition(paramval);
							if (docDef != null)
							{
								// get the measure type
								IfdBase ifdType = SearchConcept(baseurl, sessionid, docDef.Name, IfdConceptTypeEnum.MEASURE);
								if (ifdType == null)
								{
									// create concept
									ifdType = CreateConcept(baseurl, sessionid, docDef, docDef.Name, docDef.Name, null, IfdConceptTypeEnum.MEASURE);
								}
								CreateRelationship(baseurl, sessionid, ifdItem.guid, ifdType.guid, IfdRelationshipTypeEnum.ASSIGNS_MEASURES);
							}
						}
					}
				}
				else
				{
					// otherwise its a nest
					if (docItem.Name != null)
					{
						IfdBase ifdItem = CreateConcept(baseurl, sessionid, docItem, docItem.Name, docItem.Name, null, IfdConceptTypeEnum.NEST);
						CreateRelationship(baseurl, sessionid, parentid, ifdItem.guid, IfdRelationshipTypeEnum.ASSIGNS_COLLECTIONS);

						// recurse -- e.g. properties within property sets
						foreach (DocTemplateUsage docInner in docItem.Concepts)
						{
							UploadTemplateUsage(docProject, baseurl, sessionid, ifdItem.guid, docInner);
						}
					}
				}
			}

		}

		public static string LanguageEN = "3vvsOOoT0Hsm00051Mm008";
		public static string LanguageID = "1ASQw0qJqHuO00025QrE$V";

		public static IfdName CreateName(string baseurl, string sessionid, string langid, string name)
		{
			try
			{

				string url = baseurl + "api/4.0/IfdName?languageGuid=" + langid + "&name=" + name + "&nameType=FULLNAME";
				HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
				request.Method = "POST";
				request.ContentType = "application/x-www-form-urlencoded";
				request.ContentLength = 0;
				request.Accept = "application/json";
				request.Headers.Add("cookie", "peregrineapisessionid=" + sessionid);
				HttpWebResponse response = (HttpWebResponse)request.GetResponse();
				Stream stream = response.GetResponseStream();

				DataContractJsonSerializerSettings settings = new DataContractJsonSerializerSettings();
				DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(IfdName));
				IfdName ifdRoot = (IfdName)ser.ReadObject(stream);
				return ifdRoot;
			}
			catch (Exception xx)
			{
				System.Diagnostics.Debug.WriteLine(xx.Message);
			}

			return null;
		}

		public static IfdBase CreateDescription(string baseurl, string sessionid, string langid, string desc)
		{
			try
			{
				string url = baseurl + "api/4.0/IfdDescription?languageGuid=" + langid + "&description=" + desc + "&descriptionType=DEFINITION";
				HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
				request.Method = "POST";
				request.ContentType = "application/x-www-form-urlencoded";
				request.ContentLength = 0;
				request.Accept = "application/json";
				request.Headers.Add("cookie", "peregrineapisessionid=" + sessionid);
				HttpWebResponse response = (HttpWebResponse)request.GetResponse();
				Stream stream = response.GetResponseStream();

				DataContractJsonSerializerSettings settings = new DataContractJsonSerializerSettings();
				DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(IfdBase));
				IfdBase ifdRoot = (IfdBase)ser.ReadObject(stream);
				return ifdRoot;
			}
			catch (Exception xx)
			{
				System.Diagnostics.Debug.WriteLine(xx.Message);
			}

			return null;
		}

		/// <summary>
		/// Finds an existing item by IFC identifier
		/// </summary>
		/// <param name="baseurl"></param>
		/// <param name="sessionid"></param>
		/// <param name="identifier"></param>
		/// <returns></returns>
		public static IfdConcept SearchConcept(string baseurl, string sessionid, string identifier, IfdConceptTypeEnum type)
		{
			try
			{
				string url = baseurl + "api/4.0/IfdConcept/search/filter/language/" + LanguageID + "/type/" + type.ToString() + "/" + identifier;
				HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
				request.Method = "GET";
				request.ContentType = "application/x-www-form-urlencoded";
				request.ContentLength = 0;
				request.Accept = "application/json";
				request.Headers.Add("cookie", "peregrineapisessionid=" + sessionid);
				HttpWebResponse response = (HttpWebResponse)request.GetResponse();
				Stream stream = response.GetResponseStream();

				DataContractJsonSerializerSettings settings = new DataContractJsonSerializerSettings();
				DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(ResponseSearch));
				ResponseSearch search = (ResponseSearch)ser.ReadObject(stream);
				if (search != null && search.IfdConcept != null && search.IfdConcept.Length > 0)
					return search.IfdConcept[0];
			}
			catch (Exception xx)
			{
				System.Diagnostics.Debug.WriteLine(xx.Message);
			}

			return null;
		}

		/// <summary>
		/// Maps locale to bsDD guid.
		/// </summary>
		/// <param name="locale"></param>
		/// <returns>Language ID if it exists, or null.</returns>
		private static string GetLanguageId(string locale)
		{
			if (locale == null)
				return null;

			string major = locale.Substring(0, 2).ToLower();
			switch (major)
			{
				case "en":
					return "3vvsOOoT0Hsm00051Mm008";

				case "zn":
				case "zh":
					return "1WfhO0obaHsm00W01W3_W0";

				case "it":
					return "1WfhOAobaHsm00W01W3_W0";

				case "ru":
					return "1WfhOKobaHsm00W01W3_W0";

				case "ko":
				case "kr":
					return "1WfhOoobaHsm00W01W3_W0";

				case "es":
					return "2Ym1xvKZb4sfGlIwkz49rS";

				case "de":
					return "3vvsOYoT0Hsm00051Mm008";

				case "nn":
					return "3vvsOEoT0Hsm00051Mm008";

				case "fr":
					return "3vvsOsoT0Hsm00051Mm008";

				case "ja":
					return "3vvsPyoT0Hsm00051Mm008";
			}

			return null;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="baseurl">URL of server</param>
		/// <param name="sessionid">identifies server session</param>
		/// <param name="docObject">object to be published</param>
		/// <param name="identifier">identifier to use for IFC name</param>
		/// <param name="name">identifier to use for English name</param>
		/// <param name="type">identifier to use for IFC property type encoding, which is stored within description of IFC language</param>
		/// <param name="conctype">type of concept</param>
		/// <returns>guid of new concept</returns>
		public static IfdBase CreateConcept(string baseurl, string sessionid, DocObject docObject, string identifier, string name, string type, IfdConceptTypeEnum conctype)
		{
			System.Diagnostics.Debug.WriteLine(DateTime.Now + " " + conctype.ToString() + " " + identifier + " START");

			List<IfdName> listNames = new List<IfdName>();
			List<IfdBase> listDescs = new List<IfdBase>();

			// create name for IFC
			IfdName ifdNameIFC = CreateName(baseurl, sessionid, LanguageID, identifier);
			if (ifdNameIFC == null)
				return null;

			listNames.Add(ifdNameIFC);

			// create type identifier if indicated
			if (!String.IsNullOrEmpty(type))
			{
				string desc = HttpUtility.UrlEncode(type);
				IfdBase ifdDescID = CreateDescription(baseurl, sessionid, LanguageID, desc);
				if (ifdDescID == null)
					return null;

				listDescs.Add(ifdDescID);
			}

			// localization
			bool hasEnglishName = false;
			bool hasEnglishDesc = false;
			foreach (DocLocalization docLoc in docObject.Localization)
			{
				// look up language id
				string langid = GetLanguageId(docLoc.Locale);
				if (langid != null)
				{
					if (!String.IsNullOrEmpty(docLoc.Name))
					{
						string locname = HttpUtility.UrlEncode(docLoc.Name);
						IfdName ifdName = CreateName(baseurl, sessionid, langid, locname);
						if (ifdName == null)
							return null;

						listNames.Add(ifdName);

						if (langid == LanguageEN)
						{
							hasEnglishName = true;
						}
					}

					if (!String.IsNullOrEmpty(docLoc.Documentation))
					{
						string locdesc = HttpUtility.UrlEncode(docLoc.Documentation);
						IfdBase ifdDesc = CreateDescription(baseurl, sessionid, langid, locdesc);
						if (ifdDesc == null)
							return null;

						listDescs.Add(ifdDesc);

						if (langid == LanguageEN)
						{
							hasEnglishDesc = true;
						}
					}
				}
			}

			if (!hasEnglishName && !String.IsNullOrEmpty(name))
			{
				// add default english name
				IfdName ifdNameEN = CreateName(baseurl, sessionid, LanguageEN, name);
				if (ifdNameEN == null)
					return null;

				listNames.Add(ifdNameEN);
			}

			if (!hasEnglishDesc && !String.IsNullOrEmpty(docObject.Documentation))
			{
				string desc = HttpUtility.UrlEncode(docObject.Documentation);
				IfdBase ifdDescEN = CreateDescription(baseurl, sessionid, LanguageEN, desc);
				if (ifdDescEN == null)
					return null;

				listDescs.Add(ifdDescEN);
			}

			// create concept
			StringBuilder sb = new StringBuilder();

			sb.Append(baseurl);
			sb.Append("api/4.0/IfdConcept?fullNameGuids=");
			for (int i = 0; i < listNames.Count; i++)
			{
				if (i != 0)
				{
					sb.Append(",");
				}
				sb.Append(listNames[i].guid);
			}
			sb.Append("&conceptType=");
			sb.Append(conctype.ToString());

			if (listDescs.Count > 0)
			{
				sb.Append("&definitionGuids=");
				for (int i = 0; i < listDescs.Count; i++)
				{
					if (i != 0)
					{
						sb.Append(",");
					}
					sb.Append(listDescs[i].guid);
				}
			}

			string url = sb.ToString();

			HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
			request.Method = "POST";
			request.ContentType = "application/x-www-form-urlencoded";
			request.ContentLength = 0;
			request.Accept = "application/json";
			request.Headers.Add("cookie", "peregrineapisessionid=" + sessionid);
			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			Stream stream = response.GetResponseStream();

			DataContractJsonSerializerSettings settings = new DataContractJsonSerializerSettings();
			DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(IfdBase));
			IfdBase ifdRoot = (IfdBase)ser.ReadObject(stream);

			// record the ID on the doc object
			docObject.RegisterDictionary(baseurl, ifdRoot.guid);

			System.Diagnostics.Debug.WriteLine(DateTime.Now + " " + conctype.ToString() + " " + identifier + " FINISH");

			return ifdRoot;
		}

		public static void CreateRelationship(string baseurl, string sessionid, string idParent, string idChild, IfdRelationshipTypeEnum reltype)
		{
			//// requires admin access: string url = baseurl + "api/4.0/IfdRelationship?relationshipType=" + reltype + "&parentGuid=" + idParent + "&childGuid=" + idChild; // contextGuids=,
			string url = baseurl + "api/4.0/IfdConcept/" + idParent + "/child?childGuid=" + idChild + "&relationshipType=" + reltype; // context guids???
			HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
			request.Method = "POST";
			request.ContentType = "application/x-www-form-urlencoded";
			request.ContentLength = 0;
			request.Accept = "application/json";
			request.Headers.Add("cookie", "peregrineapisessionid=" + sessionid);

			try
			{
				HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			}
			catch (Exception xx)
			{
				System.Diagnostics.Debug.WriteLine(xx.Message);
			}

		}
	}

	[DataContract]
	public class ResponseContext
	{
		[DataMember] public IfdContext[] IfdContext;
	}

	[DataContract]
	public class ResponseSearch
	{
		[DataMember] public IfdConcept[] IfdConcept;
	}

	[DataContract]
	public class ResponseConceptInRelationship
	{
		[DataMember] public IfdConceptInRelationship[] IfdConceptInRelationship;
	}

	[DataContract]
	public class IfdConceptInRelationship : IfdConcept
	{
	}

	[DataContract]
	public class IfdBase
	{
		[DataMember(Order = 0)] public string guid;
	}

	[DataContract]
	public class IfdContext : IfdBase
	{
		[DataMember(Order = 0)] public IfdName[] fullNames;
		[DataMember(Order = 1)] public IfdDescription[] definitions;
		[DataMember(Order = 2)] public string status;
		[DataMember(Order = 3)] public string versionDate;
		[DataMember(Order = 4)] public string versionId;
		[DataMember(Order = 5)] public bool readOnly;
		[DataMember(Order = 6)] public bool restricted;
	}

	[DataContract]
	public class IfdConcept : IfdBase
	{
		[DataMember(Order = 0)] public string versionId;
		[DataMember(Order = 1)] public string versionDate;
		[DataMember(Order = 2)] public string status;
		[DataMember(Order = 3)] public IfdName[] fullNames;
		[DataMember(Order = 4)] public IfdDescription definitions;
		[DataMember(Order = 5)] public IfdDescription comments;
		[DataMember(Order = 6)] public string conceptType;
		[DataMember(Order = 7)] public IfdName[] shortNames;
		[DataMember(Order = 8)] public IfdName[] lexemes;
		[DataMember(Order = 9)] public IfdIllustration[] illustrations;
		[DataMember(Order = 10)] public IfdOrganization owner;

		public override string ToString()
		{
			foreach (IfdName ifdName in this.fullNames)
			{
				if (ifdName.languageFamily == "IFC")
				{
					return ifdName.name;
				}
			}

			foreach (IfdName ifdName in this.fullNames)
			{
				if (ifdName.language.languageCode == "en")
				{
					return ifdName.name;
				}
			}

			return this.guid;
		}
	}

	[DataContract]
	public class IfdIllustration : IfdLanguageRepresentationBase
	{
		[DataMember(Order = 0)] public string blobstoreKey;
		[DataMember(Order = 1)] public string illustrationUrl;
	}

	[DataContract]
	public class IfdOrganization
	{
		[DataMember(Order = 0)] public string guid;
		[DataMember(Order = 1)] public string name;
		[DataMember(Order = 2)] public string url;
	}

	public enum IfdConceptTypeEnum
	{
		NULL = 0,
		ACTOR = 1,
		ACTIVITY = 2,
		DOCUMENT = 3,
		PROPERTY = 4,
		SUBJECT = 5,
		UNIT = 6,
		MEASURE = 7,
		VALUE = 8,
		NEST = 9,
		BAG = 10,
		CLASSIFICATION = 11,
		UNDEFINED = -1,
	}

	public enum IfdRelationshipTypeEnum
	{
		NULL = 0,
		COLLECTS = 1,
		ASSIGNS_COLLECTIONS = 2,
		ASSOCIATES = 3,
		COMPOSES = 4,
		GROUPS = 5,
		SPECIALIZES = 6,
		ACTS_UPON = 7,
		SEQUENCES = 8,
		DOCUMENTS = 9,
		CLASSIFIES = 10,
		ASSIGNS_MEASURES = 11,
		ASSIGNS_PROPERTIES = 12,
		ASSIGNS_UNITS = 13,
		ASSIGNS_VALUES = 14,
		ASSIGNS_PROPERTY_WITH_VALUES = 15,
		UNDEFINED = -1,
	}

	[DataContract]
	public abstract class IfdLanguageRepresentationBase
	{
		[DataMember(Order = 0)] public string guid;
		[DataMember(Order = 1)] public IfdLanguage language;
		[DataMember(Order = 2)] public string languageFamily;

	}

	[DataContract]
	public class IfdName : IfdLanguageRepresentationBase
	{
		[DataMember(Order = 3)] public string name;
		[DataMember(Order = 4)] public string nameType;
	}

	[DataContract]
	public class IfdDescription : IfdLanguageRepresentationBase
	{
		[DataMember(Order = 3)] public string description;
		[DataMember(Order = 4)] public string descriptionType; // enum...
	}

	public enum IfdDescriptionTypeEnum
	{
		NULL = 0,
		DEFINITION = 1,
		COMMENT = 2,
		UNDEFINED = -1,
	}

	[DataContract]
	public class IfdLanguage
	{
		[DataMember(Order = 0)] public string guid;
		[DataMember(Order = 1)] public string nameInEnglish;
		[DataMember(Order = 2)] public string nameInSelf;
		[DataMember(Order = 3)] public string languageCode;
	}

	// 2018-01-05 Meeting Notes: don't encode property type in IFC description, but use type relationship with IfcPropertySingleValue, etc.
	// Enclose entities within schema
	// Enclose schema within IFC top node

	// Reference Value / Time Series
	// Table Value
	// List Value
	// Complex Value
}
