# Figma Importer v2.2 (UPM)

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

## 🛠 New in v2.2
*   **Batch Image Processing**: Reliable download for large files (chunks of 25).
*   **Auto-Prefab Pipeline**: Instant prefab saving/connection after sync.
*   **Non-Destructive Policy**: Preserves manual names, components, and animations.
*   **Enhanced Fault Tolerance**: Isolated handler errors and NRE protection.

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
