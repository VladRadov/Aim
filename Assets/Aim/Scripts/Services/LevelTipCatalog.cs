using Aim.Config;
using Aim.Models;

namespace Aim.Services
{
    public readonly struct LevelTipContent
    {
        public readonly string Title;
        public readonly string Body;
        public readonly string ModeLine;

        public LevelTipContent(string title, string body, string modeLine)
        {
            Title = title;
            Body = body;
            ModeLine = modeLine;
        }
    }

    public static class LevelTipCatalog
    {
        public static LevelTipContent GetTip(LevelDefinition definition)
        {
            if (definition == null)
                return new LevelTipContent("Уровень", "Следуй цели на HUD.", "—");

            var title = string.IsNullOrWhiteSpace(definition.DisplayName)
                ? "Уровень"
                : definition.DisplayName;

            return definition.LevelType switch
            {
                LevelType.CharacterHeadshot => new LevelTipContent(
                    title,
                    "Стреляй только в голову персонажей. Попадания в тело не считаются.",
                    "Стрельба · 1 выстрел · без полоски HP"),

                LevelType.FlyingObjects => new LevelTipContent(
                    title,
                    "Сбивай летящие шары, пока они не исчезли.",
                    "Стрельба · 1 выстрел · без полоски HP"),

                LevelType.CustomHitZones => new LevelTipContent(
                    title,
                    "Попади в подсвеченную зону на мишени. Остальные зоны не дают очко.",
                    "Стрельба · 1 точное попадание · без полоски HP"),

                LevelType.ShootingGallery => new LevelTipContent(
                    title,
                    "Стреляй только в подсвеченную банку. После попадания цель сменится.",
                    "Стрельба · 1 выстрел · без полоски HP"),

                LevelType.BouncingBalls => new LevelTipContent(
                    title,
                    "Сбивай прыгающие шары. Они остаются, пока ты их не поразишь.",
                    "Стрельба · 1 выстрел · без полоски HP"),

                LevelType.TrackingBall => new LevelTipContent(
                    title,
                    "Не стреляй. Держи прицел на большом шаре — так снимается HP.",
                    "Без стрельбы · полоска HP · держи прицел"),

                LevelType.TrackingMovers => new LevelTipContent(
                    title,
                    "Не стреляй. Следи прицелом за движущимися шарами и снимай их HP.",
                    "Без стрельбы · полоска HP · держи прицел"),

                LevelType.StaticBalls => new LevelTipContent(
                    title,
                    "Стреляй по неподвижным шарам, пока они не пропали.",
                    "Стрельба · 1 выстрел · без полоски HP"),

                LevelType.FlickTargets => new LevelTipContent(
                    title,
                    "Шары вспыхивают коротко. Быстро фликай прицелом и успей выстрелить.",
                    "Стрельба · 1 выстрел · без полоски HP"),

                LevelType.PeekTargets => new LevelTipContent(
                    title,
                    "Шары выглядывают из-за укрытий. Стреляй только когда цель видна.",
                    "Стрельба · 1 выстрел · без полоски HP"),

                LevelType.MovingRails => new LevelTipContent(
                    title,
                    "Шары едут по рельсам туда-обратно. Веди прицел с упреждением.",
                    "Стрельба · 1 выстрел · без полоски HP"),

                LevelType.PopupDucks => new LevelTipContent(
                    title,
                    "Мишени поднимаются и опускаются. Стреляй только пока они наверху.",
                    "Стрельба · 1 выстрел · без полоски HP"),

                LevelType.PriorityTargets => new LevelTipContent(
                    title,
                    "Стреляй только в жёлтую приоритетную цель. Серые не дают очко.",
                    "Стрельба · 1 выстрел · без полоски HP"),

                LevelType.PrecisionCircles => new LevelTipContent(
                    title,
                    "Маленькие цели на разной дистанции. Стреляй точно, патронов мало.",
                    "Стрельба · 1 выстрел · без полоски HP"),

                LevelType.DoubleTap => new LevelTipContent(
                    title,
                    "У шаров несколько HP. Смотри полоску сверху и добивай цель.",
                    "Стрельба · несколько попаданий · есть полоска HP"),

                _ => new LevelTipContent(
                    title,
                    "Выполни цель уровня по счётчику попаданий.",
                    definition.AllowsShooting ? "Стрельба" : "Без стрельбы")
            };
        }
    }
}
