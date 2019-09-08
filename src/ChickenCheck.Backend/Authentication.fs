module ChickenCheck.Backend.Authentication

open ChickenCheck.Domain
open System.Security.Claims
open System.IdentityModel.Tokens.Jwt
open Microsoft.IdentityModel.Tokens
open System.Text
open System
open FsToolkit.ErrorHandling

let private secret = "8d84e655-48cb-446d-a8e3-cfce107ff80d"
let private securityKey = secret |> Encoding.UTF8.GetBytes |> SymmetricSecurityKey

let generateToken username =
    let claims = [| Claim(JwtRegisteredClaimNames.Sub, username) |]
    let expires = Nullable(DateTime.UtcNow.AddHours(1.0))
    let notBefore = Nullable(DateTime.UtcNow)
    let signingCredentials = SigningCredentials(key = securityKey, algorithm = SecurityAlgorithms.HmacSha256)

    let token =
        JwtSecurityToken(
            issuer = "chickencheck",
            audience = "chickencheck",
            claims = claims,
            expires = expires,
            notBefore = notBefore,
            signingCredentials = signingCredentials)
    ChickenCheck.Domain.SecurityToken <| JwtSecurityTokenHandler().WriteToken(token)

let private validateToken (SecurityToken token) =
    let tokenValidationParameters =
        let validationParams = TokenValidationParameters()
        validationParams.ValidAudience <- "chickencheck"
        validationParams.ValidIssuer <- "chickencheck"
        validationParams.ValidateLifetime <- true
        validationParams.ValidateIssuerSigningKey <- true
        validationParams.IssuerSigningKey <- securityKey
        validationParams
    try
        let handler = JwtSecurityTokenHandler()
        let _ = handler.ValidateToken(token, tokenValidationParameters, ref null)
        Ok ()
    with
        | _ -> TokenInvalid |> Error

type Validate<'T> = SecureRequest<'T> -> Result<'T, AuthenticationError>
let validate : Validate<'T> =
    fun request ->
        request.Token
        |> validateToken
        |> Result.map (fun _ -> request.Content)
