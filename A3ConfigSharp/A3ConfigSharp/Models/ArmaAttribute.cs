using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace A3ConfigSharp
{
    public class ArmaAttribute
    {
        public int Type { get; set; } = 0;
        public double ScalarValue { get; set; } = 0;
        public string TextValue { get; set; } = "";
        public List<object> ArrayValue { get; set; } = new List<object>();

        public ArmaAttribute() { }
        public ArmaAttribute(int type, double scalarValue)
        {
            this.Type = type;
            this.ScalarValue = scalarValue;
        }
        public ArmaAttribute(int type, string TextValue)
        {
            this.Type = type;
            this.TextValue = TextValue;
        }
        public ArmaAttribute(int type, List<object> arrayValue)
        {
            this.Type = type;
            this.ArrayValue = arrayValue;
        }

        public string ToJSON()
        {
            StringBuilder stringBuilder = new StringBuilder();

            switch (this.Type)
            {
                case 0:
                    stringBuilder.Append($"{this.ScalarValue}");
                    break;
                case 1:
                    string value = this.TextValue;
                    if (!value.Equals("\"\""))
                    {
                        value = value.Substring(1, value.Count() - 2).Replace(@"\", @"\\").Replace(@"""""", "\\\"");
                        value = $"\"{value}\"";
                    }
                    stringBuilder.Append(value);
                    break;
                case 2:

                    if (this.ArrayValue.Count() == 0)
                    {
                        stringBuilder = new StringBuilder("[]");
                    }
                    else
                    {
                        stringBuilder.Append($"{Util.PrintList(ArrayValue)}");
                    }
                    break;
            }

            return stringBuilder.ToString();
        }
    }
}
