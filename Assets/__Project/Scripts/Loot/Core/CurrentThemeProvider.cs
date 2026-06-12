using System;
using LevelGeneration;

namespace Loot.Core
{
    public class CurrentThemeProvider : ICurrentThemeProvider
    {
        private LevelTheme _theme;
        private bool _isSet;

        public LevelTheme CurrentTheme
        {
            get
            {
                if (!_isSet)
                {
                    throw new InvalidOperationException(
                        "Current theme was read before area generation set it.");
                }

                return _theme;
            }
        }

        public void SetTheme(LevelTheme theme)
        {
            _theme = theme;
            _isSet = true;
        }
    }
}
