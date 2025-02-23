using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace FruitBoxHelper.Managers
{
    public class OverlayDrawManager
    {
        private OverlayWindow _owner;

        public OverlayDrawManager(OverlayWindow owner)
        {
            _owner = owner;
        }

        public void DrawRectangle(float left, float top, float width, float height, Color strokeColor, double strokeThickness = 2.0)
        {
            Rectangle rect = new Rectangle
            {
                Width = width,
                Height = height,
                Stroke = new SolidColorBrush(strokeColor),
                StrokeThickness = strokeThickness
            };


            Canvas.SetLeft(rect, left);
            Canvas.SetTop(rect, top);

            _owner.overlayCanvas.Children.Add(rect);
        }
    }
}
