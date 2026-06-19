CREATE OR REPLACE PROCEDURE settle_reservation(
    p_reservation_id UUID
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_user_id UUID;
    v_amount DECIMAL;
    v_status VARCHAR;
BEGIN
    SELECT user_id, amount, status INTO v_user_id, v_amount, v_status 
    FROM reservations
    WHERE id = p_reservation_id FOR UPDATE;

    IF v_status IS NULL THEN 
        RAISE EXCEPTION 'Reservation not found';
    END IF;
    
    IF v_status != 'PENDING' THEN 
        RAISE EXCEPTION 'Cannot settle. Status is %', v_status;
    END IF;

    UPDATE wallets
    SET balance = balance - v_amount,
        held_balance = held_balance - v_amount
    WHERE user_id = v_user_id;

    UPDATE reservations
    SET status = 'SETTLED'
    WHERE id = p_reservation_id;
END;
$$;
