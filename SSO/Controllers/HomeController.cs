using System;
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
using Newtonsoft.Json;
using Microsoft.Graph;

namespace SSO.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHttpClientFactory httpClientFactory;
        private const string SignInMicrosoftRoute = "/signin-microsoft";
        private const string TokenUrl = "/api/services/Org/UserLoginIntegrationByUserLoginName";
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
        public async Task<IActionResult> SignInCallBackAsync(string code)
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

                string json = JsonConvert.SerializeObject(new
                {
                    IntegrationKey = IntegrationKey,
                    LoginName = UserCode,
                    IPAddress = GetLocalIp(),
                });
                _logger.LogInformation($"SignInCallBack 获取token Url: {TokenUrl}，数据：{json}");

                var httpclientHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, error) => true
                };
                HttpClient httpClient = new HttpClient(httpclientHandler);
                httpClient.BaseAddress = new Uri(EDoc2_V5_Path);
                StringContent stringContent = new StringContent(json, Encoding.UTF8, "application/json");

                var result2 = await httpClient.PostAsync(TokenUrl, stringContent);

                _logger.LogInformation($"SignInCallBack 获取token返回结果: {result2}");
                dynamic dyObj =  JsonConvert.DeserializeObject(await result2.Content.ReadAsStringAsync());
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
                _logger.LogError(ex, ex.Message);
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
    }
}
