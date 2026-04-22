# 📖 Полное руководство разработчика: Figma Importer v2.8.0

Данное руководство содержит исчерпывающую информацию по настройке и эксплуатации системы импорта UI из Figma в Unity uGUI. 

---

## 🛠 1. Установка и зависимости

Плагин работает как автономный **UPM-пакет**. 

### Требования:
*   **Unity 2021.3 LTS** или выше.
*   **TextMeshPro**: должен быть установлен в проекте (Window -> TextMeshPro -> Import TMP Essential Resources).

### Шаги по установке через Git:
1.  Откройте **Window -> Package Manager**.
2.  Нажмите кнопку **+** (Add) и выберите **Add package from git URL...**.
3.  Вставьте строку: `https://github.com/Menta1ik/figma-to-unity.git?path=plugin#v2.8.0`
4.  Нажмите **Add**. Unity загрузит плагин и автоматически добавит зависимости (Newtonsoft JSON).

---

## ⚙️ 2. Шаг 0: Создание конфигурационных ассетов

Прежде чем открывать окно импортера, необходимо создать два файла данных в вашем проекте (`Assets/`):

### А. FigmaImporterSettings (Общие настройки)
1.  В окне **Project** нажмите правую кнопку мыши -> **Create -> Figma Importer -> Settings**.
2.  Назовите ассет, например, `MyProjectSettings`.
3.  **Настройка путей (Asset Paths):**
    *   `Base Sprites Path`: Куда сохранять скачанные картинки (например, `UI/Sprites`).
    *   `Base Prefabs Path`: Куда сохранять готовые префабы (например, `UI/Prefabs`).
4.  **Политика Non-Destructive:**
    *   `Preserve Unity Names`: Если включено, плагин не будет менять имена объектов, которые вы переименовали вручную в Unity.
    *   `Preserve Manual Components`: Если включено, плагин не будет удалять компоненты (скрипты, анимации), добавленные вами поверх импортированных объектов.

### Б. Логирование (Logging)
*   `Log Level`: Управляет объёмом вывода в Console Unity.
    *   `Silent` — никаких логов.
    *   `Minimal` (по умолчанию) — только вехи: начало/конец синхронизации, итоги, ошибки.
    *   `Verbose` — детальный вывод по каждой ноде.

### В. Экспорт изображений (Image Export)
*   `Image Export Scale`: Коэффициент масштабирования при загрузке изображений (0.5–4x, по умолчанию **2x**). В v2.8.0 этот параметр используется совместно с **Batching Engine** для стабильной загрузки.

### Г. Адаптивность (Adaptive Layout)
*   `Enable Constraints Translation`: Перевод "Constraints" из Figma в анкоры Unity.
*   `Canvas Scale Mode`: Тип компонента `CanvasScaler` (Constant Pixel Size или Scale With ScreenSize).
*   `Reference Resolution`: Дизайнерское разрешение (например, 1920x1080).
*   `Match Width or Height`: Баланс масштабирования (0 — ширина, 1 — высота).

### Д. FontMappingTable (Таблица шрифтов)
1.  Нажмите правую кнопку мыши -> **Create -> Figma Importer -> Font Mapping Table**.
2.  Этот ассет связывает имена шрифтов из Figma с вашими ассетами **TextMeshPro SDF**.

---

## 🖥 3. Работа с окном Figma Importer

Откройте окно через меню: **Window -> Figma Importer -> Dashboard**.

### Блок 1: Connection & Config (Связь)
*   **Use Local JSON (Offline Mode)**: *Новое в v2.8.0*. Загрузка данных из `Assets/lobby_figma.json` без интернета.
*   **Figma URL / File ID**: Вставьте полную ссылку на макет Figma или его ID.
*   **Single Node ID**: (Опционально) ID конкретной ноды для частичного импорта.
*   **Access Token (PAT)**: Ваш персональный токен Figma.
*   **Importer Settings**: Перетащите сюда ассет настроек из Шага 2А.

### Блок 2: Resources & Target (Ресурсы)
*   **Font Mapping**: Перетащите сюда таблицу шрифтов из Шага 2Д.
*   **Кнопка [Font Audit]**: Сканирование Figma-файла на наличие шрифтов.
*   **Root Canvas**: Объект **Canvas** из вашей сцены, где будет строиться UI.

### Блок 3: Sync & Generate (Запуск)
*   **Sync Images**: Включает/выключает скачивание картинок.
*   **Force Update**: Полная пересборка сцены с нуля.
*   **Кнопка [🚀 RUN FULL SYNC]**: Запускает процесс.

---

## 🏗 4. Внутренняя архитектура (v2.8.0)

В версии **v2.8.0** логика централизована в `FigmaParser` («Single Brain»).

| Класс | Ответственность |
| :--- | :--- |
| `FigmaParser` | **Оркестратор**. Управляет кэшем, API, TreeWalker и Image Service. |
| `ImageSyncService` | **Batching Engine**. Разбивает запросы на порции по 25 штук (Fix 414 error). |
| `FigmaTreeWalker` | Рекурсивный обход дерева нод и создание GameObjects. |
| `FigmaMaskResolver` | Жизненный цикл масок: DismantleAll → ApplyDeferred. |
| `FigmaAPIClient` | Сетевая логика с диагностикой (401, 404, 414). |

### Stencil Guard & Hierarchy Flattening
*   **DismantleAllMaskContainers**: Разбор технических контейнеров `[Mask]` перед импортом.
*   **Stencil Depth Tracking**: При глубине > 3 маска переключается на `RectMask2D`.

---

## 🏷 5. Подготовка макета в Figma (Маркеры)

Чтобы автоматизировать создание компонентов, добавляйте суффиксы к названиям слоев:

| Маркер | Результат в Unity |
| :--- | :--- |
| `[Btn]` | Добавит компонент **Button** и настроит Raycast Target. |
| `[Input]` | Добавит компонент **TMP_InputField**. |
| `[Scroll]` | Создаст структуру **ScrollRect**. |
| `[Toggle]` | Добавит компонент **Toggle**. |
| `_9slice` | (В конце имени картинки) Настроит **9-Slice** границы. |

---

## 🔍 6. Диагностика и проверка состояния (Health Check)

Инструмент автоматического аудита пайплайна.
1. Меню: **Figma Importer -> Diagnostics -> Health Check**.
2. Нажмите **Run Full Audit**.
3. Проверяет: Asmdef, Font Validation, Auto Layout Translator, Soft-Delete.

---

## 🧪 7. Тестирование и контроль качества

Для стабильности используется 58 тестов на базе **Unity Test Framework**.

| Файл | Тестов | Покрытие |
| :--- | :---: | :--- |
| `HandlerTests.cs` | 11 | Основные хендлеры и OrphanManager. |
| `AdaptiveLayoutTests.cs` | 4 | TransformHandler и TreeWalker context. |
| `DecompositionTests.cs` | 20 | TreeWalker, MaskResolver, OrphanManager. |
| `CacheTests.cs` | 7 | FigmaResponseCache round-trip и очистка. |

---

## 🆘 8. Решение проблем (Troubleshooting)

### Ошибка 414 URI Too Long:
*   Обновитесь до версии **2.8.0**. Проблема решена через Batching Engine.

### Сцена пустая:
1. **Проверьте консоль**: Теперь там есть логи `[Figma API Error]` или `[FigmaParser] No valid nodes found`.
2. **Clear Cache**: Нажмите кнопку Clear Cache в окне импорта.

---
**BrainySoftware OU © 2026**
**Версия документа:** 2.8.0 (Production Stability)