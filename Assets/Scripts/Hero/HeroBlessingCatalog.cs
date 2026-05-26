using System.Collections.Generic;

namespace Labyrinth.Hero
{
    public static class HeroBlessingCatalog
    {
        public static readonly HeroBlessingDefinition[] PurchaseOrder =
        {
            new HeroBlessingDefinition(
                HeroBlessingType.LastBreath,
                "Последний вздох",
                "Один смертельный удар за вылазку оставляет рыцаря с 1 HP.",
                60),
            new HeroBlessingDefinition(
                HeroBlessingType.GoldenVow,
                "Золотой обет",
                "Награда золотом за победы над мобами увеличена на 35%.",
                40),
            new HeroBlessingDefinition(
                HeroBlessingType.PilgrimLight,
                "Свет пилигрима",
                "Радиус личной видимости рыцаря увеличен на 2 клетки.",
                40),
            new HeroBlessingDefinition(
                HeroBlessingType.TrueHand,
                "Верная рука",
                "Первый удар рыцаря в каждом бою получает +5 Attack Points.",
                35),
            new HeroBlessingDefinition(
                HeroBlessingType.StrongBones,
                "Крепкая кость",
                "На время вылазки Max HP и текущие HP увеличены на 4.",
                30),
            new HeroBlessingDefinition(
                HeroBlessingType.CartographerMercy,
                "Милость картографа",
                "При сдаче знаний у входа герой уточняет клетки вокруг своей памяти во всех 8 направлениях.",
                30),
            new HeroBlessingDefinition(
                HeroBlessingType.SilentStep,
                "Тихая поступь",
                "Темный повторный спаун хуже цепляется за уже пройденный путь героя в радиусе 2 клеток.",
                25),
            new HeroBlessingDefinition(
                HeroBlessingType.VowOfReturn,
                "Обет возвращения",
                "Возвращение к входу по памяти происходит заметно быстрее.",
                20),
            new HeroBlessingDefinition(
                HeroBlessingType.HeartyPath,
                "Сытный путь",
                "Первый использованный паек в вылазке восстанавливает на 7 выносливости больше.",
                18),
            new HeroBlessingDefinition(
                HeroBlessingType.DarkHunter,
                "Охотник на тьму",
                "Победа над мобом, появившимся из темноты, дает +8 XP.",
                15)
        };

        public static HeroBlessingDefinition Get(HeroBlessingType type)
        {
            for (var i = 0; i < PurchaseOrder.Length; i++)
            {
                if (PurchaseOrder[i].Type == type)
                {
                    return PurchaseOrder[i];
                }
            }

            return PurchaseOrder[0];
        }

        public static string FormatActiveNames(IEnumerable<HeroBlessingType> activeBlessings)
        {
            if (activeBlessings == null)
            {
                return "нет";
            }

            foreach (var blessing in activeBlessings)
            {
                return Get(blessing).DisplayName;
            }

            return "нет";
        }
    }
}
