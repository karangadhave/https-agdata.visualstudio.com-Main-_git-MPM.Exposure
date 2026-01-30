using OpenQA.Selenium;
using System.Security.Policy;

namespace AGData.Test.MarketIntelligenceUS.DTOs
{
    public class Constants
    {
        public class FilterSelections
        {
            public IWebElement SelectionBox { get; set; }
            public string FilterName { get; set; }
        }

        public class SelectedFilters
        {
            public string FilterTitle { get; set; }
            public string FilterSelectionsCsv { get; set; }
        }

        public class FilterOptionsWithData
        {
            public string Uom { get; set; }
            public string Product { get; set; }
            public string Region { get; set; }
            public string Month { get; set; }
        }

        public class CommonFilters
        {
            public string Uom { get; set; }
            public string Manufacturer { get; set; }
            //public string Supplier { get; set; }
            public string Retailer { get; set; }   
            public string Category { get; set; }
            public string SubCategory { get; set; }
            public string MoaClass { get; set; }
            public string MoaGroup { get; set; }
            public string ActiveIngredient { get; set; }
            public string NumberOfActiveIngredients { get; set; }
            public string Season { get; set; }
            public string Product { get; set; }
            public string? Region { get; set; }
            public string? Month { get; set; }
            public string? TransactionType { get; set; }
        }

        public class ExportQueryParts
        {
            public string Joins { get; set; } = string.Empty;
            public string BaseColumns { get; set; } = string.Empty;
            public string Columns {  get; set; } = string.Empty;
            public string GroupByColumns { get; set; } = string.Empty;
        }
    }
}
