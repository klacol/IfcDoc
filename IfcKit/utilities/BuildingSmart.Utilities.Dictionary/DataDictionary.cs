using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;
using System.Resources;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Web;

/* -- use types directly, rather than taking dependency on IFC schema
using BuildingSmart.IFC.IfcKernel;
using BuildingSmart.IFC.IfcPropertyResource;
using BuildingSmart.IFC.IfcMeasureResource;
using BuildingSmart.IFC.IfcUtilityResource;
*/
using BuildingSmart.IFC.IfcUtilityResource;
using BuildingSmart.Utilities.Conversion;


namespace BuildingSmart.Utilities.Dictionary
{
	public class DataDictionary
	{
		public const string bsdd_buildingsmart_org = "http://bsdd.buildingsmart.org/";
		public const string test_bsdd_buildingsmart_org = "http://test.bsdd.buildingsmart.org/";

		private static string LanguageEN = "3vvsOOoT0Hsm00051Mm008";
		private static string LanguageID = "1ASQw0qJqHuO00025QrE$V";

		private string m_uri;
		private string m_username;
		private string m_password;
		private string m_session;
		private string m_context; // active context guid, or null if none

		// cache mappings for efficiency (avoiding lookups)
		private Dictionary<Type, string> m_mapTypes = new Dictionary<Type, string>();
		private Dictionary<string, string> m_mapNamespaces = new Dictionary<string, string>();

		private AssemblyBuilder m_assembly; // for reading, construct assembly dynamically
		private ModuleBuilder m_module;
		private Dictionary<string, Type> m_mapDynamicTypes = new Dictionary<string, Type>();

		/// <summary>
		/// Connects to server using supplied credentials.
		/// Note that current implementation of bsDD does not support secure transmission of username/password; they will be sent in the clear.
		/// </summary>
		/// <param name="uri">Fully qualified URL of web server.</param>
		/// <param name="username">Username</param>
		/// <param name="password">Password</param>
		/// <returns>Connection to dictionary</returns>
		public static DataDictionary Connect(string uri, string username, string password) //... if/when bsDD supports secure authentication, then change to SecureString
		{
			string url = uri + "api/4.0/session/login?email=" + HttpUtility.UrlEncode(username) + "&password=" + HttpUtility.UrlEncode(password);

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
			if (cookie == null)
			{
				throw new InvalidDataException("Cannot connect to this server from the current internet connection, as it filters out cookies.");
			}

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

			DataDictionary dd = new DataDictionary();
			dd.m_uri = uri;
			dd.m_username = username;
			dd.m_password = password;
			dd.m_session = sessionid;

			string version = "0.0.0.0";
			string assembly = "BuildingSmart.Dictionary.Dynamic";
			string module = assembly + ".dll";
			ConstructorInfo conContract = (typeof(AssemblyVersionAttribute).GetConstructor(new Type[] { typeof(string) }));
			CustomAttributeBuilder cabAssemblyVersion = new CustomAttributeBuilder(conContract, new object[] { version });
			dd.m_assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(assembly), AssemblyBuilderAccess.RunAndSave, new CustomAttributeBuilder[] { cabAssemblyVersion });
			dd.m_module = dd.m_assembly.DefineDynamicModule(module, module);
			return dd;
		}

		/// <summary>
		/// Returns map of contexts accessible to the logged in account.
		/// </summary>
		/// <param name="writable">If true, returns only writable contexts; if false, returns all readable contexts for the logged in account.</param>
		/// <returns>Map of guids to display names containing all contexts available to user.</returns>
		public Dictionary<string, string> ReadContexts(bool writable)
		{
			try
			{
				string filter;
				if (writable)
				{
					filter = "currentUserCanEdit"; // note: not returning anything for account used at test server.
				}
				else
				{
					filter = "currentUserHasReadAccess";
				}

				string nextpage = null;
				do
				{
					string url = this.m_uri + "api/4.0/IfdContext/" + filter;

					if (nextpage != null)
					{
						url += "?page=" + nextpage;
					}

					HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
					request.Method = "GET";
					request.ContentType = "application/x-www-form-urlencoded";
					request.ContentLength = 0;
					request.Accept = "application/json";
					//request.Accept = "application/xml";
					request.Headers.Add("cookie", "peregrineapisessionid=" + this.m_session);
					HttpWebResponse response = (HttpWebResponse)request.GetResponse();
					nextpage = response.Headers.Get("Next-Page");
					Stream stream = response.GetResponseStream();

					byte[] buffer = new byte[response.ContentLength];
					int read = 0;
					do
					{
						read += stream.Read(buffer, read, (int)response.ContentLength - read);
					} while (read < response.ContentLength);
					MemoryStream ms = new MemoryStream(buffer);
					StreamReader reader = new StreamReader(ms, Encoding.UTF8);
					string jsondebug = reader.ReadToEnd();
					ms.Position = 0;

					DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(ifdContexts));
					//NetDataContractSerializer ser = new NetDataContractSerializer(typeof(ResponseContext).Name);
					//DataContractSerializer ser = new DataContractSerializer(typeof(ifdContexts), "ifdContexts", "http://peregrine.catenda.no/objects");

					ifdContexts context = (ifdContexts)ser.ReadObject(ms);
					if (context != null && context.IfdContext != null)
					{
						Dictionary<string, string> map = new Dictionary<string, string>();

						foreach (IfdContext ifdContext in context.IfdContext)
						{
							System.Diagnostics.Debug.Write("ReadContexts: " + ifdContext.guid + " ");

							if (ifdContext.fullNames != null)
							{

								string name = null;
								foreach (IfdName ifdName in ifdContext.fullNames)
								{
									System.Diagnostics.Debug.Write("ifdname[" + ifdName.languageFamily + "]=" + ifdName.name + ", ");

									if (ifdName.language.guid.Equals(LanguageEN, StringComparison.InvariantCultureIgnoreCase))
									{
										name = ifdName.name;// map.Add(ifdName.guid, ifdName.name);
															//break;
									}
								}

								if (name == null && ifdContext.fullNames.Length > 0)
								{
									// try the first one
									name = ifdContext.fullNames[0].name;
								}

								if (name != null)
								{
									map.Add(ifdContext.guid, name);
								}
							}

							if (ifdContext.definitions != null)
							{
								System.Diagnostics.Debug.Write("def=" + ifdContext.definitions[0].description);
							}


							System.Diagnostics.Debug.WriteLine("");
						}

						return map;
					}
				}
				while (!String.IsNullOrEmpty(nextpage));


			}
			catch (Exception xx)
			{
				System.Diagnostics.Debug.WriteLine(xx.Message);
			}

			return null;
		}

		private IfdName CreateName(string langid, string name)
		{
			try
			{
				string url = this.m_uri + "api/4.0/IfdName?languageGuid=" + langid + "&name=" + name + "&nameType=FULLNAME";
				HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
				request.Method = "POST";
				request.ContentType = "application/x-www-form-urlencoded";
				request.ContentLength = 0;
				request.Accept = "application/json";
				request.Headers.Add("cookie", "peregrineapisessionid=" + this.m_session);
				HttpWebResponse response = (HttpWebResponse)request.GetResponse();
				Stream stream = response.GetResponseStream();

				DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(IfdName));
				IfdName ifdRoot = (IfdName)ser.ReadObject(stream);
				return ifdRoot;
			}
			catch (Exception xx)
			{
				System.Diagnostics.Debug.WriteLine(xx.Message);
				throw (xx);
			}
		}

		public string GetActiveContext()
		{
			return this.m_context;
		}

		public void SetActiveContext(string context)
		{
			this.m_context = context;
		}

		private IfdBase CreateDescription(string langid, string desc)
		{
			try
			{
				// convert to form parameter...
				if (desc.Length > 8000)
				{
					//if (encodedesc.Length > 8000)//8192)
					//encodedesc = "!BSDD DATA TRANSFER ERROR -- description too large to encode within URL";

					desc = "[TRUNCATED] " + desc.Substring(0, 8000);// +"||TRUNCATED||";
				}

				string url = this.m_uri + "api/4.0/IfdDescription?languageGuid=" + langid + "&description=" + desc + "&descriptionType=DEFINITION";
				HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
				request.Method = "POST";
				request.ContentType = "application/x-www-form-urlencoded";
				request.ContentLength = 0;
				request.Accept = "application/json";
				request.Headers.Add("cookie", "peregrineapisessionid=" + this.m_session);
				HttpWebResponse response = (HttpWebResponse)request.GetResponse();
				Stream stream = response.GetResponseStream();

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

		private IfdBase CreateConcept(string identifier, string name, string desc, IfdConceptTypeEnum conctype, DisplayAttribute[] localize)
		{
			System.Diagnostics.Debug.WriteLine(DateTime.Now + " " + conctype.ToString() + " " + identifier + " START");

			List<IfdName> listNames = new List<IfdName>();
			List<IfdBase> listDescs = new List<IfdBase>();

			// create name for IFC
			IfdName ifdNameIFC = CreateName(LanguageID, identifier);
			if (ifdNameIFC == null)
				return null;

			listNames.Add(ifdNameIFC);

			// localization
			bool hasEnglishName = false;
			bool hasEnglishDesc = false;

			if (localize != null)
			{
				foreach (DisplayAttribute docLoc in localize)
				{
					string locale = docLoc.ShortName;

					// look up language id
					string langid = GetLanguageId(locale);
					if (langid != null)
					{
						if (!String.IsNullOrEmpty(docLoc.Name))
						{
							string locname = HttpUtility.UrlEncode(docLoc.Name);
							IfdName ifdName = CreateName(langid, locname);
							if (ifdName == null)
								return null;

							listNames.Add(ifdName);

							if (langid == LanguageEN)
							{
								hasEnglishName = true;
							}
						}

						if (!String.IsNullOrEmpty(docLoc.Description))
						{
							string locdesc = HttpUtility.UrlEncode(docLoc.Description);
							IfdBase ifdDesc = CreateDescription(langid, locdesc);
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
			}

			if (!hasEnglishName && !String.IsNullOrEmpty(name))
			{
				// add default english name
				IfdName ifdNameEN = CreateName(LanguageEN, name);
				if (ifdNameEN == null)
					return null;

				listNames.Add(ifdNameEN);
			}

			if (!hasEnglishDesc && !String.IsNullOrEmpty(desc))
			{
				string encodedesc = HttpUtility.UrlEncode(desc);
				IfdBase ifdDescEN = CreateDescription(LanguageEN, encodedesc);
				if (ifdDescEN == null)
					return null;

				listDescs.Add(ifdDescEN);
			}

			// create concept
			StringBuilder sb = new StringBuilder();

			sb.Append(this.m_uri);
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
			request.Headers.Add("cookie", "peregrineapisessionid=" + this.m_session);
			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			Stream stream = response.GetResponseStream();

			DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(IfdBase));
			IfdBase ifdRoot = (IfdBase)ser.ReadObject(stream);

			System.Diagnostics.Debug.WriteLine(DateTime.Now + " " + conctype.ToString() + " " + identifier + " FINISH");

			return ifdRoot;
		}

		private void CreateRelationship(string idParent, string idChild, IfdRelationshipTypeEnum reltype)
		{
			string url = this.m_uri + "api/4.0/IfdConcept/" + idParent + "/child?childGuid=" + idChild + "&relationshipType=" + reltype; // context guids???

			if (this.m_context != null)
			{
				url += "&contextGuids=" + this.m_context; // verify...
			}

			HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
			request.Method = "POST";
			request.ContentType = "application/x-www-form-urlencoded";
			request.ContentLength = 0;
			request.Accept = "application/json";
			request.Headers.Add("cookie", "peregrineapisessionid=" + this.m_session);

			try
			{
				HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			}
			catch (Exception xx)
			{
				System.Diagnostics.Debug.WriteLine(xx.Message);
			}

		}

		/// <summary>
		/// Finds an existing item by IFC identifier
		/// </summary>
		/// <param name="baseurl"></param>
		/// <param name="sessionid"></param>
		/// <param name="identifier"></param>
		/// <returns></returns>
		private IfdConcept SearchConcept(string identifier, IfdConceptTypeEnum type)
		{
			try
			{
				string url = this.m_uri + "api/4.0/IfdConcept/search/filter/language/" + LanguageID + "/type/" + type.ToString() + "/" + identifier;
				HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
				request.Method = "GET";
				request.ContentType = "application/x-www-form-urlencoded";
				request.ContentLength = 0;
				request.Accept = "application/json";
				request.Headers.Add("cookie", "peregrineapisessionid=" + this.m_session);
				HttpWebResponse response = (HttpWebResponse)request.GetResponse();
				Stream stream = response.GetResponseStream();

				DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(ResponseSearch));
				ResponseSearch search = (ResponseSearch)ser.ReadObject(stream);
				if (search != null && search.IfdConcept != null && search.IfdConcept.Length > 0)
				{
					// return latest one
					string lastversiondate = null;
					IfdConcept thatconc = null;
					foreach (IfdConcept conc in search.IfdConcept)
					{
						if (lastversiondate == null)
						{
							lastversiondate = conc.versionDate;
							thatconc = conc;
						}
						else
						{
							if (String.Compare(conc.versionDate, lastversiondate) > 0)
							{
								lastversiondate = conc.versionDate;
								thatconc = conc;
							}
						}
					}

					return thatconc;

					//return search.IfdConcept[search.IfdConcept.Length - 1]; // use last one
				}
			}
			catch (Exception xx)
			{
				System.Diagnostics.Debug.WriteLine(xx.Message);
			}

			return null;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="guidSource">Identifies the source object.</param>
		/// <param name="direction">True for children; False for parents.</param>
		/// <returns></returns>
		private IList<IfdConceptInRelationship> GetRelatedConcepts(string guidSource, bool direction)
		{
			SortedList<string, IfdConceptInRelationship> list = new SortedList<string, IfdConceptInRelationship>();

			// get children
			string nextpage = null;
			do
			{
				string url = this.m_uri + "api/4.0/IfdConcept/" + guidSource;
				if (direction)
				{
					url += "/children";//filter/SUBJECT"; // ifc-2X4
				}
				else
				{
					url += "/parents";
				}

				// filter
				//url += "/" + IfdRelationshipTypeEnum.COLLECTS.ToString();//!!!!

				if (nextpage != null)
				{
					url += "?page=" + nextpage;
				}

				HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
				request.Method = "GET";
				request.ContentType = "application/x-www-form-urlencoded";
				request.ContentLength = 0;
				request.Accept = "application/json";
				request.Headers.Add("cookie", "peregrineapisessionid=" + this.m_session);
				HttpWebResponse response = (HttpWebResponse)request.GetResponse();
				nextpage = response.Headers.Get("Next-Page");
				Stream stream = response.GetResponseStream();

				byte[] buffer = new byte[response.ContentLength];
				int read = 0;
				do
				{
					read += stream.Read(buffer, read, (int)response.ContentLength - read);
				} while (read < response.ContentLength);

				MemoryStream ms = new MemoryStream(buffer);

				StreamReader reader = new StreamReader(ms, Encoding.UTF8);
				string jsondebug = reader.ReadToEnd();
				ms.Position = 0;

				//ResponseConceptInRelationship responseSearch = null;
				ResponseConceptInRelationship responseSearch = null;
				try
				{
					DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(ResponseConceptInRelationship));
					responseSearch = (ResponseConceptInRelationship)ser.ReadObject(ms);
					if (responseSearch != null)
					{
						foreach (IfdConceptInRelationship ifdRel in responseSearch.IfdConceptInRelationship)
						{
							if (!list.ContainsKey(ifdRel.ToString()))
							{
								list.Add(ifdRel.ToString(), ifdRel);
							}
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

		/// <summary>
		/// Returns the IFC identifier (e.g. entity name) for a concept.
		/// </summary>
		/// <param name="ifdConcept"></param>
		/// <returns></returns>
		private string GetConceptIdentifier(IfdConcept ifdConcept)
		{
			if (ifdConcept.fullNames != null)
			{
				foreach (IfdName ifdName in ifdConcept.fullNames)
				{
					if (ifdName.languageFamily == "IFC")
						return ifdName.name;
				}
			}

			return null;
		}

		private string GetConceptName(IfdConcept ifdConcept)
		{
			if (ifdConcept.fullNames != null)
			{
				foreach (IfdName ifdName in ifdConcept.fullNames)
				{
					if (ifdName.language.languageCode == "en")
						return ifdName.name;
				}
			}

			return null;
		}

		private string GetConceptDescription(IfdConcept ifdConcept)
		{
			foreach (IfdDescription ifdName in ifdConcept.definitions)
			{
				if (ifdName.language.languageCode == "en")
					return ifdName.description;
			}

			return null;
		}

		private string CreateNamespaceFromConcept(IfdConcept ifdConcept)
		{
			string suffix = GetConceptIdentifier(ifdConcept);
			string prefix = null;
			IList<IfdConceptInRelationship> listHost = GetRelatedConcepts(ifdConcept.guid, false);
			foreach (IfdConceptInRelationship rel in listHost)
			{
				IfdRelationshipTypeEnum reltype;
				if (Enum.TryParse<IfdRelationshipTypeEnum>(rel.relationshipType, out reltype))
				{
					switch (reltype)
					{
						case IfdRelationshipTypeEnum.GROUPS:
							prefix = CreateNamespaceFromConcept(rel);
							break;
					}
				}
			}

			if (prefix != null)
			{
				return prefix + "." + suffix;
			}
			else
			{
				return suffix;
			}
		}

		private PropertyInfo CreatePropertyInfoFromConcept(IfdConcept ifdConcept)
		{
			//...
			return null;
		}

		private Type CreateTypeFromConcept(IfdConcept ifdConcept)
		{
			Type type = null;
			if (m_mapDynamicTypes.TryGetValue(ifdConcept.guid, out type))
				return type;

			// get base definition if any
			Type typeBase = null;
			string ns = null;
			IList<IfdConceptInRelationship> listHost = GetRelatedConcepts(ifdConcept.guid, false);
			foreach (IfdConceptInRelationship rel in listHost)
			{
				IfdRelationshipTypeEnum reltype;
				if (Enum.TryParse<IfdRelationshipTypeEnum>(rel.relationshipType, out reltype))
				{
					switch (reltype)
					{
						case IfdRelationshipTypeEnum.SPECIALIZES: // base type
							typeBase = CreateTypeFromConcept(rel);
							break;

						case IfdRelationshipTypeEnum.GROUPS: // namespace
							ns = CreateNamespaceFromConcept(rel);
							break;
					}
					//rel.guid
				}
			}

			// construct a type
			TypeAttributes attr = TypeAttributes.Public | TypeAttributes.Class;
			// abstract...

			string typename = GetConceptIdentifier(ifdConcept);

			try
			{
				TypeBuilder tb = this.m_module.DefineType(ns + "." + typename, attr, typeBase);

				// custom attributes, e.g. guid
				if (ifdConcept.guid != null)
				{
					Guid guid = GlobalId.Parse(ifdConcept.guid);

					ConstructorInfo conReq = typeof(GuidAttribute).GetConstructor(new Type[] { typeof(string) });
					CustomAttributeBuilder cabReq = new CustomAttributeBuilder(conReq, new object[] { guid.ToString() });
					tb.SetCustomAttribute(cabReq);
				}

				string displayname = GetConceptName(ifdConcept);
				if (displayname != null)
				{
					ConstructorInfo conReq = typeof(DisplayNameAttribute).GetConstructor(new Type[] { typeof(string) });
					CustomAttributeBuilder cabReq = new CustomAttributeBuilder(conReq, new object[] { displayname });
					tb.SetCustomAttribute(cabReq);
				}

				string description = GetConceptDescription(ifdConcept);
				if (description != null)
				{
					ConstructorInfo conReq = typeof(DescriptionAttribute).GetConstructor(new Type[] { typeof(string) });
					CustomAttributeBuilder cabReq = new CustomAttributeBuilder(conReq, new object[] { description });
					tb.SetCustomAttribute(cabReq);
				}

				// properties
				IList<IfdConceptInRelationship> listItem = GetRelatedConcepts(ifdConcept.guid, true);
				foreach (IfdConceptInRelationship rel in listItem)
				{
					IfdRelationshipTypeEnum reltype;
					if (Enum.TryParse<IfdRelationshipTypeEnum>(rel.relationshipType, out reltype))
					{
						switch (reltype)
						{
							case IfdRelationshipTypeEnum.ASSIGNS_PROPERTIES: //...
								break;
						}
					}
				}

				type = tb.CreateType();
				this.m_mapDynamicTypes.Add(ifdConcept.guid, type);
				return type;
			}
			catch
			{
				return null; // duplicate??
			}
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
		/// Reads items within namespace
		/// </summary>
		/// <param name="identifier"></param>
		/// <returns>List of namespaces (System.String) and/or types (System.Type) within namespace.</returns>
		public Dictionary<string, object> ReadNamespace(string identifier)
		{
			// first try for entity concept
			IfdConcept conc = SearchConcept(identifier, IfdConceptTypeEnum.BAG);
			if (conc == null)
				return null;

			IList<IfdConceptInRelationship> list = GetRelatedConcepts(conc.guid, true);
			Dictionary<string, object> ls = new Dictionary<string, object>();
			foreach (IfdConceptInRelationship cr in list)
			{
				IfdConceptTypeEnum conctype;
				if (Enum.TryParse<IfdConceptTypeEnum>(cr.conceptType, out conctype))
				{
					string name = GetConceptName(cr);

					switch (conctype)
					{
						case IfdConceptTypeEnum.SUBJECT:
							{
								Type t = CreateTypeFromConcept(cr);
								ls.Add(cr.guid, t);
							}
							break;

						case IfdConceptTypeEnum.BAG:
							ls.Add(cr.guid, name);
							break;
					}
				}
			}

			return ls;
		}

		/// <summary>
		/// Reads a data type from the dictionary and all properties within.
		/// </summary>
		/// <param name="identifier">Identifier of the object (IFC class name).</param>
		/// <returns></returns>
		public Type ReadType(string identifier)
		{
			// first try for entity concept
			IfdConcept conc = SearchConcept(identifier, IfdConceptTypeEnum.SUBJECT);
			if (conc == null)
			{
				// then try for measure
				conc = SearchConcept(identifier, IfdConceptTypeEnum.MEASURE);
				if (conc == null)
					return null;
			}

			Type type = CreateTypeFromConcept(conc);

			return type;
		}

		/// <summary>
		/// Writes a data type and all public instance properties. 
		/// Such type may correspond to an IFC entity or a ConceptRoot of a model view definition (which may also correspond to a property set)
		/// The type is nested according to namespace tokens.
		/// The type is linked to superclass according to base type.
		/// The type is expanded into subclasses if the last property is an enumeration for a classification (e.g. PredefinedType).
		/// Any referenced types are also retrieved and written if they don't yet exist.
		/// Localized names and descriptions are extracted from .NET resources.
		/// </summary>
		/// <param name="type"></param>
		public void WriteType(Type type)
		{
			if (type == null)
				return;

			if (m_mapTypes.ContainsKey(type))
				return; // already written

			string name = type.Name;
			DisplayNameAttribute attrName = (DisplayNameAttribute)type.GetCustomAttribute<DisplayNameAttribute>();
			if (attrName != null)
			{
				name = attrName.DisplayName;
			}

			string desc = null;
			DescriptionAttribute attrDesc = (DescriptionAttribute)type.GetCustomAttribute<DescriptionAttribute>();
			if (attrDesc != null)
			{
				desc = attrDesc.Description;
			}

			IfdConceptTypeEnum conctype = IfdConceptTypeEnum.SUBJECT;
			IfdRelationshipTypeEnum relbase = IfdRelationshipTypeEnum.SPECIALIZES;

			if (type.Name.StartsWith("Qto_"))
			{
				type.ToString();
			}

			if (type.Name.StartsWith("Pset_") || type.Name.StartsWith("Qto_"))
			{
				// hack
				conctype = IfdConceptTypeEnum.NEST;
				relbase = IfdRelationshipTypeEnum.ASSIGNS_COLLECTIONS;// ASSIGNS_PROPERTIES;// ASSIGNS_COLLECTIONS;
			}
			else if (type.IsValueType || type.IsEnum)
			{
				conctype = IfdConceptTypeEnum.MEASURE;
			}

			// retrieve existing -- enable once final uploaded correctly!
#if false
            IfdConcept conc = SearchConcept(type.Name, conctype);
            if(conc != null)
            {
                this.m_mapTypes.Add(type, conc.guid);
                return;
            }
#endif

			DisplayAttribute[] localize = (DisplayAttribute[])type.GetCustomAttributes(typeof(DisplayAttribute), false);
			IfdBase ifdThis = CreateConcept(type.Name, name, desc, conctype, localize);
			if (ifdThis == null)
				return;

			this.m_mapTypes.Add(type, ifdThis.guid);

			// get namespace
			string[] namespaces = type.Namespace.Split('.');
			string guidSchemaParent = null;
			string guidSchemaChild = null;
			for (int iNS = 0; iNS < namespaces.Length; iNS++)
			{
				string ns = namespaces[iNS];
				if (!this.m_mapNamespaces.TryGetValue(ns, out guidSchemaChild))
				{
					StringBuilder sbQual = new StringBuilder();
					for (int x = 0; x <= iNS; x++)
					{
						if (x > 0)
						{
							sbQual.Append(".");
						}
						sbQual.Append(namespaces[x]);
					}
					string qualname = sbQual.ToString();

					// call server to find namespace
					IfdConcept ifdNamespace = SearchConcept(qualname, IfdConceptTypeEnum.BAG);
					if (ifdNamespace != null)
					{
						guidSchemaChild = ifdNamespace.guid;
					}
					else
					{
						IfdBase ifdNS = CreateConcept(qualname, ns, String.Empty, IfdConceptTypeEnum.BAG, null);
						guidSchemaChild = ifdNS.guid;
					}

					this.m_mapNamespaces.Add(ns, guidSchemaChild);

					if (guidSchemaParent != null)
					{
						CreateRelationship(guidSchemaParent, guidSchemaChild, IfdRelationshipTypeEnum.COLLECTS);
					}
				}

				if (iNS == namespaces.Length - 1)
				{
					CreateRelationship(guidSchemaChild, ifdThis.guid, IfdRelationshipTypeEnum.COLLECTS);
				}

				guidSchemaParent = guidSchemaChild;
			}

			// get base type
			if (type.IsClass && type.BaseType != typeof(object))
			{
				WriteType(type.BaseType);

				string guidbase = null;
				if (m_mapTypes.TryGetValue(type.BaseType, out guidbase))
				{
					CreateRelationship(guidbase, ifdThis.guid, relbase);
				}
			}
			else if (type.IsEnum)
			{
				FieldInfo[] enumvals = type.GetFields(BindingFlags.Public | BindingFlags.Static);
				foreach (FieldInfo f in enumvals)
				{
					if (f.Name != "USERDEFINED" &&
						f.Name != "NOTDEFINED" &&
						f.Name != "OTHER" &&
						f.Name != "NOTKNOWN" &&
						f.Name != "UNSET")
					{
						string fname = f.Name;
						DisplayNameAttribute attrFName = (DisplayNameAttribute)f.GetCustomAttribute<DisplayNameAttribute>();
						if (attrFName != null)
						{
							fname = attrFName.DisplayName;
						}

						string fdesc = null;
						DescriptionAttribute attrFDesc = (DescriptionAttribute)f.GetCustomAttribute<DescriptionAttribute>();
						if (attrFDesc != null)
						{
							fdesc = attrFDesc.Description;
						}

						DisplayAttribute[] localizePredef = (DisplayAttribute[])f.GetCustomAttributes(typeof(DisplayAttribute), false);
						IfdBase ifdPredef = CreateConcept(type.Name + "." + f.Name, fname, fdesc, IfdConceptTypeEnum.VALUE, localizePredef);
						if (ifdPredef != null)
						{
							CreateRelationship(ifdThis.guid, ifdPredef.guid, IfdRelationshipTypeEnum.ASSIGNS_VALUES);
						}
					}
				}
			}

			PropertyInfo[] fields = type.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
			foreach (PropertyInfo prop in fields)
			{
				// write the property itself

				// resolve property type
				Type typeProp = prop.PropertyType;
				if (typeProp.IsGenericType && typeProp.GetGenericTypeDefinition() == typeof(Nullable<>))
				{
					typeProp = typeProp.GetGenericArguments()[0];
				}

				if (typeProp.IsGenericType && typeProp.GetGenericTypeDefinition() == typeof(IList<>))
				{
					typeProp = typeProp.GetGenericArguments()[0];
				}

				if (typeProp.IsGenericType && typeProp.GetGenericTypeDefinition() == typeof(ISet<>))
				{
					typeProp = typeProp.GetGenericArguments()[0];
				}

				if (typeProp.IsArray)
				{
					typeProp = typeProp.GetElementType();
				}

				// special case for specialization
				if (prop.Name.Equals("PredefinedType"))
				{
					FieldInfo[] enumvals = typeProp.GetFields(BindingFlags.Public | BindingFlags.Static);
					foreach (FieldInfo f in enumvals)
					{
						if (f.Name != "USERDEFINED" && f.Name != "NOTDEFINED")
						{

							string fname = f.Name;
							DisplayNameAttribute attrFName = (DisplayNameAttribute)f.GetCustomAttribute<DisplayNameAttribute>();
							if (attrFName != null)
							{
								fname = attrFName.DisplayName;
							}

							string fdesc = null;
							DescriptionAttribute attrFDesc = (DescriptionAttribute)f.GetCustomAttribute<DescriptionAttribute>();
							if (attrFDesc != null)
							{
								fdesc = attrFDesc.Description;
							}

							DisplayAttribute[] localizePredef = (DisplayAttribute[])f.GetCustomAttributes(typeof(DisplayAttribute), false);
							IfdBase ifdPredef = CreateConcept(type.Name + "." + f.Name, fname, fdesc, IfdConceptTypeEnum.SUBJECT, localizePredef);
							if (ifdPredef != null)
							{
								CreateRelationship(ifdThis.guid, ifdPredef.guid, IfdRelationshipTypeEnum.SPECIALIZES);
							}
						}
					}
				}
				else if (conctype == IfdConceptTypeEnum.NEST) // BAG// psetprop.FieldType.IsValueType) //!!!
				{

					name = type.Name;
					attrName = (DisplayNameAttribute)prop.GetCustomAttribute<DisplayNameAttribute>();
					if (attrName != null)
					{
						name = attrName.DisplayName;
					}

					desc = null;
					attrDesc = (DescriptionAttribute)prop.GetCustomAttribute<DescriptionAttribute>();
					if (attrDesc != null)
					{
						desc = attrDesc.Description;
					}

					string propidentifier = type.Name + "." + prop.Name;
					IfdBase ifdProp = CreateConcept(propidentifier, name, desc, IfdConceptTypeEnum.PROPERTY, null);
					if (ifdProp == null)
						return;

					// include the property
					CreateRelationship(ifdThis.guid, ifdProp.guid, IfdRelationshipTypeEnum.COLLECTS);

					WriteType(typeProp);

					if (typeProp.IsValueType)
					{
						string guidDataType = null;
						if (m_mapTypes.TryGetValue(typeProp, out guidDataType))
						{
							CreateRelationship(ifdProp.guid, guidDataType, IfdRelationshipTypeEnum.ASSIGNS_MEASURES);//...verify
						}
					}
				}
			}
		}
	}
}

// Mapping => Expand into Concept's for each item, with template generated on the fly

// PropertySet => Concept Root, with constraint indicating applicable type
// Property => Concept, using template elaborated to data type for specific usage (or, use template with parameters?)

// QuantitySet => Concept Root
// Quantity =>

// generate code from concepts:
// rather than mappings,
// each ConceptRoot => Type
// each Concept => Property -- Concept name is the name; need to mark "value" leaf node -- indicated by last parameter value, or named "Value"?

// 