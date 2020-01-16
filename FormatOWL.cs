using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

using IfcDoc.Schema;
using IfcDoc.Schema.DOC;

namespace IfcDoc
{
	public class FormatOWL :
		IFormatExtension
	{
		public static HashSet<string> listPropertiesOutput = new HashSet<string>();
		public static HashSet<string> addedIndividuals = new HashSet<string>();
		Dictionary<Tuple<string, string>, int> attribInverses = new Dictionary<Tuple<string, string>, int>(); // number of inverses of an attribute of a specific entity
		Dictionary<string, HashSet<string>> subTypesOfEntity = new Dictionary<string, HashSet<string>>(); // all subtypes of an Entity


		public FormatOWL()
		{ }

		public string FormatEntity(DocEntity docEntity, Dictionary<string, DocObject> map, Dictionary<DocObject, bool> included)
		{
			return FormatEntityFull(docEntity, map, included, false);
		}

		public string FormatEntityFull(DocEntity docEntity, Dictionary<string, DocObject> map, Dictionary<DocObject, bool> included, bool fullListing)
		{



			// entity
			StringBuilder sb = new StringBuilder();
			// object properties
			StringBuilder sbProps = new StringBuilder();
			// lists
			StringBuilder sbLists = new StringBuilder();

			sb.AppendLine("ifc:" + docEntity.Name);

			// superclass
			if (docEntity.BaseDefinition != null)
			{
				DocEntity super = map[docEntity.BaseDefinition] as DocEntity;
				if (super != null)
				{
					sb.AppendLine("\trdfs:subClassOf \tifc:" + super.Name + " ;");

					// disjoint
					if (subTypesOfEntity.ContainsKey(super.Name))
					{
						string tmp = "";
						foreach (string subtype in subTypesOfEntity[super.Name])
						{
							if (subtype != docEntity.Name)
							{
								if (tmp.Length > 0)
									tmp += ", ";
								tmp += ToOwlClass(subtype) + " ";
							}
						}
						if (tmp.Length > 0)
							sb.AppendLine("\towl:disjointWith " + tmp + ";");
					}
				}
			}

			// abstract class
			if (docEntity.IsAbstract)
			{
				sb.AppendLine("\trdfs:subClassOf ");
				sb.AppendLine("\t\t[ ");
				sb.AppendLine("\t\t\trdf:type owl:Class ;");
				sb.Append("\t\t\towl:unionOf ( ");
				if (subTypesOfEntity.ContainsKey(docEntity.Name))
				{
					foreach (string subtype in subTypesOfEntity[docEntity.Name])
					{
						sb.Append(ToOwlClass(subtype) + " ");
					}
				}
				sb.Append(")");
				sb.AppendLine();
				sb.AppendLine("\t\t] ;");
			}


			// attributes -> create properties and restrictions
			foreach (DocAttribute docAttr in docEntity.Attributes)
			{
				// check if Attr must be skipped (1) - not included attribute
				if (included != null)
					if (!included.ContainsKey(docAttr))
						continue;

				string propname = "";
				string propfullname = "";
				string invpropname = "";
				string invpropfullname = "";

				string targetString = docAttr.DefinedType;
				DocEntity targetEntity = null;
				if (map.ContainsKey(targetString))
				{
					targetEntity = map[targetString] as DocEntity;
				}

				// check if Attr must be skipped (2) - DERIVE attribute
				if (docAttr.Derived != null)
					continue;

				// check if Attr must be skipped (3) - not manageable INVERSE attribute
				invpropname = docAttr.Inverse;
				if (invpropname != null)
				{
					// check if there are problems with inverse and in this case skip the attribute!
					// 1) the inverse is an inverse of two or more properties
					// 2) a list/array is involved as range in the property or its inverse

					// 1)
					var key = new Tuple<string, string>(docAttr.Inverse, targetString);
					if (attribInverses.ContainsKey(key))
						if (attribInverses[key] > 1)
							continue;

					// 2.a)
					if (docAttr.GetAggregation() == DocAggregationEnum.LIST || docAttr.GetAggregation() == DocAggregationEnum.ARRAY)
						continue;
					// 2.b)
					if (targetEntity != null)
					{
						bool toBeSkipped = false;
						foreach (DocAttribute docAttrInv in targetEntity.Attributes)
						{
							if (docAttrInv.Name == invpropname)
								if (docAttrInv.GetAggregation() == DocAggregationEnum.LIST || docAttrInv.GetAggregation() == DocAggregationEnum.ARRAY)
								{
									toBeSkipped = true;
									break;
								}
						}
						if (toBeSkipped)
							continue;
					}
				}



				// set actual target
				string actualTargetString = ToOwlClass(targetString);
				if (docAttr.GetAggregation() == DocAggregationEnum.LIST || docAttr.GetAggregation() == DocAggregationEnum.ARRAY)
				{
					string newlistdef = createListClass(actualTargetString);
					actualTargetString = actualTargetString + "_List";
					if (newlistdef.Length > 0)
						sbLists.Append(newlistdef);

					DocAttribute docAggregate = docAttr.AggregationAttribute;
					while (docAggregate != null)
					{
						newlistdef = createListClass(actualTargetString);
						actualTargetString = actualTargetString + "_List";
						if (newlistdef.Length > 0) sbLists.Append(newlistdef);

						docAggregate = docAggregate.AggregationAttribute;
					}
				}

				// create property
				propname = docAttr.Name;
				propfullname = propname + "_" + docEntity.Name;
				if (propfullname.Length > 0) propfullname = char.ToLower(propfullname[0]) + propfullname.Substring(1);
				propfullname = "ifc:" + propfullname;
				sbProps.AppendLine(propfullname);
				sbProps.Append("\trdf:type \towl:ObjectProperty ");
				// functional
				if (docAttr.GetAggregation() == DocAggregationEnum.NONE ||
				   docAttr.GetAggregation() == DocAggregationEnum.LIST ||
				   docAttr.GetAggregation() == DocAggregationEnum.ARRAY ||
				   (docAttr.GetAggregation() == DocAggregationEnum.SET && docAttr.GetAggregationNestingUpper() == 1))
				{
					sbProps.Append(", owl:FunctionalProperty ");
				}
				sbProps.Append(";");
				sbProps.AppendLine();
				// inverse
				if (invpropname != null && targetEntity != null)
				{
					invpropfullname = invpropname + "_" + targetEntity.Name;
					if (invpropfullname.Length > 0) invpropfullname = char.ToLower(invpropfullname[0]) + invpropfullname.Substring(1);
					invpropfullname = "ifc:" + invpropfullname;
					sbProps.AppendLine("\towl:inverseOf \t" + invpropfullname + " ;");
				}
				// domain
				sbProps.AppendLine("\trdfs:domain " + ToOwlClass(docEntity.Name) + " ;");
				// range
				sbProps.AppendLine("\trdfs:range " + actualTargetString + " ;");
				// label
				sbProps.AppendLine("\trdfs:label  \"" + propname + "\" .");
				sbProps.AppendLine();


				// create restrictions
				{

					// only
					sb.AppendLine("\trdfs:subClassOf ");
					sb.AppendLine("\t\t[ ");
					sb.AppendLine("\t\t\trdf:type owl:Restriction ;");
					sb.AppendLine("\t\t\towl:allValuesFrom " + actualTargetString + " ;");
					sb.AppendLine("\t\t\towl:onProperty " + propfullname);
					sb.AppendLine("\t\t] ;");


					if (docAttr.GetAggregation() == DocAggregationEnum.NONE ||
					   docAttr.GetAggregation() == DocAggregationEnum.LIST ||
					   docAttr.GetAggregation() == DocAggregationEnum.ARRAY)
					{
						sb.AppendLine("\t" + "rdfs:subClassOf ");
						sb.AppendLine("\t\t" + "[");
						sb.AppendLine("\t\t\t" + "rdf:type owl:Restriction ;");
						if (docAttr.IsOptional)
							sb.AppendLine("\t\t\t" + "owl:maxQualifiedCardinality \"" + 1 + "\"^^xsd:nonNegativeInteger ;");
						else
							sb.AppendLine("\t\t\t" + "owl:qualifiedCardinality \"" + 1 + "\"^^xsd:nonNegativeInteger ;");
						sb.AppendLine("\t\t\towl:onProperty " + propfullname + " ;");
						sb.AppendLine("\t\t\t" + "owl:onClass " + actualTargetString);
						sb.AppendLine("\t\t] ;");
					}


					if (docAttr.GetAggregation() == DocAggregationEnum.SET)
					{
						int mincard = 0;
						if (docAttr.AggregationLower != null) mincard = Int32.Parse(docAttr.AggregationLower);
						int maxcard = 0;
						if (String.IsNullOrEmpty(docAttr.AggregationUpper) || !Int32.TryParse(docAttr.AggregationUpper, out maxcard))
							maxcard = 0;

						if (docAttr.IsOptional)
							mincard = 0;

						if (mincard == maxcard && mincard > 0)
						{
							sb.AppendLine("\t" + "rdfs:subClassOf ");
							sb.AppendLine("\t\t" + "[");
							sb.AppendLine("\t\t\t" + "rdf:type owl:Restriction ;");
							sb.AppendLine("\t\t\t" + "owl:qualifiedCardinality \"" + mincard + "\"^^xsd:nonNegativeInteger ;");
							sb.AppendLine("\t\t\towl:onProperty " + propfullname + " ;");
							sb.AppendLine("\t\t\t" + "owl:onClass " + actualTargetString);
							sb.AppendLine("\t\t] ;");
						}
						else
						{
							if (mincard > 0)
							{
								sb.AppendLine("\t" + "rdfs:subClassOf ");
								sb.AppendLine("\t\t" + "[");
								sb.AppendLine("\t\t\t" + "rdf:type owl:Restriction ;");
								sb.AppendLine("\t\t\t" + "owl:minQualifiedCardinality \"" + mincard + "\"^^xsd:nonNegativeInteger ;");
								sb.AppendLine("\t\t\towl:onProperty " + propfullname + " ;");
								sb.AppendLine("\t\t\t" + "owl:onClass " + actualTargetString);
								sb.AppendLine("\t\t] ;");
							}
							if (maxcard > 0)
							{
								sb.AppendLine("\t" + "rdfs:subClassOf ");
								sb.AppendLine("\t\t" + "[");
								sb.AppendLine("\t\t\t" + "rdf:type owl:Restriction ;");
								sb.AppendLine("\t\t\t" + "owl:maxQualifiedCardinality \"" + maxcard + "\"^^xsd:nonNegativeInteger ;");
								sb.AppendLine("\t\t\towl:onProperty " + propfullname + " ;");
								sb.AppendLine("\t\t\t" + "owl:onClass " + actualTargetString);
								sb.AppendLine("\t\t] ;");
							}
						}

					}

					if (docAttr.GetAggregation() == DocAggregationEnum.LIST ||
					   docAttr.GetAggregation() == DocAggregationEnum.ARRAY)
					{

						int mincard = 0;
						if (docAttr.AggregationLower != null) mincard = Int32.Parse(docAttr.AggregationLower);
						int maxcard = 0;
						if (String.IsNullOrEmpty(docAttr.AggregationUpper) || !Int32.TryParse(docAttr.AggregationUpper, out maxcard))
							maxcard = 0;


						if (docAttr.GetAggregation() == DocAggregationEnum.ARRAY)
						{
							mincard = maxcard - mincard + 1;
							maxcard = mincard;
						}

						if (mincard >= 1)
						{
							string cards = "";
							cards += WriteMinCardRestr(actualTargetString, propfullname, mincard, true);
							cards += " ;";
							sb.AppendLine(cards);
						}

						if (maxcard > 1)
						{
							string cards = "";
							string emptyListTgt = actualTargetString.Substring(0, actualTargetString.Length - 4) + "EmptyList";
							cards += WriteMaxCardRestr(emptyListTgt, propfullname, maxcard, true);
							cards += " ;";
							sb.AppendLine(cards);
						}
					}
				}
			}

			sb.AppendLine("\trdf:type \towl:Class .");
			sb.AppendLine();
			if (fullListing)
			{
				sb.Append(sbProps.ToString());
				sb.Append(sbLists.ToString());
			}

			return sb.ToString();
		}

		public string FormatEnumeration(DocEnumeration docEnumeration, Dictionary<string, DocObject> map, Dictionary<DocObject, bool> included)
		{
			return FormatEnumerationFull(docEnumeration, false);
		}

		// version for computer interpretable listing
		public string FormatEnumerationFull(DocEnumeration docEnumeration, bool fullListing)
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendLine("ifc:" + docEnumeration.Name);
			sb.AppendLine("\trdf:type owl:Class ;");
			if (!fullListing)
			{
				//possibly add the items in a oneof
				sb.AppendLine("\towl:equivalentClass");
				sb.AppendLine("\t\t[");
				sb.AppendLine("\t\t\trdf:type owl:Class ;");
				sb.AppendLine("\t\t\towl:oneOf ");
				sb.AppendLine("\t\t\t\t( ");
				foreach (DocConstant docConst in docEnumeration.Constants)
				{
					sb.AppendLine("\t\t\t\tifc:" + docConst.Name.ToUpper() + " ");
				}
				//close oneof
				sb.AppendLine("\t\t\t\t) ");
				sb.AppendLine("\t\t] ; ");
			}
			sb.AppendLine("\trdfs:subClassOf expr:ENUMERATION .");
			sb.AppendLine();

			// define individuals
			if (fullListing)
			{
				foreach (DocConstant docConst in docEnumeration.Constants)
				{
					string indivLocalUri = docConst.Name.ToUpper();
					if (!addedIndividuals.Contains(indivLocalUri))
					{
						addedIndividuals.Add(indivLocalUri);
						sb.AppendLine("ifc:" + indivLocalUri);
						sb.AppendLine("\trdf:type " + "ifc:" + docEnumeration.Name + " , owl:NamedIndividual ;");
						sb.AppendLine("\trdfs:label  \"" + docConst.Name.ToUpper() + "\" .");
						sb.AppendLine();
					}
					else
					{
						sb.AppendLine("ifc:" + indivLocalUri);
						sb.AppendLine("\trdf:type " + "ifc:" + docEnumeration.Name + " .");
						sb.AppendLine();
					}
				}
			}

			return sb.ToString();
		}

		public string FormatSelect(DocSelect docSelect, Dictionary<string, DocObject> map, Dictionary<DocObject, bool> included)
		{
			return FormatSelectFull(docSelect, map, included, false);
		}

		// version for computer interpretable listing
		public string FormatSelectFull(DocSelect docSelect, Dictionary<string, DocObject> map, Dictionary<DocObject, bool> included, bool fullListing)
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendLine("ifc:" + docSelect.Name);
			sb.AppendLine("\trdf:type owl:Class ;");

			if (!fullListing)
			{
				//possibly add the individuals here as a union
				sb.AppendLine("\towl:equivalentClass");
				sb.AppendLine("\t\t[");
				sb.AppendLine("\t\t\trdf:type owl:Class ;");
				sb.AppendLine("\t\t\towl:unionOf ");
				sb.AppendLine("\t\t\t\t( ");
				// entities
				foreach (DocSelectItem docItem in docSelect.Selects)
				{
					DocObject mapDef = null;
					if (map.TryGetValue(docItem.Name, out mapDef))
					{
						if (included == null || included.ContainsKey(mapDef))
						{
							sb.AppendLine("\t\t\t\t\tifc:" + docItem.Name + " ");
						}
					}
				}
				//close unionof
				sb.AppendLine("\t\t\t\t) ");
				sb.AppendLine("\t\t] ; ");
			}
			sb.AppendLine("\trdfs:subClassOf expr:SELECT .");
			sb.AppendLine();

			// add members of SELECT as subclasses
			if (fullListing)
			{
				foreach (DocSelectItem docItem in docSelect.Selects)
				{
					DocObject mapDef = null;
					if (map.TryGetValue(docItem.Name, out mapDef))
					{
						if (included == null || included.ContainsKey(mapDef))
						{
							sb.AppendLine("ifc:" + docItem.Name);
							sb.AppendLine("\trdfs:subClassOf ifc:" + docSelect.Name + " .");
							sb.AppendLine();
						}
					}
				}
			}
			return sb.ToString();
		}

		public static string ToOwlClass(string typename)
		{
			string defined = "ifc:" + typename;
			switch (typename)
			{
				case "BOOLEAN":
					defined = "expr:BOOLEAN";
					break;

				case "LOGICAL":
					defined = "expr:LOGICAL";
					break;

				case "INTEGER":
					defined = "expr:INTEGER";
					break;

				case "STRING":
					defined = "expr:STRING";
					break;

				case "REAL":
					defined = "expr:REAL";
					break;

				case "NUMBER":
					defined = "expr:NUMBER";
					break;

				case "BINARY":
					defined = "expr:BINARY";
					break;

				case "BINARY (32)":
					defined = "expr:BINARY";
					break;
			}

			return defined;
		}

		private string WriteMinCardRestr(string className, string attrName, int minCard, bool asEntity)
		{
			string output = "";
			if (!asEntity) output += " ;\r\n";
			output += "\trdfs:subClassOf" + "\r\n";

			string tab = "\t";

			if (asEntity == true)
			{
				output += "\t\t[" + "\r\n";
				output += "\t\t\trdf:type owl:Restriction ; " + "\r\n";
				output += "\t\t\towl:onProperty " + attrName + " ;\r\n";
				output += "\t\t\towl:allValuesFrom" + "\r\n";
				tab += "\t\t";
			}

			for (int i = 0; i <= minCard - 1; i++)
			{
				tab += "\t";
				output += tab + "[" + "\r\n";
				output += tab + "\trdf:type owl:Restriction ; " + "\r\n";
				output += tab + "\towl:onProperty list:hasNext ; " + "\r\n";
				output += tab + "\towl:someValuesFrom ";
				if (i != minCard - 1)
					output += "\r\n";
			}
			output += className + "\r\n";
			for (int i = 0; i <= minCard - 1; i++)
			{
				tab = tab.Substring(1);
				output += tab + "\t]";
				if (i != minCard - 1)
					output += "\r\n";
			}

			if (asEntity == true)
			{
				output += "\r\n\t\t]";
			}
			return output;
		}

		private string WriteMaxCardRestr(string className, string attrName, int maxCard, bool asEntity)
		{
			string output = "";
			if (!asEntity) output += " ;\r\n";
			output += "\trdfs:subClassOf" + "\r\n";

			string tab = "\t";
			if (asEntity == true)
			{
				output += "\t\t[" + "\r\n";
				output += "\t\t\trdf:type owl:Restriction ; " + "\r\n";
				output += "\t\t\towl:onProperty " + attrName + " ;\r\n";
				output += "\t\t\towl:allValuesFrom" + "\r\n";
				tab += "\t\t";
			}
			for (int i = 0; i < maxCard - 1; i++)
			{
				tab += "\t";
				output += tab + "[" + "\r\n";
				output += tab + "\trdf:type owl:Restriction ; " + "\r\n";
				output += tab + "\towl:onProperty list:hasNext ; " + "\r\n";
				output += tab + "\towl:allValuesFrom ";
				if (i != maxCard)
					output += "\r\n";
			}

			tab += "\t";
			output += tab + "[" + "\r\n";
			output += tab + "\trdf:type owl:Restriction ; " + "\r\n";
			output += tab + "\towl:onProperty list:hasNext ; " + "\r\n";
			output += tab + "\towl:onClass " + className + " ;" + "\r\n";
			output += tab + "\towl:qualifiedCardinality \"1\"^^xsd:nonNegativeInteger " + "\r\n";

			tab = tab.Substring(1);
			output += tab + "\t]" + "\r\n";

			for (int i = 0; i < maxCard - 1; i++)
			{
				tab = tab.Substring(1);
				output += tab + "\t]";
				if (i != maxCard)
					output += "\r\n";
			}
			if (asEntity == true)
				output += "\t\t]";

			return output;
		}

		public string FormatDefined(DocDefined docDefined, Dictionary<string, DocObject> map, Dictionary<DocObject, bool> included)
		{
			return FormatDefinedFull(docDefined, false);
		}

		public string FormatDefinedFull(DocDefined docDefined, bool fullListing)
		{
			StringBuilder sb = new StringBuilder();

			string defined = ToOwlClass(docDefined.DefinedType);

			if (docDefined.Aggregation != null)
			{
				string aggtype = docDefined.Aggregation.GetAggregation().ToString().ToLower();

				if (docDefined.Aggregation.GetAggregation() == DocAggregationEnum.SET)
				{
					//IfcPropertySetDefinitionSet

					sb.AppendLine("ifc:" + docDefined.Name);
					sb.AppendLine("\trdf:type owl:Class ;");

					//TODO:: Add (SELECT) parent classes  

					sb.AppendLine("\trdfs:subClassOf ");
					sb.AppendLine("\t\t[ ");
					sb.AppendLine("\t\t\trdf:type owl:Restriction ;");
					sb.AppendLine("\t\t\towl:allValuesFrom " + defined + " ;");
					sb.AppendLine("\t\t\towl:onProperty expr:hasSet");
					sb.AppendLine("\t\t] ;");
					sb.AppendLine("\t" + "rdfs:subClassOf ");
					sb.AppendLine("\t\t" + "[");
					sb.AppendLine("\t\t\t" + "rdf:type owl:Restriction ;");
					sb.AppendLine("\t\t\t" + "owl:minQualifiedCardinality \"" + 1
						+ "\"^^xsd:nonNegativeInteger ;");
					sb.AppendLine("\t\t\towl:onProperty expr:hasSet ;");
					sb.AppendLine("\t\t\t" + "owl:onClass " + defined);
					sb.AppendLine("\t\t] .");
					sb.AppendLine();
				}
				else if (docDefined.Aggregation.GetAggregation() == DocAggregationEnum.LIST || docDefined.Aggregation.GetAggregation() == DocAggregationEnum.ARRAY)
				{

					//Console.Out.WriteLine("defined type --" + docDefined.Aggregation.GetAggregation() + "-- : " + docDefined.Name);

					int mincard = 0;
					if (docDefined.Aggregation.AggregationLower != null)
						mincard = Int32.Parse(docDefined.Aggregation.AggregationLower);
					int maxcard = 0;
					if (String.IsNullOrEmpty(docDefined.Aggregation.AggregationUpper) ||
					   !Int32.TryParse(docDefined.Aggregation.AggregationUpper, out maxcard))
						maxcard = 0;

					if (docDefined.Aggregation.GetAggregation() == DocAggregationEnum.ARRAY)
					{
						mincard = maxcard - mincard + 1;
						maxcard = mincard;
					}

					sb.AppendLine("ifc:" + docDefined.Name);
					sb.AppendLine("\trdf:type owl:Class ;");
					sb.Append("\trdfs:subClassOf " + defined + "_List ");


					//check for cardinality restrictions and add if available
					string cards = "";
					if (docDefined.Aggregation.GetAggregationNestingLower() >= 1)
						cards += WriteMinCardRestr(defined + "_List", "hasNext", mincard, false);
					if (docDefined.Aggregation.GetAggregationNestingUpper() > 1)
						cards += WriteMaxCardRestr(defined + "_EmptyList", "hasNext", maxcard, false);
					cards += ".";
					sb.AppendLine(cards);
					sb.AppendLine();

					if (fullListing)
					{
						sb.AppendLine(createListClass(defined));
					}
				}
			}
			else
			{
				sb.AppendLine("ifc:" + docDefined.Name);
				sb.AppendLine("\trdf:type owl:Class ;");

				sb.AppendLine("\trdfs:subClassOf " + defined + " .");
				sb.AppendLine();

				if (docDefined.Length > 0 || docDefined.Length < 0 || docDefined.Aggregation != null)
				{
					//TODO: possibly add restrictions here

					//TYPE IfcGloballyUniqueId = STRING(22) FIXED;
					//END_TYPE;

					//TYPE IfcIdentifier = STRING(255);
					//END_TYPE;

					//TYPE IfcLabel = STRING(255);
					//END_TYPE;
				}
			}

			return sb.ToString();
		}

		public string createListClass(string defined)
		{
			StringBuilder sb = new StringBuilder();

			if (!listPropertiesOutput.Contains(defined) && defined.StartsWith("ifc"))
			{
				listPropertiesOutput.Add(defined);

				sb.AppendLine(defined + "_EmptyList");
				sb.AppendLine("\trdf:type owl:Class ;");
				sb.AppendLine("\trdfs:subClassOf list:EmptyList, " + defined + "_List" + " .");
				sb.AppendLine();

				sb.AppendLine(defined + "_List");
				sb.AppendLine("\trdf:type owl:Class ;");
				sb.AppendLine("\trdfs:subClassOf list:OWLList ;");
				sb.AppendLine("\trdfs:subClassOf");
				sb.AppendLine("\t\t[");
				sb.AppendLine("\t\t\trdf:type owl:Restriction ;");
				sb.AppendLine("\t\t\towl:onProperty list:hasContents ;");
				sb.AppendLine("\t\t\towl:allValuesFrom " + defined);
				sb.AppendLine("\t\t] ;");
				sb.AppendLine("\trdfs:subClassOf");
				sb.AppendLine("\t\t[");
				sb.AppendLine("\t\t\trdf:type owl:Restriction ;");
				sb.AppendLine("\t\t\towl:onProperty list:isFollowedBy ;");
				sb.AppendLine("\t\t\towl:allValuesFrom " + defined + "_List");
				sb.AppendLine("\t\t] ;");
				sb.AppendLine("\trdfs:subClassOf");
				sb.AppendLine("\t\t[");
				sb.AppendLine("\t\t\trdf:type owl:Restriction ;");
				sb.AppendLine("\t\t\towl:onProperty list:hasNext ;");
				sb.AppendLine("\t\t\towl:allValuesFrom " + defined + "_List");
				sb.AppendLine("\t\t] .");
				sb.AppendLine();
			}

			return sb.ToString();
		}

		public string FormatDefinitions(DocProject docProject, DocPublication docPublication, Dictionary<string, DocObject> map, Dictionary<DocObject, bool> included)
		{
			// clear containers
			listPropertiesOutput.Clear();
			addedIndividuals.Clear();
			attribInverses.Clear();
			subTypesOfEntity.Clear();

			StringBuilder sb = new StringBuilder();

			string ifcversion = docProject.GetSchemaIdentifier();

			//string ifcns = "http://www.buildingsmart-tech.org/ifcOWL/" + ifcversion;
			//string ifcns = "http://ifcowl.openbimstandards.org/" + ifcversion;
			string ifcns = "http://www.buildingsmart-tech.org/ifc/" + ifcversion;

			// namespace definitions
			sb.AppendLine("@prefix :      <" + ifcns + "#> .");
			sb.AppendLine("@prefix rdfs:  <http://www.w3.org/2000/01/rdf-schema#> .");
			sb.AppendLine("@prefix dce:   <http://purl.org/dc/elements/1.1/> .");
			sb.AppendLine("@prefix owl:   <http://www.w3.org/2002/07/owl#> .");
			sb.AppendLine("@prefix xsd:   <http://www.w3.org/2001/XMLSchema#> .");
			sb.AppendLine("@prefix rdf:   <http://www.w3.org/1999/02/22-rdf-syntax-ns#> .");
			sb.AppendLine("@prefix vann:  <http://purl.org/vocab/vann/> .");
			sb.AppendLine("@prefix list:  <https://w3id.org/list#> .");
			sb.AppendLine("@prefix expr:  <https://w3id.org/express#> .");
			sb.AppendLine("@prefix ifc:   <" + ifcns + "#> .");
			sb.AppendLine("@prefix cc:    <http://creativecommons.org/ns#> .");
			sb.AppendLine("");

			// ontology definition
			sb.AppendLine("<" + ifcns + ">");
			sb.AppendLine("\ta                              owl:Ontology ;");
			sb.AppendLine("\trdfs:comment                   \"Ontology automatically generated from the EXPRESS schema using the IfcDoc functions developed by Pieter Pauwels (pipauwel.pauwels@ugent.be) and Walter Terkaj (walter.terkaj@itia.cnr.it) \" ;");
			sb.AppendLine("\tcc:license                     <http://creativecommons.org/licenses/by/3.0/> ;");
			sb.AppendLine("\tdce:contributor                \"Jakob Beetz (j.beetz@tue.nl)\" , \"Maria Poveda Villalon (mpoveda@fi.upm.es)\" ;"); // \"Aleksandra Sojic (aleksandra.sojic@itia.cnr.it)\" , 
			sb.AppendLine("\tdce:creator                    \"Pieter Pauwels (pipauwel.pauwels@ugent.be)\" , \"Walter Terkaj  (walter.terkaj@itia.cnr.it)\" ;");
			sb.AppendLine("\tdce:date                       \"2015/10/02\" ;");
			sb.AppendLine("\tdce:description                \"OWL ontology for the IFC conceptual data schema and exchange file format for Building Information Model (BIM) data\" ;");
			sb.AppendLine("\tdce:identifier                 \"" + ifcversion + "\" ;");
			sb.AppendLine("\tdce:language                   \"en\" ;");
			sb.AppendLine("\tdce:title                      \"" + ifcversion + "\" ;");
			sb.AppendLine("\tvann:preferredNamespacePrefix  \"ifc\" ;");
			sb.AppendLine("\tvann:preferredNamespaceUri     \"" + ifcns + "\" ;");
			sb.AppendLine("\towl:imports                    <https://w3id.org/express> .");
			sb.AppendLine();

			// check which Inverse Attributes must be discarded because of conflicts
			// get subtypes of an entity
			foreach (DocSection docSection in docProject.Sections)
			{
				foreach (DocSchema docSchema in docSection.Schemas)
				{
					foreach (DocEntity docEntity in docSchema.Entities)
					{
						// get supertype/subtype
						if (docEntity.BaseDefinition != null)
						{
							if (!subTypesOfEntity.ContainsKey(docEntity.BaseDefinition))
								subTypesOfEntity.Add(docEntity.BaseDefinition, new HashSet<string>());
							subTypesOfEntity[docEntity.BaseDefinition].Add(docEntity.Name);
						}

						// check attributes
						foreach (DocAttribute docAttr in docEntity.Attributes)
						{
							if (docAttr.Inverse != null)
							{
								var key = new Tuple<string, string>(docAttr.Inverse, docAttr.DefinedType);
								if (!attribInverses.ContainsKey(key))
									attribInverses.Add(key, 1);
								else
									attribInverses[key] += 1;
							}
						}
					}
				}
			}

			// generate definitions
			foreach (DocSection docSection in docProject.Sections)
			{
				foreach (DocSchema docSchema in docSection.Schemas)
				{
					foreach (DocType docType in docSchema.Types)
					{
						if (included == null || included.ContainsKey(docType))
						{
							if (docType is DocDefined)
							{
								DocDefined docDefined = (DocDefined)docType;
								string text = this.FormatDefinedFull(docDefined, true);
								sb.Append(text);
							}
							else if (docType is DocSelect)
							{
								DocSelect docSelect = (DocSelect)docType;
								string text = this.FormatSelectFull(docSelect, map, included, true);
								sb.Append(text);
							}
							else if (docType is DocEnumeration)
							{
								DocEnumeration docEnumeration = (DocEnumeration)docType;
								string text = this.FormatEnumerationFull(docEnumeration, true);
								sb.Append(text);
							}
						}
					}

					foreach (DocEntity docEntity in docSchema.Entities)
					{
						if (included == null || included.ContainsKey(docEntity))
						{
							string text = this.FormatEntityFull(docEntity, map, included, true);
							sb.Append(text);
						}
					}
				}

			}
			listPropertiesOutput.Clear();
			return sb.ToString();
		}
	}
}
