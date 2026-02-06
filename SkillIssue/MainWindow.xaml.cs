using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Text.RegularExpressions;

namespace SkillIssue
{
    public partial class MainWindow : Window
    {
        private readonly HttpClient _client;
        private string _rawCookies = "";

        public MainWindow()
        {
            InitializeComponent();

            _client = new HttpClient(new HttpClientHandler { UseCookies = false })
            {
                BaseAddress = new Uri("http://127.0.0.1:8000")
            };
            _client.DefaultRequestHeaders.Add("Accept", "application/json");
            _client.DefaultRequestHeaders.Add("Referer", "http://127.0.0.1:8000/");
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var csrfRes = await _client.GetAsync("/api/csrf-cookie");
                UpdateCookies(csrfRes);

                var authData = new { email = emailTextBox.Text, password = passwordBox.Password };
                var request = new HttpRequestMessage(HttpMethod.Post, "/api/login")
                {
                    Content = new StringContent(JsonSerializer.Serialize(authData), Encoding.UTF8, "application/json")
                };

                request.Headers.Add("Cookie", _rawCookies);
                request.Headers.Add("X-XSRF-TOKEN", GetToken());

                var loginRes = await _client.SendAsync(request);
                if (loginRes.IsSuccessStatusCode)
                {
                    UpdateCookies(loginRes);
                    await CheckAdmin();
                }
                else MessageBox.Show("Hibás adatok!");
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private async Task CheckAdmin()
        {
            var req = new HttpRequestMessage(HttpMethod.Get, "/api/users");
            req.Headers.Add("Cookie", _rawCookies);
            req.Headers.Add("X-XSRF-TOKEN", GetToken());

            var res = await _client.SendAsync(req);
            var json = await res.Content.ReadAsStringAsync();

            if (res.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(json);
                int isAdmin = doc.RootElement.GetProperty("is_admin").ValueKind == JsonValueKind.Number
                    ? doc.RootElement.GetProperty("is_admin").GetInt32()
                    : int.Parse(doc.RootElement.GetProperty("is_admin").GetString());

                if (isAdmin == 1) {
                    Dispatcher.Invoke(() =>
                    {
                        var dashboard = new Dashboard(_client, _rawCookies);
                        dashboard.Show();
                        this.Close();
                    });

                }
                else MessageBox.Show("Nem vagy admin.");
            }
            else MessageBox.Show("401 - Belépés megtagadva.");
        }

        private void UpdateCookies(HttpResponseMessage res)
        {
            if (!res.Headers.TryGetValues("Set-Cookie", out var newCookies)) return;

            var cookies = _rawCookies.Split(';')
                .Select(v => v.Trim().Split('='))
                .Where(p => p.Length >= 2)
                .ToDictionary(p => p[0], p => string.Join("=", p.Skip(1)));

            foreach (var c in newCookies)
            {
                var parts = c.Split(';')[0].Split('=');
                if (parts.Length >= 2) cookies[parts[0].Trim()] = string.Join("=", parts.Skip(1));
            }
            _rawCookies = string.Join("; ", cookies.Select(k => $"{k.Key}={k.Value}"));
        }

        private string GetToken()
        {
            var m = Regex.Match(_rawCookies, @"XSRF-TOKEN=([^;]+)");
            return m.Success ? Uri.UnescapeDataString(m.Groups[1].Value) : "";
        }
    }
}