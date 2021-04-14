namespace RedisTimeSeriesEdge
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    ///Body:
    ///{
    ///  “machine”:{
    ///    “temperature”:,
    ///    “pressure”:
    ///  },
    ///  “ambient”:{
    ///    “temperature”: ,
    ///    “humidity”:
    ///  }
    ///  “timeCreated”:”UTC iso format”
    ///}
    ///Units and types:
    ///Temperature: double, C
    ///Humidity: int, %
    ///Pressure: double, psi
    /// </summary>
    class MessageBody
    {
        [JsonProperty(PropertyName = "machine")]
        public Machine Machine { get; set; }

        [JsonProperty(PropertyName = "ambient")]
        public Ambient Ambient { get; set; }

        [JsonProperty(PropertyName = "timeCreated")]
        public DateTimeOffset TimeCreated { get; set; }
    }

    class Machine
    {
        [JsonProperty(PropertyName = "temperature")]
        public double Temperature { get; set; }

        [JsonProperty(PropertyName = "pressure")]
        public double Pressure { get; set; }
    }

    class Ambient
    {
        [JsonProperty(PropertyName = "temperature")]
        public double Temperature { get; set; }

        [JsonProperty(PropertyName = "humidity")]
        public int Humidity { get; set; }
    }
}
