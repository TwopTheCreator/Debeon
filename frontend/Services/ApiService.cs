using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Debeon.Models;

namespace Debeon.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "http://127.0.0.1:8080/api";

        public ApiService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        public async Task<List<RobloxInstallation>> GetInstallationsAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<RobloxInstallation>>>($"{_baseUrl}/installations");
                return response?.Success == true ? response.Data : new List<RobloxInstallation>();
            }
            catch
            {
                return new List<RobloxInstallation>();
            }
        }

        public async Task<RobloxConfig> GetConfigAsync(string profileName)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<RobloxConfig>>($"{_baseUrl}/config/{profileName}");
                return response?.Success == true ? response.Data : new RobloxConfig();
            }
            catch
            {
                return new RobloxConfig();
            }
        }

        public async Task<bool> SaveConfigAsync(string profileName, RobloxConfig config)
        {
            try
            {
                var json = JsonConvert.SerializeObject(config);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_baseUrl}/config/{profileName}", content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ApplyConfigAsync(RobloxConfig config)
        {
            try
            {
                var json = JsonConvert.SerializeObject(config);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_baseUrl}/apply", content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<string>> GetProfilesAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<string>>>($"{_baseUrl}/profiles");
                return response?.Success == true ? response.Data : new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        public async Task<Dictionary<string, string>> GetFlagsAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<Dictionary<string, string>>>($"{_baseUrl}/flags");
                return response?.Success == true ? response.Data : new Dictionary<string, string>();
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }

        public async Task<bool> SetFlagsAsync(Dictionary<string, string> flags)
        {
            try
            {
                var json = JsonConvert.SerializeObject(flags);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_baseUrl}/flags", content);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
