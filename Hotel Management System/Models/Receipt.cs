using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hotel_Management_System.Models
{
    public class Receipt
    {
        [Key]
        public int ReceiptID { get; set; }

        [Required]
        [ForeignKey("Booking")]
        public int BookingID { get; set; }

        public Booking Booking { get; set; }

        [Required]
        public decimal TotalAmount { get; set; }

        public DateTime DateCreated { get; set; } = DateTime.Now;
    }
}
