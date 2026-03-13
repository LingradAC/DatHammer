using DatReaderWriter;
using DatReaderWriter.Extensions;
using DatReaderWriter.Extensions.DBObjs;
using DatReaderWriter.Options;
using DatReaderWriter.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace DatHammer
{
    public static class Globals
    {

        
        public static string optionsPath = "./Options.json";
        public static string json = File.ReadAllText(optionsPath);
        public static JObject config = JObject.Parse(json);
        public static string datFolder = config["DatFilesPath"]?.ToString();

        //public static string consoleText = "";
        public static List<string> consoleText = new List<string>();

        public static string portalPath = System.IO.Path.Combine(Globals.datFolder, "client_portal.dat");
        public static string cellPath = System.IO.Path.Combine(Globals.datFolder, "client_cell_1.dat");
        public static string langPath = System.IO.Path.Combine(Globals.datFolder, "client_local_English.dat");
        public static string resPath = System.IO.Path.Combine(Globals.datFolder, "client_highres.dat");

        public static PortalDatabase portalDat = new PortalDatabase(portalPath, DatAccessType.ReadWrite);
        public static CellDatabase cellDat = new CellDatabase(cellPath, DatAccessType.ReadWrite);
        public static LocalDatabase langDat = new LocalDatabase(langPath, DatAccessType.ReadWrite);
        public static DatCollection datFiles = new DatCollection(datFolder, DatAccessType.ReadWrite);

        public static DatEasyWriter DEW = new DatEasyWriter(datFolder);
        public static DatEasyWriterOptions DEWop = new DatEasyWriterOptions();

        public static void UpdateConfigOption(string key, string value)
        {
            config[key] = value;
            File.WriteAllText(optionsPath, config.ToString(Newtonsoft.Json.Formatting.Indented));
        }

        public class AppState : INotifyPropertyChanged
        {
            private string _consoleText;

            public string ConsoleText
            {
                get => _consoleText;
                set
                {
                    _consoleText = value;
                    OnPropertyChanged(nameof(ConsoleText));
                }
            }

            public event PropertyChangedEventHandler? PropertyChanged;

            protected void OnPropertyChanged(string name)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }

    }
}
