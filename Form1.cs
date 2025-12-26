using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MasaustuChatBot
{
    /// <summary>
    /// Ana form sınıfı - Kullanıcı arayüzü ve chatbot etkileşimini yönetir.
    /// GeminiService sınıfını kullanarak API ile iletişim kurar (Dependency kullanımı).
    /// </summary>
    public partial class Form1 : Form
    {
        // GeminiService instance'ı - API iletişimi için
        private readonly GeminiService _geminiService;

        /// <summary>
        /// Form constructor - Bileşenleri ve servisi başlatır.
        /// </summary>
        public Form1()
        {
            InitializeComponent();
            
            // GeminiService nesnesini oluşturuyoruz
            _geminiService = new GeminiService();

            // Enter tuşu ile mesaj gönderme özelliği
            txtMessage.KeyDown += TxtMessage_KeyDown;
        }

        /// <summary>
        /// Enter tuşuna basıldığında mesaj gönderme işlemi.
        /// NOT: txtMessage.Multiline = true olmalı ki Shift+Enter alt satıra geçsin.
        /// </summary>
        private async void TxtMessage_KeyDown(object sender, KeyEventArgs e)
        {
            // Sadece Enter: Mesaj gönder
            // Shift+Enter: Alt satıra geç (mesaj gönderme)
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                e.SuppressKeyPress = true; // Enter sesini ve yeni satır eklemeyi engelle
                await SendMessageAsync();
            }
            // Shift+Enter durumunda hiçbir şey yapma, TextBox kendi davranışıyla alt satıra geçer
        }

        /// <summary>
        /// Gönder butonuna tıklandığında çalışan event handler.
        /// async void kullanılır çünkü bu bir event handler'dır.
        /// </summary>
        private async void btnSend_Click(object sender, EventArgs e)
        {
            await SendMessageAsync();
        }

        /// <summary>
        /// Mesaj gönderme işlemini gerçekleştiren ana metot.
        /// Async/await yapısı ile UI thread'i bloklanmadan çalışır.
        /// </summary>
        private async Task SendMessageAsync()
        {
            // Boş mesaj kontrolü
            string userMessage = txtMessage.Text.Trim();
            if (string.IsNullOrEmpty(userMessage))
            {
                MessageBox.Show("Lütfen bir mesaj yazın.", "Uyarı", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // İşlem sürerken butonu ve textbox'ı pasif yap
                SetControlsEnabled(false);

                // Kullanıcı mesajını sohbet geçmişine ekle (Mavi renk)
                AppendColoredText("Ben: ", Color.Blue, true);
                AppendColoredText(userMessage + Environment.NewLine, Color.Blue, false);

                // Mesaj kutusunu temizle
                txtMessage.Clear();

                // "Düşünüyor..." mesajını göster
                AppendColoredText("AI: ", Color.DarkGreen, true);
                AppendColoredText("Düşünüyor..." + Environment.NewLine, Color.Gray, false);

                // API'den yanıt al (asenkron)
                string response = await _geminiService.GetResponseAsync(userMessage);

                // Markdown formatını temizle (*, **, ` gibi karakterleri kaldır)
                string cleanResponse = CleanMarkdown(response);

                // "Düşünüyor..." mesajını kaldır (son 2 satırı sil)
                RemoveLastLines(2);

                // AI yanıtını sohbet geçmişine ekle (Yeşil renk)
                AppendColoredText("AI: ", Color.DarkGreen, true);
                AppendColoredText(cleanResponse + Environment.NewLine + Environment.NewLine, Color.Black, false);

                // Sohbeti en alta kaydır
                rtbHistory.ScrollToCaret();
            }
            catch (Exception ex)
            {
                // Hata durumunda "Düşünüyor..." mesajını kaldır
                RemoveLastLines(2);

                // Hata mesajını göster (Kırmızı renk)
                AppendColoredText("Hata: ", Color.Red, true);
                AppendColoredText(ex.Message + Environment.NewLine + Environment.NewLine, Color.Red, false);
            }
            finally
            {
                // İşlem bitince kontrolleri aktif yap
                SetControlsEnabled(true);
                txtMessage.Focus();
            }
        }

        /// <summary>
        /// RichTextBox'a renkli metin ekler.
        /// </summary>
        /// <param name="text">Eklenecek metin</param>
        /// <param name="color">Metin rengi</param>
        /// <param name="bold">Kalın yazı mı?</param>
        private void AppendColoredText(string text, Color color, bool bold)
        {
            // Ekleme pozisyonunu kaydet
            int start = rtbHistory.TextLength;
            
            // Metni ekle
            rtbHistory.AppendText(text);

            // Eklenen metni seç
            rtbHistory.Select(start, text.Length);

            // Renk uygula
            rtbHistory.SelectionColor = color;

            // Kalın yazı uygula
            if (bold)
            {
                rtbHistory.SelectionFont = new Font(rtbHistory.Font, FontStyle.Bold);
            }
            else
            {
                rtbHistory.SelectionFont = new Font(rtbHistory.Font, FontStyle.Regular);
            }

            // Seçimi kaldır ve imleci sona taşı
            rtbHistory.SelectionLength = 0;
            rtbHistory.SelectionStart = rtbHistory.TextLength;
        }

        /// <summary>
        /// RichTextBox'tan son satırları kaldırır.
        /// </summary>
        /// <param name="lineCount">Kaldırılacak satır sayısı</param>
        private void RemoveLastLines(int lineCount)
        {
            string[] lines = rtbHistory.Lines;
            if (lines.Length >= lineCount)
            {
                // Son satırları hariç tutarak yeniden oluştur
                string[] newLines = new string[lines.Length - lineCount];
                Array.Copy(lines, 0, newLines, 0, newLines.Length);
                
                rtbHistory.Clear();
                
                // Satırları yeniden ekle (renksiz - basitleştirilmiş versiyon)
                foreach (string line in newLines)
                {
                    rtbHistory.AppendText(line + Environment.NewLine);
                }
            }
        }

        /// <summary>
        /// Form kontrollerinin aktif/pasif durumunu ayarlar.
        /// </summary>
        /// <param name="enabled">True: Aktif, False: Pasif</param>
        private void SetControlsEnabled(bool enabled)
        {
            btnSend.Enabled = enabled;
            txtMessage.Enabled = enabled;
            
            // Buton metnini güncelle
            btnSend.Text = enabled ? "Gönder" : "Bekleyin...";
        }

        /// <summary>
        /// Markdown formatını temizler (*, **, `, # gibi karakterleri kaldırır).
        /// </summary>
        /// <param name="text">Temizlenecek metin</param>
        /// <returns>Düz metin</returns>
        private string CleanMarkdown(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Kalın ve italik formatları temizle
            text = text.Replace("**", "");  // Kalın (**text**)
            text = text.Replace("*", "");   // İtalik (*text*)
            text = text.Replace("__", "");  // Alternatif kalın
            text = text.Replace("_", " ");  // Alternatif italik (boşlukla değiştir)
            
            // Kod formatlarını temizle
            text = text.Replace("```", ""); // Kod bloğu
            text = text.Replace("`", "");   // Satır içi kod
            
            // Başlık işaretlerini temizle (satır başındaki # karakterleri)
            string[] lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = lines[i].TrimStart('#', ' ');
            }
            text = string.Join("\n", lines);

            return text;
        }
    }
}
