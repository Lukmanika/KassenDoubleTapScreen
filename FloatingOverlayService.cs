using System;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Provider;
using Android.Views;
using Android.Widget;

namespace KassenDoubleTapScreen
{
    [Service(Label = "Kassen Floating DoubleTap", Exported = false)]
    public class FloatingOverlayService : Service
    {
        private const string ChannelId = "kassen_overlay_service_channel";
        private const int NotificationId = 1001;

        private IWindowManager? _windowManager;
        private View? _floatingButtonView;
        private View? _topBarView;
        private WindowManagerLayoutParams? _buttonLayoutParams;
        private WindowManagerLayoutParams? _topBarLayoutParams;
        private Vibrator? _vibrator;

        // Variabel pelacak gestur tombol melayang
        private float _btnStartX, _btnStartY;
        private int _btnInitX, _btnInitY;
        private bool _btnIsMoving = false;
        private long _btnLastTapTime = 0;

        // Variabel pelacak gestur bilah atas (top bar)
        private long _topBarLastTapTime = 0;

        public static FloatingOverlayService? Instance { get; private set; }

        public override void OnCreate()
        {
            base.OnCreate();
            Instance = this;

            _windowManager = GetSystemService(WindowService) as IWindowManager;
#pragma warning disable CA1422
            _vibrator = (Vibrator?)GetSystemService(VibratorService);
#pragma warning restore CA1422

            CreateNotificationChannel();
            var notification = BuildNotification();
            StartForeground(NotificationId, notification);

            RefreshOverlays();
        }

        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            RefreshOverlays();
            return StartCommandResult.Sticky;
        }

        public override void OnDestroy()
        {
            RemoveFloatingButton();
            RemoveTopBar();
            if (Instance == this)
            {
                Instance = null;
            }
            base.OnDestroy();
        }

        public override IBinder? OnBind(Intent? intent) => null;

        public void RefreshOverlays()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M && !Settings.CanDrawOverlays(this))
            {
                return;
            }

            if (AppSettings.IsFloatingEnabled(this))
            {
                ShowFloatingButton();
            }
            else
            {
                RemoveFloatingButton();
            }

            if (AppSettings.IsTopBarEnabled(this))
            {
                ShowTopBar();
            }
            else
            {
                RemoveTopBar();
            }
        }

        private void ShowFloatingButton()
        {
            if (_windowManager == null) return;

            if (_floatingButtonView != null)
            {
                UpdateFloatingButtonAppearance();
                return;
            }

            var inflater = (LayoutInflater?)GetSystemService(LayoutInflaterService);
            _floatingButtonView = inflater?.Inflate(Resource.Layout.view_floating_button, null);
            if (_floatingButtonView == null) return;

            var displayMetrics = Resources?.DisplayMetrics;
            int screenWidth = displayMetrics?.WidthPixels ?? 720;
            int screenHeight = displayMetrics?.HeightPixels ?? 1280;

            int sizePx = DpToPx(GetButtonSizeDp());
            int savedX = AppSettings.GetFloatingX(this, screenWidth - sizePx - DpToPx(16));
            int savedY = AppSettings.GetFloatingY(this, screenHeight / 3);

            var layoutType = Build.VERSION.SdkInt >= BuildVersionCodes.O
                ? WindowManagerTypes.ApplicationOverlay
                : WindowManagerTypes.Phone;

            _buttonLayoutParams = new WindowManagerLayoutParams(
                sizePx,
                sizePx,
                layoutType,
                WindowManagerFlags.NotFocusable | WindowManagerFlags.LayoutNoLimits,
                Format.Translucent
            )
            {
                Gravity = GravityFlags.Top | GravityFlags.Left,
                X = Math.Max(0, Math.Min(savedX, screenWidth - sizePx)),
                Y = Math.Max(0, Math.Min(savedY, screenHeight - sizePx))
            };

            // Pasang Touch Listener dengan deteksi tap & drag mandiri yang akurat
            _floatingButtonView.SetOnTouchListener(new ButtonTouchListener(this));

            try
            {
                _windowManager.AddView(_floatingButtonView, _buttonLayoutParams);
                UpdateFloatingButtonAppearance();
            }
            catch (Exception)
            {
                _floatingButtonView = null;
            }
        }

        private void ShowTopBar()
        {
            if (_windowManager == null) return;
            if (_topBarView != null) return;

            // Buat view transparan di area status bar pojok atas (zona ketuk ganda di atas layar)
            _topBarView = new View(this);
            _topBarView.SetBackgroundColor(Color.Transparent);

            var displayMetrics = Resources?.DisplayMetrics;
            int screenWidth = displayMetrics?.WidthPixels ?? 720;

            var layoutType = Build.VERSION.SdkInt >= BuildVersionCodes.O
                ? WindowManagerTypes.ApplicationOverlay
                : WindowManagerTypes.Phone;

            // Lebar: seluruh area atas layar atau 200dp pojok kanan, tinggi 42dp
            _topBarLayoutParams = new WindowManagerLayoutParams(
                WindowManagerLayoutParams.MatchParent,
                DpToPx(42),
                layoutType,
                WindowManagerFlags.NotFocusable | WindowManagerFlags.LayoutInScreen,
                Format.Translucent
            )
            {
                Gravity = GravityFlags.Top | GravityFlags.CenterHorizontal,
                X = 0,
                Y = 0
            };

            _topBarView.SetOnTouchListener(new TopBarTouchListener(this));

            try
            {
                _windowManager.AddView(_topBarView, _topBarLayoutParams);
            }
            catch (Exception)
            {
                _topBarView = null;
            }
        }

        public void UpdateFloatingButtonAppearance()
        {
            if (_floatingButtonView == null || _buttonLayoutParams == null) return;

            int alphaPercent = AppSettings.GetFloatingAlpha(this);
            float alpha = Math.Max(0.20f, alphaPercent / 100.0f);
            _floatingButtonView.Alpha = alpha;

            int sizePx = DpToPx(GetButtonSizeDp());
            var icon = _floatingButtonView.FindViewById<ImageView>(Resource.Id.imgFloatingIcon);
            if (icon != null)
            {
                var p = icon.LayoutParameters;
                if (p != null)
                {
                    p.Width = sizePx;
                    p.Height = sizePx;
                    icon.LayoutParameters = p;
                }
            }

            _buttonLayoutParams.Width = sizePx;
            _buttonLayoutParams.Height = sizePx;

            try
            {
                _windowManager?.UpdateViewLayout(_floatingButtonView, _buttonLayoutParams);
            }
            catch { }
        }

        private void RemoveFloatingButton()
        {
            if (_floatingButtonView != null && _windowManager != null)
            {
                try
                {
                    _windowManager.RemoveView(_floatingButtonView);
                }
                catch { }
                _floatingButtonView = null;
            }
        }

        private void RemoveTopBar()
        {
            if (_topBarView != null && _windowManager != null)
            {
                try
                {
                    _windowManager.RemoveView(_topBarView);
                }
                catch { }
                _topBarView = null;
            }
        }

        private int GetButtonSizeDp()
        {
            int sizeIndex = AppSettings.GetFloatingSize(this);
            return sizeIndex switch
            {
                0 => 44, // Kecil
                2 => 66, // Besar
                _ => 54  // Normal
            };
        }

        private int DpToPx(int dp)
        {
            float density = Resources?.DisplayMetrics?.Density ?? 1.0f;
            return (int)(dp * density + 0.5f);
        }

        public void TriggerScreenOffAction()
        {
            VibrateFeedback();

            string mode = AppSettings.GetOperationMode(this);

            if (mode == "standby")
            {
                // Mode Siaga Layar Hitam (Rekomendasi Utama Kassen POS)
                var standbyIntent = new Intent(this, typeof(StandbyActivity));
                standbyIntent.AddFlags(ActivityFlags.NewTask | ActivityFlags.SingleTop);
                StartActivity(standbyIntent);
            }
            else
            {
                // Mode Kunci Sistem Penuh (via Aksesibilitas)
                bool locked = KassenAccessibilityService.LockScreen();
                if (!locked)
                {
                    // Fallback otomatis ke Mode Siaga jika aksesibilitas belum aktif
                    var standbyIntent = new Intent(this, typeof(StandbyActivity));
                    standbyIntent.AddFlags(ActivityFlags.NewTask | ActivityFlags.SingleTop);
                    StartActivity(standbyIntent);
                }
            }
        }

        private void VibrateFeedback()
        {
            try
            {
                if (AppSettings.IsVibrateEnabled(this) && _vibrator != null && _vibrator.HasVibrator)
                {
#pragma warning disable CA1422
                    if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                    {
                        _vibrator.Vibrate(VibrationEffect.CreateOneShot(55, VibrationEffect.DefaultAmplitude));
                    }
                    else
                    {
                        _vibrator.Vibrate(55);
                    }
#pragma warning restore CA1422
                }
            }
            catch { }
        }

        // Listener Sentuh Tombol Melayang
        private class ButtonTouchListener : Java.Lang.Object, View.IOnTouchListener
        {
            private readonly FloatingOverlayService _svc;

            public ButtonTouchListener(FloatingOverlayService svc) => _svc = svc;

            public bool OnTouch(View? v, MotionEvent? e)
            {
                if (e == null || _svc._buttonLayoutParams == null || _svc._windowManager == null)
                    return false;

                switch (e.Action)
                {
                    case MotionEventActions.Down:
                        _svc._btnStartX = e.RawX;
                        _svc._btnStartY = e.RawY;
                        _svc._btnInitX = _svc._buttonLayoutParams.X;
                        _svc._btnInitY = _svc._buttonLayoutParams.Y;
                        _svc._btnIsMoving = false;
                        return true;

                    case MotionEventActions.Move:
                        float dx = e.RawX - _svc._btnStartX;
                        float dy = e.RawY - _svc._btnStartY;

                        if (!_svc._btnIsMoving && (Math.Abs(dx) > 25 || Math.Abs(dy) > 25))
                        {
                            _svc._btnIsMoving = true;
                        }

                        if (_svc._btnIsMoving)
                        {
                            _svc._buttonLayoutParams.X = _svc._btnInitX + (int)dx;
                            _svc._buttonLayoutParams.Y = _svc._btnInitY + (int)dy;
                            try
                            {
                                _svc._windowManager.UpdateViewLayout(_svc._floatingButtonView, _svc._buttonLayoutParams);
                            }
                            catch { }
                        }
                        return true;

                    case MotionEventActions.Up:
                        if (!_svc._btnIsMoving)
                        {
                            // Ini adalah sentuhan/tap!
                            long now = SystemClock.ElapsedRealtime();
                            long diff = now - _svc._btnLastTapTime;

                            string tapMode = AppSettings.GetTapTriggerMode(_svc);

                            if (tapMode == "single")
                            {
                                // 1x Sentuh langsung matikan / siaga
                                _svc.TriggerScreenOffAction();
                            }
                            else
                            {
                                // 2x Sentuh (rentang waktu santai 80ms s/d 550ms)
                                if (diff >= 80 && diff <= 550)
                                {
                                    _svc._btnLastTapTime = 0;
                                    _svc.TriggerScreenOffAction();
                                }
                                else
                                {
                                    _svc._btnLastTapTime = now;
                                }
                            }
                        }
                        else
                        {
                            // Selesai geser, simpan posisi
                            AppSettings.SetFloatingX(_svc, _svc._buttonLayoutParams.X);
                            AppSettings.SetFloatingY(_svc, _svc._buttonLayoutParams.Y);
                        }
                        return true;
                }

                return false;
            }
        }

        // Listener Sentuh Bilah Status Bar Atas
        private class TopBarTouchListener : Java.Lang.Object, View.IOnTouchListener
        {
            private readonly FloatingOverlayService _svc;

            public TopBarTouchListener(FloatingOverlayService svc) => _svc = svc;

            public bool OnTouch(View? v, MotionEvent? e)
            {
                if (e == null) return false;

                if (e.Action == MotionEventActions.Down)
                {
                    long now = SystemClock.ElapsedRealtime();
                    long diff = now - _svc._topBarLastTapTime;

                    if (diff >= 80 && diff <= 550)
                    {
                        _svc._topBarLastTapTime = 0;
                        _svc.TriggerScreenOffAction();
                        return true;
                    }
                    else
                    {
                        _svc._topBarLastTapTime = now;
                        return true;
                    }
                }

                return false;
            }
        }

        private void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(
                    ChannelId,
                    "Kassen Tombol Melayang",
                    NotificationImportance.Low
                )
                {
                    Description = "Layanan aktif untuk mematikan layar POS Kassen"
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

            builder.SetContentTitle("Kassen Double Tap Layar")
                   .SetContentText("Ketuk tombol atau bilah atas layar untuk mematikan.")
                   .SetSmallIcon(Resource.Drawable.ic_lock_power)
                   .SetContentIntent(pendingIntent)
                   .SetOngoing(true);

            return builder.Build();
        }
    }
}
