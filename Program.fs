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

// Handler nou pentru lista apartamentelor
let apartmentsHandler : HttpHandler =
    fun next ctx ->
        task {
            try
                let apartamente = Database.getApartments()
                printfn "Apartamente găsite: %d" (List.length apartamente)
                return! json {| apartamente = apartamente |} next ctx
            with
            | ex ->
                printfn "Eroare getApartments: %s" ex.Message
                return! json {| error = ex.Message; apartamente = [] |} next ctx
        }

let ResidentsHandler : HttpHandler =
    fun next ctx ->
        task {
            try
                let locatari = Database.getResidents()
                printfn "Locatari găsiți: %d" (List.length locatari)
                return! json {| locatari = locatari |} next ctx
            with
            | ex ->
                printfn "Eroare getResidents: %s" ex.Message
                return! json {| error = ex.Message; locatari = [] |} next ctx
        }

let addResidentsHandler : HttpHandler =
    fun next ctx ->
        task {
            try
                let! locatar = ctx.BindJsonAsync<Locatar>()
                printfn "Request adăugare locatar: %A" locatar
                
                // Validare de bază
                if String.IsNullOrWhiteSpace(locatar.nume) then
                    return! json {| success = false; error = "Numele este obligatoriu" |} next ctx
                elif String.IsNullOrWhiteSpace(locatar.cnp) then
                    return! json {| success = false; error = "CNP-ul este obligatoriu" |} next ctx
                elif locatar.cnp.Length <> 13 then
                    return! json {| success = false; error = "CNP-ul trebuie să aibă 13 cifre" |} next ctx
                elif locatar.varsta < 1 || locatar.varsta > 120 then
                    return! json {| success = false; error = "Vârsta trebuie să fie între 1 și 120 ani" |} next ctx
                elif String.IsNullOrWhiteSpace(locatar.apartament) then
                    return! json {| success = false; error = "Apartamentul este obligatoriu" |} next ctx
                else
                    let idGenerat = Database.addLocatar locatar
                    printfn "Locatar adăugat cu succes cu ID: %s" idGenerat
                    return! json {| success = true; message = "Locatar adăugat cu succes"; id = idGenerat |} next ctx
            with
            | ex ->
                printfn "Eroare addLocatar: %s" ex.Message
                return! json {| success = false; error = ex.Message |} next ctx
        }

let updateResidentsHandler : HttpHandler =
    fun next ctx ->
        task {
            try
                let! locatar = ctx.BindJsonAsync<Locatar>()
                printfn "Request editare locatar: %A" locatar
                
                // Validare de bază
                if String.IsNullOrWhiteSpace(locatar.nume) then
                    return! json {| success = false; error = "Numele este obligatoriu" |} next ctx
                elif String.IsNullOrWhiteSpace(locatar.cnp) then
                    return! json {| success = false; error = "CNP-ul este obligatoriu" |} next ctx
                elif locatar.cnp.Length <> 13 then
                    return! json {| success = false; error = "CNP-ul trebuie să aibă 13 cifre" |} next ctx
                elif locatar.varsta < 1 || locatar.varsta > 120 then
                    return! json {| success = false; error = "Vârsta trebuie să fie între 1 și 120 ani" |} next ctx
                elif String.IsNullOrWhiteSpace(locatar.apartament) then
                    return! json {| success = false; error = "Apartamentul este obligatoriu" |} next ctx
                else
                    Database.updateLocatar locatar
                    printfn "Locatar editat cu succes"
                    return! json {| success = true; message = "Locatar editat cu succes" |} next ctx
            with
            | ex ->
                printfn "Eroare updateLocatar: %s" ex.Message
                return! json {| success = false; error = ex.Message |} next ctx
        }

let deleteResidentsHandler : HttpHandler =
    fun next ctx ->
        task {
            try
                let! data = ctx.BindJsonAsync<{| cnp: string |}>()
                printfn "Request ștergere locatar cu CNP: %s" data.cnp
                
                if String.IsNullOrWhiteSpace(data.cnp) then
                    return! json {| success = false; error = "CNP-ul este obligatoriu" |} next ctx
                else
                    Database.deleteLocatar data.cnp
                    printfn "Locatar șters cu succes"
                    return! json {| success = true; message = "Locatar șters cu succes" |} next ctx
            with
            | ex ->
                printfn "Eroare deleteLocatar: %s" ex.Message
                return! json {| success = false; error = ex.Message |} next ctx
        }

let webApp =
    choose [
        route "/" >=> htmlFile "index.html"
        POST >=> route "/login" >=> loginHandler 
        GET >=> route "/cheltuieli/getTotalApartments" >=> totalApartmentsHandler
        GET >=> route "/cheltuieli/getTotalLocatari" >=> totalLocatariHandler
        GET >=> route "/cheltuieli/getTotalPlati" >=> totalPlatiHandler
        GET >=> route "/cheltuieli/getPlatiPerLuna" >=> platiPerLunaHandler
        GET >=> route "/locatari/getApartments" >=> apartmentsHandler
        GET >=> route "/locatari/getResidents" >=> ResidentsHandler
        POST >=> route "/locatari/add" >=> addResidentsHandler
        POST >=> route "/locatari/update" >=> updateResidentsHandler
        POST >=> route "/locatari/delete" >=> deleteResidentsHandler
    ]

let configureApp (app: IApplicationBuilder) =
    // CORS trebuie să fie primul middleware
    app.UseCors("AllowAll") |> ignore
    app.UseGiraffe webApp

let configureServices (services: IServiceCollection) =
    services.AddGiraffe() |> ignore
    // Configurare CORS mai permisivă
    services.AddCors(fun options ->
        options.AddPolicy("AllowAll", fun builder ->
            builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader()
            |> ignore)
    ) |> ignore

[<EntryPoint>]
let main args =
    printfn "Pornesc serverul pe portul 5176..."
    printfn "CORS configurat pentru toate originile"
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