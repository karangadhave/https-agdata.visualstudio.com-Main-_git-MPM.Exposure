using AGData.Test.Framework.Helpers;
using AGData.Test.Framework.SeleniumExtensions;
using AGData.Test.MarketIntelligenceUS;
using OpenQA.Selenium;
using AGData.Test.MarketIntelligenceUS.Helpers;
using static AGData.Test.MarketIntelligenceUS.DTOs.PricingDTO;
using MarketInteillignceUS.PageModel.UI.Test.PageObjects.Common;
using static AGData.Test.MarketIntelligenceUS.DTOs.Constants;
using OpenQA.Selenium.Interactions;

namespace MarketInteillignceUS.PageModel.RetailerPricing
{
    class RetailerPricePerAcreReportPage : NavMenu
    {
        public RetailerPricePerAcreReportPage(IWebDriver driver) : base(driver)
        {
            _driver = driver;
            TimingHelper.WaitForResult(() => driver.Url.Contains("11200AD9405B4ACA94930088561AD206/D17928354D96C200B1FFC3A817BC6C8E/W27EBEF2CE62746F9B42BDEDCA1FABF3A--KDB10077D49931F7B500B659E87C7C2D6"), 10000, 1000, "Took too long to load the Contract Info Page");
        }
        IWebDriver _driver;

        public string ReportURLString = "11200AD9405B4ACA94930088561AD206/D17928354D96C200B1FFC3A817BC6C8E/W27EBEF2CE62746F9B42BDEDCA1FABF3A--KDB10077D49931F7B500B659E87C7C2D6";
        private IList<IWebElement> TabSection => _driver.Finds(By.XPath("//div[contains(@id,'*lK7D5A443E4B7FA7EA9A57C5B16C4DDDB9*kIGK448173C04825743E278EDBBF8023D926*')]//div[@aria-live='assertive']//div//div"));
        public IWebElement ApplyFiltersButton => _driver.Find(By.XPath("//span[text()='Apply']//parent::span"));
        public decimal UseRate => decimal.Parse(_driver.Find(By.XPath("//span[contains(@aria-label,'Use Rate')]")).Text);
        public PriceMap PriceMapTab { get { return new PriceMap(_driver); } }
        public YouVsOthersMap YouVsOthersMapTab { get { return new YouVsOthersMap(_driver); } }
        public PriceTrends PriceTrendsTab { get { return new PriceTrends(_driver); } }

        public void SelectTabByName(string tabName)
        {
            WaitForGridToFinishLoading();
            ClickAway();
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

        public CommonFilters GetSetFilters()
        {
            WaitForGridToFinishLoading();
            FilterPageObject filterPage = new(_driver);
            return filterPage.GetSetFilters();
        }

        public void GetAndSetParametersWithData(bool randomSet = false, CommonFilters filters = null, string yearsFromCurrent = "0, 1", bool currentMarketYear = false)
        {
            var parametersWithData = UsMiDBHelper.GetFilterSelectionsWithData("Pricing", filters, yearsFromCurrent: yearsFromCurrent, currentMarketYear);
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

            SetFilterByFilterTitle("UoM", parameter.Uom);
            SetFilterByFilterTitle("Product", parameter.Product);
            SetFilterByFilterTitle("Month", parameter.Month);
            SetFilterByFilterTitle("Region", parameter.Region);

            ApplyFilters();
        }

        public void ApplyFilters()
        {
            WaitForGridToFinishLoading();
            TimingHelper.WaitForResult(() => FilterPageObject.WaitUntilElementIsContinuouslyEnabled(ApplyFiltersButton));
            ApplyFiltersButton.Click();
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
            ApplyFiltersButton.Click();
            WaitForGridToFinishLoading();
        }

        public string GetProductUseRateValue()
        {
            WaitForGridToFinishLoading();
            return _driver.Find(By.XPath("//div[contains(@id, '*lK7D5A443E4B7FA7EA9A57C5B16C4DDDB9*kK7B17056842568D7BB1F0A1AD2C47F8C0*')]//span[contains(@aria-label,'Use Rate')]")).Text;
        }

        public void UpdateProductUseRate(string useRate = "0")
        {
            if (useRate == "0")
            {
                useRate = Math.Round(new Random().NextDouble() * (2 - .0001) + .0001, 4).ToString(); //get a random number between 2 and .0001        
            }

            WaitForGridToFinishLoading();
            Actions actions = new Actions(_driver);

            ClickAway();
            _driver.Find(By.XPath($"//div[contains(@id, '*lK7D5A443E4B7FA7EA9A57C5B16C4DDDB9*kK7B17056842568D7BB1F0A1AD2C47F8C0*')]//span[contains(@aria-label,'Use Rate')]//parent::div")).Click();
            actions.DoubleClick(_driver.Find(By.XPath($"//div[contains(@id, '*lK7D5A443E4B7FA7EA9A57C5B16C4DDDB9*kK7B17056842568D7BB1F0A1AD2C47F8C0*')]//span[contains(@aria-label,'Use Rate')]//parent::div"))).Perform();

            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].value = '';", _driver.Find(By.XPath("//input[@class='ant-input focus-visible']")));
            _driver.Find(By.XPath("//input[@class='ant-input focus-visible']")).SendKeysSlowly(useRate);
            _driver.Find(By.XPath("//div[@class='confirm-transaction-edit']")).Click();

            WaitForGridToFinishLoading();

        }

        public class PriceMap : NavMenu
        {
            public PriceMap(IWebDriver driver) : base(driver)
            {
                _driver = driver;
            }
            IWebDriver _driver;

            public static string TotalKpiId = "*lK7D5A443E4B7FA7EA9A57C5B16C4DDDB9*kK40C72A624B2B81404FBA12BECC35756E*";
            public static string RegionalKpiId = "*lK7D5A443E4B7FA7EA9A57C5B16C4DDDB9*kK4C1E28184F3A32FD02E74CA09557E614*";
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
                                        //if (IsElementVisible(_driver, $"//div[contains(@id,'{kpi.Item2}')]//div[contains(@class,'primary')]//ancestor::article//header"))
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
                        //if (kpiDetails.Growth == "")
                        //    kpiDetails.Growth = "0.0";

                        //kpis.Add(kpiDetails);
                    }
                    return kpis;
                }
            }

            public List<MapDataBaseValues> GetMapData()
            {
                WaitForGridToFinishLoading();

                List<MapDataBaseValues> mapValues = new();
                Actions actions = new(_driver);
                var mapElement = _driver.Find(By.XPath("//div[contains(@id,'*lK7D5A443E4B7FA7EA9A57C5B16C4DDDB9*kK9C2B8A7F48DCBD97B1DD3BA5B532BDA6*')]//canvas"));
                mapElement.Click();
                actions.MoveToElement(mapElement).Perform();

                _driver.Find(By.XPath("//div[contains(@id,'*lK7D5A443E4B7FA7EA9A57C5B16C4DDDB9*kK19C812CF43B17999922161920E2A3BEE*')]//div[@aria-roledescription='Map']//div[contains(@class,'hover-menu-btn')]")).Click();
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
                var mapElement = _driver.Find(By.XPath("//div[contains(@id,'*lK7D5A443E4B7FA7EA9A57C5B16C4DDDB9*kK2A5942EE44F6C062148BEF9A13D17000*')]//canvas"));
                mapElement.Click();
                actions.MoveToElement(mapElement).Perform();

                _driver.Find(By.XPath("//div[contains(@id,'*lK7D5A443E4B7FA7EA9A57C5B16C4DDDB9*kW8670084956CB466DB332D3317FFDEBC4*')]//div[@aria-roledescription='Map']//div[contains(@class,'hover-menu-btn')]")).Click();
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

            public static string ProductGridId = "*lK7D5A443E4B7FA7EA9A57C5B16C4DDDB9*kKE9B1D9964AEED95D5BD926B695939057*";
            public static string ProductTableId = "*lK7D5A443E4B7FA7EA9A57C5B16C4DDDB9*kKAF5184D14B06550D1516AE8B3EE42C57*";

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
                    graphValues.AveragePrice = _driver.Find(By.XPath("//div[@class='vis-tooltip-container']//tbody//tr[3]//td[2]")).Text.GetNumbersFromString();

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
                    var rows = _driver.FindElements(By.XPath($"//div[contains(@id,'{ProductTableId}')]//table[@role='grid']//tr[2]"));
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
                            AvgPricePerAcre = sharedRow.FindElement(By.XPath("./td[7]")).Text,
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
                                TransactionVolume = splitRow.FindElement(By.XPath("./td[5]")).Text,
                                ChangeInPriceYoY = splitRow.FindElement(By.XPath("./td[6]")).Text,
                                AvgPricePerAcre = splitRow.FindElement(By.XPath("./td[7]")).Text,
                                PriceVariance = splitRow.FindElement(By.XPath("./td[8]")).Text
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
                var element = _driver.Find(By.XPath("//div[contains(@id,'*lK7D5A443E4B7FA7EA9A57C5B16C4DDDB9*kKE9B1D9964AEED95D5BD926B695939057*')]"));
                element.Click();
                actions.MoveToElement(element).Perform();

                _driver.Find(By.XPath("//div[contains(@id,'*lK7D5A443E4B7FA7EA9A57C5B16C4DDDB9*kK206A755646C2303D578D509D2CF7FC86*')]//div[@aria-roledescription='Line Chart']//div[contains(@class,'hover-menu-btn')]")).Click();
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
