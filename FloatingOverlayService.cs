using System;
using Android.AccessibilityServices;
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
        private View? _navHomeView;
        private View? _navBackLeftView;
        private View? _navBackRightView;
        private View? _floatingButtonView;
        private View? _topBarView;

        private Vibrator? _vibrator;

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
            RemoveAllOverlays();
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

            // 1. Selalu pasang zona deteksi Tombol Home & Back di bilah navigasi bawah
            SetupNavBarOverlays();

            // 2. Pasang zona bilah atas jika diaktifkan
            if (AppSettings.IsTopBarEnabled(this))
            {
                ShowTopBar();
            }
            else
            {
                RemoveTopBar();
            }

            // 3. Pasang tombol melayang jika diaktifkan
            if (AppSettings.IsFloatingEnabled(this))
            {
                ShowFloatingButton();
            }
            else
            {
                RemoveFloatingButton();
            }
        }

        private void SetupNavBarOverlays()
        {
            if (_windowManager == null) return;

            var layoutType = Build.VERSION.SdkInt >= BuildVersionCodes.O
                ? WindowManagerTypes.ApplicationOverlay
                : WindowManagerTypes.Phone;

            // A. Zona Tombol HOME (Tengah Bawah)
            if (_navHomeView == null)
            {
                _navHomeView = new View(this);
                _navHomeView.SetBackgroundColor(Color.Transparent);

                var homeParams = new WindowManagerLayoutParams(
                    DpToPx(130),
                    DpToPx(52),
                    layoutType,
                    WindowManagerFlags.NotFocusable | WindowManagerFlags.LayoutInScreen,
                    Format.Translucent
                )
                {
                    Gravity = GravityFlags.Bottom | GravityFlags.CenterHorizontal,
                    X = 0,
                    Y = 0
                };

                _navHomeView.SetOnTouchListener(new NavTouchListener(this, GlobalAction.Home));

                try
                {
                    _windowManager.AddView(_navHomeView, homeParams);
                }
                catch { _navHomeView = null; }
            }

            // B. Zona Tombol BACK (Kiri Bawah - Standar AOSP)
            if (_navBackLeftView == null)
            {
                _navBackLeftView = new View(this);
                _navBackLeftView.SetBackgroundColor(Color.Transparent);

                var backLeftParams = new WindowManagerLayoutParams(
                    DpToPx(110),
                    DpToPx(52),
                    layoutType,
                    WindowManagerFlags.NotFocusable | WindowManagerFlags.LayoutInScreen,
                    Format.Translucent
                )
                {
                    Gravity = GravityFlags.Bottom | GravityFlags.Left,
                    X = 0,
                    Y = 0
                };

                _navBackLeftView.SetOnTouchListener(new NavTouchListener(this, GlobalAction.Back));

                try
                {
                    _windowManager.AddView(_navBackLeftView, backLeftParams);
                }
                catch { _navBackLeftView = null; }
            }

            // C. Zona Tombol BACK (Kanan Bawah - Alternatif ROM POS)
            if (_navBackRightView == null)
            {
                _navBackRightView = new View(this);
                _navBackRightView.SetBackgroundColor(Color.Transparent);

                var backRightParams = new WindowManagerLayoutParams(
                    DpToPx(110),
                    DpToPx(52),
                    layoutType,
                    WindowManagerFlags.NotFocusable | WindowManagerFlags.LayoutInScreen,
                    Format.Translucent
                )
                {
                    Gravity = GravityFlags.Bottom | GravityFlags.Right,
                    X = 0,
                    Y = 0
                };

                _navBackRightView.SetOnTouchListener(new NavTouchListener(this, GlobalAction.Back));

                try
                {
                    _windowManager.AddView(_navBackRightView, backRightParams);
                }
                catch { _navBackRightView = null; }
            }
        }

        private void ShowTopBar()
        {
            if (_windowManager == null || _topBarView != null) return;

            _topBarView = new View(this);
            _topBarView.SetBackgroundColor(Color.Transparent);

            var layoutType = Build.VERSION.SdkInt >= BuildVersionCodes.O
                ? WindowManagerTypes.ApplicationOverlay
                : WindowManagerTypes.Phone;

            var topParams = new WindowManagerLayoutParams(
                WindowManagerLayoutParams.MatchParent,
                DpToPx(40),
                layoutType,
                WindowManagerFlags.NotFocusable | WindowManagerFlags.LayoutInScreen,
                Format.Translucent
            )
            {
                Gravity = GravityFlags.Top | GravityFlags.CenterHorizontal,
                X = 0,
                Y = 0
            };

            _topBarView.SetOnTouchListener(new SimpleDoubleTapListener(this));

            try
            {
                _windowManager.AddView(_topBarView, topParams);
            }
            catch { _topBarView = null; }
        }

        private void ShowFloatingButton()
        {
            if (_windowManager == null || _floatingButtonView != null) return;

            var inflater = (LayoutInflater?)GetSystemService(LayoutInflaterService);
            _floatingButtonView = inflater?.Inflate(Resource.Layout.view_floating_button, null);
            if (_floatingButtonView == null) return;

            var displayMetrics = Resources?.DisplayMetrics;
            int screenWidth = displayMetrics?.WidthPixels ?? 720;
            int screenHeight = displayMetrics?.HeightPixels ?? 1280;

            int sizePx = DpToPx(52);
            int savedX = AppSettings.GetFloatingX(this, screenWidth - sizePx - DpToPx(16));
            int savedY = AppSettings.GetFloatingY(this, screenHeight / 3);

            var layoutType = Build.VERSION.SdkInt >= BuildVersionCodes.O
                ? WindowManagerTypes.ApplicationOverlay
                : WindowManagerTypes.Phone;

            var buttonParams = new WindowManagerLayoutParams(
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

            _floatingButtonView.SetOnTouchListener(new DraggableButtonListener(this, buttonParams));

            try
            {
                _windowManager.AddView(_floatingButtonView, buttonParams);
            }
            catch { _floatingButtonView = null; }
        }

        private void RemoveAllOverlays()
        {
            RemoveNavBarOverlays();
            RemoveTopBar();
            RemoveFloatingButton();
        }

        private void RemoveNavBarOverlays()
        {
            if (_windowManager == null) return;
            try { if (_navHomeView != null) _windowManager.RemoveView(_navHomeView); } catch { }
            try { if (_navBackLeftView != null) _windowManager.RemoveView(_navBackLeftView); } catch { }
            try { if (_navBackRightView != null) _windowManager.RemoveView(_navBackRightView); } catch { }
            _navHomeView = null;
            _navBackLeftView = null;
            _navBackRightView = null;
        }

        private void RemoveTopBar()
        {
            if (_topBarView != null && _windowManager != null)
            {
                try { _windowManager.RemoveView(_topBarView); } catch { }
                _topBarView = null;
            }
        }

        private void RemoveFloatingButton()
        {
            if (_floatingButtonView != null && _windowManager != null)
            {
                try { _windowManager.RemoveView(_floatingButtonView); } catch { }
                _floatingButtonView = null;
            }
        }

        public void TriggerScreenOffAction()
        {
            VibrateFeedback();

            string mode = AppSettings.GetOperationMode(this);

            if (mode == "standby")
            {
                var standbyIntent = new Intent(this, typeof(StandbyActivity));
                standbyIntent.AddFlags(ActivityFlags.NewTask | ActivityFlags.SingleTop);
                StartActivity(standbyIntent);
            }
            else
            {
                bool locked = KassenAccessibilityService.LockScreen();
                if (!locked)
                {
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

        private int DpToPx(int dp)
        {
            float density = Resources?.DisplayMetrics?.Density ?? 1.0f;
            return (int)(dp * density + 0.5f);
        }

        // Listener Pintar Tombol Navigasi (Home & Back): 1x ketuk = fungsi asli, 2x ketuk = matikan layar
        private class NavTouchListener : Java.Lang.Object, View.IOnTouchListener
        {
            private readonly FloatingOverlayService _svc;
            private readonly GlobalAction _singleTapActionType;
            private long _lastTapTime = 0;
            private readonly Handler _handler = new Handler(Looper.MainLooper ?? Looper.MyLooper()!);
            private Action? _pendingSingleTapAction;

            public NavTouchListener(FloatingOverlayService svc, GlobalAction actionType)
            {
                _svc = svc;
                _singleTapActionType = actionType;
            }

            public bool OnTouch(View? v, MotionEvent? e)
            {
                if (e == null) return false;

                if (e.Action == MotionEventActions.Down)
                {
                    long now = SystemClock.ElapsedRealtime();
                    long diff = now - _lastTapTime;

                    // DOUBLE TAP TERDETEKSI! (antara 80ms s/d 550ms)
                    if (diff >= 80 && diff <= 550)
                    {
                        _lastTapTime = 0;
                        if (_pendingSingleTapAction != null)
                        {
                            _handler.RemoveCallbacks(_pendingSingleTapAction);
                            _pendingSingleTapAction = null;
                        }
                        _svc.TriggerScreenOffAction();
                        return true;
                    }
                    else
                    {
                        _lastTapTime = now;

                        // Jadwalkan aksi 1x ketuk normal (Home atau Back) jika dalam 240ms tidak ada ketukan kedua
                        if (_pendingSingleTapAction != null)
                        {
                            _handler.RemoveCallbacks(_pendingSingleTapAction);
                        }

                        _pendingSingleTapAction = () =>
                        {
                            KassenAccessibilityService.Instance?.PerformGlobalAction(_singleTapActionType);
                        };
                        _handler.PostDelayed(_pendingSingleTapAction, 240);
                        return true;
                    }
                }
                return false;
            }
        }

        private class SimpleDoubleTapListener : Java.Lang.Object, View.IOnTouchListener
        {
            private readonly FloatingOverlayService _svc;
            private long _lastTap = 0;

            public SimpleDoubleTapListener(FloatingOverlayService svc) => _svc = svc;

            public bool OnTouch(View? v, MotionEvent? e)
            {
                if (e?.Action == MotionEventActions.Down)
                {
                    long now = SystemClock.ElapsedRealtime();
                    long diff = now - _lastTap;
                    if (diff >= 80 && diff <= 550)
                    {
                        _lastTap = 0;
                        _svc.TriggerScreenOffAction();
                        return true;
                    }
                    _lastTap = now;
                    return true;
                }
                return false;
            }
        }

        private class DraggableButtonListener : Java.Lang.Object, View.IOnTouchListener
        {
            private readonly FloatingOverlayService _svc;
            private readonly WindowManagerLayoutParams _params;
            private float _startX, _startY;
            private int _initX, _initY;
            private bool _isMove = false;
            private long _lastTap = 0;

            public DraggableButtonListener(FloatingOverlayService svc, WindowManagerLayoutParams p)
            {
                _svc = svc;
                _params = p;
            }

            public bool OnTouch(View? v, MotionEvent? e)
            {
                if (e == null || _svc._windowManager == null) return false;

                switch (e.Action)
                {
                    case MotionEventActions.Down:
                        _startX = e.RawX;
                        _startY = e.RawY;
                        _initX = _params.X;
                        _initY = _params.Y;
                        _isMove = false;
                        return true;

                    case MotionEventActions.Move:
                        float dx = e.RawX - _startX;
                        float dy = e.RawY - _startY;
                        if (!_isMove && (Math.Abs(dx) > 25 || Math.Abs(dy) > 25))
                        {
                            _isMove = true;
                        }
                        if (_isMove)
                        {
                            _params.X = _initX + (int)dx;
                            _params.Y = _initY + (int)dy;
                            try { _svc._windowManager.UpdateViewLayout(v, _params); } catch { }
                        }
                        return true;

                    case MotionEventActions.Up:
                        if (!_isMove)
                        {
                            long now = SystemClock.ElapsedRealtime();
                            long diff = now - _lastTap;
                            if (diff >= 80 && diff <= 550)
                            {
                                _lastTap = 0;
                                _svc.TriggerScreenOffAction();
                            }
                            else
                            {
                                _lastTap = now;
                            }
                        }
                        else
                        {
                            AppSettings.SetFloatingX(_svc, _params.X);
                            AppSettings.SetFloatingY(_svc, _params.Y);
                        }
                        return true;
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

            builder.SetContentTitle("Kassen Double Tap Layar Aktif")
                   .SetContentText("Ketuk 2x tombol Home atau Back untuk mematikan layar.")
                   .SetSmallIcon(Resource.Drawable.ic_lock_power)
                   .SetContentIntent(pendingIntent)
                   .SetOngoing(true);

            return builder.Build();
        }
    }
}
