# 🌌 Figma to Unity Importer v2.4.0 (Stencil Guard Edition)

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

## 🛠 Ключевые особенности v2.4.0 (Performance & Stability)

- **Stencil Mask Guard**: Автоматический контроль вложенности масок (лимит 3 уровня). Предотвращает ошибки Unity "stencil mask depth > 8".
- **Auto RectMask2D Fallback**: Автоматическое переключение на `RectMask2D` при превышении лимита глубины или для простых фигур.
- **Aggressive Hierarchy Flattening**: Умный демонтаж технических контейнеров `[Mask]` при каждой синхронизации для поддержания чистоты иерархии.
- **Global API First**: Плагин переключен на работу через Figma API по умолчанию для бесшовной интеграции.
- **Soft-Delete Mode**: Объекты больше не удаляются физически, а выключаются и помечаются `FigmaOrphanedElement`.
- **Auto Layout Support**: Полная трансляция Figma Auto Layout в Unity `Horizontal/Vertical Layout Group`.
- **Non-Destructive Update**: Сохранение ваших скриптов и анимаций при синхронизации.
- **Batch Image Sync**: Пакетная загрузка ссылок для предотвращения тайм-аутов API.

---

## 📜 Лицензия

© 2026 **BrainySoftware OU**. Все права защищены.
Разработано для профессионального использования в игровых проектах.
