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

// Funcție nouă pentru a obține lista apartamentelor pentru dropdown
let getApartments() =
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

    results |> List.ofSeq

let getResidents () =
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

    results |> List.ofSeq

// Funcție pentru generarea unui ID unic
let generateLocatarId() =
    "loc_" + System.Guid.NewGuid().ToString("N").Substring(0, 8)

let addLocatar (locatar: Locatar) =
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

let updateLocatar (locatar: Locatar) =
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

let deleteLocatar (cnp: string) =
    use conn = new MySqlConnection(connectionString)
    conn.Open()
    let query = "DELETE FROM Locatari WHERE CNP = @cnp"
    use cmd = new MySqlCommand(query, conn)
    cmd.Parameters.AddWithValue("@cnp", cnp) |> ignore
    
    printfn "Ștergere locatar cu CNP: %s" cnp
    let rowsAffected = cmd.ExecuteNonQuery()
    printfn "Rânduri afectate: %d" rowsAffected