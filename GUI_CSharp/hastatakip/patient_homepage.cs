using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MongoDB;
using System.Linq;
using hastatakip;
using MongoDB.Driver;

namespace hastatakip
{
    public partial class patient_homepage : Form
    {
        // Giriş yapan kullanıcının bilgilerini tutacak alan
        // Sadece bir alan tutucu kullanmak daha iyi bir yaklaşımdır.
        private User _kullanici;

        private MongoDBService _mongoDBService;
      

        // Yapıcı metot, giriş yapan kullanıcı nesnesini alacak
        public patient_homepage(User kullanici)
        {
            InitializeComponent();

            _mongoDBService = new MongoDBService();
            
            _kullanici = kullanici ?? throw new ArgumentNullException(nameof(kullanici));

           
           
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            // Bu metot boş bırakılabilir veya ihtiyaca göre doldurulabilir.
        }

        // Panel içine form yükleyen metot
    
        private void button1_Click(object sender, EventArgs e)//Anasayfa butonu
        {
            // Bu butonun işlevi buraya yazılabilir.
            // Örnek: Ana sayfaya dönme butonuysa, form içine başka bir formu yükleyebilirsiniz.
            // LoadFormIntoPanel(new patient_analysis(_kullanici)); // örnek kullanım
        }

        private void button2_Click(object sender, EventArgs e)//istatiksel analiz sayfası butonu
        {
            // 'kullanici' yerine artık '_kullanici' alanını kullanıyoruz.
            if (_kullanici != null)
            {
                // patient_analysis formunu açarken _kullanici nesnesini gönder
                patient_analysis analiz = new patient_analysis(_kullanici);
                analiz.Show();
                this.Hide();
            }
            else
            {
                MessageBox.Show("Giriş yapan kullanıcı bilgisi bulunamadı. Lütfen tekrar giriş yapın.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button3_Click(object sender, EventArgs e)//Profil sayfası butonu
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


        private async void patient_homepage_LoadAsync(object sender, EventArgs e)//Anasayfanın loadında son verielrin gözükmesi için
        {
            await LoadLastSensorDataAsync();
         

        }


        private async Task LoadLastSensorDataAsync()//Anasayfanın loadında son verielrin gözükmesi için metod

        {

            try

            {

                // Check if the user's cardUID exists.

                if (string.IsNullOrEmpty(_kullanici.cardUID))

                {

                    MessageBox.Show("Bu kullanıcıya atanmış kart bulunmamaktadır.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    return;

                }





                string cleanCardUID = _kullanici.cardUID.Trim();



                //Mongodbdeki kodu kullanılıyor en son veriyiçekmek için

                var lastData = await _mongoDBService.GetLastSensorDataByCardUID(cleanCardUID);



                // Bir belgenin bulunup bulunmadığını ve herhangi bir okuma içerip içermediğini kontrol edin.

                if (lastData != null && lastData.readings != null && lastData.readings.Any())

                {

                    // Okumalar dizisinin son elemanını al (en son okuma)

                    var lastReading = lastData.readings.LastOrDefault();



                    if (lastReading != null)

                    {

                        //sensör verilerini labellara yazdırma

                        lblkalp.Text = lastReading.heartRate.ToString();

                        lblspo2.Text = lastReading.spo2.ToString();



                        // decimale çeviriyor

                        lblsicaklik.Text = lastReading.tempC.ToString("F2");





                        lblecg.Text = lastReading.ecg.ToString();

                    }

                    else

                    {

                        // Eğer okuma listesi boşsa etiketleri temizleyin ve kullanıcıyı bilgilendirin.

                        ClearLabelsAndInformUser();

                    }

                }

                else

                {



                    ClearLabelsAndInformUser();

                }

            }

            catch (Exception ex)

            {

                // Veri yükleme sırasında herhangi bir hata oluşursa kullanıcıya bilgi ver.

                MessageBox.Show("Veri yüklenirken hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);

            }

        }

        private void ClearLabelsAndInformUser()//LoadLastSensorDataAsync metodunda olası durumlar dışında labelları boş değer döndürmek için
        {
            lblkalp.Text = "-";
            lblspo2.Text = "-";
            lblsicaklik.Text = "-";
            lblecg.Text = "-";
            MessageBox.Show("Sensör verisi bulunamadı.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    





    private void button4_Click(object sender, EventArgs e)//kart tanımlama sayfası
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

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)//Çıkış yapma butonu
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

        private void label16_Click(object sender, EventArgs e)
        {

        }

        private void label19_Click(object sender, EventArgs e)
        {

        }

        private void lblspo2_Click(object sender, EventArgs e)
        {

        }

        private async void btnYenile_ClickAsync(object sender, EventArgs e)
        {
            await LoadLastSensorDataAsync();
        }

        
    }
}
