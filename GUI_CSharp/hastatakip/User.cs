using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace hastatakip
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string _id { get; set; }




        public string ad { get; set; }
        public string soyad { get; set; }
        public string tc { get; set; }
        public string eposta { get; set; }
        public string sifre { get; set; }
        public string cinsiyet { get; set; }

        public string cardUID { get; set; }
     
    }

}
