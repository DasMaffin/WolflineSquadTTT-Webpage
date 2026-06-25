--[[
    WolflineSquad TTT — website integration (CLIENTSIDE)
    Place in:  garrysmod/lua/autorun/client/

    No API key here — the client only ever receives a short-lived, single-use login token
    from the server and opens it in the in-game browser.
]]

local SITE = "https://mwlp.dasmaffin.com"

net.Receive("WLSQ_OpenSite", function()
    local token     = net.ReadString()
    local returnUrl = net.ReadString()

    local frame = vgui.Create("DFrame")
    frame:SetSize(math.min(1100, ScrW() - 80), math.min(760, ScrH() - 80))
    frame:Center()
    frame:SetTitle("WolflineSquad")
    frame:MakePopup()

    local browser = vgui.Create("DHTML", frame)
    browser:Dock(FILL)
    -- The consume endpoint validates the token, logs the browser in, then redirects to returnUrl.
    browser:OpenURL(SITE .. "/auth/gmod?token=" .. token .. "&returnUrl=" .. returnUrl)
end)
