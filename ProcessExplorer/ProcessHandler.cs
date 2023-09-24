using System;
using System.Data;
using System.Linq;
using System.IO;
using ProcessExplorer.components;
using ProcessExplorer.components.impl;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using PluginInterface;

namespace ProcessExplorer
{
    class ProcessHandler
    {
        public string FilePath { get; private set; }

        public DataStorage dataStorage { get; private set; }

        public Enums.OffsetType Offset { get; set; }

        private int HeaderEndPoint { get; set; }

        // These following fields are all only initlized inside the constructor and thus marked 'readonly' 
        public readonly Dictionary<string, SuperHeader> componentMap = new Dictionary<string, SuperHeader>();
        private readonly FileStream file;

        private readonly BlockingCollection<Settings> settingsQueue = new BlockingCollection<Settings>();
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly Task writerTask;

        /* This prevents us from getting an error if the map does not contain a specific ProcessComponent */
        public SuperHeader GetComponentFromMap(string comp)
        {
            if (!componentMap.ContainsKey(comp)) return null;
            return componentMap[comp];
        }

        public ProcessHandler(FileStream file)
        {
            if (file == null) return;

            this.file = file;
            string fileName = new FileInfo(file.Name).Name;
            Offset = Enums.OffsetType.FILE_OFFSET;

            // This specifies that the array will be 16 across and (file.Length / 16) down
            // I am precomputing these values so that I dont have to recompute them when the user switches windows or modes
            string[,] filesHex = new string[(int)Math.Ceiling(file.Length / 16.0), 3];

            PopulateArrays(filesHex); // Passes a reference to the memory address of the above arrays for them to be populated
            // Create our main storage object that will be passed around to every plugin
            dataStorage = new DataStorage(HandleSettingsFileIO(), filesHex, true, fileName); 

            writerTask = Task.Run(() => FileWriterTaskMethod(), cancellationTokenSource.Token);

            CalculateHeaders(filesHex);
        }

        public void CalculateHeaders(string[,] filesHex)
        {
            // The following populates the dictionary for PE's
            componentMap.Add("everything", new Everything(dataStorage, filesHex.GetLength(0)));
            if (GetComponentFromMap("everything").EndPoint <= 2) return; // The file is esentially blank  

            componentMap.Add("dos header", new DosHeader(dataStorage));
            if (GetComponentFromMap("dos header").FailedToInitlize) return; // This means this is not a PE

            componentMap.Add("dos stub", new DosStub(this, dataStorage, GetComponentFromMap("dos header").EndPoint)); // This means our PE does not contain a dos stub which is not normal
            if (GetComponentFromMap("dos stub").FailedToInitlize) return;

            // Possible To:Do look into finding a PE that uses RichHeaders since those can sometimes appear before the PE Header
            PeHeader peHeader = new PeHeader(this, dataStorage, GetComponentFromMap("dos stub").EndPoint);
            componentMap.Add("pe header", peHeader);
            if (GetComponentFromMap("pe header").FailedToInitlize) return; // This means our PE Header is either too short or does not contain the signature

            componentMap.Add("optional pe header", new OptionalPeHeader(dataStorage, GetComponentFromMap("pe header").EndPoint));

            int endPoint;
            if (((OptionalPeHeader)GetComponentFromMap("optional pe header")).validHeader)
            { // This means we most likely have either 64 or 32 bit option headers
                if (((OptionalPeHeader)GetComponentFromMap("optional pe header")).peThirtyTwoPlus)
                {   // This means our optional header is valid and that we are using 64 bit headers
                    componentMap.Add("optional pe header 64", new OptionalPeHeader64(GetComponentFromMap("optional pe header").EndPoint));
                    OptionalPeHeaderDataDirectories dataDirectories = new OptionalPeHeaderDataDirectories(dataStorage, GetComponentFromMap("optional pe header 64").EndPoint);
                    componentMap.Add("optional pe header data directories", dataDirectories);

                    if (dataDirectories.CertificateTablePointer > 0 && dataDirectories.CertificateTableSize > 0)
                    {
                        componentMap.Add("certificate table", new CertificationTable(dataDirectories.CertificateTablePointer, dataDirectories.CertificateTableSize));
                    }
                }
                else
                {
                    componentMap.Add("optional pe header 32", new OptionalPeHeader32(GetComponentFromMap("optional pe header").EndPoint));
                    OptionalPeHeaderDataDirectories dataDirectories = new OptionalPeHeaderDataDirectories(dataStorage, GetComponentFromMap("optional pe header 32").EndPoint);
                    componentMap.Add("optional pe header data directories", dataDirectories);

                    if (dataDirectories.CertificateTablePointer > 0 && dataDirectories.CertificateTableSize > 0)
                    {
                        componentMap.Add("certificate table", new CertificationTable(dataDirectories.CertificateTablePointer, dataDirectories.CertificateTableSize));
                    }
                }
                endPoint = GetComponentFromMap("optional pe header data directories").EndPoint;
            }
            else endPoint = GetComponentFromMap("pe header").EndPoint;
            HeaderEndPoint = endPoint;

            // Recursively adds sections
            AssignSectionHeaders(endPoint, int.MaxValue, peHeader.SectionAmount, 0);
        }

        private void AssignSectionHeaders(int startPoint, int stoppingPoint, int sectionAmount, int sectionCount)
        {
            int initialSkipAmount = startPoint % 16; // The amount we need to skip before we reach our target byte 
            int startingIndex = startPoint <= 0 ? 0 : (int)Math.Floor(startPoint / 16.0);

            string ascii = ""; // Section name
            int headerNameStart = 0;
            int headerNameCount = 0;
            for (int row = startingIndex; row < dataStorage.FilesHex.GetLength(0); row++) // Loop through the rows
            {
                string[] hexBytes = dataStorage.FilesHex[row, 1].Split(' ');

                for (int j = initialSkipAmount; j < hexBytes.Length; j++) // Loop through each rows bytes
                {
                    if (byte.TryParse(hexBytes[j], System.Globalization.NumberStyles.HexNumber, null, out byte b))
                    {
                        char asciiChar = b > 32 && b <= 126 ? (char)b : ' '; // Space is normally 32 in decimal
                        int currentOffset = startPoint + ((row - startingIndex) * 16) + (initialSkipAmount > 0 ? 0 : j); // initialSkipAmount is already compensated by startPoint

                        if (currentOffset >= stoppingPoint) return; // This means a section body is about to start so we found all the headers

                        ascii += asciiChar;

                        if (headerNameStart == 0 && (asciiChar == '.' || asciiChar == '_'))
                        {   // This means we are possibly at the start of a new section header
                            headerNameStart = currentOffset;
                            ++headerNameCount;
                        }
                        else if(++headerNameCount < 8) // The Name field in section headers are 8 bytes long
                        {
                            if(headerNameCount < 4 && asciiChar == ' ')
                            {   // If the first few characters are not valid ASCII then we know its not a section header
                                headerNameStart = headerNameCount = 0;
                                ascii = "";
                            }
                        }
                        else
                        {   // This means we must be at the 8th byte which always should be null terminating if its a valid section header name
                            if(b == 00)
                            {
                                string udpatedAscii = ascii.Replace(" ", "").ToLower(); // Remove the null characters which were swapped to a space
                                string sectionType = udpatedAscii + " section header";
                                string sectionBodyType = udpatedAscii + " section body";
                                SectionHeader header = new SectionHeader(dataStorage, headerNameStart, sectionType);
                                SectionBody body = new SectionBody(header.bodyStartPoint, header.bodyEndPoint, sectionBodyType);

                                if (componentMap.ContainsKey(sectionType)) componentMap[sectionType] = header;
                                else componentMap.Add(sectionType, header);

                                if (componentMap.ContainsKey(sectionBodyType)) componentMap[sectionBodyType] = body;
                                else componentMap.Add(sectionBodyType, body);

                                // This sets the stopping point to the start of the nearest section body relative to the section header table
                                int stopPoint = Math.Min(body.StartPoint > 0 && body.EndPoint - body.StartPoint > 0 ? body.StartPoint : stoppingPoint, stoppingPoint);
                                
                                if(++sectionCount < sectionAmount) // This means we found all of the section headers
                                    AssignSectionHeaders(header.EndPoint, stopPoint, sectionAmount, sectionCount); 
                                return;
                            }
                            headerNameStart = headerNameCount = 0;
                            ascii = "";
                        }
                    }
                }
                initialSkipAmount = 0;
            }
            return;
        }

        private void PopulateArrays(string[,] filesHex)
        {
            try
            {
                byte[] buffer = new byte[16];
                int bytesRead;
                int down = 0;

                while ((bytesRead = file.Read(buffer, 0, buffer.Length)) > 0)
                {
                    string hex = BitConverter.ToString(buffer, 0, bytesRead).Replace("-", " ");
                    string[] hexBytes = hex.Split(' ');
                    string decimalNumbers = string.Join(" ", hexBytes.Select(hexByte => Convert.ToInt32(hexByte, 16).ToString()));
                    string binary = string.Join(" ", buffer.Take(bytesRead).Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));

                    // Now we need to generate the ASCII characters that will be added to our hex array 
                    string ascii = "";
                    foreach (byte b in buffer)
                    {
                        char asciiChar = b >= 32 && b <= 126 ? (char)b : '.';
                        ascii += asciiChar;
                    }

                    // filesHex only gets the ascii since it will be the same for the other two arrays
                    // so theres no point wasting memory on adding it.
                    filesHex[down, 0] = "0x" + (down * 16).ToString("X");
                    filesHex[down, 1] = hex;
                    filesHex[down, 2] = ascii;

                    down++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
        }

        public void PopulateArrays(string[] hex, int arrayLength)
        {
            string[,] filesHex = new string[arrayLength, 3];
            string[,] filesDecimal = new string[arrayLength, 2];

            int taken = 0;
            for(int i=0; i<arrayLength; i++)
            {
                int take = Math.Min(hex.Length - taken, 16);
                string[] newHexLine = hex.Skip(taken).Take(take).ToArray();
                int offset = i * 16;

                filesHex[i, 0] = "0x" + offset.ToString("X");
                filesHex[i, 1] = string.Join(" ", newHexLine);

                filesDecimal[i, 0] = offset.ToString();
                filesDecimal[i, 1] = string.Join(" ", newHexLine.Select(hexValue => Convert.ToInt32(hexValue, 16).ToString()).ToArray());

                string ascii = "";
                string hexString = filesHex[i, 1].Replace(" ", "");
                for (int j = 0; j < hexString.Length; j += 2)
                {
                    string hexChar = hexString.Substring(j, 2);
                    int charValue = Convert.ToInt32(hexChar, 16);

                    // Check if the character is within the printable ASCII range (32-126)
                    if (charValue >= 32 && charValue <= 126) ascii += (char)charValue;
                    else ascii += '.';
                }

                filesHex[i, 2] = ascii;
                taken += take;
            }

            dataStorage.UpdateArrays(filesHex);
        }

        /* Here index 0 is hex, index 1 is decimal, and index 2 is binary */
        public string[,] GetValueVariations(string newValue, bool hexChecked, bool decimalChecked)
        {
            string[,] values = new string[1, 3];
            string[] bytes = newValue.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (hexChecked)
            {
                bytes = newValue.Replace(" ", "") // Remove spaces
                    .Select((c, index) => new { Char = c, Index = index })
                    .GroupBy(x => x.Index / 2).Select(group => new string(group.Select(x => x.Char).ToArray())).ToArray();
               
                values[0, 0] = string.Join(" ", bytes); // Hex
                values[0, 1] = string.Join(" ", Array.ConvertAll(bytes, hex => long.Parse(hex, System.Globalization.NumberStyles.HexNumber))); // Decimal
                values[0, 2] = string.Join(" ", bytes.Select(hexByte => Convert.ToString(long.Parse(hexByte, System.Globalization.NumberStyles.HexNumber), 2).PadLeft(8, '0'))); // Binary
            }
            else if (decimalChecked)
            {
                var decimalNumbers = bytes.Select(number => long.Parse(number)).ToList();
                values[0, 0] = string.Join(" ", decimalNumbers.Select(decimalValue => decimalValue.ToString("X"))); // Hex
                values[0, 1] = newValue; // Decimal
                values[0, 2] = string.Join(" ", decimalNumbers.Select(decimalValue => Convert.ToString(decimalValue, 2).PadLeft(8, '0'))); // Binary
            }
            else
            {
                var binaryBytes = bytes.Select(binary => Convert.ToByte(binary, 2)).ToArray();
                values[0, 0] = BitConverter.ToString(binaryBytes).Replace("-", " "); // Hex
                values[0, 1] = string.Join(" ", binaryBytes.Select(byteValue => byteValue.ToString())); // Decimal
                values[0, 2] = newValue; // Binary
            }
            return values;
        }

        public int GetComponentsRowIndexCount(string component)
        {
            SuperHeader header = GetComponentFromMap(component);
            if (header == null) return 0;
            return header.RowSize;
        }

        public int GetComponentsColumnIndexCount(string component)
        {
            SuperHeader header = GetComponentFromMap(component);
            if (header == null) return 0;
            return header.RowSize;
        }


        /* Returns data that will fill up the DataDisplayView */
        public string GetValue(int row, int column, bool doubleByte, string component, Enums.DataType type)
        {
            SuperHeader header = GetComponentFromMap(component);
            if (header == null) return "";
            return header.GetData(row, column, type, doubleByte, Offset == Enums.OffsetType.FILE_OFFSET, dataStorage);
        }
         
        public void OpenDescrptionForm(string component, int row)
        {
            SuperHeader header = GetComponentFromMap(component);
            if(header != null) header.OpenForm(row, dataStorage);
        }

        public void SaveFile(string outputPath)
        {
            int length = GetComponentFromMap("everything").RowSize;
            string[] hexDataArray = new string[length];

            for (int row = 0; row < length; row++)
            {
                hexDataArray[row] = dataStorage.FilesHex[row, 1];
            }

            try
            {
                using (FileStream outputFileStream = new FileStream(outputPath, FileMode.Create))
                {
                    foreach (string hexRow in hexDataArray)
                    {
                        // Remove any spaces or other non-hex characters
                        string cleanedHexRow = hexRow.Replace(" ", "");

                        // Convert the cleaned hex string to bytes
                        byte[] bytes = new byte[cleanedHexRow.Length / 2];
                        for (int i = 0; i < bytes.Length; i++)
                        {
                            bytes[i] = Convert.ToByte(cleanedHexRow.Substring(i * 2, 2), 16);
                        }

                        // Write the bytes to the output file
                        outputFileStream.Write(bytes, 0, bytes.Length);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
        }


        private Settings CreateSettingsFile()
        {
            Settings settings = new Settings(true, true, false, true, true);
            string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(FilePath, json);
            return settings;
        }

        private Settings HandleSettingsFileIO()
        {
            FilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

            try
            {
                if (!File.Exists(FilePath)) return CreateSettingsFile();

                string settingsContents = File.ReadAllText(FilePath);
                if (settingsContents == null || settingsContents == "") 
                    return CreateSettingsFile();

                Settings deserializedObject = JsonConvert.DeserializeObject<Settings>(settingsContents);
                if (deserializedObject == null)
                    return CreateSettingsFile();

                return deserializedObject;
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
            return CreateSettingsFile();
        }

        /// <summary>
        ///  This should be called when deleting this ProcessHandler objects. Call this will stop and clean up resources
        ///   for the file writing thread.
        /// </summary>
        public void StopThread()
        {
            cancellationTokenSource.Cancel();
        }

        public void UpdateSettingsFile(bool async)
        {
            if(!async)
            {
                string json = JsonConvert.SerializeObject(dataStorage.Settings, Formatting.Indented);
                File.WriteAllText(FilePath, json);
                return;
            }

            // This means our task has been canceled (most likely opening a new file)
            if (writerTask == null || cancellationTokenSource.IsCancellationRequested) return;

            settingsQueue.Add(dataStorage.Settings);
        }

        /// <summary>
        ///  This should only be called from 'writerTask = Task.Run' inside of Processhandlers constructor. This is method is
        ///   designed to be ran on a seperate thread and uses a producer consumer scheme. 
        /// </summary>
        private void FileWriterTaskMethod()
        {
            try
            {
                foreach (var newSettings in settingsQueue.GetConsumingEnumerable(cancellationTokenSource.Token))
                {
                    // Check for cancellation
                    if (cancellationTokenSource.Token.IsCancellationRequested) return;

                    if (newSettings == null) continue;

                    string json = JsonConvert.SerializeObject(dataStorage.Settings, Formatting.Indented);
                    File.WriteAllText(FilePath, json);
                }
            }
            catch (OperationCanceledException)
            {
                
            }
        }

    }
}
