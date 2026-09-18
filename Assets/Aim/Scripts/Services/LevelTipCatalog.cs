using Aim.Config;
using Aim.Models;

namespace Aim.Services
{
    public readonly struct LevelTipContent
    {
        public readonly string TitleRu;
        public readonly string TitleEn;
        public readonly string BodyRu;
        public readonly string BodyEn;
        public readonly string ModeRu;
        public readonly string ModeEn;

        public LevelTipContent(
            string titleRu,
            string titleEn,
            string bodyRu,
            string bodyEn,
            string modeRu,
            string modeEn)
        {
            TitleRu = titleRu;
            TitleEn = titleEn;
            BodyRu = bodyRu;
            BodyEn = bodyEn;
            ModeRu = modeRu;
            ModeEn = modeEn;
        }
    }

    public static class LevelTipCatalog
    {
        const string ShootModeRu = "Стрельба · 1 выстрел · без полоски HP";
        const string ShootModeEn = "Shooting · 1 shot · no HP bar";
        const string TrackModeRu = "Без стрельбы · полоска HP · держи прицел";
        const string TrackModeEn = "No shooting · HP bar · hold the crosshair";
        const string MultiHitModeRu = "Стрельба · несколько попаданий · есть полоска HP";
        const string MultiHitModeEn = "Shooting · several hits · HP bar";

        public static LevelTipContent GetTip(LevelDefinition definition)
        {
            if (definition == null)
            {
                return new LevelTipContent(
                    "Уровень",
                    "Level",
                    "Следуй цели на HUD.",
                    "Follow the objective on the HUD.",
                    "—",
                    "—");
            }

            var titleRu = GameModeCatalog.GetDisplayName(definition.LevelType);
            var titleEn = GameModeCatalog.GetDisplayNameEn(definition.LevelType);

            return definition.LevelType switch
            {
                LevelType.CharacterHeadshot => new LevelTipContent(
                    titleRu, titleEn,
                    "Стреляй только в голову персонажей. Попадания в тело не считаются.",
                    "Shoot only the characters' heads. Body shots do not count.",
                    ShootModeRu, ShootModeEn),

                LevelType.FlyingObjects => new LevelTipContent(
                    titleRu, titleEn,
                    "Сбивай летящие шары, пока они не исчезли.",
                    "Shoot the flying balls before they disappear.",
                    ShootModeRu, ShootModeEn),

                LevelType.CustomHitZones => new LevelTipContent(
                    titleRu, titleEn,
                    "Попади в подсвеченную зону на мишени. Остальные зоны не дают очко.",
                    "Hit the highlighted zone on the target. Other zones do not score.",
                    ShootModeRu, ShootModeEn),

                LevelType.ShootingGallery => new LevelTipContent(
                    titleRu, titleEn,
                    "Стреляй только в подсвеченную банку. После попадания цель сменится.",
                    "Shoot only the highlighted can. After a hit the target changes.",
                    ShootModeRu, ShootModeEn),

                LevelType.BouncingBalls => new LevelTipContent(
                    titleRu, titleEn,
                    "Сбивай прыгающие шары. Они остаются, пока ты их не поразишь.",
                    "Shoot the bouncing balls. They stay until you hit them.",
                    ShootModeRu, ShootModeEn),

                LevelType.TrackingBall => new LevelTipContent(
                    titleRu, titleEn,
                    "Не стреляй. Держи прицел на большом шаре — так снимается HP.",
                    "Do not shoot. Keep the crosshair on the big ball to drain HP.",
                    TrackModeRu, TrackModeEn),

                LevelType.TrackingMovers => new LevelTipContent(
                    titleRu, titleEn,
                    "Не стреляй. Следи прицелом за движущимися шарами и снимай их HP.",
                    "Do not shoot. Track the moving balls with the crosshair to drain HP.",
                    TrackModeRu, TrackModeEn),

                LevelType.StaticBalls => new LevelTipContent(
                    titleRu, titleEn,
                    "Стреляй по неподвижным шарам, пока они не пропали.",
                    "Shoot the static balls before they disappear.",
                    ShootModeRu, ShootModeEn),

                LevelType.FlickTargets => new LevelTipContent(
                    titleRu, titleEn,
                    "Шары вспыхивают коротко. Быстро фликай прицелом и успей выстрелить.",
                    "Balls flash briefly. Flick the crosshair quickly and shoot in time.",
                    ShootModeRu, ShootModeEn),

                LevelType.PeekTargets => new LevelTipContent(
                    titleRu, titleEn,
                    "Шары выглядывают из-за укрытий. Стреляй только когда цель видна.",
                    "Balls peek from cover. Shoot only while the target is visible.",
                    ShootModeRu, ShootModeEn),

                LevelType.MovingRails => new LevelTipContent(
                    titleRu, titleEn,
                    "Шары едут по рельсам туда-обратно. Веди прицел с упреждением.",
                    "Balls move back and forth on rails. Lead the crosshair.",
                    ShootModeRu, ShootModeEn),

                LevelType.PopupDucks => new LevelTipContent(
                    titleRu, titleEn,
                    "Мишени поднимаются и опускаются. Стреляй только пока они наверху.",
                    "Targets rise and drop. Shoot only while they are up.",
                    ShootModeRu, ShootModeEn),

                LevelType.PriorityTargets => new LevelTipContent(
                    titleRu, titleEn,
                    "Стреляй только в жёлтую приоритетную цель. Серые не дают очко.",
                    "Shoot only the yellow priority target. Grey ones do not score.",
                    ShootModeRu, ShootModeEn),

                LevelType.PrecisionCircles => new LevelTipContent(
                    titleRu, titleEn,
                    "Маленькие цели на разной дистанции. Стреляй точно, патронов мало.",
                    "Small targets at different distances. Shoot accurately — ammo is limited.",
                    ShootModeRu, ShootModeEn),

                LevelType.DoubleTap => new LevelTipContent(
                    titleRu, titleEn,
                    "У шаров несколько HP. Смотри полоску сверху и добивай цель.",
                    "Balls have several HP. Watch the bar on top and finish the target.",
                    MultiHitModeRu, MultiHitModeEn),

                _ => new LevelTipContent(
                    titleRu, titleEn,
                    "Выполни цель уровня по счётчику попаданий.",
                    "Complete the level objective on the hit counter.",
                    definition.AllowsShooting ? "Стрельба" : "Без стрельбы",
                    definition.AllowsShooting ? "Shooting" : "No shooting")
            };
        }
    }
}
