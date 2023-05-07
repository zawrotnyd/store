using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Store.Tests;

public class ApiTests : Tester
{
    public ApiTests() : base()
    {
    }

    [Fact]
    public async Task Test_Items_Get()
    {
        var (_, json) = await ExecuteFunction("items_get");
        var expected = @"[{ id: 2, name: 'Everlasting Gobstopper', price: 149.99, weight: 0.25},
        { id: 3, name: 'Fizzy Lifting Drink', price: 5, weight: 1},
        { id: 4, name: 'JPG of Mr. Wonka', price: 2, weight: nil},
        { id: 1, name: 'Smell the Factory', price: 21.23, weight: nil}]";
        json.Should().Equals(expected);
    }

    [Fact]
    public async Task Test_Items_Get_For()
    {
        var (_, json) = await ExecuteFunction("items_get_for", 3);
        json.Should().Equals("[{id:2, name:'Everlasting Gobstopper'}]");
        (_, json) = await ExecuteFunction("items_get_for", 4);
        json.Should().Equals("Fizzy Lifting Drink");
        (_, json) = await ExecuteFunction("items_get_for", 99);
        json.Should().Equals("[]");
    }

    [Fact]
    public async Task Test_Invoices_Get()
    {
        var (_, json) = await ExecuteFunction("invoices_get");
        var invoices = JsonSerializer.Deserialize<IReadOnlyList<InvoiceView>>(json);
        var ids = invoices?.Select(x => x.id).ToArray();
        ids.Should().Equal(new int[] { 1, 2, 3, 4 });
        // uses same invoice_view as invoice_get
    }

    [Fact]
    public async Task Test_Invoices_Get_For()
    {
        var (_, json) = await ExecuteFunction("invoices_get_for", 4);
        var invoices = JsonSerializer.Deserialize<IReadOnlyList<InvoiceView>>(json);
        invoices.Should().HaveCount(1);
        invoices?[0].name.Should().Equals("Charlie Buckets");
        // uses same invoice_view as invoice_get
        (_, json) = await ExecuteFunction("invoices_get_for", 99);
        invoices.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task Test_Invoice_Get()
    {
        var (_, json) = await ExecuteFunction("invoice_get", 1);
        var invoiceView = new InvoiceView(
                id: 1,
                person_id: 4,
                name: "Charlie Buckets",
                order_date: new DateTime(2019, 10, 2),
                payment_date: new DateTime(2019, 10, 2),
                payment_info: "PayPal #abc123",
                subtotal: 15,
                shipping: 6,
                total: 21,
                country: "US",
                address: "Charlie Buckets\n3 Skid Row\nHershey, PA 04141",
                ship_date: new DateTime(2019, 10, 3),
                ship_info: "usps# a1b2",
                line_items: new List<LineItemView>
                {
                    new LineItemView(id: 1, item_id: 3, name: "Fizzy Lifting Drink", quantity: 3, price: 15)
                });
        var expected = JsonSerializer.Serialize(invoiceView);
        var invoice = JsonSerializer.Deserialize<Invoice>(json);
        invoice.Should().Equals(expected);
    }

    [Fact]
    public async Task Test_Invoice_Delete()
    {
        var (_, json) = await ExecuteFunction("invoice_delete", 4);
        var invoiceView = new InvoiceView(
                id: 1,
                person_id: 7,
                name: "巩俐",
                order_date: new DateTime(2019, 10, 2),
                payment_date: null,
                payment_info: null,
                subtotal: 2,
                shipping: 0,
                total: 2,
                country: "CN",
                address: null,
                ship_date: null,
                ship_info: null,
                line_items: new List<LineItemView>
                {
                    new LineItemView(id: 4, item_id: 4, name: "JPG of Mr. Wonka", quantity: 1, price: 2)
                });
        (_, json) = await ExecuteFunction("invoice_get", 4);
        json.Should().Equals("{error: 'not found'}");
        (_, json) = await ExecuteFunction("invoice_delete", 1);
        var jsonObject = JsonNode.Parse(json);
        jsonObject?["error"].Should().Equals("{error: 'not found'}");
    }

    [Fact]
    public async Task Test_Invoice_Paid()
    {
        var paymentInfo = "info here";
        var (_, json) = await ExecuteFunction("invoice_paid", 4, paymentInfo);
        var invoice = JsonSerializer.Deserialize<Invoice>(json);
        invoice?.payment_date.Should().Equals(DateTime.Now);
        invoice?.payment_info.Should().Equals(paymentInfo);
    }

    [Fact]
    public async Task Test_Invoice_Update()
    {
        var country = "TW";
        var id = 4;
        var (_, json) = await ExecuteFunction("invoice_update", id, country);
        var invoice = JsonSerializer.Deserialize<Invoice>(json);
        invoice?.id.Should().Equals(id);
        invoice?.country.Should().Equals(country);
        country = "XX";
        (_, json) = await ExecuteFunction("invoice_update", id, country);
        var jsonObject = JsonNode.Parse(json);
        jsonObject?["error"]?.ToString().Should().Contain("violates");
        id = 99;
        country = "TW";
        (_, json) = await ExecuteFunction("invoice_update", id, country);
        invoice = JsonSerializer.Deserialize<Invoice>(json);
        invoice.Should().BeNull();
        id = 4;
        country = "CA";
        var address = "street address here";
        (_, json) = await ExecuteFunction("invoice_update", id, country, address);
        invoice = JsonSerializer.Deserialize<Invoice>(json);
        invoice?.country.Should().Equals(country);
        invoice?.address.Should().Equals(address);
        id = 1;
        country = "XX";
        (_, json) = await ExecuteFunction("invoice_update", id, country);
        jsonObject = JsonNode.Parse(json);
        jsonObject?["error"]?.ToString().Should().Contain("shipped");
    }

    [Fact]
    public async Task Test_LineItem_Delete()
    {
        var (_, json) = await ExecuteFunction("lineitem_delete", 4);
        var lineItem = new LineItem(id: 4, invoice_id: 4, item_id: 4, quantity: 1, price: 2);
        var expected = JsonSerializer.Serialize(lineItem);
        json.Should().Equals(expected);
        (_, json) = await ExecuteFunction("invoice_get", 4);
        var invoice = JsonSerializer.Deserialize<InvoiceView>(json);
        invoice?.line_items.Should().BeNullOrEmpty();
        (_, json) = await ExecuteFunction("lineitem_delete", 4);
        json.Should().BeNull();
        (_, json) = await ExecuteFunction("lineitem_delete", 1);
        var jsonObject = JsonNode.Parse(json);
        jsonObject?["error"]?.ToString().Should().Equals("no_alter_paid_lineitem");
        (_, json) = await ExecuteFunction("lineitem_delete", 3);
        jsonObject = JsonNode.Parse(json);
        jsonObject?["error"]?.ToString().Should().Equals("no_alter_paid_lineitem");
    }

    [Fact]
    public async Task Test_LineItem_Add()
    {
        var (_, json) = await ExecuteFunction("lineitem_add", 7, 3, 10);
        var lineItem = JsonSerializer.Deserialize<LineItem>(json);
        lineItem?.id.Should().BeGreaterThan(4);
        lineItem?.invoice_id.Should().Equals(4);
        lineItem?.item_id.Should().Equals(3);
        lineItem?.quantity.Should().Equals(10);
        lineItem?.price.Should().Equals(50); //trigger
    }

    [Fact]
    public async Task Test_LineItem_Add_New()
    {
        var (_, json) = await ExecuteFunction("lineitem_add", 1, 3, 3);
        var lineItem = JsonSerializer.Deserialize<LineItem>(json);
        lineItem?.id.Should().BeGreaterThan(4);
        lineItem?.invoice_id.Should().BeGreaterThan(4);
        (_, json) = await ExecuteFunction("cart_get", 1);
        var invoiceView = JsonSerializer.Deserialize<InvoiceView>(json);
        invoiceView?.id.Should().BeGreaterThan(4);
        invoiceView?.person_id.Should().Equals(1);
        invoiceView?.order_date.Should().Equals(DateTime.Now);
        invoiceView?.country.Should().Equals("SG");
    }

    [Fact]
    public async Task Test_LineItem_Add_Update()
    {
        var (_, json) = await ExecuteFunction("lineitem_add", 7, 4, 1);
        var lineItem = JsonSerializer.Deserialize<LineItem>(json);
        lineItem?.quantity.Should().Equals(2);
        (_, json) = await ExecuteFunction("lineitem_add", 7, 4, 1);
        lineItem = JsonSerializer.Deserialize<LineItem>(json);
        lineItem?.quantity.Should().Equals(3);
        (_, json) = await ExecuteFunction("lineitem_add", 7, 4, 5);
        lineItem = JsonSerializer.Deserialize<LineItem>(json);
        lineItem?.quantity.Should().Equals(8);
    }

    [Fact]
    public async Task Test_LineItem_Update()
    {
        var (_, json) = await ExecuteFunction("lineitem_update", 4, 5);
        var li = new LineItem(id: 4, invoice_id: 4, item_id: 4, quantity: 5, price: 10);
        var expected = JsonSerializer.Serialize(li);
        var lineItem = JsonSerializer.Deserialize<LineItem>(json);
        lineItem?.Should().Equals(expected);
        (_, json) = await ExecuteFunction("lineitem_update", 4, 0);
        lineItem = JsonSerializer.Deserialize<LineItem>(json);
        lineItem.Should().BeNull();
        (_, json) = await ExecuteFunction("invoice_get", 4);
        var invoiceView = JsonSerializer.Deserialize<InvoiceView>(json);
        invoiceView?.line_items.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task Test_Cart_Get()
    {
        var (_, json) = await ExecuteFunction("cart_get", 1);
        json.Should().Equals("{error: 'not found'}");
        (_, json) = await ExecuteFunction("cart_get", 6);
        json.Should().Equals("{error: 'not found'}");
        (_, json) = await ExecuteFunction("cart_get", 7);
        var invoiceView = JsonSerializer.Deserialize<InvoiceView>(json);
        invoiceView?.person_id.Should().Equals(7);
        invoiceView?.id.Should().Equals(4);
        invoiceView?.line_items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Test_Addresses_Get()
    {
        var (_, json) = await ExecuteFunction("addresses_get", 1);
        var addresses = JsonSerializer.Deserialize<IReadOnlyCollection<AddressView>>(json);
        addresses.Should().BeEmpty();
        (_, json) = await ExecuteFunction("addresses_get", 6);
        addresses = JsonSerializer.Deserialize<IReadOnlyCollection<AddressView>>(json);
        addresses.Should().BeEmpty();
        (_, json) = await ExecuteFunction("addresses_get", 3);
        var expected = JsonSerializer.Serialize(
            new AddressView(
                id: 2,
                country: "GB",
                address: "Veruca Salt\n10 Posh Lane\nPoshest House\nKensington, London WC1 7NT"));
        json.Should().Equals(expected);
    }

    [Fact]
    public async Task Test_Invoices_Unshipped()
    {
        var (_, json) = await ExecuteFunction("invoices_unshipped");
        var invoices = JsonSerializer.Deserialize<IReadOnlyList<InvoiceView>>(json);
        invoices.Should().HaveCount(1);
        invoices?[0].address.Should()
            .Equals("Veruca Salt\n10 Posh Lane\nPoshest House\nKensington, London WC1 7NT");
    }

    [Fact]
    public async Task Test_Invoices_Shipped()
    {
        var shipInfo = "Airmail";
        var (_, json) = await ExecuteFunction("invoice_shipped", 2, shipInfo);
        var invoice = JsonSerializer.Deserialize<InvoiceView>(json);
        invoice?.ship_info.Should().Equals(shipInfo);
        invoice?.ship_date.Should().Equals(DateTime.Now);
    }
}