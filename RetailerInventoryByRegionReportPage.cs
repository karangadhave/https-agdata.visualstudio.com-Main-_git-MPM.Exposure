using AGData.Test.Framework.Helpers;
using AGData.Test.Framework.SeleniumExtensions;
using AGData.Test.MarketIntelligenceUS;
using OpenQA.Selenium;
using AGData.Test.MarketIntelligenceUS.Helpers;
using static AGData.Test.MarketIntelligenceUS.DTOs.SupplyPlanningDTO;
using MarketInteillignceUS.PageModel.UI.Test.PageObjects.Common;
using static AGData.Test.MarketIntelligenceUS.DTOs.Constants;

namespace MarketInteillignceUS.PageModel.RetailerSupplyPlanning
{
    class RetailerInventoryByRegionReportPage : NavMenu
    {
        public RetailerInventoryByRegionReportPage(IWebDriver driver) : base(driver)
        {
            _driver = driver;
            TimingHelper.WaitForResult(() => driver.Url.Contains("11200AD9405B4ACA94930088561AD206/47D771F142E75E77A97A619AD343DAFB/W20CC1CEA83D84552BCC07219A8CF90FB--K7F89185640457043769132858E5AB0E2"), 10000, 1000, "Took too long to load the Contract Info Page");
        }
        IWebDriver _driver;

        public string ReportURLString = "11200AD9405B4ACA94930088561AD206/47D771F142E75E77A97A619AD343DAFB/W20CC1CEA83D84552BCC07219A8CF90FB--K7F89185640457043769132858E5AB0E2";
        private IList<IWebElement> TabSection => _driver.Finds(By.XPath("//div[contains(@id,'*lK0B6C17A54A7A81B3BBE3F28735A806FF*kIGK17ABA33B4DB5F3B8490931B90F15BBDD')]//div[contains(text(),'Price')]"));
        public IWebElement ApplyFiltersButton => _driver.Find(By.XPath("//span[text()='Apply']//parent::span"));
        public RegionInventory RegionInventoryTab { get { return new RegionInventory(_driver); } }

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

        public void ClearAllFilters()
        {
            WaitForGridToFinishLoading();
            FilterPageObject filterPageObject = new(_driver);
            filterPageObject.ClearAllFilters();
            WaitForGridToFinishLoading();
            ApplyFilters();
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

            // Now all records in `parameter` will have the same UOM
            SetFilterByFilterTitle("UOM", string.Join(", ", parameter.Select(p => p.Uom).Distinct()));
            SetFilterByFilterTitle("Product", string.Join(", ", parameter.Select(p => p.Product).Distinct()));
            SetFilterByFilterTitle("Region", string.Join(", ", parameter.SelectMany(p => p.Region.Split(',')).Select(m => m.Trim()).Distinct()));
            // SetFilterByFilterTitle("Month", string.Join(", ", parameter.SelectMany(p => p.Month.Split(',')).Select(m => m.Trim()).Distinct()));

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

        public class RegionInventory : NavMenu
        {
            public RegionInventory(IWebDriver driver) : base(driver)
            {
                _driver = driver;
            }
            IWebDriver _driver;

            public static string TotalKpiId = "*lWFAC231356A0C422B9497F39319F3CD0A*kW2762D4E7163242338CDBD5553DB3A875*";
            public static string RegionKpiId = "*lWFAC231356A0C422B9497F39319F3CD0A*kW330210B19E294E7B9A9E731FB1D6D530*";         

            Tuple<string, string>[] KpiStrings = new Tuple<string, string>[]
            {
            new Tuple<string, string>("TotalKpiId", $"{TotalKpiId}"),
            new Tuple<string, string>("RegionKpiId", $"{RegionKpiId}")
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
                            kpiDetails.Territory = "Total";

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
                                            kpiDetails.Territory = page.FindElement(By.XPath($".//header")).Text;
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
                                kpiDetails.Territory = _driver.Find(By.XPath($"//div[contains(@id,'{kpi.Item2}')]//div[contains(@class,'primary')]//ancestor::article//header")).Text;
                                kpiDetails.CY_InventoryAsPercentOfSales = _driver.Find(By.XPath($"//div[contains(@id,'{kpi.Item2}')]//div[contains(@class,'primary')]")).Text.GetNumbersFromString();
                                kpiDetails.PY_InventoryAsPercentOfSales = _driver.Find(By.XPath($"//div[contains(@id,'{kpi.Item2}')]//div[contains(text(),'PYTD')]")).Text.GetNumbersFromString();
                                kpiDetails.Growth = _driver.Find(By.XPath($"//div[contains(@id,'{kpi.Item2}')]//div[@class='card__comparison-indicator']")).Text.GetNumbersFromString();

                                if (kpiDetails.Growth == "")
                                    kpiDetails.Growth = "0.0";

                                kpis.Add(kpiDetails);
                            }
                        }
                    }

                    var distinctKpis = kpis.DistinctBy(p => new { p.Territory }).ToList();

                    return distinctKpis;
                }
            }           
        }
    }
}
