using Hackney.Shared.HousingSearch.Domain.Transactions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.ComponentModel;

namespace HousingSearchListener.V1.Infrastructure.Converters
{
    public class StringToTransactionTypeConverter : StringEnumConverter
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (!objectType.IsEnum)
            {
                throw new ArgumentException($"From {nameof(StringToTransactionTypeConverter)}: provided type is not Enum!");
            }
            if (!objectType.Equals(typeof(TransactionType)))
            {
                throw new ArgumentException($"From {nameof(StringToTransactionTypeConverter)}: provided type is a TransactionType enum!");
            }

            var description = reader.Value.ToString();

            foreach (var field in objectType.GetFields())
            {
                if (field.Name?.ToLower() == description?.ToLower())
                {
                    return (TransactionType) field.GetValue(null);
                }

                if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attribute)
                {
                    if (attribute.Description?.ToLower() == description?.ToLower())
                    {
                        return (TransactionType) field.GetValue(null);
                    }
                }
            }

            throw new ArgumentException($"Transaction type with value {description} can not be found.", nameof(description));
        }
    }
}
