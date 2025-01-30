using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using NINA.Astrometry;
using NINA.Core.Utility;
using NINA.Sequencer.SequenceItem;
using OxyPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace NINA.Sequencer.Logic {
    [JsonObject(MemberSerialization.OptIn)]
    public class Expression : BaseINPC {
        public bool HasError => string.IsNullOrEmpty(Error);
        public string Error { get; set; }
        public double Value { get; set; }
        public ISequenceEntity Context { get; set; }
        public string Definition {  get; set; }
        public double Default { get; set; } = Double.NaN;

    }
}
