# Project Evaluation System

Proje değerlendirme sistemi — öğrenci projelerini listeleyen, ziyaretçilerin 1–5 puan ve yorumla değerlendirebileceği bir ASP.NET Core MVC uygulaması.

## Özellikler

- **Proje yönetimi**: Proje ekleme, düzenleme, silme; açıklama, GitHub linki, proje görseli (Cloudinary)
- **Değerlendirme**: Ziyaretçiler ad, soyad, 1–5 puan ve isteğe bağlı yorum ile değerlendirme yapabilir
- **Admin paneli**: Oturum tabanlı giriş, proje CRUD, değerlendirmeleri görüntüleme
- **E-posta**: MailKit ile SMTP üzerinden bildirim/e-posta desteği
- **Medya**: Cloudinary ile proje görsellerinin yüklenmesi ve saklanması

## Teknolojiler

- **.NET 9** — ASP.NET Core MVC
- **Entity Framework Core 9** — ORM
- **PostgreSQL** — Veritabanı (Npgsql)
- **Cloudinary** — Görsel depolama
- **MailKit** — E-posta gönderimi
- **Bootstrap 5** — Arayüz
- **Session** — Admin oturum yönetimi

## Gereksinimler

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- PostgreSQL sunucusu (veya `appsettings.json` içinde bağlantı ayarı)
- Cloudinary hesabı (görsel yükleme için)
- SMTP ayarları (e-posta için, isteğe bağlı)

## Kurulum

1. Depoyu klonlayın:
   ```bash
   git clone <repo-url>
   cd ProjectEvaluationSystem
   ```

2. Veritabanı bağlantısını ayarlayın. `appsettings.json` (veya `appsettings.Development.json`) içinde:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=...;Database=...;Username=...;Password=..."
   }
   ```

3. Gerekirse Cloudinary ve SMTP ayarlarını `appsettings.json` içine ekleyin.

4. Migration’ları uygulayın:
   ```bash
   dotnet ef database update
   ```

5. Uygulamayı çalıştırın:
   ```bash
   dotnet run
   ```

   Tarayıcıda genelde `https://localhost:7xxx` veya `http://localhost:5xxx` adresinden erişilir (port `Properties/launchSettings.json` içinde tanımlıdır).

## Proje yapısı (özet)

- **Controllers**: `HomeController` (anasayfa, proje detay, değerlendirme), `AdminController` (giriş, proje CRUD, değerlendirmeler)
- **Models**: `Project`, `Evaluation`, `Contributor`, `User`
- **Data**: `ApplicationDbContext`, EF Core migration’lar
- **Services**: `ICloudinaryService` / `CloudinaryService`, `IEmailService` / `EmailService`
- **Views**: Razor view’lar (Home, Admin, Shared layout’lar)

## Hosting’e yükleme

Sunucuya hangi dosyaların yükleneceği ve adımlar için: **[HOSTING.md](HOSTING.md)**

## Demo

Canlı demo: **https://yusufakin.online/**

---

*Proje değerlendirme sistemi — ASP.NET Core 9 ile geliştirilmiştir.*
