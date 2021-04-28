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

namespace QuadSMU_control
{
    /// <summary>
    /// Interaction logic for stability_settings.xaml
    /// </summary>
    public partial class stability_settings : Window
    {
        public String[] polarity_combobox_list = { "Positive", "Negative" };
        public String[] hold_condition_combobox_list = { "VOC", "ISC", "MPP" };

        public class stability_sweep_parameters
        {
            public double ch1_start_v;
            public double ch2_start_v;
            public double ch3_start_v;
            public double ch4_start_v;

            public double ch1_end_v;
            public double ch2_end_v;
            public double ch3_end_v;
            public double ch4_end_v;

        }



        public stability_settings()
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

        }

        private void import_parameters_button(object sender, RoutedEventArgs e)
        {

        }
    }
}
