# PillarSys (WinForms + SQL Server Express + Python Analiz)

Bu proje: **tek pencereli** ilaç stok takibi yapar ve **Python ile veri analizi grafiği** üretip arayüzde gösterir.
- Sol: Ekle / Çıkart (stok düş) / Güncelle / Temizle
- Sağ üst: Stok tablosu
- Sağ alt: Python ile üretilen analiz grafiği (PNG)

## 0) Gerekenler (Kurulum Sırası Önemli)

### A) Visual Studio 2022
1. Visual Studio Installer → **Modify**
2. Workloads:
   - **.NET desktop development** (zorunlu)
3. Individual components (varsa):
   - **.NET 8.0 SDK** (zorunlu)
   - **.NET Framework targeting pack** (opsiyonel)

### B) SQL Server Express + SSMS
1. **SQL Server Express** kurulu olmalı (Instance: `SQLEXPRESS`)
2. **SQL Server Management Studio (SSMS)** kurulu olmalı
3. SQL Server servisinin çalıştığını doğrula:
   - Windows Services → `SQL Server (SQLEXPRESS)` → Running

### C) ODBC Driver (Python için zorunlu)
- **Microsoft ODBC Driver 18 for SQL Server** (veya 17) kurulu olmalı.

### D) Python (Analiz için)
- Öneri: **Python 3.11+**
- Kurulumda:
  - **Add Python to PATH** işaretli olsun

## 1) Projeyi Açma
1. ZIP’i çıkart
2. Klasöre sağ tık → **Properties** → (varsa) **Unblock** / Engellemeyi kaldır
3. `PillarSys.sln` dosyasını Visual Studio 2022 ile aç

## 2) NuGet Paketlerini Yükleme
Visual Studio:
- Solution Explorer → Solution’a sağ tık → **Restore NuGet Packages**
- Ya da Build alırken otomatik restore olur.

## 3) Python Paketlerini Kurma
Komut İstemi / PowerShell:
```bash
pip install pyodbc pandas matplotlib
```

> Not: `pyodbc` kurulurken hata verirse ODBC Driver kurulu değil demektir.

## 4) SQL Bağlantısı (.\SQLEXPRESS)
Proje `appsettings.json` içinden bağlanır:

`src/PillarSys.UI/appsettings.json`

Varsayılan:
- Server: `.\SQLEXPRESS`
- Database: `PillarSys`
- Windows Integrated Security

## 5) Veritabanı Otomatik Oluşturma
Uygulama ilk açılışta:
- `PillarSys` DB yoksa **oluşturur**
- `Ilaclar` ve `StokHareketleri` tablolarını **kurar**

> Bu işlem için Windows kullanıcının SQL Server’da DB oluşturma yetkisi olmalıdır (genelde yerel admin hesaplarında olur).

## 6) Çalıştırma
1. `PillarSys.UI` projesini **Startup Project** yap
2. **F5** ile çalıştır

## 7) Kullanım Notları
- **Ekle**: Yeni ilaç kaydı oluşturur (aynı isim/barkod varsa engeller).
- **Çıkart (Stok Düş)**: Sol taraftaki **Stok** alanını “düşülecek miktar” gibi kullanır.
- **Güncelle**: Önce sağdaki tablodan bir ilaç seç → sol taraf dolsun → güncelle.
- İlaç adında yazım hatası yaparsan alanı terk edince **en yakın eşleşmeye otomatik düzeltir**.

## 8) Python Analiz Çalışmıyorsa (En Sık 3 Sebep)
1) Python PATH’te değil → `python --version` çalışmıyor  
2) `pip install ...` paketleri kurulmamış  
3) ODBC Driver yok → `pyodbc` bağlantı hatası

## 9) Ayar Değiştirme
Python exe farklıysa:
- `appsettings.json` → `Python:PythonExe` değerini örn:
  - `python`
  - `C:\Users\...\python.exe`

Analiz script yolu:
- `Python:ScriptRelativePath` (default doğru)

