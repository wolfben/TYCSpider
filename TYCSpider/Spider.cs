using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.PhantomJS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Utility.Helper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TYCSpider.Model;
using System.Threading;
using OpenQA.Selenium.Chrome;

namespace TYCSpider
{
    public class Spider
    {
        private Queue<CompanyBasicInfo> queueCompanyList = new Queue<CompanyBasicInfo>();

        public void Run()
        {
            Thread detailThread = new Thread(CompanyDetailHandle);
            detailThread.Start();

            CompanyInfoHandle();
        }

        private void CompanyInfoHandle()
        {
            PhantomJSDriverService pjsService = PhantomJSDriverService.CreateDefaultService();
            pjsService.LoadImages = false;
            var driver = new PhantomJSDriver(pjsService);
            driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(12);//设置页面加载超时时间为10秒
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(3);//设置查找元素不成功时，等待的时间

            Random rd = new Random();

            while (true)
            {
                var companyList = new List<string>();

                companyList.Add("浙江");
                companyList.Add("华天酒店");

                companyList = companyList.Where(p => Regex.IsMatch(p, @"^[a-zA-Z\s]+$") == false).ToList();//筛选不是全英文的公司名称

                foreach (var key in companyList)
                {
                    var url = string.Format("http://www.tianyancha.com/search?key={0}&checkFrom=searchBox", HttpUtility.UrlEncode(key, Encoding.UTF8));

                    Console.WriteLine("开始抓取公司：{0}", key);

                    try
                    {
                       
                        driver.Navigate().GoToUrl(url);

                        var firstElement = driver.FindElementByXPath("//div[@class='search_right_item']//a[contains(@class,'query_name')][1]");
                        if (firstElement == null)
                        {
                            Console.WriteLine("获取元素失败");
                            Thread.Sleep(rd.Next(2000, 3000));
                            continue;
                        }

                        var companyName = firstElement.Text;
                        var companyUrl = firstElement.GetAttribute("href");
                        Console.WriteLine("{0}的url：{1}", companyName, companyUrl);

                        Regex reg = new Regex(@".*?/company/(?<id>(\d+))", RegexOptions.IgnoreCase);
                        var match = reg.Match(companyUrl);
                        if (match.Success)
                        {
                            var id = match.Groups["id"].Value;
                            var companyInfo = new CompanyBasicInfo
                            {
                                Id = id,
                                CompanyName = companyName,
                                TYCCompanyUrl = companyUrl
                            };

                            //加入队列
                            queueCompanyList.Enqueue(companyInfo);                       
                        }

                    }
                    catch(WebDriverTimeoutException ex)
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

        private void CompanyDetailHandle()
        {
            HttpItem httpItem = new HttpItem { };
            var httpHelper = new HttpHelper();
            var driver = new PhantomJSDriver();
            while (true)
            {
                if (queueCompanyList != null && queueCompanyList.Count > 0)
                {
                    var company = queueCompanyList.Dequeue();
                    if (company != null)
                    {

                        #region 公司高管信息

                        httpItem.URL = string.Format("http://www.tianyancha.com/expanse/staff.json?id={0}&ps=5000&pn=1", company.Id);
                        var json1 = httpHelper.GetHtml(httpItem).Html;
                        if (!string.IsNullOrEmpty(json1))
                        {
                            var result = JObject.Parse(json1);
                            if (result != null)
                            {
                                var dataList = result["data"]["result"].ToArray();
                                var resultList = dataList.Select(p => new CompanyManager
                                {
                                    CompanyName = company.CompanyName,
                                    UserName = p["name"] + string.Empty,
                                    JobName = string.Join(",", p["typeJoin"].ToArray().Select(q => q.ToString()))
                                }).ToList();

                                foreach (var item in resultList)
                                {
                                    Console.WriteLine("公司名：{0}，高管：{1}，职称：{2}", item.CompanyName, item.UserName, item.JobName);
                                }
                            }
                        }
                        Thread.Sleep(500);

                        #endregion

                        #region 控股股东

                        httpItem.URL = string.Format("http://www.tianyancha.com/expanse/holder.json?id={0}&ps=5000&pn=1", company.Id);
                        var json2 = httpHelper.GetHtml(httpItem).Html;
                        if (!string.IsNullOrEmpty(json2))
                        {
                            var result = JObject.Parse(json2);
                            if (result != null)
                            {
                                var dataList = result["data"]["result"].ToArray();
                                var resultList = dataList.Select(p => new CompanyShareHolder
                                {
                                    CompanyName = company.CompanyName,
                                    UserName = p["name"] + string.Empty,
                                    Percent = string.Join(",", p["capital"].ToArray().Select(q => q["percent"].ToString())),
                                    RenJiaoMoney = string.Join(",", p["capital"].ToArray().Select(q => q["amomon"].ToString())),
                                    ShiJiaoMoney = string.Join(",", p["capitalActl"].ToArray().Select(q => q["amomon"].ToString()))
                                }).ToList();
                            }
                        }
                        Thread.Sleep(500);

                        #endregion

                        #region 控股参股

                        httpItem.URL = string.Format("http://www.tianyancha.com/expanse/inverst.json?id={0}&ps=5000&pn=1", company.Id);
                        var json3 = httpHelper.GetHtml(httpItem).Html;
                        if (!string.IsNullOrEmpty(json3))
                        {
                            var result = JObject.Parse(json3);
                            if (result != null)
                            {
                                var dataList = result["data"]["result"].ToArray();
                                var resultList = dataList.Select(p => new CompanyKongGu
                                {
                                    CompanyName = company.CompanyName,
                                    KongGuCompanyName = p["name"] + string.Empty,
                                    KongGuLegalPersonName = p["legalPersonName"] + string.Empty,
                                    KongGuRegisterMoney = p["regCapital"] + string.Empty,
                                    KongGuMoney = p["amount"] + string.Empty,
                                    KongGuPercent = p["percent"] + string.Empty,
                                    KongGuRegisterDate = ConvertIntDateTime(long.Parse(p["estiblishTime"] + string.Empty)).ToString("yyyy-MM-dd"),
                                    KongGuStatus = p["regStatus"] + string.Empty,
                                }).ToList();
                            }
                        }
                        Thread.Sleep(500);

                        #endregion

                        #region 对外投资

                        httpItem.URL = string.Format("http://www.tianyancha.com/expanse/findTzanli.json?name={0}&ps=5000&pn=1", HttpUtility.UrlEncode(company.CompanyName, Encoding.UTF8));
                        var json4 = httpHelper.GetHtml(httpItem).Html;
                        if (!string.IsNullOrEmpty(json4))
                        {
                            var result = JObject.Parse(json4);
                            if (result != null)
                            {
                                var dataList = result["data"]["page"]["rows"].ToArray();
                                var resultList = dataList.Select(p => new CompanyInvest
                                {
                                    CompanyName = company.CompanyName,
                                    Date = ConvertIntDateTime(long.Parse(p["tzdate"] + string.Empty)).ToString("yyyy-MM-dd"),
                                    TurnCount = p["lunci"] + string.Empty,
                                    Money = p["money"] + string.Empty,
                                    InvestName = p["organization_name"] + string.Empty,
                                    ProductName = p["product"] + string.Empty,
                                    City = p["location"] + string.Empty,
                                    Industry = p["hangye1"] + string.Empty,
                                    Profession = p["yewu"] + string.Empty
                                }).ToList();
                            }
                        }
                        Thread.Sleep(500);

                        #endregion

                        #region 招聘

                        httpItem.URL = string.Format("http://www.tianyancha.com/extend/getEmploymentList.json?companyName={0}&pn=1&ps=5000", HttpUtility.UrlEncode(company.CompanyName, Encoding.UTF8));
                        var json5 = httpHelper.GetHtml(httpItem).Html;
                        if (!string.IsNullOrEmpty(json5))
                        {
                            var result = JObject.Parse(json5);
                            if (result != null)
                            {
                                var dataList = result["data"]["companyEmploymentList"].ToArray();
                                var resultList = dataList.Select(p => new CompanyZhaoPin
                                {
                                    CompanyName = company.CompanyName,
                                    Date = ConvertIntDateTime(long.Parse(p["createTime"] + string.Empty)).ToString("yyyy-MM-dd"),
                                    JobName = p["title"] + string.Empty,
                                    InCome = p["oriSalary"] + string.Empty,
                                    Experience = p["experience"] + string.Empty,
                                    EmployerNumber = p["employerNumber"] + string.Empty,
                                    City = p["city"] + string.Empty,
                                }).ToList();
                            }
                        }
                        Thread.Sleep(500);

                        #endregion

                        #region 变更信息

                        httpItem.URL = string.Format("http://www.tianyancha.com/expanse/changeinfo.json?id={0}&ps=5000&pn=1", company.Id);
                        var json6 = httpHelper.GetHtml(httpItem).Html;
                        if (!string.IsNullOrEmpty(json6))
                        {
                            var result = JObject.Parse(json6);
                            if (result != null)
                            {
                                var dataList = result["data"]["result"].ToArray();
                                var resultList = dataList.Select(p => new CompanyChange
                                {
                                    CompanyName = company.CompanyName,
                                    ChangeDate = p["changeTime"] + string.Empty,
                                    ChangeProject = p["changeItem"] + string.Empty,
                                    ChangeBefore = p["contentBefore"] + string.Empty,
                                    ChangeAfter = p["contentAfter"] + string.Empty,
                                }).ToList();
                            }
                        }
                        Thread.Sleep(500);

                        #endregion

                        #region 网站备案

                        httpItem.URL = string.Format("http://www.tianyancha.com/v2/IcpList/{0}.json", company.Id);
                        var json7 = httpHelper.GetHtml(httpItem).Html;
                        if (!string.IsNullOrEmpty(json7))
                        {
                            var result = JObject.Parse(json7);
                            if (result != null)
                            {
                                var dataList = result["data"].ToArray();
                                var resultList = dataList.Select(p => new CompanyWebSite
                                {
                                    CompanyName = company.CompanyName,
                                    Date = p["examineDate"] + string.Empty,
                                    SiteName = p["webName"] + string.Empty,
                                    SiteUrl = p["webSite"] + string.Empty,
                                }).ToList();
                            }
                        }
                        Thread.Sleep(500);

                        #endregion

                    }
                }
            }
        }

        /// <summary>
        /// 将Unix时间戳转换为DateTime类型时间
        /// </summary>
        /// <param name="d">double 型数字</param>
        /// <returns>DateTime</returns>
        public DateTime ConvertIntDateTime(double d)
        {
            DateTime time = DateTime.MinValue;
            DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            time = startTime.AddMilliseconds(d);
            return time;
        }
    }
}

