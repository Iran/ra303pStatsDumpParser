﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
