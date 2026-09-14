using System;
using System.Collections.Generic;
using Aim.Config;
using Aim.Models;
using UnityEngine;

namespace Aim.Services
{
    public readonly struct GameModeInfo
    {
        public readonly LevelType Type;
        public readonly string DisplayName;
        public readonly string DisplayNameEn;
        public readonly string IconPath;
        public readonly LevelDefinition Template;

        public GameModeInfo(LevelType type, string displayName, string displayNameEn, string iconPath, LevelDefinition template)
        {
            Type = type;
            DisplayName = displayName;
            DisplayNameEn = displayNameEn;
            IconPath = iconPath;
            Template = template;
        }

        public bool AllowsShooting => Template == null || Template.AllowsShooting;
    }

    public sealed class GameModeCatalog
    {
        readonly List<GameModeInfo> _modes = new();

        public IReadOnlyList<GameModeInfo> Modes => _modes;

        public GameModeCatalog(IEnumerable<LevelDefinition> levels)
        {
            var templates = new Dictionary<LevelType, LevelDefinition>();
            if (levels != null)
            {
                foreach (var level in levels)
                {
                    if (level == null)
                        continue;
                    if (!templates.ContainsKey(level.LevelType))
                        templates[level.LevelType] = level;
                }
            }

            foreach (LevelType type in Enum.GetValues(typeof(LevelType)))
            {
                if (!templates.TryGetValue(type, out var template) || template == null)
                    continue;

                _modes.Add(new GameModeInfo(
                    type,
                    GetDisplayName(type),
                    GetDisplayNameEn(type),
                    GetIconPath(type),
                    template));
            }
        }

        public bool TryGet(LevelType type, out GameModeInfo info)
        {
            for (var i = 0; i < _modes.Count; i++)
            {
                if (_modes[i].Type != type)
                    continue;
                info = _modes[i];
                return true;
            }

            info = default;
            return false;
        }

        public static string GetDisplayName(LevelType type) => type switch
        {
            LevelType.CharacterHeadshot => "Хедшоты",
            LevelType.FlyingObjects => "Летящие цели",
            LevelType.CustomHitZones => "Зоны попадания",
            LevelType.ShootingGallery => "Тир",
            LevelType.BouncingBalls => "Прыгающие шары",
            LevelType.TrackingBall => "Ведение цели",
            LevelType.TrackingMovers => "Слежка за роем",
            LevelType.StaticBalls => "Статичные шары",
            LevelType.FlickTargets => "Флик-шоты",
            LevelType.PeekTargets => "Выглядывание",
            LevelType.MovingRails => "Рельсы",
            LevelType.PopupDucks => "Поп-ап мишени",
            LevelType.PriorityTargets => "Приоритет",
            LevelType.PrecisionCircles => "Точность",
            LevelType.DoubleTap => "Мультихит",
            _ => type.ToString()
        };

        public static string GetDisplayNameEn(LevelType type) => type switch
        {
            LevelType.CharacterHeadshot => "Headshots",
            LevelType.FlyingObjects => "Flying targets",
            LevelType.CustomHitZones => "Hit zones",
            LevelType.ShootingGallery => "Gallery",
            LevelType.BouncingBalls => "Bouncing balls",
            LevelType.TrackingBall => "Tracking ball",
            LevelType.TrackingMovers => "Tracking swarm",
            LevelType.StaticBalls => "Static balls",
            LevelType.FlickTargets => "Flick shots",
            LevelType.PeekTargets => "Peek targets",
            LevelType.MovingRails => "Rails",
            LevelType.PopupDucks => "Popup targets",
            LevelType.PriorityTargets => "Priority",
            LevelType.PrecisionCircles => "Precision",
            LevelType.DoubleTap => "Multi-hit",
            _ => type.ToString()
        };

        public static string GetIconPath(LevelType type) =>
            $"Assets/Aim/Resources/Modes/{type.ToString().ToLowerInvariant()}_icon.png";
    }
}
