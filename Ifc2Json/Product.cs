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
        public void TraverseEntity(object o, HashSet<object> saved, Queue<object> queue)
        {
        }
    }
}
