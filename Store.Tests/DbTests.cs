using FluentAssertions;
using Store.Tests;

public class DbTests : Tester
{
    public DbTests() : base()
    {
    }

    [Fact]
    public async Task Test_Unique_Inv_Item()
    {
        Func<Task> act = async () => await Exec("insert into lineitems(invoice_id, item_id) values(4, 4)");
        await act.Should().ThrowAsync<PostgresException>();
    }

    /* #########################################
    ##############################TEST TRIGGERS: 
    ############################################
    */

    [Fact]
    public async Task Test_LineItem_Calc_Price()
    {
        await Exec("update lineitems set quantity = 10 where id = 4");
        var res = await QueryFirst<int>("select price from lineitems where id = 4");
        res.Should().Equals(20);

        await Exec("update lineitems set quantity = 50 where id = 4");
        res = await QueryFirst<int>("select price from lineitems where id = 4");
        res.Should().Equals(100);

        await Exec("update lineitems set quantity = 1, item_id = 2 where id = 4");
        var f = await QueryFirst<float>("select price from lineitems where id = 4");
        f.Should().Equals(149.99);

        await Exec("update lineitems set quantity = 2 where id = 4");
        f = await QueryFirst<float>("select price from lineitems where id = 4");
        f.Should().Equals(299.98);
    }

    [Fact]
    public async Task Test_LineItem_Calc_Invoice()
    {
        await Exec("update lineitems set quantity = 10 where id = 4");
        var res = await QueryFirst<float>("select subtotal from invoices where id = 4");
        res.Should().Equals(20);

        await Exec("insert into lineitems values(default, 4, 2, 1, default)");
        res = await QueryFirst<float>("select subtotal from invoices where id = 4");
        res.Should().Equals(169.99);
    }

    [Fact]
    public async Task Test_LineItem_Calc_Shipping()
    {
        await Exec("insert into lineitems(invoice_id, item_id, quantity) values (4, 2, 1)");
        var res = await QueryFirst<Invoice>("select shipping, total from invoices where id = 4");
        res.shipping.Should().Equals(7);
        res.total.Should().Equals(158.99);

        await Exec("insert into lineitems(invoice_id, item_id, quantity) values (4, 3, 1)");
        res = await QueryFirst<Invoice>("select shipping, total from invoices where id = 4");
        res.shipping.Should().Equals(9);
        res.total.Should().Equals(165.99);

        await Exec("update lineitems set quantity = 100 where invoice_id = 4 and item_id = 3");
        res = await QueryFirst<Invoice>("select shipping, total from invoices where id = 4");
        res.shipping.Should().Equals(14);
        res.total.Should().Equals(665.99);

        await Exec("delete from lineitems where invoice_id = 4 and id > 4");
        res = await QueryFirst<Invoice>("select shipping, total from invoices where id = 4");
        res.shipping.Should().Equals(0);
        res.total.Should().Equals(2);

        await Exec("delete from lineitems where invoice_id = 4");
        res = await QueryFirst<Invoice>("select shipping, total from invoices where id = 4");
        res.shipping.Should().Equals(0);
        res.total.Should().Equals(0);
    }

    [Fact]
    public async Task Test_No_Alter_Paid_Lineitem1()
    {
        Func<Task> act = async () => await Exec("delete from lineitems where id = 1");
        await act.Should().ThrowAsync<PostgresException>().WithMessage("no_alter_paid_lineitem");
    }

    [Fact]
    public async Task Test_No_Alter_Paid_Lineitem2()
    {
        Func<Task> act = async () => await Exec("update lineitems set quantity = 9 where id = 1");
        await act.Should().ThrowAsync<PostgresException>().WithMessage("no_alter_paid_lineitem");
    }

    [Fact]
    public async Task Test_No_Alter_Shipped_Invoice1()
    {
        Func<Task> act = async () => await Exec("delete from invoices where id = 1");
        await act.Should().ThrowAsync<PostgresException>().WithMessage("no_alter_shipped_invoice");
    }

    [Fact]
    public async Task Test_No_Alter_Shipped_Invoice2()
    {
        Func<Task> act = async () => await Exec("update invoices set total = 1 where id = 1");
        await act.Should().ThrowAsync<PostgresException>().WithMessage("no_alter_shipped_invoice");
    }

    [Fact]
    public async Task Test_Ok_Alter_Unshipped_Invoice()
    {
        var invoice = await QueryFirst<Invoice>("update invoices set ship_date=now(), ship_info='FedEx' where id=2 returning *");
        invoice.ship_info.Should().Equals("FedEx");
        invoice.ship_date.Should().Equals(DateTime.Now);
    }

    /* #########################################
    #############################TEST FUNCTIONS: 
    ############################################
    */

    [Fact]
    public async Task Test_Invoice_Needs_Shipment()
    {
        var res = await QueryFirst<string>("select invoice_needs_shipment from store.invoice_needs_shipment(1)");
        res.Should().Equals("t");

        res = await QueryFirst<string>("select invoice_needs_shipment from store.invoice_needs_shipment(2)");
        res.Should().Equals("t");

        res = await QueryFirst<string>("select invoice_needs_shipment from store.invoice_needs_shipment(3)");
        res.Should().Equals("f");

        res = await QueryFirst<string>("select invoice_needs_shipment from store.invoice_needs_shipment(4)");
        res.Should().Equals("f");

        res = await QueryFirst<string>("select invoice_needs_shipment from store.invoice_needs_shipment(99)");
        res.Should().Equals("f");
    }

    [Fact]
    public async Task Test_Shipcost()
    {
        var res = await QueryFirst<int>("select shipcost from store.shipcost('US', 0)");
        res.Should().Equals(0);

        res = await QueryFirst<int>("select shipcost from store.shipcost('CA', 0)");
        res.Should().Equals(0);

        res = await QueryFirst<int>("select shipcost from store.shipcost('ZH', 0)");
        res.Should().Equals(0);

        res = await QueryFirst<int>("select shipcost from store.shipcost('US', 0.1)");
        res.Should().Equals(3);

        res = await QueryFirst<int>("select shipcost from store.shipcost('US', 1)");
        res.Should().Equals(4);

        res = await QueryFirst<int>("select shipcost from store.shipcost('US', 1.5)");
        res.Should().Equals(5);

        res = await QueryFirst<int>("select shipcost from store.shipcost('US', 4)");
        res.Should().Equals(7);

        res = await QueryFirst<int>("select shipcost from store.shipcost('US', 4.01)");
        res.Should().Equals(12);

        res = await QueryFirst<int>("select shipcost from store.shipcost('CA', 0.3)");
        res.Should().Equals(5);

        res = await QueryFirst<int>("select shipcost from store.shipcost('CA', 1)");
        res.Should().Equals(6);

        res = await QueryFirst<int>("select shipcost from store.shipcost('CA', 400)");
        res.Should().Equals(13);

        res = await QueryFirst<int>("select shipcost from store.shipcost('RU', 0.5)");
        res.Should().Equals(7);

        res = await QueryFirst<int>("select shipcost from store.shipcost('IE', 1)");
        res.Should().Equals(8);

        res = await QueryFirst<int>("select shipcost from store.shipcost('IE', 1.01)");
        res.Should().Equals(9);

        res = await QueryFirst<int>("select shipcost from store.shipcost('SG', 400)");
        res.Should().Equals(14);

        res = await QueryFirst<int>("select shipcost from store.shipcost('XX', 1000)");
        res.Should().Equals(20);

        res = await QueryFirst<int>("select shipcost from store.shipcost('XX', 1)");
        res.Should().Equals(20);
    }

    [Fact]
    public async Task Test_Invoice_Shipcost()
    {
        var res = await QueryFirst<int>("select cost from store.invoice_shipcost(1)");
        res.Should().Equals(6);

        res = await QueryFirst<int>("select cost from store.invoice_shipcost(2)");
        res.Should().Equals(10);

        res = await QueryFirst<int>("select cost from store.invoice_shipcost(3)");
        res.Should().Equals(0);

        res = await QueryFirst<int>("select cost from store.invoice_shipcost(4)");
        res.Should().Equals(0);
    }

    [Fact]
    public async Task Test_Cart_Get()
    {
        var res = await QueryFirst<int?>("select id from store.cart_get_id(1)");
        res.Should().BeNull();

        res = await QueryFirst<int>("select cost from store.invoice_shipcost(2)");
        res.Should().Equals(10);

        res = await QueryFirst<int>("select cost from store.invoice_shipcost(3)");
        res.Should().Equals(0);

        res = await QueryFirst<int>("select cost from store.invoice_shipcost(4)");
        res.Should().Equals(0);
    }

    [Fact]
    public async Task Test_Cart_New()
    {
        var newId = await QueryFirst<int>("select id from store.cart_new_id(1)");
        newId.Should().BeGreaterThan(4);

        var invoice = await QueryFirst<Invoice>($"select person_id, country from invoices where id={newId}");
        invoice.person_id.Should().Equals("1");
        invoice.country.Should().Equals("SG");
    }
}
