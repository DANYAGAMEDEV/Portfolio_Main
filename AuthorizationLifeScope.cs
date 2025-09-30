namespace Code.DI {
    using Code.Gameplay.Authorization;
    using Code.Infrastructure.Services.Auth;
    using Colyseus;
    using UnityEngine;
    using VContainer;

    public class AuthorizationLifeScope : LifetimeScope {
        [SerializeField]
        private ColyseusSettings colyseusSettings;

        protected override void Configure(IContainerBuilder builder) {
            builder.RegisterInstance(this.colyseusSettings);

            HttpClientSubModule.Register(builder);
            ClientSubModule.Register(builder);
            AuthServiceSubModule.Register(builder);

            builder.Register<ScreenRouter>(Lifetime.Singleton);

            builder.Register<NavigationProcessor>(Lifetime.Singleton)
                   .AsImplementedInterfaces();
            builder.Register<AuthorizationEntryPoint>(Lifetime.Singleton)
                   .AsImplementedInterfaces();
        }
    }
}