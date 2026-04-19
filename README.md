# 🌌 Figma to Unity Importer V2.3.1 (Stable & Hardening)

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

Подробные инструкции находятся в папке `plugin/docs/Developer/`:
- [Руководство разработчика (Setup & Usage)](plugin/docs/Developer/DEVELOPER_MANUAL.md)
- [Техническая концепция и Smart Sync](plugin/docs/Developer/DEV_CONCEPTS.md)
- [Чек-лист для дизайнеров](plugin/docs/Developer/DEV_DESIGNER_GUIDE.md)

---

## 🛠 Ключевые особенности v2.3.1 (Stable)

- **Soft-Delete Mode**: Объекты больше не удаляются физически, а выключаются и помечаются `FigmaOrphanedElement`.
- **Auto Layout Support**: Полная трансляция Figma Auto Layout в Unity `Horizontal/Vertical Layout Group`.
- **9-Slice Automation**: Авто-настройка границ спрайтов через суффикс `_9slice`.
- **Parallel Image Sync**: Многопоточная загрузка изображений для максимальной скорости.
- **Batch Image Sync**: Пакетная загрузка ссылок для предотвращения тайм-аутов API.
- **Auto-Prefab Pipeline**: Автоматическое сохранение и обновление префабов.
- **Non-Destructive Update**: Сохранение ваших скриптов и анимаций при синхронизации.
- **Font Mapping**: Гибкая таблица шрифтов с блокирующей валидацией ошибок.

---

## 📜 Лицензия

© 2026 **BrainySoftware OU**. Все права защищены.
Разработано для профессионального использования в игровых проектах.
