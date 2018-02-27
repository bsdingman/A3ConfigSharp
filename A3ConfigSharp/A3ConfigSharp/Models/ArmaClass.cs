using System.Collections.Generic;
using System.Text;

namespace A3ConfigSharp
{
    public class ArmaClass
    {
        public string InheritsFrom { get; set; } = "";
        public string ExtractedFrom { get; set; } = "";
        public Dictionary<string, ArmaAttribute> Attributes { get; set; } = new Dictionary<string, ArmaAttribute>();
        public Dictionary<string, ArmaClass> Children { get; set; } = new Dictionary<string, ArmaClass>();

        public void ToMySQL()
        {

        }

        public string ToJSON()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append("{\"inheritsFrom\": \"");
            stringBuilder.Append(this.InheritsFrom);
            stringBuilder.Append("\",\"attributes\": ");
            if (this.Attributes.Count == 0)
            {
                stringBuilder.Append("{},");
            }
            else
            {
                stringBuilder.Append("{");

                foreach (var attribute in this.Attributes)
                {
                    stringBuilder.Append($"\"{attribute.Key}\":");
                    stringBuilder.Append(attribute.Value.ToJSON());
                    stringBuilder.Append(",");
                }

                stringBuilder.Length--;
                stringBuilder.Append("},");
            }

            stringBuilder.Append("\"children\": ");

            if (this.Children.Count == 0)
            {
                stringBuilder.Append("{}");
            }
            else
            {
                stringBuilder.Append("{");

                foreach (var child in this.Children)
                {
                    stringBuilder.Append($"\"{child.Key}\":");
                    stringBuilder.Append(child.Value.ToJSON());
                    stringBuilder.Append(",");
                }

                stringBuilder.Length--;
                stringBuilder.Append("}");
            }

            stringBuilder.Append("}");

            return stringBuilder.ToString();
        }
    }
}
