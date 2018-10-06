using Autofac;

namespace EvilBot
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var container = ContainerConfig.Config();
            using (var scope = container.BeginLifetimeScope())
            {
                var app = scope.Resolve<IApplication>();
                app.Run();
            }
        }
    }
}