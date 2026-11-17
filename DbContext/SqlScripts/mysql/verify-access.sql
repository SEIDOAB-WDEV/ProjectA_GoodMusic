USE `sql-music`;

DELIMITER $$

/* Test as gstusr */
CREATE OR REPLACE DEFINER='gstusr'@'%' PROCEDURE gstusr_TestAccessRights()
BEGIN

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        GET DIAGNOSTICS CONDITION 1
        @sqlstate = RETURNED_SQLSTATE,
        @errno = MYSQL_ERRNO,
        @text = MESSAGE_TEXT;

        SELECT 'gstUsrRole', @sqlstate, @errno, @text;
    END;

    /* SELECT * FROM `sql-music`.`gstusr_vwInfoDb`; */
    SELECT * FROM `sql-music`.`supusr_MusicGroups`;
    SELECT * FROM `sql-music`.`dbo_Users`;

END$$

/* Test as usr */
CREATE OR REPLACE DEFINER='usr'@'%' PROCEDURE usr_TestAccessRights()
BEGIN
    
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        GET DIAGNOSTICS CONDITION 1
        @sqlstate = RETURNED_SQLSTATE,
        @errno = MYSQL_ERRNO,
        @text = MESSAGE_TEXT;

        SELECT 'usrRole', @sqlstate, @errno, @text;
    END;

    /* SELECT * FROM `sql-music`.`gstusr_vwInfoDb`; */
    /* SELECT * FROM `sql-music`.`supusr_MusicGroups`; */
    SELECT * FROM `sql-music`.`dbo_Users`;
END$$

/* Test as supusr */
CREATE OR REPLACE DEFINER='supusr'@'%' PROCEDURE supusr_TestAccessRights()
BEGIN

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        GET DIAGNOSTICS CONDITION 1
        @sqlstate = RETURNED_SQLSTATE,
        @errno = MYSQL_ERRNO,
        @text = MESSAGE_TEXT;

        SELECT 'supUsrRole', @sqlstate, @errno, @text;
    END;

    /* SELECT * FROM `sql-music`.`gstusr_vwInfoDb`; */
    /* SELECT * FROM `sql-music`.`supusr_MusicGroups`; */
    SELECT * FROM `sql-music`.`dbo_Users`;
END$$

/* Test as dbo */
CREATE OR REPLACE DEFINER='dbo'@'%' PROCEDURE dbo_TestAccessRights()
BEGIN

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        GET DIAGNOSTICS CONDITION 1
        @sqlstate = RETURNED_SQLSTATE,
        @errno = MYSQL_ERRNO,
        @text = MESSAGE_TEXT;

        SELECT 'dboRole', @sqlstate, @errno, @text;
    END;

    /* SELECT * FROM `sql-music`.`gstusr_vwInfoDb`; */
    /* SELECT * FROM `sql-music`.`supusr_MusicGroups`; */
    SELECT * FROM `sql-music`.`dbo_Users`;
END$$

DELIMITER ;

GRANT EXECUTE ON PROCEDURE `sql-music`.`gstusr_TestAccessRights` TO 'gstUsrRole';
GRANT EXECUTE ON PROCEDURE `sql-music`.`usr_TestAccessRights` TO 'usrRole';
GRANT EXECUTE ON PROCEDURE `sql-music`.`supusr_TestAccessRights` TO 'supUsrRole';
GRANT EXECUTE ON PROCEDURE `sql-music`.`dbo_TestAccessRights` TO 'dboRole';
FLUSH PRIVILEGES;

CALL gstusr_TestAccessRights(); /* should cause access error */
CALL usr_TestAccessRights(); /* should cause access error */
CALL supusr_TestAccessRights(); /* should access cause error */
CALL dbo_TestAccessRights();

REVOKE EXECUTE ON PROCEDURE `sql-music`.`gstusr_TestAccessRights` FROM 'gstUsrRole';
REVOKE EXECUTE ON PROCEDURE `sql-music`.`usr_TestAccessRights` FROM 'usrRole';
REVOKE EXECUTE ON PROCEDURE `sql-music`.`supusr_TestAccessRights` FROM 'supUsrRole';
REVOKE EXECUTE ON PROCEDURE `sql-music`.`dbo_TestAccessRights` FROM 'dboRole';
FLUSH PRIVILEGES;

DROP PROCEDURE IF EXISTS `sql-music`.`gstusr_TestAccessRights`;
DROP PROCEDURE IF EXISTS `sql-music`.`usr_TestAccessRights`;
DROP PROCEDURE IF EXISTS `sql-music`.`supusr_TestAccessRights`;
DROP PROCEDURE IF EXISTS `sql-music`.`dbo_TestAccessRights`;