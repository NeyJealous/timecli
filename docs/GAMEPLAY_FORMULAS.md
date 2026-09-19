# Подтверждённые формулы Time Clickers 1.4.5

Эти формулы перенесены в `src/PortCore/` как независимо написанные compatibility implementations.

## Arena HP

Для wave `w`:

- boss = `w % 5 == 0`;
- до 130-й волны рост `1.6^(w-1)`, boss получает базовый множитель x10;
- после 130: `1.25^(min(500,w)-130)`;
- после 500 дополнительно: `1.125^(w-500)`.

## Time Cubes

`floor(1.3^((wave - 40) / 25) * timeCubeMultiplier)`

Special boss rewards:

- waves: `100, 250, 500, 1000, 2000, 3000`
- counts: `10, 50, 200, 500, 1500, 2500`

Оригинальная математика использует float32 Unity `Mathf`.

## Weapon Cubes

Special boss rewards:

- waves: `1000, 1500, 2000, 3000, 4000`
- counts: `1, 150, 350, 775, 1250`

Обычная награда использует экспоненту `1.2`, WeaponCubeFind и сдвиг стартовой волны.

## Time Cube DPS multiplier

`1 + TotalTimeCubes * 0.1`

## Offline gold

`offlineGoldPerSecond = timelineGoldPerSecond * 0.5`

## Artifact cost

Если `upgradeExponent != 0`:

`floor(ceil(((level + baseCost) / upgradeExponentScale)^upgradeExponent) * upgradeMultiplier)`

Иначе:

`ceil(baseCost * upgradeMultiplier^level)`

После bend цена линейно интерполируется до конечной стоимости с float32-семантикой Unity.

## Weapon Augment cost

До bend:

`ceil(curveMultiplier^level * startingCost)`

Затем округление:

- 3–4 цифры: вниз до десятков;
- >4 цифр: вниз до сотен.

После bend — float32 interpolation и то же округление.

## Hero progression

`GetCostLevelMultiplier(costLevel, specOpsLevel)`:

- 0 -> `45`
- 1 -> `90`
- иначе -> `720 * 9.5^((costLevel - 2) * 0.77)`

В 1.4.5 второй параметр метода присутствует, но не используется.

Promotion DPS multipliers:

`4745, 4745, 4745, 4745, 600, 600, 600, 600, 600, 100000`
