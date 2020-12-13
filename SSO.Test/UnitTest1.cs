using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
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

            //HttpClient httpClient = new HttpClient();
            //httpClient.BaseAddress = new Uri(EDoc2_V5_Path);
            //StringContent stringContent = new StringContent(json, Encoding.UTF8, "application/json");
            //var result = httpClient.PostAsync("/api/services/Org/UserLoginIntegrationByUserLoginName", stringContent).Result.Content.ReadAsStringAsync() ;
            var result2 = PostUrl(url2, json);
        }

        private static string PostUrl(string url, string postData)
        {
            HttpWebRequest request = null;
            if (url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            {
                request = WebRequest.Create(url) as HttpWebRequest;
                request.Timeout = 5000;
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
            using (Stream newStream = request.GetRequestStream())
            {
                newStream.Write(data, 0, data.Length);
                newStream.Close();
            }

            //获取网页响应结果
            using HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            using Stream stream = response.GetResponseStream();
            //client.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
            using StreamReader sr = new StreamReader(stream);
            return sr.ReadToEnd();
        }

        private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true; //总是接受  
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
