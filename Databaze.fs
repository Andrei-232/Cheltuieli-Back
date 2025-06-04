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

let getResidents () =
    use conn = new MySqlConnection(connectionString)
    conn.Open()

    let query = "SELECT Nume, CNP, Varsta, Pensionar, id_apartament FROM Locatari;"
    use cmd = new MySqlCommand(query, conn)
    use reader = cmd.ExecuteReader()

    let results = ResizeArray<Locatar>()

    while reader.Read() do
        results.Add({
            nume = reader.GetString("Nume")
            cnp = reader.GetString("CNP")
            varsta = reader.GetInt32("Varsta")
            pensionar = reader.GetBoolean("Pensionar")
            apartament = reader.GetString("id_apartament")
        })

    results |> List.ofSeq

let addLocatar (locatar: Locatar) =
    use conn = new MySqlConnection(connectionString)
    conn.Open()
    let query = "INSERT INTO Locatari (Nume, CNP, Varsta, Pensionar, id_apartament) VALUES (@nume, @cnp, @varsta, @pensionar, @apartament)"
    use cmd = new MySqlCommand(query, conn)
    cmd.Parameters.AddWithValue("@nume", locatar.nume) |> ignore
    cmd.Parameters.AddWithValue("@cnp", locatar.cnp) |> ignore
    cmd.Parameters.AddWithValue("@varsta", locatar.varsta) |> ignore
    cmd.Parameters.AddWithValue("@pensionar", locatar.pensionar) |> ignore
    cmd.Parameters.AddWithValue("@apartament", locatar.apartament) |> ignore
    cmd.ExecuteNonQuery() |> ignore

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
    cmd.ExecuteNonQuery() |> ignore

let deleteLocatar (cnp: string) =
    use conn = new MySqlConnection(connectionString)
    conn.Open()
    let query = "DELETE FROM Locatari WHERE CNP = @cnp"
    use cmd = new MySqlCommand(query, conn)
    cmd.Parameters.AddWithValue("@cnp", cnp) |> ignore
    cmd.ExecuteNonQuery() |> ignore