﻿using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace SSO.Controllers
{
    public class HomeController : Controller
    {
        public const string EDoc2_V5_Token_Path = "https://10.10.21.132:443";
        public const string EDoc2_V5_Path = "https://47.92.240.66:2443";
        private const string TokenUrl = "/api/services/Org/UserLoginIntegrationByUserLoginName";
        public const string IntegrationKey = "46aa92ec-66af-4818-b7c1-8495a9bd7f17";      //集成登录用到 
        public const string PortalUrl = "/index.html";
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                LogCookie();
                //从世纪互联过来则登录
                var urlReferer = Request.Headers["Referer"].FirstOrDefault();

                _logger.LogInformation($"urlReferer:{urlReferer}");
                _logger.LogInformation($"从世纪互联过来则登录");
                string json = JsonConvert.SerializeObject(new
                {
                    IntegrationKey,
                    LoginName = HttpContext.User.FindFirst("name").Value,
                    IPAddress = GetLocalIp(),
                });
                var httpclientHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, error) => true
                };
                HttpClient httpClient = new HttpClient(httpclientHandler);
                httpClient.BaseAddress = new Uri(EDoc2_V5_Token_Path);
                StringContent stringContent = new StringContent(json, Encoding.UTF8, "application/json");
                var result2 = await httpClient.PostAsync(TokenUrl, stringContent);

                _logger.LogInformation($"SignInCallBack 获取token返回结果: {result2}");
                dynamic dyObj = JsonConvert.DeserializeObject(await result2.Content.ReadAsStringAsync());
                //判断是否成功
                string token = dyObj.data;
                if (token != null)
                {
                    _logger.LogInformation($"用户wwid集成登录成功跳转");
                    //获取urlcookie
                    HttpContext.Request.Cookies.TryGetValue("returnUrl", out string returnUrl);
                    if (string.IsNullOrEmpty(returnUrl))
                    {
                        returnUrl = $"{EDoc2_V5_Path}{PortalUrl}";
                    }

                    return Redirect($"{EDoc2_V5_Path}/jump.html?token={token}&returnUrl={returnUrl}");
                }
                else
                {
                    _logger.LogWarning($"用户wwid 集成登录失败");
                }

                _logger.LogInformation($"Index 登录： /MicrosoftIdentity/Account/SignIn");
                return Redirect("/MicrosoftIdentity/Account/SignIn");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return Redirect($"{EDoc2_V5_Path}/loginerror.html");
            }
        }

        public IActionResult Login()
        {
            LogCookie();

            if (this.HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/MicrosoftIdentity/Account/SignOut");
            }
            _logger.LogInformation($"Login 登录： /MicrosoftIdentity/Account/SignIn");
            return Redirect("/MicrosoftIdentity/Account/SignIn");
        }

        private void LogCookie()
        {
            var cookies = this.HttpContext.Request.Cookies;
            foreach (var item in cookies)
            {
                _logger.LogWarning($"Cookie Key: {item.Key},Value: {item.Value}");
            }
        }

        private string GetLocalIp()
        {
            string AddressIP = string.Empty;
            foreach (var _IPAddress in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (_IPAddress.AddressFamily.ToString() == "InterNetwork")
                {
                    AddressIP = _IPAddress.ToString();
                }
            }
            return AddressIP;
        }
    }
}
