using System.Text.Json;
using DestinoPeruBlazor.Models;
using Microsoft.JSInterop;

namespace DestinoPeruBlazor.Services;

public class CartService
{
    private const string Key = "destinoperu_cart";
    private readonly IJSRuntime _js;
    public List<CartItem> Items { get; private set; } = [];
    public event Action? OnChange;

    public CartService(IJSRuntime js) => _js = js;

    public async Task InitializeAsync()
    {
        try
        {
            var json = await _js.InvokeAsync<string?>("localStorage.getItem", Key);
            if (!string.IsNullOrWhiteSpace(json))
                Items = JsonSerializer.Deserialize<List<CartItem>>(json) ?? [];
        }
        catch { Items = []; }
    }

    public async Task AddAsync(TourDto tour, int quantity = 1)
    {
        var existing = Items.FirstOrDefault(i => i.TourId == tour.Id);
        if (existing is not null)
            existing.Quantity = Math.Min(existing.Quantity + quantity, tour.AvailableCapacity);
        else
            Items.Add(new CartItem
            {
                TourId = tour.Id,
                Slug = tour.Slug,
                Title = tour.Title,
                Department = tour.Department,
                Price = tour.Price,
                Quantity = Math.Clamp(quantity, 1, tour.AvailableCapacity)
            });
        await SaveAsync();
    }

    public async Task RemoveAsync(int tourId)
    {
        Items.RemoveAll(i => i.TourId == tourId);
        await SaveAsync();
    }

    public async Task ClearAsync()
    {
        Items.Clear();
        await SaveAsync();
    }

    private async Task SaveAsync()
    {
        try
        {
            await _js.InvokeVoidAsync("localStorage.setItem", Key, JsonSerializer.Serialize(Items));
        }
        catch { /* ignore */ }
        OnChange?.Invoke();
    }
}
