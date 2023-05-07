public record InvoiceView(
    int id,
    int person_id,
    string name,
    DateTime order_date,
    DateTime? payment_date,
    string? payment_info,
    decimal subtotal,
    decimal shipping,
    decimal total,
    string country,
    string? address,
    DateTime? ship_date,
    string? ship_info,
    List<LineItemView> line_items
);
