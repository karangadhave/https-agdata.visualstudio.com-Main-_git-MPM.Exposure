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
    class RetailerProductPricePerAcreReportPage : NavMenu
    {
        public RetailerProductPricePerAcreReportPage(IWebDriver driver) : base(driver)
        {
            _driver = driver;
            TimingHelper.WaitForResult(() => driver.Url.Contains("11200AD9405B4ACA94930088561AD206/6D818378458EBCEBCD01069F41D16246/W1F6F99ED4B13460A8DBD3662C8F2F9B7--K54F3D549473B68E34090FBB89E99AB81"), 10000, 1000, "Took too long to load the Contract Info Page");
        }
        IWebDriver _driver;

        public string ReportURLString = "11200AD9405B4ACA94930088561AD206/6D818378458EBCEBCD01069F41D16246/W1F6F99ED4B13460A8DBD3662C8F2F9B7--K54F3D549473B68E34090FBB89E99AB81";
        private IList<IWebElement> TabSection => _driver.Finds(By.XPath("//div[contains(@id,'*lW40FAA856D6B04F439381D15BBA15DACE*kIGK3AC3643A47AA3DA99B4D959BF0246C37*')]//div[contains(@class,'item-text')]"));
        public IWebElement ApplyFiltersButton => _driver.Find(By.XPath("//span[text()='Apply']//parent::span"));
        public UseRateInput UseRateInputTab { get { return new UseRateInput(_driver); } }
        public BarChart BarChartTab { get { return new BarChart(_driver); } }


        public void SelectTabByName(string tabName)
        {
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

        public void GetAndSetParametersWithData(int numberOfProducts = 1, bool randomSet = false, CommonFilters filters = null, string yearsFromCurrent = "0, 1")
        {
            var parametersWithData = UsMiDBHelper.GetFilterSelectionsWithData("Pricing", filters, yearsFromCurrent);

            if (parametersWithData.Count == 0)
                Assert.Inconclusive("No filters have data");

            List<FilterOptionsWithData> parameter = new();

            if (randomSet)
            {
                Random rand = new();
                parameter = parametersWithData
                    .OrderBy(_ => rand.Next())
                    .Take(numberOfProducts)
                    .ToList();
            }
            else
            {
                parameter = parametersWithData.Take(numberOfProducts).ToList();
            }

            SetFilterByFilterTitle("Product", string.Join(", ", parameter.Select(p => p.Product).Distinct()));
            SetFilterByFilterTitle("UoM", string.Join(", ", parameter.Select(p => p.Uom).Distinct()));           
            SetFilterByFilterTitle("Month", string.Join(", ", parameter.SelectMany(p => p.Month.Split(',')).Select(m => m.Trim()).Distinct()));
            SetFilterByFilterTitle("Region", string.Join(", ", parameter.SelectMany(p => p.Region.Split(',')).Select(m => m.Trim()).Distinct()));           

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

        public class UseRateInput : NavMenu
        {
            public UseRateInput(IWebDriver driver) : base(driver)
            {
                _driver = driver;
            }
            IWebDriver _driver;

            public static string PricePerAcreTableId = "*lW40FAA856D6B04F439381D15BBA15DACE*kK4475C8654407ADD7E10A79A6A6AFCCA4*";

            public List<ProductPriceAcreTable> GetBrandPricePerAcreTableData()
            {
                WaitForGridToFinishLoading();
                List<ProductPriceAcreTable> table = new();
                var rows = _driver.Finds(By.XPath($"//div[contains(@id,'{PricePerAcreTableId}')]//div[@row-index]"));

                foreach (var row in rows)
                {
                    ProductPriceAcreTable data = new();

                    //if (row.FindElement(By.XPath(".//div[1]//span")).Text == "")
                    //{
                    //    data.Uom = table.LastOrDefault().Uom;
                    //}
                    //else
                    //{
                    //    data.Uom = row.FindElement(By.XPath(".//div[1]//span")).Text;
                    //}

                    data.Product = row.FindElement(By.XPath(".//div[1]//span")).Text;
                    data.Uom = row.FindElement(By.XPath(".//div[2]//span")).Text;
                    data.Manufacturer = row.FindElement(By.XPath(".//div[3]//span")).Text;
                    data.UseRate = row.FindElement(By.XPath(".//div[4]//span")).Text.NormalizeNegativeString();
                    data.CyAvgPrice = row.FindElement(By.XPath(".//div[5]//span")).Text.GetNumbersFromString();
                    data.PriceVariance = row.FindElement(By.XPath(".//div[6]//span")).Text.GetNumbersFromString();
                    data.ChangeInPriceYoy = row.FindElement(By.XPath(".//div[7]//span")).Text.GetNumbersFromString();

                    table.Add(data);
                }

                return table;
            }

            public string SetProductUseRate(string productName, string useRate = "0")
            {
                WaitForGridToFinishLoading();
                Actions actions = new Actions(_driver);
                ClickAway();

                if (useRate == "0")
                {
                    useRate = Math.Round(new Random().NextDouble() * (2 - .0001) + .0001, 4).ToString(); //get a random number between 2 and .0001        
                }

                foreach (var product in productName.Split(","))
                {
                    WaitForGridToFinishLoading();
                    _driver.Find(By.XPath($"//div[contains(@id,'{PricePerAcreTableId}')]//following::span[text()='{product.Trim()}']//following::div[contains(@class,'editable')]")).Click();
                    actions.DoubleClick(_driver.Find(By.XPath($"//div[contains(@id,'{PricePerAcreTableId}')]//following::span[text()='{product.Trim()}']//following::div[contains(@class,'editable')]"))).Perform();

                    ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].value = '';", _driver.Find(By.XPath("//input[@class='ant-input focus-visible']")));
                    _driver.Find(By.XPath("//input[@class='ant-input focus-visible']")).SendKeysSlowly(useRate.ToString());
                    _driver.Find(By.XPath("//div[@class='confirm-transaction-edit']")).Click();
                }

                WaitForGridToFinishLoading();
                return useRate;
            }
        }

        public class BarChart : NavMenu
        {
            public BarChart(IWebDriver driver) : base(driver)
            {
                _driver = driver;
            }
            IWebDriver _driver;

            public static string ProductGridId = "*lW40FAA856D6B04F439381D15BBA15DACE*kK0A56D93743068996B4D943B726A88903*";

            public List<ProductPricePerAcreBarGraph> GetGridData()
            {
                List<ProductPricePerAcreBarGraph> data = new();
                Actions actions = new(_driver);
                WaitForGridToFinishLoading();
                var dataPoints = _driver.Finds(By.XPath($"//div[contains(@id,'{ProductGridId}')]//*[name()='svg']//*[name()='rect' and @subtype]"));

                foreach (var point in dataPoints)
                {
                    ProductPricePerAcreBarGraph graphValues = new();

                    ClickAway();
                    actions.MoveToElement(point).Perform();
                    graphValues.Season = _driver.Find(By.XPath("//div[@class='vis-tooltip-container']//tbody//tr[1]//td[2]")).Text;
                    graphValues.Product = _driver.Find(By.XPath("//div[@class='vis-tooltip-container']//tbody//tr[2]//td[2]")).Text;
                    graphValues.AveragePricePerAcre = _driver.Find(By.XPath("//div[@class='vis-tooltip-container']//tbody//tr[3]//td[2]")).Text.GetNumbersFromString();
                    graphValues.ActiveIngredients = _driver.Find(By.XPath("//div[@class='vis-tooltip-container']//tbody//tr[4]//td[2]")).Text;
                    graphValues.Manufacturer = _driver.Find(By.XPath("//div[@class='vis-tooltip-container']//tbody//tr[5]//td[2]")).Text;

                    data.Add(graphValues);
                }

                return data;
            }
        }
    }
}
