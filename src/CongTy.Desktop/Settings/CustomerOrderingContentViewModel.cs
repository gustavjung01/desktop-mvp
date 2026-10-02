using System.ComponentModel;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Settings;

public sealed class CustomerOrderingContentViewModel : INotifyPropertyChanged
{
    private static readonly HashSet<string> OwnerRoles =
        new(["system:security-owner", "system:implementation-owner"], StringComparer.Ordinal);

    private readonly ICustomerOrderingContentService _service;
    private readonly IAccessStateService _access;
    private readonly ICanonicalIdempotencyKeyProvider _idempotency;

    private bool _loaded;
    private bool _isBusy;
    private bool _messageIsError;
    private string _message = string.Empty;
    private string _sectionTitle = "Sự kiện";
    private string _programContent = string.Empty;
    private bool _visible;
    private string? _bannerUrl;
    private bool _imagePresent;
    private string? _updatedAt;
    private string? _saveFingerprint;
    private string? _saveKey;
    private string? _uploadFingerprint;
    private string? _uploadKey;

    public CustomerOrderingContentViewModel(
        ICustomerOrderingContentService service,
        IAccessStateService access,
        ICanonicalIdempotencyKeyProvider idempotency)
    {
        _service = service;
        _access = access;
        _idempotency = idempotency;

        _access.Changed += (_, _) => RunOnUiThread(() =>
        {
            _loaded = false;
            ClearPendingKeys();
            ResetContent();
            SetMessage(string.Empty, false);
            RaiseAccess();
            if (CanRead) _ = EnsureLoadedAsync();
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsAuthenticated => _access.Current.IsAuthenticated;
    public bool IsOwner => _access.Current.Roles.Any(OwnerRoles.Contains);
    public bool CanWrite => IsOwner || _access.HasPermission("core.organization.write");
    public bool CanRead => IsAuthenticated && (_access.HasPermission("core.config.read") || CanWrite);
    public bool IsBusy { get => _isBusy; private set => Set(ref _isBusy, value, nameof(IsBusy), nameof(IsNotBusy), nameof(CanSave), nameof(CanUpload)); }
    public bool IsNotBusy => !IsBusy;
    public bool CanSave => CanWrite && !IsBusy && !string.IsNullOrWhiteSpace(SectionTitle);
    public bool CanUpload => CanWrite && !IsBusy;
    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);
    public bool MessageIsError { get => _messageIsError; private set => Set(ref _messageIsError, value); }
    public string Message { get => _message; private set => Set(ref _message, value, nameof(Message), nameof(HasMessage)); }

    public string SectionTitle
    {
        get => _sectionTitle;
        set => Set(ref _sectionTitle, value ?? string.Empty, nameof(SectionTitle), nameof(CanSave));
    }

    public string ProgramContent
    {
        get => _programContent;
        set => Set(ref _programContent, value ?? string.Empty);
    }

    public bool Visible
    {
        get => _visible;
        set => Set(ref _visible, value);
    }

    public string? BannerUrl
    {
        get => _bannerUrl;
        private set => Set(ref _bannerUrl, value, nameof(BannerUrl), nameof(HasBanner));
    }

    public bool ImagePresent
    {
        get => _imagePresent;
        private set => Set(ref _imagePresent, value, nameof(ImagePresent), nameof(HasBanner), nameof(BannerActionText));
    }

    public bool HasBanner => ImagePresent && !string.IsNullOrWhiteSpace(BannerUrl);
    public string BannerActionText => ImagePresent ? "Thay ảnh" : "Chọn ảnh";
    public string UpdatedAtText => string.IsNullOrWhiteSpace(_updatedAt) ? "Chưa cập nhật" : _updatedAt!;

    public async Task EnsureLoadedAsync(CancellationToken cancellationToken = default)
    {
        if (_loaded || !CanRead || IsBusy) return;
        await RefreshAsync(cancellationToken).ConfigureAwait(true);
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (!CanRead || IsBusy) return;
        IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            var content = await _service.GetAsync(cancellationToken).ConfigureAwait(true);
            Apply(content);
            _loaded = true;
        }
        catch (Exception exception)
        {
            SetMessage(PublicError(exception, "Chưa thể tải Nội dung đặt hàng."), true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        if (!CanWrite || IsBusy) return;

        var title = SectionTitle.Trim();
        var program = ProgramContent.Trim();
        if (title.Length is < 1 or > 80)
        {
            SetMessage("Tiêu đề phải có từ 1 đến 80 ký tự.", true);
            return;
        }
        if (program.Length > 4000)
        {
            SetMessage("Nội dung chương trình không được vượt quá 4.000 ký tự.", true);
            return;
        }

        var request = new CustomerOrderingHomeContentUpdateRequest
        {
            SectionTitle = title,
            ProgramContent = program,
            Visible = Visible
        };
        var fingerprint = $"{title}{program}{Visible}";
        var key = ReuseSaveKey(fingerprint);

        IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            var saved = await _service.SaveAsync(request, key, cancellationToken).ConfigureAwait(true);
            Apply(saved);
            _saveFingerprint = null;
            _saveKey = null;
            SetMessage("Đã lưu nội dung Trang chủ khách hàng.", false);
        }
        catch (Exception exception)
        {
            SetMessage(PublicError(exception, "Không lưu được nội dung đặt hàng."), true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task UploadBannerAsync(byte[] webpBytes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(webpBytes);
        if (!CanWrite || IsBusy) return;
        if (webpBytes.Length is < 1 or > 5 * 1024 * 1024)
        {
            SetMessage("Ảnh WebP phải có dung lượng từ 1 byte đến 5 MB.", true);
            return;
        }

        var fingerprint = Convert.ToHexString(SHA256.HashData(webpBytes));
        var key = ReuseUploadKey(fingerprint);

        IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            var saved = await _service.UploadBannerAsync(webpBytes, key, cancellationToken).ConfigureAwait(true);
            Apply(saved);
            _uploadFingerprint = null;
            _uploadKey = null;
            SetMessage("Đã tải ảnh banner lên kho ảnh dùng chung.", false);
        }
        catch (Exception exception)
        {
            SetMessage(PublicError(exception, "Không tải được ảnh banner."), true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private string ReuseSaveKey(string fingerprint)
    {
        if (string.Equals(_saveFingerprint, fingerprint, StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(_saveKey)
            && _idempotency.IsValid(_saveKey))
            return _saveKey;

        _saveFingerprint = fingerprint;
        _saveKey = _idempotency.Create("customer-ordering-home-content-update");
        return _saveKey;
    }

    private string ReuseUploadKey(string fingerprint)
    {
        if (string.Equals(_uploadFingerprint, fingerprint, StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(_uploadKey)
            && _idempotency.IsValid(_uploadKey))
            return _uploadKey;

        _uploadFingerprint = fingerprint;
        _uploadKey = _idempotency.Create("customer-ordering-home-banner-upload");
        return _uploadKey;
    }

    private void Apply(CustomerOrderingHomeContentData content)
    {
        SectionTitle = content.SectionTitle;
        ProgramContent = content.ProgramContent;
        Visible = content.Visible;
        BannerUrl = content.BannerUrl;
        ImagePresent = content.ImagePresent;
        _updatedAt = content.UpdatedAt;
        OnPropertyChanged(nameof(UpdatedAtText));
    }

    private void ResetContent()
    {
        SectionTitle = "Sự kiện";
        ProgramContent = string.Empty;
        Visible = false;
        BannerUrl = null;
        ImagePresent = false;
        _updatedAt = null;
        OnPropertyChanged(nameof(UpdatedAtText));
    }

    private void ClearPendingKeys()
    {
        _saveFingerprint = null;
        _saveKey = null;
        _uploadFingerprint = null;
        _uploadKey = null;
    }

    private void RaiseAccess()
    {
        OnPropertyChanged(nameof(IsAuthenticated));
        OnPropertyChanged(nameof(IsOwner));
        OnPropertyChanged(nameof(CanWrite));
        OnPropertyChanged(nameof(CanRead));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanUpload));
    }

    private static string PublicError(Exception exception, string fallback)
    {
        if (exception is CanonicalApiException api)
            return CanonicalErrorMessages.WithRequestId(CanonicalErrorMessages.ToOfficeMessage(api), api.RequestId);
        return exception is HttpRequestException or TaskCanceledException ? fallback : exception.Message;
    }

    private void SetMessage(string value, bool error)
    {
        MessageIsError = error;
        Message = value;
    }

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null, params string[] also)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        foreach (var name in also) OnPropertyChanged(name);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private static void RunOnUiThread(Action action)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess()) action();
        else dispatcher.Invoke(action);
    }
}
