using BudgetMasterFinal.Data;
using Microsoft.EntityFrameworkCore;

namespace BudgetMasterFinal.Services
{
    public interface ICurrencyService
    {
        Task<string> GetCurrencySymbolAsync(int tenantId);
        Task<string> GetCurrencyCodeAsync(int tenantId);
        string GetCurrencySymbol(string currencyCode);
        string FormatAmount(decimal amount, string currencyCode);
    }

    public class CurrencyService : ICurrencyService
    {
        private readonly ApplicationDbContext _context;
        private readonly Dictionary<string, string> _currencySymbols;

        public CurrencyService(ApplicationDbContext context)
        {
            _context = context;
            _currencySymbols = new Dictionary<string, string>
            {
                { "PHP", "₱" },
                { "USD", "$" },
                { "EUR", "€" },
                { "GBP", "£" },
                { "JPY", "¥" },
                { "AUD", "A$" },
                { "CAD", "C$" },
                { "SGD", "S$" }
            };
        }

        public async Task<string> GetCurrencySymbolAsync(int tenantId)
        {
            var currencyCode = await GetCurrencyCodeAsync(tenantId);
            return GetCurrencySymbol(currencyCode);
        }

        public async Task<string> GetCurrencyCodeAsync(int tenantId)
        {
            var tenant = await _context.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == tenantId);
            
            return tenant?.CurrencyCode ?? "PHP";
        }

        public string GetCurrencySymbol(string currencyCode)
        {
            return _currencySymbols.TryGetValue(currencyCode, out var symbol) ? symbol : "₱";
        }

        public string FormatAmount(decimal amount, string currencyCode)
        {
            var symbol = GetCurrencySymbol(currencyCode);
            return $"{symbol}{amount:N2}";
        }
    }
}
