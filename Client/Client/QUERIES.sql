CREATE DATABASE students;
USE students;


# Disciplines
CREATE TABLE disciplines (DiscID VARCHAR(5) PRIMARY KEY, DName VARCHAR(20), CreditNr INT);

INSERT INTO disciplines (DiscID, DName, CreditNr) VALUES ('DB1', 'Databases 1', 7);
INSERT INTO disciplines (DiscID, DName, CreditNr) VALUES ('DB', 'Data Structures', 6);
INSERT INTO disciplines (DiscID, DName, CreditNr) VALUES ('CP', 'C Programming', 8);


# Specialization
CREATE TABLE specialization (SpecID VARCHAR(5) PRIMARY KEY, SpecName VARCHAR(40), Language VARCHAR(20));

INSERT INTO specialization (SpecID, SpecName, Language) VALUES ('I', 'Informatics', 'English');


# Groups
CREATE TABLE groups (GroupID INT PRIMARY KEY, SpecID VARCHAR(20) REFERENCES specialization (SpecID));

INSERT INTO groups (GroupID, SpecID) VALUES (531, 'I');


# Students
CREATE TABLE students (StudID INT PRIMARY KEY, GroupID INT REFERENCES groups (GroupID));

INSERT INTO students (StudID, GroupID, StudName, Email) VALUES (1, 531, 'John Foreman', 'JohnForeman@email.com');
INSERT INTO students (StudID, GroupID, StudName, Email) VALUES (2, 531, 'Ashley Cole', 'AshleyCole@email.com');
INSERT INTO students (StudID, GroupID, StudName, Email) VALUES (8, 532, 'Nicolas Pitt', 'NicolasPitt@email.com');


# Marks
CREATE TABLE marks (StudID INT(10) REFERENCES students (StudID), DiscID VARCHAR(20) REFERENCES disciplines (DiscID), Mark INT, PRIMARY KEY (StudID,DiscID));

INSERT INTO marks (StudID, DiscID, Mark) values (1, 'DB1', 10);
INSERT INTO marks (StudID, DiscID, Mark) values (1, 'DS', 9);


# Index
CREATE INDEX idx_marks_StudID ON marks (StudID);
CREATE INDEX idx_marks_StudIDMark ON marks (StudID,Mark);


# Delete
DELETE FROM marks WHERE StudID = 50 AND DiscID = 'OOP';
DELETE FROM marks WHERE StudID = 49 AND DiscID = 'OOP';

DELETE FROM students WHERE StudID = 50;
DELETE FROM students WHERE GroupID = 531;


# Select
# TODO


DROP TABLE marks;
DROP DATABASE db;