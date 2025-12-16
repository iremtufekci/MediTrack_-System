using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using hastatakip;
using MongoDB.Driver; // MongoClientException için gerekli olabilir

namespace hastatakip
{
    public partial class patient_registration : Form
    {
        private MongoDBService _dbService;

        public patient_registration()
        {
            InitializeComponent();
            try
            {
                // MongoDBService başlatılırken bir sorun olursa yakala
                _dbService = new MongoDBService();
            }
            catch (Exception ex)
            {
                // Kullanıcıya bağlantı hatasını bildir
                MessageBox.Show($"Veritabanı bağlantı hatası: {ex.Message}\nUygulama düzgün çalışmayabilir.", "Veritabanı Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // Uygulamanın çalışmaya devam etmesi uygun değilse, burada formu kapatabilirsiniz
                // this.Load += (s, e) => this.Close(); 
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {
            // Bu metot boş kalabilir veya Label'a özel bir işlev ekleyebilirsiniz
        }

        private async void button1_Click(object sender, EventArgs e)//Kayıt olma butonu
        {
            // TextBox'lardan ve RadioButton'lardan verileri al
            string ad = txtAd.Text;
            string soyad = txtSoyad.Text;
            string tc = txtTC.Text;
            string eposta = txtEposta.Text;
            string sifre = txtSifre.Text;

            string cinsiyet = "";
            if (radioKadın.Checked)
                cinsiyet = "Kadın";
            else if (radioErkek.Checked)
                cinsiyet = "Erkek";
            else
            {
                MessageBox.Show("Lütfen cinsiyet seçiniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return; // Cinsiyet seçilmediyse işlemi durdur
            }

            try
            {
                // _dbService'deki asenkron metodu await ile çağırın
                await _dbService.KullaniciKaydetAsync(ad, soyad, tc, eposta, sifre, cinsiyet);

                MessageBox.Show("Kayıt başarıyla tamamlandı! ✅", "Kayıt Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Kayıt başarılı olduktan sonra form alanlarını temizleyebilirsiniz
                ClearFormFields();
            }
            catch (ArgumentException ex)
            {
                // KullaniciKaydetAsync metodundan gelen doğrulama hatalarını yakala
                MessageBox.Show($"Kayıt hatası: {ex.Message}", "Geçersiz Giriş", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (MongoWriteException ex)
            {
                // MongoDB'ye yazma sırasında oluşabilecek hataları yakala.
                // Özellikle unique key hataları burada yakalanabilir.
                if (ex.Message.Contains("E11000 duplicate key error"))
                {
                    MessageBox.Show("Bu TC Kimlik Numarası veya E-posta zaten kayıtlı. Lütfen kontrol ediniz.", "Kayıt Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show($"Veritabanına yazılırken bir hata oluştu: {ex.Message}", "Veritabanı Yazma Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (TimeoutException ex)
            {
                // Veritabanı bağlantı zaman aşımı gibi hataları yakala
                MessageBox.Show($"Veritabanı bağlantısı zaman aşımına uğradı: {ex.Message}", "Bağlantı Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                // Diğer tüm beklenmedik hataları yakala
                MessageBox.Show($"Beklenmedik bir hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)//Giriş yapma sayfasına yönlendirme
        {
            Form1 git = new Form1(); // Form1'inizin Login Formu olduğunu varsayıyoruz
            git.Show(); // Login formunu göster
            this.Hide(); // Kayıt formunu gizle
            // this.Close(); // Eğer kayıt formunu tamamen kapatmak isterseniz Hide yerine Close kullanın.
        }

        // Form alanlarını temizlemek için yardımcı metot
        private void ClearFormFields()
        {
            txtAd.Clear();
            txtSoyad.Clear();
            txtTC.Clear();
            txtEposta.Clear();
            txtSifre.Clear();
            radioKadın.Checked = false;
            radioErkek.Checked = false;
            // Diğer kontrolleriniz varsa onları da temizleyin
        }

        private void button2_Click(object sender, EventArgs e)//Giriiş sayfasına yönlendiren geri butonu
        {
            Form1 giris = new Form1();
            giris.Show();
            this.Hide();
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }
    }
}