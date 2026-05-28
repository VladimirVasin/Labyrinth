using Labyrinth.Hero;
using UnityEngine;

namespace Labyrinth.UI
{
    public sealed partial class HeroHudView
    {
        private static void CaptureHover(
            Rect rect,
            string title,
            string info,
            ref string hoveredTitle,
            ref string hoveredInfo,
            ref Rect hoveredRect)
        {
            if (Event.current == null
                || !rect.Contains(Event.current.mousePosition)
                || string.IsNullOrEmpty(info))
            {
                return;
            }

            hoveredTitle = string.IsNullOrEmpty(title) ? "Пояснение" : title;
            hoveredInfo = info;
            hoveredRect = rect;
        }

        private static string BuildHeroHeaderTooltip(HeroController hero, string title)
        {
            var model = hero.Model;
            return $"{title}\nСтатус: {BuildStateText(model.State)}. {BuildStateTooltip(model)}\nXP: {model.Experience}/{model.ExperienceForNextLevel}. Уровень подземелья: {model.DungeonLevel}.";
        }

        private static string BuildStateTooltip(HeroModel model)
        {
            switch (model.State)
            {
                case HeroState.Exploring:
                    return "Герой сам исследует неизвестные проходы, запоминает маршрут и получает награду за новые клетки.";
                case HeroState.SearchingKey:
                    return "Герой ищет ключ или путь к важной запертой цели.";
                case HeroState.ReturningToDoor:
                    return "Герой уже знает нужную дверь или спуск и возвращается к ней по памяти.";
                case HeroState.OpeningDoor:
                    return "Герой тратит найденный ключ и открывает проход.";
                case HeroState.ReturningToCastle:
                    return "Герой идёт ко входу, чтобы восстановиться, сдать добычу и пополнить запасы.";
                case HeroState.Fighting:
                    return "Герой находится в бою; исход зависит от HP, боевой выносливости, инициативы, guard, брони, ран и выбранных действий.";
                case HeroState.Stuck:
                    return "Герой ждёт доступной цели или маршрута.";
                case HeroState.Defeated:
                    return "Герой погиб. Его дом сохранит родословную, а наследник может получить клятву мести.";
                default:
                    return "Текущее состояние героя.";
            }
        }

        private static string BuildHealthTooltip(HeroModel model)
        {
            var woundText = model.CombatWounds > 0
                ? $" Легкие раны: {model.CombatWounds}; зелья и лазарет лечат их, пайки не лечат."
                : string.Empty;
            var severeText = model.HasSevereInjury
                ? $" Тяжелая травма: {model.SevereInjuryText}; лечится только в лазарете."
                : string.Empty;
            var scarText = model.HasPersonalScar
                ? $" Личный шрам: {model.PersonalScarText}; не лечится и не наследуется."
                : string.Empty;
            return $"Текущее здоровье: {model.HitPoints}/{model.MaxHitPoints}. При 0 HP герой погибает, оставляя родословную и возможный жетон памяти.{woundText}{severeText}{scarText}";
        }

        private static string BuildStaminaTooltip(HeroModel model)
        {
            var woundText = model.CombatStaminaWoundPenalty > 0
                ? $" Раны уменьшают стартовую боевую выносливость на {model.CombatStaminaWoundPenalty}."
                : string.Empty;
            return $"Выносливость: {model.Stamina}/{model.MaxStamina}. Новые клетки тратят запас, а у входа герой восстанавливает его полностью.{woundText}";
        }

        private static string BuildGoldTooltip(HeroModel model)
        {
            return $"Личное золото героя: {model.Gold}. Оно тратится на услуги города, снаряжение и артефакты; часть успешной добычи может уйти в фонд дома.";
        }

        private static string BuildLevelTooltip(HeroModel model)
        {
            return $"Уровень {model.Level}. XP {model.Experience}/{model.ExperienceForNextLevel}. Повышение уровня добавляет +{HeroModel.HitPointsPerLevel} Max HP и +{HeroModel.StaminaPerLevel} выносливости.";
        }

        private static string BuildTrainingTooltip(HeroModel model)
        {
            return $"Семейная выучка: {model.LineageTrainingScore}/{HeroLineageState.MaxTrainingScore}. Бонус наследника: {model.LineageBonusText}. Выучка зависит от погибших предков, возвращённых жетонов и фонда дома.";
        }

        private static void BuildBlessingTooltip(HeroModel model, out string title, out string info)
        {
            if (TryGetActiveBlessing(model, out var activeBlessing))
            {
                title = activeBlessing.DisplayName;
                info = activeBlessing.Description;
                return;
            }

            title = "Благословение";
            info = "Нет активного благословения. Часовня позволяет купить одно благословение перед вылазкой.";
        }

        private static void BuildVengeanceTooltip(HeroModel model, out string title, out string info)
        {
            var quest = model.VengeanceQuest;
            if (quest != null && quest.IsActive)
            {
                title = quest.DisplayTitle;
                info = quest.TooltipText;
                return;
            }

            title = "Клятва мести";
            info = "У этого наследника нет личной клятвы мести.";
        }

        private static string BuildWoundsTooltip(HeroModel model)
        {
            if (model.CombatWounds <= 0)
            {
                return "Легких боевых ран нет. Пайки восстанавливают только выносливость и не лечат раны.";
            }

            return $"Легкие боевые раны: {model.CombatWounds}. Они снижают Attack Points и стартовую боевую выносливость. Зелья лечат легкие раны вместе с HP; лазарет лечит их за пищу; пайки не лечат.";
        }

        private static string BuildSevereInjuryTooltip(HeroModel model)
        {
            return model.HasSevereInjury
                ? HeroInjuryCatalog.BuildSevereInjuryTooltip(model.SevereInjury)
                : "Тяжелой травмы нет. Она может появиться после серии опасных ударов и лечится только в лазарете.";
        }

        private static string BuildScarTooltip(HeroModel model)
        {
            return model.HasPersonalScar
                ? HeroInjuryCatalog.BuildScarTooltip(model.PersonalScar)
                : "Личного шрама нет. Шрам может остаться, если герой продолжает драться с тяжелой травмой или чудом переживает смертельный удар. Шрамы не лечатся и не наследуются.";
        }

        private static string BuildCharacterTraitTooltip(HeroModel model)
        {
            return model.CharacterTrait != HeroCharacterTraitType.None
                ? HeroInjuryCatalog.BuildCharacterTraitTooltip(model.CharacterTrait)
                : "Особой черты характера нет. Черты формируются у наследников по обстоятельствам смерти предка; это отдельная система и она не наследует личные шрамы.";
        }

        private static string BuildAttackTooltip(HeroModel model)
        {
            var equipment = model.Inventory != null ? model.Inventory.AttackBonus : 0;
            var woundText = model.AttackWoundPenalty > 0 ? $", легкие раны: -{model.AttackWoundPenalty}" : string.Empty;
            var severePenalty = HeroInjuryCatalog.GetSevereAttackPenalty(model.SevereInjury);
            var scarPenalty = HeroInjuryCatalog.GetScarAttackPenalty(model.PersonalScar);
            var severeText = severePenalty > 0 ? $", травма: -{severePenalty}" : string.Empty;
            var scarText = scarPenalty > 0 ? $", шрам: -{scarPenalty}" : string.Empty;
            return $"Сила удара героя: {model.AttackPoints}. Личная база: {model.BaseAttackPoints}, оружие: +{equipment}, выучка: +{model.LineageAttackBonus}{woundText}{severeText}{scarText}. Ситуативные бонусы шрамов и характера применяются в бою.";
        }

        private static string BuildArmorTooltip(HeroModel model)
        {
            var equipment = model.Inventory != null ? model.Inventory.ArmorBonus : 0;
            var severePenalty = HeroInjuryCatalog.GetSevereArmorPenalty(model.SevereInjury);
            var severeText = severePenalty > 0 ? $", травма: -{severePenalty}" : string.Empty;
            return $"Броня героя: {model.ArmorPoints}. Она снижает входящий урон. Личная база: {model.BaseArmorPoints}, броня: +{equipment}{severeText}; обеты и жетоны могут добавить ещё бонус.";
        }

        private static string BuildInventorySlotTooltipTitle(HeroInventorySlot slot)
        {
            return slot.HasItem ? slot.DisplayItem : slot.Label;
        }

        private static string BuildInventorySlotTooltip(HeroInventorySlot slot)
        {
            if (slot.HasItem && !string.IsNullOrEmpty(slot.HoverInfo))
            {
                return slot.HoverInfo;
            }

            if (slot.HasItem)
            {
                return BuildOccupiedSlotFallback(slot);
            }

            switch (slot.Type)
            {
                case HeroInventorySlotType.Weapon:
                    return "Слот оружия. Оружие повышает Attack Points.";
                case HeroInventorySlotType.Armor:
                    return "Слот брони. Броня повышает Armor Points и снижает входящий урон.";
                case HeroInventorySlotType.Footwear:
                    return "Слот обуви. Обувь может ускорять перемещение героя.";
                case HeroInventorySlotType.Potion:
                    return $"Пустой слот зелий. Герой может носить до {HeroInventory.MaxHealthPotionCount} зелий здоровья; они лечат HP и легкие раны, но не тяжелые травмы и не шрамы.";
                case HeroInventorySlotType.Ration:
                    return $"Пустой слот пайков. Герой может носить до {HeroInventory.MaxRationCount} пайков для восстановления выносливости. Пайки не лечат раны, травмы и шрамы.";
                case HeroInventorySlotType.Artifact:
                    return "Пустой слот артефакта. Здесь могут лежать редкие одноразовые предметы.";
                default:
                    return "Свободный слот для ключей, жетонов памяти и особой добычи.";
            }
        }

        private static string BuildOccupiedSlotFallback(HeroInventorySlot slot)
        {
            switch (slot.Type)
            {
                case HeroInventorySlotType.Weapon:
                    return $"Оружие экипировано. Бонус к атаке: +{slot.AttackBonus}.";
                case HeroInventorySlotType.Armor:
                    return $"Броня экипирована. Бонус к броне: +{slot.ArmorBonus}.";
                case HeroInventorySlotType.Footwear:
                    return $"Обувь экипирована. Бонус к скорости: +{slot.MoveSpeedBonusPercent}%.";
                case HeroInventorySlotType.Potion:
                case HeroInventorySlotType.Ration:
                    return $"Расходник в запасе: {slot.DisplayItem}.";
                case HeroInventorySlotType.Artifact:
                    return $"Артефакт в запасе: {slot.DisplayItem}.";
                default:
                    return $"Предмет в свободном слоте: {slot.DisplayItem}.";
            }
        }
    }
}
