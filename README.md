# Double Tap Screen Control - Kassen XA-02 Pro POS

Aplikasi Android (APK) khusus untuk perangkat mesin kasir / POS **Kassen XA-02 Pro** (Safedroid OS Android 10/11) untuk mengontrol layar mati (*Double Tap to Screen Off*) dan menyalakan layar (*Double Tap to Screen On / Wake*) tanpa harus menekan tombol fisik power yang rawan aus/rusak.

---

## 📦 Lokasi File APK Siap Pakai
File APK sudah dikompilasi secara Native AOT (Ahead-of-Time), sangat ringan (~6.8 MB), dan sudah ditandatangani (*Signed*):
- **Path**: `c:\DATA D\Project\KassenDoubleTapScreen\KassenDoubleTap_XA02Pro.apk`

---

## 🚀 Cara Memasang (Install) ke Kassen XA-02 Pro
Pilih salah satu cara termudah berikut:
1. **Menggunakan Flashdisk OTG (Type-C)**:
   - Salin file `KassenDoubleTap_XA02Pro.apk` ke flashdisk.
   - Colokkan ke port USB Type-C di Kassen XA-02 Pro.
   - Buka aplikasi **File Manager** bawaan Kassen, klik file APK dan pilih **Install**.
2. **Menggunakan Kabel Data USB (ADB)**:
   - Hubungkan Kassen ke komputer via kabel USB.
   - Buka PowerShell dan jalankan:
     ```powershell
     & "C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe" install -r "c:\DATA D\Project\KassenDoubleTapScreen\KassenDoubleTap_XA02Pro.apk"
     ```
3. **Menggunakan WhatsApp Web / Google Drive**:
   - Kirim file APK ke chat/Drive, buka browser Chrome di Kassen, unduh dan install.

---

## ⚙️ Panduan Pengaturan Pertama Kali (Di Layar POS)
Setelah aplikasi dibuka di Kassen XA-02 Pro:

1. **Berikan 3 Izin yang Diminta** (Cukup klik tombol "Izinkan" berwarna biru):
   - **Izin Tampilkan di Atas Aplikasi Lain (Overlay)**: Agar tombol melayang dapat tampil di atas aplikasi kasir (Moka, Pawoon, Olsera, Majoo, dll).
   - **Izin Layanan Aksesibilitas**: Cari nama **"Double Tap Kassen"** di daftar dan aktifkan saklarnya. Ini digunakan untuk perintah penguncian layar resmi.
   - **Izin Abaikan Optimasi Baterai**: Agar service tidak ditutup otomatis oleh sistem Safedroid saat perangkat standby.

2. **Pilih Mode Kerja yang Diinginkan**:
   - **Mode 1: Mode Siaga POS (Rekomendasi Kasir)**:
     - Ketika tombol melayang diketuk 2x, layar langsung berubah hitam pekat dengan kecerahan 0 (hemat baterai).
     - Ketuk 2x di mana saja pada layar hitam -> **Layar langsung menyala seketika (0 milidetik respon)** dan langsung kembali ke aplikasi kasir.
   - **Mode 2: Mode Kunci Sistem Penuh (Native Lock)**:
     - Ketuk 2x tombol melayang -> Sistem Android benar-benar mengunci layar.
     - Bangunkan layar dengan **Sensor Ketukan Fisik (Accelerometer)**: Cukup ketuk layar/bodi Kassen 2x agak mantap untuk menyalakan layar, atau gunakan tombol power fisik.

3. **Nyalakan Saklar Utama**:
   - Geser switch **"Aktifkan Fitur Double Tap"** ke posisi **ON** (Layanan Aktif - Warna Hijau).
   - Tombol melayang akan muncul di layar. Anda dapat menggeser posisinya ke sudut atas atau samping sesuai kenyamanan kasir.
