using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility.Profile {

    public interface IProfileService {
        Profiles Profiles { get; }
        IProfile ActiveProfile { get; }

        void Clone(Guid guid);

        void Add();

        void SelectProfile(Guid guid);

        void RemoveProfile(Guid guid);
    }
}