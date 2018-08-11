using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.ViewModel {

    internal interface IFocuserVM {

        Task<bool> ChooseFocuser();

        Task<int> MoveFocuser(int position);

        Task<int> MoveFocuserRelative(int position);

        void Disconnect();
    }
}