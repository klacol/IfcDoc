using BuildingSmart.Serialization.Xml;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Ifc2Json
{
    public class Product : XmlSerializer
    {
        public HashSet<object> elements = new HashSet<object>();//保存物理构件
        public HashSet<object> spatialElements = new HashSet<object>();//保存空间构件ifspace、ifcbuilding等
        public Product(Type typeProject) : base(typeProject)
        {
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
        protected string strToJson(string str)
        {
            return str.Replace("\n", "\\n").Replace("\r", "").Replace("\\", "\\\\");
        }
    }
}
