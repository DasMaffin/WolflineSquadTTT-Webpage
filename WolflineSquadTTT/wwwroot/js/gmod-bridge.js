// Loaded ONLY for in-game (GMod-authenticated) sessions — see _Layout.cshtml.
//
// The site runs inside Garry's Mod's embedded DHTML panel. External links (Discord, the
// Tebex shop, Steam Workshop, …) can't sensibly open there — a new tab isn't possible and
// in-place navigation would replace the whole site with a third-party page the player isn't
// logged into. Instead we hand them to a Lua bridge the GMod client exposes:
//
//     -- clientside (garrysmod/lua/autorun/client), on the DHTML panel that loads the site:
//     panel:AddFunction("wlsq", "openURL", function(url) gui.OpenURL(url) end)
//
// gui.OpenURL opens the Steam overlay browser if the overlay is enabled, otherwise the
// player's default OS browser — so Steam picks the best target automatically. Internal links
// keep navigating inside the panel as normal.
(function () {
    "use strict";

    function openExternal(url) {
        if (window.wlsq && typeof window.wlsq.openURL === "function") {
            window.wlsq.openURL(url);
            return true;
        }
        return false;   // bridge not present (client Lua not deployed) — leave the click alone
    }

    function isExternal(anchor) {
        return (anchor.protocol === "http:" || anchor.protocol === "https:")
            && anchor.host !== window.location.host;
    }

    document.addEventListener("click", function (e) {
        if (!e.target || !e.target.closest) return;
        var anchor = e.target.closest("a[href]");
        if (!anchor || !isExternal(anchor)) return;

        // Only swallow the click if the bridge actually took it.
        if (openExternal(anchor.href)) {
            e.preventDefault();
        }
    }, true);
})();
