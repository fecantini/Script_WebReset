
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System.Net.NetworkInformation;

namespace WebReset.Pages {
    public class Page {
        private readonly IWebDriver driver;
        private readonly WebDriverWait wait;
        public Page(IWebDriver driver) {
            this.driver = driver;
            this.wait = new WebDriverWait(driver, TimeSpan.FromSeconds(3));
        }

        // Change to Management tab   
        public bool ManagementTab() {
            try {
                Console.WriteLine("Opening Management tab...");

                var infoButton = wait.Until(d =>
                    d.FindElement(By.Id("idGeneralConfigTab1")));

                infoButton.Click();

                driver.SwitchTo().Frame("contentIFrameId");
                driver.SwitchTo().Frame("contentIFrameManagerId");

                wait.Until(d =>
                    d.FindElement(By.Id("idUsername")));

                return true;

            } catch(Exception ex) {

                Console.WriteLine($"Could not access management tab: {ex.Message}");
                return false;
            }
        }

        // Login to System Control
        public bool Login(string username, string password) {
            try {
                Console.WriteLine("Entering credentials...");

                var usernameInput = wait.Until(d =>
                    d.FindElement(By.Id("idUsername")));

                usernameInput.SendKeys(username);

                var passwordInput = wait.Until(d =>
                    d.FindElement(By.Id("idPassword")));

                passwordInput.SendKeys(password);

                var loginButton = wait.Until(d =>
                    d.FindElement(By.Id("idBtnLoading")));

                loginButton.Click();

                Console.WriteLine("Login performed");

                return true;
            } catch (Exception ex) {
                Console.WriteLine($"Login failed: {ex.Message}");
                return false;
            }
        }

        // Perform Reset
        public bool Reset(bool deleteApp, string username, string password) {
            try {
                Console.WriteLine("Process of Reset...");

                if (deleteApp) {
                    Console.WriteLine("Deleting application data...");

                    var deleteBox = wait.Until(d =>
                        d.FindElement(By.Id("idSystemControlEraseApp")));

                    deleteBox.Click();
                }

                Console.WriteLine("Waiting reset button...");

                wait.Until(d => {
                    var button = d.FindElement(By.Id("idBtnSystemControl"));

                    return button.Displayed && button.Enabled;
                });

                bool clicked = false;

                for (int attempt = 1; attempt <= 3; attempt++) {
                    try {
                        var resetButton = driver.FindElement(By.Id("idBtnSystemControl"));

                        resetButton.Click();

                        clicked = true;

                        break;

                    } catch (ElementClickInterceptedException) {
                        Console.WriteLine($"Reset button click intercepted. Attempt {attempt}/3");

                        if (attempt == 3) {
                            throw;
                        }

                        Thread.Sleep(1000);
                    } catch (StaleElementReferenceException) {
                        Console.WriteLine($"Reset button became stale. Attempt {attempt}/3");

                        if (attempt == 3) {
                            throw;
                        }

                        Thread.Sleep(500);
                    }
                }

                if (!clicked) {
                    throw new Exception("Could not click reset button.");
                }

                Console.WriteLine("Waiting first alert...");

                var alertWait = new WebDriverWait(driver, TimeSpan.FromSeconds(5));

                IAlert alert = alertWait.Until(d => {
                    try {
                        return d.SwitchTo().Alert();
                    } catch (NoAlertPresentException) {
                        return null;
                    }
                });

                Console.WriteLine($"Alert: {alert.Text}");

                alert.Accept();

                Console.WriteLine("Entering confirmation credentials...");

                var usernameConfirm = wait.Until(d =>
                    d.FindElement(By.Id("idUsernameConfirmationInput")));

                usernameConfirm.SendKeys(username);

                var passwordConfirm = wait.Until(d =>
                    d.FindElement(By.Id("idPasswordConfirmationInput")));

                passwordConfirm.SendKeys(password);

                var confirmButton = wait.Until(d =>
                    d.FindElement(By.Id("idBtnPasswordConfirmation")));

                confirmButton.Click();

                Console.WriteLine("Waiting final alert...");

                alert = alertWait.Until(d => {
                    try {
                        return d.SwitchTo().Alert();
                    } catch (NoAlertPresentException) {
                        return null;
                    }
                });

                Console.WriteLine($"Alert: {alert.Text}");

                alert.Accept();

                Console.WriteLine("Reset completed.");

                return true;

            } catch (Exception ex) {
                Console.WriteLine($"Reset failed: {ex.Message}");
                return false;
            }
        }
    }
}