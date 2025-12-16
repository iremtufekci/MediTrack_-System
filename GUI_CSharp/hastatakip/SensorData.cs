using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hastatakip
{
    public class SensorData
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string _id { get; set; }

        public string cardUID { get; set; }

        public string ad { get; set; } // "Yeni Hasta" değeri için

        public string notes { get; set; } // Boş string değeri için

        // readings array içindeki object için nested class
        public List<ReadingData> readings { get; set; }

        public double tempC { get; set; }
        public double ecg { get; set; }
        public double heartRate { get; set; }
        public double spo2 { get; set; }
        public double irValue { get; set; }
        public double redValue { get; set; }
        public double accel_x { get; set; }
        public double accel_y { get; set; }
        public double accel_z { get; set; }
        public double gyro_x { get; set; }
        public double gyro_y { get; set; }
        public double gyro_z { get; set; }
        public DateTime timestamp { get; set; }

        [BsonElement("lastUpdated")]
        public DateTime LastUpdated { get; set; }

        [BsonElement("__v")]
        public int Version { get; set; } // MongoDB versioning field

        // Computed properties
        public string FormattedTimestamp => timestamp.ToString("dd.MM.yyyy HH:mm");
        public double HeartRate => heartRate;
        public double Spo2 => spo2;
        public double TempC => tempC;
        public string Ecg => ecg.ToString();
        public DateTime Timestamp => timestamp;
    }

    // readings array içindeki object için ayrı class
    public class ReadingData
    {
        public double tempC { get; set; }
        public double ecg { get; set; }
        public double heartRate { get; set; }
        public double spo2 { get; set; }
        public double accel_x { get; set; }
        public double accel_y { get; set; }
        public double accel_z { get; set; }
        public double gyro_x { get; set; }
        public double gyro_y { get; set; }
        public double gyro_z { get; set; }
        public DateTime timestamp { get; set; }

        [BsonElement("_id")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
    }
}