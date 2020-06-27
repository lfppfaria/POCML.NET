USE machine_learning;

DELIMITER //

CREATE PROCEDURE populate_ml_database(begin_year int, end_year int)
BEGIN

DECLARE current_year INT;
DECLARE current_month INT;
DECLARE counter INT;

set current_year = begin_year;

year_loop: LOOP	
    SET current_month = 1;
    
    month_loop: LOOP
		INSERT INTO input_data VALUES(null, current_year, current_month, RAND() * 100000);        
        
        IF(current_month = 12) THEN
			LEAVE month_loop;
		END IF;
        
        SET current_month = current_month + 1;
    END LOOP month_loop;    
    
    IF(current_year = end_year) THEN
		LEAVE year_loop;
    END IF;
    
    SET current_year = current_year + 1;
    -- SET counter = counter + 1;
END LOOP year_loop;

END //
