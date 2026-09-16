using System.Net;

namespace CongTy.ApiClient;

public static class CanonicalErrorMessages
{
    public static string ToOfficeMessage(CanonicalApiException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Phiên đăng nhập không còn hiệu lực. Vui lòng đăng nhập lại.",
            HttpStatusCode.Forbidden => "Tài khoản chưa được cấp quyền thực hiện thao tác này.",
            HttpStatusCode.ServiceUnavailable => "Hệ thống Công Ty tạm thời chưa sẵn sàng. Vui lòng thử lại sau.",
            _ => string.IsNullOrWhiteSpace(exception.Message)
                ? "Không thể hoàn tất yêu cầu."
                : exception.Message
        };
    }

    public static string WithRequestId(string message, string? requestId) =>
        string.IsNullOrWhiteSpace(requestId)
            ? message
            : $"{message} Mã đối chiếu: {requestId}.";
}
