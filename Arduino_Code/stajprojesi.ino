#include <WiFi.h>
#include <HTTPClient.h>
#include <ArduinoJson.h>
#include <Wire.h>
#include <MPU6050.h>
#include <OneWire.h>//Tek hat haberleşme spı
#include <DallasTemperature.h>//Sıcaklık
#include "MAX30105.h"
#include "spo2_algorithm.h"
#include <SPI.h>
#include <MFRC522.h>
#include <time.h>

// ---------------- AYARLAR ----------------
const char* ssid = "Galaxy S21 FE 5G b1a1";
const char* password = "malsudee";

// Bilgisayarının IP adresi
const char* serverApiUrl = "http://192.168.83.74:3000/data";
const char* cardRegistrationUrl = "http://192.168.83.74:3000/register-card";

// NTP Zaman Ayarları
const char* ntpServer = "pool.ntp.org";
const long  gmtOffset_sec = 3 * 3600;   // Türkiye UTC+3
const int   daylightOffset_sec = 0;

// ---------------- PIN TANIMLAMALARI ----------------
#define RST_PIN 4
#define rfidSDA_PIN 17
#define SCL_PIN 22
#define SDA_PIN 21
#define BUZZER_PIN 33
#define ONE_WIRE 15
#define AD8232_PIN 34

// ---------------- SENSÖR NESNELERİ ----------------
MFRC522 mfrc522(rfidSDA_PIN, RST_PIN);
OneWire oneWire(ONE_WIRE);
DallasTemperature sensors(&oneWire);
MPU6050 mpu;
MAX30105 particleSensor;

// ---------------- DEĞİŞKENLER ----------------
// Kalp Atışı ve SpO2
int beatAvg = 0;
int spo2 = 0;

// MPU (İvme ve Jiroskop)
int16_t ax, ay, az, gx, gy, gz;
unsigned long lastMpuUpdate = 0;//Mpu son güncellenme
const long mpuUpdateInterval = 1000; //MPU Güncelleme Aralığı

// Sistem Durumu
bool mpuActive = false; // Kullanıcı giriş yaptı mı?
String currentCardUID = "";

// ---------------- FONKSİYON ----------------
void connectToWiFi();
unsigned long getUnixTimestamp(); //O anki saati alma
void initializeMPU6050();
void initializeDS18B20();
void initializeMAX30102();
void initializeRFID();
void readRFID();
void sendCardRegistration();
void readMAX30102ForSingleReading();
void readAllSensorsAndSend();
void sendSensorDataToServer();


// ================= SETUP =================
void setup() {
  Serial.begin(9600);
  
  // I2C Başlatma
  Wire.begin(SDA_PIN, SCL_PIN);
  
  // Wi-Fi ve Zaman Başlatma
  connectToWiFi();
  
  // Sensörleri Başlat (MPU hariç)
  initializeDS18B20();
  initializeMAX30102();
  initializeRFID();
  
  // Buzzer Ayarı
  pinMode(BUZZER_PIN, OUTPUT);
  digitalWrite(BUZZER_PIN, LOW);
  
  Serial.println("-----------------------------------------");
  Serial.println("SİSTEM HAZIR. Lütfen kartınızı okutun...");
  Serial.println("-----------------------------------------");
}

// ================= LOOP =================
void loop() {
  //RFID KONTROLÜ
  if (mfrc522.PICC_IsNewCardPresent() && mfrc522.PICC_ReadCardSerial()) {//Yeni bir kart var mı ve veri çekebilir mi
    readRFID(); // UID'yi alır
    mfrc522.PICC_HaltA();
    
    // Eğer kart geçerliyse ve sistem kapalıysa başlat
    if (!mpuActive && currentCardUID != "") {
      initializeMPU6050();
      mpuActive = true; 
      Serial.println("✅ GİRİŞ BAŞARILI! Sensörler aktif...");
    }
    
    // Kart okutulduğunda bir kerelik tüm sensörleri oku ve gönder
    if (currentCardUID != "") {
      readAllSensorsAndSend();
    }
  }

  //MPU KONTROLÜ (Aktifse sürekli okuma yapar, ama göndermez
  if (mpuActive && millis() - lastMpuUpdate >= mpuUpdateInterval) {
    readMPU6050();
    lastMpuUpdate = millis();
  }
}

// ================= YARDIMCI FONKSİYONLAR =================

//GERÇEK ZAMANI ALMA 
unsigned long getUnixTimestamp() {
  time_t now;  //Zamanı saklayacak değişken
  struct tm timeinfo;//Anlaşılır şekile çevirme
  if (!getLocalTime(&timeinfo)) {//Esp32 den zamanı çekip çekmediği
    Serial.println("⚠️ Zaman alınamadı! (0 dönüyor)");
    return 0; 
  }
  time(&now);
  return now; // 1970'ten bugüne geçen saniye
}

//WI-FI BAĞLANTISI VE ZAMAN SENKRONİZASYONU
void connectToWiFi() {
  Serial.println("\nWi-Fi ağına bağlanılıyor...");
  WiFi.begin(ssid, password);
  
  int attempts = 0;
  // 30 saniye boyunca bağlanmayı dene
  while (WiFi.status() != WL_CONNECTED && attempts < 30) {
    delay(1000);
    Serial.print(".");
    attempts++;
  }
  
  if (WiFi.status() == WL_CONNECTED) {
    Serial.println("\n✅ Wi-Fi bağlantısı BAŞARILI!");
    Serial.print("IP Adresi: ");
    Serial.println(WiFi.localIP());

    // --- ZAMAN AYARLARI ---
    Serial.println("NTP sunucusundan zaman bekleniyor...");
    configTime(gmtOffset_sec, daylightOffset_sec, ntpServer);//Pool.ntp.org
    
    struct tm timeinfo;//Anlayacağımız şekile çeviren
    int retry = 0;//Sayaç
    // Zamanı alana kadar bekle
    while(!getLocalTime(&timeinfo) && retry < 20){
      Serial.print(".");
      delay(500);
      retry++;
    }
    
    if(retry >= 20){
        Serial.println("\n❌ Zaman senkronizasyonu BAŞARISIZ! (Timestamp hatalı olabilir)");
    } else {
        Serial.println("\n✅ Zaman SENKRONİZE EDİLDİ!");
        Serial.println(&timeinfo, "%A, %B %d %Y %H:%M:%S");//gün ismi,ay,gün sayı,yıl,saat dakika
    }
    // ----------------------

  } else {
    Serial.println("\n❌ Wi-Fi bağlantısı BAŞARISIZ!");
  }
}

// MPU OKUMA
void readMPU6050() {
  mpu.getAcceleration(&ax, &ay, &az);
  mpu.getRotation(&gx, &gy, &gz);
  // Sadece ekrana yazdırır
  Serial.print("MPU: "); Serial.print(ax); Serial.print(", "); Serial.print(ay); Serial.print(", "); Serial.println(az);
}

// SENSÖRLERİ BAŞLATMA
void initializeMPU6050() {
  Serial.println("MPU6050 başlatılıyor...");
  mpu.initialize();
  if (mpu.testConnection()) Serial.println("MPU6050 Başarılı!");
  else { Serial.println("MPU6050 Başarısız!"); mpuActive = false; }
}

void initializeDS18B20() { sensors.begin(); }

void initializeMAX30102() {
  if (!particleSensor.begin(Wire, I2C_SPEED_FAST)) {//ı2c başlatma
    Serial.println("MAX30102 bulunamadı!");
    while (1);
  }
  particleSensor.setup(); //Başlatma
  particleSensor.setPulseAmplitudeRed(0x1F);//Kırmızı led parlaklığı
  particleSensor.setPulseAmplitudeIR(0x1F);//Kızılötesi led
  particleSensor.setPulseAmplitudeGreen(0);//Yeşil led kapalı
}

void initializeRFID() {
  SPI.begin();
  mfrc522.PCD_Init();//RFID ayağa kaldırma ve sıfırlama
  Serial.println("RFID Hazır");
}

void readRFID() {
  currentCardUID = "";
  for (byte i = 0; i < mfrc522.uid.size; i++) {
    if (mfrc522.uid.uidByte[i] < 0x10) currentCardUID += "0";
    currentCardUID += String(mfrc522.uid.uidByte[i], HEX);
  }
  Serial.print("CARD_UID:"); Serial.println(currentCardUID);
  
  sendCardRegistration(); 
  
  tone(BUZZER_PIN, 2000); delay(500); noTone(BUZZER_PIN);
}

// MAX30102 UZUN OKUMA
void readMAX30102ForSingleReading() {
  const int bufferLength = 100; //100 tane veri alacak
  uint32_t irBuffer[bufferLength];//Kızılötesinden gelecek veri dizisi
  uint32_t redBuffer[bufferLength];
  int32_t spo2Value; int8_t validSPO2;      
  int32_t heartRateValue; int8_t validHeartRate;  

  Serial.println(">> PARMAK SENSÖRÜ: Lütfen parmağınızı yerleştirin...");

  unsigned long startWait = millis();//Değer 50000 den küçük ise parmak yok
  while (particleSensor.getIR() < 50000 && millis() - startWait < 5000) {
    delay(100);
  }

  if (particleSensor.getIR() < 50000) {//5 saniye sonunda hala yoksa parmak algılanmadı yazsın
    Serial.println(">> Parmak algılanamadı!");
    beatAvg = 0; spo2 = 0;
    return;
  }

  Serial.println(">> Ölçüm yapılıyor...");
  for (int i = 0; i < bufferLength; i++) {//!00 defa kaydeder 
    while (!particleSensor.check()) ; 
    redBuffer[i] = particleSensor.getRed();
    irBuffer[i] = particleSensor.getIR();//kan akışını
    particleSensor.nextSample();
  }

  maxim_heart_rate_and_oxygen_saturation(irBuffer, bufferLength, redBuffer, &spo2Value, &validSPO2, &heartRateValue, &validHeartRate);

  if (validHeartRate) beatAvg = heartRateValue; else beatAvg = 0;
  if (validSPO2) spo2 = spo2Value; else spo2 = 0;

  Serial.print("Nabız: "); Serial.print(beatAvg); Serial.print(" SpO2: "); Serial.println(spo2);
}

// KART KAYDI GÖNDERME
void sendCardRegistration() {
  if (WiFi.status() != WL_CONNECTED) return;
  
  StaticJsonDocument<128> doc;//JSON formatı
  doc["cardUID"] = currentCardUID;
  // DÜZELTME: millis() yerine gerçek zaman fonksiyonu
  doc["timestamp"] = getUnixTimestamp(); 
  
  String jsonOutput; //JSON ın son halini tutacak container
  serializeJson(doc, jsonOutput);//Verilerin geleceği hal
  
  HTTPClient http;//Sanal nesne endpoint
  http.begin(cardRegistrationUrl);//Nereye gideceği
  http.addHeader("Content-Type", "application/json");//JSON verisi gönderiyorum
  int code = http.POST(jsonOutput);//POST işlemi yapar
  Serial.print("Kart Kaydı Yanıt: "); Serial.println(code);
  http.end();
}

// TÜM SENSÖRLERİ OKU VE GÖNDER
void readAllSensorsAndSend() {
  sensors.requestTemperatures();//SIcaklık okuma
  float tempC = sensors.getTempCByIndex(0);
  if (tempC == DEVICE_DISCONNECTED_C) tempC = 0.0;//Sıcaklık hata kontrolü
  
  int ecg = analogRead(AD8232_PIN);//EKG okuma
  
  readMAX30102ForSingleReading(); //Nabız,SPO2 okuma
  
  sendSensorDataToServer(tempC, ecg);//Verileri internete atma
}

// VERİ GÖNDERME
void sendSensorDataToServer(float temp, int ecg) {
  if (WiFi.status() != WL_CONNECTED || currentCardUID == "") return;//WİFİ kopmuşsa bitirir
  
  StaticJsonDocument<512> doc;//512 bytelık hafıza alanı ayırır
  doc["cardUID"] = currentCardUID;
  doc["tempC"] = temp;
  doc["ecg"] = ecg;
  doc["heartRate"] = beatAvg;
  doc["spo2"] = spo2;
  doc["accel_x"] = ax; doc["accel_y"] = ay; doc["accel_z"] = az;
  doc["gyro_x"] = gx; doc["gyro_y"] = gy; doc["gyro_z"] = gz;
  
 //Zaman fonksiyonu
  doc["timestamp"] = getUnixTimestamp();
  
  String jsonOutput;
  serializeJson(doc, jsonOutput);//Metine çevirip gönderme
  
  Serial.println("Veri gönderiliyor...");
  HTTPClient http;
  http.begin(serverApiUrl);//Bağlantı portunu açar
  http.addHeader("Content-Type", "application/json");
  int code = http.POST(jsonOutput);
  Serial.print("HTTP Yanıt: "); Serial.println(code);
  http.end();//Kapat
}