﻿using OpenQA.Selenium;
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
using OpenQA.Selenium.Support.UI;

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
            WebDriverWait wait = new WebDriverWait(chromeDriver, TimeSpan.FromSeconds(5));//超过3秒将WebDriverTimeoutException异常

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

                        var baseInfoElements = wait.Until(p =>
                        {
                            return p.FindElements(By.XPath("//div[@class='baseInfo_model2017']//table[contains(@class,'companyInfo-table')]//tbody//td"));
                        });

                        if (baseInfoElements != null && baseInfoElements.Count > 0)
                        {
                            if (baseInfoElements[0] != null)
                            {
                                item.LegalPersonName = baseInfoElements[0].Text.Replace("他的所有公司 >", "");
                                Console.WriteLine("获取基本信息公司法人：{0}", item.LegalPersonName);
                            }

                            if (baseInfoElements.Count > 1 && baseInfoElements[1] != null)
                            {
                                item.RegisterMoney = baseInfoElements[1].Text;
                            }

                            if (baseInfoElements.Count > 2 && baseInfoElements[2] != null)
                            {
                                item.RegisterDate = baseInfoElements[2].Text;
                            }

                            if (baseInfoElements.Count > 3 && baseInfoElements[3] != null)
                            {
                                item.Status = baseInfoElements[3].Text;
                            }
                        }

                        var baseInfoDetailElements = wait.Until(p =>
                        {
                            return p.FindElements(By.XPath("//td[contains(@class,'basic-td')]//span[contains(@class,'ng-binding')]"));
                        });
                        if (baseInfoDetailElements != null && baseInfoDetailElements.Count > 0)
                        {
                            if (baseInfoDetailElements.Count > 3)
                            {
                                item.CompanyType = baseInfoDetailElements[3].Text;
                            }
                            if (baseInfoDetailElements.Count > 5)
                            {
                                item.OperationPeriod = baseInfoDetailElements[5].Text;
                            }
                            if (baseInfoDetailElements.Count > 8)
                            {
                                item.Address = baseInfoDetailElements[8].Text;
                            }
                            if (baseInfoDetailElements.Count > 9)
                            {
                                item.Scope = baseInfoDetailElements[9].Text;
                            }
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

