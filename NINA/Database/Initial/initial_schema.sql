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
CREATE TABLE IF NOT EXISTS `visualdescription` (
	`dsodetailid`	TEXT,
	`description`	TEXT,
	PRIMARY KEY(`dsodetailid`,`description`),
	FOREIGN KEY(`dsodetailid`) REFERENCES `dsodetail`(`id`)
);
CREATE TABLE IF NOT EXISTS `dsodetail` (
	`id`	TEXT NOT NULL,
	`ra`	REAL,
	`dec`	REAL,
	`magnitude`	REAL,
	`surfacebrightness`	REAL,
	`sizemin`	NUMERIC,
	`sizemax`	REAL,
	`positionangle`	REAL,
	`nrofstars`	REAL,
	`brighteststar`	REAL,
	`constellation`	TEXT,
	`dsotype`	TEXT,
	`dsoclass`	TEXT,
	`notes`	REAL,
	`syncedfrom`	TEXT,
	`lastmodified`	TEXT,
	PRIMARY KEY(`id`)
);
CREATE TABLE IF NOT EXISTS `constellationstar` (
	`id`	INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE,
	`name`	TEXT,
	`ra`	REAL NOT NULL,
	`dec`	REAL NOT NULL,
	`mag`	REAL
);
CREATE TABLE IF NOT EXISTS `constellationboundaries` (
	`constellation`	TEXT,
	`position`	INTEGER,
	`ra`	REAL,
	`dec`	REAL
);
CREATE TABLE IF NOT EXISTS `constellation` (
	`constellationid`	TEXT,
	`starid`	INTEGER,
	`followstarid`	INTEGER
);
CREATE TABLE IF NOT EXISTS `cataloguenr` (
	`dsodetailid`	TEXT,
	`catalogue`	TEXT,
	`designation`	TEXT,
	PRIMARY KEY(`dsodetailid`,`catalogue`,`designation`),
	FOREIGN KEY(`dsodetailid`) REFERENCES `dsodetail`(`id`)
);
CREATE TABLE IF NOT EXISTS `brightstars` (
	`name`	TEXT NOT NULL,
	`ra`	REAL,
	`dec`	REAL,
	`magnitude`	REAL,
	`syncedfrom`	TEXT,
	PRIMARY KEY(`name`)
);
