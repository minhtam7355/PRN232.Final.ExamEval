using Microsoft.AspNetCore.SignalR;

namespace PRN232.Final.ExamEval.API.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time submission processing progress updates
    /// </summary>
    public class SubmissionProgressHub : Hub
    {
        public async Task SubscribeToJob(string folderId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, folderId);
        }

        public async Task UnsubscribeFromJob(string folderId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, folderId);
        }
    }
}

