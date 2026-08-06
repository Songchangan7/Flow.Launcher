using Flow.Launcher.Localization.Attributes;

namespace Creta.Plugin.Calculator
{
    [EnumLocalize]
    public enum DecimalSeparator
    {
        [EnumLocalizeKey(nameof(Localize.creta_plugin_calculator_decimal_separator_use_system_locale))]
        UseSystemLocale,

        [EnumLocalizeKey(nameof(Localize.creta_plugin_calculator_decimal_separator_dot))]
        Dot,

        [EnumLocalizeKey(nameof(Localize.creta_plugin_calculator_decimal_separator_comma))]
        Comma
    }
}
