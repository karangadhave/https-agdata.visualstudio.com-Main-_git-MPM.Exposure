using MiniExcelLibs.Attributes;

namespace AGData.Test.MarketIntelligenceUS.DTOs
{
    public class PricingDTO
    {
        public class KPIValues
        {
            public string KpiName { get; set; }
            public string? TerritoryName { get; set; }
            public string CurrentYearPrice { get; set; }
            public string PastYearToDatePrice { get; set; }
            public string Growth { get; set; }
        }

        public class PriceTrendsGraph
        {
            public string Season { get; set; }
            public string Month { get; set; }
            public string AveragePrice { get; set; }
        }

        public class PriceTrendsQueryValues
        {
            public string ProductBrandDesc { get; set; }
            public string Season { get; set; }
            public string MonthId { get; set; }
            public string Price { get; set; }
        }

        public class ProductPriceTrendsTable
        {
            public string Product { get; set; }
            public string UoM { get; set; }
            public string Manufacturer { get; set; }
            public string ProductTransactionRange { get; set; }
            public string TransactionVolume { get; set; }
            public string ChangeInPriceYoY { get; set; }
            public string AvgPricePerUoM { get; set; }
            public string AvgPricePerAcre { get; set; }
            public string PriceVariance { get; set; }
        }

        public class KPIDataBaseValues
        {
            public string ProductBrandDesc { get; set; }
            public string TerritoryName { get; set; }
            public string CYTD_Price { get; set; }
            public string PYTD_Price { get; set; }
            public string Growth { get; set; }
        }

        public class MapDataBaseValues
        {
            public string Region { get; set; }
            public string Avg_Price_CY { get; set; }
            public string Product { get; set; }
            public string Price_Difference { get; set; }
            public string AI { get; set; }
            public string Manufacturer { get; set; }
        }

        public class ProductPriceAcreTable
        {
            public string Uom { get; set; }
            public string Product { get; set; }
            public string? Manufacturer { get; set; }
            public string? UseRate { get; set; }
            public string? CyAvgPrice { get; set; }
            public string? PriceVariance { get; set; }
            public string? ChangeInPriceYoy { get; set; }
        }

        public class ProductPricePerAcreFitlers
        {
            public string Product { get; set; }
            public string Manufacturer { get; set; }
            public string? Region { get; set; }
            public string Category { get; set; }
            public string SubCategory { get; set; }
            public string Uom { get; set; }
            public string MoaClass { get; set; }
            public string MoaGroup { get; set; }
            public string ActiveIngredient { get; set; }
            public string? Month { get; set; }
            public string? TransactionType { get; set; }
        }

        public class ProductPricePerAcreBarGraph
        {
            public string Season { get; set; }
            public string Product { get; set; }
            public string AveragePricePerAcre { get; set; }
            public string ActiveIngredients { get; set; }
            public string Manufacturer { get; set; }
        }

        public class PricePerUomExportFitlers
        {
            public string Product { get; set; }
            public string Manufacturer { get; set; }
            public string? Region { get; set; }
            public string Category { get; set; }
            public string SubCategory { get; set; }
            public string Uom { get; set; }
            public string MoaClass { get; set; }
            public string MoaGroup { get; set; }
            public string ActiveIngredient { get; set; }
            public string NumberOfActiveIngredients { get; set; }
            public string Season { get; set; }
            public string? Month { get; set; }
            public string? TransactionType { get; set; }
        }

        public class PricePerUomExportTable
        {
            public string? Product { get; set; }
            public string? Manufacturer { get; set; }
            public string? Category { get; set; }
            [ExcelColumnName("Sub-Category")]
            public string? SubCategory { get; set; }
            public string? Region { get; set; }
            public string? Date { get; set; }
            public string Season { get; set; }
            public string Uom { get; set; }
            [ExcelColumnName("Price per UoM")]
            public string PricePerUom { get; set; }
            [ExcelColumnName("Price Variance")]
            public string PriceVariance { get; set; }
        }

        public class PricePerAcreExportTable
        {
            public string? Product { get; set; }
            public string? Manufacturer { get; set; }
            public string? Category { get; set; }
            [ExcelColumnName("Sub-Category")]
            public string? SubCategory { get; set; }
            public string? Region { get; set; }
            public string? Date { get; set; }
            public string Season { get; set; }
            [ExcelColumnName("UoM")]
            public string Uom { get; set; }
            [ExcelColumnName("Use Rate (Units/Treated Acre)")]
            public string UseRate { get; set; }
            [ExcelColumnName("Price per Acre")]
            public string PricePerAcre { get; set; }
            [ExcelColumnName("Price Variance")]
            public string PriceVariance { get; set; }
        }

    }
}
