using Microsoft.AspNetCore.SignalR;
using StoreManagementSystem.Models;
using System;
using System.Threading.Tasks;

namespace StoreManagementSystem.Hubs
{
    public class NotificationHub : Hub
    {
        private readonly StoreManagementDbContext _context;

        // Inject the database context
        public NotificationHub(StoreManagementDbContext context)
        {
            _context = context;
        }

        public async Task SendMessage(string senderEmail, string receiverEmail, string message)
        {
            // 1. Permanently save the message to the database
            var chatMessage = new ChatMessage
            {
                SenderEmail = senderEmail,
                ReceiverEmail = receiverEmail,
                Message = message,
                IsFromAdmin = senderEmail == "admin@store.com", // Checks if admin sent it
                Timestamp = DateTime.Now
            };

            _context.ChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync();

            // 2. Broadcast it through the live tunnel
            await Clients.All.SendAsync("ReceiveMessage", senderEmail, receiverEmail, message);
        }
    }
}