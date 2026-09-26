using Android.Content;

namespace KassenDoubleTapScreen
{
    public static class AppSettings
    {
        private const string PrefsName = "kassen_double_tap_prefs";

        private const string KeyServiceEnabled = "key_service_enabled";
        private const string KeyOperationMode = "key_operation_mode"; // "standby" or "lock"
        private const string KeyFloatingEnabled = "key_floating_enabled";
        private const string KeyVibrateEnabled = "key_vibrate_enabled";
        private const string KeyFloatingAlpha = "key_floating_alpha"; // 10..100
        private const string KeyFloatingSize = "key_floating_size"; // 0: Small, 1: Normal, 2: Large
        private const string KeyFloatingX = "key_floating_x";
        private const string KeyFloatingY = "key_floating_y";
        private const string KeySensorWakeEnabled = "key_sensor_wake_enabled";
        private const string KeySensorSensitivity = "key_sensor_sensitivity"; // 0: Low, 1: Medium, 2: High

        private static ISharedPreferences? GetPrefs(Context context)
        {
            return context.GetSharedPreferences(PrefsName, FileCreationMode.Private);
        }

        public static bool IsServiceEnabled(Context context) =>
            GetPrefs(context)?.GetBoolean(KeyServiceEnabled, false) ?? false;

        public static void SetServiceEnabled(Context context, bool enabled) =>
            GetPrefs(context)?.Edit()?.PutBoolean(KeyServiceEnabled, enabled)?.Apply();

        public static string GetOperationMode(Context context) =>
            GetPrefs(context)?.GetString(KeyOperationMode, "standby") ?? "standby";

        public static void SetOperationMode(Context context, string mode) =>
            GetPrefs(context)?.Edit()?.PutString(KeyOperationMode, mode)?.Apply();

        public static bool IsFloatingEnabled(Context context) =>
            GetPrefs(context)?.GetBoolean(KeyFloatingEnabled, true) ?? true;

        public static void SetFloatingEnabled(Context context, bool enabled) =>
            GetPrefs(context)?.Edit()?.PutBoolean(KeyFloatingEnabled, enabled)?.Apply();

        public static bool IsVibrateEnabled(Context context) =>
            GetPrefs(context)?.GetBoolean(KeyVibrateEnabled, true) ?? true;

        public static void SetVibrateEnabled(Context context, bool enabled) =>
            GetPrefs(context)?.Edit()?.PutBoolean(KeyVibrateEnabled, enabled)?.Apply();

        public static int GetFloatingAlpha(Context context) =>
            GetPrefs(context)?.GetInt(KeyFloatingAlpha, 80) ?? 80;

        public static void SetFloatingAlpha(Context context, int alpha) =>
            GetPrefs(context)?.Edit()?.PutInt(KeyFloatingAlpha, alpha)?.Apply();

        public static int GetFloatingSize(Context context) =>
            GetPrefs(context)?.GetInt(KeyFloatingSize, 1) ?? 1;

        public static void SetFloatingSize(Context context, int size) =>
            GetPrefs(context)?.Edit()?.PutInt(KeyFloatingSize, size)?.Apply();

        public static int GetFloatingX(Context context, int defaultVal) =>
            GetPrefs(context)?.GetInt(KeyFloatingX, defaultVal) ?? defaultVal;

        public static void SetFloatingX(Context context, int x) =>
            GetPrefs(context)?.Edit()?.PutInt(KeyFloatingX, x)?.Apply();

        public static int GetFloatingY(Context context, int defaultVal) =>
            GetPrefs(context)?.GetInt(KeyFloatingY, defaultVal) ?? defaultVal;

        public static void SetFloatingY(Context context, int y) =>
            GetPrefs(context)?.Edit()?.PutInt(KeyFloatingY, y)?.Apply();

        public static bool IsSensorWakeEnabled(Context context) =>
            GetPrefs(context)?.GetBoolean(KeySensorWakeEnabled, true) ?? true;

        public static void SetSensorWakeEnabled(Context context, bool enabled) =>
            GetPrefs(context)?.Edit()?.PutBoolean(KeySensorWakeEnabled, enabled)?.Apply();

        public static int GetSensorSensitivity(Context context) =>
            GetPrefs(context)?.GetInt(KeySensorSensitivity, 1) ?? 1;

        public static void SetSensorSensitivity(Context context, int sensitivity) =>
            GetPrefs(context)?.Edit()?.PutInt(KeySensorSensitivity, sensitivity)?.Apply();
    }
}
