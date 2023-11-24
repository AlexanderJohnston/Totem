namespace Totem.Map.Builder;

internal abstract class ObserverBuilder<TObserver> : RuntimeTypeBuilder<TObserver>
    where TObserver : ObserverType
{
    protected ObserverBuilder(Type declaredType) : base(declaredType)
    { }

    internal bool IsSingleInstance { get; private set; }
    internal List<ObserverRouteMethodBuilder> Routes { get; }  = new();
    internal List<ObserverGivenMethodBuilder> Givens { get; } = new();
    internal List<ObserverWhenMethodBuilder> Whens { get; } = new();
    internal List<ObservationBuilder> Observations { get; } = new();

    protected abstract Type ExtendedContextType { get; }

    internal override void Reflect(RuntimeMapBuilder map)
    {
        foreach(var method in DeclaredType.GetPossibleTimelineMethods())
        {
            var route = new ObserverRouteMethodBuilder(method);

            route.Reflect(map);

            if(!route.Ignored)
            {
                Routes.Add(route);

                if(route.HasValue)
                {
                    continue;
                }
            }

            var given = new ObserverGivenMethodBuilder(method);

            given.Reflect(map);

            if(!given.Ignored)
            {
                Givens.Add(given);

                if(given.HasValue)
                {
                    continue;
                }
            }

            var when = new ObserverWhenMethodBuilder(method, ExtendedContextType);

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
        var givenValues = Givens.Where(x => x.HasValue).ToList();
        var whenValues = Whens.Where(x => x.HasValue).ToList();

        var events = routeValues.Select(x => x.Parameter.Event)
            .Union(givenValues.Select(x => x.Parameter.Event))
            .Union(whenValues.Select(x => x.Parameter.Event));

        var observations = (
            from e in events
            join route in routeValues on e equals route.Parameter.Event into eventRoutes
            join given in givenValues on e equals given.Parameter.Event into eventGivens
            join when in whenValues on e equals when.Parameter.Event into eventWhens
            select (e, eventRoutes.SingleOrDefault(), eventGivens.SingleOrDefault(), eventWhens.SingleOrDefault()))
            .ToList();

        if(observations.Count == 0)
        {
            SetError(BuildErrors.ObserverEventsNotFound);
            return;
        }

        foreach(var (e, route, given, when) in observations)
        {
            if(!IsSingleInstance && route is null)
            {
                SetError(BuildErrors.RuntimeRouteNotFound, new { Event = e });
                return;
            }

            if(given is null && when is null)
            {
                SetError(BuildErrors.ObserverGivenOrWhenNotFound, new { Event = e });
                return;
            }

            if(route.HasError)
            {
                SetError(BuildErrors.RuntimeRouteHasError, new { Event = e });
                return;
            }

            if(when.HasError)
            {
                SetError(BuildErrors.RuntimeWhenHasError, new { Event = e });
                return;
            }

            Observations.Add(new(e, route, given, when));
        }
    }

    protected override IEnumerable<RuntimeMapError> CollectMethodErrors() =>
        ErrorCollector.Collect(Routes, Givens, Whens);
}
