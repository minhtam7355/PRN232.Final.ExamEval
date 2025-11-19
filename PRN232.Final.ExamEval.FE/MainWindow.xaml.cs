using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using PRN232.Final.ExamEval.FE.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PRN232.Final.ExamEval.FE
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private const string ApiBaseUrl = "http://localhost:5000";
        private const string HubUrl = ApiBaseUrl + "/hubs/progress";

        private HubConnection _connection;
        private string _currentFolderId;
        private string _selectedFilePath;

        private readonly ObservableCollection<StudentRow> _students = new();
        private readonly ObservableCollection<string> _logs = new();


        public MainWindow()
        {
            InitializeComponent();

            dgStudents.ItemsSource = _students;
            lstLogs.ItemsSource = _logs;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await ConnectSignalRAsync();
        }

        #region SignalR

        private async Task ConnectSignalRAsync()
        {
            UpdateConnectionStatus("Connecting...", "Orange");

            _connection = new HubConnectionBuilder()
                .WithUrl(HubUrl)
                .WithAutomaticReconnect()
                .Build();

            _connection.On<ProgressUpdate>("ProgressUpdate", progress =>
            {
                Dispatcher.Invoke(() => UpdateProgress(progress));
            });

            _connection.On<object>("QueueUpdate", info =>
            {
                Log("Queue updated");
            });

            _connection.On<object>("JobStarted", data =>
            {
                Dispatcher.Invoke(() =>
                {
                    txtQueueInfo.Visibility = Visibility.Collapsed;
                    Log("Job started");
                });
            });

            _connection.Reconnecting += error =>
            {
                Dispatcher.Invoke(() => UpdateConnectionStatus("Reconnecting...", "Orange"));
                Log("Reconnecting...");
                return Task.CompletedTask;
            };

            _connection.Reconnected += connectionId =>
            {
                Dispatcher.Invoke(() => UpdateConnectionStatus("Connected", "Green"));
                Log("Reconnected");
                if (!string.IsNullOrEmpty(_currentFolderId))
                {
                    _ = _connection.InvokeAsync("SubscribeToJob", _currentFolderId);
                }
                return Task.CompletedTask;
            };

            _connection.Closed += error =>
            {
                Dispatcher.Invoke(() => UpdateConnectionStatus("Disconnected", "Red"));
                Log("Connection closed");
                return Task.CompletedTask;
            };

            try
            {
                await _connection.StartAsync();
                UpdateConnectionStatus("Connected", "Green");
                Log("Connected to SignalR hub");
            }
            catch (Exception ex)
            {
                UpdateConnectionStatus("Disconnected", "Red");
                Log("Failed to connect SignalR: " + ex.Message);
            }
        }

        private void UpdateConnectionStatus(string text, string color)
        {
            txtConnectionStatus.Text = text;
            txtConnectionStatus.Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString(color);
        }

        #endregion

        #region Upload

        private void btnChooseFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Zip/Rar|*.zip;*.rar"
            };

            if (dlg.ShowDialog() == true)
            {
                _selectedFilePath = dlg.FileName;
                txtSelectedFile.Text = _selectedFilePath;
                btnUpload.IsEnabled = true;
                Log("Selected file: " + _selectedFilePath);
            }
        }

        private async void btnUpload_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedFilePath))
            {
                MessageBox.Show("Please choose a file first!");
                return;
            }

            btnUpload.IsEnabled = false;
            btnUpload.Content = "Uploading...";
            Log("Uploading file...");

            try
            {
                using var client = new HttpClient();

                var content = new MultipartFormDataContent();
                var fileStream = File.OpenRead(_selectedFilePath);
                var fileContent = new StreamContent(fileStream);

                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
                content.Add(fileContent, "file", System.IO.Path.GetFileName(_selectedFilePath));

                var response = await client.PostAsync($"{ApiBaseUrl}/api/submissions/run", content);
                var json = await response.Content.ReadAsStringAsync();

                Log("Upload response: " + json);

                var result = JsonConvert.DeserializeObject<RunResponse>(json);

                _currentFolderId = result.FolderId;

                if (result.Status == "queued")
                {
                    txtQueueInfo.Text = $"Job queued - Position: {result.QueuePosition}";
                    txtQueueInfo.Visibility = Visibility.Visible;
                }
                else
                {
                    txtQueueInfo.Visibility = Visibility.Collapsed;
                }

                // subscribe to job updates
                if (_connection?.State == HubConnectionState.Connected)
                {
                    await _connection.InvokeAsync("SubscribeToJob", _currentFolderId);
                    Log("Subscribed to folder: " + _currentFolderId);
                }

                btnUpload.Content = "Upload & Process";
            }
            catch (Exception ex)
            {
                Log("Upload failed: " + ex.Message);
                MessageBox.Show("Upload failed: " + ex.Message);
            }
            finally
            {
                btnUpload.IsEnabled = true;
            }
        }
        private void UpdateProgress(ProgressUpdate p)
        {
            txtTotal.Text = p.Total.ToString();
            txtCompleted.Text = p.Completed.ToString();
            txtFailed.Text = p.Failed.ToString();
            txtStatus.Text = p.Status.ToUpper();
            txtCurrentStudent.Text = p.CurrentStudent ?? "-";

            // progress bar
            pbOverall.Value = p.PercentComplete;
            txtProgressPercent.Text = $"{p.PercentComplete}%";

            // update table
            _students.Clear();
            if (p.Students != null)
            {
                foreach (var kvp in p.Students)
                {
                    var name = kvp.Key;
                    var info = kvp.Value;

                    _students.Add(new StudentRow
                    {
                        StudentId = name,

                        // STATUS: passed nhưng dính đạo văn
                        Status = info.PlagiarismDetected
                ? $"{info.Status} (Plagiarism)"
                : info.Status,

                        Started = info.StartedAt?.ToString("HH:mm:ss") ?? "-",
                        Completed = info.CompletedAt?.ToString("HH:mm:ss") ?? "-",

                        Violations = info.Violations != null && info.Violations.Count > 0
                    ? string.Join("; ", info.Violations)
                    : "-",

                        // PLAGIARISM INFO
                        PlagiarismInfo = info.PlagiarismDetected
                        ? $"YES - Max Similarity: {info.PlagiarismSimilarityMax}% | With: {string.Join(", ", info.SuspiciousGroupMembers)}"
                        : "NO",

                        // ISSUE DETAILS WHEN FAILED/WARNING
                        IssueDescription = info.Issues != null && info.Issues.Count > 0
                        ? string.Join("\n", info.Issues.Select(i => $"{i.Type}: {i.Description}"))
                        : "-"
                    });
                    ;
                }
            }

            // done → load summary
            if (p.Status == "done")
            {
                Log("Processing completed");
                _ = LoadSummaryAsync();
                _ = LoadFullReportAsync();
            }
            else if (p.Status == "failed")
            {
                Log("Processing failed");
            }
        }

        private async Task LoadFullReportAsync()
        {
            try
            {
                using var client = new HttpClient();

                var json = await client.GetStringAsync(
                    $"{ApiBaseUrl}/api/submissions/report/{_currentFolderId}");

                var result = JsonConvert.DeserializeObject<FullReport>(json);

                // update students table by full final data
                _students.Clear();

                foreach (var stu in result.Students)
                {
                    _students.Add(new StudentRow
                    {
                        StudentId = stu.StudentId,
                        Status = stu.PlagiarismDetected
                                ? $"{stu.Status} (Plagiarism)"
                                : stu.Status,

                        Started = stu.StartedAt?.ToString("HH:mm:ss") ?? "-",
                        Completed = stu.CompletedAt?.ToString("HH:mm:ss") ?? "-",

                        Violations = stu.IssueCount > 0
                                ? $"{stu.IssueCount} issues"
                                : "-",

                        PlagiarismInfo = stu.PlagiarismDetected
                                ? $"YES - Max {stu.PlagiarismSimilarityMax}% | With: {String.Join(", ", stu.SuspiciousGroupMembers)}"
                                : "NO",

                        IssueDescription = stu.Issues != null && stu.Issues.Count > 0
                                ? String.Join("\n", stu.Issues.Select(i => $"{i.Type}: {i.Description}"))
                                : "-"
                    });
                }
            }
            catch (Exception ex)
            {
                Log("Error loading final report: " + ex.Message);
            }
        }


        private async Task LoadSummaryAsync()
        {
            try
            {
                using var client = new HttpClient();
                Log("Fetching summary report...");

                var json = await client.GetStringAsync($"{ApiBaseUrl}/api/submissions/report/{_currentFolderId}");
                var result = JsonConvert.DeserializeObject<ReportResponse>(json);

                txtSummaryPassed.Text = result.Summary.Passed.ToString();
                txtSummaryWarning.Text = result.Summary.Warning.ToString();
                txtSummaryFailed.Text = result.Summary.Failed.ToString();
                txtSummaryRate.Text = result.Summary.SuccessRate + "%";

                Log("Summary loaded.");
            }
            catch (Exception ex)
            {
                Log("Error loading summary: " + ex.Message);
            }
        }
        private void Log(string message)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => Log(message));
                return;
            }

            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            _logs.Add($"[{timestamp}] {message}");

            // Auto-scroll
            if (lstLogs.Items.Count > 0)
                lstLogs.ScrollIntoView(lstLogs.Items[lstLogs.Items.Count - 1]);
        }
        #endregion

    }
}