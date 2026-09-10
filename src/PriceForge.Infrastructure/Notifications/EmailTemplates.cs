namespace PriceForge.Infrastructure.Notifications;

public static class EmailTemplates
{
    public static string ConfirmEmailHtml(string confirmUrl)
        => Layout("Подтвердите адрес",
                  "Вы зарегистрировались в PriceForge. Подтвердите почту, чтобы оформлять заказы.",
                  "Подтвердить почту",
                  confirmUrl,
                  "Ссылка действует 24 часа. Если вы не регистрировались, просто проигнорируйте письмо.");

    public static string ConfirmEmailText(string confirmUrl)
        => $"""
            Подтвердите адрес

            Вы зарегистрировались в PriceForge. Подтвердите почту, чтобы оформлять заказы.

            {confirmUrl}

            Ссылка действует 24 часа. Если вы не регистрировались, просто проигнорируйте письмо.
            """;

    public static string ResetPasswordHtml(string resetUrl)
        => Layout("Сброс пароля",
                  "Мы получили запрос на смену пароля в PriceForge.",
                  "Задать новый пароль",
                  resetUrl,
                  "Ссылка действует 30 минут и сработает один раз. Если вы не запрашивали сброс, никаких действий не требуется.");

    public static string ResetPasswordText(string resetUrl)
        => $"""
            Сброс пароля

            Мы получили запрос на смену пароля в PriceForge.

            {resetUrl}

            Ссылка действует 30 минут и сработает один раз. Если вы не запрашивали сброс, никаких действий не требуется.
            """;

    private static string Layout(string heading, string intro, string buttonText, string url, string footer)
        => $"""
            <table width="100%" cellpadding="0" cellspacing="0" border="0" style="background:#f4f4f7;margin:0;padding:32px 0;">
              <tr>
                <td align="center">
                  <table width="600" cellpadding="0" cellspacing="0" border="0" style="background:#ffffff;border-radius:10px;padding:40px;font-family:-apple-system,Segoe UI,Roboto,Arial,sans-serif;color:#1a1a1a;">
                    <tr>
                      <td style="font-size:14px;font-weight:600;color:#2f6fed;padding-bottom:24px;letter-spacing:0.5px;">PRICEFORGE</td>
                    </tr>
                    <tr>
                      <td style="font-size:22px;font-weight:700;padding-bottom:12px;">{heading}</td>
                    </tr>
                    <tr>
                      <td style="font-size:15px;line-height:22px;color:#444444;padding-bottom:28px;">{intro}</td>
                    </tr>
                    <tr>
                      <td style="padding-bottom:28px;">
                        <a href="{url}" style="display:inline-block;background:#2f6fed;color:#ffffff;text-decoration:none;font-size:15px;font-weight:600;padding:14px 28px;border-radius:6px;">{buttonText}</a>
                      </td>
                    </tr>
                    <tr>
                      <td style="font-size:13px;line-height:20px;color:#777777;border-top:1px solid #ececf1;padding-top:20px;">
                        Если кнопка не открывается, скопируйте ссылку в браузер:<br>
                        <span style="color:#2f6fed;word-break:break-all;">{url}</span>
                      </td>
                    </tr>
                    <tr>
                      <td style="font-size:12px;line-height:18px;color:#999999;padding-top:16px;">{footer}</td>
                    </tr>
                  </table>
                </td>
              </tr>
            </table>
            """;
}
