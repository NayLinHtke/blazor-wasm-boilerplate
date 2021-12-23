﻿using FSH.BlazorWebAssembly.Client.Infrastructure.ApiClient;
using FSH.BlazorWebAssembly.Shared.Authorization;
using Microsoft.AspNetCore.Components;

namespace FSH.BlazorWebAssembly.Client.Infrastructure.Authentication.Jwt;

public class JwtAuthenticationService : IAuthenticationService
{
    private readonly ITokensClient _tokensClient;
    private readonly JwtAuthenticationStateProvider _authStateProvider;
    private readonly NavigationManager _navigationManager;

    public JwtAuthenticationService(
        ITokensClient tokensClient,
        JwtAuthenticationStateProvider authStateProvider,
        NavigationManager navigationManager)
    {
        _tokensClient = tokensClient;
        _authStateProvider = authStateProvider;
        _navigationManager = navigationManager;
    }

    public AuthProvider ProviderType => AuthProvider.Jwt;

    public async Task<Result> LoginAsync(string tenantKey, TokenRequest request)
    {
        var result = await _tokensClient.GetTokenAsync(tenantKey, request);
        if (result.Succeeded)
        {
            string? token = result.Data?.Token;
            string? refreshToken = result.Data?.RefreshToken;

            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(refreshToken))
            {
                return new Result { Succeeded = false, Messages = new List<string>() { "Invalid token received." } };
            }

            await _authStateProvider.MarkUserAsLoggedInAsync(token, refreshToken);
        }

        return result;
    }

    public async Task LogoutAsync()
    {
        await _authStateProvider.MarkUserAsLoggedOutAsync();

        _navigationManager.NavigateTo("/login");
    }

    public async Task<ResultOfTokenResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        string? tenantKey = authState.User.GetTenant();
        if (string.IsNullOrWhiteSpace(tenantKey))
        {
            throw new InvalidOperationException("Can't refresh token when user is not logged in!");
        }

        var tokenResponse = await _tokensClient.RefreshAsync(tenantKey, request);
        if (tokenResponse.Succeeded && tokenResponse.Data is not null)
        {
            await _authStateProvider.SaveAuthTokens(tokenResponse.Data.Token, tokenResponse.Data.RefreshToken);
        }

        return tokenResponse;
    }
}