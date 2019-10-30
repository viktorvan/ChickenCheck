module ChickenCheck.Backend.Authentication

open ChickenCheck.Domain
open System.Security.Claims
open System.IdentityModel.Tokens.Jwt
open Microsoft.IdentityModel.Tokens
open System.Text
open System
open FsToolkit.ErrorHandling

let private securityKey (secret: string) = secret |> Encoding.UTF8.GetBytes |> SymmetricSecurityKey

let generateToken : GenerateToken = 
    fun tokenSecret username ->
        try
            let claims = [| Claim(JwtRegisteredClaimNames.Sub, username) |]
            let expires = Nullable(DateTime.UtcNow.AddDays(7.0))
            let notBefore = Nullable(DateTime.UtcNow)
            let signingCredentials = SigningCredentials(key = securityKey tokenSecret, algorithm = SecurityAlgorithms.HmacSha256)

            let token =
                JwtSecurityToken(
                    issuer = "chickencheck",
                    audience = "chickencheck",
                    claims = claims,
                    expires = expires,
                    notBefore = notBefore,
                    signingCredentials = signingCredentials)
            JwtSecurityTokenHandler().WriteToken(token)
            |> String1000.create "security token" |> Result.mapError (fun _ -> TokenGenerationFailed "invalid token length")
            |> Result.map ChickenCheck.Domain.SecurityToken 
        with exn -> 
            exn.Message
            |> TokenGenerationFailed
            |> Error

let private validateToken tokenSecret (SecurityToken token) =
    let tokenValidationParameters =
        let validationParams = TokenValidationParameters()
        validationParams.ValidAudience <- "chickencheck"
        validationParams.ValidIssuer <- "chickencheck"
        validationParams.RequireExpirationTime <- true
        validationParams.ValidateLifetime <- true
        validationParams.ValidateIssuerSigningKey <- true
        validationParams.ClockSkew <- TimeSpan.FromSeconds(1.)
        validationParams.IssuerSigningKey <- securityKey tokenSecret
        validationParams
    try
        let handler = JwtSecurityTokenHandler()
        let _ = handler.ValidateToken(token.Value, tokenValidationParameters, ref null)

        Ok ()
    with 
        | :? SecurityTokenInvalidLifetimeException | :? SecurityTokenExpiredException -> 
            UserTokenExpired |> Error
        | exn ->
            exn.Message |> TokenInvalid |> Error

type Validate<'T> = SecureRequest<'T> -> Result<'T, AuthenticationError>
let validate<'T> tokenSecret : Validate<'T> =
    fun request ->
        request.Token
        |> validateToken tokenSecret
        |> Result.map (fun _ -> request.Content)
