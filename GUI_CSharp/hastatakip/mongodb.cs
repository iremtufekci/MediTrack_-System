using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace hastatakip
{
 
    public class MongoDBService
    {
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<User> _usersCollection;
        private readonly IMongoCollection<SensorData> _sensordatasCollection;

        public object usersCollection { get; internal set; }

        public MongoDBService(string connectionString = "mongodb+srv://iremhastatakip:Samsun.2955@hastatakip.nus8ex1.mongodb.net/?retryWrites=true&w=majority&appName=hastatakip", string databaseName = "test")
        {
            try
            {
                var client = new MongoClient(connectionString);
                _database = client.GetDatabase(databaseName);

                // Bağlantının başarılı olduğunu doğrulamak için ping komutu
                _database.RunCommand<BsonDocument>(new BsonDocument("ping", 1));

                _usersCollection = _database.GetCollection<User>("users");
                // Eğer sensör verisi collection'ı yoksa, bu satırı kaldırabilirsiniz.
                _sensordatasCollection = _database.GetCollection<SensorData>("sensordatas");

                Console.WriteLine("MongoDB Atlas bağlantısı başarılı! 🎉");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MongoDB Atlas bağlantı hatası: {ex.Message}");
                throw; // Uygulamanın başlangıcında yakalanması için hatayı tekrar fırlat
            }
        }

        
        // ---------KULLANICI KAYIT VE GİRİŞ İŞLEMLERİ-----------------


        //Kullanıcı giriş yapma metodu
        public async Task<User> KullaniciGirisAsync(string tc, string eposta, string sifre)
        {
            try
            {
                var filter = Builders<User>.Filter.And(
                    Builders<User>.Filter.Eq(u => u.tc, tc),
                    Builders<User>.Filter.Eq(u => u.eposta, eposta),
                    Builders<User>.Filter.Eq(u => u.sifre, sifre)
                );

                var kullanici = await _usersCollection.Find(filter).FirstOrDefaultAsync();

                if (kullanici == null)
                {
                    throw new UnauthorizedAccessException("Kullanıcı adı veya şifre hatalı.");
                }

                return kullanici;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Kullanıcı giriş hatası: {ex.Message}");
                throw;
            }
        }
       



        //Kullanıcı kaydolma metodu
        public async Task KullaniciKaydetAsync(string ad, string soyad, string tc, string eposta, string sifre, string cinsiyet)
        {
            try
            {
                // Veri doğrulama
                if (string.IsNullOrWhiteSpace(ad) || !Regex.IsMatch(ad, @"^[a-zA-ZçÇğĞıİöÖşŞüÜ\s]+$"))
                {
                    throw new ArgumentException("Ad sadece harf içermelidir.");
                }
                if (string.IsNullOrWhiteSpace(soyad) || !Regex.IsMatch(soyad, @"^[a-zA-ZçÇğĞıİöÖşŞüÜ\s]+$"))
                {
                    throw new ArgumentException("Soyad sadece harf içermelidir.");
                }
                if (string.IsNullOrWhiteSpace(tc) || !Regex.IsMatch(tc, @"^\d{11}$"))
                {
                    throw new ArgumentException("TC Kimlik Numarası 11 haneli bir sayı olmalıdır.");
                }
                string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
                if (string.IsNullOrWhiteSpace(eposta) || !Regex.IsMatch(eposta, emailPattern))
                {
                    throw new ArgumentException("Geçerli bir e-posta adresi giriniz.");
                }
                string sifreRegex = @"^(?=.*[A-Z])(?=.*\d).{6,}$";
                if (string.IsNullOrWhiteSpace(sifre) || !Regex.IsMatch(sifre, sifreRegex))
                {
                    // Güncellenmiş hata mesajı
                    throw new ArgumentException("Şifre en az 6 haneli olmalı, en az bir büyük harf ve bir rakam içermelidir.");
                }
                var filtre = Builders<User>.Filter.Or(
            Builders<User>.Filter.Eq(u => u.tc, tc),
            Builders<User>.Filter.Eq(u => u.eposta, eposta)
        );

                var mevcutKullanici = await _usersCollection.Find(filtre).FirstOrDefaultAsync();

                if (mevcutKullanici != null)
                {
                    string hataMesaji = "";
                    if (mevcutKullanici.tc == tc)
                    {
                        hataMesaji = "Bu TC Kimlik Numarası zaten kayıtlıdır. Lütfen kontrol ediniz.";
                    }
                    else if (mevcutKullanici.eposta == eposta)
                    {
                        hataMesaji = "Bu e-posta adresi zaten kayıtlıdır. Lütfen kontrol ediniz.";
                    }

                    // ArgumentException fırlatmak, butondaki catch bloğunun bu hatayı kullanıcıya göstermesini sağlar.
                    throw new ArgumentException(hataMesaji);
                }
                var yeniKullanici = new User
                {
                    ad = ad,
                    soyad = soyad,
                    tc = tc,
                    eposta = eposta,
                    sifre = sifre, // Güvenlik için hashlenmelidir
                    cinsiyet = cinsiyet
                };

                await _usersCollection.InsertOneAsync(yeniKullanici);
                Console.WriteLine($"Kullanıcı {ad} {soyad} başarıyla kaydedildi!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Kullanıcı kaydetme hatası: {ex.Message}");
                throw;
            }
        }




        //Anasayfa için son verileri getirme kodu
        public async Task<SensorData> GetLastSensorDataByCardUID(string cardUID)
        {
            try
            {
                // cardUID ile filtreleme (MongoDB'deki field adıyla eşleşmeli)
                var sensorFilter = Builders<SensorData>.Filter.Eq("cardUID", cardUID);

                // lastUpdated alanına göre sıralama (çünkü timestamp readings array'inde)
                var lastSensorData = await _sensordatasCollection
                    .Find(sensorFilter)
                    .SortByDescending(x => x.LastUpdated) // LastUpdated kullan
                    .FirstOrDefaultAsync();

                return lastSensorData;
            }
            catch (Exception ex)
            {
                throw new Exception("Son sensör verisi çekilirken hata oluştu: " + ex.Message);
            }
        }




        //istatiksel analiz sayfası için datagridlere verileri çekme metodu

        public async Task<List<SensorData>> GetAllSensorDataByCardUID(string cardUID)
        {
            try
            {
                var sensorFilter = Builders<SensorData>.Filter.Eq("cardUID", cardUID);
                var allSensorData = await _sensordatasCollection
                    .Find(sensorFilter)
                    .SortByDescending(x => x.LastUpdated) // En yeni belgeler üstte olsun
                    .ToListAsync();
                return allSensorData;
            }
            catch (Exception ex)
            {
                throw new Exception("Tüm sensör verileri çekilirken hata oluştu: " + ex.Message);
            }
        }







        //İstatiksel analiz sayfsındaki filtreleme butonu için
        public async Task<List<SensorData>> GetSensorDataByDateRange(string cardUID, DateTime startDate, DateTime endDate)
        {
            try
            {
                // Filtre: Hem cardUID'e göre hem de tarih aralığına göre
                var filter = Builders<SensorData>.Filter.And(
                    Builders<SensorData>.Filter.Eq(s => s.cardUID, cardUID),
                    Builders<SensorData>.Filter.Gte("readings.timestamp", startDate),
                    Builders<SensorData>.Filter.Lte("readings.timestamp", endDate)
                );

                var sensorDataList = await _sensordatasCollection
                    .Find(filter)
                    .SortByDescending(x => x.LastUpdated) // en güncel olan en üstte
                    .ToListAsync();

                return sensorDataList;
            }
            catch (Exception ex)
            {
                throw new Exception("Tarih aralığına göre veriler çekilirken hata oluştu: " + ex.Message);
            }
        }


        





        // --------------KART UID YÖNETİMİ METOTLARI----------------


        public async Task<User> GetUserByIdAsync(string id)
        {
            try
            {
                // ObjectId'nin string olarak temsil edildiğini varsayıyoruz.
                // Doğrudan string olarak karşılaştırma yapabiliriz.
                var filter = Builders<User>.Filter.Eq(u => u._id, id);

                return await _usersCollection.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Kullanıcı ID ile arama hatası: {ex.Message}");
                return null;
            }
        }



        public async Task<User> KartUIDIleKullaniciBulAsync(string cardUID)
        {
            try
            {
                var filter = Builders<User>.Filter.Eq(u => u.cardUID, cardUID);
                var kullanici = await _usersCollection.Find(filter).FirstOrDefaultAsync();
                return kullanici;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Kart UID kontrolü hatası: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Belirtilen kullanıcıya kart UID'si atar.
        /// Eğer bu kart UID'si başka bir kullanıcıya aitse, önce o kullanıcının kartUID'sini siler.
        /// </summary>
        /// <param name="kullaniciId">Güncellenecek kullanıcının ID'si.</param>
        /// <param name="kartUID">Atanacak kart UID'si.</param>
        /// <returns>İşlem başarılıysa true, aksi halde false.</returns>
        public async Task<bool> KartUIDIleKullaniciEslestirAsync(string kullaniciId, string cardUID)
        {
            try
            {
                if (_database == null)
                {
                    throw new InvalidOperationException("MongoDB bağlantısı kurulmamış.");
                }

                // Önce bu kart UID'sinin başka bir kullanıcıda olup olmadığını kontrol et
                var mevcutKartSahibi = await _usersCollection.Find(u => u.cardUID == cardUID).FirstOrDefaultAsync();

                if (mevcutKartSahibi != null && mevcutKartSahibi._id != kullaniciId)
                {
                    // Bu kart başka bir kullanıcıya ait, önce onu temizle
                    var temizlemeFilter = Builders<User>.Filter.Eq(u => u._id, mevcutKartSahibi._id);
                    var temizlemeUpdate = Builders<User>.Update.Unset(u => u.cardUID);
                    await _usersCollection.UpdateOneAsync(temizlemeFilter, temizlemeUpdate);
                }

                // Şimdi yeni kullanıcıya kart UID'sini ata
                var filter = Builders<User>.Filter.Eq(u => u._id, kullaniciId);
                var update = Builders<User>.Update.Set(u => u.cardUID, cardUID);

                var result = await _usersCollection.UpdateOneAsync(filter, update);

                return result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"KartUIDIleKullaniciEslestirAsync hatası: {ex.Message}");
                return false;
            }
        }

        // ===== SENSÖR VERİSİ METOTLARI =====

        /// <summary>
        /// Bir kart UID'sine bağlı en son sensör verilerini asenkron olarak getirir.
        /// </summary>
        /// <param name="kartUID">Sensör verileri getirilecek kartın UID'si.</param>
        /// <param name="limit">Getirilecek kayıt sayısı.</param>
        /// <returns>Sensör verilerinin listesi.</returns>
        public async Task<List<SensorData>> GetSensorDataByCardUIDAsync(string cardUID, int limit = 10)
        {
            try
            {
                var user = await KartUIDIleKullaniciBulAsync(cardUID);
                if (user == null)
                {
                    Console.WriteLine($"Kart UID bulunamadı: {cardUID}");
                    return new List<SensorData>();
                }

                var filter = Builders<SensorData>.Filter.Eq(s => s.cardUID, cardUID);
                var sort = Builders<SensorData>.Sort.Descending(s => s.timestamp);

                var sensorDataList = await _sensordatasCollection
                    .Find(filter)
                    .Sort(sort)
                    .Limit(limit)
                    .ToListAsync();

                Console.WriteLine($"Bulunan sensör verisi sayısı: {sensorDataList.Count}");
                return sensorDataList;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Sensör verilerini getirme hatası: {ex.Message}");
                throw;
            }
        }

      
       
    }
}