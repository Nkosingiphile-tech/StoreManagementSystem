using System;
using System.ComponentModel.DataAnnotations;

namespace StoreManagementSystem.Models
{
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string SenderEmail { get; set; } 

        [Required]
        public string ReceiverEmail { get; set; } 

        [Required]
        public string Message { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;

        public bool IsFromAdmin { get; set; } 
    }
}