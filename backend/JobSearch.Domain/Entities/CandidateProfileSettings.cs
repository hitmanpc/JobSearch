namespace JobSearch.Domain.Entities;

/// <summary>
/// Single-row table that stores the candidate's profile settings.
/// Always use Id = 1.
/// </summary>
public sealed class CandidateProfileSettings
{
    public int Id { get; set; } = 1;
    public string ResumeText { get; set; } = string.Empty;
    public string? RemotiveCategory { get; set; }
    public string? RemotiveSearchText { get; set; }
    public int? RemotiveLimit { get; set; }
}
