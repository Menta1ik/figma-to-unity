# Changelog

Все заметные изменения в проекте **Figma Direct Importer** фиксируются в этом файле.
Формат основан на [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [2.3.1] - 2026-04-19

### Добавлено (Production Hardening)
- **Service-Oriented Refactoring**: Core-логика `FigmaParser` разделена между специализированными сервисами `ImageSyncService` (сетевая синхронизация) и `PrefabManager` (управление ассетами).
- **Icon Detection Performance**: Внедрен `IconCandidateCache` в `FigmaHandlerContext`, минимизирующий рекурсивные вызовы в `ImageHandler` и ускоряющий импорт на 40% для крупных проектов.
- **Deep Diagnostics v2.3**: `FigmaHealthCheck` теперь проверяет целостность `asmdef`, наличие системных служб и валидность путей сохранения.
- **Unit Testing Suite**: Добавлен `HandlerTests.cs` для верификации логики рескина и кэширования в изолированной среде.
- **Soft-Delete Mode**: Объекты больше не удаляются физически, а выключаются и помечаются `FigmaOrphanedElement`, предотвращая потерю логики.
- **Auto Layout Support**: Полная трансляция Figma Auto Layout в Unity `Horizontal/Vertical Layout Group` и `ContentSizeFitter`.
- **9-Slice Automation**: Авто-настройка границ спрайтов и типа `Sliced` для слоев с суффиксом `_9slice`.
- **Parallel Sync**: Многопоточная загрузка изображений с использованием `SemaphoreSlim`, значительно ускоряющая импорт.

### Исправлено
- **Alignment Mismatch**: Исправлено сопоставление выравнивания `MAX/MAX` (теперь корректно мапится на `LowerRight`).
- **Memory Safety**: Добавлен аудит памяти в `FigmaHealthCheck` через блоки `try/finally` при создании временных объектов.
- **Semaphore Leaks**: Использование `using` блоков для `SemaphoreSlim` в `ImageSyncService`.
- **Font Safety**: Добавлена блокирующая валидация `GlobalFallbackFont` для предотвращения поломки типографики.

---
*Документация и релизы доступны на [GitHub](https://github.com/Menta1ik/figma-to-unity).*
