using System;
using System.Threading;

namespace test1111
{
    class testProgram
    {
        static void Main()
        {
            while (true)
            {
                string input = Console.ReadLine();


                if (input == "stop")
                {
                    break;
                }
                Console.WriteLine("入力された文字列: " + input);
            }
        }
    }
}