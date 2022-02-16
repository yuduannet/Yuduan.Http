#if NET452
using System.Net.Http;
using System.Web;
#else
using System.Net.Http;
#endif
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Yuduan.Http
{
    /// <summary>
    /// HTTP请求的封装
    /// </summary>
    public class HttpHelper : IDisposable
    {
        public CookieContainer Cookies { get; set; } = new CookieContainer();

        public readonly HttpClient HttpClient;
        /// <summary>
        /// 默认超时时间
        /// </summary>
        public int DefaultTimeOut { get; set; } = 15000;


        private readonly HttpClientHandler _clientHandler;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="useCookie">是否使用Cookie</param>
        /// <param name="proxy">代理</param>
        /// <param name="automaticDecompression">压缩</param>
        /// <param name="cookie"></param>
        /// <param name="userAgent"></param>
        public HttpHelper(bool useCookie = true, IWebProxy proxy = null, bool automaticDecompression = true, CookieContainer cookie = null, string userAgent = null)
        {
            _clientHandler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
#if !NET452
                ServerCertificateCustomValidationCallback = (message, certificate2, arg3, arg4) => true, //fix ssl error
#endif
                UseCookies = useCookie
            };
            if (automaticDecompression)
                _clientHandler.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            HttpClient = new HttpClient(_clientHandler);
            if (useCookie)
            {
                if (cookie != null)
                    Cookies = cookie;
                _clientHandler.CookieContainer = Cookies;
            }

            if (proxy != null)
            {
                _clientHandler.Proxy = proxy;
                _clientHandler.UseProxy = true;
            }

            if (!string.IsNullOrEmpty(userAgent))
                HttpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

        }

        public HttpHelper(HttpClientHandler clientHandler)
        {
            _clientHandler = clientHandler;
            HttpClient = new HttpClient(_clientHandler);
        }

        public HttpHelper(HttpClient httpClient)
        {
            HttpClient = httpClient;
        }
        /// <summary>
        /// 添加标头（该实例下的所有http请求都会带上该headers）
        /// </summary>
        public void AddGlobalHeader(string name, string value)
        {
            HttpClient.DefaultRequestHeaders.TryAddWithoutValidation(name, value);
        }

        /// <summary>
        /// 添加标头（该实例下的所有http请求都会带上该headers）
        /// </summary>
        /// <param name="headers"></param>
        public void AddGlobalHeaders(IDictionary<string, string> headers)
        {
            foreach (var keyValuePair in headers)
            {
                HttpClient.DefaultRequestHeaders.Add(keyValuePair.Key, keyValuePair.Value);
            }
        }

        /// <summary>
        /// 移除标头
        /// </summary>
        /// <param name="name"></param>
        public void RemoveGlobalHeader(string name)
        {
            HttpClient.DefaultRequestHeaders.Remove(name);
        }

        public void Dispose()
        {
            _clientHandler?.Dispose();
            HttpClient?.Dispose();
        }

        #region GET请求

        /// <summary>
        /// 获取HTTP
        /// </summary>
        /// <param name="url"></param>
        /// <param name="headers"></param>
        /// <returns></returns>
        public async Task<HttpResult<string>> GetStringAsync(string url, IDictionary<string, string> headers = null)
        {
            return await SendAsync(url, HttpMethod.Get, c => c.ReadAsStringAsync(), headers);
        }

        /// <summary>
        /// 获取HTTP
        /// </summary>
        /// <param name="url"></param>
        /// <param name="model"></param>
        /// <param name="headers"></param>
        /// <returns></returns>
        public async Task<HttpResult<string>> GetStringAsync<T>(string url, T model, IDictionary<string, string> headers = null)
        {
            try
            {
                url = url.TrimEnd('?');
                var keyValues = model.GetProperties();

                var query = string.Join("&", keyValues.Select(i =>
                {
#if NET452
                    return $"{i.Key}={HttpUtility.UrlEncode(i.Value)}";
#else
                    return $"{i.Key}={WebUtility.UrlEncode(i.Value)}";
#endif

                }));
                url = $"{url}?{query}";
            }
            catch (Exception e)
            {
                return new HttpResult
                {
                    Success = false,
                    Message = $"地址处理失败{e.Message}"
                };
            }

            return await SendAsync(url, HttpMethod.Get, c => c.ReadAsStringAsync(), headers);

        }

        /// <summary>
        /// 获取HTTP
        /// </summary>
        /// <param name="url"></param>
        /// <param name="headers"></param>
        /// <returns></returns>
        public async Task<HttpResult<byte[]>> GetBytesAsync(string url, IDictionary<string, string> headers = null)
        {
            return await SendAsync(url, HttpMethod.Get, c => c.ReadAsByteArrayAsync(), headers);

        }

        /// <summary>
        /// 获取HTTP
        /// </summary>
        /// <param name="url"></param>
        /// <param name="headers"></param>
        /// <returns></returns>
        public async Task<HttpResult<Stream>> GetStreamAsync(string url, IDictionary<string, string> headers = null)
        {
            return await SendAsync(url, HttpMethod.Get, c => c.ReadAsStreamAsync(), headers);
        }



        #endregion

        #region POST请求
        /// <summary>
        /// POST提交表单（实体方式）
        /// </summary>
        /// <param name="url"></param>
        /// <param name="model">实体或FormUrlEncodedContent</param>
        /// <param name="headers"></param>
        /// <returns>字符串</returns>
        public async Task<HttpResult<string>> PostFormDataAsync<T>(string url, T model, IDictionary<string, string> headers = null) where T : class
        {
            FormUrlEncodedContent content;
            if (model is FormUrlEncodedContent encodedContent)
                content = encodedContent;
            else
            {
                var keyValues = model.GetProperties();
                content = new FormUrlEncodedContent(keyValues);
            }

            return await SendAsync(url, HttpMethod.Post, c => c.ReadAsStringAsync(), headers, content);

        }
        /// <summary>
        /// POST提交表单（&amp;方式）
        /// </summary>
        /// <param name="url"></param>
        /// <param name="postData"></param>
        /// <param name="encoding"></param>
        /// <param name="headers"></param>
        /// <returns></returns>
        public async Task<HttpResult<string>> PostFormTextAsync(string url, string postData, Encoding encoding = null, IDictionary<string, string> headers = null)
        {
            return await PostTextAsync(url, postData, encoding, "application/x-www-form-urlencoded", headers);
        }

        /// <summary>
        /// POST提交JSON对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="url"></param>
        /// <param name="model"></param>
        /// <param name="headers"></param>
        /// <returns>字符串</returns>
        public async Task<HttpResult<string>> PostJsonDataAsync<T>(string url, T model, IDictionary<string, string> headers = null)
        {
            try
            {
                var data = JsonConvert.SerializeObject(model);
                return await PostTextAsync(url, data, mediaType: "application/json", headers: headers);
            }
            catch (Exception e)
            {
                return new HttpResult<string>(false, $"serialize object failed:{e.Message}");
            }

        }
        /// <summary>
        /// POST提交JSON
        /// </summary>
        /// <param name="url"></param>
        /// <param name="model"></param>
        /// <param name="headers"></param>
        /// <returns>字符串</returns>
        public async Task<HttpResult<string>> PostJsonTextAsync(string url, string model, IDictionary<string, string> headers = null)
        {
            string data;
            try
            {
                data = JsonConvert.SerializeObject(model);

            }
            catch (Exception e)
            {
                return new HttpResult<string>(false, $"serialize object failed , {e.Message}");
            }
            return await PostTextAsync(url, data, mediaType: "application/json", headers: headers);
        }

        /// <summary>
        /// Post提交文本
        /// </summary>
        /// <param name="url"></param>
        /// <param name="data"></param>
        /// <param name="encoding">默认为UTF-8</param>
        /// <param name="mediaType">默认为text/plain</param>
        /// <param name="headers"></param>
        /// <returns></returns>
        public async Task<HttpResult<string>> PostTextAsync(string url, string data, Encoding encoding = null, string mediaType = null, IDictionary<string, string> headers = null)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;
            if (mediaType == null)
                mediaType = "text/plain";
            StringContent content = new StringContent(data, encoding, mediaType);
            return await SendAsync(url, HttpMethod.Post, c => c.ReadAsStringAsync(), headers, content);
        }

        /// <summary>
        /// POST提交字节集
        /// </summary>
        /// <param name="url"></param>
        /// <param name="data"></param>
        /// <param name="headers"></param>
        /// <returns></returns>
        public async Task<HttpResult<byte[]>> PostBytesAsync(string url, byte[] data, IDictionary<string, string> headers = null)
        {
            ByteArrayContent content = new ByteArrayContent(data);
            return await SendAsync<byte[]>(url, HttpMethod.Post, c => c.ReadAsByteArrayAsync(), headers, content);
        }

        public async Task<HttpResult<string>> PostAsync(string url, HttpContent content, IDictionary<string, string> headers = null)
        {
            return await SendAsync(url, HttpMethod.Post, c => c.ReadAsStringAsync(), headers, content);
        }

        #endregion


        private async Task<HttpResult<T>> SendAsync<T>(string url, HttpMethod method, Func<HttpContent, Task<T>> func, IDictionary<string, string> headers = null, HttpContent content = null)
        {
            var message = new HttpRequestMessage(method, url);
            if (headers != null)
            {
                foreach (var keyValuePair in headers)
                {
                    message.Headers.Add(keyValuePair.Key, keyValuePair.Value);
                }
            }

            if (content != null)
                message.Content = content;

            var result = new HttpResult<T>();
            try
            {
                var resp = await HttpClient.SendAsync(message, new CancellationTokenSource(DefaultTimeOut).Token);
                result.Headers = resp.Headers;
                result.StatusCode = resp.StatusCode;
                result.Success = resp.IsSuccessStatusCode;
                result.Response = await func(resp.Content);
                resp.Dispose();
                return result;

            }
            catch (TaskCanceledException)
            {
                result.Success = false;
                result.Message = "timeout";
                return result;
            }
            catch (Exception e)
            {
                result.Success = false;
                result.Message = e.Message;
                return result;

            }
            finally
            {
                message.Dispose();
            }

        }

    }
}
