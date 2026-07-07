# Enterprise License Deployment Manager

Aplikasi Windows Forms (.NET 8, C#) untuk Visual Studio 2022 yang secara otomatis:

1. Mendeteksi **IP address aktif** dan **MAC address aktif** komputer saat aplikasi dijalankan.
2. Membandingkan IP aktif dengan IP yang dikonfigurasi (`Required Active IP`).
3. Jika cocok, mencari folder di dalam **License Root Folder** yang namanya sama dengan MAC address aktif (contoh: folder `AA-BB-CC-DD-EE-FF`).
4. Jika ditemukan, menyalin seluruh isi folder tersebut ke **7 folder tujuan** yang dikonfigurasi.
5. Menjalankan **7 aplikasi** yang dikonfigurasi.
6. Mencatat semua aktivitas ke **audit log** (file harian + tampilan live di layar).
7. Menjalankan ulang seluruh proses di atas secara otomatis setiap hari jam **06:50** (bisa diubah di Settings), dan menampilkan **Current Time** serta **Next Scheduled Run** secara real-time.

## Cara membuka di Visual Studio 2022

1. Buka file `EnterpriseLicenseDeployer.sln` dengan Visual Studio 2022.
2. Pastikan workload **.NET Desktop Development** sudah terinstall (untuk dukungan Windows Forms).
3. Jika VS2022 meminta retarget SDK, pastikan **.NET 8 SDK** sudah terinstall di komputer Anda (Tools > Get Tools and Features, atau download dari dotnet.microsoft.com). Jika Anda hanya punya .NET Framework, project bisa di-retarget ke `net48` — tinggal ganti `<TargetFramework>` di file `.csproj` menjadi `net48` (fitur yang dipakai kompatibel).
4. Set `EnterpriseLicenseDeployer` sebagai Startup Project (klik kanan project > Set as Startup Project).
5. Tekan F5 untuk build & run.

## Struktur folder project

```
EnterpriseLicenseDeployer/
├── EnterpriseLicenseDeployer.sln
└── EnterpriseLicenseDeployer/
    ├── EnterpriseLicenseDeployer.csproj
    ├── Program.cs                     # Entry point
    ├── MainForm.cs                    # UI utama (status box, tombol, audit log)
    ├── SettingsForm.cs                # UI konfigurasi (IP, folder, aplikasi, jadwal)
    ├── appsettings.json               # Konfigurasi default (dipakai saat pertama kali run)
    ├── Models/
    │   └── AppConfig.cs               # Model konfigurasi
    └── Services/
        ├── ConfigService.cs           # Load/save config.json
        ├── AuditLogger.cs             # Audit log (file + live event)
        ├── NetworkService.cs          # Deteksi IP & MAC aktif
        ├── LicenseService.cs          # Cari folder MAC & copy file
        ├── AppLauncherService.cs      # Jalankan 7 aplikasi
        ├── DeploymentOrchestrator.cs  # Gabungkan semua langkah jadi 1 rutin
        └── ScheduleService.cs         # Hitung waktu jadwal berikutnya
```

## Konfigurasi (dari dalam aplikasi, menu File > Settings)

Semua path berikut **bisa diatur langsung dari UI aplikasi** (tidak perlu edit file manual):

- **Required Active IP** — IP yang harus aktif agar proses lanjut.
- **License Root Folder** — 1 folder induk berisi sub-folder per MAC address, contoh:
  ```
  C:\License\
  ├── AA-BB-CC-DD-EE-FF\   (isi lisensi untuk MAC ini)
  ├── 11-22-33-44-55-66\
  └── ...
  ```
  Nama folder MAC bisa pakai format `AA-BB-CC-DD-EE-FF`, `AA:BB:CC:DD:EE:FF`, atau `AABBCCDDEEFF` — aplikasi akan mencocokkan otomatis.
- **Log Folder** — folder penyimpanan audit log harian. Default: `%ProgramData%\EnterpriseLicenseDeployer\Logs`.
- **7 Destination Folders** — folder tujuan tempat file lisensi disalin.
- **7 Applications** — path .exe aplikasi yang akan dijalankan setelah lisensi berhasil disalin.
- **Close time (HH:MM)** — jam berapa aplikasi yang masih berjalan akan ditutup otomatis setiap hari (default 06:45), supaya run pagi tidak bentrok dengan proses lama.
- **Run time (HH:MM)** — jam berapa proses recheck otomatis dijalankan setiap hari (default 06:50).

Konfigurasi disimpan di:
```
%ProgramData%\EnterpriseLicenseDeployer\config.json
```

Audit log tersimpan di folder yang bisa diatur dari menu **File > Settings**. Default-nya:
```
%ProgramData%\EnterpriseLicenseDeployer\Logs\audit_yyyyMMdd.log
```

## Catatan

- Karena aplikasi menulis ke `%ProgramData%` dan menyalin file ke folder tujuan (biasanya di `C:\Apps\...`), jalankan sebagai user yang punya izin tulis ke folder-folder tersebut (atau jalankan as Administrator jika perlu).
- Jadwal **Close time** menutup proses aplikasi yang path-nya sama dengan daftar **7 Applications** sebelum jadwal recheck berjalan.
- Tombol **Run Now** di layar utama memicu proses secara manual kapan saja, tanpa menunggu jadwal jam 06:50.
- UI dibuat secara programatik (bukan lewat file `.Designer.cs` terpisah) supaya seluruh tampilan mudah dibaca dalam satu file — silakan sesuaikan warna/logo di bagian atas `MainForm.cs` (`HeaderColor`, `AccentColor`) sesuai identitas perusahaan Anda.
