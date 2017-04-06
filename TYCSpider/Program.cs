using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TYCSpider
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("程序开始执行....");
            Spider spider = new Spider();

            spider.Run();
        }
    }
}
