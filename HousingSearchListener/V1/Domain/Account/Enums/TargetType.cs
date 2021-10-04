﻿using System.Text.Json.Serialization;

namespace HousingSearchListener.V1.Domain.Account.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TargetType
    {
        Housing,
        Garage
    }
}
