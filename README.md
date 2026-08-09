# 🌌 Figma to Unity Importer v2.8.2

[![Version](https://img.shields.io/badge/version-2.8.2-blue)](plugin/CHANGELOG.md)
[![Unity](https://img.shields.io/badge/Unity-2021.3%2B%20LTS-black?logo=unity)](https://unity.com/)
[![C#](https://img.shields.io/badge/C%23-.NET%20Standard%202.1-239120?logo=csharp&logoColor=white)](plugin/Editor)
[![License](https://img.shields.io/badge/license-Proprietary-red)](LICENSE)

**Antigravity Figma Importer** — профессиональный инструмент для «пиксель-в-пиксель» переноса интерфейсов из Figma в Unity uGUI. Плагин использует неразрушающую архитектуру **Smart Sync**, позволяя обновлять UI из Figma без потери ваших изменений (скриптов, анимаций) в Unity.

---

## About the Author

I'm a **vibe-coding evangelist** — a solo builder running multiple large projects simultaneously, using AI agents as my development team.

I believe the future of software creation is not about typing code line by line. It's about *thinking in systems*, orchestrating AI agents, and shipping products that matter — fast, intentionally, and without burning out.

This repository is the **quintessence of everything I use**. Every tool here has been battle-tested across real products: a legal platform for the Spanish jurisdiction, a talent competition system with AI judging, a veterinary assistant, and more. This isn't a curated list — it's a living toolkit, shaped by hundreds of hours of actual vibe-coding sessions.

---

### 🐱 The Cats of Kharkiv

I run a cat shelter in **Kharkiv, Ukraine** — a city that has been under constant bombardment since the full-scale Russian invasion began in 2022.

While sirens go off and windows shake, the cats still need to be fed. Wounds still need treatment. Kittens found in the rubble still need warmth. The shelter keeps running — because someone has to.

**Every star, every fork, every donation from this project goes directly to the shelter.**
Not to cloud infrastructure. Not to marketing. To food, medicine, and the people who show up every day — war or no war.

If this toolkit saved you time, made your project better, or just gave you a useful idea — please consider paying it forward.

🌍 **[meowroom.top](https://meowroom.top)** — see the shelter, meet the cats  
💛 **[Donate via PayPal](https://paypal.me/wesavecats)** — 100% goes to the animals

> *"We write code to build the future. We feed cats to keep our humanity."*
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
