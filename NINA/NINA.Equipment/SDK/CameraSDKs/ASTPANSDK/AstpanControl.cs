using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Equipment.SDK.CameraSDKs.ASTPANSDK {
    class AstpanControl {
        private ASTPAN_MUL_CONFIG capabilities;

        public AstpanControl(ASTPAN_AUTO_TYPE index, ASTPAN_MUL_CONFIG capabilities) {
            this.Index = index;
            this.capabilities = capabilities;
        }

        public ASTPAN_AUTO_TYPE Index { get; }
        public int Min { get => capabilities.r_MinValue; }
        public int Max { get => capabilities.r_MaxValue; }
        public bool Supported { get => capabilities.r_IsSupported > 0; }
    }
}
