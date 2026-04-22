# 🌌 Antigravity Figma Importer v2.8.0

**Antigravity Figma Importer** — профессиональный инструмент для «пиксель-в-пиксель» переноса интерфейсов из Figma в Unity uGUI. Плагин использует неразрушающую архитектуру **Smart Sync**, позволяя обновлять UI из Figma без потери ваших изменений (скриптов, анимаций) в Unity.

---

## 🚀 Быстрый старт (UPM)

Самый простой способ установить плагин — использовать Unity Package Manager.

1. Откройте **Window -> Package Manager** в Unity.
2. Нажмите **+** -> **Add package from git URL...**.
3. Вставьте ссылку:
   `https://github.com/Menta1ik/figma-to-unity.git?path=/plugin#v2.8.0`

---

## ⚡ Ключевые особенности v2.8.0 (Production Stability)

### **BATCHING ENGINE (NEW)**
- **Решение ошибки 414 (URI Too Long)**: Внедрена система порционной загрузки изображений. Теперь плагин стабильно импортирует макеты с сотнями и тысячами изображений, разбивая запросы к Figma API на безопасные пакеты.

### **OFFLINE MODE (NEW)**
- **Локальный импорт**: Поддержка режима "Use Local JSON". Импортируйте UI напрямую из локальных файлов (`Assets/lobby_figma.json`) без необходимости подключения к интернету.

### **STABILITY & DIAGNOSTICS**
- **Прозрачные ошибки API**: Все сетевые ошибки (401 Unauthorized, 404 Not Found, 414 URI Too Long) теперь выводятся в консоль Unity с подробным описанием причины.
- **Orchestrator Consolidation**: Полное устранение дублирования логики («Double Brain»). Единый центр управления кэшированием и жизненным циклом в `FigmaParser`.
- **Prefab & Mask Safety**: Гарантированная распаковка префаб-инстансов и безопасный репарентинг при работе с масками.

---

## 📚 Документация

Подробные инструкции находятся в папке `plugin/docs/Developer/`:
- [Руководство разработчика (Setup & Usage)](plugin/docs/Developer/DEVELOPER_MANUAL.md)
- [Техническая концепция и Smart Sync](plugin/docs/Developer/DEV_CONCEPTS.md)
- [Чек-лист для дизайнеров](plugin/docs/Developer/DEV_DESIGNER_GUIDE.md)

---

## 📜 Лицензия

© 2026 **BrainySoftware OU**. Все права защищены.
Разработано для профессионального использования в игровых проектах.
