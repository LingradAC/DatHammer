using System.Configuration;
using System.Data;
using System.Windows;
using static DatHammer.Globals;

namespace DatHammer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static AppState State { get; } = new AppState();
    }
}
