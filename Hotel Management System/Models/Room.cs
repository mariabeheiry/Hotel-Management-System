using System.ComponentModel.DataAnnotations;

namespace Hotel_Management_System.Models
{
    public class Room
    {
        [Key]
        public int RoomID { get; set; }

        [Required]
        public string RoomNumber { get; set; }   // Unique constraint will be handled in DbContext

        [Required]
        public string RoomType { get; set; }     // Single, Double, Suite

        [Required]
        [Range(0, 999999)]
        public decimal Price { get; set; }

        public bool IsAvailable { get; set; }

        public ICollection<Booking>? Bookings { get; set; } = new List<Booking>();
    }
}
