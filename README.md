# 🌌 Antigravity Figma Importer v2.8.2

[![Version](https://img.shields.io/badge/version-2.8.2-blue)](plugin/CHANGELOG.md)
[![Unity](https://img.shields.io/badge/Unity-2021.3%2B%20LTS-black?logo=unity)](https://unity.com/)
[![C#](https://img.shields.io/badge/C%23-.NET%20Standard%202.1-239120?logo=csharp&logoColor=white)](plugin/Editor)
[![License](https://img.shields.io/badge/license-Proprietary-red)](LICENSE)

**Antigravity Figma Importer** — профессиональный инструмент для «пиксель-в-пиксель» переноса интерфейсов из Figma в Unity uGUI. Плагин использует неразрушающую архитектуру **Smart Sync**, позволяя обновлять UI из Figma без потери ваших изменений (скриптов, анимаций) в Unity.

---

## 🚀 Быстрый старт (UPM)

Самый простой способ установить плагин — использовать Unity Package Manager.

1. Откройте **Window -> Package Manager** в Unity.
2. Нажмите **+** -> **Add package from git URL...**.
3. Вставьте ссылку:
   `https://github.com/Menta1ik/figma-to-unity.git?path=/plugin#v2.8.2`

---

## ⚡ Ключевые особенности v2.8.2 (Turbo Batching)

### **TURBO BATCHING ENGINE (NEW)**
- **Решение ошибки 414 & Оптимизация**: Плагин автоматически разбивает запросы на порции по 100 объектов и обрабатывает их в 5 параллельных потоков.
- **Производительность**: Скорость подготовки импорта для файлов с 10,000+ объектов выросла на 90%. Подтверждена стабильная работа на макетах с **23,000+ нодами**.
- **Thread Safety**: Полная совместимость с Unity Main Thread.

### **OFFLINE MODE**
- **Локальный импорт**: Поддержка режима "Use Local JSON". Импортируйте UI напрямую из локальных файлов (`Assets/lobby_figma.json`) без необходимости подключения к интернету.

### **STABILITY & DIAGNOSTICS**
- **Прозрачные ошибки API**: Все сетевые ошибки (401 Unauthorized, 404 Not Found, 414 URI Too Long) теперь выводятся в консоль Unity с подробным описанием причины.
- **Orchestrator Consolidation**: Полное устранение дублирования логики («Double Brain»). Единый центр управления кэшированием и жизненным циклом в `FigmaParser`.
- **Prefab & Mask Safety**: Гарантированная распаковка префаб-инстансов и безопасный репарентинг при работе с масками.

---

## 📚 Документация

Подробные инструкции находятся в папке `plugin/figma-to-unity/Developer/`:
- [Руководство разработчика (Setup & Usage)](plugin/figma-to-unity/Developer/DEVELOPER_MANUAL.md)
- [Техническая концепция и Smart Sync](plugin/figma-to-unity/Developer/DEV_CONCEPTS.md)
- [Чек-лист для дизайнеров](plugin/figma-to-unity/Developer/DEV_DESIGNER_GUIDE.md)

---

## 📜 Лицензия

© 2026 **BrainySoftware OU**. Все права защищены.
Разработано для профессионального использования в игровых проектах.
