using System;
using Android.App;
using Android.Content;
using Android.Hardware;
using Android.OS;
using Android.Views;

namespace KassenDoubleTapScreen
{
    [Service(Label = "Kassen Sensor Wake Service", Exported = false)]
    public class SensorWakeService : Service, ISensorEventListener
    {
        private const string ChannelId = "kassen_sensor_service_channel";
        private const int NotificationId = 1002;

        private PowerManager? _powerManager;
        private PowerManager.WakeLock? _partialWakeLock;
        private SensorManager? _sensorManager;
        private Sensor? _accelerometer;
        private Vibrator? _vibrator;
        private ScreenStateReceiver? _screenReceiver;

        private long _lastTapTime = 0;
        private bool _isListening = false;

        public override void OnCreate()
        {
            base.OnCreate();

            _powerManager = (PowerManager?)GetSystemService(PowerService);
            _sensorManager = (SensorManager?)GetSystemService(SensorService);
            _accelerometer = _sensorManager?.GetDefaultSensor(SensorType.Accelerometer);
#pragma warning disable CA1422
            _vibrator = (Vibrator?)GetSystemService(VibratorService);
#pragma warning restore CA1422

            CreateNotificationChannel();
            var notification = BuildNotification();
            StartForeground(NotificationId, notification);

            // Daftarkan listener layar mati dan menyala
            _screenReceiver = new ScreenStateReceiver(this);
            var filter = new IntentFilter();
            filter.AddAction(Intent.ActionScreenOff);
            filter.AddAction(Intent.ActionScreenOn);
            RegisterReceiver(_screenReceiver, filter);

            // Jika saat service dinyalakan layar sedang mati, langsung dengarkan sensor
            if (_powerManager != null && !_powerManager.IsInteractive)
            {
                StartSensorListening();
            }
        }

        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            return StartCommandResult.Sticky;
        }

        public override void OnDestroy()
        {
            StopSensorListening();

            if (_screenReceiver != null)
            {
                try
                {
                    UnregisterReceiver(_screenReceiver);
                }
                catch { }
                _screenReceiver = null;
            }

            base.OnDestroy();
        }

        public override IBinder? OnBind(Intent? intent) => null;

        public void StartSensorListening()
        {
            if (_isListening) return;

            if (!AppSettings.IsSensorWakeEnabled(this)) return;

            try
            {
                if (_partialWakeLock == null && _powerManager != null)
                {
                    _partialWakeLock = _powerManager.NewWakeLock(WakeLockFlags.Partial, "KassenDoubleTap:SensorWakeLock");
                }
                _partialWakeLock?.Acquire();

                if (_sensorManager != null && _accelerometer != null)
                {
                    _sensorManager.RegisterListener(this, _accelerometer, SensorDelay.Game);
                    _isListening = true;
                }
            }
            catch (Exception)
            {
                // Tangani exception jika izin belum lengkap
            }
        }

        public void StopSensorListening()
        {
            if (!_isListening) return;

            try
            {
                if (_sensorManager != null)
                {
                    _sensorManager.UnregisterListener(this);
                }

                if (_partialWakeLock != null && _partialWakeLock.IsHeld)
                {
                    _partialWakeLock.Release();
                }
            }
            catch (Exception) { }

            _isListening = false;
            _lastTapTime = 0;
        }

        public void OnSensorChanged(SensorEvent? e)
        {
            if (e == null || e.Sensor?.Type != SensorType.Accelerometer || e.Values == null || e.Values.Count < 3)
                return;

            float x = e.Values[0];
            float y = e.Values[1];
            float z = e.Values[2];

            // Hitung magnitudo akselerasi total
            double magnitude = Math.Sqrt(x * x + y * y + z * z);
            double delta = Math.Abs(magnitude - 9.80665);

            // Ambil ambang batas berdasarkan sensitivitas yang dipilih
            int sensitivity = AppSettings.GetSensorSensitivity(this);
            double threshold = sensitivity switch
            {
                0 => 18.0, // Rendah: butuh ketukan agak kuat
                2 => 8.5,  // Tinggi: ketukan ringan terdeteksi
                _ => 12.5  // Sedang (default): seimbang & akurat
            };

            if (delta > threshold)
            {
                long now = SystemClock.ElapsedRealtime();
                long diff = now - _lastTapTime;

                // Jika jarak antara ketukan pertama dan kedua berada dalam rentang 140ms - 550ms
                if (diff >= 140 && diff <= 550)
                {
                    _lastTapTime = 0;
                    WakeUpScreen();
                }
                else if (diff > 550 || diff < 80)
                {
                    _lastTapTime = now;
                }
            }
        }

        public void OnAccuracyChanged(Sensor? sensor, SensorStatus accuracy) { }

        private void WakeUpScreen()
        {
            try
            {
                // Getaran konfirmasi
                if (AppSettings.IsVibrateEnabled(this) && _vibrator != null && _vibrator.HasVibrator)
                {
#pragma warning disable CA1422
                    if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                    {
                        _vibrator.Vibrate(VibrationEffect.CreateOneShot(60, VibrationEffect.DefaultAmplitude));
                    }
                    else
                    {
                        _vibrator.Vibrate(60);
                    }
#pragma warning restore CA1422
                }

                // Nyalakan layar dengan WakeLock
                if (_powerManager != null)
                {
#pragma warning disable CS0618
                    var wakeLock = _powerManager.NewWakeLock(
                        WakeLockFlags.ScreenBright | WakeLockFlags.AcquireCausesWakeup | WakeLockFlags.OnAfterRelease,
                        "KassenDoubleTap:WakeUpAction"
                    );
                    wakeLock.Acquire(3000);
#pragma warning restore CS0618
                }

                // Jalankan WakeActivity untuk membuka tampilan dan bypass lock
                var wakeIntent = new Intent(this, typeof(WakeActivity));
                wakeIntent.AddFlags(ActivityFlags.NewTask | ActivityFlags.ReorderToFront | ActivityFlags.SingleTop);
                StartActivity(wakeIntent);
            }
            catch (Exception)
            {
                // Abaikan jika terjadi kendala izin wake lock
            }
        }

        private void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(
                    ChannelId,
                    "Kassen Sensor Wake Service",
                    NotificationImportance.Low
                )
                {
                    Description = "Layanan pemantau sensor ketukan untuk menyalakan layar POS Kassen"
                };

                var notificationManager = (NotificationManager?)GetSystemService(NotificationService);
                notificationManager?.CreateNotificationChannel(channel);
            }
        }

        private Notification BuildNotification()
        {
            var pendingIntent = PendingIntent.GetActivity(
                this,
                0,
                new Intent(this, typeof(MainActivity)),
                PendingIntentFlags.Immutable
            );

            Notification.Builder builder;
#pragma warning disable CA1422
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                builder = new Notification.Builder(this, ChannelId);
            }
            else
            {
                builder = new Notification.Builder(this);
            }
#pragma warning restore CA1422

            builder.SetContentTitle("Kassen Double Tap Layar Aktif")
                   .SetContentText("Sensor ketukan siap membangunkan layar.")
                   .SetSmallIcon(Resource.Drawable.ic_lock_power)
                   .SetContentIntent(pendingIntent)
                   .SetOngoing(true);

            return builder.Build();
        }

        private class ScreenStateReceiver : BroadcastReceiver
        {
            private readonly SensorWakeService _service;

            public ScreenStateReceiver(SensorWakeService service)
            {
                _service = service;
            }

            public override void OnReceive(Context? context, Intent? intent)
            {
                if (intent?.Action == Intent.ActionScreenOff)
                {
                    _service.StartSensorListening();
                }
                else if (intent?.Action == Intent.ActionScreenOn)
                {
                    _service.StopSensorListening();
                }
            }
        }
    }
}
