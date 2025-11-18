# 🚀 Real-time Progress Tracking - Hướng dẫn tích hợp FE

## 📋 Tổng quan

Hệ thống đã được nâng cấp với **SignalR** để tracking real-time progress khi xử lý bài nộp của sinh viên.

### ✨ Tính năng mới:
- ✅ **Real-time updates** qua SignalR
- ✅ **Chi tiết từng sinh viên** đang xử lý
- ✅ **Progress percentage** chính xác
- ✅ **Violations tracking** theo thời gian thực
- ✅ **Fallback polling** nếu SignalR không khả dụng

---

## 🏗️ Architecture

```
┌─────────────┐         ┌─────────────┐         ┌──────────────┐
│   Frontend  │ ──────▶ │  SignalR    │ ◀────── │   Backend    │
│   (WPF/JS)  │         │     Hub     │         │  Processing  │
└─────────────┘         └─────────────┘         └──────────────┘
       │                                                │
       │                                                │
       └──────────── GET /api/submissions/progress ────┘
                    (Fallback nếu SignalR fail)
```

---

## 🔌 API Endpoints

### 1. **Upload và Start Processing**
```http
POST /api/submissions/run
Content-Type: multipart/form-data

file: [binary .zip/.rar file]
```

**Response:**
```json
{
  "folderId": "20251118103045_submissions",
  "message": "Upload accepted. Processing started.",
  "signalRHub": "/hubs/progress"
}
```

### 2. **Get Progress (Polling fallback)**
```http
GET /api/submissions/progress/{folderId}
```

**Response:**
```json
{
  "folderId": "20251118103045_submissions",
  "status": "processing",
  "startedAt": "2025-11-18T10:30:45Z",
  "total": 35,
  "completed": 15,
  "failed": 2,
  "percentComplete": 42,
  "currentStudent": "AnhNASE183208",
  "students": {
    "AnhNASE183208": {
      "studentName": "AnhNASE183208",
      "status": "processing",
      "startedAt": "2025-11-18T10:31:12Z",
      "error": null,
      "violations": []
    },
    "DuyPNSE173520": {
      "studentName": "DuyPNSE173520",
      "status": "completed",
      "startedAt": "2025-11-18T10:30:50Z",
      "completedAt": "2025-11-18T10:31:05Z",
      "error": null,
      "violations": ["Build failed: ..."]
    }
  }
}
```

### 3. **Get Final Report**
```http
GET /api/submissions/report/{folderId}
```

---

## 🔥 SignalR Integration

### SignalR Hub URL:
```
ws://localhost:5000/hubs/progress
```

### Event: `ProgressUpdate`

Được broadcast mỗi khi có update (mỗi khi 1 sinh viên bắt đầu/kết thúc xử lý).

---

## 💻 Code Examples

### **C# WPF Example (với Microsoft.AspNetCore.SignalR.Client)**

#### 1. Install package:
```bash
dotnet add package Microsoft.AspNetCore.SignalR.Client
```

#### 2. Code implementation:

```csharp
using Microsoft.AspNetCore.SignalR.Client;
using System.Text.Json;

public class SubmissionProgressTracker
{
    private HubConnection? _connection;
    private string _folderId;

    public event Action<JobProgress>? OnProgressUpdate;

    public async Task ConnectAsync(string baseUrl, string folderId)
    {
        _folderId = folderId;
        
        _connection = new HubConnectionBuilder()
            .WithUrl($"{baseUrl}/hubs/progress")
            .WithAutomaticReconnect()
            .Build();

        // Subscribe to progress updates
        _connection.On<JobProgress>("ProgressUpdate", (progress) =>
        {
            // Update UI on main thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                OnProgressUpdate?.Invoke(progress);
            });
        });

        await _connection.StartAsync();
        
        // Join the group for this specific folderId
        await _connection.InvokeAsync("SubscribeToJob", folderId);
    }

    public async Task DisconnectAsync()
    {
        if (_connection != null)
        {
            await _connection.InvokeAsync("UnsubscribeFromJob", _folderId);
            await _connection.StopAsync();
            await _connection.DisposeAsync();
        }
    }
}

// Usage in ViewModel/Window:
public class SubmissionViewModel
{
    private SubmissionProgressTracker _tracker = new();

    public async Task UploadAndTrack(string filePath)
    {
        // 1. Upload file
        var response = await UploadFileAsync(filePath);
        var folderId = response.FolderId;

        // 2. Connect to SignalR
        _tracker.OnProgressUpdate += UpdateUI;
        await _tracker.ConnectAsync("http://localhost:5000", folderId);
    }

    private void UpdateUI(JobProgress progress)
    {
        // Update progress bar
        ProgressValue = progress.PercentComplete;
        
        // Update status text
        StatusText = $"Processing {progress.Completed}/{progress.Total} students...";
        CurrentStudent = progress.CurrentStudent;

        // Update student list
        foreach (var student in progress.Students)
        {
            // Update DataGrid/ListView with student status
            UpdateStudentRow(student.Key, student.Value);
        }

        // Check if done
        if (progress.Status == "done")
        {
            StatusText = "✅ Processing completed!";
            _ = _tracker.DisconnectAsync();
            _ = LoadFinalReport(progress.FolderId);
        }
    }
}
```

---

### **JavaScript Example (cho Web UI)**

#### 1. Install SignalR:
```bash
npm install @microsoft/signalr
```

#### 2. Code implementation:

```javascript
import * as signalR from "@microsoft/signalr";

class SubmissionTracker {
  constructor(baseUrl) {
    this.baseUrl = baseUrl;
    this.connection = null;
  }

  async connect(folderId) {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${this.baseUrl}/hubs/progress`)
      .withAutomaticReconnect()
      .build();

    // Listen for progress updates
    this.connection.on("ProgressUpdate", (progress) => {
      this.onProgressUpdate(progress);
    });

    await this.connection.start();
    await this.connection.invoke("SubscribeToJob", folderId);
  }

  async disconnect(folderId) {
    if (this.connection) {
      await this.connection.invoke("UnsubscribeFromJob", folderId);
      await this.connection.stop();
    }
  }

  onProgressUpdate(progress) {
    // Update UI
    document.getElementById("progress-bar").value = progress.percentComplete;
    document.getElementById("status").innerText = 
      `Processing ${progress.completed}/${progress.total} students (${progress.percentComplete}%)`;
    
    if (progress.currentStudent) {
      document.getElementById("current-student").innerText = 
        `Current: ${progress.currentStudent}`;
    }

    // Update student list
    this.updateStudentList(progress.students);

    // Check completion
    if (progress.status === "done") {
      this.onComplete(progress.folderId);
    }
  }

  updateStudentList(students) {
    const tbody = document.querySelector("#student-table tbody");
    tbody.innerHTML = "";

    for (const [name, info] of Object.entries(students)) {
      const row = tbody.insertRow();
      row.innerHTML = `
        <td>${name}</td>
        <td><span class="badge ${this.getStatusClass(info.status)}">${info.status}</span></td>
        <td>${info.error || "-"}</td>
        <td>${info.violations.length}</td>
      `;
    }
  }

  getStatusClass(status) {
    switch (status) {
      case "completed": return "badge-success";
      case "processing": return "badge-primary";
      case "failed": return "badge-danger";
      default: return "badge-secondary";
    }
  }

  async onComplete(folderId) {
    console.log("Processing completed!");
    await this.disconnect(folderId);
    
    // Load final report
    const report = await fetch(`${this.baseUrl}/api/submissions/report/${folderId}`);
    // Display report...
  }
}

// Usage:
async function uploadAndTrack(file) {
  const formData = new FormData();
  formData.append("file", file);

  const response = await fetch("http://localhost:5000/api/submissions/run", {
    method: "POST",
    body: formData
  });

  const { folderId } = await response.json();

  const tracker = new SubmissionTracker("http://localhost:5000");
  await tracker.connect(folderId);
}
```

---

## 🎯 Progress Data Structure

```csharp
public class JobProgress
{
    public string FolderId { get; set; }
    public string Status { get; set; }              // "processing", "done", "failed"
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public int Total { get; set; }                  // Tổng số sinh viên
    public int Completed { get; set; }              // Đã xử lý xong
    public int Failed { get; set; }                 // Lỗi
    public int PercentComplete { get; set; }        // 0-100
    public string? CurrentStudent { get; set; }     // Đang xử lý
    public Dictionary<string, StudentProcessingInfo> Students { get; set; }
}

public class StudentProcessingInfo
{
    public string StudentName { get; set; }
    public string Status { get; set; }              // "pending", "processing", "completed", "failed"
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Error { get; set; }
    public List<string> Violations { get; set; }
}
```

---

## 🔄 Fallback Strategy (nếu SignalR fail)

```csharp
// Polling every 2 seconds as fallback
private async Task PollProgress(string folderId)
{
    while (true)
    {
        try
        {
            var progress = await GetProgressAsync(folderId);
            UpdateUI(progress);

            if (progress.Status != "processing")
                break;

            await Task.Delay(2000);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Polling error: {ex.Message}");
            await Task.Delay(5000);
        }
    }
}
```

---

## 📊 UI Design Suggestions

### Progress Bar Layout:
```
┌─────────────────────────────────────────────────────┐
│  Processing Submissions...                    42%   │
│  ████████████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░      │
│  Completed: 15/35  |  Failed: 2  |  Processing: 1  │
│  Current: AnhNASE183208                            │
└─────────────────────────────────────────────────────┘
```

### Student List Table:
```
┌──────────────────┬────────────┬─────────────┬────────────┐
│ Student ID       │ Status     │ Error       │ Violations │
├──────────────────┼────────────┼─────────────┼────────────┤
│ AnhNASE183208    │ ✅ Done    │ -           │ 0          │
│ DuyPNSE173520    │ ⚠️ Done    │ Build failed│ 1          │
│ HungDMSE173190   │ 🔄 Working │ -           │ 0          │
│ huydqse183078    │ ⏳ Pending │ -           │ 0          │
└──────────────────┴────────────┴─────────────┴────────────┘
```

---

## 🐛 Troubleshooting

### SignalR không kết nối được:
1. Check CORS settings trong `Program.cs`
2. Verify WebSocket support
3. Check firewall/antivirus

### Progress không update:
1. Verify folderId đúng
2. Check SignalR connection status
3. Fallback sang polling mode

---

## 📝 Summary

✅ **Real-time tracking** với SignalR  
✅ **Chi tiết từng sinh viên** với status riêng  
✅ **Đa luồng xử lý** 4 bài đồng thời  
✅ **Persistent storage** - có thể load lại sau khi server restart  
✅ **Graceful degradation** - fallback polling khi cần

Giờ FE có thể hiển thị progress bar real-time và user biết chính xác đang xử lý bài nào! 🎉

