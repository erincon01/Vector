
-- differences:

--- cannot define the vector size in table creation.
--- when calling the VECTOR_DISTANCE function, dbo. is required.
--- type vector is SAFE.
--- performance analysis is required.


-- enable instance level SQL CLR
EXEC sp_configure 'clr enabled', 1;
RECONFIGURE;
GO

-- configure database
ALTER AUTHORIZATION ON DATABASE::[VERNE] TO sa;
ALTER DATABASE VERNE SET TRUSTWORTHY on;
GO


CREATE ASSEMBLY myVector
FROM 'C:\Users\erincon\Sources\_personal\Vector\bin\Release\net4.8\myVector.dll'
WITH PERMISSION_SET = SAFE;

/*

DROP TABLE player_positions;
DROP FUNCTION VECTOR_DISTANCE;
DROP TYPE Vector;
DROP ASSEMBLY myVector;

*/


CREATE TYPE Vector
EXTERNAL NAME myVector.Vector;
GO


CREATE FUNCTION dbo.VECTOR_DISTANCE
(
    @distanceMetric NVARCHAR(MAX),
    @v1 Vector,
    @v2 Vector
)
RETURNS REAL
AS EXTERNAL NAME myVector.Vector.VectorDistance;
GO

CREATE ASSEMBLY myRestEndpoint 
FROM 'C:\Users\erincon\Sources\_personal\Vector\bin\Release\net4.8\myRestEndpoint.dll'
WITH PERMISSION_SET = EXTERNAL_ACCESS;
GO

/*

DROP PROCEDURE dbo.sp_invoke_external_rest_endpoint2
DROP PROCEDURE dbo.sp_invoke_ollama_model
DROP ASSEMBLY myRestEndpoint;

*/

-- stored procedure to call external REST endpoint

CREATE PROCEDURE dbo.sp_invoke_external_rest_endpoint2
    @url NVARCHAR(MAX),
    @method NVARCHAR(10),
    @payload NVARCHAR(MAX),
    @headers NVARCHAR(MAX),
    @response NVARCHAR(MAX) OUTPUT
AS EXTERNAL NAME myRestEndpoint.RestEndpoint.InvokeRestEndpoint;
GO


-- external procedure for local Ollama model request

CREATE PROCEDURE dbo.sp_invoke_ollama_model
    @endpoint NVARCHAR(MAX),
    @model NVARCHAR(MAX),
    @prompt NVARCHAR(MAX),
    @response NVARCHAR(MAX) OUTPUT
AS EXTERNAL NAME myRestEndpoint.RestEndpoint.InvokeOllamaModel;
GO

