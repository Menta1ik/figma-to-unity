# Changelog

Все заметные изменения в проекте **Figma Direct Importer** будут фиксироваться в этом файле.

Формат основан на [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
и проект придерживается [Семантического Версионирования](https://semver.org/spec/v2.0.0.html).

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
