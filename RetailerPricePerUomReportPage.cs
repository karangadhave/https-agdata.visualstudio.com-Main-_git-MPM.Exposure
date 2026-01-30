using AGData.Test.Framework.Helpers;
using AGData.Test.Framework.SeleniumExtensions;
using AGData.Test.MarketIntelligenceUS;
using OpenQA.Selenium;
using AGData.Test.MarketIntelligenceUS.Helpers;
using static AGData.Test.MarketIntelligenceUS.DTOs.PricingDTO;
using MarketInteillignceUS.PageModel.UI.Test.PageObjects.Common;
using static AGData.Test.MarketIntelligenceUS.DTOs.Constants;
using OpenQA.Selenium.Interactions;
using NPOI.XWPF.UserModel;

namespace MarketInteillignceUS.PageModel.RetailerPricing
{
    class RetailerPricePerUomReportPage : NavMenu
    {
        public RetailerPricePerUomReportPage(IWebDriver driver) : base(driver)
        {
            _driver = driver;
            TimingHelper.WaitForResult(() => driver.Url.Contains("11200AD9405B4ACA94930088561AD206/6D818378458EBCEBCD01069F41D16246/KEA314F7B48A49ACF8BA70088561A1A6C--K6E5B1AFF4D6C4E6E07B599B00A0072B8"), 10000, 1000, "Took too long to load the Contract Info Page");
        }
        IWebDriver _driver;

        public string ReportURLString = "11200AD9405B4ACA94930088561AD206/6D818378458EBCEBCD01069F41D16246/KEA314F7B48A49ACF8BA70088561A1A6C--K6E5B1AFF4D6C4E6E07B599B00A0072B8";
        private IList<IWebElement> TabSection => _driver.Finds(By.XPath("//div[contains(@id,'*lK7D5A443E4B7FA7EA9A57C5B16C4DDDB9*kIGKAFD739824893A57FD8186BA84B15F279')]//div[@class='item-text']"));
        public IWebElement ApplyFiltersButton => _driver.Find(By.XPath("//span[text()='Apply']//parent::span"));
        public PriceMap PriceMapTab { get { return new PriceMap(_driver); } }
        public YouVsOthersMap YouVsOthersMapTab { get { return new YouVsOthersMap(_driver); } }
        public PriceTrends PriceTrendsTab { get { return new PriceTrends(_driver); } }


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

        public void GetAndSetParametersWithData(bool randomSet = false, CommonFilters filters = null, string yearsFromCurrent = "0, 1", bool currentMarketYear = false)
        {
            var parametersWithData = UsMiDBHelper.GetFilterSelectionsWithData("Pricing", filters, yearsFromCurrent, currentMarketYear);

            if (parametersWithData.Count == 0)
                Assert.Inconclusive("No filters have data");

            FilterOptionsWithData parameter = new();

            if (randomSet)
            {
                Random rand = new();
                int parameterNumber = rand.Next(parametersWithData.Count());
                parameter = parametersWithData[parameterNumber];
            }
            else
            {
                parameter = parametersWithData.First();
            }

            Console.WriteLine("Setting UOM");
            SetFilterByFilterTitle("UoM", parameter.Uom);
            Console.WriteLine("Setting Product");
            SetFilterByFilterTitle("Product", parameter.Product);
            Console.WriteLine("Setting Month");
            SetFilterByFilterTitle("Month", parameter.Month);
            Console.WriteLine("Setting Region");
            SetFilterByFilterTitle("Region", parameter.Region);

            ApplyFitlers();
        }

        public void SetFilterByFilterTitle(string filterName, string fitlerSelections)
        {
            WaitForGridToFinishLoading();
            List<SelectedFilters> setFilters = new();
            FilterPageObject filterPageObject = new(_driver);

            filterPageObject.SelectFilterOptionsByFilterTitle(filterName, fitlerSelections);
        }

        public void ApplyFitlers()
        {
            WaitForGridToFinishLoading();
            TimingHelper.WaitForResult(() => FilterPageObject.WaitUntilElementIsContinuouslyEnabled(ApplyFiltersButton));
            ApplyFiltersButton.Click();
        }

        public class PriceMap : NavMenu
        {
            public PriceMap(IWebDriver driver) : base(driver)
            {
                _driver = driver;
            }
            IWebDriver _driver;

            public static string TotalKpiId = "*lK7D5A443E4B7FA7EA9A57C5B16C4DDDB9*kK037301D0494E58900EB78591FA4437BE*";
            public static string RegionalKpiId = "*lK7D5A443E4B7FA7EA9A57C5B16C4DDDB9*kK001DE228438E8A2BA392418D9D276849*";
            public static string MapDotId = "string";

            Tuple<string, string>[] KpiStrings = new Tuple<string, string>[]
            {
            new Tuple<string, string>("TotalKpiId", $"{TotalKpiId}"),
            new Tuple<string, string>("RegionalKpiId", $"{RegionalKpiId}")
            };

            public List<KPIValues> GetKPIValues
            {
                get
                {
                    WaitForGridToFinishLoading();
                    var kpis = new List<KPIValues>();
                    var kpiIdStrings = KpiStrings;

                    foreach (var kpi in kpiIdStrings)
                    {
                        var kpiDetails = new KPIValues();

                        kpiDetails.KpiName = kpi.Item1;

                        if (kpi.Item1 == "TotalKpiId")
                        {
                            kpiDetails.TerritoryName = "Total";
                            kpiDetails.CurrentYearPrice = _driver.Find(By.XPath($"//div[contains(@id,'{kpi.Item2}')]//div[contains(@class,'primary')]")).Text.GetNumbersFromString();
                            kpiDetails.PastYearToDatePrice = _driver.Find(By.XPath($"//div[contains(@id,'{kpi.Item2}')]//div[contains(text(),'PYTD')]")).Text.GetNumbersFromString();
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
                                var regionPages = _driver.Finds(By.XPath($"//article[contains(@id,'{kpi.Item2}')]"));

                                for (int i = 0; i < buttons.Count(); i++)
                                {
                                    kpiDetails = new KPIValues();
                                    kpiDetails.KpiName = kpi.Item1;
                                    _driver.Find(By.XPath($"//div[@class='controls controls--dot-indicators']//button[{i + 1}]")).Click();
                                    WaitForGridToFinishLoading();

                                    foreach (var page in regionPages)
                                    {
                                        if (IsElementVisibleAndContainsText(page, ".//header"))
                                        {
                                            kpiDetails.TerritoryName = page.FindElement(By.XPath($".//header")).Text;
                                            kpiDetails.CurrentYearPrice = page.FindElement(By.XPath($".//div//div[contains(@class,'primary-value')]")).Text.GetNumbersFromString();
                                            kpiDetails.PastYearToDatePrice = page.FindElement(By.XPath($".//div//div[contains(@class,'secondary-value')]")).Text.GetNumbersFromString();
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
                                kpiDetails.TerritoryName = _driver.Find(By.XPath($"//div[contains(@id,'{kpi.Item2}')]//div[contains(@class,'primary')]//ancestor::article//header")).Text;
                                kpiDetails.CurrentYearPrice = _driver.Find(By.XPath($"//div[contains(@id,'{kpi.Item2}')]//div[contains(@class,'primary')]")).Text.GetNumbersFromString();
                                kpiDetails.PastYearToDatePrice = _driver.Find(By.XPath($"//div[contains(@id,'{kpi.Item2}')]//div[contains(text(),'PYTD')]")).Text.GetNumbersFromString();
                                kpiDetails.Growth = _driver.Find(By.XPath($"//div[contains(@id,'{kpi.Item2}')]//div[@class='card__comparison-indicator']")).Text.GetNumbersFromString();

                                if (kpiDetails.Growth == "")
                                    kpiDetails.Growth = "0.0";

                                kpis.Add(kpiDetails);
                            }
                        }
                    }
                    return kpis;
                }
            }

            public List<MapDataBaseValues> GetMapData()
            {
                WaitForGridToFinishLoading();

                List<MapDataBaseValues> mapValues = new();
                Actions actions = new(_driver);
                var mapElement = _driver.Find(By.XPath("//div[contains(@id,'*lK7D5A443E4B7FA7EA9A57C5B16C4DDDB9*kKF74EE8F345A46FACDD91CFB646C30F5F*')]//canvas"));
                mapElement.Click();
                actions.MoveToElement(mapElement).Perform();

                _driver.Find(By.XPath("//div[contains(@id,'*lK7D5A443E4B7FA7EA9A57C5B16C4DDDB9*kKECAFBDF44B4BBB0C106603B76C9DB7F3*')]//div[@aria-roledescription='Map']//div[contains(@class,'hover-menu-btn')]")).Click();
                _driver.Find(By.XPath("//div[text()='Show Data']")).Click();

                var rows = _driver.Finds(By.XPath("//div[contains(@id,'*lK7D5A443E4B7FA7EA9A57C5B16C4DDDB9*')][contains(@class,'viewData')]//div[@ref='eBody']//div[@role='row']"));

                foreach (var row in rows)
                {
                    MapDataBaseValues mapData = new();

                    mapData.Product = row.FindElement(By.XPath(".//div[1]//div[1]//span[@aria-label]")).Text;
                    mapData.Region = row.FindElement(By.XPath(".//div[2]//span[@aria-label]")).Text;
                    mapData.Avg_Price_CY = row.FindElement(By.XPath(".//div[5]//span[@aria-label]")).Text.GetNumbersFromString();
                    mapData.Manufacturer = row.FindElement(By.XPath(".//div[7]//span[@aria-label]")).Text;

                    mapValues.Add(mapData);
                }

                _driver.Find(By.XPath("//table//div[text()='Close']")).Click();

                return mapValues;
            }
        }

        public class YouVsOthersMap : NavMenu
        {
            public YouVsOthersMap(IWebDriver driver) : base(driver)
            {
                _driver = driver;
            }
            IWebDriver _driver;

            public List<MapDataBaseValues> GetMapData()
            {
                WaitForGridToFinishLoading();

                List<MapDataBaseValues> mapValues = new();
                Actions actions = new(_driver);
                var mapElement = _driver.Find(By.XPath("//div[contains(@id,'*lK7D5A443E4B7FA7EA9A57C5B16C4DDDB9*kK9C38991248B26FBECFAAC4A4ED7A55D1*')]//canvas"));
                mapElement.Click();
                actions.MoveToElement(mapElement).Perform();

                _driver.Find(By.XPath("//div[contains(@id,'*lK7D5A443E4B7FA7EA9A57C5B16C4DDDB9*kW12647A3012EE424E8F5082CB77F5EC07*')]//div[@aria-roledescription='Map']//div[contains(@class,'hover-menu-btn')]")).Click();
                _driver.Find(By.XPath("//div[text()='Show Data']")).Click();

                var rows = _driver.Finds(By.XPath("//div[contains(@id,'*lK7D5A443E4B7FA7EA9A57C5B16C4DDDB9*')][contains(@class,'viewData')]//div[@ref='eBody']//div[@role='row']"));

                foreach (var row in rows)
                {
                    MapDataBaseValues mapData = new();

                    mapData.Product = row.FindElement(By.XPath(".//div[1]//div[1]//span[@aria-label]")).Text;
                    mapData.Region = row.FindElement(By.XPath(".//div[2]//span[@aria-label]")).Text;
                    mapData.Price_Difference = row.FindElement(By.XPath(".//div[5]//span[@aria-label]")).Text.GetNumbersFromString();
                    mapData.Manufacturer = row.FindElement(By.XPath(".//div[8]//span[@aria-label]")).Text;

                    mapValues.Add(mapData);
                }

                _driver.Find(By.XPath("//table//div[text()='Close']")).Click();

                return mapValues;
            }
        }

        public class PriceTrends : NavMenu
        {
            public PriceTrends(IWebDriver driver) : base(driver)
            {
                _driver = driver;
            }
            IWebDriver _driver;

            public static string ProductTableId = "*lK7D5A443E4B7FA7EA9A57C5B16C4DDDB9*kKAE4834FF467E014E070AC492CBB48D45*";
            public static string ProductGridId = "*lK7D5A443E4B7FA7EA9A57C5B16C4DDDB9*kK37CCB4E74B8C0ED6EBDD52BCC062AADF*";

            public List<PriceTrendsGraph> GetGridData()
            {
                List<PriceTrendsGraph> data = new();
                Actions actions = new(_driver);
                WaitForGridToFinishLoading();
                var dataPoints = _driver.Finds(By.XPath($"//div[contains(@id,'{ProductGridId}')]//*[name()='svg']//*[name()='circle' and @size]"));

                foreach (var point in dataPoints)
                {
                    PriceTrendsGraph graphValues = new();

                    ClickAway();
                    actions.MoveToElement(point).Perform();
                    graphValues.Season = _driver.Find(By.XPath("//div[@class='vis-tooltip-container']//tbody//tr[1]//td[2]")).Text;
                    graphValues.Month = _driver.Find(By.XPath("//div[@class='vis-tooltip-container']//tbody//tr[2]//td[2]")).Text;
                    graphValues.AveragePrice = _driver.Find(By.XPath("//div[@class='vis-tooltip-container']//tbody//tr[3]//td[2]")).Text;

                    data.Add(graphValues);
                }

                return data;
            }

            public List<ProductPriceTrendsTable> GetPriceTrendsTableData
            {
                get
                {
                    WaitForGridToFinishLoading();
                    var tableRowData = new List<ProductPriceTrendsTable>();

                    var rows = _driver.FindElements(By.XPath($"//div[contains(@id,'{ProductTableId}')]//table[contains(@aria-label,'Visualization')]//tr[@style]"));
                    int i = 0;
                    while (i < rows.Count / 2)
                    {
                        var sharedRow = rows[i];
                        // Get rowspan if present, default to 1
                        int rowspan = 1;
                        try
                        {
                            var rowspanValue = sharedRow.FindElement(By.XPath("./td[1]")).GetAttribute("rowspan");
                            if (!string.IsNullOrEmpty(rowspanValue))
                            {
                                rowspan = int.Parse(rowspanValue);
                            }
                        }
                        catch
                        {
                            // No rowspan attribute means it's a normal row
                            rowspan = 1;
                        }
                        // Shared columns
                        string product = sharedRow.FindElement(By.XPath("./td[1]")).Text;
                        string uom = sharedRow.FindElement(By.XPath("./td[2]")).Text;
                        string manufacturer = sharedRow.FindElement(By.XPath("./td[3]")).Text;
                        string transactionRange = sharedRow.FindElement(By.XPath("./td[4]")).Text;
                        // First set of data from sharedRow (td[5]–td[8])
                        tableRowData.Add(new ProductPriceTrendsTable
                        {
                            Product = product,
                            UoM = uom,
                            Manufacturer = manufacturer,
                            ProductTransactionRange = transactionRange,
                            TransactionVolume = sharedRow.FindElement(By.XPath("./td[5]")).Text,
                            ChangeInPriceYoY = sharedRow.FindElement(By.XPath("./td[6]")).Text,
                            AvgPricePerUoM = sharedRow.FindElement(By.XPath("./td[7]")).Text,
                            PriceVariance = sharedRow.FindElement(By.XPath("./td[8]")).Text
                        });
                        // Additional split rows (if any)
                        for (int j = 1; j < rowspan; j++)
                        {
                            if ((i + j) >= rows.Count)
                                break; // safety check
                            var splitRow = rows[i + j];
                            tableRowData.Add(new ProductPriceTrendsTable
                            {
                                Product = product,
                                UoM = uom,
                                Manufacturer = manufacturer,
                                ProductTransactionRange = transactionRange,
                                TransactionVolume = splitRow.FindElement(By.XPath("./td[1]")).Text,
                                ChangeInPriceYoY = splitRow.FindElement(By.XPath("./td[2]")).Text,
                                AvgPricePerUoM = splitRow.FindElement(By.XPath("./td[3]")).Text,
                                PriceVariance = splitRow.FindElement(By.XPath("./td[4]")).Text
                            });
                        }
                        i += rowspan; // skip to the next group
                    }
                    return tableRowData;
                }
            }

            public List<PriceTrendsGraph> GetTrendsShowData()
            {
                WaitForGridToFinishLoading();
                ClickAway();

                List<PriceTrendsGraph> trendsValues = new();
                Actions actions = new(_driver);
                var element = _driver.Find(By.XPath("//div[contains(@id,'*lK7D5A443E4B7FA7EA9A57C5B16C4DDDB9*kK37CCB4E74B8C0ED6EBDD52BCC062AADF*')]"));
                element.Click();
                actions.MoveToElement(element).Perform();

                _driver.Find(By.XPath("//div[contains(@id,'*lK7D5A443E4B7FA7EA9A57C5B16C4DDDB9*kKB41D0F6B4B222550D4CF03807EEF3FD5*')]//div[@aria-roledescription='Line Chart']//div[contains(@class,'hover-menu-btn')]")).Click();
                _driver.Find(By.XPath("//div[text()='Show Data']")).Click();

                int rowCount = Int32.Parse(_driver.Find(By.XPath("//div[@aria-label=\"Show Data\"]//div[contains(@class,'rowcount')]")).Text.GetNumbersFromString());

                for (int i = 0; i < rowCount; i++)
                {
                    PriceTrendsGraph data = new();

                    MoveToElement(_driver.Find(By.XPath($"//div[contains(@id,'*lK7D5A443E4B7FA7EA9A57C5B16C4DDDB9*')][contains(@class,'viewData')]//div[@ref='eBody']//div[@role='row'][@row-index='{i}']")));

                    data.Season = _driver.Find(By.XPath($"//div[contains(@id,'*lK7D5A443E4B7FA7EA9A57C5B16C4DDDB9*')][contains(@class,'viewData')]//div[@ref='eBody']//div[@role='row'][@row-index='{i}']//div[1]//div[1]//span[@aria-label]")).Text;
                    data.Month = _driver.Find(By.XPath($"//div[contains(@id,'*lK7D5A443E4B7FA7EA9A57C5B16C4DDDB9*')][contains(@class,'viewData')]//div[@ref='eBody']//div[@role='row'][@row-index='{i}']//div[2]//span[@aria-label]")).Text;
                    data.AveragePrice = _driver.Find(By.XPath($"//div[contains(@id,'*lK7D5A443E4B7FA7EA9A57C5B16C4DDDB9*')][contains(@class,'viewData')]//div[@ref='eBody']//div[@role='row'][@row-index='{i}']//div[3]//span[@aria-label]")).Text.GetNumbersFromString();

                    trendsValues.Add(data);
                }

                _driver.Find(By.XPath("//table//div[text()='Close']")).Click();

                return trendsValues;
            }
        }
    }
}



