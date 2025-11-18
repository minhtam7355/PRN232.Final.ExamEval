using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SubmitionsChecker
{
    /// <summary>
    /// Minimal MOSS protocol client. Sends files to a MOSS server and returns result URL.
    /// NOTE: The caller can supply a MOSS user id or rely on configured default.
    /// This module is temporarily disabled because a MOSS user id is not configured.
    /// Re-enable by removing the surrounding #if false / #endif.
    /// </summary>
    public class MossClient
    {
        private readonly string _host;
        private readonly int _port;
        private readonly MossOptions _options;
        private readonly ILogger<MossClient>? _logger;

        public MossClient(IOptions<MossOptions> options, ILogger<MossClient>? logger = null)
        {
            _options = options?.Value ?? new MossOptions();
            _host = _options.Host ?? "moss.stanford.edu";
            _port = _options.Port;
            _logger = logger;
        }

        /// <summary>
        /// Send files to moss. files is a sequence of (filepath, content) pairs. language is MOSS language tag (e.g. "c#" or "text").
        /// If userId is null, will use configured default from options.
        /// Returns URL string on success or null on failure.
        /// </summary>
        public async Task<string?> RunMossAsync(IEnumerable<(string FilePath, string Content)> files, string? userId = null, string? language = null, CancellationToken ct = default)
        {
            var effectiveUser = userId ?? _options.DefaultUserId;
            if (string.IsNullOrWhiteSpace(effectiveUser))
                throw new ArgumentException("MOSS user id is required either as parameter or in configuration", nameof(userId));

            var lang = language ?? _options.Language ?? "text";

            using var client = new TcpClient();
            try
            {
                _logger?.LogInformation("Connecting to MOSS server {Host}:{Port}", _host, _port);
                await client.ConnectAsync(_host, _port, ct);

                using var network = client.GetStream();
                using var writer = new StreamWriter(network, new UTF8Encoding(false)) { NewLine = "\n", AutoFlush = true };
                using var reader = new StreamReader(network, Encoding.UTF8);

                // Send initial commands
                await writer.WriteLineAsync($"moss {effectiveUser}");
                await writer.WriteLineAsync($"language {lang}");

                // Set options
                if (_options.MaxMatches.HasValue)
                    await writer.WriteLineAsync($"maxmatches {_options.MaxMatches.Value}");
                else
                    await writer.WriteLineAsync("maxmatches 250");

                foreach (var (filePath, content) in files)
                {
                    ct.ThrowIfCancellationRequested();

                    var bytes = Encoding.UTF8.GetBytes(content);
                    var size = bytes.Length;

                    // The file command: file <language> <size> <path>
                    await writer.WriteLineAsync($"file {lang} {size} {filePath}");

                    // Write raw bytes of content. Use network stream directly to ensure byte counts.
                    await network.WriteAsync(bytes, 0, bytes.Length, ct);
                    // Write a newline after content as required by protocol
                    var nl = Encoding.UTF8.GetBytes("\n");
                    await network.WriteAsync(nl, 0, nl.Length, ct);
                    await network.FlushAsync(ct);

                    _logger?.LogDebug("Sent file {File} ({Size} bytes)", filePath, size);

                    // small pause
                    await Task.Delay(10, ct);
                }

                // Tell server we're done and request a query
                await writer.WriteLineAsync("query 0");

                // Read response lines until we get a URL or EOF
                string? line;
                string? lastUrl = null;
                var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCts.CancelAfter(TimeSpan.FromMinutes(_options.TimeoutMinutes));

                while (!timeoutCts.IsCancellationRequested && (line = await reader.ReadLineAsync()) != null)
                {
                    _logger?.LogInformation("MOSS: {Line}", line);
                    if (line.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        lastUrl = line.Trim();
                        break;
                    }
                }

                return lastUrl;
            }
            catch (OperationCanceledException)
            {
                _logger?.LogWarning("MOSS operation cancelled");
                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "MOSS client failed");
                return null;
            }
        }
    }
}
