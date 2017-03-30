using System;
using System.Collections.Generic;
using System.Text;

namespace Minio.Functional.Tests
{
    class TestDataHelper
    {
        private static Random rnd = new Random();

        // Generate a random string
        public static String GetRandomName(int length = 5)
        {
            string characters = "0123456789abcdefghijklmnopqrstuvwxyz";
          
            StringBuilder result = new StringBuilder(length);

            for (int i = 0; i < length; i++)
            {
                result.Append(characters[rnd.Next(characters.Length)]);
            }
            return result.ToString();
        }
       
    }
}
