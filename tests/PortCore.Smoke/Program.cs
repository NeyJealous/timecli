using TimeClickers.PortCore;

static void AssertEqual(double expected, double actual, string name, double relativeTolerance = 1e-12)
{
    var scale = Math.Max(1.0, Math.Abs(expected));
    if (Math.Abs(expected - actual) > scale * relativeTolerance)
        throw new Exception($"{name}: expected {expected:R}, got {actual:R}");
}

static UpgradeSpec U(int level, params (UpgradeMod Mod, double Value)[] mods)
{
    var dict = new Dictionary<UpgradeMod, double>();
    foreach (var (mod, value) in mods)
        dict[mod] = value;
    return new UpgradeSpec(0, level, dict);
}

static InfiniteUpgradeSchedule BuildPulsePistolSchedule()
{
    int[] levels =
    {
        10, 25, 50, 75, 85, 100,
        110, 125, 150, 175, 185, 200,
        210, 225, 250, 275, 285, 300,
        310, 325, 350, 375, 385, 400,
        410, 425, 450, 475, 485,
        500, 600, 700, 800, 900, 1000
    };

    var upgrades = levels.Select(level =>
    {
        if (level is 100 or 200 or 300 or 400)
            return U(level, (UpgradeMod.Promotion, 1));
        if (level is 500 or 600 or 700 or 800 or 900)
            return U(level, (UpgradeMod.Training, level / 100 - 4));
        if (level == 1000)
            return U(level, (UpgradeMod.SpecOps, 1));
        return U(level);
    }).ToArray();

    upgrades[0] = U(10, (UpgradeMod.FireRate, 2), (UpgradeMod.HeroDps, 2));
    upgrades[1] = U(25, (UpgradeMod.FireRate, 2), (UpgradeMod.HeroDps, 2));
    upgrades[2] = U(50, (UpgradeMod.HeroDps, 2.5));
    upgrades[3] = U(75, (UpgradeMod.FireRate, 2.5), (UpgradeMod.HeroDps, 2.5));
    upgrades[4] = U(85, (UpgradeMod.ClickDamage, 0.005));
    upgrades[6] = U(110, (UpgradeMod.FireRate, 2), (UpgradeMod.HeroDps, 2));

    return new InfiniteUpgradeSchedule(upgrades);
}

// Arena / currency
AssertEqual(10, ArenaMath.GetArenaHP(1), "Arena HP wave 1");
AssertEqual(30, ArenaMath.GetArenaHP(2), "Arena HP wave 2");
AssertEqual(50, ArenaMath.GetArenaHP(3), "Arena HP wave 3");
AssertEqual(696, ArenaMath.GetArenaHP(5), "Arena HP wave 5");
AssertEqual(6962, ArenaMath.GetArenaHP(10), "Arena HP wave 10");
AssertEqual(1267901, ArenaMath.GetArenaHP(26), "Arena HP wave 26");
AssertEqual(10, ArenaMath.TimeCubeBossCounts[0], "Time Cube boss reward 100");
AssertEqual(2500, ArenaMath.TimeCubeBossCounts[^1], "Time Cube boss reward 3000");
AssertEqual(1, ArenaMath.WeaponCubeBossCounts[0], "Weapon Cube boss reward 1000");
AssertEqual(1250, ArenaMath.WeaponCubeBossCounts[^1], "Weapon Cube boss reward 4000");
AssertEqual(1.0, ArenaMath.GetTimeCubeDpsMultiplier(0), "TC DPS multiplier 0");
AssertEqual(11.0, ArenaMath.GetTimeCubeDpsMultiplier(100), "TC DPS multiplier 100");
AssertEqual(50.0, ArenaMath.GetOfflineGoldPerSecond(100), "Offline gold");

// Hero cost progression
AssertEqual(45, HeroProgressionMath.GetLevelUpCostForLevel(0, 0, 1), "Hero cost b0 r1 l0");
AssertEqual(97, HeroProgressionMath.GetLevelUpCostForLevel(0, 10, 1), "Hero cost b0 r1 l10");
AssertEqual(90, HeroProgressionMath.GetLevelUpCostForLevel(1, 0, 1), "Hero cost b1 r1 l0");
AssertEqual(720, HeroProgressionMath.GetLevelUpCostForLevel(2, 0, 1), "Hero cost b2 r1 l0");

// Infinite upgrade schedule
var schedule = BuildPulsePistolSchedule();
AssertEqual(35, schedule.BaseUpgradeCount, "Base upgrade count");
AssertEqual(30, schedule.GeneratedCycleCount, "Generated Spec Ops cycle count");
AssertEqual(1, schedule.GetRank(0), "Rank before upgrades");
AssertEqual(2, schedule.GetRank(6), "Rank after first promotion");
AssertEqual(11, schedule.GetRank(35), "Rank after first Spec Ops");
AssertEqual(11, schedule.GetRank(36), "Rank in second cycle");
AssertEqual(21, schedule.GetRank(65), "Rank after second Spec Ops");
AssertEqual(1010, schedule.GetUpgrade(35).LevelRequired, "First generated upgrade level");
AssertEqual(2000, schedule.GetUpgrade(64).LevelRequired, "Generated Spec Ops level");

// Hero combat regression
var pulse = new HeroBaseSpec(
    0, WeaponType.Pistol, 5.0, 3.0f, 0, schedule.BaseUpgrades);

var neutral = new HeroDpsMultipliers();
AssertEqual(100, HeroCombatMath.GetDpsForLevel(pulse, schedule, 10, 1, neutral), "Pulse DPS level 10");
AssertEqual(237250, HeroCombatMath.GetDpsForLevel(pulse, schedule, 100, 6, neutral), "Pulse DPS promotion");
AssertEqual(949000, HeroCombatMath.GetDpsForLevel(pulse, schedule, 110, 7, neutral), "Pulse DPS post-promotion");
AssertEqual(6.0, HeroCombatMath.GetRateOfFire(3.0f, schedule, 1), "Pulse fire rate first upgrade");
AssertEqual(3.0, HeroCombatMath.GetRateOfFire(3.0f, schedule, 6), "Promotion resets fire rate");
AssertEqual(6.0, HeroCombatMath.GetRateOfFire(3.0f, schedule, 7), "Post-promotion fire rate");
AssertEqual(0.005, HeroCombatMath.GetExtraClickDamagePercent(schedule, 5), "Hero extra click damage");

// Click skill / abilities
AssertEqual(5, SkillMath.GetLevelUpCostForLevel(5, 0), "Click skill level cost 0");
AssertEqual(18, SkillMath.GetUpgradeCostForLevel(5, 1), "Click skill upgrade cost");
var clickUpgrades = new[]
{
    U(10, (UpgradeMod.ClickDamage, 2)),
    U(25, (UpgradeMod.ClickDamage, 2))
};
AssertEqual(84, SkillMath.GetDamageForLevel(1, 10, clickUpgrades, 2, 40, 1), "Click skill damage");
AssertEqual(250, AbilityMath.GetCostForLevel(0), "Ability cost 0");
AssertEqual(22500, AbilityMath.GetCostForLevel(1), "Ability cost 1");
AssertEqual(1.21, AbilityMath.GetDimensionShiftMultiplier(2, 1.1), "Dimension shift multiplier", 1e-10);

// Artifact aggregate effects
var artifactValues = new double[(int)ArtifactType.Total];
artifactValues[(int)ArtifactType.GoldFind] = 25;
artifactValues[(int)ArtifactType.TimeCubeChance] = 7.5;
artifactValues[(int)ArtifactType.TimeCubeFind] = 50;
artifactValues[(int)ArtifactType.DamageTargeted] = 20;
artifactValues[(int)ArtifactType.CriticalStrikeChance] = 8;
artifactValues[(int)ArtifactType.CriticalStrikeMultiplier] = 6;
artifactValues[(int)ArtifactType.AbilityDuration1] = 5;
artifactValues[(int)ArtifactType.AbilityDuration2] = 10;
artifactValues[(int)ArtifactType.AbilityCooldown1] = 15;
artifactValues[(int)ArtifactType.AbilityCooldown2] = 5;
artifactValues[(int)ArtifactType.RapidFire] = 20;
artifactValues[(int)ArtifactType.DimensionShift] = 10;
artifactValues[(int)ArtifactType.PulsePistolDps] = 4;
artifactValues[(int)ArtifactType.RocketSplashDiagonal] = 1;
artifactValues[(int)ArtifactType.NeverMissFlak] = 1;

var artifacts = ArtifactEffectsMath.Recalculate(artifactValues);
AssertEqual(1.25, artifacts.GoldFindMultiplier, "Artifact gold find");
AssertEqual(0.075, artifacts.TimeCubeChance, "Artifact time cube chance", 1e-6);
AssertEqual(1.5, artifacts.TimeCubeMultiplier, "Artifact time cube multiplier");
AssertEqual(1.2, artifacts.TargetedMultiplier, "Artifact targeted multiplier");
AssertEqual(5, artifacts.AdditionalCriticalMultiplier, "Artifact critical multiplier delta");
AssertEqual(15, artifacts.AbilityDurationAddSeconds, "Artifact ability duration");
AssertEqual(0.8, artifacts.AbilityRechargeMultiplier, "Artifact recharge multiplier", 1e-6);
AssertEqual(0.05, artifacts.RapidFireDelay, "Artifact rapid fire delay", 1e-6);
AssertEqual(1.1, artifacts.DimensionShiftMultiplier, "Artifact dimension shift", 1e-12);
AssertEqual(4, artifacts.GetWeaponDpsMultiplier(WeaponType.Pistol), "Artifact pistol DPS");
AssertEqual(3, artifacts.RocketSplashDistance, "Artifact rocket splash distance");
if (!artifacts.FlakNeverMisses) throw new Exception("Artifact NeverMissFlak should be enabled");

// Enemy gold rewards
artifactValues[(int)ArtifactType.GoldFindRed] = 2;
artifactValues[(int)ArtifactType.GoldFindRainbowBlock] = 3;
artifactValues[(int)ArtifactType.GoldRush] = 4;
artifacts = ArtifactEffectsMath.Recalculate(artifactValues);

AssertEqual(
    200,
    GoldRewardMath.GetKillGoldTotal(600, EnemyType.Red, 2, artifacts, false),
    "Red enemy kill gold");

AssertEqual(
    30000,
    GoldRewardMath.GetKillGoldTotal(600, EnemyType.Rainbow, 2, artifacts, false),
    "Rainbow enemy kill gold");

AssertEqual(
    800,
    GoldRewardMath.GetHitGold(600, 1, EnemyType.Red, 2, artifacts, true),
    "Hit gold with Gold Rush");

AssertEqual(
    0,
    GoldRewardMath.GetKillGoldTotal(600, EnemyType.TimeCube, 2, artifacts, true),
    "Time Cube enemy does not drop gold");

// Weapon augments and deterministic cube/rainbow rules
var augmentValues = new double[(int)WeaponAugmentType.Total];
augmentValues[(int)WeaponAugmentType.WeaponCubeChance] = 25;
augmentValues[(int)WeaponAugmentType.WeaponCubeFind] = 50;
augmentValues[(int)WeaponAugmentType.WeaponCubeStartWave] = 1000;
augmentValues[(int)WeaponAugmentType.ClickLauncherUnlock] = 1;
augmentValues[(int)WeaponAugmentType.ClickCannonDamagePerShot] = 25;
var augments = WeaponAugmentEffectsMath.Recalculate(augmentValues);

if (!augments.ClickLauncherUnlocked) throw new Exception("Click Launcher should be unlocked");
AssertEqual(25, augments.WeaponCubeChancePercent, "Weapon Cube chance");
AssertEqual(50, augments.WeaponCubeFindPercent, "Weapon Cube find");
AssertEqual(1000, augments.WeaponCubeStartWave, "Weapon Cube start wave");
AssertEqual(10, SpawnRulesMath.ResolveTimeCubeReward(100, false, artifacts, 1f), "Time Cube boss reward");
AssertEqual(2, SpawnRulesMath.ResolveTimeCubeReward(105, false, artifacts, 0.05f), "Time Cube random reward");
AssertEqual(0, SpawnRulesMath.ResolveTimeCubeReward(105, false, artifacts, 0.5f), "Time Cube failed roll");
AssertEqual(1, SpawnRulesMath.ResolveWeaponCubeReward(1000, false, augments, 1f), "Weapon Cube first boss reward");
AssertEqual(2, SpawnRulesMath.ResolveWeaponCubeReward(1025, false, augments, 0.1f), "Weapon Cube random reward");
AssertEqual(0, SpawnRulesMath.ResolveWeaponCubeReward(1025, false, augments, 0.9f), "Weapon Cube failed roll");

artifactValues[(int)ArtifactType.RainbowEnemyChance] = 20;
artifacts = ArtifactEffectsMath.Recalculate(artifactValues);
if (!SpawnRulesMath.ShouldSpawnRainbowEnemy(101, 1, artifacts, 0.1f))
    throw new Exception("Rainbow enemy should spawn for a successful roll");
if (SpawnRulesMath.ShouldSpawnRainbowEnemy(101, 0, artifacts, 0.1f))
    throw new Exception("First enemy on a wave cannot be rainbow");
if (SpawnRulesMath.ShouldSpawnRainbowEnemy(100, 1, artifacts, 0f))
    throw new Exception("Boss waves cannot become rainbow enemies");

// Headless wave progression
var timeline = new TimelineProgression(1);
for (int i = 0; i < 9; i++)
{
    if (timeline.CompleteEnemy(10) != ArenaClearResult.ContinueSameWave)
        throw new Exception("Regular wave advanced too early");
}
if (timeline.CompleteEnemy(10) != ArenaClearResult.AdvancedToNextWave)
    throw new Exception("Regular wave did not advance");
AssertEqual(2, timeline.Wave, "Timeline wave after regular clear");
AssertEqual(2, timeline.MaxWave, "Timeline max wave");

timeline.TimeWarpTo(5);
if (timeline.CompleteEnemy(10) != ArenaClearResult.AdvancedToNextWave)
    throw new Exception("Boss wave must advance after one complete enemy");
AssertEqual(6, timeline.Wave, "Timeline wave after boss clear");

// Ability runtime state machine
var rapidFireAbility = new AbilityRuntime(
    AbilityCatalog.Get(AbilityType.AutomaticFire),
    artifacts,
    100);
AssertEqual(45, rapidFireAbility.DurationSeconds, "Ability duration with artifacts");
AssertEqual(48, rapidFireAbility.RechargeSeconds, "Ability recharge with artifacts");
if (!rapidFireAbility.Activate(100)) throw new Exception("Ready ability should activate");
if (rapidFireAbility.State != AbilityState.Active) throw new Exception("Ability should be active");
AssertEqual(45, rapidFireAbility.GetRemainingActiveSeconds(100), "Ability active seconds");
if (rapidFireAbility.Activate(101)) throw new Exception("Active ability must not activate again");
if (rapidFireAbility.Update(145) != AbilityState.Recharging)
    throw new Exception("Ability should enter recharge");
if (rapidFireAbility.Update(193) != AbilityState.Ready)
    throw new Exception("Ability should become ready");

var dimensionShiftAbility = new AbilityRuntime(
    AbilityCatalog.Get(AbilityType.DimensionShift),
    artifacts,
    0);
AssertEqual(0, dimensionShiftAbility.DurationSeconds, "Dimension Shift has zero duration");
AssertEqual(23040, dimensionShiftAbility.RechargeSeconds, "Dimension Shift recharge");

var abilitySet = AbilityCatalog.All
    .Select(spec => new AbilityRuntime(spec, artifacts, 0))
    .ToArray();
abilitySet[0].SetSecondsUntilRecharged(4000, 0);
abilitySet[(int)AbilityType.Cooldown].SetSecondsUntilRecharged(4000, 0);
AbilityRuntimeMath.ApplyCooldownAbility(
    abilitySet,
    abilitySet[(int)AbilityType.Cooldown]);
AssertEqual(400, abilitySet[0].GetSecondsUntilRecharged(0), "Cooldown ability reduces other cooldowns");
AssertEqual(4000, abilitySet[(int)AbilityType.Cooldown].GetSecondsUntilRecharged(0), "Cooldown does not reduce itself");

// Canonical hero data
AssertEqual(5, CanonicalHeroes.All.Count, "Canonical hero count");
AssertEqual(35, CanonicalHeroes.PulsePistol.BaseUpgrades.Count, "Pulse canonical upgrade count");
AssertEqual(22, CanonicalHeroes.FlakCannon.BaseDamage, "Flak base damage");
AssertEqual(74, CanonicalHeroes.SpreadRifle.BaseDamage, "Spread base damage");
AssertEqual(245, CanonicalHeroes.RocketLauncher.BaseDamage, "Rocket base damage");
AssertEqual(976, CanonicalHeroes.ParticleBall.BaseDamage, "Particle base damage");
AssertEqual(8, CanonicalHeroes.FlakCannon.BaseUpgrades[2].Get(UpgradeMod.Projectiles), "Flak projectile upgrade");
AssertEqual(1.2, CanonicalHeroes.SpreadRifle.BaseUpgrades[4].Get(UpgradeMod.UnitedFront), "Spread United Front");
AssertEqual(0.5, CanonicalHeroes.RocketLauncher.BaseUpgrades[1].Get(UpgradeMod.Splash), "Rocket splash");
AssertEqual(10, CanonicalHeroes.ParticleBall.BaseUpgrades[2].Get(UpgradeMod.Collider), "Particle collider");

var canonicalPulseSchedule = new InfiniteUpgradeSchedule(CanonicalHeroes.PulsePistol.BaseUpgrades);
AssertEqual(
    237250,
    HeroCombatMath.GetDpsForLevel(CanonicalHeroes.PulsePistol, canonicalPulseSchedule, 100, 6, neutral),
    "Canonical Pulse promotion DPS");

var canonicalFlakSchedule = new InfiniteUpgradeSchedule(CanonicalHeroes.FlakCannon.BaseUpgrades);
var teamEffects = TeamMath.RecalculateBaseUpgradeEffects(new[]
{
    new HeroUpgradeProgress(canonicalPulseSchedule, 17),
    new HeroUpgradeProgress(canonicalFlakSchedule, 11)
});
AssertEqual(1.2, teamEffects.UnitedFrontMultiplier, "Canonical team United Front");
AssertEqual(1.25, teamEffects.GoldFindMultiplier, "Canonical team gold find");
AssertEqual(0.02, teamEffects.CriticalChance, "Canonical team critical chance", 1e-6);

// Canonical artifact / augment loadouts
var artifactLoadout = new ArtifactLoadout();
var defaultArtifacts = artifactLoadout.BuildEffects();
AssertEqual(1.0, defaultArtifacts.GoldFindMultiplier, "Default artifact global gold");
AssertEqual(1.0, defaultArtifacts.GoldFindRed, "Default red gold");
AssertEqual(0.25, defaultArtifacts.TimeCubeChance, "Default Time Cube chance", 1e-6);
AssertEqual(1.0, defaultArtifacts.TimeCubeMultiplier, "Default Time Cube find multiplier");
AssertEqual(30, defaultArtifacts.BossTime, "Default boss timer");
AssertEqual(10, defaultArtifacts.EnemiesToAdvance, "Default enemies to advance");
AssertEqual(0.1, defaultArtifacts.RapidFireDelay, "Default rapid fire delay", 1e-6);
AssertEqual(1.05, defaultArtifacts.DimensionShiftMultiplier, "Default Dimension Shift multiplier", 1e-12);

artifactLoadout.SetLevel(ArtifactType.GoldFind, 10);
defaultArtifacts = artifactLoadout.BuildEffects();
AssertEqual(1.3, defaultArtifacts.GoldFindMultiplier, "Artifact loadout level evaluation", 1e-12);

var augmentLoadout = new WeaponAugmentLoadout();
var defaultAugments = augmentLoadout.BuildEffects();
AssertEqual(25, defaultAugments.WeaponCubeChancePercent, "Default Weapon Cube chance");
AssertEqual(1000, defaultAugments.WeaponCubeStartWave, "Default Weapon Cube start wave");
AssertEqual(19, defaultAugments.ClickPistolGoldSpawnChance, "Default hit-gold chance");
if (defaultAugments.ClickLauncherUnlocked) throw new Exception("Click Launcher must start locked");

augmentLoadout.SetLevel(WeaponAugmentType.WeaponCubeFind, 10);
defaultAugments = augmentLoadout.BuildEffects();
AssertEqual(5, defaultAugments.WeaponCubeFindPercent, "Weapon Cube Find level evaluation");

// Canonical click skill
AssertEqual(6, CanonicalSkills.ClickPistol.Upgrades.Count, "Click Pistol upgrade count");
AssertEqual(3, CanonicalSkills.ClickPistol.Upgrades[4].Get(UpgradeMod.ClickDamage), "Click Pistol 100 upgrade");
AssertEqual(
    432,
    SkillMath.GetDamageForLevel(
        CanonicalSkills.ClickPistol.BaseDamage,
        50,
        CanonicalSkills.ClickPistol.Upgrades,
        3,
        24,
        1),
    "Canonical Click Pistol damage");

// Hero purchase planner/runtime
var pulseRuntime = new HeroRuntime(CanonicalHeroes.PulsePistol);

var oneLevelPlan = pulseRuntime.PlanPurchase(double.PositiveInfinity, HeroBuyMode.OneLevel);
AssertEqual(1, oneLevelPlan.LevelCount, "One-level plan count");
AssertEqual(0, oneLevelPlan.UpgradeCount, "One-level upgrade count");
AssertEqual(45, oneLevelPlan.TotalCost, "One-level hire cost");

var nextUpgradePlan = pulseRuntime.PlanPurchase(double.PositiveInfinity, HeroBuyMode.NextUpgrade);
AssertEqual(10, nextUpgradePlan.LevelCount, "Next-upgrade level count");
AssertEqual(0, nextUpgradePlan.UpgradeCount, "Next-upgrade upgrade count");
AssertEqual(648, nextUpgradePlan.TotalCost, "Cost to Pulse level 10");
if (!nextUpgradePlan.UpgradeNext) throw new Exception("A normal upgrade should be next at level 10");

var nextRankPlan = pulseRuntime.PlanPurchase(double.PositiveInfinity, HeroBuyMode.NextRank);
AssertEqual(100, nextRankPlan.LevelCount, "Next-rank level count");
AssertEqual(5, nextRankPlan.UpgradeCount, "Next-rank intermediate upgrade count");
AssertEqual(1593793, nextRankPlan.TotalCost, "Cost to first promotion boundary");
if (!nextRankPlan.PromotionNext) throw new Exception("Promotion should be next at level 100");

var noGoldPlan = pulseRuntime.PlanPurchase(44, HeroBuyMode.OneLevel);
if (noGoldPlan.Affordable) throw new Exception("45-cost hire must not be affordable with 44 gold");
var failedPurchase = pulseRuntime.Purchase(44, HeroBuyMode.OneLevel);
if (failedPurchase.Purchased) throw new Exception("Unaffordable hero purchase should not mutate state");
AssertEqual(0, pulseRuntime.Level, "Failed purchase keeps hero level");

var toUpgrade = pulseRuntime.Purchase(648, HeroBuyMode.NextUpgrade);
if (!toUpgrade.Purchased) throw new Exception("Next-upgrade purchase should succeed");
AssertEqual(10, pulseRuntime.Level, "Pulse level after next-upgrade purchase");
AssertEqual(0, pulseRuntime.PurchasedUpgrades, "Boundary upgrade is not auto-purchased");

var upgradeOnly = pulseRuntime.PlanPurchase(720, HeroBuyMode.NextUpgrade);
AssertEqual(0, upgradeOnly.LevelCount, "Available-upgrade plan has no level purchase");
AssertEqual(1, upgradeOnly.UpgradeCount, "Available-upgrade plan buys one upgrade");
AssertEqual(720, upgradeOnly.TotalCost, "First Pulse upgrade cost");
if (upgradeOnly.UpgradeNext || upgradeOnly.PromotionNext || upgradeOnly.TrainingNext || upgradeOnly.SpecOpsNext)
    throw new Exception("Immediate-upgrade path clears all next flags");

var purchasedUpgrade = pulseRuntime.Purchase(720, HeroBuyMode.NextUpgrade);
if (!purchasedUpgrade.Purchased) throw new Exception("First Pulse upgrade should purchase");
AssertEqual(1, pulseRuntime.PurchasedUpgrades, "Pulse purchased upgrade count");

var maxRuntime = new HeroRuntime(CanonicalHeroes.PulsePistol);
var maxPlan = maxRuntime.PlanPurchase(double.PositiveInfinity, HeroBuyMode.Max);
AssertEqual(1000, maxPlan.LevelCount, "Max mode level cap");
AssertEqual(34, maxPlan.UpgradeCount, "Max mode upgrades before level 1000 boundary");
var maxResult = HeroPurchaseMath.Apply(
    maxRuntime.Spec,
    maxRuntime.Schedule,
    maxRuntime.Level,
    maxRuntime.PurchasedUpgrades,
    double.PositiveInfinity,
    maxPlan);
if (!maxResult.Purchased) throw new Exception("Infinite-gold max purchase should succeed");
AssertEqual(1000, maxResult.NewLevel, "Max purchase level");
AssertEqual(34, maxResult.NewPurchasedUpgrades, "Max purchase upgrade count");
AssertEqual(10, maxRuntime.Schedule.GetRank(maxResult.NewPurchasedUpgrades), "Rank before Spec Ops upgrade");

// Skill purchase runtime
var skillRuntime = new SkillRuntime(CanonicalSkills.ClickPistol);
var skillToUpgrade = skillRuntime.PlanPurchase(1000, HeroBuyMode.NextUpgrade);
AssertEqual(10, skillToUpgrade.LevelCount, "Skill next-upgrade level count");
AssertEqual(135, skillToUpgrade.TotalCost, "Skill cost to level 10");
if (!skillToUpgrade.UpgradeNext) throw new Exception("Skill upgrade should be next at level 10");

var skillLevels = skillRuntime.Purchase(1000, HeroBuyMode.NextUpgrade);
if (!skillLevels.Purchased) throw new Exception("Skill bulk level purchase should succeed");
AssertEqual(10, skillRuntime.Level, "Skill level after bulk purchase");
AssertEqual(0, skillRuntime.PurchasedUpgrades, "Skill boundary upgrade remains unpurchased");

var skillUpgrade = skillRuntime.PlanPurchase(1000, HeroBuyMode.NextUpgrade);
if (!skillUpgrade.PurchasesUpgrade) throw new Exception("Skill should purchase available upgrade");
AssertEqual(87, skillUpgrade.TotalCost, "Skill first upgrade cost");
skillRuntime.Purchase(1000, HeroBuyMode.NextUpgrade);
AssertEqual(1, skillRuntime.PurchasedUpgrades, "Skill purchased upgrade count");

// Aggregate headless game state / Time Warp
var game = new GameState();
AssertEqual(1000, game.Gold.TotalGold, "New timeline starting gold");
AssertEqual(1, game.Arena.Wave, "New timeline starting wave");
AssertEqual(1, game.GetClickDamage(), "Initial click damage");

if (!game.TryPurchaseHero(0, HeroBuyMode.NextUpgrade))
    throw new Exception("GameState should buy Pulse levels with starting gold");
AssertEqual(10, game.Heroes[0].Level, "GameState Pulse level");
AssertEqual(352, game.Gold.TotalGold, "Gold after Pulse level-10 purchase");

game.TimeCubes.AddPendingReward(25);
game.WeaponCubes.Add(7);
game.Abilities[(int)AbilityType.AutomaticFire].Activate(0);
ulong earnedOnWarp = game.TimeWarp();

AssertEqual(25, (double)earnedOnWarp, "Time Warp earned cubes");
AssertEqual(25, (double)game.TimeCubes.Spendable, "Spendable Time Cubes after warp");
AssertEqual(25, (double)game.TimeCubes.LifetimeEarned, "Lifetime Time Cubes after warp");
AssertEqual(3.5, game.TimeCubes.DpsMultiplier, "Time Cube DPS after warp");
AssertEqual(7, (double)game.WeaponCubes.Spendable, "Weapon Cubes persist through warp");
AssertEqual(0, game.Heroes[0].Level, "Heroes reset on Time Warp");
AssertEqual(0, game.ClickPistol.Level, "Skill resets on Time Warp");
AssertEqual(1000, game.Gold.TotalGold, "Gold resets to artifact starting gold");
AssertEqual(1, game.Arena.Wave, "Arena resets to artifact start wave");
if (game.Abilities[(int)AbilityType.AutomaticFire].State != AbilityState.Ready)
    throw new Exception("Abilities reset on Time Warp");

// Artifact economy: prerequisites, exact refund and dependency-safe selling
var artifactBank = new TimeCubeBank();
artifactBank.AddPendingReward(1000);
artifactBank.TimeWarp();
var artifactTree = new ArtifactLoadout();

var lockedArtifactBuy = ArtifactEconomy.BuyOne(
    artifactTree, artifactBank, ArtifactType.GoldFindRed);
if (lockedArtifactBuy.Succeeded || lockedArtifactBuy.Reason != "locked")
    throw new Exception("GoldFindRed must be locked before GoldFind is owned");
AssertEqual(1000, (double)artifactBank.Spendable, "Locked artifact purchase does not spend cubes");

var goldFindBuy = ArtifactEconomy.BuyOne(
    artifactTree, artifactBank, ArtifactType.GoldFind);
if (!goldFindBuy.Succeeded) throw new Exception("GoldFind first level should purchase");
AssertEqual(1, (double)goldFindBuy.CostOrRefund, "GoldFind first-level cost");
AssertEqual(999, (double)artifactBank.Spendable, "Time Cubes after GoldFind purchase");

var redGoldBuy = ArtifactEconomy.BuyOne(
    artifactTree, artifactBank, ArtifactType.GoldFindRed);
if (!redGoldBuy.Succeeded) throw new Exception("GoldFindRed should unlock after GoldFind");
AssertEqual(2, (double)ArtifactEconomy.GetTotalSpent(artifactTree), "Total Artifact spend");

if (ArtifactEconomy.CanSell(artifactTree, ArtifactType.GoldFind))
    throw new Exception("Required Artifact at level 1 cannot be sold while dependent is owned");

var redGoldSell = ArtifactEconomy.SellOne(
    artifactTree, artifactBank, ArtifactType.GoldFindRed);
if (!redGoldSell.Succeeded) throw new Exception("GoldFindRed sell should succeed");
AssertEqual(1, (double)redGoldSell.CostOrRefund, "Artifact sell refunds exact previous-level cost");

var goldFindSell = ArtifactEconomy.SellOne(
    artifactTree, artifactBank, ArtifactType.GoldFind);
if (!goldFindSell.Succeeded) throw new Exception("GoldFind should sell after dependent is removed");
AssertEqual(1000, (double)artifactBank.Spendable, "Artifact round-trip refund");

// Weapon Augment economy
var weaponBank = new WeaponCubeBankState();
weaponBank.Add(1000);
var augmentTree = new WeaponAugmentLoadout();

var lockedAugmentBuy = WeaponAugmentEconomy.BuyOne(
    augmentTree, weaponBank, WeaponAugmentType.ClickLauncherClicks);
if (lockedAugmentBuy.Succeeded || lockedAugmentBuy.Reason != "locked")
    throw new Exception("ClickLauncherClicks must be locked before launcher unlock");

var launcherUnlock = WeaponAugmentEconomy.BuyOne(
    augmentTree, weaponBank, WeaponAugmentType.ClickLauncherUnlock);
if (!launcherUnlock.Succeeded) throw new Exception("Click Launcher unlock should purchase");
AssertEqual(1, (double)launcherUnlock.CostOrRefund, "Click Launcher unlock cost");

var launcherClicks = WeaponAugmentEconomy.BuyOne(
    augmentTree, weaponBank, WeaponAugmentType.ClickLauncherClicks);
if (!launcherClicks.Succeeded) throw new Exception("Click Launcher clicks should purchase");
AssertEqual(5, (double)launcherClicks.CostOrRefund, "Click Launcher clicks first cost");
AssertEqual(6, (double)WeaponAugmentEconomy.GetTotalSpent(augmentTree), "Total Weapon Augment spend");

if (WeaponAugmentEconomy.CanSell(augmentTree, WeaponAugmentType.ClickLauncherUnlock))
    throw new Exception("Launcher unlock cannot sell while dependent augment is owned");

WeaponAugmentEconomy.SellOne(
    augmentTree, weaponBank, WeaponAugmentType.ClickLauncherClicks);
WeaponAugmentEconomy.SellOne(
    augmentTree, weaponBank, WeaponAugmentType.ClickLauncherUnlock);
AssertEqual(1000, (double)weaponBank.Spendable, "Weapon Augment round-trip refund");

// Full original-style Artifact respec
var respecGame = new GameState();
respecGame.TimeCubes.AddPendingReward(100);
respecGame.TimeWarp();
var boughtForRespec = respecGame.BuyArtifact(ArtifactType.GoldFind);
if (!boughtForRespec.Succeeded) throw new Exception("Respec fixture Artifact purchase failed");
AssertEqual(99, (double)respecGame.TimeCubes.Spendable, "Spent Time Cube before respec");

respecGame.TimeCubes.AddPendingReward(25);
ulong respecPending = respecGame.RespecArtifacts();
AssertEqual(25, (double)respecPending, "Respec includes pending timeline Time Cubes");
AssertEqual(125, (double)respecGame.TimeCubes.LifetimeEarned, "Respec lifetime Time Cubes");
AssertEqual(125, (double)respecGame.TimeCubes.Spendable, "Respec restores all earned Time Cubes");
AssertEqual(0, (double)respecGame.Artifacts.GetLevel(ArtifactType.GoldFind), "Respec clears Artifact tree");
AssertEqual(1000, respecGame.Gold.TotalGold, "Respec starts fresh timeline with default starting gold");
AssertEqual(1, respecGame.Arena.Wave, "Respec resets arena to default start wave");

// Arena farm/navigation and boss timeout behavior
var farmArena = new TimelineProgression(4, 6);
if (!farmArena.IsFarmingLowerWave) throw new Exception("Wave 4/6 should be farming");
if (farmArena.CompleteEnemy(10) != ArenaClearResult.ContinueSameWave)
    throw new Exception("Lower regular farm wave should not auto-advance");
AssertEqual(1, farmArena.EnemyKillsOnWave, "Lower farm wave first kill marker");

for (int i = 0; i < 20; i++)
{
    if (farmArena.CompleteEnemy(10) != ArenaClearResult.ContinueSameWave)
        throw new Exception("Lower regular farm wave must remain selected");
}
AssertEqual(4, farmArena.Wave, "Farm wave remains selected");
AssertEqual(1, farmArena.EnemyKillsOnWave, "Lower farm wave kill counter stays at one");

if (!farmArena.RequestNextArena()) throw new Exception("Manual next should move to unlocked wave 5");
AssertEqual(5, farmArena.Wave, "Manual next wave");
AssertEqual(0, farmArena.EnemyKillsOnWave, "Manual navigation resets enemy counter");

farmArena.StartArena(100, 30);
if (!farmArena.FightingBoss) throw new Exception("Wave 5 should start boss timer");
AssertEqual(20, farmArena.GetBossTimeRemaining(110), "Boss remaining time");
if (farmArena.TickBoss(129.9, 30) != BossTickResult.None)
    throw new Exception("Boss should not fail before timer");
if (farmArena.TickBoss(130, 30) != BossTickResult.FailedWaitingForRestart)
    throw new Exception("Boss timeout should enter restart delay");
AssertEqual(5, farmArena.Wave, "Boss failure stays on same wave");
if (!farmArena.BossRestartPending) throw new Exception("Boss restart delay should be pending");
if (farmArena.TickBoss(130.4, 30) != BossTickResult.None)
    throw new Exception("Boss should wait full 0.5 seconds");
if (farmArena.TickBoss(130.5, 30) != BossTickResult.Restarted)
    throw new Exception("Boss should restart after 0.5 seconds");
if (!farmArena.FightingBoss) throw new Exception("Restarted boss should be fighting");

if (farmArena.CompleteEnemy(10, 140) != ArenaClearResult.AdvancedToNextWave)
    throw new Exception("Boss clear should advance");
AssertEqual(6, farmArena.Wave, "Boss clear returns to unlocked max wave");
AssertEqual(6, farmArena.MaxWave, "Boss clear preserves max wave");
AssertEqual(20.5, farmArena.BossTimerStoppedAt, "Boss clear stores remaining timer", 1e-6);

if (farmArena.RequestNextArena())
    throw new Exception("Cannot navigate beyond max wave");
if (!farmArena.RequestPreviousArena())
    throw new Exception("Previous wave navigation should work");
AssertEqual(5, farmArena.Wave, "Previous wave selection");
if (!farmArena.SelectUnlockedWave(4))
    throw new Exception("Direct unlocked-wave selection should work");
AssertEqual(4, farmArena.Wave, "Direct farm-wave selection");
if (farmArena.SelectUnlockedWave(7))
    throw new Exception("Cannot select a locked wave");

// BoxEnemy-style block combat
var targetArtifacts = new ArtifactLoadout();
targetArtifacts.SetLevel(ArtifactType.DamageTargeted, 1);
targetArtifacts.SetLevel(ArtifactType.DamageTargetedPulsePistol, 1);
targetArtifacts.SetLevel(ArtifactType.ClickDamageRed, 1);
var targetEffects = targetArtifacts.BuildEffects();

var targetedRedBlock = new EnemyBlockState(100, EnemyType.Red);
targetedRedBlock.AddTargeted(WeaponType.Pistol);
var targetedHit = targetedRedBlock.ApplyClickDamage(
    10,
    targetEffects,
    heroesGoldFindMultiplier: 1,
    goldRushActive: false);
AssertEqual(44, targetedHit.AppliedDamage, "Targeted red click damage");
AssertEqual(56, targetedHit.RemainingHealth, "Targeted red remaining HP");
if (targetedHit.Killed) throw new Exception("Targeted fixture should remain alive");

var normalBlock = new EnemyBlockState(100, EnemyType.Red);
var normalKill = normalBlock.ApplyDamage(
    1000,
    new ArtifactLoadout().BuildEffects(),
    heroesGoldFindMultiplier: 1,
    goldRushActive: false);
if (!normalKill.Killed) throw new Exception("Overkill should destroy block");
AssertEqual(7, normalKill.Reward.Gold, "Normal block kill gold");
AssertEqual(9, normalKill.OverkillNormalized, "Overkill normalization", 1e-6);

var rainbowBlock = new EnemyBlockState(100, EnemyType.Rainbow);
var rainbowKill = rainbowBlock.ApplyDamage(
    1000,
    new ArtifactLoadout().BuildEffects(),
    heroesGoldFindMultiplier: 1,
    goldRushActive: false);
AssertEqual(670, rainbowKill.Reward.Gold, "Rainbow ten-pickup gold");

var timeCubeBlock = new EnemyBlockState(
    100,
    EnemyType.TimeCube,
    timeCubeCount: 5);
var cubeKill = timeCubeBlock.ApplyDamage(
    1000,
    new ArtifactLoadout().BuildEffects(),
    heroesGoldFindMultiplier: 1,
    goldRushActive: false);
AssertEqual(0, cubeKill.Reward.Gold, "Time Cube block has no gold reward");
AssertEqual(5, (double)cubeKill.Reward.TimeCubes, "Time Cube block reward count");

// GameState deferred special-cube pickups and per-timeline reward history
var combatGame = new GameState();
var trackedCubeBlock = new EnemyBlockState(
    100,
    EnemyType.TimeCube,
    timeCubeCount: 3);
combatGame.ApplyDamageToBlock(
    trackedCubeBlock,
    1000,
    nowSeconds: 0);

AssertEqual(0, (double)combatGame.TimeCubes.PendingTimelineReward, "Time Cubes are not credited at block death");
AssertEqual(3, combatGame.CubePickups.Count, "Time Cube pickup object count");

var firstTrackedPickup = combatGame.CubePickups.Active[0];
if (firstTrackedPickup.IsColliderExpanded(1.999))
    throw new Exception("Pickup collider must remain original size before two seconds");
if (!firstTrackedPickup.IsColliderExpanded(2.0))
    throw new Exception("Pickup collider must expand at two seconds");

AssertEqual(
    0,
    combatGame.UpdateCubePickups(5.49).Count,
    "Auto pickup must wait for 0.5-second collection tween");
AssertEqual(0, (double)combatGame.TimeCubes.PendingTimelineReward, "No Time Cubes before collection tween completes");

var autoCollected = combatGame.UpdateCubePickups(5.5);
AssertEqual(3, autoCollected.Count, "Three Time Cube pickups auto-collected");
AssertEqual(3, (double)combatGame.TimeCubes.PendingTimelineReward, "Auto-collected Time Cubes enter pending timeline reward");
AssertEqual(0, combatGame.CubePickups.Count, "Collected Time Cube pickups are removed");

if (!combatGame.ArenaRewards.HasTimeCubeReward(combatGame.Arena.Wave))
    throw new Exception("Destroyed Time Cube block must mark current wave");

// Manual Weapon Cube collection uses the same original 0.5-second tween.
var weaponPickupBlock = new EnemyBlockState(
    100,
    EnemyType.WeaponCube,
    weaponCubeCount: 2);
combatGame.ApplyDamageToBlock(
    weaponPickupBlock,
    1000,
    nowSeconds: 10);

AssertEqual(2, combatGame.CubePickups.Count, "Weapon Cube pickup object count");
long manualWeaponPickupId = combatGame.CubePickups.Active[0].Id;
if (!combatGame.TryCollectCubePickup(manualWeaponPickupId, 10.2))
    throw new Exception("Manual Weapon Cube collection should start");

AssertEqual(
    0,
    combatGame.UpdateCubePickups(10.69).Count,
    "Manual collection must wait the full 0.5-second tween");
AssertEqual(0, (double)combatGame.WeaponCubes.Spendable, "Weapon Cube not credited before tween completes");

AssertEqual(
    1,
    combatGame.UpdateCubePickups(10.7).Count,
    "Manual Weapon Cube collection completion");
AssertEqual(1, (double)combatGame.WeaponCubes.Spendable, "Manual Weapon Cube bank credit");

// The second pickup follows the 5.0 s auto-collect + 0.5 s tween path.
AssertEqual(
    1,
    combatGame.UpdateCubePickups(15.5).Count,
    "Remaining Weapon Cube auto-collection");
AssertEqual(2, (double)combatGame.WeaponCubes.Spendable, "Both Weapon Cubes collected");

combatGame.TimeWarp();
if (combatGame.ArenaRewards.HasTimeCubeReward(combatGame.Arena.Wave))
    throw new Exception("Time Warp must clear per-wave cube reward history");

// Physical Gold pickup lifecycle
var goldPickupGame = new GameState();
double startingPickupGold = goldPickupGame.Gold.TotalGold;

var normalGoldBlock = new EnemyBlockState(
    100,
    EnemyType.Red);

var normalGoldKill =
    goldPickupGame.ApplyDamageToBlock(
        normalGoldBlock,
        1000,
        nowSeconds: 0);

AssertEqual(7, normalGoldKill.Reward.Gold, "Normal kill Gold reward value");
AssertEqual(startingPickupGold, goldPickupGame.Gold.TotalGold, "Kill Gold waits for physical pickup collection");
AssertEqual(1, goldPickupGame.GoldPickups.Count, "Normal kill spawns one Gold pickup");

AssertEqual(
    0,
    goldPickupGame.UpdateGoldPickups(3.74).Count,
    "Default Gold pickup waits 0.25 + 3 + 0.5 seconds");
AssertEqual(startingPickupGold, goldPickupGame.Gold.TotalGold, "Gold bank unchanged before pickup tween completes");

AssertEqual(
    1,
    goldPickupGame.UpdateGoldPickups(3.75).Count,
    "Default Gold pickup auto-collection completion");
AssertEqual(startingPickupGold + 7, goldPickupGame.Gold.TotalGold, "Collected normal Gold enters bank");

var rainbowGoldBlock = new EnemyBlockState(
    100,
    EnemyType.Rainbow);

var rainbowGoldKill =
    goldPickupGame.ApplyDamageToBlock(
        rainbowGoldBlock,
        1000,
        nowSeconds: 10);

AssertEqual(670, rainbowGoldKill.Reward.Gold, "Rainbow total Gold reward");
AssertEqual(10, goldPickupGame.GoldPickups.Count, "Rainbow block spawns ten physical Gold pickups");
AssertEqual(67, goldPickupGame.GoldPickups.Active[0].GoldValue, "Rainbow per-pickup Gold value");

goldPickupGame.UpdateGoldPickups(13.75);
AssertEqual(startingPickupGold + 677, goldPickupGame.Gold.TotalGold, "Rainbow ten-pickup collection total");

// ClickerPistol.ProcessHit hit-gold branch on special cube blocks.
goldPickupGame.WeaponAugments.SetLevel(
    WeaponAugmentType.ClickPistolGoldSpawnChance,
    1);
goldPickupGame.WeaponAugments.SetLevel(
    WeaponAugmentType.ClickPistolGoldSpawnValue,
    1);

var hitGoldSource = new EnemyBlockState(
    100,
    EnemyType.TimeCube,
    timeCubeCount: 1);

var hitGoldPickup =
    goldPickupGame.TrySpawnClickPistolHitGold(
        hitGoldSource,
        random01: 0f,
        nowSeconds: 20);

if (hitGoldPickup is null)
    throw new Exception("Owned hit-gold augment with zero random roll should spawn");

AssertEqual(0.5, hitGoldPickup.TimeBeforeCollection, "Hit Gold collection delay");
AssertEqual(4, hitGoldPickup.FloatAwaySpeed, "Hit Gold float-away speed");
AssertEqual(1, hitGoldPickup.GoldValue, "Hit Gold value fixture");

AssertEqual(
    0,
    goldPickupGame.UpdateGoldPickups(23.99).Count,
    "Hit Gold waits 0.5 + 3 + 0.5 seconds");

AssertEqual(
    1,
    goldPickupGame.UpdateGoldPickups(24.0).Count,
    "Hit Gold auto-collection completion");

// Offline progression
var offline = OfflineProgression.Calculate(
    secondsSinceSave: 200000,
    teamDps: 100,
    timelineGoldPerSecond: 10,
    heroesGoldFindMultiplier: 2,
    artifactGoldFindMultiplier: 3,
    currentArenaBaseHp: 50);
AssertEqual(172800, offline.Seconds, "Offline cap");
AssertEqual(8640000, offline.OfflineDamage, "Offline half-DPS damage");
AssertEqual(864000, offline.GoldEarned, "Offline stored-GPS gold");
AssertEqual(43200, offline.EstimatedBaseBlocksDestroyed, "Offline kill estimate cap");

var offlineFallback = OfflineProgression.Calculate(
    secondsSinceSave: 172800,
    teamDps: 100,
    timelineGoldPerSecond: 0,
    heroesGoldFindMultiplier: 2,
    artifactGoldFindMultiplier: 3,
    currentArenaBaseHp: 50);
AssertEqual(3456000, offlineFallback.GoldEarned, "Offline damage-to-gold fallback");

// Sequential ability purchasing and Dimension Shift
var abilityGold = new GoldWallet();
abilityGold.Add(1e21);
var abilityProgression = new ActiveAbilityProgression(
    new ArtifactLoadout().BuildEffects());

for (int i = 0; i <= (int)AbilityType.DimensionShift; i++)
{
    if (!abilityProgression.TryPurchaseNext(abilityGold))
        throw new Exception($"Ability purchase {i} should succeed");
}
AssertEqual(7, abilityProgression.PurchasedCount, "Purchased abilities through Dimension Shift");
AssertEqual(
    250 * Math.Pow(90, 7),
    abilityProgression.NextPurchaseCost,
    "Next sequential ability purchase cost",
    1e-12);

if (!abilityProgression.Activate(AbilityType.DimensionShift, 100))
    throw new Exception("Purchased Dimension Shift should activate");
AssertEqual(1, abilityProgression.DimensionShifts, "Dimension Shift stack count");
AssertEqual(
    1.05,
    abilityProgression.GetDimensionShiftMultiplier(new ArtifactLoadout().BuildEffects()),
    "Dimension Shift DPS multiplier",
    1e-12);

abilityProgression.Update(100);
if (abilityProgression.Abilities[(int)AbilityType.DimensionShift].State != AbilityState.Recharging)
    throw new Exception("Zero-duration Dimension Shift should enter recharge on update");

abilityProgression.TimeWarp(new ArtifactLoadout().BuildEffects());
AssertEqual(0, abilityProgression.PurchasedCount, "Ability purchases reset on Time Warp");
AssertEqual(0, abilityProgression.DimensionShifts, "Dimension Shifts reset on Time Warp");

// VoxelLibrary HP decomposition/model selection
var wave5Plan = VoxelSpawnMath.BuildHpPlan(
    ArenaMath.GetArenaHP(5),
    VoxelSpawnMath.GetMinEnemyCountForWave(5),
    VoxelSpawnMath.GetMaxEnemyCountForWave(5));
AssertEqual(10, wave5Plan.BaseHp, "Wave 5 voxel base HP");
AssertEqual(39, wave5Plan.RedRequired, "Wave 5 red blocks");
AssertEqual(3, wave5Plan.WhiteRequired, "Wave 5 white blocks");
AssertEqual(0, wave5Plan.YellowRequired, "Wave 5 yellow blocks");
AssertEqual(42, wave5Plan.TotalRequired, "Wave 5 required total");
AssertEqual(52, wave5Plan.CandidateMaxEnemyCount, "Wave 5 candidate max");

var wave10Plan = VoxelSpawnMath.BuildHpPlan(
    ArenaMath.GetArenaHP(10),
    VoxelSpawnMath.GetMinEnemyCountForWave(10),
    VoxelSpawnMath.GetMaxEnemyCountForWave(10));
AssertEqual(56, wave10Plan.RedRequired, "Wave 10 red blocks");
AssertEqual(54, wave10Plan.WhiteRequired, "Wave 10 white blocks");
AssertEqual(1, wave10Plan.YellowRequired, "Wave 10 yellow blocks");
AssertEqual(111, wave10Plan.TotalRequired, "Wave 10 required total");

var transformedCounts = VoxelSpawnMath.TransformEnemyCounts(
    new[] { 20, 10, 5 },
    convertYellowToWhite: 3,
    convertWhiteToRed: 4);
AssertEqual(24, transformedCounts[0], "Transformed red requirement");
AssertEqual(9, transformedCounts[1], "Transformed white requirement");
AssertEqual(2, transformedCounts[2], "Transformed yellow requirement");

var syntheticModels = new[]
{
    new VoxelModelDescriptor("too-small", 0, 41),
    new VoxelModelDescriptor("regular-a", 0, 42),
    new VoxelModelDescriptor("regular-b", 0, 50),
    new VoxelModelDescriptor("too-large", 0, 53),
    new VoxelModelDescriptor("future", 100, 45),
    new VoxelModelDescriptor("boss-5", 999999, 120, 5)
};

var bossSelected = VoxelSpawnMath.SelectModel(
    syntheticModels, wave5Plan, 5, randomIndex: 0);
if (bossSelected?.Id != "boss-5")
    throw new Exception("Exact boss voxel must bypass normal filters");

var regularSelected = VoxelSpawnMath.SelectModel(
    syntheticModels, wave5Plan, 6, randomIndex: 1);
if (regularSelected?.Id != "regular-b")
    throw new Exception("Voxel candidate selection should use filtered random index");

var eligibleAt6 = VoxelSpawnMath.GetEligibleModels(
    syntheticModels, wave5Plan, 6);
AssertEqual(2, eligibleAt6.Count, "Eligible voxel model count");

// Voxel block allocation
var syntheticLayout = new VoxelModelLayout(
    "synthetic",
    Red: new[] { new VoxelPoint(0, 0, 0) },
    White: new[] { new VoxelPoint(1, 0, 0), new VoxelPoint(2, 0, 0) },
    Yellow: new[] { new VoxelPoint(3, 0, 0) },
    Blue: new[] { new VoxelPoint(4, 0, 0), new VoxelPoint(5, 0, 0) });

var allocation = VoxelSpawnPlanner.Build(
    syntheticLayout,
    baseHp: 10,
    requiredCounts: new[] { 2, 1, 1 },
    rainbowConversions: new[] { 1, 0, 0 });

AssertEqual(4, allocation.SpawnedBlockCount, "Voxel allocation block count");
if (allocation.Blocks[0].EnemyType != EnemyType.Rainbow)
    throw new Exception("First red requirement should convert to Rainbow");
AssertEqual(10, allocation.Blocks[0].MaxHealth, "Rainbow conversion uses base HP");
if (allocation.Blocks[1].EnemyType != EnemyType.White)
    throw new Exception("White matching slot type");
AssertEqual(100, allocation.Blocks[1].MaxHealth, "White tier HP");
if (allocation.Blocks[2].EnemyType != EnemyType.Yellow)
    throw new Exception("Yellow matching slot type");
AssertEqual(1000, allocation.Blocks[2].MaxHealth, "Yellow tier HP");
if (allocation.Blocks[3].EnemyType != EnemyType.Red)
    throw new Exception("Red fallback retains required enemy type");
AssertEqual(2, allocation.Blocks[3].Position.X, "Red fallback takes next white slot");

var specialAllocation = VoxelSpawnPlanner.Build(
    syntheticLayout,
    baseHp: 10,
    requiredCounts: new[] { 1, 1, 1 },
    rainbowConversions: new[] { 0, 0, 0 },
    timeCubeReward: 7,
    weaponCubeReward: 9);
AssertEqual(3, specialAllocation.SpawnedBlockCount, "Cube replacement allocation count");
if (specialAllocation.Blocks[0].EnemyType != EnemyType.TimeCube)
    throw new Exception("Time Cube must occupy white slot first");
AssertEqual(1000, specialAllocation.Blocks[0].MaxHealth, "Time Cube yellow-tier HP");
AssertEqual(7, specialAllocation.Blocks[0].TimeCubeCount, "Time Cube reward payload");
if (specialAllocation.Blocks[1].EnemyType != EnemyType.WeaponCube)
    throw new Exception("Weapon Cube must occupy yellow slot second");
AssertEqual(9, specialAllocation.Blocks[1].WeaponCubeCount, "Weapon Cube reward payload");

var forcedRainbow = VoxelSpawnPlanner.Build(
    syntheticLayout,
    baseHp: 10,
    requiredCounts: new[] { 99, 99, 99 },
    rainbowConversions: new[] { 0, 0, 0 },
    forceRainbowEnemy: true);
AssertEqual(4, forcedRainbow.SpawnedBlockCount, "Forced Rainbow excludes blue fallback slots");
if (forcedRainbow.Blocks.Any(b => b.EnemyType != EnemyType.Rainbow))
    throw new Exception("Forced Rainbow model must contain only Rainbow blocks");

var firstEnemy = VoxelSpawnPlanner.Build(
    syntheticLayout,
    baseHp: 10,
    requiredCounts: new[] { 1, 0, 0 },
    rainbowConversions: new[] { 1, 0, 0 },
    isVeryFirstEnemy: true);
AssertEqual(1, firstEnemy.SpawnedBlockCount, "Very first enemy is a single block");
if (firstEnemy.Blocks[0].EnemyType != EnemyType.Rainbow)
    throw new Exception("Very first enemy honors red-to-rainbow conversion");

var modelStateFromPlan = VoxelSpawnPlanner.CreateEnemyModel(allocation);
AssertEqual(4, modelStateFromPlan.BlockCount, "Spawn plan creates enemy model state");

// End-to-end deterministic headless Arena: new game -> boss 5 -> wave 6
static VoxelPoint[] Points(int count, float offset = 0) =>
    Enumerable.Range(0, count)
        .Select(i => new VoxelPoint(offset + i, 0, 0))
        .ToArray();

var arenaCatalog = new InMemoryVoxelModelCatalog(
    new[]
    {
        new VoxelModelDescriptor("small", 0, 11),
        new VoxelModelDescriptor("boss5", 999999, 42, BossWave: 5)
    },
    new[]
    {
        new VoxelModelLayout(
            "small",
            Red: Points(11),
            White: Array.Empty<VoxelPoint>(),
            Yellow: Array.Empty<VoxelPoint>(),
            Blue: Array.Empty<VoxelPoint>()),
        new VoxelModelLayout(
            "boss5",
            Red: Points(39),
            White: Points(3, 100),
            Yellow: Array.Empty<VoxelPoint>(),
            Blue: Array.Empty<VoxelPoint>())
    });

var endToEndGame = new GameState();
var arenaEngine = new HeadlessArenaEngine(endToEndGame, arenaCatalog);
var deterministicRolls = new ArenaSpawnRolls(
    ModelRandomIndex: 0,
    RainbowEnemyRoll: 1f,
    TimeCubeRoll: 1f,
    WeaponCubeRoll: 1f);

while (endToEndGame.Arena.Wave < 5)
{
    int startingWave = endToEndGame.Arena.Wave;

    for (int enemy = 0; enemy < endToEndGame.ArtifactEffects.EnemiesToAdvance; enemy++)
    {
        var spawned = arenaEngine.SpawnCurrentEnemy(deterministicRolls);
        int blockCount = spawned.Model.BlockCount;
        ArenaBlockAttackResult? finalHit = null;

        for (int blockIndex = 0; blockIndex < blockCount; blockIndex++)
            finalHit = arenaEngine.DamageBlock(blockIndex, double.MaxValue);

        if (enemy < endToEndGame.ArtifactEffects.EnemiesToAdvance - 1)
        {
            if (finalHit?.ArenaResult != ArenaClearResult.ContinueSameWave)
                throw new Exception("Regular wave advanced before required enemy count");
        }
    }

    AssertEqual(startingWave + 1, endToEndGame.Arena.Wave, "Headless regular wave progression");
}

AssertEqual(5, endToEndGame.Arena.Wave, "Reached first boss wave");
var bossSpawn = arenaEngine.SpawnCurrentEnemy(deterministicRolls, nowSeconds: 100);
if (bossSpawn.ModelId != "boss5")
    throw new Exception("Exact wave-5 boss model should be selected");
AssertEqual(42, bossSpawn.Model.BlockCount, "Boss 5 block allocation");
if (!endToEndGame.Arena.FightingBoss)
    throw new Exception("Boss timer should be running");

ArenaBlockAttackResult? bossFinal = null;
for (int blockIndex = 0; blockIndex < bossSpawn.Model.BlockCount; blockIndex++)
    bossFinal = arenaEngine.DamageBlock(blockIndex, double.MaxValue, nowSeconds: 105);

if (bossFinal?.ArenaResult != ArenaClearResult.AdvancedToNextWave)
    throw new Exception("Boss model clear should advance Arena");
AssertEqual(6, endToEndGame.Arena.Wave, "Headless boss clear reaches wave 6");
if (arenaEngine.CurrentEnemy is not null)
    throw new Exception("Cleared model should be released by headless engine");

// Exact Qubicle -> Arena local coordinate transform
var centered = VoxelCoordinateMath.ToArenaLocalPosition(
    new VoxelPoint(6, 3, 2),
    new VoxelPoint(13, 13, 13));
AssertEqual(0, centered.X, "Voxel centered X");
AssertEqual(3, centered.Y, "Voxel preserves Y");
AssertEqual(4, centered.Z, "Voxel centered/inverted Z");

var centeredCorner = VoxelCoordinateMath.ToArenaLocalPosition(
    new VoxelPoint(0, 0, 0),
    new VoxelPoint(10, 5, 8));
AssertEqual(-4.5, centeredCorner.X, "Voxel even-size X offset");
AssertEqual(0, centeredCorner.Y, "Voxel even-size Y");
AssertEqual(3.5, centeredCorner.Z, "Voxel even-size Z inversion");

// Original ClickerUtils presentation formatting
if (DisplayFormatting.FormatValue(999) != "999")
    throw new Exception("Simplified value below 1000");
if (DisplayFormatting.FormatValue(1000) != "1.00K")
    throw new Exception("Simplified 1000");
if (DisplayFormatting.FormatValue(1234) != "1.23K")
    throw new Exception("Simplified 1234");
if (DisplayFormatting.FormatValue(999999) != "999K")
    throw new Exception("Simplified 999999");
if (DisplayFormatting.FormatValue(1_000_000) != "1.00M")
    throw new Exception("Simplified one million");

if (DisplayFormatting.FormatValue(
        1_234_567,
        NumberDisplayType.Scientific) != "1.23e⁶")
    throw new Exception("Engineering scientific formatting");

if (DisplayFormatting.FormatValue(
        1_999,
        NumberDisplayType.ScientificNormalized) != "1.99e³")
    throw new Exception("Normalized scientific truncation");

if (DisplayFormatting.GetSuffix(6) != "a" ||
    DisplayFormatting.GetSuffix(31) != "z" ||
    DisplayFormatting.GetSuffix(32) != "a¹")
    throw new Exception("Suffix rollover formatting");

if (DisplayFormatting.GetSuperscriptNumber(-12) != "⁻¹²")
    throw new Exception("Superscript formatting");

if (DisplayFormatting.FormatSeconds(3661) != "1:01:01")
    throw new Exception("Integer duration formatting");
if (DisplayFormatting.FormatSeconds(60f) != "60.0")
    throw new Exception("Float duration <= 60");
if (DisplayFormatting.FormatSeconds(61.29f) != "1:01.2")
    throw new Exception("Float duration > 60");

if (DisplayFormatting.FormatULong(999_999) != "999,999" ||
    DisplayFormatting.FormatULong(1_234_567) != "1.234 M")
    throw new Exception("ULong formatting");

// Original ArenaDisplay / UIWaveHP / WidgetGold snapshot
var hudGame = new GameState();
hudGame.Gold.Add(12_000);

var hudBlock = new EnemyBlockState(
    100,
    EnemyType.Red);
hudBlock.ApplyDamage(
    25,
    hudGame.ArtifactEffects,
    heroesGoldFindMultiplier: 1,
    goldRushActive: false);
var hudModel = new EnemyModelState(
    new[] { hudBlock });

var hud = ArenaHudMath.Build(
    hudGame,
    hudModel,
    nowSeconds: 0);

AssertEqual(75, hud.RemainingHp, "HUD remaining HP");
AssertEqual(100, hud.TotalHp, "HUD total HP");
AssertEqual(0.75, hud.HpNormalized, "HUD normalized HP");
if (hud.WaveHpText != "75 HP")
    throw new Exception("HUD wave HP text");
if (hud.ArenaRemainingText != "10" || hud.ShowInfinity)
    throw new Exception("Highest-wave ArenaDisplay remaining state");
if (hud.GoldText != "13.0K")
    throw new Exception($"HUD gold formatting: {hud.GoldText}");

hudGame.Arena.TimeWarpTo(5);
hudGame.Arena.StartArena(
    nowSeconds: 100,
    hudGame.ArtifactEffects.BossTime);

var bossHud = ArenaHudMath.Build(
    hudGame,
    hudModel,
    nowSeconds: 110);

if (!bossHud.IsBossWave)
    throw new Exception("Boss HUD visibility");
AssertEqual(20, bossHud.BossTimeRemaining, "Boss HUD time remaining");
AssertEqual(2.0 / 3.0, bossHud.BossTimeNormalized, "Boss HUD timer normalized", 1e-6);
if (bossHud.BossTimeText != "20.0")
    throw new Exception($"Boss HUD formatted time: {bossHud.BossTimeText}");

hudGame.Arena.TimeWarpTo(2);
hudGame.Arena.RequestPreviousArena();

var farmHud = ArenaHudMath.Build(
    hudGame,
    hudModel,
    nowSeconds: 0);

if (!farmHud.ShowInfinity ||
    !farmHud.IsFarmingLowerWave ||
    farmHud.ArenaRemainingText != string.Empty)
{
    throw new Exception("Lower-wave ArenaDisplay infinity state");
}

Console.WriteLine("PortCore smoke tests passed.");
