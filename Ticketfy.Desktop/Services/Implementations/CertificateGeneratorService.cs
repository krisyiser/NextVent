using System;
using Ticketfy.Data.Dtos;
using Ticketfy.Data.Entities;
using Ticketfy.Services.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Ticketfy.Services.Implementations;

public class CertificateGeneratorService : ICertificateGeneratorService
{
    public CertificateGeneratorService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateCertificateOfAuthenticity(SaleDto sale, ProductEntity premiumProduct)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(12).FontFamily("Times New Roman"));

                page.Header().Text("CERTIFICADO DE AUTENTICIDAD")
                    .SemiBold().FontSize(24).FontColor(Colors.Grey.Darken4)
                    .AlignCenter();

                page.Content().PaddingVertical(1, Unit.Centimetre).Column(x =>
                {
                    x.Spacing(20);
                    x.Item().Text("Este documento certifica que el producto:");
                    x.Item().Text(premiumProduct.Name).Bold().FontSize(18).AlignCenter();
                    x.Item().Text($"Adquirido el {sale.LocalDateDisplay} bajo el folio {sale.SerieFolio ?? sale.Id}");
                    x.Item().Text("Es una pieza original y auténtica, avalada por Ticketfy Corp.");
                    // Inyectar código QR de validación aquí
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Firma Autorizada: _______________________");
                });
            });
        });

        return document.GeneratePdf();
    }
}
