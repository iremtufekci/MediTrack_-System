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
    // patient_analysis yerine form adını "profile" olarak değiştiriyoruz
    public partial class profile : Form
    {
        private User _kullanici;

        // Yapıcı metot da form adıyla eşleşmeli
        public profile(User kullanici)
        {
            InitializeComponent();
            _kullanici = kullanici;

            // Form yüklendiğinde bilgileri Label'lara yaz
            this.Load += profile_Load;
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
            else
            {

            }
        }

        private void button4_Click(object sender, EventArgs e)//Hesapla kart eşleştirme sayfası
        {
            if (_kullanici != null)
            {
                // 'kullanici' nesnesini direkt gönder
                patient_cartuid cart = new patient_cartuid(_kullanici);
                cart.Show();
                this.Hide();
            }
            else
            {
                MessageBox.Show("Giriş yapan kullanıcı bilgisi bulunamadı. Lütfen tekrar giriş yapın.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void label9_Click(object sender, EventArgs e)
        {

        }

        private void label8_Click(object sender, EventArgs e)
        {

        }

        private void profile_Load(object sender, EventArgs e)
        {
            lblad.Text = _kullanici.ad;
            lblsoyad.Text = _kullanici.soyad;
            lbltc.Text = _kullanici.tc;
            lbleposta.Text = _kullanici.eposta;
            lblcinsiyet.Text = _kullanici.cinsiyet;
            lblkartuıd.Text = _kullanici.cardUID;


            //Sadece okunur
            lblad.ReadOnly = true;
            lblsoyad.ReadOnly = true;
            lbltc.ReadOnly = true;
            lbleposta.ReadOnly = true;
            lblcinsiyet.ReadOnly = true;
            lblkartuıd.ReadOnly = true;


            // Form başlığını da dinamik olarak güncelleyebilirsiniz.
            this.Text = $"Profil Sayfası - {_kullanici.ad} {_kullanici.soyad}";

        }

        // Bu metot artık patient_analysis formuna ait olduğu için, çağrıldığı yerleri de değiştirmelisiniz.
        private void button2_Click(object sender, EventArgs e)//İstatiksel analiz sayfasına yönlendirme
        {
            if (_kullanici != null)
            {
                // 'kullanici' nesnesini direkt gönder
                // Artık formun adı profile olduğu için, burayı düzeltiyoruz.
                // Eğer bu butona tıklanınca analiz sayfası açılacaksa, bu kısım olduğu gibi kalabilir.
                // Ancak amacınız profil sayfasını açmaksa, bu kodu çıkarmalısınız.
                // Şu anda bu butonun ne işe yaradığı tam olarak belli olmadığı için,
                // varsayılan olarak patient_analysis formunu açtığını varsayıyorum.
                patient_analysis analiz = new patient_analysis(_kullanici);
                analiz.Show();
                this.Hide();
            }
            else
            {
                MessageBox.Show("Giriş yapan kullanıcı bilgisi bulunamadı. Lütfen tekrar giriş yapın.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)//Profil sayfasına yönlendirme
        {

        }

      
        

        private void button1_Click_1(object sender, EventArgs e)
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
    }
}
