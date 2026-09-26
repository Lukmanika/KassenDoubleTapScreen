using System;
using Android.AccessibilityServices;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Views;
using Android.Views.Accessibility;

namespace KassenDoubleTapScreen
{
    [Service(
        Label = "@string/app_name",
        Permission = Android.Manifest.Permission.BindAccessibilityService,
        Exported = true
    )]
    [IntentFilter(new[] { "android.accessibilityservice.AccessibilityService" })]
    [MetaData("android.accessibilityservice", Resource = "@xml/accessibility_service_config")]
    public class KassenAccessibilityService : AccessibilityService
    {
        public static KassenAccessibilityService? Instance { get; private set; }

        private long _lastBackPressTime = 0;
        private long _lastHomePressTime = 0;
        private long _lastBackClickTime = 0;
        private long _lastHomeClickTime = 0;

        protected override void OnServiceConnected()
        {
            base.OnServiceConnected();
            Instance = this;
        }

        public override void OnAccessibilityEvent(AccessibilityEvent? e)
        {
            if (e == null) return;

            // Deteksi klik pada tombol navigasi SystemUI (on-screen navigation bar)
            if (e.EventType == EventTypes.ViewClicked && e.PackageName == "com.android.systemui")
            {
                string desc = (e.ContentDescription?.ToString() ?? string.Empty).ToLowerInvariant();
                long now = SystemClock.ElapsedRealtime();

                // 1. Double tap tombol Home SystemUI
                if (desc.Contains("home") || desc.Contains("beranda"))
                {
                    long diff = now - _lastHomeClickTime;
                    if (diff >= 80 && diff <= 550)
                    {
                        _lastHomeClickTime = 0;
                        TriggerScreenOff();
                    }
                    else
                    {
                        _lastHomeClickTime = now;
                    }
                }
                // 2. Double tap tombol Back SystemUI
                else if (desc.Contains("back") || desc.Contains("kembali"))
                {
                    long diff = now - _lastBackClickTime;
                    if (diff >= 80 && diff <= 550)
                    {
                        _lastBackClickTime = 0;
                        TriggerScreenOff();
                    }
                    else
                    {
                        _lastBackClickTime = now;
                    }
                }
            }
        }

        protected override bool OnKeyEvent(KeyEvent? e)
        {
            if (e == null || e.Action != KeyEventActions.Down)
                return base.OnKeyEvent(e);

            long now = SystemClock.ElapsedRealtime();

            // 1. Double tap tombol BACK (Keycode.Back)
            if (e.KeyCode == Keycode.Back)
            {
                long diff = now - _lastBackPressTime;
                if (diff >= 80 && diff <= 550)
                {
                    _lastBackPressTime = 0;
                    TriggerScreenOff();
                    return true; // Tangkap event agar tidak keluar dari aplikasi saat double tap
                }
                _lastBackPressTime = now;
                return false;
            }

            // 2. Double tap tombol HOME (Keycode.Home)
            if (e.KeyCode == Keycode.Home)
            {
                long diff = now - _lastHomePressTime;
                if (diff >= 80 && diff <= 550)
                {
                    _lastHomePressTime = 0;
                    TriggerScreenOff();
                    return true;
                }
                _lastHomePressTime = now;
                return false;
            }

            return base.OnKeyEvent(e);
        }

        public void TriggerScreenOff()
        {
            VibrateBriefly();

            string mode = AppSettings.GetOperationMode(this);

            if (mode == "standby")
            {
                var intent = new Intent(this, typeof(StandbyActivity));
                intent.AddFlags(ActivityFlags.NewTask | ActivityFlags.SingleTop);
                StartActivity(intent);
            }
            else
            {
                bool locked = LockScreen();
                if (!locked)
                {
                    var intent = new Intent(this, typeof(StandbyActivity));
                    intent.AddFlags(ActivityFlags.NewTask | ActivityFlags.SingleTop);
                    StartActivity(intent);
                }
            }
        }

        private void VibrateBriefly()
        {
            try
            {
                if (AppSettings.IsVibrateEnabled(this))
                {
#pragma warning disable CA1422
                    var vibrator = (Vibrator?)GetSystemService(VibratorService);
                    if (vibrator != null && vibrator.HasVibrator)
                    {
                        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                        {
                            vibrator.Vibrate(VibrationEffect.CreateOneShot(50, VibrationEffect.DefaultAmplitude));
                        }
                        else
                        {
                            vibrator.Vibrate(50);
                        }
                    }
#pragma warning restore CA1422
                }
            }
            catch { }
        }

        public override void OnInterrupt() { }

        public override void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            base.OnDestroy();
        }

        public static bool LockScreen()
        {
            if (Instance == null)
            {
                return false;
            }

#pragma warning disable CA1416
            if (Build.VERSION.SdkInt >= BuildVersionCodes.P)
            {
                return Instance.PerformGlobalAction(GlobalAction.LockScreen);
            }
#pragma warning restore CA1416

            return false;
        }

        public static bool IsRunning(Context context)
        {
            if (Instance != null)
            {
                return true;
            }

            try
            {
                string pkg = context.PackageName ?? string.Empty;
                if (string.IsNullOrEmpty(pkg)) return false;

                string enabledServices = Settings.Secure.GetString(
                    context.ContentResolver,
                    Settings.Secure.EnabledAccessibilityServices
                ) ?? string.Empty;

                return enabledServices.Contains(pkg);
            }
            catch
            {
                return false;
            }
        }
    }
}
