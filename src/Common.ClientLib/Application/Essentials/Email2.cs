using System.Application.Services;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Xamarin.Essentials;

// ReSharper disable once CheckNamespace
namespace System.Application
{
    /// <summary>
    /// 电子邮件，参考 Xamarin.Essentials.Email。
    /// <para><see cref="https://docs.microsoft.com/zh-cn/xamarin/essentials/email"/></para>
    /// <para><see cref="https://github.com/xamarin/Essentials/blob/main/Xamarin.Essentials/Email/Email.shared.cs"/></para>
    /// </summary>
    public static class Email2
    {
        /// <summary>
        /// 打开默认电子邮件客户端以允许用户发送邮件。
        /// </summary>
        /// <returns></returns>
        public static Task ComposeAsync()
            => ComposeAsync(null);

        /// <summary>
        /// 打开默认的电子邮件客户端，允许用户发送带有所提供主题、正文和收件人的邮件。
        /// </summary>
        /// <param name="subject">电子邮件主题。</param>
        /// <param name="body">电子邮件正文。</param>
        /// <param name="to">电子邮件收件人。</param>
        /// <returns></returns>
        public static Task ComposeAsync(string subject, string body, params string[] to)
            => ComposeAsync(new EmailMessage(subject, body, to));

        /// <summary>
        /// 打开默认电子邮件客户端以允许用户发送邮件。
        /// </summary>
        /// <param name="message">电子邮件。</param>
        /// <returns></returns>
        public static async Task ComposeAsync(EmailMessage? message)
        {
            if (Essentials.IsSupported)
            {
                await Email.ComposeAsync(message);
            }
            else
            {
                var s = IEmailPlatformService.Instance;
                if (s != null)
                {
                    try
                    {
                        await s.PlatformComposeAsync(message);
                    }
                    catch
                    {
                    }
                }
                await PlatformComposeAsync(message);
            }
        }

        static Task PlatformComposeAsync(EmailMessage? message)
        {
            var uri = GetMailToUri(message);
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = uri,
                    UseShellExecute = true,
                });
            }
            catch
            {
            }
            return Task.CompletedTask;
        }

        public static string GetMailToUri(EmailMessage? message)
        {
            if (message != null && message.BodyFormat != EmailBodyFormat.PlainText)
                throw new FeatureNotSupportedException("Only EmailBodyFormat.PlainText is supported if no email account is set up.");

            var parts = new List<string>();
            if (!string.IsNullOrEmpty(message?.Body))
                parts.Add("body=" + Uri.EscapeDataString(message.Body));
            if (!string.IsNullOrEmpty(message?.Subject))
                parts.Add("subject=" + Uri.EscapeDataString(message.Subject));
            if (message?.Cc?.Count > 0)
                parts.Add("cc=" + Uri.EscapeDataString(string.Join(",", message.Cc)));
            if (message?.Bcc?.Count > 0)
                parts.Add("bcc=" + Uri.EscapeDataString(string.Join(",", message.Bcc)));

            var uri = "mailto:";

            if (message?.To?.Count > 0)
                uri += Uri.EscapeDataString(string.Join(",", message.To));

            if (parts.Count > 0)
                uri += "?" + string.Join("&", parts);

            return uri;
        }
    }
}