using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magic.Cli.Toolkit
{
    public class CliMenu
    {
        public string Title { get; set; }
        public string? Description { get; set; }
        public List<MenuOption> Options { get; set; }
        public bool ClearScreen { get; set; }

        public CliMenu(string title, string? description = null, bool clearScreen = true)
        {
            Title = title;
            Description = description;
            Options = new List<MenuOption>();
            ClearScreen = clearScreen;
        }

        public void AddOption(string label, Func<Task> action)
        {
            Options.Add(new MenuOption(label, action));
        }

        public void AddOption(string label, Action action)
        {
            Options.Add(new MenuOption(label, action));
        }

        public async Task ShowAsync()
        {
            while (true)
            {
                if (ClearScreen)
                {
                    Console.Clear();
                }
                else
                {
                    Console.WriteLine(); // Empty line for spacing
                }

                // Render Title
                Console.WriteLine($"=== {Title} ===");

                // Render Description if available
                if (!string.IsNullOrEmpty(Description))
                {
                    foreach (var line in Description.Split('\n'))
                    {
                        Console.WriteLine(line);
                    }
                    Console.WriteLine(); // Space after description
                }

                // Render Menu Options
                for (int i = 0; i < Options.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {Options[i].Label}");
                }

                Console.WriteLine();
                Console.Write("Choice: ");

                if (int.TryParse(Console.ReadLine(), out int choice) && choice >= 1 && choice <= Options.Count)
                {
                    await Options[choice - 1].ExecuteAsync();
                }
                else
                {
                    Console.WriteLine($"Invalid choice. Please enter a number between 1 and {Options.Count}. Retrying soon...");
                    await Task.Delay(3500); // Small delay to allow user to read the message
                }
            }
        }
    }
}
