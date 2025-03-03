using Magic.GeneralSystem.Toolkit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magic.GeneralSystem.Toolkit.Helpers.Dotnet
{
    public static class DotnetHelper
    {
        /// <summary>
        /// Runs a dotnet command, streams output in real-time, and returns the result.
        /// </summary>
        /// <param name="arguments">The dotnet CLI arguments (e.g., "tool list -g")</param>
        /// <param name="workingDirectory">Optional working directory for the command. If provided, executes within that path.</param>
        /// <returns>A MagicSystemResponse containing success status and full output.</returns>
        public static async Task<MagicSystemResponse> RunDotnetCommandAsync(string arguments, string? workingDirectory = null)
        {
            var response = new MagicSystemResponse();
            var outputBuilder = new StringBuilder();

            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                // Set working directory if provided
                if (!string.IsNullOrWhiteSpace(workingDirectory))
                {
                    if (Directory.Exists(workingDirectory))
                    {
                        processStartInfo.WorkingDirectory = workingDirectory;
                    }
                    else
                    {
                        return new MagicSystemResponse
                        {
                            Success = false,
                            Message = $"Error: The provided working directory '{workingDirectory}' does not exist."
                        };
                    }
                }

                using var process = new Process { StartInfo = processStartInfo };

                // Capture output and errors in real-time
                process.OutputDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrWhiteSpace(args.Data))
                    {
                        Console.WriteLine(args.Data);
                        outputBuilder.AppendLine(args.Data);
                    }
                };

                process.ErrorDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrWhiteSpace(args.Data))
                    {
                        Console.WriteLine(args.Data);
                        outputBuilder.AppendLine(args.Data);
                    }
                };

                // Start process and begin capturing output
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Wait for process to exit
                await process.WaitForExitAsync();

                // Set success flag based on exit code
                response.Success = process.ExitCode == 0;
                response.Message = outputBuilder.ToString().Trim();
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error running dotnet command: {ex.Message}";
            }

            return response;
        }

        /// <summary>
        /// Installs a .NET tool if not already installed, and optionally updates it if it exists.
        /// </summary>
        /// <param name="toolName">The name of the tool to install.</param>
        /// <param name="updateIfInstalled">If true, updates the tool if it is already installed.</param>
        /// <param name="installGlobally">If true, installs the tool globally.</param>
        public static async Task<MagicSystemResponse> InstallOrUpdateToolAsync(string toolName,
            bool updateIfInstalled = false, bool installGlobally = false)
        {
            return await InstallOrUpdateToolAsync(toolName, null, updateIfInstalled, installGlobally);
        }

        /// <summary>
        /// Installs a .NET tool if not already installed, and optionally updates it if it exists.
        /// </summary>
        /// <param name="toolName">The name of the tool to install.</param>
        /// <param name="workingDirectory">Optional working directory for the command. If provided, executes within that path.</param>
        /// <param name="updateIfInstalled">If true, updates the tool if it is already installed.</param>
        /// <param name="installGlobally">If true, installs the tool globally.</param>
        public static async Task<MagicSystemResponse> InstallOrUpdateToolAsync(string toolName, 
            string? workingDirectory,
            bool updateIfInstalled = false, bool installGlobally = false)
        {
            string globalFlag = installGlobally ? "--global" : "";

            // Check if the tool is installed
            var checkResponse = await RunDotnetCommandAsync($"tool list {globalFlag}");
            if (!checkResponse.Success)
            {
                return new MagicSystemResponse
                {
                    Success = false,
                    Message = $"Failed to check installed tools: {checkResponse.Message}"
                };
            }

            bool isInstalled = checkResponse.Message.Contains(toolName, StringComparison.OrdinalIgnoreCase);

            // If the tool is already installed and update is requested, update it
            if (isInstalled && updateIfInstalled)
            {
                var updateResponse = await RunDotnetCommandAsync($"tool update {globalFlag} {toolName}", workingDirectory);
                return new MagicSystemResponse
                {
                    Success = updateResponse.Success,
                    Message = updateResponse.Message
                };
            }
            // If not installed, install it
            else if (!isInstalled)
            {
                var installResponse = await RunDotnetCommandAsync($"tool install {globalFlag} {toolName}", workingDirectory);
                return new MagicSystemResponse
                {
                    Success = installResponse.Success,
                    Message = installResponse.Message
                };
            }

            return new MagicSystemResponse
            {
                Success = true,
                Message = $"{toolName} is already installed and no update was requested."
            };
        }
    }
}
