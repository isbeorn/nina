/*
    Copyright © 2016 - 2022 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
INSERT OR REPLACE INTO `brightstars` (name,ra,dec,magnitude,syncedfrom) VALUES
 ('Deneb Kaitos',10.89738,-17.986606,2.04,'Simbad'),
 ('Kakkab',220.482315,-47.388199,2.28,'Simbad'),
 ('Ankaa',6.57105,-42.305987,2.4,'Simbad'),
 ('Alpha Reticuli',63.60618,-62.473859,3.33,'Simbad'),
 ('Alpha Arae',262.96038,-49.876145,2.84,'Simbad'),
 ('Alherem',161.69241,-49.420257,2.72,'Simbad'),
 ('Alphaulka',340.66809,-45.113361,2.11,'Simbad');
 
  PRAGMA user_version = 3;