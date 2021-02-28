using System;

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

        public static string InputText(string header, int width, int height, int x, int y)
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
    }
}