using System.Threading;
using System.Threading.Tasks;

namespace Redbean
{
	public class Bootstrap : IBootstrap
	{
		private CancellationTokenSource source;
		protected CancellationToken cancellationToken => source.Token;
		
		public async Task Start()
		{
			ApplicationLifeCycle.OnApplicationExit += OnApplicationExit;

			source = new CancellationTokenSource();
			await Setup();
		}
		
		private async void OnApplicationExit()
		{
			ApplicationLifeCycle.OnApplicationExit -= OnApplicationExit;

			source?.Cancel();
			await Teardown();
		}

		protected virtual Task Setup() => Task.CompletedTask;
		protected virtual Task Teardown() => Task.CompletedTask;
	}
}