using Autofac;
using CKAN.Exporters;
using CKAN.GameVersionProviders;
using System;

namespace CKAN
{
    /// <summary>
    /// This class serves as the source of a static Autofac
    /// container which is automatically configured.
    ///
    /// TODO: Make the container initialization declarative
    /// and configurable using exposed builder methods.
    /// </summary>
    public static class Application
    {
        public static readonly IContainer Container;

        /// <summary>
        /// Initializes the container. If this operation fails,
        /// it will throw a nasty TypeInitializationException.
        /// </summary>
        static Application() {
            var builder = new ContainerBuilder();

            builder.RegisterType<GrasGameComparator>()
                .As<IGameComparator>();

            builder.RegisterType<Win32Registry>()
                .As<IWin32Registry>();

            builder.RegisterType<KspBuildMap>()
                .As<IKspBuildMap>()
                .SingleInstance(); // Since it stores cached data we want to keep it around

            builder.RegisterType<KspBuildIdVersionProvider>()
                .As<IGameVersionProvider>()
                .Keyed<IGameVersionProvider>(KspVersionSource.BuildId);

            builder.RegisterType<KspReadmeVersionProvider>()
                .As<IGameVersionProvider>()
                .Keyed<IGameVersionProvider>(KspVersionSource.Readme);

            builder.RegisterTypes(new Type[] {
                typeof(BbCodeExporter),
                typeof(DelimeterSeperatedValueExporter),
                typeof(MarkdownExporter),
                typeof(PlainTextExporter)
            })
                .As<IExporter>();

            builder.RegisterType<ModuleInstaller>()
                .As<IModuleInstaller>();

            Container = builder.Build();
        }

        // todo: accept a configuration of some sort?
        public static void Initialize()
        {
            
        }
    }
}