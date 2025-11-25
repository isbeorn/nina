#region "copyright"

/*
    Copyright © 2025 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;

namespace Accord.Imaging {
    /// <summary>
    /// Exception thrown when image properties are invalid or unsupported
    /// </summary>
    public class InvalidImagePropertiesException : ArgumentException {
        public InvalidImagePropertiesException() : base() { }
        
        public InvalidImagePropertiesException(string message) : base(message) { }
        
        public InvalidImagePropertiesException(string message, Exception innerException) 
            : base(message, innerException) { }
    }
}
