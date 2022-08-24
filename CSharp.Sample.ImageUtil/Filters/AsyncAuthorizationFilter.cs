using BaoXia.Utils.Cache;
using CSharp.Sample.ImageUtil.Attributes;
using CSharp.Sample.ImageUtil.ConfigFiles;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;

namespace CSharp.Sample.ImageUtil.Filters
{
        public class AsyncAuthorizationFilter : IAsyncAuthorizationFilter
        {
                ////////////////////////////////////////////////
                // @静态常量
                ////////////////////////////////////////////////

                #region 静态常量

                public enum AuthorizationRequiredMode
                {
                        Required,
                        NotRequired
                }

                public class HttpContextItemKeys
                {
                        public const string CurrentAuthorizationInfo = "AsyncAuthorizationFilter.CurrentAuthorizationInfo";
                }

                public class AuthorizationInfo
                {
                        public int UserId { get; set; }

                        public DateTime LastVerifyTime { get; set; }

                        public DateTime ExpireTime
                        {
                                get
                                {
                                        var authorizationTokenVerifyIntervalSeconds = Config.Service.AuthorizationTokenVerifyIntervalSeconds;
                                        var expireTime
                                                = authorizationTokenVerifyIntervalSeconds > 0.0
                                                ? this.LastVerifyTime.AddSeconds(authorizationTokenVerifyIntervalSeconds)
                                                : DateTime.MaxValue;
                                        { }
                                        return expireTime;
                                }
                        }

                        public bool IsExpired
                        {
                                get
                                {
                                        if (this.ExpireTime < DateTime.Now)
                                        {
                                                return true;
                                        }
                                        return false;
                                }
                        }

                        public bool IsValid
                        {
                                get
                                {
                                        if (this.UserId != 0
                                                && this.IsExpired == false)
                                        {
                                                return true;
                                        }
                                        return false;
                                }
                        }

                        public AuthorizationInfo(
                                int userId,
                                DateTime verifyTime)
                        {
                                this.UserId = userId;

                                this.LastVerifyTime = verifyTime;
                        }
                }

                public class UnauthorizedResponse
                {
                        public System.Net.HttpStatusCode HttpStatusCode { get; set; }

                        public string? ContentType { get; set; }

                        public string? Content { get; set; }

                        public UnauthorizedResponse(
                                HttpStatusCode httpStatusCode,
                                string? contentType,
                                string? content)
                        {
                                this.HttpStatusCode = httpStatusCode;
                                this.ContentType = contentType;
                                this.Content = content;
                        }
                }


                #endregion

                ////////////////////////////////////////////////
                // @静态常量
                ////////////////////////////////////////////////

                #region 静态变量

                private readonly static ItemsCache<string, AuthorizationInfo, object> _authorizationInfoCache = new(
                                       (authorizationTokenInCookie, _) =>
                                       {
                                               // 测试代码，用户鉴权：
                                               var userIdLogined = 1;
                                               if (userIdLogined == 0)
                                               {
                                                       return null;
                                               }
                                               return new AuthorizationInfo(
                                                       userIdLogined,
                                                       DateTime.Now);
                                       },
                                       null,
                                       () => Config.Service.AuthorizationTokenVerifyIntervalSeconds);

                #endregion


                ////////////////////////////////////////////////
                // @自身属性
                ////////////////////////////////////////////////

                #region 自身属性

                public AuthorizationRequiredMode AuthorizationRequiredModeDefault { get; set; }

                public UnauthorizedResponse ResponseForUnauthorizedRequest { get; set; }

                #endregion



                ////////////////////////////////////////////////
                // @自身实现
                ////////////////////////////////////////////////

                #region 自身实现

                public AsyncAuthorizationFilter(
                        AuthorizationRequiredMode authorizationRequiredModeDefault,
                        UnauthorizedResponse responseForUnauthorizedRequest)
                {
                        this.AuthorizationRequiredModeDefault = authorizationRequiredModeDefault;

                        this.ResponseForUnauthorizedRequest = responseForUnauthorizedRequest;
                }

                #endregion

                ////////////////////////////////////////////////
                // @重载
                ////////////////////////////////////////////////

                #region 重载

                public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
                {
                        var httpContext = context.HttpContext;

                        // 模拟“异步”鉴权：
                        {
                                await Task.Delay(1);
                        }
                        // var request = httpContext.Request;
                        var authorizationToken = "auth 0123456789";

                        AuthorizationInfo? authorizationInfo = null;
                        if (authorizationToken?.Length > 0)
                        {
                                // !!!
                                authorizationInfo
                                        = _authorizationInfoCache.Get(authorizationToken, null);
                                // !!!
                        }

                        var isAuthorizationInfoValid = authorizationInfo?.IsValid == true;
                        var isRequestAuthorizationCheckSuccess = false;
                        if (isAuthorizationInfoValid == true)
                        {
                                // !!!
                                isRequestAuthorizationCheckSuccess = true;
                                // !!! 
                                context.HttpContext.Items.Add(
                                        HttpContextItemKeys.CurrentAuthorizationInfo,
                                        authorizationInfo);
                                // !!!
                        }
                        else
                        {
                                var controllerActionDescriptor
                                        = context.ActionDescriptor
                                        as
                                        Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor;
                                if (controllerActionDescriptor != null)
                                {

                                        var controllerTypeInfo = controllerActionDescriptor.ControllerTypeInfo;
                                        var controllerCustomAttributes = controllerTypeInfo.CustomAttributes;
                                        if (controllerCustomAttributes?.Any() == true)
                                        {
                                                switch (this.AuthorizationRequiredModeDefault)
                                                {
                                                        case AuthorizationRequiredMode.Required:
                                                                {
                                                                        if (controllerTypeInfo.GetCustomAttributes(
                                                                                typeof(AuthorizationNotRequiredAttribute),
                                                                                true).Length > 0)
                                                                        {
                                                                                // !!!
                                                                                isRequestAuthorizationCheckSuccess = true;
                                                                                // !!!
                                                                        }
                                                                }
                                                                break;
                                                        case AuthorizationRequiredMode.NotRequired:
                                                                {
                                                                        if (controllerTypeInfo.GetCustomAttributes(
                                                                                typeof(AuthorizationRequiredAttribute),
                                                                                true).Length > 0)
                                                                        {
                                                                                // !!!
                                                                                isRequestAuthorizationCheckSuccess = false;
                                                                                // !!!
                                                                        }
                                                                        else
                                                                        {
                                                                                // !!!
                                                                                isRequestAuthorizationCheckSuccess = true;
                                                                                // !!!
                                                                        }
                                                                }
                                                                break;
                                                }
                                        }
                                }
                        }

                        var response = httpContext.Response;
                        if (isRequestAuthorizationCheckSuccess == true)
                        {
                                return;
                        }


                        ////////////////////////////////////////////////
                        // 拦截未授权的请求：
                        ////////////////////////////////////////////////

                        var responseForUnauthorizedRequest = this.ResponseForUnauthorizedRequest;
                        context.Result = new ContentResult()
                        {
                                StatusCode = (int?)responseForUnauthorizedRequest.HttpStatusCode,
                                ContentType = responseForUnauthorizedRequest.ContentType,
                                Content = responseForUnauthorizedRequest.Content
                        };
                }

                #endregion
        }
}

