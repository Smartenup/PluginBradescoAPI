using Newtonsoft.Json;
using System;
using System.Text.RegularExpressions;

namespace Nop.Plugin.Payments.BoletoBradescoAPI.Serializer
{
    public class DecimalConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(decimal) || objectType == typeof(decimal?));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                if (String.IsNullOrEmpty(reader.Value.ToString()))
                {
                    return null;
                }
                else
                {
                    int valorInteiro = Convert.ToInt32(reader.Value.ToString());

                    decimal valorDecimal = (decimal)valorInteiro / (decimal)100;

                    return valorDecimal;
                }
            }
            else if (reader.TokenType == JsonToken.Float ||
                     reader.TokenType == JsonToken.Integer)
            {
                return Convert.ToDecimal(reader.Value);
            }

            throw new JsonSerializationException("Unexpected token type: " +
                                                 reader.TokenType.ToString());
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            decimal dec = (decimal)value;

            if (dec == decimal.MinValue)
            {
                writer.WriteValue(string.Empty);
            }
            else
            {
                var regx = new Regex("[^0-9]");

                string specifier = "N2";

                string decString = regx.Replace(dec.ToString(specifier), string.Empty);

                writer.WriteValue(decString);
            }
        }
    }
}
