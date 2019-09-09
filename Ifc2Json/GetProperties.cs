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
    class GetProperties : Product
    {
        ArrayList products = new ArrayList();//存储构件与其对应的属性实例
        ArrayList rooms = new ArrayList();//存储房间及其相关属性
        ArrayList buildingStoreys = new ArrayList();//存储楼层及其相关属性
        public GetProperties(Type typeProject) : base(typeProject)
        {
        }
        protected internal class ProductProperties
        {
            public string Type { get; set; }//构件的类型
            public string Guid { get; set; }//构件的id
           // public bool IsBuildingStorey { get; set; }//一标志判断获取的stroey是否是楼层
            public Dictionary<string, Dictionary<string, string>> properties { get; set; }
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
            
            writer.WriteLine("{");//
            writer.Write("\"buildingStoreys\":");
            string json;
            json = JsonConvert.SerializeObject(buildingStoreys, Newtonsoft.Json.Formatting.Indented);
            writer.Write(json);
            writer.WriteLine(",");
            writer.Write("\"rooms\":");
            json = JsonConvert.SerializeObject(rooms, Newtonsoft.Json.Formatting.Indented);
            writer.Write(json);
            writer.WriteLine(",");
            writer.Write("\"products\":");
            json = JsonConvert.SerializeObject(products, Newtonsoft.Json.Formatting.Indented);
            writer.WriteLine(json);
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
                HashSet<object> RelPropertyEntities = new HashSet<object>();//该构件的属性信息所在的实体
                HashSet<object> TypePropertyEntities = new HashSet<object>();//该构件的类型属性信息
                GetpropertyEntities(e, RelPropertyEntities, TypePropertyEntities);//获取与属性集相关的实体
                GetDirectFieldsValue(e, BasicProperties);
                string guid; string type = e.GetType().Name;
                if (BasicProperties.TryGetValue("GlobalId", out guid))
                {
                    p.Guid = guid;
                }
                p.Type = type;
                entityProperties.Add("基本属性", BasicProperties);
                GetRelPropertyEntitiesValue(RelPropertyEntities, entityProperties);//获取关系实体集中的key-value
                GetTypePropertyEntitiesValue(TypePropertyEntities, entityProperties);//获取Type的属性
                p.properties = entityProperties;
                if (p.Type == "IfcSpace")
                {
                    rooms.Add(p);
                }
                else if (p.Type == "IfcBuildingStorey")
                {
                    buildingStoreys.Add(p);
                }
                //products.Add(p);
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
                string guid; string type = e.GetType().Name;
                if (BasicProperties.TryGetValue("GlobalId", out guid))
                {
                    p.Guid = guid;
                }
                p.Type = type;
                entityProperties.Add("基本属性", BasicProperties);

                HashSet<object> RelPropertyEntities = new HashSet<object>();//该构件的属性信息所在的实体
                HashSet<object> TypePropertyEntities = new HashSet<object>();//该构件的类型属性信息
                GetpropertyEntities(e, RelPropertyEntities, TypePropertyEntities);//获取与属性集相关的实体
                GetRelPropertyEntitiesValue(RelPropertyEntities, entityProperties);//获取关系实体集中的key-value
                GetTypePropertyEntitiesValue(TypePropertyEntities, entityProperties);//获取Type的属性
                p.properties = entityProperties;
                products.Add(p);
            }
            Console.WriteLine("结束");
        }
        //获取实体的直接属性的key-value值
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
        protected void GetRelPropertyEntitiesValue(HashSet<object> RelEntities, Dictionary<string, Dictionary<string, string>> entityProperties)
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
                entityProperties.Add(propertySetName, propertiesFields);
            }
        }
        protected void GetTypePropertyEntitiesValue(HashSet<object> RelTypeEntities, Dictionary<string, Dictionary<string, string>> entityProperties)
        {
            if (RelTypeEntities.Count > 1)
            {
                Console.WriteLine("RelTypeEntitiesc相关的实体Type有多个");
            }
            foreach (object e in RelTypeEntities)
            {
                object typeEntity;
                Type t; PropertyInfo f;
             
                Dictionary<string, string> propertiesFields = new Dictionary<string, string>();
                t = e.GetType();
                f = t.GetProperty("RelatingType");
                typeEntity = f.GetValue(e);//得到相关的Type,例如IfcDoorType
                //IfcTypeObject  HasPropertySets: OPTIONAL SET[1:?] OF IfcPropertySetDefinition;
                f = typeEntity.GetType().GetProperty("HasPropertySets");
                Type ft = f.PropertyType;
                if (IsEntityCollection(ft))
                {
                    IEnumerable list = (IEnumerable)typeEntity;
                    foreach (object propertySet in list)
                    {
                        string propertySetName = "";
                        GetPropertySetProperties(propertySet, ref propertySetName, propertiesFields);
                        entityProperties.Add(propertySetName, propertiesFields);
                    }
                }
            }
        }
    }
}
