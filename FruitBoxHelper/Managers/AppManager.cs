using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Media;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Color = System.Drawing.Color;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV;
using Tesseract;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FruitBoxHelper.Managers
{
    public enum AppState
    {
        Idle = 0,
        Detecting,
        Detected,
        Scoring,
        Scored,
        Proceed,
        GameOver,
        Paused
    }

    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public struct BOARD
    {
        public RECT rect;
        public int width;
        public int height;
        public float aspectRatio;
    }

    public struct Case
    {
        public Point beginIndex;
        public Point endIndex;
    }

    public struct Drag
    {
        public Point begin;
        public Point end;
    }

    class AppManager
    {
        private MainWindow _mainWindow;
        private OverlayWindow _overlayWindow;

        public event EventHandler? AppStateChanged;
        private AppState _appState;
        public AppState AppState
        {
            get { return _appState; }
            set
            {
                if(_appState != value)
                {
                    _appState = value;
                    AppStateChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        // 녹색 테두리 포함 보드
        private BOARD _gameBoard = new BOARD();
        // 17x10 사과 보드
        private BOARD _appleBoard = new BOARD();

        public static int rows = 10;
        public static int cols = 17;

        // 1-based index
        private int[,] _appleCosts = new int[rows + 1, cols + 1];
        private int[,] _appleExists = new int[rows + 1, cols + 1];
        private RECT[,] _appleRects = new RECT[rows + 1, cols + 1];
        private int[,] _pAppleCostSum = new int[rows + 1, cols + 1];
        private int[,] _pAppleCountSum = new int[rows + 1, cols + 1];


        private PriorityQueue<Case, int> _pq = new PriorityQueue<Case, int>(); // {Case, Cost}
        private int _score;
        public int score { get { return _score; } }
        Queue<Drag> _dragQueue = new Queue<Drag>();

        public AppManager(MainWindow mainWindow, OverlayWindow overlayWindow)
        {
            _mainWindow = mainWindow;
            _overlayWindow = overlayWindow;

            _appState = AppState.Idle;
        }

        public bool TryGameBoardDetection()
        {
            Rectangle screenRect = _mainWindow.screenRect;
            Bitmap screenshot = new Bitmap(screenRect.Width, screenRect.Height);
            
            using (Graphics g = Graphics.FromImage(screenshot))
            {
                g.CopyFromScreen(screenRect.Location, Point.Empty, screenRect.Size);
            }

#if DEBUG
            screenshot.Save("screenshot.png");
#endif

            int min_X = int.MaxValue;
            int min_Y = int.MaxValue;
            int max_X = int.MinValue;
            int max_Y = int.MinValue;

            // 화면 중앙 아래에서 위로 올라가면서 녹색 테두리 가장 아래와 가장 위의 y 좌표 찾기
            for (int y = screenshot.Height - 1; y >= screenshot.Height / 10; --y)
            {
                int x = screenshot.Width / 2;
                Color pixel = screenshot.GetPixel(x, y);

                if (pixel.R < 40 && pixel.G >= 150 && pixel.B < 150)
                {
                    if(max_Y == int.MinValue)
                    {
                        max_Y = y;
                    }
                    else
                    {
                        min_Y = y;
                    }
                }
            }

            // 녹색 테두리 중앙 높이에서 화면 좌우로 이동하면서 녹색 테두리 가장 왼쪽과 가장 오른쪽의 x 좌표 찾기
            for(int x=0; x<screenshot.Width; ++x)
            {
                int y = (min_Y + max_Y) / 2;
                Color pixel = screenshot.GetPixel(x, y);
                if (pixel.R < 40 && pixel.G >= 150 && pixel.B < 150)
                {
                    if (min_X == int.MaxValue)
                    {
                        min_X = x;
                    }
                    else
                    {
                        max_X = x;
                    }
                }
            }

            int width = max_X - min_X;
            int height = max_Y - min_Y;
            float aspectRatio = (float)width / height;

            bool ret = (1.53f < aspectRatio && aspectRatio < 1.55f);
            if(ret)
            {
                _gameBoard.rect.Left = (int)(min_X / _mainWindow.monitorScale);
                _gameBoard.rect.Top = (int)(min_Y / _mainWindow.monitorScale);
                _gameBoard.rect.Right = (int)(max_X / _mainWindow.monitorScale);
                _gameBoard.rect.Bottom = (int)(max_Y / _mainWindow.monitorScale);
                _gameBoard.width = (int)(width / _mainWindow.monitorScale);
                _gameBoard.height = (int)(height / _mainWindow.monitorScale);
                _gameBoard.aspectRatio = aspectRatio;

                float dx = _gameBoard.width / 200.0f;
                float dy = _gameBoard.height / 200.0f;

                _appleBoard.rect.Left = _gameBoard.rect.Left + (int)(18 * dx);
                _appleBoard.rect.Top = _gameBoard.rect.Top + (int)(29 * dy);
                _appleBoard.rect.Right = _gameBoard.rect.Right - (int)(23 * dx);
                _appleBoard.rect.Bottom = _gameBoard.rect.Bottom - (int)(27 * dy);
                _appleBoard.width = _appleBoard.rect.Right - _appleBoard.rect.Left;
                _appleBoard.height = _appleBoard.rect.Bottom - _appleBoard.rect.Top;
                _appleBoard.aspectRatio = (float)_appleBoard.width / (float)_appleBoard.height;

                dx = (float)_appleBoard.width / cols;
                dy = (float)_appleBoard.height / rows;
                float ddx = dx * 0.5f;
                float ddy = dy * 0.5f;
                for(int y=0;  y<rows; ++y)
                {
                    for(int x=0; x<cols; ++x)
                    {
                        float sx = _appleBoard.rect.Left + (int)(x * dx) + ddx * 0.5f;
                        float sy = _appleBoard.rect.Top + (int)(y * dy) + ddy * 0.5f;

                        _appleRects[y + 1, x + 1].Left = (int)(sx);
                        _appleRects[y + 1, x + 1].Top = (int)(sy);
                        _appleRects[y + 1, x + 1].Right = (int)(sx + ddx);
                        _appleRects[y + 1, x + 1].Bottom = (int)(sy + ddy);
                    }
                }

#if DEBUG
                _overlayWindow.DrawRectangle(_gameBoard.rect.Left, _gameBoard.rect.Top, _gameBoard.width, _gameBoard.height, Colors.Green);
                _overlayWindow.DrawRectangle(_appleBoard.rect.Left, _appleBoard.rect.Top, _appleBoard.width, _appleBoard.height, Colors.DarkRed);

                for (int y = 1; y <= rows; ++y)
                {
                    for (int x = 1; x <= cols; ++x)
                    {
                        _overlayWindow.DrawRectangle(_appleRects[y, x].Left, _appleRects[y, x].Top, _appleRects[y, x].Right - _appleRects[y, x].Left, _appleRects[y, x].Bottom - _appleRects[y, x].Top, Colors.Aqua);
                    }
                }
#endif
            }

            return ret;
        }

        public bool TryAppleBoardDetection()
        {
            Rectangle screenRect = _mainWindow.screenRect;
            Bitmap screenshot = new Bitmap(screenRect.Width, screenRect.Height);

            using (Graphics g = Graphics.FromImage(screenshot))
            {
                g.CopyFromScreen(screenRect.Location, Point.Empty, screenRect.Size);
            }

            int left = (int)(_appleBoard.rect.Left * _mainWindow.monitorScale);
            int top = (int)(_appleBoard.rect.Top * _mainWindow.monitorScale);
            int width = (int)(_appleBoard.width * _mainWindow.monitorScale);
            int height = (int)(_appleBoard.height * _mainWindow.monitorScale);

            Rectangle roi = new Rectangle(left, top, width, height);
            Bitmap roiBitmap = screenshot.Clone(roi, screenshot.PixelFormat);
            Mat roiBitmapMat = roiBitmap.ToMat();
            if (roiBitmapMat.NumberOfChannels == 4)
            {
                Mat covertedMat = new Mat();
                CvInvoke.CvtColor(roiBitmapMat, covertedMat, ColorConversion.Bgra2Bgr);
                roiBitmapMat = covertedMat;
            }

            Image<Bgr, byte> colorImage = new Image<Bgr, byte>(roiBitmapMat);

            Color lowerWhite = Color.FromArgb(245, 245, 245);
            Color upperWhite = Color.FromArgb(255, 255, 255);

            Bgr lowerWhiteBgr = new Bgr(lowerWhite);
            Bgr upperWhiteBgr = new Bgr(upperWhite);

            Image<Gray, byte> whiteMask = colorImage.InRange(lowerWhiteBgr, upperWhiteBgr);


#if DEBUG
            roiBitmap.Save("roi.png");
            whiteMask.Save("whiteMask.png");
#endif

            using (TesseractEngine ocrEngine = new TesseractEngine(@"./tessdata", "eng", EngineMode.LstmOnly))
            {
                ocrEngine.SetVariable("tessedit_char_whitelist", "123456789");
                using (var pix = PixConverter.ToPix(whiteMask.ToBitmap()))
                {
                    using (var page = ocrEngine.Process(pix, Tesseract.PageSegMode.SingleBlock))
                    {
                        string text = page.GetText();

                        // parse
                        string[] lines = text.Trim().Split('\n');
                        if (lines.Length == rows)
                        {
                            for (int i = 0; i < rows; ++i)
                            {
                                string[] numbers = lines[i].Trim().Split(' ');
                                if (numbers.Length == cols)
                                {
                                    for (int j = 0; j < cols; ++j)
                                    {
                                        _appleCosts[i + 1, j + 1] = int.Parse(numbers[j]);
                                    }
                                }
                            }
                        }

                        
                    }
                }
            }

            return true;
        }

        private void ClickPlayButton()
        {
            float dx = (float)_gameBoard.width / 40.0f;
            float dy = (float)_gameBoard.height / 40.0f;

            float x = _gameBoard.rect.Left + dx * 11.0f;
            float y = _gameBoard.rect.Top + dy * 22.0f;

            x *= _mainWindow.monitorScale;
            y *= _mainWindow.monitorScale;

            InputManager.LeftClickAt((int)x, (int)y);
        }

        private void ClickResetButton()
        {
            float dx = (float)_gameBoard.width / 40.0f;
            float dy = (float)_gameBoard.height / 40.0f;

            float x = _gameBoard.rect.Left + dx * 4.5f;
            float y = _gameBoard.rect.Top + dy * 38.5f;

#if DEBUG
            _overlayWindow.DrawRectangle((int)x, (int)y, 10, 10, Colors.Red, 4.0);
#endif

            x *= _mainWindow.monitorScale;
            y *= _mainWindow.monitorScale;

            InputManager.LeftClickAt((int)x, (int)y);
        }

        public void SetAppState(AppState state)
        {
            AppState = state;
        }

        private void FindAllCases()
        {
            for (int sy = 1; sy <= rows; ++sy)
            {
                for (int sx = 1; sx <= cols; ++sx)
                {
                    for (int ey = sy; ey <= rows; ++ey)
                    {
                        for (int ex = sx; ex <= cols; ++ex)
                        {
                            int costSum = _pAppleCostSum[ey, ex] - _pAppleCostSum[sy - 1, ex] - _pAppleCostSum[ey, sx - 1] + _pAppleCostSum[sy - 1, sx - 1];
                            if (costSum != 10) continue;

                            int useCount = _pAppleCountSum[ey, ex] - _pAppleCountSum[sy - 1, ex] - _pAppleCountSum[ey, sx - 1] + _pAppleCountSum[sy - 1, sx - 1];

                            int area = (ey - sy + 1) * (ex - sx + 1);
                            int cost = useCount * 10000 + area;
                            Case newCase = new Case();
                            newCase.beginIndex = new Point(sx, sy);
                            newCase.endIndex = new Point(ex, ey);

                            _pq.Enqueue(newCase, cost);
                        }
                    }
                }
            }
        }

        private void EraseApples(Point beginIndex, Point endIndex)
        {
            for (int y = beginIndex.Y; y <= endIndex.Y; ++y)
            {
                for (int x = beginIndex.X; x <= endIndex.X; ++x)
                {
                    _appleCosts[y, x] = 0;
                    _appleExists[y, x] = 0;
                }
            }

            for (int y = beginIndex.Y; y <= rows; ++y)
            {
                for (int x = beginIndex.X; x <= cols; ++x)
                {
                    _pAppleCostSum[y, x] = _pAppleCostSum[y - 1, x] + _pAppleCostSum[y, x - 1] - _pAppleCostSum[y - 1, x - 1] + _appleCosts[y, x];
                    _pAppleCountSum[y, x] = _pAppleCountSum[y - 1, x] + _pAppleCountSum[y, x - 1] - _pAppleCountSum[y - 1, x - 1] + _appleExists[y, x];
                }
            }
        }

        private (int, Queue<Drag>) Scoring()
        {
            int score = 0;
            Queue<Drag> dragQueue = new Queue<Drag>();

            // Initialize
            for (int y=1; y<=rows; ++y)
            {
                for (int x = 1; x <= cols; ++x)
                {
                    
                    _pAppleCostSum[y, x] = _pAppleCostSum[y - 1, x] + _pAppleCostSum[y, x - 1] - _pAppleCostSum[y - 1, x - 1] + _appleCosts[y, x];
                    _appleExists[y, x] = 1;
                    _pAppleCountSum[y, x] = _pAppleCountSum[y - 1, x] + _pAppleCountSum[y, x - 1] - _pAppleCountSum[y - 1, x - 1] + _appleExists[y, x];
                }
            }

            while(true)
            {
                Point beginIndex;
                Point endIndex;

                _pq.Clear();
                FindAllCases();
                if(_pq.Count == 0)
                {
                    break;
                }

                Case bestCase = _pq.Dequeue();
                beginIndex = bestCase.beginIndex;
                endIndex = bestCase.endIndex;

                Drag drag = new Drag();
                drag.begin = new Point(_appleRects[beginIndex.Y, beginIndex.X].Left, _appleRects[beginIndex.Y, beginIndex.X].Top);
                drag.end = new Point(_appleRects[endIndex.Y, endIndex.X].Right, _appleRects[endIndex.Y, endIndex.X].Bottom);
                dragQueue.Enqueue(drag);

                score += _pAppleCountSum[endIndex.Y, endIndex.X] - _pAppleCountSum[beginIndex.Y - 1, endIndex.X] - _pAppleCountSum[endIndex.Y, beginIndex.X - 1] + _pAppleCountSum[beginIndex.Y - 1, beginIndex.X - 1];
                EraseApples(beginIndex, endIndex);

            }


            return (score, dragQueue);
        }

        public void StartGame()
        {
            SetAppState(AppState.Scoring);

            ClickPlayButton();
            _mainWindow.Activate();

            // 0.5초 대기 후 게임 보드 탐색
            Thread.Sleep(500);

            if (TryAppleBoardDetection())
            {
                Thread scoringThread = new Thread(() =>
                {

                    (_score, _dragQueue) = Scoring();
                });
                scoringThread.IsBackground = true;
                scoringThread.Start();
                scoringThread.Join();
                SetAppState(AppState.Scored);


            }
            else
            {
                // TODO : Handle error   
            }
        }

        public void ProceedGame()
        {
            SetAppState(AppState.Proceed);

            while (_dragQueue.Count > 0)
            {
                Drag drag = _dragQueue.Dequeue();
                drag.begin.X = (int)(drag.begin.X * _mainWindow.monitorScale);
                drag.begin.Y = (int)(drag.begin.Y * _mainWindow.monitorScale);
                drag.end.X = (int)(drag.end.X * _mainWindow.monitorScale);
                drag.end.Y = (int)(drag.end.Y * _mainWindow.monitorScale);

                InputManager.DragMouse(drag.begin, drag.end);
            }

            _mainWindow.Activate();
            SetAppState(AppState.GameOver);
        }

        public void ResetGame()
        {
            ClickResetButton();

            _mainWindow.Activate();
            SetAppState(AppState.Detected);
        }

    }
}
