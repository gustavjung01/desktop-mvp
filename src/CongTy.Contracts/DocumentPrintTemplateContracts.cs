namespace CongTy.Contracts;

public sealed record DocumentPrintTemplateFieldData
{
    public string Key { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public bool DefaultSelected { get; init; }
    public bool Required { get; init; }
}

public sealed record DocumentPrintTemplateData
{
    public string DocumentType { get; init; } = string.Empty;
    public string TemplateCode { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string PageSize { get; init; } = "A4";
    public int FontSizePercent { get; init; } = 100;
    public string[] VisibleFieldKeys { get; init; } = [];
    public DocumentPrintTemplateFieldData[] Fields { get; init; } = [];
    public string? Heading { get; init; }
    public string? Title { get; init; }
    public string? Subtitle { get; init; }
    public bool HeadingVisible { get; init; } = true;
    public string HeadingAlign { get; init; } = "left";
    public string TitleAlign { get; init; } = "right";
    public bool IsCustomized { get; init; }
    public string? UpdatedAt { get; init; }
}

public sealed record DocumentPrintTemplateUpdateRequest
{
    public string PageSize { get; init; } = "A4";
    public string[] VisibleFieldKeys { get; init; } = [];
    public string? Heading { get; init; }
    public bool HeadingVisible { get; init; } = true;
    public string HeadingAlign { get; init; } = "left";
    public string TitleAlign { get; init; } = "right";
    public string? ExpectedUpdatedAt { get; init; }
}

public sealed record DocumentPrintTemplateResetRequest
{
    public bool ResetToDefault { get; init; } = true;
    public string? ExpectedUpdatedAt { get; init; }
}
