using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Globalization;

namespace ClipboardCalc
{
    public partial class Settings : Window
    {
        public delegate void SettingsWindowApplyHandler(ClipboardCalcSettings settings);
        public event SettingsWindowApplyHandler Apply;

        ClipboardCalcSettings _settings;

        public Settings(ClipboardCalcSettings settings)
        {
            InitializeComponent();

            _settings = settings;

            Closing += delegate(object sender, System.ComponentModel.CancelEventArgs e) { e.Cancel = true; Hide(); };
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            radioButtonInputComma.IsChecked = _settings.InputDecimalSeperator == ',';
            radioButtonInputPeriod.IsChecked = _settings.InputDecimalSeperator == '.';
            radioButtonOutputComma.IsChecked = _settings.OutputDecimalSeperator == ',';
            radioButtonOutputPeriod.IsChecked = _settings.OutputDecimalSeperator == '.';
        }

        private void buttonApply_Click(object sender, RoutedEventArgs e)
        {
            var newSettings = new ClipboardCalcSettings(
                        new CultureInfo(radioButtonInputComma.IsChecked == true ? "nl-NL" : "en-US"),
                        new CultureInfo(radioButtonOutputComma.IsChecked == true ? "nl-NL" : "en-US")
                );
            newSettings.Save();

            if (Apply != null)
                Apply(newSettings);

            Hide();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }
    }
}
