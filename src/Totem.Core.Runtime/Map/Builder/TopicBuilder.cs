namespace Totem.Map.Builder;

internal sealed class TopicBuilder : RuntimeTypeBuilder<TopicType>
{
    internal TopicBuilder(Type declaredType) : base(declaredType)
    { }

    internal bool IsSingleInstance { get; private set; }
    internal List<TopicRouteMethodBuilder> Routes { get; } = new();
    internal RuntimeMethodCollectionBuilder<TopicGivenMethodBuilder, TopicGivenMethod> Givens { get; } = new();
    internal List<TopicWhenMethodBuilder> Whens { get; } = new();
    internal List<CommandBuilder> Commands { get; } = new();

    internal override void Reflect(RuntimeMapBuilder map)
    {
        foreach(var method in DeclaredType.GetPossibleTimelineMethods())
        {
            var route = new TopicRouteMethodBuilder(method);

            route.Reflect(map);

            if(!route.Ignored)
            {
                Routes.Add(route);

                if(route.HasValue)
                {
                    continue;
                }
            }

            var given = new TopicGivenMethodBuilder(method);

            given.Reflect(map);

            if(!given.Ignored)
            {
                Givens.Add(given);

                if(given.HasValue)
                {
                    continue;
                }
            }

            var when = new TopicWhenMethodBuilder(method);

            when.Reflect(map);

            if(!when.Ignored)
            {
                Whens.Add(when);
            }
        }

        if(Routes.Count == 0)
        {
            IsSingleInstance = true;
        }

        var routeValues = Routes.Where(x => x.HasValue).ToList();
        var whenValues = Whens.Where(x => x.HasValue).ToList();

        var commands = (
            from command in routeValues.Select(x => x.Parameter.Command).Union(whenValues.Select(x => x.Parameter.Command))
            join route in routeValues on command equals route.Parameter.Command into commandRoutes
            join when in whenValues on command equals when.Parameter.Command into commandWhens
            select (command, commandRoutes.FirstOrDefault(), commandWhens.FirstOrDefault()))
            .ToList();

        if(commands.Count == 0)
        {
            SetError(BuildErrors.TopicCommandsNotFound);
            return;
        }

        foreach(var (command, route, when) in commands)
        {
            if(!IsSingleInstance && route is null)
            {
                SetError(BuildErrors.RuntimeRouteNotFound, new { command });
                return;
            }

            if(when is null)
            {
                SetError(BuildErrors.TopicWhenNotFound, new { command });
                return;
            }

            if(route?.HasError == true)
            {
                SetError(BuildErrors.RuntimeRouteHasError, new { command });
                return;
            }

            if(when.HasError)
            {
                SetError(BuildErrors.RuntimeWhenHasError, new { command });
                return;
            }

            command.Topic = this;
            command.Route = route;
            command.When = when;

            Commands.Add(command);
        }
    }

    protected override TopicType BuildValue()
    {
        var topic = new TopicType(DeclaredType, IsSingleInstance, Givens.Build());
        var commands = new RuntimeTypeCollection<CommandType>();

        foreach(var command in Commands)
        {
            if(command.HasError)
            {
                continue;
            }

            command.Value.Topic = topic;

            command.BuildMethodValues();

            commands.Add(command.Value);
        }

        return topic;
    }

    protected override IEnumerable<RuntimeMapError> CollectMethodErrors() =>
        ErrorCollector.Collect(Routes, Givens, Whens);
}
