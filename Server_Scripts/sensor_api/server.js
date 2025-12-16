// server.js

require('dotenv').config();

const express = require('express');
const mongoose = require('mongoose');
const { DateTime } = require('luxon');

const app = express();
const port = process.env.PORT || 3000;

// ------------------ MongoDB Bağlantısı ------------------
mongoose.connect(process.env.MONGODB_URI, {
    useNewUrlParser: true,
    useUnifiedTopology: true
})
.then(() => console.log('MongoDB bağlantısı başarılı!'))
.catch(err => console.error('MongoDB bağlantı hatası:', err));

// ------------------ Schema ------------------
const SensorDataSchema = new mongoose.Schema({
    cardUID: { type: String, required: true },
    ad: { type: String, default: '' },
    notes: { type: String, default: '' },
    readings: [{
        tempC: Number,
        ecg: Number,
        heartRate: Number,
        spo2: Number,
        irValue: Number,
        redValue: Number,
        accel_x: Number,
        accel_y: Number,
        accel_z: Number,
        gyro_x: Number,
        gyro_y: Number,
        gyro_z: Number,
        timestamp: Date
    }],
    lastUpdated: Date
});

const SensorData = mongoose.model('SensorData', SensorDataSchema);

// ------------------ Middleware ------------------
app.use(express.json());

// ------------------ Kart Kaydı ------------------
app.post('/register-card', async (req, res) => {
    try {
        const cardUID = req.body.cardUID.trim();

        let card = await SensorData.findOne({ cardUID });
        if (card) {
            return res.json({
                success: true,
                message: 'Kart zaten kayıtlı',
                isNew: false
            });
        }

        const newCard = new SensorData({
            cardUID,
            ad: 'Yeni Hasta',
            notes: '',
            lastUpdated: new Date()
        });

        await newCard.save();

        res.status(201).json({
            success: true,
            message: 'Kart başarıyla kaydedildi',
            isNew: true
        });

    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

// ------------------ SENSÖR VERİSİ (SAAT DÜZELTİLMİŞ) ------------------
app.post('/data', async (req, res) => {
    try {
        console.log("Gelen veri:", req.body);

        const { cardUID: rawCardUID, timestamp, ...sensorData } = req.body;
        const cardUID = rawCardUID.trim();

        const card = await SensorData.findOne({ cardUID });
        if (!card) {
            return res.status(404).json({ message: "Kart bulunamadı" });
        }

        // 🔥 UNIX → TÜRKİYE SAATİ
        const trDate = DateTime
            .fromSeconds(Number(timestamp))
            .setZone("Europe/Istanbul")
            .toJSDate();

        card.readings.push({
            ...sensorData,
            timestamp: trDate
        });

        card.lastUpdated = trDate;
        await card.save();

        res.json({
            success: true,
            message: "Veri başarıyla kaydedildi",
            savedTime: trDate
        });

    } catch (err) {
        console.error(err);
        res.status(500).json({ error: err.message });
    }
});

// ------------------ Kartları Listele ------------------
app.get('/cards', async (req, res) => {
    const cards = await SensorData.find({}, 'cardUID ad lastUpdated')
        .sort({ lastUpdated: -1 });

    res.json(cards);
});

// ------------------ Kart Verisi ------------------
app.get('/data/:cardUID', async (req, res) => {
    const cardUID = req.params.cardUID.trim();
    const data = await SensorData.findOne({ cardUID });

    if (!data) {
        return res.status(404).json({ message: "Kart bulunamadı" });
    }

    res.json(data);
});

// ------------------ Kart Güncelle ------------------
app.put('/cards/:cardUID', async (req, res) => {
    const cardUID = req.params.cardUID.trim();
    const { ad, notes } = req.body;

    const updated = await SensorData.findOneAndUpdate(
        { cardUID },
        {
            ad,
            notes,
            lastUpdated: new Date()
        },
        { new: true }
    );

    if (!updated) {
        return res.status(404).json({ message: "Kart bulunamadı" });
    }

    res.json(updated);
});

// ------------------ Ana Sayfa ------------------
app.get('/', (req, res) => {
    res.json({
        message: "ESP32 Sensör API",
        endpoints: [
            "POST /register-card",
            "POST /data",
            "GET /cards",
            "GET /data/:cardUID"
        ]
    });
});

// ------------------ Server Başlat ------------------
app.listen(port, () => {
    console.log(`Server çalışıyor → http://localhost:${port}`);
});
