using DatReaderWriter.Extensions;
using DatReaderWriter.Extensions.DBObjs;
using DatReaderWriter;
using DatReaderWriter.DBObjs;
using DatReaderWriter.Enums;
using DatReaderWriter.Lib;
using DatReaderWriter.Options;
using DatReaderWriter.Types;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.Drawing.Imaging;
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
using System.Globalization;
using static System.Runtime.InteropServices.JavaScript.JSType;
using SixLabors.ImageSharp.Formats;

namespace DatHammer
{
    public static class Globals
    {
        public static string json = File.ReadAllText("./Options.json");
        public static JObject config = JObject.Parse(json);
        public static string datFolder = config["DatFilesPath"].ToString();

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
    }

    public partial class MainWindow : Window
    {
        public List<string> consoleText = new List<string>();

        public MainWindow()
        {
            InitializeComponent();

            OutputConsole.Text = string.Join("\n", consoleText);
            //OutputConsole.Text = "Test";
            ACPath.Text = Globals.datFolder.ToString();

            Console.WriteLine("Testing!");

        }

        public static readonly int[] BucketSizes = new int[]
        {
            11,
            23,
            47,
            89,
            191,
            383,
            761,
            1531,
            3067,
            6143,
            12281,
            24571,
            49139,
            98299,
            196597,
            393209,
            786431,
            1572853,
            3145721,
            6291449,
            12582893,
            25165813,
            50331599
        };

        /*private void TitleReplace(string title)
        {
            // new title info
            var inputTitle = title;
            var enumTitle = inputTitle.Replace(" ", "");
            string outputTitle = Regex.Replace(enumTitle, @"[^a-zA-Z_]", "");
            if (outputTitle.EndsWith("_") || outputTitle.EndsWith("_ "))
            {
                outputTitle = outputTitle.Substring(0, outputTitle.Length - 1);
            }
            if (outputTitle == "" || outputTitle == " " || outputTitle == null)
            {
                AddConsoleLine("[ERROR]: Title is invalid.");
                return;
            }

            var enumId = "ID_CharacterTitle_" + $"{outputTitle}";
            var titleString = $"{inputTitle}";

            // open the dat collection
            var datPath = ACPath.Text;
            var dats = new DatCollection(datPath, DatAccessType.ReadWrite);

            // load the relevant string table and enum mapper
            var stringTableTitles = dats.Local.Get<StringTable>(0x2300000E);
            var enumTitles = dats.Portal.Get<EnumMapper>(0x22000041);

            // make sure they loaded correctly
            if (stringTableTitles == null)
            {
                Console.WriteLine("Failed to load string table 0x2300000E");
                AddConsoleLine("[ERROR]: Failed to load string table 0x2300000E");
                return;
            }
            if (enumTitles == null)
            {
                Console.WriteLine("Failed to load titles enum 0x22000041");
                AddConsoleLine("[ERROR]: Failed to load titles enum 0x22000041");
                return;
            }

            // check if the enum already exists
            if (enumTitles.IdToStringMap.Values.Contains(enumId))
            {
                var existingId = enumTitles.IdToStringMap.First(kv => kv.Value == enumId).Key;
                Console.WriteLine($"Enum ID '{enumId}' already exists with key {existingId}.");
                AddConsoleLine($"[ERROR]: Enum ID '{enumId}' already exists with key {existingId}.");
                return;
            }

            // first we add a new enum mapper for the new title, at the next available ID
            var newEnumId = enumTitles.IdToStringMap.Keys.Max() + 1;
            enumTitles.IdToStringMap[newEnumId] = enumId;

            // now we compute the hash based on the enum string, and add it to the string table
            var newEnumHash = ComputeHash(enumId);
            stringTableTitles.StringTableData[newEnumHash] = new StringTableData()
            {
                Strings = [titleString]
            };

            // TODO: DatReaderWriter should sort the titles before saving, and properly read the bucket sizes...
            // these are the original values from the dats
            var enumBucketSize = BucketSizes[(int)enumTitles.NumberingType]; // NumberingType is actually BucketSize index
            var stringTableBucketSize = BucketSizes[stringTableTitles.Unknown]; // Unknown is actually BucketSize index

            // update the bucket sizes based on the new entry counts
            enumBucketSize = GetBucketSize(enumTitles.IdToStringMap.Count);
            stringTableBucketSize = GetBucketSize(stringTableTitles.StringTableData.Count);
            enumTitles.NumberingType = (NumberingType)GetBucketSizeIndex(enumBucketSize);
            stringTableTitles.Unknown = (byte)GetBucketSizeIndex(stringTableBucketSize);

            // re-sort the dictionaries based on the bucket sizes
            enumTitles.IdToStringMap = enumTitles.IdToStringMap
                .OrderBy(i => i.Key % enumBucketSize)
                .ToDictionary(i => i.Key, i => i.Value);
            stringTableTitles.StringTableData = stringTableTitles.StringTableData
                .OrderBy(x => x.Key % stringTableBucketSize)
                .ToDictionary(i => i.Key, i => i.Value);

            // save the changes back to the dats (no iteration increase, just overwrite)
            if (!dats.Portal.TryWriteFile(enumTitles))
            {
                Console.WriteLine("Failed to write updated enum mapper back to dat.");
                AddConsoleLine("[ERROR]: Failed to write updated enum mapper back to dat.");
                return;
            }
            if (!dats.Local.TryWriteFile(stringTableTitles))
            {
                Console.WriteLine("Failed to write updated string table back to dat.");
                AddConsoleLine("[ERROR]: Failed to write updated string table back to dat.");
                return;
            }

            dats.Dispose();

            Console.WriteLine($"Added new title enum {newEnumId} with hash {newEnumHash:X8} and string '{titleString}'");
            AddConsoleLine($"[INFO]: Successfully added new title '{titleString}' (enum: '{newEnumId}') with hash '{newEnumHash:X8}'.");
        }*/

        private void TitleReplaceNew(string title)
        {
            // new title info
            var inputTitle = title;
            var enumTitle = inputTitle.Replace(" ", "");
            string outputTitle = Regex.Replace(enumTitle, @"[^a-zA-Z_]", "");
            if (outputTitle.EndsWith("_") || outputTitle.EndsWith("_ "))
            {
                outputTitle = outputTitle.Substring(0, outputTitle.Length - 1);
            }
            if (outputTitle == "" || outputTitle == " " || outputTitle == null)
            {
                AddConsoleLine("[ERROR]: Title is invalid.");
                return;
            }

            var enumId = "ID_CharacterTitle_" + $"{outputTitle}";
            var titleString = $"{inputTitle}";

            // open the dat collection in write mode
            var datPath = ACPath.Text;
            var dats = new DatCollection(datPath, DatAccessType.ReadWrite);

            // load the relevant string table and enum mapper
            if (!dats.TryGet<StringTable>(0x2300000E, out var stringTableTitles))
            {
                AddConsoleLine("[ERROR]: Could not read StringTable!");
                return;
            }

            if (!dats.TryGet<EnumMapper>(0x22000041, out var enumTitles))
            {
                AddConsoleLine("[ERROR]: Could not read EnumMapper!");
                return;
            }

            // check if the enum already exists
            if (enumTitles.IdToStringMap.ContainsValue(enumId))
            {
                var existingId = enumTitles.IdToStringMap.First(kv => kv.Value == enumId).Key;
                Console.WriteLine($"Enum ID '{enumId}' already exists with key {existingId}.");
                AddConsoleLine($"[ERROR]: Enum ID '{enumId}' already exists with key {existingId}.");
                return;
            }

            // first we add a new enum mapper for the new title, at the next available ID
            var newEnumId = enumTitles.IdToStringMap.Keys.Max() + 1;
            enumTitles.IdToStringMap[newEnumId] = enumId;

            // now we compute the hash based on the enum string, and add it to the string table
            var newEnumHash = ComputeHash(enumId);
            stringTableTitles.Strings[newEnumHash] = new StringTableString()
            {
                Strings = [titleString]
            };

            // save the changes back to the dats (no iteration increase, just overwrite)
            if (!dats.Portal.TryWriteFile(enumTitles) || !dats.Local.TryWriteFile(stringTableTitles))
            {
                Console.WriteLine("Failed to write updates back to dat.");
                AddConsoleLine("[ERROR]: Failed to write updated strings back to dat.");
                return;
            }

            dats.Dispose();

            Console.WriteLine($"Added new title enum {newEnumId} with hash {newEnumHash:X8} and string '{titleString}'");
            AddConsoleLine($"[INFO]: Successfully added new title '{titleString}' (enum: '{newEnumId}') with hash '{newEnumHash:X8}'.");
        }

        public static uint ComputeHash(string strToHash)
        {
            long result = 0;

            if (strToHash.Length > 0)
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                byte[] str = Encoding.GetEncoding(1252).GetBytes(strToHash);

                foreach (sbyte c in str)
                {
                    result = c + (result << 4);

                    if ((result & 0xF0000000) != 0)
                        result = (result ^ ((result & 0xF0000000) >> 24)) & 0x0FFFFFFF;
                }
            }

            return (uint)result;
        }

        public static int GetBucketSize(int entryCount)
        {
            // pick the smallest bucket
            foreach (var size in BucketSizes)
            {
                if (size >= entryCount)
                    return size;
            }
            return BucketSizes[^1]; // largest
        }

        public static int GetBucketSizeIndex(int entry)
        {
            for (int i = 0; i < BucketSizes.Length; i++)
            {
                if (BucketSizes[i] == entry)
                    return i;
            }
            return -1; // not found
        }

        /*public static uint ComputeHash(string strToHash)
        {
            long result = 0;

            if (strToHash.Length > 0)
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                byte[] str = Encoding.GetEncoding(1252).GetBytes(strToHash);

                foreach (sbyte c in str)
                {
                    result = c + (result << 4);

                    if ((result & 0xF0000000) != 0)
                        result = (result ^ ((result & 0xF0000000) >> 24)) & 0x0FFFFFFF;
                }
            }

            return (uint)result;
        }*/

        public void UpdateConfigOption(string key, string value)
        {
            string path = "./Options.json";
            JObject config = JObject.Parse(File.ReadAllText(path));
            config[key] = value;
            File.WriteAllText(path, config.ToString(Newtonsoft.Json.Formatting.Indented));
        }

        public void AddConsoleLine(string text)
        {
            if (text != "" && text != null && text != " ")
            {
                consoleText.Add($"{text}");
                //ACPath.Clear();
                OutputConsole.Text = string.Join("\n", consoleText);
                ScrollViewerConsole.ScrollToEnd();
            }
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            //OutputConsole.Text = string.Join("\n", consoleText);

            AddConsoleLine($"[Path]: {ACPath.Text}");
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
                UpdateConfigOption("DatFilesPath", $@"{selectedPath}");
                ACPath.Text = selectedPath;
                AddConsoleLine("AC Path: " + $"{selectedPath}");
            }
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
                AddConsoleLine("Image path: " + $"{selectedPath}");
            }
        }

        private byte[] LoadImageBytes(string path) // DEPRECATED - USE ConvertImageToPngBytes
        {
            using var img = System.Drawing.Image.FromFile(path);
            using var ms = new MemoryStream();
            img.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp); // normalize everything to BMP
            return ms.ToArray();
        }

        public static byte[] ConvertImageToPngBytes(string imagePath) // ChatGPT helped with this
        {
            // Load image (jpg, png, bmp, etc.)
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze(); // important for threading safety

            // Encode as BMP
            BmpBitmapEncoder encoder = new BmpBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using MemoryStream ms = new MemoryStream();
            encoder.Save(ms);

            return ms.ToArray(); // Unified PNG bytes
        }

        public static byte[] ConvertBmpToPFID_R8G8B8(string bmpPath) // ChatGPT helped with this
        {
            using Bitmap bmp = new Bitmap(bmpPath);

            int width = bmp.Width;
            int height = bmp.Height;

            using Bitmap rgb = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            using (Graphics g = Graphics.FromImage(rgb))
            {
                g.DrawImage(bmp, 0, 0, width, height);
            }

            byte[] data = new byte[width * height * 3];
            int i = 0;

            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    System.Drawing.Color c = rgb.GetPixel(x, y);
                    data[i++] = c.R;
                    data[i++] = c.G;
                    data[i++] = c.B;
                }

            return data;
        }

        private void AddImageButton_Click(object sender, RoutedEventArgs e) // Credits to Trevis
        {
            //OutputConsole.Text = string.Join("\n", consoleText);

            //AddImage(sender, e);

            // Replace a texture with a PNG/BMP/etc on disk

            string imageInput = ImgPath.Text.Trim();
            string imageIDInput = ImgID.Text;
            var imageDropdownInput = dropdownImageResize.SelectedItem;
            var imageDropdownInput2 = dropdownImageType.SelectedItem;

            if (uint.TryParse(imageIDInput, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint imageValue))
            {
                Console.WriteLine($"Image ID is valid.");
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
            if (imageDropdownInput is ComboBoxItem item)
            {
                string text = item.Tag.ToString();
                //AddConsoleLine($"[INFO]: Image type: {text}");

                if (text == "ResizeTrue")
                {
                    resizeImage = true;
                }
                else if (text == null || text == "")
                {
                    AddConsoleLine($"[ERROR]: ComboBox error! Send to devs.");
                    return;
                }
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

            // Export a texture to a file
            //renderSurface.SaveToImageFile("path/to/extracted_texture.png", Globals.DEW);

            // Get raw RGBA bytes
            //byte[] rgbaBytes = renderSurface.ToRgba8(Globals.DEW);


            //byte[] imageBytes = LoadImageBytes(imageInput);
            //textureTable.SourceData.Append(imageBytes);


            //textureTable.SourceData.Append(byte.Parse(imageInput));
        }

        private void AddImage(object sender, RoutedEventArgs e)
        {
            RenderSurface? textureTable = Globals.datFiles.Get<RenderSurface>(0x06000000u);

            string imageInput = ImgPath.Text.Trim();
            string imageIDInput = ImgID.Text;
            var imageDropdownInput = dropdownImageType.SelectedItem;
            if (uint.TryParse(imageIDInput, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint imageValue))
            {
                Console.WriteLine($"Image ID is valid.");
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

            //uint newImageID = "0x{imageIDInput}";
            if (string.IsNullOrWhiteSpace(imageInput))
            {
                AddConsoleLine("[ERROR]: Image path cannot be blank!");
                return;
            }

            AddConsoleLine($"[Info]: Working Properly! Path is {ImgPath.Text}");
            byte[] imageData = ConvertImageToPngBytes(ImgPath.Text);
            if (textureTable == null)
            {
                AddConsoleLine("TextureTable not found.");
                //return;
            }

            if (imageDropdownInput is ComboBoxItem item)
            {
                string text = item.Tag.ToString();
                AddConsoleLine($"[INFO]: Image type: {text}");

                if (text == "RGB")
                {
                    byte[] sourceData = ConvertBmpToPFID_R8G8B8(ImgPath.Text);
                    File.WriteAllBytes($"Image_{finalImageID:X8}_RGB.raw", sourceData); // Testing
                    

                    //File.WriteAllBytes($"Image_{finalImageID:X8}_RGB.bmp", imageData); // Testing
                }
                else if (text == "ARGB")
                {

                }
                else if (text == "JPG")
                {

                }
            }
            else
            {
                AddConsoleLine("[ERROR]: Image format error!");
                return;
            }
        }

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

        private void ImgPathInput_Changed(object sender, TextChangedEventArgs e)
        {
            ImagePathPlaceholder.Visibility =
                string.IsNullOrEmpty(ImgPath.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void ImgIDInput_Changed(object sender, TextChangedEventArgs e)
        {
            ImageIDPlaceholder.Visibility =
                string.IsNullOrEmpty(ImgID.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
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

        private void AddTitle(string title)
        {
            var datPath = ACPath.Text;
            var dats = new DatCollection(datPath, DatAccessType.ReadWrite);

            StringTable? stringTable = dats.Get<StringTable>(0x23000001u);

            if (stringTable == null)
            {
                AddConsoleLine("StringTable not found.");
                return;
            }

            stringTable.Strings.Add(0x1234u, new StringTableString()
            {
                Strings = ["foo", "bar"]
            });
            var newIteration = dats.Local.Iteration.CurrentIteration + 1;
            if (!dats.TryWriteFile(stringTable, newIteration)) throw new Exception($"Failed to write StringTable");

            // Dispose dat collection to flush any changes and close the files
            dats.Dispose();

        }

        static void GettingStarted(string[] args)
        {
            // var EORCommonData = @"C:\Turbine\Asheron's Call\";

            // Open a set of dat file for reading. This will open all the eor dat files as a single collection.
            // (client_portal.dat, client_cell_1.dat, client_local_English.dat, and client_highres.dat)
            //var datPath = @"C:\Turbine\Asheron's Call\client_cell_1.dat";
            var dats = new DatCollection(Globals.datFolder, DatAccessType.ReadWrite);

            // read files
            LayoutDesc? layoutDesc = dats.Get<LayoutDesc>(0x21000000u);

            // check iteration of portal dat
            Console.WriteLine($"Portal Iteration: {dats.Portal.Iteration.CurrentIteration}");

            // get ids of all Animations
            /*IEnumerable<uint> allAnimationIds = dats.GetAllIdsOfType<Animation>();

            // determine type from a file id
            var type = dats.Local.TypeFromId(0x21000000u);*/

            // write a file with a new iteration
            /*StringTable? stringTable = dats.Get<StringTable>(0x23000001u) ?? throw new Exception("StringTable not found");
            stringTable.StringTableData.Remove(0x1234u);
            stringTable.StringTableData.Add(0x1234u, new StringTableData()
            {
                Strings = ["foo", "bar"]
            });*/

            // Convert.ChangeType(2072, ToD);

            // dats.TypeToDatFileType<>();

            /*SkillTable? skillTable = dats.Get<SkillTable>(0xE000004u) ?? throw new Exception("SkillTable not found");
            skillTable.Skills.Add(0x0055, new SkillTable()
            {
                Skills = [dats, 10]
            });*/
            var newIteration = dats.Local.Iteration.CurrentIteration + 1;
            //if (!dats.TryWriteFile(stringTable, newIteration)) throw new Exception($"Failed to write StringTable");

            // Dispose dat collection to flush any changes and close the files

            dats.Dispose();

        }

        static void DatMapping(string[] args)
        {
            //var datPath = @"C:\Turbine\SS_Dats\client_local_English.dat";
            var dats = new DatCollection(Globals.datFolder, DatAccessType.ReadWrite);

            LayoutDesc? layoutDesc = dats.Get<LayoutDesc>(0x21000000u);

            IEnumerable<uint> allLanguageUiIds = dats.GetAllIdsOfType<LayoutDesc>();

            Console.WriteLine($"Language Iteration: {dats.Local.Iteration.CurrentIteration}");
            //Console.WriteLine($"Dumped Language: {allAnimationIds}");
            //Console.WriteLine(allLanguageUiIds);
            //Console.WriteLine(allAnimationIds);

            foreach (uint id in allLanguageUiIds)
            {
                Console.WriteLine($"0x{id:X8}");
            }
        }

        static void ExtractImages(string[] args)
        {
            //var datPath = @"C:\Turbine\SS_Dats\client_local_English.dat";
            var dats = new DatCollection(Globals.datFolder, DatAccessType.ReadWrite);

            LayoutDesc? layoutDesc = dats.Get<LayoutDesc>(0x21000000u);

            IEnumerable<uint> allLanguageUiIds = dats.GetAllIdsOfType<LayoutDesc>();

            Console.WriteLine($"Language Iteration: {dats.Local.Iteration.CurrentIteration}");
            //Console.WriteLine($"Dumped Language: {allAnimationIds}");
            //Console.WriteLine(allLanguageUiIds);
            //Console.WriteLine(allAnimationIds);

            foreach (uint id in allLanguageUiIds)
            {
                Console.WriteLine($"0x{id:X8}");
            }
        }
    }
}