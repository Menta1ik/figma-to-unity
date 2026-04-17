# 🌌 Figma to Unity Importer V2

**Figma-to-Unity Pipeline** — мощный инструмент для «пиксель-в-пиксель» переноса интерфейсов из Figma в Unity uGUI. Плагин использует неразрушающую архитектуру **Smart Sync**, позволяя обновлять UI из Figma без потери ваших изменений в Unity.

---

## 🚀 Быстрый старт (UPM)

Самый простой способ установить плагин — использовать Unity Package Manager.

1. Откройте **Window -> Package Manager** в Unity.
2. Нажмите **+** -> **Add package from git URL...**.
3. Вставьте ссылку:
   `https://github.com/Menta1ik/figma-to-unity.git?path=/plugin`

---

## 📚 Документация

Подробные инструкции находятся в папке `plugin/docs/`:
- [Руководство разработчика (Setup & Usage)](plugin/docs/DEVELOPER_MANUAL_v2.1.md)
- [Техническая концепция и Smart Sync](plugin/docs/CONCEPTS_v2.1.md)
- [Чек-лист для дизайнеров](plugin/docs/FigmaDirectImporter_Manual.md)

---

## 🛠 Ключевые особенности

- **Smart Sync:** Обновляйте макеты, сохраняя ссылки на компоненты и логику в Unity.
- **Auto Layout Support:** Частичная поддержка Figma Auto Layout для создания адаптивных интерфейсов.
- **Font Mapping:** Гибкая таблица соответствия шрифтов Figma и TextMeshPro.
- **Image Caching:** Автоматическое скачивание и оптимизация спрайтов.

---

## 📜 Лицензия

© 2026 **BrainySoftware OU**. Все права защищены.
Разработано для внутреннего использования в проекте **Front-Strike**.
