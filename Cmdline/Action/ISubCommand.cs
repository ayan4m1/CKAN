namespace CKAN.CmdLine.Action
{
    internal interface ISubCommand
    {
        int RunSubCommand(SubCommandOptions options);
    }
}