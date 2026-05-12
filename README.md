# 1984

**[English](#english) | [Русский](#русский)**

---

## English

### What is it

Local-only Windows activity tracker for employee productivity monitoring. Runs in tray, no cloud, no network.

### Features

- Active window tracking (process name, window title, duration)
- Browser tab tracking via extension (URL, title, time per page)
- Periodic screenshots (configurable interval)
- Idle detection
- Daily HTML reports
- Password-protected UI
- English / Russian interface
- Auto-start with Windows (toggleable)

### What it does NOT do

No cloud, no keylogger, no page content scraping, no remote access, no AI, no OCR.

### Files required to run

```
1984.exe              # self-contained app
1984.config.json      # install config (optional, defaults are built-in)
```

Runtime data in `%APPDATA%\1984\`:
```
tracker.db            # SQLite database
screenshots\          # JPEG screenshots
reports\              # HTML reports
```

### Browser plugin

Two separate extensions for full URL-level tracking:

| Browser | Folder |
|---------|--------|
| Chrome, Edge, Brave, Opera | `browser-extension/chromium/` |
| Firefox | `browser-extension/firefox/` |

**Install:**
1. Open extensions page (`chrome://extensions`, `edge://extensions`, or `about:debugging` for Firefox)
2. Enable Developer mode
3. Load unpacked  select the folder

Without the plugin, browser tracking falls back to window title only.

### Build & run

```powershell
# Build
dotnet build ProductivityTracker.sln -c Release

# Run from source
dotnet run --project src\ProductivityTracker

# Publish self-contained EXE
dotnet publish src\ProductivityTracker -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish\
```

**Requirements:** .NET 8 SDK (build), Windows 10/11 (run). Self-contained EXE needs no .NET runtime.

### Settings

All settings editable in UI  Settings tab. Most apply immediately (screenshot interval, cleanup; language requires restart).

### Deployment

1. Publish `1984.exe`
2. Create `1984.config.json` (optional)
3. Place both in `%LOCALAPPDATA%\Programs\1984\`
4. App auto-registers HKCU auto-start on first launch

---

## Русский

### Что это

Локальный трей-трекер активности сотрудников для Windows. Без облака, без сети.

### Функции

- Отслеживание активного окна (процесс, заголовок, длительность)
- Отслеживание вкладок браузера через плагин (URL, заголовок, время на странице)
- Периодические скриншоты (настраиваемый интервал)
- Определение бездействия
- Ежедневные HTML-отчёты
- Интерфейс под паролем
- Английский / русский интерфейс
- Автозапуск с Windows (отключаемый)

### Что НЕ делает

Без облака, без кейлоггера, без чтения содержимого страниц, без удалённого доступа, без ИИ, без OCR.

### Файлы для запуска

```
1984.exe              # автономное приложение
1984.config.json      # конфигурация (необязательно, вшиты значения по умолчанию)
```

Данные в `%APPDATA%\1984\`:
```
tracker.db            # база SQLite
screenshots\          # скриншоты JPEG
reports\              # HTML-отчёты
```

### Плагин для браузера

Два отдельных расширения для полного отслеживания URL:

| Браузер | Папка |
|---------|-------|
| Chrome, Edge, Brave, Opera | `browser-extension/chromium/` |
| Firefox | `browser-extension/firefox/` |

**Установка:**
1. Откройте страницу расширений (`chrome://extensions`, `edge://extensions` или `about:debugging` для Firefox)
2. Включите режим разработчика
3. Загрузить распакованное  выберите папку

Без плагина отслеживание браузера  только по заголовку окна.

### Сборка и запуск

```powershell
# Сборка
dotnet build ProductivityTracker.sln -c Release

# Запуск из исходников
dotnet run --project src\ProductivityTracker

# Публикация автономного EXE
dotnet publish src\ProductivityTracker -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish\
```

**Требования:** .NET 8 SDK (для сборки), Windows 10/11 (для запуска). Автономный EXE не требует .NET.

### Настройки

Все настройки редактируются в UI  вкладка Настройки. Большинство применяется сразу (интервал скриншотов, очистка; язык требует перезапуска).

### Развёртывание

1. Опубликовать `1984.exe`
2. Создать `1984.config.json` (опционально)
3. Разместить оба файла в `%LOCALAPPDATA%\Programs\1984\`
4. Приложение само регистрирует автозапуск HKCU при первом запуске
