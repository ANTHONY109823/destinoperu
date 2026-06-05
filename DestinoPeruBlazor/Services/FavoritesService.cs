using System.Text.Json;
using Microsoft.JSInterop;

namespace DestinoPeruBlazor.Services;

/// <summary>Lista de tours favoritos del usuario, persistida en localStorage (estilo "guardar" de Booking).</summary>
public class FavoritesService
{
    private const string Key = "destinoperu_favorites";
    private readonly IJSRuntime _js;
    private bool _loaded;

    public List<string> Slugs { get; private set; } = [];
    public int Count => Slugs.Count;
    public event Action? OnChange;

    public FavoritesService(IJSRuntime js) => _js = js;

    public async Task InitializeAsync()
    {
        if (_loaded) return;
        try
        {
            var json = await _js.InvokeAsync<string?>("localStorage.getItem", Key);
            if (!string.IsNullOrWhiteSpace(json))
                Slugs = JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch { Slugs = []; }
        _loaded = true;
    }

    public bool IsFavorite(string slug) => Slugs.Contains(slug);

    public async Task<bool> ToggleAsync(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return false;
        await InitializeAsync();
        var added = !Slugs.Contains(slug);
        if (added) Slugs.Add(slug); else Slugs.Remove(slug);
        await SaveAsync();
        OnChange?.Invoke();
        return added;
    }

    private async Task SaveAsync()
    {
        try { await _js.InvokeVoidAsync("localStorage.setItem", Key, JsonSerializer.Serialize(Slugs)); }
        catch { /* ignore */ }
    }
}
