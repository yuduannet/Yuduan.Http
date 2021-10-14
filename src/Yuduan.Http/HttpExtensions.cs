#if NET452
using System.Net.Http;
#else
using System.Net.Http;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;


namespace Yuduan.Http
{

    public static class HttpExtensions
    {
       


        public static async Task<HttpResult> GetAsyncEx(this HttpClient client, string url, int timeout = 5000)
        {
            HttpResult result;
            try
            {
                HttpResponseMessage httpResponseMessage = await client.GetAsync(url, new CancellationTokenSource(timeout).Token);
                HttpResponseMessage resp = httpResponseMessage;
                if (resp.IsSuccessStatusCode)
                {
                    result = new HttpResult(true, await resp.Content.ReadAsStringAsync());
                }
                else
                {
                    result = new HttpResult(false, "返回码：" + resp.StatusCode);
                }
            }
            catch (Exception e)
            {
                result = new HttpResult(false, e.Message);
            }
            return result;
        }

        public static async Task<HttpResult> PostAsyncEx(this HttpClient client, string url, HttpContent content, int timeout = 5000)
        {
            HttpResult result;
            try
            {
                HttpResponseMessage httpResponseMessage = await client.PostAsync(url, content, new CancellationTokenSource(timeout).Token);
                HttpResponseMessage resp = httpResponseMessage;
                if (resp.IsSuccessStatusCode)
                {
                    result = new HttpResult(true, await resp.Content.ReadAsStringAsync());
                }
                else
                {
                    result = new HttpResult(false, "返回码：" + resp.StatusCode);
                }
            }
            catch (Exception e)
            {
                result = new HttpResult(false, e.Message);
            }
            return result;
        }



        /// <summary>
        /// HTTP请求重试
        /// </summary>
        /// <param name="func"></param>
        /// <param name="tryTime">重试次数</param>
        /// <returns></returns>
        public static async Task<HttpResult<T>> Retry<T>(Func<Task<HttpResult<T>>> func, int tryTime = 5)
        {
            for (int i = 0; i < tryTime; i++)
            {
                var result = await func();
                if (result.Success || i >= tryTime - 1) return result;
                Thread.Sleep(500);
            }
            return new HttpResult<T> { Success = false, Message = "超时" };
        }


        /// <summary>
        /// 对象转集合
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        internal static IEnumerable<KeyValuePair<string, string>> GetProperties(this object parameters)
        {
            if (parameters == null) yield break;
            if (parameters is ExpandoObject)
            {
                var dictionary = (IDictionary<string, object>)parameters;
                foreach (var property in dictionary)
                {
                    yield return new KeyValuePair<string, string>(property.Key, property.Value.ToString());
                }
            }
            else
            {
                var properties = TypeDescriptor.GetProperties(parameters);
                foreach (PropertyDescriptor propertyDescriptor in properties)
                {
                    var val = propertyDescriptor.GetValue(parameters);
                    if (val != null)
                    {
                        yield return new KeyValuePair<string, string>(propertyDescriptor.Name, val.ToString());
                    }
                }
            }
        }

        

        /// <summary>
        /// Cookie转LIST
        /// </summary>
        /// <returns></returns>
        public static List<Cookie> ToList(this CookieContainer cookies)
        {
            Hashtable table = (Hashtable)cookies.GetType().InvokeMember("m_domainTable", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance, null, cookies, new object[] { });
            return (from object pathList in table.Values select (SortedList)pathList.GetType().InvokeMember("m_list", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance, null, pathList, new object[] { }) into lstCookieCol from CookieCollection colCookies in lstCookieCol.Values from Cookie c in colCookies select c).ToList();
        }

        /// <summary>
        /// 根据Cookie的键获取值
        /// </summary>
        /// <param name="cookies"></param>
        /// <param name="key"></param>
        /// <param name="path">路径（不填则忽略）</param>
        /// <returns></returns>
        public static string GetValue(this CookieContainer cookies, string key, string path = null)
        {
            if (cookies == null)
                return string.Empty;
            Hashtable table = (Hashtable)cookies.GetType().InvokeMember("m_domainTable", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance, null, cookies, new object[] { });

            foreach (Cookie c in from object pathList in table.Values
                                 select (SortedList)pathList.GetType().InvokeMember("m_list", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance, null, pathList, new object[] { })
                into lstCookieCol
                                 from c in
                                 (from CookieCollection colCookies in lstCookieCol.Values
                                  from Cookie c in
                 colCookies
                                  where string.Equals(c.Name, key, StringComparison.CurrentCultureIgnoreCase) && path == null || string.Equals(c.Path, path)
                                  select c)
                                 select c)
            {
                return c.Value;
            }
            return "";
        }

        /// <summary>
        /// 从浏览器中的格式导入
        /// </summary>
        /// <param name="cookieContainer"></param>
        /// <param name="cookieString"></param>
        /// <param name="domain"></param>
        public static void Import(this CookieContainer cookieContainer, string cookieString, string domain)
        {
            StringBuilder sb = new StringBuilder(cookieString);
            sb.Replace("; ", ";");
            sb.Replace("\"", "");
            string[] cookies = sb.ToString().Split(';');
            foreach (string[] tempStrS in cookies.Select(ck => ck.Split('=')).Where(tempStrS => tempStrS.Length >= 2))
            {
                Cookie cookieTemp = new Cookie
                {
                    Name = tempStrS[0],
                    Value = HttpUtility.UrlEncode(tempStrS[1]),
                    Domain = domain,
                    Path = "/"
                };
                cookieContainer.Add(cookieTemp);
            }

        }

      
        /// <summary>
        /// 导出成浏览器
        /// </summary>
        /// <param name="cookieContainer"></param>
        /// <param name="domain"></param>
        /// <returns></returns>
        public static string Export(this CookieContainer cookieContainer, Uri domain = null)
        {
            List<Cookie> listCookies;
            if (domain != null)
            {
                var cookies = cookieContainer.GetCookies(domain);
                listCookies = cookies.Cast<Cookie>().ToList();
            }
            else
            {
                listCookies = cookieContainer.ToList();

            }
            return string.Join("; ", listCookies.Select(i => $"{i.Name}={i.Value}"));
        }


        /// <summary>
        /// 修改Cookie的作用域
        /// </summary>
        /// <param name="cookies"></param>
        /// <param name="domain"></param>
        public static CookieContainer ConvertDomain(this CookieContainer cookies, string domain)
        {
            if (cookies == null)
                return null;
            CookieContainer coo = new CookieContainer();
            Hashtable table = (Hashtable)cookies.GetType().InvokeMember("m_domainTable", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance, null, cookies, new object[] { });
            foreach (object pathList in table.Values)
            {
                SortedList lstCookieCol = (SortedList)pathList.GetType().InvokeMember("m_list", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance, null, pathList, new object[] { });
                foreach (CookieCollection colCookies in lstCookieCol.Values)
                    foreach (Cookie c in colCookies)
                    {
                        if (domain.StartsWith("."))
                            c.Domain = domain;
                        else
                            c.Domain = "." + domain;
                        coo.Add(c);

                    }
            }
            return coo;
        }

        /// <summary>
        /// 新增一条Cookie记录到CookieContainer中
        /// </summary>
        /// <param name="cookies"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="domain"></param>
        /// <param name="path"></param>
        /// <param name="httpOnly"></param>
        public static void AddCookie(this CookieContainer cookies, string name, string value, string domain, string path = "/", bool httpOnly = false)
        {
            Cookie cookie = new Cookie(name, value)
            {
                Domain = domain,
                Path = path,
                HttpOnly = httpOnly
            };
            cookies.Add(cookie);
        }
    }

}

