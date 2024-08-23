using System;
using System.Text;
using PluginInterface;

namespace ProcessExplorer.components.impl.innerSectionImpl
{
    public class ResourceString : SuperHeader
    {
        private readonly DataStorage dataStorage;
        public int StringLength { get; private set; }

        public ResourceString(uint startingPoint, string name, DataStorage dataStorage) : base(name, 2, 3)
        {
            this.dataStorage = dataStorage;
            StartPoint = startingPoint;

            Desc = new string[RowSize];
            Size = new int[RowSize];
            Size[0] = 2; Desc[0] = "String Length (2 bytes) length of the following string.";
            StringLength = Convert.ToInt32(GetData(0, 1, Enums.DataType.DECIMAL, 2, true, dataStorage)) * 2;
            Size[1] = StringLength; Desc[1] = $"UnicodeString ({StringLength} bytes) length requires us to multiple the above value by 2.";

            SetEndPoint();
        }

        public override void OpenForm(int row, DataStorage dataStorage)
        {
            return;
        }

        public string ReadUnicodeString()
        {
            string unicodeHex = GetData(1, 1, Enums.DataType.HEX, 2, true, dataStorage);
            unicodeHex = unicodeHex.Replace(" ", ""); // replace spaces

            StringBuilder result = new StringBuilder();

            for (int i = unicodeHex.Length - 4; i >= 0; i -= 4)
            {   // Process each pair of bytes (4 hex digits) in the hex string
                string hexValue = unicodeHex.Substring(i, 4); // Get 4 hex digits
                int value = Convert.ToInt32(hexValue, 16); // Convert hex to an integer
                char character = (char)value; // Cast to char
                result.Append(character); // Append character to the result
            }
            return result.ToString().Trim();
        }

        public void UpdateComponent(string name)
        {
            Component = Component + $" {name}";
        }
    }
}
