using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MasaustuChatBot // <-- BURAYI KONTROL ET: Proje isminle aynı olmalı
{
    public class GeminiService
    {
        // 1. DİKKAT: API Anahtarını tırnakların içine yapıştır (Boşluk bırakma!)
        private const string ApiKey = "AIzaSyCgb_nRzXZwQ484bNxpbmkHRuUM1wKq-Zk";

        // 2. GÜNCELLEME: Artık 'gemini-2.5-flash' modelini kullanıyoruz.
        private const string ApiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key=";

        private readonly HttpClient _httpClient;

        public GeminiService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<string> GetResponseAsync(string userMessage)
        {
            // API anahtarı boşsa uyarı ver
            if (string.IsNullOrWhiteSpace(ApiKey))
            {
                return "HATA: Lütfen GeminiService.cs dosyasına Google API Anahtarınızı yapıştırın!";
            }

            try
            {
                // JSON Verisini Hazırla (Google'ın istediği format)
                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = userMessage }
                            }
                        }
                    }
                };

                string jsonContent = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // İsteği Gönder (URL + API Key birleşimi)
                var response = await _httpClient.PostAsync(ApiUrl + ApiKey, content);

                // Cevabı Kontrol Et
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    // Hata detayını daha okunaklı gösterelim
                    return $"Bağlantı Hatası ({response.StatusCode}):\n{errorContent}";
                }

                // Gelen Cevabı Oku
                string responseString = await response.Content.ReadAsStringAsync();

                // Cevabı Ayrıştır (Sadece yapay zekanın cümlesini al)
                JObject jsonResponse = JObject.Parse(responseString);
                string botResponse = jsonResponse["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();

                return botResponse ?? "Yapay zeka boş bir cevap döndürdü.";
            }
            catch (Exception ex)
            {
                return "Kritik Hata: " + ex.Message;
            }
        }
    }
}