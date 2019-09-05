using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ifc2Json
{
    class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("需要给定输入ifc和输出json文件名！");
                return -1;
            }

            ifc2json_cmd convert = new ifc2json_cmd();


            Console.WriteLine("开始转换 …………");
            DateTime startT = DateTime.Now;

            int iSuccess = convert.StartConvert(args[0], args[1]);

            DateTime endT = DateTime.Now;
            TimeSpan ts = endT - startT;


            if (iSuccess == 0)
            {
                Console.WriteLine("成功完成转换！\r\n此次转换总共耗时 {0}秒！", ts.TotalSeconds.ToString("0.00"));
                return 0;

            }
            else
            {
                Console.WriteLine("转换出错！\r\n此次不成功转换总共耗时 {0}秒！", ts.TotalSeconds.ToString("0.00"));
                return -1;
            }
        }
    }
}
