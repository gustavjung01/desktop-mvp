using System.Windows;
using System.Windows.Controls;
using CongTy.Desktop.Operations;

namespace CongTy.Desktop.Workforce;

public partial class WorkScheduleView : UserControl
{
    public WorkScheduleView(WorkScheduleViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = viewModel;
    }

    public WorkScheduleViewModel ViewModel { get; }

    private async void WorkScheduleView_OnLoaded(object sender, RoutedEventArgs e) =>
        await ViewModel.EnsureLoadedAsync();

    private async void Refresh_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.RefreshAsync();

    private async void ViewSchedules_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.ReloadSchedulesAsync();

    private void CreateSchedule_OnClick(object sender, RoutedEventArgs e) =>
        ViewModel.OpenCreate();

    private void EditSchedule_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: WorkScheduleRowView row })
            ViewModel.OpenEdit(row);
    }

    private void CloseScheduleEditor_OnClick(object sender, RoutedEventArgs e) =>
        ViewModel.CloseEditor();

    private async void SaveSchedule_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.SaveScheduleAsync();

    private void NewShift_OnClick(object sender, RoutedEventArgs e) =>
        ViewModel.NewShift();

    private void EditShift_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ShiftTemplateRowView row })
            ViewModel.EditShift(row);
    }

    private async void SaveShift_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.SaveShiftAsync();

    private void NewWeek_OnClick(object sender, RoutedEventArgs e) =>
        ViewModel.NewWeek();

    private void EditWeek_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: WeekTemplateRowView row })
            ViewModel.EditWeek(row);
    }

    private async void SaveWeek_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.SaveWeekAsync();

    private void NewCalendar_OnClick(object sender, RoutedEventArgs e) =>
        ViewModel.NewCalendarDay();

    private void EditCalendar_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: CalendarDayRowView row })
            ViewModel.EditCalendarDay(row);
    }

    private async void SaveCalendar_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.SaveCalendarDayAsync();

    private void BulkApplyMode_OnClick(object sender, RoutedEventArgs e) =>
        ViewModel.SelectBulkMode("APPLY");

    private void BulkCopyMode_OnClick(object sender, RoutedEventArgs e) =>
        ViewModel.SelectBulkMode("COPY");

    private void ToggleAllBulk_OnClick(object sender, RoutedEventArgs e) =>
        ViewModel.ToggleAllBulkEmployees();

    private async void RunBulk_OnClick(object sender, RoutedEventArgs e) =>
        await ViewModel.RunBulkAsync();

    private async void ExportSchedules_OnClick(object sender, RoutedEventArgs e) =>
        await OfficeExportDialog.RunAsync(
            this,
            () => ((WorkScheduleViewModel)DataContext).ExportSchedulesAsync());
}
