using System;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace KassenDoubleTapScreen
{
    [Activity(
        Label = "Standby",
        Theme = "@android:style/Theme.NoTitleBar.Fullscreen",
        LaunchMode = LaunchMode.SingleTask,
        ExcludeFromRecents = true,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.KeyboardHidden,
        ScreenOrientation = ScreenOrientation.Portrait,
        Exported = false
    )]
    public class StandbyActivity : Activity, GestureDetector.IOnGestureListener, GestureDetector.IOnDoubleTapListener
    {
        private GestureDetector? _gestureDetector;
        private TextView? _tvHint;
        private Vibrator? _vibrator;
        private long _lastTouchDownTime = 0;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.OMr1)
            {
#pragma warning disable CA1416
                SetShowWhenLocked(true);
                SetTurnScreenOn(true);
#pragma warning restore CA1416
            }
            else
            {
#pragma warning disable CS0618
                Window?.AddFlags(
                    WindowManagerFlags.ShowWhenLocked |
                    WindowManagerFlags.TurnScreenOn |
                    WindowManagerFlags.DismissKeyguard
                );
#pragma warning restore CS0618
            }

            Window?.AddFlags(WindowManagerFlags.KeepScreenOn | WindowManagerFlags.Fullscreen);

            // Redupkan kecerahan layar ke tingkat terendah (0.001f = hampir padam/hitam total)
            if (Window?.Attributes != null)
            {
                var lp = Window.Attributes;
                lp.ScreenBrightness = 0.001f;
                Window.Attributes = lp;
            }

            SetContentView(Resource.Layout.activity_standby);

            _tvHint = FindViewById<TextView>(Resource.Id.tvStandbyHint);
#pragma warning disable CA1422
            _vibrator = (Vibrator?)GetSystemService(VibratorService);
#pragma warning restore CA1422
            _gestureDetector = new GestureDetector(this, this);
            _gestureDetector.SetOnDoubleTapListener(this);

            var root = FindViewById<FrameLayout>(Resource.Id.layoutStandbyRoot);
            if (root != null)
            {
                root.Touch += (s, e) =>
                {
                    if (e.Event != null)
                    {
                        if (e.Event.Action == MotionEventActions.Down)
                        {
                            long now = SystemClock.ElapsedRealtime();
                            long diff = now - _lastTouchDownTime;

                            // Jika dua ketukan berjarak 60ms s/d 650ms, langsung bangunkan layar!
                            if (diff >= 60 && diff <= 650)
                            {
                                _lastTouchDownTime = 0;
                                WakeUpNow();
                                return;
                            }
                            else
                            {
                                _lastTouchDownTime = now;
                                ShowHintBriefly();
                            }
                        }

                        _gestureDetector.OnTouchEvent(e.Event);
                    }
                };
            }

            HideSystemUI();
        }

        protected override void OnResume()
        {
            base.OnResume();
            HideSystemUI();
        }

        private void HideSystemUI()
        {
            if (Window?.DecorView != null)
            {
#pragma warning disable CS0618
                Window.DecorView.SystemUiVisibility = (StatusBarVisibility)(
                    SystemUiFlags.ImmersiveSticky |
                    SystemUiFlags.HideNavigation |
                    SystemUiFlags.Fullscreen |
                    SystemUiFlags.LayoutHideNavigation |
                    SystemUiFlags.LayoutFullscreen |
                    SystemUiFlags.LayoutStable
                );
#pragma warning restore CS0618
            }
        }

        private void WakeUpNow()
        {
            VibrateBriefly();
            Finish();
#pragma warning disable CA1422
            OverridePendingTransition(0, 0);
#pragma warning restore CA1422
        }

        private void VibrateBriefly()
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

        private void ShowHintBriefly()
        {
            if (_tvHint != null)
            {
                _tvHint.Alpha = 1.0f;
                _tvHint.Animate()?.Alpha(0.0f)?.SetDuration(1000)?.Start();
            }
        }

        // GestureDetector.IOnDoubleTapListener
        public bool OnDoubleTap(MotionEvent e)
        {
            WakeUpNow();
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

        public override bool OnKeyDown(Keycode keyCode, KeyEvent? e)
        {
            // Jika tombol fisik power, volume, atau back ditekan, segera bangunkan layar
            if (keyCode == Keycode.Back || keyCode == Keycode.VolumeDown || keyCode == Keycode.VolumeUp || keyCode == Keycode.Power)
            {
                WakeUpNow();
                return true;
            }
            return base.OnKeyDown(keyCode, e);
        }

        public override bool OnKeyUp(Keycode keyCode, KeyEvent? e)
        {
            if (keyCode == Keycode.Back || keyCode == Keycode.VolumeDown || keyCode == Keycode.VolumeUp || keyCode == Keycode.Power)
            {
                WakeUpNow();
                return true;
            }
            return base.OnKeyUp(keyCode, e);
        }
    }
}
