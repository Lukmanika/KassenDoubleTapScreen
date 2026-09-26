using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;

namespace KassenDoubleTapScreen
{
    [Activity(
        Label = "Wake",
        Theme = "@android:style/Theme.Translucent.NoTitleBar",
        ExcludeFromRecents = true,
        ShowWhenLocked = true,
        TurnScreenOn = true
    )]
    public class WakeActivity : Activity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.OMr1)
            {
                SetShowWhenLocked(true);
                SetTurnScreenOn(true);
                var keyguardManager = (KeyguardManager?)GetSystemService(KeyguardService);
                keyguardManager?.RequestDismissKeyguard(this, null);
            }
            else
            {
#pragma warning disable CS0618
                Window?.AddFlags(
                    WindowManagerFlags.ShowWhenLocked |
                    WindowManagerFlags.TurnScreenOn |
                    WindowManagerFlags.DismissKeyguard |
                    WindowManagerFlags.KeepScreenOn
                );
#pragma warning restore CS0618
            }

            // Segera tutup activity ini agar layar menyala langsung kembali ke aplikasi POS sebelumnya
            Finish();
            OverridePendingTransition(0, 0);
        }
    }
}
