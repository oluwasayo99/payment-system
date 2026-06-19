CREATE OR REPLACE PROCEDURE release_reservation(
    p_reservation_id UUID
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_status VARCHAR;
BEGIN
    SELECT status INTO v_status 
    FROM reservations
    WHERE id = p_reservation_id FOR UPDATE;

    IF v_status IS NULL THEN 
        RAISE EXCEPTION 'Reservation not found';
    END IF;
    
    IF v_status != 'PENDING' THEN 
        RAISE EXCEPTION 'Cannot release. Status is %', v_status;
    END IF;

    UPDATE reservations SET status = 'REVERSED' WHERE id = p_reservation_id;
END;
$$;
