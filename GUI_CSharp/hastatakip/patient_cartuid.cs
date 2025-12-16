using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Windows.Forms;

namespace hastatakip
{
    public partial class patient_cartuid : Form
    {
        private SerialPort _arduinoPort;
        private readonly MongoDBService _mongoDbService;

        // Giriş yapan kullanıcının bilgilerini tutacak alan
        private readonly User _kullanici;

        // Kart bilgilerini ve eşleşen kullanıcıyı tutmak için
        private string _okunanKartUID;
        private User _ilgiliKullanici; // Kartla eşleşen kullanıcı (varsa)

        public patient_cartuid(User kullanici)
        {
            InitializeComponent();
            _mongoDbService = new MongoDBService();

            // Giriş yapan kullanıcı nesnesini sakla
            _kullanici = kullanici;

            // UI başlangıç ayarları
            button1.Enabled = false;
            button2.Enabled = false;
            lblKartUID.Text = "Kart okutun...";

          
        }

        private void patient_cartuid_Load(object sender, EventArgs e)
        {
            try
            {
                // Seri port bağlantısını başlatıyoruz
                // Port numarasını projenizin ayarlarına göre değiştirin
                _arduinoPort = new SerialPort("COM8", 9600);
                _arduinoPort.DataReceived += ArduinoPort_DataReceived;
                _arduinoPort.Open();

                lblKartUID.Text = "Arduino bağlandı. Kart okutmaya hazır...";
                lblKartUID.ForeColor = Color.DarkGreen;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Arduino bağlantı hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblKartUID.Text = "Arduino bağlantı hatası!";
                lblKartUID.ForeColor = Color.Red;
            }
        }

        private void ArduinoPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string satir = _arduinoPort.ReadLine().Trim();
                string kartUid = ExtractUidFromData(satir);

                if (!string.IsNullOrEmpty(kartUid))
                {
                    this.Invoke((MethodInvoker)(() =>
                    {
                        if (kartUid != _okunanKartUID)
                        {
                            _okunanKartUID = kartUid;
                            SearchUserByCardUID(kartUid);
                        }
                    }));
                }
            }
            catch
            {
                // seri port bazen yarım satır gönderir → görmezden gel
            }
        }
        private async void SearchUserByCardUID(string kartUID)
        {
            try
            {
                lblKartUID.Text = $"Kart: {kartUID}\nKullanıcı aranıyor...";
                lblKartUID.ForeColor = Color.DarkBlue;

                // MongoDB'den eşleşen kullanıcıyı ara
                _ilgiliKullanici = await _mongoDbService.KartUIDIleKullaniciBulAsync(kartUID);

                if (_ilgiliKullanici != null)
                {
                    // Kullanıcı bulunduysa
                    string successText = $"✅ Kart: {kartUID}\n" +
                                         $"👤 Hasta: {_ilgiliKullanici.ad}\n" +
                                         $"📧 Email: {_ilgiliKullanici.eposta}";

                    lblKartUID.Text = successText;
                    lblKartUID.ForeColor = Color.DarkGreen;

                    button1.Enabled = true;
                    button1.Text = "Sensör Verilerini Gör";
                    button2.Enabled = true;
                    button2.Text = "Temizle";

                    MessageBox.Show($"Hasta bulundu: {_ilgiliKullanici.ad}", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    // Kullanıcı bulunamadıysa
                    string warningText = $"⚠️ Kart: {kartUID}\n" +
                                         $"Bu kart kayıtlı değil!\n" +
                                         $"🏥 Bu kullanıcı ile eşleştirilsin mi?";

                    lblKartUID.Text = warningText;
                    lblKartUID.ForeColor = Color.DarkRed;

                    button1.Enabled = true;
                    button1.Text = "Bu Kart ile Eşleştir";
                    button2.Enabled = true;
                    button2.Text = "İptal";

                    MessageBox.Show($"Bu kart sisteme kayıtlı değil!\n\n" +
                                    $"{_kullanici.ad}) ile eşleştirmek için 'Bu Kart ile Eşleştir' butonuna tıklayın.",
                                    "Kart Bulunamadı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                lblKartUID.Text = $"❌ Kart: {kartUID}\nArama hatası!";
                lblKartUID.ForeColor = Color.DarkRed;
                MessageBox.Show($"Hasta arama hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string ExtractUidFromData(string rawData)
        {
            if (string.IsNullOrEmpty(rawData)) return null;

            rawData = rawData.Trim().ToUpper();

            if (rawData.StartsWith("CARD_UID:")) return rawData.Substring(9).Trim();
            if (rawData.StartsWith("UID:"))
            {
                string veri = rawData.Substring(4).Trim();
                if (!veri.Contains("MPU") && !veri.Contains("MEASUREMENT"))
                {
                    return veri.Split(' ')[0];
                }
            }

            if (rawData.Contains("HEARTRATE") || rawData.Contains("SPO2") ||
                rawData.Contains("TEMP") || rawData.Contains("ECG") ||
                rawData.Contains("MPU") || rawData.Contains("MEASUREMENT") ||
                rawData.Contains("ACCEL") || rawData.Contains("GYRO") ||
                rawData.Contains(","))
            {
                return null;
            }

            if (rawData.Length >= 4 && rawData.Length <= 16 &&
                rawData.All(c => char.IsLetterOrDigit(c)))
            {
                return rawData;
            }

            return null;
        }

        private async void button1_Click(object sender, EventArgs e)//Hesapla kart eşleştirme butonu
        {
            if (string.IsNullOrEmpty(_okunanKartUID))
            {
                MessageBox.Show("Önce bir kart okutun!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                if (_ilgiliKullanici != null)
                {
                    // Hasta bulunduysa sensör verilerini göster
                    MessageBox.Show("Hasta bulundu");
                }
                else
                {
                    // Hasta bulunamadıysa giriş yapan kullanıcı ile eşleştir
                    await EslestirGirisYapanKullaniciIle();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"İşlem hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task EslestirGirisYapanKullaniciIle()
        {
            try
            {
                var result = MessageBox.Show(
                    $"Bu kartı (UID: {_okunanKartUID})  ile eşleştirmek istiyor musunuz?\n\n" +
                    $"🆔 Kullanıcı ID: {_kullanici._id}\n" +
                    $"💳 Kart UID: {_okunanKartUID}",
                    "Kart Eşleştirme Onayı",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result != DialogResult.Yes) return;

                lblKartUID.Text = $"🔄 Kart: {_okunanKartUID}\nEşleştirme yapılıyor...";
                lblKartUID.ForeColor = Color.DarkBlue;

                bool eslesmeBasarili = await _mongoDbService.KartUIDIleKullaniciEslestirAsync(_kullanici._id, _okunanKartUID);

                if (eslesmeBasarili)
                {
                    // Kullanıcı bilgilerini güncelle
                    _ilgiliKullanici = await _mongoDbService.GetUserByIdAsync(_kullanici._id);

                    if (_ilgiliKullanici != null)
                    {
                        string successText = $"✅ Kart: {_okunanKartUID}\n" +
                                             $"👤 Hasta: {_ilgiliKullanici.ad}\n" +
                                             $"📧 Email: {_ilgiliKullanici.eposta}\n" +
                                             "🔗 Eşleştirme tamamlandı!";

                        lblKartUID.Text = successText;
                        lblKartUID.ForeColor = Color.DarkGreen;
                       
                    }
                    else
                    {
                        MessageBox.Show("Eşleştirme yapıldı ancak kullanıcı bilgileri güncellenemedi!\nSayfayı yenileyin.",
                                        "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    lblKartUID.Text = $"❌ Kart: {_okunanKartUID}\nEşleştirme başarısız!";
                    lblKartUID.ForeColor = Color.DarkRed;
                    MessageBox.Show("❌ Eşleştirme başarısız!\n\nVeritabanı hatası oluşmuş olabilir.\nLütfen tekrar deneyin.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                lblKartUID.Text = $"❌ Kart: {_okunanKartUID}\nEşleştirme hatası!";
                lblKartUID.ForeColor = Color.DarkRed;
                MessageBox.Show($"Eşleştirme hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

      
        private void button2_Click(object sender, EventArgs e)//İptal etme butonu
        {
            ClearForm();
            MessageBox.Show("Form temizlendi. Yeni bir kart okutabilirsiniz.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ClearForm()
        {
            lblKartUID.Text = "Kart okutun...";
            lblKartUID.ForeColor = Color.Black;

            _okunanKartUID = null;
            _ilgiliKullanici = null;

            button1.Enabled = false;
            button1.Text = "Bekliyor...";
            button2.Enabled = false;
            button2.Text = "Temizle";
            
        }

        private void patient_cartuid_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (_arduinoPort != null && _arduinoPort.IsOpen)
                {
                    _arduinoPort.Close();
                    _arduinoPort.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Port kapatma hatası: {ex.Message}");
            }
        }

        private void lblKartUID_TextChanged(object sender, EventArgs e)
        {
            // Renk güncellemeleri
            if (lblKartUID.Text.Contains("✅"))
            {
                lblKartUID.ForeColor = Color.DarkGreen;
            }
            else if (lblKartUID.Text.Contains("⚠️") || lblKartUID.Text.Contains("❌"))
            {
                lblKartUID.ForeColor = Color.DarkRed;
            }
            else if (lblKartUID.Text.Contains("aranıyor") || lblKartUID.Text.Contains("yükleniyor"))
            {
                lblKartUID.ForeColor = Color.DarkBlue;
            }
            else
            {
                lblKartUID.ForeColor = Color.Black;
            }

            
        }

        private void button3_Click(object sender, EventArgs e)//Anasayfaya geri gtime butonu
        {
            patient_homepage anasayfa = new patient_homepage(_kullanici);
            anasayfa.Show();
            this.Hide();
        }
    }
}