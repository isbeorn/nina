#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using System;

namespace NINA.Sequencer.Logic {
    public enum SymbolChangeKind {
        Added,
        Updated,
        Removed
    }

    public sealed class SymbolChangedEventArgs : EventArgs {
        public SymbolChangedEventArgs(SymbolChangeKind changeKind, Symbol symbol, object oldValue, object newValue) {
            ChangeKind = changeKind;
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            OldValue = oldValue;
            NewValue = newValue;
        }

        public SymbolChangeKind ChangeKind { get; }
        public Symbol Symbol { get; }
        public object OldValue { get; }
        public object NewValue { get; }
        public string ProviderName => Symbol.Category;
        public string Key => Symbol.Key;
        public string QualifiedKey => $"{ProviderName}{SymbolBroker.DELIMITER}{Key}";
    }

    internal static class SymbolEventPublisher {
        public static void Publish(EventHandler<SymbolChangedEventArgs> handlers, object sender, SymbolChangedEventArgs args, string eventName) {
            if (handlers == null) {
                return;
            }

            foreach (EventHandler<SymbolChangedEventArgs> handler in handlers.GetInvocationList()) {
                try {
                    handler(sender, args);
                } catch (Exception ex) {
                    var methodInfo = handler.Method;
                    string declaringType = methodInfo.DeclaringType?.FullName ?? "<unknown>";
                    string handlerName = $"{declaringType}.{methodInfo.Name}";
                    Logger.Error($"Symbol event handler {handlerName} failed while handling {eventName} for {args.QualifiedKey}", ex);
                }
            }
        }
    }
}
