using OpenQA.Selenium;
using OpenQA.Selenium.PhantomJS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TYCSpider.Model;
using System.Threading;
using OpenQA.Selenium.Chrome;

namespace TYCSpider.BaseInfo
{
    public class Spider
    {
        public void Run()
        {
            CompanyInfoHandle();
        }

        /// <summary>
        /// 获取公司基本信息
        /// </summary>
        private void CompanyInfoHandle()
        {
            IWebDriver chromeDriver = new ChromeDriver();
            chromeDriver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(10000);//设置页面加载超时时间为10秒
            chromeDriver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(3000);//设置查找元素不成功时，等待的时间

            Random rd = new Random();

            while (true)
            {
                //此处获取公司列表信息
                var companyList = new List<CompanyBasicInfo>();

                companyList.Add(new CompanyBasicInfo
                {
                    TYCCompanyUrl = "http://www.tianyancha.com/company/24690101"
                });

                foreach (var item in companyList)
                {
                    Console.WriteLine("开始抓取公司：{0}", item.CompanyName);

                    try
                    {
                        #region 公司基本信息（需要用类似ChromeDriver这样的游览器模式进行渲染获取数据）

                        chromeDriver.Navigate().GoToUrl(item.TYCCompanyUrl);

                        var baseInfoElements = chromeDriver.FindElements(By.XPath("//div[contains(@class,'baseinfo-module-content-value')]"));
                        if (baseInfoElements != null && baseInfoElements.Count > 0)
                        {
                            var baseInfoE1 = chromeDriver.FindElement(By.XPath("//div[contains(@class,'baseinfo-module-content-value')]//a[1]"));
                            if (baseInfoE1 != null)
                            {
                                item.LegalPersonName = baseInfoE1.Text;
                                Console.WriteLine("获取基本信息公司法人：{0}", item.LegalPersonName);
                            }

                            if (baseInfoElements[1] != null)
                            {
                                item.RegisterMoney = baseInfoElements[1].Text;
                            }

                            if (baseInfoElements[2] != null)
                            {
                                item.RegisterDate = baseInfoElements[2].Text;
                            }

                            if (baseInfoElements[3] != null)
                            {
                                item.Status = baseInfoElements[3].Text;
                            }
                        }

                        var baseInfoDetailElements = chromeDriver.FindElements(By.XPath("//td[contains(@class,'basic-td')]//span[1]"));
                        if (baseInfoDetailElements != null && baseInfoDetailElements.Count > 0)
                        {
                            item.CompanyType = baseInfoDetailElements[4].Text;
                            item.OperationPeriod = baseInfoDetailElements[5].Text;
                            item.Address = baseInfoDetailElements[8].Text;
                            item.Scope = baseInfoDetailElements[9].Text;
                        }

                        //进行更新操作

                        #endregion

                    }
                    catch (WebDriverTimeoutException ex)
                    {
                        //超时日志不记录
                    }
                    catch (Exception ex)
                    {
                        //记录日志
                    }
                    Thread.Sleep(1000);
                }
            }
        }

    }
}

