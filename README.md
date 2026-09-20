# timecli

Реконструкция и перенос **Time Clickers 1.4.5** на современный iOS/ARM64.

> Проект не является официальным продуктом Proton Studio и не связан с правообладателем.  
> Оригинальные APK, DLL, Unity serialized assets, графика, звук и другие исходные бинарные материалы в публичный репозиторий не добавляются.

## Цель

Восстановить игровой цикл Time Clickers максимально близко к поведению версии
1.4.5 и перенести его на современный Unity/iOS без зависимости от старых
Google Play / Steam / Amazon / Kongregate SDK.

## Текущее состояние

- **Phase 1 — forensic recovery: complete**
- **Phase 2 — portable gameplay core: complete**
- **Phase 3 — Unity reconstruction: in progress**

PortCore уже проходит отдельные regression-сценарии экономики, полного
timeline до wave 100, всех Hero auto-fire механик и сборку
`netstandard2.1` для Unity.

Современный Unity-проект находится в:

`unity/TimeCli/`

## Структура

```text
src/PortCore/          восстановленное Unity-независимое игровое ядро
tests/                 regression/headless scenarios
unity/TimeCli/         современный Unity presentation/runtime
docs/                  аудит, архитектура, Phase status
reference/             контрольные данные
tools/                 extraction / validation / Unity sync tooling
```

## Запуск Unity-ветки разработки

Сначала собрать PortCore:

```powershell
.\tools\unity\sync-portcore.ps1
```

Затем открыть `unity/TimeCli` в зафиксированной версии Unity.

## Принцип реконструкции

Игровая логика считается канонической только после подтверждения анализом
1.4.5 и/или воспроизводимым regression-тестом. Unity-слой не должен повторять
математику PortCore: он отвечает за визуал, input, audio и platform lifecycle.

См. [ROADMAP.md](ROADMAP.md) и
[PortCore <-> Unity contract](docs/PORTCORE_UNITY_CONTRACT.md).

## Репозиторная политика

Публично хранятся наш код, тесты, инструменты, документация и минимальные
reference-данные. Оригинальные APK/DLL, bulk-decompile и извлечённые
графика/аудио/serialized assets хранятся отдельно и не публикуются здесь.
