// Name:        XmlSerializer.cs
// Description: XML serializer
// Author:      Tim Chipman
// Origination: Work performed for BuildingSmart by Constructivity.com LLC.
// Copyright:   (c) 2017 BuildingSmart International Ltd.
// License:     http://www.buildingsmart-tech.org/legal

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Xml;

namespace BuildingSmart.Serialization.Xml
{
	public class XmlFolderSerializer : XmlSerializer
	{
		private Dictionary<string, string> m_typeFilePrefix = new Dictionary<string, string>();
		private Dictionary<string, string> m_typeNoFilePrefix = new Dictionary<string, string>();
		private Dictionary<Type, string> m_NominatedTypeFilePrefix = new Dictionary<Type, string>();

		private char[] InvalidFileNameChars = new char[0];

		public XmlFolderSerializer(Type type) : base(type)
		{
			// get the XML namespace
			_ObjectStore.UseUniqueIdReferences = true;
			InvalidFileNameChars = Path.GetInvalidFileNameChars();
		}
		public void AddFilePrefix(Type type, string prefix)
		{
			m_NominatedTypeFilePrefix[type] = prefix;
			m_typeFilePrefix[type.FullName] = prefix;
		}
		private string hasFilePrefix(Type type)
		{
			string prefix = "", name = type.FullName, str = "";
			if (m_typeFilePrefix.TryGetValue(name, out prefix))
				return prefix;
			if (m_typeNoFilePrefix.TryGetValue(name, out str))
				return "";
			foreach (KeyValuePair<Type, string> pair in m_NominatedTypeFilePrefix)
			{
				if (type.IsSubclassOf(pair.Key))
				{
					m_typeFilePrefix[name] = pair.Value;
					return pair.Value;
				}
			}
			m_typeNoFilePrefix[name] = name;
			return "";

		}
		private char initialChar(string name, string prefix, int prefixLength)
		{
			if (name.StartsWith(prefix))
				return name.Substring(prefixLength)[0];
			return name[0];
		}
		protected override void WriteHeader(StreamWriter writer)
		{
			string header = "<?xml version=\"1.0\" encoding=\"utf-8\"?>";
			writer.WriteLine(header);
		}
		protected override void WriteFooter(StreamWriter writer)
		{
			writer.Write("\r\n");
		}
		/// <summary>
		/// Writes an object graph to a folder hierarchy with local xml files.
		/// </summary>
		/// <param name="stream">The stream to write.</param>
		/// <param name="root">The root object to write (typically IfcProject)</param>
		public void WriteObject(string folderPath, object root)
		{
			if (Directory.Exists(folderPath))
				Directory.CreateDirectory(folderPath);
			if (!Directory.Exists(folderPath))
				throw new ArgumentNullException("Folder not Created");

			if (root == null)
				throw new ArgumentNullException("root");


			writeFirstPassForIds(root, new HashSet<string>());
			writeObjectFolder(Path.Combine(folderPath, removeInvalidFile(root.GetType().Name)+".xml"), root);
		}

		private string removeInvalidFile(string str)
		{
			string result = str;
			foreach (char c in InvalidFileNameChars)
				result = result.Replace(c, '_');

			return result;
		}
		private void writeObjectFolder(string filePath, object obj)
		{
			_ObjectStore.UnMarkSerialized(obj);
			Type objectType = obj.GetType(), stringType = typeof(String);

			string folderPath = Path.GetDirectoryName(filePath);
			HashSet<string> nestedProperties = new HashSet<string>();

			List<Tuple<string, object, bool>> queued = new List<Tuple<string, object, bool>>();
			IList<PropertyInfo> fields = this.GetFieldsOrdered(objectType);
			foreach (PropertyInfo propertyInfo in fields)
			{
				if (propertyInfo == null)
					continue;
				Type propertyType = propertyInfo.PropertyType;
				XmlArrayAttribute xmlArrayAttribute = propertyInfo.GetCustomAttribute<XmlArrayAttribute>();
				XmlArrayItemAttribute xmlArrayItemAttribute = propertyInfo.GetCustomAttribute<XmlArrayItemAttribute>();

				if (xmlArrayAttribute != null && xmlArrayItemAttribute != null)
				{
					bool isLeaf = xmlArrayItemAttribute != null && string.Compare(xmlArrayItemAttribute.DataType, "_leaf_", true) == 0;
					if (string.IsNullOrEmpty(xmlArrayItemAttribute.ElementName) && xmlArrayItemAttribute.NestingLevel > 0 && propertyType.IsGenericType && typeof(IEnumerable).IsAssignableFrom(propertyType.GetGenericTypeDefinition()))
					{
						Type genericType = propertyType.GetGenericArguments()[0];
						PropertyInfo uniqueIdProperty = genericType.GetProperty("id", typeof(string));
						PropertyInfo folderNameProperty = genericType.GetProperty("_folderName", typeof(string));
						if (folderNameProperty == null)
							folderNameProperty = uniqueIdProperty;
						
						PropertyInfo nameProp = objectType.GetProperty("Name", typeof(string));
						if (uniqueIdProperty != null)
						{
							IEnumerable enumerable = propertyInfo.GetValue(obj) as IEnumerable;
							if (enumerable != null)
							{
								bool allSaved = true, nestingValid = true; ;
								int count = 0;
								foreach (object nested in enumerable)
								{
									if (nested == null)
										continue;
									count++;
									if (!_ObjectStore.isSerialized(nested))
										allSaved = false;
									object objId = uniqueIdProperty.GetValue(nested);
									if (objId == null || string.IsNullOrEmpty(objId.ToString()))
									{
										nestingValid = false;
										break;
									}
								}
								if (nestingValid && !allSaved)
								{
									string nestedPath = Path.Combine(folderPath, removeInvalidFile(propertyInfo.Name));
									Directory.CreateDirectory(nestedPath);
									nestedProperties.Add(propertyInfo.Name);
									if (xmlArrayItemAttribute.NestingLevel > 1)
									{
										string prefix = m_NominatedTypeFilePrefix.Count > 0 ? hasFilePrefix(genericType) : "";
										IEnumerable<IGrouping<char, object>> groups = null;
										if (string.IsNullOrEmpty(prefix))
											groups = enumerable.Cast<object>().GroupBy(x => char.ToLower(uniqueIdProperty.GetValue(x).ToString()[0]));
										else
										{
											int prefixLength = prefix.Length;
											groups = enumerable.Cast<object>().GroupBy(x => char.ToLower(initialChar(uniqueIdProperty.GetValue(x).ToString(), prefix, prefixLength)));
										}
										foreach (IGrouping<char, object> group in groups)
										{
											string alphaPath = Path.Combine(nestedPath, group.Key.ToString());
											Directory.CreateDirectory(alphaPath);
											foreach (object nested in group)
											{
												_ObjectStore.MarkSerialized(nested);
												string nestedObjectPath = isLeaf ? alphaPath : Path.Combine(alphaPath, removeInvalidFile(folderNameProperty.GetValue(nested).ToString()));
												Directory.CreateDirectory(nestedObjectPath);
												string fileName = removeInvalidFile((isLeaf ? uniqueIdProperty.GetValue(nested).ToString() : nested.GetType().Name) + ".xml");
												queued.Add(new Tuple<string, object, bool>(Path.Combine(nestedObjectPath, fileName), nested, isLeaf));
											}
										}
										continue;
									}
									foreach (object nested in enumerable)
									{
										if (nested == null)
											continue;
										_ObjectStore.MarkSerialized(nested);
										string nestedObjectPath = isLeaf ? nestedPath : Path.Combine(nestedPath, removeInvalidFile(isLeaf ? uniqueIdProperty.GetValue(nested).ToString() : folderNameProperty.GetValue(nested).ToString()));
										Directory.CreateDirectory(nestedObjectPath);
										string fileName = removeInvalidFile((isLeaf ? uniqueIdProperty.GetValue(nested).ToString() : nested.GetType().Name) + ".xml");
										queued.Add(new Tuple<string, object, bool>(Path.Combine(nestedObjectPath, fileName), nested, isLeaf));
									}
								}
							}
						}
					}
				}
				else
				{
					object propertyObject = propertyInfo.GetValue(obj);
					if (propertyObject == null)
					{
						nestedProperties.Add(propertyInfo.Name);
					}
					else
					{
						DataType dataType = DataType.Custom;
						string fileExtension = ".txt", txt = "";
						foreach (DataTypeAttribute dataTypeAttribute in propertyInfo.GetCustomAttributes<DataTypeAttribute>())
						{
							FileExtensionsAttribute fileExtensionsAttribute = dataTypeAttribute as FileExtensionsAttribute;
							if (fileExtensionsAttribute != null && !string.IsNullOrEmpty(fileExtensionsAttribute.Extensions))
								fileExtension = fileExtensionsAttribute.Extensions;
							if (dataTypeAttribute.DataType != DataType.Custom)
								dataType = dataTypeAttribute.DataType;
						}
						if (dataType == DataType.Html)
						{
							string html = propertyObject.ToString();
							if (!string.IsNullOrEmpty(html))
							{
								nestedProperties.Add(propertyInfo.Name);
								string htmlPath = Path.Combine(folderPath, propertyInfo.Name + ".html");
								File.WriteAllText(htmlPath, html.TrimEnd() + Environment.NewLine, Encoding.UTF8);
								continue;
							}
						}
						else if (dataType == DataType.MultilineText)
						{
							byte[] byteArray = propertyObject as byte[];
							if (byteArray != null)
							{
								nestedProperties.Add(propertyInfo.Name);
								txt = Encoding.ASCII.GetString(byteArray);
							}
							else
								txt = propertyObject.ToString();
							if (!string.IsNullOrEmpty(txt))
							{
								nestedProperties.Add(propertyInfo.Name);
								string txtPath = Path.Combine(folderPath, propertyInfo.Name + fileExtension);
								File.WriteAllText(txtPath, txt.TrimEnd() + Environment.NewLine, Encoding.UTF8);
								continue;
							}
						}
					}
				}
			}
			if (nestedProperties.Count < fields.Count)
			{
				_ObjectStore.UnMarkSerialized(obj);
				using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
				{
					writeObject(fileStream, obj, nestedProperties);
				}
			}
			foreach (Tuple<string, object,bool> o in queued)
			{
				if(o.Item3)
				{
					_ObjectStore.UnMarkSerialized(o.Item2);
					using (FileStream fileStream = new FileStream(o.Item1, FileMode.Create, FileAccess.Write))
					{
						writeObject(fileStream, o.Item2, new HashSet<string>());
					}
				}
				else
					writeObjectFolder(o.Item1, o.Item2);
			}
		}

		public object ReadObject(string folderPath)
		{
			if (!Directory.Exists(folderPath))
				throw new ArgumentNullException("Folder doesn't exist");

			Dictionary<string, object> instances = new Dictionary<string, object>();
			QueuedObjects queuedObjects = new QueuedObjects();

			return readFolder(folderPath, RootType, instances, queuedObjects);
		}
		private object readFolder(string folderPath, Type nominatedType, Dictionary<string, object> instances, QueuedObjects queuedObjects)
		{
			string[] files = Directory.GetFiles(folderPath, "*.xml", SearchOption.TopDirectoryOnly);
			if (files == null || files.Length == 0)
				return null;

			object result = readFile(files[0], nominatedType, instances, queuedObjects);
			if (result == null)
				return null;

			Type objectType = result.GetType();
			
			foreach (string file in Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly))
			{
				string extension = Path.GetExtension(file);
				if (string.Compare(file, ".xml", true) == 0)
					continue;
				PropertyInfo f = GetFieldByName(objectType, Path.GetFileNameWithoutExtension(file));
				if (f != null)
				{
					Type type = f.PropertyType;
					if (type == typeof(byte[]))
					{
						string text = File.ReadAllText(file);
						f.SetValue(result, Encoding.ASCII.GetBytes(text));
					}
					else if (type == typeof(string))
					{
						string text = File.ReadAllText(file);
						f.SetValue(result, text);
					}
				}
			}
			
			string[] directories = Directory.GetDirectories(folderPath, "*", SearchOption.TopDirectoryOnly);
			foreach(string directory in directories)
			{
				string directoryName = new DirectoryInfo(directory).Name;	
				PropertyInfo f = GetFieldByName(objectType, directoryName);
				if (f != null)
				{
					if (IsEntityCollection(f.PropertyType))
					{
						IEnumerable list = f.GetValue(result) as IEnumerable;
						Type typeCollection = list.GetType();
						MethodInfo methodAdd = typeCollection.GetMethod("Add");
						if (methodAdd == null)
						{
							throw new Exception("Unsupported collection type " + typeCollection.Name);
						}
						Type collectionGeneric = typeCollection.GetGenericArguments()[0];
						List<object> objects = new List<object>();
						string[] subDirectories = Directory.GetDirectories(directory, "*", SearchOption.TopDirectoryOnly);
						foreach(string subDir in subDirectories)
						{
							string[] subfiles = Directory.GetFiles(subDir, "*.xml", SearchOption.TopDirectoryOnly);
							if(subfiles.Length == 1)
							{
								object o = readFolder(subDir, collectionGeneric, instances, queuedObjects);
								if (o != null)
									objects.Add(o);
							}
							if (subfiles.Length > 1)
							{
								foreach (string subfile in subfiles)
								{
									object o = readFile(subfile, collectionGeneric, instances, queuedObjects);

									if (o != null)
									{
										try
										{
											methodAdd.Invoke(list, new object[] { o }); // perf!!
										}
										catch (Exception) { }
									}
								}

							}
							else
							{
								string[] subsubDirectories = Directory.GetDirectories(subDir, "*", SearchOption.TopDirectoryOnly);
								foreach(string subsubDir in subsubDirectories)
								{
									object o = readFolder(subsubDir, collectionGeneric, instances, queuedObjects);
									if (o != null)
										objects.Add(o);
								}

							}
						}
						foreach (object o in objects)
						{
							try
							{
								methodAdd.Invoke(list, new object[] { o }); // perf!!
							}
							catch (Exception) { }
						}
					}
					else
					{
						object o = readFolder(directory, f.PropertyType, instances, queuedObjects);
						if (o != null)
							LoadEntityValue(result, f, o);
					}
				}

			}
			return result;
		}
		private object readFile(string filePath, Type nominatedType, Dictionary<string,object> instances, QueuedObjects queuedObjects)
		{
			string fileName = Path.GetFileNameWithoutExtension(filePath);
			Type detectedType = GetTypeByName(fileName);
			if (detectedType != null && nominatedType != null && !detectedType.IsSubclassOf(nominatedType))
				detectedType = null;

			string typeName = detectedType == null ? "" : detectedType.Name;

			object result = null;
			using (FileStream streamSource = new FileStream(filePath, FileMode.Open))
			{
				XmlReaderSettings settings = new XmlReaderSettings { NameTable = new NameTable() };
				XmlNamespaceManager xmlns = new XmlNamespaceManager(settings.NameTable);
				xmlns.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
				XmlParserContext context = new XmlParserContext(null, xmlns, "", XmlSpace.Default);
				using (XmlReader reader = XmlReader.Create(streamSource, settings, context))
				{
					result = ReadEntity(reader, instances, typeName, queuedObjects);
				}
			}
			return result;
		}
	}
}


