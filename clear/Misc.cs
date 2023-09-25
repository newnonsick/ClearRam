using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;

namespace clear
{
    internal class Misc
    {

        public static void Cleartext(String text)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("\n   [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Clearing");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("] : ");
            Console.Write(text);
        }

        public static void Infotext(String text)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("\n   [");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("Information");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("] ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(": ");
            Console.Write(text);
        }

        public static void Outtext(String text, ConsoleColor color)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("\n   [");
            Console.ForegroundColor = color;
            Console.Write("+");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("] ");
            Console.Write(text);
        }

        public static void Totalramtext(double ramUsage, string BA, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.Write("   [");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"Total Ram Usage ({BA})");
            Console.ForegroundColor = color;
            Console.Write("]");
            Console.ForegroundColor = ConsoleColor.White; ;
            Console.Write($" : {ramUsage} MB");
        }
    }
}
