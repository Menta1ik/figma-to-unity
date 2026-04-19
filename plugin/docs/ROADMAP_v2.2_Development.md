# 🗺️ Roadmap: Разработка Figma Direct Importer v2.2.5 (Hardening)

Этот документ фиксирует стратегию разработки профессиональной модульной системы.

## 🚀 Стратегия разработки
1.  **Git Flow:** Вся разработка ведется в ветке `main`.
2.  **Технический стек:** Строго **uGUI** (Canvas System).
3.  **Изоляция (Namespaces):** Весь код оборачивается в пространство имен `FigmaImporter.V2`.

---

## 🛠 Ключевые этапы (Спринты)

### Спринт 1-5: Базовая архитектура (ЗАВЕРШЕНО ✅)
*   [x] Миграция в UPM Package.
*   [x] Smart Sync (обновление без удаления).
*   [x] Typography & Font Mapping.
*   [x] Interactive Handlers (`[Btn]`, `[Input]`, etc.).

### Спринт 6: Оптимизация и Надежность (ЗАВЕРШЕНО ✅)
*   [x] **Image Batching**: Пакетная загрузка изображений по 25 штук для обхода таймаутов Figma API.
*   [x] **Auto-Prefab**: Автоматическое сохранение/обновление префабов после синхронизации.
*   [x] **Fault Tolerance**: Изоляция ошибок в хендлерах (try-catch) и защита от NullReferenceException.
*   [x] **Prefab Protection**: Запрет на принудительное изменение структуры экземпляров префабов.
*   [x] **Visibility Restoration**: Автоматическое восстановление прозрачности после загрузки спрайтов.

### Спринт 7: Расширенная верстка (В ПЛАНЕ 🕒)
*   [x] **Auto Layout Support**: Поддержка Figma Vertical/Horizontal Layout через Unity `VerticalLayoutGroup` и `HorizontalLayoutGroup`.
*   [ ] **Shadows & Effects**: Базовая поддержка теней через `Shadow` или `Outline`.
*   [ ] **Atlas Optimizer**: Автоматическая сборка скачанных спрайтов в Unity Sprite Atlas.

---

## 🎉 СТАТУС РЕЛИЗА
**Версия 2.2.5 (Hardening)** — Релиз выпущен `19.04.2026`. Инструмент включает в себя Auto Layout, 9-Slice, Soft-Delete и параллельную синхронизацию.

---
*Документ утвержден. Релиз 2.2.5 — ВЫПУЩЕН.*
