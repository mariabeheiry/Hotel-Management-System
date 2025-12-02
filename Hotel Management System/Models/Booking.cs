using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HotelBookingSystem.Validators;

namespace Hotel_Management_System.Models
{
    public enum BookingStatus
    {
        Confirmed,
        Cancelled,
        Completed
    }

    public class Booking
    {
        [Key]
        public int BookingID { get; set; }

        [Required(ErrorMessage = "Guest is required")]
        [ForeignKey("Guest")]
        public int GuestID { get; set; }
        public Guest Guest { get; set; }

        [Required(ErrorMessage = "Room is required")]
        [ForeignKey("Room")]
        public int RoomID { get; set; }
        public Room Room { get; set; }

        [Required(ErrorMessage = "Check-in date is required")]
        [DataType(DataType.Date)]
        public DateTime CheckInDate { get; set; }

        [Required(ErrorMessage = "Check-out date is required")]
        [DataType(DataType.Date)]
        [DateGreaterThan("CheckInDate", ErrorMessage = "Check-out must be after check-in")]
        public DateTime CheckOutDate { get; set; }

        [Required]
        public BookingStatus BookingStatus { get; set; } = BookingStatus.Confirmed; // confirmed, cancelled, completed

        public Receipt Receipt { get; set; }
    }
}
