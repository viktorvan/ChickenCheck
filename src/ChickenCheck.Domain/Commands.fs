namespace ChickenCheck.Domain.Commands
open ChickenCheck.Domain


type CreateSession =
    { Email: Email 
      Password: Password }
