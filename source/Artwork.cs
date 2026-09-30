using System.Collections.Generic;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace YesRelic2;

[HarmonyPatch]
internal static class Artwork
{
    static Texture2D texture;
    static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.PropertyGetter(typeof(RelicModel), nameof(RelicModel.Icon));
        yield return AccessTools.PropertyGetter(typeof(RelicModel), nameof(RelicModel.IconOutline));
        yield return AccessTools.PropertyGetter(typeof(RelicModel), nameof(RelicModel.BigIcon));
    }
    static bool Prefix(RelicModel __instance, ref Texture2D __result)
    {
        if (__instance is not YesRelic) return true;
        if (texture == null)
        {
            var image = new Image();
            var error = image.LoadSvgFromString("<svg xmlns='http://www.w3.org/2000/svg' width='128' height='128' viewBox='0 0 128 128'><path d='M20 13h88l9 14v62l-12 12H67l-28 19 4-19H20L9 89V27z' fill='#282033' stroke='#f4cf6c' stroke-width='7' stroke-linejoin='round'/><path d='M27 37l22 23V83h13V60l22-23H68L56 51 43 37z' fill='#ffe599'/><path d='M94 34v33' stroke='#ffe599' stroke-width='12' stroke-linecap='round'/><circle cx='94' cy='83' r='7' fill='#ffe599'/></svg>");
            if (error != Error.Ok) return true;
            texture = ImageTexture.CreateFromImage(image);
        }
        __result = texture;
        return false;
    }
}
