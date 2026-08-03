namespace NextVent.Core.Models;

public class AttendanceResultModel
{
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string AttendanceId { get; set; } = string.Empty;
}
