namespace Pymen.Infrastructure.Documents;

public interface IDeliveryNotePdfGenerator
{
    byte[] Generate(DeliveryNotePdfModel model);
}
