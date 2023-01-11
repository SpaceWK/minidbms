CREATE DATABASE students;
USE students;


# Disciplines
CREATE TABLE disciplines (DiscID VARCHAR(5) PRIMARY KEY, DName VARCHAR(20), CreditNr INT);

INSERT INTO disciplines (DiscID, DName, CreditNr) VALUES ('DB1', 'Databases 1', 7);
INSERT INTO disciplines (DiscID, DName, CreditNr) VALUES ('DS', 'Data Structures', 6);
INSERT INTO disciplines (DiscID, DName, CreditNr) VALUES ('CP', 'C Programming', 6);

INSERT INTO disciplines (DiscID, DName, CreditNr) VALUES ('C', 'Calculator', 6);


# Specialization
CREATE TABLE specialization (SpecID VARCHAR(5) PRIMARY KEY, SpecName VARCHAR(40), Language VARCHAR(20));

INSERT INTO specialization (SpecID, SpecName, Language) VALUES ('I', 'Informatics', 'English');
INSERT INTO specialization (SpecID, SpecName, Language) VALUES ('MI', 'Maths', 'English');
INSERT INTO specialization (SpecID, SpecName, Language) VALUES ('S', 'Sport', 'Deutch');
INSERT INTO specialization (SpecID, SpecName, Language) VALUES ('MJ', 'Michael Jackson', 'English');


# Groups
CREATE TABLE groups (GroupID INT PRIMARY KEY, SpecID VARCHAR(20) REFERENCES specialization (SpecID));

INSERT INTO groups (GroupID, SpecID) VALUES (531, 'I');
INSERT INTO groups (GroupID, SpecID) VALUES (532, 'I');
INSERT INTO groups (GroupID, SpecID) VALUES (631, 'MI'); # MI does not exist in 'specialization'


# Students
CREATE TABLE students (StudID INT PRIMARY KEY, GroupID INT REFERENCES groups (GroupID), StudName VARCHAR(40), Email VARCHAR(40));

INSERT INTO students (StudID, GroupID, StudName, Email) VALUES (1, 531, 'John Foreman', 'JohnForeman@email.com');
INSERT INTO students (StudID, GroupID, StudName, Email) VALUES (2, 531, 'Ashley Cole', 'AshleyCole@email.com');
INSERT INTO students (StudID, GroupID, StudName, Email) VALUES (3, 532, 'Nicolas Pitt', 'NicolasPitt@email.com');
INSERT INTO students (StudID, GroupID, StudName, Email) VALUES (4, 532, 'John Doe', 'JohnDoe@email.com');


# Marks
CREATE TABLE marks (StudID INT(10) REFERENCES students (StudID), DiscID VARCHAR(20) REFERENCES disciplines (DiscID), Mark INT, PRIMARY KEY (StudID,DiscID));

INSERT INTO marks (StudID, DiscID, Mark) values (1, 'DB1', 10);
INSERT INTO marks (StudID, DiscID, Mark) values (1, 'DS', 9);
INSERT INTO marks (StudID, DiscID, Mark) values (2, 'DB1', 8);
INSERT INTO marks (StudID, DiscID, Mark) values (2, 'CP', 9);
INSERT INTO marks (StudID, DiscID, Mark) values (3, 'DS', 5);
INSERT INTO marks (StudID, DiscID, Mark) values (1, 'CP', 7);


# Index
CREATE INDEX idx_marks_Mark ON marks (Mark);
CREATE INDEX idx_disciplines_CreditNr ON disciplines (CreditNr);

INSERT INTO marks (StudID, DiscID, Mark) values (1, 'CP', 10); # Should update marks IDX collections


# Delete
DELETE FROM students WHERE GroupID = 531;


# Select
SELECT DISTINCT GroupID FROM students;

SELECT DName, CreditNr FROM disciplines;

SELECT DName, CreditNr FROM disciplines WHERE CreditNr = 6;
SELECT DiscID, DName FROM disciplines WHERE CreditNr > 1;
SELECT CreditNr FROM disciplines WHERE CreditNr > 1 AND DName = 'Databases 1';

SELECT Mark FROM marks WHERE DiscID = 'DB1';

SELECT students.StudID, students.StudName, marks.DiscID, marks.Mark FROM students INNER JOIN marks ON students.StudID = marks.StudID;
SELECT specialization.SpecID, groups.GroupID, specialization.SpecName, specialization.Language FROM specialization INNER JOIN groups ON specialization.SpecID = groups.SpecID;

DROP TABLE marks;
DROP DATABASE db;