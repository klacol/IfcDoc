using BuildingSmart.Serialization.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
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
            // mark as saved
            saved.Add(o);
            //存储
            EntityClassify(o);
            Type t = o.GetType();
            //不遍历几何信息实体（节省遍历时间）
            if (t.Name == "IfcShapeRepresentation" || t.Name == "IfcPolyline" || t.Name == "IfcShapeRepresentation" || t.Name == "IfcExtrudedAreaSolid" || t.Name == "IfcIShapeProfileDef" ||
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
    }
}
