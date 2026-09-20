using TimeClickers.PortCore;
using UnityEngine;

namespace TimeCli.UnityRuntime
{
    public sealed class PrototypeHud : MonoBehaviour
    {
        [SerializeField]
        private TimeCliBootstrap bootstrap;

        [SerializeField]
        private ArenaRuntimeController arena;

        private Vector2 _scroll;

        private void Start()
        {
            if (bootstrap == null)
                bootstrap = FindFirstObjectByType<TimeCliBootstrap>();

            if (arena == null)
                arena = FindFirstObjectByType<ArenaRuntimeController>();
        }

        private void OnGUI()
        {
            if (bootstrap == null || !bootstrap.IsReady)
                return;

            GUILayout.BeginArea(new Rect(12, 12, 340, Screen.height - 24), GUI.skin.box);
            _scroll = GUILayout.BeginScrollView(_scroll);

            var game = bootstrap.Game;

            GUILayout.Label("<b>TimeCli — Phase 3 prototype</b>", RichLabel());
            GUILayout.Space(6);

            GUILayout.Label($"Wave: {game.Arena.Wave} / Max {game.Arena.MaxWave}");
            GUILayout.Label($"Gold: {game.Gold.TotalGold:0.##}");
            GUILayout.Label($"Team DPS: {game.GetTeamDps():0.##}");
            GUILayout.Label($"Click: {game.GetClickDamage():0.##}");
            GUILayout.Label(
                $"Time Cubes: {game.TimeCubes.Spendable} " +
                $"(+{game.TimeCubes.PendingTimelineReward})");
            GUILayout.Label($"Weapon Cubes: {game.WeaponCubes.Spendable}");

            if (game.Arena.FightingBoss)
            {
                GUILayout.Label(
                    $"Boss: {game.Arena.GetBossTimeRemaining(Time.timeAsDouble):0.0}s");
            }

            GUILayout.Space(8);
            GUILayout.Label("<b>Farm navigation</b>", RichLabel());

            GUILayout.BeginHorizontal();
            GUI.enabled = game.Arena.CanGoPrevious;
            if (GUILayout.Button("Previous"))
                arena.RequestPreviousWave();

            GUI.enabled = game.Arena.CanGoNext;
            if (GUILayout.Button("Next"))
                arena.RequestNextWave();

            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.Label("<b>Click Pistol</b>", RichLabel());

            if (GUILayout.Button("Buy +1 Click level"))
            {
                if (game.TryPurchaseClickPistol(HeroBuyMode.OneLevel))
                    arena.RebuildAfterExternalStateChange();
            }

            if (GUILayout.Button("Buy to next Click upgrade"))
            {
                if (game.TryPurchaseClickPistol(HeroBuyMode.NextUpgrade))
                    arena.RebuildAfterExternalStateChange();
            }

            GUILayout.Space(8);
            GUILayout.Label("<b>Heroes</b>", RichLabel());

            for (int i = 0; i < game.Heroes.Length; i++)
            {
                var hero = game.Heroes[i];

                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label(
                    $"{hero.Spec.Name}: LV {hero.Level}, Rank {hero.Rank}, " +
                    $"DPS {game.GetHeroDps(i):0.##}");

                GUILayout.BeginHorizontal();

                if (GUILayout.Button("+1"))
                    game.TryPurchaseHero(i, HeroBuyMode.OneLevel);

                if (GUILayout.Button("Next upgrade"))
                    game.TryPurchaseHero(i, HeroBuyMode.NextUpgrade);

                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }

            GUILayout.Space(8);
            GUILayout.Label("<b>Timeline debug controls</b>", RichLabel());

            GUI.enabled = game.TimeCubes.PendingTimelineReward > 0;

            if (GUILayout.Button(
                $"Time Warp (+{game.TimeCubes.PendingTimelineReward} TC)"))
            {
                game.TimeWarp();
                arena.RebuildAfterExternalStateChange();
            }

            GUI.enabled = true;

            GUILayout.Label(
                "This HUD is development UI only; original UI reconstruction " +
                "will replace it.");

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private static GUIStyle RichLabel()
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                richText = true
            };
            return style;
        }
    }
}
