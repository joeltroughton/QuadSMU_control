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

            var newLine = string.Format("Paramter, Channel 1, Channel 2, Channel 3, Channel 4");
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

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Comma separated value (*.csv)|*.csv";

            if (saveFileDialog.ShowDialog() == true)

                File.WriteAllText(saveFileDialog.FileName, settings_csv.ToString());
        }

        private void import_parameters_button(object sender, RoutedEventArgs e)
        {

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

            stability_params.params_accessed_count++;
        }
    }
}
