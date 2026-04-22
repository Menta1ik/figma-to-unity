# Antigravity Figma Importer v2.7.7 (UPM)

Мощный инструмент для профессионального импорта UI из Figma в Unity с поддержкой **Smart Sync**, автоматизацией префабов и неразрушающим обновлением.

---

## 📦 Installation (UPM)

Этот пакет предназначен для работы с **Unity Package Manager (UPM)**.

### Вариант A: Через Git URL (Рекомендуемый)
1. Откройте ваш проект Unity.
2. Перейдите в `Window -> Package Manager`.
3. Нажмите `+` -> **"Add package from git URL..."**.
4. Вставьте ссылку: `https://github.com/Menta1ik/figma-to-unity.git?path=/plugin#v2.7.7`

### Вариант B: Локальная установка
Выберите файл `package.json` внутри папки `plugin`.

---

## 🚀 Быстрый старт (Пошагово)

1.  **Открыть Dashboard**: `Window -> Figma Importer -> Sync & Reskin Dashboard`.
2.  **Настроить API**:
    *   Вставьте **Figma File URL** или **File ID**.
    *   Введите ваш **Personal Access Token** (PAT).
3.  **Подготовить ресурсы**:
    *   Назначьте ассеты `FigmaImporterSettings` и `FontMappingTable`.
    *   Перетащите целевой **Canvas** из иерархии в поле **Root Canvas**.
4.  **Запустить синхронизацию**: 
    *   Нажмите зеленую кнопку **RUN FULL SYNC**.
5.  **Получить префаб**: Система автоматически создаст или обновит префаб в папке, указанной в настройках.

---

## 🛠 Новое в v2.7.7 — UI Stability & UPM Fix

*   **Полная стабилизация IMGUI**: Ошибки `Invalid GUILayout state` устранены во всех редакторских окнах через внедрение Scopes (`VerticalScope`, `HorizontalScope`, `ScrollViewScope`).
*   **Исправлен авто-апдейт**: Метод обновления плагина в один клик теперь корректно находит директорию пакета в GitHub.
*   **Ребрендинг**: Проект полностью переименован в **Antigravity Figma Importer**.

### Ранее в v2.7.0 — Architecture Decomposition & API Caching
*   **Модульная архитектура**: FigmaParser разделен на 6 специализированных классов (TreeWalker, MaskResolver, OrphanManager и др.).
*   **Кеширование ответов API**: Хранение JSON-ответов в `Library/FigmaCache/`. Моментальный повторный синк при отсутствии правок в Figma.
*   **58 Юнит-тестов**: Покрытие логики парсера выросло до 87%.

---

## 📚 Документация
*   [Developer Manual](docs/Developer/DEVELOPER_MANUAL.md) — Полное руководство.
*   [Technical Concepts](docs/Developer/DEV_CONCEPTS.md) — Архитектура и Smart Sync.
*   [Designer Guide](docs/Developer/DEV_DESIGNER_GUIDE.md) — Гайд для дизайнеров.

---

## 📜 Лицензия

© 2026 **BrainySoftware OU**. Все права защищены.
Разработано для профессионального использования в игровых проектах.
