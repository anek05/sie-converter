# SIE till Excel Konverterare

En säker webbapplikation för att konvertera SIE-filer (SIE4-format) till Excel-format. Designad för finansiella företag med fokus på säkerhet och korrekt hantering av SIE-specifikationen.

## 🚀 Snabbstart

### Med Docker (rekommenderat)

```bash
docker-compose up -d
open http://localhost:8080
```

### Lokal utveckling

```bash
# Backend
cd src/backend
dotnet run

# Frontend (ny terminal)
cd src/frontend/public
npx http-server -p 8080
```

## ✨ Funktioner

- **🔒 Säker konvertering**: Filer sparas inte permanent på servern
- **📊 Full SIE4-support**: Komplett stöd för SIE4-specifikationen
- **⚙️ Anpassningsbar**: Anpassa kolumnnamn för olika företagsstandarder
- **📁 Valbar data**: Välj vilka delar av SIE-filen som ska exporteras
- **🏷️ Dimensioner**: Fullt stöd för SIE-dimensioner och objekt
- **🌐 Svenskt gränssnitt**: Anpassat för svenska redovisningsföretag

## 🔐 Säkerhetsfunktioner

| Funktion | Beskrivning |
|----------|-------------|
| **Ingen persistent lagring** | Filer processas i minnet eller säkra temporära filer |
| **Automatisk rensning** | Temporära filer raderas omedelbart efter konvertering |
| **Säker borttagning** | Filerna skrivs över innan de raderas |
| **Storleksbegränsning** | Max 50 MB filstorlek |
| **Inputvalidering** | Kontroll av filtyp och innehåll |
| **Säkerhetsheaders** | CSP, X-Frame-Options, X-Content-Type-Options |
| **Kryptografiska filnamn** | Slumpmässiga filnamn för temporära filer |

## 📋 SIE-format som stöds

### Header-information
- `#FLAGGA` - Filflagga
- `#FORMAT` - Teckenkodning (PC8/CP437)
- `#SIETYP` - SIE-version (4)
- `#PROGRAM` - Programinformation
- `#GEN` - Genereringsdatum

### Företagsinformation
- `#FNAMN` - Företagsnamn
- `#FNR` - Filnamn
- `#ORGNR` - Organisationsnummer
- `#ADRESS` - Adressinformation
- `#RAR` - Räkenskapsår
- `#VALUTA` - Valuta

### Konton och dimensioner
- `#KONTO` - Konton med namn
- `#KTYP` - Kontotyper (T, S, I, K)
- `#SRU` - SRU-koder för skatterapportering
- `#DIM` - Dimensioner (resultatenheter, projekt, etc.)
- `#OBJEKT` - Objekt inom dimensioner

### Saldo och resultat
- `#IB` - Ingående saldon
- `#UB` - Utgående saldon
- `#RES` - Resultat per konto

### Verifikationer
- `#VER` - Verifikationer (journalposter)
- `#TRANS` - Transaktioner med dimensioner, datum, kvantitet

## 🎨 Anpassade kolumnnamn

Du kan anpassa kolumnnamnen i Excel-exporten för att matcha ditt företags standard:

| Parameter | Standardvärde | Beskrivning |
|-----------|---------------|-------------|
| `accountNumberColumnName` | Kontonummer | Kontonummerkolumn |
| `accountNameColumnName` | Kontonamn | Kontonamnkolumn |
| `accountTypeColumnName` | Kontotyp | Kontotypkolumn |
| `verificationSeriesColumnName` | Serie | Verifikationsserie |
| `verificationNumberColumnName` | Verifikationsnummer | Verifikationsnummer |
| `verificationDateColumnName` | Datum | Verifikationsdatum |
| `verificationDescriptionColumnName` | Beskrivning | Verifikationsbeskrivning |
| `transactionAccountColumnName` | Konto | Transaktionskonto |
| `transactionAmountColumnName` | Belopp | Beloppskolumn |
| `transactionDateColumnName` | Transaktionsdatum | Transaktionsdatum |
| `transactionDescriptionColumnName` | Transaktionsbeskrivning | Transaktionsbeskrivning |
| `transactionQuantityColumnName` | Kvantitet | Kvantitetskolumn |
| `transactionDimensionsColumnName` | Dimensioner | Dimensionskolumn |

## 🔌 API-endpoints

### POST /api/conversion/convert

Konverterar en SIE-fil till Excel.

**Request**: `multipart/form-data`

```bash
curl -X POST http://localhost:5101/api/conversion/convert \
  -F "file=@SIE4 Exempelfil.SE" \
  -F "includeAccounts=true" \
  -F "includeVerifications=true" \
  -F "accountNumberColumnName=Kontonr" \
  -F "transactionAmountColumnName=Belopp" \
  --output output.xlsx
```

**Response**: Excel-fil (.xlsx)

### POST /api/conversion/validate

Validerar om en fil är en giltig SIE-fil.

```bash
curl -X POST http://localhost:5101/api/conversion/validate \
  -F "file=@SIE4 Exempelfil.SE"
```

**Response**:
```json
{
  "valid": true,
  "company": "Övningsbolaget AB",
  "accounts": 350,
  "verifications": 120,
  "version": "4"
}
```

### GET /api/conversion/options

Hämtar standardalternativ för export.

```bash
curl http://localhost:5101/api/conversion/options
```

## 🛠️ Konfiguration

### Miljövariabler

| Variabel | Beskrivning | Standardvärde |
|----------|-------------|---------------|
| `ASPNETCORE_ENVIRONMENT` | Miljö (Development/Production) | Production |
| `ASPNETCORE_URLS` | URLs att lyssna på | http://+:8080 |
| `EPPlus__LicenseContext` | EPPlus-licenstyp | NonCommercial |

### AppSettings

```json
{
  "EPPlus": {
    "LicenseContext": "NonCommercial"
  }
}
```

## 🧪 Testning

Kör testsriptet för att verifiera att allt fungerar:

```powershell
cd src/backend
.\test-converter.ps1
```

Detta testar:
1. Byggprocessen
2. API-start
3. Validering av exempelfilen
4. Konvertering till Excel
5. Uppstädning

## 📦 Projektstruktur

```
sie-converter/
├── src/
│   ├── backend/
│   │   ├── Controllers/
│   │   │   └── ConversionController.cs
│   │   ├── Models/
│   │   │   └── SieFile.cs
│   │   ├── Services/
│   │   │   ├── SieParserService.cs
│   │   │   ├── ExcelExportService.cs
│   │   │   └── TempFileService.cs
│   │   ├── Program.cs
│   │   └── Dockerfile
│   └── frontend/
│       └── public/
│           └── index.html
├── docker-compose.yml
├── nginx.conf
├── README.md
└── DEPLOYMENT.md
```

## 🔧 Teknisk information

### SIE-parsering

Parsern hanterar korrekt:
- PC8/CP437 teckenkodning (vanlig i SIE-filer)
- Citerade strängar med `"`
- Multipla dimensioner per transaktion `{1 Nord 6 0001}`
- Valfria fält i transaktioner (datum, beskrivning, kvantitet)
- Blockstruktur med `{}` för verifikationer

### Excel-export

- EPPlus 8 för Excel-generering
- Separata blad för: Företagsinfo, Konton, Transaktioner, Saldo, Resultat, Dimensioner, Objekt
- Formaterade rubriker och valuta
- Autopassning av kolumnbredder
- Möjlighet att platta ut transaktioner (en rad per transaktion)

### Säkerhet

- Temporära filer med kryptografiskt säkra filnamn
- Automatisk uppstädning vid fel
- Ingen loggning av känslig finansiell data
- Validering av all input
- Begränsning av filstorlek

## 📄 Licens

EPPlus används under NonCommercial-licens för icke-kommersiellt bruk. För kommersiell användning, vänligen köp en EPPlus-licens.

## 🤝 Bidra

Bidrag är välkomna! Vänligen:
1. Forka repot
2. Skapa en feature branch
3. Commita dina ändringar
4. Pusha till branchen
5. Öppna en Pull Request

## 🐛 Kända problem

- Varningar om nullable referenser (påverkar inte funktionalitet)
- EPPlus licensvarning i utvecklingsläge (förväntat beteende)

## 📞 Support

För frågor eller problem:
- Öppna en issue på GitHub
- Kontakta projektägaren

---

**Säker SIE-konvertering för svenska företag** 🇸🇪
