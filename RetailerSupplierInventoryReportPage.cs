using AGData.Test.Framework.Helpers;
using AGData.Test.Framework.SeleniumExtensions;
using AGData.Test.MarketIntelligenceUS;
using OpenQA.Selenium;
using AGData.Test.MarketIntelligenceUS.Helpers;
using MarketInteillignceUS.PageModel.UI.Test.PageObjects.Common;
using static AGData.Test.MarketIntelligenceUS.DTOs.Constants;
using OpenQA.Selenium.Interactions;
using static AGData.Test.MarketIntelligenceUS.DTOs.SupplyPlanningDTO;

namespace MarketInteillignceUS.PageModel.RetailerSupplyPlanning
{
    class RetailerSupplierInventoryReportPage : NavMenu
    {
        public RetailerSupplierInventoryReportPage(IWebDriver driver) : base(driver)
        {
            _driver = driver;
            TimingHelper.WaitForResult(() => driver.Url.Contains("11200AD9405B4ACA94930088561AD206/47D771F142E75E77A97A619AD343DAFB/K7EFC40074AECC6724902E9A00A38A05E--KAE638ACA41686D09E6857B97608EC7A2"), 10000, 1000, "Took too long to load the Contract Info Page");
        }
        IWebDriver _driver;

        public string ReportURLString = "11200AD9405B4ACA94930088561AD206/47D771F142E75E77A97A619AD343DAFB/K7EFC40074AECC6724902E9A00A38A05E--KAE638ACA41686D09E6857B97608EC7A2";
        private IList<IWebElement> TabSection => _driver.Finds(By.XPath("//div[contains(@id,'*lK0B6C17A54A7A81B3BBE3F28735A806FF*kIGK17ABA33B4DB5F3B8490931B90F15BBDD')]//div[contains(text(),'Price')]"));
        public IWebElement ApplyFiltersButton => _driver.Find(By.XPath("//span[text()='Apply']//parent::span"));

        public void SelectTabByName(string tabName)
        {
            WaitForGridToFinishLoading();
            TabSection.Where(x => x.Text == tabName).First().Click();
        }

        public List<string> GetFilters()
        {
            WaitForGridToFinishLoading();
            List<string> filterNames = new();
            FilterPageObject filterPageObject = new(_driver);
            var filters = filterPageObject.GetAvailableFilters();

            foreach (var filter in filters)
            {
                filterNames.Add(filter.Text);
            }

            return filterNames;
        }

        public List<string> GetFilterOptions(string filterName)
        {
            WaitForGridToFinishLoading();
            List<string> filterOptions = new();
            FilterPageObject filterPageObject = new(_driver);
            var options = filterPageObject.GetAvailableFilterOptions(filterName);

            foreach (var option in options)
            {
                filterOptions.Add(option.FilterName);
            }

            return filterOptions;
        }

        public void ClearAllFilters()
        {
            WaitForGridToFinishLoading();
            FilterPageObject filterPageObject = new(_driver);
            filterPageObject.ClearAllFilters();
            WaitForGridToFinishLoading();
            ApplyFilters();
        }

        //Use this when fitler is known to contain a large amount of items and you need the text for each one
        public List<string> GetExtendedFilterOptions(string filterName)
        {
            WaitForGridToFinishLoading();
            List<string> filterOptions = new();
            FilterPageObject filterPageObject = new(_driver);
            return filterPageObject.GetAllVirtualizedListItems(filterName);
        }

        public CommonFilters GetSetFilters()
        {
            WaitForGridToFinishLoading();
            FilterPageObject filterPage = new(_driver);
            return filterPage.GetSetFilters();
        }

        public void GetAndSetParametersWithData(int numberOfProducts = 1, bool randomSet = false, CommonFilters filters = null, string yearsFromCurrent = "0, 1", bool currentMarketYear = false)
        {
            var parametersWithData = UsMiDBHelper.GetInventoryFilterSelectionsWithData(filters, yearsFromCurrent: yearsFromCurrent, currentMarketYear);

            // Group all data by UOM
            var groupedByUom = parametersWithData
                .GroupBy(p => p.Uom)
                .ToList();

            // Select the UOM group with enough products
            var eligibleUomGroup = groupedByUom
                .FirstOrDefault(g => g.Select(x => x.Product).Distinct().Count() >= numberOfProducts);

            if (eligibleUomGroup == null)
                Assert.Inconclusive("Not enough data to validate"); // No UOM group has enough products to meet the selection count

            List<FilterOptionsWithData> parameter;

            if (randomSet)
            {
                Random rand = new Random();
                parameter = eligibleUomGroup
                    .GroupBy(p => p.Product)
                    .OrderBy(_ => rand.Next())
                    .Take(numberOfProducts)
                    .SelectMany(g => g)
                    .ToList();
            }
            else
            {
                parameter = eligibleUomGroup
                    .GroupBy(p => p.Product)
                    .Take(numberOfProducts)
                    .SelectMany(g => g)
                    .ToList();
            }

            SetFilterByFilterTitle("UOM", string.Join(", ", parameter.Select(p => p.Uom).Distinct()));
            SetFilterByFilterTitle("Region", string.Join(", ", parameter.SelectMany(p => p.Region.Split(',')).Select(m => m.Trim()).Distinct()));
            SetFilterByFilterTitle("Product", string.Join(", ", parameter.Select(p => p.Product).Distinct()));
            SetFilterByFilterTitle("Month", string.Join(", ", parameter.SelectMany(p => p.Month.Split(',')).Select(m => m.Trim()).Distinct()));

            WaitForGridToFinishLoading();
            ApplyFilters();
        }

        public void SetFilterByFilterTitle(string filterName, string fitlerSelections)
        {
            WaitForGridToFinishLoading();
            List<SelectedFilters> setFilters = new();
            FilterPageObject filterPageObject = new(_driver);

            filterPageObject.SelectFilterOptionsByFilterTitle(filterName, fitlerSelections);
        }

        public void ApplyFilters()
        {
            WaitForGridToFinishLoading();
            TimingHelper.WaitForResult(() => FilterPageObject.WaitUntilElementIsContinuouslyEnabled(ApplyFiltersButton));
            ApplyFiltersButton.Click();
        }

        public static string TotalKpiId = "*lWFAC231356A0C422B9497F39319F3CD0A*kW4130F795C69B42EDB1045DD93A92C990*";
        public static string ManufacturerKpiId = "*lWFAC231356A0C422B9497F39319F3CD0A*kWAD80334678EE4559BFB9750E7A867601*";
        public static string ManufacturerGridId = "*lWFAC231356A0C422B9497F39319F3CD0A*kK39443326459CB2AC09E3ACB934B56493*";

        Tuple<string, string>[] KpiStrings = new Tuple<string, string>[]
        {
            new Tuple<string, string>("TotalKpiId", $"{TotalKpiId}"),
            new Tuple<string, string>("SupplierKpiId", $"{ManufacturerKpiId}")
        };

        public List<InventoryKPIDataBaseValues> GetKPIValues
        {
            get
            {
                WaitForGridToFinishLoading();
                ClickAway();
                var kpis = new List<InventoryKPIDataBaseValues>();
                var kpiIdStrings = KpiStrings;

                foreach (var kpi in kpiIdStrings)
                {
                    var kpiDetails = new InventoryKPIDataBaseValues();

                    kpiDetails.KpiName = kpi.Item1;

                    if (kpi.Item1 == "TotalKpiId")
                    {
                        kpiDetails.Manufacturer = "Total";

                        var element = _driver.FindElement(By.XPath($"//div[contains(@id,'{kpi.Item2}')]//div[contains(@class,'primary')]"));
                        if (!string.IsNullOrWhiteSpace(element.Text))
                        {
                            kpiDetails.CY_InventoryAsPercentOfSales = _driver.Find(By.XPath($"//div[contains(@id,'{kpi.Item2}')]//div[contains(@class,'primary')]")).Text.GetNumbersFromString();
                        }
                        else
                        {
                            kpiDetails.CY_InventoryAsPercentOfSales = "0.00";
                        }

                        kpiDetails.PY_InventoryAsPercentOfSales = _driver.Find(By.XPath($"//div[contains(@id,'{kpi.Item2}')]//div[contains(text(),'PYTD')]")).Text.GetNumbersFromString();
                        kpiDetails.Growth = _driver.Find(By.XPath($"//div[contains(@id,'{kpi.Item2}')]//div[@class='card__comparison-indicator']")).Text.GetNumbersFromString();

                        if (kpiDetails.Growth == "")
                            kpiDetails.Growth = "0.0";

                        kpis.Add(kpiDetails);
                    }
                    else
                    {
                        if (_driver.IsElementPresent(By.XPath($"//div[contains(@id,'{kpi.Item2}')]//div[@class='controls controls--dot-indicators']//button")))
                        {
                            var buttons = _driver.Finds(By.XPath($"//div[contains(@id,'{kpi.Item2}')]//div[@class='controls controls--dot-indicators']//button"));
                            buttons.First().Click();
                            var manufacturerPages = _driver.Finds(By.XPath($"//article[contains(@id,'{kpi.Item2}')]"));

                            for (int i = 0; i < buttons.Count(); i++)
                            {
                                kpiDetails = new InventoryKPIDataBaseValues();
                                kpiDetails.KpiName = kpi.Item1;
                                _driver.Find(By.XPath($"//div[@class='controls controls--dot-indicators']//button[{i + 1}]")).Click();
                                WaitForGridToFinishLoading();

                                foreach (var page in manufacturerPages)
                                {
                                    if (IsElementVisibleAndContainsText(page, ".//header"))
                                    {
                                        kpiDetails.Manufacturer = page.FindElement(By.XPath($".//header")).Text;
                                        kpiDetails.CY_InventoryAsPercentOfSales = page.FindElement(By.XPath($".//div//div[contains(@class,'primary-value')]")).Text.GetNumbersFromString();
                                        kpiDetails.PY_InventoryAsPercentOfSales = page.FindElement(By.XPath($".//div//div[contains(@class,'secondary-value')]")).Text.GetNumbersFromString();
                                        kpiDetails.Growth = page.FindElement(By.XPath($".//div[@class='card__comparison-indicator']")).Text.GetNumbersFromString();

                                        if (kpiDetails.Growth == "")
                                            kpiDetails.Growth = "0.0";
                                        kpis.Add(kpiDetails);

                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            kpiDetails.Manufacturer = _driver.Find(By.XPath($"//div[contains(@id,'{kpi.Item2}')]//div[contains(@class,'primary')]//ancestor::article//header")).Text;
                            kpiDetails.CY_InventoryAsPercentOfSales = _driver.Find(By.XPath($"//div[contains(@id,'{kpi.Item2}')]//div[contains(@class,'primary')]")).Text.GetNumbersFromString();
                            kpiDetails.PY_InventoryAsPercentOfSales = _driver.Find(By.XPath($"//div[contains(@id,'{kpi.Item2}')]//div[contains(text(),'PYTD')]")).Text.GetNumbersFromString();
                            kpiDetails.Growth = _driver.Find(By.XPath($"//div[contains(@id,'{kpi.Item2}')]//div[@class='card__comparison-indicator']")).Text.GetNumbersFromString();

                            if (kpiDetails.Growth == "")
                                kpiDetails.Growth = "0.0";

                            kpis.Add(kpiDetails);
                        }
                    }
                }

                var distinctKpis = kpis.DistinctBy(p => new { p.Manufacturer }).ToList();

                return distinctKpis;
            }
        }

        public List<ManufacturerInventoryGraph> GetGridData()
        {
            List<ManufacturerInventoryGraph> data = new();
            Actions actions = new(_driver);
            WaitForGridToFinishLoading();
            var dataPoints = _driver.Finds(By.XPath($"//div[contains(@id,'{ManufacturerGridId}')]//*[name()='svg']//*[name()='rect']"));

            foreach (var point in dataPoints)
            {
                ManufacturerInventoryGraph graphValues = new();

                ClickAway();
                actions.MoveToElement(point).Perform();
                graphValues.Season = _driver.Find(By.XPath("//div[@class='vis-tooltip-container']//tbody//tr[1]//td[2]")).Text;
                graphValues.Manufacturer = _driver.Find(By.XPath("//div[@class='vis-tooltip-container']//tbody//tr[2]//td[2]")).Text;
                graphValues.InventoryPercentOfSales = _driver.Find(By.XPath("//div[@class='vis-tooltip-container']//tbody//tr[3]//td[2]")).Text.GetNumbersFromString();

                data.Add(graphValues);
            }

            return data;
        }
    }
}
