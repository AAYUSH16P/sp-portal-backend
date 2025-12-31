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
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheet(1);

        var headers = sheet.Row(1).Cells()
            .Select(c => c.GetString().Trim().ToLower())
            .ToList();

        var missing = RequiredHeaders
            .Where(h => !headers.Contains(h))
            .ToList();

        if (missing.Any())
            throw new Exception($"Missing columns: {string.Join(", ", missing)}");
    }
}