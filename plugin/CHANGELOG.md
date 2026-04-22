# Changelog

Все заметные изменения в проекте **Figma Direct Importer** будут фиксироваться в этом файле.

Формат основан на [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
и проект придерживается [Семантического Версионирования](https://semver.org/spec/v2.0.0.html).

## [2.7.2] - 2026-04-22
### Исправлено (Critical Prefab Instance Hotfix)
- **Hardened Unpacking**: `FigmaParserUtils.EnsureUnpacked` теперь гарантированно находит и распаковывает ближайший корень префаб-инстанса.
- **Mask Dismantle Safety**: Добавлен принудительный анпак детей и родителей при демонтаже масок, что предотвращает ошибки `SetParent is not possible`.
- **TreeWalker Sync Safety**: `FigmaTreeWalker` теперь освобождает существующие объекты от префаб-связей перед репарентингом.
- **Deferred Mask Safety**: Исправлено создание контейнеров масок при работе внутри префаб-инстансов.

## [2.7.1] - 2026-04-22
### Исправлено (Core Hardening & Prefab Stability)
- **Nested Prefab Protection**: `FigmaMaskResolver` теперь проверяет успешность репарентинга при демонтаже масок. Это предотвращает случайное удаление вложенных объектов, заблокированных префаб-инстансом.
- **Cache Lifecycle Fix**: Инициализация кэша существующих объектов теперь происходит строго после демонтажа масок, что исключает MissingReferenceException при обращении к удаленным объектам.
- **MissingReference Guard**: В `FigmaTreeWalker` добавлены проверки на `null` и автоматическая инвалидация битых ссылок в кэше.
- **Improved Exception Handling**: Добавлена детальная диагностика при ошибках репарентинга в логах.

## [2.7.0] - 2026-04-22
### Добавлено (Architecture Decomposition & API Caching)
- **Модульная архитектура**: FigmaParser декомпозирован из God Object (647 строк) в тонкий оркестратор (356 строк, -45%).
  - `FigmaTreeWalker` — рекурсивный обход дерева нод и построение иерархии
  - `FigmaMaskResolver` — жизненный цикл масок (dismantle/apply/cleanup)
  - `FigmaOrphanManager` — детекция и маркировка удалённых элементов
  - `FigmaFontAuditor` — аудит шрифтов против FontMappingTable
  - `FigmaParserUtils` — общие утилиты (unpack prefab)
- **API Response Caching**: Файловый кеш в `Library/FigmaCache/`. Перед полным запросом — лёгкий `?fields=version` чек. При совпадении версии — мгновенный кеш-хит.
  - `FigmaResponseCache` — сервис кеширования (save/load/clear)
  - `FigmaAPIClient.GetFileVersionAsync` — легковесная проверка версии файла
- **Централизованная версия**: Единая константа `FigmaImporter.Version` заменяет 15+ хардкод-строк. `FigmaLog.VersionPrefix` для логов.
- **Кнопка Clear Cache**: Реально работающая очистка кеша из UI (раньше был заглушка).

### Тесты
- **58 юнит-тестов** (было 31, +87%):
  - `DecompositionTests.cs` — 20 тестов для всех извлечённых классов
  - `CacheTests.cs` — 7 тестов для кеша (round-trip, инвалидация, очистка)
- Все тесты переведены с рефлексии на прямые вызовы
- `InternalsVisibleTo` для доступа тестов к internal классам

### Изменено
- `FigmaParser.cs` — тонкий оркестратор, делегирует работу новым классам
- `FigmaAPIClient.cs` — добавлен `GetFileVersionAsync()`, рабочий `ClearLocalCache()`
- `HandlerTests.cs`, `AdaptiveLayoutTests.cs` — без рефлексии, прямые вызовы
- `FigmaImporterWindow.cs` — версия через `FigmaImporter.Version`, рабочая кнопка кеша

## [2.6.0] - 2026-04-20
### Добавлено (Metadata Unblock Edition)
- **UNBLOCK METADATA**: Исправлен .gitignore, блокировавший загрузку критических .meta файлов в Unity, что приводило к потере связей при установке через Git.
- **Unified Versioning**: Синхронизация версий во всех модулях и окне импортера для исключения путаницы при диагностике.
- **Improved Hierarchy Guard**: Дополнительные проверки при очистке Canvas для предотвращения "призрачных" объектов.

## [2.5.0] - 2026-04-20
### Добавлено (Production Hardening Edition)
...
- **Unity Test Suite**: Набор тестов для верификации адаптивности.
- **Prefab Root Heuristic**: Автоматическое определение корневого фрейма импорта как корня префаба.
