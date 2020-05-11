using NINA.Utility.ExternalCommand;
using NUnit.Framework;
using System.IO;

namespace NINATest {
    [TestFixture]
    public class ExternalCommandExecutorTest {
        [Test]
        public void TestCommandParser() {
            string cmdsrc = Path.GetTempFileName();
            string argsrc = @" arg1 ""arg2 """;
            string input = cmdsrc + argsrc;
            string[] args = ExternalCommandExecutor.ParseArguments(input);
            Assert.IsTrue(args.Length == 3);
            
            //basic command / arg parsing
            string cmd = ExternalCommandExecutor.GetComandFromString(input);
            Assert.IsTrue(ExternalCommandExecutor.CommandExists(cmd));
            Assert.IsNotNull(cmd);

            string arg = ExternalCommandExecutor.GetArgumentsFromString(input);
            Assert.IsNotNull(arg);
            Assert.IsTrue(arg.Equals(argsrc.Trim()));
            //no args
            cmd = ExternalCommandExecutor.GetComandFromString(cmdsrc);
            Assert.IsNotNull(cmd);
            Assert.IsTrue(cmd.Equals(cmdsrc));
            arg = ExternalCommandExecutor.GetArgumentsFromString(cmdsrc);
            Assert.IsNull(arg);
            //sanity checks
            args = ExternalCommandExecutor.ParseArguments("");
            Assert.IsNotNull(args);
            cmd = ExternalCommandExecutor.GetComandFromString("");
            Assert.IsNotNull(cmd);
            Assert.IsFalse(ExternalCommandExecutor.CommandExists(cmd));
         
        }
    }
}
