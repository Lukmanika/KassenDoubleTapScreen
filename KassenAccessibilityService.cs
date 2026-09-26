using Android.AccessibilityServices;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
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

        protected override void OnServiceConnected()
        {
            base.OnServiceConnected();
            Instance = this;
        }

        public override void OnAccessibilityEvent(AccessibilityEvent? e)
        {
            // Tidak perlu memproses event UI spesifik, service ini digunakan untuk GlobalAction Lock
        }

        public override void OnInterrupt()
        {
        }

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
