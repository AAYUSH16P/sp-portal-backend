using ClosedXML.Excel;

public static class ExcelTemplateValidator
{
    private static readonly string[] RequiredHeaders =
    {
        "companyemployeeid",
        "workingsince",
        "ctc",
        "jobtitle",
        "role",
        "gender",
        "location",
        "totalexperience",
        "technicalskills",
        "tools",
        "numberofprojects",
        "employernote",
        "certifications"
    };

    public static void Validate(Stream stream)
    {
        try
        {
            using var workbook = new XLWorkbook(stream);
            var sheet = workbook.Worksheet(1);

            if (sheet == null)
                throw new TemplateValidationException("Excel sheet is missing");

            var headers = sheet.Row(1).CellsUsed()
                .Select(c => c.GetString().Trim().ToLower())
                .ToList();

            var missing = RequiredHeaders
                .Where(h => !headers.Contains(h))
                .ToList();

            if (missing.Any())
                throw new TemplateValidationException(
                    $"Missing columns: {string.Join(", ", missing)}"
                );
        }
        catch (TemplateValidationException)
        {
            throw;
        }
        catch
        {
            throw new TemplateValidationException("Invalid Excel format");
        }
    }
}