using FruitBoxHelper.Managers;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;

using Tesseract;
using Point = System.Drawing.Point;

namespace FruitBoxHelper;

public partial class MainWindow : Window
{
    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    // 상수: 모니터 가로 해상도, 세로 해상도
    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;

    private Rectangle _screenRect;
    public Rectangle screenRect
    {  
        get { return _screenRect; }
        set
        {
            if (value != null)
            {
                _screenRect = value;
            }
        }
    }

    public float monitorScale = 1.0f;

    private OverlayWindow _overlayWindow;
    private AppManager _appManager;

    public MainWindow()
    {
        InitializeComponent();

        int screenWidth = GetSystemMetrics(SM_CXSCREEN);
        int screenHeight = GetSystemMetrics(SM_CYSCREEN);
        _screenRect = new Rectangle(0, 0, screenWidth, screenHeight);
        monitorScale = (float)screenWidth / (float)SystemParameters.PrimaryScreenWidth;

        _overlayWindow = new OverlayWindow();
        _appManager = new AppManager(this, _overlayWindow);
        _appManager.AppStateChanged += AppManager_AppStateChanged;

        this.Closing += MainWindow_Closing;
    }

    private void ShowOverlayButton_Click(object sender, RoutedEventArgs e)
    {
        _overlayWindow.Show();
    }

    private void HideOverlayButton_Click(object sender, RoutedEventArgs e)
    {
        _overlayWindow.Hide();
    }

    private void MainWindow_Closing(object? sender, EventArgs e)
    {
        if (_overlayWindow != null)
        {
            _overlayWindow.Close();
        }
    }

    private void DetectionGameButton_Click(object sender, RoutedEventArgs e)
    {
        _appManager.SetAppState(AppState.Detecting);
        if (_appManager.TryGameBoardDetection())
        {
            _appManager.SetAppState(AppState.Detected);
        }
        else
        {
            _appManager.SetAppState(AppState.Idle);
            MessageBox.Show("Game board not detected!");
        }
    }

    private void AppManager_AppStateChanged(object? sender, EventArgs e)
    {
        switch(_appManager.AppState)
        {
            case AppState.Idle:
                AppStateTextBox.Text = "게임 탐지 X";
                break;
            case AppState.Detected:
                AppStateTextBox.Text = "게임 탐지 O";
                ScoreTextBox.Text = "";
                break;
            case AppState.Scoring:
                AppStateTextBox.Text = "예상 점수 계산 중";
                break;
            case AppState.Scored:
                AppStateTextBox.Text = "예상 점수 계산 완료";
                ScoreTextBox.Text = _appManager.score.ToString();
                break;
            case AppState.Proceed:
                AppStateTextBox.Text = "게임 진행 중";
                break;
            case AppState.GameOver:
                AppStateTextBox.Text = "플레이 끝남";
                break;
            case AppState.Paused:
                AppStateTextBox.Text = "일시정지";
                break;
        }

        DetectionGameButton.IsEnabled = (_appManager.AppState == AppState.Idle);
        PlayButton.IsEnabled = (_appManager.AppState == AppState.Detected);
        ProceedGameButton.IsEnabled = (_appManager.AppState == AppState.Scored);
        SkipGameButton.IsEnabled = (_appManager.AppState == AppState.Scored);
        ResetButton.IsEnabled = (_appManager.AppState == AppState.GameOver);
    }

    private void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        Point clickPoint = InputManager.GetMousePosition();
        _appManager.StartGame();
        InputManager.SetCursorPosition(clickPoint);
    }

    private void ProceedGameButton_Click(object sender, RoutedEventArgs e)
    {
        Point clickPoint = InputManager.GetMousePosition();
        _appManager.ProceedGame();
        InputManager.SetCursorPosition(clickPoint);
    }

    private void SkipGameButton_Click(object sender, RoutedEventArgs e)
    {
        Point clickPoint = InputManager.GetMousePosition();
        _appManager.ResetGame();
        _appManager.StartGame();
        InputManager.SetCursorPosition(clickPoint);
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        Point clickPoint = InputManager.GetMousePosition();
        _appManager.ResetGame();
        InputManager.SetCursorPosition(clickPoint);
    }
}