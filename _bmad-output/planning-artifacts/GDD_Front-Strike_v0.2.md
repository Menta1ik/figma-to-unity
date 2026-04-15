# 🌌 Game Design Document: Front-Strike
**Version:** 0.2-Beta (Full Specs)  
**Project:** Military Cyberpunk Tactical Shooter  
**Status:** In-Progress / Expanding  

---

## 1. Vision & Core Pillars
### 1.1 High Concept
**Front-Strike** — тактический сессионный шутер, где владение уникальным арсеналом подтверждается через блокчейн (Web3), а визуальное качество достигается за счет гибридного рендеринга (URP + Compute Shaders).

### 1.2 Core Pillars
- **Tactical Precision:** Каждый выстрел важен. Хэдшоты — ключевой навык.
- **True Ownership:** Игрок реально владеет своими скинами и оружием (Wallet Integration).
- **Squad Cohesion:** Игра строится вокруг взаимодействия в малых группах (3-4 человека).
- **High-End Visuals:** Технологичный UI и материалы, использующие Path Tracing элементы для редких предметов.

---

## 2. Gameplay Mechanics
### 2.1 Movement & Perspective
- **Perspective:** First Person / Third Person Concept (согласно Figma `3D Concept`).
- **Pacing:** Тактический, с упором на позиционирование.

### 2.2 Combat & Damage System (Derived from HUD)
- **Health System:** 100 HP база.
- **Armor System (Plates):** 
    - До 3-х слоев брони (Armor Plates).
    - Урон сначала поглощается броней (индикация `Damage_Armor`).
    - Критический урон проходит сквозь броню при попадании в голову (`Headshot_Hit`).
- **Visual Feedback:** 
    - Направленный индикатор урона (`GamePlay_Damage`).
    - Уникальный оверлей при убийстве в голову (`Headshot_Kill`).

### 2.3 Shooting Mechanics
- **Ballistics:** Просчет падения пули и разброса.
- **Aiming:** Режим прицеливания (ADS) меняет точность и поле зрения (FOV).

---

## 3. Meta-Game & Progression
### 3.1 Web3 Ecosystem
- **Entry Point:** `Wallet Login`. Без подключения кошелька игрок имеет доступ только к базовому снаряжению.
- **Assets:** Оружие и скины хранятся как цифровые активы.
- **Marketplace:** Покупка/Продажа внутри Лобби через `Pop-up_Balance`.

### 3.2 Player Progression
- **Leveling:** Опыт за матчи повышает уровень аккаунта (Lvl 01-99).
- **Ranks:** Иерархия от Новичка до Админа/Элиты (`Icn_Lby_Char_Admin`).
- **Achievements:** Награды за особые достижения в матчах (`Reward Screen`).

---

## 4. Game Systems & UI Flow
### 4.1 Lobby (The Hub)
- **Squad Slots:** Отображение 3D моделей напарников, их уровней и статуса готовности.
- **Voice Status:** Иконки активности микрофона (`Icn_Lby_Char_Mic_On`).
- **Ready Check:** Визуальное подтверждение готовности всех участников отряда.

### 4.2 Armory (Deep Customization)
- **Rarity Framework:**
    - **Common (Grey):** Базовое.
    - **Uncommon (Green):** Улучшенное.
    - **Rare (Blue):** Редкое.
    - **Epic (Purple):** Очень редкое (*Purple Ban on UI ignored for icons*).
    - **Legendary (Gold):** Уникальное (Path Tracing effects).
- **Filtering:** Быстрый поиск по типу оружия и редкости.

---

## 5. Technical Requirements
### 5.1 Rendering Architecture
- **Pipeline:** Universal Render Pipeline (URP).
- **Compute Shaders:** Система `SetAlphaChannel.compute` используется для динамической подгрузки и обработки высококачественных текстур оружия и эффектов прозрачности (стекло прицелов, щиты).

### 5.2 Social & Networking
- **Multiplayer:** Dedicated Servers.
- **Voice:** Интегрированный VoIP (Vivox или аналог).

---

## 6. Implementation Roadmap (Phase 1)
1.  **Core UI Framework:** Реализация `UIManager` и переходов между экранами (Lobby -> Armory -> Game).
2.  **Wallet Integration Mockup:** Создание системы логина.
3.  **Basic Player Controller:** Движение и базовая стрельба.
4.  **Armor Logic:** Реализация классов `Health` и `ArmorController`.

---
*Generated and Expanded by BMad Game Dev Studio.*
