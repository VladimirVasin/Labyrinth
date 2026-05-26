using System.Collections.Generic;
using Labyrinth.Combat;
using Labyrinth.Hero;
using Labyrinth.Maze;
using UnityEngine;

namespace Labyrinth.Core
{
    public sealed class HeroConsumableAutomation
    {
        private readonly ResourceWallet resources;
        private readonly BaseDevelopment baseDevelopment;
        private readonly MazeRenderer mazeRenderer;

        public HeroConsumableAutomation(
            ResourceWallet resources,
            BaseDevelopment baseDevelopment,
            MazeRenderer mazeRenderer)
        {
            this.resources = resources;
            this.baseDevelopment = baseDevelopment;
            this.mazeRenderer = mazeRenderer;
        }

        public void Update(IReadOnlyList<HeroController> heroes, Vector2Int shopPosition)
        {
            if (heroes == null
                || resources == null
                || baseDevelopment == null
                || (!baseDevelopment.HasAlchemistShop
                    && !baseDevelopment.HasTavern
                    && !baseDevelopment.HasForge
                    && !baseDevelopment.HasInfirmary
                    && !baseDevelopment.HasChapel))
            {
                return;
            }

            for (var i = 0; i < heroes.Count; i++)
            {
                var hero = heroes[i];
                if (hero == null || hero.Model == null || !hero.Model.IsAlive)
                {
                    continue;
                }

                if (hero.Model.Position == shopPosition)
                {
                    RefreshChapelBlessings(hero);
                    TryHealAtInfirmary(hero);
                    StockHealthPotions(hero);
                    StockRations(hero);
                    StockForgeEquipment(hero);
                    StockChapelBlessings(hero);
                }

                UseAvailableHealthPotions(hero);
                UseAvailableRations(hero);
            }
        }

        private void UseAvailableHealthPotions(HeroController hero)
        {
            while (TryUseHealthPotion(hero))
            {
            }
        }

        private void UseAvailableRations(HeroController hero)
        {
            while (TryUseRation(hero))
            {
            }
        }

        private void StockHealthPotions(HeroController hero)
        {
            while (TryBuyHealthPotion(hero))
            {
            }
        }

        private void StockRations(HeroController hero)
        {
            while (TryBuyRation(hero))
            {
            }
        }

        private void StockForgeEquipment(HeroController hero)
        {
            if (!baseDevelopment.HasForge)
            {
                return;
            }

            if (baseDevelopment.ForgeLevel >= 3)
            {
                TryBuyMasterBlade(hero);
                TryBuyPlateHarness(hero);
                TryBuySwiftwalkerBoots(hero);
                return;
            }

            if (baseDevelopment.ForgeLevel >= 2)
            {
                TryBuyKnightSword(hero);
                TryBuyBrigandine(hero);
                TryBuyPathfinderBoots(hero);
                return;
            }

            TryBuySteelSword(hero);
            TryBuyChainmail(hero);
            TryBuyLeatherBoots(hero);
        }

        private void RefreshChapelBlessings(HeroController hero)
        {
            if (hero?.Model?.Blessings == null || !hero.Model.Blessings.HasLeftEntrance)
            {
                return;
            }

            hero.Model.ClearExpeditionBlessings();
            GameDebugLog.Info("Hero", "Hero returned to entrance and cleared expedition blessings.");
        }

        private void StockChapelBlessings(HeroController hero)
        {
            if (!baseDevelopment.HasChapel || hero?.Model == null)
            {
                return;
            }

            foreach (var blessing in HeroBlessingCatalog.PurchaseOrder)
            {
                if (hero.Model.Blessings.ActiveCount >= HeroBlessings.MaxActiveBlessings)
                {
                    return;
                }

                TryBuyBlessing(hero, blessing);
            }
        }

        private bool TryBuyBlessing(HeroController hero, HeroBlessingDefinition blessing)
        {
            if (hero.Model.HasBlessing(blessing.Type)
                || !hero.Model.TrySpendGold(blessing.GoldCost))
            {
                return false;
            }

            if (!hero.Model.TryActivateBlessing(blessing.Type))
            {
                hero.Model.AddGold(blessing.GoldCost);
                return false;
            }

            resources.AddGold(blessing.GoldCost);
            DamageNumberView.CreateText(
                mazeRenderer,
                baseDevelopment.ChapelPosition,
                blessing.DisplayName,
                new Color(1f, 0.9f, 0.48f),
                2.25f);
            GameAudioController.Play(GameSfx.Purchase, mazeRenderer.GridToWorld(baseDevelopment.ChapelPosition), 0.92f);
            GameDebugLog.Info(
                "Hero",
                $"Hero bought blessing {blessing.Type} for {blessing.GoldCost} gold. heroGold={hero.Model.Gold}, treasuryGold={resources.Gold}, activeBlessings={hero.Model.Blessings.ActiveCount}/{HeroBlessings.MaxActiveBlessings}");
            return true;
        }

        private bool TryBuyHealthPotion(HeroController hero)
        {
            if (!baseDevelopment.HasAlchemistShop
                || hero.Model.Inventory == null
                || !hero.Model.Inventory.CanAddHealthPotionWithLimit(baseDevelopment.HealthPotionMaxCount)
                || !hero.Model.TrySpendGold(BaseDevelopment.HealthPotionGoldCost))
            {
                return false;
            }

            if (!hero.Model.Inventory.TryAddHealthPotion(baseDevelopment.HealthPotionMaxCount, baseDevelopment.HealthPotionHealAmount))
            {
                hero.Model.AddGold(BaseDevelopment.HealthPotionGoldCost);
                return false;
            }

            resources.AddGold(BaseDevelopment.HealthPotionGoldCost);
            GameAudioController.Play(GameSfx.PotionPurchase, mazeRenderer.GridToWorld(baseDevelopment.AlchemistShopPosition));
            GameDebugLog.Info(
                "Hero",
                $"Hero bought health potion for {BaseDevelopment.HealthPotionGoldCost} gold. heroGold={hero.Model.Gold}, treasuryGold={resources.Gold}");
            return true;
        }

        private bool TryHealAtInfirmary(HeroController hero)
        {
            if (!baseDevelopment.HasInfirmary
                || hero == null
                || hero.Model == null
                || !hero.Model.IsAlive
                || hero.Model.State == HeroState.Fighting)
            {
                return false;
            }

            var missingHitPoints = hero.Model.MaxHitPoints - hero.Model.HitPoints;
            var affordableHitPoints = resources.Food / BaseDevelopment.InfirmaryFoodPerHitPoint;
            var requestedHealing = Mathf.Min(missingHitPoints, affordableHitPoints);
            if (requestedHealing <= 0)
            {
                return false;
            }

            var foodCost = requestedHealing * BaseDevelopment.InfirmaryFoodPerHitPoint;
            if (!resources.TrySpendFood(foodCost))
            {
                return false;
            }

            var restored = hero.Model.RestoreHitPoints(requestedHealing);
            if (restored <= 0)
            {
                resources.AddFood(foodCost);
                return false;
            }

            var unusedFood = (requestedHealing - restored) * BaseDevelopment.InfirmaryFoodPerHitPoint;
            if (unusedFood > 0)
            {
                resources.AddFood(unusedFood);
            }

            DamageNumberView.CreateText(
                mazeRenderer,
                baseDevelopment.InfirmaryPosition,
                $"+{restored} HP",
                new Color(0.52f, 1f, 0.62f),
                1.95f);
            GameAudioController.Play(GameSfx.Potion, mazeRenderer.GridToWorld(baseDevelopment.InfirmaryPosition), 0.9f);
            GameDebugLog.Info(
                "Hero",
                $"Hero healed at infirmary: restored={restored}, foodSpent={restored * BaseDevelopment.InfirmaryFoodPerHitPoint}, foodLeft={resources.Food}, hp={hero.Model.HitPoints}/{hero.Model.MaxHitPoints}");
            return true;
        }

        private bool TryBuyRation(HeroController hero)
        {
            if (!baseDevelopment.HasTavern
                || hero.Model.Inventory == null
                || !hero.Model.Inventory.CanAddRationWithLimit(baseDevelopment.RationMaxCount)
                || !resources.CanSpendFood(BaseDevelopment.RationFoodCost)
                || !hero.Model.TrySpendGold(BaseDevelopment.RationGoldCost))
            {
                return false;
            }

            if (!resources.TrySpendFood(BaseDevelopment.RationFoodCost))
            {
                hero.Model.AddGold(BaseDevelopment.RationGoldCost);
                return false;
            }

            if (!hero.Model.Inventory.TryAddRation(baseDevelopment.RationMaxCount, baseDevelopment.RationStaminaRestore))
            {
                hero.Model.AddGold(BaseDevelopment.RationGoldCost);
                resources.AddFood(BaseDevelopment.RationFoodCost);
                return false;
            }

            resources.AddGold(BaseDevelopment.RationGoldCost);
            GameAudioController.Play(GameSfx.RationPurchase, mazeRenderer.GridToWorld(baseDevelopment.TavernPosition));
            DamageNumberView.CreateText(
                mazeRenderer,
                baseDevelopment.TavernPosition,
                HeroInventory.RationItemName,
                new Color(1f, 0.78f, 0.36f),
                2.2f);
            GameDebugLog.Info(
                "Hero",
                $"Hero bought ration: foodSpent={BaseDevelopment.RationFoodCost}, goldPaid={BaseDevelopment.RationGoldCost}, heroGold={hero.Model.Gold}, treasuryGold={resources.Gold}, food={resources.Food}");
            return true;
        }

        private bool TryBuySteelSword(HeroController hero)
        {
            if (hero.Model.Inventory == null
                || !hero.Model.Inventory.CanEquipSteelSword
                || !hero.Model.TrySpendGold(BaseDevelopment.SteelSwordGoldCost))
            {
                return false;
            }

            if (!hero.Model.Inventory.TryEquipSteelSword(out _))
            {
                hero.Model.AddGold(BaseDevelopment.SteelSwordGoldCost);
                return false;
            }

            resources.AddGold(BaseDevelopment.SteelSwordGoldCost);
            ShowForgeText(HeroInventory.SteelSwordItemName);
            GameAudioController.Play(GameSfx.ForgeUpgrade, mazeRenderer.GridToWorld(baseDevelopment.ForgePosition));
            GameDebugLog.Info("Hero", $"Hero bought steel sword for {BaseDevelopment.SteelSwordGoldCost} gold. heroGold={hero.Model.Gold}, treasuryGold={resources.Gold}, treasuryIron={resources.Iron}, attack={hero.Model.AttackPoints}");
            return true;
        }

        private bool TryBuyChainmail(HeroController hero)
        {
            if (hero.Model.Inventory == null
                || !hero.Model.Inventory.CanEquipChainmail
                || !hero.Model.TrySpendGold(BaseDevelopment.ChainmailGoldCost))
            {
                return false;
            }

            if (!hero.Model.Inventory.TryEquipChainmail(out _))
            {
                hero.Model.AddGold(BaseDevelopment.ChainmailGoldCost);
                return false;
            }

            resources.AddGold(BaseDevelopment.ChainmailGoldCost);
            ShowForgeText(HeroInventory.ChainmailItemName);
            GameAudioController.Play(GameSfx.ForgeUpgrade, mazeRenderer.GridToWorld(baseDevelopment.ForgePosition));
            GameDebugLog.Info("Hero", $"Hero bought chainmail for {BaseDevelopment.ChainmailGoldCost} gold. heroGold={hero.Model.Gold}, treasuryGold={resources.Gold}, treasuryIron={resources.Iron}, armor={hero.Model.ArmorPoints}");
            return true;
        }

        private bool TryBuyLeatherBoots(HeroController hero)
        {
            if (hero.Model.Inventory == null
                || !hero.Model.Inventory.CanEquipLeatherBoots
                || !hero.Model.TrySpendGold(BaseDevelopment.LeatherBootsGoldCost))
            {
                return false;
            }

            if (!hero.Model.Inventory.TryEquipLeatherBoots(out _))
            {
                hero.Model.AddGold(BaseDevelopment.LeatherBootsGoldCost);
                return false;
            }

            resources.AddGold(BaseDevelopment.LeatherBootsGoldCost);
            ShowForgeText(HeroInventory.LeatherBootsItemName);
            GameAudioController.Play(GameSfx.ForgeUpgrade, mazeRenderer.GridToWorld(baseDevelopment.ForgePosition));
            GameDebugLog.Info("Hero", $"Hero bought leather boots for {BaseDevelopment.LeatherBootsGoldCost} gold. heroGold={hero.Model.Gold}, treasuryGold={resources.Gold}, moveSpeedBonus={hero.Model.MoveSpeedBonusPercent}%");
            return true;
        }

        private bool TryBuyKnightSword(HeroController hero)
        {
            if (hero.Model.Inventory == null
                || !hero.Model.Inventory.CanEquipKnightSword
                || !hero.Model.TrySpendGold(BaseDevelopment.KnightSwordGoldCost))
            {
                return false;
            }

            if (!hero.Model.Inventory.TryEquipKnightSword(out _))
            {
                hero.Model.AddGold(BaseDevelopment.KnightSwordGoldCost);
                return false;
            }

            resources.AddGold(BaseDevelopment.KnightSwordGoldCost);
            ShowForgeText(HeroInventory.KnightSwordItemName);
            GameAudioController.Play(GameSfx.ForgeUpgrade, mazeRenderer.GridToWorld(baseDevelopment.ForgePosition));
            GameDebugLog.Info("Hero", $"Hero bought knight sword for {BaseDevelopment.KnightSwordGoldCost} gold. heroGold={hero.Model.Gold}, treasuryGold={resources.Gold}, treasuryIron={resources.Iron}, attack={hero.Model.AttackPoints}");
            return true;
        }

        private bool TryBuyBrigandine(HeroController hero)
        {
            if (hero.Model.Inventory == null
                || !hero.Model.Inventory.CanEquipBrigandine
                || !hero.Model.TrySpendGold(BaseDevelopment.BrigandineGoldCost))
            {
                return false;
            }

            if (!hero.Model.Inventory.TryEquipBrigandine(out _))
            {
                hero.Model.AddGold(BaseDevelopment.BrigandineGoldCost);
                return false;
            }

            resources.AddGold(BaseDevelopment.BrigandineGoldCost);
            ShowForgeText(HeroInventory.BrigandineItemName);
            GameAudioController.Play(GameSfx.ForgeUpgrade, mazeRenderer.GridToWorld(baseDevelopment.ForgePosition));
            GameDebugLog.Info("Hero", $"Hero bought brigandine for {BaseDevelopment.BrigandineGoldCost} gold. heroGold={hero.Model.Gold}, treasuryGold={resources.Gold}, treasuryIron={resources.Iron}, armor={hero.Model.ArmorPoints}");
            return true;
        }

        private bool TryBuyPathfinderBoots(HeroController hero)
        {
            if (hero.Model.Inventory == null
                || !hero.Model.Inventory.CanEquipPathfinderBoots
                || !hero.Model.TrySpendGold(BaseDevelopment.PathfinderBootsGoldCost))
            {
                return false;
            }

            if (!hero.Model.Inventory.TryEquipPathfinderBoots(out _))
            {
                hero.Model.AddGold(BaseDevelopment.PathfinderBootsGoldCost);
                return false;
            }

            resources.AddGold(BaseDevelopment.PathfinderBootsGoldCost);
            ShowForgeText(HeroInventory.PathfinderBootsItemName);
            GameAudioController.Play(GameSfx.ForgeUpgrade, mazeRenderer.GridToWorld(baseDevelopment.ForgePosition));
            GameDebugLog.Info("Hero", $"Hero bought pathfinder boots for {BaseDevelopment.PathfinderBootsGoldCost} gold. heroGold={hero.Model.Gold}, treasuryGold={resources.Gold}, moveSpeedBonus={hero.Model.MoveSpeedBonusPercent}%");
            return true;
        }

        private bool TryBuyMasterBlade(HeroController hero)
        {
            if (hero.Model.Inventory == null
                || !hero.Model.Inventory.CanEquipMasterBlade
                || !hero.Model.TrySpendGold(BaseDevelopment.MasterBladeGoldCost))
            {
                return false;
            }

            if (!hero.Model.Inventory.TryEquipMasterBlade(out _))
            {
                hero.Model.AddGold(BaseDevelopment.MasterBladeGoldCost);
                return false;
            }

            resources.AddGold(BaseDevelopment.MasterBladeGoldCost);
            ShowForgeText(HeroInventory.MasterBladeItemName);
            GameAudioController.Play(GameSfx.ForgeUpgrade, mazeRenderer.GridToWorld(baseDevelopment.ForgePosition));
            GameDebugLog.Info("Hero", $"Hero bought master blade for {BaseDevelopment.MasterBladeGoldCost} gold. heroGold={hero.Model.Gold}, treasuryGold={resources.Gold}, treasuryIron={resources.Iron}, attack={hero.Model.AttackPoints}");
            return true;
        }

        private bool TryBuyPlateHarness(HeroController hero)
        {
            if (hero.Model.Inventory == null
                || !hero.Model.Inventory.CanEquipPlateHarness
                || !hero.Model.TrySpendGold(BaseDevelopment.PlateHarnessGoldCost))
            {
                return false;
            }

            if (!hero.Model.Inventory.TryEquipPlateHarness(out _))
            {
                hero.Model.AddGold(BaseDevelopment.PlateHarnessGoldCost);
                return false;
            }

            resources.AddGold(BaseDevelopment.PlateHarnessGoldCost);
            ShowForgeText(HeroInventory.PlateHarnessItemName);
            GameAudioController.Play(GameSfx.ForgeUpgrade, mazeRenderer.GridToWorld(baseDevelopment.ForgePosition));
            GameDebugLog.Info("Hero", $"Hero bought plate harness for {BaseDevelopment.PlateHarnessGoldCost} gold. heroGold={hero.Model.Gold}, treasuryGold={resources.Gold}, treasuryIron={resources.Iron}, armor={hero.Model.ArmorPoints}");
            return true;
        }

        private bool TryBuySwiftwalkerBoots(HeroController hero)
        {
            if (hero.Model.Inventory == null
                || !hero.Model.Inventory.CanEquipSwiftwalkerBoots
                || !hero.Model.TrySpendGold(BaseDevelopment.SwiftwalkerBootsGoldCost))
            {
                return false;
            }

            if (!hero.Model.Inventory.TryEquipSwiftwalkerBoots(out _))
            {
                hero.Model.AddGold(BaseDevelopment.SwiftwalkerBootsGoldCost);
                return false;
            }

            resources.AddGold(BaseDevelopment.SwiftwalkerBootsGoldCost);
            ShowForgeText(HeroInventory.SwiftwalkerBootsItemName);
            GameAudioController.Play(GameSfx.ForgeUpgrade, mazeRenderer.GridToWorld(baseDevelopment.ForgePosition));
            GameDebugLog.Info("Hero", $"Hero bought swiftwalker boots for {BaseDevelopment.SwiftwalkerBootsGoldCost} gold. heroGold={hero.Model.Gold}, treasuryGold={resources.Gold}, moveSpeedBonus={hero.Model.MoveSpeedBonusPercent}%");
            return true;
        }

        private bool TryUseHealthPotion(HeroController hero)
        {
            var healAmount = baseDevelopment.HealthPotionHealAmount;
            if (!ShouldUseHealthPotion(hero.Model, healAmount) || !hero.Model.Inventory.TryConsumeHealthPotion(healAmount))
            {
                return false;
            }

            var restored = hero.Model.RestoreHitPoints(healAmount);
            if (restored <= 0)
            {
                return false;
            }

            DamageNumberView.CreateText(
                mazeRenderer,
                hero.Model.Position,
                $"+{restored} HP",
                new Color(0.45f, 1f, 0.48f),
                1.65f);
            GameAudioController.Play(GameSfx.Potion, mazeRenderer.GridToWorld(hero.Model.Position));
            GameDebugLog.Info(
                "Hero",
                $"Hero used health potion: restored={restored}, hp={hero.Model.HitPoints}/{hero.Model.MaxHitPoints}");
            return true;
        }

        private void ShowForgeText(string text)
        {
            DamageNumberView.CreateText(
                mazeRenderer,
                baseDevelopment.ForgePosition,
                text,
                new Color(0.86f, 0.88f, 0.92f),
                2.2f);
        }

        private bool TryUseRation(HeroController hero)
        {
            var baseRestoreAmount = baseDevelopment.RationStaminaRestore;
            if (!ShouldUseRation(hero.Model, baseRestoreAmount) || !hero.Model.Inventory.TryConsumeRation(baseRestoreAmount))
            {
                return false;
            }

            var restoreAmount = baseRestoreAmount + hero.Model.ConsumeRationBlessingBonus();
            var restored = hero.Model.RestoreStamina(restoreAmount);
            if (restored <= 0)
            {
                return false;
            }

            if (hero.Model.State == HeroState.ReturningToCastle)
            {
                hero.Model.SetState(HeroState.Exploring);
            }

            DamageNumberView.CreateText(
                mazeRenderer,
                hero.Model.Position,
                $"+{restored} выносл.",
                new Color(0.45f, 0.78f, 1f),
                1.75f);
            GameAudioController.Play(GameSfx.Ration, mazeRenderer.GridToWorld(hero.Model.Position));
            GameDebugLog.Info(
                "Hero",
                $"Hero used ration: restored={restored}, stamina={hero.Model.Stamina}/{hero.Model.MaxStamina}");
            return true;
        }

        private static bool ShouldUseHealthPotion(HeroModel model, int healAmount)
        {
            return model != null
                && model.IsAlive
                && model.MaxHitPoints - model.HitPoints >= Mathf.Min(healAmount, BaseDevelopment.HealthPotionBaseHealAmount);
        }

        private static bool ShouldUseRation(HeroModel model, int restoreAmount)
        {
            return model != null
                && model.IsAlive
                && model.State != HeroState.Fighting
                && model.MaxStamina - model.Stamina >= Mathf.Min(restoreAmount, BaseDevelopment.RationBaseStaminaRestore);
        }
    }
}
