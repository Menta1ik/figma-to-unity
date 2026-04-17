# 🎨 Figma Direct Importer v2.1.0 (uGUI)

Бесшовный перенос UI из **Figma** в **Unity** (uGUI) с поддержкой умной синхронизации, автоматической генерации префабов и настройки интерактивных компонентов.

---

## 🚀 Основные возможности v2.1.0
- **Smart Sync**: Обновляйте существующие префабы без потери данных. Система отслеживает хеши и Node ID.
- **Auto-Prefab Mode**: Создает готовые UI-префабы из Figma-фреймов нажатием одной кнопки.
- **Interactive Pipeline**: Автоматическое назначение компонентов `Button`, `InputField`, `Toggle`, `ScrollRect` по суффиксам в именах.
- **Local/Cloud Image Sync**: Продвинутый кэшинг и загрузка графики в Retina 3x.
- **UX Localization**: Интерфейс плагина полностью на английском для удобства команд.

## 📦 Установка (UPM)
Этот инструмент распространяется как стандартный **Unity Package Manager (UPM)** пакет.

### Способ 1: Git URL (Рекомендуемый)
1. Откройте **Unity Project Browser** -> **Package Manager**.
2. Нажмите `+` -> **Add package from git URL...**
3. Вставьте ссылку: `https://github.com/Menta1ik/figma-to-unity.git?path=/Game/Packages/com.figmaimporter.v2`

### Способ 2: Local Disk
1. Клонируйте репозиторий.
2. В Package Manager выберите **Add package from disk...** и укажите файл `package.json` в папке `Game/Packages/com.figmaimporter.v2`.

---

## 📖 Документация
Подробные руководства по работе с новой версией доступны в папке `docs/`:
- 📕 **[Developer Manual (v2.1)](docs/DEVELOPER_MANUAL_v2.1.md)** — Пошаговая настройка и использование.
- 🗺 **[Roadmap Development](docs/ROADMAP_v2.1_Development.md)** — Статус проекта и планы.
- 💡 **[Concepts](docs/CONCEPTS_v2.1.md)** — Архитектурные принципы плагина.

## 🛠 Требования
- **Unity 2021.3 (LTS)** или выше.
- **TextMesh Pro** (установлен через Package Manager).
- **Newtonsoft.Json** (автоматически подтягивается как зависимость).

---
*Разработано BrainySoftware OU. Версия 2.1.0 "Stable"*
