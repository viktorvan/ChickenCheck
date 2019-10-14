module ChickenCheck.PasswordHasher
open System.Security.Cryptography
open Microsoft.AspNetCore.Cryptography.KeyDerivation
open ChickenCheck.Domain

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
        , numBytesRequested = 256/8
        )

let hashPassword =
    fun (pw:Password) ->
        let salt = getSalt ()
        let hash = getHash salt pw.Value

        { Hash = hash
          Salt = salt }

let verifyPasswordHash =   
    fun (hash, pw:Password) ->
        let actual = getHash hash.Salt pw.Value
        actual = hash.Hash