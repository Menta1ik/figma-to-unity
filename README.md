# 🌌 Figma to Unity Importer v2.4.1 (Deep Constraints Edition)

**Figma-to-Unity Pipeline** — профессиональный инструмент для «пиксель-в-пиксель» переноса интерфейсов из Figma в Unity uGUI. Плагин использует неразрушающую архитектуру **Smart Sync**, позволяя обновлять UI из Figma без потери ваших изменений (скриптов, анимаций) в Unity.

---

## 🚀 Быстрый старт (UPM)

Самый простой способ установить плагин — использовать Unity Package Manager.

1. Откройте **Window -> Package Manager** в Unity.
2. Нажмите **+** -> **Add package from git URL...**.
3. Вставьте ссылку:
   `https://github.com/Menta1ik/figma-to-unity.git?path=plugin#v2.4.1`

---

## 📚 Документация

Подробные инструкции находятся в папке `plugin/docs/Developer/`:
- [Руководство разработчика (Setup & Usage)](plugin/docs/Developer/DEVELOPER_MANUAL.md)
- [Техническая концепция и Smart Sync](plugin/docs/Developer/DEV_CONCEPTS.md)
- [Чек-лист для дизайнеров](plugin/docs/Developer/DEV_DESIGNER_GUIDE.md)

---

## 🛠 Ключевые особенности v2.4.1

- **Deep Adaptive Layout**: Полная трансляция Figma Constraints (`Left`, `Right`, `Center`, `Stretch`, `Scale`) в Unity Anchors/Offsets. Покрыто тестами.
- **Stencil Mask Guard**: Автоматический контроль вложенности масок (лимит 3 уровня). Предотвращает ошибки Unity "stencil mask depth > 8".
- **Auto RectMask2D Fallback**: Автоматическое переключение на `RectMask2D` при превышении лимита глубины или для простых фигур.
- **Aggressive Hierarchy Flattening**: Умный демонтаж технических контейнеров `[Mask]` при каждой синхронизации.
- **Global API First**: Плагин оптимизирован для работы через Figma API.
- **Soft-Delete Mode**: Объекты помечаются `FigmaOrphanedElement` вместо физического удаления.
- **Auto Layout Support**: Полная трансляция Figma Auto Layout в Unity `Horizontal/Vertical Layout Group`.
- **Non-Destructive Update**: Сохранение ваших скриптов и анимаций при синхронизации.

### 📱 Adaptive Layout (v2.4.1)
Плагин поддерживает продвинутое масштабирование и перенос констрейнтов:
- **Enable Constraints Translation**: Автоматический маппинг Figma constraints в анкоры `RectTransform`.
- **Canvas Scale Mode**: Режим масштабирования (`Scale With Screen Size`) настраивается автоматически.
- **Match Width or Height**: Интеллектуальный расчет параметра под разрешение фрейма. 
- **Reference Resolution**: Математически точное определение разрешения из Figma.

---

## 📜 Лицензия

© 2026 **BrainySoftware OU**. Все права защищены.
Разработано для профессионального использования в игровых проектах.
