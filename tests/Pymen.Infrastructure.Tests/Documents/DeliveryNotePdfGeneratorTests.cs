using System.Text;
using FluentAssertions;
using Pymen.Infrastructure.Documents;

namespace Pymen.Infrastructure.Tests.Documents;

public class DeliveryNotePdfGeneratorTests
{
    [Fact]
    public void Generate_produces_pdf_containing_delivery_note_number()
    {
        var sut = new QuestDeliveryNotePdfGenerator();
        const string remito = "RN-000100000042";

        var bytes = sut.Generate(new DeliveryNotePdfModel(
            "Metalúrgica Demo",
            remito,
            "000100000042",
            "Cliente Acero Sur",
            DateTimeOffset.Parse("2026-10-24T15:00:00Z"),
            [
                new DeliveryNotePdfLine(1, "FG-BRACKET", "Soporte metálico", 2)
            ]));

        bytes.Should().NotBeEmpty();
        Encoding.ASCII.GetString(bytes.AsSpan(0, 5)).Should().Be("%PDF-");
        Encoding.UTF8.GetString(bytes).Should().Contain(remito);
    }
}
