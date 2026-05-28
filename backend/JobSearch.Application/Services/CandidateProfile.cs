namespace JobSearch.Application.Services;

public sealed class CandidateProfile
{
    public IReadOnlyCollection<string> Skills { get; set; } =
    [
        "Angular",
        "React",
        ".NET",
        "C#",
        "Docker",
        "CI/CD",
        "GitHub Actions",
        "microfrontends",
        "single-spa",
        "SQL",
        "MongoDB"
    ];
}
