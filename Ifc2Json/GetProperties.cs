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
        ArrayList productsType = new ArrayList();//若将类型属性写在构件json文件大
        ArrayList rooms = new ArrayList();//存储房间及其相关属性
        ArrayList buildingStoreys = new ArrayList();//存储楼层及其相关属性
        public GetProperties(Type typeProject) : base(typeProject)
        {
        }
        protected internal class ProductProperties
        {
            public string Type { get; set; }//构件的类型
            public string Guid { get; set; }//构件的id            
            public string TypePropertyId { get; set; }  //构件的类型属性的id                                       
            public Dictionary<string, Dictionary<string, string>> properties { get; set; }
        }
        /// <summary>
        /// 空间几何表示的定义,点定义为key-value,方向定义为数组
        /// position{
        /// Location:Dictionary<string,float>,
        /// Axis:List<float>,
        /// RefDirection:List<float>
        /// }
        /// ExtrudedDirection:List<float>
        /// Polyline:List<Dictionary<string,float>>
        /// </summary>
        protected internal class RoomProperties//房间有几何信息，其存储结构与构件相差较大
        {
            public string Type { get; set; }
            public string Guid { get; set; }           
            public string TypePropertyId { get; set; } //房间的类型属性还未处理    
            public string height { get; set; }      //拉伸高度                          
            public Dictionary<string, Dictionary<string, string>> properties { get; set; }//属性信息
            public Dictionary<string, object> shape {get; set;}//房间的几何信息
        }
        //写json
        public void WriteJson(Stream stream, object root)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            if (root == null)
                throw new ArgumentNullException("root");
            StreamWriter writer = new StreamWriter(stream);
            TraverseProject(root);//遍历内部结构获取构件集
            GetProductAndProperties();
            GetRoomAndProperties();
            GetTypeProperty();
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
            writer.WriteLine(",");
            writer.Write("\"productsType\":");
            json = JsonConvert.SerializeObject(productsType, Newtonsoft.Json.Formatting.Indented);
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
                Dictionary<string, Dictionary<string, string>> entityTypeProperties = new Dictionary<string, Dictionary<string, string>>();
                p.Type = e.GetType().Name;
                p.Guid = GetEntityId(e);
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
                //空间结构还有几何表达信息（此处还未添加）
                //创建一对 
                ProductProperties p = new ProductProperties();
                Dictionary<string, Dictionary<string, string>> entityProperties = new Dictionary<string, Dictionary<string, string>>();
                Dictionary<string, string> BasicProperties = new Dictionary<string, string>();
                HashSet<object> RelPropertyEntities = new HashSet<object>();//该构件的属性信息所在的实体
                HashSet<object> TypePropertyEntities = new HashSet<object>();//该构件的类型属性信息
                GetpropertyEntities(e, RelPropertyEntities, TypePropertyEntities);//获取与属性集相关的实体
                GetDirectFieldsValue(e, BasicProperties);
                string type = e.GetType().Name;
                p.Guid = GetEntityId(e);
                p.Type = type;
                p.TypePropertyId = GetTypePropertyEntitiesId(TypePropertyEntities);
                string floor = GetStoreyName(e);
                BasicProperties.Add("floor", floor);
                entityProperties.Add("基本属性", BasicProperties);
                GetRelPropertyEntitiesValue(RelPropertyEntities, entityProperties);//获取关系实体集中的key-value
                p.properties = entityProperties;
                products.Add(p);
            }
            Console.WriteLine("结束");
        }
        public void GetRoomAndProperties()
        {
            //获取空间结构的属性信息（先输出空间结构的，构件的位置信息会用到此）
            foreach (object e in spatialElements)
            {
                //空间结构还有几何表达信息（此处还未添加）               
                Dictionary<string, Dictionary<string, string>> entityProperties = new Dictionary<string, Dictionary<string, string>>();
                Dictionary<string, string> BasicProperties = new Dictionary<string, string>();
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
                    string TypePropertyid = GetTypePropertyEntitiesId(TypePropertyEntities);
                    BasicProperties.Add("TypePropertyid", TypePropertyid);
                    entityProperties.Add("基本属性", BasicProperties);
                    GetRelPropertyEntitiesValue(RelPropertyEntities, entityProperties);//获取关系实体集中的key-value
                    p.TypePropertyId = GetTypePropertyEntitiesId(TypePropertyEntities);
                    p.properties = entityProperties;
                    string height = "";
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
                    entityProperties.Add("基本属性", BasicProperties);
                    GetRelPropertyEntitiesValue(RelPropertyEntities, entityProperties);//获取关系实体集中的key-value
                    p.TypePropertyId = GetTypePropertyEntitiesId(TypePropertyEntities);
                    p.properties = entityProperties;
                    buildingStoreys.Add(p);
                }
            }
            Console.WriteLine("spatialElements结束");
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
                if (propertySetName != "")
                {
                    entityProperties.Add(propertySetName, propertiesFields);
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
                if (o.GetType().Name == "IfcOpeningElement")
                {
                    //do nothing 开洞实体无楼层信息---需要之后验证 
                }
                else
                {
                    Console.WriteLine(o.GetType().Name + "与该构件的空间信息出错");
                }
                return null;
            }
            else
            {
                if (SpatialProperties.Count > 1)
                {
                    //若其body有多个，返回第一个
                    Console.WriteLine(o.GetType().Name + "与该构件有关的实体从属关系有多个");
                }
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
                    else
                    {
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
        public void ProductTypePropertiesValue(object o, Dictionary<string, Dictionary<string, string>> entityTypeProperties)
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
                    if (propertySetName != "")
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
        //处理空间的几何的描述方式，目前讨论拉伸和边界生成实体两种方式
        public void ShapeRepresentationWay(object SpatialShapeEntity, ref string height,Dictionary<string,object> shape)
        {
            if (SpatialShapeEntity != null)
            {
                //IfcProductDefinitionShape Representations	 : 	LIST [1:?] OF IfcRepresentation;
                //IfcRepresentation中包含了style 和shape，只处理shape
                PropertyInfo f; Type ft;
                f = SpatialShapeEntity.GetType().GetProperty("Representations");
                ft = f.PropertyType;
                if (IsEntityCollection(ft))
                {
                    object v = f.GetValue(SpatialShapeEntity);
                    IEnumerable list = (IEnumerable)v;
                    foreach (object e in list)
                    {
                        int i = 0;
                        if (e.GetType().Name == "IfcShapeRepresentation")
                        {
                            string value = "";
                            //RepresentationType	 : 	OPTIONAL IfcLabel;描述方式
                            f = e.GetType().GetProperty("RepresentationType");
                            GetPropertyInfoValue(e, f, ref value);
                            if (value == "SweptSolid")
                            {                                
                                //拉伸
                                DealSweptSolid(e,ref height, shape);
                            }
                            else if (value == "Brep")
                            {
                                //边界生成实体
                                DealBrep(e, shape);
                            }
                            else
                            {
                                Console.Write("空间的几何描述还有其他方式" + value);
                            }
                        }
                        i++;
                        if (i > 1) { Console.Write("相关联的几何表达实体有多个" + SpatialShapeEntity.GetType().Name); }
                    }
                }
            }
            else
            {
                Console.Write("空间的几何表达获取失败");
            }
        }
        //获取描述空间几何的实体
        public object GetSpaceShapeEntity(object o)
        {
            //错误，会有多个
            object SpatialShapeEntity = null;
            //Representation	 : 	OPTIONAL IfcProductRepresentation;
            Type t = o.GetType();
            PropertyInfo f = t.GetProperty("Representation");
            object v = f.GetValue(o);//获取其属性值
                                     //IsDefinedBy:SET OF IfcRelDefines FOR RelatedObjects;
            if (v.GetType().Name == "IfcProductDefinitionShape")
            {
                SpatialShapeEntity = v;
            }
            else if (v.GetType().Name == "IfcMaterialDefinitionRepresentation")
            {
                Console.WriteLine(o.GetType().Name + "该实体的几何描述实体应该在其他地方");
            }
            return SpatialShapeEntity;
        }
        //处理拉伸方式
        public void DealSweptSolid(object o, ref string height, Dictionary<string, object> shape)
        {                                
           //获取其Items	 : 	SET [1:?] OF IfcRepresentationItem;
            PropertyInfo f;
            f = o.GetType().GetProperty("Items");
            Type ft = f.PropertyType;
            if (IsEntityCollection(ft))
            {
                object v = f.GetValue(o); 
                IEnumerable list = (IEnumerable)v;                
                foreach (object e in list)
                {                   
                    //只处理第一个
                    if (e.GetType().Name == "IfcExtrudedAreaSolid")
                    {
                        //IfcExtrudedAreaSolid中的属性（ExtrudedDirection拉伸方向、Depth拉伸长度、Position位置信息）                     
                        //拉伸高度
                        f = e.GetType().GetProperty("Depth");
                        GetPropertyInfoValue(e, f, ref height);
                        //拉伸方向                        
                        f = e.GetType().GetProperty("ExtrudedDirection");
                        object Direction = f.GetValue(e);
                        List<float> ExtrudedDirection = GetDirection(Direction);
                        object value = ExtrudedDirection;
                        shape.Add("ExtrudedDirection", value);
                        //value.Clear();会导致shape中存的value清空                       
                        //Position：IfcAxis2Placement3D位置信息
                        f = e.GetType().GetProperty("Position");
                        object Position = f.GetValue(e);
                        Dictionary<string, object> positionValue = new Dictionary<string, object>();
                        GetPosition(Position, positionValue);
                        value = positionValue;
                        shape.Add("Position", value);
                        //SweptArea:截面的定义（截面的表示也有多种，多种实体类型表示）
                        f = e.GetType().GetProperty("SweptArea");
                        object SweptArea = f.GetValue(e);
                        //内外曲线
                        string sweptName = SweptArea.GetType().Name;
                        if (sweptName == "IfcArbitraryProfileDefWithVoids" || sweptName == "IfcArbitraryClosedProfileDef")
                        {
                            f = SweptArea.GetType().GetProperty("OuterCurve");
                            object poly = f.GetValue(SweptArea);
                            shape.Add("OuterCurve", GetPolyline(poly));
                            f = SweptArea.GetType().GetProperty("InnerCurves");//InnerCurves	 : 	SET [1:?] OF IfcCurve;
                            if (f != null)
                            {
                                poly = f.GetValue(SweptArea);
                                IEnumerable list1 = (IEnumerable)poly;
                                foreach (object po in list1)
                                {
                                    shape.Add("InnerCurves", GetPolyline(po));
                                }
                            }
                        }
                        else if (sweptName == "IfcRectangleProfileDef")//矩形面
                        {
                            string x="";
                            f = SweptArea.GetType().GetProperty("XDim");
                            GetPropertyInfoValue(SweptArea, f, ref x);
                            shape.Add("XDim", x);
                            f = SweptArea.GetType().GetProperty("YDim");
                            GetPropertyInfoValue(SweptArea, f, ref x);
                            shape.Add("YDim", x);
                        }
                        else if (sweptName == "IfcCircleProfileDef")//圆面
                        {
                            string radius="";
                            f = SweptArea.GetType().GetProperty("Radius");
                            GetPropertyInfoValue(SweptArea, f, ref radius);
                            shape.Add("Radius", radius);
                        }
                        else
                        {
                            Console.WriteLine("截面的表达还有" + sweptName);
                        }
                    }
                    else
                    {
                        Console.WriteLine(e.GetType().Name + "该几何表达方式需要处理");
                    }
                    return;                   
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
        //获取空间的Position信息
        public void GetPosition(object position, Dictionary<string, object> positionValue)
        {
            //IfcAxis2Placement3D获取其location、axis\RefDirection
            PropertyInfo f;object v;
            object value;//获取的属性对应的值
            Dictionary<string, float> point=new Dictionary<string, float> ();
            f = position.GetType().GetProperty("Location");
            v = f.GetValue(position);//Location	 : 	IfcCartesianPoint;
            point = GetPoint(v);
            value=point;
            positionValue.Add("Location", value);            
            List<float> direct;
            //Axis	 : 	OPTIONAL IfcDirection 可以为空
            f = position.GetType().GetProperty("Axis");
            v = f.GetValue(position);
            if(v!=null)
            {
                direct = GetDirection(v);
                value=direct;
                positionValue.Add("Axis", value);
            }         
            //RefDirection	 : 	OPTIONAL IfcDirection;
            f = position.GetType().GetProperty("RefDirection");
            v = f.GetValue(position);
            if (v != null)
            {
                direct = GetDirection(v);
                value=direct;
                positionValue.Add("RefDirection", value);
            }
        }
        //获取IfcCartesianPoint点的坐标，结果显示的形式为x:12,y:12,Z:1
        public Dictionary<string,float>GetPoint(object CartesianPoint)
        {
            Dictionary<string, float> point = new Dictionary<string, float>();
            string[] str = { "x", "y", "z" };
            string pointValue = "";//获得的点的坐标用空格隔开
            if (CartesianPoint.GetType().Name == "IfcCartesianPoint")
            {
                PropertyInfo f = CartesianPoint.GetType().GetProperty("Coordinates");
                GetPropertyInfoValue(CartesianPoint, f, ref pointValue);
                string[] ps = pointValue.Split(' ');
                for(int i=0;i<ps.Length;i++)
                {
                    string p = ps[i];
                    float value = float.Parse(p, System.Globalization.NumberStyles.Float);//将字符串转换为float
                    point.Add(str[i], value);
                }
            }
            return point;
        }
        //获取方向IfcDirection的值。结果输出为数组
        public List<float> GetDirection(object Directiont)
        {
            List<float> direction = new List<float>();
            string pointValue = "";
            if (Directiont.GetType().Name == "IfcDirection")
            {               
                PropertyInfo f = Directiont.GetType().GetProperty("DirectionRatios");
                GetPropertyInfoValue(Directiont, f, ref pointValue);
            }
            string[]  ps= pointValue.Split(' ');//分割字符串
            foreach (string p in ps)
            {
                float value= float.Parse(p, System.Globalization.NumberStyles.Float);//将字符串转换为float
                direction.Add(value);
            }
            return direction;
        }
        //线段的表示
        public object GetPolyline(object o)
        {
            object polyLine=null; PropertyInfo f = null;
            List<object> points = new List<object>();
            if (o.GetType().Name == "IfcPolyline")
            {
                //获取其Points
                f= o.GetType().GetProperty("Points");               
            }
            else if(o.GetType().Name == "IfcPolyLoop")
            {
                f = o.GetType().GetProperty("Polygon");
            }
            object v = f.GetValue(o);
            IEnumerable list = (IEnumerable)v;
            foreach (object point in list)
            {
                Dictionary<string, float> value = GetPoint(point);
                if (value != null)
                {
                   points.Add(value);
                 }
                else
                 {
                    Console.WriteLine("获取点的坐标失败");
                 }
            }
            polyLine = points;
            return polyLine;
        }
        //处理边界描述几何的方式
        public void DealBrep(object o, Dictionary<string, object> shape)
        {
            //"IfcFacetedBrep",
            PropertyInfo f;ArrayList points = new ArrayList();
            f = o.GetType().GetProperty("Items");
            Type ft = f.PropertyType;
            object v = f.GetValue(o);
            IEnumerable list = (IEnumerable)v;
            foreach (object e in list)
            {
                if (e.GetType().Name == "IfcFacetedBrep")
                {
                    f = e.GetType().GetProperty("Outer");
                    v = f.GetValue(e);
                    f = v.GetType().GetProperty("CfsFaces");
                    v = f.GetValue(v);//其类型是set                   
                    IEnumerable list1 = (IEnumerable)v;
                    foreach (object e1 in list1)
                    {
                        if (e1.GetType().Name == "IfcFace")
                        {
                            f = e1.GetType().GetProperty("Bounds");
                            //set
                            v = f.GetValue(e1);//其类型是set IfcFaceOuterBound     
                            IEnumerable list2 = (IEnumerable)v;
                            foreach (object e2 in list2)
                            {
                                f = e2.GetType().GetProperty("Bound");//IfcPolyLoop
                                v = f.GetValue(e2);
                                object value=GetPolyline(v);
                                points.Add(value);
                            }
                        }
                    }                  
                }
            }
            object polyhedron = points;
            shape.Add("polyhedron", polyhedron);
        }
        
    }
}
