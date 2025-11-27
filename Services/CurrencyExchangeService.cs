using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Allva.Desktop.Models;

namespace Allva.Desktop.Services
{
    /// <summary>
    /// Servicio para obtener tasas de cambio de divisas en tiempo real
    /// </summary>
    public class CurrencyExchangeService
    {
        private readonly HttpClient _httpClient;
        private Dictionary<string, decimal> _cachedRates;
        private DateTime _lastUpdate;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(10);
        
        private const string API_BASE_URL = "https://cdn.jsdelivr.net/npm/@fawazahmed0/currency-api@latest/v1/currencies/eur.json";
        private const string API_FALLBACK_URL = "https://latest.currency-api.pages.dev/v1/currencies/eur.json";
        
        public CurrencyExchangeService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
            _cachedRates = new Dictionary<string, decimal>();
            _lastUpdate = DateTime.MinValue;
        }
        
        public async Task<Dictionary<string, decimal>> GetExchangeRatesAsync()
        {
            if (_cachedRates.Count > 0 && DateTime.Now - _lastUpdate < _cacheExpiration)
            {
                return _cachedRates;
            }
            
            try
            {
                var response = await _httpClient.GetStringAsync(API_BASE_URL);
                var rates = ParseApiResponse(response);
                
                if (rates.Count > 0)
                {
                    _cachedRates = rates;
                    _lastUpdate = DateTime.Now;
                    return rates;
                }
            }
            catch (Exception)
            {
                try
                {
                    var response = await _httpClient.GetStringAsync(API_FALLBACK_URL);
                    var rates = ParseApiResponse(response);
                    
                    if (rates.Count > 0)
                    {
                        _cachedRates = rates;
                        _lastUpdate = DateTime.Now;
                        return rates;
                    }
                }
                catch (Exception)
                {
                }
            }
            
            return GetDefaultRates();
        }
        
        private Dictionary<string, decimal> ParseApiResponse(string jsonResponse)
        {
            var rates = new Dictionary<string, decimal>();
            
            try
            {
                using var doc = JsonDocument.Parse(jsonResponse);
                var root = doc.RootElement;
                
                if (root.TryGetProperty("eur", out JsonElement eurRates))
                {
                    foreach (var property in eurRates.EnumerateObject())
                    {
                        string code = property.Name.ToUpper();
                        if (property.Value.TryGetDecimal(out decimal rate) && rate > 0)
                        {
                            rates[code] = 1m / rate;
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
            
            return rates;
        }
        
        public async Task<decimal> GetRateToEurAsync(string currencyCode)
        {
            var rates = await GetExchangeRatesAsync();
            
            if (rates.TryGetValue(currencyCode.ToUpper(), out decimal rate))
            {
                return rate;
            }
            
            var defaults = GetDefaultRates();
            if (defaults.TryGetValue(currencyCode.ToUpper(), out decimal defaultRate))
            {
                return defaultRate;
            }
            
            return 1m;
        }
        
        public async Task<decimal> ConvertToEurAsync(string currencyCode, decimal amount)
        {
            var rate = await GetRateToEurAsync(currencyCode);
            return amount * rate;
        }
        
        public async Task<List<FavoriteCurrencyModel>> GetFavoriteCurrenciesAsync(List<string> favoriteCodes)
        {
            var rates = await GetExchangeRatesAsync();
            var allCurrencies = AvailableCurrencies.GetAllCurrencies();
            var defaults = GetDefaultRates();
            var result = new List<FavoriteCurrencyModel>();
            
            foreach (var code in favoriteCodes)
            {
                var currency = allCurrencies.Find(c => c.Code == code);
                if (currency != null)
                {
                    decimal rate = 1m;
                    if (rates.TryGetValue(code.ToUpper(), out decimal apiRate))
                    {
                        rate = apiRate;
                    }
                    else if (defaults.TryGetValue(code.ToUpper(), out decimal defaultRate))
                    {
                        rate = defaultRate;
                    }
                    
                    result.Add(new FavoriteCurrencyModel
                    {
                        Code = currency.Code,
                        Name = currency.Name,
                        Country = currency.Country,
                        RateToEur = rate
                    });
                }
            }
            
            return result;
        }
        
        private Dictionary<string, decimal> GetDefaultRates()
        {
            return new Dictionary<string, decimal>
            {
                { "USD", 0.92m },
                { "CAD", 0.67m },
                { "CHF", 1.05m },
                { "GBP", 1.17m },
                { "CNY", 0.13m },
                { "JPY", 0.0061m },
                { "AUD", 0.60m },
                { "MXN", 0.052m },
                { "BRL", 0.17m },
                { "ARS", 0.0010m },
                { "CLP", 0.0010m },
                { "COP", 0.00023m },
                { "PEN", 0.25m },
                { "UYU", 0.022m },
                { "INR", 0.011m },
                { "KRW", 0.00067m },
                { "SGD", 0.69m },
                { "HKD", 0.12m },
                { "NZD", 0.56m },
                { "SEK", 0.087m },
                { "NOK", 0.084m },
                { "DKK", 0.13m },
                { "PLN", 0.23m },
                { "CZK", 0.041m },
                { "HUF", 0.0025m },
                { "RUB", 0.010m },
                { "TRY", 0.027m },
                { "ZAR", 0.051m },
                { "AED", 0.25m },
                { "SAR", 0.24m }
            };
        }
        
        public async Task RefreshRatesAsync()
        {
            _lastUpdate = DateTime.MinValue;
            await GetExchangeRatesAsync();
        }
        
        public DateTime GetLastUpdateTime()
        {
            return _lastUpdate;
        }
    }
}