using System;

namespace ibank.Extra
{
    public static class Box
    {
        public static void Draw(int width, int height, int x, int y, string leftRight, string top)
        {
            for (var i = 0; i < height; i++)
            {
                Console.SetCursorPosition(x, y + i);
                if (i == 0 || i == height - 1)
                {
                    for (var j = 0; j < width; j++)
                        Console.Write(top);
                }
                else
                {
                    Console.Write(leftRight);
                    for (var k = 1; k < width - 1; k++)
                        Console.Write(" ");
                    Console.Write(leftRight);
                }

                Console.Write("\n");
            }
        }
    }
}