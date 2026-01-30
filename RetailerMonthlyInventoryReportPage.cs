using AGData.Test.Framework.Helpers;
using AGData.Test.Framework.SeleniumExtensions;
using AGData.Test.MarketIntelligenceUS;
using OpenQA.Selenium;
using AGData.Test.MarketIntelligenceUS.Helpers;
using static AGData.Test.MarketIntelligenceUS.DTOs.SupplyPlanningDTO;
using MarketInteillignceUS.PageModel.UI.Test.PageObjects.Common;
using static AGData.Test.MarketIntelligenceUS.DTOs.Constants;
using OpenQA.Selenium.Interactions;

namespace MarketInteillignceUS.PageModel.RetailerSupplyPlanning
{
    class RetailerMonthlyInventoryReportPage : NavMenu
    {
        public RetailerMonthlyInventoryReportPage(IWebDriver driver) : base(driver)
        {
            _driver = driver;
            TimingHelper.WaitForResult(() => driver.Url.Contains("11200AD9405B4ACA94930088561AD206/47D771F142E75E77A97A619AD343DAFB/W20CC1CEA83D84552BCC07219A8CF90FB--K7F89185640457043769132858E5AB0E2"), 10000, 1000, "Took too long to load the Contract Info Page");
        }
        IWebDriver _driver;

        public string ReportURLString = "11200AD9405B4ACA94930088561AD206/47D771F142E75E77A97A619AD343DAFB/W20CC1CEA83D84552BCC07219A8CF90FB--K7F89185640457043769132858E5AB0E2";
        private IList<IWebElement> TabSection => _driver.Finds(By.XPath("//div[contains(@id,'*lK0B6C17A54A7A81B3BBE3F28735A806FF*kIGK17ABA33B4DB5F3B8490931B90F15BBDD')]//div[contains(text(),'Price')]"));
        public IWebElement ApplyFiltersButton => _driver.Find(By.XPath("//span[text()='Apply']//parent::span"));

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

        public static string MonthTableId = "*lWAE8A092F24654083BD52479A675E6C83*kK71681D4C42CD3373E01F5CB4905F49EE*";
        public static string MonthGridId = "*lWAE8A092F24654083BD52479A675E6C83*kKAAE2170549699426F4BBFC930FD66A6A*";

        public List<MonthTrendsGraph> GetGridData()
        {
            List<MonthTrendsGraph> data = new();
            Actions actions = new(_driver);
            ClickAway();
            WaitForGridToFinishLoading();
            var dataPoints = _driver.Finds(By.XPath($"//div[contains(@id,'{MonthGridId}')]//*[name()='svg']//*[name()='circle' and @size]"));

            foreach (var point in dataPoints)
            {
                MonthTrendsGraph graphValues = new();

                ClickAway();
                actions.MoveToElement(point).Perform();
                //point.Click();
                graphValues.Season = _driver.Find(By.XPath("//div[@class='vis-tooltip-container']//tbody//tr[1]//td[2]")).Text;
                graphValues.Month = _driver.Find(By.XPath("//div[@class='vis-tooltip-container']//tbody//tr[2]//td[2]")).Text;
                graphValues.InventoryPercentOfSales = _driver.Find(By.XPath("//div[@class='vis-tooltip-container']//tbody//tr[3]//td[2]")).Text.GetNumbersFromString();

                data.Add(graphValues);
            }

            return data;
        }

        public List<MonthTrendsTable> GetMonthTrendsTableData
        {
            get
            {
                WaitForGridToFinishLoading();
                var tableRowData = new List<MonthTrendsTable>();

                var rows = _driver.FindElements(By.XPath($"//div[contains(@id,'{MonthTableId}')]//table[contains(@aria-label,'Visualization')]//tr[@style]"));

                foreach (var row in rows.Where(x => x.Text != ""))
                {
                    MonthTrendsTable tableRow = new();

                    tableRow.Month = row.FindElement(By.XPath(".//td[1]")).Text;
                    tableRow.PY_InventoryAsSales = row.FindElement(By.XPath(".//td[3]")).Text.GetNumbersFromString();
                    tableRow.PY_Growth = row.FindElement(By.XPath(".//td[4]")).Text.GetNumbersFromString();
                    tableRow.CY_InventoryAsSales = row.FindElement(By.XPath(".//td[5]")).Text.GetNumbersFromString();
                    tableRow.CY_Growth = row.FindElement(By.XPath(".//td[6]")).Text.GetNumbersFromString();

                    tableRowData.Add(tableRow);
                }

                return tableRowData;
            }
        }
    }
}
