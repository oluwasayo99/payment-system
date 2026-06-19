CREATE OR REPLACE PROCEDURE process_reservation(
    p_user_id UUID,
    p_amount DECIMAL,
    INOUT p_reservation_id UUID DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_balance DECIMAL;
    v_held_balance DECIMAL;
BEGIN
    SELECT balance, held_balance INTO v_balance, v_held_balance
    FROM wallets
    WHERE user_id = p_user_id
    FOR UPDATE;

    IF v_balance IS NULL THEN
        RAISE EXCEPTION 'Wallet not found';
    END IF;

    IF (v_balance - v_held_balance) < p_amount THEN
        RAISE EXCEPTION 'Insufficient funds available';
    END IF;

    UPDATE wallets
    SET held_balance = held_balance + p_amount
    WHERE user_id = p_user_id;

    INSERT INTO reservations (user_id, amount, status)
    VALUES (p_user_id, p_amount, 'PENDING')
    RETURNING id INTO p_reservation_id;
END;
$$;
