using Heat.Core;
using Heat.Data;
using UnityEngine;
using Zenject;

namespace Core.DI
{
    /// <summary>
    /// Heat settings binding (Track Y), shared by the Hub and Area scene contexts — and deliberately
    /// NEVER installed on the Arena (FR14: Heat is a Journey system; the Arena budget guard holds).
    /// Must run before <see cref="MetaProgressionInstaller"/> on containers that also bind an
    /// <c>IHeatLens</c>, so the vocabulary can pick the lens up. A missing config asset degrades to
    /// the inert Core defaults (Heat absent = today's game).
    /// </summary>
    public static class HeatInstaller
    {
        private const string ConfigResourcePath = "Configs/HeatConfig";

        public static void InstallSettings(DiContainer container)
        {
            container.Bind<HeatSettings>()
                .FromMethod(_ => HeatConfigMapper.ToSettings(
                    Resources.Load<HeatConfig>(ConfigResourcePath)))
                .AsSingle();
        }
    }
}
