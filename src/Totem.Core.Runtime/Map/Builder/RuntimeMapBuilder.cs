namespace Totem.Map.Builder;

internal sealed class RuntimeMapBuilder
{
    internal RuntimeMapBuilder(IEnumerable<Type> types)
    {
        var totemAssemblies = new[] { typeof(IMessage).Assembly, Assembly.GetExecutingAssembly() };
        var totemTypes = totemAssemblies.SelectMany(x => x.GetExportedTypes());

        foreach(var type in types.Concat(totemTypes).Distinct())
        {
            if(type.IsClass
                && !type.IsAbstract
                && !TryAddReportRow(type)
                && !TryAddMessage(type)
                && !TryAddHandler(type))
            {
                TryAddTimeline(type);
            }
        }
    }

    internal ReportRowPhase ReportRows { get; } = new();
    internal MessagePhase Messages { get; } = new();
    internal HandlerPhase Handlers { get; } = new();
    internal TimelinePhase Timelines { get; } = new();

    internal RuntimeMap Build()
    {
        ReflectTypes();
        ValidateTypes();
        BuildTypes();

        return BuildMap();
    }

    bool TryAddReportRow(Type type)
    {
        if(typeof(IReportRow).IsAssignableFrom(type))
        {
            ReportRows.Rows.Add(new(type));
            return true;
        }

        return false;
    }

    bool TryAddMessage(Type type)
    {
        if(typeof(ICommand).IsAssignableFrom(type))
        {
            Messages.Commands.Add(new(type));
        }
        else if(typeof(IEvent).IsAssignableFrom(type))
        {
            Messages.Events.Add(new(type));
        }
        else if(typeof(INotification).IsAssignableFrom(type))
        {
            Messages.Notifications.Add(new(type));
        }
        else if(typeof(IReportQuery).IsAssignableFrom(type))
        {
            Messages.ReportQueries.Add(new(type));
        }
        else if(typeof(IReportListQuery).IsAssignableFrom(type))
        {
            Messages.ReportListQueries.Add(new(type));
        }
        else if(typeof(ISubscription).IsAssignableFrom(type))
        {
            Messages.Subscriptions.Add(new(type));
        }
        else
        {
            return false;
        }

        return true;
    }

    bool TryAddHandler(Type type)
    {
        if(type.ImplementsGenericInterface(typeof(IEventHandler<>)))
        {
            Handlers.EventHandlers.Add(new(type));
        }
        else if(type.ImplementsGenericInterface(typeof(INotificationHandler<>)))
        {
            Handlers.NotificationHandlers.Add(new(type));
        }
        else if(type.ImplementsGenericInterface(typeof(ISubscriptionHandler<>)))
        {
            Handlers.SubscriptionHandlers.Add(new(type));
        }
        else
        {
            return false;
        }

        return true;
    }

    void TryAddTimeline(Type type)
    {
        if(typeof(IReport).IsAssignableFrom(type))
        {
            Timelines.Reports.Add(new(type));
        }
        else if(typeof(ITopic).IsAssignableFrom(type))
        {
            Timelines.Topics.Add(new(type));
        }
        else
        {
            if(typeof(IWorkflow).IsAssignableFrom(type))
            {
                Timelines.Workflows.Add(new(type));
            }
        }
    }

    void ReflectTypes()
    {
        ReportRows.Reflect(this);
        Messages.Reflect(this);
        Handlers.Reflect(this);
        Timelines.Reflect(this);
    }

    void ValidateTypes()
    {
        ReportRows.Validate();
        Messages.Validate();
        Handlers.Validate();
        Timelines.Validate();
    }

    void BuildTypes()
    {
        ReportRows.Build();
        Messages.Build();
        Handlers.Build();
        Timelines.Build();
    }

    RuntimeMap BuildMap() =>
        new(Messages.Commands.Value,
            Messages.Events.Value,
            Handlers.EventHandlers.Value,
            Messages.Notifications.Value,
            Handlers.NotificationHandlers.Value,
            Timelines.Reports.Value,
            ReportRows.Rows.Value,
            Messages.ReportQueries.Value,
            Messages.ReportListQueries.Value,
            Messages.Subscriptions.Value,
            Handlers.SubscriptionHandlers.Value,
            Timelines.Topics.Value,
            Timelines.Workflows.Value,
            ReportRows.ConcatErrors(Messages, Handlers, Timelines).ToList());

    internal abstract class BuilderPhase : IErrorCollector
    {
        internal abstract void Reflect(RuntimeMapBuilder map);
        internal abstract void Validate();
        internal abstract void Build();
        public abstract IEnumerable<RuntimeMapError> CollectErrors();
    }

    internal sealed class ReportRowPhase : BuilderPhase
    {
        internal RuntimeTypeCollectionBuilder<ReportRowBuilder, ReportRowType> Rows = new();

        internal override void Reflect(RuntimeMapBuilder map) =>
            Rows.Reflect(map);

        internal override void Validate() =>
            Rows.Validate();

        internal override void Build() =>
            Rows.Build();

        public override IEnumerable<RuntimeMapError> CollectErrors() =>
            Rows.CollectErrors();
    }

    internal sealed class MessagePhase : BuilderPhase
    {
        internal RuntimeTypeCollectionBuilder<CommandBuilder, CommandType> Commands = new();
        internal RuntimeTypeCollectionBuilder<EventBuilder, EventType> Events = new();
        internal RuntimeTypeCollectionBuilder<NotificationBuilder, NotificationType> Notifications = new();
        internal RuntimeTypeCollectionBuilder<ReportQueryBuilder, ReportQueryType> ReportQueries = new();
        internal RuntimeTypeCollectionBuilder<ReportListQueryBuilder, ReportListQueryType> ReportListQueries = new();
        internal RuntimeTypeCollectionBuilder<SubscriptionBuilder, SubscriptionType> Subscriptions = new();

        internal override void Reflect(RuntimeMapBuilder map)
        {
            Commands.Reflect(map);
            Events.Reflect(map);
            Notifications.Reflect(map);
            ReportQueries.Reflect(map);
            ReportListQueries.Reflect(map);
            Subscriptions.Reflect(map);
        }

        internal override void Validate()
        {
            Commands.Validate();
            Events.Validate();
            Notifications.Validate();
            ReportQueries.Validate();
            ReportListQueries.Validate();
            Subscriptions.Validate();
        }

        internal override void Build()
        {
            Commands.Build();
            Events.Build();
            Notifications.Build();
            ReportQueries.Build();
            ReportListQueries.Build();
            Subscriptions.Build();
        }

        public override IEnumerable<RuntimeMapError> CollectErrors() =>
            Events.ConcatErrors(Notifications, ReportQueries, ReportListQueries, Subscriptions);
    }

    internal sealed class HandlerPhase : BuilderPhase
    {
        internal RuntimeTypeCollectionBuilder<EventHandlerBuilder, EventHandlerType> EventHandlers = new();
        internal RuntimeTypeCollectionBuilder<NotificationHandlerBuilder, NotificationHandlerType> NotificationHandlers = new();
        internal RuntimeTypeCollectionBuilder<SubscriptionHandlerBuilder, SubscriptionHandlerType> SubscriptionHandlers = new();

        internal override void Reflect(RuntimeMapBuilder map)
        {
            EventHandlers.Reflect(map);
            NotificationHandlers.Reflect(map);
            SubscriptionHandlers.Reflect(map);
        }

        internal override void Validate()
        {
            EventHandlers.Validate();
            NotificationHandlers.Validate();
            SubscriptionHandlers.Validate();
        }

        internal override void Build()
        {
            EventHandlers.Build();
            NotificationHandlers.Build();
            SubscriptionHandlers.Build();
        }

        public override IEnumerable<RuntimeMapError> CollectErrors() =>
            EventHandlers.ConcatErrors(NotificationHandlers, SubscriptionHandlers);
    }

    internal sealed class TimelinePhase : BuilderPhase
    {
        internal RuntimeTypeCollectionBuilder<ReportBuilder, ReportType> Reports = new();
        internal RuntimeTypeCollectionBuilder<TopicBuilder, TopicType> Topics = new();
        internal RuntimeTypeCollectionBuilder<WorkflowBuilder, WorkflowType> Workflows = new();

        internal override void Reflect(RuntimeMapBuilder map)
        {
            Reports.Reflect(map);
            Topics.Reflect(map);
            Workflows.Reflect(map);
        }

        internal override void Validate()
        {
            Reports.Validate();
            Topics.Validate();
            Workflows.Validate();
        }

        internal override void Build()
        {
            Reports.Build();
            Topics.Build();
            Workflows.Build();
        }

        public override IEnumerable<RuntimeMapError> CollectErrors() =>
            Reports.ConcatErrors(Topics, Workflows);
    }
}
