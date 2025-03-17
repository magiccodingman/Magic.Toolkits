using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magic.Cli.Toolkit
{
    public static class MagicConsole
    {
        public static void WriteLine()
        {
            Console.WriteLine();
        }

        public static void WriteLine(string text)
        {
            
            SafeWrite(text, newLine: true);
        }

        public static void Write(string text)
        {
            SafeWrite(text, newLine: false);
        }

        private static void SafeWrite(string text, bool newLine)
        {
            try
            {
                int? consoleWidth = GetConsoleWidth();
                if (consoleWidth.HasValue)
                {
                    // Console width detected, apply word wrapping
                    string wrappedText = WordWrap(text, consoleWidth.Value);
                    if (newLine)
                        Console.WriteLine(wrappedText);
                    else
                        Console.Write(wrappedText);
                }
                else
                {
                    // Unable to detect console width, fallback to normal behavior
                    if (newLine)
                        Console.WriteLine(text);
                    else
                        Console.Write(text);
                }
            }
            catch
            {
                // Ultimate fail-safe: if anything crashes, revert to direct console output
                if (newLine)
                    Console.WriteLine(text);
                else
                    Console.Write(text);
            }
        }

        private static int? GetConsoleWidth()
        {
            try
            {
                return Console.WindowWidth > 0 ? Console.WindowWidth : (int?)null;
            }
            catch
            {
                return null; // Null means console width is undetectable
            }
        }

        private static string WordWrap(string text, int maxWidth)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;

            StringBuilder sb = new StringBuilder();
            string[] words = text.Split(' ');
            int currentLineLength = 0;

            foreach (string word in words)
            {
                if (currentLineLength + word.Length >= maxWidth)
                {
                    sb.AppendLine();
                    currentLineLength = 0;
                }
                if (currentLineLength > 0)
                {
                    sb.Append(' ');
                    currentLineLength++;
                }
                sb.Append(word);
                currentLineLength += word.Length;
            }
            return sb.ToString();
        }


        public static MagicReadLineResponse<T> Read<T>(string? description = null, string? prompt = null)
        {
            WriteLine();

            if (!string.IsNullOrWhiteSpace(description))
            {
                foreach (var line in description.Split('\n'))
                {
                    WriteLine(line.Trim());
                }
                WriteLine(); // Extra spacing after description
            }

            WriteLine("Press Ctrl+X to cancel.");

            // Process the prompt (clean input)
            if (!string.IsNullOrWhiteSpace(prompt))
            {
                prompt = prompt.Trim().TrimEnd(':').Trim();
            }

            string inputPrompt = $"Enter {typeof(T).Name.ToLower()} {prompt}: ";

            while (true)
            {
                Write(inputPrompt);

                string? input = ReadInputWithCancel();
                if (input == null)
                {
                    return new MagicReadLineResponse<T>(); // Canceled
                }

                try
                {
                    // Convert input to T and return
                    T result = (T)Convert.ChangeType(input, typeof(T));
                    return new MagicReadLineResponse<T>(result);
                }
                catch
                {
                    WriteLine($"Invalid input. Expected {typeof(T).Name}, but received '{input}'. Try again.");
                }
            }
        }

        public static MagicReadLineResponse<string> Read(string? description = null, string? prompt = null)
        {
            return Read<string>(description, prompt);
        }

        private static string? ReadInputWithCancel()
        {
            string input = "";
            while (true)
            {
                var keyInfo = Console.ReadKey(intercept: true);

                if ((keyInfo.Modifiers & ConsoleModifiers.Control) != 0 && keyInfo.Key == ConsoleKey.X)
                {
                    WriteLine("\nOperation canceled.");
                    return null; // Cancellation detected
                }

                if (keyInfo.Key == ConsoleKey.Enter)
                {
                    WriteLine(); // Move to new line
                    return input.Trim();
                }
                else if (keyInfo.Key == ConsoleKey.Backspace && input.Length > 0)
                {
                    input = input[..^1]; // Remove last character
                    Write("\b \b"); // Remove character from console display
                }
                else if (!char.IsControl(keyInfo.KeyChar))
                {
                    input += keyInfo.KeyChar;
                    Write(keyInfo.KeyChar.ToString()); // Display typed character
                }
            }
        }
    }
}
