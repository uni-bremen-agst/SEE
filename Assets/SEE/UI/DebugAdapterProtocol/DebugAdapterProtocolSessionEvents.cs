using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using SEE.UI.Window.CodeWindow;
using SEE.UI.Window.ConsoleWindow;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Debug = UnityEngine.Debug;

namespace SEE.UI.DebugAdapterProtocol
{
    /// <summary>
    /// This part of the <see cref="DebugAdapterProtocolSession"/> class handles sending and receiving events to and from the debug adapter.
    /// </summary>
    public partial class DebugAdapterProtocolSession
    {
        /// <summary>
        /// Updates the breakpoints.
        /// </summary>
        /// <param name="path">The source code path.</param>
        /// <param name="line">The code line.</param>
        private void OnBreakpointsChanged(string path, int line)
        {
            actions.Enqueue(() =>
            {
                adapterHost.SendRequest(new SetBreakpointsRequest
                {
                    Source = new Source { Path = path, Name = path },
                    Breakpoints = DebugBreakpointManager.Breakpoints[path].Values.ToList(),
                }, _ => { });
            });
        }

        /// <summary>
        /// Handles the beginning of hovering a word.
        /// </summary>
        /// <param name="codeWindow">The code window containing the hovered word.</param>
        /// <param name="wordInfo">The info of the hovered word.</param>
        private void OnWordHoverBegin(CodeWindow codeWindow, TMP_WordInfo wordInfo)
        {
            hoveredWord = wordInfo;
            actions.Enqueue(UpdateHoverTooltip);
        }

        /// <summary>
        /// Handles the end of hovering a word.
        /// </summary>
        /// <param name="codeWindow">The code window of the hovered word.</param>
        /// <param name="wordInfo">The info of the hovered word.</param>
        private void OnWordHoverEnd(CodeWindow codeWindow, TMP_WordInfo wordInfo)
        {
            hoveredWord = null;
            Tooltip.Deactivate();
        }

        /// <summary>
        /// Evaluates the hovered word.
        /// Only allowed on the main thread.
        /// </summary>
        private void UpdateHoverTooltip()
        {
            if (hoveredWord is null || IsRunning)
            {
                return;
            }

            string expression = hoveredWord.Value.GetWord();

            try
            {
                EvaluateResponse result = adapterHost.SendRequestSync(new EvaluateRequest
                {
                    Expression = expression,
                    Context = capabilities.SupportsEvaluateForHovers == true ? EvaluateArguments.ContextValue.Hover : null,
                    FrameId = StackFrame.Id
                });
                Tooltip.ActivateWith(result.Result);
            }
            catch (ProtocolException)
            {
                // Ignore exceptions on hover
                // Thrown when the hovered word isn't known
            }

        }

        /// <summary>
        /// Handles events of the debug adapter.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="theEvent">The event to be handled.</param>
        private void OnEventReceived(object sender, EventReceivedEventArgs theEvent)
        {
            switch (theEvent.Body)
            {
                case InitializedEvent initializedEvent:
                    OnInitializedEvent(initializedEvent);
                    break;
                case OutputEvent outputEvent:
                    OnOutputEvent(outputEvent);
                    break;
                case TerminatedEvent terminatedEvent:
                    OnTerminatedEvent(terminatedEvent);
                    break;
                case ExitedEvent exitedEvent:
                    OnExitedEvent(exitedEvent);
                    break;
                case StoppedEvent stoppedEvent:
                    OnStoppedEvent(stoppedEvent);
                    break;
                case ThreadEvent threadEvent:
                    OnThreadEvent(threadEvent);
                    break;
                case ContinuedEvent continuedEvent:
                    OnContinuedEvent(continuedEvent);
                    break;
                case CapabilitiesEvent capabilitiesEvent:
                    OnCapabilitiesEvent(capabilitiesEvent);
                    break;
            }
        }

        /// <summary>
        /// Handles the initialized event.
        /// </summary>
        /// <param name="initializedEvent">The event.</param>
        private void OnInitializedEvent(InitializedEvent initializedEvent)
        {
            actions.Enqueue(() =>
            {
                adapterHost.SendRequest(Adapter.GetLaunchRequest(), _ => IsRunning = true);
                foreach ((string path, Dictionary<int, SourceBreakpoint> breakpoints) in DebugBreakpointManager.Breakpoints)
                {
                    adapterHost.SendRequest(new SetBreakpointsRequest
                    {
                        Source = new Source { Path = path, Name = path },
                        Breakpoints = breakpoints.Values.ToList(),
                    }, _ => { });
                }
                adapterHost.SendRequest(new SetFunctionBreakpointsRequest { Breakpoints = new() }, _ => { });
                adapterHost.SendRequest(new SetExceptionBreakpointsRequest { Filters = new() }, _ => { });
                if (capabilities.SupportsConfigurationDoneRequest == true)
                {
                    adapterHost.SendRequest(new ConfigurationDoneRequest(), _ => { });
                }
            });
        }

        /// <summary>
        /// Handles output events.
        /// </summary>
        /// <param name="outputEvent">The event.</param>
        private void OnOutputEvent(OutputEvent outputEvent)
        {
            string channel = outputEvent.Category switch
            {
                OutputEvent.CategoryValue.Console => "Adapter",
                OutputEvent.CategoryValue.Stdout => "Program",
                OutputEvent.CategoryValue.Stderr => "Program",
                OutputEvent.CategoryValue.Telemetry => null,
                OutputEvent.CategoryValue.MessageBox => "Adapter",
                OutputEvent.CategoryValue.Exception => "Adapter",
                OutputEvent.CategoryValue.Important => "Adapter",
                OutputEvent.CategoryValue.Unknown => "Adapter",
                null => "Adapter",
                _ => "Adapter",
            };
            string level = outputEvent.Category switch
            {
                OutputEvent.CategoryValue.Console => "Log",
                OutputEvent.CategoryValue.Stdout => "Log",
                OutputEvent.CategoryValue.Stderr => "Error",
                OutputEvent.CategoryValue.Telemetry => null,
                OutputEvent.CategoryValue.MessageBox => "Warning",
                OutputEvent.CategoryValue.Exception => "Error",
                OutputEvent.CategoryValue.Important => "Warning",
                OutputEvent.CategoryValue.Unknown => "Log",
                null => "Log",
                _ => "Log",
            };
            if (channel is not null && level is not null)
            {
                if (level == "Error")
                {
                    Debug.LogWarning(outputEvent.Output);
                }
                ConsoleWindow.AddMessage(outputEvent.Output, channel, level);
            }
        }

        /// <summary>
        /// Handles the terminated event.
        /// </summary>
        /// <param name="terminatedEvent">The event.</param>
        private void OnTerminatedEvent(TerminatedEvent terminatedEvent)
        {
            ConsoleWindow.AddMessage("Terminated\n");
            actions.Enqueue(() => Destroyer.Destroy(this));
        }

        /// <summary>
        /// Handles the exited event.
        /// </summary>
        /// <param name="exitEvent">The event.</param>
        private void OnExitedEvent(ExitedEvent exitedEvent)
        {
            OpenConsole();
            ConsoleWindow.AddMessage($"Exited with exit code {exitedEvent.ExitCode}\n", "Program", "Log");
            actions.Enqueue(() => Destroyer.Destroy(this));
        }

        /// <summary>
        /// Handles stopped events.
        /// </summary>
        /// <param name="stoppedEvent">The event.</param>
        private void OnStoppedEvent(StoppedEvent stoppedEvent)
        {
            IsRunning = false;
            if (stoppedEvent.Reason == StoppedEvent.ReasonValue.Exception)
            {
                actions.Enqueue(() =>
                {
                    if (capabilities.SupportsExceptionInfoRequest != true)
                    {
                        return;
                    }

                    ExceptionInfoResponse exceptionInfo = adapterHost.SendRequestSync(new ExceptionInfoRequest
                    {
                        ThreadId = MainThread.Id,
                    });
                    string description = $"{exceptionInfo.ExceptionId}" + (exceptionInfo.Description != null ? $"\n{exceptionInfo.Description}" : "");
                    OpenConsole();
                    ConsoleWindow.AddMessage(description + "\n", "Program", "Error");
                });
            }
        }

        /// <summary>
        /// Handles thread events.
        /// </summary>
        /// <param name="threadEvent">The event.</param>
        private void OnThreadEvent(ThreadEvent threadEvent)
        {
            if (threadEvent.Reason == ThreadEvent.ReasonValue.Started)
            {
                threads.Add(new(threadEvent.ThreadId, threadEvent.ThreadId.ToString()));
            }
            else if (threadEvent.Reason == ThreadEvent.ReasonValue.Exited)
            {
                threads.RemoveAll(t => t.Id == threadEvent.ThreadId);
            }
        }

        /// <summary>
        /// Handles continued events.
        /// </summary>
        /// <param name="continuedEvent">The event.</param>
        private void OnContinuedEvent(ContinuedEvent continuedEvent)
        {
            IsRunning = true;
        }

        /// <summary>
        /// Updates the capabilities.
        /// </summary>
        /// <param name="capabilitiesEvent">The changed capabilities.</param>
        private void OnCapabilitiesEvent(CapabilitiesEvent capabilitiesEvent)
        {
            if (capabilities == null)
            {
                actions.Enqueue(UpdateCapabilities);
            }
            else
            {
                UpdateCapabilities();
            }

            void UpdateCapabilities()
            {
                capabilities.SupportsConfigurationDoneRequest = capabilitiesEvent.Capabilities.SupportsConfigurationDoneRequest ?? capabilities.SupportsConfigurationDoneRequest;
                capabilities.SupportsFunctionBreakpoints = capabilitiesEvent.Capabilities.SupportsFunctionBreakpoints ?? capabilities.SupportsFunctionBreakpoints;
                capabilities.SupportsConditionalBreakpoints = capabilitiesEvent.Capabilities.SupportsConditionalBreakpoints ?? capabilities.SupportsConditionalBreakpoints;
                capabilities.SupportsHitConditionalBreakpoints = capabilitiesEvent.Capabilities.SupportsHitConditionalBreakpoints ?? capabilities.SupportsHitConditionalBreakpoints;
                capabilities.SupportsEvaluateForHovers = capabilitiesEvent.Capabilities.SupportsEvaluateForHovers ?? capabilities.SupportsEvaluateForHovers;
                capabilities.ExceptionBreakpointFilters = capabilitiesEvent.Capabilities.ExceptionBreakpointFilters ?? capabilities.ExceptionBreakpointFilters;
                capabilities.SupportsStepBack = capabilitiesEvent.Capabilities.SupportsStepBack ?? capabilities.SupportsStepBack;
                capabilities.SupportsSetVariable = capabilitiesEvent.Capabilities.SupportsSetVariable ?? capabilities.SupportsSetVariable;
                capabilities.SupportsRestartFrame = capabilitiesEvent.Capabilities.SupportsRestartFrame ?? capabilities.SupportsRestartFrame;
                capabilities.SupportsGotoTargetsRequest = capabilitiesEvent.Capabilities.SupportsGotoTargetsRequest ?? capabilities.SupportsGotoTargetsRequest;
                capabilities.SupportsStepInTargetsRequest = capabilitiesEvent.Capabilities.SupportsStepInTargetsRequest ?? capabilities.SupportsStepInTargetsRequest;
                capabilities.SupportsCompletionsRequest = capabilitiesEvent.Capabilities.SupportsCompletionsRequest ?? capabilities.SupportsCompletionsRequest;
                capabilities.CompletionTriggerCharacters = capabilitiesEvent.Capabilities.CompletionTriggerCharacters ?? capabilities.CompletionTriggerCharacters;
                capabilities.SupportsModulesRequest = capabilitiesEvent.Capabilities.SupportsModulesRequest ?? capabilities.SupportsModulesRequest;
                capabilities.AdditionalModuleColumns = capabilitiesEvent.Capabilities.AdditionalModuleColumns ?? capabilities.AdditionalModuleColumns;
                capabilities.SupportedChecksumAlgorithms = capabilitiesEvent.Capabilities.SupportedChecksumAlgorithms ?? capabilities.SupportedChecksumAlgorithms;
                capabilities.SupportsRestartRequest = capabilitiesEvent.Capabilities.SupportsRestartRequest ?? capabilities.SupportsRestartRequest;
                capabilities.SupportsExceptionOptions = capabilitiesEvent.Capabilities.SupportsExceptionOptions ?? capabilities.SupportsExceptionOptions;
                capabilities.SupportsValueFormattingOptions = capabilitiesEvent.Capabilities.SupportsValueFormattingOptions ?? capabilities.SupportsValueFormattingOptions;
                capabilities.SupportsExceptionInfoRequest = capabilitiesEvent.Capabilities.SupportsExceptionInfoRequest ?? capabilities.SupportsExceptionInfoRequest;
                capabilities.SupportTerminateDebuggee = capabilitiesEvent.Capabilities.SupportTerminateDebuggee ?? capabilities.SupportTerminateDebuggee;
                capabilities.SupportSuspendDebuggee = capabilitiesEvent.Capabilities.SupportSuspendDebuggee ?? capabilities.SupportSuspendDebuggee;
                capabilities.SupportsDelayedStackTraceLoading = capabilitiesEvent.Capabilities.SupportsDelayedStackTraceLoading ?? capabilities.SupportsDelayedStackTraceLoading;
                capabilities.SupportsLoadedSourcesRequest = capabilitiesEvent.Capabilities.SupportsLoadedSourcesRequest ?? capabilities.SupportsLoadedSourcesRequest;
                capabilities.SupportsLogPoints = capabilitiesEvent.Capabilities.SupportsLogPoints ?? capabilities.SupportsLogPoints;
                capabilities.SupportsTerminateThreadsRequest = capabilitiesEvent.Capabilities.SupportsTerminateThreadsRequest ?? capabilities.SupportsTerminateThreadsRequest;
                capabilities.SupportsSetExpression = capabilitiesEvent.Capabilities.SupportsSetExpression ?? capabilities.SupportsSetExpression;
                capabilities.SupportsTerminateRequest = capabilitiesEvent.Capabilities.SupportsTerminateRequest ?? capabilities.SupportsTerminateRequest;
                capabilities.SupportsDataBreakpoints = capabilitiesEvent.Capabilities.SupportsDataBreakpoints ?? capabilities.SupportsDataBreakpoints;
                capabilities.SupportsReadMemoryRequest = capabilitiesEvent.Capabilities.SupportsReadMemoryRequest ?? capabilities.SupportsReadMemoryRequest;
                capabilities.SupportsWriteMemoryRequest = capabilitiesEvent.Capabilities.SupportsWriteMemoryRequest ?? capabilities.SupportsWriteMemoryRequest;
                capabilities.SupportsDisassembleRequest = capabilitiesEvent.Capabilities.SupportsDisassembleRequest ?? capabilities.SupportsDisassembleRequest;
                capabilities.SupportsCancelRequest = capabilitiesEvent.Capabilities.SupportsCancelRequest ?? capabilities.SupportsCancelRequest;
                capabilities.SupportsBreakpointLocationsRequest = capabilitiesEvent.Capabilities.SupportsBreakpointLocationsRequest ?? capabilities.SupportsBreakpointLocationsRequest;
                capabilities.SupportsClipboardContext = capabilitiesEvent.Capabilities.SupportsClipboardContext ?? capabilities.SupportsClipboardContext;
                capabilities.SupportsSteppingGranularity = capabilitiesEvent.Capabilities.SupportsSteppingGranularity ?? capabilities.SupportsSteppingGranularity;
                capabilities.SupportsInstructionBreakpoints = capabilitiesEvent.Capabilities.SupportsInstructionBreakpoints ?? capabilities.SupportsInstructionBreakpoints;
                capabilities.SupportsExceptionFilterOptions = capabilitiesEvent.Capabilities.SupportsExceptionFilterOptions ?? capabilities.SupportsExceptionFilterOptions;
                capabilities.SupportsSingleThreadExecutionRequests = capabilitiesEvent.Capabilities.SupportsSingleThreadExecutionRequests ?? capabilities.SupportsSingleThreadExecutionRequests;
                capabilities.SupportsResumableDisconnect = capabilitiesEvent.Capabilities.SupportsResumableDisconnect ?? capabilities.SupportsResumableDisconnect;
                capabilities.SupportsExceptionConditions = capabilitiesEvent.Capabilities.SupportsExceptionConditions ?? capabilities.SupportsExceptionConditions;
                capabilities.SupportsLoadSymbolsRequest = capabilitiesEvent.Capabilities.SupportsLoadSymbolsRequest ?? capabilities.SupportsLoadSymbolsRequest;
                capabilities.SupportsModuleSymbolSearchLog = capabilitiesEvent.Capabilities.SupportsModuleSymbolSearchLog ?? capabilities.SupportsModuleSymbolSearchLog;
                capabilities.SupportsDebuggerProperties = capabilitiesEvent.Capabilities.SupportsDebuggerProperties ?? capabilities.SupportsDebuggerProperties;
                capabilities.SupportsSetSymbolOptions = capabilitiesEvent.Capabilities.SupportsSetSymbolOptions ?? capabilities.SupportsSetSymbolOptions;
                capabilities.SupportsAuthenticatedSymbolServers = capabilitiesEvent.Capabilities.SupportsAuthenticatedSymbolServers ?? capabilities.SupportsAuthenticatedSymbolServers;
            }
        }

        /// <summary>
        /// Handles user input of the console.
        /// </summary>
        /// <param name="text">The text.</param>
        private void OnConsoleInput(string text)
        {
            actions.Enqueue(() =>
            {
                try
                {
                    EvaluateResponse result = adapterHost.SendRequestSync(new EvaluateRequest()
                    {
                        Expression = text,
                        Context = EvaluateArguments.ContextValue.Repl,
                        FrameId = IsRunning ? null : StackFrame.Id
                    });
                    ConsoleWindow.AddMessage(result.Result + "\n", "Program", "Log");
                }
                catch (Exception e)
                {
                    ConsoleWindow.AddMessage(e.Message, "Program", "Error");
                }

            });
        }

        /// <summary>
        /// Queues a continue request.
        /// </summary>
        private void OnContinue()
        {
            actions.Enqueue(() =>
            {
                if (IsRunning)
                {
                    return;
                }
                adapterHost.SendRequest(new ContinueRequest { ThreadId = MainThread.Id }, _ => { });
            });
        }

        /// <summary>
        /// Queues a pause request.
        /// </summary>
        private void OnPause()
        {
            actions.Enqueue(() =>
            {
                if (IsRunning)
                {
                    return;
                }
                adapterHost.SendRequest(new PauseRequest { ThreadId = MainThread.Id }, _ => { });
            });
        }

        /// <summary>
        /// Queues a reverse continue request.
        /// </summary>
        private void OnReverseContinue()
        {
            actions.Enqueue(() =>
            {
                if (IsRunning)
                {
                    return;
                }
                adapterHost.SendRequest(new ReverseContinueRequest { ThreadId = MainThread.Id }, _ => { });
            });
        }

        /// <summary>
        /// Queues a next request.
        /// </summary>
        private void OnNext()
        {
            actions.Enqueue(() =>
            {
                if (IsRunning)
                {
                    return;
                }
                adapterHost.SendRequest(new NextRequest { ThreadId = MainThread.Id, Granularity = steppingGranularity }, _ => { });
            });
        }

        /// <summary>
        /// Queues a step back request.
        /// </summary>
        private void OnStepBack()
        {
            actions.Enqueue(() =>
            {
                if (IsRunning)
                {
                    return;
                }
                adapterHost.SendRequest(new StepBackRequest { ThreadId = MainThread.Id, Granularity = steppingGranularity }, _ => { });
            });
        }

        /// <summary>
        /// Queues a step in request.
        /// </summary>
        private void OnStepIn()
        {
            actions.Enqueue(() =>
            {
                if (IsRunning)
                {
                    return;
                }
                adapterHost.SendRequest(new StepInRequest { ThreadId = MainThread.Id, Granularity = steppingGranularity }, _ => { });
            });
        }

        /// <summary>
        /// Queues a step out request.
        /// </summary>
        private void OnStepOut()
        {
            actions.Enqueue(() =>
            {
                if (IsRunning)
                {
                    return;
                }
                adapterHost.SendRequest(new StepOutRequest { ThreadId = MainThread.Id, Granularity = steppingGranularity }, _ => { });
            });
        }

        /// <summary>
        /// Queues a restart request.
        /// </summary>
        private void OnRestart()
        {
            actions.Enqueue(() =>
            {
                adapterHost.SendRequest(new RestartRequest { Arguments = Adapter.GetLaunchRequest() }, _ => IsRunning = true);
            });
        }

        /// <summary>
        /// Queues a terminate request.
        /// </summary>
        private void OnStop()
        {
            actions.Enqueue(() =>
            {
                if (capabilities.SupportsTerminateRequest == true)
                {
                    Terminate();
                }
                else
                {
                    Disconnect();
                }
            });
            return;

            // Tries to stop the debuggee gracefully.
            void Terminate()
            {
                adapterHost.SendRequest(new TerminateRequest(),
                    _ => QueueDestroy(),
                    (_, _) => actions.Enqueue(Disconnect));
            }
            // Forcefully shuts down the debuggee.
            void Disconnect()
            {
                adapterHost.SendRequest(new DisconnectRequest(),
                    _ => QueueDestroy(),
                    (_, _) => QueueDestroy()
                );
            }
            void QueueDestroy()
            {
                actions.Enqueue(() => Destroyer.Destroy(this));
            }
        }
    }
}
