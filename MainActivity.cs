using System;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Provider;
using Android.Views;
using Android.Widget;
using AndroidUri = Android.Net.Uri;

namespace KassenDoubleTapScreen
{
    [Activity(
        Label = "@string/app_name",
        MainLauncher = true,
        Icon = "@mipmap/appicon",
        Theme = "@android:style/Theme.Material.Light.NoActionBar",
        WindowSoftInputMode = SoftInput.AdjustResize
    )]
    public class MainActivity : Activity
    {
        private Switch? _switchMain;
        private TextView? _tvStatusLabel;
        private TextView? _tvStatusSubtext;
        private View? _viewStatusDot;

        private RadioGroup? _radioGroupMode;
        private RadioButton? _radioModeStandby;
        private RadioButton? _radioModeLock;

        private Switch? _switchFloating;
        private Switch? _switchVibrate;
        private SeekBar? _seekBarAlpha;
        private TextView? _tvAlphaLabel;
        private SeekBar? _seekBarSize;
        private TextView? _tvSizeLabel;

        private Switch? _switchSensorWake;
        private SeekBar? _seekBarSensitivity;
        private TextView? _tvSensitivityLabel;

        private Button? _btnPermOverlay;
        private Button? _btnPermAccessibility;
        private Button? _btnPermBattery;

        private Button? _btnTestStandby;
        private Button? _btnTestScreenOff;
        private Button? _btnOpenGestures;

        private bool _isUpdatingUi = false;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            InitViews();
            LoadSettingsToUi();
            BindEventHandlers();
        }

        protected override void OnResume()
        {
            base.OnResume();
            RefreshPermissionsAndStatus();
        }

        private void InitViews()
        {
            _switchMain = FindViewById<Switch>(Resource.Id.switchMainService);
            _tvStatusLabel = FindViewById<TextView>(Resource.Id.tvStatusLabel);
            _tvStatusSubtext = FindViewById<TextView>(Resource.Id.tvStatusSubtext);
            _viewStatusDot = FindViewById<View>(Resource.Id.viewStatusDot);

            _radioGroupMode = FindViewById<RadioGroup>(Resource.Id.radioGroupMode);
            _radioModeStandby = FindViewById<RadioButton>(Resource.Id.radioModeStandby);
            _radioModeLock = FindViewById<RadioButton>(Resource.Id.radioModeLock);

            _switchFloating = FindViewById<Switch>(Resource.Id.switchFloating);
            _switchVibrate = FindViewById<Switch>(Resource.Id.switchVibrate);
            _seekBarAlpha = FindViewById<SeekBar>(Resource.Id.seekBarAlpha);
            _tvAlphaLabel = FindViewById<TextView>(Resource.Id.tvAlphaLabel);
            _seekBarSize = FindViewById<SeekBar>(Resource.Id.seekBarSize);
            _tvSizeLabel = FindViewById<TextView>(Resource.Id.tvSizeLabel);

            _switchSensorWake = FindViewById<Switch>(Resource.Id.switchSensorWake);
            _seekBarSensitivity = FindViewById<SeekBar>(Resource.Id.seekBarSensitivity);
            _tvSensitivityLabel = FindViewById<TextView>(Resource.Id.tvSensitivityLabel);

            _btnPermOverlay = FindViewById<Button>(Resource.Id.btnPermOverlay);
            _btnPermAccessibility = FindViewById<Button>(Resource.Id.btnPermAccessibility);
            _btnPermBattery = FindViewById<Button>(Resource.Id.btnPermBattery);

            _btnTestStandby = FindViewById<Button>(Resource.Id.btnTestStandby);
            _btnTestScreenOff = FindViewById<Button>(Resource.Id.btnTestScreenOff);
            _btnOpenGestures = FindViewById<Button>(Resource.Id.btnOpenGestures);
        }

        private void LoadSettingsToUi()
        {
            _isUpdatingUi = true;

            bool isEnabled = AppSettings.IsServiceEnabled(this);
            if (_switchMain != null) _switchMain.Checked = isEnabled;

            string mode = AppSettings.GetOperationMode(this);
            if (mode == "lock")
            {
                if (_radioModeLock != null) _radioModeLock.Checked = true;
            }
            else
            {
                if (_radioModeStandby != null) _radioModeStandby.Checked = true;
            }

            if (_switchFloating != null) _switchFloating.Checked = AppSettings.IsFloatingEnabled(this);
            if (_switchVibrate != null) _switchVibrate.Checked = AppSettings.IsVibrateEnabled(this);

            int alpha = AppSettings.GetFloatingAlpha(this);
            if (_seekBarAlpha != null) _seekBarAlpha.Progress = alpha;
            UpdateAlphaLabel(alpha);

            int size = AppSettings.GetFloatingSize(this);
            if (_seekBarSize != null) _seekBarSize.Progress = size;
            UpdateSizeLabel(size);

            if (_switchSensorWake != null) _switchSensorWake.Checked = AppSettings.IsSensorWakeEnabled(this);
            int sensitivity = AppSettings.GetSensorSensitivity(this);
            if (_seekBarSensitivity != null) _seekBarSensitivity.Progress = sensitivity;
            UpdateSensitivityLabel(sensitivity);

            _isUpdatingUi = false;
        }

        private void BindEventHandlers()
        {
            if (_switchMain != null)
            {
                _switchMain.CheckedChange += (s, e) =>
                {
                    if (_isUpdatingUi) return;

                    if (e.IsChecked)
                    {
                        if (Build.VERSION.SdkInt >= BuildVersionCodes.M && !Settings.CanDrawOverlays(this))
                        {
                            Toast.MakeText(this, "Silakan izinkan 'Tampilkan di atas aplikasi lain' terlebih dahulu.", ToastLength.Long)?.Show();
                            RequestOverlayPermission();
                            _isUpdatingUi = true;
                            _switchMain.Checked = false;
                            _isUpdatingUi = false;
                            return;
                        }

                        AppSettings.SetServiceEnabled(this, true);
                        StartAllServices();
                    }
                    else
                    {
                        AppSettings.SetServiceEnabled(this, false);
                        StopAllServices();
                    }

                    RefreshPermissionsAndStatus();
                };
            }

            if (_radioGroupMode != null)
            {
                _radioGroupMode.CheckedChange += (s, e) =>
                {
                    if (_isUpdatingUi) return;
                    if (e.CheckedId == Resource.Id.radioModeLock)
                    {
                        AppSettings.SetOperationMode(this, "lock");
                        if (!KassenAccessibilityService.IsRunning(this))
                        {
                            Toast.MakeText(this, "Perhatian: Mode Kunci Sistem memerlukan Izin Aksesibilitas.", ToastLength.Long)?.Show();
                        }
                    }
                    else
                    {
                        AppSettings.SetOperationMode(this, "standby");
                    }
                };
            }

            if (_switchFloating != null)
            {
                _switchFloating.CheckedChange += (s, e) =>
                {
                    if (_isUpdatingUi) return;
                    AppSettings.SetFloatingEnabled(this, e.IsChecked);
                    if (AppSettings.IsServiceEnabled(this))
                    {
                        if (e.IsChecked) StartOverlayService();
                        else StopOverlayService();
                    }
                };
            }

            if (_switchVibrate != null)
            {
                _switchVibrate.CheckedChange += (s, e) =>
                {
                    if (_isUpdatingUi) return;
                    AppSettings.SetVibrateEnabled(this, e.IsChecked);
                };
            }

            if (_seekBarAlpha != null)
            {
                _seekBarAlpha.ProgressChanged += (s, e) =>
                {
                    int val = Math.Max(15, e.Progress);
                    AppSettings.SetFloatingAlpha(this, val);
                    UpdateAlphaLabel(val);
                    FloatingOverlayService.Instance?.UpdateFloatingAppearance();
                };
            }

            if (_seekBarSize != null)
            {
                _seekBarSize.ProgressChanged += (s, e) =>
                {
                    AppSettings.SetFloatingSize(this, e.Progress);
                    UpdateSizeLabel(e.Progress);
                    FloatingOverlayService.Instance?.UpdateFloatingAppearance();
                };
            }

            if (_switchSensorWake != null)
            {
                _switchSensorWake.CheckedChange += (s, e) =>
                {
                    if (_isUpdatingUi) return;
                    AppSettings.SetSensorWakeEnabled(this, e.IsChecked);
                    if (AppSettings.IsServiceEnabled(this))
                    {
                        if (e.IsChecked) StartSensorWakeService();
                        else StopSensorWakeService();
                    }
                };
            }

            if (_seekBarSensitivity != null)
            {
                _seekBarSensitivity.ProgressChanged += (s, e) =>
                {
                    AppSettings.SetSensorSensitivity(this, e.Progress);
                    UpdateSensitivityLabel(e.Progress);
                };
            }

            // Tombol Izin
            _btnPermOverlay?.SetOnClickListener(new ViewClickListener(_ => RequestOverlayPermission()));
            _btnPermAccessibility?.SetOnClickListener(new ViewClickListener(_ => RequestAccessibilityPermission()));
            _btnPermBattery?.SetOnClickListener(new ViewClickListener(_ => RequestBatteryExemption()));

            // Tombol Uji Coba
            _btnTestStandby?.SetOnClickListener(new ViewClickListener(_ =>
            {
                var intent = new Intent(this, typeof(StandbyActivity));
                StartActivity(intent);
            }));

            _btnTestScreenOff?.SetOnClickListener(new ViewClickListener(_ =>
            {
                if (KassenAccessibilityService.IsRunning(this))
                {
                    KassenAccessibilityService.LockScreen();
                }
                else
                {
                    Toast.MakeText(this, "Izin Aksesibilitas belum aktif. Mengalihkan ke Mode Siaga Hitam...", ToastLength.Short)?.Show();
                    var intent = new Intent(this, typeof(StandbyActivity));
                    StartActivity(intent);
                }
            }));

            _btnOpenGestures?.SetOnClickListener(new ViewClickListener(_ =>
            {
                try
                {
                    var intent = new Intent(Settings.ActionDisplaySettings);
                    StartActivity(intent);
                }
                catch
                {
                    Toast.MakeText(this, "Gagal membuka menu pengaturan layar.", ToastLength.Short)?.Show();
                }
            }));
        }

        private void RefreshPermissionsAndStatus()
        {
            bool hasOverlay = Build.VERSION.SdkInt < BuildVersionCodes.M || Settings.CanDrawOverlays(this);
            bool hasAccessibility = KassenAccessibilityService.IsRunning(this);
            bool hasBatteryExemption = IsBatteryExempted();

            UpdatePermissionButton(_btnPermOverlay, hasOverlay, "Izin Overlay");
            UpdatePermissionButton(_btnPermAccessibility, hasAccessibility, "Izin Aksesibilitas");
            UpdatePermissionButton(_btnPermBattery, hasBatteryExemption, "Abaikan Baterai");

            bool isEnabled = AppSettings.IsServiceEnabled(this);

            if (isEnabled)
            {
                _tvStatusLabel?.SetText(Resource.String.status_active);
                _tvStatusLabel?.SetTextColor(Color.ParseColor("#10B981"));
                _tvStatusSubtext?.SetText("Layanan Double Tap aktif dan siap digunakan kasir.", TextView.BufferType.Normal);
                _viewStatusDot?.SetBackgroundResource(Resource.Drawable.bg_floating_btn);
            }
            else
            {
                _tvStatusLabel?.SetText(Resource.String.status_inactive);
                _tvStatusLabel?.SetTextColor(Color.ParseColor("#EF4444"));
                _tvStatusSubtext?.SetText("Nyalakan saklar untuk mengaktifkan fitur ketuk 2x di layar Kassen.", TextView.BufferType.Normal);
                _viewStatusDot?.SetBackgroundResource(Resource.Drawable.bg_card);
            }
        }

        private void UpdatePermissionButton(Button? btn, bool isGranted, string permName)
        {
            if (btn == null) return;

            if (isGranted)
            {
                btn.Text = GetString(Resource.String.btn_granted);
                btn.SetBackgroundResource(Resource.Drawable.bg_button_secondary);
                btn.SetTextColor(Color.ParseColor("#10B981"));
                btn.Enabled = false;
            }
            else
            {
                btn.Text = GetString(Resource.String.btn_grant);
                btn.SetBackgroundResource(Resource.Drawable.bg_button_primary);
                btn.SetTextColor(Color.White);
                btn.Enabled = true;
            }
        }

        private bool IsBatteryExempted()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                var pm = (PowerManager?)GetSystemService(PowerService);
                return pm?.IsIgnoringBatteryOptimizations(PackageName) ?? false;
            }
            return true;
        }

        private void RequestOverlayPermission()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M && !Settings.CanDrawOverlays(this))
            {
                try
                {
                    var intent = new Intent(
                        Settings.ActionManageOverlayPermission,
                        AndroidUri.Parse($"package:{PackageName}")
                    );
                    StartActivity(intent);
                }
                catch
                {
                    StartActivity(new Intent(Settings.ActionManageOverlayPermission));
                }
            }
        }

        private void RequestAccessibilityPermission()
        {
            try
            {
                var intent = new Intent(Settings.ActionAccessibilitySettings);
                StartActivity(intent);
                Toast.MakeText(this, "Cari dan aktifkan 'Double Tap Kassen' di daftar layanan.", ToastLength.Long)?.Show();
            }
            catch
            {
                Toast.MakeText(this, "Gagal membuka menu Aksesibilitas.", ToastLength.Short)?.Show();
            }
        }

        private void RequestBatteryExemption()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M && !IsBatteryExempted())
            {
                try
                {
                    var intent = new Intent(
                        Settings.ActionRequestIgnoreBatteryOptimizations,
                        AndroidUri.Parse($"package:{PackageName}")
                    );
                    StartActivity(intent);
                }
                catch
                {
                    Toast.MakeText(this, "Buka Pengaturan > Baterai untuk mengabaikan optimasi baterai.", ToastLength.Short)?.Show();
                }
            }
        }

        private void StartAllServices()
        {
            if (AppSettings.IsFloatingEnabled(this))
            {
                StartOverlayService();
            }
            if (AppSettings.IsSensorWakeEnabled(this))
            {
                StartSensorWakeService();
            }
        }

        private void StopAllServices()
        {
            StopOverlayService();
            StopSensorWakeService();
        }

        private void StartOverlayService()
        {
            try
            {
                var intent = new Intent(this, typeof(FloatingOverlayService));
                if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                {
                    StartForegroundService(intent);
                }
                else
                {
                    StartService(intent);
                }
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, $"Gagal memulai tombol melayang: {ex.Message}", ToastLength.Short)?.Show();
            }
        }

        private void StopOverlayService()
        {
            try
            {
                StopService(new Intent(this, typeof(FloatingOverlayService)));
            }
            catch { }
        }

        private void StartSensorWakeService()
        {
            try
            {
                var intent = new Intent(this, typeof(SensorWakeService));
                if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                {
                    StartForegroundService(intent);
                }
                else
                {
                    StartService(intent);
                }
            }
            catch (Exception ex)
            {
                Toast.MakeText(this, $"Gagal memulai sensor bangun: {ex.Message}", ToastLength.Short)?.Show();
            }
        }

        private void StopSensorWakeService()
        {
            try
            {
                StopService(new Intent(this, typeof(SensorWakeService)));
            }
            catch { }
        }

        private void UpdateAlphaLabel(int alpha)
        {
            if (_tvAlphaLabel != null)
                _tvAlphaLabel.Text = $"Transparansi Tombol Melayang: {alpha}%";
        }

        private void UpdateSizeLabel(int sizeIndex)
        {
            string name = sizeIndex switch
            {
                0 => "Kecil",
                2 => "Besar",
                _ => "Normal"
            };
            if (_tvSizeLabel != null)
                _tvSizeLabel.Text = $"Ukuran Tombol Melayang: {name}";
        }

        private void UpdateSensitivityLabel(int sensIndex)
        {
            string name = sensIndex switch
            {
                0 => "Rendah (Butuh ketukan mantap)",
                2 => "Tinggi (Ketukan ringan)",
                _ => "Sedang (Rekomendasi)"
            };
            if (_tvSensitivityLabel != null)
                _tvSensitivityLabel.Text = $"Sensitivitas Sensor: {name}";
        }

        private class ViewClickListener : Java.Lang.Object, View.IOnClickListener
        {
            private readonly Action<View?> _action;
            public ViewClickListener(Action<View?> action) => _action = action;
            public void OnClick(View? v) => _action(v);
        }
    }
}