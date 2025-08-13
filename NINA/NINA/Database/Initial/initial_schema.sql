/*
    Copyright © 2016 - 2022 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
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
