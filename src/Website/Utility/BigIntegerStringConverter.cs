// <copyright file="BigIntegerStringConverter.cs" company="Clicker Heroes Tracker">
// Copyright (c) Clicker Heroes Tracker. All rights reserved.
// </copyright>

namespace ClickerHeroesTrackerWebsite.Utility
{
    using System;
    using System.Globalization;
    using System.Numerics;
    using Newtonsoft.Json;

    public class BigIntegerStringConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(BigInteger));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var str = (string)reader.Value;
            if (str.Contains("e"))
            {
                return BigInteger.Parse(str, NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent);
            }
            else
            {
                // Parse as a double first in case there are rounding problems in the json.
                return new BigInteger(Math.Floor(double.Parse(str)));
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteRawValue(value.ToString());
        }
    }
}
