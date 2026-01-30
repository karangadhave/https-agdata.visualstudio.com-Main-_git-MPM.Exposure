using OpenQA.Selenium;
using AGData.Test.Framework.SeleniumExtensions;
using AGData.Test.Framework.Helpers;

namespace AGData.Test.MarketIntelligenceUS
{
    public class LoginPage
    {
        public LoginPage(IWebDriver driver)
        {
            _driver = driver;
        }
        IWebDriver _driver;

        //public IWebElement Email => _driver.FindElement(By.Id("username"));

        //public IWebElement Password => _driver.FindElement(By.Id("password"));

        //public IWebElement LoginButton => _driver.Find(By.Id("loginButton"));              

        public void LoginUser(string userName, string password, bool signIn = true)
        {           
            if (_driver.IsElementPresent(By.XPath("//span[@id='placeholderPassword']")))
            {
                TimingHelper.WaitForCondition(() => _driver.FindElement(By.Id("username")).Displayed, 10000, 500);

                _driver.FindElement(By.Id("username")).SendKeys(userName);
                _driver.FindElement(By.Id("password")).SendKeys(password);


                if (signIn)
                {
                    _driver.Find(By.Id("loginButton")).Click();
                    Thread.Sleep(1000); //wait for the page to open
                }
                else
                {
                    //Do nothing
                }
            }
            else if (_driver.IsElementPresent(By.Id("email"))) //Added for OIDC login
            {
                TimingHelper.WaitForCondition(() => _driver.Find(By.XPath("//input[@id='email']")).Displayed, 10000, 500);

                _driver.Find(By.XPath("//input[@id='email']")).SendKeys(userName);

                if(signIn)
                {
                    _driver.Find(By.XPath("//button[@type='submit']")).Click();         
                }
                else
                {
                    //Do nothing
                }
            }
            else
            {
                TimingHelper.WaitForCondition(() => _driver.Find(By.XPath("//div[@id='oidcText']")).Displayed, 10000, 500);

                if (signIn)
                {
                    _driver.Find(By.XPath("//div[@id='oidcText']")).Click();
                }
                else
                {
                    //Do nothing
                }
            }
        }
    }
}
