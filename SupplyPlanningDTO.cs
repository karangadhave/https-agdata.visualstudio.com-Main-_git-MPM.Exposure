
using MiniExcelLibs.Attributes;

namespace AGData.Test.MarketIntelligenceUS.DTOs
{
    public class SupplyPlanningDTO
    {      
        public class KPIValues
        {
            public string KpiName { get; set; }
            public string? Territory { get; set; }
            public string CurrentYearInventoryPercent { get; set; }
            public string PastYearInventoryPercent { get; set; }
            public string Growth { get; set; }
        }

        public class ManufacturerInventoryGraph
        {
            public string Season { get; set; }
            public string Manufacturer { get; set; }
            public string InventoryPercentOfSales { get; set; }
        }

        public class ProductInventoryGraph
        {
            public string Season { get; set; }
            public string Product { get; set; }
            public string InventoryPercentOfSales { get; set; }
            public string ActiveIngredients { get; set; }
            public string Manufacturer { get; set; }            
        }


        public class ManufacturerProductCategoryGraph
        {
            public string Season { get; set; }
            public string Category { get; set; }
            public string InventoryPercentOfSales { get; set; }
        }

        public class ManufacturerProductSubCategoryGraph
        {
            public string Season { get; set; }
            public string Category { get; set; }
            public string SubCategory { get; set; }
            public string InventoryPercentOfSales { get; set; }
        }

        public class ManufacturerProductCategoryGrowthGraph
        {            
            public string Category { get; set; }
            public string Growth { get; set; }
        }

        public class ManufacturerProductSubCategoryGrowthGraph
        {
            public string Category { get; set; }
            public string SubCategory { get; set; }
            public string Growth { get; set; }
        }

        public class InventoryTrendsQueryValues
        {
            public string ProductBrandDesc { get; set; }
            public string Season { get; set; }
            public string MonthId { get; set; }
            public string Price { get; set; }
        }
      
        public class InventoryKPIDataBaseValues
        {
            public string KpiName { get; set; }
            public string Manufacturer { get; set; }
            public string Product { get; set; }
            public string Category { get; set; }
            public string SubCategory { get; set; }
            public string Season { get; set;  }
            public string Territory { get; set; }
            public string MonthName { get; set; }
            public string CY_InventoryAsPercentOfSales { get; set; }
            public string PY_InventoryAsPercentOfSales { get; set; }
            public string Growth { get; set; }
        }

        public class MonthTrendsGraph
        {
            public string Season { get; set; }
            public string Month { get; set; }
            public int YearsFromCurrent { get; set; }
            public string InventoryPercentOfSales { get; set; }
            public string Growth { get; set; }
        }

        public class MonthTrendsTable
        {       
            public string Month { get; set; }
            public string PY_InventoryAsSales { get; set; }
            public string PY_Growth { get; set; }
            public string CY_InventoryAsSales { get; set; }
            public string CY_Growth { get; set; }         
        }

        public class MapDataBaseValues
        {
            public string Region { get; set; }
            public string Growth { get; set; }
            public string InventoryPercentOfSales { get; set; }
            public string PY_InventoryPercentOfSales { get; set; }
        }

        public class InventoryExportTable
        {
            public string? Product { get; set; }
            public string? Manufacturer { get; set; }
            public string? Region { get; set; }
            public string? Category { get; set; }
            [ExcelColumnName("Sub-Category")]
            public string? SubCategory { get; set; }
            public string TransactionType { get; set; }        
            public string Season { get; set; }
            public string? Date { get; set; }
            public string Uom { get; set; }
            [ExcelColumnName("Inventory as % of Sales")]
            public string InventoryPercentOfSales { get; set; }         
        }
    }
}
