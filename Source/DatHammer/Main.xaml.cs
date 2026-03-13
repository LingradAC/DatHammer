using DatReaderWriter;
using DatReaderWriter.DBObjs;
using DatReaderWriter.Enums;
using DatReaderWriter.Extensions;
using DatReaderWriter.Extensions.DBObjs;
using DatReaderWriter.Lib;
using DatReaderWriter.Options;
using DatReaderWriter.Types;
using DotNet.Standard.Common.Utilities;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp.Formats;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Printing;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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

    public partial class Main : Window
    {
        public Main()
        {
            InitializeComponent();
            DataContext = App.State;

            ACPath.Text = Globals.datFolder.ToString();
            ACPath2.Text = Globals.datFolder.ToString();

            Console.WriteLine("Testing!");
            //MainContent.Content = new MainWindow2();
        }

        public void AddConsoleLine(string text)
        {
            App.State.ConsoleText += $"{text}\n";
            ScrollViewerConsole.ScrollToEnd();
            ScrollViewerConsole2.ScrollToEnd();

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
                string selectedPath = $"{pathDialog.FolderName}\\";
                Globals.UpdateConfigOption("DatFilesPath", $@"{selectedPath}");
                ACPath.Text = selectedPath;
                ACPath2.Text = selectedPath;
                AddConsoleLine("AC Path: " + $"{selectedPath}");
            }
        }

          ///////////////////
         /// IMAGE TOOLS ///
        ///////////////////
        
        private void ReplacePalette(object sender, RoutedEventArgs e)
        {

        }

        private void ImgPathDialog_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog imgPathDialog = new OpenFileDialog();
            imgPathDialog.Title = "Select an image";
            imgPathDialog.ValidateNames = true;
            imgPathDialog.DefaultDirectory = @"C:\Turbine\Asheron's Call\";
            imgPathDialog.InitialDirectory = Globals.datFolder;
            imgPathDialog.AddExtension = true;
            imgPathDialog.Filter = "Image Files|*.png;*.jpg;*.bmp|AllFiles|*.*";

            if (imgPathDialog.ShowDialog() == true)
            {
                string selectedPath = imgPathDialog.FileName;
                ImgPath.Text = selectedPath;
                if (!System.IO.Path.Exists(ImgPath.Text))
                {
                    AddConsoleLine($"[ERROR]: Path does not exist: {selectedPath}");
                    return;
                }
                var selectedPathFile = System.IO.Path.GetFileNameWithoutExtension(ImgPath.Text);
                //var selectedPathID = ImgPath.Text.Remove(selectedPath.Length - 4);
                ImgID.Text = selectedPathFile;
                AddConsoleLine("Image path: " + $"{selectedPath}");
            }
        }

        private void FontTesting(object sender, RoutedEventArgs e)
        {
            uint ID = 0x40000000u;
            
            var font = Globals.datFiles.Get<DatReaderWriter.DBObjs.Font>(ID);
            var fontAll = Globals.datFiles.GetAllIdsOfType<DatReaderWriter.DBObjs.Font>();

            /*if (font == null)
            {
                AddConsoleLine($"{ID} not found");
                return;
            }*/

            foreach (var f in fontAll)
            {
                var fontAllFont = Globals.datFiles.Get<DatReaderWriter.DBObjs.Font>(f);
                
                AddConsoleLine($"Found 0x{fontAllFont?.Id:X8}");
            }

            //string text = $"{font?.Id:X8}";
            //AddConsoleLine($"Found {text}");
        }

        private void AddImageButton_Click(object sender, RoutedEventArgs e) // Credits to Trevis
        {
            //OutputConsole.Text = string.Join("\n", consoleText);

            //AddImage(sender, e);

            // Replace a texture with a PNG/BMP/etc on disk

            string imageInput = ImgPath.Text.Trim();
            string imageIDInput = ImgID.Text;
            bool? resizeChecked = imageResize.IsChecked;
            var imageDropdownInput2 = dropdownImageType.SelectedItem;

            if (uint.TryParse(imageIDInput, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint imageValue))
            {
                AddConsoleLine($"Image ID: 0x{0x06000000u | imageValue:X8}");
            }
            else
            {
                AddConsoleLine($"[ERROR]: Image ID is invalid!");
                return;
            }
            if (imageValue > 0x00FFFFFFu)
            {
                AddConsoleLine("[ERROR]: Image ID must be 6 hex digits or less!");
                return;
            }
            uint finalImageID = 0x06000000u | imageValue;

            var renderSurface = Globals.DEW.Get<RenderSurface>(finalImageID).Value;

            if (string.IsNullOrWhiteSpace(imageInput))
            {
                AddConsoleLine("[ERROR]: Image path cannot be blank!");
                return;
            }

            bool resizeImage = false;
            if (resizeChecked == true)
            {
                //AddConsoleLine($"[INFO]: Image type: {text}");
                resizeImage = true;
            }
            else if (resizeChecked == false)
            {
                resizeImage = false;
            }
            else if (resizeChecked == null)
            {
                AddConsoleLine($"[ERROR]: CheckBox error! Send to devs.");
                return;
            }

            string imageType = "";
            if (imageDropdownInput2 is ComboBoxItem type)
            {
                string text = type.Tag.ToString();
                //AddConsoleLine($"[INFO]: Image type: {text}");

                if (text == "PFID_A8B8G8R8")
                {
                    imageType = $"{text}";
                }
                else if (text == "PFID_A8R8G8B8")
                {
                    imageType = $"{text}";
                }
                else if (text == "PFID_CUSTOM_RAW_JPEG")
                {
                    imageType = $"{text}";
                }
                else if (text == "PFID_INDEX16")
                {
                    imageType = $"{text}";
                }
                else if (text == "PFID_R8G8B8")
                {
                    imageType = $"{text}";
                }
                else if (text == "PFID_DXT1")
                {
                    imageType = $"{text}";
                }
                else if (text == "PFID_DXT2")
                {
                    imageType = $"{text}";
                }
                else if (text == "PFID_DXT3")
                {
                    imageType = $"{text}";
                }
                else if (text == "PFID_DXT4")
                {
                    imageType = $"{text}";
                }
                else if (text == "PFID_DXT5")
                {
                    imageType = $"{text}";
                }
                else if (text == null || text == "")
                {
                    AddConsoleLine($"[ERROR]: ComboBox error! Send to devs.");
                    return;
                }
            }

            // Replace and optionally resize to match original dimensions
            renderSurface.ReplaceWith($"{imageInput}", shouldResize: resizeImage);

            Globals.DEW.Save(renderSurface);
            AddConsoleLine($"[INFO]: Successfully added image {imageInput}.");

            // Export a texture to a file
            //renderSurface.SaveToImageFile("path/to/extracted_texture.png", Globals.DEW);

            // Get raw RGBA bytes
            //byte[] rgbaBytes = renderSurface.ToRgba8(Globals.DEW);


            //byte[] imageBytes = LoadImageBytes(imageInput);
            //textureTable.SourceData.Append(imageBytes);


            //textureTable.SourceData.Append(byte.Parse(imageInput));
        }

        private void ImgPathInput_Changed(object sender, TextChangedEventArgs e)
        {
            ImagePathPlaceholder.Visibility =
                string.IsNullOrEmpty(ImgPath.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
            if (!System.IO.Path.Exists(ImgPath.Text))
            {
                return;
            }
            var selectedPath = System.IO.Path.GetFileNameWithoutExtension(ImgPath.Text);
            //var selectedPathID = ImgPath.Text.Remove(selectedPath.Length - 4);
            ImgID.Text = selectedPath;
        }

        private void ImgIDInput_Changed(object sender, TextChangedEventArgs e)
        {
            ImageIDPlaceholder.Visibility =
                string.IsNullOrEmpty(ImgID.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
          ///////////////////
         ///     END     ///
        ///////////////////

        private void ACPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            Globals.UpdateConfigOption("DatFilesPath", $@"{ACPath.Text}");
        }

          ////////////////////
         /// STRING TOOLS ///
        ////////////////////
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
          ////////////////////
         ///      END     ///
        ////////////////////

        private void MainWindow_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Visibility = Visibility.Visible;
            StringTools.Visibility = Visibility.Collapsed;
        }

        private void StringTools_Click(object sender, RoutedEventArgs e)
        {
            StringTools.Visibility = Visibility.Visible;
            MainWindow.Visibility = Visibility.Collapsed;
        }

        private void PopulateStrings()
        {

            //StringTable? stringTableA = Globals.datFiles.Get<StringTable>(0x23000001u);

            //var st = Globals.datFiles.Get<StringTable>(0x23000001u);
            
            /*if (st == null)
            {
                AddConsoleLine("[ERROR]: Could not read StringTable!");
                return;
            }*/

            var stringTableData2 = Globals.datFiles.GetAllIdsOfType<DatReaderWriter.DBObjs.StringTable>();
            //Globals.datFiles.Portal.
            //var stringTableData22 = Globals.datFiles.Local.GetStringTable(0x23000001u);

            //string tableData = string.Join("\n", stringTableData2);

            foreach (var tableData2 in stringTableData2)
            {
                var data = Globals.datFiles.Get<StringTable>(tableData2);

                AddConsoleLine($"Found 0x{data.Id:X8}");

                Border border = new Border();
                border.Height = 40;
                border.HorizontalAlignment = HorizontalAlignment.Stretch;
                border.VerticalAlignment = VerticalAlignment.Top;
                border.CornerRadius = new CornerRadius(3, 3, 3, 3);
                border.Margin = new Thickness(3, 3, 3, 1);
                border.ClipToBounds = true;
                border.BorderThickness = new Thickness(1);
                border.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(74, 74, 74));
                StringStackPanel.Children.Add(border);

                Grid grid = new Grid();
                grid.HorizontalAlignment = HorizontalAlignment.Stretch;
                grid.VerticalAlignment = VerticalAlignment.Stretch;
                grid.Background = new SolidColorBrush(Colors.Transparent);
                border.Child = grid;

                string stringId = $"{data.Id:X8}";
                TextBlock textBlock = new TextBlock();
                //textBlock.Width = 100;
                textBlock.Height = Double.NaN;
                textBlock.HorizontalAlignment = HorizontalAlignment.Stretch;
                textBlock.VerticalAlignment = VerticalAlignment.Top;
                //textBlock.Margin = new Thickness(2, 0, 0, 0);
                textBlock.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(238, 238, 238));
                textBlock.Text = $"0x{stringId}";
                textBlock.Padding = new Thickness(3, 0, 0, 3);

                /*if (!Globals.datFiles.TryGet<StringTable>(tableData2, out var stringTableData3))
                {
                    AddConsoleLine("[ERROR]: Failed to read string table!");
                    return;
                }*/

                List<string> stringlist = [];

                //string string1 = data.Strings.Values;

                foreach (var item in data.Strings)
                {
                    //AddConsoleLine($"Found 0x{item.Value.DataId.DataId:X8}");
                    stringlist.Add($"Found 0x{item.Value.DataId.DataId:X8}");

                    foreach (var item2 in item.Value.Strings)
                    {
                        stringlist.Add($"Found string: \u0022{item2}\u0022");
                        
                        //AddConsoleLine($"Found {item2}");
                        //AddConsoleLine($"String: \"{item2.Value}\"");
                    }
                }

                //AddConsoleLine($"{stringlist.Count}");

                IEnumerable<string> firstfive = stringlist.Take(5);
                foreach (var item in firstfive)
                {
                    AddConsoleLine(item);
                }

                /*stringlist.ForEach(async item =>
                {
                    AddConsoleLine($"{item}");
                    await Task.Delay(5000);
                });*/

                //string string2 = string.Join("", string1);

                TextBox textBox = new TextBox();
                textBox.Text = "{string2}"; // Add $
                textBox.HorizontalAlignment = HorizontalAlignment.Stretch;
                textBox.VerticalAlignment = VerticalAlignment.Bottom;
                //textBox.Margin = new Thickness(2, 20, 0, 0);
                textBox.Height = 18;
                textBox.BorderThickness = new Thickness(0);
                textBox.Background = new SolidColorBrush(Colors.Transparent);
                textBox.ClipToBounds = false;
                textBox.Padding = new Thickness(2, 0, 0, 1);

                grid.Children.Add(textBlock);
                grid.Children.Add(textBox);

                /*var testLangString = Globals.datFiles.Local.GetStringTable(tableData2);
                if (testLangString == null)
                {
                    AddConsoleLine($"String not found: {tableData2}");
                    return;
                }*/
                
                //AddConsoleLine($"{string.Join("", testLangString.Strings.Keys)}");

                /*foreach (var stringData in stringTableData3.Strings.Keys)  //// NOT DISCARDED ////
                {
                    AddConsoleLine($"Found string: '{stringData}'");
                }
                //stringTableData3.Strings;

                if (stringTableData3.Strings.TryGetValue(stringTableData3.Strings.Keys.First<uint>(), out StringTableString stringTableData4))
                {
                    AddConsoleLine($"Found string: '{stringTableData4}'");
                }
                else
                {
                    AddConsoleLine("[ERROR]: Failed to read StringTableString!");
                    return;
                }*/

                // Remove and add string
                /*stringTableData3.Strings.Remove(tableData2);
                stringTableData3.Strings.Add(tableData2, new StringTableString()
                {
                    Strings = [""]
                });*/
            }

            //if (stringTableData.Strings.Keys)

            /*foreach (var stringTableData in stringTableData.Strings.Values)
            {
                AddConsoleLine($"Found key: '{key}'");
            }*/

            //List<string> stringTableStrings = new List<string>();

            /*TextBox textBox = new TextBox();
            textBox.Height = 40;
            textBox.Text = tableData;
            textBox.HorizontalAlignment = HorizontalAlignment.Stretch;
            textBox.VerticalAlignment = VerticalAlignment.Top;
            StringStackPanel.Children.Add(textBox);*/

        }

        private void CreateLayoutInfoWindow(object sender, RoutedEventArgs e)
        {

        }

        private Border selectedBorder = null;
        private Border hoveredBorder = null;
        private void SelectableBorder_Click(object sender, MouseButtonEventArgs e)
        {
            Border clickedBorder = (Border)sender;

            if (selectedBorder != null)
            {
                ThumbVisibility(selectedBorder, Visibility.Collapsed);
                selectedBorder.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 50, 160));
                selectedBorder.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(51, 110, 150, 210));
            }

            selectedBorder = clickedBorder;
            ThumbVisibility(selectedBorder, Visibility.Visible);
            selectedBorder.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 175, 0));
            selectedBorder.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(64, 240, 187, 30));
        }

        private void SelectableBorder_Hover(object sender, MouseEventArgs e)
        {
            Border hoverBorder = (Border)sender;

            if (hoveredBorder != null)
            {
                if (hoveredBorder == selectedBorder)
                {
                    hoveredBorder.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 175, 0));
                    hoveredBorder.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(64, 240, 187, 30));
                }
                else
                {
                    hoveredBorder.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 50, 160));
                    hoveredBorder.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(51, 110, 150, 210));
                }
            }

            hoveredBorder = hoverBorder;
            if (hoveredBorder == selectedBorder)
            {
                hoveredBorder.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 175, 0));
                hoveredBorder.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(64, 240, 187, 30));
            }
            else
            {
                hoveredBorder.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(200, 200, 20));
                hoveredBorder.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(64, 245, 245, 30));
            }
        }
        private void SelectableBorder_Unhover(object sender, MouseEventArgs e)
        {
            Border hoverBorder = (Border)sender;

            if (hoveredBorder == selectedBorder)
            {
                hoveredBorder.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 175, 0));
                hoveredBorder.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(64, 240, 187, 30));
                return;
            }

            hoveredBorder = hoverBorder;
            hoveredBorder.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 50, 160));
            hoveredBorder.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(51, 110, 150, 210));
        }

        private uint layoutID = 0x21000000;
        private void CreateLayout(object sender, RoutedEventArgs e)
        {
            LayoutWindow.Content = null;

            var layoutAll = Globals.datFiles.GetAllIdsOfType<LayoutDesc>();
            foreach (var layoutItem in layoutAll)
            {
                bool exists = dropdownLayout.Items
                    .OfType<ComboBoxItem>()
                    .Any(i => (string)i.Tag == $"{layoutItem:X8}");

                if (!exists)
                {
                    ComboBoxItem newItem = new ComboBoxItem();
                    newItem.Tag = $"{layoutItem:X8}";
                    newItem.Content = $"0x{layoutItem:X8}";
                    newItem.Name = $"x{layoutItem:X8}";
                    newItem.FontSize = 12;

                    dropdownLayout.Items.Add(newItem);
                }
            }

            //dropdownLayout.Items.Clear();

            //uint layoutID = 0x21000004; // LayoutDesc to render.
            if (dropdownLayout.SelectedItem is ComboBoxItem type)
            {
                string text = type.Tag.ToString();
                //AddConsoleLine($"[INFO]: Image type: {text}");

                layoutID = uint.Parse(text, NumberStyles.HexNumber);
            }

            var layout = Globals.datFiles.Get<LayoutDesc>(layoutID);

            if (layout == null)
            {
                AddConsoleLine($"Can't find {layoutID}");
                return;
            }

            System.Windows.Controls.Border border = new System.Windows.Controls.Border();
            border.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 50, 160));
            border.BorderThickness = new Thickness(1);
            border.Background = new SolidColorBrush(Colors.Transparent);
            border.Width = layout.Width / 2.5;
            border.Height = layout.Height / 2.5;
            border.IsHitTestVisible = false;
            //border.MouseLeftButtonDown += SelectableBorder_Click;
            //border.MouseEnter += SelectableBorder_Hover;

            Panel.SetZIndex(border, -100);
            Canvas canvas = new Canvas();
            canvas.Children.Add(border);
            canvas.Width = layout.Width / 2.5;
            canvas.Height = layout.Height / 2.5;
            canvas.Margin = new Thickness(0, 0, 0, 0);
            canvas.IsHitTestVisible = true;
            canvas.Background = new SolidColorBrush(Colors.Transparent);
            Panel.SetZIndex(canvas, -200);

            LayoutWindow.Content = canvas;

            AddConsoleLine("Success 1");

            foreach (var item in layout.Elements)
            {
                CreateCanvas(sender, e, item, canvas, 0);
                
                /*System.Windows.Controls.Border border2 = new System.Windows.Controls.Border();
                border2.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 70, 150));
                border2.BorderThickness = new Thickness(1);
                border2.Width = item.Value.Width / 2.5;
                border2.Height = item.Value.Height / 2.5;
                Panel.SetZIndex(border2, System.Convert.ToInt32(item.Value.ZLevel) + 1);
                Canvas canvas2 = new Canvas();
                canvas2.Children.Add(border2);
                canvas2.Width = item.Value.Width / 2.5;
                canvas2.Height = item.Value.Height / 2.5;
                canvas2.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 0, 30, 80));
                canvas2.Margin = new Thickness(item.Value.X / 2.5, item.Value.Y / 2.5, 0, 0);
                Panel.SetZIndex(canvas2, System.Convert.ToInt32(item.Value.ZLevel));
                canvas.Children.Add(canvas2);*/

                AddConsoleLine($"Loaded 0x{item.Value.ElementId:X8}");

                /*foreach (var item2 in item.Value.Children)
                {
                    
                    
                    System.Windows.Controls.Border border3 = new System.Windows.Controls.Border();
                    border3.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 70, 150));
                    border3.BorderThickness = new Thickness(1);
                    border3.Width = item2.Value.Width / 2.5;
                    border3.Height = item2.Value.Height / 2.5;
                    Panel.SetZIndex(border3, System.Convert.ToInt32(item2.Value.ZLevel) + 1);
                    Canvas canvas3 = new Canvas();
                    canvas3.Children.Add(border3);
                    canvas3.Width = item2.Value.Width / 2.5;
                    canvas3.Height = item2.Value.Height / 2.5;
                    canvas3.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 0, 30, 80));
                    canvas3.Margin = new Thickness(item2.Value.X / 2.5, item2.Value.Y / 2.5, 0, 0);
                    Panel.SetZIndex(canvas3, System.Convert.ToInt32(item2.Value.ZLevel));
                    canvas2.Children.Add(canvas3);*/

                    /*AddConsoleLine("Success 3");

                    foreach (var item3 in item2.Value.Children)
                    {
                        System.Windows.Controls.Border border4 = new System.Windows.Controls.Border();
                        border4.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 70, 150));
                        border4.BorderThickness = new Thickness(1);
                        border4.Width = item3.Value.Width / 2.5;
                        border4.Height = item3.Value.Height / 2.5;
                        Panel.SetZIndex(border4, System.Convert.ToInt32(item3.Value.ZLevel) + 1);
                        Canvas canvas4 = new Canvas();
                        canvas4.Children.Add(border4);
                        canvas4.Width = item3.Value.Width / 2.5;
                        canvas4.Height = item3.Value.Height / 2.5;
                        canvas4.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 0, 30, 80));
                        canvas4.Margin = new Thickness(item3.Value.X / 2.5, item3.Value.Y / 2.5, 0, 0);
                        Panel.SetZIndex(canvas4, System.Convert.ToInt32(item3.Value.ZLevel));
                        canvas3.Children.Add(canvas4);

                        AddConsoleLine("Success 4");

                        foreach (var item4 in item3.Value.Children)
                        {
                            System.Windows.Controls.Border border5 = new System.Windows.Controls.Border();
                            border5.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 70, 150));
                            border5.BorderThickness = new Thickness(1);
                            border5.Width = item4.Value.Width / 2.5;
                            border5.Height = item4.Value.Height / 2.5;
                            Panel.SetZIndex(border5, System.Convert.ToInt32(item4.Value.ZLevel) + 1);
                            Canvas canvas5 = new Canvas();
                            canvas5.Children.Add(border5);
                            canvas5.Width = item4.Value.Width / 2.5;
                            canvas5.Height = item4.Value.Height / 2.5;
                            canvas5.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 0, 30, 80));
                            canvas5.Margin = new Thickness(item4.Value.X / 2.5, item4.Value.Y / 2.5, 0, 0);
                            Panel.SetZIndex(canvas5, System.Convert.ToInt32(item4.Value.ZLevel));
                            canvas4.Children.Add(canvas5);

                            AddConsoleLine("Success 5");
                        }
                    }
                }*/
            }
        }

        private void CreateCanvas(object sender, RoutedEventArgs e, KeyValuePair<uint, ElementDesc> element, Canvas canvas, int zindex) // Removed "HashTable<uint, ElementDesc> parent"
        {
            System.Windows.Controls.Border border2 = new System.Windows.Controls.Border();
            border2.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 50, 160));
            border2.BorderThickness = new Thickness(0.3);
            if (element.Value.Width == 800 && element.Value.Height == 600)
            {
                border2.Background = new SolidColorBrush(Colors.Transparent);
                border2.IsHitTestVisible = false;
            }
            else
            {
                border2.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(51, 110, 150, 210));
            }
            border2.Width = element.Value.Width / 2.5;
            border2.Height = element.Value.Height / 2.5;
            border2.MouseLeftButtonDown += SelectableBorder_Click;
            border2.MouseEnter += SelectableBorder_Hover;
            border2.MouseLeave += SelectableBorder_Unhover;
            int ZIndex = System.Convert.ToInt32(element.Value.ZLevel) + zindex + 2;
            Panel.SetZIndex(border2, ZIndex);
            Canvas canvas2 = new Canvas();
            canvas2.Children.Add(border2);
            canvas2.Width = element.Value.Width / 2.5;
            canvas2.Height = element.Value.Height / 2.5;
            canvas2.Margin = new Thickness(( element.Value.X) / 2.5, ( element.Value.Y) / 2.5, 0, 0);
            Thumb resize = new Thumb();
            resize.Width = 8;
            resize.Height = 8;
            resize.Background = System.Windows.Media.Brushes.White;
            resize.BorderBrush = System.Windows.Media.Brushes.Black;
            resize.BorderThickness = new Thickness(1);
            resize.Visibility = Visibility.Collapsed;
            resize.DragDelta += (sender, e) => Thumb_DragDelta(sender, e, border2);
            resize.DragCompleted += (sender, e) => Thumb_DragCompleted(sender, e, element);
            resize.Cursor = Cursors.SizeNWSE;
            Panel.SetZIndex(resize, ZIndex + 1);
            canvas2.Children.Add(resize);
            Canvas.SetRight(resize, -4);
            Canvas.SetBottom(resize, -4);
            Panel.SetZIndex(canvas2, System.Convert.ToInt32(element.Value.ZLevel) + zindex + 1);
            canvas.Children.Add(canvas2);
            
            //var media = element.Value.StateDesc.Media;
            /*foreach (var state in element.Value.States)
            {
                foreach (MediaDescImage media in state.Value.Media)
                {
                    //AddConsoleLine($"Found {media.MediaType.ToString()}");

                    uint File = media.File;
                    AddConsoleLine($"[Image] - Found 0x{File:X8}");

                    if (media.MediaType == MediaType.Image)
                    {
                        MediaDescImage image = (MediaDescImage)media;
                        //uint File = image.File;

                        //AddConsoleLine($"[Image] - Found 0x{File:X8}");
                    }
                }

                string stateId = $"0x{state.Value.StateId:X8}";
                AddConsoleLine($"Found state {stateId}");
            }*/

            foreach (var child in element.Value.Children)
            {
                CreateCanvas(sender, e, child, canvas2, ZIndex);
            }
        }

        private void TestStrings_Click(object sender, RoutedEventArgs e)
        {
            PopulateStrings();
        }

        private void AddStrings_Click(object sender, RoutedEventArgs e)
        {
            return;
        }

        private void dropdownLayout_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CreateLayout(sender, e);
        }

        Border selectedElement = null;
        private void Thumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e, Border border)
        {
            Thumb thumb = sender as Thumb;
            Canvas element = thumb.Parent as Canvas;

            element.Width = Math.Max(1, element.Width + e.HorizontalChange);
            element.Height = Math.Max(1, element.Height + e.VerticalChange);
            border.Width = Math.Max(1, element.Width + e.HorizontalChange);
            border.Height = Math.Max(1, element.Height + e.VerticalChange);
        }

        private void Thumb_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e, KeyValuePair<uint, ElementDesc> elementDesc)
        {
            elementDesc.Value.Width += (uint)e.HorizontalChange;
            elementDesc.Value.Height += (uint)e.VerticalChange;

            var layoutDesc = Globals.datFiles.Local.GetLayoutDesc(layoutID);

            if (!Globals.datFiles.TryWriteFile(layoutDesc))
            {
                AddConsoleLine($"[ERROR]: Failed to write layoutDesc 0x{layoutID:X8}");
                return;
            }
            AddConsoleLine($"[INFO]: Successfully wrote layoutDesc 0x{layoutID:X8}");
            Globals.datFiles.Dispose();
        }

        private void ThumbVisibility(Border element, Visibility vis)
        {
            if (element.Parent is Canvas canvas)
            {
                foreach (var child in canvas.Children)
                {
                    if (child is Thumb thumb)
                        thumb.Visibility = vis;
                }
            }
        }
    }
}