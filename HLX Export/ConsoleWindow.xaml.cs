using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace HLXExport
{

    public delegate void ConsoleLogEvent(ConsoleLogData e);

    public enum ConsoleLogLevel { WARN, ERR, INFO };

    public struct ConsoleLogData
    {
        public ConsoleLogLevel LogLevel;
        public string Message;
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

        public static void Error(string message)
        {
            ConsoleLogData consoleLogData = new()
            {
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

            if (_instance != null)
                Trace.TraceError("Some shit got fucked up second instance of console logger created");

            _instance = this;

            Debug.LogEvent += HandleConsoleEvent;
        }

        protected void HandleConsoleEvent(ConsoleLogData data)
        {
            Run line = new Run(data.Message + '\n');
            if (data.LogLevel == ConsoleLogLevel.ERR)
            {
                line.Foreground = Brushes.Red;
            }

            if (data.LogLevel == ConsoleLogLevel.WARN)
            {
                line.Foreground = Brushes.Orange;
            }

            if (data.LogLevel == ConsoleLogLevel.INFO)
            {
                line.Foreground = Brushes.WhiteSmoke;
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
