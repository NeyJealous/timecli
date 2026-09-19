# Платформенные зависимости

В старой сборке обнаружены интеграции, которые не должны блокировать первый iOS prototype.

## Stub/remove на первом проходе

- Google Play Games / `GooglePlayWS`
- Steamworks / `SteamWS`
- Amazon Mobile Ads / `AmazonAdsHelper`
- Kongregate / `KongregateWS`
- legacy Unity Ads helper
- `ProtonAds`

## Стратегия

Вводим тонкий platform-service layer:

- achievements: local/no-op;
- cloud: local save;
- ads: disabled;
- store/IAP: disabled;
- analytics: disabled;
- social/leaderboards: disabled.

После стабильного offline prototype сервисы возвращаются по одному.
