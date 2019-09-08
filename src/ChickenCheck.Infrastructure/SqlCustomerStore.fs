module ChickenCheck.Infrastructure.SqlCustomerStore

open ChickenCheck.Domain
open Customer
open Seller
open ChickenCheck.Domain.Events
open FsToolkit.ErrorHandling
open FSharp.Data
open ChickenCheck.Infrastructure.SqlHelpers
open Store.Customer

type internal InsertCustomerSql = SqlCommandProvider<"
                            INSERT INTO Customer (Id, Name, SellerId, Created, LastModified)
                            VALUES (@Id, @Name, @SellerId, @Now, @Now)
                            ", DevConnectionString>

let internal insertCustomer (ConnectionString conn) : InsertCustomer = 
    fun ev -> 
        asyncResult {
            use cmd = new InsertCustomerSql(conn)
            return!
                cmd.AsyncExecute(ev.Id.Value, ev.Name.Value, ev.SellerId.Value, now())
                |> Async.Ignore
        }

type internal UpdateCustomerNameSql = SqlCommandProvider<"
                                UPDATE Customer
                                SET 
                                    Name = @Name,
                                    LastModified = @Now
                                WHERE Id = @Id
                                ", DevConnectionString>

type internal UpdateCustomerSellerSql = SqlCommandProvider<"
                                UPDATE Customer
                                SET 
                                    SellerId = @SellerId,
                                    LastModified = @Now
                                WHERE Id = @Id
                                ", DevConnectionString>

let internal updateCustomer (ConnectionString conn) : UpdateCustomer = 
    fun ev -> 
        asyncResult {
            match ev with
            | CustomerNameUpdated e ->
                use cmd = new UpdateCustomerNameSql(conn)
                return! cmd.AsyncExecute(e.NewName.Value, now(), e.Id.Value) |> Async.Ignore

            | CustomerSellerUpdated e ->
                use cmd = new UpdateCustomerSellerSql(conn)
                return! cmd.AsyncExecute(e.NewSellerId.Value, now(), e.Id.Value) |> Async.Ignore
        }

type internal DeleteCustomerSql = SqlCommandProvider<"
                                UPDATE Customer
                                SET 
                                    Deleted = @Now1,
                                    LastModified = @Now2
                                WHERE Id = @Id 
                                ", DevConnectionString>

let internal deleteCustomer (ConnectionString conn) : DeleteCustomer = 
    fun ev -> 
        asyncResult {
            use cmd = new DeleteCustomerSql(conn)
            return! cmd.AsyncExecute(now(), now(), ev.Id.Value) |> Async.Ignore
        }

type internal GetCustomerSql = SqlCommandProvider<"
                                SELECT TOP 2 Id, Name, SellerId FROM Customer
                                WHERE ID = @Id
                                AND Deleted IS NULL
                                ", DevConnectionString, SingleRow=true>

let getCustomer (ConnectionString conn) : GetCustomer = 
    let toDomain (entity:GetCustomerSql.Record) = 
        result {
            let id = entity.Id |> CustomerId
            let! name = entity.Name |> String200.create "Name" |> Result.mapError toDatabaseError
            let sellerId = entity.SellerId |> SellerId
            return  
                { Id = id
                  Name = name
                  Seller = sellerId } 
        }

    fun customerId -> 
        asyncResult {
            try
                use cmd = new GetCustomerSql(conn)
                let! res = cmd.AsyncExecute(customerId.Value)
                return! res |> Option.map toDomain |> Option.sequenceResult
            with exn -> return! exn.ToString() |> DatabaseError |> Error
        }
    
type internal GetCustomersSql = SqlCommandProvider<"
                                SELECT Id, Name, SellerId 
                                FROM Customer 
                                WHERE Deleted IS NULL
                                ORDER BY Name
                                ", DevConnectionString>

let getCustomers (ConnectionString conn) : GetCustomers =
    let toDomain (entity: GetCustomersSql.Record) =
        result {
            let id = entity.Id |> CustomerId
            let! name = entity.Name |> String200.create "Name"
            let sellerId = entity.SellerId |> SellerId
            return 
                { Id = id
                  Name = name
                  Seller = sellerId }
        } |> Result.mapError toDatabaseError

    fun () ->   
        asyncResult {
            try
                use cmd = new GetCustomersSql(conn)
                let! res = cmd.AsyncExecute() |> Async.map Seq.toList
                return! res |> List.map toDomain |> List.sequenceResultM
            with exn -> return! exn.ToString() |> DatabaseError |> Error
        }
