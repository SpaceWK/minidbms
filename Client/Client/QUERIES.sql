CREATE DATABASE students;
USE students;


# Disciplines
CREATE TABLE disciplines (DiscID VARCHAR(5) PRIMARY KEY, DName VARCHAR(20), CredirNr INT);

INSERT INTO disciplines (DiscID, DName, CredirNr) VALUES ('DB1', 'Databases 1', 7);
INSERT INTO disciplines (DiscID, DName, CredirNr) VALUES ('DB', 'Data Structures', 6);
INSERT INTO disciplines (DiscID, DName, CredirNr) VALUES ('CP', 'C Programming', 8);


# Specialization
CREATE TABLE specialization (SpecID VARCHAR(5) PRIMARY KEY, SpecName VARCHAR(40), Language VARCHAR(20));

INSERT INTO specialization (SpecID, SpecName, Language) VALUES ('I', 'Informatics', 'English');


# Groups
CREATE TABLE groups (GroupID INT PRIMARY KEY, SpecID VARCHAR(20) REFERENCES specialization (SpecID));

INSERT INTO groups (GroupID, SpecID) VALUES (531, 'I');


#

insert into marks (StudID,DiscID,Mark) values (1,'DB1',10); 
insert into marks (StudID,DiscID,Mark) values (1,'DS',9); 
insert into marks (StudID,DiscID,Mark) values (2,'AI',9);
insert into marks (StudID,DiscID,Mark) values (2,'CP',9);

CREATE INDEX idx_StudID ON marks (StudID);
CREATE INDEX idx_StudID ON marks (StudID, Mark);


DROP DATABASE db;
DROP TABLE marks;


INSERT INTO marks (StudID, DiscID, Mark) VALUES (1, 'OOP', 5);
DELETE FROM marks WHERE StudID = 1 AND DiscID = 'OOP';