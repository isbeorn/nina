using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Sequencer.Logic
{
    public interface ISymbolProvider {
        void AddOrUpdateSymbol(string token, object value);

        void AddOrUpdateSymbol(string token, object value, Symbol[] values);

        bool RemoveSymbol(string token);

        string GetProviderName();
        void RegisterFunction(SymbolFunction function);
        void Execute(Expression expr, ISequenceEntity context);
    }
}
