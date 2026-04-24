# Shoppinglist app: SignalR based shopping list for quick syncing accross devices and users



Very quick and dirty shoppinglist webapp. Solves an annoyance I had with existing shoppinglist, which probably are polling-based, and took anywhere from 2 seconds to 20 seconds between updates. Websocket based updates solves this.

No fancy javascript latest framework or whatever. 


![](image.png)

## Development


Run npm start and dotnet run in frontend and backend projects respectively. 

### Auth override for local testing

Set `SSO_OVERRIDE=true` on the backend to enable a dev-only login endpoint
that skips Google OAuth. Requires `DEV_MODE=true` — the backend refuses to
start with `SSO_OVERRIDE=true` unless dev mode is also enabled. Defaults off.
