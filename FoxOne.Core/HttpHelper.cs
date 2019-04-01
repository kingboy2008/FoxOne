using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Web;
namespace FoxOne.Core
{
    public static class HttpHelper
    {
        public static void GetImage(string imgUrl, string imgPath)
        {
            var url = HttpContext.Current.Request.Url;
            WebClient client = new WebClient();
            FileInfo dirInfo = new FileInfo(imgPath);
            string acceptExts = ".js|.css|.jpg|.png|.bmp|.jpeg|.html|.ico|.gif";
            if (acceptExts.Split('|').Contains(dirInfo.Extension))
            {
                if (!dirInfo.Directory.Exists)
                {
                    dirInfo.Directory.Create();
                }
                try
                {
                    client.DownloadFile(imgUrl, imgPath);
                }
                catch
                {
                }
            }
        }

        /// <summary>  
        /// 执行基本的命令方法,以Get方式  
        /// </summary>  
        /// <param name="apiurl"></param>  
        /// <returns></returns>  
        public static String Get(string apiurl)
        {
            return RequestInner(apiurl, "GET");
        }

        /// <summary>  
        /// 以Post方式提交命令  
        /// </summary>  
        public static String Post(string apiurl, string jsonString)
        {
            return RequestInner(apiurl, "POST", jsonString);
        }

        /// <summary>  
        /// 以Put方式提交命令  
        /// </summary>  
        public static String Put(string apiurl, string jsonString)
        {
            return RequestInner(apiurl, "PUT", jsonString);
        }

        /// <summary>  
        /// 以Delete方式提交命令  
        /// </summary>  
        public static string Delete(string apiurl)
        {
            return RequestInner(apiurl, "DELETE");
        }

        /// <summary>
        /// 异步执行基本的命令方法,以Get方式
        /// </summary>
        public static string GetAsync(string apiurl)
        {
            return RequestAsyncInner(apiurl, "GET").Result;
        }

        /// <summary>
        /// 异步以Post方式提交命令
        /// </summary>
        public static string PostAsync(string apiurl, string jsonString)
        {
            return RequestAsyncInner( apiurl, "POST",jsonString).Result;
        }

        /// <summary>
        /// 异步以Put方式提交命令  
        /// </summary>
        public static string PutAsync(string apiurl, string jsonString)
        {
            return RequestAsyncInner(apiurl, "PUT", jsonString).Result;
        }

        /// <summary>
        /// 异步以Delete方式提交命令  
        /// </summary>
        public static string DeleteAsync(string apiurl, string jsonString)
        {
            return RequestAsyncInner(apiurl, "DELETE").Result;
        }


        private static String RequestInner(string apiurl, string method, string jsonString = default(string), string contentType = "application/json")
        {
            Logger.Info($"try-get:{method}-url:{apiurl},jsonString:{jsonString}");
            WebRequest request = WebRequest.Create(@apiurl);
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, error) => { return true; };//主要用来解决：“基础连接已经关闭: 未能为 SSL/TLS 安全通道建立信任关系”这个问题。
            request.Method = method;
            if (jsonString.IsNotNullOrEmpty())
            {
                request.ContentType = contentType;
                byte[] bs = Encoding.UTF8.GetBytes(jsonString);
                request.ContentLength = bs.Length;
                Stream newStream = request.GetRequestStream();
                newStream.Write(bs, 0, bs.Length);
                newStream.Close();
            }
            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Stream stream = response.GetResponseStream();
                Encoding encode = Encoding.UTF8;
                using (StreamReader reader = new StreamReader(stream, encode))
                {
                    string resultJson = reader.ReadToEnd();
                    Logger.Info($"{method}-url:{apiurl},jsonString:{jsonString},content:{resultJson}");
                    return resultJson;
                }
            }
            else
            {
                throw new Exception($"{method}-url:{apiurl},jsonString:{jsonString},,statusDescription:{response.StatusDescription}");
            }
        }

        private static async Task<string> RequestAsyncInner( string apiurl, string method, string jsonString = default(string),int timeout=60000, string contentType = "application/json")
        {
            Logger.Info($"try-get:{method}-url:{apiurl},jsonString:{jsonString}");
            WebRequest request = WebRequest.Create(@apiurl);
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, error) => { return true; };//主要用来解决：“基础连接已经关闭: 未能为 SSL/TLS 安全通道建立信任关系”这个问题。
            request.Method = method;
            request.Timeout = timeout;
            if (jsonString.IsNotNullOrEmpty())
            {
                request.ContentType = contentType;
                byte[] bs = Encoding.UTF8.GetBytes(jsonString);
                request.ContentLength = bs.Length;
                Stream newStream = request.GetRequestStream();
                newStream.Write(bs, 0, bs.Length);
                newStream.Close();
            }
            var response= await request.GetResponseAsync() as HttpWebResponse;
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Stream stream = response.GetResponseStream();
                Encoding encode = Encoding.UTF8;
                using (StreamReader reader = new StreamReader(stream, encode))
                {
                    string resultJson = reader.ReadToEnd();
                    Logger.Info($"{method}-url:{apiurl},jsonString:{jsonString},content:{resultJson}");
                    return resultJson;
                }
            }
            else
            {
                throw new Exception($"{method}-url:{apiurl},jsonString:{jsonString},,statusDescription:{response.StatusDescription}");
            }
        }

        static void GetWebApi(string apiUrl)
        {
            var request = WebRequest.Create(apiUrl) as HttpWebRequest;
            request.Method = "GET";
            request.Accept = "application/json";
            request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip,deflate");
            var response = request.GetResponse() as HttpWebResponse;

            Stream stream = response.GetResponseStream();
            if (response.ContentEncoding.Contains("gzip"))
            {
                stream = new GZipStream(stream, CompressionMode.Decompress);
            }
            using (StreamReader reader = new StreamReader(stream))
            {
                var result = reader.ReadToEnd();
            }
        }

        public static string BuildUrl(string url, HttpRequestBase request)
        {
            if (request == null || string.IsNullOrEmpty(url))
            {
                return url;
            }
            foreach (var p in request.QueryString.AllKeys)
            {
                if (url.IndexOf('?') > 0)
                {
                    url += string.Format("&{0}={1}", p, request.QueryString[p]);
                }
                else
                {
                    url += string.Format("?{0}={1}", p, request.QueryString[p]);
                }
            }
            return url;
        }

        public static string BuildUrl(string url, IDictionary<string, object> parameters)
        {
            if (parameters.IsNullOrEmpty() || string.IsNullOrEmpty(url))
            {
                return url;
            }
            foreach (var p in parameters)
            {
                if (url.IndexOf('?') > 0)
                {
                    url += string.Format("&{0}={1}", p.Key, p.Value);
                }
                else
                {
                    url += string.Format("?{0}={1}", p.Key, p.Value);
                }
            }
            return url;
        }
    }
}
