using System;
using Aim.Config;

namespace Aim.Services.Levels
{
    public interface ILevelRunner : IDisposable
    {
        void Start(LevelDefinition definition);
        void Stop();
    }
}
