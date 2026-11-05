using System.Diagnostics;

namespace Api.Agente.Application.Pipeline;

public static class EcdPipelineTelemetry
{
    public const string ActivitySourceName = "Api.Agente.Pipeline";

    public const string RunActivityName = "ecd.validation.run";

    public const string ExecutionIdTag = "ecd.execution_id";

    public const string PhaseTag = "ecd.phase";

    public static readonly ActivitySource Source = new(ActivitySourceName, "1.0.0");

    public static Activity? StartRun(Guid executionId)
    {
        var activity = Source.StartActivity(RunActivityName, ActivityKind.Internal);
        activity?.SetTag(ExecutionIdTag, executionId.ToString());
        return activity;
    }

    public static Activity? StartPhase(string phase, Guid executionId)
    {
        var activity = Source.StartActivity($"ecd.phase.{phase}", ActivityKind.Internal);
        activity?.SetTag(ExecutionIdTag, executionId.ToString());
        activity?.SetTag(PhaseTag, phase);
        return activity;
    }

    public static void RecordFailure(Activity? activity, Exception exception)
    {
        if (activity is null)
        {
            return;
        }

        if (exception is OperationCanceledException)
        {
            activity.SetTag("ecd.cancelled", true);
            activity.SetStatus(ActivityStatusCode.Error, "Cancelled");
            return;
        }

        activity.AddException(exception);
        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
    }
}
