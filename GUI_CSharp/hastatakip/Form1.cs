using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MongoDB.Bson; // BsonDocument için gerekli
using MongoDB.Driver; // MongoConnectionException için gerekli


namespace hastatakip
{
    public partial class Form1 : Form
    {
        
        private MongoDBService _dbService;
     

        public Form1()
        {
            InitializeComponent();
            
            _dbService = new MongoDBService();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
          
           
        }

   

       
        
        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)//Kayıt Olma sayfası yönlendirmesi
        {
            patient_registration git = new patient_registration();
            git.Show();
            this.Hide();
        }

        private async void button1_Click_1(object sender, EventArgs e)//Giriş Yap butonu
        {
            string tc = textBox1.Text.Trim();
            string eposta = textBox2.Text.Trim();
            string sifre = textBox3.Text.Trim();

            // --- Kullanıcı Arayüzü (UI) Doğrulamaları ---
            if (string.IsNullOrEmpty(tc) || string.IsNullOrEmpty(eposta) || string.IsNullOrEmpty(sifre))
            {
                MessageBox.Show("Lütfen tüm alanları doldurunuz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // TC Kimlik No format kontrolü
            if (tc.Length != 11 || !tc.All(char.IsDigit))
            {
                MessageBox.Show("TC Kimlik Numarası 11 haneli bir sayı olmalıdır.", "Geçersiz Giriş", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Basit e-posta format kontrolü
            if (!eposta.Contains("@") || !eposta.Contains("."))
            {
                MessageBox.Show("Geçerli bir e-posta adresi giriniz.", "Geçersiz Giriş", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Şifre uzunluk kontrolü
            if (sifre.Length < 6)
            {
                MessageBox.Show("Şifre en az 6 karakter uzunluğunda olmalıdır.", "Geçersiz Giriş", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // --- Asenkron Veritabanı İşlemi ---
            try
            {
                // Metot adını KullaniciGirisAsync olarak güncelledik.
                // Asenkron işlemi beklemek için 'await' anahtar kelimesini ekledik.
                User kullanici = await _dbService.KullaniciGirisAsync(tc, eposta, sifre);

                if (kullanici != null)
                {
                    // Kullanıcı veritabanında bulunduysa
                    MessageBox.Show($"Giriş başarılı! Hoş geldiniz {kullanici.ad} {kullanici.soyad}.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Kullanıcı nesnesini parametre olarak göndererek yeni formu oluştur
                    patient_homepage mainForm = new patient_homepage(kullanici);
                    mainForm.Show();
                    this.Hide();
                }
                else
                {
                    // Bu kısım, KullaniciGirisAsync'te 'throw new UnauthorizedAccessException' olduğu için
                    // aslında burada çalışmayacaktır. Hata direkt catch bloğuna düşecektir.
                    // Bu 'else' bloğu artık gereksiz, ancak kodu sadeleştirmek isterseniz kaldırabilirsiniz.
                    // Eğer metot null dönerse bu blok çalışır, ancak sizin async metodunuz exception fırlatıyor.
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                // Kullanıcı giriş hatasını özel olarak yakala
                MessageBox.Show(ex.Message, "Giriş Başarısız", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                // Veritabanı bağlantısı gibi beklenmedik hataları yakala
                MessageBox.Show($"Beklenmedik bir hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
