# 🌌 Antigravity Figma Importer v2.8.0

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
