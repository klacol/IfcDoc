using BuildingSmart.Serialization.Xml;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Ifc2Json
{
    public class Product : XmlSerializer
    {
        ArrayList products = new ArrayList();//存储构件与其对应的属性实例
        protected internal class ProductProperties
        {
            public string Type { get; set; }//构件的类型
            public string Guid { get; set; }//构件的id
            public bool IsBuildingStorey { get; set; }//一标志判断获取的stroey是否是楼层
            public Dictionary<string, Dictionary<string, string>> properties { get; set; }
        }

        public HashSet<object> elements = new HashSet<object>();//保存物理构件
        public HashSet<object> spatialElements = new HashSet<object>();//保存空间构件ifspace、ifcbuilding等
        public Product(Type typeProject) : base(typeProject)
        {
        }
        //写json
        public void WriteJson(Stream stream, object root)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            if (root == null)
                throw new ArgumentNullException("root");
            StreamWriter writer = new StreamWriter(stream);
            GetProductAndProperties(root);
            this.WriteHeader(writer);//写头部ifc:]
            int count = 0;
            foreach (ProductProperties e in products)
            {
                count++;
                string json = JsonConvert.SerializeObject(e, Newtonsoft.Json.Formatting.Indented);
                writer.WriteLine(json);
                if (count != products.Count)
                {
                    writer.WriteLine(",");//最后一个对象不输出，
                }

            }
            this.WriteFooter(writer);
            writer.Flush();
        }
        //获取构件及其构件属性
        public void GetProductAndProperties(object root)
        {
            TraverseProject(root);//遍历内部结构获取构件集
            //获取空间结构的属性信息（先输出空间结构的，构件的位置信息会用到此）
            foreach (object e in spatialElements)
            {
                //空间结构还有几何表达信息（此处还未添加）
                //创建一对 
                ProductProperties p = new ProductProperties();
                Dictionary<string, Dictionary<string, string>> entityProperties = new Dictionary<string, Dictionary<string, string>>();
                Dictionary<string, string> BasicProperties = new Dictionary<string, string>();

                GetDirectFieldsValue(e, BasicProperties);
                string guid; string type = e.GetType().Name;
                if (BasicProperties.TryGetValue("GlobalId", out guid))
                {
                    p.Guid = guid;
                }
                p.Type = type;
                entityProperties.Add("基本属性", BasicProperties);
                p.properties = entityProperties;
                products.Add(p);
            }
            Console.WriteLine("spatialElements结束");
            //物理构件
            foreach (object e in elements)
            {
                //空间结构还有几何表达信息（此处还未添加）
                //创建一对 
                ProductProperties p = new ProductProperties();
                Dictionary<string, Dictionary<string, string>> entityProperties = new Dictionary<string, Dictionary<string, string>>();
                Dictionary<string, string> BasicProperties = new Dictionary<string, string>();

                GetDirectFieldsValue(e, BasicProperties);
                string guid;string type=e.GetType().Name;
                if (BasicProperties.TryGetValue("GlobalId", out guid))
                {
                    p.Guid = guid;
                }
                p.Type = type;
                entityProperties.Add("基本属性", BasicProperties);
                p.properties = entityProperties;
                products.Add(p);
            }
            Console.WriteLine("结束");
        }
        //获取实体的直接属性的key-value值
        protected void GetDirectFieldsValue(object e, Dictionary<string, string> Specific)
        {
            //只获取基本属性实体的直接属性
            Type t = e.GetType(), stringType = typeof(String);
            //实体名称
            //Specific.Add("IfcEntity", t.Name);
            IList<PropertyInfo> fields = this.GetFieldsAll(t);
            foreach (PropertyInfo f in fields)
            {
                //获取属性对应的值
                string key = f.Name;
                string v = "";
                GetPropertyInfoValue(e, f, ref v);
                //过滤出值""
                if (v == "")
                {
                    continue;
                }
                else
                {
                    Specific.Add(key, v);
                }
            }
        }
        //根据属性PropertyInfo获取其对应的值只讨论直接属性
        protected void GetPropertyInfoValue(object e, PropertyInfo f, ref string v)
        {
            Type t = e.GetType(), stringType = typeof(String);
            if (f != null) // derived fields are null
            {
                DocXsdFormatEnum? xsdformat = this.GetXsdFormat(f);
                Type ft = f.PropertyType, valueType = null;
                DataMemberAttribute dataMemberAttribute = null;
                object value = GetSerializeValue(e, f, out dataMemberAttribute, out valueType);
                if (value != null)
                {
                    if (dataMemberAttribute != null && (xsdformat == null || xsdformat == DocXsdFormatEnum.Attribute))
                    {
                        // direct field
                        bool isvaluelist = IsValueCollection(ft);
                        bool isvaluelistlist = ft.IsGenericType && // e.g. IfcTriangulatedFaceSet.Normals
                            typeof(System.Collections.IEnumerable).IsAssignableFrom(ft.GetGenericTypeDefinition()) &&
                            IsValueCollection(ft.GetGenericArguments()[0]);
                        if (isvaluelistlist || isvaluelist || ft.IsValueType || ft == stringType)
                        {
                            //string key = f.Name;
                            if (ft == stringType && string.IsNullOrEmpty(value.ToString()))
                                return;
                            if (isvaluelistlist)
                            {
                                ft = ft.GetGenericArguments()[0].GetGenericArguments()[0];
                                PropertyInfo fieldValue = ft.GetProperty("Value");
                                if (fieldValue != null)
                                {    
                                    string va = "";
                                    System.Collections.IList list = (System.Collections.IList)value;
                                    for (int i = 0; i < list.Count; i++)
                                    {
                                        System.Collections.IList listInner = (System.Collections.IList)list[i];
                                        for (int j = 0; j < listInner.Count; j++)
                                        {
                                            if (i > 0 || j > 0)
                                            {
                                                v = v + " ";//加空格
                                            }
                                            object elem = listInner[j];
                                            if (elem != null) // should never be null, but be safe
                                            {
                                                elem = fieldValue.GetValue(elem);
                                                string encodedvalue = System.Security.SecurityElement.Escape(elem.ToString());
                                                // 对Json字符串中回车符处理;
                                                va = this.strToJson(encodedvalue);
                                            }
                                        }
                                        v = v + va;
                                    }
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine("XXX Error serializing ValueListlist" + e.ToString());
                                }
                            }
                            else if (isvaluelist)
                            {
                                ft = ft.GetGenericArguments()[0];
                                PropertyInfo fieldValue = ft.GetProperty("Value");

                                IEnumerable list = (IEnumerable)value;
                                int i = 0;
                                string va = "";
                                foreach (object o in list)
                                {
                                    if (e != null) // should never be null, but be safe
                                    {
                                        object elem = e;
                                        if (fieldValue != null)
                                        {
                                            elem = fieldValue.GetValue(e);
                                        }
                                        if (elem is byte[])
                                        {
                                            // IfcPixelTexture.Pixels
                                            va = SerializeBytes((byte[])elem);                                           
                                        }
                                        else
                                        {
                                            string encodedvalue = System.Security.SecurityElement.Escape(elem.ToString());
                                            // 对Json字符串中回车符处理
                                            va = this.strToJson(encodedvalue);
                                        }
                                    }
                                    if (i == 0)
                                    {
                                        v = va;
                                    }
                                    v = v+ " "+va;
                                }
                            }
                            else
                            {
                                if (ft.IsGenericType && ft.GetGenericTypeDefinition() == typeof(Nullable<>))
                                {
                                    // special case for Nullable types
                                    ft = ft.GetGenericArguments()[0];
                                }
                                Type typewrap = null;
                                while (ft.IsValueType && !ft.IsPrimitive)
                                {
                                    PropertyInfo fieldValue = ft.GetProperty("Value");
                                    if (fieldValue != null)
                                    {
                                        value = fieldValue.GetValue(value);
                                        if (typewrap == null)
                                        {
                                            typewrap = ft;
                                        }
                                        ft = fieldValue.PropertyType;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }

                                if (ft.IsEnum || ft == typeof(bool))
                                {
                                    value = value.ToString().ToLowerInvariant();
                                }

                                if (value is IList)
                                {
                                    // IfcCompoundPlaneAngleMeasure
                                    IList list = (IList)value;
                                    string va = "";
                                    for (int i = 0; i < list.Count; i++)
                                    {
                                        object elem = list[i];
                                      
                                        if (elem != null) // should never be null, but be safe
                                        {
                                            string encodedvalue = System.Security.SecurityElement.Escape(elem.ToString());
                                            // 对Json字符串中回车符处理
                                          //  v = this.strToJson(encodedvalue);//eg:(39,54,57,601318)
                                          if (i == 0)
                                          {
                                              va =  this.strToJson(encodedvalue);
                                          }
                                          va = va+ " "+ this.strToJson(encodedvalue) ;
                                        }
                                    }
                                    v = va;
                                }
                                else if (value != null)
                                {
                                    string encodedvalue = System.Security.SecurityElement.Escape(value.ToString());
                                    v = this.strToJson(encodedvalue);
                                }
                            }
                        }
                    }
                }
            }
        }
        //遍历project
        public void TraverseProject(object root)
        {
            if (root == null)
                throw new ArgumentNullException("root");

            Queue<object> queue = new Queue<object>();//存储反转属性实体当作根（递归遍历）
            HashSet<object> saved = new HashSet<object>();//保存之前遇到的实体，不重复遍历
            queue.Enqueue(root);

            DateTime startT = DateTime.Now;
            while (queue.Count > 0)
            {
                object ent = queue.Dequeue();
                if (!saved.Contains(ent))
                {
                    this.TraverseEntity(ent, saved, queue);//遍历当前实体
                }
            }
            DateTime endT = DateTime.Now;
            TimeSpan ts = endT - startT;
            Console.WriteLine("遍历project内部结构所需的时间：   {0}秒！\r\n", ts.TotalSeconds.ToString("0.00"));
            Console.WriteLine("总共实体（去除几何表达）的个数:" + saved.Count);
            Console.WriteLine("空间实体的个数:" + spatialElements.Count);
            Console.WriteLine("构件实体的个数:" + elements.Count);
        }
        /// <summary>
        /// 遍历过程中将物件实体和空间实体存储
        /// </summary>
        /// <param name="o"></param>
        /// <param name="saved"></param>
        /// <param name="queue"></param>
        public Boolean TraverseEntity(object o, HashSet<object> saved, Queue<object> queue)
        {
            if (o == null)
                return false;
            //存储
            EntityClassify(o);
            Type t = o.GetType();
            //不遍历几何信息实体（节省遍历时间）
            if (t.Name == "IfcLocalPlacement" || t.Name == "IfcPolyline" || t.Name == "IfcShapeRepresentation" || t.Name == "IfcExtrudedAreaSolid" || t.Name == "IfcIShapeProfileDef" ||
               t.Name == "IfcProductDefinitionShape" || t.Name == "IfcGeometricRepresentationSubContext" || t.Name == "IfcFacetedBrep" || t.Name == "IfcClosedShell" || t.Name == "IfcFace" ||
              t.Name == "IfcFaceOuterBound" || t.Name == "IfcPolyLoop" || t.Name == "IfcProductDefinitionShape" || t.Name == "IfcCompositeCurveSegment" || t.Name == "IfcRelSpaceBoundary")
            //  || t.Name == "IfcRelSpaceBoundary" || t.Name == "" || t.Name == "" || t.Name == ""
            {
                return true;
            }

            this.TraverseEntityAttributes(o, saved, queue);
            return true;
        }
        //遍历实体属性
        public void TraverseEntityAttributes(object o, HashSet<object> saved, Queue<object> queue)
        {
            Type t = o.GetType();
            // mark as saved
            if (saved.Contains(o))
            {
                return;
            }
            saved.Add(o);
            IList<PropertyInfo> fields = this.GetFieldsAll(t);//获取其所有属性
            //直接属性不获取其具体的值，其不包含实体
            List<Tuple<PropertyInfo, DataMemberAttribute, object>> elementFields = new List<Tuple<PropertyInfo, DataMemberAttribute, object>>();//反转属性和导出属性
            //获取其反转属性和导出
            foreach (PropertyInfo f in fields)
            {
                if (f != null) // derived fields are null
                {
                    DocXsdFormatEnum? xsdformat = this.GetXsdFormat(f);

                    Type ft = f.PropertyType, valueType = null;
                    DataMemberAttribute dataMemberAttribute = null;
                    object value = GetSerializeValue(o, f, out dataMemberAttribute, out valueType);
                    if (value == null)
                        continue;
                    if (!IsDirectField(f, o))
                    {
                        elementFields.Add(new Tuple<PropertyInfo, DataMemberAttribute, object>(f, dataMemberAttribute, value));
                    }
                }
            }
            if (elementFields.Count > 0)
            {
                // write direct object references and lists
                foreach (Tuple<PropertyInfo, DataMemberAttribute, object> tuple in elementFields) // derived attributes are null
                {
                    PropertyInfo f = tuple.Item1;
                    Type ft = f.PropertyType;
                    bool isvaluelist = IsValueCollection(ft);
                    bool isvaluelistlist = ft.IsGenericType && // e.g. IfcTriangulatedFaceSet.Normals
                        typeof(IEnumerable).IsAssignableFrom(ft.GetGenericTypeDefinition()) &&
                        IsValueCollection(ft.GetGenericArguments()[0]);
                    DataMemberAttribute dataMemberAttribute = tuple.Item2;
                    object value = tuple.Item3;
                    DocXsdFormatEnum? format = GetXsdFormat(f);
                    if (format == DocXsdFormatEnum.Element)
                    {
                        bool showit = true; //...check: always include tag if Attribute (even if zero); hide if Element 
                        IEnumerable ienumerable = value as IEnumerable;
                        if (ienumerable == null)
                        {
                            if (!ft.IsValueType && !isvaluelist && !isvaluelistlist)
                            {
                                TraverseEntity(value, saved, queue);
                                continue;
                            }
                        }
                        // for collection is must be non-zero (e.g. IfcProject.IsNestedBy)
                        else // what about IfcProject.RepresentationContexts if empty? include???
                        {
                            showit = false;
                            foreach (object check in ienumerable)
                            {
                                showit = true; // has at least one element
                                break;
                            }
                        }
                        if (showit)
                        {
                            TraverseAttributes(o, f, saved, queue);
                        }
                    }
                    else if (dataMemberAttribute != null)
                    {
                        // hide fields where inverse attribute used instead
                        if (!ft.IsValueType && !isvaluelist && !isvaluelistlist)
                        {
                            if (value != null)
                            {
                                IEnumerable ienumerable = value as IEnumerable;
                                if (ienumerable == null)
                                {
                                    string fieldName = PropertySerializeName(f), fieldTypeName = TypeSerializeName(ft);
                                    if (string.Compare(fieldName, fieldTypeName) == 0)
                                    {
                                        TraverseEntity(value, saved, queue);
                                        continue;
                                    }
                                }
                                bool showit = true;

                                if (!f.IsDefined(typeof(System.ComponentModel.DataAnnotations.RequiredAttribute), false) && ienumerable != null)
                                {
                                    showit = false;
                                    foreach (object sub in ienumerable)
                                    {
                                        showit = true;
                                        break;
                                    }
                                }
                                if (showit)
                                {
                                    TraverseAttributes(o, f, saved, queue);
                                }
                            }
                        }
                    }
                    else
                    {
                        // inverse
                        // record it for downstream serialization
                        if (value is IEnumerable)
                        {
                            IEnumerable invlist = (IEnumerable)value;
                            foreach (object invobj in invlist)
                            {
                                if (!saved.Contains(invobj))
                                {
                                    queue.Enqueue(invobj);
                                }
                            }
                        }
                    }
                }
            }
            IEnumerable enumerable = o as IEnumerable;
            if (enumerable != null)
            {
                foreach (object obj in enumerable)
                    TraverseEntity(obj, saved, queue);
            }
        }
        public void TraverseAttributes(object o, PropertyInfo f, HashSet<object> saved, Queue<object> queue)
        {
            object v = f.GetValue(o);
            if (v == null)
                return;
            string memberName = PropertySerializeName(f);
            Type objectType = o.GetType();
            string typeName = TypeSerializeName(o.GetType());
            if (string.Compare(memberName, typeName) == 0)
            {
                TraverseEntity(v, saved, queue);
                return;
            }

            Type ft = f.PropertyType;
            PropertyInfo fieldValue = null;
            if (ft.IsValueType)
            {
                if (ft == typeof(DateTime)) // header datetime
                {
                    return;
                }
                fieldValue = ft.GetProperty("Value"); // if it exists for value type
            }
            else if ((ft == typeof(string))|| (ft == typeof(byte[])))
            {
                return;
            }
            DocXsdFormatEnum? format = GetXsdFormat(f);
            if (IsEntityCollection(ft))
            {
                IEnumerable list = (IEnumerable)v;
                // for nested lists, flatten; e.g. IfcBSplineSurfaceWithKnots.ControlPointList
                if (typeof(IEnumerable).IsAssignableFrom(ft.GetGenericArguments()[0]))
                {
                    // special case
                    if (f.Name.Equals("InnerCoordIndices")) //hack
                    {
                        return;
                    }
                    ArrayList flatlist = new ArrayList();
                    foreach (IEnumerable innerlist in list)
                    {
                        foreach (object e in innerlist)
                        {
                            flatlist.Add(e);
                        }
                    }
                    list = flatlist;
                }
                foreach (object e in list)
                {
                    if (e != null) // could be null if buggy file -- not matching schema
                    {
                        if (!e.GetType().IsValueType && !(e is string)) // presumes an entity
                        {
                            if (format != null && format == DocXsdFormatEnum.Attribute)
                            {
                                // only one item, e.g. StyledByItem\IfcStyledItem
                                TraverseEntityAttributes(e, saved, queue);
                                break; // if more items, skip them -- buggy input data; no way to encode
                            }
                            else
                            {
                                TraverseEntity(e, saved, queue);
                            }
                        }
                    }
                }
            } // otherwise if not collection...
            else if (ft.IsInterface && v is ValueType)//Type 元数据中函数有IsInterface
            {//eg:IfcValue
                return; 
            }
            else if (fieldValue != null) // must be IfcBinary -- but not DateTime or other raw primitives
            {
                v = fieldValue.GetValue(v);
            }
            else 
            {
                if (format != null && format == DocXsdFormatEnum.Attribute)
                {
                    TraverseEntityAttributes(v,saved, queue);
                }
                else
                {
                    // if rooted, then check if we need to use reference; otherwise embed
                    TraverseEntity(v, saved, queue);
                }
            }
        }
        //判断以属性是否是直接属性
        public Boolean IsDirectField(PropertyInfo f,object o)
        {
            DocXsdFormatEnum? xsdformat = this.GetXsdFormat(f);

            Type ft = f.PropertyType, valueType = null;
            DataMemberAttribute dataMemberAttribute = null;
            object value = GetSerializeValue(o, f, out dataMemberAttribute, out valueType);
            //if (value == null)
            //    return false;
            if (dataMemberAttribute != null && (xsdformat == null || xsdformat == DocXsdFormatEnum.Attribute))
            {
                // direct field
                bool isvaluelist = IsValueCollection(ft);
                bool isvaluelistlist = ft.IsGenericType && // e.g. IfcTriangulatedFaceSet.Normals
                    typeof(System.Collections.IEnumerable).IsAssignableFrom(ft.GetGenericTypeDefinition()) &&
                    IsValueCollection(ft.GetGenericArguments()[0]);

                if (isvaluelistlist || isvaluelist || ft.IsValueType || ft == typeof(String))
                {
                    return true;
                }
                else
                {
                    return false;//导出属性
                }
            }
            else
            {
                return false;//反转属性
            }
        }
        //获取空间结构实体集和构件结构实体集
        public void EntityClassify(object e)
        {
            Type t = e.GetType();
            if (Basetype(t))
            {
                elements.Add(e);
            }
            else if (t.BaseType.Name == "IfcSpatialStructureElement")
            {
                spatialElements.Add(e);  //将空间实体和物理实体存储至Element中
            }
        }
        //判断物体类型是否为ifcElement，墙、门等构件的定义都是此基类
        public bool Basetype(Type t)
        {
            int i = 0;
            while (t != null)
            {
                if (i > 3)
                {
                    return false;
                }
                else
                {
                    if (t.Name == "IfcElement")
                    {
                        return true;
                    }
                    else
                    {
                        t = t.BaseType; //BaseType(t)
                        i++;
                    }
                }

            }
            return false;
        }
        // JSON字符串中回车符的处理
        protected override void WriteHeader(StreamWriter writer)
        {
            writer.WriteLine("{");
            this.WriteIndent(writer, 1);
            writer.WriteLine("\"ifc\": [");
        }
        protected override void WriteFooter(StreamWriter writer)
        {
            writer.WriteLine("  ]");
            writer.WriteLine("}");
        }
        protected string strToJson(string str)
        {
            return str.Replace("\n", "\\n").Replace("\r", "").Replace("\\", "\\\\");
        }
    }
}
