using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using Xunit;

namespace SSO.Test
{
    public class UnitTest1
    {
        public const string EDoc2_V5_Path = "https://47.92.240.66:2443";                  //项目根路径        
        public const string IntegrationKey = "46aa92ec-66af-4818-b7c1-8495a9bd7f17";      //集成登录用到        

        [Fact]
        public void Test1()
        {
            string url2 = $"{EDoc2_V5_Path}/api/services/Org/UserLoginIntegrationByUserLoginName";

            string json = JsonConvert.SerializeObject(new
            {
                IntegrationKey = IntegrationKey,
                LoginName = "EdocAdmin",
                IPAddress = GetLocalIp(),
            });

            var httpclientHandler = new HttpClientHandler();
            httpclientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, error) => true;
            HttpClient httpClient = new HttpClient(httpclientHandler);
            httpClient.BaseAddress = new Uri(EDoc2_V5_Path);
            StringContent stringContent = new StringContent(json, Encoding.UTF8, "application/json");
           
            var result = httpClient.PostAsync("/api/services/Org/UserLoginIntegrationByUserLoginName", stringContent).Result.Content.ReadAsStringAsync();
        }
        
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
