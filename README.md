# 🌌 Figma to Unity Importer V2.2 (Stable)

**Figma-to-Unity Pipeline** — профессиональный инструмент для «пиксель-в-пиксель» переноса интерфейсов из Figma в Unity uGUI. Плагин использует неразрушающую архитектуру **Smart Sync**, позволяя обновлять UI из Figma без потери ваших изменений (скриптов, анимаций) в Unity.

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
- [Руководство разработчика v2.2 (Setup & Usage)](plugin/docs/DEVELOPER_MANUAL_v2.2.md)
- [Техническая концепция и Smart Sync](plugin/docs/CONCEPTS_v2.2.md)
- [Чек-лист для дизайнеров](plugin/docs/FigmaDirectImporter_Manual.md)

---

## 🛠 Ключевые особенности v2.2

- **Batch Image Sync**: Пакетная загрузка изображений по 25 штук. Больше никаких ошибок "400 Render Timeout" на огромных файлах.
- **Auto-Prefab Pipeline**: Плагин автоматически сохраняет и обновляет префабы в папке `Generated/Prefabs`.
- **Non-Destructive Update**: Полное сохранение ваших имен объектов, скриптов и анимаций при синхронизации.
- **Fault Tolerance**: Изоляция ошибок в хендлерах — одна проблемная нода не ломает весь импорт.
- **Font Mapping**: Гибкая таблица соответствия шрифтов Figma и TextMeshPro с расчетом межстрочного интервала.

---

## 📜 Лицензия

© 2026 **BrainySoftware OU**. Все права защищены.
Разработано для профессионального использования в игровых проектах.
