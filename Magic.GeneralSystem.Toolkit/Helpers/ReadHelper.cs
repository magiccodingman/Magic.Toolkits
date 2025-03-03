using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magic.GeneralSystem.Toolkit.Helpers
{
    public class ReadHelper
    {
        public static string ReadSecureInput()
        {
            string input = "";
            ConsoleKeyInfo key;

            while (true)
            {
                key = Console.ReadKey(intercept: true); // Read key but don't display

                if (key.Key == ConsoleKey.Enter)
                    break; // Stop input on Enter key

                if (key.Key == ConsoleKey.Backspace && input.Length > 0)
                {
                    // Remove last character from input
                    input = input[..^1];

                    // Move cursor back, overwrite last * with space, move back again
                    Console.Write("\b \b");
                }
                else if (!char.IsControl(key.KeyChar)) // Ignore control characters
                {
                    input += key.KeyChar;
                    Console.Write("*"); // Show asterisk for each typed character
                }
            }

            return input;
        }
    }
}
