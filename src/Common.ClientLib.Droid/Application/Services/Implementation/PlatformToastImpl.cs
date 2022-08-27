using Android.OS;
using Microsoft.Extensions.DependencyInjection;
using AndroidToast = Android.Widget.Toast;
using AndroidToastLength = Android.Widget.ToastLength;
using JException = Java.Lang.Exception;
#if NET6_0_OR_GREATER
using XEPlatform = Microsoft.Maui.ApplicationModel.Platform;
#else
using XEPlatform = Xamarin.Essentials.Platform;
#endif

namespace System.Application.Services.Implementation
{
    /// <summary>
    /// https://developer.android.google.cn/guide/topics/ui/notifiers/toasts
    /// </summary>
    internal sealed class PlatformToastImpl : ToastBaseImpl
    {
        public PlatformToastImpl(IToastIntercept intercept) : base(intercept)
        {
        }

        //static AndroidToast? toast;
        //static DateTime hideTime;

        //static int GetHideTimeByDuration(int duration, AndroidToastLength duration2)
        //{
        //    if (duration2 == AndroidToastLength.Long)
        //    {
        //        return (int)ToastLength.Long;
        //    }
        //    else if (duration2 == AndroidToastLength.Short)
        //    {
        //        return (int)ToastLength.Short;
        //    }
        //    return duration;
        //}

        protected override void PlatformShow(string text, int duration)
        {
            var context = XEPlatform.CurrentActivity?.ApplicationContext ?? XEPlatform.AppContext;
            var duration2 = (AndroidToastLength)duration;
            // https://blog.csdn.net/android157/article/details/80267737
            try
            {
                //if (toast == null)
                //{
                var toast = AndroidToast.MakeText(context, text, duration2);
                //if (toast == null) throw new NullReferenceException("toast markeText Fail");
                //}
                //else
                //{
                //    toast.Duration = duration2;
                //}
                SetTextAndShow(toast!, text);
            }
            catch (JException e)
            {
                Log.Error(TAG, e, "ShowDroidToast Error, text: {0}, IsMainThread: {1}", text, IsMainThread);
                // 解决在子线程中调用Toast的异常情况处理
                Looper.Prepare();
                var toast_ = AndroidToast.MakeText(context, text, duration2)
                    ?? throw new NullReferenceException("toast markeText Fail(2)");
                SetTextAndShow(toast_, text);
                Looper.Loop();
            }

            static void SetTextAndShow(AndroidToast t, string text)
            {
                // 某些定制ROM会更改内容文字，例如MIUI，重新设置可强行指定文本
                t.SetText(text);
                //var now = DateTime.Now;
                //if (hideTime == default || hideTime <= now) // 同一时间内多次调用 Show 将会导致无法显示
                //{
                //    hideTime = now.AddMilliseconds(GetHideTimeByDuration(duration, duration2));
                t.Show();
                //}
            }
        }

        protected override int ToDuration(ToastLength toastLength) => toastLength switch
        {
            ToastLength.Short => (int)AndroidToastLength.Short,
            ToastLength.Long => (int)AndroidToastLength.Long,
            _ => base.ToDuration(toastLength),
        };

        internal static IServiceCollection TryAddToast(IServiceCollection services)
            => TryAddToast<PlatformToastImpl>(services);
    }
}