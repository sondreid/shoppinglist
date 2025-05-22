using Microsoft.AspNetCore.SignalR;
namespace handleliste.Hubs;
public class ItemHub : Hub
{
    public async Task UpdateShoppingList()
    {
        await Clients.All.SendAsync("ReceiveUpdate");
    }

    public async Task ItemAdded(ShoppingItem item)
    {
        await Clients.All.SendAsync("ItemAdded", item);
    }

    public async Task ItemUpdated(ShoppingItem item)
    {
        await Clients.All.SendAsync("ItemUpdated", item);
    }

    public async Task ItemDeleted(int id)
    {
        await Clients.All.SendAsync("ItemDeleted", id);
    }
}
