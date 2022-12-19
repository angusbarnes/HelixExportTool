using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace HLXExport
{
    public delegate void ConsoleLogEvent(ConsoleLogData e);

    public enum ConsoleLogLevel { WARN, ERR, INFO };

    public struct ConsoleLogData
    {
        public ConsoleLogLevel LogLevel;
        public string Message;
        public string ColorCode;
    }

    public static class Debug
    {
        public static event ConsoleLogEvent LogEvent;
        public static void Warn(string message)
        {
            ConsoleLogData consoleLogData = new()
            {
                Message = "WARN: " + message,
                LogLevel = ConsoleLogLevel.WARN
            };
            LogEvent?.Invoke(consoleLogData);
        }

        public static void Log(string message)
        {
            ConsoleLogData consoleLogData = new()
            {
                Message = "INFO: " + message,
                LogLevel = ConsoleLogLevel.INFO
            };
            LogEvent?.Invoke(consoleLogData);
        }

        public static void Status(string message)
        {
            ConsoleLogData consoleLogData = new()
            {
                Message = "System Status: " + message,
                LogLevel = ConsoleLogLevel.WARN,
                ColorCode = "#34eb34"
            };
            LogEvent?.Invoke(consoleLogData);
        }

        public static void Notify(string message) {
            ConsoleLogData consoleLogData = new() {
                Message = "INFO: " + message,
                LogLevel = ConsoleLogLevel.WARN,
                ColorCode = "#faf219"
            };
            LogEvent?.Invoke(consoleLogData);
        }

        public static void Error(string message) {
            ConsoleLogData consoleLogData = new() {
                Message = "ERROR: " + message,
                LogLevel = ConsoleLogLevel.ERR
            };
            LogEvent?.Invoke(consoleLogData);
        }
    }
    /// <summary>
    /// Interaction logic for ConsoleWindow.xaml
    /// </summary>
    public partial class ConsoleWindow : Window
    {
        private static ConsoleWindow _instance;

        public ConsoleWindow()
        {
            InitializeComponent();

            this.Left = SystemParameters.PrimaryScreenWidth - this.Width;
            this.Top = 0;

            if (_instance != null)
                Trace.TraceError("Some shit got fucked up second instance of console logger created");

            _instance = this;

            Debug.LogEvent += HandleConsoleEvent;

            DispatcherTimer dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += DispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 5);
            dispatcherTimer.Start();
        }

        private void DispatcherTimer_Tick(object? sender, EventArgs e)
        {
            float memory = GC.GetTotalMemory(false);
            this.Title = "Console Window | Memory Usage: " + (memory/1024f)/1024f;
        }

        protected void HandleConsoleEvent(ConsoleLogData data)
        {
            Run line = new Run(data.Message + '\n');

            if (data.ColorCode == null) {
                switch (data.LogLevel) {
                    case ConsoleLogLevel.WARN:
                        line.Foreground = Brushes.Orange;
                        break;
                    case ConsoleLogLevel.ERR:
                        line.Foreground = Brushes.Red;
                        break;
                    case ConsoleLogLevel.INFO:
                        line.Foreground = Brushes.WhiteSmoke;
                        break;
                    default:
                        throw new NotImplementedException("You forgot a usecase dumbass");
                }
            } else {
                // TODO: This is fucking horrific and needs fixing asap
                line.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(data.ColorCode));
            }
                

            AppendElement(line);
        }

        public void AppendText(string input)
        {
            ConsoleTextBuffer.Text += input + '\n';
            ScrollContainer.ScrollToEnd();
        }

        public void AppendElement(Run element)
        {
            ConsoleTextBuffer.Inlines.Add(element);
            ScrollContainer.ScrollToEnd();
        }

        public static void WriteToBuffer(string input)
        {
            _instance.AppendText(input);
        }

        public bool IsClosed { get; private set; }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            IsClosed = true;
        }
    }
}
