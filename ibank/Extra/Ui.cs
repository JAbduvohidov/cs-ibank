using System;
using System.Collections.Generic;

namespace ibank.Extra
{
    public static class Ui
    {
        private static void Draw(string header, int width, int height, int x, int y)
        {
            Console.SetCursorPosition(x, y);
            Console.WriteLine(" " + header);
            Box.Draw(width, height, x + 1, y + 1, "█", "▀");

            Console.SetCursorPosition(x + 3, y + 2);
        }

        public static string InputText(string header, int width = 20, int height = 3, int x = 0, int y = 0)
        {
            Draw(header, width, height, x, y);
            return Console.ReadLine();
        }

        public static string InputText(string header, int width, int height, int x, int y, string passwordCharacter)
        {
            Draw(header, width, height, x, y);

            var characters = string.Empty;
            ConsoleKey key;
            do
            {
                var keyInfo = Console.ReadKey(true);
                key = keyInfo.Key;

                if (key == ConsoleKey.Backspace && characters.Length > 0)
                {
                    Console.Write("\b \b");
                    characters = characters[..^1];
                }
                else if (!char.IsControl(keyInfo.KeyChar))
                {
                    Console.Write(passwordCharacter);
                    characters += keyInfo.KeyChar;
                }
            } while (key != ConsoleKey.Enter);

            return characters;
        }

        public static int ComboBox(List<string> items, int selectedIndex = 0,
            ConsoleColor selectedBackgroundColor = ConsoleColor.White,
            ConsoleColor selectedForegroundColor = ConsoleColor.Black,
            ConsoleColor color = ConsoleColor.White)
        {
            Console.CursorVisible = false;
            var currentForegroundColor = Console.ForegroundColor;
            var currentBackgroundColor = Console.BackgroundColor;
            Console.ForegroundColor = color;
            var (left, top) = Console.GetCursorPosition();

            while (true)
            {
                for (var i = 0; i < items.Count; i++)
                {
                    Console.SetCursorPosition(left, top + i);
                    if (i == selectedIndex)
                    {
                        Console.ForegroundColor = selectedForegroundColor;
                        Console.BackgroundColor = selectedBackgroundColor;
                        Console.Write($" ➩  {i + 1}. {items[i]}");
                        Console.ForegroundColor = currentForegroundColor;
                        Console.BackgroundColor = currentBackgroundColor;
                        Console.Write("\n");
                        continue;
                    }

                    Console.WriteLine($"    {i + 1}. {items[i]}");
                }

                Console.ForegroundColor = currentForegroundColor;


                var key = Console.ReadKey();

                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                    {
                        if (selectedIndex == 0)
                        {
                            selectedIndex = items.Count - 1;
                            break;
                        }

                        selectedIndex--;
                        break;
                    }
                    case ConsoleKey.DownArrow:
                    {
                        if (selectedIndex == items.Count - 1)
                        {
                            selectedIndex = 0;
                            break;
                        }

                        selectedIndex++;

                        break;
                    }
                    case ConsoleKey.Enter:
                    {
                        Console.CursorVisible = true;
                        return selectedIndex;
                    }
                }
            }
        }
    }
}