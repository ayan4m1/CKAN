namespace CKAN.CmdLine.Action
{
    public interface ICommand
    {
        int RunCommand(CKAN.KSP ksp, object options);
    }
}