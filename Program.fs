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

// ===== HANDLER-URI PENTRU APARTAMENTE =====

let getAllApartmentsHandler : HttpHandler =
    fun next ctx ->
        task {
            try
                printfn "=== Încărcare apartamente ==="
                let apartamente = Database.getAllApartments()
                printfn "Apartamente găsite: %d" (List.length apartamente)
                printfn "Apartamente: %A" apartamente
                return! json {| apartamente = apartamente |} next ctx
            with
            | ex ->
                printfn "Eroare getAllApartments: %s" ex.Message
                printfn "Stack trace: %s" ex.StackTrace
                return! json {| error = ex.Message; apartamente = [] |} next ctx
        }

let addApartmentHandler : HttpHandler =
    fun next ctx ->
        task {
            try
                let! apartament = ctx.BindJsonAsync<Apartament>()
                printfn "=== Request adăugare apartament ==="
                printfn "Apartament primit: %A" apartament
                
                // Validare de bază
                if apartament.numar <= 0 then
                    return! json {| success = false; error = "Numărul apartamentului trebuie să fie pozitiv" |} next ctx
                elif apartament.etaj < 0 then
                    return! json {| success = false; error = "Etajul nu poate fi negativ" |} next ctx
                elif apartament.suprafata <= 0.0 then
                    return! json {| success = false; error = "Suprafața trebuie să fie pozitivă" |} next ctx
                elif apartament.numarCamere <= 0 then
                    return! json {| success = false; error = "Numărul de camere trebuie să fie pozitiv" |} next ctx
                else
                    let idGenerat = Database.addApartment apartament
                    printfn "Apartament adăugat cu succes cu ID: %s" idGenerat
                    return! json {| success = true; message = "Apartament adăugat cu succes"; id = idGenerat |} next ctx
            with
            | ex ->
                printfn "Eroare addApartment: %s" ex.Message
                printfn "Stack trace: %s" ex.StackTrace
                return! json {| success = false; error = ex.Message |} next ctx
        }

let updateApartmentHandler : HttpHandler =
    fun next ctx ->
        task {
            try
                let! data = ctx.BindJsonAsync<{| id: string; apartament: Apartament |}>()
                printfn "=== Request editare apartament ==="
                printfn "Date primite: %A" data
                
                // Validare de bază
                if String.IsNullOrWhiteSpace(data.id) then
                    return! json {| success = false; error = "ID-ul apartamentului este obligatoriu" |} next ctx
                elif data.apartament.numar <= 0 then
                    return! json {| success = false; error = "Numărul apartamentului trebuie să fie pozitiv" |} next ctx
                elif data.apartament.etaj < 0 then
                    return! json {| success = false; error = "Etajul nu poate fi negativ" |} next ctx
                elif data.apartament.suprafata <= 0.0 then
                    return! json {| success = false; error = "Suprafața trebuie să fie pozitivă" |} next ctx
                elif data.apartament.numarCamere <= 0 then
                    return! json {| success = false; error = "Numărul de camere trebuie să fie pozitiv" |} next ctx
                else
                    Database.updateApartment data.id data.apartament
                    printfn "Apartament editat cu succes"
                    return! json {| success = true; message = "Apartament editat cu succes" |} next ctx
            with
            | ex ->
                printfn "Eroare updateApartment: %s" ex.Message
                printfn "Stack trace: %s" ex.StackTrace
                return! json {| success = false; error = ex.Message |} next ctx
        }

// ===== HANDLER-URI PENTRU SERVICII =====

let getAllServiciiHandler : HttpHandler =
    fun next ctx ->
        task {
            try
                printfn "=== Încărcare servicii ==="
                let servicii = Database.getAllServicii()
                printfn "Servicii găsite: %d" (List.length servicii)
                printfn "Servicii: %A" servicii
                return! json {| servicii = servicii |} next ctx
            with
            | ex ->
                printfn "Eroare getAllServicii: %s" ex.Message
                printfn "Stack trace: %s" ex.StackTrace
                return! json {| error = ex.Message; servicii = [] |} next ctx
        }

let addServiciuHandler : HttpHandler =
    fun next ctx ->
        task {
            try
                let! serviciu = ctx.BindJsonAsync<Serviciu>()
                printfn "=== Request adăugare serviciu ==="
                printfn "Serviciu primit: %A" serviciu
                
                // Validare de bază
                if String.IsNullOrWhiteSpace(serviciu.nume) then
                    return! json {| success = false; error = "Numele serviciului este obligatoriu" |} next ctx
                elif String.IsNullOrWhiteSpace(serviciu.cod) then
                    return! json {| success = false; error = "Codul serviciului este obligatoriu" |} next ctx
                else
                    let idGenerat = Database.addServiciu serviciu
                    printfn "Serviciu adăugat cu succes cu ID: %s" idGenerat
                    return! json {| success = true; message = "Serviciu adăugat cu succes"; id = idGenerat |} next ctx
            with
            | ex ->
                printfn "Eroare addServiciu: %s" ex.Message
                printfn "Stack trace: %s" ex.StackTrace
                return! json {| success = false; error = ex.Message |} next ctx
        }

let updateServiciuHandler : HttpHandler =
    fun next ctx ->
        task {
            try
                let! data = ctx.BindJsonAsync<{| id: string; serviciu: Serviciu |}>()
                printfn "=== Request editare serviciu ==="
                printfn "Date primite: %A" data
                
                // Validare de bază
                if String.IsNullOrWhiteSpace(data.id) then
                    return! json {| success = false; error = "ID-ul serviciului este obligatoriu" |} next ctx
                elif String.IsNullOrWhiteSpace(data.serviciu.nume) then
                    return! json {| success = false; error = "Numele serviciului este obligatoriu" |} next ctx
                elif String.IsNullOrWhiteSpace(data.serviciu.cod) then
                    return! json {| success = false; error = "Codul serviciului este obligatoriu" |} next ctx
                else
                    Database.updateServiciu data.id data.serviciu
                    printfn "Serviciu editat cu succes"
                    return! json {| success = true; message = "Serviciu editat cu succes" |} next ctx
            with
            | ex ->
                printfn "Eroare updateServiciu: %s" ex.Message
                printfn "Stack trace: %s" ex.StackTrace
                return! json {| success = false; error = ex.Message |} next ctx
        }

let deleteServiciuHandler : HttpHandler =
    fun next ctx ->
        task {
            try
                let! data = ctx.BindJsonAsync<{| id: string |}>()
                printfn "=== Request ștergere serviciu ==="
                printfn "ID serviciu: %s" data.id
                
                if String.IsNullOrWhiteSpace(data.id) then
                    return! json {| success = false; error = "ID-ul serviciului este obligatoriu" |} next ctx
                else
                    Database.deleteServiciu data.id
                    printfn "Serviciu șters cu succes"
                    return! json {| success = true; message = "Serviciu șters cu succes" |} next ctx
            with
            | ex ->
                printfn "Eroare deleteServiciu: %s" ex.Message
                printfn "Stack trace: %s" ex.StackTrace
                return! json {| success = false; error = ex.Message |} next ctx
        }

// ===== HANDLER-URI PENTRU PLĂȚI (FĂRĂ DELETE) =====

let getAllPlatiHandler : HttpHandler =
    fun next ctx ->
        task {
            try
                printfn "=== Încărcare plăți ==="
                let plati = Database.getAllPlati()
                printfn "Plăți găsite: %d" (List.length plati)
                printfn "Plăți: %A" plati
                return! json {| plati = plati |} next ctx
            with
            | ex ->
                printfn "Eroare getAllPlati: %s" ex.Message
                printfn "Stack trace: %s" ex.StackTrace
                return! json {| error = ex.Message; plati = [] |} next ctx
        }

let addPlataHandler : HttpHandler =
    fun next ctx ->
        task {
            try
                let! plata = ctx.BindJsonAsync<Plata>()
                printfn "=== Request adăugare plată ==="
                printfn "Plată primită: %A" plata
                
                // Validare de bază
                if String.IsNullOrWhiteSpace(plata.idApartament) then
                    return! json {| success = false; error = "Apartamentul este obligatoriu" |} next ctx
                elif String.IsNullOrWhiteSpace(plata.idServiciu) then
                    return! json {| success = false; error = "Serviciul este obligatoriu" |} next ctx
                elif plata.suma <= 0.0 then
                    return! json {| success = false; error = "Suma trebuie să fie pozitivă" |} next ctx
                elif String.IsNullOrWhiteSpace(plata.luna) then
                    return! json {| success = false; error = "Luna este obligatorie" |} next ctx
                else
                    let idGenerat = Database.addPlata plata
                    printfn "Plată adăugată cu succes cu ID: %s" idGenerat
                    return! json {| success = true; message = "Plată adăugată cu succes"; id = idGenerat |} next ctx
            with
            | ex ->
                printfn "Eroare addPlata: %s" ex.Message
                printfn "Stack trace: %s" ex.StackTrace
                return! json {| success = false; error = ex.Message |} next ctx
        }

let updatePlataHandler : HttpHandler =
    fun next ctx ->
        task {
            try
                let! data = ctx.BindJsonAsync<{| id: string; plata: Plata |}>()
                printfn "=== Request editare plată ==="
                printfn "Date primite: %A" data
                
                // Validare de bază
                if String.IsNullOrWhiteSpace(data.id) then
                    return! json {| success = false; error = "ID-ul plății este obligatoriu" |} next ctx
                elif String.IsNullOrWhiteSpace(data.plata.idApartament) then
                    return! json {| success = false; error = "Apartamentul este obligatoriu" |} next ctx
                elif String.IsNullOrWhiteSpace(data.plata.idServiciu) then
                    return! json {| success = false; error = "Serviciul este obligatoriu" |} next ctx
                elif data.plata.suma <= 0.0 then
                    return! json {| success = false; error = "Suma trebuie să fie pozitivă" |} next ctx
                elif String.IsNullOrWhiteSpace(data.plata.luna) then
                    return! json {| success = false; error = "Luna este obligatorie" |} next ctx
                else
                    Database.updatePlata data.id data.plata
                    printfn "Plată editată cu succes"
                    return! json {| success = true; message = "Plată editată cu succes" |} next ctx
            with
            | ex ->
                printfn "Eroare updatePlata: %s" ex.Message
                printfn "Stack trace: %s" ex.StackTrace
                return! json {| success = false; error = ex.Message |} next ctx
        }

// ELIMINAT deletePlataHandler - nu mai există funcționalitatea de ștergere pentru plăți

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
        // Endpoint-uri pentru apartamente (FĂRĂ DELETE)
        GET >=> route "/apartamente/getAll" >=> getAllApartmentsHandler
        POST >=> route "/apartamente/add" >=> addApartmentHandler
        POST >=> route "/apartamente/update" >=> updateApartmentHandler
        // Endpoint-uri pentru servicii
        GET >=> route "/servicii/getAll" >=> getAllServiciiHandler
        POST >=> route "/servicii/add" >=> addServiciuHandler
        POST >=> route "/servicii/update" >=> updateServiciuHandler
        POST >=> route "/servicii/delete" >=> deleteServiciuHandler
        // Endpoint-uri pentru plăți (FĂRĂ DELETE)
        GET >=> route "/plati/getAll" >=> getAllPlatiHandler
        POST >=> route "/plati/add" >=> addPlataHandler
        POST >=> route "/plati/update" >=> updatePlataHandler
        // ELIMINAT: POST >=> route "/plati/delete" >=> deletePlataHandler
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
    printfn "=== Pornesc serverul pe portul 5176 ==="
    printfn "CORS configurat pentru toate originile"
    printfn "Endpoint-uri disponibile:"
    printfn "  === APARTAMENTE ==="
    printfn "  GET  /apartamente/getAll"
    printfn "  POST /apartamente/add"
    printfn "  POST /apartamente/update"
    printfn "  === SERVICII ==="
    printfn "  GET  /servicii/getAll"
    printfn "  POST /servicii/add"
    printfn "  POST /servicii/update"
    printfn "  POST /servicii/delete"
    printfn "  === LOCATARI ==="
    printfn "  GET  /locatari/getResidents"
    printfn "  POST /locatari/add"
    printfn "  POST /locatari/update"
    printfn "  POST /locatari/delete"
    printfn "  === PLĂȚI (FĂRĂ DELETE) ==="
    printfn "  GET  /plati/getAll"
    printfn "  POST /plati/add"
    printfn "  POST /plati/update"
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