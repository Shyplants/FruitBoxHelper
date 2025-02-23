using FruitBoxHelper.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace FruitBoxHelper
{
    public partial class OverlayWindow : Window
    {
        private OverlayDrawManager _overlayDrawManager;
        public OverlayDrawManager overlayDrawManager
        {
            get { return _overlayDrawManager; }
            set
            {
                if(value != null)
                {
                    _overlayDrawManager = value;
                }
            }
        }

        public OverlayWindow()
        {
            InitializeComponent();

            OverlayWindow_Loaded();
            _overlayDrawManager = new OverlayDrawManager(this);
        }

        private void OverlayWindow_Loaded()
        {
            this.WindowStyle = WindowStyle.None;
            this.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            this.AllowsTransparency = true;

            this.Topmost = true;
            this.ShowInTaskbar = false;
            this.ResizeMode = ResizeMode.NoResize;

            this.Left = 0;
            this.Top = 0;
            this.Width = SystemParameters.PrimaryScreenWidth;
            this.Height = SystemParameters.PrimaryScreenHeight;
        }

        public void DrawRectangle(float left, float top, float width, float height, Color strokeColor, double strokeThickness = 2.0)
        {
            _overlayDrawManager.DrawRectangle(left, top, width, height, strokeColor, strokeThickness);
        }
    }
}
