# Figma Importer v2.1

Мощный инструмент для профессионального импорта UI из Figma в Unity с поддержкой **Smart Sync**, автоматизацией префабов и неразрушающим обновлением.

---

## 📦 Installation (UPM)

This package is designed for the **Unity Package Manager (UPM)**.

### Option A: From Local Disk (Recommended for Dev)
1. Open your Unity project.
2. Go to `Window -> Package Manager`.
3. Click `+` -> **"Add package from disk..."**.
4. Select the `package.json` file inside the `com.figmaimporter.v2` folder.

### Option B: Via Git URL
1. Push this folder to a Git repository.
2. In `Package Manager`, select `+` -> **"Add package from git URL..."**.
3. Paste the repository URL.

### Option C: Manual Installation
Copy the `com.figmaimporter.v2` folder into your project's `Packages/` directory.

---

## 🚀 Быстрый старт (Пошагово)

1.  **Настройка проекта:**
    *   Создайте ассет настроек: `Project -> Create -> Figma Importer -> Settings`.
    *   Создайте таблицу шрифтов: `Project -> Create -> Figma Importer -> Font Mapping Table`.
2.  **Подключение:**
    *   Откройте окно: `Window -> Figma Importer -> V2.1 - Smart Importer`.
    *   Вставьте ваш **Access Token** и **File ID** из Figma.
3.  **Первый импорт:**
    *   Выберите **Root Canvas** (объект в сцене, куда будет идти импорт).
    *   Нажмите **Run Full Sync**. Плагин создаст структуру UI и автоматически сохранит её как Prefab.

---

## 🚀 Quick Start (Step-by-Step)

1.  **Open Dashboard**: Go to `Window -> Figma Importer -> V2.1 - Smart Importer`.
2.  **Configure API**:
    *   Paste your **Figma File URL**.
    *   Paste your **Personal Access Token** (PAT).
3.  **Setup Resources**:
    *   Assign `FigmaImporterSettings` and `FontMappingTable` from your project.
    *   Drag your target **Canvas** from the hierarchy into the **Root Canvas** field.
4.  **Verify Fonts**: Press **Font Audit** and check the console to ensure all used fonts are mapped.
5.  **Run Sync**: 
    *   Enable **Sync Images** if needed.
    *   Press the green **RUN FULL SYNC** button.
6.  **Get Prefab**: Once the sync is done, the system automatically creates a prefab in `Assets/Game/Generated/UI/Prefabs/`.

---

## 🛠 Features
*   **Smart Sync**: Only updates modified elements using state hashing.
*   **Auto-Prefab**: Automatically generates and saves prefabs for top-level UI containers.
*   **Marker System**: Use `[Btn]`, `[Input]`, `[Scroll]`, and `[Toggle]` suffixes in Figma for automatic component assignment.
*   **Non-Destructive**: Preserves your custom scripts and manual components on synced objects.

---

## 🕹 Руководство по интерфейсу

### 1. Connection & Config (Связь)
*   **Figma URL / File ID:** Основной идентификатор вашего дизайна.
*   **Single Node ID:** Оставьте пустым для импорта всего файла или вставьте ID конкретного фрейма (экрана), чтобы обновить только его.
*   **Access Token (PAT):** Ваш личный ключ доступа Figma API.
*   **Importer Settings:** Ссылка на файл с настройками путей и маркеров.

### 2. Resources & Target (Ресурсы)
*   **Font Mapping:** Таблица сопоставления шрифтов Figma с ассетами TextMeshPro.
*   **Root Canvas:** UI-контейнер в сцене (RectTransform).
*   **Кнопка [Font Audit]:** Анализирует Figma-файл и выводит список всех используемых в нем шрифтов. Используйте это для заполнения таблицы маппинга.
*   **Кнопка [Clear Image Cache]:** Очистка временных файлов загрузки.

### 3. Sync & Generate (Синхронизация)
*   **Sync Images:** Если выключено, плагин обновит только текст и структуру (очень быстро). Включите, если нужно обновить спрайты.
*   **Force Update:** По умолчанию выключено (**Smart Sync**). Если включить, плагин проигнорирует хеши и принудительно пересоздаст все объекты и скачает все картинки.
*   **Кнопка [RUN FULL SYNC]:** Основная кнопка запуска. После завершения в папке `Prefabs` (из настроек) появится готовый к работе префаб.

---

## 🛠 Технические стандарты разработки

### Маркеры в Figma
Добавляйте эти суффиксы к названиям слоев в Figma, чтобы плагин автоматически назначил Unity-компоненты:
- `[Btn]` — `UnityEngine.UI.Button`
- `[Input]` — `TMP_InputField`
- `[Scroll]` — `ScrollRect`
- `[Toggle]` — `Toggle`

### Smart Sync и "Паспорта"
На каждом объекте в Unity висит компонент `FigmaElement`.
- **Никогда не удаляйте его**: он хранит уникальный ID, без которого обновление (Sync) превратится в создание дубликатов.
- **Хеширование**: Плагин сравнивает текущее состояние объекта в Figma с сохраненным хешем в `FigmaElement`. Если изменений нет — объект пропускается, что ускоряет импорт в десятки раз.

### Автоматизация Префабов
Система спроектирована так, что вам не нужно вручную создавать префабы из импортированных экранов. Плагин делает это сам после каждой синхронизации, автоматически обновляя файлы в папке `Generated/Prefabs`.

---

## 📝 Решение проблем

- **Тексты не того размера/шрифта:** Убедитесь, что в Figma и в Unity используются совместимые настройки. Проверьте `FontMappingTable`.
- **Картинки "мылятся":** После первого импорта проверьте настройки `Texture Type` у спрайтов в папке `Sprites/Generated`. Плагин ставит стандартные настройки, но их можно подкрутить под требования проекта.
- **Объекты улетают за экран:** Проверьте, что в Figma у фрейма заданы корректные размеры и что масштаб `Canvas` в Unity соответствует дизайну.
---

## 📦 Сборка и распространение (Distribution)

Плагин поставляется как стандартный **UPM-пакет**. Чтобы перенести его в другой проект студии:

1.  **Локально:** В новом проекте откройте `Package Manager`, нажмите `+` -> `Add package from disk` и выберите файл `package.json` в папке плагина.
2.  **Через Git:** Залейте содержимое папки `com.figmaimporter.v2` в отдельный репозиторий. В новом проекте добавьте его через `Add package from git URL`.
3.  **Зависимости:** Пакет автоматически подтянет `TextMeshPro` и `Unity UI`, если они еще не установлены в проекте.

---

**BrainySoft © 2026**

---
**BrainySoft © 2026**
