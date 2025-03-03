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
        public static async Task<List<MagicSystemResponse>> ValidateAndInstallPackages(string workingPath, string[] packageNames)
        {
            Console.Clear();

            MagicSystemResponse packageListString = await DotnetHelper.GetPackageListString(workingPath);

            if (packageListString == null)
                throw new Exception("packageListString came in null!");

            if (packageListString.Success == false)
            {
                Console.WriteLine($"Something went wrong trying to detect the packages downloaded. " +
                    $"Sending you back shortly.. Message provided was: {packageListString?.Message??"NO MESSAGE PROVIDED"}");
                await Task.Delay(3500);
            }

            List<(string packageName, bool requiresInstall)> installPackages = new List<(string packageName, bool requiresInstall)>();

            foreach (var pName in packageNames)
            {
                installPackages.Add(DoesPackageRequireInstall(pName, packageListString));
            }

            Console.Clear();

            List<MagicSystemResponse> responses = new List<MagicSystemResponse>();
            foreach (var package in installPackages)
            {
                if (package.requiresInstall == true)
                {
                    responses.Add(await DotnetHelper.InstallOrUpdatePackage(package.packageName, workingPath));
                }
                else
                {
                    Console.WriteLine($"The following package is already installed: {package.packageName}");
                }
            }

            return responses;
        }

        private static (string packageName, bool requiresInstall) DoesPackageRequireInstall(string packageName, MagicSystemResponse packageListString)
        {
            if (string.IsNullOrWhiteSpace(packageListString.Message))
            {
                return (packageName, true);
            }
            var response = DotnetHelper.DoesPackageExist(packageName,
                packageListString);

            if (response.Success == true && response.Result == false)
            {
                return (packageName, true);
            }
            else
            {
                return (packageName, false);
            }
        }

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

        public static async Task<MagicSystemResponse<bool>> DoesPackageExist(string packageName, string workingDirectory)
        {
            // Validate input early
            if (string.IsNullOrWhiteSpace(packageName) || string.IsNullOrWhiteSpace(workingDirectory) || !Directory.Exists(workingDirectory))
            {
                return new MagicSystemResponse<bool>
                {
                    Result = false,
                    Message = "Invalid package name or working directory does not exist.",
                    Success = false
                };
            }

            // Trim the package name to ensure clean comparison
            packageName = packageName.Trim();

            // Run the dotnet list package command
            var response = await GetPackageListString(workingDirectory);

            return DoesPackageExist(packageName, response);
        }

        public static MagicSystemResponse<bool> DoesPackageExist(string packageName, 
            MagicSystemResponse getPackageListStringResponse)
        {
            // Validate input early
            if (string.IsNullOrWhiteSpace(packageName) || getPackageListStringResponse == null)
            {
                return new MagicSystemResponse<bool>
                {
                    Result = false,
                    Message = "Invalid package name or getPackageListStringResponse does not exist.",
                    Success = false
                };
            }

            // Trim the package name to ensure clean comparison
            packageName = packageName.Trim();

            // Run the dotnet list package command
            var response = getPackageListStringResponse;

            // If the command execution failed, return failure response
            if (!response.Success)
            {
                return new MagicSystemResponse<bool>
                {
                    Result = false,
                    Message = response.Message,
                    Success = false
                };
            }

            // Check if the package exists using exact line starts or surrounded by spaces
            bool packageExists = response.Message.Split('\n')
                .Select(line => line.Trim()) // Trim each line
                .Any(line =>
                    line.StartsWith(packageName + " ", StringComparison.OrdinalIgnoreCase) || // Check if package starts the line
                    line.Contains(" " + packageName + " ", StringComparison.OrdinalIgnoreCase) // Check if package is surrounded by spaces
                );

            return new MagicSystemResponse<bool>
            {
                Result = packageExists,
                Message = response.Message,
                Success = true
            };
        }

        public static async Task<MagicSystemResponse> GetPackageListString(string workingDirectory)
        {
            return await RunDotnetCommandAsync("list package", workingDirectory); ;
        }

        public static async Task<MagicSystemResponse> InstallOrUpdatePackage(string packageName, string workingDirectory)
        {
            return await RunDotnetCommandAsync($"add package {packageName.Trim()}", workingDirectory); ;
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
