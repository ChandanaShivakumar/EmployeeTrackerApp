using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

class Program
{
    static async Task Main(string[] args)
    {
        string apiKey = "vO17RnE8vuzXzPJo5eaLLjXjmRW07law99QTD90zat9FfOQJKKUcgQ==";
        string apiUrl = $"https://rc-vault-fap-live-1.azurewebsites.net/api/gettimeentries?code={apiKey}";

        using HttpClient client = new HttpClient();
        var response = await client.GetStringAsync(apiUrl);
        var entries = JsonConvert.DeserializeObject<List<TimeEntry>>(response);

        var employees = entries
            .GroupBy(e => e.EmployeeName)
            .Select(g => new Employee
            {
                Name = g.Key,
                TotalTimeWorked = g.Sum(e => (DateTime.Parse(e.EndTimeUtc) - DateTime.Parse(e.StarTimeUtc)).TotalHours)
            })
            .OrderByDescending(e => e.TotalTimeWorked)
            .ToList();

        // Generate HTML Table
        string html = "<html><body><table border='1'><tr><th>Name</th><th>Total Time Worked</th></tr>";

        foreach (var employee in employees)
        {
            string rowColor = employee.TotalTimeWorked < 100 ? "style='background-color: #FFC0CB;'" : "";
            html += $"<tr {rowColor}><td>{employee.Name}</td><td>{employee.TotalTimeWorked:F2} hours</td></tr>";
        }

        html += "</table></body></html>";
        File.WriteAllText("employees.html", html);
        Console.WriteLine("HTML file created: employees.html");

        // Generate Pie Chart
        GeneratePieChart(employees);
    }

    static void GeneratePieChart(List<Employee> employees)
    {
        int width = 600;
        int height = 400;
        Bitmap bitmap = new Bitmap(width, height);
        Graphics graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.White);

        float totalHours = employees.Sum(e => (float)e.TotalTimeWorked);
        float startAngle = 0;
        Random random = new Random();
        Font font = new Font("Arial", 10);
        Brush textBrush = Brushes.Black;

        foreach (var employee in employees)
        {
            float sweepAngle = (float)(employee.TotalTimeWorked / totalHours) * 360;
            Color color = Color.FromArgb(random.Next(256), random.Next(256), random.Next(256));
            Brush brush = new SolidBrush(color);

            // Draw pie slice
            graphics.FillPie(brush, 100, 50, 400, 300, startAngle, sweepAngle);

            // Calculate label position
            float midAngle = startAngle + sweepAngle / 2;
            float labelX = 300 + (float)(200 * Math.Cos(midAngle * Math.PI / 180));
            float labelY = 200 + (float)(150 * Math.Sin(midAngle * Math.PI / 180));

            // Draw label with name and total time worked
            graphics.DrawString($"{employee.Name}: {employee.TotalTimeWorked:F2} hrs", font, textBrush, labelX, labelY);

            startAngle += sweepAngle;
        }

        string filePath = "employees_pie_chart.png";
        bitmap.Save(filePath, ImageFormat.Png);
        Console.WriteLine($"Pie chart created: {filePath}");

        // Clean up
        graphics.Dispose();
        bitmap.Dispose();
    }
}

class TimeEntry
{
    public string EmployeeName { get; set; }
    public string StarTimeUtc { get; set; }
    public string EndTimeUtc { get; set; }
}

class Employee
{
    public string Name { get; set; }
    public double TotalTimeWorked { get; set; }
}
