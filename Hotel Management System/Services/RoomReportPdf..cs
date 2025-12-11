using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Hotel_Management_System.Models;
using System.Linq;

public class RoomReportPdf : IDocument
{
    public Room Room { get; }

    public RoomReportPdf(Room room)
    {
        Room = room;
    }

    public DocumentMetadata GetMetadata()
    {
        return new DocumentMetadata
        {
            Title = $"Room Report {Room.RoomNumber}",
            Author = "Hotel Management System"
        };
    }

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(20);

            page.Header()
                .Text($"Room Report - Room {Room.RoomNumber}")
                .FontSize(20).Bold().AlignCenter();

            page.Content().Column(col =>
            {
                col.Spacing(10);

                col.Item().Text($"Room Type: {Room.RoomType}");
                col.Item().Text($"Price Per Night: {Room.Price} EGP");

                col.Item().Text("Booking History")
                    .FontSize(16).Bold();

                if (Room.Bookings != null && Room.Bookings.Any())
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.ConstantColumn(100);
                            columns.ConstantColumn(100);
                            columns.ConstantColumn(100);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Guest").Bold();
                            header.Cell().Text("Check-In").Bold();
                            header.Cell().Text("Check-Out").Bold();
                            header.Cell().Text("Status").Bold();
                        });

                        foreach (var b in Room.Bookings.OrderBy(x => x.CheckInDate))
                        {
                            table.Cell().Text(b.Guest.Name);
                            table.Cell().Text(b.CheckInDate.ToString("yyyy-MM-dd"));
                            table.Cell().Text(b.CheckOutDate.ToString("yyyy-MM-dd"));
                            table.Cell().Text(b.BookingStatus.ToString());
                        }
                    });
                }
                else
                {
                    col.Item().Text("No bookings found for this room.");
                }
            });

            page.Footer().AlignCenter().Text($"Generated on {DateTime.Now:yyyy-MM-dd HH:mm}");
        });
    }
}
