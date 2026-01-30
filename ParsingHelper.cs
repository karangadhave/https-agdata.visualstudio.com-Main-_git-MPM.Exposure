using System.Globalization;
using static AGData.Test.MarketIntelligenceUS.DTOs.Constants;
using System.Text.RegularExpressions;
using System.Linq.Expressions;
using System.Reflection;

namespace AGData.Test.MarketIntelligenceUS.Helpers
{
    public static class ParsingHelper
    {

        private static string QuoteAndJoin(string filter)
        {
            return string.Join(", ", filter.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => $"'{s.Trim()}'"));
        }

        public static string ParseAllQueryFilers(this string filter, string filterName)
        {
            switch (filterName)
            {
                case "Month":
                    List<string> monthIds = new();
                    var months = filter.Split(',').Select(x => x.Trim());

                    foreach (var month in months)
                    {
                        var monthTrim = string.Empty;

                        if (month.Length <= 3)
                        {
                            monthTrim = month;
                        }
                        else
                        {
                            monthTrim = month.Substring(0, 3).Trim();
                        }

                        monthIds.Add(DateTime.ParseExact(monthTrim, "MMM", CultureInfo.CurrentCulture).Month.ToString());
                    }

                    return string.Join(", ", monthIds);

                case "Product":
                    var parts = filter.Split(',')
                                        .Select(p => p.Trim())
                                        .ToList();

                    var results = new List<string>();
                    int i = 0;

                    while (i < parts.Count)
                    {
                        // If this part is "2", combine it with the next part
                        if (Regex.IsMatch(parts[i], @"^\d+$") && i + 1 < parts.Count)
                        {
                            string combined = $"{parts[i]},{parts[i + 1]}";
                            results.Add($"'{combined}'");
                            i += 2; // Skip next because we just used it
                        }
                        else
                        {
                            // Standalone product name
                            results.Add($"'{parts[i]}'");
                            i++;
                        }
                    }

                    return string.Join(", ", results);

                case "Manufacturer":
                case "Category":
                case "Retailer":
                case "Sub-Category":
                case "Uom":
                case "Moa Class":
                case "Moa Group":
                case "Season":
                case "Active Ingredient":
                case "Number of Active Ingredients":
                case "Region":
                case "TransactionType":
                    return QuoteAndJoin(filter);

                default:
                    return $"The Filter {filterName} has not been added to the ParseAllQueryFilters helper method";
            }
        }

        public static string GetQueryAppendStringBasedOnFilters(CommonFilters filters, List<string> attributes = null)
        {
            string monthsAppendToQuery = String.Empty;
            string productsAppendToQuery = String.Empty;
            string retailerAppendToQuery = String.Empty;
            string manufacturersAppendToQuery = String.Empty;
            string categoryAppendToQuery = String.Empty;
            string subCategoryAppendToQuery = String.Empty;
            string uomAppendToQuery = String.Empty;
            string moaClassAppendToQuery = String.Empty;
            string moaGroupAppendToQuery = String.Empty;
            string seasonAppendToQuery = String.Empty;
            string activeIngredientAppendToQuery = String.Empty;
            string numberOfActiveIngredientAppendToQuery = String.Empty;
            string regionsAppendToQuery = String.Empty;
            string transactionsAppendToQuery = String.Empty;
            string appendQuery;

            if (filters.Month != null)
            {
                monthsAppendToQuery = $"AND right([d].[MonthID], 2) IN ({filters.Month.ParseAllQueryFilers("Month")})";
            }
            if (filters.Product != null)
            {
                productsAppendToQuery = $"AND pb.ProductBrandDesc IN ({filters.Product.ParseAllQueryFilers("Product")})";
            }
            if (filters.Retailer != null)
            {
                if (filters.Retailer.Split(',').Count() == 1 && filters.Retailer.Trim() == "Others")
                {
                    retailerAppendToQuery = $"AND fs.DistributorAccountID != '1062065'--1052339"; //BAD Hardcoded Distributor Account Id needs to be dynamic
                }
                else if (filters.Retailer.Split(',').Count() == 1 && filters.Retailer.Trim() != "Others")
                {
                    retailerAppendToQuery = $"AND fs.DistributorAccountID = '1062065'--1052339"; //BAD Hardcoded Distributor Account Id needs to be dynamic
                }
            }
            if (filters.Manufacturer != null)
            {
                manufacturersAppendToQuery = $"AND m.ManufacturerDesc IN ({filters.Manufacturer.ParseAllQueryFilers("Manufacturer")})";
            }
            if (filters.Category != null && (attributes == null || attributes.Contains("Category")))
            {
                categoryAppendToQuery = $"AND pc.ProductCategoryDesc IN ({filters.Category.ParseAllQueryFilers("Category")})";
            }
            if (filters.SubCategory != null && (attributes == null || attributes.Contains("Sub-Category")))
            {
                subCategoryAppendToQuery = $"AND pc.ProductSubCategoryDesc IN ({filters.SubCategory.ParseAllQueryFilers("Sub-Category")})";
            }
            if (filters.Uom != null)
            {
                uomAppendToQuery = $"AND u.ProductUomDesc IN ({filters.Uom.ParseAllQueryFilers("Uom")})";
            }
            if (filters.MoaClass != null)
            {
                moaClassAppendToQuery = $"AND mc.MOAClassDesc IN ({filters.MoaClass.ParseAllQueryFilers("MoA Class")})";
            }
            if (filters.MoaGroup != null)
            {
                moaGroupAppendToQuery = $"AND mg.MoaGroupDesc IN ({filters.MoaGroup.ParseAllQueryFilers("MoA Group")})";
            }
            if (filters.Season != null)
            {
                seasonAppendToQuery = $"AND fs.MarketYearId IN ({filters.Season.ParseAllQueryFilers("Season")})";
            }
            if (filters.ActiveIngredient != null)
            {
                activeIngredientAppendToQuery = $"AND ai.ActiveIngredientDesc IN ({filters.ActiveIngredient.ParseAllQueryFilers("Active Ingredient")})";
            }
            if (filters.NumberOfActiveIngredients != null)
            {
                numberOfActiveIngredientAppendToQuery = $"HAVING Count(ai.ActiveIngredientDesc) = ({filters.NumberOfActiveIngredients.ParseAllQueryFilers("Number of Active Ingredients")})";
            }
            if (filters.Region != null && (attributes == null || attributes.Contains("Region")))
            {
                regionsAppendToQuery = $"AND t.TerritoryLevel1Name IN ({filters.Region.ParseAllQueryFilers("Region")})";
            }
            if (filters.TransactionType != null)
            {
                transactionsAppendToQuery = $"AND tt.ProductTransactionTypeDesc IN ({filters.TransactionType.ParseAllQueryFilers("TransactionType")})";
            }

            appendQuery = string.Join(" ",
                                 monthsAppendToQuery,
                                 productsAppendToQuery,
                                 manufacturersAppendToQuery,
                                 retailerAppendToQuery,
                                 categoryAppendToQuery,
                                 subCategoryAppendToQuery,
                                 uomAppendToQuery,
                                 moaClassAppendToQuery,
                                 moaGroupAppendToQuery,
                                 seasonAppendToQuery,
                                 activeIngredientAppendToQuery,
                                 numberOfActiveIngredientAppendToQuery,
                                 regionsAppendToQuery,
                                 transactionsAppendToQuery);
            return appendQuery;
        }

        public static ExportQueryParts GetAttributesBasedOnExportSettings(List<string> attributes)
        {
            var joins = new HashSet<string>();
            var baseColumns = new List<string>();
            var columns = new List<string>();
            var groupByColumns = new List<string>();

            foreach (string attribute in attributes)
            {
                switch (attribute)
                {
                    case "Product":
                        baseColumns.Add("ProductBrandDesc [Product]");
                        columns.Add("[Product]");
                        groupByColumns.Add("ProductBrandDesc");
                        break;

                    case "Manufacturer" or "Supplier":
                        baseColumns.Add("ManufacturerDesc Manufacturer");
                        columns.Add("Manufacturer");
                        groupByColumns.Add("ManufacturerDesc");
                        break;

                    case "Category":
                        joins.Add("JOIN vw_dim_ProductBrand_SubCategory (NOLOCK) AS pc ON fs.ProductBrandID = pc.ProductBrandID");
                        baseColumns.Add("ProductCategoryDesc Category");
                        columns.Add("Category");
                        groupByColumns.Add("ProductCategoryDesc");
                        break;

                    case "Sub-Category":
                        joins.Add("JOIN vw_dim_ProductBrand_SubCategory (NOLOCK) AS pc ON fs.ProductBrandID = pc.ProductBrandID");
                        baseColumns.Add("ProductSubCategoryDesc SubCategory");
                        columns.Add("SubCategory");
                        groupByColumns.Add("ProductSubCategoryDesc");
                        break;

                    case "Region":
                        joins.Add("INNER JOIN [dbo].[vw_Dim_TerritoryLevel1] t ON bc.TerritoryLevel1ID = t.[TerritoryIDLevel1ID]");
                        baseColumns.Add("TerritoryLevel1Name Region");
                        columns.Add("Region");
                        groupByColumns.Add("TerritoryLevel1Name");
                        break;

                    case "Date":
                        baseColumns.Add("FORMAT(CAST(CAST(fs.MonthID AS VARCHAR(6)) + '01' AS DATE), 'yyyy-MM') AS Date");
                        columns.Add("[Date]");
                        groupByColumns.Add("FORMAT(CAST(CAST(fs.MonthID AS VARCHAR(6)) + '01' AS DATE), 'yyyy-MM')");
                        break;               

                    case "Transaction Type":
                        joins.Add("JOIN Dim_ProductTransactionType ptt ON ptt.ProductTransactionTypeID = vp.ProductTransactionTypeID");
                        baseColumns.Add("ProductTransactionTypeDesc TransactionType");
                        columns.Add("TransactionType");
                        groupByColumns.Add("ProductTransactionTypeDesc");
                        break;

                    default:
                        break;
                }
            }

            return new ExportQueryParts
            {
                Joins = string.Join(" ", joins),
                BaseColumns = baseColumns.Any() ? ", " + string.Join(", ", baseColumns) : "",
                Columns = columns.Any() ? ", " + string.Join(", ", columns) : "",
                GroupByColumns = groupByColumns.Any() ? ", " + string.Join(", ", groupByColumns) : ""
            };
        }

        public static string BuildJoinConditionFromString(string commaSeparatedColumns, string leftAlias, string rightAlias)
        {
            if (string.IsNullOrWhiteSpace(commaSeparatedColumns))
                return string.Empty;

            var columns = commaSeparatedColumns
                .Split(',')
                .Select(col => col.Trim())
                .Where(col => !string.IsNullOrEmpty(col));

            return string.Join(" AND ", columns.Select(col =>
                $"{leftAlias}.{col} = {rightAlias}.{col}"));
        }


        public static ExportQueryParts SetQueryParts(string pageName)
        {
            string joins = String.Empty;
            string baseColumns = String.Empty;
            string columns = String.Empty;
            string groupByColumns = String.Empty;

            switch (pageName)
            {
                case "Manufacturer":
                    baseColumns = "ManufacturerDesc [Manufacturer]";
                    columns = "Manufacturer";
                    groupByColumns = "ManufacturerDesc";
                    break;

                case "Product":
                    baseColumns = "ProductBrandDesc [Product]";
                    columns = "Product";
                    groupByColumns = "ProductBrandDesc";
                    break;

                case "Region":
                    baseColumns = "TerritoryLevel1Name Territory";
                    columns = "Territory";
                    groupByColumns = "TerritoryLevel1Name";
                    break;

                case "Category":
                    joins = "JOIN vw_dim_ProductBrand_SubCategory (NOLOCK) AS pc ON fs.ProductBrandID = pc.ProductBrandID";
                    baseColumns = "ProductCategoryDesc Category";
                    columns = "Category";
                    groupByColumns = "ProductCategoryDesc";
                    break;

                case "Sub-Category":
                    joins = "JOIN vw_dim_ProductBrand_SubCategory (NOLOCK) AS pc ON fs.ProductBrandID = pc.ProductBrandID";
                    baseColumns = "ProductSubCategoryDesc SubCategory";
                    columns = "SubCategory";
                    groupByColumns = "ProductSubCategoryDesc";
                    break;

                case "Monthly":                    
                    baseColumns = "DATENAME(MONTH, CAST(CONCAT(d.MonthId, '01') AS DATE)) [MonthName]";
                    columns = "[MonthName]";
                    groupByColumns = "DATENAME(MONTH, CAST(CONCAT(d.MonthId, '01') AS DATE))";
                    break;

                case "Retailer":
                    baseColumns = "CASE WHEN ad.DistributorAccountId = 1052339 THEN ad.DistributorAccountName ELSE 'Others' END AS Retailer";
                    columns = "Retailer";
                    groupByColumns = "CASE WHEN ad.DistributorAccountId = 1052339 THEN ad.DistributorAccountName ELSE 'Others' END";
                    break;

                default:
                    break;
            }

            return new ExportQueryParts
            {
                Joins = !string.IsNullOrEmpty(joins) ? joins : "",
                BaseColumns = !string.IsNullOrEmpty(baseColumns) ? baseColumns : "",
                Columns = !string.IsNullOrEmpty(columns) ? columns : "",
                GroupByColumns = !string.IsNullOrEmpty(groupByColumns) ? groupByColumns : ""
            };
        }


        //For keeping only Top filters that the 'GetFiltersWithData' should account for
        public static T KeepOnly<T, TProp>(T source, Expression<Func<T, TProp>> propertyToKeep) where T : new()
        {
            var result = new T();
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            if (propertyToKeep.Body is MemberExpression memberExpr)
            {
                string keepName = memberExpr.Member.Name;

                foreach (var prop in props)
                {
                    if (prop.Name == keepName && prop.CanRead && prop.CanWrite)
                    {
                        prop.SetValue(result, prop.GetValue(source));
                    }
                    else if (prop.CanWrite)
                    {
                        prop.SetValue(result, null);
                    }
                }
            }

            return result;
        }

        public static string ModifyColumns(string commaSeparatedColumns, bool changeMarketYear = true)
        {
            if (string.IsNullOrWhiteSpace(commaSeparatedColumns))
                return string.Empty;

            var columns = commaSeparatedColumns
                .Split(',')
                .Select(col => col.Trim())
                .Where(col => !string.IsNullOrEmpty(col))
                .ToList();

            // Remove unwanted column
            columns.Remove("FORMAT(CAST(CAST(fs.MonthID AS VARCHAR(6)) + '01' AS DATE), 'yyyy-MM')");

            if (changeMarketYear)
            {
                // Modify MarketYearId
                for (int i = 0; i < columns.Count; i++)
                {
                    if (columns[i].Equals("MarketYearId", StringComparison.OrdinalIgnoreCase))
                    {
                        columns[i] = "MarketYearId + 1 AS MarketYearId"; // Apply transformation
                    }
                }
            }

            return string.Join(", ", columns);
        }


    }
}