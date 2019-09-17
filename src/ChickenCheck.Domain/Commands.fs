namespace ChickenCheck.Domain.Commands
open ChickenCheck.Domain


type CreateSession =
    { Email: Email 
      Password: Password }

type AddEgg =
    { ChickenId : ChickenId
      Date : Date }

type RemoveEgg =
    { ChickenId : ChickenId
      Date : Date }
