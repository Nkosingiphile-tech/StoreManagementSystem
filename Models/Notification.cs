using System.ComponentModel.DataAnnotations;

namespace StoreManagementSystem.Models
{
    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        // The ID of the user receiving the notification
        [Required]
        public string UserId { get; set; }

        [Required]
        public string Message { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Optional: A link to click (e.g., "/Orders/Details/5")
        public string ActionUrl { get; set; }
    }
}
