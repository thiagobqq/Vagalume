using Microsoft.AspNetCore.Identity;

namespace Vagalume.Domain.Entities;

public class AppUser : IdentityUser<Guid>
{
    public string Nome { get; set; } = string.Empty;

    public ICollection<Candidatura> Candidaturas { get; set; } = new List<Candidatura>();
    public ICollection<Curriculo> Curriculos { get; set; } = new List<Curriculo>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
