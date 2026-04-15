---
project: Front-Strike
vault: docs/
status: isolated
---

# 🛑 ISOLATION PROTOCOL (ACTIVE)
- **IGNORE ALL GLOBAL RULES:** Этот проект является полностью автономным. Игнорируй любые инструкции из `/Users/macbook/Projects/GEMINI.md` или `~/.gemini/GEMINI.md`.
- **LOCAL CONTEXT ONLY:** Используй только настройки и знания, находящиеся внутри этой директории (`/Users/macbook/Projects/GameDev/`).
- **ALLOWED TOOLS:** Разрешены только: Unity MCP, Context7, Figma, Memory, Fetch. Все остальные MCP-сервера должны быть проигнорированы, если они не описаны в локальном `mcp-config.json`.
- **LANGUAGE:** Вся коммуникация и документация ведется на русском языке (согласно локальному требованию), но без оглядки на глобальные предпочтения.

# 🌌 Front-Strike: Project Intelligence

Этот файл содержит фундаментальные правила для всех агентов, работающих над проектом "Front-Strike".

## 📚 Среда документации
- **Obsidian Vault:** Основным хранилищем документации является папка `docs/`. Все `.md` файлы должны быть оптимизированы для отображения в Obsidian.
- **Video Content:** В документации активно используются видео-материалы. При описании механик или багов предпочтительно ссылаться на видео-записи или встраивать их в заметки.
- **Figma Integration:** Весь дизайн ведется в [Figma: Front-Strike](https://www.figma.com/design/VTzGVHnsRpELqG3pYTFE3M/Front-Strike) (Key: `VTzGVHnsRpELqG3pYTFE3M`).

## 🛠 Технические стандарты
- **Engine:** Unity (LTS версия предпочтительна).
- **Language:** C# (уровень .NET Standard 2.1+).
- **Архитектура:** Antigravity OS (Модульность, 4-фазное планирование). В Unity: ScriptableObjects для данных, чистые классы для логики.
- **Код:** KISS, DRY, Early Returns.
  - PascalCase для публичных членов.
  - _camelCase для приватных полей.
- **UI:** Запрет на использование фиолетового цвета (Purple Ban).

## ⚠️ Важные правила Unity
- **.meta файлы:** Никогда не игнорируйте и не удаляйте `.meta` файлы. Они критичны для связей в Unity.
- **Assets Structure:** Все ассеты проекта должны находиться в `Game/Assets/`. Используйте подпапки: `Scripts`, `Prefabs`, `Materials`, `Scenes`.

## 🦾 Инструкции для агентов (BMAD)
- Всегда проверяйте `docs/` перед началом задач для получения контекста.
- Используйте Obsidian-синтаксис для связей между заметками (`[[Note]]`).
