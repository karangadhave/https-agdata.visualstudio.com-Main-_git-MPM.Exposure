using AGData.Test.Framework.Helpers;
using AGData.Test.Framework.SeleniumExtensions;
using AGData.Test.MarketIntelligenceUS;
using OpenQA.Selenium;
using AGData.Test.MarketIntelligenceUS.Helpers;
using static AGData.Test.MarketIntelligenceUS.DTOs.PricingDTO;
using MarketInteillignceUS.PageModel.UI.Test.PageObjects.Common;
using static AGData.Test.MarketIntelligenceUS.DTOs.Constants;

namespace MarketInteillignceUS.PageModel.RetailerPricing
{
    class RetailerProductPricePerAcreExportReportPage : NavMenu
    {
        public RetailerProductPricePerAcreExportReportPage(IWebDriver driver) : base(driver)
        {
            _driver = driver;
            TimingHelper.WaitForResult(() => driver.Url.Contains("11200AD9405B4ACA94930088561AD206/6D818378458EBCEBCD01069F41D16246/K2547DC3F49C42D93AA08339712F3FDC3--KA2853C264EDCAADC699888AA34562A70"), 10000, 1000, "Took too long to load the Contract Info Page");
        }
        IWebDriver _driver;

        public string ReportURLString = "11200AD9405B4ACA94930088561AD206/D17928354D96C200B1FFC3A817BC6C8E/W3CECADCEB3DF4EA287066B014853D16A--KE75ED16E45FDFA4540DF1C8C29B36FCF";
        public string AttributeSelectorId = "*lWA1311E5E060F4AA5B7D345D34911135C*kIGK3FA4C374403E4075368B0AAE2E9FB250*";
        public IWebElement ApplyFiltersButton => _driver.Find(By.XPath("//span[text()='Apply']//parent::span"));
        public ExportTable ExportTableSection { get { return new ExportTable(_driver); } }

        public void SelectAttributeSelectorByName(string attributeName)
        {
            WaitForGridToFinishLoading();
            _driver.Find(By.XPath($"//div[contains(@id,'{AttributeSelectorId}')]//div[@class='items-container ']//div[text()='{attributeName}']")).Click();
        }

        public List<string> GetSelectedAttributes()
        {
            List<string> attributes = new();
            var selections = _driver.Finds(By.XPath($"//div[contains(@id,'{AttributeSelectorId}')]//div[@class='items-container ']//div[@aria-selected='true']"));

            foreach (var selection in selections)
            {
                attributes.Add(selection.FindElement(By.XPath(".//div")).Text);
            }

            return attributes;
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

        public void GetAndSetParametersWithData(int numberOfProducts = 1, bool randomSet = false)
        {
            var parametersWithData = UsMiDBHelper.GetFilterSelectionsWithData("Pricing");

            if (!parametersWithData.Any())
                return; // Nothing to select from

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
            SetFilterByFilterTitle("Month", string.Join(", ", parameter.SelectMany(p => p.Month.Split(',')).Select(m => m.Trim()).Distinct()));
            SetFilterByFilterTitle("Region", string.Join(", ", parameter.SelectMany(p => p.Region.Split(',')).Select(m => m.Trim()).Distinct()));

            WaitForGridToFinishLoading();
            Thread.Sleep(2000); //Sleep to allow targeting to finish
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

        public class ExportTable : NavMenu
        {
            public ExportTable(IWebDriver driver) : base(driver)
            {
                _driver = driver;
            }
            IWebDriver _driver;

            public static string ExportTableId = "*lWA1311E5E060F4AA5B7D345D34911135C*kK2DCCFD7D40E491048C1A9D86AE629978*";

            public List<PricePerAcreExportTable> GetPricePerAcreExportTableData()
            {
                WaitForGridToFinishLoading();
                List<PricePerAcreExportTable> table = new();
                List<string> headers = new();
                var headerRows = _driver.Finds(By.XPath($"//div[contains(@id,'{ExportTableId}')]/div[contains(@style,'relative')]//table[@aria-label]//tbody//tr[1]//td"));

                Dictionary<string, int> headerIndexMap = new();

                for (int i = 0; i < headerRows.Count; i++)
                {
                    var headerText = headerRows[i].Text.Trim();
                    headerIndexMap[headerText] = i + 1;
                }

                var rows = _driver.Finds(By.XPath($"//div[contains(@id,'{ExportTableId}')]/div[contains(@style,'relative')]//table[@aria-label]//tbody//tr[position() > 1]"));

                foreach (var row in rows)
                {
                    PricePerAcreExportTable data = new();

                    if (headerIndexMap.ContainsKey("Product"))
                        data.Product = row.FindElement(By.XPath($".//td[{headerIndexMap["Product"]}]")).Text;
                    if (headerIndexMap.ContainsKey("Supplier"))
                        data.Manufacturer = row.FindElement(By.XPath($".//td[{headerIndexMap["Supplier"]}]")).Text;
                    if (headerIndexMap.ContainsKey("Category"))
                        data.Category = row.FindElement(By.XPath($".//td[{headerIndexMap["Category"]}]")).Text;
                    if (headerIndexMap.ContainsKey("Sub-Category"))
                        data.SubCategory = row.FindElement(By.XPath($".//td[{headerIndexMap["Sub-Category"]}]")).Text;
                    if (headerIndexMap.ContainsKey("Region"))
                        data.Region = row.FindElement(By.XPath($".//td[{headerIndexMap["Region"]}]")).Text;
                    if (headerIndexMap.ContainsKey("Date"))
                        data.Date = row.FindElement(By.XPath($".//td[{headerIndexMap["Date"]}]")).Text;
                    if (headerIndexMap.ContainsKey("Season"))
                        data.Season = row.FindElement(By.XPath($".//td[{headerIndexMap["Season"]}]")).Text;
                    if (headerIndexMap.ContainsKey("UoM"))
                        data.Uom = row.FindElement(By.XPath($".//td[{headerIndexMap["UoM"]}]")).Text;
                    if (headerIndexMap.ContainsKey("Use Rate (Units/Treated Acre)"))
                        data.UseRate = row.FindElement(By.XPath($".//td[{headerIndexMap["Use Rate (Units/Treated Acre)"]}]")).Text.NormalizeNegativeString();
                    if (headerIndexMap.ContainsKey("Price per Acre"))
                        data.PricePerAcre = row.FindElement(By.XPath($".//td[{headerIndexMap["Price per Acre"]}]")).Text.GetNumbersFromString();
                    if (headerIndexMap.ContainsKey("Price Variance"))
                        data.PriceVariance = row.FindElement(By.XPath($".//td[{headerIndexMap["Price Variance"]}]")).Text.GetNumbersFromString();

                    table.Add(data);
                }

                return table;
            }
        }
    }
}
