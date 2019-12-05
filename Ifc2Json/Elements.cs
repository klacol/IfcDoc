using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Ifc2Json
{
    class Elements : Basic
    {
        public ArrayList units = new ArrayList();
        Dictionary<string, string> unitSymbol = new Dictionary<string, string>();//将单位与其symbol另存储，方便在属性值后加单位时查找
        public float lengthUnit;//ifc中定义的长度单位
        public Elements(Type typeProject) : base(typeProject)
        {
        }
        protected internal class ProductProperties
        {
            public string Type { get; set; }//构件的类型
            public string Guid { get; set; }//构件的id            
            public string TypePropertyId { get; set; }  //构件的类型属性的id                                       
            public Dictionary<string, object> properties { get; set; }
        }
        //单位的结构
        protected internal class Unit
        {
            public string UnitType { get; set; }
            public string Prefix { get; set; }
            public string Name { get; set; }
            public string Symbol { get; set; }

        }
        //项目的结构
        protected internal class Project
        {
            public string Type { get; set; }//项目的实体名称
            public string Guid { get; set; }//项目的 guid 
            public object properties { get; set; }//项目的属性
        }

        //获取属性集，//其属性在构件的属性集IsDefinedBy
        public void GetpropertyEntities(object o, HashSet<object> RelPropertyEntities, HashSet<object> TypePropertyEntities)
        {
            Type t = o.GetType();
            PropertyInfo f = t.GetProperty("IsDefinedBy");
            object v = f.GetValue(o);//获取其属性值
            Type ft = f.PropertyType;
            if (IsEntityCollection(ft))
            {
                IEnumerable list = (IEnumerable)v;
                foreach (object invobj in list)
                {
                    //IsDefinedBy:SET OF IfcRelDefines FOR RelatedObjects;
                    Type type = invobj.GetType();
                    if (type.Name == "IfcRelDefinesByProperties")//属性信息
                    {
                        RelPropertyEntities.Add(invobj);
                    }
                    else if (type.Name == "IfcRelDefinesByType")//类型的属性信息
                    {
                        TypePropertyEntities.Add(invobj);
                    }
                    else
                    {
                        Console.WriteLine(t.Name + "该属性还有其他实体类型表达" + type.Name);//输出该构件名称
                    }
                }
            }
        }
        //获取与构件的关系实体中的PropertySet
        protected void GetRelPropertyEntitiesValue(HashSet<object> RelEntities, Dictionary<string, object> entityProperties)
        {
            //RelatingPropertyDefinition: IfcPropertySetDefinition;
            //每个IfcRelDefinesByProperties中包含了一个IfcPropertySet实体
            //IfcPropertySet实体解析
            foreach (object e in RelEntities)
            {
                object propertySet;
                Type t = e.GetType();
                PropertyInfo f = t.GetProperty("RelatingPropertyDefinition");
                propertySet = f.GetValue(e);
                string propertySetName = "";
                Dictionary<string, string> propertiesFields = new Dictionary<string, string>();
                GetPropertySetProperties(propertySet, ref propertySetName, propertiesFields);
                if (propertySetName != ""&& propertiesFields!=null)
                {
                    //出现有相同的propertySetName名称                
                    try
                    {
                        int repeatedkey = 0;
                        if (entityProperties.ContainsKey(propertySetName))
                        {
                            repeatedkey++;
                            propertySetName = propertySetName +"_" + repeatedkey;//例如其他_1
                        }
                        entityProperties.Add(propertySetName, propertiesFields);
                    }
                    catch(Exception xx)
                    {
                        Console.Write(xx.Message);
                        Console.Write(propertySetName);
                    }                   
                }
            }
        }
        protected string GetTypePropertyEntitiesId(HashSet<object> RelTypeEntities)
        {
            string value = "";
            if (RelTypeEntities.Count == 0)
            {
                //该构件无相关的类型属性
                return value;
            }
            else if (RelTypeEntities.Count > 1)
            {
                Console.WriteLine("RelTypeEntitiesc相关的实体Type有多个");
            }
            else
            {
                foreach (object e in RelTypeEntities)
                {
                    object typeEntity;
                    Type t; PropertyInfo f;

                    Dictionary<string, string> propertiesFields = new Dictionary<string, string>();
                    t = e.GetType();
                    f = t.GetProperty("RelatingType");
                    typeEntity = f.GetValue(e);//得到相关的Type,例如IfcDoorType
                                               //获取type的id 
                    if (elementsType.Contains(typeEntity))
                    {
                        value = GetEntityId(typeEntity);
                        //会出现ifcdoorstyle,目前不需要style
                    }
                    else if (typeEntity.GetType().Name == "IfcDoorStyle" || typeEntity.GetType().Name == "IfcWindowStyle")
                    {
                        return value;
                    }
                    else
                    {
                        Console.WriteLine("elementsType实体实例不全" + typeEntity.GetType().Name);
                    }

                }
            }
            return value;
        }
        //获取楼层的名称
        protected string GetStoreyName(object e)
        {
            object stoery = GetStoreyEntity(e);
            if (stoery == null)
            {
                Console.Write("所处楼层信息出错");
            }
            return GetEntityId(stoery);
        }
        //获取构件所在楼层实体
        protected object GetStoreyEntity(object o)
        {
            object RelatingStructure = null;
            HashSet<object> SpatialProperties = new HashSet<object>();
            //只获取ifcspce的楼层信息
            GetSpatialProperty(o, SpatialProperties);
            if (SpatialProperties.Count == 0)
            {
                Console.WriteLine(o.GetType().Name + "与该构件的空间信息出错");
                return null;
            }
            else
            {
                //if (SpatialProperties.Count > 1)
                //{
                //    //若其body有多个，返回第一个
                //    //与管道有关的例如IfcFlowController、IfcFlowFitting(三通)等连接性的会出现多个的情况
                //    Console.WriteLine(o.GetType().Name + "与该构件有关的实体从属关系有多个");
                //}
                foreach (object e in SpatialProperties)//如果是构件的空间位置是个 IfcRelContainedInSpatialStructure实体
                {
                    Type t = e.GetType();
                    if (t.Name == "IfcRelContainedInSpatialStructure")
                    {
                        //获取其属性RelatingStructure: IfcSpatialStructureElement;
                        PropertyInfo f = t.GetProperty("RelatingStructure");
                        RelatingStructure = f.GetValue(e);
                        break;
                    }
                    else if (t.Name == "IfcRelAggregates")//ifcspace的空间位置是IfcRelAggregates实体
                    {
                        //RelatingObject	 : 	IfcObjectDefinition;
                        PropertyInfo f = t.GetProperty("RelatingObject");
                        RelatingStructure = f.GetValue(e);
                        break;
                    }
                    else if (t.Name == "IfcRelVoidsElement")//开洞实体IfcOpeningElement的空间位置所在
                    {
                        PropertyInfo f = t.GetProperty("RelatingBuildingElement");
                        RelatingStructure = f.GetValue(e);
                        break;
                    }
                    else{
                        Console.WriteLine(o.GetType().Name + "该构件的空间位置还会用其他实体表示");
                    }                   
                }
                if (RelatingStructure.GetType().Name == "IfcBuildingStorey")
                {
                    return RelatingStructure;
                }
                else
                {
                    return GetStoreyEntity(RelatingStructure);//递归
                }
            }
        }
        //获取构件实体的位置信息（该空间所属楼层）
        public void GetSpatialProperty(object o, HashSet<object> SpatialProperties)
        {
            Type t = o.GetType();
            PropertyInfo f;
            object v;
            if (t.Name == "IfcOpeningElement")
            {
                //开洞实体的所在空间实体的关系为VoidsElements: 	IfcRelVoidsElement FOR RelatedOpeningElement;
                f = t.GetProperty("VoidsElements");
                v = f.GetValue(o);
                if (v != null)
                {
                    SpatialProperties.Add(v);
                }              
                return;
            }
            //Decomposes: 	SET [0:1] OF IfcRelDecomposes FOR RelatedObjects;ifcspace实体的空间位置所在属性
            //ContainedInStructure: SET[0:1] OF IfcRelContainedInSpatialStructure FOR RelatedElements;物理构件的空间位置所在的属性
            if (t.BaseType.Name == "IfcSpatialStructureElement")
            {
                f = t.GetProperty("Decomposes");
            }
            else
            {
                f = t.GetProperty("ContainedInStructure");
            }
            v = f.GetValue(o);//获取其属性值
            Type ft = f.PropertyType;
            if (IsEntityCollection(ft))
            {
                IEnumerable list = (IEnumerable)v;
                foreach (object invobj in list)
                {
                    SpatialProperties.Add(invobj);
                }
            }
            //有个别实体类型例如ifcplate （不会直接与空间产生联系例如开洞实体）
            if (SpatialProperties.Count == 0)
            {
                f = t.GetProperty("Decomposes");
                v = f.GetValue(o);//获取其属性值
                ft = f.PropertyType;
                if (IsEntityCollection(ft))
                {
                    IEnumerable list = (IEnumerable)v;
                    foreach (object invobj in list)
                    {
                        SpatialProperties.Add(invobj);
                    }
                }
            }
        }
        //获取类型属性单独存储
        public void ProductTypePropertiesValue(object o, Dictionary<string, object> entityTypeProperties)
        {
            //IfcTypeObject  HasPropertySets: OPTIONAL SET[1:?] OF IfcPropertySetDefinition;
            PropertyInfo f = o.GetType().GetProperty("HasPropertySets");
            Type ft = f.PropertyType;
            if (IsEntityCollection(ft))
            {
                object v = f.GetValue(o);
                IEnumerable list = (IEnumerable)v;
                foreach (object propertySet in list)
                {
                    string propertySetName = "";
                    Dictionary<string, string> propertiesFields = new Dictionary<string, string>();
                    GetPropertySetProperties(propertySet, ref propertySetName, propertiesFields);
                    if (propertySetName != ""&& propertiesFields!=null)
                    {
                        if (entityTypeProperties.ContainsKey(propertySetName))//会出现type属性和关系属性名称起的一样
                        {
                            propertySetName = propertySetName + "(Type)";
                        }
                        entityTypeProperties.Add(propertySetName, propertiesFields);
                    }
                }
            }
        }
        //获取某一实体的id
        public string GetEntityId(object o)
        {
            string value = "";
            PropertyInfo f = o.GetType().GetProperty("GlobalId");
            GetPropertyInfoValue(o, f, ref value);
            return value;
        }

        public void DealUnits(object root)
        {
            //单位的集合在ifcProject中
            if (root.GetType().Name == "IfcProject")
            {
                //UnitsInContext	 : 	IfcUnitAssignment;
                PropertyInfo f = root.GetType().GetProperty("UnitsInContext");
                object unitAssignment = f.GetValue(root);
                f = unitAssignment.GetType().GetProperty("Units");
                object Ifcunit = f.GetValue(unitAssignment);
                IEnumerable list = (IEnumerable)Ifcunit;
                foreach (object unit in list)
                {
                    if (unit.GetType().Name == "IfcSIUnit")//不处理派生单位和转换单位
                    {
                        Unit u = new Unit();
                        u.UnitType = GetDirectPropertyValueByName(unit, "UnitType");
                        u.Prefix = GetDirectPropertyValueByName(unit, "Prefix");
                        u.Name = GetDirectPropertyValueByName(unit, "Name");
                        DealUnitsSymbol(u);
                        units.Add(u);
                    }
                }                
            }
            else
            {
                Console.WriteLine("内部结构的root不是IfcProject实体");
            }
            
        }
        public void DealUnitsSymbol(Unit u)
        {
            if (u.UnitType == "lengthunit")
            {
                if (u.Prefix == "milli" && u.Name == "metre")
                {
                    lengthUnit = 0.001f; u.Symbol = "mm";
                }
                else if (u.Prefix == "" && u.Name == "metre")
                {
                    lengthUnit = 1; u.Symbol = "m";
                }
                else
                {
                    Console.WriteLine("该文件的长度单位还有其他定义" + u.Prefix + u.Name);
                }
                unitSymbol.Add("lengthunit", u.Symbol);
            }
            else if (u.UnitType == "areaunit")
            {
                if (u.Prefix == "" && u.Name == "square_metre")
                {
                    u.Symbol = "㎡";
                    unitSymbol.Add("areaunit", u.Symbol);
                }              
            }
            else if (u.UnitType == "volumeunit")
            {
                if (u.Prefix == "" && u.Name == "cubic_metre")
                {
                    u.Symbol = "m³";
                    unitSymbol.Add("volumeunit", u.Symbol);
                }              
            }
            else
            {
                u.Symbol = "";
            }           
        }
        //获取直接属性的值
        protected void GetDirectFieldsValue(object e, Dictionary<string, object> Specific)
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
                if (key == "GlobalId")
                    continue;//基本属性中不显示id
                GetPropertyInfoValue(e, f, ref v);               
                //过滤出值""
                if (v == "")
                {
                    continue;
                }
                else
                {
                    object value = f.GetValue(e);
                    if (value.GetType().Name == "IfcLengthMeasure")
                    {
                        float va;
                        if (v.Contains("E") || v.Contains("e"))//将科学计数法转为数值
                        {
                            decimal data = Convert.ToDecimal(Decimal.Parse(v, System.Globalization.NumberStyles.Float));
                            va = float.Parse(data.ToString("0.00"));
                        }
                        else
                        {
                            va = float.Parse(v, System.Globalization.NumberStyles.Float) * lengthUnit;
                        }
                        Specific.Add(key, va);
                    }
                    else
                    {
                        string unitname = GetPropertyUnit(e, f);
                        if (unitname != "")
                        {
                            v = v + unitname;
                        }
                        Specific.Add(key, v);
                    }
                }
            }
        }
        //处理属性值类型,添加单位，例如：IfcLengthMeasure
        public string GetPropertyUnit(object e, PropertyInfo f)
        {
            string unitStr = "";
            object value = f.GetValue(e);
            if (value == null)
                return unitStr;
            Type valueType = value.GetType();
            if (valueType.Name == "IfcLengthMeasure" || valueType.Name == "IfcPositiveLengthMeasure")
            {
                //获取单位
                unitSymbol.TryGetValue("lengthunit", out unitStr);
            }
            else if (valueType.Name == "IfcAreaMeasure")
            {
                unitSymbol.TryGetValue("areaunit", out unitStr);
            }
            else if (valueType.Name == "IfcVolumeMeasure")
            {
                unitSymbol.TryGetValue("volumeunit", out unitStr);
            }
            return unitStr;
        }
        //根据字符串获取直接属性的值
        public string GetDirectPropertyValueByName(object o, string name)
        {
            string value="";
            PropertyInfo f =o.GetType().GetProperty(name);            
            GetPropertyInfoValue(o, f, ref value);
            return value;
        }
        //获取IfcPropertySet集中的信息key-valuez值
        protected void GetPropertySetProperties(object propertySet, ref string name, Dictionary<string, string> PropertiesFields)
        {
            Type setType = propertySet.GetType();
            if (setType.Name == "IfcPropertySet")
            {
                PropertyInfo f; Type ft = null;
                f = setType.GetProperty("Name");
                GetPropertyInfoValue(propertySet, f, ref name);//返回其name值
                // HasProperties: 	SET [1:?] OF IfcProperty;
                f = setType.GetProperty("HasProperties");
                object properties = f.GetValue(propertySet);
                ft = f.PropertyType;
                if (IsEntityCollection(ft))
                {
                    IEnumerable list = (IEnumerable)properties;
                    foreach (object invobj in list)
                    {
                        if (invobj != null)
                        {
                            Type type = invobj.GetType();
                            if (type.Name == "IfcPropertySingleValue")//属性信息
                            {
                                //Name	 : 	IfcIdentifier;//名称此名称与其他的Name：IfcLable定义不同
                                //NominalValue: OPTIONAL IfcValue;//对应的值信息
                                //Unit: OPTIONAL IfcUnit;单位 给出其id 都是导出属性
                                string key = "";
                                string value = "";
                                int repeatedKey = 0;//重复的键的次数
                                f = type.GetProperty("Name");
                                GetPropertyInfoValue(invobj, f, ref key);
                                f = type.GetProperty("NominalValue");//
                                ft = f.PropertyType;
                                object v = f.GetValue(invobj);
                                if (ft.IsInterface && v is ValueType)
                                {
                                    Type vt = v.GetType();
                                    PropertyInfo fieldValue = vt.GetProperty("Value");
                                    while (fieldValue != null)
                                    {
                                        v = fieldValue.GetValue(v);
                                        if (v != null)
                                        {
                                            Type wt = v.GetType();
                                            if (wt.IsEnum || wt == typeof(bool))
                                            {
                                                v = v.ToString().ToLowerInvariant();
                                            }

                                            fieldValue = wt.GetProperty("Value");
                                        }
                                        else
                                        {
                                            fieldValue = null;
                                        }
                                    }
                                    string encodedvalue = String.Empty;
                                    if (v is IEnumerable && !(v is string))
                                    {
                                        // IfcIndexedPolyCurve.Segments
                                        IEnumerable list1 = (IEnumerable)v;
                                        StringBuilder sb = new StringBuilder();
                                        foreach (object o in list1)
                                        {
                                            if (sb.Length > 0)
                                            {
                                                sb.Append(" ");
                                            }

                                            PropertyInfo fieldValueInner = o.GetType().GetProperty("Value");
                                            if (fieldValueInner != null)
                                            {
                                                //...todo: recurse for multiple levels of indirection, e.g. 
                                                object vInner = fieldValueInner.GetValue(o);
                                                sb.Append(vInner.ToString());
                                            }
                                            else
                                            {
                                                sb.Append(o.ToString());
                                            }
                                        }
                                        encodedvalue = sb.ToString();
                                    }
                                    else if (v != null)
                                    {
                                        encodedvalue = System.Security.SecurityElement.Escape(v.ToString());
                                        if (v.GetType().Name == "Double")
                                        {
                                            float val = float.Parse(encodedvalue, System.Globalization.NumberStyles.Float);//强制转换为float
                                            encodedvalue = val.ToString();
                                        }
                                    }
                                    value = encodedvalue;
                                }
                                //处理单位
                                string str = DealIfcPropertySingleValueUnit(invobj);
                                if (str != "")
                                {
                                    value = value + str;
                                }
                                //当属性中出现相同的key时                               
                                if (PropertiesFields.ContainsKey(key))
                                {
                                    repeatedKey++;
                                    key = key + "_" + repeatedKey;
                                }
                                if (value != "")//去除空值
                                {
                                    PropertiesFields.Add(key, value);//在IFC中会出现有相同的key的情况}
                                }
                            }
                            else
                            {
                                Console.WriteLine("IfcPropertySet中还包含了实体" + type.Name);
                            }
                        }
                        //IsDefinedBy:SET OF IfcRelDefines FOR RelatedObjects;
                    }
                }
                else
                {
                    Console.WriteLine("HasProperties属性处理出错");
                }
            }
            else if (setType.Name == "IfcElementQuantity")
            {
                PropertyInfo f;
                f = setType.GetProperty("Name");
                GetPropertyInfoValue(propertySet, f, ref name);//返回其name值
                                                               //Quantities: SET[1:?] OF IfcPhysicalQuantity;
                f = setType.GetProperty("Quantities");
                object Quantities = f.GetValue(propertySet);
                //获取其name和value
                Type ft = f.PropertyType;
                if (IsEntityCollection(ft))
                {
                    IEnumerable list = (IEnumerable)Quantities;
                    foreach (object invobj in list)
                    {
                        string key = "", value = "";
                        f = invobj.GetType().GetProperty("Name");
                        GetPropertyInfoValue(invobj, f, ref key);

                        f = invobj.GetType().GetProperty("LengthValue");

                        GetPropertyInfoValue(invobj, f, ref value);
                        PropertiesFields.Add(key, value);
                    }
                }
            }
            else
            {
                Console.WriteLine(setType.Name + "这一类型属性集需要处理");
            }
        }
        public string DealIfcPropertySingleValueUnit(object o)
        {
            //处理单位：o类型为IfcPropertySingleValue
            string str = "";
            Type type = o.GetType();
            PropertyInfo f = type.GetProperty("Unit");
            object unitstr = f.GetValue(o);
            if (unitstr != null)
            {
                if (unitstr.GetType().Name == "IfcSIUnit")
                {
                    string unitname=GetDirectPropertyValueByName(unitstr, "UnitType");
                    unitSymbol.TryGetValue(unitname, out str);//查找其对应的符号表示
                }
               //else
              //  {
                    //类型可能为转换单位等（非直接）需要另外处理
                   // Console.WriteLine("IfcPropertySingleValue中自带单位" +unitstr.GetType().Name);
              //  }
            }
            else
            {
                //使用全局默认的
                f = type.GetProperty("NominalValue"); //IfcValue
                str = GetPropertyUnit(o, f);
            }
            return str;
        }
        // 获取构件的层级
        public string GetProductLayer(object e)
        {
            string layer = "";
            //Representation	 : 	OPTIONAL IfcProductRepresentation;
            Type t = e.GetType();
            PropertyInfo f = t.GetProperty("Representation");
            object v = f.GetValue(e);//获取其属性值
            if (v != null)
            {
                if (v.GetType().Name == "IfcProductDefinitionShape")
                {
                    f = v.GetType().GetProperty("Representations");
                    t = f.PropertyType;
                    if (IsEntityCollection(t))
                    {
                        object v1 = f.GetValue(v);
                        IEnumerable list = (IEnumerable)v1;
                        foreach (object list1 in list)
                        {
                            f = list1.GetType().GetProperty("LayerAssignments");
                            v1 = f.GetValue(list1);
                            IEnumerable list2 = (IEnumerable)v1;
                            foreach (object list3 in list2)
                            {
                                f = list3.GetType().GetProperty("Name");
                                GetPropertyInfoValue(list3, f, ref layer);
                                break;
                            }
                            break;
                        }
                    }
                }
            }
            return layer;
        }
    }
}
