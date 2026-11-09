using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using HRAnalyticsApp.Repositories;
using HRAnalyticsApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;

namespace HRAnalyticsApp
{
    public record DepartmentRow(string Name, int EmployeesCount, int LogsCount, double TotalHours);
    public record EmployeeRow(string Code, string FullName, string Department, int LogsCount, double TotalHours);
    public record LogRow(DateTime Date, string Code, string FullName, string Department, string Project, double Hours);

    public partial class MainWindow : Window
    {
        private const string JsonBinUrl = "https://api.jsonbin.io/v3/b/6aecb7f0ffd5d16053a99239/latest";

        private readonly string _dbPath;
        private readonly HrAnalyticsService _analyticsService = new();

        private string? _selectedFile;
        private CancellationTokenSource? _cts;

        private static readonly Dictionary<string, string> ColumnHeaders = new()
        {
            ["EmployeeId"] = "Код",
            ["FullName"] = "ПІБ",
            ["StartDate"] = "Початок",
            ["EndDate"] = "Кінець",
            ["DaysCount"] = "Днів поспіль",
            ["AvgHours"] = "Сер. годин/день",
            ["ProjectCode"] = "Проєкт",
            ["TotalHours"] = "Годин усього",
            ["LogsCount"] = "Записів",
            ["DepartmentName"] = "Департамент",
            ["EmployeesCount"] = "Співробітників",
            ["AvgHoursPerLog"] = "Сер. годин/запис",
        };

        public MainWindow()
        {
            InitializeComponent();

            var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            _dbPath = Path.Combine(documents, "hr_database.db");

            GridAnalytics.AutoGeneratingColumn += GridAnalytics_AutoGeneratingColumn;
            Loaded += MainWindow_Loaded;
        }

        private AppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={_dbPath}")
                .Options;
            return new AppDbContext(options);
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await using (var db = CreateDbContext())
                {
                    await db.Database.EnsureDeletedAsync();
                    await db.Database.EnsureCreatedAsync();
                }

                TxtSelectedFile.Text = "Джерело не обрано";
                TxtAnalyticsHint.Text = "Оберіть джерело та імпортуйте дані для формування аналітики.";

                SetAnalyticsButtonsEnabled(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Помилка під час ініціалізації: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnChooseFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Оберіть JSON-файл",
                Filter = "JSON (*.json)|*.json|Усі файли (*.*)|*.*",
                InitialDirectory = @"D:\ТРЕТІЙ КУРС\.NET"
            };

            if (dialog.ShowDialog(this) != true) return;

            _selectedFile = dialog.FileName;
            TxtSelectedFile.Text = $"Файл: {Path.GetFileName(_selectedFile)}";

            await ImportAsync(_selectedFile, "файлу");
        }

        private async void BtnImportCloud_Click(object sender, RoutedEventArgs e)
        {
            TxtSelectedFile.Text = "Джерело: JSONBin Cloud";
            await ImportAsync(JsonBinUrl, "JSONBin");
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (_cts != null)
            {
                _cts.Cancel();
            }
        }

        private async Task ImportAsync(string source, string sourceName)
        {
            _cts = new CancellationTokenSource();
            SetBusy(true);

            try
            {
                ImportSummary summary;
                await using (var db = CreateDbContext())
                {
                    await db.Database.EnsureDeletedAsync();
                    await db.Database.EnsureCreatedAsync();

                    var initializer = new DbInitializer(db, new JsonBinFileReader());
                    summary = await initializer.InitializeAsync(source, _cts.Token);
                }

                await RefreshDataAsync();
                MainTabs.SelectedIndex = 0;
                GridAnalytics.ItemsSource = null;
                ResetAnalyticsButtons();

                SetAnalyticsButtonsEnabled(true);
                TxtAnalyticsHint.Text = "Оберіть необхідний звіт для формування статистики.";

                string skipped = "";
                if (summary.SkippedLogs > 0)
                {
                    skipped = $"\nПропущено записів з некоректною датою: {summary.SkippedLogs}.";
                }

                MessageBox.Show(this,
                    $"Імпортовано з {sourceName}:\n" +
                    $"- департаментів: {summary.Departments}\n" +
                    $"- співробітників: {summary.Employees}\n" +
                    $"- записів часу: {summary.TimeLogs}{skipped}",
                    "Імпорт завершено", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show(this, "Імпорт скасовано. Дані в базі не змінено.", "Інформація", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Помилка під час імпорту: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (_cts != null)
                {
                    _cts.Dispose();
                    _cts = null;
                }
                SetBusy(false);
            }
        }

        private void SetBusy(bool busy)
        {
            if (busy)
            {
                PbBusy.Visibility = Visibility.Visible;
                BtnCancel.Visibility = Visibility.Visible;
                BtnChooseFile.IsEnabled = false;
                BtnImportCloud.IsEnabled = false;
                PanelAnalytics.IsEnabled = false;
            }
            else
            {
                PbBusy.Visibility = Visibility.Collapsed;
                BtnCancel.Visibility = Visibility.Collapsed;
                BtnChooseFile.IsEnabled = true;
                BtnImportCloud.IsEnabled = true;
                PanelAnalytics.IsEnabled = true;
            }
        }

        private void SetAnalyticsButtonsEnabled(bool isEnabled)
        {
            BtnStreaks.IsEnabled = isEnabled;
            BtnProjectStats.IsEnabled = isEnabled;
            BtnEmployeeHours.IsEnabled = isEnabled;
            BtnDepartmentStats.IsEnabled = isEnabled;
        }

        private async Task RefreshDataAsync()
        {
            await using var db = CreateDbContext();
            await db.Database.EnsureCreatedAsync();

            var catalog = new HrCatalog(new SqliteHrRepository(db));
            var departments = await catalog.GetCatalogAsync();

            var departmentRows = new List<DepartmentRow>();
            var employeeRows = new List<EmployeeRow>();
            var logRows = new List<LogRow>();

            foreach (var dept in departments)
            {
                departmentRows.Add(new DepartmentRow(
                    dept.DepartmentName,
                    dept.Employees.Count,
                    dept.Employees.Sum(emp => emp.TimeLogs.Count),
                    dept.Employees.Sum(emp => emp.TimeLogs.Sum(t => t.HoursWorked))));

                foreach (var emp in dept.Employees)
                {
                    employeeRows.Add(new EmployeeRow(
                        emp.EmployeeId,
                        emp.FullName,
                        dept.DepartmentName,
                        emp.TimeLogs.Count,
                        emp.TimeLogs.Sum(t => t.HoursWorked)));

                    foreach (var log in emp.TimeLogs)
                    {
                        logRows.Add(new LogRow(
                            log.Date,
                            emp.EmployeeId,
                            emp.FullName,
                            dept.DepartmentName,
                            log.ProjectCode,
                            log.HoursWorked));
                    }
                }
            }

            GridDepartments.ItemsSource = departmentRows;
            GridEmployees.ItemsSource = employeeRows;
            GridLogs.ItemsSource = logRows.OrderBy(l => l.Date).ThenBy(l => l.Code).ToList();

            TxtStatDept.Text = departmentRows.Count.ToString();
            TxtStatEmp.Text = employeeRows.Count.ToString();
            TxtStatHours.Text = departmentRows.Sum(d => d.TotalHours).ToString("0.0");

            int overtimeCount = 0;
            foreach (var l in logRows)
            {
                if (l.Hours > 8.0)
                {
                    overtimeCount++;
                }
            }
            TxtStatOvertime.Text = overtimeCount.ToString();
        }

        private void ResetAnalyticsButtons()
        {
            var secondaryStyle = (Style)FindResource("SecondaryButton");
            BtnStreaks.Style = secondaryStyle;
            BtnProjectStats.Style = secondaryStyle;
            BtnEmployeeHours.Style = secondaryStyle;
            BtnDepartmentStats.Style = secondaryStyle;
        }

        private void SetActiveAnalyticsButton(Button activeButton)
        {
            var primaryStyle = (Style)FindResource("PrimaryButton");
            ResetAnalyticsButtons();
            activeButton.Style = primaryStyle;
        }

        private async void BtnStreaks_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                SetActiveAnalyticsButton(btn);
            }

            await RunReportAsync(async db =>
            {
                var logs = await db.TimeLogs.AsNoTracking().Include(t => t.Employee).ToListAsync();
                var streaks = _analyticsService.GetOvertimeStreaks(logs);
                return (streaks, $"Знайдено {streaks.Count} серій перепрацювань (>8 год поспіль).");
            });
        }

        private async void BtnProjectStats_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                SetActiveAnalyticsButton(btn);
            }

            await RunReportAsync(async db =>
            {
                var logs = await db.TimeLogs.AsNoTracking().ToListAsync();
                var stats = _analyticsService.GetProjectStats(logs);
                return (stats, $"Знайдено {stats.Count} активних проєктів.");
            });
        }

        private async void BtnEmployeeHours_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                SetActiveAnalyticsButton(btn);
            }

            await RunReportAsync(async db =>
            {
                var employees = await db.Employees.AsNoTracking().Include(emp => emp.TimeLogs).ToListAsync();
                var hours = _analyticsService.GetEmployeeTotalHours(employees);
                return (hours, $"Знайдено {hours.Count} співробітників з обліком часу.");
            });
        }

        private async void BtnDepartmentStats_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                SetActiveAnalyticsButton(btn);
            }

            await RunReportAsync(async db =>
            {
                var logs = await db.TimeLogs.AsNoTracking()
                    .Include(t => t.Employee)
                    .ThenInclude(emp => emp.Department)
                    .ToListAsync();
                var stats = _analyticsService.GetDepartmentStats(logs);
                return (stats, $"Знайдено {stats.Count} департаментів.");
            });
        }

        private async Task RunReportAsync(Func<AppDbContext, Task<(IList Items, string Hint)>> buildReport)
        {
            try
            {
                await using var db = CreateDbContext();
                await db.Database.EnsureCreatedAsync();

                if (!await db.TimeLogs.AnyAsync())
                {
                    GridAnalytics.ItemsSource = null;
                    TxtAnalyticsHint.Text = "База порожня: спочатку завантажте дані.";
                    return;
                }

                var (items, hint) = await buildReport(db);
                GridAnalytics.ItemsSource = items;

                if (items.Count == 0)
                {
                    TxtAnalyticsHint.Text = hint + " Записи відсутні.";
                }
                else
                {
                    TxtAnalyticsHint.Text = hint;
                }
            }
            catch (Exception ex)
            {
                TxtAnalyticsHint.Text = "Не вдалося сформувати звіт.";
                MessageBox.Show(this, $"Помилка під час побудови звіту: {ex.Message}", "Помилка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GridAnalytics_AutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            e.Column.Header = ColumnHeaders.TryGetValue(e.PropertyName, out var header) ? header: e.PropertyName;

            if (e.Column is DataGridTextColumn textColumn)
            {
                var binding = textColumn.Binding as Binding;
                if (binding != null)
                {
                    if (e.PropertyType == typeof(DateTime))
                    {
                        binding.StringFormat = "dd.MM.yyyy";
                    }
                    else if (e.PropertyType == typeof(double))
                    {
                        binding.StringFormat = "0.0#";
                    }
                }
            }
        }
    }
}