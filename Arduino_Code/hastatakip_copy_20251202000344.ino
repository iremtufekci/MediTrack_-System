#include <WiFi.h>
#include <HTTPClient.h>
#include <ArduinoJson.h>
#include <Wire.h>
#include <MPU6050.h>
#include <OneWire.h>
#include <DallasTemperature.h>
#include "MAX30105.h"
#include "heartRate.h"
#include <SPI.h>
#include <MFRC522.h>
#include <time.h>

const char* ntpServer = "pool.ntp.org";
const long  gmtOffset_sec = 3 * 3600;   // Türkiye UTC+3
const int   daylightOffset_sec = 0;


// Wi-Fi Ayarları
const char* ssid = "Galaxy S21 FE 5G b1a1";
const char* password = "malsudee";
const char* serverApiUrl = "http://192.168.132.74:3000/data";
const char* cardRegistrationUrl = "http://192.168.132.74:3000/register-card"; // Masaüstü için

// Pin Tanımlamaları
#define RST_PIN 4
#define rfidSDA_PIN 17
#define SCL_PIN 22
#define SDA_PIN 21
#define BUZZER_PIN 33
#define ONE_WIRE 15
#define AD8232_PIN 34

// Sensör Nesneleri
MFRC522 mfrc522(rfidSDA_PIN, RST_PIN);
OneWire oneWire(ONE_WIRE);
DallasTemperature sensors(&oneWire);
MPU6050 mpu;
MAX30105 particleSensor;

// Kalp Atışı Değişkenleri
const byte RATE_SIZE = 8;
byte rates[RATE_SIZE];
byte rateSpot = 0;
long lastBeat = 0;
float beatsPerMinute;
int beatAvg;
int spo2;

// MPU Değişkenleri
int16_t ax, ay, az, gx, gy, gz;
unsigned long lastMpuUpdate = 0;
const long mpuUpdateInterval = 1000; // MPU verilerini saniyede bir güncelle
bool mpuActive = false; // MPU aktif mi kontrolü

// RFID Değişkenleri
String currentCardUID = "";



void setup() {
  Serial.begin(9600);
  
  // I2C Başlatma
  Wire.begin(SDA_PIN, SCL_PIN);
  
  // Wi-Fi Bağlantısı
  connectToWiFi();
  
  Serial.print("TEST Unix Timestamp: ");
  Serial.println(getUnixTimestamp());
  // Sensör Başlatmaları (MPU hariç)
  initializeDS18B20();
  initializeMAX30102();
  initializeRFID();
  
  // Buzzer Ayarları
  pinMode(BUZZER_PIN, OUTPUT);
  digitalWrite(BUZZER_PIN, LOW);
  
  Serial.println("Sistem hazır. Lütfen kartınızı okutun...");
}

void loop() {
  // RFID kart kontrolü (sürekli)
  if (mfrc522.PICC_IsNewCardPresent() && mfrc522.PICC_ReadCardSerial()) {
    readRFID();
    mfrc522.PICC_HaltA();
    
    // İlk kez kart okutulduysa MPU'yu başlat
    if (!mpuActive && currentCardUID != "") {
      initializeMPU6050();
      mpuActive = true;
      Serial.println("MPU6050 başlatıldı ve saniyede bir ölçüm yapacak!");
    }
    
    // Kart okutulduğunda tüm sensör verilerini oku ve gönder
    if (currentCardUID != "") {
      readAllSensorsAndSend();
    }
  }

  // MPU aktifse saniyede bir ölçüm yap (ama veri gönderme)
  if (mpuActive && millis() - lastMpuUpdate >= mpuUpdateInterval) {
    readMPU6050();
    lastMpuUpdate = millis();
  }
}

// Wi-Fi Bağlantı Fonksiyonu
void connectToWiFi() {
  Serial.println("\nWi-Fi ağına bağlanılıyor...");
  WiFi.begin(ssid, password);
  
  int attempts = 0;
  while (WiFi.status() != WL_CONNECTED && attempts < 20) {
    delay(1000);
    Serial.print(".");
    attempts++;
  }
  
  if (WiFi.status() == WL_CONNECTED) {
    Serial.println("\nWi-Fi bağlantısı başarılı!");
    Serial.print("IP Adresi: ");
    Serial.println(WiFi.localIP());
  } else {
    Serial.println("\nWi-Fi bağlantısı başarısız!");
  }
   configTime(gmtOffset_sec, daylightOffset_sec, ntpServer);

Serial.println("NTP zamanı bekleniyor...");

struct tm timeinfo;
int retry = 0;
while (!getLocalTime(&timeinfo) && retry < 10) {
  Serial.print(".");
  delay(1000);
  retry++;
}

if (retry >= 10) {
  Serial.println("\n❌ NTP zamanı alınamadı!");
} else {
  Serial.println("\n✅ NTP zamanı alındı!");
  Serial.printf("Yerel saat: %02d:%02d:%02d\n",
                timeinfo.tm_hour,
                timeinfo.tm_min,
                timeinfo.tm_sec);
}

}
long getUnixTimestamp() {
  time_t now;
  time(&now);
  return now;  // saniye cinsinden Unix timestamp
}

// MPU6050 Başlatma (Sadece kart okutulduktan sonra çalışır)
void initializeMPU6050() {
  Serial.println("MPU6050 başlatılıyor...");
  mpu.initialize();
  if (mpu.testConnection()) {
    Serial.println("MPU6050 bağlantısı başarılı!");
  } else {
    Serial.println("MPU6050 bağlantısı başarısız!");
    mpuActive = false;
  }
}

// DS18B20 Başlatma
void initializeDS18B20() {
  sensors.begin();
}

// MAX30102 Başlatma
void initializeMAX30102() {
  Serial.println("MAX30102 başlatılıyor...");
  if (!particleSensor.begin(Wire, I2C_SPEED_FAST)) {
    Serial.println("MAX30102 bulunamadı!");
    while (1);
  }
  particleSensor.setup();
  particleSensor.setPulseAmplitudeRed(0x3F);
  particleSensor.setPulseAmplitudeGreen(0);
}

// RFID Başlatma
void initializeRFID() {
  SPI.begin();
  mfrc522.PCD_Init();
  Serial.println("RFID okuyucu başlatıldı");
}

// MPU6050 Okuma Fonksiyonu (Saniyede bir çalışır)
void readMPU6050() {
  mpu.getAcceleration(&ax, &ay, &az);
  mpu.getRotation(&gx, &gy, &gz);
  
  Serial.print("MPU Ölçüm - Accel X:"); Serial.print(ax); 
  Serial.print(" Y:"); Serial.print(ay); 
  Serial.print(" Z:"); Serial.print(az);
  Serial.print(" Gyro X:"); Serial.print(gx); 
  Serial.print(" Y:"); Serial.print(gy); 
  Serial.print(" Z:"); Serial.println(gz);
}

// RFID Okuma Fonksiyonu
void readRFID() {
  currentCardUID = "";
  for (byte i = 0; i < mfrc522.uid.size; i++) {
    if (mfrc522.uid.uidByte[i] < 0x10) currentCardUID += "0";
    currentCardUID += String(mfrc522.uid.uidByte[i], HEX);
  }

  // C# uygulamasının okuyabilmesi için 'CARD_UID:' ön ekini ekleyin.
  // Bu ön ek, ExtractUidFromData metodu tarafından filtrelenir.
  Serial.print("CARD_UID:");
  Serial.println(currentCardUID);
  
  // HTTP POST isteği ayrı bir işlem olarak devam edebilir
  sendCardRegistration(); 
  
  tone(BUZZER_PIN, 2000);
  delay(1000);
  noTone(BUZZER_PIN);
}

// MAX30102 Tek Seferlik Okuma (Sadece kart okutulduğunda)
void readMAX30102ForSingleReading() {
  Serial.println("MAX30102 ölçüm yapılıyor...");
  
  // Birkaç saniye ölçüm al
  unsigned long startTime = millis();
  int validReadings = 0;
  long totalIR = 0;
  long totalRed = 0;
  
  while (millis() - startTime < 3000 && validReadings < 10) { // 3 saniye veya 10 geçerli okuma
    long irValue = particleSensor.getIR();
    long redValue = particleSensor.getRed();
    
    if (irValue > 50000) { // Parmak algılandı
      totalIR += irValue;
      totalRed += redValue;
      validReadings++;
      
      if (checkForBeat(irValue)) {
        long delta = millis() - lastBeat;
        lastBeat = millis();
        beatsPerMinute = 60.0 / (delta / 1000.0);
        
        if (beatsPerMinute >= 40 && beatsPerMinute <= 160) {
          rates[rateSpot++] = (byte)beatsPerMinute;
          rateSpot %= RATE_SIZE;
          
          // Ortalama hesapla
          beatAvg = 0;
          for (byte i = 0; i < RATE_SIZE; i++) {
            beatAvg += rates[i];
          }
          beatAvg /= RATE_SIZE;
        }
      }
    }
    delay(100);
  }
  
  // SpO2 hesapla
  if (validReadings > 0) {
    float avgIR = totalIR / validReadings;
    float avgRed = totalRed / validReadings;
    float ratio = (avgRed / avgIR);
    spo2 = 100 - (ratio * 25);
    
    if (spo2 > 100) spo2 = 100;
    if (spo2 < 70) spo2 = 70;
  } else {
    beatAvg = 0;
    spo2 = 0;
    Serial.println("Parmak algılanamadı!");
  }
  
  Serial.print("Kalp Atışı: "); Serial.print(beatAvg); Serial.println(" BPM");
  Serial.print("SpO2: "); Serial.print(spo2); Serial.println("%");
}

// Masaüstü uygulaması için kart kaydı gönderme
void sendCardRegistration() {
  if (WiFi.status() != WL_CONNECTED) {
    Serial.println("Wi-Fi bağlantısı kesik!");
    return;
  }
  
  StaticJsonDocument<128> doc;
  doc["cardUID"] = currentCardUID;
  doc["timestamp"] = getUnixTimestamp();

  
  String jsonOutput;
  serializeJson(doc, jsonOutput);
  
  Serial.println("Kart kaydı gönderiliyor:");
  Serial.println(jsonOutput);
  
  HTTPClient http;
  http.begin(cardRegistrationUrl);
  http.addHeader("Content-Type", "application/json");
  
  int httpResponseCode = http.POST(jsonOutput);
  Serial.print("Kart kaydı HTTP Yanıt Kodu: ");
  Serial.println(httpResponseCode);
  
  if (httpResponseCode > 0) {
    String response = http.getString();
    Serial.println("Kart kaydı yanıtı:");
    Serial.println(response);
  }
  http.end();
}

// Tüm Sensörleri Oku ve Veri Gönder (Sadece kart okutulduğunda)
void readAllSensorsAndSend() {
  Serial.println("=== Tüm sensörler okunuyor ===");
  
  // Sıcaklık sensörünü oku
  sensors.requestTemperatures();
  float temperatureC = sensors.getTempCByIndex(0);
  if (temperatureC == DEVICE_DISCONNECTED_C) {
    temperatureC = -127.0;
  }
  Serial.print("Sıcaklık: "); Serial.print(temperatureC); Serial.println("°C");
  
  // ECG oku
  int ecgValue = analogRead(AD8232_PIN);
  Serial.print("ECG: "); Serial.println(ecgValue);
  
  // MAX30102 oku (kalp atışı ve SpO2)
  readMAX30102ForSingleReading();
  
  // MPU değerleri zaten güncel (saniyede bir okunan)
  Serial.print("Son MPU Değerleri - Accel X:"); Serial.print(ax); 
  Serial.print(" Y:"); Serial.print(ay); Serial.print(" Z:"); Serial.println(az);
  
  // Tüm verileri gönder
  sendSensorDataToServer();
}

// Sensör Veri Gönderme Fonksiyonu
void sendSensorDataToServer() {
  if (WiFi.status() != WL_CONNECTED) {
    Serial.println("Wi-Fi bağlantısı kesik!");
    return;
  }
  
  if (currentCardUID == "") {
    Serial.println("Kart UID'si boş!");
    return;
  }
  
  // Sıcaklık tekrar oku
  sensors.requestTemperatures();
  float temperatureC = sensors.getTempCByIndex(0);
  if (temperatureC == DEVICE_DISCONNECTED_C) {
    temperatureC = -127.0;
  }
  
  int ecgValue = analogRead(AD8232_PIN);
  
  // JSON Oluştur
  StaticJsonDocument<512> doc;
  doc["cardUID"] = currentCardUID;
  doc["tempC"] = temperatureC;
  doc["ecg"] = ecgValue;
  doc["heartRate"] = beatAvg;
  doc["spo2"] = spo2;
  doc["accel_x"] = ax;
  doc["accel_y"] = ay;
  doc["accel_z"] = az;
  doc["gyro_x"] = gx;
  doc["gyro_y"] = gy;
  doc["gyro_z"] = gz;
  doc["timestamp"] = getUnixTimestamp();

  
  String jsonOutput;
  serializeJson(doc, jsonOutput);
  
  Serial.println("=== Sensör verisi gönderiliyor ===");
  Serial.println(jsonOutput);
  
  // HTTP POST
  HTTPClient http;
  http.begin(serverApiUrl);
  http.addHeader("Content-Type", "application/json");
  
  int httpResponseCode = http.POST(jsonOutput);
  Serial.print("Sensör verisi HTTP Yanıt Kodu: ");
  Serial.println(httpResponseCode);
  
  if (httpResponseCode > 0) {
    String response = http.getString();
    Serial.println("Sensör verisi yanıtı:");
    Serial.println(response);
  } else {
    Serial.print("Hata: ");
    Serial.println(http.errorToString(httpResponseCode));
  }
  http.end();
  
  Serial.println("=== Veri gönderimi tamamlandı ===\n");
}