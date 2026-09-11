using DietPlan.Application;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DietPlan.Api;

/// <summary>全局业务异常过滤器：BizException 按其状态码返回 JSON 错误</summary>
public class BizExceptionFilter : IExceptionFilter
{
    /// <summary>拦截 BizException，转为结构化错误响应</summary>
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is BizException biz)
        {
            context.Result = new JsonResult(new { error = biz.Message })
            {
                StatusCode = biz.StatusCode
            };
            context.ExceptionHandled = true;
        }
    }
}
