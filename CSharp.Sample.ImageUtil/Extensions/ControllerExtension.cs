using CSharp.Sample.ImageUtil.Filters;
using Microsoft.AspNetCore.Mvc;

namespace CSharp.Sample.ImageUtil.Extensions
{
        public static class ControllerExtension
        {
                public static AsyncAuthorizationFilter.AuthorizationInfo GetCurrentAuthorizationInfo(
                        this Controller controller)
                {
                        controller.HttpContext.Items.TryGetValue(
                                AsyncAuthorizationFilter.HttpContextItemKeys.CurrentAuthorizationInfo,
                                out var currentAuthorizationInfoObject);
                        AsyncAuthorizationFilter.AuthorizationInfo currentAuthorizationInfo
                                = (AsyncAuthorizationFilter.AuthorizationInfo)currentAuthorizationInfoObject!;
                        { }
                        return currentAuthorizationInfo;
                }
        }
}
