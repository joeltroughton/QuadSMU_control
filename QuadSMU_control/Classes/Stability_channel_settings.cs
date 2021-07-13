using System;

namespace QuadSMU_control
{
    public partial class MainWindow
    {
        public class Stability_channel_settings
        {
            public double start_v;
            public double end_v;
            public double step_mv;
            public int delay_ms;
            public double ilim_ma;
            public int osr;
            public double area_cm2;
            public int polarity;
            public int hold_state;
            public double arb_hold_v;
            public String name;
            public double irradience;

        }
    }
}




