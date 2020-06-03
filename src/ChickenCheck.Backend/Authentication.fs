module ChickenCheck.Backend.Authentication

open ChickenCheck.Shared
open System.Security.Claims
open System.IdentityModel.Tokens.Jwt
open Microsoft.IdentityModel.Tokens
open System.Text
open System
open FsToolkit.ErrorHandling
open System.Security.Cryptography
open Microsoft.AspNetCore.Cryptography.KeyDerivation

let private securityKey (secret: string) = secret |> Encoding.UTF8.GetBytes |> SymmetricSecurityKey


let generateToken =
    fun tokenSecret username ->
        let claims = [| Claim(JwtRegisteredClaimNames.Sub, username) |]
        let expires = Nullable(DateTime.UtcNow.AddDays(7.0))
        let notBefore = Nullable(DateTime.UtcNow)
        let signingCredentials = SigningCredentials(key = securityKey tokenSecret, algorithm = SecurityAlgorithms.HmacSha256)

        let token =
            JwtSecurityToken(
                issuer = "activeautomation",
                audience = "activeautomation",
                claims = claims,
                expires = expires,
                notBefore = notBefore,
                signingCredentials = signingCredentials)
        JwtSecurityTokenHandler().WriteToken(token)
        |> SecurityToken.create

let private validateToken tokenSecret (SecurityToken token) =
    let tokenValidationParameters =
        let validationParams = TokenValidationParameters()
        validationParams.ValidAudience <- "activeautomation"
        validationParams.ValidIssuer <- "activeautomation"
        validationParams.RequireExpirationTime <- true
        validationParams.ValidateLifetime <- true
        validationParams.ValidateIssuerSigningKey <- true
        validationParams.ClockSkew <- TimeSpan.FromSeconds(1.)
        validationParams.IssuerSigningKey <- securityKey tokenSecret
        validationParams
    try
        JwtSecurityTokenHandler().ValidateToken(token, tokenValidationParameters, ref null)
        |> ignore
        |> Ok
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


module PasswordHash =
    let toBase64String = System.Convert.ToBase64String

    let private getSalt() =
        let salt : byte [] = Array.zeroCreate 16
        use rng = RandomNumberGenerator.Create()
        rng.GetBytes(salt)
        salt

    let private getHash salt pw =
        KeyDerivation.Pbkdf2
            ( password = pw
            , salt = salt
            , prf = KeyDerivationPrf.HMACSHA1
            , iterationCount = 10000
            , numBytesRequested = 256/8 )

    let create =
        fun (pw:Password) ->
            let salt = getSalt ()
            let hash = getHash salt pw.Val

            { Hash = hash
              Salt = salt }

    let verify =
        fun (hash, pw:Password) ->
            let actual = getHash hash.Salt pw.Val
            actual = hash.Hash


type ITokenService =
    abstract VerifyPasswordHash: PasswordHash * Password -> bool
    abstract GenerateUserToken: string -> ChickenCheck.Shared.SecurityToken

type TokenService(tokenSecret) =
    interface ITokenService with
        member this.VerifyPasswordHash(hash,pw) = PasswordHash.verify(hash, pw)
        member this.GenerateUserToken(username) = generateToken tokenSecret username

