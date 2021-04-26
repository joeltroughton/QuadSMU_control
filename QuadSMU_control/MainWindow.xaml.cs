﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO.Ports;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using ScottPlot;
using System.Windows.Forms;
using System.Data;

namespace QuadSMU_control
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        SerialPort sp = new SerialPort();

        public string[] ports = SerialPort.GetPortNames();
        public String[] smu_channels = { "Channel 1", "Channel 2", "Channel 3", "Channel 4" };

        List<double> temp_voltage_list = new List<double>();
        List<double> temp_current_list = new List<double>();

        double[] dvoltageArray; // For on-the-fly graph rendering
        double[] dcurrentArray;

        List<iv_curve> measurements_list = new List<iv_curve>();

        PlottableScatter jvplot;
        DispatcherTimer renderTimer = new DispatcherTimer();

        bool replyRecieved;

        bool measurement_in_progress;

        public class iv_curve
        {
            public List<double> voltage = new List<double>();
            public List<double> current = new List<double>();
            public List<double> power = new List<double>();

            public int start_timestamp;
            public int end_timestamp;
            public int scan_duration;
            public String device_name;

            public double voc;
            public double jsc;
            public double fill_factor;
            public double pce;

            public int smu_channel;
            public int step_delay; // Milliseconds
            public int v_step_size; // Millivolts
            public bool polarity;
            public bool sweep_direction;
            public double active_area;
            public double irradiance;
            public double start_v;
            public double end_v;
            public double i_limit;

            public void calc_voc()
            {
                double[] voltage_array = this.voltage.ToArray();
                double[] current_array = this.current.ToArray();
                double[] power_array = this.power.ToArray();

                if (current_array[0] < 0)
                {
                    Array.Reverse(voltage_array);
                    Array.Reverse(current_array);
                    Array.Reverse(power_array);

                }

                for (int i = 0; i < voltage_array.Count(); i++)
                {
                    Debug.Print("{0} \t {1} \t {2}", voltage_array[i], current_array[i], power_array[i]);
                }

                double lastPositiveJ = 0;
                double lastPositiveV = 0;

                double firstNegativeJ = 0;
                double firstNegativeV = 0;

                int lastPositiveIndex = 0;

                for (int i = 1; i < voltage_array.Count(); i++)
                {
                    if (current_array[i] > 0)
                    {
                        lastPositiveJ = current_array[i];
                        lastPositiveIndex = i;

                        lastPositiveV = voltage_array[i];

                        firstNegativeJ = current_array[i + 1];
                        firstNegativeV = voltage_array[i + 1];
                    }
                }

                double vocSlope = (firstNegativeJ - lastPositiveJ) / (firstNegativeV - lastPositiveV);
                double jIntercept = lastPositiveJ - (vocSlope * lastPositiveV);
                double voc = (0 - jIntercept) / vocSlope;

                this.voc = Math.Round(Math.Abs(voc), 3);
            }

            public void calc_jsc()
            {
                double[] voltage_array = voltage.ToArray();
                double[] current_array = current.ToArray();

                if (voltage_array[0] < 0)
                {
                    Array.Reverse(voltage_array);
                    Array.Reverse(current_array);
                }

                double lastNegativeV = 0;
                double lastNegativeJ = 0;

                double firstPositiveV = 0;
                double firstPositiveJ = 0;


                for (int i = 1; i < voltage_array.Count(); i++)
                {
                    if (voltage_array[i] > 0)
                    {
                        lastNegativeV = voltage_array[i];
                        lastNegativeJ = current_array[i];

                        firstPositiveV = voltage_array[i + 1];
                        firstPositiveJ = current_array[i + 1];
                    }
                }

                double jscSlope = (firstPositiveV - lastNegativeV) / (firstPositiveJ - lastNegativeJ);
                double vIntercept = lastNegativeV - (jscSlope * lastNegativeJ);
                double jsc = (0 - vIntercept) / jscSlope;

                this.jsc = Math.Round((Math.Abs(jsc)), 2);
            }

            public void calc_fill_factor()
            {
                double[] power_array = this.power.ToArray();

                try
                {
                    double maximum_power_point = power_array.Max();
                    int maximum_power_point_index = power_array.ToList().IndexOf(maximum_power_point);

                    double fill_factor = Math.Abs(maximum_power_point) / (Math.Abs(this.voc) * Math.Abs(this.jsc));

                    this.fill_factor = Math.Round(fill_factor, 3);
                }
                catch
                {
                    Debug.Print("FF incalculable");
                }
            }
            public void calc_pce(double irradience)
            {
                this.pce = Math.Round(((this.voc * this.jsc * this.fill_factor) / irradience), 3);
            }
            public void export_file()
            {

            }

            public void set_start_timestamp()
            {
                this.start_timestamp = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            }

            public void set_end_timestamp()
            {
                this.end_timestamp = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
                this.scan_duration = this.end_timestamp - this.start_timestamp;
            }
        }
        public class datagrid_update_class
        {
            public String cell_name { get; set; }
            public double voc { get; set; }
            public double jsc { get; set; }
            public double ff { get; set; }
            public double pce { get; set; }

        }

        public MainWindow()
        {
            this.InitializeComponent();
            port_box.ItemsSource = ports;
            smu_channel_box.ItemsSource = smu_channels;

            jvPlot.plt.YLabel("Current density (mA/cm²)");
            jvPlot.plt.XLabel("Voltage (V)");


            renderTimer.Interval = TimeSpan.FromMilliseconds(67);
            renderTimer.Tick += RenderGraph;
            //connect_serial_port();

        }

        private async Task call_measurement()
        {
            iv_curve scan1 = new iv_curve();

            switch (smu_channel_box.Text)
            {
                case "Channel 1":
                    scan1.smu_channel = 1;
                    break;
                case "Channel 2":
                    scan1.smu_channel = 2;
                    break;
                case "Channel 3":
                    scan1.smu_channel = 3;
                    break;
                case "Channel 4":
                    scan1.smu_channel = 4;
                    break;
            }

            scan1.start_v = double.Parse(start_voltage.Text);
            scan1.end_v = double.Parse(end_voltage.Text);
            scan1.v_step_size = int.Parse(step_size_mv.Text);
            scan1.device_name = cell_name.Text;

            temp_voltage_list.Clear();
            temp_current_list.Clear();

            await Task.Run(() => send_levels(scan1));

            Debug.Print("JV curve levels sent");


            scan1.voltage = temp_voltage_list.ToList();
            scan1.current = temp_current_list.ToList();

            // Calculate cell power
            for (int i = 0; i < scan1.voltage.Count(); i++)
            {
                scan1.power.Add(scan1.voltage[i] * scan1.current[i]);
            }

            scan1.calc_voc();
            scan1.calc_jsc();
            scan1.calc_fill_factor();
            scan1.calc_pce(double.Parse(irradience.Text));

            updateDatagrid(scan1);

            Debug.Print("VOC: {0}, JSC: {1}, FF: {2}, PCE: {3}", scan1.voc, scan1.jsc, scan1.fill_factor, scan1.pce);

            measurements_list.Add(scan1);

            foreach (iv_curve name in measurements_list)
            {
                Debug.Print("{0} \t {1} \t {2} \t {3} \t {4}", name.device_name, name.voc, name.jsc, name.fill_factor, name.pce);
            }

            renderTimer.Stop();

        }

        private void connect_serial_port(String com_port)
        {
            sp.PortName = com_port;
            sp.BaudRate = 115200;
            sp.DtrEnable = true;
            sp.RtsEnable = true;
            sp.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
            sp.Open();

            string send = String.Format("CH1:ENA");
            sp.WriteLine(send);

            send = String.Format("CH1:OSR 5");
            sp.WriteLine(send);


        }

        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            string inLine = sp.ReadLine();
            Dispatcher.Invoke(() =>
            {
                try
                {
                    UpdateData(inLine);
                    replyRecieved = true;

                }
                catch (Exception ex)
                {
                    //  MessageBox.Show("Error interpreting input data");
                    //  Console.WriteLine("UpdateData error - {0}", ex);
                }
            });
        }

        private async void UpdateData(string inLine)
        {
            Debug.Print("Recieved: {0}", inLine);

            string str = inLine.Substring(0, inLine.Length);
            str = str.Replace("\0", string.Empty);

            double active_area = double.Parse(active_area_textbox.Text);

            try
            {
                string[] parts = str.Split(',');

                temp_voltage_list.Add(double.Parse(parts[0]));
                temp_current_list.Add((double.Parse(parts[1]) / active_area) * 1000);

                dvoltageArray = temp_voltage_list.ToArray();
                dcurrentArray = temp_current_list.ToArray();
            }

            catch (Exception ex)
            {
                Console.WriteLine("Error - Data from SMU not understood. {0}", ex);
            }
        }

        private async Task send_levels(iv_curve new_curve)
        {
            measurement_in_progress = true;

            int step_delay_millis = new_curve.step_delay;
            int v_step_size = new_curve.v_step_size;
            double start_v = new_curve.start_v;
            double end_v = new_curve.end_v;

            double voltage_sweep_span = Math.Abs(start_v) + Math.Abs(end_v);
            double v_step_size_volts = (double)v_step_size / 1000;
            int number_of_steps = (int)(voltage_sweep_span / v_step_size_volts);

            double[] voltage_step_array = new double[number_of_steps + 2]; // 2 extra steps because we want to measure the start and the end voltagess

            new_curve.set_start_timestamp();

            for (int i = 0; i < number_of_steps + 2; i++)
            {
                voltage_step_array[i] = start_v - (v_step_size_volts * i);
            }


            foreach (float voltage in voltage_step_array)
            {
                string send = String.Format("CH{0}:MEA:VOL {1}", new_curve.smu_channel, voltage);
                replyRecieved = false;
                Debug.Print("Sending: {0}", send);
                sp.WriteLine(send);

                while (!replyRecieved)
                {
                    Debug.Print("waiting...");
                    await Task.Delay(25);
                }
                replyRecieved = false;
            }

            new_curve.set_end_timestamp();

            Debug.Print("finished");
            measurement_in_progress = false;

        }

        private void output_button_Click(object sender, RoutedEventArgs e)
        {
            foreach (iv_curve measurement_item in measurements_list)
            {
                Debug.Print("{0}", measurement_item.device_name);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //MessageBox.Show("button 1");
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            //MessageBox.Show("button 2");
        }

        private void port_box_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void start_voltage_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
        private void connect_Click(object sender, RoutedEventArgs e)
        {
            String com_port_id = port_box.Text;
            connect_serial_port(com_port_id);
        }
        private void disconnect_Click(object sender, RoutedEventArgs e)
        {
            //PlotAll();
            PrintChecked();
            //sp.Close();

        }

        private void run_iv_button(object sender, RoutedEventArgs e)
        {
            if (!measurement_in_progress)
            {
                renderTimer.Start();
                call_measurement();
            }
        }

        private void updateDatagrid(iv_curve most_recent_iv_curve)
        {

            var data = new datagrid_update_class
            {
                cell_name = most_recent_iv_curve.device_name,
                voc = most_recent_iv_curve.voc,
                jsc = most_recent_iv_curve.jsc,
                ff = most_recent_iv_curve.fill_factor,
                pce = most_recent_iv_curve.pce
            };

            iv_datagrid.Items.Add(data);
        }

        void RenderGraph(object sender, EventArgs e)
        {
            if (dvoltageArray != null)
            {

                jvPlot.plt.Clear();

                jvPlot.plt.PlotScatter(dvoltageArray, dcurrentArray, color: System.Drawing.Color.Red, lineWidth: 4, markerSize: 10);
                jvPlot.plt.PlotHLine(0, color: System.Drawing.Color.Black, lineWidth: 1);
                jvPlot.plt.PlotVLine(0, color: System.Drawing.Color.Black, lineWidth: 1);
                jvPlot.plt.AxisAuto(0.1, 0.1); // no horizontal padding, 50% vertical padding
                jvPlot.Render(skipIfCurrentlyRendering: true);

            }
        }

        void PlotAll()
        {

            jvPlot.plt.Clear();

            foreach (iv_curve measurement in measurements_list)
            {
                double[] plot_v_array = measurement.voltage.ToArray();
                double[] plot_j_array = measurement.current.ToArray();

                jvPlot.plt.PlotScatter(plot_v_array, plot_j_array, lineWidth: 4, markerSize: 10, label: measurement.device_name);

            }

            jvPlot.plt.PlotHLine(0, color: System.Drawing.Color.Black, lineWidth: 1);
            jvPlot.plt.PlotVLine(0, color: System.Drawing.Color.Black, lineWidth: 1);
            jvPlot.plt.Legend();
            jvPlot.plt.AxisAuto(0.1, 0.1); // no horizontal padding, 50% vertical padding
            jvPlot.Render(skipIfCurrentlyRendering: true);

        }

        void PrintChecked()
        {

            //IList<String> items = iv_datagrid.SelectedItems.ToString();


        }


    }


}

