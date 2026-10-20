using System.Configuration;
using System.Data;
using System.Windows;

namespace SpotiFloat;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    protected override void OnExit(ExitEventArgs e)
    {
        AudioProcessor.Instance.Dispose();
        base.OnExit(e);
    }
}
