# Yuduan.Http
a library base on HttpClient.



```
using (HttpHelper http = new HttpHelper())
            {
                var postResp = await http.PostAsync("https://xxx.com",
                       new FormUrlEncodedContent(new[]
                       {
                        new KeyValuePair<string, string>("key", "value"),
                        new KeyValuePair<string, string>("key1", "value1")
                       }));
                if (postResp.Success)
                    Console.WriteLine(postResp.Response);
                else
                    Console.WriteLine($"statusCode:{postResp.StatusCode},error:{postResp.Message}");

                var getResp = await http.GetStringAsync("https://xxx.com");
}
```
