using Microsoft.AspNetCore.SignalR;

namespace handleliste.Hubs
{
    public class ItemHub : Hub
    {
        
        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
        
        
        public async Task UpdateShoppingList()
        {
            await Clients.All.SendAsync("ReceiveUpdate");
        }

        public async Task ItemAdded(ShoppingItem item)
        {
            await Clients.All.SendAsync("ItemAdded", item);
        }
        

        public async Task SendNotification(string content)
        {
            await Clients.All.SendAsync("ReceiveNotification", content);
        }

        public async Task ItemDeleted(int id)
        {
            await Clients.All.SendAsync("ItemDeleted", id);
        }
    }
}
