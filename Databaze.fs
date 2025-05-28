module Database

open MySql.Data.MySqlClient
open Models
open System.Data

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
