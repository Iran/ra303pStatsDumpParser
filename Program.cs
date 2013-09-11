using System;
using System.Collections.Generic;
using System.Text;
using ra303pStatsDumpParser;

namespace ra303pStatsDumpReader
{
    class Program
    {
        static void Main(string[] args)
        {

            StatsDumpParser StatsDump = new StatsDumpParser("stats.dmp");

            StatsDump.Print_Parsed_Data();

            Console.Read();
        }
    }
}
