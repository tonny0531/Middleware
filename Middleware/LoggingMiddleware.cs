using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogMiddleware.MiddleWare
{
    /// <summary>
    /// Ref: https://poychang.github.io/logging-http-request-in-asp-net-core/
    /// Ref:https://blog.johnwu.cc/article/asp-net-core-3-read-request-response-body.html
    /// </summary>
    public class LoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public LoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            context.Request.EnableBuffering();

            string responseContent, requestContent;

            #region Request
            //Request Reader
            using (var bodyReader = new StreamReader(stream: context.Request.Body,
                                               encoding: Encoding.UTF8,
                                               detectEncodingFromByteOrderMarks: false,
                                               bufferSize: 1024,
                                               leaveOpen: true))
            {
                requestContent = await bodyReader.ReadToEndAsync();
                Console.WriteLine($"Request.Body={requestContent}");
                //必須特別注意要將Position歸回原位 不做實際執行時會有問題
                context.Request.Body.Position = 0;
            }

            #endregion

            #region Response
            var originalBodyStream = context.Response.Body;
            //需獨力建立MemoryStream原因是ResponseBody不能直接讀寫
            using (var fakeResponseBody = new MemoryStream())
            {
                context.Response.Body = fakeResponseBody;


                await _next(context);

                fakeResponseBody.Seek(0, SeekOrigin.Begin);
                using (var reader = new StreamReader(fakeResponseBody))
                {
                    responseContent = await reader.ReadToEndAsync();
                    fakeResponseBody.Seek(0, SeekOrigin.Begin);

                    await fakeResponseBody.CopyToAsync(originalBodyStream);
                }
            }
            Console.WriteLine($"Response.Body={responseContent}");
            #endregion
        }
    }
}
