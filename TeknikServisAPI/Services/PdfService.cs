using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TeknikServisAPI.Models;
using System.Text.Json;

namespace TeknikServisAPI.Services
{
    public class PdfService
    {
        /// <summary>
        /// Tarih ve saati formatlar. Saat 00:00 ise sadece tarihi gösterir.
        /// </summary>
        private string FormatDateTimeForDisplay(DateTime? dateTime)
        {
            if (!dateTime.HasValue)
                return "-";

            var date = dateTime.Value;
            var dateStr = date.ToString("dd.MM.yyyy");
            var timeStr = date.ToString("HH:mm");

            // Eğer saat 00:00 ise, sadece tarihi göster
            return timeStr == "00:00" ? dateStr : $"{dateStr} {timeStr}";
        }

        public byte[] GenerateServiceFormPdf(ServiceForm form)
        {
            // QuestPDF License zaten Program.cs'de global olarak ayarlandı
            
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Header().Element(c => ComposeHeader(c, form));
                    page.Content().Element(content => ComposeContent(content, form));
                    page.Footer().Element(ComposeFooter);
                });

                // Fotoğraflar için ikinci sayfa
                List<PhotoData>? photos = null;
                
                try
                {
                    if (!string.IsNullOrEmpty(form.Photos))
                    {
                        photos = JsonSerializer.Deserialize<List<PhotoData>>(form.Photos);
                    }
                    else
                    {
                        photos = new List<PhotoData>();
                    }
                }
                catch
                {
                    photos = new List<PhotoData>();
                }

                if (photos != null && photos.Count > 0)
                {
                    const int photosPerPage = 6; // 3 satir x 2 foto
                    for (int i = 0; i < photos.Count; i += photosPerPage)
                    {
                        var pagePhotos = photos.Skip(i).Take(photosPerPage).ToList();
                        container.Page(page =>
                        {
                            page.Size(PageSizes.A4);
                            page.Margin(30);
                            page.PageColor(Colors.White);
                            page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));
                            
                            page.Header().Element(c => ComposeHeader(c, form));
                            page.Content().Element(content => ComposePhotos(content, pagePhotos));
                            page.Footer().Element(ComposeFooter);
                        });
                    }
                }
            });

            return document.GeneratePdf();
        }

        private void ComposeHeader(IContainer container, ServiceForm form)
        {
            container.PaddingBottom(10).Row(row =>
            {
                // Logo (sol üst)
                row.RelativeItem(2).Column(column =>
                {
                    try
                    {
                        var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "etisan-logo.png");
                        if (File.Exists(logoPath))
                        {
                            // Logo'yu genişlik ile sınırla, yükseklik otomatik ayarlanacak
                            column.Item().Width(100).Image(logoPath);
                        }
                        else
                        {
                            // Logo bulunamadı - text fallback
                            column.Item().Text("etisan").FontSize(24).Bold()
                                .FontColor(Colors.Orange.Darken2).FontFamily("Arial");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Logo yükleme hatası - text fallback
                        Console.WriteLine($"Logo yükleme hatası: {ex.Message}");
                        column.Item().Text("etisan").FontSize(24).Bold()
                            .FontColor(Colors.Orange.Darken2).FontFamily("Arial");
                    }
                });

                // Başlık (orta)
                row.RelativeItem(3).AlignCenter().AlignMiddle().Text("TEKNİK SERVİS TALEP FORMU")
                    .FontSize(14).Bold().FontFamily("Arial");

                // Form No (sağ)
                row.RelativeItem(2).AlignRight().Column(column =>
                {
                    column.Item().Text($"Form No: #{form.Id.ToString().PadLeft(4, '0')}")
                        .FontSize(12).Bold().FontColor(Colors.Red.Medium).FontFamily("Arial");
                    column.Item().Text($"Tarih: {form.FormDate:dd.MM.yyyy}")
                        .FontSize(9).FontFamily("Arial");
                });
            });
        }

        private void ComposeFooter(IContainer container)
        {
            container.AlignBottom().Column(column =>
            {
                column.Spacing(2);
                
                column.Item().AlignCenter().Text("NOT: SERVİS VERİLİRKEN SADECE YAZILAN TALEPLER YERİNE GETİRİLECEKTİR.")
                    .FontSize(6).Bold().FontFamily("Arial");
                
                column.Item().AlignCenter().Text("ETİSAN PROJE BİLGİ VE YAZILIM TEK. SAN. TİC. A.Ş.")
                    .FontSize(6).FontFamily("Arial");
                
                column.Item().AlignCenter().Text("Prof. Dr. A. Taner Kışlalı Mah. 2853. Cad. Önder Sok. NO:4 ÇANKAYA - ANKARA")
                    .FontSize(6).FontFamily("Arial");
                
                column.Item().AlignCenter().Text("t: +90 312 442 42 24 / +90 312 442 42 88 f: +90 312 442 14 64")
                    .FontSize(6).FontFamily("Arial");
            });
        }

        private void ComposeContent(IContainer container, ServiceForm form)
        {
            container.Column(column =>
            {
                column.Spacing(10);

                // Genel Bilgiler
                column.Item().Element(c => ComposeSection(c, "GENEL BİLGİLER", new List<(string Label, string Value)>
                {
                    ("Firma Adı", (form.InstitutionIsOther ? form.InstitutionOtherName : form.InstitutionName) ?? "-"),
                    ("İletişim Kişisi", $"{form.ContactFirstName ?? ""} {form.ContactLastName ?? ""}"),
                    ("Adres", form.Address ?? "-"),
                    ("E-posta", form.Email ?? "-"),
                    ("Servis Başlangıç", FormatDateTimeForDisplay(form.ServiceStartDate)),
                    ("Telefon", form.Phone ?? "-"),
                    ("Servis Bitiş", FormatDateTimeForDisplay(form.ServiceEndDate)),
                    ("", "")
                }));

                // Servis Kapsamı
                column.Item().Element(c => ComposeScopeSection(c, form));

                // Bildirilen Sorun
                column.Item().Element(c => ComposeTextSection(c, "BİLDİRİLEN SORUN", form.ReportedIssue));

                // Yapılan İşlemler
                column.Item().Element(c => ComposeTextSection(c, "YAPILAN İŞLEMLER", form.WorkPerformed));

                // Kullanılan Malzemeler
                var formattedMaterials = FormatMaterialsUsed(form.MaterialsUsed);
                column.Item().Element(c => ComposeTextSection(c, "KULLANILAN MALZEMELER", formattedMaterials));

                // Durum
                column.Item().Element(c => ComposeStatusSection(c, form));

                // İmzalar
                column.Item().Element(c => ComposeSignatures(c, form));
            });
        }

        private void ComposeSection(IContainer container, string title, List<(string Label, string Value)> fields)
        {
            container.Column(column =>
            {
                column.Item().BorderBottom(1).BorderColor(Colors.Black).PaddingBottom(2)
                    .Text(title).FontSize(8).Bold().FontFamily("Arial");

                column.Item().PaddingTop(2).Border(1).BorderColor(Colors.Grey.Medium)
                    .Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(80);
                            columns.RelativeColumn();
                            columns.ConstantColumn(80);
                            columns.RelativeColumn();
                        });

                        for (int i = 0; i < fields.Count; i += 2)
                        {
                            table.Cell().Border(1).BorderColor(Colors.Grey.Medium).Padding(3)
                                .Text(string.IsNullOrEmpty(fields[i].Label) ? "" : $"{fields[i].Label}:")
                                .FontSize(7).Bold().FontFamily("Arial");
                            
                            table.Cell().Border(1).BorderColor(Colors.Grey.Medium).Padding(3)
                                .Text(fields[i].Value ?? "-").FontSize(7).FontFamily("Arial");

                            if (i + 1 < fields.Count)
                            {
                                table.Cell().Border(1).BorderColor(Colors.Grey.Medium).Padding(3)
                                    .Text(string.IsNullOrEmpty(fields[i + 1].Label) ? "" : $"{fields[i + 1].Label}:")
                                    .FontSize(7).Bold().FontFamily("Arial");
                                
                                table.Cell().Border(1).BorderColor(Colors.Grey.Medium).Padding(3)
                                    .Text(fields[i + 1].Value ?? "-").FontSize(7).FontFamily("Arial");
                            }
                            else
                            {
                                table.Cell().Border(1).BorderColor(Colors.Grey.Medium);
                                table.Cell().Border(1).BorderColor(Colors.Grey.Medium);
                            }
                        }
                    });
            });
        }

        private void ComposeScopeSection(IContainer container, ServiceForm form)
        {
            container.Column(column =>
            {
                column.Item().BorderBottom(1).BorderColor(Colors.Black).PaddingBottom(2)
                    .Text("SERVİS KAPSAMI").FontSize(8).Bold().FontFamily("Arial");

                column.Item().PaddingTop(2).Border(1).BorderColor(Colors.Grey.Medium).Padding(5)
                    .Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(90);
                            columns.RelativeColumn();
                        });
                        
                        table.Cell().Padding(2).Text("Servis Kapsamı:").FontSize(7).Bold().FontFamily("Arial");
                        table.Cell().Padding(2).Row(row =>
                        {
                            if (form.ServiceScope == "Diger")
                            {
                                row.AutoItem().Text($"☑ Diğer: {form.ServiceScopeOther ?? ""}").FontSize(7).FontFamily("Arial");
                            }
                            else
                            {
                                row.AutoItem().PaddingRight(2).Text(form.ServiceScope == "SozlesmeIci" ? "☑" : "☐").FontSize(7).FontFamily("Arial");
                                row.AutoItem().PaddingRight(10).Text("Sözleşme İçi").FontSize(7).FontFamily("Arial");
                                row.AutoItem().PaddingRight(2).Text(form.ServiceScope == "SozlesmeDisi" ? "☑" : "☐").FontSize(7).FontFamily("Arial");
                                row.AutoItem().Text("Sözleşme Dışı").FontSize(7).FontFamily("Arial");
                            }
                        });
                        
                        table.Cell().Padding(2).Text("Hizmet Türü:").FontSize(7).Bold().FontFamily("Arial");
                        table.Cell().Padding(2).Row(row =>
                        {
                            if (form.ServiceType == "Diger")
                            {
                                row.AutoItem().Text($"☑ Diğer: {form.ServiceTypeOther ?? ""}").FontSize(7).FontFamily("Arial");
                            }
                            else
                            {
                                row.AutoItem().PaddingRight(2).Text(form.ServiceType == "ArizaGiderme" ? "☑" : "☐").FontSize(7).FontFamily("Arial");
                                row.AutoItem().PaddingRight(10).Text("Arıza Giderme").FontSize(7).FontFamily("Arial");
                                row.AutoItem().PaddingRight(2).Text(form.ServiceType == "Bakim" ? "☑" : "☐").FontSize(7).FontFamily("Arial");
                                row.AutoItem().PaddingRight(10).Text("Bakım").FontSize(7).FontFamily("Arial");
                                row.AutoItem().PaddingRight(2).Text(form.ServiceType == "Kesif" ? "☑" : "☐").FontSize(7).FontFamily("Arial");
                                row.AutoItem().Text("Keşif").FontSize(7).FontFamily("Arial");
                            }
                        });
                    });
            });
        }

        private void ComposeTextSection(IContainer container, string title, string? content)
        {
            container.Column(column =>
            {
                column.Item().BorderBottom(1).BorderColor(Colors.Black).PaddingBottom(2)
                    .Text(title).FontSize(8).Bold().FontFamily("Arial");

                column.Item().PaddingTop(2).Border(1).BorderColor(Colors.Grey.Medium).Padding(5)
                    .MinHeight(60)
                    .Text(content ?? "-").FontSize(7).FontFamily("Arial");
            });
        }

        private void ComposeStatusSection(IContainer container, ServiceForm form)
        {
            container.Column(column =>
            {
                column.Item().BorderBottom(1).BorderColor(Colors.Black).PaddingBottom(2)
                    .Text("DURUM").FontSize(8).Bold().FontFamily("Arial");

                column.Item().PaddingTop(2).Border(1).BorderColor(Colors.Grey.Medium).Padding(5)
                    .Row(row =>
                    {
                        if (form.IssueStatus == "Diger")
                        {
                            row.AutoItem().Text($"☑ Diğer: {form.IssueStatusOther ?? ""}").FontSize(7).FontFamily("Arial");
                        }
                        else
                        {
                            row.AutoItem().PaddingRight(2).Text(form.IssueStatus == "SorunGiderilmedi" ? "☑" : "☐").FontSize(7).FontFamily("Arial");
                            row.AutoItem().PaddingRight(10).Text("Sorun giderilmedi").FontSize(7).FontFamily("Arial");
                            row.AutoItem().PaddingRight(2).Text(form.IssueStatus == "SorunGiderildi" ? "☑" : "☐").FontSize(7).FontFamily("Arial");
                            row.AutoItem().PaddingRight(10).Text("Sorun giderildi").FontSize(7).FontFamily("Arial");
                            row.AutoItem().PaddingRight(2).Text(form.IssueStatus == "SorunaRastlanmadi" ? "☑" : "☐").FontSize(7).FontFamily("Arial");
                            row.AutoItem().PaddingRight(10).Text("Soruna rastlanmadı").FontSize(7).FontFamily("Arial");
                            row.AutoItem().PaddingRight(2).Text(form.IssueStatus == "ParcaDegistirildi" ? "☑" : "☐").FontSize(7).FontFamily("Arial");
                            row.AutoItem().PaddingRight(10).Text("Parça değiştirildi").FontSize(7).FontFamily("Arial");
                            row.AutoItem().PaddingRight(2).Text(form.IssueStatus == "ParcaDegistirilecek" ? "☑" : "☐").FontSize(7).FontFamily("Arial");
                            row.AutoItem().Text("Parça değiştirilecek").FontSize(7).FontFamily("Arial");
                        }
                    });
            });
        }

        private void ComposeSignatures(IContainer container, ServiceForm form)
        {
            container.Column(column =>
            {
                column.Item().BorderBottom(1).BorderColor(Colors.Black).PaddingBottom(2)
                    .Text("ONAY VE İMZALAR").FontSize(8).Bold().FontFamily("Arial");

                column.Item().Border(1).BorderColor(Colors.Grey.Medium).Padding(8)
                    .Row(row =>
                    {
                        // Etisan Servis Yetkilisi
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Etisan Servis Yetkilisi").FontSize(9).Bold().FontFamily("Arial");
                            col.Item().Text($"{form.EtisanFirstName ?? ""} {form.EtisanLastName ?? ""}").FontSize(9).FontFamily("Arial");
                            
                            var etisanImage = GetBase64Image(form.EtisanSignature);
                            if (etisanImage != null)
                            {
                                col.Item().Height(40).Border(1).BorderColor(Colors.Grey.Lighten1)
                                    .Image(etisanImage);
                            }
                        });

                        // Kurum Yetkilisi 
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Firma Kurum Yetkilisi").FontSize(9).Bold().FontFamily("Arial");
                            col.Item().Text($"{form.AuthorityFirstName ?? ""} {form.AuthorityLastName ?? ""}").FontSize(9).FontFamily("Arial");
                            
                            var authorityImage = GetBase64Image(form.AuthoritySignature);
                            if (authorityImage != null)
                            {
                                col.Item().Height(40).Border(1).BorderColor(Colors.Grey.Lighten1)
                                    .Image(authorityImage);
                            }
                        });
                    });

                column.Item().PaddingTop(5).Text(
                    "Elektronik imzamın elle atılmış bir imza kadar geçerli olacağını ve yürürlükteki yasaların imza verdiği ölçüde orijinal olarak kabul edileceğini anlıyor ve kabul ediyorum."
                ).FontSize(7).Italic().FontFamily("Arial");
            });
        }

        private byte[]? GetBase64Image(string? base64String)
        {
            if (string.IsNullOrEmpty(base64String))
                return null;

            try
            {
                base64String = base64String.Trim();
                
                if (base64String.Contains(","))
                {
                    var parts = base64String.Split(',');
                    if (parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]))
                    {
                        return Convert.FromBase64String(parts[1].Trim());
                    }
                }
                else
                {
                    return Convert.FromBase64String(base64String);
                }
            }
            catch
            {
                // Ignore image loading errors
            }
            
            return null;
        }

        private void ComposePhotos(IContainer container, List<PhotoData> photos)
        {
            container.Column(column =>
            {
                column.Spacing(10);

                if (photos.Count == 0)
                {
                    column.Item().Text("Fotoğraf eklenmedi.").FontSize(10).Italic().FontFamily("Arial");
                    return;
                }

                for (int i = 0; i < photos.Count; i += 2)
                {
                    column.Item().Row(row =>
                    {
                        row.Spacing(10);

                        // İlk fotoğraf
                        var photoUrl = photos[i].GetImageUrl();
                        var firstImage = GetBase64Image(photoUrl);
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text($"Fotoğraf {i + 1}").FontSize(9).Bold().FontFamily("Arial");
                            if (firstImage != null)
                            {
                                col.Item().Border(1).BorderColor(Colors.Grey.Medium)
                                    .Height(200)
                                    .Image(firstImage).FitArea();
                            }
                            else
                            {
                                col.Item().Border(1).BorderColor(Colors.Grey.Medium)
                                    .Height(200)
                                    .AlignCenter().AlignMiddle()
                                    .Text("Fotoğraf yüklenemedi").FontSize(9).Italic().FontFamily("Arial");
                            }
                        });

                        // İkinci fotoğraf (varsa)
                        if (i + 1 < photos.Count)
                        {
                            var secondPhotoUrl = photos[i + 1].GetImageUrl();
                            var secondImage = GetBase64Image(secondPhotoUrl);
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text($"Fotoğraf {i + 2}").FontSize(9).Bold().FontFamily("Arial");
                                if (secondImage != null)
                                {
                                    col.Item().Border(1).BorderColor(Colors.Grey.Medium)
                                        .Height(200)
                                        .Image(secondImage).FitArea();
                                }
                                else
                                {
                                    col.Item().Border(1).BorderColor(Colors.Grey.Medium)
                                        .Height(200)
                                        .AlignCenter().AlignMiddle()
                                        .Text("Fotoğraf yüklenemedi").FontSize(9).Italic().FontFamily("Arial");
                                }
                            });
                        }
                        else
                        {
                            row.RelativeItem();
                        }
                    });
                }
            });
        }

        private string FormatMaterialsUsed(string? materialsUsed)
        {
            if (string.IsNullOrEmpty(materialsUsed))
                return "-";

            var parts = materialsUsed.Split(new[] { ", " }, StringSplitOptions.None);
            var formattedParts = parts.Select(part =>
            {
                var match = System.Text.RegularExpressions.Regex.Match(part, @"^(\d+)x\s+(.+)$");
                if (match.Success)
                {
                    var quantity = match.Groups[1].Value;
                    var itemName = match.Groups[2].Value;
                    return $"{quantity} adet {itemName}";
                }
                return part;
            });

            return string.Join(", ", formattedParts);
        }

        private class PhotoData
        {
            public string Uid { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;
            public string? url { get; set; }
            public string? thumbUrl { get; set; }
            public string? response { get; set; }
            
            public string GetImageUrl()
            {
                if (!string.IsNullOrEmpty(Url)) return Url;
                if (!string.IsNullOrEmpty(url)) return url;
                if (!string.IsNullOrEmpty(thumbUrl)) return thumbUrl;
                if (!string.IsNullOrEmpty(response)) return response;
                return string.Empty;
            }
        }
    }
}
