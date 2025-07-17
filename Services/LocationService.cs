using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace QuanLyDatHang.Services
{
    public class LocationService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LocationService> _logger;

        public LocationService(HttpClient httpClient, IConfiguration configuration, ILogger<LocationService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<(decimal? Latitude, decimal? Longitude)> GetLocationFromIpAsync(string ipAddress)
        {
            try
            {
                // Bỏ qua IP local
                if (IsLocalIp(ipAddress))
                {
                    // Trả về vị trí mặc định (TP.HCM)
                    return (10.762622m, 106.660172m);
                }

                // Sử dụng ip-api.com (free service)
                var response = await _httpClient.GetAsync($"http://ip-api.com/json/{ipAddress}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<IpApiResponse>(content);
                    
                    if (result?.Status == "success" && result.Lat.HasValue && result.Lon.HasValue)
                    {
                        return (result.Lat.Value, result.Lon.Value);
                    }
                }

                // Fallback: Sử dụng ipapi.co
                response = await _httpClient.GetAsync($"https://ipapi.co/{ipAddress}/json/");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<IpApiCoResponse>(content);
                    
                    if (result?.Latitude.HasValue == true && result.Longitude.HasValue == true)
                    {
                        return (result.Latitude.Value, result.Longitude.Value);
                    }
                }

                _logger.LogWarning($"Không thể lấy vị trí từ IP: {ipAddress}");
                return (null, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy vị trí từ IP: {ipAddress}");
                return (null, null);
            }
        }

        public async Task<(decimal? Latitude, decimal? Longitude)> GetLocationFromIpAsync(string ipAddress, string apiKey = null)
        {
            try
            {
                // Bỏ qua IP local
                if (IsLocalIp(ipAddress))
                {
                    return (10.762622m, 106.660172m); // TP.HCM
                }

                // Sử dụng API có key (nếu có)
                if (!string.IsNullOrEmpty(apiKey))
                {
                    var response = await _httpClient.GetAsync($"https://api.ipgeolocation.io/ipgeo?apiKey={apiKey}&ip={ipAddress}");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var result = JsonSerializer.Deserialize<IpGeoLocationResponse>(content);
                        
                        if (result?.Latitude.HasValue == true && result.Longitude.HasValue == true)
                        {
                            return (result.Latitude.Value, result.Longitude.Value);
                        }
                    }
                }

                // Fallback về method không có key
                return await GetLocationFromIpAsync(ipAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi lấy vị trí từ IP với API key: {ipAddress}");
                return (null, null);
            }
        }

        private static bool IsLocalIp(string ipAddress)
        {
            return ipAddress == "127.0.0.1" || 
                   ipAddress == "::1" || 
                   ipAddress == "localhost" ||
                   ipAddress.StartsWith("192.168.") ||
                   ipAddress.StartsWith("10.") ||
                   ipAddress.StartsWith("172.");
        }

        // Response models cho các API khác nhau
        private class IpApiResponse
        {
            public string? Status { get; set; }
            public decimal? Lat { get; set; }
            public decimal? Lon { get; set; }
        }

        private class IpApiCoResponse
        {
            public decimal? Latitude { get; set; }
            public decimal? Longitude { get; set; }
        }

        private class IpGeoLocationResponse
        {
            public decimal? Latitude { get; set; }
            public decimal? Longitude { get; set; }
        }
    }
}
