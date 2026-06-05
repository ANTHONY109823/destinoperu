using System.Text.Json;
using DestinoPeruAPI.Application.DTOs;
using DestinoPeruAPI.Domain.Entities;

namespace DestinoPeruAPI.Application.Common;

public static class TourContentMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static TourDto ToDto(Tour t, string partnerName) => new(
        t.Id, t.PartnerId, partnerName, t.Slug, t.Title, t.Description,
        t.MetaTitle, t.MetaDescription, t.Price, t.Location, t.Department,
        t.AdventureType, t.Date, t.Capacity, t.AvailableCapacity,
        t.ImageUrl, t.IsActive, t.CreatedAt,
        t.PuntoPartida, t.PuntoRetorno, t.HoraSalida, t.DuracionAproximada,
        DeserializeItinerary(t.ItinerarioJson),
        DeserializeStrings(t.QueIncluyeJson),
        DeserializeStrings(t.QueNoIncluyeJson),
        DeserializeStrings(t.QueLlevarJson),
        DeserializeStrings(t.GaleriaJson));

    public static void ApplyContent(Tour tour, TourContentInput? input)
    {
        if (input is null) return;
        if (input.PuntoPartida is not null) tour.PuntoPartida = input.PuntoPartida;
        if (input.PuntoRetorno is not null) tour.PuntoRetorno = input.PuntoRetorno;
        if (input.HoraSalida is not null) tour.HoraSalida = input.HoraSalida;
        if (input.DuracionAproximada is not null) tour.DuracionAproximada = input.DuracionAproximada;
        if (input.Itinerario is not null) tour.ItinerarioJson = SerializeItinerary(input.Itinerario);
        if (input.QueIncluye is not null) tour.QueIncluyeJson = SerializeStrings(input.QueIncluye);
        if (input.QueNoIncluye is not null) tour.QueNoIncluyeJson = SerializeStrings(input.QueNoIncluye);
        if (input.QueLlevar is not null) tour.QueLlevarJson = SerializeStrings(input.QueLlevar);
        if (input.Galeria is not null) tour.GaleriaJson = SerializeStrings(input.Galeria.Take(8).ToList());
    }

    public static void ApplyCreateDefaults(Tour tour, CreateTourRequest request)
    {
        tour.PuntoPartida = request.PuntoPartida;
        tour.PuntoRetorno = request.PuntoRetorno;
        tour.HoraSalida = request.HoraSalida;
        tour.DuracionAproximada = request.DuracionAproximada;
        tour.ItinerarioJson = SerializeItinerary(request.Itinerario);
        tour.QueIncluyeJson = SerializeStrings(request.QueIncluye);
        tour.QueNoIncluyeJson = SerializeStrings(request.QueNoIncluye);
        tour.QueLlevarJson = SerializeStrings(request.QueLlevar);
        var gallery = request.Galeria?.Take(8).ToList() ?? [];
        if (!string.IsNullOrWhiteSpace(request.ImageUrl) && !gallery.Contains(request.ImageUrl))
            gallery.Insert(0, request.ImageUrl);
        tour.GaleriaJson = gallery.Count > 0 ? SerializeStrings(gallery) : null;
    }

    public static List<TourItineraryStepDto> DeserializeItinerary(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<TourItineraryStepDto>>(json, JsonOptions) ?? [];
        }
        catch { return []; }
    }

    public static List<string> DeserializeStrings(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, JsonOptions)?
                .Where(s => !string.IsNullOrWhiteSpace(s)).ToList() ?? [];
        }
        catch { return []; }
    }

    public static string? SerializeItinerary(IReadOnlyList<TourItineraryStepDto>? steps) =>
        steps is null || steps.Count == 0 ? null : JsonSerializer.Serialize(steps, JsonOptions);

    public static string? SerializeStrings(IReadOnlyList<string>? items) =>
        items is null || items.Count == 0 ? null : JsonSerializer.Serialize(items.Where(s => !string.IsNullOrWhiteSpace(s)).ToList(), JsonOptions);
}
