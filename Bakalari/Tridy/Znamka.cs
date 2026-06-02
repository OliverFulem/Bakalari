using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Bakalari.Tridy;

/// <summary>
/// Představuje jednu konkrétní známku studenta.
/// Implementuje INotifyPropertyChanged, aby se UI automaticky aktualizovalo při změně hodnoty.
/// </summary>
public class Znamka : INotifyPropertyChanged
{
    private float _hodnota;
    private Predmet _predmet = null!;
    private DateOnly? _datum;
    private string _poznamka = string.Empty;
    private int _vaha = 1;

    /// <summary>Číselná hodnota známky (1 = výborný, 5 = nedostatečný).</summary>
    public float Hodnota
    {
        get => _hodnota;
        set { _hodnota = value; OnPropertyChanged(); }
    }

    /// <summary>Předmět, ke kterému tato známka patří.</summary>
    public Predmet Predmet
    {
        get => _predmet;
        set { _predmet = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Datum udělení známky. Nullable – starší záznamy datum nemusí mít.
    /// Změna data automaticky přepočítá i zobrazované texty DatumText a MetadataText.
    /// </summary>
    public DateOnly? Datum
    {
        get => _datum;
        set { _datum = value; OnPropertyChanged(); OnPropertyChanged(nameof(DatumText)); OnPropertyChanged(nameof(MetadataText)); }
    }

    /// <summary>Volitelná poznámka – téma testu, druh zkoušení apod.</summary>
    public string Poznamka
    {
        get => _poznamka;
        set { _poznamka = value; OnPropertyChanged(); OnPropertyChanged(nameof(MetadataText)); }
    }

    /// <summary>
    /// Váha známky pro výpočet váženého průměru (1–10).
    /// Například písemná práce může mít váhu 3, zatímco ústní odpověď váhu 1.
    /// </summary>
    public int Vaha
    {
        get => _vaha;
        set { _vaha = value; OnPropertyChanged(); OnPropertyChanged(nameof(VahaText)); }
    }

    /// <summary>Datum naformátované pro zobrazení v UI, např. "28. 5. 2026". Prázdný řetězec, pokud datum chybí.</summary>
    public string DatumText => _datum.HasValue ? _datum.Value.ToString("d. M. yyyy") : string.Empty;

    /// <summary>Váha naformátovaná pro zobrazení, např. "×3".</summary>
    public string VahaText => $"×{_vaha}";

    /// <summary>
    /// Kombinovaný text data a poznámky oddělených středníkem pro zobrazení pod názvem předmětu.
    /// Zobrazí se jen části, které mají hodnotu.
    /// </summary>
    public string MetadataText
    {
        get
        {
            if (_datum.HasValue && !string.IsNullOrWhiteSpace(_poznamka))
                return $"{DatumText} · {_poznamka}";
            if (_datum.HasValue)
                return DatumText;
            if (!string.IsNullOrWhiteSpace(_poznamka))
                return _poznamka;
            return string.Empty;
        }
    }

    /// <summary>
    /// Konstruktor používaný při načítání ze souboru JSON.
    /// Výchozí hodnoty zajišťují zpětnou kompatibilitu se staršími záznamy,
    /// které datum nebo váhu neobsahovaly.
    /// </summary>
    [JsonConstructor]
    public Znamka(float hodnota, Predmet predmet, DateOnly? datum = null, string? poznamka = null, int vaha = 1)
    {
        _hodnota = hodnota;
        _predmet = predmet;
        _datum = datum;
        _poznamka = poznamka ?? string.Empty;
        // Váhu uchováme vždy v povoleném rozsahu 1–10.
        _vaha = vaha < 1 ? 1 : vaha > 10 ? 10 : vaha;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Vyvolá událost PropertyChanged, která informuje UI o změně vlastnosti.
    /// Díky [CallerMemberName] není nutné název vlastnosti psát ručně.
    /// </summary>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
