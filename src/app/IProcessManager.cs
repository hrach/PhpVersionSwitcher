using System.Threading.Tasks;

namespace PhpVersionSwitcher
{
	internal interface IProcessManager
	{
		string Name { get; }
		bool RestartWhenPhpVersionChanges { get; }
		bool IsRunning();
		string GroupName { get;  }
		Task Start();
		Task Stop();
		Task Restart();
	}
}
