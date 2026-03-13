using CommunityToolkit.HighPerformance.Buffers;
using DatReaderWriter;
using DatReaderWriter.DBObjs;
using DatReaderWriter.Enums;
using DatReaderWriter.Extensions;
using DatReaderWriter.Extensions.DBObjs;
using DatReaderWriter.Lib;
using DatReaderWriter.Options;
using DatReaderWriter.Types;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp.Formats;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Printing;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DatHammer
{
    /// <summary>
    /// Interaction logic for StringTools.xaml
    /// </summary>
    public partial class StringTools2 : UserControl
    {
        //StringTools2 stringTools;
        //MainWindow2 mainWindow;

        public StringTools2()
        {
            InitializeComponent();
            DataContext = App.State;

            //// OutputConsole.Text = string.Join("\n", Globals.consoleText);
            ACPath.Text = Globals.datFolder.ToString();
        }

        public void AddConsoleLine(string text)
        {
            App.State.ConsoleText += $"{text}\n";
            ScrollViewerConsole.ScrollToEnd();
            
            /*if (text != "" && text != null && text != " ") // OLD CODE
            {
                Globals.consoleText.Add($"{text}");
                //ACPath.Clear();
                OutputConsole.Text = string.Join("\n", Globals.consoleText);
                ScrollViewerConsole.ScrollToEnd();
            }*/
        }

        private void PathDialog_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog pathDialog = new OpenFolderDialog();
            pathDialog.Title = "Select a folder";
            pathDialog.ValidateNames = true;
            pathDialog.DefaultDirectory = @"C:\Turbine\Asheron's Call\";
            pathDialog.InitialDirectory = Globals.datFolder;

            if (pathDialog.ShowDialog() == true)
            {
                string selectedPath = pathDialog.FolderName;
                Globals.UpdateConfigOption("DatFilesPath", $@"{selectedPath}");
                ACPath.Text = selectedPath;
                AddConsoleLine("AC Path: " + $"{selectedPath}");
            }
        }

        /*private void StringToolsWindow_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new StringTools2();
            
            var existing = Application.Current.Windows
                .OfType<StringTools>()
                .FirstOrDefault();

            if (existing != null)
            {
                existing.Activate();
                return;
            }

            StringTools stringTools = new StringTools();
            stringTools.Show();

            if (stringTools.IsVisible == true)
            {
                //stringTools.Hide();
                mainWindow.Show();
            }
            else
            {
                //mainWindow.Hide();
                stringTools.Show();
            }
        }*/

        /*private void MainWindowWindow_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new MainWindow2();
            
            var existing = Application.Current.Windows
                .OfType<MainWindow2>()
                .FirstOrDefault();

            if (existing != null)
            {
                existing.Activate();
                return;
            }

            MainWindow2 mainWindow = new MainWindow2();
            mainWindow.Show();

            if (mainWindow == null)
            {
                mainWindow = new MainWindow();
            }

            if (mainWindow.IsVisible == true)
            {
                return;
            }
            else
            {
                //stringTools.Hide();
                mainWindow.Show();
            }
        }*/

        private void AddTitleButton_Click(object sender, RoutedEventArgs e)
        {
            string userInput = AddTitleInput.Text;
            //TitleReplaceNew(userInput);

            //Globals.EW.Dats.Portal.GetRenderSurface(0x06000000u);
            Globals.DEW.AddTitle(userInput);
            AddConsoleLine($"[INFO]: Title added: {userInput}");
        }

        private void ReplaceSpellButton_Click(object sender, RoutedEventArgs e)
        {
            string idInput = ReplaceSpellNumInput.Text;
            uint idValue = uint.Parse(idInput);
            string nameInput = ReplaceSpellNameInput.Text;
            string descInput = ReplaceSpellDescInput.Text;
            if (idInput == null || idInput == "")
            {
                AddConsoleLine($"[ERROR]: Spell ID cannot be empty!");
            }
            if (nameInput == null || nameInput == "")
            {
                AddConsoleLine($"[ERROR]: Spell name cannot be empty!");
            }
            if (descInput == null || descInput == "")
            {
                AddConsoleLine($"[ERROR]: Spell description cannot be empty!");
            }

            if (idInput != null && idInput != "" && nameInput != null && nameInput != "" && descInput != null && descInput != "")
            {
                var portalPath = System.IO.Path.Combine(Globals.datFolder, "client_portal.dat");
                if (!File.Exists(portalPath))
                {
                    AddConsoleLine($"[ERROR]: File not found!");
                    return;
                }

                var portalDat = new PortalDatabase(portalPath, DatAccessType.ReadWrite);
                var spellTable = portalDat.SpellTable;
                if (spellTable == null)
                {
                    AddConsoleLine($"[ERROR]: Failed to read SpellTable!");
                    return;
                }


                // update spell name / description (no need to worry about updating Components with newly
                // encrypted values, they will be transparently decrypted/encrypted during (un)packing).
                spellTable.Spells[idValue].Name = $"{nameInput}";
                spellTable.Spells[idValue].Description = $"{descInput}";

                //write the updated spell table
                if (!portalDat.TryWriteFile(spellTable))
                {
                    AddConsoleLine($"[ERROR]: Unable to write spell table!");
                    return;
                }
                AddConsoleLine($"[INFO]: Successfully replaced Spell '{nameInput}' ({idInput}) with desc: '{descInput}'.");

                // close dat
                portalDat.Dispose();
            }

            //TitleReplace(userInput);
        }

        private void SpellNumInput_Changed(object sender, TextChangedEventArgs e)
        {
            var spellTable = Globals.portalDat.SpellTable;
            SpellIDPlaceholder.Visibility =
                string.IsNullOrEmpty(ReplaceSpellNumInput.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;

            string idInput = ReplaceSpellNumInput.Text;
            if (idInput == null || idInput == "" || idInput == " ")
            {
                ReplaceSpellNameInput.Clear();
                ReplaceSpellDescInput.Clear();
                return;
            }
            if (!idInput.All(char.IsDigit))
            {
                ReplaceSpellNameInput.Clear();
                ReplaceSpellDescInput.Clear();
                return;
            }
            uint.TryParse(idInput, out uint idValue);
            string nameInput = ReplaceSpellNameInput.Text;
            string descInput = ReplaceSpellDescInput.Text;
            //string nameOutput = spellTable.Spells[idValue].Name;
            //uint nameOutput2 = spellTable.Spells.TryGetValue(idValue);
            //string descOutput = spellTable.Spells[idValue].Description;
            if (spellTable.Spells.TryGetValue(idValue, out SpellBase spellValue))
            {
                AddConsoleLine($"Found {spellValue}!");
            }
            else
            {
                ReplaceSpellNameInput.Clear();
                ReplaceSpellDescInput.Clear();
                return;
            }

            string nameOutput = spellValue.Name;
            string descOutput = spellValue.Description;

            if (nameOutput == null || nameOutput == "")
            {
                ReplaceSpellNameInput.Clear();
                ReplaceSpellNameInput.Text = "";
                if (descOutput == null || descOutput == "")
                {
                    ReplaceSpellDescInput.Clear();
                    ReplaceSpellDescInput.Text = "";
                }
                return;
            }
            if (descOutput == null || descOutput == "")
            {
                ReplaceSpellDescInput.Clear();
                ReplaceSpellDescInput.Text = "";
                if (nameOutput == null || nameOutput == "")
                {
                    ReplaceSpellNameInput.Clear();
                    ReplaceSpellNameInput.Text = "";
                }
                return;
            }

            ReplaceSpellNameInput.Clear();
            ReplaceSpellDescInput.Clear();
            ReplaceSpellNameInput.Text = $"{nameOutput}";
            ReplaceSpellDescInput.Text = $"{descOutput}";
        }

        private void SpellNameInput_Changed(object sender, TextChangedEventArgs e)
        {
            SpellNamePlaceholder.Visibility =
                string.IsNullOrEmpty(ReplaceSpellNameInput.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void SpellDescInput_Changed(object sender, TextChangedEventArgs e)
        {
            SpellDescPlaceholder.Visibility =
                string.IsNullOrEmpty(ReplaceSpellDescInput.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void ACPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            Globals.UpdateConfigOption("DatFilesPath", $@"{ACPath.Text}");
        }

        /*private void StringToolsWindow_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            StringToolsWindow_Click(sender, e);
        }
        private void MainWindowWindow_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            MainWindowWindow_Click(sender, e);
        }*/
    }
}
