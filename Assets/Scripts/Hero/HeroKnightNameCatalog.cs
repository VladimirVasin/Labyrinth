using System;
using System.Collections.Generic;

namespace Labyrinth.Hero
{
    public static class HeroKnightNameCatalog
    {
        private static readonly string[] Names =
        {
            "Сэр Агравейн Непомерно Куртуазный",
            "Сэр Бальтазар Сверкающе Неловкий",
            "Сэр Виллибальд Торжественно Пыхтящий",
            "Сэр Годфруа Непобедимо Озадаченный",
            "Сэр Дезидерий Громогласно Учтивый",
            "Сэр Евстафий Беспримерно Плюмажный",
            "Сэр Жоффруа Неистово Вежливый",
            "Сэр Зигисмунд Слегка Апокалиптический",
            "Сэр Изамбард Достославно Косолапый",
            "Сэр Криспин Великолепно Обремененный",
            "Сэр Ламберик Непоколебимо Бархатный",
            "Сэр Мортимер Трехкратно Рассудительный",
            "Сэр Норберт Невыносимо Благородный",
            "Сэр Октавий Пламенно Недоумевающий",
            "Сэр Персивальд Пышно Самоотверженный",
            "Сэр Рудольфус Звеняще Непрактичный",
            "Сэр Сигеберт Сверхъестественно Церемонный",
            "Сэр Теобальд Величаво Запыхавшийся",
            "Сэр Ульрик Дивно Нерешительный",
            "Сэр Флорибунд Несокрушимо Галантный"
        };

        public static string Pick(int seed, int heroNumber, int roll, ISet<string> unavailableNames)
        {
            var random = new Random(seed ^ heroNumber * 73856093 ^ roll * 19349663);
            var start = random.Next(Names.Length);
            for (var i = 0; i < Names.Length; i++)
            {
                var candidate = Names[(start + i) % Names.Length];
                if (unavailableNames == null || !unavailableNames.Contains(candidate))
                {
                    return candidate;
                }
            }

            return Names[start];
        }
    }
}
