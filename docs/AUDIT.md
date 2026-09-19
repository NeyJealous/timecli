# Технический аудит APK

## Идентификация

| Параметр | Значение |
|---|---|
| Package | `com.ProtonStudio.TimeClickers` |
| Версия | `1.4.5` |
| Движок | Unity `5.4.2f2` |
| Script backend | Mono |
| APK SHA-256 | `b09838cf48fa864c9baea10e33f774ceedab1a5a1a0f1af3cc5031eeefc2e4c6` |
| Assembly-CSharp.dll | 933,888 bytes |
| .NET types | 1,204 |
| .NET methods | 7,157 |
| .NET fields | 4,828 |
| MemberRefs | 1,836 |
| OBB | не используется (`useObb=False`) |

## Build scenes

1. `Assets/_CustomAssets/_Scenes/Splash.unity`
2. `Assets/_CustomAssets/_Scenes/Arena.unity`
3. `Assets/_CustomAssets/_Scenes/TimeWarp.unity`
4. `Assets/_CustomAssets/_Scenes/AdditiveScenes/Artifacts.unity`
5. `Assets/_CustomAssets/_Scenes/AdditiveScenes/WeaponAugments.unity`
6. `Assets/_CustomAssets/_Scenes/AdditiveScenes/TimelineSummary.unity`

## Основные игровые подсистемы

- `Arena`, `Enemy`, `Heroes`, `Hero`, `HeroWeapons`
- `Skill`, `Skills`, `Upgrade`, `InfiniteUpgrades`
- `Artifact`, `Artifacts`
- `WeaponAugment`, `WeaponAugments`
- `ActiveAbilities`
- `GoldBank`, `TimeCubes`, `WeaponCubes`
- `NewGamePlus` / Time Warp
- `SaveLoad`, import/export, cloud hooks
- offline earnings
- achievements/statistics

## Почему порт реалистичен

`Assembly-CSharp.dll` — обычная Mono/.NET assembly, а не IL2CPP. Имена типов, методов и полей сохранены, поэтому игровую логику можно восстанавливать детерминированно, а не воспроизводить только по внешнему поведению.

## Уже подтверждено

- 53 реальных Artifact types;
- 19 реальных Weapon Augment types;
- 5 героев с базовыми upgrade-цепочками;
- Click Pistol и active abilities;
- special boss rewards для Time Cubes / Weapon Cubes;
- ключевые формулы стоимости и progression;
- legacy save crypto.

## Ограничения

- публичный репозиторий не хранит оригинальные DLL/serialized assets;
- полный compile-ready source tree ещё не собран;
- asset/scene reconstruction выполняется локально;
- старые SDK должны быть заменены platform abstraction layer.
