using OpenQA.Selenium;
using Org.BouncyCastle.Bcpg.OpenPgp;
using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;
using static AGData.Test.MarketIntelligenceUS.DTOs.MarketShareDTO;

namespace AGData.Test.MarketIntelligenceUS.Helpers
{
    public static class DataHelper
    {
        public static Double ConvertPercentageToDouble(string percentageField)
        {
            decimal num = 0;

            if (percentageField != "")
            {
                num = decimal.Parse(percentageField.TrimEnd(new char[] { '%', ' ' })) / 100M;
            }
            return Convert.ToDouble(num);
        }

        public static string GetNumbersFromString(this string dirtyNumbers)
        {
            if (string.IsNullOrWhiteSpace(dirtyNumbers))
                return "0.00";

            dirtyNumbers = dirtyNumbers.Trim();

            // Check if the numeric portion is wrapped in parentheses
            bool isNegative = (dirtyNumbers.Contains("(") && dirtyNumbers.Contains(")")) && !dirtyNumbers.Contains("+") || dirtyNumbers.Contains("-");

            // Remove all non-digit and non-decimal characters
            string cleaned = Regex.Replace(dirtyNumbers, @"[^0-9.]", "");

            if (string.IsNullOrWhiteSpace(cleaned))
                return "0.00";

            return isNegative ? "-" + cleaned : cleaned;
        }

        public static string RemoveSpaceFromString(string oldString)
        {
            var newString = oldString.Replace(" ", "");

            return newString;
        }

        public static string RemoveDoubleSpacesFromString(this string oldString)
        {
            var newString = oldString.Replace("  ", " ");

            return newString.Trim();
        }

        public static string RemoveCommasFromString(string oldString)
        {
            var newString = oldString.Replace(",", "");

            return newString;
        }

        public static string RemoveValueSuffixFromString(this string oldString)
        {
            var newString = RemoveCommasFromString(oldString.Replace("lb", "").Replace("kg", "")).RemoveAllStringWhiteSpace();

            return newString;
        }

        public static string GetHexCodeFromElement(IWebElement element)
        {
            var styleString = element.GetAttribute("style").ToString();
            var rgbCode = styleString.Substring(styleString.IndexOf('(') + 1, 13).Trim(')', ';').Split(',');
            var color = Color.FromArgb(0, Int32.Parse(rgbCode[0]), Int32.Parse(rgbCode[1]), Int32.Parse(rgbCode[2]));
            return color.Name;
        }

        public static string? RemoveAllStringWhiteSpace(this string oldString)
        {
            if (!string.IsNullOrEmpty(oldString))
            {
                Regex sWhitespace = new Regex(@"\s+");
                return sWhitespace.Replace(oldString, "");
            }

            return oldString;
        }

        public static double RoundDoubleToDigit(this double number, int digit = 3)
        {
            double limitDigit = Math.Round(number, digit + 1);
            return Math.Round(limitDigit, digit, MidpointRounding.AwayFromZero);
        }

        public static bool CompareDoubles(double a, double b, double deviation = .011)
        {
            if (Math.Abs(a.RoundDoubleToDigit() - b.RoundDoubleToDigit()) < deviation)
            {
                return true;
            }

            Console.WriteLine($"The values {a} and {b} were not within the set deviation of {deviation}");

            return false;
        }

        public static string NullIfWhiteSpace(this string value)
        {
            if (String.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value;
        }

        public static string NormalizeNegativeString(this string input)
        {
            input = input.Trim();

            if (input.StartsWith("(") && input.EndsWith(")"))
            {
                string number = input.Substring(1, input.Length - 2);
                return "-" + number;
            }

            return input;
        }

        public static string MatchMarketShareKPIFormatting(this string rawValue, int decimalPlaces = 2)
        {
            double convertedRawValue = Convert.ToDouble(rawValue);
            string formatted;

            if (Math.Abs(convertedRawValue) >= 1_000_000)
            {
                formatted = (convertedRawValue / 1_000_000).ToString($"F{decimalPlaces}", CultureInfo.InvariantCulture);
            }
            else if (Math.Abs(convertedRawValue) >= 1_000)
            {
                formatted = (convertedRawValue / 1_000).ToString($"F{decimalPlaces}", CultureInfo.InvariantCulture);
            }
            else
            {
                formatted = convertedRawValue.ToString($"F{decimalPlaces}", CultureInfo.InvariantCulture);
            }

            return formatted;
        }

        public static List<PVMGrowthMetrics> GetPVMMetricsForGrowth(List<PvmCalculationValuesDTO> pvmValues, string marketSharePage)
        {
            List<PVMGrowthMetrics> pivotedData = new List<PVMGrowthMetrics>();

            foreach (var item in pvmValues)
            {
                decimal salesCY = 0;
                decimal salesPY = 0;
                decimal qtyPY = 0;
                decimal qtyCY = 0;
                decimal salesYoy = 0;

                decimal.TryParse(item.SALES_CY, out salesCY);
                decimal.TryParse(item.SALES_PY, out salesPY);
                decimal.TryParse(item.QTY_PY, out qtyPY);
                decimal.TryParse(item.QTY_CY, out qtyCY);
                decimal.TryParse(item.SALES_YOY, out salesYoy);

                item.VolumeDifference = qtyCY - qtyPY;
                item.Price_PY = (salesPY != 0) ? salesPY / qtyPY : 0;
                item.VolumeEffect2 = (salesPY != 0) ? item.VolumeDifference * item.Price_PY : 0;
                item.Price_CY = (salesCY != 0) ? salesCY / qtyCY : 0;
                item.PriceEffect = (item.Price_CY - item.Price_PY) * qtyPY;
                item.MixEffect = salesYoy - item.VolumeEffect2 - item.PriceEffect;
            }

            if (marketSharePage == "Manufacturer")
            {

                pivotedData = pvmValues.GroupBy(d => d.ManufacturerName)
                                                      .Select(g =>
                                                      {
                                                          decimal SumOfQty_CY = g.Sum(x => Convert.ToDecimal(x.QTY_CY));
                                                          decimal SumOfQty_PY = g.Sum(x => Convert.ToDecimal(x.QTY_PY));
                                                          decimal SumOfPriceEffect = g.Sum(x => Convert.ToDecimal(x.PriceEffect));
                                                          decimal SumOfSales_YoY = g.Sum(x => Convert.ToDecimal(x.SALES_YOY));
                                                          decimal sumSalesCY = g.Sum(x => Convert.ToDecimal(x.SALES_CY));
                                                          decimal sumSalesPY = g.Sum(x => Convert.ToDecimal(x.SALES_PY));
                                                          decimal SumOfVolumeEffect2 = g.Sum(x => Convert.ToDecimal(x.VolumeEffect2));
                                                          decimal SumOfMixEffect = g.Sum(x => Convert.ToDecimal(x.MixEffect));

                                                          decimal sumQtyCY = g.Sum(x => Convert.ToDecimal(x.QTY_CY));

                                                          return new PVMGrowthMetrics
                                                          {
                                                              ManufacturerName = g.Key,
                                                              GrowthFromQuantity = Math.Round((SumOfVolumeEffect2 / sumSalesPY) * 100, 1),
                                                              TotalGrowth = Math.Round(((sumSalesCY - sumSalesPY) / sumSalesPY) * 100, 1),
                                                              MixEffect = Math.Round((SumOfMixEffect / sumSalesPY) * 100, 1),
                                                              GrowthFromPrice = Math.Round((SumOfPriceEffect / sumSalesPY) * 100, 1)
                                                          };

                                                      }).ToList();
            }
            else if (marketSharePage == "Retailer")
            {

                pivotedData = pvmValues.GroupBy(d => d.RetailerName)
                                                      .Select(g =>
                                                      {
                                                          decimal SumOfQty_CY = g.Sum(x => Convert.ToDecimal(x.QTY_CY));
                                                          decimal SumOfQty_PY = g.Sum(x => Convert.ToDecimal(x.QTY_PY));
                                                          decimal SumOfPriceEffect = g.Sum(x => Convert.ToDecimal(x.PriceEffect));
                                                          decimal SumOfSales_YoY = g.Sum(x => Convert.ToDecimal(x.SALES_YOY));
                                                          decimal sumSalesCY = g.Sum(x => Convert.ToDecimal(x.SALES_CY));
                                                          decimal sumSalesPY = g.Sum(x => Convert.ToDecimal(x.SALES_PY));
                                                          decimal SumOfVolumeEffect2 = g.Sum(x => Convert.ToDecimal(x.VolumeEffect2));
                                                          decimal SumOfMixEffect = g.Sum(x => Convert.ToDecimal(x.MixEffect));

                                                          decimal sumQtyCY = g.Sum(x => Convert.ToDecimal(x.QTY_CY));

                                                          return new PVMGrowthMetrics
                                                          {
                                                              Retailer = g.Key,
                                                              GrowthFromQuantity = Math.Round((SumOfVolumeEffect2 / sumSalesPY) * 100, 1),
                                                              TotalGrowth = Math.Round(((sumSalesCY - sumSalesPY) / sumSalesPY) * 100, 1),
                                                              MixEffect = Math.Round((SumOfMixEffect / sumSalesPY) * 100, 1),
                                                              GrowthFromPrice = Math.Round((SumOfPriceEffect / sumSalesPY) * 100, 1)
                                                          };

                                                      }).ToList();
            }
            else
            {
                pivotedData = pvmValues.GroupBy(d => d.ProductBrand)
                                                     .Select(g =>
                                                     {
                                                         decimal SumOfQty_CY = g.Sum(x => Convert.ToDecimal(x.QTY_CY));
                                                         decimal SumOfQty_PY = g.Sum(x => Convert.ToDecimal(x.QTY_PY));
                                                         decimal SumOfPriceEffect = g.Sum(x => Convert.ToDecimal(x.PriceEffect));
                                                         decimal SumOfSales_YoY = g.Sum(x => Convert.ToDecimal(x.SALES_YOY));
                                                         decimal sumSalesCY = g.Sum(x => Convert.ToDecimal(x.SALES_CY));
                                                         decimal sumSalesPY = g.Sum(x => Convert.ToDecimal(x.SALES_PY));
                                                         decimal SumOfVolumeEffect2 = g.Sum(x => Convert.ToDecimal(x.VolumeEffect2));
                                                         decimal SumOfMixEffect = g.Sum(x => Convert.ToDecimal(x.MixEffect));

                                                         decimal sumQtyCY = g.Sum(x => Convert.ToDecimal(x.QTY_CY));

                                                         return new PVMGrowthMetrics
                                                         {
                                                             ProductBrand = g.Key,
                                                             GrowthFromQuantity = Math.Round((SumOfVolumeEffect2 / sumSalesPY) * 100, 1),
                                                             TotalGrowth = Math.Round(((sumSalesCY - sumSalesPY) / sumSalesPY) * 100, 1),
                                                             MixEffect = Math.Round((SumOfMixEffect / sumSalesPY) * 100, 1),
                                                             GrowthFromPrice = Math.Round((SumOfPriceEffect / sumSalesPY) * 100, 1)
                                                         };

                                                     }).ToList();
            }

            return pivotedData;
        }

        public static string CalculatePercentageChange(string ytdValue, string lytdValue)
        {
            decimal ytd = decimal.Parse(ytdValue.Trim());
            decimal lytd = decimal.Parse(lytdValue.Trim());
            // Avoid divide-by-zero
            if (ytd == 0)
                return "0.00% ↑";

            decimal percent = ((ytd - lytd) / ytd) * 100;
            string arrow = percent < 0 ? " ↓" : " ↑";
            string formattedPercent =
                percent > 0 ? "+" + percent.ToString("0.00") :
                percent < 0 ? percent.ToString("0.00") :
                "0.00";
            return $"{formattedPercent}%{arrow}";
        }
    }
}