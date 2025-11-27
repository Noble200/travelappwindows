using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Allva.Desktop.Models;
using Allva.Desktop.Services;

namespace Allva.Desktop.ViewModels
{
    public partial class CurrencyExchangePanelViewModel : ObservableObject
    {
        private readonly CurrencyExchangeService _currencyService;
        
        [ObservableProperty]
        private ObservableCollection<FavoriteCurrencyModel> _favoriteCurrencies = new();
        
        [ObservableProperty]
        private ObservableCollection<CurrencyModel> _availableCurrencies = new();
        
        [ObservableProperty]
        private CurrencyModel? _selectedCurrency;
        
        [ObservableProperty]
        private string _amountReceived = string.Empty;
        
        [ObservableProperty]
        private decimal _totalInEuros;
        
        [ObservableProperty]
        private decimal _currentRate;
        
        [ObservableProperty]
        private string _rateDisplayText = string.Empty;
        
        [ObservableProperty]
        private bool _isLoading;
        
        [ObservableProperty]
        private string _errorMessage = string.Empty;
        
        [ObservableProperty]
        private string _lastUpdateText = string.Empty;
        
        [ObservableProperty]
        private bool _showTransactionDialog;
        
        [ObservableProperty]
        private TransactionResult? _transactionResult;
        
        private List<string> _favoriteCodes;
        private string _localAddress = "Calle Principal 123, Ciudad";
        
        public CurrencyExchangePanelViewModel()
        {
            _currencyService = new CurrencyExchangeService();
            _favoriteCodes = new List<string> { "USD", "CAD", "CHF", "GBP", "CNY" };
            
            InitializeAsync();
        }
        
        private async void InitializeAsync()
        {
            await LoadDataAsync();
        }
        
        [RelayCommand]
        private async Task LoadDataAsync()
        {
            IsLoading = true;
            ErrorMessage = string.Empty;
            
            try
            {
                var allCurrencies = GetAllCurrenciesList();
                var rates = await _currencyService.GetExchangeRatesAsync();
                
                foreach (var currency in allCurrencies)
                {
                    if (rates.TryGetValue(currency.Code, out decimal rate))
                    {
                        currency.RateToEur = rate;
                    }
                    else
                    {
                        currency.RateToEur = 1m;
                    }
                    currency.DisplayText = $"{currency.Code} | {currency.Name}";
                }
                
                AvailableCurrencies = new ObservableCollection<CurrencyModel>(allCurrencies);
                
                await LoadFavoriteCurrenciesAsync();
                
                if (AvailableCurrencies.Count > 0 && SelectedCurrency == null)
                {
                    SelectedCurrency = AvailableCurrencies.First();
                }
                
                var lastUpdate = _currencyService.GetLastUpdateTime();
                LastUpdateText = lastUpdate > DateTime.MinValue 
                    ? $"Última actualización: {lastUpdate:HH:mm:ss}" 
                    : "Tasas predeterminadas";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al cargar datos: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        private List<CurrencyModel> GetAllCurrenciesList()
        {
            return new List<CurrencyModel>
            {
                new CurrencyModel { Code = "USD", Name = "Dólar USA", Country = "Estados Unidos" },
                new CurrencyModel { Code = "CAD", Name = "Dólar Canadiense", Country = "Canadá" },
                new CurrencyModel { Code = "CHF", Name = "Franco Suizo", Country = "Suiza" },
                new CurrencyModel { Code = "GBP", Name = "Libra Esterlina", Country = "Reino Unido" },
                new CurrencyModel { Code = "CNY", Name = "Yuan Chino", Country = "China" },
                new CurrencyModel { Code = "JPY", Name = "Yen Japonés", Country = "Japón" },
                new CurrencyModel { Code = "AUD", Name = "Dólar Australiano", Country = "Australia" },
                new CurrencyModel { Code = "MXN", Name = "Peso Mexicano", Country = "México" },
                new CurrencyModel { Code = "BRL", Name = "Real Brasileño", Country = "Brasil" },
                new CurrencyModel { Code = "ARS", Name = "Peso Argentino", Country = "Argentina" },
                new CurrencyModel { Code = "CLP", Name = "Peso Chileno", Country = "Chile" },
                new CurrencyModel { Code = "COP", Name = "Peso Colombiano", Country = "Colombia" },
                new CurrencyModel { Code = "PEN", Name = "Sol Peruano", Country = "Perú" },
                new CurrencyModel { Code = "UYU", Name = "Peso Uruguayo", Country = "Uruguay" },
                new CurrencyModel { Code = "INR", Name = "Rupia India", Country = "India" },
                new CurrencyModel { Code = "KRW", Name = "Won Surcoreano", Country = "Corea del Sur" },
                new CurrencyModel { Code = "SGD", Name = "Dólar Singapur", Country = "Singapur" },
                new CurrencyModel { Code = "HKD", Name = "Dólar Hong Kong", Country = "Hong Kong" },
                new CurrencyModel { Code = "NZD", Name = "Dólar Neozelandés", Country = "Nueva Zelanda" },
                new CurrencyModel { Code = "SEK", Name = "Corona Sueca", Country = "Suecia" },
                new CurrencyModel { Code = "NOK", Name = "Corona Noruega", Country = "Noruega" },
                new CurrencyModel { Code = "DKK", Name = "Corona Danesa", Country = "Dinamarca" },
                new CurrencyModel { Code = "PLN", Name = "Zloty Polaco", Country = "Polonia" },
                new CurrencyModel { Code = "CZK", Name = "Corona Checa", Country = "República Checa" },
                new CurrencyModel { Code = "HUF", Name = "Florín Húngaro", Country = "Hungría" },
                new CurrencyModel { Code = "RUB", Name = "Rublo Ruso", Country = "Rusia" },
                new CurrencyModel { Code = "TRY", Name = "Lira Turca", Country = "Turquía" },
                new CurrencyModel { Code = "ZAR", Name = "Rand Sudafricano", Country = "Sudáfrica" },
                new CurrencyModel { Code = "AED", Name = "Dírham EAU", Country = "Emiratos Árabes" },
                new CurrencyModel { Code = "SAR", Name = "Riyal Saudí", Country = "Arabia Saudita" }
            };
        }
        
        private async Task LoadFavoriteCurrenciesAsync()
        {
            var favorites = await _currencyService.GetFavoriteCurrenciesAsync(_favoriteCodes);
            FavoriteCurrencies = new ObservableCollection<FavoriteCurrencyModel>(favorites);
        }
        
        [RelayCommand]
        private async Task RefreshRatesAsync()
        {
            IsLoading = true;
            ErrorMessage = string.Empty;
            
            try
            {
                await _currencyService.RefreshRatesAsync();
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al actualizar: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        partial void OnSelectedCurrencyChanged(CurrencyModel? value)
        {
            if (value != null)
            {
                CurrentRate = value.RateToEur;
                RateDisplayText = $"1 {value.Code} = {value.RateToEur:F4} EUR";
                CalculateTotal();
            }
            else
            {
                RateDisplayText = string.Empty;
                TotalInEuros = 0;
            }
        }
        
        partial void OnAmountReceivedChanged(string value)
        {
            CalculateTotal();
        }
        
        private void CalculateTotal()
        {
            if (SelectedCurrency == null || string.IsNullOrWhiteSpace(AmountReceived))
            {
                TotalInEuros = 0;
                return;
            }
            
            var cleanAmount = AmountReceived.Replace(',', '.');
            if (decimal.TryParse(cleanAmount, System.Globalization.NumberStyles.Any, 
                System.Globalization.CultureInfo.InvariantCulture, out decimal amount))
            {
                TotalInEuros = Math.Round(amount * CurrentRate, 2);
            }
            else
            {
                TotalInEuros = 0;
            }
        }
        
        [RelayCommand]
        private void ConfirmTransaction()
        {
            if (SelectedCurrency == null || TotalInEuros <= 0)
            {
                ErrorMessage = "Por favor, seleccione una divisa e ingrese una cantidad válida.";
                return;
            }
            
            var cleanAmount = AmountReceived.Replace(',', '.');
            if (!decimal.TryParse(cleanAmount, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal amount))
            {
                ErrorMessage = "Cantidad no válida.";
                return;
            }
            
            TransactionResult = new TransactionResult
            {
                CurrencyCode = SelectedCurrency.Code,
                CurrencyName = SelectedCurrency.Name,
                AmountReceived = amount,
                AmountInEuros = TotalInEuros,
                ExchangeRate = CurrentRate,
                TransactionDate = DateTime.Now,
                TransactionTime = DateTime.Now.ToString("HH:mm:ss"),
                Location = _localAddress,
                TransactionId = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()
            };
            
            ShowTransactionDialog = true;
            ErrorMessage = string.Empty;
        }
        
        [RelayCommand]
        private void CloseTransactionDialog()
        {
            ShowTransactionDialog = false;
            TransactionResult = null;
            AmountReceived = string.Empty;
            TotalInEuros = 0;
        }
        
        [RelayCommand]
        private async Task AddToFavoritesAsync(CurrencyModel currency)
        {
            if (currency == null || _favoriteCodes.Contains(currency.Code))
                return;
            
            _favoriteCodes.Add(currency.Code);
            await LoadFavoriteCurrenciesAsync();
        }
        
        [RelayCommand]
        private async Task RemoveFromFavoritesAsync(FavoriteCurrencyModel currency)
        {
            if (currency == null || !_favoriteCodes.Contains(currency.Code))
                return;
            
            _favoriteCodes.Remove(currency.Code);
            await LoadFavoriteCurrenciesAsync();
        }
        
        public void SetLocalAddress(string address)
        {
            _localAddress = address;
        }
    }
}