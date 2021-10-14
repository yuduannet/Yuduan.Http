# Yuduan.Http
a library base on HttpClient. 一个基于HttpClient实现的HTTP请求扩展库



### 使用示例
```csharp
   //从CookieContainer
   var cookies = new CookieContainer();
   //手动构造cookies
   cookies.AddCookie("name", "value", ".xxx.com");
   //或 导入浏览器抓包得到的cookies
   cookies.Import("cookie=1; cookie1=2", ".xxx.com");
   using (HttpHelper http = new HttpHelper(cookie: cookies))
   {
       //添加一个全局header，所有请求都会带上该header
       http.AddGlobalHeader("User-Agent", "Mozilla/5.0 (Linux; Android 6.0.1; Moto G (4)) ....");
       //http.AddGlobalHeaders(new Dictionary<string, string> { { "User-Agent", "Mozilla/5.0 (Linux; Android 6.0.1; Moto G (4)) ...." }, { "Referer", "https://www.bbb.com" } });
       await http.GetStringAsync("https://www.xxx.com/a/b?key=value&key1=value1");
       await http.GetStringAsync("https://www.xxx.com/a/b", new
       {
           key = "value",
           key1 = "value1"
       });
       await http.GetBytesAsync("https://www.xxx.com/1.jpg");
       await http.GetStreamAsync("https://www.xxx.com/1.jpg");
       // 提交一个 Content-Type 为 application/x-www-form-urlencoded 的POST请求
       var resp = await http.PostFormDataAsync("https://www.xxx.com", new
       {
           key1 = "value1",
           key2 = "value2"
       });
       if (resp.Success)
           Console.WriteLine(resp.Response);
       else
           Console.WriteLine($"statusCode:{resp.StatusCode},error:{resp.Message}");
       await http.PostFormTextAsync("https://www.xxx.com", "key=value&key1=value1");
       //提交一个 Content-Type 为 application/json 的POST请求
       resp = await http.PostJsonDataAsync("https://www.xx.com", new
       {
           key1 = "value1",
           key2 = "value2"
       });
       //POST json字符串
       await http.PostJsonTextAsync("https://www.xxx.com", @"{""key"":""value"",""key1"":12}");
       //POST 字节集
       await http.PostBytesAsync("https://www.xxx.com", new byte[] { 0, 1, 2 });
       //POST 字符串 并指定Content-Type
       await http.PostTextAsync("https://www.xx.com", "abc", Encoding.UTF8, "text/plain");
       //POST 原生的Content对象
       await http.PostAsync("https://www.xx.com", new StringContent(""));
       //POST 并带上指定header，该header仅对本次请求有效，不影响下个请求
       await http.PostJsonTextAsync("https://www.xxx.com", @"{""key"":""value"",""key1"":12}",
           new Dictionary<string, string>()
           {
               {"Referer", "https://www.xxx.com/a"},
               {"User-Agent", "Mozilla/5.0 (Linux; Android 6.0.1;"},
               {"sign", ""}
           });
       //使用原生的HttpClient对象
       var postResp = await http.HttpClient.PostAsync("https://www.xxx.com", new ByteArrayContent(new byte[] { 0 }));
       if (postResp.IsSuccessStatusCode)
       {
           var text = await postResp.Content.ReadAsStringAsync();
           Console.WriteLine(text);
       }
       //使用原生的HttpClient对象，并使用扩展方法
       resp = await http.HttpClient.PostAsyncEx("https://www.xxx.com", new ByteArrayContent(new byte[] { 0 }));
       if (resp.Success)
       {
           Console.WriteLine(resp.Message);
       }
   }
```
