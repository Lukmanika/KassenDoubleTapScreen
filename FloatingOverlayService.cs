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
    public class FloatingOverlayService : Service, View.IOnTouchListener, GestureDetector.IOnGestureListener, GestureDetector.IOnDoubleTapListener
    {
        private const string ChannelId = "kassen_overlay_service_channel";
        private const int NotificationId = 1001;

        private IWindowManager? _windowManager;
        private View? _floatingView;
        private WindowManagerLayoutParams? _layoutParams;
        private GestureDetector? _gestureDetector;
        private Vibrator? _vibrator;

        private int _initialX;
        private int _initialY;
        private float _initialTouchX;
        private float _initialTouchY;
        private bool _isDragging = false;
        private int _touchSlop = 10;

        public static FloatingOverlayService? Instance { get; private set; }

        public override void OnCreate()
        {
            base.OnCreate();
            Instance = this;

            _windowManager = GetSystemService(WindowService) as IWindowManager;
#pragma warning disable CA1422
            _vibrator = (Vibrator?)GetSystemService(VibratorService);
#pragma warning restore CA1422
            _gestureDetector = new GestureDetector(this, this);
            _gestureDetector.SetOnDoubleTapListener(this);

            var vc = ViewConfiguration.Get(this);
            _touchSlop = vc?.ScaledTouchSlop ?? 10;

            CreateNotificationChannel();
            var notification = BuildNotification();
            StartForeground(NotificationId, notification);

            ShowFloatingButton();
        }

        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            UpdateFloatingAppearance();
            return StartCommandResult.Sticky;
        }

        public override void OnDestroy()
        {
            RemoveFloatingButton();
            if (Instance == this)
            {
                Instance = null;
            }
            base.OnDestroy();
        }

        public override IBinder? OnBind(Intent? intent) => null;

        private void ShowFloatingButton()
        {
            if (_floatingView != null || _windowManager == null) return;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.M && !Settings.CanDrawOverlays(this))
            {
                // Tidak memiliki izin overlay
                return;
            }

            var inflater = (LayoutInflater?)GetSystemService(LayoutInflaterService);
            _floatingView = inflater?.Inflate(Resource.Layout.view_floating_button, null);
            if (_floatingView == null) return;

            _floatingView.SetOnTouchListener(this);

            var displayMetrics = Resources?.DisplayMetrics;
            int screenWidth = displayMetrics?.WidthPixels ?? 720;
            int screenHeight = displayMetrics?.HeightPixels ?? 1280;

            int sizeDp = GetButtonSizeDp();
            int sizePx = DpToPx(sizeDp);

            int savedX = AppSettings.GetFloatingX(this, screenWidth - sizePx - DpToPx(16));
            int savedY = AppSettings.GetFloatingY(this, screenHeight / 3);

            var layoutType = Build.VERSION.SdkInt >= BuildVersionCodes.O
                ? WindowManagerTypes.ApplicationOverlay
                : WindowManagerTypes.Phone;

            _layoutParams = new WindowManagerLayoutParams(
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

            UpdateFloatingAppearance();

            try
            {
                _windowManager.AddView(_floatingView, _layoutParams);
            }
            catch (Exception)
            {
                // Gagal menambahkan view overlay
            }
        }

        public void UpdateFloatingAppearance()
        {
            if (_floatingView == null) return;

            int alphaPercent = AppSettings.GetFloatingAlpha(this);
            float alpha = Math.Max(0.15f, alphaPercent / 100.0f);
            _floatingView.Alpha = alpha;

            var icon = _floatingView.FindViewById<ImageView>(Resource.Id.imgFloatingIcon);
            int sizeDp = GetButtonSizeDp();
            int sizePx = DpToPx(sizeDp);

            if (icon != null)
            {
                var iconParams = icon.LayoutParameters;
                if (iconParams != null)
                {
                    iconParams.Width = sizePx;
                    iconParams.Height = sizePx;
                    icon.LayoutParameters = iconParams;
                }
            }

            if (_layoutParams != null && _windowManager != null)
            {
                _layoutParams.Width = sizePx;
                _layoutParams.Height = sizePx;
                try
                {
                    _windowManager.UpdateViewLayout(_floatingView, _layoutParams);
                }
                catch { }
            }
        }

        private void RemoveFloatingButton()
        {
            if (_floatingView != null && _windowManager != null)
            {
                try
                {
                    _windowManager.RemoveView(_floatingView);
                }
                catch { }
                _floatingView = null;
            }
        }

        private int GetButtonSizeDp()
        {
            int sizeIndex = AppSettings.GetFloatingSize(this);
            return sizeIndex switch
            {
                0 => 42, // Kecil
                2 => 62, // Besar
                _ => 52  // Normal
            };
        }

        private int DpToPx(int dp)
        {
            float density = Resources?.DisplayMetrics?.Density ?? 1.0f;
            return (int)(dp * density + 0.5f);
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
                        _vibrator.Vibrate(VibrationEffect.CreateOneShot(50, VibrationEffect.DefaultAmplitude));
                    }
                    else
                    {
                        _vibrator.Vibrate(50);
                    }
#pragma warning restore CA1422
                }
            }
            catch { }
        }

        private void ExecuteDoubleTapAction()
        {
            VibrateFeedback();

            string mode = AppSettings.GetOperationMode(this);

            if (mode == "standby")
            {
                // Mode Siaga Layar Hitam (Rekomendasi POS Kassen)
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
                    // Jika Aksesibilitas belum diaktifkan, alihkan ke Mode Standby agar kasir tetap bisa mematikan layar
                    var standbyIntent = new Intent(this, typeof(StandbyActivity));
                    standbyIntent.AddFlags(ActivityFlags.NewTask | ActivityFlags.SingleTop);
                    StartActivity(standbyIntent);
                }
            }
        }

        // View.IOnTouchListener
        public bool OnTouch(View? v, MotionEvent? e)
        {
            if (e == null || _layoutParams == null || _windowManager == null) return false;

            _gestureDetector?.OnTouchEvent(e);

            switch (e.Action)
            {
                case MotionEventActions.Down:
                    _initialX = _layoutParams.X;
                    _initialY = _layoutParams.Y;
                    _initialTouchX = e.RawX;
                    _initialTouchY = e.RawY;
                    _isDragging = false;
                    return true;

                case MotionEventActions.Move:
                    float dx = e.RawX - _initialTouchX;
                    float dy = e.RawY - _initialTouchY;

                    if (!_isDragging && (Math.Abs(dx) > _touchSlop || Math.Abs(dy) > _touchSlop))
                    {
                        _isDragging = true;
                    }

                    if (_isDragging)
                    {
                        _layoutParams.X = _initialX + (int)dx;
                        _layoutParams.Y = _initialY + (int)dy;
                        try
                        {
                            _windowManager.UpdateViewLayout(_floatingView, _layoutParams);
                        }
                        catch { }
                    }
                    return true;

                case MotionEventActions.Up:
                    if (_isDragging)
                    {
                        // Simpan posisi terakhir tombol
                        AppSettings.SetFloatingX(this, _layoutParams.X);
                        AppSettings.SetFloatingY(this, _layoutParams.Y);
                    }
                    return true;
            }

            return false;
        }

        // GestureDetector.IOnDoubleTapListener
        public bool OnDoubleTap(MotionEvent e)
        {
            ExecuteDoubleTapAction();
            return true;
        }

        public bool OnDoubleTapEvent(MotionEvent e) => false;
        public bool OnSingleTapConfirmed(MotionEvent e) => false;

        // GestureDetector.IOnGestureListener
        public bool OnDown(MotionEvent e) => true;
        public bool OnFling(MotionEvent? e1, MotionEvent e2, float velocityX, float velocityY) => false;
        public void OnLongPress(MotionEvent e) { }
        public bool OnScroll(MotionEvent? e1, MotionEvent e2, float distanceX, float distanceY) => false;
        public void OnShowPress(MotionEvent e) { }
        public bool OnSingleTapUp(MotionEvent e) => false;

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
                    Description = "Tombol melayang untuk mematikan layar POS Kassen"
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
                   .SetContentText("Tombol melayang aktif di layar POS.")
                   .SetSmallIcon(Resource.Drawable.ic_lock_power)
                   .SetContentIntent(pendingIntent)
                   .SetOngoing(true);

            return builder.Build();
        }
    }
}
