﻿namespace silicon_tokenProvider.Infrastructure.Models;

public class VerifyTokenRequest
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
}
