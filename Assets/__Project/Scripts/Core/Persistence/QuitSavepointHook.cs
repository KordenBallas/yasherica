using System;
using UnityEngine;
using Zenject;

namespace Core.Persistence
{
    /// <summary>
    /// Thin Unity adapter turning a graceful exit into a savepoint: <see cref="Application.quitting"/>
    /// (fired on player quit AND on stopping play mode in the editor) triggers
    /// <see cref="AutosaveService.SaveGraceful"/>, so progress made on the current platform since
    /// entering it survives the quit. A crash still falls back to the last platform-entry save.
    /// </summary>
    public sealed class QuitSavepointHook : IInitializable, IDisposable
    {
        private readonly AutosaveService _autosave;

        public QuitSavepointHook(AutosaveService autosave)
        {
            _autosave = autosave;
        }

        public void Initialize()
        {
            Application.quitting += OnQuitting;
        }

        public void Dispose()
        {
            Application.quitting -= OnQuitting;
        }

        private void OnQuitting()
        {
            _autosave.SaveGraceful();
        }
    }
}
