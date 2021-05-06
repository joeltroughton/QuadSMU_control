using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using System.Globalization;
using System.Text.RegularExpressions;

namespace QuadSMU_control
{
    /// <summary>
    /// Interaction logic for stability_settings.xaml
    /// </summary>
    public partial class stability_settings : Window
    {
        public String[] polarity_combobox_list = { "Positive", "Negative" };
        public String[] hold_condition_combobox_list = { "VOC", "ISC", "MPP" };


        public class x_stability_sweep_parameters
        {
            public double ch1_start_v;
            public double ch2_start_v;
            public double ch3_start_v;
            public double ch4_start_v;

            public double ch1_end_v;
            public double ch2_end_v;
            public double ch3_end_v;
            public double ch4_end_v;

            public double ch1_step_mv;
            public double ch2_step_mv;
            public double ch3_step_mv;
            public double ch4_step_mv;

            public double ch1_delay_ms;
            public double ch2_delay_ms;
            public double ch3_delay_ms;
            public double ch4_delay_ms;

            public double ch1_ilim_ma;
            public double ch2_ilim_ma;
            public double ch3_ilim_ma;
            public double ch4_ilim_ma;

            public double ch1_osr;
            public double ch2_osr;
            public double ch3_osr;
            public double ch4_osr;

            public double ch1_area_cm2;
            public double ch2_area_cm2;
            public double ch3_area_cm2;
            public double ch4_area_cm2;

            public int ch1_polarity;
            public int ch2_polarity;
            public int ch3_polarity;
            public int ch4_polarity;

            public int ch1_hold_state;
            public int ch2_hold_state;
            public int ch3_hold_state;
            public int ch4_hold_state;




        }

        MainWindow.stability_sweep_parameters stability_params = new MainWindow.stability_sweep_parameters();


        public stability_settings(MainWindow.stability_sweep_parameters incoming_stability_params)
        {
            InitializeComponent();
            ch1_polarity_box.ItemsSource = polarity_combobox_list;
            ch2_polarity_box.ItemsSource = polarity_combobox_list;
            ch3_polarity_box.ItemsSource = polarity_combobox_list;
            ch4_polarity_box.ItemsSource = polarity_combobox_list;

            ch1_hold_box.ItemsSource = hold_condition_combobox_list;
            ch2_hold_box.ItemsSource = hold_condition_combobox_list;
            ch3_hold_box.ItemsSource = hold_condition_combobox_list;
            ch4_hold_box.ItemsSource = hold_condition_combobox_list;

            //MainWindow.stability_sweep_parameters stability_params = new MainWindow.stability_sweep_parameters();
            stability_params = incoming_stability_params;

            if (stability_params.params_accessed_count > 0)
            {
                // Load parameters into textboxes
                irradience.Text = stability_params.stability_irradience.ToString("0.00");

                ch1_start_v.Text = stability_params.ch1_start_v.ToString("0.00");
                ch2_start_v.Text = stability_params.ch2_start_v.ToString("0.00");
                ch3_start_v.Text = stability_params.ch3_start_v.ToString("0.00");
                ch4_start_v.Text = stability_params.ch4_start_v.ToString("0.00");

                ch1_end_v.Text = stability_params.ch1_end_v.ToString("0.00");
                ch2_end_v.Text = stability_params.ch2_end_v.ToString("0.00");
                ch3_end_v.Text = stability_params.ch3_end_v.ToString("0.00");
                ch4_end_v.Text = stability_params.ch4_end_v.ToString("0.00");

                ch1_step_mv.Text = stability_params.ch1_step_mv.ToString("0");
                ch2_step_mv.Text = stability_params.ch2_step_mv.ToString("0");
                ch3_step_mv.Text = stability_params.ch3_step_mv.ToString("0");
                ch4_step_mv.Text = stability_params.ch4_step_mv.ToString("0");

                ch1_delay_ms.Text = stability_params.ch1_delay_ms.ToString("0");
                ch2_delay_ms.Text = stability_params.ch2_delay_ms.ToString("0");
                ch3_delay_ms.Text = stability_params.ch3_delay_ms.ToString("0");
                ch4_delay_ms.Text = stability_params.ch4_delay_ms.ToString("0");

                ch1_ilim_ma.Text = stability_params.ch1_ilim_ma.ToString("0");
                ch2_ilim_ma.Text = stability_params.ch2_ilim_ma.ToString("0");
                ch3_ilim_ma.Text = stability_params.ch3_ilim_ma.ToString("0");
                ch4_ilim_ma.Text = stability_params.ch4_ilim_ma.ToString("0");

                ch1_osr.Text = stability_params.ch1_osr.ToString("0");
                ch2_osr.Text = stability_params.ch2_osr.ToString("0");
                ch3_osr.Text = stability_params.ch3_osr.ToString("0");
                ch4_osr.Text = stability_params.ch4_osr.ToString("0");

                ch1_area_cm2.Text = stability_params.ch1_area_cm2.ToString("0.0");
                ch2_area_cm2.Text = stability_params.ch2_area_cm2.ToString("0.0");
                ch3_area_cm2.Text = stability_params.ch3_area_cm2.ToString("0.0");
                ch4_area_cm2.Text = stability_params.ch4_area_cm2.ToString("0.0");

                ch1_polarity_box.SelectedIndex = stability_params.ch1_polarity;
                ch2_polarity_box.SelectedIndex = stability_params.ch2_polarity;
                ch3_polarity_box.SelectedIndex = stability_params.ch3_polarity;
                ch4_polarity_box.SelectedIndex = stability_params.ch4_polarity;

                ch1_hold_box.SelectedIndex = stability_params.ch1_hold_state;
                ch2_hold_box.SelectedIndex = stability_params.ch2_hold_state;
                ch3_hold_box.SelectedIndex = stability_params.ch3_hold_state;
                ch4_hold_box.SelectedIndex = stability_params.ch4_hold_state;

            }

        }

        private void set_all_ch1_button(object sender, RoutedEventArgs e)
        {
            ch2_start_v.Text = ch3_start_v.Text = ch4_start_v.Text = ch1_start_v.Text;
            ch2_end_v.Text = ch3_end_v.Text = ch4_end_v.Text = ch1_end_v.Text;
            ch2_step_mv.Text = ch3_step_mv.Text = ch4_step_mv.Text = ch1_step_mv.Text;
            ch2_delay_ms.Text = ch3_delay_ms.Text = ch4_delay_ms.Text = ch1_delay_ms.Text;
            ch2_ilim_ma.Text = ch3_ilim_ma.Text = ch4_ilim_ma.Text = ch1_ilim_ma.Text;
            ch2_osr.Text = ch3_osr.Text = ch4_osr.Text = ch1_osr.Text;
            ch2_area_cm2.Text = ch3_area_cm2.Text = ch4_area_cm2.Text = ch1_area_cm2.Text;
            ch2_polarity_box.SelectedIndex = ch3_polarity_box.SelectedIndex = ch4_polarity_box.SelectedIndex = ch1_polarity_box.SelectedIndex;
            ch2_hold_box.SelectedIndex = ch3_hold_box.SelectedIndex = ch4_hold_box.SelectedIndex = ch1_hold_box.SelectedIndex;
        }

        private void export_parameters_button(object sender, RoutedEventArgs e)
        {
            var settings_csv = new StringBuilder();

            var newLine = string.Format("Parameter, Channel 1, Channel 2, Channel 3, Channel 4");
            settings_csv.AppendLine(newLine);

            newLine = string.Format("Start V, {0}, {1}, {2}, {3}", ch1_start_v.Text, ch2_start_v.Text, ch3_start_v.Text, ch4_start_v.Text);
            settings_csv.AppendLine(newLine);

            newLine = string.Format("End V, {0}, {1}, {2}, {3}", ch1_end_v.Text, ch2_end_v.Text, ch3_end_v.Text, ch4_end_v.Text);
            settings_csv.AppendLine(newLine);

            newLine = string.Format("Step size (mV), {0}, {1}, {2}, {3}", ch1_step_mv.Text, ch2_step_mv.Text, ch3_step_mv.Text, ch4_step_mv.Text);
            settings_csv.AppendLine(newLine);

            newLine = string.Format("Delay time (ms), {0}, {1}, {2}, {3}", ch1_delay_ms.Text, ch2_delay_ms.Text, ch3_delay_ms.Text, ch4_delay_ms.Text);
            settings_csv.AppendLine(newLine);

            newLine = string.Format("Current limit (mA), {0}, {1}, {2}, {3}", ch1_ilim_ma.Text, ch2_ilim_ma.Text, ch3_ilim_ma.Text, ch4_ilim_ma.Text);
            settings_csv.AppendLine(newLine);

            newLine = string.Format("Oversample rate, {0}, {1}, {2}, {3}", ch1_osr.Text, ch2_osr.Text, ch3_osr.Text, ch4_osr.Text);
            settings_csv.AppendLine(newLine);

            newLine = string.Format("Active area (cm2), {0}, {1}, {2}, {3}", ch1_area_cm2.Text, ch2_area_cm2.Text, ch3_area_cm2.Text, ch4_area_cm2.Text);
            settings_csv.AppendLine(newLine);

            newLine = string.Format("Polarity, {0}, {1}, {2}, {3}", ch1_polarity_box.SelectedIndex, ch2_polarity_box.SelectedIndex, ch3_polarity_box.SelectedIndex, ch4_polarity_box.SelectedIndex);
            settings_csv.AppendLine(newLine);

            newLine = string.Format("Hold condition, {0}, {1}, {2}, {3}", ch1_hold_box.SelectedIndex, ch2_hold_box.SelectedIndex, ch3_hold_box.SelectedIndex, ch4_hold_box.SelectedIndex);
            settings_csv.AppendLine(newLine);

            newLine = string.Format("");
            settings_csv.AppendLine(newLine);

            newLine = string.Format("Irradience, {0}", irradience.Text);
            settings_csv.AppendLine(newLine);

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Comma separated value (*.csv)|*.csv";

            if (saveFileDialog.ShowDialog() == true)

                File.WriteAllText(saveFileDialog.FileName, settings_csv.ToString());
        }

        private void import_parameters_button(object sender, RoutedEventArgs e)
        {
            // This is so crude.
            // TODO: Get settings CSV in a format that can be read straight into a class by csvhelper

            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {

            }

            try
            {
                var lines = File.ReadLines(openFileDialog.FileName);
                bool first_line = true;

                string[] read_lines;
                List<string> read_lines_list = new List<string>();

                foreach (string line in lines)
                {
                    if (first_line)
                    {
                        first_line = false;
                    }
                    else
                    {
                        read_lines_list.Add(line);
                    }
                }

                read_lines = read_lines_list.ToArray();

                string[] start_v = read_lines[0].Split(",");
                string[] end_v = read_lines[1].Split(",");
                string[] step_mv = read_lines[2].Split(",");
                string[] delay_ms = read_lines[3].Split(",");
                string[] ilim_ma = read_lines[4].Split(",");
                string[] osr = read_lines[5].Split(",");
                string[] active_area = read_lines[6].Split(",");
                string[] polarity = read_lines[7].Split(",");
                string[] hold = read_lines[8].Split(",");
                string[] str_irradience = read_lines[10].Split(",");


                ch1_start_v.Text = start_v[1];
                ch2_start_v.Text = start_v[2];
                ch3_start_v.Text = start_v[3];
                ch4_start_v.Text = start_v[4];

                ch1_end_v.Text = end_v[1];
                ch2_end_v.Text = end_v[2];
                ch3_end_v.Text = end_v[3];
                ch4_end_v.Text = end_v[4];

                ch1_step_mv.Text = step_mv[1];
                ch2_step_mv.Text = step_mv[2];
                ch3_step_mv.Text = step_mv[3];
                ch4_step_mv.Text = step_mv[4];

                ch1_delay_ms.Text = delay_ms[1];
                ch2_delay_ms.Text = delay_ms[2];
                ch3_delay_ms.Text = delay_ms[3];
                ch4_delay_ms.Text = delay_ms[4];

                ch1_ilim_ma.Text = ilim_ma[1];
                ch2_ilim_ma.Text = ilim_ma[2];
                ch3_ilim_ma.Text = ilim_ma[3];
                ch4_ilim_ma.Text = ilim_ma[4];

                ch1_osr.Text = osr[1];
                ch2_osr.Text = osr[2];
                ch3_osr.Text = osr[3];
                ch4_osr.Text = osr[4];

                ch1_area_cm2.Text = active_area[1];
                ch2_area_cm2.Text = active_area[2];
                ch3_area_cm2.Text = active_area[3];
                ch4_area_cm2.Text = active_area[4];

                ch1_polarity_box.SelectedIndex = int.Parse(polarity[1]);
                ch2_polarity_box.SelectedIndex = int.Parse(polarity[2]);
                ch3_polarity_box.SelectedIndex = int.Parse(polarity[3]);
                ch4_polarity_box.SelectedIndex = int.Parse(polarity[4]);

                ch1_hold_box.SelectedIndex = int.Parse(hold[1]);
                ch2_hold_box.SelectedIndex = int.Parse(hold[2]);
                ch3_hold_box.SelectedIndex = int.Parse(hold[3]);
                ch4_hold_box.SelectedIndex = int.Parse(hold[4]);

                irradience.Text = str_irradience[1];
            }
            catch (Exception a)
            {
                Debug.Print("Stability settings import file empty or otherwise wrong");
            }






        }

        private void save_settings_button(object sender, RoutedEventArgs e)
        {
            return_settings();
            this.Close();
        }

        private void stability_sweep_param_window_close(object sender, System.ComponentModel.CancelEventArgs e)
        {
            return_settings();
        }

        private void return_settings()
        {
            stability_params.ch1_start_v = double.Parse(ch1_start_v.Text);
            stability_params.ch2_start_v = double.Parse(ch2_start_v.Text);
            stability_params.ch3_start_v = double.Parse(ch3_start_v.Text);
            stability_params.ch4_start_v = double.Parse(ch4_start_v.Text);

            stability_params.ch1_end_v = double.Parse(ch1_end_v.Text);
            stability_params.ch2_end_v = double.Parse(ch2_end_v.Text);
            stability_params.ch3_end_v = double.Parse(ch3_end_v.Text);
            stability_params.ch4_end_v = double.Parse(ch4_end_v.Text);

            stability_params.ch1_step_mv = double.Parse(ch1_step_mv.Text);
            stability_params.ch2_step_mv = double.Parse(ch2_step_mv.Text);
            stability_params.ch3_step_mv = double.Parse(ch3_step_mv.Text);
            stability_params.ch4_step_mv = double.Parse(ch4_step_mv.Text);

            stability_params.ch1_delay_ms = int.Parse(ch1_delay_ms.Text);
            stability_params.ch2_delay_ms = int.Parse(ch2_delay_ms.Text);
            stability_params.ch3_delay_ms = int.Parse(ch3_delay_ms.Text);
            stability_params.ch4_delay_ms = int.Parse(ch4_delay_ms.Text);

            stability_params.ch1_ilim_ma = int.Parse(ch1_ilim_ma.Text);
            stability_params.ch2_ilim_ma = int.Parse(ch2_ilim_ma.Text);
            stability_params.ch3_ilim_ma = int.Parse(ch3_ilim_ma.Text);
            stability_params.ch4_ilim_ma = int.Parse(ch4_ilim_ma.Text);

            stability_params.ch1_osr = int.Parse(ch1_osr.Text);
            stability_params.ch2_osr = int.Parse(ch1_osr.Text);
            stability_params.ch3_osr = int.Parse(ch1_osr.Text);
            stability_params.ch4_osr = int.Parse(ch1_osr.Text);

            stability_params.ch1_area_cm2 = double.Parse(ch1_area_cm2.Text);
            stability_params.ch2_area_cm2 = double.Parse(ch2_area_cm2.Text);
            stability_params.ch3_area_cm2 = double.Parse(ch3_area_cm2.Text);
            stability_params.ch4_area_cm2 = double.Parse(ch4_area_cm2.Text);

            stability_params.ch1_polarity = ch1_polarity_box.SelectedIndex;
            stability_params.ch2_polarity = ch2_polarity_box.SelectedIndex;
            stability_params.ch3_polarity = ch3_polarity_box.SelectedIndex;
            stability_params.ch4_polarity = ch4_polarity_box.SelectedIndex;

            stability_params.ch1_hold_state = ch1_hold_box.SelectedIndex;
            stability_params.ch2_hold_state = ch2_hold_box.SelectedIndex;
            stability_params.ch3_hold_state = ch3_hold_box.SelectedIndex;
            stability_params.ch4_hold_state = ch4_hold_box.SelectedIndex;

            stability_params.stability_irradience = double.Parse(irradience.Text);

            stability_params.params_accessed_count++;

        }

        /// <summary>Highlights textbox content when focussed upon</summary>
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            textBox.Dispatcher.BeginInvoke(new Action(() => textBox.SelectAll()));
        }

        /// <summary>Checks textbox input for forbidden characters (letters & symbol)</summary>
        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            e.Handled = Regex.IsMatch(e.Text, "[^0-9.-]+");
        }

    }
}