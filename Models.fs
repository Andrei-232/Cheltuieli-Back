module Models

type LoginData = {
    username: string
    password: string
}

type Locatar = {
    nume: string
    cnp: string
    varsta: int
    pensionar: bool
    apartament: string
}

type Apartament = {
    numar: int
    etaj: int
    suprafata: float
    numarCamere: int
    incalzireCentralizata: bool
    incalzireAutonoma: bool
}

type Serviciu = {
    nume: string
    cod: string
}

type Plata = {
    idApartament: string
    idServiciu: string
    suma: float
    luna: string
}