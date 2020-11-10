using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Threading;

namespace JdCheckIn
{
    class Program
    {
        static void Main(string[] args)
        {
            JdCheckInCore core = new JdCheckInCore();

            core.OpenBrowser();

            core.GoToUrl("https://m.jd.com/");
            core.InitializeCookies(File.ReadAllLines("cookies.txt"));

            core.GoToUrl("https://m.jd.com/");

            List<CheckInRecord> records = new List<CheckInRecord>();
            foreach (string line in File.ReadAllLines("tasks.txt"))
            {
                var lineElement = line.Split('^');
                records.Add(new CheckInRecord()
                {
                    Name = lineElement[0],
                    Url = lineElement[1],
                    Target = lineElement[2],
                    NeedScrollDown = lineElement[3] == "Y"
                });
            }

            core.DailyCheckIn(records);
            core.CloseBrowser();
        }
    }

    public class JdCheckInCore
    {
        public IWebDriver Driver { get; private set; }

        public void OpenBrowser()
        {
            Driver = new ChromeDriver();
        }

        public void CloseBrowser()
        {
            Driver.Quit();
        }

        public void GoToUrl(string url)
        {
            Driver.Navigate().GoToUrl(url);
        }

        public void InitializeCookies(string[] cookies)
        {
            foreach (string cookie in cookies)
            {
                string cookieKey = null;
                string cookieValue = null;
                DateTime? expiry = null;
                string path = null;
                string domain = null;
                foreach (string cookieKvp in cookie.Split(';'))
                {
                    if (!cookieKvp.Contains('='))
                    {
                        continue;
                    }

                    string key = cookieKvp.Split('=')[0].Trim();
                    string value = cookieKvp.Split('=')[1].Trim();

                    switch (key)
                    {
                        case "expires":
                            expiry = DateTime.Parse(value.Replace("UTC", ""));
                            break;

                        case "path":
                            path = value;
                            break;

                        case "domain":
                            domain = value;
                            break;

                        default:
                            cookieKey = key;
                            cookieValue = value;
                            break;
                    }
                }

                Driver.Manage().Cookies.AddCookie(new Cookie(cookieKey, cookieValue, domain, path, expiry));
            }
        }

        public void ExecuteScript(string script)
        {
            IJavaScriptExecutor jse = (IJavaScriptExecutor)Driver;
            jse.ExecuteScript(script);
        }

        public void DailyCheckIn(IEnumerable<CheckInRecord> records)
        {
            foreach (CheckInRecord record in records)
            {
                if (record.Target == "SKIP")
                {
                    continue;
                }

                Driver.Navigate().GoToUrl(record.Url);

                int scrollDownTime = record.NeedScrollDown ? 10 : 2;
                bool operated = false;

                foreach (string target in record.Target.Split('|'))
                {
                    while (scrollDownTime >= 0)
                    {
                        if (FindElementIfExist(target, out IWebElement element))
                        {
                            try
                            {
                                element.Click();
                            }
                            catch (ElementClickInterceptedException)
                            {
                                ExecuteScript("window.scrollBy(0,500)");
                                element.Click();
                            }
                            Thread.Sleep(1000);
                            operated = true;
                            break;
                        }
                        else
                        {
                            ExecuteScript("window.scrollBy(0,500)");
                            Thread.Sleep(1000);
                        }
                        scrollDownTime--;
                    }
                }

                if (operated)
                {
                    Console.WriteLine(record.Name + " Passed");
                }
                else
                {
                    Console.WriteLine(record.Name + " Failed");
                }
            }
        }

        public bool FindElementIfExist(string xpath, out IWebElement webElement)
        {
            try
            {
                webElement = Driver.FindElement(By.XPath(xpath));
                return true;
            }
            catch (Exception)
            {
                webElement = null;
                return false;
            }
        }
    }

    public class CheckInRecord
    {
        public string Name;

        public string Url;

        public string Target;

        public bool NeedScrollDown;
    }
}
