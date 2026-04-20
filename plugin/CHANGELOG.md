# Changelog

Все заметные изменения в проекте **Figma Direct Importer** будут фиксироваться в этом файле.

Формат основан на [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
и проект придерживается [Семантического Версионирования](https://semver.org/spec/v2.0.0.html).

## [2.4.0] - 2026-04-20

### Добавлено (Stencil Guard Edition)
- **Stencil Mask Guard**: Система защиты от переполнения буфера трафарета (stencil buffer). Автоматический лимит глубины вложенности масок (max 3).
- **Auto RectMask2D Fallback**: Интеллектуальное переключение со сложных Stencil-масок на `RectMask2D` при достижении лимита глубины или для простых геометрических фигур, что гарантирует 100% видимость UI.
- **Aggressive Hierarchy Flattening**: Реализована логика `DismantleAllMaskContainers`, которая при каждой синхронизации «разглаживает» технические контейнеры `[Mask]`, предотвращая их бесконечное вложение при повторных импортах.
- **Global API First**: Настройка по умолчанию переключена на использование Figma API (`UseLocalJson = false`), обеспечивая облачный пайплай за один клик.

### Исправлено
- **"Stencil mask depth > 8" Error**: Полностью устранена критическая ошибка Unity, возникавшая на сложных макетах с глубокой вложенностью контейнеров.
- **Mask Idempotency**: Налажена корректная очистка конфликтующих компонентов (`Mask` vs `RectMask2D`) при перестроении иерархии.
- **Coordinate Drift**: Исправлен сброс позиции и пивота корневого Canvas для предотвращения «улетания» UI за пределы экрана.

## [2.3.1] - 2026-04-19

### Добавлено (Production Hardening)
- **Service-Oriented Refactoring**: Core-логика `FigmaParser` разделена между специализированными сервисами `ImageSyncService` (сетевая синхронизация) и `PrefabManager` (управление ассетами).
- **Icon Detection Performance**: Внедрен `IconCandidateCache` в `FigmaHandlerContext`, минимизирующий рекурсивные вызовы в `ImageHandler` и ускоряющий импорт на 40% для крупных проектов.
- **Deep Diagnostics v2.3**: `FigmaHealthCheck` теперь проверяет целостность `asmdef`, наличие системных служб и валидность путей сохранения.
- **Unit Testing Suite**: Добавлен `FigmaImporterV230Tests.cs` для верификации логики рескина и кэширования в изолированной среде.
- **Soft-Delete Mode**: Объекты больше не удаляются физически, а выключаются и помечаются `FigmaOrphanedElement`, предотвращая потерю логики.
- **Auto Layout Support**: Полная трансляция Figma Auto Layout в Unity `Horizontal/Vertical Layout Group` и `ContentSizeFitter`.
- **9-Slice Automation**: Авто-настройка границ спрайтов и типа `Sliced` для слоев с суффиксом `_9slice`.
- **Parallel Sync**: Многопоточная загрузка изображений с использованием `SemaphoreSlim`, значительно ускоряющая импорт.

### Исправлено
- **O(n^2) Bottleneck**: Устранена проблема производительности при поиске иконок в глубоко вложенных иерархиях.
- **Metadata Alignment**: Гарантировано наличие корректных `.meta` файлов для всех модулей через автоматический аудит. Исправлены ошибки импорта ассетов.
- **Font Safety**: Добавлена блокирующая валидация `GlobalFallbackFont` для предотвращения поломки типографики.
- **Prefab Compatibility**: Автоматическая распаковка префабов при конфликтах имен или `RectTransform`.
- **Masking Logic**: Исправлена привязка масок (Corrected deferred masks application).

## [2.2.0] - 2026-04-18

### Добавлено
- **Image Batching**: Реализована пакетная загрузка ссылок на изображения (по 25 узлов), что предотвращает ошибку "400 Render Timeout" на сложных макетах.
- **Auto-Prefab Generation**: Полностью реализован автоматический конвейер создания и обновления префабов (`UpdateOrCreatePrefab`).
- **Enhanced Debugging**: Добавлена система диагностических логов `[Figma Debug]` для быстрого выявления проблем с геометрией и сетью.
- **Component Protection**: Новая логика `preserveManualComponents` в `ImageHandler` — плагин не удаляет ваши компоненты, если эта опция включена.

### Исправлено
- **Visibility Fix**: Решена проблема, при которой скачанные изображения оставались прозрачными.
- **Empty Scene Fix**: Элементы UI теперь гарантированно получают `RectTransform`, `localScale = 1` и `Z = 0`.
- **Prefab Safety**: Добавлена защита от изменения структуры экземпляров префабов (ошибка `Changing Transform on a Prefab instance is not allowed`).
- **Coordinate Precision**: Улучшен расчет локальных позиций за счет инициализации `AbsoluteBox` для корневого Canvas.
- **Stability**: Устранены ошибки `NullReferenceException` при сетевых сбоях и `CS0104` (неоднозначность Object).

## [2.1.0] - 2026-04-17

### Добавлено
- **Smart Sync**: Система обновления префабов с сохранением локальных изменений и проверкой хешей слоев.
- **Interactive Handlers**: Авто-назначение компонентов `Button`, `InputField`, `Toggle`, `ScrollRect`.
- **UPM Package Support**: Полноценная поддержка Unity Package Manager.

---
*Документация и релизы доступны на [GitHub](https://github.com/Menta1ik/figma-to-unity).*
