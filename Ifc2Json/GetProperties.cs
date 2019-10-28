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
        ArrayList units = new ArrayList();
        Dictionary<string, string> unitSymbol = new Dictionary<string, string>();//将单位与其symbol另存储，方便在属性值后加单位时查找
        ArrayList products = new ArrayList();//存储构件与其对应的属性实例
        ArrayList productsType = new ArrayList();//若将类型属性写在构件json文件大
        ArrayList rooms = new ArrayList();//存储房间及其相关属性
        ArrayList buildingStoreys = new ArrayList();//存储楼层及其相关属性
        float lengthUnit;//ifc中定义的长度单位
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
            public float height { get; set; }      //拉伸高度                          
            public Dictionary<string, Dictionary<string, string>> properties { get; set; }//属性信息
            public Dictionary<string, object> shape {get; set;}//房间的几何信息
        }
        protected internal class Unit
        {
            public string UnitType { get; set; }
            public string Prefix { get; set; }
            public string Name { get; set; }
            public string Symbol { get; set; }

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
            DealUnits(root);
            GetProductAndProperties();
            GetRoomAndProperties();
            GetTypeProperty();
            writer.WriteLine("{");//
            WriteHead(writer);
            writer.Write("\"units\":");
            string json;
            json = JsonConvert.SerializeObject(units, Newtonsoft.Json.Formatting.Indented);
            writer.Write(json);
            writer.WriteLine(",");
            writer.Write("\"buildingStoreys\":");           
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
                Dictionary<string, string> BasicProperties = new Dictionary<string, string>();
                Dictionary<string, Dictionary<string, string>> entityTypeProperties = new Dictionary<string, Dictionary<string, string>>();
                p.Type = e.GetType().Name;
                p.Guid = GetEntityId(e);
                GetDirectFieldsValue(e, BasicProperties);
                entityTypeProperties.Add("基本属性", BasicProperties);
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
                catch (Exception xx)
                {
                    //MessageBox.Show(xx.Message);
                    Console.WriteLine(xx.Message);
                    Console.WriteLine(p.Guid+" _"+p.Type);//当出现错误时输出当前构件的id
                }
            }
           // Console.WriteLine("物理构件结束");
        }
        public void GetRoomAndProperties()
        {
            //获取空间结构的属性信息（先输出空间结构的，构件的位置信息会用到此）
            foreach (object e in spatialElements)
            {
                try
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
                        entityProperties.Add("基本属性", BasicProperties);
                        GetRelPropertyEntitiesValue(RelPropertyEntities, entityProperties);//获取关系实体集中的key-value
                        p.TypePropertyId = GetTypePropertyEntitiesId(TypePropertyEntities);
                        p.properties = entityProperties;
                        float height=0;
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
                catch (Exception xx)
                {
                    //MessageBox.Show(xx.Message);
                    Console.WriteLine(xx.Message);
                    Console.WriteLine(e.GetType().Name);
                }
            }
            //Console.WriteLine("spatialElements结束");
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
                if (SpatialProperties.Count > 1)
                {
                    //若其body有多个，返回第一个
                    //与管道有关的例如IfcFlowController、IfcFlowFitting(三通)等连接性的会出现多个的情况
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
        //处理空间的几何的描述方式，目前讨论拉伸和边界生成实体两种方式
        public void ShapeRepresentationWay(object SpatialShapeEntity, ref float height,Dictionary<string,object> shape)
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
                                Console.Write("空间的几何描述还有其他方式" + value);//还有一种SurfaceModel方式
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
        public void DealSweptSolid(object o, ref float height, Dictionary<string, object> shape)
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
                        string depth = "";
                        f = e.GetType().GetProperty("Depth");
                        GetPropertyInfoValue(e, f, ref depth);
                        height = float.Parse(depth, System.Globalization.NumberStyles.Float) * lengthUnit;
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
                                // int i = 1;
                                List<object> InnerCurves = new List<object>();
                                foreach (object po in list1)
                                {
                                    InnerCurves.Add(GetPolyline(po));
                                    //注：内曲线可能有多个，所以此处Dictionary不能添加重复键
                                    // shape.Add("InnerCurves_"+i, GetPolyline(po));
                                    //   i++;
                                }
                                shape.Add("InnerCurves", InnerCurves);
                            }
                        }
                        else if (sweptName == "IfcRectangleProfileDef")//矩形面
                        {
                            object OuterCurve = DealRectangleProfileDef(SweptArea);                     
                            shape.Add("OuterCurve", OuterCurve);//将长宽处理为四个点
                        }
                        else if (sweptName == "IfcCircleProfileDef")//圆面
                        {
                            string radius="";
                            f = SweptArea.GetType().GetProperty("Radius");
                            GetPropertyInfoValue(SweptArea, f, ref radius);
                            float radiusValue= float.Parse(radius, System.Globalization.NumberStyles.Float) * lengthUnit;
                            shape.Add("Radius", radiusValue);
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
            //IfcPlacement IfcAxis2Placement2D无axis IfcAxis1Placement无 RefDirection
            //IfcAxis2Placement3D获取其location、axis\RefDirection
            PropertyInfo f; object v;
            object value;//获取的属性对应的值
            Dictionary<string, float> point = new Dictionary<string, float>();
            f = position.GetType().GetProperty("Location");
            v = f.GetValue(position);//Location	 : 	IfcCartesianPoint;
            point = GetPoint(v);
            value = point;
            positionValue.Add("Location", value);
            List<float> direct;
            //Axis	 : 	OPTIONAL IfcDirection 可以为空
            if (position.GetType().Name == "IfcAxis2Placement2D")
            {
                //do nothing ;
            }
            else
            {
                f = position.GetType().GetProperty("Axis");
                v = f.GetValue(position);
                if (v != null)
                {
                    direct = GetDirection(v);
                    value = direct;
                    positionValue.Add("Axis", value);
                }
            }
            //RefDirection	 : 	OPTIONAL IfcDirection;
            if (position.GetType().Name == "IfcAxis1Placement")
            {
                //do nothing;
            }
            else
            {
                f = position.GetType().GetProperty("RefDirection");
                v = f.GetValue(position);
                if (v != null)
                {
                    direct = GetDirection(v);
                    value = direct;
                    positionValue.Add("RefDirection", value);
                }
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
                    float value = float.Parse(p, System.Globalization.NumberStyles.Float)* lengthUnit;//将字符串转换为float
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
                f = o.GetType().GetProperty("Points");
            }
            else if (o.GetType().Name == "IfcPolyLoop")
            {
                f = o.GetType().GetProperty("Polygon");
            }
            else if (o.GetType().Name == "IfcCompositeCurve")//复合曲线,由多个曲线段组成
            {
                PropertyInfo f1 = o.GetType().GetProperty("Segments");
                object v1 = f1.GetValue(o);
                IEnumerable Segments = (IEnumerable)v1;
                ArrayList lines = new ArrayList();
                foreach (object segment in Segments)
                {
                    f1 = segment.GetType().GetProperty("ParentCurve");
                    v1 = f1.GetValue(segment);//v1是IfcPolyline
                    object line = GetPolyline(v1);
                    lines.Add(line);
                }
                polyLine = lines;
                return polyLine;
            }
            else if (o.GetType().Name == "IfcTrimmedCurve")
            {
                return DealTrimmedCurve(o);
            }
            else
            {
                Console.WriteLine("曲线还有其他表达方式" + o.GetType().Name);
                return polyLine;
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
        //修剪曲线,只获取了其基本的曲线，修剪的点还未处理
        public object  DealTrimmedCurve(object o)
        {
            object trimmedCurve = null;
            Dictionary<string, object> line = new Dictionary<string, object>();
            PropertyInfo f = o.GetType().GetProperty("BasisCurve");           
            object v = f.GetValue(o);//IfcCurve
            if (v.GetType().Name == "IfcLine")
            {
                //Pnt:  IfcCartesianPoint线的位置 Dir：线的方向
                f = v.GetType().GetProperty("Pnt");
                object temp = f.GetValue(v);
                Dictionary<string, float> point = GetPoint(temp);                
                line.Add("Pnt", point);
                f = v.GetType().GetProperty("Dir");// IfcVector
                temp = f.GetValue(v);
                if (temp.GetType().Name == "IfcVector")
                {
                    f = temp.GetType().GetProperty("Orientation");
                    object dir = f.GetValue(temp);
                    List<float> direction = GetDirection(dir);                   
                    line.Add("Dir", direction);
                }
                trimmedCurve = line;
            }
            else if (v.GetType().Name == "IfcCircle")
            {
                f = v.GetType().GetProperty("Position");
                object Position = f.GetValue(v);
                Dictionary<string, object> positionValue = new Dictionary<string, object>();
                GetPosition(Position, positionValue);
                line.Add("Position", positionValue);
                string radius = "";
                f = v.GetType().GetProperty("Radius");
                GetPropertyInfoValue(v, f, ref radius);
                line.Add("Radius", radius);
                trimmedCurve = line;

            }
            else
            {
                Console.WriteLine("IfcTrimmedCurve中还有类型未处理" + v.GetType().Name);
            }              
            return trimmedCurve;
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
                if (key == "GlobalId")
                    continue;//基本属性中不显示id
                GetPropertyInfoValue(e, f, ref v);
                string unitname=GetPropertyUnit(e, f);
                //过滤出值""
                if (v == "")
                {
                    continue;
                }
                else
                {
                    if (unitname != "")
                    {
                        v = v + unitname;
                     }
                    Specific.Add(key, v);
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
        public void WriteHead(StreamWriter writer)
        {           
            string ApplicationFullName = GetDirectPropertyValueByName(application,"ApplicationFullName");
            writer.WriteLine("\"Head\": {");
            writer.Write("\"applicationFullName\": \"");
            writer.Write(ApplicationFullName);
            writer.WriteLine("\",");
            writer.WriteLine("\"Schema\": \"IFC2×3\",");
            writer.Write("\"楼层数目\":");
            writer.Write(buildingStoreys.Count);
            writer.WriteLine(",");
            writer.Write("\"房间数目\":");
            writer.Write(rooms.Count);
            writer.WriteLine(",");
            writer.Write("\"构件数目\":");
            writer.WriteLine(products.Count);

            writer.WriteLine("},");
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
        public object DealRectangleProfileDef(object SweptArea)
        {
            string x = "";
            PropertyInfo f = SweptArea.GetType().GetProperty("XDim");
            GetPropertyInfoValue(SweptArea, f, ref x);//几何信息输出都为float
            float length = float.Parse(x, System.Globalization.NumberStyles.Float) * lengthUnit;  //获取长宽                       

            f = SweptArea.GetType().GetProperty("YDim");
            GetPropertyInfoValue(SweptArea, f, ref x);
            float width = float.Parse(x, System.Globalization.NumberStyles.Float) * lengthUnit;

            float[][] points = new float[4][];

            points[0] = new float[] { -length / 2, -width / 2 };
            points[1] = new float[] { +length / 2, -width / 2 };
            points[2] = new float[] { +length / 2, +width / 2 };
            points[3] = new float[] { -length / 2, +width / 2 };

            return points;
        }
    }
}
