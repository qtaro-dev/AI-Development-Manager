namespace Adm.Application.Errors;

public sealed record AdmErrorDescriptor(
    string Code,
    int Status,
    string MessageKey,
    string UserMessage,
    bool InputRetained,
    bool Retryable,
    string NextAction);

public static class AdmErrorCatalog
{
    public static AdmErrorDescriptor From(AdmApplicationException exception) => exception.Kind switch
    {
        AdmErrorKind.Validation => new(
            "validation_failed",
            400,
            "errors.validation.invalid_input",
            "入力内容を確認してください。",
            true,
            false,
            "表示された項目を修正して、もう一度送信してください。"),
        AdmErrorKind.NotFound => new(
            "not_found",
            404,
            "errors.resource.not_found",
            "指定された情報が見つかりません。",
            false,
            false,
            "一覧を更新して、存在する項目を選び直してください。"),
        AdmErrorKind.Conflict => new(
            "conflict",
            409,
            "errors.resource.conflict",
            "他の変更と競合しました。",
            true,
            false,
            "最新版を読み込み、内容を確認してから保存してください。"),
        AdmErrorKind.Forbidden => new(
            "forbidden",
            403,
            "errors.access.forbidden",
            "この操作を実行する権限がありません。",
            false,
            false,
            "権限のある利用者へ確認してください。"),
        _ => Internal
    };

    public static AdmErrorDescriptor FromUnknown() => Internal;

    private static AdmErrorDescriptor Internal => new(
        "internal_error",
        500,
        "errors.system.unexpected",
        "処理を完了できませんでした。",
        true,
        true,
        "時間をおいて再試行してください。解決しない場合は追跡IDを添えて管理者へ連絡してください。");
}
