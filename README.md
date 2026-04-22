# 🌌 Figma to Unity Importer v2.7.0

**Figma-to-Unity Pipeline** — профессиональный инструмент для «пиксель-в-пиксель» переноса интерфейсов из Figma в Unity uGUI. Плагин использует неразрушающую архитектуру **Smart Sync**, позволяя обновлять UI из Figma без потери ваших изменений (скриптов, анимаций) в Unity.

---

## 🚀 Быстрый старт (UPM)

Самый простой способ установить плагин — использовать Unity Package Manager.

1. Откройте **Window -> Package Manager** в Unity.
2. Нажмите **+** -> **Add package from git URL...**.
3. Вставьте ссылку:
   `https://github.com/Menta1ik/figma-to-unity.git?path=plugin#v2.7.0`

---

## 📚 Документация

Подробные инструкции находятся в папке `plugin/docs/Developer/`:
- [Руководство разработчика (Setup & Usage)](plugin/docs/Developer/DEVELOPER_MANUAL.md)
- [Техническая концепция и Smart Sync](plugin/docs/Developer/DEV_CONCEPTS.md)
- [Чек-лист для дизайнеров](plugin/docs/Developer/DEV_DESIGNER_GUIDE.md)

---

## 🛠 Ключевые особенности v2.7.0

### Новое в v2.7.0 — Architecture Decomposition & API Caching
- **Модульная архитектура**: FigmaParser декомпозирован из God Object (647 строк) в 6 фокусированных классов (-45% строк). Каждый класс — одна ответственность.
- **API Response Caching**: Кеширование ответов Figma API в `Library/FigmaCache/`. Повторный sync без изменений в Figma — мгновенный (кеш-хит вместо сетевого запроса).
- **Централизованная версия**: Единая константа `FigmaImporter.Version` вместо 15+ хардкод-строк по всему коду.
- **58 юнит-тестов**: Покрытие выросло на 87% (было 31). Все тесты переведены с рефлексии на прямые вызовы.
- **Рабочая кнопка Clear Cache**: Очистка кеша API-ответов прямо из UI.

### Из v2.6.0
- **Metadata Unblock Edition**: Исправлен .gitignore, блокировавший загрузку критических .meta файлов в Unity.
- **Система логирования (FigmaLog)**: Три уровня вывода (`Silent` / `Minimal` / `Verbose`) через настройки — консоль больше не захламляется при импорте сотен нод.
- **Настраиваемый масштаб изображений**: Слайдер `Image Export Scale` (0.5–4x) в настройках и окне импортера. По умолчанию 2x вместо хардкодного 3x.
- **Fill Container (layoutGrow)**: Поддержка Figma Auto Layout "Fill Container" — автоматическая трансляция в `LayoutElement.flexibleWidth/Height`.
- **Безопасность токена**: Access Token хранится в `SessionState` — переживает перезагрузку домена, но никогда не попадает на диск.
- **Deep Adaptive Layout**: Полная трансляция Figma Constraints (`Left`, `Right`, `Center`, `Stretch`, `Scale`) в Unity Anchors/Offsets.
- **Stencil Mask Guard**: Автоматический контроль вложенности масок (лимит 3 уровня).
- **Auto Layout Support**: Полная трансляция Figma Auto Layout в Unity `Horizontal/Vertical Layout Group`.
- **Non-Destructive Update**: Сохранение ваших скриптов и анимаций при синхронизации.

### 📱 Adaptive Layout (v2.7.0)
Плагин поддерживает продвинутое масштабирование и перенос констрейнтов:
- **Enable Constraints Translation**: Автоматический маппинг Figma constraints в анкоры `RectTransform`.
- **Canvas Scale Mode**: Режим масштабирования (`Scale With Screen Size`) настраивается автоматически.
- **Match Width or Height**: Интеллектуальный расчет параметра под разрешение фрейма. 
- **Reference Resolution**: Математически точное определение разрешения из Figma.

---

## 📜 Лицензия

© 2026 **BrainySoftware OU**. Все права защищены.
Разработано для профессионального использования в игровых проектах.
