# Figma Importer v2.3.0 (UPM)

Мощный инструмент для профессионального импорта UI из Figma в Unity с поддержкой **Smart Sync**, автоматизацией префабов и неразрушающим обновлением.

---

## 📦 Installation (UPM)

This package is designed for the **Unity Package Manager (UPM)**.

### Option A: Via Git URL (Recommended)
1. Open your Unity project.
2. Go to `Window -> Package Manager`.
3. Click `+` -> **"Add package from git URL..."**.
4. Paste: `https://github.com/Menta1ik/figma-to-unity.git?path=/plugin`

### Option B: Local Disk
Select the `package.json` file inside the `plugin` folder.

---

## 🚀 Quick Start (Step-by-Step)

1.  **Open Dashboard**: Go to `Window -> Figma Importer -> Dashboard`.
2.  **Configure API**:
    *   Paste your **Figma File URL** or **File ID**.
    *   Paste your **Personal Access Token** (PAT).
3.  **Setup Resources**:
    *   Assign `FigmaImporterSettings` and `FontMappingTable`.
    *   Drag your target **Canvas** from hierarchy into the **Root Canvas** field.
4.  **Run Sync**: 
    *   Press the green **RUN FULL SYNC** button.
5.  **Get Prefab**: The system automatically creates/updates a prefab in `Assets/UI/Generated/Prefabs/`.

---

## 🛠 New in v2.3.0 (Production Hardening)
*   **Service-Oriented Architecture**: Рефакторинг ядра (выделены `ImageSyncService`, `PrefabManager`), повышающий стабильность и расширяемость.
*   **Icon Detection Cache**: Оптимизация производительности в `ImageHandler` (кэширование кандидатов на иконки), решающая проблему больших макетов.
*   **Deep Diagnostics**: Улучшенный `FigmaHealthCheck` с проверкой целостности `asmdef`, версий и архитектурных связей.
*   **Reliable Unit Tests**: Добавлен набор тестов для верификации логики рескина и кэширования.

## 🛠 New in v2.2.5 (Hardening)
*   **Soft-Delete**: Prevent data loss by marking old elements instead of deleting them.
*   **Auto Layout**: Support for horizontal/vertical layouts and content fit.
*   **9-Slice**: Automatic sprite configuration for `_9slice` layers.
*   **Parallel Loading**: Multithreaded image sync for better performance.
*   **Font Safety**: Strict validation for fallback fonts.
*   **Batch Image Processing**: Reliable download for large files (chunks of 25).

---

## 🕹 Руководство по интерфейсу

### 1. Connection & Config
*   **Figma URL / File ID:** Ссылка на макет.
*   **Single Node ID:** Обновление только одного конкретного экрана.
*   **Access Token (PAT):** Ваш ключ API.

### 2. Resources & Target
*   **Font Mapping:** Таблица шрифтов TextMeshPro.
*   **Root Canvas:** Место сборки в сцене.

### 3. Sync & Generate
*   **Smart Sync**: Обновляет только то, что изменилось.
*   **Force Update**: Принудительный полный перебор всех элементов.

---

## 📜 Лицензия

© 2026 **BrainySoftware OU**. Все права защищены.
Разработано для внутреннего использования.
