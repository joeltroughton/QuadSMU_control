using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace QuadSMU_control
{
    public partial class MainWindow
    {
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
            public int hold_state; // 0 = VOC, 1 = JSC, 2 = MPP 3 = Arb (see arb_hold_v)
            public int step_delay; // Milliseconds
            public double v_step_size; // Millivolts
            public double arb_hold_v; // Arbitrary hold voltage
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
    }
}




