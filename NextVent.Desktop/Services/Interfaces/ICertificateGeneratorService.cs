using NextVent.Data.Dtos;
using NextVent.Data.Entities;

namespace NextVent.Services.Interfaces;

public interface ICertificateGeneratorService
{
    byte[] GenerateCertificateOfAuthenticity(SaleDto sale, ProductEntity premiumProduct);
}
