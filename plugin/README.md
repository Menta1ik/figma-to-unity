# Antigravity Figma Importer v2.8.0 (UPM)

Мощный инструмент для профессионального импорта UI из Figma в Unity с поддержкой **Smart Sync**, автоматизацией префабов и неразрушающим обновлением.

---

## 📦 Installation (UPM)

Этот пакет предназначен для работы с **Unity Package Manager (UPM)**.

### Вариант A: Через Git URL (Рекомендуемый)
1. Откройте ваш проект Unity.
2. Перейдите в `Window -> Package Manager`.
3. Нажмите `+` -> **"Add package from git URL..."**.
4. Вставьте ссылку: `https://github.com/Menta1ik/figma-to-unity.git?path=/plugin#v2.8.0`

---

## ⚡ Новое в v2.8.0 — Production Stability

*   **BATCHING ENGINE**: Внедрена система порционной загрузки изображений. Ошибка **414 URI Too Long** устранена.
*   **OFFLINE MODE**: Полноценная поддержка "Use Local JSON" для импорта из `Assets/lobby_figma.json`.
*   **Improved Diagnostics**: Вывод сетевых ошибок (401, 404, 414) с пояснениями.
*   **Clean Orchestration**: Устранение дублирования логики («Double Brain»).

### Ранее в v2.7.0 — Architecture Decomposition
*   **Модульная архитектура**: FigmaParser разделен на 6 специализированных классов (TreeWalker, MaskResolver, OrphanManager и др.).
*   **Кеширование ответов API**: Хранение JSON-ответов в `Library/FigmaCache/`.
*   **58 Юнит-тестов**: Покрытие логики парсера выросло до 87%.

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

## 📚 Документация
*   [Developer Manual](docs/Developer/DEVELOPER_MANUAL.md) — Полное руководство.
*   [Technical Concepts](docs/Developer/DEV_CONCEPTS.md) — Архитектура и Smart Sync.
*   [Designer Guide](docs/Developer/DEV_DESIGNER_GUIDE.md) — Гайд для дизайнеров.

---

## 📜 Лицензия

© 2026 **BrainySoftware OU**. Все права защищены.
Разработано для профессионального использования в игровых проектах.
