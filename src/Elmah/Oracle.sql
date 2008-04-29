/*
  
   ELMAH - Error Logging Modules and Handlers for ASP.NET
   Copyright (c) 2007 Atif Aziz. All rights reserved.
  
    Author(s):
  
      James Driscoll, mailto:jamesdriscoll@btinternet.com
  
   This library is free software; you can redistribute it and/or modify it 
   under the terms of the New BSD License, a copy of which should have 
   been delivered along with this distribution.
  
   THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS 
   "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT 
   LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A 
   PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT 
   OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, 
   SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT 
   LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, 
   DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY 
   THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT 
   (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE 
   OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
  
*/

-- NB This script assumes you have logged on in the schema where you want to create the elmah objects

-- create a sequence for the errors (user to simulate an identity in SQL Server)
CREATE SEQUENCE elmah$error_seq START WITH 1 INCREMENT BY 1 NOMAXVALUE NOCYCLE NOCACHE NOORDER;

-- create the table to store the data
-- you can optionally specify tablespaces here too!
CREATE TABLE elmah$error
(
	-- if using Oracle 10g and above you can add DEFAULT SYS_GUID() 
	-- to the errorid definition.
	-- Oracle 8i doesn't like it with an NVARCHAR2
	-- haven't tested it against 9i
    errorid			NVARCHAR2(32) NOT NULL,
    application		NVARCHAR2(60) NOT NULL,
    host			NVARCHAR2(50) NOT NULL,
    type			NVARCHAR2(100) NOT NULL,
    source			NVARCHAR2(60) NOT NULL,
    message			NVARCHAR2(500) NOT NULL,
    username        NVARCHAR2(50) NOT NULL,
    statuscode		NUMBER NOT NULL,
    timeutc			DATE NOT NULL,
    sequencenumber	NUMBER NOT NULL,
    allxml			NCLOB NOT NULL,
    CONSTRAINT idx_elmah$error_pk 
        PRIMARY KEY (errorid) 
        USING INDEX -- TABLESPACE "TABLESPACE FOR INDEX"
) -- TABLESPACE "TABLESPACE FOR DATA"
/

-- trigger to make sure we get our sequence number in the table
CREATE TRIGGER trg_elmah$error_bi
BEFORE INSERT ON elmah$error
FOR EACH ROW
BEGIN
    SELECT elmah$error_seq.NEXTVAL INTO :new.sequencenumber FROM dual;
END trg_elmah$error_bi;
/

-- create the index on the table
CREATE INDEX idx_elmah$error_app_time_seq ON elmah$error(application, timeutc DESC, sequencenumber DESC)
/

-- package containing the procedures we need for Elmah to work
CREATE OR REPLACE PACKAGE pkg_elmah$error
IS
    TYPE t_cursor IS REF CURSOR;
    
    PROCEDURE GetErrorXml
    (
        v_Application IN elmah$error.application%TYPE,
        v_ErrorId IN elmah$error.errorid%TYPE,
        v_AllXml OUT elmah$error.allxml%TYPE
    );

    PROCEDURE GetErrorsXml
    (
        v_Application IN elmah$error.application%TYPE,
        v_PageIndex IN NUMBER DEFAULT 0,
        v_PageSize IN NUMBER DEFAULT 15,
        v_TotalCount OUT NUMBER,
        v_Results OUT t_cursor
    );
    
    PROCEDURE LogError
    (
        v_ErrorId IN elmah$error.errorid%TYPE,
        v_Application IN elmah$error.application%TYPE,
        v_Host IN elmah$error.host%TYPE,
        v_Type IN elmah$error.type%TYPE,
        v_Source IN elmah$error.source%TYPE,
        v_Message IN elmah$error.message%TYPE,
        v_User IN elmah$error.username%TYPE,
        v_AllXml IN elmah$error.allxml%TYPE,
        v_StatusCode IN elmah$error.statuscode%TYPE,
        v_TimeUtc IN elmah$error.timeutc%TYPE
    );

END pkg_elmah$error;
/

CREATE OR REPLACE PACKAGE BODY pkg_elmah$error
IS
    PROCEDURE GetErrorXml
    (
        v_Application IN elmah$error.application%TYPE,
        v_ErrorId IN elmah$error.errorid%TYPE,
        v_AllXml OUT elmah$error.allxml%TYPE
    )
    IS
    BEGIN
        SELECT	allxml
        INTO	v_AllXml
        FROM	elmah$error
        WHERE	errorid = UPPER(v_ErrorId)
        AND		application = v_Application;
    EXCEPTION
        WHEN NO_DATA_FOUND THEN
            v_AllXml := NULL;
    END GetErrorXml;

    PROCEDURE GetErrorsXml
    (
        v_Application IN elmah$error.application%TYPE,
        v_PageIndex IN NUMBER DEFAULT 0,
        v_PageSize IN NUMBER DEFAULT 15,
        v_TotalCount OUT NUMBER,
        v_Results OUT t_cursor
    )
    IS
        l_StartRowIndex NUMBER;
        l_EndRowIndex	NUMBER;
    BEGIN
        -- Get the ID of the first error for the requested page
        l_StartRowIndex := v_PageIndex * v_PageSize + 1;
        l_EndRowIndex := l_StartRowIndex + v_PageSize - 1;
        
        -- find out how many rows we've got in total
        SELECT	COUNT(*)
        INTO	v_TotalCount
        FROM	elmah$error
        WHERE	application = v_Application;

        OPEN v_Results FOR
            SELECT	*
            FROM
            (
                SELECT	e.*,
                        ROWNUM row_number
                FROM
                (
                    SELECT	/*+ INDEX(elmah$error, idx_elmah$error_app_time_seq) */
                            errorid,
                            application,
                            host,
                            type,
                            source,
                            message,
                            username,
                            statuscode,
                            timeutc
                    FROM	elmah$error
                    WHERE	application = v_Application
                    ORDER BY
                            timeutc DESC, 
                            sequencenumber DESC
                ) e
                WHERE ROWNUM <= l_EndRowIndex
            )
            WHERE	row_number >= l_StartRowIndex;
            
    END GetErrorsXml;

    PROCEDURE LogError
    (
        v_ErrorId IN elmah$error.errorid%TYPE,
        v_Application IN elmah$error.application%TYPE,
        v_Host IN elmah$error.host%TYPE,
        v_Type IN elmah$error.type%TYPE,
        v_Source IN elmah$error.source%TYPE,
        v_Message IN elmah$error.message%TYPE,
        v_User IN elmah$error.username%TYPE,
        v_AllXml IN elmah$error.allxml%TYPE,
        v_StatusCode IN elmah$error.statuscode%TYPE,
        v_TimeUtc IN elmah$error.timeutc%TYPE
    )
    IS
    BEGIN
        INSERT INTO elmah$error
            (
                errorid,
                application,
                host,
                type,
                source,
                message,
                username,
                allxml,
                statuscode,
                timeutc
            )
        VALUES
            (
                UPPER(v_ErrorId),
                v_Application,
                v_Host,
                v_Type,
                v_Source,
                v_Message,
                v_User,
                v_AllXml,
                v_StatusCode,
                v_TimeUtc
            );

    END LogError;	

END pkg_elmah$error;
/


/* 
-- Optional steps to make Elmah publicly available across Oracle users.
-- NB As long as you use the schema owner for the connection string, this is not necessary.

-- replace OWNER for the schema owner in the following 2 statements
CREATE OR REPLACE PUBLIC SYNONYM pkg_elmah$error FOR OWNER.pkg_elmah$error;
GRANT EXECUTE ON OWNER.pkg_elmah$error TO PUBLIC;

-- Alternatively make available to a single Oracle user.
-- first log on as the user that you want to access the Elmah data
-- replace OWNER for the schema owner in the following 2 statements
CREATE OR REPLACE SYNONYM pkg_elmah$error FOR OWNER.pkg_elmah$error;
-- now log on as OWNER and substitute USER_NAME for the user
GRANT EXECUTE ON OWNER.pkg_elmah$error TO USER_NAME;
*/
