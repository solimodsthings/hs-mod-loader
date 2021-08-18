using System;
using System.Collections.Generic;
using System.Text;

namespace HSModLoader
{
    /// <summary>
    /// The permitted states for a mod.
    /// <para><b>Enabled</b>: active and can be installed onto the game</para>
    /// <para><b>Soft-Disabled</b>: mod scripts are disabled, but content packages and localization files are still active and installed onto the game</para>
    /// <para><b>Disabled</b>: all mod content is inactive and not installed</para>
    /// </summary>
    public enum ModState
    {
        Disabled,
        SoftDisabled,
        Enabled
    }

}
