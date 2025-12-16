using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace hastatakip
{
    public partial class patient_analysis : Form
    {

        private User _kullanici;

        private MongoDBService _mongoDBService;


        public patient_analysis(User kullanici)
        {
            InitializeComponent();
            _kullanici = kullanici;
            this.Load += async (s, e) => await LoadAllSensorDataAsync();

            _mongoDBService = new MongoDBService();
        }


        private async Task LoadAllSensorDataAsync()
        {
            try
            {
                var sensorDataList = await _mongoDBService.GetAllSensorDataByCardUID(_kullanici.cardUID);
                if (sensorDataList != null && sensorDataList.Any())
                {
                    var allReadings = sensorDataList
                        .Where(sd => sd.readings != null)
                        .SelectMany(sd => sd.readings)
                        .ToList();

                    if (allReadings.Any())
                    {
                        // 📊 ECG Tablosu
                        dataGridViewECG.DataSource = allReadings
                            .Select(r => new
                            {
                                Zaman = r.timestamp,
                                ECG = r.ecg
                            })
                            .ToList();

                        // 🌡️ Sıcaklık Tablosu
                        dataGridViewTemp.DataSource = allReadings
                            .Select(r => new
                            {
                                Zaman = r.timestamp,
                                Sicaklik = r.tempC
                            })
                            .ToList();

                        // ❤️ Nabız Tablosu
                        dataGridViewHeartRate.DataSource = allReadings
                            .Select(r => new
                            {
                                Zaman = r.timestamp,
                                Nabiz = r.heartRate
                            })
                            .ToList();

                        // 🫁 SpO2 Tablosu
                        dataGridViewSpO2.DataSource = allReadings
                            .Select(r => new
                            {
                                Zaman = r.timestamp,
                                SpO2 = r.spo2
                            })
                            .ToList();
                
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Veri yüklenirken hata: {ex.Message}");
            }
        }

        private void button1_Click(object sender, EventArgs e)//Anasayfa yönlendirme
        {
            if (_kullanici != null)
            {
                // 'kullanici' nesnesini direkt gönder
                patient_homepage homepage = new patient_homepage(_kullanici);
                homepage.Show();
                this.Hide();
            }
            else
            {
                MessageBox.Show("Giriş yapan kullanıcı bilgisi bulunamadı. Lütfen tekrar giriş yapın.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button3_Click(object sender, EventArgs e)//Profil sayfası yönlendirme
        {

            // 'kullanici' yerine artık '_kullanici' alanını kullanıyoruz.
            if (_kullanici != null)
            {
                // patient_analysis formunu açarken _kullanici nesnesini gönder
                profile analiz = new profile(_kullanici);
                analiz.Show();
                this.Hide();
            }
            else
            {
                MessageBox.Show("Giriş yapan kullanıcı bilgisi bulunamadı. Lütfen tekrar giriş yapın.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            DialogResult result = MessageBox.Show(
              "Çıkış yapmak istediğinizden emin misiniz?",
              "Çıkış Onayı",
              MessageBoxButtons.YesNo,
              MessageBoxIcon.Question
          );
            if (result == DialogResult.Yes)
            {
                Form1 git = new Form1();
                git.Show();
                this.Hide();
            }
            // 'else' bloğu, eğer çıkış yapılmazsa bir şey yapmayacağı için boş bırakılabilir.
        }

        private void button4_Click(object sender, EventArgs e)//Hesaba kart tanımlama sayfası
        {
            // 'kullanici' yerine artık '_kullanici' alanını kullanıyoruz.
            if (_kullanici != null)
            {
                // patient_cartuid formunu açarken _kullanici nesnesini gönder
                patient_cartuid cartuidForm = new patient_cartuid(_kullanici);
                cartuidForm.Show();
                this.Hide();
            }
            else
            {
                MessageBox.Show("Giriş yapan kullanıcı bilgisi bulunamadı. Lütfen tekrar giriş yapın.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
       

     

       

        private void button2_Click(object sender, EventArgs e)//İstatiksel analiz sayfası
        {

        }

        

        private async Task patient_analysis_LoadAsync(object sender, EventArgs e)
        {
            await LoadAllSensorDataAsync();
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
