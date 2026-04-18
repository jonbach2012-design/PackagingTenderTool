> [!NOTE]
> ## Bemærkning til underviser
> Dette projekt er udviklet som en del af en læringsproces i C# og WinForms.  
> Løsningen er bygget som en prototype med fokus på struktur, databehandling og beslutningsstøtte i forbindelse med evaluering af emballage-tenders.  
> Den version, der ligger i repository nu, er den aktuelle version, som ønskes vurderet.

# PackagingTenderTool

## Formål

Dette projekt er udviklet som en prototype til evaluering af leverandørtilbud (tenders) inden for emballage med fokus på **labels**.

Formålet er at kunne:

- importere data fra Excel
- evaluere leverandører på flere parametre
- beregne en samlet vægtet score
- klassificere leverandører efter anbefaling
- præsentere et samlet beslutningsgrundlag i en WinForms-brugerflade

---

## Funktionalitet

Applikationen kan:

- importere tender-data fra Excel
- håndtere forskellige talformater, fx `1,250.50` og `1.250,50`
- evaluere leverandører inden for:
  - Commercial
  - Technical
  - Regulatory
- anvende regulatoriske vurderinger inspireret af **PPWR/EPR**
- beregne samlet score baseret på vægtning
- klassificere leverandører som:
  - **Recommended**
  - **Conditional**
  - **Manual Review**
- markere manglende eller ugyldige data via **Manual Review flags**
- vise resultater i en grafisk brugerflade bygget i **WinForms**

---

## Teknologier

Projektet er udviklet med:

- C#
- .NET
- Windows Forms (WinForms)
- Excel-import
- Git og GitHub til versionsstyring

---

## Arkitektur

Projektet er opdelt i flere lag for at sikre struktur og genbrug:

- **UI-lag**  
  Håndterer brugerfladen i WinForms og viser resultaterne.

- **Application-lag**  
  Styrer programflow og binder UI sammen med forretningslogikken.

- **Domain / Logic-lag**  
  Indeholder regler for evaluering, scoring og klassificering.

- **Infrastructure / Data-lag**  
  Håndterer import og parsing af Excel-data.

Denne opdeling gør løsningen mere overskuelig og lettere at videreudvikle.

---

## Eksempel på vurderingsområder

Ved evaluering af leverandører arbejdes der blandt andet med:

- pris og kommercielle forhold
- tekniske krav og specifikationer
- regulatoriske forhold
- datavalidering og håndtering af usikkerhed
- samlet vægtet leverandørscore

---

## Output

Systemet leverer et samlet beslutningsgrundlag ved at:

- vise scorer pr. leverandør
- fremhæve dataproblemer
- klassificere leverandører efter anbefalet status
- understøtte hurtigere og mere ensartet tender-evaluering

---

## Versionsstyring

Projektet er versionsstyret med Git og GitHub.

Repository dokumenterer udviklingsforløbet fra idé og strukturering til prototype og forbedringer undervejs.

---

## Mulige videreudviklinger

Projektet kan senere udvides med fx:

- flere tender-typer end labels
- mere avanceret vægtning og scoring
- eksport af resultater til Excel eller PDF
- visualiseringer og dashboards
- integration til andre datakilder eller systemer

---