using CKAN.Types;

namespace CKAN.CmdLine.Action
{
    public class Compare : ICommand
    {
        private readonly IUser user;

        public Compare(IUser user)
        {
            this.user = user;
        }

        public int RunCommand(CKAN.KSP ksp, object rawOptions)
        {
            var options = (CompareOptions) rawOptions;

            if (options.Left != null && options.Right != null)
            {
                var leftVersion = new Version(options.Left);
                var rightVersion = new Version(options.Right);

                var compareResult = leftVersion.CompareTo(rightVersion);
                if (compareResult == 0)
                {
                    user.RaiseMessage(
                        "\"{0}\" and \"{1}\" are the same versions.", leftVersion, rightVersion);
                }
                else if (compareResult < 0)
                {
                    user.RaiseMessage(
                        "\"{0}\" is lower than \"{1}\".", leftVersion, rightVersion);
                }
                else if (compareResult > 0)
                {
                    user.RaiseMessage(
                        "\"{0}\" is higher than \"{1}\".", leftVersion, rightVersion);
                }
                else
                {
                    user.RaiseMessage(
                        "Usage: ckan compare version1 version2");
                }
            }
            else
            {
                user.RaiseMessage(
                    "Usage: ckan compare version1 version2");
                return Exit.BADOPT;
            }

            return Exit.OK;
        }
    }
}