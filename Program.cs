using RasterEditor.WinFormsDemo.Forms;

namespace RasterEditor.WinFormsDemo;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

