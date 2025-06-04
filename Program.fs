open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open FSharp.Control
open System.Text.Json
open Giraffe
open Models

let loginHandler : HttpHandler =
    fun next ctx ->
        task {
            let! data = ctx.BindJsonAsync<LoginData>()
            let isValid = Database.validateUser data.username data.password

            if isValid then
                return! json {| success = true |} next ctx
            else
                return! json {| success = false; message = "Utilizator sau parolă greșită!" |} next ctx
        }

let totalApartmentsHandler : HttpHandler =
    fun next ctx ->
        task {
            try
                let total = Database.getTotalApartments()
                printfn "Total apartamente: %d" total
                return! json {| totalApartments = total |} next ctx
            with
            | ex ->
                printfn "Eroare getTotalApartments: %s" ex.Message
                return! json {| error = ex.Message |} next ctx
        }

let totalLocatariHandler : HttpHandler =
    fun next ctx ->
        task {
            try
                let total = Database.getTotalLocatari()
                printfn "Total locatari: %d" total
                return! json {| totalLocatari = total |} next ctx
            with
            | ex ->
                printfn "Eroare getTotalLocatari: %s" ex.Message
                return! json {| error = ex.Message |} next ctx
        }

let totalPlatiHandler : HttpHandler =
    fun next ctx ->
        task {
            try
                let total = Database.getTotalPlati()
                printfn "Total plăți: %d" total
                return! json {| totalPlati = total |} next ctx
            with
            | ex ->
                printfn "Eroare getTotalPlati: %s" ex.Message
                return! json {| error = ex.Message |} next ctx
        }

let platiPerLunaHandler : HttpHandler =
    fun next ctx ->
        task {
            try
                let apartamente = Database.getPlatiPerLuna()
                printfn "Plăți per lună: %A" apartamente
                return! json {| apartamente = apartamente |} next ctx
            with
            | ex ->
                printfn "Eroare getPlatiPerLuna: %s" ex.Message
                return! json {| error = ex.Message; apartamente = [] |} next ctx
        }

let ResidentsHandler : HttpHandler =
    fun next ctx ->
        task {
            let total = Database.getResidents()
            return! json {| locatari = total |} next ctx
        }

let addResidentsHandler : HttpHandler =
    fun next ctx ->
        task {
            let! locatar = ctx.BindJsonAsync<Locatar>()
            Database.addLocatar locatar
            return! json {| success = true |} next ctx
        }

let updateResidentsHandler : HttpHandler =
    fun next ctx ->
        task {
            let! locatar = ctx.BindJsonAsync<Locatar>()
            Database.updateLocatar locatar
            return! json {| success = true |} next ctx
        }

let deleteResidentsHandler : HttpHandler =
    fun next ctx ->
        task {
            let! data = ctx.BindJsonAsync<{| cnp: string |}>()
            Database.deleteLocatar data.cnp
            return! json {| success = true |} next ctx
        }

let webApp =
    choose [
        route "/" >=> htmlFile "index.html"
        POST >=> route "/login" >=> loginHandler 
        GET >=> route "/cheltuieli/getTotalApartments" >=> totalApartmentsHandler
        GET >=> route "/cheltuieli/getTotalLocatari" >=> totalLocatariHandler
        GET >=> route "/cheltuieli/getTotalPlati" >=> totalPlatiHandler
        GET >=> route "/cheltuieli/getPlatiPerLuna" >=> platiPerLunaHandler
        GET >=> route "/locatari/getResidents" >=> ResidentsHandler
        POST >=> route "/locatari/add" >=> addResidentsHandler
        POST >=> route "/locatari/update" >=> updateResidentsHandler
        POST >=> route "/locatari/delete" >=> deleteResidentsHandler
    ]

let configureApp (app: IApplicationBuilder) =
    app.UseCors("AllowAll") |> ignore
    app.UseGiraffe webApp

let configureServices (services: IServiceCollection) =
    services.AddGiraffe() |> ignore
    services.AddCors(fun options ->
    options.AddPolicy("AllowAll", fun builder ->
        builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader() |> ignore)
    ) |> ignore

[<EntryPoint>]
let main args =
    printfn "Pornesc serverul pe portul 5176..."
    Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(fun webHostBuilder ->
            webHostBuilder
                .UseUrls("http://localhost:5176")
                .Configure(configureApp)
                .ConfigureServices(configureServices)
                |> ignore)
        .Build()
        .Run()
    0