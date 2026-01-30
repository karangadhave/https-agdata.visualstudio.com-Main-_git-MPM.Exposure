using AGData.Test.Framework.Helpers;
using static AGData.Test.MarketIntelligenceUS.Helpers.ParsingHelper;
using static AGData.Test.MarketIntelligenceUS.DTOs.PricingDTO;
using static AGData.Test.MarketIntelligenceUS.DTOs.Constants;
using Microsoft.Azure.Amqp.Framing;
using static AGData.Test.MarketIntelligenceUS.DTOs.SupplyPlanningDTO;
using static Raven.Client.Constants.Documents.PeriodicBackup;
using Raven.Client.Documents.Queries.Timings;
using static AGData.Test.MarketIntelligenceUS.DTOs.MarketShareDTO;
using Org.BouncyCastle.Asn1.Cms;


namespace AGData.Test.MarketIntelligenceUS.Helpers
{
    public static class UsMiDBHelper
    {
        public static List<KPIDataBaseValues> GetPricingKPIValuesBasedOnProductName(CommonFilters filters, bool includeUseRates = false, bool onlyMaskedProducts = false)
        {
            string maskedProducts = string.Empty;
            string useRate = "1";
            var appendQuery = GetQueryAppendStringBasedOnFilters(filters);

            if (onlyMaskedProducts)
            {
                maskedProducts = "AND bc.Pricing_DisplayIndicator = 'Mask'";
            }

            if (includeUseRates)
            {
                useRate = "ur.UseRate";
            }

            var TotalDataQuery =
                $@" WITH CY AS
                    (
                    Select DISTINCT ProductBrandDesc, CASE WHEN TerritoryLevel1Name IS NULL THEN 'Total' ELSE TerritoryLevel1Name END TerritoryLevel1Name, AVG(Price*{useRate}) Price
                    FROM vw_Fact_Sales fs 
                    INNER JOIN vw_BrandSecurity_County bc ON bc.ProductBrandId = fs.ProductBrandID AND bc.MarketYearId = fs.MarketYearID AND bc.CountyId = fs.CountyID
                    JOIN [dbo].[Dim_DateDay] d ON fs.DayID = d.DateID
                    INNER JOIN vw_Dim_ProductBrand pb ON pb.ProductBrandId = bc.Pricing_Masked_ProductBrandId
                    INNER JOIN vw_Dim_Products p ON p.ProductBrandId = pb.ProductBrandId
                    INNER JOIN vw_Dim_ProductUom u ON u.ProductUomId = p.ProductUOMId
                    INNER JOIN [dbo].[vw_Dim_TerritoryLevel1] t on bc.[TerritoryLevel1ID] = t.[TerritoryIDLevel1ID]
                    INNER JOIN [dbo].[vw_Dim_ProductTransactionType] tt ON tt.ProductTransactionTypeID = p.ProductTransactionTypeId
                    INNER JOIN [dbo].[Dim_DateMarketYear] dm ON dm.MarketYearID = fs.MarketYearID
                    INNER JOIN [dbo].[vw_UserProductBrandRates] ur On ur.ProductBrandID = fs.ProductBrandID AND ur.UserLogin = 'DUSTIN.WANDER@AGDATA.COM'
                    WHERE dm.YearsFromCurrent = 0
                    AND IncludeMarketToDate = 1                          
                    AND fs.SaleTypeID = 2
                    AND Released = 1
                    AND bc.TenantId = 101
                    AND ISOUTLIER <> 1
                    AND ISNULL(Price, 0) <> 0
                    AND bc.Pricing_DisplayIndicator <> 'HIDE'    
                    {maskedProducts}
                    {appendQuery}
                    GROUP BY GROUPING SETS ((ProductBrandDesc), (ProductBrandDesc, TerritoryLevel1Name))
                    ), PY AS
                    (
                    Select DISTINCT ProductBrandDesc, CASE WHEN TerritoryLevel1Name IS NULL THEN 'Total' ELSE TerritoryLevel1Name END TerritoryLevel1Name,  AVG(Price*{useRate}) Price
                    FROM vw_Fact_Sales fs 
                    INNER JOIN vw_BrandSecurity_County bc ON bc.ProductBrandId = fs.ProductBrandID AND bc.MarketYearId = fs.MarketYearID AND bc.CountyId = fs.CountyID
                    JOIN [dbo].[Dim_DateDay] d ON fs.DayID = d.DateID
                    INNER JOIN vw_Dim_ProductBrand pb ON pb.ProductBrandId = bc.Pricing_Masked_ProductBrandId
                    INNER JOIN vw_Dim_Products p ON p.ProductId = fs.ProductId
                    INNER JOIN vw_Dim_ProductUom u ON u.ProductUomId = p.ProductUOMId
                    INNER JOIN [dbo].[vw_Dim_TerritoryLevel1] t on bc.[TerritoryLevel1ID] = t.[TerritoryIDLevel1ID]
                    INNER JOIN [dbo].[Dim_DateMarketYear] dm ON dm.MarketYearID = fs.MarketYearID
                    INNER JOIN [dbo].[vw_UserProductBrandRates] ur On ur.ProductBrandID = fs.ProductBrandID AND ur.UserLogin = 'DUSTIN.WANDER@AGDATA.COM'
                    WHERE dm.YearsFromCurrent = 1
                    AND IncludeMarketToDate = 1                                
                    AND fs.SaleTypeID = 2
                    AND Released = 1
                    AND bc.TenantId = 101
                    AND ISOUTLIER <> 1
                    AND ISNULL(Price, 0) <> 0
                    AND bc.Pricing_DisplayIndicator <> 'HIDE'
                    {maskedProducts}
                    {appendQuery}
                    GROUP BY GROUPING SETS ((ProductBrandDesc), (ProductBrandDesc, TerritoryLevel1Name))
                    )
                    
                    Select CASE WHEN cy.ProductBrandDesc IS NULL THEN py.ProductBrandDesc ELSE cy.ProductBrandDesc END as ProductBrandDesc, 
                    CASE WHEN cy.TerritoryLevel1Name IS NULL THEN py.TerritoryLevel1Name ELSE cy.TerritoryLevel1Name END AS TerritoryName
                    ,SUM(ISNULL(cast(cy.Price as decimal(15,1)), 0.0))  CYTD_Price, SUM(ISNULL(cast(py.Price as decimal(15,1)), 0.0)) PYTD_Price, CASE WHEN SUM(cy.Price) IS NULL THEN MAX('-100') ELSE SUM(ISNULL(cast(((cy.Price-py.Price)/py.Price)*100 as decimal(15,1)), 0.00)) END AS Growth
                    FROM cy cy 
                    FULL OUTER JOIN PY py On py.ProductBrandDesc = cy.ProductBrandDesc AND py.TerritoryLevel1Name = cy.TerritoryLevel1Name
                    GROUP BY (CASE WHEN cy.ProductBrandDesc IS NULL THEN py.ProductBrandDesc ELSE cy.ProductBrandDesc END),
                    (CASE WHEN cy.ProductBrandDesc IS NULL THEN py.ProductBrandDesc ELSE cy.ProductBrandDesc END), 
                    (CASE WHEN cy.TerritoryLevel1Name IS NULL THEN py.TerritoryLevel1Name ELSE cy.TerritoryLevel1Name END)";

            var response = DBHelper.ExecuteSqlString<KPIDataBaseValues>(TestConfig.ImpactVetConnectionString, TotalDataQuery, 200).ToList();

            response.Select(x =>
            {
                if (x.Growth == "0.0")
                    x.Growth = "0.00";
                return x;
            }).ToList();

            return response;
        }

        public static List<InventoryKPIDataBaseValues> GetInventoryValuesBasedOnAttribute(CommonFilters filters, string inventoryPage)
        {

            var queryParts = SetQueryParts(inventoryPage);
            var appendQuery = GetQueryAppendStringBasedOnFilters(filters);

            // Find the index of the second "AND"
            int firstAndIndex = appendQuery.IndexOf("AND", StringComparison.OrdinalIgnoreCase);
            int secondAndIndex = appendQuery.IndexOf("AND", firstAndIndex + 3, StringComparison.OrdinalIgnoreCase);

            // Keep everything from the second "AND" onward
            string trimmedAppendQuery = secondAndIndex >= 0 ? appendQuery.Substring(secondAndIndex).Trim() : appendQuery;           

            var TotalDataQuery =
                $@";WITH Cte_InventoryVolume AS (
                    SELECT   fs.[ManufacturerID]  ,{queryParts.BaseColumns},u.ProductUomDesc, fs.MarketYearID,
		            sum(fs.InventoryVolume) InventoryVolume  
                    FROM [vw_Fact_Inventory] fs
                    Join [vw_BrandSecurity_County] bc on (fs.[MarketYearID] = bc.[Inventory_MarketYearID] and 
                                                fs.[ProductBrandID] = bc.[ProductBrandID] and 
                                                fs.[CountyId] = bc.[CountyId])
                    join [dbo].[vw_Dim_TerritoryLevel1] t on (bc.[TerritoryLevel1ID] = t.[TerritoryIDLevel1ID])
                    join [dbo].[Dim_DateDay]   d on (fs.[AsOfDayID] = d.[DateID])
                    join [dbo].[Dim_DateMarketYear]   dm on (fs.[MarketYearID] = dm.[MarketYearID])
                    LEFT join dbo.vw_Dim_Products vp ON vp.ProductID = fs.ProductID 
				    LEFT JOIN dbo.vw_Dim_ProductUom u ON u.ProductUomID  = vp.ProductUOMID 
				    INNER JOIN vw_Dim_ProductBrand pb ON pb.ProductBrandID = bc.Inventory_ProductBrandID
				    LEFT join [dbo].[vw_Accounts_Retailer] as r ON r.RetailerAccountID = fs.DistributorAccountID
				    Left Join [dbo].[vw_Dim_Manufacturers] as M on M.ManufacturerID = fs.ManufacturerID
                    {queryParts.Joins}
				    WHERE bc.[TenantID] in (101)  --this is the tenant you are logged in as
                    and fs.Released = 1  
                    and dm.YearsFromCurrent IN (0,1)
                    AND Inventory_DisplayIndicator IS NOT NULL
                    {appendQuery}
                    GROUP BY fs.[ManufacturerID] ,{queryParts.GroupByColumns},u.ProductUomDesc, fs.MarketYearID
                    ),Cte_SalesVolumne 
                    AS (
                    SELECT  fs.[ManufacturerID] ,{queryParts.BaseColumns}, u.ProductUomDesc, bc.Inventory_MarketYearID MarketYearId,
                    Sum(fs.[FS_Volume])  SalesVolumne	
                    FROM [vw_Fact_Sales] fs
                    join [vw_BrandSecurity_County] bc on (fs.[MarketYearID] + 1 = bc.[Inventory_MarketYearID] and 
                                                fs.[ProductBrandID] = bc.[ProductBrandID] and 
                                                fs.[CountyID] = bc.CountyID)
                    join [dbo].[Dim_DateDay] d on (fs.[DayID] = d.[DateID])
				    --join [dbo].[vw_trans_month_totalLY]	d on (da.[MonthID] = d.[MonthID_TotalLY])
                    join [dbo].[vw_Dim_TerritoryLevel1] t on (bc.[TerritoryLevel1ID] = t.[TerritoryIDLevel1ID])
                    join [dbo].[Dim_DateMarketYear]   dm on (dm.MarketYearID = bc.Inventory_MarketYearID)
				    LEFT join dbo.vw_Dim_Products vp ON vp.ProductID = fs.ProductID 
				    INNER JOIN vw_Dim_ProductBrand pb ON pb.ProductBrandID = bc.Inventory_ProductBrandID
				    LEFT JOIN dbo.vw_Dim_ProductUom u ON u.ProductUomID  = vp.ProductUOMID 
				 	LEFT join [dbo].[vw_Accounts_Retailer] as r ON r.RetailerAccountID = fs.DistributorAccountID
					Left Join [dbo].[vw_Dim_Manufacturers] as M on M.ManufacturerID = fs.ManufacturerID
                    {queryParts.Joins}
                    WHERE  t.[TenantID] in (101)
                    and fs.Released = 1                    
                    AND fs.InventorySubmitterFlag = 1
                    and dm.YearsFromCurrent IN (0,1)
                    {trimmedAppendQuery}
                    group by  u.ProductUomDesc,{queryParts.GroupByColumns}, bc.Inventory_MarketYearID	,fs.[ManufacturerID]
                    ) 
                    SELECT 
                     ISNULL(ISNULL(v.{queryParts.Columns}, s.{queryParts.Columns}), 'Total') AS {queryParts.Columns},
                     CAST(
                       ROUND(
                         (SUM(CASE WHEN ISNULL(v.MarketYearID, s.MarketYearID) = 2025 THEN ISNULL(v.InventoryVolume, 0) END) /
                          NULLIF(SUM(CASE WHEN ISNULL(v.MarketYearID, s.MarketYearID) = 2025 THEN ISNULL(s.SalesVolumne, 0) END), 0)
                         ) * 100, 2) AS decimal(10,2)
                     ) AS CY_InventoryAsPercentOfSales,
                     CAST(
                       ISNULL(ROUND(
                         (SUM(CASE WHEN ISNULL(v.MarketYearID, s.MarketYearID) = 2024 THEN ISNULL(v.InventoryVolume, 0) END) /
                          NULLIF(SUM(CASE WHEN ISNULL(v.MarketYearID, s.MarketYearID) = 2024 THEN ISNULL(s.SalesVolumne, 0) END), 0)
                         ) * 100, 2), 100.00) AS decimal(10,2)
                     ) AS PY_InventoryAsPercentOfSales                  
                   FROM Cte_InventoryVolume AS v
                   FULL OUTER JOIN Cte_SalesVolumne AS s 
                     ON v.ManufacturerID = s.ManufacturerID 
                     AND v.MarketYearID = s.MarketYearID 
                     AND s.{queryParts.Columns} = v.{queryParts.Columns}                   
                   GROUP BY GROUPING SETS ((), (ISNULL(v.{queryParts.Columns}, s.{queryParts.Columns})))
                   ORDER BY {queryParts.Columns};
                   ";

            var response = DBHelper.ExecuteSqlString<InventoryKPIDataBaseValues>(TestConfig.ImpactVetConnectionString, TotalDataQuery, 300).ToList();

            var updatedData = response.Select(item =>
            {
                if (double.TryParse(item.CY_InventoryAsPercentOfSales, out double cy) &&
                    double.TryParse(item.PY_InventoryAsPercentOfSales, out double py) &&
                    py != 0)
                {
                    if (cy == 0 && inventoryPage == "Monthly")
                    {
                        item.Growth = "-100.00"; //Only values for PY so growth is -100%                        
                    }
                    else
                    {
                        double growth = (cy - py);// / py) * 100;
                        item.Growth = growth.ToString("F2"); // Format to 2 decimal places
                    }
                }
                else
                {
                    // If parsing fails or PY is 0, show negative of PY (or "0" if parse failed)
                    if (double.TryParse(item.PY_InventoryAsPercentOfSales, out double pyFallback))
                    {
                        if (pyFallback != 0.00 && inventoryPage == "Monthly")
                        {
                            item.Growth = "-100.00";
                        }
                        else if (pyFallback != 0.00)
                        {
                            item.Growth = (-pyFallback).ToString("F2"); // Show negative PY value
                        }
                        else
                        {
                            item.Growth = "0.00"; // Fallback if growth is 0
                        }
                    }
                    else
                    {
                        item.Growth = "0.00"; // Fallback if parsing fails completely
                    }
                }

                return item;
            }).ToList();

            return response;
        }

        public static List<MonthTrendsGraph> GetInventoryMonthlyValues(CommonFilters filters, bool calcualateGrowth = false)
        {

            var appendQuery = GetQueryAppendStringBasedOnFilters(filters);

            // Find the index of the second "AND"
            int firstAndIndex = appendQuery.IndexOf("AND", StringComparison.OrdinalIgnoreCase);
            int secondAndIndex = appendQuery.IndexOf("AND", firstAndIndex + 3, StringComparison.OrdinalIgnoreCase);

            // Keep everything from the second "AND" onward
            string trimmedAppendQuery = secondAndIndex >= 0 ? appendQuery.Substring(secondAndIndex).Trim() : appendQuery;

            var TotalDataQuery =
                $@";WITH Cte_InventoryVolume AS (
                    SELECT  DATENAME(MONTH, CAST(CONCAT(d.MonthId, '01') AS DATE)) [MonthName], dm.YearsFromCurrent, fs.MarketYearID,
		            sum(fs.InventoryVolume) InventoryVolume  
                    FROM [vw_Fact_Inventory] fs
                    Join [vw_BrandSecurity_County] bc on (fs.[MarketYearID] = bc.[Inventory_MarketYearID] and 
                                                fs.[ProductBrandID] = bc.[ProductBrandID] and 
                                                fs.[CountyId] = bc.[CountyId])
                    join [dbo].[vw_Dim_TerritoryLevel1] t on (bc.[TerritoryLevel1ID] = t.[TerritoryIDLevel1ID])
                    join [dbo].[Dim_DateDay]   d on (fs.[AsOfDayID] = d.[DateID])
                    join [dbo].[Dim_DateMarketYear]   dm on (fs.[MarketYearID] = dm.[MarketYearID])
                    LEFT join dbo.vw_Dim_Products vp ON vp.ProductID = fs.ProductID 
				    LEFT JOIN dbo.vw_Dim_ProductUom u ON u.ProductUomID  = vp.ProductUOMID 
				    INNER JOIN vw_Dim_ProductBrand pb ON pb.ProductBrandID = bc.Inventory_ProductBrandID
				    LEFT join [dbo].[vw_Accounts_Retailer] as r ON r.RetailerAccountID = fs.DistributorAccountID
				    Left Join [dbo].[vw_Dim_Manufacturers] as M on M.ManufacturerID = fs.ManufacturerID             
				    WHERE t.[TenantID] in (101)  --this is the tenant you are logged in as
                    and fs.Released = 1  
                    and dm.YearsFromCurrent IN (0,1)
                    AND Inventory_DisplayIndicator IS NOT NULL
                    {appendQuery}
                    GROUP BY DATENAME(MONTH, CAST(CONCAT(d.MonthId, '01') AS DATE)), dm.YearsFromCurrent, fs.MarketYearID
                    ),Cte_SalesVolumne 
                    AS (
                    SELECT dm.YearsFromCurrent, bc.Inventory_MarketYearID MarketYearId,
                    Sum(fs.[FS_Volume])  SalesVolumne	
                    FROM [vw_Fact_Sales] fs
                    join [vw_BrandSecurity_County] bc on (fs.[MarketYearID] + 1 = bc.[Inventory_MarketYearID] and 
                                                fs.[ProductBrandID] = bc.[ProductBrandID] and 
                                                fs.[CountyID] = bc.CountyID)
                    join [dbo].[Dim_DateDay] d on (fs.[DayID] = d.[DateID])
				   -- join [dbo].[vw_trans_month_totalLY]	d on (da.[MonthID] = d.[MonthID_TotalLY])
                    join [dbo].[vw_Dim_TerritoryLevel1] t on (bc.[TerritoryLevel1ID] = t.[TerritoryIDLevel1ID])
                    join [dbo].[Dim_DateMarketYear]   dm on (dm.MarketYearID = bc.Inventory_MarketYearID)
				    LEFT join dbo.vw_Dim_Products vp ON vp.ProductID = fs.ProductID 
				    INNER JOIN vw_Dim_ProductBrand pb ON pb.ProductBrandID = bc.Inventory_ProductBrandID
				    LEFT JOIN dbo.vw_Dim_ProductUom u ON u.ProductUomID  = vp.ProductUOMID 
				 	LEFT join [dbo].[vw_Accounts_Retailer] as r ON r.RetailerAccountID = fs.DistributorAccountID
					Left Join [dbo].[vw_Dim_Manufacturers] as M on M.ManufacturerID = fs.ManufacturerID                
                    WHERE  t.[TenantID] in (101)
                    and fs.Released = 1            
                    AND fs.InventorySubmitterFlag = 1
                    and dm.YearsFromCurrent IN (0,1)
                   {trimmedAppendQuery}
                    group by  dm.YearsFromCurrent, bc.Inventory_MarketYearID
                    ) 
                    SELECT 
                     v.MonthName AS [Month],
                     ISNULL(v.YearsFromCurrent, s.YearsFromCurrent) YearsFromCurrent,
					 ISNULL(v.MarketYearID, s.MarketYearId) AS [Season],
                     CAST(
                       ISNULL(ROUND(
                         (SUM (ISNULL(v.InventoryVolume, 0) / NULLIF(s.SalesVolumne, 0))) * 100, 2), 100.00) AS decimal(10,2)
                    ) AS InventoryPercentOfSales                                       
                   FROM Cte_InventoryVolume AS v
                   FULL OUTER JOIN Cte_SalesVolumne AS s 
                   --  ON v.MonthName = s.MonthName
                     ON v.MarketYearID = s.MarketYearID              
                   
                   GROUP BY v.MonthName, ISNULL(v.YearsFromCurrent, s.YearsFromCurrent),  ISNULL(v.MarketYearID, s.MarketYearId) 
                   ORDER BY [Month]";

            var response = DBHelper.ExecuteSqlString<MonthTrendsGraph>(TestConfig.ImpactVetConnectionString, TotalDataQuery, 200).ToList();

            if (calcualateGrowth)
            {

                var growthByMonth = response
                    .GroupBy(x => x.Month)
                    .Select(g =>
                    {
                        var cy = g.FirstOrDefault(r => r.Season == DateTime.Now.Year.ToString());
                        var py = g.FirstOrDefault(r => r.Season == (DateTime.Now.Year - 1).ToString());

                        decimal cyValue = cy != null ? decimal.Parse(cy.InventoryPercentOfSales ?? "0.00") : 0;
                        decimal pyValue = py != null ? decimal.Parse(py.InventoryPercentOfSales ?? "0.00") : 0;

                        return new
                        {
                            Month = g.Key,
                            CY = cyValue,
                            PY = pyValue,
                            Growth = ((cyValue - pyValue)/ pyValue) * 100
                        };
                    })
                    .ToList();


                foreach (var item in growthByMonth)
                {
                    if (!string.IsNullOrEmpty(item.Month))
                    {
                        var targetRow = response.FirstOrDefault(x =>
                            string.Equals(x.Month?.Trim(), item.Month.Trim(), StringComparison.OrdinalIgnoreCase) &&
                            x.Season == DateTime.Now.Year.ToString());

                        if (targetRow != null)
                        {
                            targetRow.Growth = item.Growth.ToString();
                        }
                    }
                }
            }

            return response;
        }

        public static List<ManufacturerInventoryGraph> GetInventoryBarValuesBasedOnManufacturer(CommonFilters filters)
        {
            var appendQuery = GetQueryAppendStringBasedOnFilters(filters);

            // Find the index of the second "AND"
            int firstAndIndex = appendQuery.IndexOf("AND", StringComparison.OrdinalIgnoreCase);
            int secondAndIndex = appendQuery.IndexOf("AND", firstAndIndex + 3, StringComparison.OrdinalIgnoreCase);

            // Keep everything from the second "AND" onward
            string trimmedAppendQuery = secondAndIndex >= 0 ? appendQuery.Substring(secondAndIndex).Trim() : appendQuery;

            var TotalDataQuery =
                $@"; WITH Cte_InventoryVolume AS (
                     SELECT fs.[ManufacturerID], M.ManufacturerDesc,u.ProductUomDesc, fs.MarketYearID, 
                            sum(fs.InventoryVolume) InventoryVolume  
                      FROM [vw_Fact_Inventory]         fs
                      join [vw_BrandSecurity_County] bc on (fs.[MarketYearID] = bc.[Inventory_MarketYearID] and 
                                                                     fs.[ProductBrandID] = bc.[ProductBrandID] and 
                                                                     fs.[CountyId] = bc.[CountyId])
                      join [dbo].[vw_Dim_TerritoryLevel1] t on (bc.[TerritoryLevel1ID] = t.[TerritoryIDLevel1ID])
                      join [dbo].[Dim_DateDay]   d on (fs.[AsOfDayID] = d.[DateID])
                      join [dbo].[Dim_DateMarketYear]   dm on (fs.[MarketYearID] = dm.[MarketYearID])
                      LEFT join dbo.vw_Dim_Products vp ON vp.ProductID = fs.ProductID 
                      LEFT JOIN dbo.vw_Dim_ProductUom u ON u.ProductUomID  = vp.ProductUOMID 
                      INNER JOIN vw_Dim_ProductBrand pb ON pb.ProductBrandID = bc.Inventory_ProductBrandID
                      LEFT join [dbo].[vw_Accounts_Retailer] as r ON r.RetailerAccountID = fs.DistributorAccountID
                      Left Join [dbo].[vw_Dim_Manufacturers] as M on M.ManufacturerID = fs.ManufacturerID
                      WHERE   t.[TenantID] in (101)  --this is the tenant you are logged in as
                      and fs.Released = 1  
                      and dm.YearsFromCurrent IN (0,1)
                           {appendQuery}
                      AND Inventory_DisplayIndicator IS NOT NULL
                      GROUP BY fs.[ManufacturerID] ,M.ManufacturerDesc,u.ProductUomDesc, fs.MarketYearID
                      ),Cte_SalesVolumne AS (
                      SELECT  fs.[ManufacturerID] ,M.ManufacturerDesc , u.ProductUomDesc, bc.Inventory_MarketYearID MarketYearId,
                      Sum(fs.[FS_Volume])  SalesVolumne	
                      FROM [vw_Fact_Sales] fs
                      join [vw_BrandSecurity_County] bc on (fs.[MarketYearID] + 1 = bc.[Inventory_MarketYearID] and 
                                                                     fs.[ProductBrandID] = bc.[ProductBrandID] and 
                                                                     fs.[CountyID] = bc.CountyID)
                       join [dbo].[Dim_DateDay] da on (fs.[DayID] = da.[DateID])                      
                       join [dbo].[vw_Dim_TerritoryLevel1] t on (bc.[TerritoryLevel1ID] = t.[TerritoryIDLevel1ID])
                       join [dbo].[Dim_DateMarketYear]   dm on (dm.MarketYearID = bc.Inventory_MarketYearID)
                       LEFT join dbo.vw_Dim_Products vp ON vp.ProductID = fs.ProductID 
                       INNER JOIN vw_Dim_ProductBrand pb ON pb.ProductBrandID = bc.Inventory_ProductBrandID
                       LEFT JOIN dbo.vw_Dim_ProductUom u ON u.ProductUomID  = vp.ProductUOMID 
                       LEFT join [dbo].[vw_Accounts_Retailer] as r ON r.RetailerAccountID = fs.DistributorAccountID
                       Left Join [dbo].[vw_Dim_Manufacturers] as M on M.ManufacturerID = fs.ManufacturerID
                       WHERE  t.[TenantID] in (101)
                       and fs.Released = 1                       
                       AND fs.InventorySubmitterFlag = 1
                       and dm.YearsFromCurrent IN (0, 1)
                         {trimmedAppendQuery}
                       group by  u.ProductUomDesc
                       ,M.ManufacturerDesc	
                       , bc.Inventory_MarketYearID			
                       ,fs.[ManufacturerID]
                     
                     ) 
                     
                     SELECT DISTINCT ISNULL(s.ManufacturerDesc, v.ManufacturerDesc) Manufacturer,
                     CASE WHEN v.MarketYearID IS NULL THEN s.MarketYearID ELSE v.MarketYearID END AS Season
                     ,  CASE WHEN SUM(v.InventoryVolume) IS NULL THEN 0.00
                     WHEN SUM(s.SalesVolumne) IS NULL THEN 100.00
                     ELSE CAST(ROUND(ISNULL(SUM(v.InventoryVolume)/NULLIF(SUM(s.SalesVolumne), 0) * 100, 100.00), 2) AS decimal(10, 2)) END AS InventoryPercentOfSales
                     FROM Cte_InventoryVolume as v
                     FULL Outer JOIN Cte_SalesVolumne  as s ON v.ManufacturerID = s.ManufacturerID AND v.MarketYearId = s.MarketYearID AND s.ManufacturerDesc = v.ManufacturerDesc
                     GROUP BY CASE WHEN v.MarketYearID IS NULL THEN s.MarketYearID ELSE v.MarketYearID END, s.ManufacturerDesc, v.ManufacturerDesc
                   ";

            var response = DBHelper.ExecuteSqlString<ManufacturerInventoryGraph>(TestConfig.ImpactVetConnectionString, TotalDataQuery, 200).ToList();

            return response;
        }

        public static List<PriceTrendsQueryValues> GetMonthPricePerUomValuesBasedOnProductName(CommonFilters filters, bool includeUseRates = false, bool onlyMaskedProducts = false)
        {
            string maskedProducts = string.Empty;
            string useRate = "1";
            var appendQuery = GetQueryAppendStringBasedOnFilters(filters);

            if (onlyMaskedProducts)
            {
                maskedProducts = "AND bc.Pricing_DisplayIndicator = 'Mask'";
            }

            if (includeUseRates)
            {
                useRate = "ur.UseRate";
            }

            var TotalDataQuery =
                $@"WITH CY AS
                            (
                            Select DISTINCT ProductBrandDesc, fs.MarketYearID, right([d].[MonthID], 2) MonthId, ROUND(AVG(Price*{useRate}), 1) Price
                            FROM vw_Fact_Sales fs 
                            INNER JOIN vw_BrandSecurity_County bc ON bc.ProductBrandId = fs.ProductBrandID AND bc.MarketYearId = fs.MarketYearID AND bc.CountyId = fs.CountyID
                            JOIN [dbo].[Dim_DateDay] d ON fs.DayID = d.DateID
                            INNER JOIN vw_Dim_ProductBrand pb ON pb.ProductBrandId = bc.Pricing_Masked_ProductBrandId
                            INNER JOIN vw_Dim_Products p ON p.ProductId = fs.ProductId
                            INNER JOIN vw_Dim_ProductUom u ON u.ProductUomId = p.ProductUOMId
                            INNER JOIN [dbo].[vw_Dim_TerritoryLevel1] t on bc.[TerritoryLevel1ID] = t.[TerritoryIDLevel1ID]
                            INNER JOIN [dbo].[vw_Dim_ProductTransactionType] tt ON tt.ProductTransactionTypeID = p.ProductTransactionTypeId
                            INNER JOIN [dbo].[Dim_DateMarketYear] dm ON dm.MarketYearID = fs.MarketYearID
                            INNER JOIN [dbo].[vw_UserProductBrandRates] ur On ur.ProductBrandID = fs.ProductBrandID AND ur.UserLogin = 'DUSTIN.WANDER@AGDATA.COM'
                            WHERE dm.YearsFromCurrent = 0                                                                     
                            AND fs.SaleTypeID = 2
                            AND bc.TenantId = 101
                            AND Released = 1
                            AND ISOUTLIER <> 1
                            AND bc.Pricing_DisplayIndicator <> 'HIDE'
                            {appendQuery}
                            {maskedProducts}
                            GROUP BY ProductBrandDesc, right([d].[MonthID], 2), fs.MarketYearID
                            ), PY AS
                            (
                            Select DISTINCT ProductBrandDesc, fs.MarketYearID, right([d].[MonthID], 2) MonthId,  ROUND(AVG(Price*{useRate}), 1) Price
                            FROM vw_Fact_Sales fs 
                            INNER JOIN vw_BrandSecurity_County bc ON bc.ProductBrandId = fs.ProductBrandID AND bc.MarketYearId = fs.MarketYearID AND bc.CountyId = fs.CountyID
                            JOIN [dbo].[Dim_DateDay] d ON fs.DayID = d.DateID
                            INNER JOIN vw_Dim_ProductBrand pb ON pb.ProductBrandId = bc.Pricing_Masked_ProductBrandId
                            INNER JOIN vw_Dim_Products p ON p.ProductId = fs.ProductId
                            INNER JOIN vw_Dim_ProductUom u ON u.ProductUomId = p.ProductUOMId
                            INNER JOIN [dbo].[vw_Dim_TerritoryLevel1] t on bc.[TerritoryLevel1ID] = t.[TerritoryIDLevel1ID]
                            INNER JOIN [dbo].[Dim_DateMarketYear] dm ON dm.MarketYearID = fs.MarketYearID
                            INNER JOIN [dbo].[vw_UserProductBrandRates] ur On ur.ProductBrandID = fs.ProductBrandID AND ur.UserLogin = 'DUSTIN.WANDER@AGDATA.COM'
                            WHERE dm.YearsFromCurrent = 1                                                
                            AND fs.SaleTypeID = 2
                            AND bc.TenantId = 101
                            AND Released = 1
                            AND ISOUTLIER <> 1
                            AND bc.Pricing_DisplayIndicator <> 'HIDE'
                            {appendQuery}
                            {maskedProducts}
                            GROUP BY ProductBrandDesc, right([d].[MonthID], 2), fs.MarketYearID
                            ), PY2 AS
                            (
                            Select DISTINCT ProductBrandDesc, fs.MarketYearID, right([d].[MonthID], 2) MonthId,  ROUND(AVG(Price*{useRate}), 1) Price
                            FROM vw_Fact_Sales fs 
                            INNER JOIN vw_BrandSecurity_County bc ON bc.ProductBrandId = fs.ProductBrandID AND bc.MarketYearId = fs.MarketYearID AND bc.CountyId = fs.CountyID
                            JOIN [dbo].[Dim_DateDay] d ON fs.DayID = d.DateID
                            INNER JOIN vw_Dim_ProductBrand pb ON pb.ProductBrandId = bc.Pricing_Masked_ProductBrandId
                            INNER JOIN vw_Dim_Products p ON p.ProductId = fs.ProductId
                            INNER JOIN vw_Dim_ProductUom u ON u.ProductUomId = p.ProductUOMId
                            INNER JOIN [dbo].[vw_Dim_TerritoryLevel1] t on bc.[TerritoryLevel1ID] = t.[TerritoryIDLevel1ID]
                            INNER JOIN [dbo].[Dim_DateMarketYear] dm ON dm.MarketYearID = fs.MarketYearID
                            INNER JOIN [dbo].[vw_UserProductBrandRates] ur On ur.ProductBrandID = fs.ProductBrandID AND ur.UserLogin = 'DUSTIN.WANDER@AGDATA.COM'
                            WHERE dm.YearsFromCurrent = 2                                        
                            AND fs.SaleTypeID = 2
                            AND bc.TenantId = 101
                            AND Released = 1
                            AND ISOUTLIER <> 1
                            AND bc.Pricing_DisplayIndicator <> 'HIDE'
                            {appendQuery}
                            {maskedProducts}
                            GROUP BY ProductBrandDesc, right([d].[MonthID], 2), fs.MarketYearID
                            )
                            Select ProductBrandDesc, MarketYearId Season, SUBSTRING(DATENAME(month, DATEADD(month, MonthId-1, CAST('2008-01-01' AS datetime))), 1, 3) MonthId, SUM(ISNULL(cast(Price as decimal(15,1)), 0.0))  Price
                            FROM cy 
                            GROUP BY ProductBrandDesc, MarketYearId,SUBSTRING(DATENAME(month, DATEADD(month, MonthId-1, CAST('2008-01-01' AS datetime))), 1, 3)
                            UNION
                            SELECT ProductBrandDesc, MarketYearId Season, SUBSTRING(DATENAME(month, DATEADD(month, MonthId-1, CAST('2008-01-01' AS datetime))), 1, 3) MonthId, SUM(ISNULL(cast(Price as decimal(15,1)), 0.0)) Price
                            FROM PY
                            GROUP BY ProductBrandDesc, MarketYearId, SUBSTRING(DATENAME(month, DATEADD(month, MonthId-1, CAST('2008-01-01' AS datetime))), 1, 3)
                            UNION
                            SELECT ProductBrandDesc, MarketYearId Season, SUBSTRING(DATENAME(month, DATEADD(month, MonthId-1, CAST('2008-01-01' AS datetime))), 1, 3) MonthId, SUM(ISNULL(cast(Price as decimal(15,1)), 0.0)) Price
                            FROM PY2
                            GROUP BY ProductBrandDesc, MarketYearId, SUBSTRING(DATENAME(month, DATEADD(month, MonthId-1, CAST('2008-01-01' AS datetime))), 1, 3)";

            var response = DBHelper.ExecuteSqlString<PriceTrendsQueryValues>(TestConfig.ImpactVetConnectionString, TotalDataQuery, 60).ToList();

            return response;
        }

        public static List<FilterOptionsWithData> GetFilterSelectionsWithData(string dossierName, CommonFilters filters = null, string yearsFromCurrent = "0, 1", bool currentMarketYear = false)
        {
            var appendQuery = String.Empty;
            var marketYearAppend = String.Empty;
            var hiddenData = String.Empty;

            if (dossierName != "Inventory")
            {
                hiddenData = "'HIDE', 'Mask'";
            }
            else
            {
                hiddenData = "'hide'";
            }


            if (filters != null)
                appendQuery = GetQueryAppendStringBasedOnFilters(filters);

            if (currentMarketYear)
                marketYearAppend = "AND dm.IsCurrentMarketYear = 1";

            var TotalDataQuery =
                $@"		Select DISTINCT ProductUomDesc Uom, ProductBrandDesc [Product], FORMAT(TRY_CAST(CONCAT(fs.MonthID, '01') AS DATE), 'MMM') AS [Month], t.TerritoryLevel1Name Region
                        FROM vw_Fact_Sales fs 
                        INNER JOIN vw_BrandSecurity_County bc ON bc.ProductBrandId = fs.ProductBrandID AND bc.MarketYearId = fs.MarketYearID AND bc.CountyId = fs.CountyID
                        JOIN [dbo].[Dim_DateDay] d ON fs.DayID = d.DateID
                        INNER JOIN vw_Dim_ProductBrand pb ON pb.ProductBrandId = bc.Pricing_Masked_ProductBrandId
                        INNER JOIN vw_Dim_Products p ON p.ProductId = fs.ProductId
                        INNER JOIN vw_Dim_ProductUom u ON u.ProductUomId = p.ProductUOMId
                        INNER JOIN [dbo].[Dim_DateMarketYear] dm ON dm.MarketYearID = fs.MarketYearID
						INNER JOIN [dbo].[vw_Dim_TerritoryLevel1] t on bc.[TerritoryLevel1ID] = t.[TerritoryIDLevel1ID]
                        WHERE dm.YearsFromCurrent IN ({yearsFromCurrent})                        
                        AND ProductUomDesc IN ('GAL', 'LB')                      
                        AND fs.SaleTypeID = 2
                        AND Released = 1
                        AND ISOUTLIER <> 1
                        AND bc.TenantId = 101                       
                        AND bc.{dossierName}_DisplayIndicator NOT IN ({hiddenData})
                        {appendQuery}
                        {marketYearAppend}
                        GROUP BY ProductUomDesc, ProductBrandDesc, FORMAT(TRY_CAST(CONCAT(fs.MonthID, '01') AS DATE), 'MMM'), TerritoryLevel1Name
						HAVING AVG(Price) > 0";

            var response = DBHelper.ExecuteSqlString<FilterOptionsWithData>(TestConfig.ImpactVetConnectionString, TotalDataQuery, 200).ToList();

            var groupedResponse = response.GroupBy(x => new { x.Uom, x.Product })
                                          .Select(g => new FilterOptionsWithData
                                          {
                                              Uom = g.Key.Uom,
                                              Product = g.Key.Product,
                                              Month = string.Join(", ", g.Select(x => x.Month).Distinct()),
                                              Region = string.Join(", ", g.Select(x => x.Region).Distinct()),
                                          }).ToList();

            return groupedResponse;
        }

        public static List<FilterOptionsWithData> GetInventoryFilterSelectionsWithData(CommonFilters filters = null, string yearsFromCurrent = "0, 1", bool currentMarketYear = false)
        {
            var appendQuery = String.Empty;
            var marketYearAppend = String.Empty;             

            if (filters != null)
                appendQuery = GetQueryAppendStringBasedOnFilters(filters);

            if (currentMarketYear)
                marketYearAppend = "AND dm.IsCurrentMarketYear = 1";

            var TotalDataQuery =
                $@"		Select DISTINCT ProductUomDesc Uom, ProductBrandDesc [Product], FORMAT(TRY_CAST(CONCAT(fs.MonthID, '01') AS DATE), 'MMM') AS [Month], t.TerritoryLevel1Name Region
                        FROM vw_Fact_Inventory fs 
                        INNER JOIN vw_BrandSecurity_County bc ON bc.ProductBrandId = fs.ProductBrandID AND bc.MarketYearId = fs.MarketYearID AND bc.CountyId = fs.CountyID
                        JOIN [dbo].[Dim_DateDay] d ON fs.ASOfDayID = d.DateID
                        INNER JOIN vw_Dim_ProductBrand pb ON pb.ProductBrandId = bc.Inventory_ProductBrandId
                        INNER JOIN vw_Dim_Products p ON p.ProductBrandId = fs.ProductBrandId
                        INNER JOIN vw_Dim_ProductUom u ON u.ProductUomId = p.ProductUOMId
                        INNER JOIN [dbo].[Dim_DateMarketYear] dm ON dm.MarketYearID = fs.MarketYearID
						INNER JOIN [dbo].[vw_Dim_TerritoryLevel1] t on bc.[TerritoryLevel1ID] = t.[TerritoryIDLevel1ID]
                        WHERE dm.YearsFromCurrent IN ({yearsFromCurrent})                        
                        AND ProductUomDesc IN ('GAL', 'LB')                                           
                        AND Released = 1               
                        AND bc.TenantId = 101                       
                        AND bc.Inventory_DisplayIndicator NOT IN ('hide')
                        {appendQuery}
                        {marketYearAppend}
                        GROUP BY ProductUomDesc, ProductBrandDesc, FORMAT(TRY_CAST(CONCAT(fs.MonthID, '01') AS DATE), 'MMM'), TerritoryLevel1Name						";

            var response = DBHelper.ExecuteSqlString<FilterOptionsWithData>(TestConfig.ImpactVetConnectionString, TotalDataQuery, 120).ToList();

            var groupedResponse = response.GroupBy(x => new { x.Uom, x.Product })
                                          .Select(g => new FilterOptionsWithData
                                          {
                                              Uom = g.Key.Uom,
                                              Product = g.Key.Product,
                                              Month = string.Join(", ", g.Select(x => x.Month).Distinct()),
                                              Region = string.Join(", ", g.Select(x => x.Region).Distinct()),
                                          }).ToList();

            return groupedResponse;
        }

        public static List<FilterOptionsWithData> GetMarketShareFilterSelectionsWithData(CommonFilters filters = null, string yearsFromCurrent = "0, 1", bool currentMarketYear = false)
        {
            var appendQuery = String.Empty;
            var marketYearAppend = String.Empty;

            if (filters != null)
                appendQuery = GetQueryAppendStringBasedOnFilters(filters);

            if (currentMarketYear)
                marketYearAppend = "AND dm.IsCurrentMarketYear = 1";

            var TotalDataQuery =
                $@"		Select DISTINCT ProductUomDesc Uom, TRIM(ProductBrandDesc) [Product], FORMAT(TRY_CAST(CONCAT(fs.MonthID, '01') AS DATE), 'MMM') AS [Month], t.TerritoryLevel1Name Region
                        FROM vw_Fact_SalesGrossup fs 
                        INNER JOIN vw_BrandSecurity_County bc ON bc.ProductBrandId = fs.ProductBrandID AND bc.MarketYearId = fs.MarketYearID AND bc.CountyId = fs.CountyID
                        JOIN [dbo].[Dim_DateDay] d ON fs.DayID = d.DateID
                        INNER JOIN vw_Dim_ProductBrand pb ON pb.ProductBrandId = bc.MarketShare_ProductBrandID
                        LEFT JOIN vw_Dim_Products p ON p.ProductId = fs.ProductId
                        LEFT JOIN vw_Dim_ProductUom u ON u.ProductUomId = p.ProductUOMId
                        LEFT JOIN [dbo].[Dim_DateMarketYear] dm ON dm.MarketYearID = fs.MarketYearID
						LEFT JOIN [dbo].[vw_Dim_TerritoryLevel1] t on bc.[TerritoryLevel1ID] = t.[TerritoryIDLevel1ID]
                        LEFT JOIN dbo.Dim_Manufacturers m ON m.ManufacturerId = fs.ManufacturerId
                        WHERE dm.YearsFromCurrent IN ({yearsFromCurrent})                                                            
                        AND fs.SaleTypeID = 2
                        AND Released = 1                  
                        AND bc.TenantId = 101     					
                        AND bc.MarketShare_DisplayIndicator NOT IN ('HIDE')  
                        AND pb.ProductBrandDesc != 'All Other'
                        {appendQuery}
                        {marketYearAppend}
                        GROUP BY ProductUomDesc, ProductBrandDesc, FORMAT(TRY_CAST(CONCAT(fs.MonthID, '01') AS DATE), 'MMM'), TerritoryLevel1Name			";

            var response = DBHelper.ExecuteSqlString<FilterOptionsWithData>(TestConfig.ImpactVetConnectionString, TotalDataQuery, 60).ToList();

            var groupedResponse = response.GroupBy(x => new { x.Uom, x.Product })
                                          .Select(g => new FilterOptionsWithData
                                          {
                                              Uom = g.Key.Uom,
                                              Product = g.Key.Product,
                                              Month = string.Join(", ", g.Select(x => x.Month).Distinct()),
                                              Region = string.Join(", ", g.Select(x => x.Region).Distinct()),
                                          }).ToList();


            // Find all "All Other" entries
            var allOtherItems = groupedResponse.Where(x => x.Product == "All Other").ToList();

            // Create new entries for GAL and LBs
            var galItems = allOtherItems.Select(item => new FilterOptionsWithData
            {
                Uom = "GAL",
                Product = item.Product,
                Month = item.Month,
                Region = item.Region
            }).ToList();

            var lbItems = allOtherItems.Select(item => new FilterOptionsWithData
            {
                Uom = "LB",
                Product = item.Product,
                Month = item.Month,
                Region = item.Region
            }).ToList();

            // Remove original "All Other" entries and add the new ones
            groupedResponse.RemoveAll(x => x.Product == "All Other");
            groupedResponse.AddRange(galItems);
            groupedResponse.AddRange(lbItems);

            return groupedResponse;
        }

        public static List<MarketShareKPIDataBaseValues> GetMarketShareDataBasedOnSetFilters(CommonFilters filters, string MarketSharePage)
        {
            var appendQuery = GetQueryAppendStringBasedOnFilters(filters);
            var queryParts = SetQueryParts(MarketSharePage);

            var TotalDataQuery =
                $@"WITH CYSales AS 
                                (
                                SELECT fs.[MarketYearID]  [MarketYearID], {queryParts.BaseColumns} , sum(fs.Volume)  [Volume]
                                		FROM            [dbo].[vw_Fact_SalesGrossUp]	fs												
                                						INNER JOIN [dbo].[vw_BrandSecurity_County] bc ON  bc.ProductBrandID = fs.ProductBrandID AND bc.MarketYearID = fs.MarketYearID and fs.CountyID = bc.CountyID
                                                              INNER JOIN  Dim_DateDay AS d ON fs.DayID = d.DateID 
                                						   INNER JOIN vw_Dim_Products AS p ON fs.ProductID = p.ProductID 
                                						   INNER JOIN Dim_ProductUom u ON u.ProductUomID = p.ProductUOMID																																		 
                                                                     INNER JOIN vw_Dim_TerritoryLevel1 AS t ON bc.TerritoryLevel1ID = t.TerritoryIDLevel1ID
                                                                    INNER JOIN Dim_DateMarketYear AS dm ON fs.MarketYearID = dm.MarketYearID
                                								   INNER JOIN vw_Dim_ProductBrand pb ON pb.ProductBrandID = bc.MarketShare_ProductBrandID																		
                                								     INNER JOIN vw_Dim_Manufacturers m ON m.ManufacturerID = fs.ManufacturerID
                                                           INNER JOIN VW_Accounts_Distributor ad ON ad.DistributorAccountId = fs.DistributorAccountId
                                           WHERE       (dm.YearsFromCurrent IN (0)) 
                                		 AND (fs.SaleTypeID IN (2)) AND (bc.TenantID IN (101)) 
                                		 AND Released = 1                                										
                                		AND bc.MarketShare_DisplayIndicator <> 'hide'			
                                        {appendQuery}
                                		GROUP BY fs.[MarketYearID], {queryParts.GroupByColumns}												
                                		), PYSales AS
                                		(
                                SELECT fs.[MarketYearID]  [MarketYearID], {queryParts.BaseColumns} , sum(fs.Volume)  [Volume]
                                		FROM            [dbo].[vw_Fact_SalesGrossUp]	fs												
                                						INNER JOIN [dbo].[vw_BrandSecurity_County] bc ON  bc.ProductBrandID = fs.ProductBrandID AND bc.MarketYearID = fs.MarketYearID and fs.CountyID = bc.CountyID
                                                           INNER JOIN  Dim_DateDay AS d ON fs.DayID = d.DateID 
                                						   INNER JOIN vw_Dim_Products AS p ON fs.ProductID = p.ProductID 
                                						   INNER JOIN Dim_ProductUom u ON u.ProductUomID = p.ProductUOMID																																		 
                                                                  INNER JOIN vw_Dim_TerritoryLevel1 AS t ON bc.TerritoryLevel1ID = t.TerritoryIDLevel1ID
                                                                 INNER JOIN Dim_DateMarketYear AS dm ON fs.MarketYearID = dm.MarketYearID
                                								   INNER JOIN vw_Dim_ProductBrand pb ON pb.ProductBrandID = bc.MarketShare_ProductBrandID																		
                                								     INNER JOIN vw_Dim_Manufacturers m ON m.ManufacturerID = fs.ManufacturerID
                                                          INNER JOIN VW_Accounts_Distributor ad ON ad.DistributorAccountId = fs.DistributorAccountId
                                        WHERE       (dm.YearsFromCurrent IN (1))
                                		AND IncludeMarketToDate = 1
                                		 AND (fs.SaleTypeID IN (2)) AND (bc.TenantID IN (101)) 
                                		 AND Released = 1                                													
                                		AND bc.MarketShare_DisplayIndicator <> 'hide'
                                        {appendQuery}
                                		GROUP BY fs.[MarketYearID], {queryParts.GroupByColumns}		
                                													)
                                
                                	  SELECT coalesce(coalesce(Cy.{queryParts.Columns}, Py.{queryParts.Columns}), 'Total') {queryParts.Columns}, SUM(cy.Volume) AS CY_Volume,  SUM(py.Volume) AS PY_Volume, cast(SUM((cy.Volume - py.Volume)/NULLIF(py.Volume, 0))*100 as decimal(15,1)) AS Growth
                                	  FROM CYSales cy
                                	  FULL  JOIN PYSales py ON  cy.{queryParts.Columns} = py.{queryParts.Columns}
                                	  GROUP BY GROUPING SETS ((),(coalesce(Cy.{queryParts.Columns}, Py.{queryParts.Columns})))
                                	  ORDER BY coalesce(Cy.{queryParts.Columns}, Py.{queryParts.Columns})";

            var response = DBHelper.ExecuteSqlString<MarketShareKPIDataBaseValues>(TestConfig.ImpactVetConnectionString, TotalDataQuery, 200).ToList();

            return response;
        }

        public static List<ProductQuantityBarGraphDTO> GetMarketShareSeasonDataBasedOnSetFilters(CommonFilters filters, string marketSharePage)
        {
            var appendQuery = GetQueryAppendStringBasedOnFilters(filters);
            var queryParts = SetQueryParts(marketSharePage);

            var baseColumnsWithComma = string.IsNullOrWhiteSpace(queryParts.BaseColumns)
                   ? ""
                   : queryParts.BaseColumns + ",";

            var groupColumnsWithComma = string.IsNullOrWhiteSpace(queryParts.GroupByColumns)
                ? ""
                : queryParts.GroupByColumns + ",";

            var TotalDataQuery =
                $@"	SELECT fs.MarketYearID Season, {baseColumnsWithComma} SUM([fs].Volume) Quantity
												FROM            [dbo].[vw_Fact_SalesGrossUp] (NOLOCK)	fs
																	INNER JOIN [dbo].[vw_BrandSecurity_County] (NOLOCK) bc ON bc.CountyID = fs.CountyID AND bc.ProductBrandID = fs.ProductBrandID AND bc.MarketYearID = fs.MarketYearID AND bc.ManufacturerID = fs.ManufacturerID
                                                                       INNER JOIN  Dim_DateDay (NOLOCK) AS d ON fs.DayID = d.DateID 
																	   INNER JOIN vw_Dim_Products (NOLOCK) AS p ON fs.ProductID = p.ProductID 		
																	   INNER JOIN Dim_ProductUom u ON u.ProductUomID = p.ProductUOMID
                                                                              INNER JOIN vw_Dim_TerritoryLevel1 AS t ON bc.TerritoryLevel1ID = t.TerritoryIDLevel1ID 
																			  INNER JOIN vw_Dim_Manufacturers m ON m.ManufacturerID = fs.ManufacturerID
																			  INNER JOIN Dim_DateMarketYear AS dm ON fs.MarketYearID = dm.MarketYearID																															
																			  INNER JOIN vw_Dim_ProductBrand (NOLOCK) pb ON pb.ProductBrandID = bc.MarketShare_ProductBrandID	
                                                                       INNER JOIN vw_Accounts_Distributor ad ON ad.DistributorAccountID = fs.DistributorAccountID
                                                   WHERE    
													  (fs.SaleTypeID IN (2)) AND (t.TenantID IN (101)) 
													 AND Released = 1		
											        AND dm.YearsFromCurrent IN (0)
													AND MarketShare_DisplayIndicator NOT IN ('Hide')																													
													{appendQuery}
													GROUP BY {groupColumnsWithComma} fs.MarketYearID

													UNION ALL 

													SELECT fs.MarketYearID Season, {baseColumnsWithComma} SUM([fs].Volume) Quantity
												FROM            [dbo].[vw_Fact_SalesGrossUp] (NOLOCK)	fs
																	INNER JOIN [dbo].[vw_BrandSecurity_County] (NOLOCK) bc ON bc.CountyID = fs.CountyID AND bc.ProductBrandID = fs.ProductBrandID AND bc.MarketYearID = fs.MarketYearID AND bc.ManufacturerID = fs.ManufacturerID
                                                                       INNER JOIN  Dim_DateDay (NOLOCK) AS d ON fs.DayID = d.DateID 
																	   INNER JOIN vw_Dim_Products (NOLOCK) AS p ON fs.ProductID = p.ProductID 		
																	   INNER JOIN Dim_ProductUom u ON u.ProductUomID = p.ProductUOMID
                                                                              INNER JOIN vw_Dim_TerritoryLevel1 AS t ON bc.TerritoryLevel1ID = t.TerritoryIDLevel1ID 
																			  INNER JOIN vw_Dim_Manufacturers m ON m.ManufacturerID = fs.ManufacturerID
																			  INNER JOIN Dim_DateMarketYear AS dm ON fs.MarketYearID = dm.MarketYearID																															
																			  INNER JOIN vw_Dim_ProductBrand (NOLOCK) pb ON pb.ProductBrandID = bc.MarketShare_ProductBrandID	
                                                                       INNER JOIN vw_Accounts_Distributor ad ON ad.DistributorAccountID = fs.DistributorAccountID
                                                   WHERE    
													  (fs.SaleTypeID IN (2)) AND (t.TenantID IN (101)) 
													 AND Released = 1		
											        AND dm.YearsFromCurrent IN (1)
													AND d.IncludeMarketToDate = 1
													AND MarketShare_DisplayIndicator NOT IN ('Hide')																													
													{appendQuery}								
													GROUP BY {groupColumnsWithComma} fs.MarketYearID


													UNION ALL 

													SELECT fs.MarketYearID Season, {baseColumnsWithComma} SUM([fs].Volume) Quantity
												FROM            [dbo].[vw_Fact_SalesGrossUp] (NOLOCK)	fs
																	INNER JOIN [dbo].[vw_BrandSecurity_County] (NOLOCK) bc ON bc.CountyID = fs.CountyID AND bc.ProductBrandID = fs.ProductBrandID AND bc.MarketYearID = fs.MarketYearID AND bc.ManufacturerID = fs.ManufacturerID
                                                                       INNER JOIN  Dim_DateDay (NOLOCK) AS d ON fs.DayID = d.DateID 
																	   INNER JOIN vw_Dim_Products (NOLOCK) AS p ON fs.ProductID = p.ProductID 		
																	   INNER JOIN Dim_ProductUom u ON u.ProductUomID = p.ProductUOMID
                                                                              INNER JOIN vw_Dim_TerritoryLevel1 AS t ON bc.TerritoryLevel1ID = t.TerritoryIDLevel1ID 
																			  INNER JOIN vw_Dim_Manufacturers m ON m.ManufacturerID = fs.ManufacturerID
																			  INNER JOIN Dim_DateMarketYear AS dm ON fs.MarketYearID = dm.MarketYearID																															
																			  INNER JOIN vw_Dim_ProductBrand (NOLOCK) pb ON pb.ProductBrandID = bc.MarketShare_ProductBrandID	
                                                                       INNER JOIN vw_Accounts_Distributor ad ON ad.DistributorAccountID = fs.DistributorAccountID
                                                   WHERE    
													  (fs.SaleTypeID IN (2)) AND (t.TenantID IN (101)) 
													 AND Released = 1		
													 AND d.IncludeMarketToDate = 1
											        AND dm.YearsFromCurrent IN (2)
													AND MarketShare_DisplayIndicator NOT IN ('Hide')																													
													{appendQuery}
													GROUP BY {groupColumnsWithComma} fs.MarketYearID";

            var response = DBHelper.ExecuteSqlString<ProductQuantityBarGraphDTO>(TestConfig.ImpactVetConnectionString, TotalDataQuery, 200).ToList();

            return response;
        }


        public static List<FilterOptionsWithData> GetUnmaskedProductsForPricing(CommonFilters filters = null)
        {
            var appendQuery = String.Empty;

            if (filters != null)
                appendQuery = GetQueryAppendStringBasedOnFilters(filters);

             var query = File.ReadAllText(@"Resources\Pricing_BrandSecurityQuery.sql");            

            var response = DBHelper.ExecuteSqlString<FilterOptionsWithData>(TestConfig.ImpactVetConnectionString, query, 200).ToList();

            foreach (var item in response)
            {
                item.Product = item.Product?.Trim();
                item.Uom = item.Uom?.Trim();
            }

            return response;
        }

        public static List<FilterOptionsWithData> GetUnmaskedProductsForSupplyPlanning(CommonFilters filters = null)
        {
            var appendQuery = String.Empty;

            if (filters != null)
                appendQuery = GetQueryAppendStringBasedOnFilters(filters);

            var query = File.ReadAllText(@"Resources\SupplyPlanning_BrandSecurityQuery.sql");

            var response = DBHelper.ExecuteSqlString<FilterOptionsWithData>(TestConfig.ImpactVetConnectionString, query, 350).ToList();

            foreach (var item in response)
            {
                item.Product = item.Product?.Trim();
                item.Uom = item.Uom?.Trim();
            }

            return response;
        }

        public static List<ProductPriceTrendsTable> GetPricePerUomVarianceTableValuesBasedOnProductName(CommonFilters filters, bool onlyMaskedProducts = false, bool includeUseRates = false)
        {
            string maskedProducts = string.Empty;
            string useRate = "1";
            var appendQuery = GetQueryAppendStringBasedOnFilters(filters);

            if (onlyMaskedProducts)
            {
                maskedProducts = "AND bc.Pricing_DisplayIndicator = 'Mask'";
            }

            if (includeUseRates)
            {
                useRate = "ur.UseRate";
            }

            var TotalDataQuery =
                $@"SELECT        ProductBrandDesc Product, ProductUom UoM, ManufacturerDesc Manufacturer, BrandTransactionRange ProductTransactionRange, ProductTransactionTypeDesc TransactionVolume, cast(Avg(aggPrice/aggCount) as decimal(15,1))AvgPricePerUoM, cast(ISNULL(STDEV(aggPrice / aggCount), 0.00) as decimal(15,2)) AS PriceVariance
                   FROM            (SELECT        ProductBrandDesc, ManufacturerDesc, MonthID, ProductUom, BrandTransactionRange, ProductTransactionTypeDesc, sum(sales) aggSales,sum(Volume) aggVolume, SUM(price) AS aggPrice, SUM(trans_count) AS aggCount
                          FROM            (SELECT      bc.ProductBrandID, pb.ProductBrandDesc, m.ManufacturerDesc, fs.MonthID, u.ProductUOMDesc AS ProductUom, BrandTransactionRange, ProductTransactionTypeDesc, 
                                                                            fs.FactSalesID, sum(fs.FS_Sales_Amount) Sales, sum(fs_volume) Volume , SUM(fs.Price  * {useRate}) AS price, COUNT(fs.FactSalesID) AS trans_count
                                                    FROM            vw_Fact_Sales AS fs INNER JOIN
                                                                              vw_BrandSecurity_County AS bc ON fs.MarketYearID = bc.MarketYearID AND fs.ProductBrandID = bc.ProductBrandID AND fs.CountyID = bc.CountyID 
																			  INNER JOIN [dbo].[Dim_DateDay] d ON fs.DayID = d.DateID
																		      INNER JOIN vw_Dim_ProductBrand pb ON pb.ProductBrandId = bc.Pricing_Masked_ProductBrandId
																		      INNER JOIN vw_Dim_Products p ON p.ProductId = fs.ProductId
																		      INNER JOIN vw_Dim_ProductUom u ON u.ProductUomId = p.ProductUOMId
																		      INNER JOIN [dbo].[vw_Dim_TerritoryLevel1] t on bc.[TerritoryLevel1ID] = t.[TerritoryIDLevel1ID]
																			  INNER JOIN dbo.Dim_ProductTransactionType tt ON tt.ProductTransactionTypeID = p.ProductTransactionTypeID
                                                                              INNER JOIN [dbo].[Dim_DateMarketYear] dm ON dm.MarketYearID = fs.MarketYearID
																			  INNER JOIN dbo.Dim_Manufacturers m ON m.ManufacturerID = fs.ManufacturerID
                                                                              INNER JOIN [dbo].[vw_UserProductBrandRates] ur On ur.ProductBrandID = fs.ProductBrandID AND ur.UserLogin = 'DUSTIN.WANDER@AGDATA.COM'
                                                    WHERE      dm.YearsFromCurrent IN (0)
                                                    AND Released = 1
													AND (fs.SaleTypeID IN (2)) 
													AND (bc.TenantID IN (101)) 											
													AND bc.Pricing_DisplayIndicator != 'Hide'
                                                    AND ISOUTLIER <> 1
                                                    {maskedProducts}
                                                    {appendQuery}
                                                    GROUP BY bc.ProductBrandID, pb.ProductBrandDesc, m.ManufacturerDesc, fs.MonthID, u.ProductUOMDesc, BrandTransactionRange, ProductTransactionTypeDesc, fs.FactSalesID) 
                                                    AS main
                            GROUP BY ProductBrandDesc, ManufacturerDesc, MonthID, ProductUom, FactSalesID, BrandTransactionRange, ProductTransactionTypeDesc) AS sliced
                       GROUP BY GROUPING SETS ((), ( ProductBrandDesc, ManufacturerDesc, ProductUom, BrandTransactionRange, ProductTransactionTypeDesc))";

            var response = DBHelper.ExecuteSqlString<ProductPriceTrendsTable>(TestConfig.ImpactVetConnectionString, TotalDataQuery, 200).ToList();

            return response;
        }

        public static List<ProductPriceAcreTable> GetProductPricePerAcreTableValuesBasedOnProductName(CommonFilters filters, bool onlyMaskedProducts = false)
        {
            string maskedProducts = string.Empty;
            var appendQuery = GetQueryAppendStringBasedOnFilters(filters);

            if (onlyMaskedProducts)
            {
                maskedProducts = "AND bc.Pricing_DisplayIndicator = 'Mask'";
            }

            var TotalDataQuery =
                $@"
                 WITH CY AS(
                 SELECT       ProductUom, ProductBrandDesc,  ManufacturerDesc, UseRate UseRate, Avg(aggPrice/aggCount) AvgPricePerAcre, cast(STDEV(aggPrice / aggCount) as decimal(15,2)) AS Stdev_avg_price
                 FROM            (SELECT        ProductBrandDesc, ManufacturerDesc, MonthID, ProductUom, BrandTransactionRange, ProductTransactionTypeDesc, UseRate, sum(sales) aggSales,sum(Volume) aggVolume, SUM(price) AS aggPrice, SUM(trans_count) AS aggCount
                                           FROM            (SELECT     bc.ProductBrandID, pb.ProductBrandDesc, m.ManufacturerDesc, fs.MonthID, u.ProductUOMDesc AS ProductUom, BrandTransactionRange, ProductTransactionTypeDesc, ur.UseRate UseRate, 
                                                                                             fs.FactSalesID, sum(fs.FS_Sales_Amount) Sales, sum(fs_volume) Volume , SUM(fs.Price)* ur.UseRate AS price, COUNT(fs.FactSalesID) AS trans_count                                                
                 												   FROM            vw_Fact_Sales AS fs INNER JOIN
                                                                                               vw_BrandSecurity_County AS bc ON fs.MarketYearID = bc.MarketYearID AND fs.ProductBrandID = bc.ProductBrandID AND fs.CountyID = bc.CountyID 
                 																			  INNER JOIN [dbo].[Dim_DateDay] d ON fs.DayID = d.DateID AND d.MonthID = fs.MonthID
                 																		      INNER JOIN vw_Dim_ProductBrand pb ON pb.ProductBrandId = bc.Pricing_Masked_ProductBrandId
                 																		      INNER JOIN vw_Dim_Products p ON p.ProductId = fs.ProductId
                 																		      INNER JOIN vw_Dim_ProductUom u ON u.ProductUomId = p.ProductUOMId
                 																		      INNER JOIN [dbo].[vw_Dim_TerritoryLevel1] t on bc.[TerritoryLevel1ID] = t.[TerritoryIDLevel1ID]
                 																			  INNER JOIN dbo.Dim_ProductTransactionType tt ON tt.ProductTransactionTypeID = p.ProductTransactionTypeID
                 																			  INNER JOIN dbo.Dim_Manufacturers m ON m.ManufacturerID = fs.ManufacturerID
                                                                                              INNER JOIN [dbo].[Dim_DateMarketYear] dm ON dm.MarketYearID = fs.MarketYearID
                 																			  INNER JOIN [dbo].[vw_UserProductBrandRates] ur On ur.ProductBrandID = fs.ProductBrandID AND ur.UserLogin = 'DUSTIN.WANDER@AGDATA.COM'
                                                                     WHERE    dm.YearsFromCurrent = 0   AND (d.IncludeMarketToDate IN (1)) 
                 													AND (fs.SaleTypeID IN (2)) 
                 													AND (bc.TenantID IN (101))   
                                                                    AND Released = 1
                                                                    AND ISOUTLIER <> 1
                 													AND bc.Pricing_DisplayIndicator != 'Hide'                  												                 											
                 													{appendQuery}
                                                                    {maskedProducts}
                                                                     GROUP BY bc.ProductBrandID, pb.ProductBrandDesc, m.ManufacturerDesc, fs.MonthID, u.ProductUOMDesc, BrandTransactionRange, ProductTransactionTypeDesc, ur.UseRate, fs.FactSalesID) 
                                                                     AS main
                                           GROUP BY ProductBrandDesc, ManufacturerDesc, MonthID, ProductUom, FactSalesID, BrandTransactionRange, ProductTransactionTypeDesc, UseRate) AS sliced
                 GROUP BY ProductBrandDesc, ManufacturerDesc, ProductUom, BrandTransactionRange, ProductTransactionTypeDesc, UseRate
                 ), PY AS
                 (
                 SELECT       ProductUom, ProductBrandDesc,  ManufacturerDesc, UseRate UseRate, Avg(aggPrice/aggCount) AvgPricePerAcre, cast(STDEV(aggPrice / aggCount) as decimal(15,2)) AS Stdev_avg_price
                 FROM            (SELECT        ProductBrandDesc, ManufacturerDesc, MonthID, ProductUom, BrandTransactionRange, ProductTransactionTypeDesc, UseRate, sum(sales) aggSales,sum(Volume) aggVolume, SUM(price) AS aggPrice, SUM(trans_count) AS aggCount
                                           FROM            (SELECT     bc.ProductBrandID, pb.ProductBrandDesc, m.ManufacturerDesc, fs.MonthID, u.ProductUOMDesc AS ProductUom, BrandTransactionRange, ProductTransactionTypeDesc, ur.UseRate UseRate, 
                                                                                             fs.FactSalesID, sum(fs.FS_Sales_Amount) Sales, sum(fs_volume) Volume , SUM(fs.Price)* ur.UseRate AS price, COUNT(fs.FactSalesID) AS trans_count                                                
                 												   FROM            vw_Fact_Sales AS fs INNER JOIN
                                                                                               vw_BrandSecurity_County AS bc ON fs.MarketYearID = bc.MarketYearID AND fs.ProductBrandID = bc.ProductBrandID AND fs.CountyID = bc.CountyID 
                 																			  INNER JOIN [dbo].[Dim_DateDay] d ON fs.DayID = d.DateID AND d.MonthID = fs.MonthID
                 																		      INNER JOIN vw_Dim_ProductBrand pb ON pb.ProductBrandId = bc.Pricing_Masked_ProductBrandId
                 																		      INNER JOIN vw_Dim_Products p ON p.ProductId = fs.ProductId
                 																		      INNER JOIN vw_Dim_ProductUom u ON u.ProductUomId = p.ProductUOMId
                 																		      INNER JOIN [dbo].[vw_Dim_TerritoryLevel1] t on bc.[TerritoryLevel1ID] = t.[TerritoryIDLevel1ID]
                 																			  INNER JOIN dbo.Dim_ProductTransactionType tt ON tt.ProductTransactionTypeID = p.ProductTransactionTypeID
                 																			  INNER JOIN dbo.Dim_Manufacturers m ON m.ManufacturerID = fs.ManufacturerID
                                                                                              INNER JOIN [dbo].[Dim_DateMarketYear] dm ON dm.MarketYearID = fs.MarketYearID
                 																			  INNER JOIN [dbo].[vw_UserProductBrandRates] ur On ur.ProductBrandID = fs.ProductBrandID AND ur.UserLogin = 'DUSTIN.WANDER@AGDATA.COM'
                                                                     WHERE    dm.YearsFromCurrent = 1   AND (d.IncludeMarketToDate IN (1)) 
                 													AND (fs.SaleTypeID IN (2)) 
                 													AND (bc.TenantID IN (101)) 
                                                                    AND Released = 1
                                                                    AND ISOUTLIER <> 1
                 													AND bc.Pricing_DisplayIndicator != 'Hide'                  												                 													
                 													{appendQuery}
                                                                    {maskedProducts}
                                                                     GROUP BY bc.ProductBrandID, pb.ProductBrandDesc, m.ManufacturerDesc, fs.MonthID, u.ProductUOMDesc, BrandTransactionRange, ProductTransactionTypeDesc, ur.UseRate, fs.FactSalesID) 
                                                                     AS main
                                           GROUP BY ProductBrandDesc, ManufacturerDesc, MonthID, ProductUom, FactSalesID, BrandTransactionRange, ProductTransactionTypeDesc, UseRate) AS sliced
                 GROUP BY ProductBrandDesc, ManufacturerDesc, ProductUom, BrandTransactionRange, ProductTransactionTypeDesc, UseRate
                 )
                 
                 SELECT CASE WHEN cy.ProductUom IS NULL THEN py.ProductUom ELSE cy.ProductUom END AS Uom
                 , CASE WHEN cy.ProductBrandDesc IS NULL THEN py.ProductBrandDesc ELSE cy.ProductBrandDesc END AS [Product]
                 , CASE WHEN cy.ManufacturerDesc IS NULL THEN py.ManufacturerDesc ELSE cy.ManufacturerDesc END AS Manufacturer
                 , CASE WHEN cy.UseRate IS NULL THEN cast(ROUND(py.UseRate, 4) as decimal(15,4)) ELSE cast(ROUND(cy.UseRate, 4) as decimal(15,4)) END AS UseRate
                 , CASE WHEN cy.AvgPricePerAcre IS NULL THEN '0.0' ELSE cast(cy.AvgPricePerAcre as decimal(15,1)) END AS CyAvgPrice
                 , CASE WHEN cy.Stdev_avg_price IS NULL THEN '0.0' ELSE cast(cy.Stdev_avg_price as decimal(15,1)) END AS PriceVariance
                 , cast(SUM(CASE WHEN cy.AvgPricePerAcre IS NULL THEN 0 ELSE cast(cy.AvgPricePerAcre as decimal(15,1)) END-py.AvgPricePerAcre) as decimal(15,2)) ChangeInPriceYoy
                 FROM CY cy
                 FULL OUTER JOIN PY py ON py.ProductUom = cy.ProductUom AND py.ProductBrandDesc = cy.ProductBrandDesc AND py.ManufacturerDesc = CY.ManufacturerDesc
                 GROUP BY CASE WHEN cy.ProductUom IS NULL THEN py.ProductUom ELSE cy.ProductUom END, CASE WHEN cy.ProductBrandDesc IS NULL THEN py.ProductBrandDesc ELSE cy.ProductBrandDesc END
                 , CASE WHEN cy.ManufacturerDesc IS NULL THEN py.ManufacturerDesc ELSE cy.ManufacturerDesc END
                 , CASE WHEN cy.UseRate IS NULL THEN cast(ROUND(py.UseRate, 4) as decimal(15,4)) ELSE cast(ROUND(cy.UseRate, 4) as decimal(15,4)) END, cy.AvgPricePerAcre, cy.Stdev_avg_price";

            var response = DBHelper.ExecuteSqlString<ProductPriceAcreTable>(TestConfig.ImpactVetConnectionString, TotalDataQuery, 60).ToList();

            return response;
        }

        public static List<ProductPricePerAcreBarGraph> GetProductPricePerAcreBarChartValuesBasedOnProductName(CommonFilters filters)
        {
            string maskedProducts = string.Empty;
            var appendQuery = GetQueryAppendStringBasedOnFilters(filters);

            var TotalDataQuery =
               $@"
                 WITH CY AS
                 (
                 Select DISTINCT ProductBrandDesc, fs.MarketYearID, m.ManufacturerDesc, ROUND(AVG(Price*ur.UseRate), 2) Price
                 FROM vw_Fact_Sales fs 
                 INNER JOIN vw_BrandSecurity_County bc ON bc.ProductBrandId = fs.ProductBrandID AND bc.MarketYearId = fs.MarketYearID AND bc.CountyId = fs.CountyID
                 JOIN [dbo].[Dim_DateDay] d ON fs.DayID = d.DateID AND d.MonthID = fs.MonthID
                 INNER JOIN vw_Dim_ProductBrand pb ON pb.ProductBrandId = bc.Pricing_Masked_ProductBrandId
                 INNER JOIN vw_Dim_Products p ON p.ProductId = fs.ProductId
                 INNER JOIN vw_Dim_ProductUom u ON u.ProductUomId = p.ProductUOMId
                 --INNER JOIN vw_Accounts_Distributor r ON r.DistributorAccountID = fs.DistributorAccountID
                 INNER JOIN [dbo].[vw_Dim_TerritoryLevel1] t on bc.[TerritoryLevel1ID] = t.[TerritoryIDLevel1ID]
                 INNER JOIN [dbo].[vw_Dim_ProductTransactionType] tt ON tt.ProductTransactionTypeID = p.ProductTransactionTypeId
                 INNER JOIN dbo.Dim_Manufacturers m ON m.ManufacturerID = fs.ManufacturerID
                 INNER JOIN [dbo].[Dim_DateMarketYear] dm ON dm.MarketYearID = fs.MarketYearID
                 INNER JOIN [dbo].[vw_UserProductBrandRates] ur On ur.ProductBrandID = fs.ProductBrandID AND ur.UserLogin = 'DUSTIN.WANDER@AGDATA.COM'
                 WHERE dm.YearsFromCurrent = 0
                 AND IncludeMarketToDate = 1                  
                 AND fs.SaleTypeID = 2
                 AND Released = 1
                 AND bc.TenantId = 101
                 AND ISOUTLIER <> 1
                 AND bc.Pricing_DisplayIndicator <> 'HIDE'
                 {appendQuery}
                 {maskedProducts}
                 GROUP BY ProductBrandDesc, fs.MarketYearID, m.ManufacturerDesc
                 ), PY AS
                 (
                 Select DISTINCT ProductBrandDesc, fs.MarketYearID, m.ManufacturerDesc, ROUND(AVG(Price*ur.UseRate), 2) Price
                 FROM vw_Fact_Sales fs 
                 INNER JOIN vw_BrandSecurity_County bc ON bc.ProductBrandId = fs.ProductBrandID AND bc.MarketYearId = fs.MarketYearID AND bc.CountyId = fs.CountyID
                 JOIN [dbo].[Dim_DateDay] d ON fs.DayID = d.DateID AND d.MonthID = fs.MonthID
                 INNER JOIN vw_Dim_ProductBrand pb ON pb.ProductBrandId = bc.Pricing_Masked_ProductBrandId
                 INNER JOIN vw_Dim_Products p ON p.ProductId = fs.ProductId
                 INNER JOIN vw_Dim_ProductUom u ON u.ProductUomId = p.ProductUOMId
                 --INNER JOIN vw_Accounts_Distributor r ON r.DistributorAccountID = fs.DistributorAccountID
                 INNER JOIN [dbo].[vw_Dim_TerritoryLevel1] t on bc.[TerritoryLevel1ID] = t.[TerritoryIDLevel1ID]
                 INNER JOIN dbo.Dim_Manufacturers m ON m.ManufacturerID = fs.ManufacturerID
                 INNER JOIN [dbo].[Dim_DateMarketYear] dm ON dm.MarketYearID = fs.MarketYearID
                 INNER JOIN [dbo].[vw_UserProductBrandRates] ur On ur.ProductBrandID = fs.ProductBrandID AND ur.UserLogin = 'DUSTIN.WANDER@AGDATA.COM'
                 WHERE dm.YearsFromCurrent = 1          
                 AND fs.SaleTypeID = 2
                 AND Released = 1
                 AND bc.TenantId = 101
                 AND ISOUTLIER <> 1
                 AND bc.Pricing_DisplayIndicator <> 'HIDE'   
                 {appendQuery}
                 {maskedProducts}
                 GROUP BY ProductBrandDesc, fs.MarketYearID, m.ManufacturerDesc
                 ), PY2 AS
                 (
                 Select DISTINCT ProductBrandDesc, fs.MarketYearID, m.ManufacturerDesc, ROUND(AVG(Price*ur.UseRate), 2) Price
                 FROM vw_Fact_Sales fs 
                 INNER JOIN vw_BrandSecurity_County bc ON bc.ProductBrandId = fs.ProductBrandID AND bc.MarketYearId = fs.MarketYearID AND bc.CountyId = fs.CountyID
                 JOIN [dbo].[Dim_DateDay] d ON fs.DayID = d.DateID AND d.MonthID = fs.MonthID
                 INNER JOIN vw_Dim_ProductBrand pb ON pb.ProductBrandId = bc.Pricing_Masked_ProductBrandId
                 INNER JOIN vw_Dim_Products p ON p.ProductId = fs.ProductId
                 --INNER JOIN vw_Accounts_Distributor r ON r.DistributorAccountID = fs.DistributorAccountID
                 INNER JOIN vw_Dim_ProductUom u ON u.ProductUomId = p.ProductUOMId
                 INNER JOIN [dbo].[vw_Dim_TerritoryLevel1] t on bc.[TerritoryLevel1ID] = t.[TerritoryIDLevel1ID]
                 INNER JOIN dbo.Dim_Manufacturers m ON m.ManufacturerID = fs.ManufacturerID
                 INNER JOIN [dbo].[Dim_DateMarketYear] dm ON dm.MarketYearID = fs.MarketYearID
                 INNER JOIN [dbo].[vw_UserProductBrandRates] ur On ur.ProductBrandID = fs.ProductBrandID AND ur.UserLogin = 'DUSTIN.WANDER@AGDATA.COM'
                 WHERE dm.YearsFromCurrent = 2          
                 AND fs.SaleTypeID = 2
                 AND Released = 1
                 AND bc.TenantId = 101
                 AND ISOUTLIER <> 1
                 AND bc.Pricing_DisplayIndicator <> 'HIDE' 
                 {appendQuery}
                 {maskedProducts}
                 GROUP BY ProductBrandDesc, fs.MarketYearID, m.ManufacturerDesc
                 )
                 Select ProductBrandDesc Product, MarketYearId Season, ManufacturerDesc Manufacturer, SUM(ISNULL(cast(Price as decimal(15,2)), 0.0))  AveragePricePerAcre
                 FROM cy 
                 GROUP BY ProductBrandDesc, MarketYearId, ManufacturerDesc
                 UNION
                 SELECT ProductBrandDesc Product, MarketYearId Season, ManufacturerDesc Manufacturer, SUM(ISNULL(cast(Price as decimal(15,2)), 0.0))  AveragePricePerAcre
                 FROM PY
                 GROUP BY ProductBrandDesc, MarketYearId, ManufacturerDesc
                 UNION
                 SELECT ProductBrandDesc Product, MarketYearId Season, ManufacturerDesc Manufacturer, SUM(ISNULL(cast(Price as decimal(15,2)), 0.0))  AveragePricePerAcre
                 FROM PY2
                 GROUP BY ProductBrandDesc, MarketYearId, ManufacturerDesc
                 ";

            var response = DBHelper.ExecuteSqlString<ProductPricePerAcreBarGraph>(TestConfig.ImpactVetConnectionString, TotalDataQuery, 120).ToList();

            return response;
        }

        public static List<PricePerAcreExportTable> GetPricePerAcreExportTableValuesBasedOnSetFilters(CommonFilters filters, List<string> attributes, bool onlyMaskedProducts = false, bool includeUseRate = true)
        {
            string maskedProducts = string.Empty;
            string useRate = "1";
            var appendQuery = GetQueryAppendStringBasedOnFilters(filters, attributes);

            if (onlyMaskedProducts)
            {
                maskedProducts = "AND bc.Pricing_DisplayIndicator = 'Mask'";
            }

            if (includeUseRate)
            {
                useRate = "ur.UseRate";
            }

            var queryParts = GetAttributesBasedOnExportSettings(attributes);

            var TotalDataQuery =
               $@"
                 SELECT  ProductUom Uom, MarketYearID Season, UseRate UseRate {queryParts.Columns}
                    ,CASE WHEN Avg(aggPrice/aggCount)IS NULL THEN '0.0' ELSE cast(ROUND(Avg(aggPrice/aggCount), 1) as decimal(15,1)) END AS PricePerAcre, CASE WHEN STDEV(aggPrice / aggCount) IS NULL THEN '0.00' ELSE cast(ROUND(STDEV(aggPrice / aggCount), 1) as decimal(15,1)) END AS PriceVariance
                    FROM            (SELECT        MarketYearID, ProductUom, UseRate {queryParts.Columns}, sum(sales) aggSales,sum(Volume) aggVolume, SUM(price) AS aggPrice, SUM(trans_count) AS aggCount
                                              FROM            (SELECT     bc.ProductBrandID, fs.MarketYearID, u.ProductUOMDesc AS ProductUom, {useRate} UseRate {queryParts.BaseColumns}, 
                                                                                                fs.FactSalesID, sum(fs.FS_Sales_Amount) Sales, sum(fs_volume) Volume , SUM(fs.Price)* ({useRate}) AS price, COUNT(fs.FactSalesID) AS trans_count                                                
                                   												   FROM            vw_Fact_Sales AS fs INNER JOIN
                                                                                                  vw_BrandSecurity_County AS bc ON fs.MarketYearID = bc.MarketYearID AND fs.ProductBrandID = bc.ProductBrandID AND fs.CountyID = bc.CountyID 
                                   																			  INNER JOIN [dbo].[Dim_DateDay] d ON fs.DayID = d.DateID
                                   																		      INNER JOIN vw_Dim_ProductBrand pb ON pb.ProductBrandId = bc.Pricing_Masked_ProductBrandId                                                                                                            
                                   																		      INNER JOIN vw_Dim_Products p ON p.ProductId = fs.ProductId
                                   																		      INNER JOIN vw_Dim_ProductUom u ON u.ProductUomId = p.ProductUOMId                                   																		                                    																		
                                   																			  INNER JOIN dbo.Dim_Manufacturers m ON m.ManufacturerID = fs.ManufacturerID
                                   																			  INNER JOIN [dbo].[vw_UserProductBrandRates] ur On ur.ProductBrandID = fs.ProductBrandID AND ur.UserLogin = 'DUSTIN.WANDER@AGDATA.COM'                    																						
                                                                                                              {queryParts.Joins}
                                                                        WHERE  
                                   													(fs.SaleTypeID IN (2)) 
                                   													AND (bc.TenantID IN (101))                  												
                                                                                    AND ISOUTLIER <> 1
                                                                                    AND Released = 1
                                   													AND bc.Pricing_DisplayIndicator != 'Hide'                      																
                                                                                    {appendQuery}
                                                                                    {maskedProducts}
                                                                        GROUP BY bc.ProductBrandID, fs.MarketYearID, u.ProductUOMDesc, ur.UseRate, fs.FactSalesID {queryParts.GroupByColumns}) 
                                                                        AS main
                                              GROUP BY  MarketYearID, ProductUom, FactSalesID, UseRate {queryParts.Columns}) AS sliced
                    GROUP BY MarketYearID, ProductUom, UseRate {queryParts.Columns}";

            var response = DBHelper.ExecuteSqlString<PricePerAcreExportTable>(TestConfig.ImpactVetConnectionString, TotalDataQuery, 120).ToList();

            return response;
        }

        public static List<InventoryExportTable> GetManufacturerSupplyPlanningExportTableValuesBasedOnSetFilters(CommonFilters filters, List<string> attributes)
        {
            string maskedProducts = string.Empty;        
            var appendQuery = GetQueryAppendStringBasedOnFilters(filters, attributes);

            // Find the index of the second "AND"
            int firstAndIndex = appendQuery.IndexOf("AND", StringComparison.OrdinalIgnoreCase);
            int secondAndIndex = appendQuery.IndexOf("AND", firstAndIndex + 3, StringComparison.OrdinalIgnoreCase);

            // Keep everything from the second "AND" onward
            string trimmedAppendQuery = secondAndIndex >= 0 ? appendQuery.Substring(secondAndIndex).Trim() : appendQuery;

            var queryParts = GetAttributesBasedOnExportSettings(attributes);
            var finalColumns = string.Join(",",queryParts.Columns.Substring(2).Split(',').Select(item => "v." + item.Trim()));            
            var finalJoin = BuildJoinConditionFromString(queryParts.Columns, "v", "s");


            var TotalDataQuery =
               $@"; WITH Cte_InventoryVolume AS (
                     SELECT fs.MarketYearID {queryParts.BaseColumns}, FORMAT(CAST(CAST(fs.MonthID AS VARCHAR(6)) + '01' AS DATE), 'yyyy-MM') AS Date, u.ProductUomDesc , sum(fs.InventoryVolume) InventoryVolume  						
                      FROM [vw_Fact_Inventory]         fs
                      join [vw_BrandSecurity_County] bc on (fs.[MarketYearID] = bc.[Inventory_MarketYearID] and 
                                                                     fs.[ProductBrandID] = bc.[ProductBrandID] and 
                                                                     fs.[CountyId] = bc.[CountyId])                      
                      join [dbo].[Dim_DateDay]   d on (fs.[AsOfDayID] = d.[DateID])
                      join [dbo].[Dim_DateMarketYear]   dm on (fs.[MarketYearID] = dm.[MarketYearID])
                      LEFT join dbo.vw_Dim_Products vp ON vp.ProductID = fs.ProductID 
                      LEFT JOIN dbo.vw_Dim_ProductUom u ON u.ProductUomID  = vp.ProductUOMID 
                      INNER JOIN vw_Dim_ProductBrand pb ON pb.ProductBrandID = bc.Inventory_ProductBrandID
                      LEFT join [dbo].[vw_Accounts_Retailer] as r ON r.RetailerAccountID = fs.DistributorAccountID
                      Left Join [dbo].[vw_Dim_Manufacturers] as M on M.ManufacturerID = fs.ManufacturerID		
                      {queryParts.Joins}
                      WHERE   bc.[TenantID] in (101)  --this is the tenant you are logged in as
                      and fs.Released = 1  
                      and dm.YearsFromCurrent IN (0,1)					
                      AND Inventory_DisplayIndicator IS NOT NULL
                      {appendQuery}
                      GROUP BY fs.MarketYearID {queryParts.GroupByColumns}, FORMAT(CAST(CAST(fs.MonthID AS VARCHAR(6)) + '01' AS DATE), 'yyyy-MM') ,u.ProductUomDesc				 
                      ),Cte_SalesVolumne AS (
                      SELECT fs.MarketYearID + 1 MarketYearId {queryParts.BaseColumns}, u.ProductUomDesc, Sum(fs.[FS_Volume])  SalesVolumne	
                      FROM [vw_Fact_Sales] fs
                      join [vw_BrandSecurity_County] bc on (fs.[MarketYearID] + 1 = bc.[Inventory_MarketYearID] and 
                                                                     fs.[ProductBrandID] = bc.[ProductBrandID] and 
                                                                     fs.[CountyID] = bc.CountyID)
                       join [dbo].[Dim_DateDay] da on (fs.[DayID] = da.[DateID])                                         
                       join [dbo].[Dim_DateMarketYear]   dm on (dm.MarketYearID = bc.Inventory_MarketYearID)
                       LEFT join dbo.vw_Dim_Products vp ON vp.ProductID = fs.ProductID 
                       INNER JOIN vw_Dim_ProductBrand pb ON pb.ProductBrandID = bc.Inventory_ProductBrandID
                       LEFT JOIN dbo.vw_Dim_ProductUom u ON u.ProductUomID  = vp.ProductUOMID 
                       LEFT join [dbo].[vw_Accounts_Retailer] as r ON r.RetailerAccountID = fs.DistributorAccountID
                       Left Join [dbo].[vw_Dim_Manufacturers] as M on M.ManufacturerID = fs.ManufacturerID	
                       {queryParts.Joins}
                       WHERE  t.[TenantID] in (101)
                       and fs.Released = 1               
                       AND fs.InventorySubmitterFlag = 1
                       and dm.YearsFromCurrent IN (0, 1)  
                       {trimmedAppendQuery}
					   GROUP BY fs.MarketYearID + 1 {queryParts.GroupByColumns},u.ProductUomDesc		                     
                     ) 
                     
                     SELECT DISTINCT {finalColumns}, v.MarketYearID Season, v.Date, v.ProductUomDesc UOM                     
                     , CASE WHEN SUM(v.InventoryVolume) IS NULL THEN 0.00
                     WHEN SUM(s.SalesVolumne) IS NULL THEN 100.00
                     ELSE CAST(ROUND(ISNULL(SUM(v.InventoryVolume)/NULLIF(SUM(s.SalesVolumne), 0) * 100, 100.00), 2) AS decimal(10, 2)) END AS InventoryPercentOfSales
                     FROM Cte_InventoryVolume as v
                     FULL Outer JOIN Cte_SalesVolumne as s ON {finalJoin} AND v.MarketYearId = s.MarketYearId
                     GROUP BY {finalColumns}, v.MarketYearID, v.Date, v.ProductUomDesc";

            var response = DBHelper.ExecuteSqlString<InventoryExportTable>(TestConfig.ImpactVetConnectionString, TotalDataQuery, 120).ToList();

            return response;
        }

        public static List<PvmCalculationValuesDTO> GetMarketSharePVMDataBasedOnSetFilters(CommonFilters filters, string marketSharePage = "Manufacturer")
        {
            var appendQuery = GetQueryAppendStringBasedOnFilters(filters);
            ExportQueryParts queryParts = new();

            if (marketSharePage != "Manufacturer")
            {
                queryParts = SetQueryParts(marketSharePage);
            }

            var baseColumnsWithComma = string.IsNullOrWhiteSpace(queryParts.BaseColumns)
                   ? ""
                   : queryParts.BaseColumns + ",";

            var groupColumnsWithComma = string.IsNullOrWhiteSpace(queryParts.GroupByColumns)
                ? ""
                : queryParts.GroupByColumns + ",";

            var columnsWithComma = string.IsNullOrWhiteSpace(queryParts.Columns)
                ? ""
                : queryParts.Columns + ",";

            var TotalDataQuery = $@"SELECT	[fs].[SaleTypeID]  [SaleTypeID],
                                	[fs].[ProductID]  [ProductID],
                                    pb.ProductBrandDesc,
                                    {baseColumnsWithComma}
                                	[fs].[DistributorAccountID]  [AccountID],
                                	t.TerritoryLevel1Name  [TerritoryLevel1ID],
                                	[bc].[MarketShare_ProductBrandID]  [MaskedBrandID_MS],
                                	dm.YearsFromCurrent,
                                	[d].[MarketYearID]  [MarketYearID],
                                	right([d].[MonthID], 2)  [MonthNumber],
                                	ManufacturerDesc  [ManufacturerID],
                                	fs.CountyID,
                                	max(p.ProductUOMID) ProductUOMID,
                                	sum((Case when [fs].[Released] = 1 then [fs].[SalesAmount] else NULL end))  [SALES],
                                	sum((Case when [fs].[Released] = 1 then [fs].[Volume] else NULL end))  [QUANTITY],
                                	sum((Case when [fs].[Released] = 1 then [fs].[Price] else NULL end))  [PRICE],
                                	count(distinct [fs].[FactSalesGrossUpID])  [MARKETSHARECOUNT]
                                into ##T6GT2HSY7MD000
                                from	[dbo].[vw_Fact_SalesGrossUp]	[fs]
                                	join	[dbo].[vw_Dim_DateDay]	[d]
                                	  on 	([fs].[DayID] = [d].[DateID])
                                	INNER JOIN Dim_DateMarketYear AS dm ON fs.MarketYearID = dm.MarketYearID                                	 
                                	  JOIN dbo.vw_Dim_Manufacturers m ON m.ManufacturerID = fs.ManufacturerID	
                                    JOIN vw_Accounts_Distributor ad ON ad.DistributorAccountID = fs.DistributorAccountID
                                    join dbo.Dim_Products p on fs.ProductID = p.ProductID
                                	join	[dbo].[vw_BrandSecurity_County]	[bc]  on 	([fs].[CountyID] = [bc].[CountyID] and
                                	[fs].[MarketYearID] = [bc].[MarketShare_MarketYearID] and
                                	[fs].[MarketYearID] = [bc].[MarketYearID] and
                                	[fs].[ProductBrandID] = [bc].[ProductBrandID])
                                    JOIN vw_Dim_ProductBrand pb ON pb.ProductBrandID = bc.MarketShare_ProductBrandID
                                	JOIN dbo.vw_Dim_TerritoryLevel1 t ON t.TerritoryIDLevel1ID = bc.TerritoryLevel1ID
                                	JOIN Dim_ProductUom u ON u.ProductUomID = p.ProductUOMID
                                where	([fs].[SaleTypeID] = 2
                                 and dm.YearsFromCurrent in (0,1)
                                 and [bc].[TenantID] in (101))
                                {appendQuery}
                                and (d.IncludeMarketToDate = 1 OR
                                d.IncludeMarketToDatePY = 1)
                                group by	[fs].[SaleTypeID],
                                	[fs].[ProductID],
                                    pb.ProductBrandDesc,
                                    {groupColumnsWithComma}
                                	dm.YearsFromCurrent,
                                	[fs].[DistributorAccountID] ,
                                	t.TerritoryLevel1Name,
                                	[bc].[MarketShare_ProductBrandID],
                                	[d].[MarketYearID],
                                	right([d].[MonthID], 2),
                                	ManufacturerDesc,
                                	fs.CountyID
                                
                                SELECT A.MonthNumber     
                                           , A.TerritoryLevel1ID Territory
                                           ,a.ProductBrandDesc ProductBrand
                                           , A.MaskedBrandID_MS ProductBrandId
                                            {columnsWithComma}
                                           , a.productuomid 
                                           ,a.ManufacturerID ManufacturerName
                                           , max(dpu.ProductUomDesc)                                      ProductUomDesc
                                           , sum(quantity)                                                QTY
                                           , SUM(A.SALES)                                                 SALES
                                           , SUM(CASE WHEN a.YearsFromCurrent = 1 then a.sales else 0 end) SALES_PY
                                           , SUM(CASE WHEN a.YearsFromCurrent = 0 then a.sales else 0 end) SALES_CY
                                           , SUM(CASE WHEN a.YearsFromCurrent = 0 then a.sales else 0 end) -
                                             SUM(CASE WHEN a.YearsFromCurrent = 1 then a.sales else 0 end) SALES_YOY
                                            , SUM(CASE WHEN a.YearsFromCurrent = 1 then a.quantity else 0 end) QTY_PY
                                           , SUM(CASE WHEN a.YearsFromCurrent = 0 then a.quantity else 0 end) QTY_CY
                                            , case when SUM(CASE WHEN a.YearsFromCurrent = 1 then a.quantity else 0 end) <> 0 then (SUM(CASE WHEN a.YearsFromCurrent = 0 then a.quantity else 0 end) -
                                             SUM(CASE WHEN a.YearsFromCurrent = 1 then a.quantity else 0 end)) *
                                              (SUM(CASE WHEN a.YearsFromCurrent = 1 then a.sales else 0 end) /
                                               SUM(CASE WHEN a.YearsFromCurrent = 1 then a.quantity else 0 end)) end VolumeEffect
                                      from ##T6GT2HSY7MD000 a
                                               join dbo.Dim_ProductUom DPU on a.ProductUOMID = DPU.ProductUomID                                    
                                      group by
                                               A.MonthNumber,  {columnsWithComma} a.ProductBrandDesc, A.TerritoryLevel1ID, A.MaskedBrandID_MS, a.productuomid,a.ManufacturerID
                                
                                			   DROP TABLE ##T6GT2HSY7MD000";

            var response = DBHelper.ExecuteSqlString<PvmCalculationValuesDTO>(TestConfig.ImpactVetConnectionString, TotalDataQuery, 200).ToList();

            return response;
        }

        public static List<ManufacturerMarketShareTableDTO> GetManufacturerMarketShareTableDataBasedOnSetFilters(CommonFilters filters, string marketSharePage = "Manufacturer")
        {
            var appendQuery = GetQueryAppendStringBasedOnFilters(filters);

            var queryParts = SetQueryParts(marketSharePage);

            var TotalDataQuery = $@"WITH CurrentYearData AS
                                                   (
													SELECT fs.MarketYearID, coalesce({queryParts.GroupByColumns}, 'Total') {queryParts.Columns}, SUM(CASE WHEN d.IncludeMarketToDate = 1 THEN [fs].Volume ELSE 0 END) CYTD_Volume, SUM([fs].Volume) CY_Volume
												FROM            [dbo].[vw_Fact_SalesGrossUp] (NOLOCK)	fs
																	INNER JOIN [dbo].[vw_BrandSecurity_County] (NOLOCK) bc ON bc.CountyID = fs.CountyID AND bc.ProductBrandID = fs.ProductBrandID AND bc.MarketYearID = fs.MarketYearID AND bc.ManufacturerID = fs.ManufacturerID
                                                                       INNER JOIN  Dim_DateDay (NOLOCK) AS d ON fs.DayID = d.DateID 
																	   INNER JOIN vw_Dim_Products (NOLOCK) AS p ON fs.ProductID = p.ProductID 		
																	   INNER JOIN Dim_ProductUom u ON u.ProductUomID = p.ProductUOMID
                                                                              INNER JOIN vw_Dim_TerritoryLevel1 AS t ON bc.TerritoryLevel1ID = t.TerritoryIDLevel1ID 
																			  INNER JOIN vw_Dim_Manufacturers m ON m.ManufacturerID = fs.ManufacturerID
																			  INNER JOIN Dim_DateMarketYear AS dm ON fs.MarketYearID = dm.MarketYearID																															
																			  INNER JOIN vw_Dim_ProductBrand (NOLOCK) pb ON pb.ProductBrandID = bc.MarketShare_ProductBrandID	
                                                                              INNER JOIN vw_Accounts_Distributor ad ON ad.DistributorAccountID = fs.DistributorAccountID
                                                   WHERE    
													  (fs.SaleTypeID IN (2)) AND (t.TenantID IN (101)) 
													 AND Released = 1		
											        AND dm.YearsFromCurrent IN (0)
													AND MarketShare_DisplayIndicator NOT IN ('Hide')																													
													{appendQuery}
													GROUP BY GROUPING SETS ((fs.MarketYearID), ({queryParts.GroupByColumns}, fs.MarketYearID))
													), PastYearData AS
													(
													SELECT fs.MarketYearID, coalesce({queryParts.GroupByColumns}, 'Total') {queryParts.Columns}, SUM(CASE WHEN d.IncludeMarketToDate = 1 THEN [fs].Volume ELSE 0 END) PYTD_Volume, SUM([fs].Volume) PY_Volume
												FROM            [dbo].[vw_Fact_SalesGrossUp] (NOLOCK)	fs
																	INNER JOIN [dbo].[vw_BrandSecurity_County] (NOLOCK) bc ON bc.CountyID = fs.CountyID AND bc.ProductBrandID = fs.ProductBrandID AND bc.MarketYearID = fs.MarketYearID AND bc.ManufacturerID = fs.ManufacturerID
                                                                       INNER JOIN  Dim_DateDay (NOLOCK) AS d ON fs.DayID = d.DateID 
																	   INNER JOIN vw_Dim_Products (NOLOCK) AS p ON fs.ProductID = p.ProductID 		
																	   INNER JOIN Dim_ProductUom u ON u.ProductUomID = p.ProductUOMID
                                                                              INNER JOIN vw_Dim_TerritoryLevel1 AS t ON bc.TerritoryLevel1ID = t.TerritoryIDLevel1ID 
																			  INNER JOIN vw_Dim_Manufacturers m ON m.ManufacturerID = fs.ManufacturerID
																			  INNER JOIN Dim_DateMarketYear AS dm ON fs.MarketYearID = dm.MarketYearID																															
																			  INNER JOIN vw_Dim_ProductBrand (NOLOCK) pb ON pb.ProductBrandID = bc.MarketShare_ProductBrandID	
                                                                              INNER JOIN vw_Accounts_Distributor ad ON ad.DistributorAccountID = fs.DistributorAccountID
                                                   WHERE    
													  (fs.SaleTypeID IN (2)) AND (t.TenantID IN (101)) 
													 AND Released = 1		
											        AND dm.YearsFromCurrent IN (1)											
													AND MarketShare_DisplayIndicator NOT IN ('Hide')																													
													{appendQuery}								
													GROUP BY GROUPING SETS ((fs.MarketYearID), ({queryParts.GroupByColumns}, fs.MarketYearID))
													)
												
													SELECT coalesce(curr.{queryParts.Columns}, prev.{queryParts.Columns}) {queryParts.Columns}
													,CASE WHEN SUM(curr.CYTD_Volume) != NULL THEN SUM(ROUND(((curr.CYTD_Volume - prev.PYTD_Volume) / prev.PYTD_Volume) * 100, 2)) ELSE 0.00 END AS GrowthYoY
													,ISNULL(SUM(curr.CYTD_Volume), 0.00) QuantityYTD
													,ISNULL(SUM(prev.PYTD_Volume), 0.00) QuantityPYTD
													,ISNULL(SUM(prev.PY_Volume), 0.00) QuantityPY														
													FROM CurrentYearData curr
													FULL OUTER JOIN PastYearData prev ON prev.MarketYearID + 1 = curr.MarketYearID AND prev.{queryParts.Columns} = curr.{queryParts.Columns}
													GROUP BY coalesce(curr.{queryParts.Columns}, prev.{queryParts.Columns})";

            var response = DBHelper.ExecuteSqlString<ManufacturerMarketShareTableDTO>(TestConfig.ImpactVetConnectionString, TotalDataQuery, 200).ToList();
            ManufacturerMarketShareTableDTO total = new();
            List<ManufacturerMarketShareTableDTO> marketShare = new();

            if (response.FirstOrDefault().Manufacturer != null)
            {
                total = response.FirstOrDefault(t => t.Manufacturer == "Total");

                marketShare = response
                    .Where(r => r.Manufacturer != "Total")
                    .Select(r => new ManufacturerMarketShareTableDTO
                    {
                        Manufacturer = r.Manufacturer,
                        GrowthYoY = Math.Round(decimal.Parse(r.GrowthYoY), 2).ToString(),
                        QuantityYTD = r.QuantityYTD.MatchMarketShareKPIFormatting(),
                        QuantityPYTD = r.QuantityPYTD.MatchMarketShareKPIFormatting(),
                        QuantityPY = r.QuantityPY.MatchMarketShareKPIFormatting(),
                        MarketShareYTD = decimal.Parse(total.QuantityYTD) != 0
                                              ? Math.Round((decimal.Parse(r.QuantityYTD) / decimal.Parse(total.QuantityYTD)) * 100, 2).ToString("F2")
                                              : "0.00",
                        MarketSharePYTD = decimal.Parse(total.QuantityPYTD) != 0
                                              ? Math.Round((decimal.Parse(r.QuantityPYTD) / decimal.Parse(total.QuantityPYTD)) * 100, 2).ToString("F2")
                                              : "0.00",
                        MarketSharePY = decimal.Parse(total.QuantityPY) != 0
                                              ? Math.Round((decimal.Parse(r.QuantityPY) / decimal.Parse(total.QuantityPY)) * 100, 2).ToString("F2")
                                              : "0.00"

                    }).ToList();
            }
            else
            {
                total = response.FirstOrDefault(t => t.Retailer == "Total");

                marketShare = response
                    .Where(r => r.Retailer != "Total")
                    .Select(r => new ManufacturerMarketShareTableDTO
                    {
                        Retailer = r.Retailer,
                        GrowthYoY = Math.Round(decimal.Parse(r.GrowthYoY), 2).ToString(),
                        QuantityYTD = r.QuantityYTD.MatchMarketShareKPIFormatting(),
                        QuantityPYTD = r.QuantityPYTD.MatchMarketShareKPIFormatting(),
                        QuantityPY = r.QuantityPY.MatchMarketShareKPIFormatting(),
                        MarketShareYTD = decimal.Parse(total.QuantityYTD) != 0
                                              ? Math.Round((decimal.Parse(r.QuantityYTD) / decimal.Parse(total.QuantityYTD)) * 100, 2).ToString("F2")
                                              : "0.00",
                        MarketSharePYTD = decimal.Parse(total.QuantityPYTD) != 0
                                              ? Math.Round((decimal.Parse(r.QuantityPYTD) / decimal.Parse(total.QuantityPYTD)) * 100, 2).ToString("F2")
                                              : "0.00",
                        MarketSharePY = decimal.Parse(total.QuantityPY) != 0
                                              ? Math.Round((decimal.Parse(r.QuantityPY) / decimal.Parse(total.QuantityPY)) * 100, 2).ToString("F2")
                                              : "0.00"
                    }).ToList();

            }

            return marketShare;
        }
    }
}