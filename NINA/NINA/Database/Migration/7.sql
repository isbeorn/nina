/*
    Copyright © 2016 - 2022 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging N Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

/*
 * Corrections to existing entries
 */

/* The Vorontsov-Vel'yaminov catalog is abbreviated "VV", not "V V" */
UPDATE cataloguenr SET
    catalogue = 'VV'
    WHERE catalogue = 'V V';
    
/* IC63 is also LBN 622, not LBN 623 */
UPDATE cataloguenr SET
    designation = '622'
    WHERE catalogue = 'LBN' AND designation = '623' AND dsodetailid = 'IC63';

/* NGC7635 is also LBN 548, not LBN 549 */
UPDATE cataloguenr SET
    designation = '548'
    WHERE catalogue = 'LBN' AND designation = '549' AND dsodetailid = 'NGC7635';

/* Sh2-132 is also LBN 473, not LBN 470 */
UPDATE cataloguenr SET
    designation = '473'
    WHERE catalogue = 'LBN' AND designation = '470' AND dsodetailid = 'Sh2-132';

/* IC448 is also LBN 931, not LBN 930 */
UPDATE cataloguenr SET
    designation = '931'
    WHERE catalogue = 'LBN' AND designation = '930' AND dsodetailid = 'IC448';

/* IC4604 is also LBN 1111, not LBN 1112 */
UPDATE cataloguenr SET
    designation = '1111'
    WHERE catalogue = 'LBN' AND designation = '1112' AND dsodetailid = 'IC4604';

/* Sh2-263 is also LBN 867, not LBN 866 */
UPDATE cataloguenr SET
    designation = '867'
    WHERE catalogue = 'LBN' AND designation = '866' AND dsodetailid = 'Sh2-263';

/* IC2162 is also Sh 2-255, not Sh 2-256 */
UPDATE cataloguenr SET
    designation = '255'
    WHERE catalogue = 'Sh2' AND designation = '256' AND dsodetailid = 'IC2162';

/* Sh2-55 is also LBN 73, not LBN 74 */
UPDATE cataloguenr SET
    designation = '73'
    WHERE catalogue = 'LBN' AND designation = '74' AND dsodetailid = 'Sh2-55';

/* NGC6559 is also LBN 29, not LBN 28 */
UPDATE cataloguenr SET
    designation = '29'
    WHERE catalogue = 'LBN' AND designation = '28' AND dsodetailid = 'NGC6559';

/* NGC6590 is also LBN 46, not LBN 43 */
UPDATE cataloguenr SET
    designation = '46'
    WHERE catalogue = 'LBN' AND designation = '43' AND dsodetailid = 'NGC6590';

/* NGC1432 is also LBN 771, not LBN 772 */
UPDATE cataloguenr SET
    designation = '771'
    WHERE catalogue = 'LBN' AND designation = '772' AND dsodetailid = 'NGC1432';

/* Sh2-88 is also LBN 139, not LBN 149 */
UPDATE cataloguenr SET
    designation = '139'
    WHERE catalogue = 'LBN' AND designation = '149' AND dsodetailid = 'Sh2-88';

/* IC63 is also LBN 622, not LBN 623 */
UPDATE cataloguenr SET
    designation = '622'
    WHERE catalogue = 'LBN' AND designation = '623' AND dsodetailid = 'IC63';

/* VDB 8 is not LBN 643 */
DELETE FROM cataloguenr
    WHERE dsodetailid = 'vdB8' AND catalogue = 'LBN' AND designation = '643';


/* NGC6523 is a star cluster within M8, not M8 itself */
UPDATE dsodetail SET
    notes = 'Bipolar nebula in M 8 (Lagoon nebula), associated to the O-star Herschel 36',
    lastmodified = '2022-06-20 04:11:54'
    WHERE id = 'NGC6523';

DELETE FROM cataloguenr
    WHERE dsodetailid = 'NGC6523' AND catalogue = 'M' AND designation = '8';
DELETE FROM cataloguenr
    WHERE dsodetailid = 'NGC6523' AND catalogue = 'NAME' AND designation = 'Lagoon Nebula';

/*
 * NGC6533 is equivalent to M8 according to ViZer and online resources.
 * Touch up the coordinates (from simbad) and associate other popular designations with it
 */
UPDATE dsodetail SET
    ra = '270.90416666666664',
    dec = '-24.386666666666667',
    magnitude = '4.6',
    surfacebrightness = '5.8',
    notes = 'M 8 contains NGC 6523 and NGC 6530',
    lastmodified = '2022-06-20 04:11:54'
    WHERE id = 'NGC6533';
INSERT OR REPLACE INTO cataloguenr VALUES 
('NGC6533', 'M', '8'),
('NGC6533', 'NAME', 'Lagoon Nebula'),
('NGC6533', 'Sh2', '25'),
('NGC6533', 'LBN', '25'),
('NGC6533', 'Gum', '72'),
('NGC6533', 'RCW', '126'),

/* Missing popular designations for existing database objects */
('NGC6302', 'Sh2', '6'),
('NGC6302', 'Gum', '60'),
('NGC6302', 'RCW', '124'),

('NGC6357', 'Sh2', '11'),
('NGC6357', 'Gum', '66'),
('NGC6357', 'RCW', '131'),

('IC410', 'LBN', '807'),

('IC417', 'Sh2', '234'),

('NGC1931', 'LBN', '810'),
('NGC1931', 'Sh2', '237'),

('Sh2-205', 'LBN', '701'),

('IC10', 'LBN', '591'),

('NGC281', 'LBN', '616'),
('NGC281', 'Sh2', '184'),

('IC1805', 'LBN', '654'),
('IC1805', 'Sh2', '190'),
('IC1805', 'Collinder', '26'),

('IC1848', 'LBN', '667'),
('IC1848', 'Sh2', '199'),
('IC1848', 'Collinder', '32'),

('Sh2-157', 'LBN', '537'),
('Sh2-157', 'Sh1', '109'),

('NGC7635', 'Sh2', '162'),

('NGC7822', 'Sh2', '171'),

('Ced214', 'LBN', '581'),

('NGC7023', 'LBN', '487'),

('NGC7129', 'LBN', '497'),

('NGC7380', 'LBN', '511'),

('Ced90', 'LBN', '1039'),

('IC468', 'LBN', '1040'),

('NGC2359', 'Gum', '4'),
('NGC2359', 'RCW', '5'),
('NGC2359', 'Sh2', '298'),

('Sh2-91', 'LBN', '147'),

('NGC6847', 'Sh2', '97'),

('Sh2-101', 'LBN', '168'),

('IC1310', 'LBN', '181'),

('Sh2-104', 'LBN', '195'),

('NGC6914', 'LBN', '280'),

('Sh2-115', 'LBN', '357'),

('IC5070', 'LBN', '350'),

('NGC7000', 'Sh2', '117'),

('NGC1909', 'LBN', '959'),

('IC2169', 'LBN', '903'),

('IC446', 'LBN', '898'),

('NGC2264', 'LBN', '911'),
('NGC2264', 'Sh2', '273'),

('IC466', 'Sh2', '288'),
('IC466', 'LBN', '1013'),

('Sh2-294', 'RCW', '3'),

('NGC1976', 'LBN', '974'),

('NGC1980', 'LBN', '977'),

('NGC2023', 'vdB', '52'),

('Ced62', 'LBN', '855'),

('NGC2175', 'LBN', '854'),
('NGC2175', 'Sh2', '252'),

('IC2162', 'LBN', '859'),

('IC348', 'LBN', '758'),
('IC348', 'Collinder', '41'),

('NGC1491', 'Sh2', '206'),

('NGC1499', 'Sh2', '220'),

('NGC1579', 'Sh2', '222'),

('Sh2-302', 'LBN', '1046'),

('Sh2-307', 'LBN', '1051'),

('Sh2-311', 'RCW', '16'),

('NGC2467', 'Sh2', '311'),

('Sh2-312', 'LBN', '1077'),

('Sh2-9', 'LBN', '1101'),
('Sh2-9', 'Gum', '65'),
('Sh2-9', 'vDB', '104'),

('vdB107', 'LBN', '1108'),

('NGC6611', 'LBN', '67'),
('NGC6611', 'IC', '4703'),

('Sh2-82', 'LBN', '129'),

('Sh2-84', 'LBN', '131'),

('Sh2-16', 'LBN', '1124'),

('NGC6514', 'LBN', '27'),
('NGC6514', 'Collinder', '360'),

('IC4684', 'LBN', '34'),

('IC1274', 'LBN', '33'),

('IC4701', 'LBN', '55'),

('NGC6589', 'LBN', '43'),

('NGC6590', 'vdB', '119'),
('NGC6590', 'Sh2', '37'),

('NGC6618', 'LBN', '60'),

('NGC1952', 'LBN', '833'),

('Sh2-240', 'LBN', '822'),

('NGC6842', 'LBN', '149'),
('NGC6842', 'Sh1', '72'),
('NGC6842', 'Sh2', '95'),

('IC4954', 'LBN', '153'),

('Sh2-223', 'LBN', '768'),

('IC410', 'LBN', '807'),

('IC417', 'Sh2', '234'),

('NGC1931', 'LBN', '810'),
('NGC1931', 'Sh2', '237'),

('Sh2-205', 'LBN', '701'),

('IC10', 'LBN', '591'),

('NGC281', 'LBN', '616'),
('NGC281', 'Sh2', '184'),

('IC1805', 'LBN', '654'),
('IC1805', 'Sh2', '190'),
('IC1805', 'Collinder', '26'),

('IC1848', 'LBN', '667'),
('IC1848', 'Sh2', '199'),
('IC1848', 'Collinder', '32'),

('Sh2-157', 'LBN', '537'),
('Sh2-157', 'Sh1', '109'),

('NGC7635', 'Sh2', '162');

PRAGMA user_version = 7;