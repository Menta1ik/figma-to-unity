# Figma Importer v2.7.1 (UPM)

Мощный инструмент для профессионального импорта UI из Figma в Unity с поддержкой **Smart Sync**, автоматизацией префабов и неразрушающим обновлением.

---

## 📦 Installation (UPM)

This package is designed for the **Unity Package Manager (UPM)**.

### Option A: Via Git URL (Recommended)
1. Open your Unity project.
2. Go to `Window -> Package Manager`.
3. Click `+` -> **"Add package from git URL..."**.
4. Paste: `https://github.com/Menta1ik/figma-to-unity.git?path=plugin#v2.7.1`

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

## 🛠 New in v2.7.0 — Architecture Decomposition & API Caching

*   **Modular Architecture**: FigmaParser decomposed from God Object (647 lines) into 6 focused classes (-45% code). Single responsibility per class.
    *   `FigmaTreeWalker` — recursive node sync and hierarchy building
    *   `FigmaMaskResolver` — mask container lifecycle (dismantle/apply/cleanup)
    *   `FigmaOrphanManager` — deleted element detection
    *   `FigmaFontAuditor` — font mapping audit
    *   `FigmaParserUtils` — shared utilities
*   **API Response Caching**: File-based cache in `Library/FigmaCache/`. Version-aware — skips full API download when Figma file unchanged. Instant re-sync on cache hit.
*   **Centralized Version**: Single `FigmaImporter.Version` constant replaces 15+ hardcoded version strings.
*   **58 Unit Tests**: Coverage grew +87% (was 31). All tests migrated from reflection to direct calls.
*   **Working Clear Cache Button**: Actually clears API response cache from UI (was a stub).

### Previous (v2.6.0)
*   Metadata Unblock (.gitignore fix), Logging System (FigmaLog), Configurable Image Scale, Fill Container, Token Security.

### Previous (v2.5.0)
*   Service-Oriented Architecture, Icon Detection Cache, Deep Diagnostics, Unit Tests.
*   Soft-Delete, Auto Layout, Parallel Loading, Batch Processing, Stencil Guard.

---

## 📚 Documentation
*   [Developer Manual](docs/Developer/DEVELOPER_MANUAL.md) — Полное руководство по настройке и API.
*   [Technical Concepts](docs/Developer/DEV_CONCEPTS.md) — Описание архитектуры и системы "паспортов".
*   [Designer Guide](docs/Developer/DEV_DESIGNER_GUIDE.md) — Инструкции для дизайнеров (маркеры, нейминг).
*   [Knowledge Base](docs/index.md) — Основной индекс документации.

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
