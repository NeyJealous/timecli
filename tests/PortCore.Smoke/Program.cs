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

Console.WriteLine("PortCore smoke tests passed.");
