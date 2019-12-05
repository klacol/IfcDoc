using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Ifc2Json
{
    class SpatialElements : Elements
    {
        public SpatialElements(Type typeProject) : base(typeProject)
        {
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
        //房间的组织结构（有几何关系）
        protected internal class RoomProperties//房间有几何信息，其存储结构与构件相差较大
        {
            public string Type { get; set; }
            public string Guid { get; set; }
            public string TypePropertyId { get; set; } //房间的类型属性还未处理    
            public float height { get; set; }      //拉伸高度                          
            public Dictionary<string, object> properties { get; set; }//属性信息
            public Dictionary<string, object> shape { get; set; }//房间的几何信息
        }
        //处理空间的几何的描述方式，目前讨论拉伸和边界生成实体两种方式
        public void ShapeRepresentationWay(object SpatialShapeEntity, ref float height, Dictionary<string, object> shape)
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
                                DealSweptSolid(e, ref height, shape);
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
                            string radius = "";
                            f = SweptArea.GetType().GetProperty("Radius");
                            GetPropertyInfoValue(SweptArea, f, ref radius);
                            float radiusValue = float.Parse(radius, System.Globalization.NumberStyles.Float) * lengthUnit;
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
        public Dictionary<string, float> GetPoint(object CartesianPoint)
        {
            Dictionary<string, float> point = new Dictionary<string, float>();
            string[] str = { "x", "y", "z" };
            string pointValue = "";//获得的点的坐标用空格隔开
            if (CartesianPoint.GetType().Name == "IfcCartesianPoint")
            {
                PropertyInfo f = CartesianPoint.GetType().GetProperty("Coordinates");
                GetPropertyInfoValue(CartesianPoint, f, ref pointValue);
                string[] ps = pointValue.Split(' ');
                for (int i = 0; i < ps.Length; i++)
                {
                    string p = ps[i];
                    float value = float.Parse(p, System.Globalization.NumberStyles.Float) * lengthUnit;//将字符串转换为float
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
            string[] ps = pointValue.Split(' ');//分割字符串
            foreach (string p in ps)
            {
                float value = float.Parse(p, System.Globalization.NumberStyles.Float);//将字符串转换为float
                direction.Add(value);
            }
            return direction;
        }
        //线段的表示
        public object GetPolyline(object o)
        {
            object polyLine = null; PropertyInfo f = null;
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
        public object DealTrimmedCurve(object o)
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
            PropertyInfo f; ArrayList points = new ArrayList();
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
                                object value = GetPolyline(v);
                                points.Add(value);
                            }
                        }
                    }
                }
            }
            object polyhedron = points;
            shape.Add("polyhedron", polyhedron);
        }
        //处理截面
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
