using Microsoft.Extensions.DependencyInjection;

namespace CongTy.Desktop;

public partial class App
{
    internal T ResolveRequired<T>() where T:notnull =>
        (_services??throw new InvalidOperationException("Ứng dụng chưa khởi tạo xong.")).GetRequiredService<T>();
}
