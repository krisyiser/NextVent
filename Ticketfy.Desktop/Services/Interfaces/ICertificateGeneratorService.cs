using Ticketfy.Data.Dtos;
using Ticketfy.Data.Entities;

namespace Ticketfy.Services.Interfaces;

public interface ICertificateGeneratorService
{
    byte[] GenerateCertificateOfAuthenticity(SaleDto sale, ProductEntity premiumProduct);
}
