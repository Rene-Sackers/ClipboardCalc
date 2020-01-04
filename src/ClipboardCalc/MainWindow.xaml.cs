using System;
using System.Globalization;
using System.Windows;
using ClipboardCalc.Maths;
using System.Windows.Forms;
using System.Net;
using System.Xml;
using Microsoft.Win32;
using Clipboard = System.Windows.Clipboard;
using MessageBox = System.Windows.MessageBox;

namespace ClipboardCalc
{
    public class ClipboardCalcSettings
    {
        public readonly CultureInfo InputCulture;
        public readonly CultureInfo OutputCulture;
        public char InputDecimalSeperator { get; private set; }
        public char OutputDecimalSeperator { get; private set; }

        private const string RegistryKey = @"HKEY_CURRENT_USER\Software\ClipboardCalc";
        private const string InputValue = "InputDecimal";
        private const string OutputValue = "OutputDecimal";

        public ClipboardCalcSettings()
        {
            string input = ".";
            string output = ".";

            try
            {
                input = (string)Registry.GetValue(RegistryKey, InputValue, ".");
                output = (string)Registry.GetValue(RegistryKey, OutputValue, ".");
            }
            catch
            {
                MessageBox.Show("Couldn't load settings.");
            }

            InputCulture = new CultureInfo(input == "," ? "nl-NL" : "en-US");
            OutputCulture = new CultureInfo(input == "," ? "nl-NL" : "en-US");
            GetSeperators();
        }

        public ClipboardCalcSettings(CultureInfo inputCulture, CultureInfo outputCulture)
        {
            InputCulture = inputCulture;
            OutputCulture = outputCulture;
            GetSeperators();
        }

        private void GetSeperators()
        {
            InputDecimalSeperator = InputCulture.NumberFormat.NumberDecimalSeparator[0];
            OutputDecimalSeperator = OutputCulture.NumberFormat.NumberDecimalSeparator[0];
        }

        public bool Save()
        {
            try
            {
                Registry.SetValue(RegistryKey, InputValue, InputDecimalSeperator, RegistryValueKind.String);
                Registry.SetValue(RegistryKey, OutputValue, OutputDecimalSeperator, RegistryValueKind.String);
                return true;
            }
            catch
            {
                MessageBox.Show("Couldn't save settings.");
                return false;
            }
        }
    }
    public partial class MainWindow
    {
        private readonly Hook _hook = new Hook();
        ClipboardCalcSettings _settings = new ClipboardCalcSettings();
        Settings _settingsWindow;

        public MainWindow()
        {
            InitializeComponent();

            _settingsWindow = new Settings(_settings);
            _settingsWindow.Apply += delegate(ClipboardCalcSettings settings) { _settings = settings; };

            CreateTrayIcon();

            Loaded += MainWindowLoaded;

            Closing += MainWindowClosing;
        }

        void MainWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _settingsWindow.Close();
            _icon.Visible = false;
            System.Windows.Application.Current.Shutdown();
        }

        void MainWindowLoaded(object sender, RoutedEventArgs e)
        {
            _hook.StartHook(this);
            _hook.ClipboardChanged += HookClipboardChanged;

            Visibility = Visibility.Hidden;
        }

        #region Tray Icon

        NotifyIcon _icon;
        void CreateTrayIcon()
        {
            _icon = new NotifyIcon();
            var streamResourceInfo = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,/Icon.ico"));
            if (streamResourceInfo == null) return;
            var stream = streamResourceInfo.Stream;
            _icon.Icon = new System.Drawing.Icon(stream);

            var menu = new ContextMenu();

            _icon.DoubleClick += delegate(object sender, EventArgs e)
            {
                _settingsWindow.Show();
            };

            var settingsMenuItem = new MenuItem("Settings");
            settingsMenuItem.Click += delegate(object sender, EventArgs e)
            {
                _settingsWindow.Show();
            };
            menu.MenuItems.Add(settingsMenuItem);

            var closeWindowMenuItem = new MenuItem("Exit");
            closeWindowMenuItem.Click += TrayIconExit;
            menu.MenuItems.Add(closeWindowMenuItem);

            _icon.ContextMenu = menu;

            _icon.Visible = true;
        }

        void TrayIconExit(object sender, EventArgs e)
        {
            _icon.Visible = false;
            Close();
        }

        #endregion

        #region General Stuff

        void HookClipboardChanged()
        {
			string clipboardText;
			try
			{
				clipboardText = Clipboard.GetText();
			}
			catch (Exception)
			{
				return;
			}

			if (!IsCalculation(clipboardText))
				return;

			try
			{
				var result = Calculate(clipboardText.Substring(1, clipboardText.Length - 2)).ToString(_settings.OutputCulture);
				Clipboard.SetText(result);
			}
			catch
			{
				try
				{
					Clipboard.SetText("f");
				}
				catch
				{
				}
			}

			SendKeys.SendWait(new Microsoft.VisualBasic.Devices.Keyboard().CtrlKeyDown ? "v" : "^v");
		}

        static bool IsCalculation(string operation)
        {
            return operation.StartsWith("(") && operation.EndsWith(")");
        }

        double Calculate(string operation)
        {
            try
            {
                // Evaluate the current expression
                var eval = new Eval() { Culture = _settings.InputCulture };
                eval.ProcessSymbol += ProcessSymbol;
                eval.ProcessFunction += ProcessFunction;
                return eval.Execute(operation);
            }
            catch
            {
                throw new Exception();
            }
        }

        #endregion

        #region Calculation Functions

        // Implement expression symbols
        protected void ProcessSymbol(object sender, SymbolEventArgs e)
        {
            if (String.Compare(e.Name, "pi", StringComparison.OrdinalIgnoreCase) == 0)
            {
                e.Result = Math.PI;
            }
            // Unknown symbol name
            else e.Status = SymbolStatus.UndefinedSymbol;
        }

        // Implement expression functions
        protected void ProcessFunction(object sender, FunctionEventArgs e)
        {
            if (String.Compare(e.Name, "abs", StringComparison.OrdinalIgnoreCase) == 0)
            {
                if (e.Parameters.Count == 1)
                    e.Result = Math.Abs(e.Parameters[0]);
                else
                    e.Status = FunctionStatus.WrongParameterCount;
            }
            else if (String.Compare(e.Name, "pow", StringComparison.OrdinalIgnoreCase) == 0)
            {
                if (e.Parameters.Count == 2)
                    e.Result = Math.Pow(e.Parameters[0], e.Parameters[1]);
                else
                    e.Status = FunctionStatus.WrongParameterCount;
            }
            else if (String.Compare(e.Name, "round", StringComparison.OrdinalIgnoreCase) == 0)
            {
                if (e.Parameters.Count == 1)
                    e.Result = Math.Round(e.Parameters[0]);
                else
                    e.Status = FunctionStatus.WrongParameterCount;
            }
            else if (String.Compare(e.Name, "sqrt", StringComparison.OrdinalIgnoreCase) == 0)
            {
                if (e.Parameters.Count == 1)
                    e.Result = Math.Sqrt(e.Parameters[0]);
                else
                    e.Status = FunctionStatus.WrongParameterCount;
            }
            // Unknown function name
            else e.Status = FunctionStatus.UndefinedFunction;
        }

        #endregion
    }
}
