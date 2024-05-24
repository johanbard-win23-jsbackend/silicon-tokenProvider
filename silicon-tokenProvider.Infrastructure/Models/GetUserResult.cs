namespace silicon_tokenProvider.Infrastructure.Models;

public class GetUserResult
{
    public int? StatusCode { get; set; }
    public string? UserId { get; set; }
    public string? Error { get; set; }
}
