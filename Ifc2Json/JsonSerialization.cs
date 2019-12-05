using System;
using System.Collections.Generic;
using System.IO;
using System.Collections;
using Newtonsoft.Json;
using System.Reflection;
using System.Runtime.Serialization;

namespace Ifc2Json
{
    class JsonSerialization : SpatialElements
    {
        ArrayList products = new ArrayList();//存储构件与其对应的属性实例
        ArrayList productsType = new ArrayList();//若将类型属性写在构件json文件大
        ArrayList rooms = new ArrayList();//存储房间及其相关属性
        ArrayList buildingStoreys = new ArrayList();//存储楼层及其相关属性

        public JsonSerialization(Type typeProject) : base(typeProject)
        {
        }
        public void WriteJson(Stream stream, object root)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            if (root == null)
                throw new ArgumentNullException("root");
            StreamWriter writer = new StreamWriter(stream);
            TraverseProject(root);//遍历内部结构获取构件集
            DealUnits(root);//处理单位
            GetProductAndProperties();//获取构件与其属性
            GetRoomAndProperties();//获取房间与其属性
            GetTypeProperty();//获取类型属性
            Project project = new Project();//一个IFC只会有一个项目project
            GetProjectProperties(root, project);//获取项目的相关信息
            //写文件
            writer.WriteLine("{");
            WriteObject(writer, "IfcProject", project);
            WriteObject(writer, "units", units);
            WriteObject(writer, "buildingStoreys", buildingStoreys);
            WriteObject(writer, "rooms", rooms);
            WriteObject(writer, "products", products);            
            writer.Write("\"productsType\":");
            string json = JsonConvert.SerializeObject(productsType, Newtonsoft.Json.Formatting.Indented);
            writer.WriteLine(json);
            this.WriteFooter(writer);
            writer.Flush();
        }
        //type类型属性
        public void GetTypeProperty()
        {
            foreach (object e in elementsType)
            {
                ProductProperties p = new ProductProperties();
                Dictionary<string, object> BasicProperties = new Dictionary<string, object>();
                //Dictionary<string, Dictionary<string, string>> entityTypeProperties = new Dictionary<string, Dictionary<string, string>>();
                Dictionary<string, object> entityTypeProperties = new Dictionary<string, object>();
                p.Type = e.GetType().Name;
                p.Guid = GetEntityId(e);
                GetDirectFieldsValue(e, BasicProperties);
                entityTypeProperties.Add("BasicProperties", BasicProperties);
                ProductTypePropertiesValue(e, entityTypeProperties);
                p.properties = entityTypeProperties;
                productsType.Add(p);
            }
        }
        //获取构件及其构件属性
        public void GetProductAndProperties()
        {

            //物理构件
            foreach (object e in elements)
            {
                ProductProperties p = new ProductProperties();
                try
                {
                    Dictionary<string, object> entityProperties = new Dictionary<string, object>();
                    Dictionary<string, object> BasicProperties = new Dictionary<string, object>();
                    HashSet<object> RelPropertyEntities = new HashSet<object>();//该构件的属性信息所在的实体
                    HashSet<object> TypePropertyEntities = new HashSet<object>();//该构件的类型属性信息
                    GetpropertyEntities(e, RelPropertyEntities, TypePropertyEntities);//获取与属性集相关的实体
                    GetDirectFieldsValue(e, BasicProperties);
                    string type = e.GetType().Name;
                    p.Guid = GetEntityId(e);
                    p.Type = type;
                    p.TypePropertyId = GetTypePropertyEntitiesId(TypePropertyEntities);
                    string floor = GetStoreyName(e);
                    string layer = GetProductLayer(e);
                    BasicProperties.Add("floor", floor);
                    BasicProperties.Add("layer", layer);
                    entityProperties.Add("BasicProperties", BasicProperties);

                    GetRelPropertyEntitiesValue(RelPropertyEntities, entityProperties);//获取关系实体集中的key-value
                    p.properties = entityProperties;
                    products.Add(p);
                }
                catch (Exception xx)
                {
                    //MessageBox.Show(xx.Message);
                    Console.WriteLine(xx.Message);
                    Console.WriteLine(p.Guid + " _" + p.Type);//当出现错误时输出当前构件的id
                }
            }
            // Console.WriteLine("物理构件结束");
        }
        //获取房间及其相关属性
        public void GetRoomAndProperties()
        {
            //获取空间结构的属性信息（先输出空间结构的，构件的位置信息会用到此）
            foreach (object e in spatialElements)
            {
                try
                {
                    //空间结构还有几何表达信息（此处还未添加）               
                    Dictionary<string, object> entityProperties = new Dictionary<string, object>();
                    Dictionary<string, object> BasicProperties = new Dictionary<string, object>();
                    HashSet<object> RelPropertyEntities = new HashSet<object>();//该构件的属性信息所在的实体
                    HashSet<object> TypePropertyEntities = new HashSet<object>();//该构件的类型属性信息
                    GetpropertyEntities(e, RelPropertyEntities, TypePropertyEntities);//获取与属性集相关的实体
                    GetDirectFieldsValue(e, BasicProperties);
                    string type = e.GetType().Name;
                    if (type == "IfcSpace")
                    {
                        RoomProperties p = new RoomProperties();
                        p.Guid = GetEntityId(e);
                        p.Type = type;
                        string floor = GetStoreyName(e);
                        BasicProperties.Add("floor", floor);
                        entityProperties.Add("BasicProperties", BasicProperties);
                        GetRelPropertyEntitiesValue(RelPropertyEntities, entityProperties);//获取关系实体集中的key-value
                        p.TypePropertyId = GetTypePropertyEntitiesId(TypePropertyEntities);
                        p.properties = entityProperties;
                        float height = 0;
                        Dictionary<string, object> shape = new Dictionary<string, object>();
                        ShapeRepresentationWay(GetSpaceShapeEntity(e), ref height, shape);
                        p.height = height;
                        p.shape = shape;
                        rooms.Add(p);
                    }
                    else if (type == "IfcBuildingStorey")
                    {
                        ProductProperties p = new ProductProperties();
                        p.Guid = GetEntityId(e);
                        p.Type = type;
                        entityProperties.Add("BasicProperties", BasicProperties);
                        GetRelPropertyEntitiesValue(RelPropertyEntities, entityProperties);//获取关系实体集中的key-value
                        p.TypePropertyId = GetTypePropertyEntitiesId(TypePropertyEntities);
                        p.properties = entityProperties;
                        buildingStoreys.Add(p);
                    }
                }
                catch (Exception xx)
                {
                    //MessageBox.Show(xx.Message);
                    Console.WriteLine(xx.Message);
                    Console.WriteLine(e.GetType().Name);
                }
            }
            //Console.WriteLine("spatialElements结束");
        }
        //获取项目的相关信息
        public void GetProjectProperties(object root, Project project)
        {
            if (root.GetType().Name == "IfcProject")
            {
                project.Type = root.GetType().Name;
                project.Guid = GetEntityId(root);
                Dictionary<string, object> BasicProperties = new Dictionary<string, object>();
                GetDirectFieldsValue(root, BasicProperties);
                string ApplicationFullName = GetDirectPropertyValueByName(application, "ApplicationFullName");//软件名称
                BasicProperties.Add("ApplicationFullName", ApplicationFullName);
                BasicProperties.Add("Schema Identifiers", "IFC2x3");
                GetInformation(root, BasicProperties);
                BasicProperties.Add("楼层数目", buildingStoreys.Count);//楼层数目
                BasicProperties.Add("房间数目", rooms.Count);
                BasicProperties.Add("构件数目", products.Count);
                project.properties = BasicProperties;
            }
        }
        //获取作者信息和单位信息
        public void GetInformation(object root, Dictionary<string, object> BasicProperties)
        {
            PropertyInfo f = root.GetType().GetProperty("OwnerHistory");
            object o = f.GetValue(root);
            //将时间戳转换为日期
            string timeStamp = GetDirectPropertyValueByName(o, "CreationDate");
            if (timeStamp != "")
            {
                DateTime dtStart = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
                long lTime = long.Parse(timeStamp + "0000000");
                TimeSpan toNow = new TimeSpan(lTime);
                DateTime targetDt = dtStart.Add(toNow);
                dtStart.Add(toNow);
                BasicProperties.Add("CreationDate", targetDt.ToString());
            }
            f = o.GetType().GetProperty("OwningUser");
            object v = f.GetValue(o);
            if (v != null) {
               object v1 = GetPropertyObject(v, "ThePerson");
               string name = GetDirectPropertyValueByName(v1, "GivenName");//获取作者信息
               BasicProperties.Add("Author", name);
               v1 = GetPropertyObject(v1, "Roles");
               BasicProperties.Add("role", GetDirectPropertyValueByName(v1, "Role"));//添加角色
               v1 = GetPropertyObject(v, "TheOrganization");
               BasicProperties.Add("Organization", GetDirectPropertyValueByName(v1, "Name"));//获取单位信息
            }
        }
        public void WriteObject(StreamWriter writer,string key,object e)
        {
            writer.Write("\"" + key + "\": ");// writer.Write("\"IfcProject\":");
            string json = JsonConvert.SerializeObject(e, Newtonsoft.Json.Formatting.Indented);
            writer.Write(json);
            writer.WriteLine(",");
        }
        public object GetPropertyObject(object o, string key)
        {
            PropertyInfo f = o.GetType().GetProperty(key);
            object v = f.GetValue(o);
            if (IsEntityCollection(f.PropertyType))//如果是集合使用第一个元素
            {                
                IEnumerable list = (IEnumerable)v;
                foreach (object invobj in list)
                {
                    v = invobj;
                    break;
                }
            }        
            return v;
        }
    }
}
