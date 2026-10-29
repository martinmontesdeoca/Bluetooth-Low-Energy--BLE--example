using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using DoorDemo.Application;

namespace DoorDemo.Infrastructure
{
    public sealed class WindowsBridgeTransport : IDoorTransport
    {
        private readonly string executable;
        private readonly Guid service;
        private readonly Guid characteristic;
        private readonly BridgeMessageQueue messages = new BridgeMessageQueue();
        private Process process;
        private DataReceivedEventHandler outputHandler;
        private DataReceivedEventHandler errorHandler;
        private int generation;
        private BridgeStreamState streams;
        private bool processStarted;
        private bool terminalStatus;
        private bool exitReported;

        /// <summary>Validates helper configuration and prepares the Windows subprocess adapter.</summary>
        public WindowsBridgeTransport(string executable, string service, string characteristic)
        {
            this.executable = executable ?? throw new ArgumentNullException(nameof(executable));
            this.service = Guid.Parse(service);
            this.characteristic = Guid.Parse(characteristic);
        }

        public event Action<byte[]> ValueReceived;
        public event Action<ConnectionStatus> StatusChanged;

        /// <summary>Replaces the previous helper session and starts redirected native output readers.</summary>
        public void Connect()
        {
            StopProcess();
            terminalStatus = false;
            exitReported = false;
            if (!File.Exists(executable))
            {
                ReportError("Falta BleBridge.exe. Ejecuta Tools/Publish-Bridge.ps1 (ver README).");
                return;
            }

            try
            {
                process = new Process
                {
                    StartInfo = new ProcessStartInfo(executable,
                        "--service " + service + " --characteristic " + characteristic)
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        WorkingDirectory = Path.GetDirectoryName(executable)
                    }
                };
                var activeStreams = new BridgeStreamState(generation);
                streams = activeStreams;
                outputHandler = (_, args) => OnOutput(activeStreams, args);
                errorHandler = (_, args) => OnError(activeStreams, args);
                process.OutputDataReceived += outputHandler;
                process.ErrorDataReceived += errorHandler;
                if (!process.Start())
                    throw new InvalidOperationException("Windows no pudo iniciar el puente BLE.");
                processStarted = true;
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                StatusChanged?.Invoke(new ConnectionStatus(ConnectionState.Searching,
                    "Buscando el servicio de la puerta..."));
            }
            catch (Exception error)
            {
                StopProcess();
                ReportError("No se pudo iniciar BLE: " + error.Message);
            }
        }

        /// <summary>Delivers bounded queued output and reports process exit only after both streams finish.</summary>
        public void Pump()
        {
            DispatchPendingMessages();
            Process finished = process;
            // EOF follows the final queued message; drain once more after observing both streams closed.
            if (finished == null || !finished.HasExited || exitReported ||
                streams == null || !streams.IsComplete) return;
            DispatchPendingMessages();
            if (process != finished) return;
            exitReported = true;
            if (!terminalStatus)
                ReportError("El puente BLE termino inesperadamente (codigo " + finished.ExitCode + ").");
        }

        /// <summary>Delivers queued native messages on the Unity thread before evaluating helper termination.</summary>
        private void DispatchPendingMessages()
        {
            while (messages.TryDequeue(out BridgeMessage message))
            {
                try
                {
                    if (message.type == "value")
                        ValueReceived?.Invoke(message.ToPayload());
                    else
                    {
                        ConnectionStatus status = message.ToStatus();
                        terminalStatus |= status.State == ConnectionState.Error ||
                            status.State == ConnectionState.Disconnected;
                        StatusChanged?.Invoke(status);
                    }
                }
                catch (FormatException error)
                {
                    ReportError("Protocolo del puente: " + error.Message);
                }
            }

        }

        /// <summary>Accepts output only from its owning process generation and records standard-output completion.</summary>
        private void OnOutput(BridgeStreamState stream, DataReceivedEventArgs args)
        {
            int session = stream.Generation;
            if (session != Volatile.Read(ref generation)) return;
            if (args.Data == null) { stream.CompleteOutput(); return; }
            if (string.IsNullOrWhiteSpace(args.Data))
                return;
            try { messages.Enqueue(BridgeMessage.Parse(args.Data), session); }
            catch (Exception error) when (error is ArgumentException || error is FormatException)
            {
                messages.Enqueue(new BridgeMessage { type = "status", state = "error",
                    message = "Respuesta BLE invalida: " + error.Message }, session);
            }
        }

        /// <summary>Queues helper diagnostics and records standard-error completion for the owning session.</summary>
        private void OnError(BridgeStreamState stream, DataReceivedEventArgs args)
        {
            int session = stream.Generation;
            if (session != Volatile.Read(ref generation)) return;
            if (args.Data == null) { stream.CompleteError(); return; }
            if (!string.IsNullOrWhiteSpace(args.Data))
                messages.Enqueue(new BridgeMessage { type = "status", state = "error", message = args.Data }, session);
        }

        /// <summary>Publishes a terminal helper failure without inventing a successful connection.</summary>
        private void ReportError(string message)
        {
            terminalStatus = true;
            StatusChanged?.Invoke(new ConnectionStatus(ConnectionState.Error, message));
        }

        /// <summary>Stops the helper and publishes the completed disconnection.</summary>
        public void Disconnect()
        {
            StopProcess();
            StatusChanged?.Invoke(new ConnectionStatus(ConnectionState.Disconnected, "Desconectado"));
        }

        /// <summary>Invalidates old callbacks, requests graceful shutdown and terminates an unresponsive helper.</summary>
        private void StopProcess()
        {
            Volatile.Write(ref generation, messages.Reset());
            streams = null;
            if (process != null)
            {
                try
                {
                    if (processStarted && !process.HasExited)
                    {
                        process.StandardInput.Close();
                        if (!process.WaitForExit(700))
                        {
                            try { process.Kill(); }
                            catch (InvalidOperationException) when (process.HasExited)
                            {
                                // The helper exited between the wait and the kill request.
                            }
                        }
                        process.WaitForExit(700);
                    }
                }
                finally
                {
                    process.OutputDataReceived -= outputHandler;
                    process.ErrorDataReceived -= errorHandler;
                    process.Dispose();
                    process = null;
                    processStarted = false;
                }
            }
        }

        /// <summary>Releases the subprocess, its stream handlers and stale pending messages.</summary>
        public void Dispose() => StopProcess();
    }
}
