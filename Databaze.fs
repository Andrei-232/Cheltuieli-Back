module Database

open MySql.Data.MySqlClient
open Models
open System.Data
open System

let connectionString = 
    "server=127.0.0.1;user=root;password=234221;database=cheltuei_bloc"

let validateUser (username: string) (password: string) =
    use conn = new MySqlConnection(connectionString)
    conn.Open()

    let query = "SELECT COUNT(*) FROM Utilizatori WHERE nume_utilizator = @username AND parola = @password"
    use cmd = new MySqlCommand(query, conn)
    cmd.Parameters.AddWithValue("@username", username) |> ignore
    cmd.Parameters.AddWithValue("@password", password) |> ignore

    let count = cmd.ExecuteScalar() :?> int64 |> int
    count > 0

let getTotalApartments() =
    use conn = new MySqlConnection(connectionString)
    conn.Open()

    let query = "SELECT COUNT(*) FROM Apartamente;"
    use cmd = new MySqlCommand(query, conn)
    let count = cmd.ExecuteScalar() :?> int64 |> int
    count

let getTotalLocatari() =
    use conn = new MySqlConnection(connectionString)
    conn.Open()

    let query = "SELECT COUNT(*) FROM Locatari;"
    use cmd = new MySqlCommand(query, conn)
    let count = cmd.ExecuteScalar() :?> int64 |> int
    count

let getTotalPlati() =
    use conn = new MySqlConnection(connectionString)
    conn.Open()

    let query = "SELECT COUNT(*) FROM Plati;"
    use cmd = new MySqlCommand(query, conn)
    let count = cmd.ExecuteScalar() :?> int64 |> int
    count

let getPlatiPerLuna() =
    try
        use conn = new MySqlConnection(connectionString)
        conn.Open()

        // Primul query - obține toate apartamentele
        let apartamenteQuery = "SELECT id_apartament, numar FROM Apartamente ORDER BY numar;"
        use apartamenteCmd = new MySqlCommand(apartamenteQuery, conn)
        use apartamenteReader = apartamenteCmd.ExecuteReader()

        let apartamente = ResizeArray<{| id: string; numar: int |}>()
        while apartamenteReader.Read() do
            apartamente.Add({|
                id = apartamenteReader.GetString("id_apartament")
                numar = apartamenteReader.GetInt32("numar")
            |})
        
        apartamenteReader.Close()

        // Al doilea query - obține plățile pentru anul curent
        let platiQuery = """
            SELECT 
                id_apartament,
                luna,
                SUM(suma) as total_suma
            FROM Plati 
            WHERE LEFT(luna, 4) = YEAR(CURDATE())
            GROUP BY id_apartament, luna
            ORDER BY id_apartament, luna;
        """
        
        use platiCmd = new MySqlCommand(platiQuery, conn)
        use platiReader = platiCmd.ExecuteReader()

        let platiDict = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<int, float>>()
        
        while platiReader.Read() do
            let idApartament = platiReader.GetString("id_apartament")
            let luna = platiReader.GetString("luna")
            let totalSuma = platiReader.GetDecimal("total_suma") |> float
            
            if not (platiDict.ContainsKey(idApartament)) then
                platiDict.[idApartament] <- System.Collections.Generic.Dictionary<int, float>()
            
            // Parsare luna din formatul 'YYYY-MM'
            if luna.Length >= 7 then
                let parts = luna.Split('-')
                if parts.Length >= 2 then
                    match System.Int32.TryParse(parts.[1]) with
                    | (true, lunaNumar) when lunaNumar >= 1 && lunaNumar <= 12 ->
                        platiDict.[idApartament].[lunaNumar] <- totalSuma
                    | _ -> ()

        platiReader.Close()

        // Construiește rezultatul final
        let rezultat = 
            apartamente
            |> Seq.map (fun apt ->
                let platiPeLuni = Array.create 12 0.0
                
                if platiDict.ContainsKey(apt.id) then
                    for kvp in platiDict.[apt.id] do
                        let lunaIndex = kvp.Key - 1
                        if lunaIndex >= 0 && lunaIndex < 12 then
                            platiPeLuni.[lunaIndex] <- kvp.Value
                
                {| 
                    numar = apt.numar
                    plati = platiPeLuni |> Array.toList 
                |})
            |> List.ofSeq

        rezultat
    with
    | ex ->
        printfn "Eroare în getPlatiPerLuna: %s" ex.Message
        []

// Funcție pentru a obține lista apartamentelor pentru dropdown
let getApartments() =
    try
        use conn = new MySqlConnection(connectionString)
        conn.Open()

        let query = "SELECT id_apartament, numar FROM Apartamente ORDER BY numar;"
        use cmd = new MySqlCommand(query, conn)
        use reader = cmd.ExecuteReader()

        let results = ResizeArray<{| id: string; numar: int |}>()

        while reader.Read() do
            results.Add({|
                id = reader.GetString("id_apartament")
                numar = reader.GetInt32("numar")
            |})

        printfn "Apartamente găsite: %d" (results.Count)
        results |> List.ofSeq
    with
    | ex ->
        printfn "Eroare în getApartments: %s" ex.Message
        []

let getResidents () =
    try
        use conn = new MySqlConnection(connectionString)
        conn.Open()

        // JOIN cu tabela Apartamente pentru a obține numărul apartamentului
        let query = """
            SELECT l.id_locatar, l.Nume, l.CNP, l.Varsta, l.Pensionar, l.id_apartament, a.numar as numar_apartament
            FROM Locatari l
            LEFT JOIN Apartamente a ON l.id_apartament = a.id_apartament
            ORDER BY l.Nume;
        """
        use cmd = new MySqlCommand(query, conn)
        use reader = cmd.ExecuteReader()

        let results = ResizeArray<{| nume: string; cnp: string; varsta: int; pensionar: bool; apartament: string; numarApartament: int option |}>()

        while reader.Read() do
            let numarApartament = 
                if reader.IsDBNull("numar_apartament") then 
                    None 
                else 
                    Some (reader.GetInt32("numar_apartament"))
            
            results.Add({|
                nume = reader.GetString("Nume")
                cnp = reader.GetString("CNP")
                varsta = reader.GetInt32("Varsta")
                pensionar = reader.GetBoolean("Pensionar")
                apartament = reader.GetString("id_apartament")
                numarApartament = numarApartament
            |})

        printfn "Locatari găsiți: %d" (results.Count)
        results |> List.ofSeq
    with
    | ex ->
        printfn "Eroare în getResidents: %s" ex.Message
        []

// Funcție pentru generarea unui ID unic
let generateLocatarId() =
    "loc_" + System.Guid.NewGuid().ToString("N").Substring(0, 8)

let addLocatar (locatar: Locatar) =
    try
        use conn = new MySqlConnection(connectionString)
        conn.Open()
        
        // Generează un ID unic pentru locatar
        let idLocatar = "loc_" + System.Guid.NewGuid().ToString("N").Substring(0, 8)
        
        let query = "INSERT INTO Locatari (id_locatar, Nume, CNP, Varsta, Pensionar, id_apartament) VALUES (@id_locatar, @nume, @cnp, @varsta, @pensionar, @apartament)"
        use cmd = new MySqlCommand(query, conn)
        cmd.Parameters.AddWithValue("@id_locatar", idLocatar) |> ignore
        cmd.Parameters.AddWithValue("@nume", locatar.nume) |> ignore
        cmd.Parameters.AddWithValue("@cnp", locatar.cnp) |> ignore
        cmd.Parameters.AddWithValue("@varsta", locatar.varsta) |> ignore
        cmd.Parameters.AddWithValue("@pensionar", locatar.pensionar) |> ignore
        cmd.Parameters.AddWithValue("@apartament", locatar.apartament) |> ignore
        
        printfn "Adăugare locatar cu ID generat automat: %s, Nume: %s, CNP: %s" idLocatar locatar.nume locatar.cnp
        cmd.ExecuteNonQuery() |> ignore
        idLocatar // Returnează ID-ul generat
    with
    | ex ->
        printfn "Eroare în addLocatar: %s" ex.Message
        reraise()

let updateLocatar (locatar: Locatar) =
    try
        use conn = new MySqlConnection(connectionString)
        conn.Open()
        let query = "UPDATE Locatari SET Nume = @nume, Varsta = @varsta, Pensionar = @pensionar, id_apartament = @apartament WHERE CNP = @cnp"
        use cmd = new MySqlCommand(query, conn)
        cmd.Parameters.AddWithValue("@nume", locatar.nume) |> ignore
        cmd.Parameters.AddWithValue("@cnp", locatar.cnp) |> ignore
        cmd.Parameters.AddWithValue("@varsta", locatar.varsta) |> ignore
        cmd.Parameters.AddWithValue("@pensionar", locatar.pensionar) |> ignore
        cmd.Parameters.AddWithValue("@apartament", locatar.apartament) |> ignore
        
        printfn "Editare locatar cu CNP: %s" locatar.cnp
        let rowsAffected = cmd.ExecuteNonQuery()
        printfn "Rânduri afectate: %d" rowsAffected
    with
    | ex ->
        printfn "Eroare în updateLocatar: %s" ex.Message
        reraise()

let deleteLocatar (cnp: string) =
    try
        use conn = new MySqlConnection(connectionString)
        conn.Open()
        let query = "DELETE FROM Locatari WHERE CNP = @cnp"
        use cmd = new MySqlCommand(query, conn)
        cmd.Parameters.AddWithValue("@cnp", cnp) |> ignore
        
        printfn "Ștergere locatar cu CNP: %s" cnp
        let rowsAffected = cmd.ExecuteNonQuery()
        printfn "Rânduri afectate: %d" rowsAffected
    with
    | ex ->
        printfn "Eroare în deleteLocatar: %s" ex.Message
        reraise()

// ===== FUNCȚII PENTRU APARTAMENTE =====

let getAllApartments() =
    try
        use conn = new MySqlConnection(connectionString)
        conn.Open()

        // Folosesc numele exacte ale coloanelor din schema ta
        let query = """
            SELECT id_apartament, numar, etaj, suprafata, numarul_camere, 
                   COALESCE(incalzire_centralizata, 0) as incalzire_centralizata, 
                   COALESCE(incalzire_autonoma, 0) as incalzire_autonoma
            FROM Apartamente 
            ORDER BY numar;
        """
        use cmd = new MySqlCommand(query, conn)
        use reader = cmd.ExecuteReader()

        let results = ResizeArray<{| id: string; numar: int; etaj: int; suprafata: string; numarCamere: int; incalzireCentralizata: bool; incalzireAutonoma: bool |}>()

        while reader.Read() do
            let incalzireCentralizata = 
                if reader.IsDBNull("incalzire_centralizata") then false
                else reader.GetBoolean("incalzire_centralizata")
            
            let incalzireAutonoma = 
                if reader.IsDBNull("incalzire_autonoma") then false
                else reader.GetBoolean("incalzire_autonoma")

            results.Add({|
                id = reader.GetString("id_apartament")
                numar = reader.GetInt32("numar")
                etaj = reader.GetInt32("etaj")
                suprafata = reader.GetString("suprafata") // CHAR(222) - string
                numarCamere = reader.GetInt32("numarul_camere")
                incalzireCentralizata = incalzireCentralizata
                incalzireAutonoma = incalzireAutonoma
            |})

        printfn "Apartamente găsite: %d" (results.Count)
        results |> List.ofSeq
    with
    | ex ->
        printfn "Eroare în getAllApartments: %s" ex.Message
        printfn "Stack trace: %s" ex.StackTrace
        []

let generateApartmentId() =
    "ap_" + System.Guid.NewGuid().ToString("N").Substring(0, 8)

let addApartment (apartament: Apartament) =
    try
        use conn = new MySqlConnection(connectionString)
        conn.Open()
        
        // Generează un ID unic pentru apartament
        let idApartament = generateApartmentId()
        
        // Query cu numele exacte ale coloanelor și bloc = NULL
        let query = """
            INSERT INTO Apartamente (id_apartament, numar, etaj, suprafata, numarul_camere, bloc, incalzire_centralizata, incalzire_autonoma) 
            VALUES (@id_apartament, @numar, @etaj, @suprafata, @numarul_camere, NULL, @incalzire_centralizata, @incalzire_autonoma)
        """
        use cmd = new MySqlCommand(query, conn)
        cmd.Parameters.AddWithValue("@id_apartament", idApartament) |> ignore
        cmd.Parameters.AddWithValue("@numar", apartament.numar) |> ignore
        cmd.Parameters.AddWithValue("@etaj", apartament.etaj) |> ignore
        cmd.Parameters.AddWithValue("@suprafata", apartament.suprafata.ToString()) |> ignore // Convert to string pentru CHAR(222)
        cmd.Parameters.AddWithValue("@numarul_camere", apartament.numarCamere) |> ignore
        cmd.Parameters.AddWithValue("@incalzire_centralizata", apartament.incalzireCentralizata) |> ignore
        cmd.Parameters.AddWithValue("@incalzire_autonoma", apartament.incalzireAutonoma) |> ignore
        
        printfn "Executare query INSERT pentru apartament: %s" query
        printfn "Parametri: ID=%s, Numar=%d, Etaj=%d, Suprafata=%f, Camere=%d, Central=%b, Autonom=%b" 
                idApartament apartament.numar apartament.etaj apartament.suprafata apartament.numarCamere apartament.incalzireCentralizata apartament.incalzireAutonoma
        
        let rowsAffected = cmd.ExecuteNonQuery()
        printfn "Apartament adăugat cu succes. Rânduri afectate: %d" rowsAffected
        idApartament // Returnează ID-ul generat
    with
    | ex ->
        printfn "Eroare detaliată în addApartment: %s" ex.Message
        printfn "Stack trace: %s" ex.StackTrace
        reraise()

let updateApartment (id: string) (apartament: Apartament) =
    try
        use conn = new MySqlConnection(connectionString)
        conn.Open()
        let query = """
            UPDATE Apartamente 
            SET numar = @numar, etaj = @etaj, suprafata = @suprafata, 
                numarul_camere = @numarul_camere, incalzire_centralizata = @incalzire_centralizata, 
                incalzire_autonoma = @incalzire_autonoma 
            WHERE id_apartament = @id_apartament
        """
        use cmd = new MySqlCommand(query, conn)
        cmd.Parameters.AddWithValue("@id_apartament", id) |> ignore
        cmd.Parameters.AddWithValue("@numar", apartament.numar) |> ignore
        cmd.Parameters.AddWithValue("@etaj", apartament.etaj) |> ignore
        cmd.Parameters.AddWithValue("@suprafata", apartament.suprafata.ToString()) |> ignore // Convert to string pentru CHAR(222)
        cmd.Parameters.AddWithValue("@numarul_camere", apartament.numarCamere) |> ignore
        cmd.Parameters.AddWithValue("@incalzire_centralizata", apartament.incalzireCentralizata) |> ignore
        cmd.Parameters.AddWithValue("@incalzire_autonoma", apartament.incalzireAutonoma) |> ignore
        
        printfn "Editare apartament cu ID: %s" id
        let rowsAffected = cmd.ExecuteNonQuery()
        printfn "Rânduri afectate: %d" rowsAffected
    with
    | ex ->
        printfn "Eroare în updateApartment: %s" ex.Message
        printfn "Stack trace: %s" ex.StackTrace
        reraise()

// ===== FUNCȚII PENTRU SERVICII =====

let getAllServicii() =
    try
        use conn = new MySqlConnection(connectionString)
        conn.Open()

        // Folosesc numele exacte ale coloanelor din schema ta
        let query = """
            SELECT id_serviciu, nume_serviciu, cod_serviciu
            FROM Servicii 
            ORDER BY nume_serviciu;
        """
        use cmd = new MySqlCommand(query, conn)
        use reader = cmd.ExecuteReader()

        let results = ResizeArray<{| id: string; nume: string; cod: string |}>()

        while reader.Read() do
            results.Add({|
                id = reader.GetString("id_serviciu")
                nume = reader.GetString("nume_serviciu")
                cod = reader.GetString("cod_serviciu")
            |})

        printfn "Servicii găsite: %d" (results.Count)
        results |> List.ofSeq
    with
    | ex ->
        printfn "Eroare în getAllServicii: %s" ex.Message
        printfn "Stack trace: %s" ex.StackTrace
        []

let generateServiciuId() =
    "serv_" + System.Guid.NewGuid().ToString("N").Substring(0, 8)

let addServiciu (serviciu: Serviciu) =
    try
        use conn = new MySqlConnection(connectionString)
        conn.Open()
        
        // Generează un ID unic pentru serviciu
        let idServiciu = generateServiciuId()
        
        // Query cu numele exacte ale coloanelor
        let query = """
            INSERT INTO Servicii (id_serviciu, nume_serviciu, cod_serviciu) 
            VALUES (@id_serviciu, @nume_serviciu, @cod_serviciu)
        """
        use cmd = new MySqlCommand(query, conn)
        cmd.Parameters.AddWithValue("@id_serviciu", idServiciu) |> ignore
        cmd.Parameters.AddWithValue("@nume_serviciu", serviciu.nume) |> ignore
        cmd.Parameters.AddWithValue("@cod_serviciu", serviciu.cod) |> ignore
        
        printfn "Executare query INSERT pentru serviciu: %s" query
        printfn "Parametri: ID=%s, Nume=%s, Cod=%s" idServiciu serviciu.nume serviciu.cod
        
        let rowsAffected = cmd.ExecuteNonQuery()
        printfn "Serviciu adăugat cu succes. Rânduri afectate: %d" rowsAffected
        idServiciu // Returnează ID-ul generat
    with
    | ex ->
        printfn "Eroare detaliată în addServiciu: %s" ex.Message
        printfn "Stack trace: %s" ex.StackTrace
        reraise()

let updateServiciu (id: string) (serviciu: Serviciu) =
    try
        use conn = new MySqlConnection(connectionString)
        conn.Open()
        let query = """
            UPDATE Servicii 
            SET nume_serviciu = @nume_serviciu, cod_serviciu = @cod_serviciu 
            WHERE id_serviciu = @id_serviciu
        """
        use cmd = new MySqlCommand(query, conn)
        cmd.Parameters.AddWithValue("@id_serviciu", id) |> ignore
        cmd.Parameters.AddWithValue("@nume_serviciu", serviciu.nume) |> ignore
        cmd.Parameters.AddWithValue("@cod_serviciu", serviciu.cod) |> ignore
        
        printfn "Editare serviciu cu ID: %s" id
        let rowsAffected = cmd.ExecuteNonQuery()
        printfn "Rânduri afectate: %d" rowsAffected
    with
    | ex ->
        printfn "Eroare în updateServiciu: %s" ex.Message
        printfn "Stack trace: %s" ex.StackTrace
        reraise()

let deleteServiciu (id: string) =
    try
        use conn = new MySqlConnection(connectionString)
        conn.Open()
        let query = "DELETE FROM Servicii WHERE id_serviciu = @id_serviciu"
        use cmd = new MySqlCommand(query, conn)
        cmd.Parameters.AddWithValue("@id_serviciu", id) |> ignore
        
        printfn "Ștergere serviciu cu ID: %s" id
        let rowsAffected = cmd.ExecuteNonQuery()
        printfn "Rânduri afectate: %d" rowsAffected
    with
    | ex ->
        printfn "Eroare în deleteServiciu: %s" ex.Message
        printfn "Stack trace: %s" ex.StackTrace
        reraise()

// ===== FUNCȚII PENTRU PLĂȚI =====

let getAllPlati() =
    try
        use conn = new MySqlConnection(connectionString)
        conn.Open()

        // JOIN cu tabela Apartamente și Servicii pentru a obține denumirile
        let query = """
            SELECT p.id_plata, p.id_apartament, p.id_serviciu, p.suma, p.luna,
                   a.numar as numar_apartament, s.nume_serviciu
            FROM Plati p
            LEFT JOIN Apartamente a ON p.id_apartament = a.id_apartament
            LEFT JOIN Servicii s ON p.id_serviciu = s.id_serviciu
            ORDER BY p.luna DESC, a.numar;
        """
        use cmd = new MySqlCommand(query, conn)
        use reader = cmd.ExecuteReader()

        let results = ResizeArray<{| id: string; idApartament: string; idServiciu: string; suma: float; luna: string; numarApartament: int option; numeServiciu: string option |}>()

        while reader.Read() do
            let numarApartament = 
                if reader.IsDBNull("numar_apartament") then None
                else Some (reader.GetInt32("numar_apartament"))
            
            let numeServiciu = 
                if reader.IsDBNull("nume_serviciu") then None
                else Some (reader.GetString("nume_serviciu"))

            results.Add({|
                id = reader.GetString("id_plata")
                idApartament = reader.GetString("id_apartament")
                idServiciu = reader.GetString("id_serviciu")
                suma = reader.GetDecimal("suma") |> float
                luna = reader.GetString("luna")
                numarApartament = numarApartament
                numeServiciu = numeServiciu
            |})

        printfn "Plăți găsite: %d" (results.Count)
        results |> List.ofSeq
    with
    | ex ->
        printfn "Eroare în getAllPlati: %s" ex.Message
        printfn "Stack trace: %s" ex.StackTrace
        []

let generatePlataId() =
    "plata_" + System.Guid.NewGuid().ToString("N").Substring(0, 8)

let addPlata (plata: Plata) =
    try
        use conn = new MySqlConnection(connectionString)
        conn.Open()
        
        // Generează un ID unic pentru plată
        let idPlata = generatePlataId()
        
        let query = """
            INSERT INTO Plati (id_plata, id_apartament, id_serviciu, suma, luna) 
            VALUES (@id_plata, @id_apartament, @id_serviciu, @suma, @luna)
        """
        use cmd = new MySqlCommand(query, conn)
        cmd.Parameters.AddWithValue("@id_plata", idPlata) |> ignore
        cmd.Parameters.AddWithValue("@id_apartament", plata.idApartament) |> ignore
        cmd.Parameters.AddWithValue("@id_serviciu", plata.idServiciu) |> ignore
        cmd.Parameters.AddWithValue("@suma", plata.suma) |> ignore
        cmd.Parameters.AddWithValue("@luna", plata.luna) |> ignore
        
        printfn "Executare query INSERT pentru plată: %s" query
        printfn "Parametri: ID=%s, Apartament=%s, Serviciu=%s, Suma=%f, Luna=%s" 
                idPlata plata.idApartament plata.idServiciu plata.suma plata.luna
        
        let rowsAffected = cmd.ExecuteNonQuery()
        printfn "Plată adăugată cu succes. Rânduri afectate: %d" rowsAffected
        idPlata // Returnează ID-ul generat
    with
    | ex ->
        printfn "Eroare detaliată în addPlata: %s" ex.Message
        printfn "Stack trace: %s" ex.StackTrace
        reraise()

let updatePlata (id: string) (plata: Plata) =
    try
        use conn = new MySqlConnection(connectionString)
        conn.Open()
        let query = """
            UPDATE Plati 
            SET id_apartament = @id_apartament, id_serviciu = @id_serviciu, 
                suma = @suma, luna = @luna 
            WHERE id_plata = @id_plata
        """
        use cmd = new MySqlCommand(query, conn)
        cmd.Parameters.AddWithValue("@id_plata", id) |> ignore
        cmd.Parameters.AddWithValue("@id_apartament", plata.idApartament) |> ignore
        cmd.Parameters.AddWithValue("@id_serviciu", plata.idServiciu) |> ignore
        cmd.Parameters.AddWithValue("@suma", plata.suma) |> ignore
        cmd.Parameters.AddWithValue("@luna", plata.luna) |> ignore
        
        printfn "Editare plată cu ID: %s" id
        let rowsAffected = cmd.ExecuteNonQuery()
        printfn "Rânduri afectate: %d" rowsAffected
    with
    | ex ->
        printfn "Eroare în updatePlata: %s" ex.Message
        printfn "Stack trace: %s" ex.StackTrace
        reraise()

let deletePlata (id: string) =
    try
        use conn = new MySqlConnection(connectionString)
        conn.Open()
        let query = "DELETE FROM Plati WHERE id_plata = @id_plata"
        use cmd = new MySqlCommand(query, conn)
        cmd.Parameters.AddWithValue("@id_plata", id) |> ignore
        
        printfn "Ștergere plată cu ID: %s" id
        let rowsAffected = cmd.ExecuteNonQuery()
        printfn "Rânduri afectate: %d" rowsAffected
    with
    | ex ->
        printfn "Eroare în deletePlata: %s" ex.Message
        printfn "Stack trace: %s" ex.StackTrace
        reraise()