using System;
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
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using ScottPlot;
using System.Windows.Forms;
using System.Data;
using WinForms = System.Windows.Forms;

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
        bool listen_for_data = false;
        bool cancel_iv_scan = false;
        bool stability_active = false;

        double stability_interval_secs;
        double stability_currentcell_active_area;

        string iv_save_datadir;
        string stability_save_datadir;

        stability_sweep_parameters stability_sweep_params = new stability_sweep_parameters();

        DispatcherTimer stabilityTimer = new DispatcherTimer();

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
            public double vmpp;


            public int smu_channel;
            public int osr;
            public int hold_state; // 0 = VOC, 1 = JSC, 2 = MPP
            public int step_delay; // Milliseconds
            public double v_step_size; // Millivolts
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

                //for (int i = 0; i < voltage_array.Count(); i++)
                //{
                //    Debug.Print("{0} \t {1} \t {2}", voltage_array[i], current_array[i], power_array[i]);
                //}

                double lastPositiveJ = 0;
                double lastPositiveV = 0;

                double firstNegativeJ = 0;
                double firstNegativeV = 0;

                int lastPositiveIndex = 0;

                for (int i = 1; i < voltage_array.Count() - 1; i++)
                {
                    if (current_array[i] > 0)
                    {
                        lastPositiveJ = current_array[i];
                        lastPositiveV = voltage_array[i];

                        firstNegativeJ = current_array[i + 1];
                        firstNegativeV = voltage_array[i + 1];
                    }

                    else if (current_array[i] < 0)
                    {
                        lastPositiveIndex = i;
                        break;
                    }

                }

                if (lastPositiveIndex == 0)
                {
                    this.voc = 0;
                    Debug.Print("Curve never passes through 0A. Don't calculate VOC");
                }
                else
                {
                    double vocSlope = (firstNegativeJ - lastPositiveJ) / (firstNegativeV - lastPositiveV);
                    double jIntercept = lastPositiveJ - (vocSlope * lastPositiveV);
                    double voc = (0 - jIntercept) / vocSlope;
                    this.voc = Math.Round(Math.Abs(voc), 3);
                }

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

                int lastPositiveIndex = 0;

                for (int i = 1; i < voltage_array.Count() - 1; i++)
                {
                    if (voltage_array[i] > 0)
                    {
                        lastNegativeV = voltage_array[i];
                        lastNegativeJ = current_array[i];

                        firstPositiveV = voltage_array[i + 1];
                        firstPositiveJ = current_array[i + 1];
                    }

                    else if (voltage_array[i] < 0)
                    {
                        lastPositiveIndex = i;
                        break;
                    }
                }

                if (lastPositiveIndex == 0)
                {
                    this.jsc = 0;
                    Debug.Print("Curve never passes through 0V. Don't calculate JSC");
                }
                else
                {
                    double jscSlope = (firstPositiveV - lastNegativeV) / (firstPositiveJ - lastNegativeJ);
                    double vIntercept = lastNegativeV - (jscSlope * lastNegativeJ);
                    double jsc = (0 - vIntercept) / jscSlope;

                    this.jsc = Math.Round((Math.Abs(jsc)), 3);
                }

            }

            public void calc_fill_factor()
            {
                double[] voltage_array = this.voltage.ToArray();
                double[] current_array = this.current.ToArray();
                double[] power_array = this.power.ToArray();

                for (int i = 0; i < voltage_array.Count(); i++)
                {
                    Debug.Print("{0} \t {1} \t {2}", voltage_array[i], current_array[i], power_array[i]);
                }

                if (current_array[0] < 0)
                {
                    Array.Reverse(voltage_array);
                    Array.Reverse(current_array);
                    Array.Reverse(power_array);
                    Debug.Print("Arrays flipped");
                }

                try
                {
                    // WARNING - Min for PGA281, Max for OLD LT1991
                    double maximum_power_point = power_array.Max();
                    //double maximum_power_point = power_array.Min();
                    int maximum_power_point_index = power_array.ToList().IndexOf(maximum_power_point);

                    this.vmpp = voltage_array[maximum_power_point_index];

                    double fill_factor = Math.Abs(maximum_power_point) / (Math.Abs(this.voc) * Math.Abs(this.jsc));


                    Debug.Print("Power array max = {0} at index {1}", maximum_power_point, maximum_power_point_index);
                    Debug.Print("VOC: {0} \t JSC: {1}", this.voc, this.jsc);

                    if (fill_factor > 1)
                    {
                        this.fill_factor = 0;
                    }
                    else
                    {
                        this.fill_factor = Math.Round(fill_factor, 3);
                    }

                }
                catch
                {
                    Debug.Print("FF incalculable");
                }
            }
            public void calc_pce()
            {
                this.pce = Math.Round(((this.voc * this.jsc * this.fill_factor) / this.irradiance), 3);
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

        public class stability_sweep_parameters
        {
            public int params_accessed_count = 0;

            public double stability_irradience;

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

            public int ch1_delay_ms;
            public int ch2_delay_ms;
            public int ch3_delay_ms;
            public int ch4_delay_ms;

            public int ch1_ilim_ma;
            public int ch2_ilim_ma;
            public int ch3_ilim_ma;
            public int ch4_ilim_ma;

            public int ch1_osr;
            public int ch2_osr;
            public int ch3_osr;
            public int ch4_osr;

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

        public MainWindow()
        {
            this.InitializeComponent();
            port_box.ItemsSource = ports;
            smu_channel_box.ItemsSource = smu_channels;
            spo_smu_channel_box.ItemsSource = smu_channels;

            jvPlot.plt.YLabel("Current density (mA/cm²)");
            jvPlot.plt.XLabel("Voltage (V)");


            renderTimer.Interval = TimeSpan.FromMilliseconds(67);
            renderTimer.Tick += RenderGraph;
            //connect_serial_port();

        }

        private async Task<iv_curve> call_measurement(iv_curve jv_scan)
        {
            iv_curve scan1 = new iv_curve();

            temp_voltage_list.Clear();
            temp_current_list.Clear();

            Debug.Print("Sending levels");
            await Task.Run(() => send_levels(jv_scan));
            Debug.Print("JV sweep completed");

            jv_scan.voltage = temp_voltage_list.ToList();
            jv_scan.current = temp_current_list.ToList();

            // Calculate cell power
            for (int i = 0; i < jv_scan.voltage.Count(); i++)
            {
                jv_scan.power.Add(jv_scan.voltage[i] * jv_scan.current[i]);
            }

            jv_scan.calc_voc();
            jv_scan.calc_jsc();
            jv_scan.calc_fill_factor();
            jv_scan.calc_pce();

            //updateDatagrid(jv_scan);

            Debug.Print("VOC: {0}, JSC: {1}, FF: {2}, PCE: {3}", jv_scan.voc, jv_scan.jsc, jv_scan.fill_factor, jv_scan.pce);

            measurements_list.Add(jv_scan);

            foreach (iv_curve name in measurements_list)
            {
                Debug.Print("{0} \t {1} \t {2} \t {3} \t {4}", name.device_name, name.voc, name.jsc, name.fill_factor, name.pce);
            }

            //renderTimer.Stop();

            //jv_export(jv_scan);

            return jv_scan;
        }

        private void connect_serial_port(String com_port)
        {
            try
            {
                sp.PortName = com_port;
                sp.BaudRate = 115200;
                sp.DtrEnable = true;
                sp.RtsEnable = true;
                sp.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
                sp.Open();

                sp.DiscardInBuffer();
                sp.DiscardOutBuffer();

                xconnect_button.IsEnabled = false;
                stability_tab.IsEnabled = true;
                iv_tab.IsEnabled = true;


            }

            catch (Exception)
            {
                smu_msg_block.Text = "Connection failed. Is the correct port selected?";
                //MessageBox.Show("Connection failed. Is the correct port selected?");
            }
        }

        private void disconnect_Click(object sender, RoutedEventArgs e)
        {
            sp.DiscardInBuffer();
            sp.DiscardOutBuffer();

            sp.DataReceived -= null;

            sp.Close();
            sp.Dispose();
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
            if (listen_for_data)
            {
                string str = inLine.Substring(0, inLine.Length);
                str = str.Replace("\0", string.Empty);
                Debug.Print("Recieved: {0}", str);

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

        }

        private async Task send_levels(iv_curve new_curve)
        {
            measurement_in_progress = true;

            sp.DiscardInBuffer();
            sp.DiscardOutBuffer();

            int step_delay_millis = new_curve.step_delay;
            double v_step_size = new_curve.v_step_size;
            double start_v = new_curve.start_v;
            double end_v = new_curve.end_v;

            double voltage_sweep_span = Math.Abs(start_v - end_v);
            double v_step_size_volts = (double)v_step_size / 1000;
            int number_of_steps = (int)(voltage_sweep_span / v_step_size_volts);

            double[] voltage_step_array = new double[number_of_steps + 1]; // 2 extra steps because we want to measure the start and the end voltagess

            if (start_v > end_v)
            {
                for (int i = 0; i < number_of_steps + 1; i++)
                {
                    voltage_step_array[i] = start_v - (v_step_size_volts * i);
                }
            }
            else
            {
                for (int i = 0; i < number_of_steps + 1; i++)
                {
                    voltage_step_array[i] = start_v + (v_step_size_volts * i);
                }
            }


            foreach (double voltage in voltage_step_array)
            {
                Debug.Print("{0}", voltage);
            }


            //Set current limit & OSR, then set the voltage to the start level and enable the output
            string send = String.Format("CH{0}:CUR {1}", new_curve.smu_channel, new_curve.i_limit);
            sp.WriteLine(send);
            await Task.Delay(5);

            send = String.Format("CH{0}:ENA", new_curve.smu_channel);
            sp.WriteLine(send);
            await Task.Delay(10);

            send = String.Format("CH{0}:OSR 1", new_curve.smu_channel);
            sp.WriteLine(send);
            await Task.Delay(5);

            send = String.Format("CH{0}:MEA:VOL {1}", new_curve.smu_channel, new_curve.start_v);
            sp.WriteLine(send);
            //await Task.Delay(50);
            await Task.Delay(500); // QuadSMU slow

            send = String.Format("CH{0}:OSR {1}", new_curve.smu_channel, new_curve.osr);
            sp.WriteLine(send);
            await Task.Delay(5);

            sp.DiscardInBuffer();
            sp.DiscardOutBuffer();

            new_curve.set_start_timestamp();
            listen_for_data = true;


            foreach (double voltage in voltage_step_array)
            {
                if (cancel_iv_scan)
                {
                    measurement_in_progress = false;
                    listen_for_data = false;
                    return;
                }
                send = String.Format("CH{0}:MEA:VOL {1:0.000}", new_curve.smu_channel, voltage);
                replyRecieved = false;
                Debug.Print("Sending: {0}", send);
                sp.WriteLine(send);
                var watch = System.Diagnostics.Stopwatch.StartNew();


                while (!replyRecieved)
                {
                    //Debug.Print("waiting...");
                    await Task.Delay(1);
                }

                while (watch.ElapsedMilliseconds < new_curve.step_delay)
                {
                    Debug.Print("Waiting for step delay: {0}ms", watch.ElapsedMilliseconds);
                    await Task.Delay(1);
                }
                Debug.Print("Step delay condition met: {0}ms", watch.ElapsedMilliseconds);
                watch.Stop();
                replyRecieved = false;
            }

            new_curve.set_end_timestamp();

            Debug.Print("finished");
            measurement_in_progress = false;
            listen_for_data = false;
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


        private async void run_iv_button(object sender, RoutedEventArgs e)
        {
            if (!measurement_in_progress)
            {
                xrun_iv_button.IsEnabled = false;
                xstop_iv_button.IsEnabled = true;
                cancel_iv_scan = false;
                renderTimer.Start();
                int smu_channel = smu_channel_box.SelectedIndex + 1;
                double start_v = double.Parse(start_voltage.Text);
                double end_v = double.Parse(end_voltage.Text);
                int step_size = int.Parse(step_size_mv.Text);
                double i_limit_ma = double.Parse(i_limit.Text);
                int delay_time = int.Parse(delay_time_ms.Text);
                double dirradience = double.Parse(irradience.Text);
                string device_name = cell_name.Text;
                int iosr = int.Parse(osr.Text);
                double active_area = double.Parse(active_area_textbox.Text);
                int polarity = 0;
                int hold_state = 0;


                iv_curve single_jv = new iv_curve();

                single_jv = generate_measurement_profile(device_name,
                    smu_channel,
                    start_v,
                    end_v,
                   step_size,
                   delay_time,
                   i_limit_ma,
                   iosr,
                   active_area,
                   dirradience,
                   polarity,
                   hold_state
                );

                string iv_output_path = String.Format("{0}\\{1}.dat", iv_save_datadir, device_name);
                await call_measurement(single_jv).ConfigureAwait(false);

                jv_export(iv_output_path, single_jv);
                add_iv_stats_to_csv(iv_save_datadir, single_jv);

                System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate { updateDatagrid(single_jv); xstop_iv_button.IsEnabled = false; xrun_iv_button.IsEnabled = true; });

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

            System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate
            {
                iv_datagrid.Items.Add(data);
                iv_datagrid.SelectedItem = iv_datagrid.Items[iv_datagrid.Items.Count - 1];
                iv_datagrid.Focus();
                iv_datagrid.ScrollIntoView(iv_datagrid.SelectedItem);

            });
        }

        void RenderGraph(object sender, EventArgs e)
        {
            if (dvoltageArray != null)
            {

                jvPlot.plt.Clear();

                jvPlot.plt.PlotScatter(dvoltageArray, dcurrentArray, color: System.Drawing.Color.CornflowerBlue, lineWidth: 4, markerSize: 10);
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

        private void run_stability_button(object sender, RoutedEventArgs e)
        {
            // Start timer
            if (!stability_active)
            {
                run_stability.IsEnabled = false;
                stop_stability.IsEnabled = true;

                stability_interval_secs = double.Parse(stability_interval_mins.Text) * 60;
                String next_measurement = String.Format("{0}", DateTime.Now.AddSeconds(stability_interval_secs).ToString("HH:mm:ss"));
                stability_countdown_textbox.Text = next_measurement;
                stability_countdown_textbox.Foreground = Brushes.YellowGreen;

                stability_save_datadir = stability_savedir.Text;


                stabilityTimer.Interval = TimeSpan.FromSeconds(stability_interval_secs);
                stabilityTimer.Tick += stability_timer_Tick;
                stabilityTimer.Start();
            }



        }

        private void halt_stability_button(object sender, RoutedEventArgs e)
        {
            run_stability.IsEnabled = true;
            stop_stability.IsEnabled = false;

            stabilityTimer.Stop();
            stability_active = false;
            stability_countdown_textbox.Foreground = Brushes.Crimson;
            stability_countdown_textbox.Text = "Not running";
        }

        private async void instant_stability_button(object sender, RoutedEventArgs e)
        {
            if (stabilityTimer.IsEnabled)
            {
                stability_countdown_textbox.Foreground = Brushes.YellowGreen;
                stability_countdown_textbox.Text = "Measurement in progress";

                stabilityTimer.Stop();
                await run_scheduled_measurements().ConfigureAwait(false);

            }
            else
            {
                await run_scheduled_measurements().ConfigureAwait(false);
                System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate
                {
                    stability_countdown_textbox.Foreground = Brushes.Crimson;
                    stability_countdown_textbox.Text = "Not running";
                });



            }


        }

        void stability_timer_Tick(object sender, EventArgs e)
        {
            run_scheduled_measurements();
            stabilityTimer.Start();

            String next_measurement = String.Format("{0}", DateTime.Now.AddSeconds(stability_interval_secs).ToString("HH:mm:ss"));
            System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate { stability_countdown_textbox.Text = next_measurement; });


        }

        private async Task run_scheduled_measurements()
        {
            bool ch1_enabled = (bool)smu_ch_1_box.IsChecked;
            bool ch2_enabled = (bool)smu_ch_2_box.IsChecked;
            bool ch3_enabled = (bool)smu_ch_3_box.IsChecked;
            bool ch4_enabled = (bool)smu_ch_4_box.IsChecked;


            //stabilityTimer.Stop();
            renderTimer.Start();

            string ch1_save_dir = stability_save_datadir;

            if (ch1_enabled)
            {

                iv_curve ch1_jv = new iv_curve();
                ch1_jv = generate_measurement_profile(
                    "CH1 device",
                    1,
                    stability_sweep_params.ch1_start_v,
                    stability_sweep_params.ch1_end_v,
                    stability_sweep_params.ch1_step_mv,
                    stability_sweep_params.ch1_delay_ms,
                    stability_sweep_params.ch1_ilim_ma,
                    stability_sweep_params.ch1_osr,
                    stability_sweep_params.ch1_area_cm2,
                    stability_sweep_params.stability_irradience,
                    stability_sweep_params.ch1_polarity,
                    stability_sweep_params.ch1_hold_state);

                stability_currentcell_active_area = ch1_jv.active_area;
                await call_measurement(ch1_jv).ConfigureAwait(false);
                updateDatagrid(ch1_jv);
                set_hold_voltage(ch1_jv);
                add_stability_stats_to_csv(stability_save_datadir, ch1_jv);
                stability_jv_export(stability_save_datadir, ch1_jv);
            }

            if (ch2_enabled)
            {
                iv_curve ch2_jv = new iv_curve();
                ch2_jv = generate_measurement_profile(
                    "CH2 device",
                    2,
                    stability_sweep_params.ch2_start_v,
                    stability_sweep_params.ch2_end_v,
                    stability_sweep_params.ch2_step_mv,
                    stability_sweep_params.ch2_delay_ms,
                    stability_sweep_params.ch2_ilim_ma,
                    stability_sweep_params.ch2_osr,
                    stability_sweep_params.ch2_area_cm2,
                    stability_sweep_params.stability_irradience,
                    stability_sweep_params.ch2_polarity,
                    stability_sweep_params.ch2_hold_state);

                stability_currentcell_active_area = ch2_jv.active_area;
                await call_measurement(ch2_jv).ConfigureAwait(false);
                updateDatagrid(ch2_jv);
                set_hold_voltage(ch2_jv);
                add_stability_stats_to_csv(stability_save_datadir, ch2_jv);
                stability_jv_export(stability_save_datadir, ch2_jv);
            }

            if (ch3_enabled)
            {
                iv_curve ch3_jv = new iv_curve();
                ch3_jv = generate_measurement_profile(
                    "CH3 device",
                    3,
                    stability_sweep_params.ch3_start_v,
                    stability_sweep_params.ch3_end_v,
                    stability_sweep_params.ch3_step_mv,
                    stability_sweep_params.ch3_delay_ms,
                    stability_sweep_params.ch3_ilim_ma,
                    stability_sweep_params.ch3_osr,
                    stability_sweep_params.ch3_area_cm2,
                    stability_sweep_params.stability_irradience,
                    stability_sweep_params.ch3_polarity,
                    stability_sweep_params.ch3_hold_state);

                stability_currentcell_active_area = ch3_jv.active_area;
                await call_measurement(ch3_jv).ConfigureAwait(false);
                updateDatagrid(ch3_jv);
                set_hold_voltage(ch3_jv);
                add_stability_stats_to_csv(stability_save_datadir, ch3_jv);
                stability_jv_export(stability_save_datadir, ch3_jv);
            }

            if (ch4_enabled)
            {
                iv_curve ch4_jv = new iv_curve();
                ch4_jv = generate_measurement_profile(
                    "CH4 device",
                    4,
                    stability_sweep_params.ch4_start_v,
                    stability_sweep_params.ch4_end_v,
                    stability_sweep_params.ch4_step_mv,
                    stability_sweep_params.ch4_delay_ms,
                    stability_sweep_params.ch4_ilim_ma,
                    stability_sweep_params.ch4_osr,
                    stability_sweep_params.ch4_area_cm2,
                    stability_sweep_params.stability_irradience,
                    stability_sweep_params.ch4_polarity,
                    stability_sweep_params.ch4_hold_state);
                
                stability_currentcell_active_area = ch4_jv.active_area;
                await call_measurement(ch4_jv).ConfigureAwait(false);
                updateDatagrid(ch4_jv);
                set_hold_voltage(ch4_jv);
                add_stability_stats_to_csv(stability_save_datadir, ch4_jv);
                stability_jv_export(stability_save_datadir, ch4_jv);
            }

            renderTimer.Stop();
            return;
        }

        void set_hold_voltage(iv_curve ivcurve)
        {
            // Apply the hold voltage to the SMU channel in question
            int smu_channel = ivcurve.smu_channel;
            int hold_condition = ivcurve.hold_state;
            string send;
            // 0 = VOC, 1 = JSC, 2 = MPP
            switch (hold_condition)
            {
                case 0:
                    send = String.Format("CH{0}:VOL {1}", smu_channel, ivcurve.voc);
                    sp.Write(send);
                    Debug.Print("Holding CH{0} at VOC: {1}", smu_channel, ivcurve.voc);
                    break;
                case 1:
                    send = String.Format("CH{0}:VOL 0", smu_channel);
                    sp.Write(send);
                    Debug.Print("Holding CH{0} at JSC (0V)", smu_channel);

                    break;

                case 2:
                    send = String.Format("CH{0}:VOL {1}", smu_channel, ivcurve.vmpp);
                    sp.Write(send);
                    Debug.Print("Holding CH{0} at Vmpp: {1}", smu_channel, ivcurve.vmpp);
                    Debug.Print("{0}", send);

                    break;

            }
        }

        private void stability_params_button(object sender, RoutedEventArgs e)
        {
            stability_settings stability_param_dialog = new stability_settings(stability_sweep_params);

            stability_param_dialog.Show();
        }

        public iv_curve generate_measurement_profile(
            string device_name,
            int smu_channel,
            double start_v,
            double end_v,
            double step_mv,
            int delay_ms,
            double ilim_ma,
            int osr,
            double active_area,
            double dirradience,
            int polarity,
            int hold_state
            )
        {
            iv_curve iv_profile = new iv_curve();

            iv_profile.device_name = device_name;
            iv_profile.smu_channel = smu_channel;
            iv_profile.start_v = start_v;
            iv_profile.end_v = end_v;
            iv_profile.v_step_size = step_mv;
            iv_profile.step_delay = delay_ms;
            iv_profile.i_limit = ilim_ma;
            iv_profile.osr = osr;
            iv_profile.active_area = active_area;
            iv_profile.irradiance = dirradience;
            iv_profile.polarity = Convert.ToBoolean(polarity);
            iv_profile.hold_state = hold_state;

            return iv_profile;
        }

        private void jv_export(string datadir, iv_curve ivcurve)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            var csv = new StringBuilder();

            double[] voltage_array = ivcurve.voltage.ToArray();
            double[] current_array = ivcurve.current.ToArray();

            String header1 = String.Format("Device ID, VOC (V), JSC (mAcm-2), Fill factor (%), PCE (%), Vmpp (V) Timestamp");
            String header2 = String.Format("{0}, {1}, {2}, {3}, {4}, {5}, {6}", ivcurve.device_name, ivcurve.voc, ivcurve.jsc, ivcurve.fill_factor, ivcurve.pce, ivcurve.vmpp, ivcurve.start_timestamp);

            csv.AppendLine(header1);
            csv.AppendLine(header2);

            for (int i = 0; i < voltage_array.Length; i++)
            {
                var voltage = voltage_array[i].ToString();
                var current = current_array[i].ToString();
                var newLine = string.Format("{0},{1}", voltage, current);
                csv.AppendLine(newLine);
            }

            if (File.Exists(datadir))
            {
                datadir = datadir.Substring(0, datadir.Length - 4);
                Debug.Print("{0}", datadir);
                datadir = String.Format("{0}_{1}.dat", datadir, ivcurve.start_timestamp);
                ivcurve.device_name = String.Format("{0}_{1}", ivcurve.device_name, ivcurve.start_timestamp);
            }
            File.WriteAllText(datadir, csv.ToString());

            watch.Stop();
            Debug.Print("jv_export took {0}ms", watch.ElapsedMilliseconds);
        }

        private void stability_jv_export(string datadir, iv_curve ivcurve)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            var csv = new StringBuilder();


            datadir = String.Format("{0}\\JV\\CH{1}", datadir, ivcurve.smu_channel, ivcurve.start_timestamp);
            DirectoryInfo di = Directory.CreateDirectory(datadir);

            datadir = String.Format("{0}\\CH{1}_{2}.dat", datadir, ivcurve.smu_channel, ivcurve.start_timestamp);


            double[] voltage_array = ivcurve.voltage.ToArray();
            double[] current_array = ivcurve.current.ToArray();

            String header1 = String.Format("Device ID, VOC (V), JSC (mAcm-2), Fill factor (%), PCE (%), Vmpp (V), Timestamp");
            String header2 = String.Format("{0}, {1}, {2}, {3}, {4}, {5}, {6}", ivcurve.device_name, ivcurve.voc, ivcurve.jsc, ivcurve.fill_factor, ivcurve.pce, ivcurve.vmpp, ivcurve.start_timestamp);

            csv.AppendLine(header1);
            csv.AppendLine(header2);

            for (int i = 0; i < voltage_array.Length; i++)
            {
                var voltage = voltage_array[i].ToString();
                var current = current_array[i].ToString();
                var newLine = string.Format("{0},{1}", voltage, current);
                csv.AppendLine(newLine);
            }

            File.WriteAllText(datadir, csv.ToString());

            watch.Stop();
            Debug.Print("stability_jv_export took {0}ms", watch.ElapsedMilliseconds);
        }

        private void add_stability_stats_to_csv(string datadir, iv_curve ivcurve)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            var csv = new StringBuilder();

            datadir = String.Format("{0}\\CH{1}_stability.csv", datadir, ivcurve.smu_channel);

            if (!File.Exists(datadir))
            {
                String header = String.Format("Timestamp, VOC (V), JSC (mAcm-2), Fill factor (%), PCE (%), Vmpp (V)");
                csv.AppendLine(header);
            }

            String stats = String.Format("{0}, {1}, {2}, {3}, {4}, {5}", ivcurve.start_timestamp, ivcurve.voc, ivcurve.jsc, ivcurve.fill_factor, ivcurve.pce, ivcurve.vmpp);

            csv.AppendLine(stats);


            File.AppendAllText(datadir, csv.ToString());

            watch.Stop();
            Debug.Print("add_stability_stats_to_csv took {0}ms", watch.ElapsedMilliseconds);
        }

        private void add_iv_stats_to_csv(string datadir, iv_curve ivcurve)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            var csv = new StringBuilder();

            datadir = String.Format("{0}\\Channel{1}_stats.csv", datadir, ivcurve.smu_channel);

            if (!File.Exists(datadir))
            {
                String header = String.Format("Cell ID, VOC (V), JSC (mAcm-2), Fill factor (%), PCE (%), Vmpp (V)");
                csv.AppendLine(header);
            }

            String stats = String.Format("{0}, {1}, {2}, {3}, {4}, {5}", ivcurve.device_name, ivcurve.voc, ivcurve.jsc, ivcurve.fill_factor, ivcurve.pce, ivcurve.vmpp);

            csv.AppendLine(stats);


            File.AppendAllText(datadir, csv.ToString());

            watch.Stop();
            Debug.Print("add_stability_stats_to_csv took {0}ms", watch.ElapsedMilliseconds);
        }

        private void stability_savedir_button(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == WinForms.DialogResult.OK)
            {
                stability_savedir.Text = dialog.SelectedPath;
            }
            else
            {
                return;
            }
        }

        private void stop_iv_button(object sender, RoutedEventArgs e)
        {
            cancel_iv_scan = true;
        }

        private void open_iv_datadir_button(object sender, RoutedEventArgs e)
        {
            string open_folder_directory = iv_save_datadir;

            if (Directory.Exists(open_folder_directory))
            {
                Process.Start("explorer.exe", open_folder_directory);
            }

        }

        private void save_iv_datadir_button(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();

            iv_save_datadir = dialog.SelectedPath;

            Debug.Print("Data directory: {0}", iv_save_datadir);

            xrun_iv_button.IsEnabled = true;

        }

        private void manual_tab_selected(object sender, MouseButtonEventArgs e)
        {
            PlottableScatter manual_plot;


        }
    }
}




