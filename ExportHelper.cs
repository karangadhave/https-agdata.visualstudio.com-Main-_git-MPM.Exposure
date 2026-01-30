using AGData.Test.Framework.Config;
using AGData.Test.Framework.Helpers;

namespace AGData.Test.MarketIntelligenceUS.Helpers
{
    public static class ExportHelper
    {  
        public static void ClearDownloadPath(string fileName)
        {
            DirectoryInfo di = new DirectoryInfo(FrameworkConfig.BrowserDownloadFilePath);

            foreach (FileInfo file in di.GetFiles())
            {
                if (file.FullName.Contains(fileName))
                {
                    try
                    {
                        FileHelper.DeleteFile(file.FullName);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        Console.WriteLine($"Unable to delete recent files from {FrameworkConfig.BrowserDownloadFilePath}.");
                    }
                }
            }
        }
    }
}