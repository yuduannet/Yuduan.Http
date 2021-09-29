using System.Net;
using System.Net.Http.Headers;

namespace Yuduan.Http
{
    /// <summary>
    /// HTTP请求返回
    /// </summary>
    public class HttpResult<T>
    {

        public HttpResult()
        {
        }

        public HttpResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }

        /// <summary>
        /// 请求成功
        /// </summary>
        public bool Success { get; internal set; }

        public T Response { get; internal set; }

        /// <summary>
        /// 响应状态码
        /// </summary>
        public HttpStatusCode StatusCode { get; internal set; }

        /// <summary>
        /// 消息
        /// </summary>
        public string Message { get; internal set; }
        /// <summary>
        /// 响应头
        /// </summary>
        public HttpResponseHeaders Headers { get; internal set; }


    }
    public class HttpResult : HttpResult<string>
    {
        public HttpResult()
        {

        }

        public HttpResult(bool success, string message)
        {
            Success = success;
            if (success)
            {
                Response = message;
                return;
            }
            Message = message;
        }
    }

}
