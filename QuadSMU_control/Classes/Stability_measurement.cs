namespace QuadSMU_control
{
    public partial class MainWindow
    {
        public class Stability_measurement
        {
            // Contains up to 16 stacks
            // Stacks contain 4 channels
            // Channels contain individual settings

            public Stack_settings[] stack = new Stack_settings[8];

            public void create_stacks()
            {
                for (int i = 0; i < 8; i++)
                {
                    stack[i] = new Stack_settings();
                }
            }

        }
    }
}




