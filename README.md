# 🌌 Antigravity Figma Importer v2.7.10

**Antigravity Figma Importer** — профессиональный инструмент для «пиксель-в-пиксель» переноса интерфейсов из Figma в Unity uGUI. Плагин использует неразрушающую архитектуру **Smart Sync**, позволяя обновлять UI из Figma без потери ваших изменений (скриптов, анимаций) в Unity.

---

## 🚀 Быстрый старт (UPM)

Самый простой способ установить плагин — использовать Unity Package Manager.

1. Откройте **Window -> Package Manager** в Unity.
2. Нажмите **+** -> **Add package from git URL...**.
3. Вставьте ссылку:
   `https://github.com/Menta1ik/figma-to-unity.git?path=/plugin#v2.7.10`

---

## 📚 Документация

Подробные инструкции находятся в папке `plugin/docs/Developer/`:
- [Руководство разработчика (Setup & Usage)](plugin/docs/Developer/DEVELOPER_MANUAL.md)
- [Техническая концепция и Smart Sync](plugin/docs/Developer/DEV_CONCEPTS.md)
- [Чек-лист для дизайнеров](plugin/docs/Developer/DEV_DESIGNER_GUIDE.md)

---

## 🛠 Ключевые особенности v2.7.10 (Stability Update)

### Новое в v2.7.10 — UI Stability & UPM Fix
- **Стабилизация OnGUI**: Весь интерфейс переведен на `EditorGUILayout.VerticalScope` и `HorizontalScope`. Ошибки `Invalid GUILayout state` и разбалансировка стека IMGUI полностью устранены.
- **Исправление UPM Update**: Скорректирован путь в методе автоматического обновления (`?path=/plugin`). Теперь кнопка "Update" в окне импорта работает корректно в Unity 2021.3+.
- **Глобальный ребрендинг**: Проект официально переименован в **Antigravity Figma Importer**.

### Новое в v2.7.10 — Prefab Stability
- **Исправление блокировки префабов**: Устранена критическая ошибка `Setting the parent of a transform which resides in a Prefab instance is not possible`. Теперь плагин безопасно распаковывает иерархию перед синхронизацией.

### Новое в v2.7.0 — Architecture Decomposition & API Caching
- **Модульная архитектура**: FigmaParser декомпозирован из God Object в 6 фокусированных классов.
- **API Response Caching**: Кеширование ответов Figma API в `Library/FigmaCache/`. Повторный sync без изменений в Figma — мгновенный.

---

## 📜 Лицензия

© 2026 **BrainySoftware OU**. Все права защищены.
Разработано для профессионального использования в игровых проектах.
