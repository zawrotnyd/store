public record Invoice(
    int id,
    int person_id,
    DateTime order_date,
    DateTime payment_date,
    string payment_info,
    decimal subtotal,
    decimal shipping,
    decimal total,
    string country,
    string address,
    DateTime ship_date,
    string ship_info
);
