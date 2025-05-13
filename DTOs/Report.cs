namespace BukuTamuAPI.DTOs;


public class DailyReportResponse
{
    public string Date { get; set; }
    public int TotalAppointments { get; set; }
    public int Completed { get; set; }
    public int Waiting { get; set; }
    public int Late { get; set; }
    public double CompletionRate { get; set; }
    public List<TeacherStat> ByTeacher { get; set; }
}


public class WeeklyReportResponse
{
    public int WeekNumber { get; set; }
    public int Year { get; set; }
    public int TotalAppointments { get; set; }
    public double CompletionRate { get; set; }
    public List<DailyStat> DailyStats { get; set; }
}


public class MonthlyReportResponse
{
    public int Month { get; set; }
    public int Year { get; set; }
    public int TotalAppointments { get; set; }
    public double CompletionRate { get; set; }
    public List<WeeklyStat> WeeklyStats { get; set; }
}


public class TeacherStat
{
    public int IdGuru { get; set; }
    public string Nama { get; set; }
    public int Total { get; set; }
    public int Completed { get; set; }
}

public class DailyStat
{
    public string Date { get; set; }
    public int Total { get; set; }
    public int Completed { get; set; }
}

public class WeeklyStat
{
    public int WeekNumber { get; set; }
    public int Total { get; set; }
    public int Completed { get; set; }
}