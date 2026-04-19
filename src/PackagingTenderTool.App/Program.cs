namespace PackagingTenderTool.App;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, args) => AppExceptionReporter.Handle(args.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception exception)
            {
                AppExceptionReporter.Handle(exception);
            }
        };

        ApplicationConfiguration.Initialize();
        Application.Run(new StartForm());
    }
}
