using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;

namespace NINA.Utility {

#nullable enable

    public class CommandLineOptions : ICommandLineOptions {

        class Options {
            [Option(shortName: 'p', longName: "profileid", Required = false, HelpText = "Load profile by given id at startup.")]            
            public string? ProfileId { get; set; }

            [Option(shortName: 's', longName: "sequencefile", Required = false, HelpText = "Load a sequence file at startup.")]
            public string? SequenceFile { get; set; }

            [Option(shortName: 'r', longName: "runsequence", Default = false, HelpText = "Automatically start a sequence loaded with -s and switch to Imaging tab.")]
            public bool RunSequence { get; set; }
        }

        public CommandLineOptions(string[] args) {
            if (args == null) { return; }

            var parser = new Parser(with => with.HelpWriter = null);
            var parserResult = parser.ParseArguments<Options>(args);
            parserResult
              .WithParsed(CaptureOptions)
              .WithNotParsed(errs => DisplayHelp(parserResult, errs));
        }

        public string? ProfileId { get; private set; }
        public string? SequenceFile { get; private set; }
        public bool RunSequence { get; private set; }
        public bool HasErrors { get; private set; }

        private void CaptureOptions(Options opts) {
            ProfileId = opts.ProfileId;
            SequenceFile = opts.SequenceFile;
            RunSequence = opts.RunSequence;
        }

        private void DisplayHelp<T>(ParserResult<T> result, IEnumerable<Error> errs) {
            HasErrors = true;

           var helpText = HelpText.AutoBuild(result);
            Console.WriteLine(string.Empty);
            Console.WriteLine(string.Empty);
            Console.WriteLine(helpText);
        }
    }
}
