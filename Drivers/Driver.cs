using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Support.UI;

namespace WebReset.Drivers {
    public static class Drive {
        public static IWebDriver CreateDriver(string ip, bool showWindow) {

            Console.WriteLine("Creating Edge driver...");
            
            var options = new EdgeOptions();
            
            // Permission to open window
            if (!showWindow) {
                Console.WriteLine("Running in headless mode");
                options.AddArgument("--headless=new");            
            } else {
                Console.WriteLine("Running showing the window");
            }

            options.AddArgument("--disable-gpu");
            options.AddArgument("--window-size=1920,1080");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");

            // Create Edge driver
            IWebDriver driver = new EdgeDriver(options);

            driver.Navigate().GoToUrl($"http://{ip}");
            Console.WriteLine("Waiting the page...");

           // Wait to return the driver
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            wait.Until(d =>
                ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState")?.ToString() == "complete");
                Console.WriteLine("Page loaded successfully!");
            return driver;
        }
    }
}