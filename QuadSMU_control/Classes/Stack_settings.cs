namespace QuadSMU_control
{
    public partial class MainWindow
    {
        public class Stack_settings
        {
            public Stability_channel_settings[] channel = new Stability_channel_settings[4];

            public void create_channels()
            {
                for (int i = 0; i < 4; i++)
                {
                    channel[i] = new Stability_channel_settings();
                }
            }

        }
    }
}




