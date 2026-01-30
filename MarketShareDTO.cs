using Org.BouncyCastle.Bcpg.OpenPgp;

namespace AGData.Test.MarketIntelligenceUS.DTOs
{
    public class MarketShareDTO
    {      
 
        public class ManufacturerMarketShareGraph
        {     
            public string Manufacturer { get; set; }
            public string Product { get; set; }
            public string GrowthFromQuantity { get; set; }
            public string GrowthFromPrice { get; set; }
            public string MarketShare { get; set; }
            public string MixEffect { get; set; }
            public string TotalGrowth { get; set; }
            public string ActiveIngredient { get; set; }
        }

        public class RetailerMarketShareGrowthGraph
        {
            public string Retailer { get; set; }
            public string GrowthFromQuantity { get; set; }
            public string GrowthFromPrice { get; set; }
            public string MarketShare { get; set; }
            public string MixEffect { get; set; }
            public string TotalGrowth { get; set; }      
        }

        public class ProductQuantityBarGraphDTO
        {
            public string Season { get; set; }
            public string Product { get; set; }
            public string Quantity { get; set; }           
            public string ActiveIngredient { get; set; }
            public string Manufacturer { get; set; }
            public string Retailer { get; set; }
        }

        public class RetailerQuantityBarGraphDTO
        {
            public string Season { get; set; }
            public string Retailer { get; set; }
            public string Quantity { get; set; }          
        }

        public class ManufacturerMarketShareBarChartDTO
        {           
            public string Season { get; set; }
            public string Product { get; set; }
            public string Manufacturer { get; set; }
            public string Quantity { get; set; }
        }

        public class RetailerMarketShareBarChartDTO
        {
            public string Season { get; set; }
            public string Product { get; set; }
            public string Supplier { get; set; }
            public string Retailer { get; set; }
            public string Quantity { get; set; }
        }

        public class MarketShareKPIDataBaseValues
        {
            public string KpiName { get; set; }
            public string Manufacturer { get; set; }
            public string Retailer { get; set; }
            public string Territory { get; set; }
            public string MonthName { get; set; }
            public string CY_Volume { get; set; }
            public string PY_Volume { get; set; }
            public string Growth { get; set; }
        }

        public class ManufacturerMarketShareTableDTO
        {       
            public string Manufacturer { get; set; }
            public string Retailer { get; set; }
            public string GrowthYoY { get; set; }
            public string ChangeInMarketSharePoints { get; set; }
            public string QuantityYTD { get; set; }
            public string QuantityPYTD { get; set; }
            public string QuantityPY { get; set; }
            public string MarketShareYTD { get; set; }
            public string MarketSharePYTD { get; set; }
            public string MarketSharePY { get; set; }
        }

        public class RetailerMarketShareTableDTO
        {
            public string Retailer { get; set; }
            public string GrowthYoY { get; set; }
            public string ChangeInMarketSharePoints { get; set; }
            public string QuantityYTD { get; set; }
            public string QuantityPYTD { get; set; }
            public string QuantityPY { get; set; }
            public string MarketShareYTD { get; set; }
            public string MarketSharePYTD { get; set; }
            public string MarketSharePY { get; set; }
        }


        public class PvmCalculationValuesDTO
        {
            public string MonthNumber { get; set; }
            public string Territory { get; set; }
            public string PublicBrandId { get; set; }
            public string ProductBrand {  get; set; }
            public string Productuomid { get; set; }
            public string RetailerName { get; set; }
            public string ManufacturerName { get; set; }
            public string ProductUomDesc { get; set; }
            public string QTY { get; set; }
            public string SALES { get; set; }
            public string SALES_PY { get; set; }
            public string SALES_CY { get; set; }
            public string SALES_YOY { get; set; }
            public string QTY_PY { get; set; }
            public string QTY_CY { get; set; }
            public string VolumeEffect { get; set; }
            public decimal VolumeDifference { get; set; }
            public decimal Price_PY {  get; set; }
            public decimal VolumeEffect2 { get; set; }
            public decimal Price_CY { get; set; }
            public decimal PriceEffect {  get; set; }
            public decimal MixEffect { get; set; }
        }

        public class PVMGrowthMetrics
        {
            public string Retailer { get; set; }
            public string ManufacturerName { get; set; }
            public string ProductBrand {  get; set; }
            public decimal GrowthFromQuantity { get; set; }
            public decimal GrowthFromPrice { get; set; }
            public decimal MixEffect { get; set; }
            public decimal TotalGrowth { get; set; }
        }
    }
}
