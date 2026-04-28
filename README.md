# 📦 Packaging Tender Decision Engine (PTD-E)

Emballageudbud afgøres stadig primært af én ting: enhedsprisen. Det er forkert – og det er dyrt.

PTD-E er en beslutningsmotor, der erstatter Excel-baseret udbudsevaluering med en struktureret, audit-klar og deterministisk proces. Den er bygget til de krav, emballagekategorien stiller i dag: præcis TCO, PPWR-compliance og robust håndtering af ufuldstændige data.

**Laveste pris vinder ikke automatisk. Regulatoriske og tekniske faktorer kan og bør overstige kortsigtede prisbesparelser.**

---

## Problemet det løser
En typisk emballageudbudsevaluering i Excel:
- Sammenligner enhedspriser – ikke reel totalomkostning.
- Overser EPR-afgifter og PPWR-strafomkostninger, der materialiserer sig 12-24 måneder efter kontraktindgåelse.
- Er afhængig af individuel ekspertviden, der ikke kan auditeres eller gentages.
- Giver ingen sporbarhed fra beslutning til datagrundlag.

---

## Forretningsværdi

**Total Cost of Ownership – ikke enhedspris**
Systemet indregner automatisk EPR-afgifter, CO₂-straffe og logistiktillæg baseret på faktiske spend-data. Det gør den "billige" leverandør synlig for, hvad den reelt koster.

**What-if simulering i realtid**
Interaktive vægtnings-sliders lader brugeren øjeblikkeligt se, hvordan leverandørrankingen ændrer sig, hvis genanvendelighed prioriteres over pris – eller omvendt. 

**Deterministisk håndtering af dårlig data**
Ufuldstændige tilbud "glider ikke igennem". Manglende data udløser automatisk maksimal straftakst. Indkøberen beskyttes mod skjulte compliance-risici.

---

## Evalueringsmodel
Tre dimensioner med konfigurerbare vægte pr. udbud:

| Dimension  | Standardvægt | Indhold |
|:-----------|:-------------|:-----------------------------------|
| Commercial | 30%          | Pris, spend-vægtet TCO |
| Technical  | 30%          | Specifikationsmatch, linjeevaluering |
| Regulatory | 40%          | EPR-afgifter, PPWR-score |

---

## Arkitektur & Dataflow

```mermaid
graph TD
    subgraph Input
        A[Leverandør Excel/JSON] --> B[Raw Data]
        C[Bruger: Vægt-sliders] --> D[Session Weights]
    end
    subgraph Engine
        B --> E{TcoEngineService}
        D --> E
        E --> F[TCO: Spend + Penalties]
        E --> G[Scoring: Relativ til bedste]
        E --> H[Breakdown: Forklaringsmodel]
    end
    F & G & H --> L((Beslutningstageren))



Teknologi,Rolle
.NET 10.0,Performance og C# 13
Blazor/Radzen,Real-time What-if via SignalR
NSubstitute,Test af logik uden Excel-afhængighed
GitHub Actions,Automatiseret CI/CD (Windows Runner)
Solution,PackagingTenderTool.sln


### Roadmap: Fremtidig Værdiskabelse
* **Fase 1:** Validering mod reel tender i produktionskategori.
* **Fase 2:** **Dynamic Indexation Library**. Integration til PIX, ICIS og Platts for hedging af råvarerisici.
* **Fase 3:** **Supplier Risk Library** & **Design for Circularity Index**.


### Pålidelighed: Golden Cases
74 deterministiske testscenarier verificerer, at motoren håndterer virkelighedens ufuldkomne data korrekt:
* **Zero Volume & Extreme Scaling**
* **Missing Data / Grades**
* **PPWR Toggles & Ranking Stability**

### Architectural Decision Records (ADR)
* **ADR 001 – SoC:** Al matematik isoleret i `TcoEngineService`. UI viser kun data.
* **ADR 002 – Deterministisk SVG:** `InvariantCulture` (FmtSvg) eliminerer fejl fra decimalkommaer.
* **ADR 003 – Blazor Frontend:** Migreret fra WinForms for real-time interaktivitet.
* **ADR 004 – Config Isolation:** Afgiftsdata er isoleret i `/config`.
* **ADR 005 – 80/20 Auditstrategi:** Fuld audit obligatorisk ved ændringer i formler.


### AI Governance (.cursorrules)
Projektet er født med arkitektoniske guardrails for AI-assisteret udvikling. ADR'erne fungerer som "Source of Truth" for alle fremtidige ændringer.

---

## Installation
1. `dotnet restore`
2. `dotnet test` (Verificér 74 tests)
3. `dotnet run --project src/PackagingTenderTool.Blazor`