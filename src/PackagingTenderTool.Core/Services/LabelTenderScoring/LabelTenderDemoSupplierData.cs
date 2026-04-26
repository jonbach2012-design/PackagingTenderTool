using PackagingTenderTool.Core.Models;

namespace PackagingTenderTool.Core.Services.LabelTenderScoring;

public static class LabelTenderDemoSupplierData
{
    public static IReadOnlyList<SupplierModel> Create()
    {
        // Scandi Standard / DSH-vibe demo set (6 suppliers).
        // Base spend is approximated as Price * QuantityLabels and targeted to ~1.2M–2.8M kr.
        return
        [
            new SupplierModel
            {
                SupplierId = "cc-pack",
                SupplierName = "CC Pack",
                LabelType = "Film label",
                Material = "PP film",
                Adhesive = "Permanent glue",
                Sites = ["DK-East"],
                QuantityLabels = 200_000m,
                MOQUnits = 55_000m,
                StartupCost = 0m,
                MonthlySupportCost = 0m,
                MoqPenaltyPct = 3m,
                LeadTimeWeeks = 4,
                LabelWeightGramsPerUnit = 0.52m,
                RecyclingGrade = RecyclingGrade.D,
                RecycledContentPercent = 18m,
                Price = 6.00m, // 1.20M
                Co2Impact = 2.10m,
                DeliveryTimeDays = 18m,
                Country = "DK",
                SiteCount = 1,
                CommercialScore = 82m,
                TechnicalScore = 74m,
                RegulatoryScore = 42m
            },
            new SupplierModel
            {
                SupplierId = "europrint",
                SupplierName = "EuroPrint",
                LabelType = "Film label",
                Material = "PET film",
                Adhesive = "Deep Freeze adhesive",
                Sites = ["DK-East"],
                QuantityLabels = 200_000m,
                MOQUnits = 130_000m,
                StartupCost = 80_000m,
                MonthlySupportCost = 1_500m,
                MoqPenaltyPct = 12m,
                LeadTimeWeeks = 7,
                LabelWeightGramsPerUnit = 0.58m,
                RecyclingGrade = RecyclingGrade.E,
                RecycledContentPercent = 5m,
                Price = 6.20m, // 1.24M
                Co2Impact = 2.40m,
                DeliveryTimeDays = 28m,
                Country = "DK",
                SiteCount = 1,
                CommercialScore = 92m,
                TechnicalScore = 58m,
                RegulatoryScore = 30m
            },
            new SupplierModel
            {
                SupplierId = "labelworks",
                SupplierName = "LabelWorks",
                LabelType = "Paper label",
                Material = "FSC paper",
                Adhesive = "Acrylic (water-based)",
                Sites = ["DK-West"],
                QuantityLabels = 220_000m,
                MOQUnits = 60_000m,
                StartupCost = 35_000m,
                MonthlySupportCost = 1_100m,
                MoqPenaltyPct = 5m,
                LeadTimeWeeks = 4,
                LabelWeightGramsPerUnit = 0.44m,
                RecyclingGrade = RecyclingGrade.B,
                RecycledContentPercent = 35m,
                Price = 7.30m, // 1.61M
                Co2Impact = 1.95m,
                DeliveryTimeDays = 14m,
                Country = "DK",
                SiteCount = 1,
                CommercialScore = 78m,
                TechnicalScore = 72m,
                RegulatoryScore = 75m
            },
            new SupplierModel
            {
                SupplierId = "nordtag",
                SupplierName = "NordTag",
                LabelType = "Film label",
                Material = "PP film",
                Adhesive = "Rubber (solvent-free)",
                Sites = ["NO-Jæren"],
                QuantityLabels = 200_000m,
                MOQUnits = 70_000m,
                StartupCost = 72_000m,
                MonthlySupportCost = 3_250m,
                MoqPenaltyPct = 9m,
                LeadTimeWeeks = 4,
                LabelWeightGramsPerUnit = 0.49m,
                RecyclingGrade = RecyclingGrade.C,
                RecycledContentPercent = 32m,
                Price = 12.00m, // 2.40M
                Co2Impact = 1.90m,
                DeliveryTimeDays = 12m,
                Country = "NO",
                SiteCount = 1,
                CommercialScore = 64m,
                TechnicalScore = 86m,
                RegulatoryScore = 70m
            },
            new SupplierModel
            {
                SupplierId = "optilabel",
                SupplierName = "OptiLabel",
                LabelType = "Film label",
                Material = "PP film",
                Adhesive = "Wash-off glue",
                Sites = ["DK-East", "SE-West"],
                QuantityLabels = 200_000m,
                MOQUnits = 28_000m,
                StartupCost = 55_000m,
                MonthlySupportCost = 2_000m,
                MoqPenaltyPct = 2m,
                LeadTimeWeeks = 3,
                LabelWeightGramsPerUnit = 0.46m,
                RecyclingGrade = RecyclingGrade.A,
                RecycledContentPercent = 45m,
                Price = 14.00m, // 2.80M
                Co2Impact = 1.60m,
                DeliveryTimeDays = 10m,
                Country = "DK",
                SiteCount = 2,
                CommercialScore = 66m,
                TechnicalScore = 82m,
                RegulatoryScore = 92m
            },
            new SupplierModel
            {
                SupplierId = "scanprint",
                SupplierName = "ScanPrint",
                LabelType = "Film label",
                Material = "PET film",
                Adhesive = "Deep Freeze adhesive",
                Sites = ["DK-East", "NO-Jæren"],
                QuantityLabels = 200_000m,
                MOQUnits = 85_000m,
                StartupCost = 62_000m,
                MonthlySupportCost = 1_250m,
                MoqPenaltyPct = 7m,
                LeadTimeWeeks = 5,
                LabelWeightGramsPerUnit = 0.55m,
                RecyclingGrade = RecyclingGrade.D,
                RecycledContentPercent = 28m,
                Price = 10.50m, // 2.10M
                Co2Impact = 2.05m,
                DeliveryTimeDays = 16m,
                Country = "DK",
                SiteCount = 2,
                CommercialScore = 74m,
                TechnicalScore = 71m,
                RegulatoryScore = 58m
            }
        ];
    }
}
