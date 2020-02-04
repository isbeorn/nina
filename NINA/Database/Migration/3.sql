/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>
    
    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.
    
    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.
    
    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.
    
    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
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