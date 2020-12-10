﻿using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using Newtonsoft.Json;
using Microsoft.Graph;

namespace SSO.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHttpClientFactory httpClientFactory;
        private const string SignInMicrosoftRoute = "/signin-microsoft";
        private const string ClientId = "6a560634-8f5f-43e0-a97c-5790180e5a07";
        public const string GraphClientBaseUrl = "https://microsoftgraph.chinacloudapi.cn/v1.0";
        public const string EDoc2_V5_Path = "https://47.92.240.66:2443";                  //项目根路径        
        public const string IntegrationKey = "46aa92ec-66af-4818-b7c1-8495a9bd7f17";      //集成登录用到        
        public const string SSoClientID = "edocadmin@genorbio.com";                       //客户端Id            
        public const string SSoClientSecret = "6b8b57e5-ddb5-4841-a8be-13b85e5270c7";     //密钥        
        public const string SSoAuthorizationPath = "https://47.92.240.66:2443/profile/oauth2/authorize";  //授权获取tokenUrl        
        public const string SSoAoauth2Path = "https://47.92.240.66:2443/profile/oauth2/accessToken";      //获取token        
        public const string SSoAuthInfo = "https://47.92.240.66:2443/profile/oauth2/profile";             //获取用户信息Url        
        public const string SSoLoginOut = "https://47.92.240.66:2443/logout";                             //注销登录接口        
        public const string PortalUrl = "/index.html";                                                    //门户地址        
        public const string talentId = "665d8ad5-743b-4826-9ace-3327992187cc";                                                    //门户地址        
        public const string CallBack = "http://10.0.49.244:5177/sso/callback";                            //回调地址

        public HomeController(ILogger<HomeController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            this.httpClientFactory = httpClientFactory;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Login()
        {
            var url = GetSignInUrl();
            return Redirect(url);
        }

        [HttpGet]
        [Route(SignInMicrosoftRoute)]
        public IActionResult SignInCallBack(string code)
        {
            _logger.LogInformation($"SignInCallBack code: {code}");
            try
            {
                var keyValuePairs = new Dictionary<string, string>()
                {
                    { "client_id", ClientId },
                    { "redirect_uri", $"https://{this.HttpContext.Request.Host.Value}/signin-microsoft" },
                    { "client_secret", "5osm29e4gm2Dgb9X6Aw~qj~8j_EKtq~UCm" },
                    { "code", code },
                    { "scope", "https://microsoftgraph.chinacloudapi.cn/.default" },
                    { "grant_type", "authorization_code" },
                    { "resource", "https://microsoftgraph.chinacloudapi.cn"},
                };

                using var client = this.httpClientFactory.CreateClient();
                string url = $"https://login.partner.microsoftonline.cn/{talentId}/oauth2/token";
                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new FormUrlEncodedContent(keyValuePairs),
                };
                using var response = client.SendAsync(request).Result;
                string result = response.Content.ReadAsStringAsync().Result;
                dynamic obj = JsonConvert.DeserializeObject(result);

                string access_token = obj.access_token;
                _logger.LogInformation($"SignInCallBack access_token: {access_token}");

                var user = GetUser(access_token);

                _logger.LogInformation($"SignInCallBack user: {user.Mail}");

                string UserCode = user.DisplayName.ToString();

                //为空判断
                if (string.IsNullOrEmpty(UserCode))
                {
                    return Redirect($"{EDoc2_V5_Path}/loginerror.html");
                }

                //获取token
                string url2 = $"{EDoc2_V5_Path}/api/services/Org/UserLoginIntegrationByUserLoginName";

                _logger.LogInformation($"SignInCallBack url2: {url2}");
                var result2 = PostUrl(url2, JsonConvert.SerializeObject(new
                {
                    IntegrationKey = IntegrationKey,
                    LoginName = UserCode,
                    IPAddress = GetLocalIp(),
                }));


                _logger.LogInformation($"SignInCallBack {nameof(result2)}: {result2}");
                dynamic dyObj = JsonConvert.DeserializeObject(result2);
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

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("错误信息:" + ex.Message.ToString());
                return Redirect($"{EDoc2_V5_Path}/loginerror.html");
            }
        }

        private User GetUser(string access_code)
        {
            var graphServiceClient = new GraphServiceClient(GraphClientBaseUrl, new DelegateAuthenticationProvider((requestMessage) =>
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access_code);
                return Task.FromResult(0);
            }));

            var user = graphServiceClient.Me.Request()
                .Select("Mail,DisplayName")
                .GetAsync().Result;
            return user;
        }

        private string GetSignInUrl()
        {
            var redirect_uri = UriHelper.BuildAbsolute("https", this.HttpContext.Request.Host, SignInMicrosoftRoute);
            var query = QueryString.Create("client_id", ClientId);
            query += QueryString.Create("response_type", "code");
            query += QueryString.Create("redirect_uri", redirect_uri);
            query += QueryString.Create("scope", "https://microsoftgraph.chinacloudapi.cn/.default");
            query += QueryString.Create("state", "11111");
            var url = "https://login.partner.microsoftonline.cn/common/oauth2/authorize" + query;
            return url;
        }
        ///获取本地的IP地址
        private string GetLocalIp()
        {
            string AddressIP = string.Empty;
            foreach (IPAddress _IPAddress in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (_IPAddress.AddressFamily.ToString() == "InterNetwork")
                {
                    AddressIP = _IPAddress.ToString();
                }
            }
            return AddressIP;
        }

        private static string PostUrl(string url, string postData)
        {
            HttpWebRequest request = null;
            if (url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            {
                request = WebRequest.Create(url) as HttpWebRequest;
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
                request.ProtocolVersion = HttpVersion.Version11;
                // 这里设置了协议类型。
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;// SecurityProtocolType.Tls1.2; 
                request.KeepAlive = false;
                ServicePointManager.CheckCertificateRevocationList = true;
                ServicePointManager.DefaultConnectionLimit = 100;
                ServicePointManager.Expect100Continue = false;
            }
            else
            {
                request = (HttpWebRequest)WebRequest.Create(url);
            }

            request.Method = "POST";    //使用get方式发送数据
            request.ContentType = "application/json";
            request.Referer = null;
            request.AllowAutoRedirect = true;
            request.UserAgent = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.2; .NET CLR 1.1.4322; .NET CLR 2.0.50727)";
            request.Accept = "*/*";

            byte[] data = Encoding.UTF8.GetBytes(postData);
            Stream newStream = request.GetRequestStream();
            newStream.Write(data, 0, data.Length);
            newStream.Close();

            //获取网页响应结果
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream stream = response.GetResponseStream();
            //client.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
            string result = string.Empty;
            using (StreamReader sr = new StreamReader(stream))
            {
                result = sr.ReadToEnd();
            }

            return result;
        }

        private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true; //总是接受  
        }
    }
}
