CREATE DATABASE students;
USE students;


CREATE TABLE disciplines (DiscID VARCHAR(5) PRIMARY KEY, DName VARCHAR(20), CredirNr INT);

CREATE TABLE marks (StudID INT(10) REFERENCES students (StudID), DiscID VARCHAR(20) REFERENCES disciplines (DiscID), Mark INT, PRIMARY KEY (StudID,DiscID));


CREATE INDEX idx_StudID ON marks (StudID);
CREATE INDEX idx_StudID ON marks (StudID, Mark);


DROP DATABASE db;
DROP TABLE marks;


INSERT INTO marks (StudID, DiscID, Mark) VALUES (1, 'OOP', 5);
DELETE FROM marks WHERE StudID = 1 AND DiscID = 'OOP';