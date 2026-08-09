# 📑 Технический аудит: План перехода к v3.0 (Antigravity Figma Importer)

Этот документ содержит результаты анализа кодовой базы v2.7.1 и определяет фронт работ для следующего мажорного релиза v3.0.

## 🎯 Цели аудита
- Выявление архитектурных монолитов (God Objects).
- Оценка производительности (GC Allocations, Memory Leaks).
- Анализ UX-барьеров и стабильности синхронизации.
- Проверка соответствия стандартам Antigravity OS.

---

## 🏗️ Архитектурные риски (Phase 1)

### 1. Декомпозиция UI-Оркестратора
- **Файлы:** `plugin/Editor/UI/FigmaImporterWindow.cs` и `plugin/Editor/Core/FigmaParser.cs`.
- **Проблема:** "Раздвоенный мозг". Логика синхронизации (`RunSync`) дублируется и в окне, и в парсере. Окно не использует преимущества кэширования, реализованные в парсере.
- **Задача v3.0:** 
    - Полностью перенести логику сетевых запросов и кэширования из `FigmaParser` в новый `FigmaImportOrchestrator`.
    - Окно должно стать "тонким" (Thin Client), делегируя всё выполнение оркестратору.
    - Переименовать текущий `FigmaParser` в `FigmaImportOrchestrator`, так как он управляет жизненным циклом (Masks, Orphans, Prefabs), а не только парсингом.

### 2. Формализация Import Pipeline
- **Проблема:** Дублирование кода подготовки в `ProcessFileAsync` и `ReskinAsync`.
- **Задача v3.0:** Создать модульный конвейер (Connect -> Fetch -> Parse -> Build -> Cleanup), где каждый этап — это отдельный тестируемый шаг.

### 3. Ревизия рекурсивного обхода (TreeWalker)
- **Файл:** `plugin/Editor/Core/FigmaTreeWalker.cs`.
- **Проблема:** Рекурсия может быть опасна на экстремально больших макетах Figma (Deep Hierarchy).
- **Задача v3.0:** Исследовать переход на итеративный подход или оптимизировать хвостовую рекурсию.

---

## ⚡ Перформанс и Стабильность (Phase 2)

### 1. Борьба с Garbage Collector (GC)
- **Фокус:** Аллокации при парсинге JSON и создании объектов Unity.
- **Инструмент:** Profiler & Memory Snapshots.

### 2. Оптимизация Prefab Lifecycle
- **Задача:** Сделать обновление префабов максимально инкрементальным, не затрагивая неизмененные ветки.

---

## 🎨 UX & Дизайн (Phase 3)
- **Цель:** Исключить ощущение "черного ящика" при импорте.
- **Задача:** Добавить визуальные превью, расширенный лог ошибок и индикацию прогресса.

---

## 🔭 Unity 7: влияние на проект

Сроки: бета — декабрь 2026, релиз — Q1 2027. Unity обещает миграцию с Unity 6 без пересборки проекта, без нового языка скриптинга, без сломанных workflow. Плагин таргетится на 2021.3 LTS+ и должен продолжить работать без правок.

На момент анонса (Unite Seoul) **нет конкретики** по uGUI, TextMeshPro, Package Manager, Editor Scripting API, Prefab workflow — то есть по всему, чем пользуется наш код. Предметно готовиться пока не к чему.

Из объявленного релевантно нам:
1. **CoreCLR-рантайм → быстрый domain reload** (перекомпилируются только изменённые скрипты) и почти мгновенный вход в Play Mode — ускоряет разработку самого плагина (частая рекомпиляция `FigmaParser`/хендлеров), не фича для конечного пользователя.
2. **Новый CLI + публичный API** для валидации ассетов и сборок без открытия полного Editor — потенциально закрывает то, что у нас нет CI (см. ниже): если API позволит гонять Editor-операции headless, можно будет автоматизировать «Figma запушили дизайн → CLI дёрнул Unity → пересобрал префабы» без ручного клика по кнопке в окне плагина.
3. **Нативная поддержка MCP** (Model Context Protocol) для AI-агентов, opt-in — направление совпадает с локальными MCP-утилитами в корне репо (`figma_mcp.py`, `mcp-config.json`); в перспективе агент сможет управлять синком прямо из Unity.

**Рекомендация:** ничего не менять сейчас, вернуться к вопросу при выходе беты (~декабрь 2026) и сверить migration notes конкретно по UGUI/Package Manager/Scripting API перед следующим мажорным релизом.

Источники: [Unity 7 Roadmap Revealed at Unite Seoul](https://unity.com/news/unity-7-roadmap-revealed-at-unite-seoul), [Unity unveils Unity 7 roadmap with update path that won't break your build](https://www.gamedeveloper.com/programming/unity-unveils-unity-7-roadmap-with-update-path-that-won-t-break-your-build), [Unity 7 Roadmap Includes Up to 90% Faster Shader Builds](https://shattered.io/unity-7-roadmap-2026/), [Unity Announces 'Unity 7' Roadmap at Unite Seoul 2026](https://www.invenglobal.com/articles/23986/unity-announces-unity-7-roadmap-at-unite-seoul-2026).

---

## 🩺 Находки код-ревью (сессия 2026-08-09)

Конкретные, проверенные в коде пункты — не гипотезы.

### 1. `[Scroll]` и `[Input]` маркеры реализованы не до конца
- **Файл:** `plugin/Editor/Core/Handlers/InteractiveHandler.cs:35-54`.
- **Проблема:** на `[Scroll]` вешается голый `ScrollRect` без `content`/`viewport` и без `Mask`/`RectMask2D`; на `[Input]` — голый `TMP_InputField` без `.textComponent`/`.placeholder`/`.textViewport`. При этом `DEV_DESIGNER_GUIDE.md` обещает «`[Scroll]` — настроит ScrollRect и Mask». По факту дизайнер ставит маркер и получает нерабочий каркас, который всё равно нужно доводить руками в Unity.
- **Задача:** автогенерировать нужную дочернюю структуру (Viewport+Content для ScrollRect, Text Area+Placeholder для TMP_InputField), либо явно задокументировать, что это только заготовка.

### 2. Нет retry/backoff на запросах к Figma API
- **Файлы:** `plugin/Editor/Core/FigmaAPIClient.cs` (`ExecuteRequest`), `plugin/Editor/Core/Services/ImageSyncService.cs` (параллельный батчинг).
- **Проблема:** при ошибке (в т.ч. вероятном 429 при 5 параллельных батчах) просто логируется ошибка и батч молча теряется. Ровно тот сценарий, где transient-сбои наиболее вероятны — плагин прямо рекламирует импорт файлов с 23,000+ нодами.
- **Задача:** минимум 2-3 попытки с экспоненциальной паузой на 429/5xx.

### 3. Устаревшая версия в User-Agent
- **Файл:** `plugin/Editor/Core/FigmaAPIClient.cs:72` — `"Unity-Figma-Importer/2.5.7"`.
- **Проблема:** забыт при рефакторинге v2.7.0, централизовавшем версию через `FigmaImporter.Version`.
- **Задача:** заменить на `$"Unity-Figma-Importer/{FigmaImporter.Version}"`.

### 4. Нет CI
- **Проблема:** ~67 NUnit-тестов (`plugin/Tests/Editor/`) гоняются только вручную через Unity Test Runner. Череда хотфиксов v2.7.0 → v2.7.10 за один день — симптом отсутствия защитной сетки перед релизом.
- **Задача:** добавить GitHub Actions workflow (например, `game-ci/unity-test-runner`), гоняющий Editor-тесты на каждый PR/push.

### 5. Пробелы в тестах
- **Проблема:** `ImageSyncService`, `FigmaAPIClient`, `InteractiveHandler`, `PrefabManager` не покрыты вообще. Существующие тесты (`DecompositionTests`, `CacheTests`, `ConstraintsMappingTests`, `AdaptiveLayoutTests`, `HandlerTests`) фокусируются на Transform/Layout/Text/Reskin. Особенно не хватает тестов на конкурентную логику батчинга (семафоры, частичный отказ батча).
- **Задача:** добавить тесты на семафор-throttling и обработку частичных сбоев батча в `ImageSyncService`.

### 6. Editor UI на 100% IMGUI
- **Проблема:** все 5 Editor UI файлов используют `OnGUI`/`EditorGUILayout`, ни одного `UIElements`/`VisualElement`. Не срочно — IMGUI полностью поддерживается, Unity 7 её не депрекейтит.
- **Задача (по желанию, не приоритет):** при желании сделать более продвинутый Dashboard (прогресс-бары, дерево diff'ов синка) — переходить на UI Toolkit.

---
*Статус: В работе (Инициировано 22.04.2026, дополнено 09.08.2026)*
*Кураторы: Cloud Dragonborn (🏛️), Link Freeman (🕹️), Paige (📚)*
