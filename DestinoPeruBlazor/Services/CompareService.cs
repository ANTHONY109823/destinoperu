using System.Text.Json;
using Microsoft.JSInterop;

namespace DestinoPeruBlazor.Services;

public class CompareService
{
    private const string Key = "destinoperu_compare";
    private const int Max = 3;
    private readonly IJSRuntime _js;
    public List<string> Slugs { get; private set; } = [];

    public CompareService(IJSRuntime js) => _js = js;

    public async Task InitializeAsync()
    {
        try
        {
            var json = await _js.InvokeAsync<string?>("localStorage.getItem", Key);
            if (!string.IsNullOrWhiteSpace(json))
                Slugs = JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch { Slugs = []; }
    }

    public async Task<bool> ToggleAsync(string slug)
    {
        if (Slugs.Contains(slug))
            Slugs.Remove(slug);
        else
        {
            if (Slugs.Count >= Max) return false;
            Slugs.Add(slug);
        }
        await SaveAsync();
        return true;
    }

    private async Task SaveAsync()
    {
        try { await _js.InvokeVoidAsync("localStorage.setItem", Key, JsonSerializer.Serialize(Slugs)); }
        catch { /* ignore */ }
    }
}
