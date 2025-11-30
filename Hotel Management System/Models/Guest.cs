using System.ComponentModel.DataAnnotations;

namespace Hotel_Management_System.Models
{
    public class Guest
    {
        [Key]
        public int GuestID { get; set; }

        [Required]
        public string Name { get; set; }

        public string Phone { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        public ICollection<Booking> Bookings { get; set; }
    }
}
