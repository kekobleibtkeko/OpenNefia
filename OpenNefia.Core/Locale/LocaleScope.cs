﻿namespace OpenNefia.Core.Locale
{
    /// <summary>
    /// <para>
    /// Lets you shorten the namespacing for a group of locale keys.
    /// </para>
    /// <para>
    /// For example:
    /// </para>
    /// <code>
    /// var str1 = Loc.GetString("Elona.UI.Layer.TitleScreen.Window.Title");
    /// 
    /// var scope = new LocaleScope("Elona.UI.Layer.TitleScreen");
    /// var str2 = scope.GetString("Window.Title");
    /// 
    /// Assert.That(str1, Is.EqualTo(str2));
    /// </code>
    /// </summary>
    public class LocaleScope : ILocalizationFetcher
    {
        private readonly ILocalizationManager _localizationManager;

        public LocaleKey KeyPrefix { get; }

        public LocaleScope(ILocalizationManager localizationManager) : this(localizationManager, new(""))
        {
        }

        public LocaleScope(ILocalizationManager localizationManager, LocaleKey keyPrefix)
        {
            _localizationManager = localizationManager;
            KeyPrefix = keyPrefix;
        }

        public string Get(LocaleKey key, params LocaleArg[] args)
        {
            return _localizationManager.Get(key.With(key), args);
        }
    }
}