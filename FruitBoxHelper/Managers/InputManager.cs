using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Point = System.Drawing.Point;

namespace FruitBoxHelper.Managers
{
    public static class InputManager
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetCursorPos(int x, int y);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;

        public static Point GetMousePosition()
        {
            if (GetCursorPos(out POINT point))
            {
                return new Point(point.X, point.Y);
            }
            else
            {
                return new Point(0, 0);
            }
        }

        public static void SetCursorPosition(Point point)
        {
            SetCursorPos(point.X, point.Y);
        }

        public static void LeftClickAt(int x, int y)
        {
            bool success = SetCursorPos(x, y);

            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
            Thread.Sleep(100);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
        }

        public static void DragMouse(Point start, Point end)
        {
            Thread t = new Thread(() =>
            {
                SetCursorPos(start.X, start.Y);
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);

                int steps = 100;
                int totalTime = 500;
                int sleepTime = totalTime / steps;
                for (int i = 1; i <= steps; i++)
                {

                    int newX = start.X + ((end.X - start.X) * i / steps);
                    int newY = start.Y + ((end.Y - start.Y) * i / steps);
                    Point intermediate = new Point(newX, newY);

                    SetCursorPos(intermediate.X, intermediate.Y);
                    Thread.Sleep(sleepTime);
                }

                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
            });
            t.IsBackground = true;
            t.Start();
            t.Join();
        }
    }
}
