using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace SkillIssue
{
    public partial class Dashboard : Window
    {
        private readonly HttpClient _client;
        private readonly string _rawCookies;

        public Dashboard(HttpClient client, string rawCookies)
        {
            InitializeComponent();
            _client = client;
            _rawCookies = rawCookies;

            Loaded += Dashboard_Loaded;
        }

        private async void Dashboard_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadUser();
        }

        private async Task LoadUser()
        {
            var req = new HttpRequestMessage(HttpMethod.Get, "/api/users");
            req.Headers.Add("Cookie", _rawCookies);
            req.Headers.Add("X-XSRF-TOKEN", GetToken());

            var res = await _client.SendAsync(req);
            var json = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
            {
                MessageBox.Show("Nem sikerült lekérni a felhasználót.");
                return;
            }

            using var doc = JsonDocument.Parse(json);
            string name = doc.RootElement.GetProperty("name").GetString();

            welcomeText.Text = $"Üdvözöllek, {name}!";
        }

        private string GetToken()
        {
            var m = Regex.Match(_rawCookies, @"XSRF-TOKEN=([^;]+)");
            return m.Success ? Uri.UnescapeDataString(m.Groups[1].Value) : "";
        }
    }
}
