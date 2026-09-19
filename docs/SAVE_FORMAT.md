# Save format

## Верхний уровень JSON

Перед шифрованием save содержит блоки:

- `saveFileCreationDate`
- `Artifacts`
- `Arena`
- `Heroes`
- `GoldBank`
- `Skills`
- `ActiveAbilities`
- `Stats`
- `NewGamePlus`
- `TimelineTime`
- `WeaponCubeBank`
- `WeaponAugments`
- `SaveTicks`

Валидация требует успешно разобранный JSON и поле `Arena`.

## Legacy encryption

Параметры compatibility format:

- key: `l5FUP7guJYYz7vBFuFTNuLqZmS2dD5j0`
- IV: 32 bytes
- key size: 256 bits
- block size: 256 bits
- mode: CBC
- padding: zero padding

Pipeline:

1. Rijndael-256/CBC;
2. key = UTF-8 bytes;
3. random 32-byte IV;
4. plaintext -> ASCII;
5. pack `IV || ciphertext`;
6. add repeating lowercase MD5-hex character codes of the key to each byte modulo 256;
7. Base64.

Compatibility implementation: `src/PortCore/SaveCrypto.cs`.

## Offline earnings

При наличии `SaveTicks`:

- interval = now - SaveTicks;
- cap = 172800 seconds;
- offline damage = `Heroes.GetDPS() * seconds * 0.5`;
- offline gold = `timelineGoldPerSec * seconds * 0.5`;
- при нулевом gold применяется fallback через damage и gold multipliers.

> Формат legacy. Для нового сохранения позже нужен versioned migration layer, но импорт оригинальных saves должен оставаться совместимым.
