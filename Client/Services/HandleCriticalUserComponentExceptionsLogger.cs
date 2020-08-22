﻿namespace BlazorRepl.Client.Services
{
    using System;
    using Microsoft.Extensions.Logging;
    using Microsoft.JSInterop;

    // This is a workaround for the currently missing global exception handling mechanism in Blazor. If the user's code generates
    // an assembly that throws TypeLoadException on load, we need to override the stored assembly in browser's cache storage
    // so the app works on reload. (Approach: https://github.com/dotnet/aspnetcore/issues/13452#issuecomment-632660280)
    public class HandleCriticalUserComponentExceptionsLogger : ILogger
    {
        private readonly IJSRuntime jsRuntime;

        public HandleCriticalUserComponentExceptionsLogger(IJSRuntime jsRuntime)
        {
            this.jsRuntime = jsRuntime;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter)
        {
            if (exception is TypeLoadException tle && tle.TypeName.StartsWith("BlazorRepl.UserComponents"))
            {
                this.jsRuntime.InvokeVoidAsync(
                    "App.Repl.updateUserAssemblyInCacheStorage",
                    Convert.FromBase64String(Constants.DefaultUserComponentsAssemblyBytes));
            }
        }

        public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Critical;

        public IDisposable BeginScope<TState>(TState state) => NoOpDisposable.Instance;

        private class NoOpDisposable : IDisposable
        {
            public static readonly NoOpDisposable Instance = new NoOpDisposable();

            public void Dispose()
            {
            }
        }
    }
}
