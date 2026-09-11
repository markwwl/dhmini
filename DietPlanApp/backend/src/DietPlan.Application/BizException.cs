namespace DietPlan.Application;

/// <summary>业务异常（控制器统一转 400/404）</summary>
public class BizException : Exception
{
    /// <summary>HTTP 状态码（400 或 404）</summary>
    public int StatusCode { get; }

    /// <summary>构造业务异常</summary>
    public BizException(string message, int statusCode = 400) : base(message) => StatusCode = statusCode;
}
