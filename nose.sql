--
-- PostgreSQL database dump
--

-- Dumped from database version 17.6 (Debian 17.6-2.pgdg13+1)
-- Dumped by pg_dump version 17.5

-- Started on 2025-12-14 06:53:16

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- TOC entry 336 (class 1255 OID 16600)
-- Name: actualizar_fecha_modificacion(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.actualizar_fecha_modificacion() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    NEW.fecha_modificacion = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$;


ALTER FUNCTION public.actualizar_fecha_modificacion() OWNER TO postgres;

--
-- TOC entry 346 (class 1255 OID 17260)
-- Name: actualizar_fecha_modificacion_admin(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.actualizar_fecha_modificacion_admin() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    NEW.fecha_modificacion = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$;


ALTER FUNCTION public.actualizar_fecha_modificacion_admin() OWNER TO postgres;

--
-- TOC entry 344 (class 1255 OID 18041)
-- Name: actualizar_fecha_modificacion_beneficiario(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.actualizar_fecha_modificacion_beneficiario() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    NEW.fecha_modificacion = CURRENT_TIMESTAMP AT TIME ZONE 'Europe/Madrid';
    RETURN NEW;
END;
$$;


ALTER FUNCTION public.actualizar_fecha_modificacion_beneficiario() OWNER TO postgres;

--
-- TOC entry 341 (class 1255 OID 17546)
-- Name: actualizar_fecha_modificacion_config(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.actualizar_fecha_modificacion_config() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    NEW.fecha_modificacion = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$;


ALTER FUNCTION public.actualizar_fecha_modificacion_config() OWNER TO postgres;

--
-- TOC entry 342 (class 1255 OID 17932)
-- Name: actualizar_fecha_modificacion_pack(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.actualizar_fecha_modificacion_pack() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    NEW.fecha_modificacion = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$;


ALTER FUNCTION public.actualizar_fecha_modificacion_pack() OWNER TO postgres;

--
-- TOC entry 339 (class 1255 OID 17363)
-- Name: asignar_modulos_por_nivel(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.asignar_modulos_por_nivel() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    -- Si es nivel 3 o 4, asignar todos los módulos automáticamente
    IF NEW.nivel_acceso IN (3, 4) THEN
        -- Eliminar módulos existentes
        DELETE FROM admin_modulos_habilitados WHERE id_administrador = NEW.id_administrador;
        
        -- Insertar todos los módulos
        INSERT INTO admin_modulos_habilitados (id_administrador, nombre_modulo)
        VALUES 
            (NEW.id_administrador, 'compra_divisa'),
            (NEW.id_administrador, 'packs_alimentos'),
            (NEW.id_administrador, 'billetes_avion'),
            (NEW.id_administrador, 'pack_viajes')
        ON CONFLICT (id_administrador, nombre_modulo) DO NOTHING;
    END IF;
    
    RETURN NEW;
END;
$$;


ALTER FUNCTION public.asignar_modulos_por_nivel() OWNER TO postgres;

--
-- TOC entry 369 (class 1255 OID 17662)
-- Name: buscar_clientes_comercio(integer, character varying, integer); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.buscar_clientes_comercio(p_id_comercio integer, p_termino_busqueda character varying DEFAULT NULL::character varying, p_limite integer DEFAULT 50) RETURNS TABLE(id_cliente integer, nombre character varying, segundo_nombre character varying, apellidos character varying, segundo_apellido character varying, telefono character varying, correo character varying, documento_tipo character varying, documento_numero character varying, nacionalidad character varying, nombre_completo text)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT 
        c.id_cliente,
        c.nombre::VARCHAR,
        c.segundo_nombre::VARCHAR,
        c.apellidos::VARCHAR,
        c.segundo_apellido::VARCHAR,
        c.telefono::VARCHAR,
        c.correo::VARCHAR,
        c.documento_tipo::VARCHAR,
        c.documento_numero::VARCHAR,
        c.nacionalidad::VARCHAR,
        CONCAT_WS(' ', c.nombre, c.segundo_nombre, c.apellidos, c.segundo_apellido)::TEXT as nombre_completo
    FROM clientes c
    WHERE c.activo = true
      AND c.id_comercio_registro = p_id_comercio
      AND (p_termino_busqueda IS NULL 
           OR c.nombre ILIKE '%' || p_termino_busqueda || '%'
           OR c.apellidos ILIKE '%' || p_termino_busqueda || '%'
           OR COALESCE(c.segundo_nombre, '') ILIKE '%' || p_termino_busqueda || '%'
           OR COALESCE(c.segundo_apellido, '') ILIKE '%' || p_termino_busqueda || '%'
           OR c.documento_numero ILIKE '%' || p_termino_busqueda || '%'
           OR c.telefono ILIKE '%' || p_termino_busqueda || '%')
    ORDER BY c.nombre, c.apellidos
    LIMIT p_limite;
END;
$$;


ALTER FUNCTION public.buscar_clientes_comercio(p_id_comercio integer, p_termino_busqueda character varying, p_limite integer) OWNER TO postgres;

--
-- TOC entry 4468 (class 0 OID 0)
-- Dependencies: 369
-- Name: FUNCTION buscar_clientes_comercio(p_id_comercio integer, p_termino_busqueda character varying, p_limite integer); Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON FUNCTION public.buscar_clientes_comercio(p_id_comercio integer, p_termino_busqueda character varying, p_limite integer) IS 'Busca clientes de un comercio especifico. Los clientes son unicos por comercio.';


--
-- TOC entry 365 (class 1255 OID 17224)
-- Name: buscar_comercios_locales(character varying, character varying, boolean); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.buscar_comercios_locales(p_termino_busqueda character varying DEFAULT NULL::character varying, p_pais character varying DEFAULT NULL::character varying, p_solo_activos boolean DEFAULT NULL::boolean) RETURNS TABLE(tipo character varying, id integer, nombre character varying, info text, activo boolean, id_comercio integer)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    -- Búsqueda en comercios
    SELECT 
        'comercio'::VARCHAR AS tipo,
        c.id_comercio AS id,
        c.nombre_comercio AS nombre,
        c.mail_contacto || ' | ' || COALESCE(c.numero_contacto, 'Sin teléfono') AS info,
        c.activo,
        c.id_comercio
    FROM comercios c
    WHERE (p_termino_busqueda IS NULL OR 
           c.nombre_comercio ILIKE '%' || p_termino_busqueda || '%' OR
           COALESCE(c.nombre_srl, '') ILIKE '%' || p_termino_busqueda || '%' OR
           c.mail_contacto ILIKE '%' || p_termino_busqueda || '%')
      AND (p_pais IS NULL OR c.pais = p_pais)
      AND (p_solo_activos IS NULL OR c.activo = p_solo_activos)
    
    UNION ALL
    
    -- Búsqueda en locales
    SELECT 
        'local'::VARCHAR AS tipo,
        l.id_local AS id,
        l.nombre_local AS nombre,
        l.codigo_local || ' | ' || COALESCE(l.email, 'Sin email') AS info,
        l.activo,
        l.id_comercio
    FROM locales l
    WHERE (p_termino_busqueda IS NULL OR 
           l.nombre_local ILIKE '%' || p_termino_busqueda || '%' OR
           l.codigo_local ILIKE '%' || p_termino_busqueda || '%' OR
           COALESCE(l.email, '') ILIKE '%' || p_termino_busqueda || '%')
      AND (p_pais IS NULL OR COALESCE(l.pais, '') = p_pais)
      AND (p_solo_activos IS NULL OR l.activo = p_solo_activos)
    
    ORDER BY tipo, nombre;
END;
$$;


ALTER FUNCTION public.buscar_comercios_locales(p_termino_busqueda character varying, p_pais character varying, p_solo_activos boolean) OWNER TO postgres;

--
-- TOC entry 4469 (class 0 OID 0)
-- Dependencies: 365
-- Name: FUNCTION buscar_comercios_locales(p_termino_busqueda character varying, p_pais character varying, p_solo_activos boolean); Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON FUNCTION public.buscar_comercios_locales(p_termino_busqueda character varying, p_pais character varying, p_solo_activos boolean) IS 'Función para buscar comercios y locales con filtros opcionales';


--
-- TOC entry 360 (class 1255 OID 17158)
-- Name: crear_notificacion_seguridad(integer, character varying, character varying, text, character varying, jsonb); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.crear_notificacion_seguridad(p_id_usuario integer, p_tipo character varying, p_titulo character varying, p_mensaje text, p_severidad character varying DEFAULT 'INFO'::character varying, p_metadata jsonb DEFAULT NULL::jsonb) RETURNS integer
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_id_notificacion INTEGER;
BEGIN
    INSERT INTO notificaciones_seguridad (
        id_usuario,
        tipo,
        titulo,
        mensaje,
        nivel_severidad,
        metadata_json
    ) VALUES (
        p_id_usuario,
        p_tipo,
        p_titulo,
        p_mensaje,
        p_severidad,
        p_metadata
    )
    RETURNING id_notificacion INTO v_id_notificacion;
    
    RETURN v_id_notificacion;
END;
$$;


ALTER FUNCTION public.crear_notificacion_seguridad(p_id_usuario integer, p_tipo character varying, p_titulo character varying, p_mensaje text, p_severidad character varying, p_metadata jsonb) OWNER TO postgres;

--
-- TOC entry 368 (class 1255 OID 17474)
-- Name: generar_codigo_licencia(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.generar_codigo_licencia() RETURNS character varying
    LANGUAGE plpgsql
    AS $$
DECLARE
    nuevo_codigo VARCHAR(50);
    existe BOOLEAN;
BEGIN
    LOOP
        nuevo_codigo := 'ALLVA-' || 
                       LPAD(FLOOR(RANDOM() * 10000)::TEXT, 4, '0') || '-' ||
                       LPAD(FLOOR(RANDOM() * 10000)::TEXT, 4, '0') || '-' ||
                       LPAD(FLOOR(RANDOM() * 10000)::TEXT, 4, '0');
        
        SELECT EXISTS(
            SELECT 1 FROM licencias WHERE codigo_licencia = nuevo_codigo
        ) INTO existe;
        
        IF NOT existe THEN
            EXIT;
        END IF;
    END LOOP;
    
    RETURN nuevo_codigo;
END;
$$;


ALTER FUNCTION public.generar_codigo_licencia() OWNER TO postgres;

--
-- TOC entry 364 (class 1255 OID 17213)
-- Name: generar_codigo_local_unico(character varying); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.generar_codigo_local_unico(p_nombre_comercio character varying) RETURNS character varying
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_prefijo VARCHAR(3);
    v_numero VARCHAR(4);
    v_codigo VARCHAR(7);
    v_existe BOOLEAN;
    v_intentos INTEGER := 0;
    v_digito1 INTEGER;
    v_digito2 INTEGER;
    v_digito3 INTEGER;
    v_digito4 INTEGER;
BEGIN
    -- Generar prefijo de 3 letras del nombre del comercio
    v_prefijo := UPPER(LEFT(REGEXP_REPLACE(p_nombre_comercio, '[^A-Za-z]', '', 'g'), 3));
    
    -- Si no hay suficientes letras, rellenar con 'X'
    WHILE LENGTH(v_prefijo) < 3 LOOP
        v_prefijo := v_prefijo || 'X';
    END LOOP;
    
    -- Generar número único de 4 dígitos DIFERENTES
    LOOP
        -- Generar 4 dígitos aleatorios distintos entre sí
        v_digito1 := FLOOR(RANDOM() * 10)::INTEGER;
        
        -- Segundo dígito diferente al primero
        LOOP
            v_digito2 := FLOOR(RANDOM() * 10)::INTEGER;
            EXIT WHEN v_digito2 != v_digito1;
        END LOOP;
        
        -- Tercer dígito diferente a los dos primeros
        LOOP
            v_digito3 := FLOOR(RANDOM() * 10)::INTEGER;
            EXIT WHEN v_digito3 != v_digito1 AND v_digito3 != v_digito2;
        END LOOP;
        
        -- Cuarto dígito diferente a los tres anteriores
        LOOP
            v_digito4 := FLOOR(RANDOM() * 10)::INTEGER;
            EXIT WHEN v_digito4 != v_digito1 AND v_digito4 != v_digito2 AND v_digito4 != v_digito3;
        END LOOP;
        
        v_numero := v_digito1::TEXT || v_digito2::TEXT || v_digito3::TEXT || v_digito4::TEXT;
        v_codigo := v_prefijo || v_numero;
        
        -- Verificar que no exista este código completo en la base de datos
        SELECT EXISTS(SELECT 1 FROM locales WHERE codigo_local = v_codigo) INTO v_existe;
        
        IF NOT v_existe THEN
            -- También verificar que no exista el número en ningún otro local (de cualquier comercio)
            SELECT EXISTS(
                SELECT 1 FROM locales 
                WHERE RIGHT(codigo_local, 4) = v_numero
            ) INTO v_existe;
            
            IF NOT v_existe THEN
                RETURN v_codigo;
            END IF;
        END IF;
        
        v_intentos := v_intentos + 1;
        
        -- Evitar bucle infinito
        IF v_intentos > 10000 THEN
            RAISE EXCEPTION 'No se pudo generar un código único después de 10000 intentos';
        END IF;
    END LOOP;
END;
$$;


ALTER FUNCTION public.generar_codigo_local_unico(p_nombre_comercio character varying) OWNER TO postgres;

--
-- TOC entry 4470 (class 0 OID 0)
-- Dependencies: 364
-- Name: FUNCTION generar_codigo_local_unico(p_nombre_comercio character varying); Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON FUNCTION public.generar_codigo_local_unico(p_nombre_comercio character varying) IS 'Genera un código único para un local: 3 letras + 4 dígitos distintos entre sí y únicos en la BD';


--
-- TOC entry 366 (class 1255 OID 17262)
-- Name: generar_nombre_usuario_admin(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.generar_nombre_usuario_admin() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
DECLARE
    base_username TEXT;
    final_username TEXT;
    counter INTEGER := 1;
BEGIN
    -- Si ya tiene nombre_usuario, no hacer nada
    IF NEW.nombre_usuario IS NOT NULL AND NEW.nombre_usuario != '' THEN
        RETURN NEW;
    END IF;
    
    -- Generar nombre base: nombre_apellidos (sin espacios, minúsculas)
    base_username := LOWER(
        REGEXP_REPLACE(
            CONCAT(NEW.nombre, '_', SPLIT_PART(NEW.apellidos, ' ', 1)),
            '[^a-z0-9_]',
            '',
            'g'
        )
    );
    
    final_username := base_username;
    
    -- Si existe, agregar número
    WHILE EXISTS (SELECT 1 FROM administradores_allva WHERE nombre_usuario = final_username) LOOP
        counter := counter + 1;
        final_username := base_username || '_' || LPAD(counter::TEXT, 2, '0');
    END LOOP;
    
    NEW.nombre_usuario := final_username;
    RETURN NEW;
END;
$$;


ALTER FUNCTION public.generar_nombre_usuario_admin() OWNER TO postgres;

--
-- TOC entry 345 (class 1255 OID 16888)
-- Name: generar_numero_operacion(character varying, integer); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.generar_numero_operacion(p_modulo character varying, p_id_local integer) RETURNS character varying
    LANGUAGE plpgsql
    AS $_$
DECLARE
    v_prefijo VARCHAR(5);
    v_contador INTEGER;
    v_numero VARCHAR(50);
BEGIN
    -- Definir prefijo según módulo
    v_prefijo := CASE p_modulo
        WHEN 'DIVISAS' THEN 'D'
        WHEN 'BILLETES' THEN 'B'
        WHEN 'PACK_ALIMENTOS' THEN 'PA'
        WHEN 'PACK_VIAJES' THEN 'PV'
        ELSE 'OP'
    END;
    
    -- Obtener contador (último número + 1)
    SELECT COALESCE(MAX(
        CAST(SUBSTRING(numero_operacion FROM '[0-9]+$') AS INTEGER)
    ), 0) + 1
    INTO v_contador
    FROM operaciones
    WHERE modulo = p_modulo
      AND id_local = p_id_local
      AND EXTRACT(YEAR FROM fecha_operacion) = EXTRACT(YEAR FROM CURRENT_DATE);
    
    -- Generar número
    v_numero := v_prefijo || LPAD(v_contador::TEXT, 4, '0');
    
    RETURN v_numero;
END;
$_$;


ALTER FUNCTION public.generar_numero_operacion(p_modulo character varying, p_id_local integer) OWNER TO postgres;

--
-- TOC entry 370 (class 1255 OID 17935)
-- Name: obtener_packs_disponibles_local(integer); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.obtener_packs_disponibles_local(p_id_local integer) RETURNS TABLE(id_pack integer, nombre_pack character varying, descripcion text, divisa character varying, precio numeric, tipo_asignacion character varying)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_id_comercio INTEGER;
BEGIN
    -- Obtener el comercio del local
    SELECT id_comercio INTO v_id_comercio FROM locales WHERE id_local = p_id_local;
    
    RETURN QUERY
    -- Packs asignados globalmente
    SELECT 
        p.id_pack,
        p.nombre_pack,
        p.descripcion,
        pr.divisa,
        pr.precio,
        'global'::VARCHAR as tipo_asignacion
    FROM packs_alimentos p
    INNER JOIN pack_alimentos_asignacion_global ag ON p.id_pack = ag.id_pack
    INNER JOIN pack_alimentos_precios pr ON ag.id_precio = pr.id_precio
    WHERE p.activo = true AND ag.activo = true AND pr.activo = true
    
    UNION
    
    -- Packs asignados al comercio
    SELECT 
        p.id_pack,
        p.nombre_pack,
        p.descripcion,
        pr.divisa,
        pr.precio,
        'comercio'::VARCHAR as tipo_asignacion
    FROM packs_alimentos p
    INNER JOIN pack_alimentos_asignacion_comercios ac ON p.id_pack = ac.id_pack
    INNER JOIN pack_alimentos_precios pr ON ac.id_precio = pr.id_precio
    WHERE p.activo = true 
      AND ac.activo = true 
      AND pr.activo = true
      AND ac.id_comercio = v_id_comercio
    
    UNION
    
    -- Packs asignados al local especifico
    SELECT 
        p.id_pack,
        p.nombre_pack,
        p.descripcion,
        pr.divisa,
        pr.precio,
        'local'::VARCHAR as tipo_asignacion
    FROM packs_alimentos p
    INNER JOIN pack_alimentos_asignacion_locales al ON p.id_pack = al.id_pack
    INNER JOIN pack_alimentos_precios pr ON al.id_precio = pr.id_precio
    WHERE p.activo = true 
      AND al.activo = true 
      AND pr.activo = true
      AND al.id_local = p_id_local;
END;
$$;


ALTER FUNCTION public.obtener_packs_disponibles_local(p_id_local integer) OWNER TO postgres;

--
-- TOC entry 4471 (class 0 OID 0)
-- Dependencies: 370
-- Name: FUNCTION obtener_packs_disponibles_local(p_id_local integer); Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON FUNCTION public.obtener_packs_disponibles_local(p_id_local integer) IS 'Obtiene todos los packs disponibles para un local especifico';


--
-- TOC entry 361 (class 1255 OID 17159)
-- Name: password_fue_usado(integer, character varying); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.password_fue_usado(p_id_usuario integer, p_password_hash character varying) RETURNS boolean
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_num_historial INTEGER;
    v_encontrado BOOLEAN;
BEGIN
    -- Obtener configuración de historial
    SELECT cs.password_historial_no_repetir
    INTO v_num_historial
    FROM usuarios u
    JOIN configuracion_seguridad cs ON u.id_comercio = cs.id_comercio
    WHERE u.id_usuario = p_id_usuario;
    
    -- Si no hay configuración, permitir cualquier password
    IF v_num_historial IS NULL OR v_num_historial = 0 THEN
        RETURN FALSE;
    END IF;
    
    -- Verificar en historial
    SELECT EXISTS (
        SELECT 1 
        FROM historial_passwords
        WHERE id_usuario = p_id_usuario
          AND password_hash = p_password_hash
        ORDER BY fecha_cambio DESC
        LIMIT v_num_historial
    ) INTO v_encontrado;
    
    RETURN v_encontrado;
END;
$$;


ALTER FUNCTION public.password_fue_usado(p_id_usuario integer, p_password_hash character varying) OWNER TO postgres;

--
-- TOC entry 337 (class 1255 OID 16615)
-- Name: registrar_auditoria(integer, character varying, character varying, character varying, integer, text, jsonb, jsonb, character varying); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.registrar_auditoria(p_id_usuario integer, p_accion character varying, p_modulo character varying, p_entidad character varying, p_id_entidad integer, p_descripcion text, p_datos_anteriores jsonb DEFAULT NULL::jsonb, p_datos_nuevos jsonb DEFAULT NULL::jsonb, p_ip character varying DEFAULT NULL::character varying) RETURNS void
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_numero_usuario VARCHAR;
    v_nombre_usuario VARCHAR;
    v_id_comercio INTEGER;
    v_id_local INTEGER;
    v_codigo_local VARCHAR;
BEGIN
    -- Obtener datos del usuario
    SELECT 
        numero_usuario,
        nombre || ' ' || apellidos,
        id_comercio,
        id_local
    INTO 
        v_numero_usuario,
        v_nombre_usuario,
        v_id_comercio,
        v_id_local
    FROM usuarios
    WHERE id_usuario = p_id_usuario;
    
    -- Obtener código de local
    SELECT codigo_local INTO v_codigo_local
    FROM locales
    WHERE id_local = v_id_local;
    
    -- Insertar en audit_log
    INSERT INTO audit_log (
        id_usuario,
        numero_usuario,
        nombre_usuario,
        id_comercio,
        id_local,
        codigo_local,
        accion,
        modulo,
        entidad,
        id_entidad,
        descripcion,
        datos_anteriores,
        datos_nuevos,
        ip_address,
        exitoso
    ) VALUES (
        p_id_usuario,
        v_numero_usuario,
        v_nombre_usuario,
        v_id_comercio,
        v_id_local,
        v_codigo_local,
        p_accion,
        p_modulo,
        p_entidad,
        p_id_entidad,
        p_descripcion,
        p_datos_anteriores,
        p_datos_nuevos,
        p_ip,
        TRUE
    );
END;
$$;


ALTER FUNCTION public.registrar_auditoria(p_id_usuario integer, p_accion character varying, p_modulo character varying, p_entidad character varying, p_id_entidad integer, p_descripcion text, p_datos_anteriores jsonb, p_datos_nuevos jsonb, p_ip character varying) OWNER TO postgres;

--
-- TOC entry 359 (class 1255 OID 17157)
-- Name: registrar_intento_login(character varying, integer, boolean, character varying, character varying, character varying, text, uuid, character varying); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.registrar_intento_login(p_numero_usuario character varying, p_id_usuario integer, p_exitoso boolean, p_motivo_fallo character varying DEFAULT NULL::character varying, p_codigo_local character varying DEFAULT NULL::character varying, p_ip_address character varying DEFAULT NULL::character varying, p_user_agent text DEFAULT NULL::text, p_uuid_dispositivo uuid DEFAULT NULL::uuid, p_mac_address character varying DEFAULT NULL::character varying) RETURNS void
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_es_sospechoso BOOLEAN := FALSE;
    v_razon_sospecha TEXT := '';
BEGIN
    -- Determinar si es sospechoso
    IF NOT p_exitoso THEN
        -- Verificar número de intentos fallidos recientes
        IF (SELECT COUNT(*) FROM intentos_login 
            WHERE numero_usuario = p_numero_usuario 
              AND exitoso = FALSE 
              AND fecha_intento > NOW() - INTERVAL '1 hour') >= 3 THEN
            v_es_sospechoso := TRUE;
            v_razon_sospecha := 'MULTIPLES_INTENTOS_FALLIDOS';
        END IF;
        
        -- Verificar si es IP desconocida
        IF p_id_usuario IS NOT NULL AND 
           NOT EXISTS (SELECT 1 FROM intentos_login 
                       WHERE id_usuario = p_id_usuario 
                         AND ip_address = p_ip_address 
                         AND exitoso = TRUE) THEN
            v_es_sospechoso := TRUE;
            v_razon_sospecha := COALESCE(v_razon_sospecha || ', ', '') || 'IP_DESCONOCIDA';
        END IF;
    END IF;
    
    -- Insertar registro
    INSERT INTO intentos_login (
        numero_usuario,
        id_usuario,
        exitoso,
        motivo_fallo,
        codigo_local,
        ip_address,
        user_agent,
        uuid_dispositivo,
        mac_address,
        es_sospechoso,
        razon_sospecha
    ) VALUES (
        p_numero_usuario,
        p_id_usuario,
        p_exitoso,
        p_motivo_fallo,
        p_codigo_local,
        p_ip_address,
        p_user_agent,
        p_uuid_dispositivo,
        p_mac_address,
        v_es_sospechoso,
        NULLIF(v_razon_sospecha, '')
    );
END;
$$;


ALTER FUNCTION public.registrar_intento_login(p_numero_usuario character varying, p_id_usuario integer, p_exitoso boolean, p_motivo_fallo character varying, p_codigo_local character varying, p_ip_address character varying, p_user_agent text, p_uuid_dispositivo uuid, p_mac_address character varying) OWNER TO postgres;

--
-- TOC entry 362 (class 1255 OID 17160)
-- Name: trigger_archivar_sesion(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.trigger_archivar_sesion() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_duracion INTEGER;
BEGIN
    -- Solo archivar si la sesión se está cerrando
    IF OLD.sesion_activa = TRUE AND NEW.sesion_activa = FALSE THEN
        -- Calcular duración en minutos
        v_duracion := EXTRACT(EPOCH FROM (NEW.fecha_cierre - NEW.fecha_inicio)) / 60;
        
        -- Insertar en histórico
        INSERT INTO sesiones_historico (
            id_sesion_original,
            id_usuario,
            id_local_activo,
            duracion_minutos,
            fecha_inicio,
            fecha_cierre,
            motivo_cierre,
            ip_address,
            user_agent,
            uuid_dispositivo
        ) VALUES (
            NEW.id_sesion,
            NEW.id_usuario,
            NEW.id_local_activo,
            v_duracion,
            NEW.fecha_inicio,
            NEW.fecha_cierre,
            NEW.motivo_cierre,
            NEW.ip_address,
            NEW.user_agent,
            NULL -- Agregar uuid si existe en la tabla sesiones
        );
    END IF;
    
    RETURN NEW;
END;
$$;


ALTER FUNCTION public.trigger_archivar_sesion() OWNER TO postgres;

--
-- TOC entry 343 (class 1255 OID 16871)
-- Name: trigger_balance_creado(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.trigger_balance_creado() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO audit_log (
        id_usuario,
        id_comercio,
        id_local,
        codigo_local,
        accion,
        modulo,
        entidad,
        id_entidad,
        descripcion,
        datos_nuevos
    ) VALUES (
        NEW.id_usuario,
        NEW.id_comercio,
        NEW.id_local,
        NEW.codigo_local,
        'BALANCE',
        NEW.modulo,
        'Balance',
        NEW.id_balance,
        NEW.tipo_movimiento || ': ' || NEW.monto || ' ' || NEW.divisa,
        row_to_json(NEW)
    );
    RETURN NEW;
END;
$$;


ALTER FUNCTION public.trigger_balance_creado() OWNER TO postgres;

--
-- TOC entry 363 (class 1255 OID 17162)
-- Name: trigger_guardar_password_historial(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.trigger_guardar_password_historial() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    -- Solo si cambió la contraseña
    IF OLD.password_hash <> NEW.password_hash THEN
        INSERT INTO historial_passwords (
            id_usuario,
            password_hash,
            motivo
        ) VALUES (
            OLD.id_usuario,
            OLD.password_hash,
            'CAMBIO_USUARIO'
        );
    END IF;
    
    RETURN NEW;
END;
$$;


ALTER FUNCTION public.trigger_guardar_password_historial() OWNER TO postgres;

--
-- TOC entry 338 (class 1255 OID 16867)
-- Name: trigger_operacion_creada(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.trigger_operacion_creada() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    INSERT INTO audit_log (
        id_usuario,
        numero_usuario,
        nombre_usuario,
        id_comercio,
        id_local,
        codigo_local,
        accion,
        modulo,
        entidad,
        id_entidad,
        descripcion,
        datos_nuevos
    ) VALUES (
        NEW.id_usuario,
        NEW.numero_usuario,
        NEW.nombre_usuario,
        NEW.id_comercio,
        NEW.id_local,
        NEW.codigo_local,
        'CREATE',
        NEW.modulo,
        'Operacion',
        NEW.id_operacion,
        'Nueva operación: ' || NEW.numero_operacion || ' - ' || NEW.modulo,
        row_to_json(NEW)
    );
    RETURN NEW;
END;
$$;


ALTER FUNCTION public.trigger_operacion_creada() OWNER TO postgres;

--
-- TOC entry 340 (class 1255 OID 16869)
-- Name: trigger_operacion_modificada(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.trigger_operacion_modificada() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    -- Solo registrar si cambió el estado
    IF OLD.estado <> NEW.estado THEN
        INSERT INTO audit_log (
            id_usuario,
            numero_usuario,
            nombre_usuario,
            id_comercio,
            id_local,
            codigo_local,
            accion,
            modulo,
            entidad,
            id_entidad,
            descripcion,
            datos_anteriores,
            datos_nuevos
        ) VALUES (
            NEW.id_usuario,
            NEW.numero_usuario,
            NEW.nombre_usuario,
            NEW.id_comercio,
            NEW.id_local,
            NEW.codigo_local,
            'UPDATE',
            NEW.modulo,
            'Operacion',
            NEW.id_operacion,
            'Cambio de estado: ' || OLD.estado || ' → ' || NEW.estado,
            jsonb_build_object('estado', OLD.estado),
            jsonb_build_object('estado', NEW.estado)
        );
    END IF;
    RETURN NEW;
END;
$$;


ALTER FUNCTION public.trigger_operacion_modificada() OWNER TO postgres;

--
-- TOC entry 367 (class 1255 OID 17311)
-- Name: trigger_registrar_cambio_usuario(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.trigger_registrar_cambio_usuario() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    -- Registrar cada campo que cambió
    IF OLD.correo IS DISTINCT FROM NEW.correo THEN
        INSERT INTO cambios_usuarios (id_usuario, campo_modificado, valor_anterior, valor_nuevo)
        VALUES (NEW.id_usuario, 'correo', OLD.correo, NEW.correo);
    END IF;
    
    IF OLD.id_rol IS DISTINCT FROM NEW.id_rol THEN
        INSERT INTO cambios_usuarios (id_usuario, campo_modificado, valor_anterior, valor_nuevo)
        VALUES (NEW.id_usuario, 'id_rol', OLD.id_rol::TEXT, NEW.id_rol::TEXT);
    END IF;
    
    IF OLD.activo IS DISTINCT FROM NEW.activo THEN
        INSERT INTO cambios_usuarios (id_usuario, campo_modificado, valor_anterior, valor_nuevo)
        VALUES (NEW.id_usuario, 'activo', OLD.activo::TEXT, NEW.activo::TEXT);
    END IF;
    
    -- ✅ CORRECCIÓN: Cambiar es_flotante por es_flooter
    IF OLD.es_flooter IS DISTINCT FROM NEW.es_flooter THEN
        INSERT INTO cambios_usuarios (id_usuario, campo_modificado, valor_anterior, valor_nuevo)
        VALUES (NEW.id_usuario, 'es_flooter', OLD.es_flooter::TEXT, NEW.es_flooter::TEXT);
    END IF;
    
    RETURN NEW;
END;
$$;


ALTER FUNCTION public.trigger_registrar_cambio_usuario() OWNER TO postgres;

--
-- TOC entry 358 (class 1255 OID 17156)
-- Name: verificar_dispositivo_autorizado(integer, uuid, character varying); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.verificar_dispositivo_autorizado(p_id_usuario integer, p_uuid_dispositivo uuid, p_mac_address character varying DEFAULT NULL::character varying) RETURNS boolean
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_autorizado BOOLEAN;
    v_requiere_autorizacion BOOLEAN;
BEGIN
    -- Verificar si el comercio requiere dispositivos autorizados
    SELECT cs.login_requiere_dispositivo_autorizado
    INTO v_requiere_autorizacion
    FROM usuarios u
    JOIN configuracion_seguridad cs ON u.id_comercio = cs.id_comercio
    WHERE u.id_usuario = p_id_usuario;
    
    -- Si no requiere autorización, permitir
    IF NOT v_requiere_autorizacion THEN
        RETURN TRUE;
    END IF;
    
    -- Verificar si el dispositivo está autorizado
    SELECT autorizado
    INTO v_autorizado
    FROM dispositivos_autorizados
    WHERE id_usuario = p_id_usuario
      AND uuid_dispositivo = p_uuid_dispositivo
      AND activo = TRUE
      AND (p_mac_address IS NULL OR mac_address = p_mac_address);
    
    RETURN COALESCE(v_autorizado, FALSE);
END;
$$;


ALTER FUNCTION public.verificar_dispositivo_autorizado(p_id_usuario integer, p_uuid_dispositivo uuid, p_mac_address character varying) OWNER TO postgres;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- TOC entry 286 (class 1259 OID 17326)
-- Name: admin_modulos_habilitados; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.admin_modulos_habilitados (
    id_admin_modulo integer NOT NULL,
    id_administrador integer NOT NULL,
    nombre_modulo character varying(50) NOT NULL,
    fecha_asignacion timestamp without time zone DEFAULT CURRENT_TIMESTAMP
);


ALTER TABLE public.admin_modulos_habilitados OWNER TO postgres;

--
-- TOC entry 4472 (class 0 OID 0)
-- Dependencies: 286
-- Name: TABLE admin_modulos_habilitados; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON TABLE public.admin_modulos_habilitados IS 'Módulos habilitados específicos para cada administrador';


--
-- TOC entry 4473 (class 0 OID 0)
-- Dependencies: 286
-- Name: COLUMN admin_modulos_habilitados.nombre_modulo; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON COLUMN public.admin_modulos_habilitados.nombre_modulo IS 'Nombre del módulo: compra_divisa, packs_alimentos, billetes_avion, pack_viajes';


--
-- TOC entry 285 (class 1259 OID 17325)
-- Name: admin_modulos_habilitados_id_admin_modulo_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.admin_modulos_habilitados_id_admin_modulo_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.admin_modulos_habilitados_id_admin_modulo_seq OWNER TO postgres;

--
-- TOC entry 4474 (class 0 OID 0)
-- Dependencies: 285
-- Name: admin_modulos_habilitados_id_admin_modulo_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.admin_modulos_habilitados_id_admin_modulo_seq OWNED BY public.admin_modulos_habilitados.id_admin_modulo;


--
-- TOC entry 278 (class 1259 OID 17231)
-- Name: administradores_allva; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.administradores_allva (
    id_administrador integer NOT NULL,
    nombre character varying(100) NOT NULL,
    apellidos character varying(100) NOT NULL,
    nombre_usuario character varying(150) NOT NULL,
    password_hash text NOT NULL,
    correo character varying(150) NOT NULL,
    telefono character varying(20),
    acceso_gestion_comercios boolean DEFAULT true,
    acceso_gestion_usuarios_locales boolean DEFAULT true,
    acceso_gestion_usuarios_allva boolean DEFAULT false,
    acceso_analytics boolean DEFAULT false,
    acceso_configuracion_sistema boolean DEFAULT false,
    acceso_facturacion_global boolean DEFAULT false,
    acceso_auditoria boolean DEFAULT false,
    activo boolean DEFAULT true,
    primer_login boolean DEFAULT true,
    intentos_fallidos integer DEFAULT 0,
    bloqueado_hasta timestamp without time zone,
    ultimo_acceso timestamp without time zone,
    fecha_creacion timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    fecha_modificacion timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    creado_por character varying(150),
    idioma character varying(2) DEFAULT 'es'::character varying,
    nivel_acceso integer DEFAULT 1,
    CONSTRAINT chk_nombre_usuario_minusculas CHECK (((nombre_usuario)::text = lower((nombre_usuario)::text)))
);


ALTER TABLE public.administradores_allva OWNER TO postgres;

--
-- TOC entry 4475 (class 0 OID 0)
-- Dependencies: 278
-- Name: TABLE administradores_allva; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON TABLE public.administradores_allva IS 'Administradores del sistema Allva - Back Office. Completamente separados de usuarios de locales.';


--
-- TOC entry 4476 (class 0 OID 0)
-- Dependencies: 278
-- Name: COLUMN administradores_allva.nombre_usuario; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON COLUMN public.administradores_allva.nombre_usuario IS 'Formato: nombre_apellidos (ej: jose_noble) - TODO en minúsculas';


--
-- TOC entry 4477 (class 0 OID 0)
-- Dependencies: 278
-- Name: COLUMN administradores_allva.password_hash; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON COLUMN public.administradores_allva.password_hash IS 'Hash BCrypt de la contraseña';


--
-- TOC entry 4478 (class 0 OID 0)
-- Dependencies: 278
-- Name: COLUMN administradores_allva.acceso_gestion_usuarios_allva; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON COLUMN public.administradores_allva.acceso_gestion_usuarios_allva IS 'Permiso para crear/editar otros administradores - Solo super admins';


--
-- TOC entry 277 (class 1259 OID 17230)
-- Name: administradores_allva_id_administrador_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.administradores_allva_id_administrador_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.administradores_allva_id_administrador_seq OWNER TO postgres;

--
-- TOC entry 4479 (class 0 OID 0)
-- Dependencies: 277
-- Name: administradores_allva_id_administrador_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.administradores_allva_id_administrador_seq OWNED BY public.administradores_allva.id_administrador;


--
-- TOC entry 232 (class 1259 OID 16568)
-- Name: audit_log; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.audit_log (
    id_log bigint NOT NULL,
    id_usuario integer,
    numero_usuario character varying(50),
    nombre_usuario character varying(200),
    id_comercio integer,
    id_local integer,
    codigo_local character varying(50),
    accion character varying(100) NOT NULL,
    modulo character varying(100),
    entidad character varying(100),
    id_entidad integer,
    descripcion text,
    datos_anteriores jsonb,
    datos_nuevos jsonb,
    fecha_hora timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    ip_address character varying(50),
    user_agent text,
    exitoso boolean DEFAULT true,
    mensaje_error text
);


ALTER TABLE public.audit_log OWNER TO postgres;

--
-- TOC entry 4480 (class 0 OID 0)
-- Dependencies: 232
-- Name: TABLE audit_log; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON TABLE public.audit_log IS 'Registro completo de auditoría de todas las acciones';


--
-- TOC entry 231 (class 1259 OID 16567)
-- Name: audit_log_id_log_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.audit_log_id_log_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.audit_log_id_log_seq OWNER TO postgres;

--
-- TOC entry 4481 (class 0 OID 0)
-- Dependencies: 231
-- Name: audit_log_id_log_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.audit_log_id_log_seq OWNED BY public.audit_log.id_log;


--
-- TOC entry 246 (class 1259 OID 16756)
-- Name: balance_cuentas; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.balance_cuentas (
    id_balance bigint NOT NULL,
    id_comercio integer NOT NULL,
    id_local integer NOT NULL,
    codigo_local character varying(50) NOT NULL,
    id_usuario integer NOT NULL,
    id_operacion bigint,
    numero_operacion character varying(50),
    tipo_movimiento character varying(50) NOT NULL,
    modulo character varying(50),
    descripcion text NOT NULL,
    divisa character varying(10) DEFAULT 'EUR'::character varying NOT NULL,
    monto numeric(12,2) NOT NULL,
    beneficio numeric(12,2) DEFAULT 0.00,
    balance_anterior numeric(12,2),
    balance_nuevo numeric(12,2),
    banco_destino character varying(100),
    numero_cuenta character varying(50),
    fecha_movimiento timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    motivo text,
    observaciones text
);


ALTER TABLE public.balance_cuentas OWNER TO postgres;

--
-- TOC entry 4482 (class 0 OID 0)
-- Dependencies: 246
-- Name: TABLE balance_cuentas; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON TABLE public.balance_cuentas IS 'Movimientos de balance y beneficios por local';


--
-- TOC entry 245 (class 1259 OID 16755)
-- Name: balance_cuentas_id_balance_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.balance_cuentas_id_balance_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.balance_cuentas_id_balance_seq OWNER TO postgres;

--
-- TOC entry 4483 (class 0 OID 0)
-- Dependencies: 245
-- Name: balance_cuentas_id_balance_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.balance_cuentas_id_balance_seq OWNED BY public.balance_cuentas.id_balance;


--
-- TOC entry 302 (class 1259 OID 17674)
-- Name: balance_divisas; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.balance_divisas (
    id_balance integer NOT NULL,
    id_comercio integer NOT NULL,
    id_local integer NOT NULL,
    id_usuario integer,
    id_operacion bigint,
    codigo_divisa character varying(10) NOT NULL,
    nombre_divisa character varying(100),
    cantidad_recibida numeric(18,4) NOT NULL,
    cantidad_entregada_eur numeric(18,4) NOT NULL,
    tasa_cambio_momento numeric(18,6) NOT NULL,
    tasa_cambio_aplicada numeric(18,6) NOT NULL,
    tipo_movimiento character varying(20) DEFAULT 'ENTRADA'::character varying NOT NULL,
    fecha_registro timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    observaciones text
);


ALTER TABLE public.balance_divisas OWNER TO postgres;

--
-- TOC entry 4484 (class 0 OID 0)
-- Dependencies: 302
-- Name: TABLE balance_divisas; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON TABLE public.balance_divisas IS 'Registro de divisas recibidas/entregadas por cada comercio y local';


--
-- TOC entry 4485 (class 0 OID 0)
-- Dependencies: 302
-- Name: COLUMN balance_divisas.tasa_cambio_momento; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON COLUMN public.balance_divisas.tasa_cambio_momento IS 'Tasa de cambio real del mercado al momento de la operación';


--
-- TOC entry 4486 (class 0 OID 0)
-- Dependencies: 302
-- Name: COLUMN balance_divisas.tasa_cambio_aplicada; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON COLUMN public.balance_divisas.tasa_cambio_aplicada IS 'Tasa de cambio con margen de ganancia aplicada';


--
-- TOC entry 4487 (class 0 OID 0)
-- Dependencies: 302
-- Name: COLUMN balance_divisas.tipo_movimiento; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON COLUMN public.balance_divisas.tipo_movimiento IS 'ENTRADA = compra de divisa al cliente, SALIDA = venta futura';


--
-- TOC entry 301 (class 1259 OID 17673)
-- Name: balance_divisas_id_balance_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.balance_divisas_id_balance_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.balance_divisas_id_balance_seq OWNER TO postgres;

--
-- TOC entry 4488 (class 0 OID 0)
-- Dependencies: 301
-- Name: balance_divisas_id_balance_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.balance_divisas_id_balance_seq OWNED BY public.balance_divisas.id_balance;


--
-- TOC entry 267 (class 1259 OID 17057)
-- Name: cambios_usuarios; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.cambios_usuarios (
    id_cambio bigint NOT NULL,
    id_usuario integer NOT NULL,
    modificado_por integer,
    nombre_modificador character varying(200),
    campo_modificado character varying(100) NOT NULL,
    valor_anterior text,
    valor_nuevo text,
    fecha_cambio timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    ip_address character varying(50),
    motivo text,
    requirio_aprobacion boolean DEFAULT false,
    aprobado_por integer
);


ALTER TABLE public.cambios_usuarios OWNER TO postgres;

--
-- TOC entry 4489 (class 0 OID 0)
-- Dependencies: 267
-- Name: TABLE cambios_usuarios; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON TABLE public.cambios_usuarios IS 'Auditoría detallada de cambios en usuarios';


--
-- TOC entry 266 (class 1259 OID 17056)
-- Name: cambios_usuarios_id_cambio_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.cambios_usuarios_id_cambio_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.cambios_usuarios_id_cambio_seq OWNER TO postgres;

--
-- TOC entry 4490 (class 0 OID 0)
-- Dependencies: 266
-- Name: cambios_usuarios_id_cambio_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.cambios_usuarios_id_cambio_seq OWNED BY public.cambios_usuarios.id_cambio;


--
-- TOC entry 311 (class 1259 OID 17765)
-- Name: cierres_dia; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.cierres_dia (
    id_cierre integer NOT NULL,
    id_comercio integer NOT NULL,
    id_local integer NOT NULL,
    fecha_cierre timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    beneficio_dia numeric(12,2) DEFAULT 0 NOT NULL,
    balance_euros numeric(12,2) DEFAULT 0 NOT NULL,
    observaciones text
);


ALTER TABLE public.cierres_dia OWNER TO postgres;

--
-- TOC entry 310 (class 1259 OID 17764)
-- Name: cierres_dia_id_cierre_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.cierres_dia_id_cierre_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.cierres_dia_id_cierre_seq OWNER TO postgres;

--
-- TOC entry 4491 (class 0 OID 0)
-- Dependencies: 310
-- Name: cierres_dia_id_cierre_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.cierres_dia_id_cierre_seq OWNED BY public.cierres_dia.id_cierre;


--
-- TOC entry 228 (class 1259 OID 16506)
-- Name: clientes; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.clientes (
    id_cliente integer NOT NULL,
    nombre character varying(100) NOT NULL,
    apellidos character varying(100) NOT NULL,
    correo character varying(100),
    telefono character varying(50) NOT NULL,
    documento_tipo character varying(50),
    documento_numero character varying(50),
    pais character varying(100),
    direccion text,
    ciudad character varying(100),
    codigo_postal character varying(20),
    fecha_nacimiento date,
    observaciones text,
    activo boolean DEFAULT true,
    fecha_registro timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    fecha_ultima_compra timestamp without time zone,
    id_comercio_registro integer,
    id_local_registro integer,
    id_usuario_registro integer,
    nacionalidad character varying(100),
    caducidad_documento date,
    imagen_documento bytea,
    nombre_archivo_documento character varying(255),
    segundo_nombre character varying(100) DEFAULT ''::character varying,
    segundo_apellido character varying(100) DEFAULT ''::character varying,
    imagen_documento_frontal bytea,
    imagen_documento_trasera bytea,
    tipo_residencia character varying(20),
    CONSTRAINT chk_cliente_email CHECK (((correo IS NULL) OR ((correo)::text ~* '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}$'::text)))
);


ALTER TABLE public.clientes OWNER TO postgres;

--
-- TOC entry 4492 (class 0 OID 0)
-- Dependencies: 228
-- Name: TABLE clientes; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON TABLE public.clientes IS 'Base de datos centralizada de clientes compartida globalmente';


--
-- TOC entry 4493 (class 0 OID 0)
-- Dependencies: 228
-- Name: COLUMN clientes.id_comercio_registro; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON COLUMN public.clientes.id_comercio_registro IS 'ID del comercio al que pertenece este cliente. Los clientes son unicos por comercio.';


--
-- TOC entry 4494 (class 0 OID 0)
-- Dependencies: 228
-- Name: COLUMN clientes.id_local_registro; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON COLUMN public.clientes.id_local_registro IS 'ID del local donde se registro el cliente originalmente.';


--
-- TOC entry 4495 (class 0 OID 0)
-- Dependencies: 228
-- Name: COLUMN clientes.id_usuario_registro; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON COLUMN public.clientes.id_usuario_registro IS 'ID del usuario que registro al cliente.';


--
-- TOC entry 4496 (class 0 OID 0)
-- Dependencies: 228
-- Name: COLUMN clientes.segundo_nombre; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON COLUMN public.clientes.segundo_nombre IS 'Segundo nombre del cliente (opcional).';


--
-- TOC entry 4497 (class 0 OID 0)
-- Dependencies: 228
-- Name: COLUMN clientes.segundo_apellido; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON COLUMN public.clientes.segundo_apellido IS 'Segundo apellido del cliente (opcional).';


--
-- TOC entry 4498 (class 0 OID 0)
-- Dependencies: 228
-- Name: COLUMN clientes.imagen_documento_frontal; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON COLUMN public.clientes.imagen_documento_frontal IS 'Imagen frontal del documento de identidad (BYTEA).';


--
-- TOC entry 4499 (class 0 OID 0)
-- Dependencies: 228
-- Name: COLUMN clientes.imagen_documento_trasera; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON COLUMN public.clientes.imagen_documento_trasera IS 'Imagen trasera del documento de identidad (BYTEA).';


--
-- TOC entry 329 (class 1259 OID 18006)
-- Name: clientes_beneficiarios; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.clientes_beneficiarios (
    id_beneficiario integer NOT NULL,
    id_cliente integer NOT NULL,
    id_comercio integer NOT NULL,
    id_local_registro integer,
    nombre character varying(100) NOT NULL,
    segundo_nombre character varying(100) DEFAULT ''::character varying,
    apellido character varying(100) NOT NULL,
    segundo_apellido character varying(100) DEFAULT ''::character varying,
    tipo_documento character varying(50) NOT NULL,
    numero_documento character varying(50) NOT NULL,
    telefono character varying(50) NOT NULL,
    pais character varying(100) NOT NULL,
    ciudad character varying(100) NOT NULL,
    calle character varying(200) NOT NULL,
    numero character varying(20) NOT NULL,
    piso character varying(20) DEFAULT ''::character varying,
    numero_departamento character varying(20) DEFAULT ''::character varying,
    codigo_postal character varying(20) DEFAULT ''::character varying,
    activo boolean DEFAULT true,
    fecha_creacion timestamp without time zone DEFAULT (CURRENT_TIMESTAMP AT TIME ZONE 'Europe/Madrid'::text),
    fecha_modificacion timestamp without time zone DEFAULT (CURRENT_TIMESTAMP AT TIME ZONE 'Europe/Madrid'::text)
);


ALTER TABLE public.clientes_beneficiarios OWNER TO postgres;

--
-- TOC entry 4500 (class 0 OID 0)
-- Dependencies: 329
-- Name: TABLE clientes_beneficiarios; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON TABLE public.clientes_beneficiarios IS 'Beneficiarios - NO compartidos entre comercios';


--
-- TOC entry 4501 (class 0 OID 0)
-- Dependencies: 329
-- Name: COLUMN clientes_beneficiarios.id_comercio; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON COLUMN public.clientes_beneficiarios.id_comercio IS 'Comercio al que pertenece este beneficiario. Los beneficiarios NO se comparten entre comercios.';


--
-- TOC entry 4502 (class 0 OID 0)
-- Dependencies: 329
-- Name: COLUMN clientes_beneficiarios.fecha_creacion; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON COLUMN public.clientes_beneficiarios.fecha_creacion IS 'Fecha y hora de creacion en zona horaria Europe/Madrid';


--
-- TOC entry 328 (class 1259 OID 18005)
-- Name: clientes_beneficiarios_id_beneficiario_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.clientes_beneficiarios_id_beneficiario_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.clientes_beneficiarios_id_beneficiario_seq OWNER TO postgres;

--
-- TOC entry 4503 (class 0 OID 0)
-- Dependencies: 328
-- Name: clientes_beneficiarios_id_beneficiario_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.clientes_beneficiarios_id_beneficiario_seq OWNED BY public.clientes_beneficiarios.id_beneficiario;


--
-- TOC entry 227 (class 1259 OID 16505)
-- Name: clientes_id_cliente_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.clientes_id_cliente_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.clientes_id_cliente_seq OWNER TO postgres;

--
-- TOC entry 4504 (class 0 OID 0)
-- Dependencies: 227
-- Name: clientes_id_cliente_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.clientes_id_cliente_seq OWNED BY public.clientes.id_cliente;


--
-- TOC entry 220 (class 1259 OID 16403)
-- Name: comercios; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.comercios (
    id_comercio integer NOT NULL,
    nombre_comercio character varying(200) NOT NULL,
    nombre_srl character varying(200),
    direccion_central text NOT NULL,
    numero_contacto character varying(50),
    mail_contacto character varying(100) NOT NULL,
    pais character varying(100) NOT NULL,
    observaciones text,
    porcentaje_comision_divisas numeric(5,2) DEFAULT 0.00,
    activo boolean DEFAULT true,
    fecha_registro timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    fecha_ultima_modificacion timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    archivos_contenido bytea[],
    archivos_nombres text[],
    archivos_tipos text[],
    archivos_tamanos integer[],
    CONSTRAINT chk_email CHECK (((mail_contacto)::text ~* '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}$'::text))
);


ALTER TABLE public.comercios OWNER TO postgres;

--
-- TOC entry 4505 (class 0 OID 0)
-- Dependencies: 220
-- Name: TABLE comercios; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON TABLE public.comercios IS 'Empresas/Sucursales principales del sistema';


--
-- TOC entry 219 (class 1259 OID 16402)
-- Name: comercios_id_comercio_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.comercios_id_comercio_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.comercios_id_comercio_seq OWNER TO postgres;

--
-- TOC entry 4506 (class 0 OID 0)
-- Dependencies: 219
-- Name: comercios_id_comercio_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.comercios_id_comercio_seq OWNED BY public.comercios.id_comercio;


--
-- TOC entry 273 (class 1259 OID 17136)
-- Name: configuracion_2fa; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.configuracion_2fa (
    id_2fa integer NOT NULL,
    id_usuario integer NOT NULL,
    habilitado boolean DEFAULT false,
    metodo character varying(50),
    secret_key character varying(255),
    telefono_backup character varying(50),
    codigos_respaldo text[],
    codigos_usados integer DEFAULT 0,
    fecha_habilitacion timestamp without time zone,
    fecha_ultima_verificacion timestamp without time zone,
    intentos_fallidos integer DEFAULT 0,
    bloqueado_hasta timestamp without time zone
);


ALTER TABLE public.configuracion_2fa OWNER TO postgres;

--
-- TOC entry 4507 (class 0 OID 0)
-- Dependencies: 273
-- Name: TABLE configuracion_2fa; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON TABLE public.configuracion_2fa IS 'Configuración de autenticación de dos factores';


--
-- TOC entry 272 (class 1259 OID 17135)
-- Name: configuracion_2fa_id_2fa_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.configuracion_2fa_id_2fa_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.configuracion_2fa_id_2fa_seq OWNER TO postgres;

--
-- TOC entry 4508 (class 0 OID 0)
-- Dependencies: 272
-- Name: configuracion_2fa_id_2fa_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.configuracion_2fa_id_2fa_seq OWNED BY public.configuracion_2fa.id_2fa;


--
-- TOC entry 265 (class 1259 OID 17017)
-- Name: configuracion_seguridad; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.configuracion_seguridad (
    id_config integer NOT NULL,
    id_comercio integer NOT NULL,
    password_min_length integer DEFAULT 8,
    password_require_uppercase boolean DEFAULT true,
    password_require_lowercase boolean DEFAULT true,
    password_require_numbers boolean DEFAULT true,
    password_require_special_chars boolean DEFAULT true,
    password_expira_dias integer DEFAULT 90,
    password_historial_no_repetir integer DEFAULT 5,
    sesion_duracion_horas integer DEFAULT 8,
    sesion_inactividad_minutos integer DEFAULT 30,
    sesion_simultaneas_max integer DEFAULT 1,
    login_max_intentos integer DEFAULT 5,
    login_bloqueo_minutos integer DEFAULT 15,
    login_requiere_2fa boolean DEFAULT false,
    login_requiere_dispositivo_autorizado boolean DEFAULT false,
    notificar_login_nuevo_dispositivo boolean DEFAULT true,
    notificar_login_ip_desconocida boolean DEFAULT true,
    notificar_cambio_password boolean DEFAULT true,
    notificar_intentos_fallidos boolean DEFAULT true,
    fecha_creacion timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    fecha_modificacion timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    modificado_por integer
);


ALTER TABLE public.configuracion_seguridad OWNER TO postgres;

--
-- TOC entry 4509 (class 0 OID 0)
-- Dependencies: 265
-- Name: TABLE configuracion_seguridad; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON TABLE public.configuracion_seguridad IS 'Políticas de seguridad personalizadas por comercio';


--
-- TOC entry 264 (class 1259 OID 17016)
-- Name: configuracion_seguridad_id_config_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.configuracion_seguridad_id_config_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.configuracion_seguridad_id_config_seq OWNER TO postgres;

--
-- TOC entry 4510 (class 0 OID 0)
-- Dependencies: 264
-- Name: configuracion_seguridad_id_config_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.configuracion_seguridad_id_config_seq OWNED BY public.configuracion_seguridad.id_config;


--
-- TOC entry 295 (class 1259 OID 17534)
-- Name: configuracion_sistema; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.configuracion_sistema (
    id_config integer NOT NULL,
    clave character varying(100) NOT NULL,
    valor_texto text,
    valor_decimal numeric(10,2),
    valor_entero integer,
    valor_booleano boolean,
    descripcion text,
    fecha_creacion timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    fecha_modificacion timestamp without time zone DEFAULT CURRENT_TIMESTAMP
);


ALTER TABLE public.configuracion_sistema OWNER TO postgres;

--
-- TOC entry 4511 (class 0 OID 0)
-- Dependencies: 295
-- Name: TABLE configuracion_sistema; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON TABLE public.configuracion_sistema IS 'Configuraciones globales del sistema Allva';


--
-- TOC entry 4512 (class 0 OID 0)
-- Dependencies: 295
-- Name: COLUMN configuracion_sistema.clave; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON COLUMN public.configuracion_sistema.clave IS 'Clave única de la configuración';


--
-- TOC entry 4513 (class 0 OID 0)
-- Dependencies: 295
-- Name: COLUMN configuracion_sistema.valor_decimal; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON COLUMN public.configuracion_sistema.valor_decimal IS 'Valor numérico decimal (para porcentajes, etc.)';


--
-- TOC entry 294 (class 1259 OID 17533)
-- Name: configuracion_sistema_id_config_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.configuracion_sistema_id_config_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.configuracion_sistema_id_config_seq OWNER TO postgres;

--
-- TOC entry 4514 (class 0 OID 0)
-- Dependencies: 294
-- Name: configuracion_sistema_id_config_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.configuracion_sistema_id_config_seq OWNED BY public.configuracion_sistema.id_config;


--
-- TOC entry 280 (class 1259 OID 17265)
-- Name: correlativo_locales; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.correlativo_locales (
    id integer NOT NULL,
    ultimo_correlativo integer DEFAULT 0 NOT NULL,
    fecha_actualizacion timestamp without time zone DEFAULT CURRENT_TIMESTAMP
);


ALTER TABLE public.correlativo_locales OWNER TO postgres;

--
-- TOC entry 293 (class 1259 OID 17487)
-- Name: correlativo_locales_global; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.correlativo_locales_global (
    id integer DEFAULT 1 NOT NULL,
    ultimo_numero integer DEFAULT 0 NOT NULL,
    fecha_actualizacion timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT solo_una_fila CHECK ((id = 1))
);


ALTER TABLE public.correlativo_locales_global OWNER TO postgres;

--
-- TOC entry 279 (class 1259 OID 17264)
-- Name: correlativo_locales_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.correlativo_locales_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.correlativo_locales_id_seq OWNER TO postgres;

--
-- TOC entry 4515 (class 0 OID 0)
-- Dependencies: 279
-- Name: correlativo_locales_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.correlativo_locales_id_seq OWNED BY public.correlativo_locales.id;


--
-- TOC entry 309 (class 1259 OID 17748)
-- Name: correlativos_operaciones; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.correlativos_operaciones (
    id integer NOT NULL,
    id_local integer NOT NULL,
    prefijo character varying(10) NOT NULL,
    ultimo_correlativo integer DEFAULT 0 NOT NULL,
    fecha_ultimo_uso timestamp without time zone DEFAULT CURRENT_TIMESTAMP
);


ALTER TABLE public.correlativos_operaciones OWNER TO postgres;

--
-- TOC entry 4516 (class 0 OID 0)
-- Dependencies: 309
-- Name: TABLE correlativos_operaciones; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON TABLE public.correlativos_operaciones IS 'Correlativos de operaciones por local y tipo';


--
-- TOC entry 4517 (class 0 OID 0)
-- Dependencies: 309
-- Name: COLUMN correlativos_operaciones.prefijo; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON COLUMN public.correlativos_operaciones.prefijo IS 'DI para divisas';


--
-- TOC entry 4518 (class 0 OID 0)
-- Dependencies: 309
-- Name: COLUMN correlativos_operaciones.ultimo_correlativo; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON COLUMN public.correlativos_operaciones.ultimo_correlativo IS 'Ultimo numero usado (ej: si es 5, el proximo sera DI0006)';


--
-- TOC entry 308 (class 1259 OID 17747)
-- Name: correlativos_operaciones_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.correlativos_operaciones_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.correlativos_operaciones_id_seq OWNER TO postgres;

--
-- TOC entry 4519 (class 0 OID 0)
-- Dependencies: 308
-- Name: correlativos_operaciones_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.correlativos_operaciones_id_seq OWNED BY public.correlativos_operaciones.id;


--
-- TOC entry 248 (class 1259 OID 16794)
-- Name: cuentas_bancarias; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.cuentas_bancarias (
    id_cuenta integer NOT NULL,
    id_comercio integer NOT NULL,
    id_local integer,
    nombre_banco character varying(100) NOT NULL,
    numero_cuenta character varying(50) NOT NULL,
    iban character varying(50),
    swift_bic character varying(20),
    modulo character varying(50),
    activa boolean DEFAULT true,
    es_principal boolean DEFAULT false,
    balance_actual numeric(12,2) DEFAULT 0.00,
    moneda character varying(10) DEFAULT 'EUR'::character varying,
    observaciones text,
    fecha_creacion timestamp without time zone DEFAULT CURRENT_TIMESTAMP
);


ALTER TABLE public.cuentas_bancarias OWNER TO postgres;

--
-- TOC entry 4520 (class 0 OID 0)
-- Dependencies: 248
-- Name: TABLE cuentas_bancarias; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON TABLE public.cuentas_bancarias IS 'Cuentas bancarias asociadas a comercios y módulos';


--
-- TOC entry 247 (class 1259 OID 16793)
-- Name: cuentas_bancarias_id_cuenta_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.cuentas_bancarias_id_cuenta_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.cuentas_bancarias_id_cuenta_seq OWNER TO postgres;

--
-- TOC entry 4521 (class 0 OID 0)
-- Dependencies: 247
-- Name: cuentas_bancarias_id_cuenta_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.cuentas_bancarias_id_cuenta_seq OWNED BY public.cuentas_bancarias.id_cuenta;


--
-- TOC entry 335 (class 1259 OID 18085)
-- Name: depositos_pack_alimentos; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.depositos_pack_alimentos (
    id_deposito integer NOT NULL,
    id_local integer NOT NULL,
    codigo_local character varying(50) NOT NULL,
    id_comercio integer,
    nombre_comercio character varying(200),
    monto_depositado numeric(12,2) NOT NULL,
    monto_aplicado numeric(12,2) NOT NULL,
    excedente numeric(12,2) DEFAULT 0,
    deuda_restante numeric(12,2) DEFAULT 0,
    cantidad_operaciones_pagadas integer NOT NULL,
    ids_operaciones_pagadas text,
    numeros_operaciones_pagadas text,
    fecha_deposito date NOT NULL,
    hora_deposito time without time zone NOT NULL,
    fecha_registro timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    id_usuario integer,
    nombre_usuario character varying(200),
    observaciones text,
    estado character varying(20) DEFAULT 'ACTIVO'::character varying
);


ALTER TABLE public.depositos_pack_alimentos OWNER TO postgres;

--
-- TOC entry 4522 (class 0 OID 0)
-- Dependencies: 335
-- Name: TABLE depositos_pack_alimentos; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON TABLE public.depositos_pack_alimentos IS 'Registro de depósitos para pagar operaciones de Pack Alimentos';


--
-- TOC entry 4523 (class 0 OID 0)
-- Dependencies: 335
-- Name: COLUMN depositos_pack_alimentos.monto_depositado; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON COLUMN public.depositos_pack_alimentos.monto_depositado IS 'Monto total depositado por el local';


--
-- TOC entry 4524 (class 0 OID 0)
-- Dependencies: 335
-- Name: COLUMN depositos_pack_alimentos.monto_aplicado; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON COLUMN public.depositos_pack_alimentos.monto_aplicado IS 'Monto efectivamente aplicado a operaciones';


--
-- TOC entry 4525 (class 0 OID 0)
-- Dependencies: 335
-- Name: COLUMN depositos_pack_alimentos.excedente; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON COLUMN public.depositos_pack_alimentos.excedente IS 'Monto sobrante que queda como beneficio del local';


--
-- TOC entry 4526 (class 0 OID 0)
-- Dependencies: 335
-- Name: COLUMN depositos_pack_alimentos.ids_operaciones_pagadas; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON COLUMN public.depositos_pack_alimentos.ids_operaciones_pagadas IS 'IDs de operaciones pagadas separados por coma';


--
-- TOC entry 334 (class 1259 OID 18084)
-- Name: depositos_pack_alimentos_id_deposito_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.depositos_pack_alimentos_id_deposito_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.depositos_pack_alimentos_id_deposito_seq OWNER TO postgres;

--
-- TOC entry 4527 (class 0 OID 0)
-- Dependencies: 334
-- Name: depositos_pack_alimentos_id_deposito_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.depositos_pack_alimentos_id_deposito_seq OWNED BY public.depositos_pack_alimentos.id_deposito;


--
-- TOC entry 257 (class 1259 OID 16920)
-- Name: dispositivos_autorizados; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.dispositivos_autorizados (
    id_dispositivo integer NOT NULL,
    id_usuario integer NOT NULL,
    uuid_dispositivo uuid NOT NULL,
    mac_address character varying(100),
    nombre_dispositivo character varying(200),
    sistema_operativo character varying(100),
    navegador character varying(100),
    version_navegador character varying(50),
    dispositivo_tipo character varying(50),
    ip_registro character varying(50),
    pais_registro character varying(100),
    ciudad_registro character varying(100),
    autorizado boolean DEFAULT false,
    requiere_aprobacion boolean DEFAULT true,
    fecha_registro timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    fecha_autorizacion timestamp without time zone,
    autorizado_por integer,
    activo boolean DEFAULT true,
    fecha_ultimo_uso timestamp without time zone,
    numero_usos integer DEFAULT 0,
    fecha_revocacion timestamp without time zone,
    motivo_revocacion text
);


ALTER TABLE public.dispositivos_autorizados OWNER TO postgres;

--
-- TOC entry 4528 (class 0 OID 0)
-- Dependencies: 257
-- Name: TABLE dispositivos_autorizados; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON TABLE public.dispositivos_autorizados IS 'Dispositivos autorizados con validación UUID/MAC';


--
-- TOC entry 256 (class 1259 OID 16919)
-- Name: dispositivos_autorizados_id_dispositivo_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.dispositivos_autorizados_id_dispositivo_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.dispositivos_autorizados_id_dispositivo_seq OWNER TO postgres;

--
-- TOC entry 4529 (class 0 OID 0)
-- Dependencies: 256
-- Name: dispositivos_autorizados_id_dispositivo_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.dispositivos_autorizados_id_dispositivo_seq OWNED BY public.dispositivos_autorizados.id_dispositivo;


--
-- TOC entry 298 (class 1259 OID 17555)
-- Name: divisas_favoritas; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.divisas_favoritas (
    id_favorita integer NOT NULL,
    id_local integer NOT NULL,
    codigo_divisa character varying(10) NOT NULL,
    nombre_divisa character varying(100) NOT NULL,
    pais character varying(100),
    orden integer DEFAULT 0,
    fecha_agregada timestamp without time zone DEFAULT CURRENT_TIMESTAMP
);


ALTER TABLE public.divisas_favoritas OWNER TO postgres;

--
-- TOC entry 4530 (class 0 OID 0)
-- Dependencies: 298
-- Name: TABLE divisas_favoritas; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON TABLE public.divisas_favoritas IS 'Divisas favoritas configuradas por cada local';


--
-- TOC entry 297 (class 1259 OID 17554)
-- Name: divisas_favoritas_id_favorita_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.divisas_favoritas_id_favorita_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.divisas_favoritas_id_favorita_seq OWNER TO postgres;

--
-- TOC entry 4531 (class 0 OID 0)
-- Dependencies: 297
-- Name: divisas_favoritas_id_favorita_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.divisas_favoritas_id_favorita_seq OWNED BY public.divisas_favoritas.id_favorita;


--
-- TOC entry 307 (class 1259 OID 17732)
-- Name: divisas_favoritas_local; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.divisas_favoritas_local (
    id integer NOT NULL,
    id_local integer NOT NULL,
    codigo_divisa character varying(10) NOT NULL,
    orden integer DEFAULT 0,
    fecha_agregado timestamp without time zone DEFAULT CURRENT_TIMESTAMP
);


ALTER TABLE public.divisas_favoritas_local OWNER TO postgres;

--
-- TOC entry 4532 (class 0 OID 0)
-- Dependencies: 307
-- Name: TABLE divisas_favoritas_local; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON TABLE public.divisas_favoritas_local IS 'Divisas favoritas por cada local';


--
-- TOC entry 306 (class 1259 OID 17731)
-- Name: divisas_favoritas_local_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.divisas_favoritas_local_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.divisas_favoritas_local_id_seq OWNER TO postgres;

--
-- TOC entry 4533 (class 0 OID 0)
-- Dependencies: 306
-- Name: divisas_favoritas_local_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.divisas_favoritas_local_id_seq OWNED BY public.divisas_favoritas_local.id;


--
-- TOC entry 327 (class 1259 OID 17951)
-- Name: historial_generacion_pdf; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.historial_generacion_pdf (
    id_generacion integer NOT NULL,
    id_comercio integer NOT NULL,
    id_local integer NOT NULL,
    codigo_local character varying(50) NOT NULL,
    id_usuario integer NOT NULL,
    nombre_usuario character varying(200) NOT NULL,
    modulo character varying(50) NOT NULL,
    tipo_reporte character varying(100) NOT NULL,
    filtros_aplicados text,
    fecha_generacion date DEFAULT CURRENT_DATE NOT NULL,
    hora_generacion time without time zone DEFAULT CURRENT_TIME NOT NULL,
    registros_incluidos integer DEFAULT 0,
    observaciones text
);


ALTER TABLE public.historial_generacion_pdf OWNER TO postgres;

--
-- TOC entry 4534 (class 0 OID 0)
-- Dependencies: 327
-- Name: TABLE historial_generacion_pdf; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON TABLE public.historial_generacion_pdf IS 'Registro de generacion de PDFs de historial por local';


--
-- TOC entry 326 (class 1259 OID 17950)
-- Name: historial_generacion_pdf_id_generacion_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.historial_generacion_pdf_id_generacion_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.historial_generacion_pdf_id_generacion_seq OWNER TO postgres;

--
-- TOC entry 4535 (class 0 OID 0)
-- Dependencies: 326
-- Name: historial_generacion_pdf_id_generacion_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.historial_generacion_pdf_id_generacion_seq OWNED BY public.historial_generacion_pdf.id_generacion;


--
-- TOC entry 259 (class 1259 OID 16950)
-- Name: historial_passwords; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.historial_passwords (
    id_historial integer NOT NULL,
    id_usuario integer NOT NULL,
    password_hash character varying(255) NOT NULL,
    fecha_cambio timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    cambiado_por integer,
    motivo character varying(100),
    ip_address character varying(50),
    forzado_por_politica boolean DEFAULT false
);


ALTER TABLE public.historial_passwords OWNER TO postgres;

--
-- TOC entry 4536 (class 0 OID 0)
-- Dependencies: 259
-- Name: TABLE historial_passwords; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON TABLE public.historial_passwords IS 'Historial de contraseñas para evitar reutilización';


--
-- TOC entry 258 (class 1259 OID 16949)
-- Name: historial_passwords_id_historial_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.historial_passwords_id_historial_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.historial_passwords_id_historial_seq OWNER TO postgres;

--
-- TOC entry 4537 (class 0 OID 0)
-- Dependencies: 258
-- Name: historial_passwords_id_historial_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.historial_passwords_id_historial_seq OWNED BY public.historial_passwords.id_historial;


--
-- TOC entry 250 (class 1259 OID 16823)
-- Name: incidencias; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.incidencias (
    id_incidencia bigint NOT NULL,
    numero_incidencia character varying(50) NOT NULL,
    id_usuario_reporta integer NOT NULL,
    id_comercio integer NOT NULL,
    id_local integer,
    id_operacion bigint,
    numero_operacion character varying(50),
    modulo character varying(50),
    tipo_incidencia character varying(50) NOT NULL,
    prioridad character varying(20) DEFAULT 'MEDIA'::character varying,
    titulo character varying(200) NOT NULL,
    descripcion text NOT NULL,
    estado character varying(50) DEFAULT 'ABIERTA'::character varying,
    id_usuario_asignado integer,
    fecha_asignacion timestamp without time zone,
    fecha_resolucion timestamp without time zone,
    resolucion text,
    fecha_creacion timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    fecha_cierre timestamp without time zone,
    observaciones text
);


ALTER TABLE public.incidencias OWNER TO postgres;

--
-- TOC entry 4538 (class 0 OID 0)
-- Dependencies: 250
-- Name: TABLE incidencias; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON TABLE public.incidencias IS 'Registro y seguimiento de incidencias';


--
-- TOC entry 249 (class 1259 OID 16822)
-- Name: incidencias_id_incidencia_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.incidencias_id_incidencia_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.incidencias_id_incidencia_seq OWNER TO postgres;

--
-- TOC entry 4539 (class 0 OID 0)
-- Dependencies: 249
-- Name: incidencias_id_incidencia_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.incidencias_id_incidencia_seq OWNED BY public.incidencias.id_incidencia;


--
-- TOC entry 261 (class 1259 OID 16971)
-- Name: intentos_login; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.intentos_login (
    id_intento bigint NOT NULL,
    numero_usuario character varying(50) NOT NULL,
    id_usuario integer,
    exitoso boolean NOT NULL,
    motivo_fallo character varying(200),
    codigo_local character varying(50),
    ip_address character varying(50),
    user_agent text,
    uuid_dispositivo uuid,
    mac_address character varying(100),
    pais character varying(100),
    ciudad character varying(100),
    coordenadas point,
    fecha_intento timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    tiempo_respuesta_ms integer,
    es_sospechoso boolean DEFAULT false,
    razon_sospecha text,
    bloqueado_temporalmente boolean DEFAULT false
);


ALTER TABLE public.intentos_login OWNER TO postgres;

--
-- TOC entry 4540 (class 0 OID 0)
-- Dependencies: 261
-- Name: TABLE intentos_login; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON TABLE public.intentos_login IS 'Registro detallado de todos los intentos de login';


--
-- TOC entry 260 (class 1259 OID 16970)
-- Name: intentos_login_id_intento_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.intentos_login_id_intento_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.intentos_login_id_intento_seq OWNER TO postgres;

--
-- TOC entry 4541 (class 0 OID 0)
-- Dependencies: 260
-- Name: intentos_login_id_intento_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.intentos_login_id_intento_seq OWNED BY public.intentos_login.id_intento;


--
-- TOC entry 290 (class 1259 OID 17456)
-- Name: licencias; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.licencias (
    id_licencia integer NOT NULL,
    codigo_licencia character varying(50) NOT NULL,
    nombre_cliente character varying(200),
    email_cliente character varying(200),
    fecha_emision timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    fecha_expiracion timestamp without time zone,
    activa boolean DEFAULT true,
    usada boolean DEFAULT false,
    fecha_activacion timestamp without time zone,
    id_maquina character varying(100),
    id_comercio integer,
    observaciones text,
    creado_por integer,
    fecha_creacion timestamp without time zone DEFAULT CURRENT_TIMESTAMP
);


ALTER TABLE public.licencias OWNER TO postgres;

--
-- TOC entry 289 (class 1259 OID 17455)
-- Name: licencias_id_licencia_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.licencias_id_licencia_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.licencias_id_licencia_seq OWNER TO postgres;

--
-- TOC entry 4542 (class 0 OID 0)
-- Dependencies: 289
-- Name: licencias_id_licencia_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.licencias_id_licencia_seq OWNED BY public.licencias.id_licencia;


--
-- TOC entry 224 (class 1259 OID 16439)
-- Name: locales; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.locales (
    id_local integer NOT NULL,
    id_comercio integer NOT NULL,
    codigo_local character varying(50) NOT NULL,
    nombre_local character varying(200) NOT NULL,
    direccion text NOT NULL,
    local_numero character varying(20),
    escalera character varying(10),
    piso character varying(10),
    numero_usuarios_max integer DEFAULT 10,
    activo boolean DEFAULT true,
    fecha_apertura timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    fecha_cierre timestamp without time zone,
    fecha_creacion timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    fecha_modificacion timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    telefono character varying(50),
    email character varying(255),
    observaciones text,
    modulo_divisas boolean DEFAULT false,
    modulo_pack_alimentos boolean DEFAULT false,
    modulo_billetes_avion boolean DEFAULT false,
    modulo_pack_viajes boolean DEFAULT false,
    pais character varying(100) DEFAULT 'España'::character varying,
    codigo_postal character varying(10),
    tipo_via character varying(50) DEFAULT 'Calle'::character varying,
    comision_divisas numeric(5,2) DEFAULT 0.00,
    movil character varying(50),
    saldo_favor_alimentos numeric(12,2) DEFAULT 0,
    beneficio_acumulado numeric(12,2) DEFAULT 0
);


ALTER TABLE public.locales OWNER TO postgres;

--
-- TOC entry 4543 (class 0 OID 0)
-- Dependencies: 224
-- Name: TABLE locales; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON TABLE public.locales IS 'Puntos de venta físicos de cada comercio';


--
-- TOC entry 4544 (class 0 OID 0)
-- Dependencies: 224
-- Name: COLUMN locales.modulo_divisas; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON COLUMN public.locales.modulo_divisas IS 'Permiso para usar el módulo de divisas en este local';


--
-- TOC entry 4545 (class 0 OID 0)
-- Dependencies: 224
-- Name: COLUMN locales.modulo_pack_alimentos; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON COLUMN public.locales.modulo_pack_alimentos IS 'Permiso para usar el módulo de pack de alimentos en este local';


--
-- TOC entry 4546 (class 0 OID 0)
-- Dependencies: 224
-- Name: COLUMN locales.modulo_billetes_avion; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON COLUMN public.locales.modulo_billetes_avion IS 'Permiso para usar el módulo de billetes de avión en este local';


--
-- TOC entry 4547 (class 0 OID 0)
-- Dependencies: 224
-- Name: COLUMN locales.modulo_pack_viajes; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON COLUMN public.locales.modulo_pack_viajes IS 'Permiso para usar el módulo de pack de viajes en este local';


--
-- TOC entry 4548 (class 0 OID 0)
-- Dependencies: 224
-- Name: COLUMN locales.movil; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON COLUMN public.locales.movil IS 'Número de móvil del local';


--
-- TOC entry 223 (class 1259 OID 16438)
-- Name: locales_id_local_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.locales_id_local_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.locales_id_local_seq OWNER TO postgres;

--
-- TOC entry 4549 (class 0 OID 0)
-- Dependencies: 223
-- Name: locales_id_local_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.locales_id_local_seq OWNED BY public.locales.id_local;


--
-- TOC entry 284 (class 1259 OID 17313)
-- Name: niveles_acceso; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.niveles_acceso (
    id_nivel integer NOT NULL,
    nombre_nivel character varying(50) NOT NULL,
    descripcion text NOT NULL,
    puede_crear_usuarios_allva boolean DEFAULT false,
    puede_editar_comercios boolean DEFAULT false,
    puede_editar_usuarios_locales boolean DEFAULT false,
    acceso_todos_modulos boolean DEFAULT false,
    fecha_creacion timestamp without time zone DEFAULT CURRENT_TIMESTAMP
);


ALTER TABLE public.niveles_acceso OWNER TO postgres;

--
-- TOC entry 4550 (class 0 OID 0)
-- Dependencies: 284
-- Name: TABLE niveles_acceso; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON TABLE public.niveles_acceso IS 'Catálogo de niveles de acceso para administradores Allva (4 niveles jerárquicos)';


--
-- TOC entry 4551 (class 0 OID 0)
-- Dependencies: 284
-- Name: COLUMN niveles_acceso.id_nivel; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON COLUMN public.niveles_acceso.id_nivel IS 'ID del nivel (1=básico, 4=super admin)';


--
-- TOC entry 269 (class 1259 OID 17087)
-- Name: notificaciones_seguridad; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.notificaciones_seguridad (
    id_notificacion bigint NOT NULL,
    id_usuario integer NOT NULL,
    tipo character varying(100) NOT NULL,
    titulo character varying(200) NOT NULL,
    mensaje text NOT NULL,
    nivel_severidad character varying(50) DEFAULT 'INFO'::character varying,
    leida boolean DEFAULT false,
    fecha_lectura timestamp without time zone,
    archivada boolean DEFAULT false,
    fecha_creacion timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    metadata_json jsonb,
    requiere_accion boolean DEFAULT false,
    accion_completada boolean DEFAULT false,
    url_accion character varying(500)
);


ALTER TABLE public.notificaciones_seguridad OWNER TO postgres;

--
-- TOC entry 4552 (class 0 OID 0)
-- Dependencies: 269
-- Name: TABLE notificaciones_seguridad; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON TABLE public.notificaciones_seguridad IS 'Sistema de notificaciones de eventos de seguridad';


--
-- TOC entry 268 (class 1259 OID 17086)
-- Name: notificaciones_seguridad_id_notificacion_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.notificaciones_seguridad_id_notificacion_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.notificaciones_seguridad_id_notificacion_seq OWNER TO postgres;

--
-- TOC entry 4553 (class 0 OID 0)
-- Dependencies: 268
-- Name: notificaciones_seguridad_id_notificacion_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.notificaciones_seguridad_id_notificacion_seq OWNED BY public.notificaciones_seguridad.id_notificacion;


--
-- TOC entry 292 (class 1259 OID 17481)
-- Name: numeros_locales_liberados; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.numeros_locales_liberados (
    numero integer NOT NULL,
    fecha_liberacion timestamp without time zone DEFAULT CURRENT_TIMESTAMP
);


ALTER TABLE public.numeros_locales_liberados OWNER TO postgres;

--
-- TOC entry 236 (class 1259 OID 16618)
-- Name: operaciones; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.operaciones (
    id_operacion bigint NOT NULL,
    numero_operacion character varying(50) NOT NULL,
    id_comercio integer NOT NULL,
    id_local integer NOT NULL,
    codigo_local character varying(50) NOT NULL,
    id_usuario integer NOT NULL,
    nombre_usuario character varying(200) NOT NULL,
    numero_usuario character varying(50) NOT NULL,
    id_cliente integer,
    nombre_cliente character varying(200),
    modulo character varying(50) NOT NULL,
    tipo_operacion character varying(50) NOT NULL,
    estado character varying(50) DEFAULT 'PENDIENTE'::character varying NOT NULL,
    importe_total numeric(12,2) NOT NULL,
    importe_pagado numeric(12,2) DEFAULT 0.00,
    importe_pendiente numeric(12,2) DEFAULT 0.00,
    moneda character varying(10) DEFAULT 'EUR'::character varying,
    metodo_pago character varying(50),
    fecha_operacion timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    hora_operacion time without time zone DEFAULT CURRENT_TIME,
    fecha_vencimiento timestamp without time zone,
    fecha_cancelacion timestamp without time zone,
    fecha_completada timestamp without time zone,
    localizador character varying(100),
    referencia_externa character varying(200),
    observaciones text,
    motivo_cancelacion text,
    ip_address character varying(50),
    dispositivo character varying(200),
    CONSTRAINT chk_operacion_estado CHECK (((estado)::text = ANY ((ARRAY['PENDIENTE'::character varying, 'PAGADA'::character varying, 'RESERVA_EMITIDA'::character varying, 'RESERVA_EXPIRADA'::character varying, 'CANCELADA'::character varying, 'REEMBOLSADA'::character varying, 'COMPLETADA'::character varying])::text[]))),
    CONSTRAINT chk_operacion_modulo CHECK (((modulo)::text = ANY ((ARRAY['DIVISAS'::character varying, 'BILLETES'::character varying, 'PACK_ALIMENTOS'::character varying, 'PACK_VIAJES'::character varying])::text[])))
);


ALTER TABLE public.operaciones OWNER TO postgres;

--
-- TOC entry 4554 (class 0 OID 0)
-- Dependencies: 236
-- Name: TABLE operaciones; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON TABLE public.operaciones IS 'Registro maestro de todas las operaciones del sistema';


--
-- TOC entry 240 (class 1259 OID 16685)
-- Name: operaciones_billetes; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.operaciones_billetes (
    id_operacion_billete bigint NOT NULL,
    id_operacion bigint NOT NULL,
    aeropuerto_origen character varying(100) NOT NULL,
    codigo_iata_origen character varying(10),
    aeropuerto_destino character varying(100) NOT NULL,
    codigo_iata_destino character varying(10),
    fecha_salida date NOT NULL,
    hora_salida time without time zone,
    fecha_regreso date,
    hora_regreso time without time zone,
    aerolinea character varying(100),
    numero_vuelo character varying(20),
    numero_pasajeros integer DEFAULT 1,
    tipo_pasajeros character varying(50),
    clase character varying(50),
    equipaje_incluido boolean DEFAULT true,
    equipaje_adicional integer DEFAULT 0,
    precio_base numeric(12,2) NOT NULL,
    tasas_aeropuerto numeric(12,2) DEFAULT 0.00,
    cargo_servicio numeric(12,2) DEFAULT 0.00,
    beneficio numeric(12,2) DEFAULT 0.00,
    proveedor character varying(100),
    pnr character varying(50),
    observaciones text
);


ALTER TABLE public.operaciones_billetes OWNER TO postgres;

--
-- TOC entry 4555 (class 0 OID 0)
-- Dependencies: 240
-- Name: TABLE operaciones_billetes; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON TABLE public.operaciones_billetes IS 'Detalle de operaciones de billetes de avión';


--
-- TOC entry 239 (class 1259 OID 16684)
-- Name: operaciones_billetes_id_operacion_billete_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.operaciones_billetes_id_operacion_billete_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.operaciones_billetes_id_operacion_billete_seq OWNER TO postgres;

--
-- TOC entry 4556 (class 0 OID 0)
-- Dependencies: 239
-- Name: operaciones_billetes_id_operacion_billete_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.operaciones_billetes_id_operacion_billete_seq OWNED BY public.operaciones_billetes.id_operacion_billete;


--
-- TOC entry 238 (class 1259 OID 16665)
-- Name: operaciones_divisas; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.operaciones_divisas (
    id_operacion_divisa bigint NOT NULL,
    id_operacion bigint NOT NULL,
    divisa_origen character varying(10) NOT NULL,
    divisa_destino character varying(10) NOT NULL,
    cantidad_origen numeric(12,2) NOT NULL,
    cantidad_destino numeric(12,2) NOT NULL,
    tipo_cambio numeric(10,6) NOT NULL,
    tipo_cambio_aplicado numeric(10,6) NOT NULL,
    comision_porcentaje numeric(5,2) DEFAULT 0.00,
    comision_monto numeric(12,2) DEFAULT 0.00,
    beneficio numeric(12,2) DEFAULT 0.00,
    fuente_tipo_cambio character varying(100),
    fecha_tipo_cambio timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    observaciones text
);


ALTER TABLE public.operaciones_divisas OWNER TO postgres;

--
-- TOC entry 4557 (class 0 OID 0)
-- Dependencies: 238
-- Name: TABLE operaciones_divisas; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON TABLE public.operaciones_divisas IS 'Detalle específico de operaciones de cambio de divisas';


--
-- TOC entry 237 (class 1259 OID 16664)
-- Name: operaciones_divisas_id_operacion_divisa_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.operaciones_divisas_id_operacion_divisa_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.operaciones_divisas_id_operacion_divisa_seq OWNER TO postgres;

--
-- TOC entry 4558 (class 0 OID 0)
-- Dependencies: 237
-- Name: operaciones_divisas_id_operacion_divisa_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.operaciones_divisas_id_operacion_divisa_seq OWNED BY public.operaciones_divisas.id_operacion_divisa;


--
-- TOC entry 235 (class 1259 OID 16617)
-- Name: operaciones_id_operacion_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.operaciones_id_operacion_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.operaciones_id_operacion_seq OWNER TO postgres;

--
-- TOC entry 4559 (class 0 OID 0)
-- Dependencies: 235
-- Name: operaciones_id_operacion_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.operaciones_id_operacion_seq OWNED BY public.operaciones.id_operacion;


--
-- TOC entry 242 (class 1259 OID 16709)
-- Name: operaciones_pack_alimentos; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.operaciones_pack_alimentos (
    id_operacion_pack_alimento bigint NOT NULL,
    id_operacion bigint NOT NULL,
    nombre_pack character varying(200) NOT NULL,
    descripcion_pack text,
    pais_destino character varying(100) NOT NULL,
    ciudad_destino character varying(100),
    tipo_pack character varying(50),
    peso_aproximado numeric(8,2),
    numero_items integer,
    precio_pack numeric(12,2) NOT NULL,
    costo_envio numeric(12,2) DEFAULT 0.00,
    beneficio numeric(12,2) DEFAULT 0.00,
    fecha_envio_estimada date,
    fecha_entrega_estimada date,
    fecha_entrega_real date,
    numero_tracking character varying(100),
    estado_envio character varying(50) DEFAULT 'PENDIENTE'::character varying,
    observaciones text,
    id_beneficiario integer,
    fecha_envio timestamp without time zone
);


ALTER TABLE public.operaciones_pack_alimentos OWNER TO postgres;

--
-- TOC entry 4560 (class 0 OID 0)
-- Dependencies: 242
-- Name: TABLE operaciones_pack_alimentos; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON TABLE public.operaciones_pack_alimentos IS 'Detalle de operaciones de packs de alimentos';


--
-- TOC entry 241 (class 1259 OID 16708)
-- Name: operaciones_pack_alimentos_id_operacion_pack_alimento_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.operaciones_pack_alimentos_id_operacion_pack_alimento_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.operaciones_pack_alimentos_id_operacion_pack_alimento_seq OWNER TO postgres;

--
-- TOC entry 4561 (class 0 OID 0)
-- Dependencies: 241
-- Name: operaciones_pack_alimentos_id_operacion_pack_alimento_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.operaciones_pack_alimentos_id_operacion_pack_alimento_seq OWNED BY public.operaciones_pack_alimentos.id_operacion_pack_alimento;


--
-- TOC entry 244 (class 1259 OID 16729)
-- Name: operaciones_pack_viajes; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.operaciones_pack_viajes (
    id_operacion_pack_viaje bigint NOT NULL,
    id_operacion bigint NOT NULL,
    nombre_paquete character varying(200) NOT NULL,
    descripcion_paquete text,
    destino character varying(200) NOT NULL,
    pais_destino character varying(100),
    fecha_inicio date NOT NULL,
    fecha_fin date NOT NULL,
    numero_noches integer NOT NULL,
    numero_dias integer NOT NULL,
    numero_pasajeros integer DEFAULT 1,
    tipo_habitacion character varying(50),
    incluye_vuelo boolean DEFAULT true,
    incluye_hotel boolean DEFAULT true,
    incluye_alimentacion boolean DEFAULT false,
    tipo_alimentacion character varying(50),
    incluye_traslados boolean DEFAULT false,
    incluye_tours boolean DEFAULT false,
    precio_base numeric(12,2) NOT NULL,
    precio_vuelo numeric(12,2) DEFAULT 0.00,
    precio_hotel numeric(12,2) DEFAULT 0.00,
    precio_extras numeric(12,2) DEFAULT 0.00,
    beneficio numeric(12,2) DEFAULT 0.00,
    nombre_hotel character varying(200),
    categoria_hotel character varying(20),
    numero_vuelo_ida character varying(20),
    numero_vuelo_regreso character varying(20),
    tour_operator character varying(100),
    observaciones text,
    itinerario text
);


ALTER TABLE public.operaciones_pack_viajes OWNER TO postgres;

--
-- TOC entry 4562 (class 0 OID 0)
-- Dependencies: 244
-- Name: TABLE operaciones_pack_viajes; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON TABLE public.operaciones_pack_viajes IS 'Detalle de operaciones de paquetes de viaje';


--
-- TOC entry 243 (class 1259 OID 16728)
-- Name: operaciones_pack_viajes_id_operacion_pack_viaje_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.operaciones_pack_viajes_id_operacion_pack_viaje_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.operaciones_pack_viajes_id_operacion_pack_viaje_seq OWNER TO postgres;

--
-- TOC entry 4563 (class 0 OID 0)
-- Dependencies: 243
-- Name: operaciones_pack_viajes_id_operacion_pack_viaje_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.operaciones_pack_viajes_id_operacion_pack_viaje_seq OWNED BY public.operaciones_pack_viajes.id_operacion_pack_viaje;


--
-- TOC entry 321 (class 1259 OID 17853)
-- Name: pack_alimentos_asignacion_comercios; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.pack_alimentos_asignacion_comercios (
    id_asignacion integer NOT NULL,
    id_pack integer NOT NULL,
    id_comercio integer NOT NULL,
    id_precio integer NOT NULL,
    activo boolean DEFAULT true,
    fecha_asignacion timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    asignado_por integer
);


ALTER TABLE public.pack_alimentos_asignacion_comercios OWNER TO postgres;

--
-- TOC entry 4564 (class 0 OID 0)
-- Dependencies: 321
-- Name: TABLE pack_alimentos_asignacion_comercios; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON TABLE public.pack_alimentos_asignacion_comercios IS 'Packs asignados a comercios completos (todos sus locales)';


--
-- TOC entry 320 (class 1259 OID 17852)
-- Name: pack_alimentos_asignacion_comercios_id_asignacion_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.pack_alimentos_asignacion_comercios_id_asignacion_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.pack_alimentos_asignacion_comercios_id_asignacion_seq OWNER TO postgres;

--
-- TOC entry 4565 (class 0 OID 0)
-- Dependencies: 320
-- Name: pack_alimentos_asignacion_comercios_id_asignacion_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.pack_alimentos_asignacion_comercios_id_asignacion_seq OWNED BY public.pack_alimentos_asignacion_comercios.id_asignacion;


--
-- TOC entry 325 (class 1259 OID 17905)
-- Name: pack_alimentos_asignacion_global; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.pack_alimentos_asignacion_global (
    id_asignacion integer NOT NULL,
    id_pack integer NOT NULL,
    id_precio integer NOT NULL,
    activo boolean DEFAULT true,
    fecha_asignacion timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    asignado_por integer
);


ALTER TABLE public.pack_alimentos_asignacion_global OWNER TO postgres;

--
-- TOC entry 4566 (class 0 OID 0)
-- Dependencies: 325
-- Name: TABLE pack_alimentos_asignacion_global; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON TABLE public.pack_alimentos_asignacion_global IS 'Packs asignados globalmente a todos los comercios';


--
-- TOC entry 324 (class 1259 OID 17904)
-- Name: pack_alimentos_asignacion_global_id_asignacion_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.pack_alimentos_asignacion_global_id_asignacion_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.pack_alimentos_asignacion_global_id_asignacion_seq OWNER TO postgres;

--
-- TOC entry 4567 (class 0 OID 0)
-- Dependencies: 324
-- Name: pack_alimentos_asignacion_global_id_asignacion_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.pack_alimentos_asignacion_global_id_asignacion_seq OWNED BY public.pack_alimentos_asignacion_global.id_asignacion;


--
-- TOC entry 323 (class 1259 OID 17879)
-- Name: pack_alimentos_asignacion_locales; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.pack_alimentos_asignacion_locales (
    id_asignacion integer NOT NULL,
    id_pack integer NOT NULL,
    id_local integer NOT NULL,
    id_precio integer NOT NULL,
    activo boolean DEFAULT true,
    fecha_asignacion timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    asignado_por integer
);


ALTER TABLE public.pack_alimentos_asignacion_locales OWNER TO postgres;

--
-- TOC entry 4568 (class 0 OID 0)
-- Dependencies: 323
-- Name: TABLE pack_alimentos_asignacion_locales; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON TABLE public.pack_alimentos_asignacion_locales IS 'Packs asignados a locales especificos';


--
-- TOC entry 322 (class 1259 OID 17878)
-- Name: pack_alimentos_asignacion_locales_id_asignacion_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.pack_alimentos_asignacion_locales_id_asignacion_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.pack_alimentos_asignacion_locales_id_asignacion_seq OWNER TO postgres;

--
-- TOC entry 4569 (class 0 OID 0)
-- Dependencies: 322
-- Name: pack_alimentos_asignacion_locales_id_asignacion_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.pack_alimentos_asignacion_locales_id_asignacion_seq OWNED BY public.pack_alimentos_asignacion_locales.id_asignacion;


--
-- TOC entry 317 (class 1259 OID 17819)
-- Name: pack_alimentos_imagenes; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.pack_alimentos_imagenes (
    id_imagen integer NOT NULL,
    id_pack integer NOT NULL,
    imagen_contenido bytea NOT NULL,
    imagen_nombre character varying(255),
    imagen_tipo character varying(50),
    descripcion character varying(500),
    orden integer DEFAULT 0,
    fecha_subida timestamp without time zone DEFAULT CURRENT_TIMESTAMP
);


ALTER TABLE public.pack_alimentos_imagenes OWNER TO postgres;

--
-- TOC entry 4570 (class 0 OID 0)
-- Dependencies: 317
-- Name: TABLE pack_alimentos_imagenes; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON TABLE public.pack_alimentos_imagenes IS 'Imagenes adicionales de los productos del pack';


--
-- TOC entry 316 (class 1259 OID 17818)
-- Name: pack_alimentos_imagenes_id_imagen_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.pack_alimentos_imagenes_id_imagen_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.pack_alimentos_imagenes_id_imagen_seq OWNER TO postgres;

--
-- TOC entry 4571 (class 0 OID 0)
-- Dependencies: 316
-- Name: pack_alimentos_imagenes_id_imagen_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.pack_alimentos_imagenes_id_imagen_seq OWNED BY public.pack_alimentos_imagenes.id_imagen;


--
-- TOC entry 319 (class 1259 OID 17835)
-- Name: pack_alimentos_precios; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.pack_alimentos_precios (
    id_precio integer NOT NULL,
    id_pack integer NOT NULL,
    divisa character varying(10) DEFAULT 'EUR'::character varying NOT NULL,
    precio numeric(12,2) NOT NULL,
    activo boolean DEFAULT true,
    fecha_creacion timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    fecha_modificacion timestamp without time zone DEFAULT CURRENT_TIMESTAMP
);


ALTER TABLE public.pack_alimentos_precios OWNER TO postgres;

--
-- TOC entry 4572 (class 0 OID 0)
-- Dependencies: 319
-- Name: TABLE pack_alimentos_precios; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON TABLE public.pack_alimentos_precios IS 'Precios del pack en diferentes divisas';


--
-- TOC entry 4573 (class 0 OID 0)
-- Dependencies: 319
-- Name: COLUMN pack_alimentos_precios.divisa; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON COLUMN public.pack_alimentos_precios.divisa IS 'Codigo de divisa: EUR, USD';


--
-- TOC entry 318 (class 1259 OID 17834)
-- Name: pack_alimentos_precios_id_precio_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.pack_alimentos_precios_id_precio_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.pack_alimentos_precios_id_precio_seq OWNER TO postgres;

--
-- TOC entry 4574 (class 0 OID 0)
-- Dependencies: 318
-- Name: pack_alimentos_precios_id_precio_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.pack_alimentos_precios_id_precio_seq OWNED BY public.pack_alimentos_precios.id_precio;


--
-- TOC entry 315 (class 1259 OID 17801)
-- Name: pack_alimentos_productos; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.pack_alimentos_productos (
    id_producto integer NOT NULL,
    id_pack integer NOT NULL,
    nombre_producto character varying(200) NOT NULL,
    descripcion text,
    cantidad integer DEFAULT 1,
    unidad_medida character varying(50) DEFAULT 'unidad'::character varying,
    orden integer DEFAULT 0,
    fecha_creacion timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    imagen bytea,
    imagen_nombre character varying(255),
    imagen_tipo character varying(50),
    detalles text
);


ALTER TABLE public.pack_alimentos_productos OWNER TO postgres;

--
-- TOC entry 4575 (class 0 OID 0)
-- Dependencies: 315
-- Name: TABLE pack_alimentos_productos; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON TABLE public.pack_alimentos_productos IS 'Productos individuales dentro de cada pack de alimentos';


--
-- TOC entry 4576 (class 0 OID 0)
-- Dependencies: 315
-- Name: COLUMN pack_alimentos_productos.unidad_medida; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON COLUMN public.pack_alimentos_productos.unidad_medida IS 'Unidad: kg, g, l, ml, unidad, paquete, etc.';


--
-- TOC entry 4577 (class 0 OID 0)
-- Dependencies: 315
-- Name: COLUMN pack_alimentos_productos.imagen; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON COLUMN public.pack_alimentos_productos.imagen IS 'Imagen del producto en formato binario';


--
-- TOC entry 4578 (class 0 OID 0)
-- Dependencies: 315
-- Name: COLUMN pack_alimentos_productos.imagen_nombre; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON COLUMN public.pack_alimentos_productos.imagen_nombre IS 'Nombre original del archivo de imagen';


--
-- TOC entry 4579 (class 0 OID 0)
-- Dependencies: 315
-- Name: COLUMN pack_alimentos_productos.imagen_tipo; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON COLUMN public.pack_alimentos_productos.imagen_tipo IS 'Tipo MIME de la imagen';


--
-- TOC entry 4580 (class 0 OID 0)
-- Dependencies: 315
-- Name: COLUMN pack_alimentos_productos.detalles; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON COLUMN public.pack_alimentos_productos.detalles IS 'Detalles adicionales del producto';


--
-- TOC entry 314 (class 1259 OID 17800)
-- Name: pack_alimentos_productos_id_producto_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.pack_alimentos_productos_id_producto_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.pack_alimentos_productos_id_producto_seq OWNER TO postgres;

--
-- TOC entry 4581 (class 0 OID 0)
-- Dependencies: 314
-- Name: pack_alimentos_productos_id_producto_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.pack_alimentos_productos_id_producto_seq OWNED BY public.pack_alimentos_productos.id_producto;


--
-- TOC entry 313 (class 1259 OID 17789)
-- Name: packs_alimentos; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.packs_alimentos (
    id_pack integer NOT NULL,
    nombre_pack character varying(200) NOT NULL,
    descripcion text,
    imagen_poster bytea,
    imagen_poster_nombre character varying(255),
    imagen_poster_tipo character varying(50),
    activo boolean DEFAULT true,
    fecha_creacion timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    fecha_modificacion timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    creado_por integer,
    modificado_por integer,
    id_pais integer
);


ALTER TABLE public.packs_alimentos OWNER TO postgres;

--
-- TOC entry 4582 (class 0 OID 0)
-- Dependencies: 313
-- Name: TABLE packs_alimentos; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON TABLE public.packs_alimentos IS 'Packs de alimentos creados por administradores';


--
-- TOC entry 4583 (class 0 OID 0)
-- Dependencies: 313
-- Name: COLUMN packs_alimentos.imagen_poster; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON COLUMN public.packs_alimentos.imagen_poster IS 'Imagen principal/poster del pack para mostrar en el front';


--
-- TOC entry 312 (class 1259 OID 17788)
-- Name: packs_alimentos_id_pack_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.packs_alimentos_id_pack_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.packs_alimentos_id_pack_seq OWNER TO postgres;

--
-- TOC entry 4584 (class 0 OID 0)
-- Dependencies: 312
-- Name: packs_alimentos_id_pack_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.packs_alimentos_id_pack_seq OWNED BY public.packs_alimentos.id_pack;


--
-- TOC entry 333 (class 1259 OID 18067)
-- Name: paises_designados; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.paises_designados (
    id_pais integer NOT NULL,
    nombre_pais character varying(100) NOT NULL,
    codigo_iso character varying(3),
    bandera_imagen bytea,
    bandera_nombre character varying(255),
    activo boolean DEFAULT true,
    fecha_creacion timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    fecha_modificacion timestamp without time zone
);


ALTER TABLE public.paises_designados OWNER TO postgres;

--
-- TOC entry 332 (class 1259 OID 18066)
-- Name: paises_designados_id_pais_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.paises_designados_id_pais_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.paises_designados_id_pais_seq OWNER TO postgres;

--
-- TOC entry 4585 (class 0 OID 0)
-- Dependencies: 332
-- Name: paises_designados_id_pais_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.paises_designados_id_pais_seq OWNED BY public.paises_designados.id_pais;


--
-- TOC entry 331 (class 1259 OID 18050)
-- Name: paises_destino; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.paises_destino (
    id_pais integer NOT NULL,
    nombre_pais character varying(100) NOT NULL,
    codigo_iso character varying(3),
    bandera_imagen bytea,
    bandera_nombre character varying(255),
    activo boolean DEFAULT true,
    fecha_creacion timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    fecha_modificacion timestamp without time zone
);


ALTER TABLE public.paises_destino OWNER TO postgres;

--
-- TOC entry 330 (class 1259 OID 18049)
-- Name: paises_destino_id_pais_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.paises_destino_id_pais_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.paises_destino_id_pais_seq OWNER TO postgres;

--
-- TOC entry 4586 (class 0 OID 0)
-- Dependencies: 330
-- Name: paises_destino_id_pais_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.paises_destino_id_pais_seq OWNED BY public.paises_destino.id_pais;


--
-- TOC entry 255 (class 1259 OID 16891)
-- Name: permisos_locales_usuarios; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.permisos_locales_usuarios (
    id_permiso_local integer NOT NULL,
    id_usuario integer NOT NULL,
    id_local integer NOT NULL,
    fecha_asignacion timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    asignado_por integer,
    activo boolean DEFAULT true,
    fecha_revocacion timestamp without time zone
);


ALTER TABLE public.permisos_locales_usuarios OWNER TO postgres;

--
-- TOC entry 4587 (class 0 OID 0)
-- Dependencies: 255
-- Name: TABLE permisos_locales_usuarios; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON TABLE public.permisos_locales_usuarios IS 'Permisos específicos de locales para usuarios flotantes';


--
-- TOC entry 254 (class 1259 OID 16890)
-- Name: permisos_locales_usuarios_id_permiso_local_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.permisos_locales_usuarios_id_permiso_local_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.permisos_locales_usuarios_id_permiso_local_seq OWNER TO postgres;

--
-- TOC entry 4588 (class 0 OID 0)
-- Dependencies: 254
-- Name: permisos_locales_usuarios_id_permiso_local_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.permisos_locales_usuarios_id_permiso_local_seq OWNED BY public.permisos_locales_usuarios.id_permiso_local;


--
-- TOC entry 222 (class 1259 OID 16419)
-- Name: permisos_modulos; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.permisos_modulos (
    id_permiso integer NOT NULL,
    id_comercio integer NOT NULL,
    modulo_divisas boolean DEFAULT false,
    modulo_pack_alimentos boolean DEFAULT false,
    modulo_billetes_avion boolean DEFAULT false,
    modulo_pack_viajes boolean DEFAULT false,
    fecha_asignacion timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    fecha_modificacion timestamp without time zone DEFAULT CURRENT_TIMESTAMP
);


ALTER TABLE public.permisos_modulos OWNER TO postgres;

--
-- TOC entry 4589 (class 0 OID 0)
-- Dependencies: 222
-- Name: TABLE permisos_modulos; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON TABLE public.permisos_modulos IS 'Define qué módulos tiene habilitado cada comercio';


--
-- TOC entry 221 (class 1259 OID 16418)
-- Name: permisos_modulos_id_permiso_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.permisos_modulos_id_permiso_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.permisos_modulos_id_permiso_seq OWNER TO postgres;

--
-- TOC entry 4590 (class 0 OID 0)
-- Dependencies: 221
-- Name: permisos_modulos_id_permiso_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.permisos_modulos_id_permiso_seq OWNED BY public.permisos_modulos.id_permiso;


--
-- TOC entry 218 (class 1259 OID 16391)
-- Name: roles; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.roles (
    id_rol integer NOT NULL,
    nombre_rol character varying(50) NOT NULL,
    descripcion text,
    fecha_creacion timestamp without time zone DEFAULT CURRENT_TIMESTAMP
);


ALTER TABLE public.roles OWNER TO postgres;

--
-- TOC entry 217 (class 1259 OID 16390)
-- Name: roles_id_rol_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.roles_id_rol_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.roles_id_rol_seq OWNER TO postgres;

--
-- TOC entry 4591 (class 0 OID 0)
-- Dependencies: 217
-- Name: roles_id_rol_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.roles_id_rol_seq OWNED BY public.roles.id_rol;


--
-- TOC entry 230 (class 1259 OID 16540)
-- Name: sesiones; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.sesiones (
    id_sesion integer NOT NULL,
    id_usuario integer NOT NULL,
    id_local_activo integer,
    token_jwt text NOT NULL,
    refresh_token text,
    ip_address character varying(50),
    user_agent text,
    dispositivo_info text,
    fecha_inicio timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    fecha_expiracion timestamp without time zone NOT NULL,
    fecha_ultimo_uso timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    sesion_activa boolean DEFAULT true,
    fecha_cierre timestamp without time zone,
    motivo_cierre character varying(100)
);


ALTER TABLE public.sesiones OWNER TO postgres;

--
-- TOC entry 4592 (class 0 OID 0)
-- Dependencies: 230
-- Name: TABLE sesiones; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON TABLE public.sesiones IS 'Gestión de tokens JWT y sesiones activas';


--
-- TOC entry 271 (class 1259 OID 17112)
-- Name: sesiones_historico; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.sesiones_historico (
    id_sesion_hist bigint NOT NULL,
    id_sesion_original integer,
    id_usuario integer,
    id_local_activo integer,
    duracion_minutos integer,
    fecha_inicio timestamp without time zone NOT NULL,
    fecha_cierre timestamp without time zone NOT NULL,
    motivo_cierre character varying(100),
    acciones_realizadas integer DEFAULT 0,
    modulos_utilizados text[],
    ip_address character varying(50),
    user_agent text,
    uuid_dispositivo uuid,
    fecha_archivado timestamp without time zone DEFAULT CURRENT_TIMESTAMP
);


ALTER TABLE public.sesiones_historico OWNER TO postgres;

--
-- TOC entry 4593 (class 0 OID 0)
-- Dependencies: 271
-- Name: TABLE sesiones_historico; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON TABLE public.sesiones_historico IS 'Histórico de sesiones cerradas';


--
-- TOC entry 270 (class 1259 OID 17111)
-- Name: sesiones_historico_id_sesion_hist_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.sesiones_historico_id_sesion_hist_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.sesiones_historico_id_sesion_hist_seq OWNER TO postgres;

--
-- TOC entry 4594 (class 0 OID 0)
-- Dependencies: 270
-- Name: sesiones_historico_id_sesion_hist_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.sesiones_historico_id_sesion_hist_seq OWNED BY public.sesiones_historico.id_sesion_hist;


--
-- TOC entry 229 (class 1259 OID 16539)
-- Name: sesiones_id_sesion_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.sesiones_id_sesion_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.sesiones_id_sesion_seq OWNER TO postgres;

--
-- TOC entry 4595 (class 0 OID 0)
-- Dependencies: 229
-- Name: sesiones_id_sesion_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.sesiones_id_sesion_seq OWNED BY public.sesiones.id_sesion;


--
-- TOC entry 263 (class 1259 OID 16993)
-- Name: tokens_recuperacion; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.tokens_recuperacion (
    id_token integer NOT NULL,
    id_usuario integer NOT NULL,
    token character varying(500) NOT NULL,
    token_hash character varying(255) NOT NULL,
    usado boolean DEFAULT false,
    fecha_creacion timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    fecha_expiracion timestamp without time zone NOT NULL,
    fecha_uso timestamp without time zone,
    ip_solicitud character varying(50),
    ip_uso character varying(50),
    motivo_solicitud character varying(100),
    intentos_uso integer DEFAULT 0,
    bloqueado boolean DEFAULT false
);


ALTER TABLE public.tokens_recuperacion OWNER TO postgres;

--
-- TOC entry 4596 (class 0 OID 0)
-- Dependencies: 263
-- Name: TABLE tokens_recuperacion; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON TABLE public.tokens_recuperacion IS 'Tokens temporales para recuperación de contraseña';


--
-- TOC entry 262 (class 1259 OID 16992)
-- Name: tokens_recuperacion_id_token_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.tokens_recuperacion_id_token_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.tokens_recuperacion_id_token_seq OWNER TO postgres;

--
-- TOC entry 4597 (class 0 OID 0)
-- Dependencies: 262
-- Name: tokens_recuperacion_id_token_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.tokens_recuperacion_id_token_seq OWNED BY public.tokens_recuperacion.id_token;


--
-- TOC entry 283 (class 1259 OID 17291)
-- Name: usuario_locales; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.usuario_locales (
    id integer NOT NULL,
    id_usuario integer NOT NULL,
    id_local integer NOT NULL,
    es_principal boolean DEFAULT false,
    fecha_asignacion timestamp without time zone DEFAULT now()
);


ALTER TABLE public.usuario_locales OWNER TO postgres;

--
-- TOC entry 281 (class 1259 OID 17273)
-- Name: usuario_locales_flooter; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.usuario_locales_flooter (
    id_usuario integer NOT NULL,
    id_local integer NOT NULL,
    fecha_asignacion timestamp without time zone DEFAULT CURRENT_TIMESTAMP
);


ALTER TABLE public.usuario_locales_flooter OWNER TO postgres;

--
-- TOC entry 282 (class 1259 OID 17290)
-- Name: usuario_locales_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.usuario_locales_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.usuario_locales_id_seq OWNER TO postgres;

--
-- TOC entry 4598 (class 0 OID 0)
-- Dependencies: 282
-- Name: usuario_locales_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.usuario_locales_id_seq OWNED BY public.usuario_locales.id;


--
-- TOC entry 226 (class 1259 OID 16463)
-- Name: usuarios; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.usuarios (
    id_usuario integer NOT NULL,
    id_comercio integer NOT NULL,
    id_local integer,
    id_rol integer NOT NULL,
    nombre character varying(100) NOT NULL,
    apellidos character varying(100) NOT NULL,
    correo character varying(100) NOT NULL,
    telefono character varying(50),
    numero_usuario character varying(50) NOT NULL,
    password_hash character varying(255) NOT NULL,
    es_flooter boolean DEFAULT false,
    idioma character varying(10) DEFAULT 'ESP'::character varying,
    activo boolean DEFAULT true,
    primer_login boolean DEFAULT true,
    ultimo_acceso timestamp without time zone,
    intentos_fallidos integer DEFAULT 0,
    bloqueado_hasta timestamp without time zone,
    fecha_creacion timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    fecha_modificacion timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT chk_flotante_local CHECK ((((es_flooter = true) AND (id_local IS NULL)) OR ((es_flooter = false) AND (id_local IS NOT NULL)) OR (id_rol = 1))),
    CONSTRAINT chk_usuario_email CHECK (((correo)::text ~* '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}$'::text))
);


ALTER TABLE public.usuarios OWNER TO postgres;

--
-- TOC entry 4599 (class 0 OID 0)
-- Dependencies: 226
-- Name: TABLE usuarios; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON TABLE public.usuarios IS 'Usuarios del sistema con diferentes roles y permisos';


--
-- TOC entry 225 (class 1259 OID 16462)
-- Name: usuarios_id_usuario_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.usuarios_id_usuario_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.usuarios_id_usuario_seq OWNER TO postgres;

--
-- TOC entry 4600 (class 0 OID 0)
-- Dependencies: 225
-- Name: usuarios_id_usuario_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.usuarios_id_usuario_seq OWNED BY public.usuarios.id_usuario;


--
-- TOC entry 288 (class 1259 OID 17358)
-- Name: v_admin_modulos; Type: VIEW; Schema: public; Owner: postgres
--

CREATE VIEW public.v_admin_modulos AS
 SELECT a.id_administrador,
    a.nombre_usuario,
    (((a.nombre)::text || ' '::text) || (a.apellidos)::text) AS nombre_completo,
    string_agg((am.nombre_modulo)::text, ', '::text) AS modulos_habilitados,
    count(am.nombre_modulo) AS cantidad_modulos
   FROM (public.administradores_allva a
     LEFT JOIN public.admin_modulos_habilitados am ON ((a.id_administrador = am.id_administrador)))
  GROUP BY a.id_administrador, a.nombre_usuario, a.nombre, a.apellidos;


ALTER VIEW public.v_admin_modulos OWNER TO postgres;

--
-- TOC entry 287 (class 1259 OID 17353)
-- Name: v_administradores_con_nivel; Type: VIEW; Schema: public; Owner: postgres
--

CREATE VIEW public.v_administradores_con_nivel AS
 SELECT a.id_administrador,
    a.nombre,
    a.apellidos,
    a.nombre_usuario,
    a.correo,
    a.telefono,
    a.activo,
    a.nivel_acceso,
    n.nombre_nivel,
    n.descripcion AS descripcion_nivel,
    n.puede_crear_usuarios_allva,
    n.puede_editar_comercios,
    n.puede_editar_usuarios_locales,
    n.acceso_todos_modulos
   FROM (public.administradores_allva a
     LEFT JOIN public.niveles_acceso n ON ((a.nivel_acceso = n.id_nivel)));


ALTER VIEW public.v_administradores_con_nivel OWNER TO postgres;

--
-- TOC entry 252 (class 1259 OID 16878)
-- Name: v_balance_actual; Type: VIEW; Schema: public; Owner: postgres
--

CREATE VIEW public.v_balance_actual AS
 SELECT bc.id_local,
    l.codigo_local,
    l.nombre_local,
    c.nombre_comercio,
    bc.divisa,
    sum(bc.monto) AS balance_total,
    sum(bc.beneficio) AS beneficio_total,
    count(*) AS numero_movimientos,
    max(bc.fecha_movimiento) AS ultimo_movimiento
   FROM ((public.balance_cuentas bc
     JOIN public.locales l ON ((bc.id_local = l.id_local)))
     JOIN public.comercios c ON ((bc.id_comercio = c.id_comercio)))
  GROUP BY bc.id_local, l.codigo_local, l.nombre_local, c.nombre_comercio, bc.divisa;


ALTER VIEW public.v_balance_actual OWNER TO postgres;

--
-- TOC entry 303 (class 1259 OID 17710)
-- Name: v_balance_divisas_comercio; Type: VIEW; Schema: public; Owner: postgres
--

CREATE VIEW public.v_balance_divisas_comercio AS
 SELECT bd.id_comercio,
    c.nombre_comercio,
    bd.codigo_divisa,
    bd.nombre_divisa,
    sum(
        CASE
            WHEN ((bd.tipo_movimiento)::text = 'ENTRADA'::text) THEN bd.cantidad_recibida
            ELSE (0)::numeric
        END) AS total_entradas,
    sum(
        CASE
            WHEN ((bd.tipo_movimiento)::text = 'SALIDA'::text) THEN bd.cantidad_recibida
            ELSE (0)::numeric
        END) AS total_salidas,
    sum(
        CASE
            WHEN ((bd.tipo_movimiento)::text = 'ENTRADA'::text) THEN bd.cantidad_recibida
            ELSE (- bd.cantidad_recibida)
        END) AS saldo_actual,
    count(*) AS total_operaciones,
    max(bd.fecha_registro) AS ultima_operacion
   FROM (public.balance_divisas bd
     JOIN public.comercios c ON ((bd.id_comercio = c.id_comercio)))
  GROUP BY bd.id_comercio, c.nombre_comercio, bd.codigo_divisa, bd.nombre_divisa
  ORDER BY c.nombre_comercio, bd.codigo_divisa;


ALTER VIEW public.v_balance_divisas_comercio OWNER TO postgres;

--
-- TOC entry 304 (class 1259 OID 17715)
-- Name: v_balance_divisas_local; Type: VIEW; Schema: public; Owner: postgres
--

CREATE VIEW public.v_balance_divisas_local AS
 SELECT bd.id_comercio,
    c.nombre_comercio,
    bd.id_local,
    l.codigo_local,
    l.nombre_local,
    bd.codigo_divisa,
    bd.nombre_divisa,
    sum(
        CASE
            WHEN ((bd.tipo_movimiento)::text = 'ENTRADA'::text) THEN bd.cantidad_recibida
            ELSE (0)::numeric
        END) AS total_entradas,
    sum(
        CASE
            WHEN ((bd.tipo_movimiento)::text = 'SALIDA'::text) THEN bd.cantidad_recibida
            ELSE (0)::numeric
        END) AS total_salidas,
    sum(
        CASE
            WHEN ((bd.tipo_movimiento)::text = 'ENTRADA'::text) THEN bd.cantidad_recibida
            ELSE (- bd.cantidad_recibida)
        END) AS saldo_actual,
    count(*) AS total_operaciones,
    max(bd.fecha_registro) AS ultima_operacion
   FROM ((public.balance_divisas bd
     JOIN public.comercios c ON ((bd.id_comercio = c.id_comercio)))
     JOIN public.locales l ON ((bd.id_local = l.id_local)))
  GROUP BY bd.id_comercio, c.nombre_comercio, bd.id_local, l.codigo_local, l.nombre_local, bd.codigo_divisa, bd.nombre_divisa
  ORDER BY c.nombre_comercio, l.codigo_local, bd.codigo_divisa;


ALTER VIEW public.v_balance_divisas_local OWNER TO postgres;

--
-- TOC entry 299 (class 1259 OID 17663)
-- Name: v_clientes_por_comercio; Type: VIEW; Schema: public; Owner: postgres
--

CREATE VIEW public.v_clientes_por_comercio AS
 SELECT co.id_comercio,
    co.nombre_comercio,
    count(cl.id_cliente) AS total_clientes,
    count(
        CASE
            WHEN (cl.activo = true) THEN 1
            ELSE NULL::integer
        END) AS clientes_activos,
    count(
        CASE
            WHEN (cl.fecha_ultima_compra > (CURRENT_DATE - '30 days'::interval)) THEN 1
            ELSE NULL::integer
        END) AS clientes_recientes
   FROM (public.comercios co
     LEFT JOIN public.clientes cl ON ((co.id_comercio = cl.id_comercio_registro)))
  WHERE (co.activo = true)
  GROUP BY co.id_comercio, co.nombre_comercio
  ORDER BY co.nombre_comercio;


ALTER VIEW public.v_clientes_por_comercio OWNER TO postgres;

--
-- TOC entry 4601 (class 0 OID 0)
-- Dependencies: 299
-- Name: VIEW v_clientes_por_comercio; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON VIEW public.v_clientes_por_comercio IS 'Vista de estadisticas de clientes agrupados por comercio';


--
-- TOC entry 274 (class 1259 OID 17166)
-- Name: v_dispositivos_usuario; Type: VIEW; Schema: public; Owner: postgres
--

CREATE VIEW public.v_dispositivos_usuario AS
 SELECT d.id_dispositivo,
    u.numero_usuario,
    (((u.nombre)::text || ' '::text) || (u.apellidos)::text) AS nombre_usuario,
    d.nombre_dispositivo,
    d.dispositivo_tipo,
    d.sistema_operativo,
    d.navegador,
    d.autorizado,
    d.activo,
    d.fecha_registro,
    d.fecha_ultimo_uso,
    d.numero_usos
   FROM (public.dispositivos_autorizados d
     JOIN public.usuarios u ON ((d.id_usuario = u.id_usuario)));


ALTER VIEW public.v_dispositivos_usuario OWNER TO postgres;

--
-- TOC entry 275 (class 1259 OID 17171)
-- Name: v_intentos_fallidos_recientes; Type: VIEW; Schema: public; Owner: postgres
--

CREATE VIEW public.v_intentos_fallidos_recientes AS
 SELECT numero_usuario,
    count(*) AS intentos_fallidos,
    max(fecha_intento) AS ultimo_intento,
    array_agg(DISTINCT ip_address) AS ips_origen,
    array_agg(DISTINCT motivo_fallo) AS motivos
   FROM public.intentos_login il
  WHERE ((exitoso = false) AND (fecha_intento > (now() - '24:00:00'::interval)))
  GROUP BY numero_usuario
 HAVING (count(*) >= 3);


ALTER VIEW public.v_intentos_fallidos_recientes OWNER TO postgres;

--
-- TOC entry 305 (class 1259 OID 17720)
-- Name: v_movimientos_divisas_recientes; Type: VIEW; Schema: public; Owner: postgres
--

CREATE VIEW public.v_movimientos_divisas_recientes AS
 SELECT bd.id_balance,
    bd.fecha_registro,
    c.nombre_comercio,
    l.codigo_local,
    bd.codigo_divisa,
    bd.tipo_movimiento,
    bd.cantidad_recibida,
    bd.cantidad_entregada_eur,
    bd.tasa_cambio_aplicada,
    o.numero_operacion,
    o.nombre_cliente
   FROM (((public.balance_divisas bd
     JOIN public.comercios c ON ((bd.id_comercio = c.id_comercio)))
     JOIN public.locales l ON ((bd.id_local = l.id_local)))
     LEFT JOIN public.operaciones o ON ((bd.id_operacion = o.id_operacion)))
  ORDER BY bd.fecha_registro DESC
 LIMIT 100;


ALTER VIEW public.v_movimientos_divisas_recientes OWNER TO postgres;

--
-- TOC entry 276 (class 1259 OID 17176)
-- Name: v_notificaciones_pendientes; Type: VIEW; Schema: public; Owner: postgres
--

CREATE VIEW public.v_notificaciones_pendientes AS
 SELECT n.id_usuario,
    u.numero_usuario,
    (((u.nombre)::text || ' '::text) || (u.apellidos)::text) AS nombre_usuario,
    count(*) AS notificaciones_pendientes,
    sum(
        CASE
            WHEN ((n.nivel_severidad)::text = 'CRITICAL'::text) THEN 1
            ELSE 0
        END) AS criticas,
    sum(
        CASE
            WHEN ((n.nivel_severidad)::text = 'DANGER'::text) THEN 1
            ELSE 0
        END) AS peligro,
    sum(
        CASE
            WHEN ((n.nivel_severidad)::text = 'WARNING'::text) THEN 1
            ELSE 0
        END) AS advertencias
   FROM (public.notificaciones_seguridad n
     JOIN public.usuarios u ON ((n.id_usuario = u.id_usuario)))
  WHERE ((n.leida = false) AND (n.archivada = false))
  GROUP BY n.id_usuario, u.numero_usuario, u.nombre, u.apellidos;


ALTER VIEW public.v_notificaciones_pendientes OWNER TO postgres;

--
-- TOC entry 251 (class 1259 OID 16873)
-- Name: v_operaciones_completas; Type: VIEW; Schema: public; Owner: postgres
--

CREATE VIEW public.v_operaciones_completas AS
 SELECT o.id_operacion,
    o.numero_operacion,
    o.modulo,
    o.tipo_operacion,
    o.estado,
    o.fecha_operacion,
    o.hora_operacion,
    o.importe_total,
    o.importe_pagado,
    o.importe_pendiente,
    o.localizador,
    c.nombre_comercio,
    l.codigo_local,
    l.nombre_local,
    u.numero_usuario,
    (((u.nombre)::text || ' '::text) || (u.apellidos)::text) AS nombre_usuario,
    (((cl.nombre)::text || ' '::text) || (cl.apellidos)::text) AS nombre_cliente,
    cl.telefono AS telefono_cliente,
    o.observaciones
   FROM ((((public.operaciones o
     JOIN public.comercios c ON ((o.id_comercio = c.id_comercio)))
     JOIN public.locales l ON ((o.id_local = l.id_local)))
     JOIN public.usuarios u ON ((o.id_usuario = u.id_usuario)))
     LEFT JOIN public.clientes cl ON ((o.id_cliente = cl.id_cliente)));


ALTER VIEW public.v_operaciones_completas OWNER TO postgres;

--
-- TOC entry 300 (class 1259 OID 17668)
-- Name: v_operaciones_divisas_comercio; Type: VIEW; Schema: public; Owner: postgres
--

CREATE VIEW public.v_operaciones_divisas_comercio AS
 SELECT o.id_operacion,
    o.numero_operacion,
    o.fecha_operacion,
    o.hora_operacion,
    co.id_comercio,
    co.nombre_comercio,
    l.id_local,
    l.codigo_local,
    l.nombre_local,
    o.id_usuario,
    o.nombre_usuario,
    o.id_cliente,
    o.nombre_cliente,
    od.divisa_origen,
    od.divisa_destino,
    od.cantidad_origen,
    od.cantidad_destino,
    od.tipo_cambio,
    o.estado
   FROM (((public.operaciones o
     JOIN public.operaciones_divisas od ON ((o.id_operacion = od.id_operacion)))
     JOIN public.comercios co ON ((o.id_comercio = co.id_comercio)))
     JOIN public.locales l ON ((o.id_local = l.id_local)))
  WHERE ((o.modulo)::text = 'DIVISAS'::text)
  ORDER BY o.fecha_operacion DESC, o.hora_operacion DESC;


ALTER VIEW public.v_operaciones_divisas_comercio OWNER TO postgres;

--
-- TOC entry 4602 (class 0 OID 0)
-- Dependencies: 300
-- Name: VIEW v_operaciones_divisas_comercio; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON VIEW public.v_operaciones_divisas_comercio IS 'Vista de operaciones de divisas con informacion completa de comercio, local y usuario';


--
-- TOC entry 253 (class 1259 OID 16883)
-- Name: v_resumen_operaciones; Type: VIEW; Schema: public; Owner: postgres
--

CREATE VIEW public.v_resumen_operaciones AS
 SELECT o.id_local,
    l.codigo_local,
    o.modulo,
    date(o.fecha_operacion) AS fecha,
    o.estado,
    count(*) AS cantidad_operaciones,
    sum(o.importe_total) AS total_facturado,
    sum(o.importe_pagado) AS total_cobrado,
    avg(o.importe_total) AS ticket_promedio
   FROM (public.operaciones o
     JOIN public.locales l ON ((o.id_local = l.id_local)))
  GROUP BY o.id_local, l.codigo_local, o.modulo, (date(o.fecha_operacion)), o.estado;


ALTER VIEW public.v_resumen_operaciones OWNER TO postgres;

--
-- TOC entry 234 (class 1259 OID 16610)
-- Name: v_sesiones_activas; Type: VIEW; Schema: public; Owner: postgres
--

CREATE VIEW public.v_sesiones_activas AS
 SELECT s.id_sesion,
    u.numero_usuario,
    (((u.nombre)::text || ' '::text) || (u.apellidos)::text) AS nombre_completo,
    c.nombre_comercio,
    l.codigo_local,
    s.fecha_inicio,
    s.fecha_ultimo_uso,
    s.ip_address
   FROM (((public.sesiones s
     JOIN public.usuarios u ON ((s.id_usuario = u.id_usuario)))
     JOIN public.comercios c ON ((u.id_comercio = c.id_comercio)))
     LEFT JOIN public.locales l ON ((s.id_local_activo = l.id_local)))
  WHERE ((s.sesion_activa = true) AND (s.fecha_expiracion > CURRENT_TIMESTAMP));


ALTER VIEW public.v_sesiones_activas OWNER TO postgres;

--
-- TOC entry 233 (class 1259 OID 16605)
-- Name: v_usuarios_completo; Type: VIEW; Schema: public; Owner: postgres
--

CREATE VIEW public.v_usuarios_completo AS
 SELECT u.id_usuario,
    u.numero_usuario,
    u.nombre,
    u.apellidos,
    u.correo,
    u.telefono,
    r.nombre_rol,
    c.nombre_comercio,
    l.codigo_local,
    l.nombre_local,
    u.es_flooter AS es_flotante,
    u.activo,
    u.ultimo_acceso,
    u.fecha_creacion
   FROM (((public.usuarios u
     JOIN public.roles r ON ((u.id_rol = r.id_rol)))
     JOIN public.comercios c ON ((u.id_comercio = c.id_comercio)))
     LEFT JOIN public.locales l ON ((u.id_local = l.id_local)));


ALTER VIEW public.v_usuarios_completo OWNER TO postgres;

--
-- TOC entry 296 (class 1259 OID 17548)
-- Name: vista_config_divisas_locales; Type: VIEW; Schema: public; Owner: postgres
--

CREATE VIEW public.vista_config_divisas_locales AS
 SELECT l.id_local,
    l.codigo_local,
    l.nombre_local,
    l.id_comercio,
    c.nombre_comercio,
    l.activo AS local_activo,
    c.activo AS comercio_activo,
    COALESCE(l.comision_divisas, (0)::numeric) AS comision_local,
    COALESCE(c.porcentaje_comision_divisas, (0)::numeric) AS margen_comercio,
    ( SELECT configuracion_sistema.valor_decimal
           FROM public.configuracion_sistema
          WHERE ((configuracion_sistema.clave)::text = 'margen_divisas_global'::text)) AS margen_global,
        CASE
            WHEN (l.comision_divisas > (0)::numeric) THEN l.comision_divisas
            WHEN (c.porcentaje_comision_divisas > (0)::numeric) THEN c.porcentaje_comision_divisas
            ELSE ( SELECT configuracion_sistema.valor_decimal
               FROM public.configuracion_sistema
              WHERE ((configuracion_sistema.clave)::text = 'margen_divisas_global'::text))
        END AS margen_efectivo,
        CASE
            WHEN (l.comision_divisas > (0)::numeric) THEN 'LOCAL'::text
            WHEN (c.porcentaje_comision_divisas > (0)::numeric) THEN 'COMERCIO'::text
            ELSE 'GLOBAL'::text
        END AS origen_margen
   FROM (public.locales l
     JOIN public.comercios c ON ((l.id_comercio = c.id_comercio)))
  ORDER BY c.nombre_comercio, l.nombre_local;


ALTER VIEW public.vista_config_divisas_locales OWNER TO postgres;

--
-- TOC entry 4603 (class 0 OID 0)
-- Dependencies: 296
-- Name: VIEW vista_config_divisas_locales; Type: COMMENT; Schema: public; Owner: postgres
--

COMMENT ON VIEW public.vista_config_divisas_locales IS 'Vista que muestra el margen de divisas efectivo para cada local';


--
-- TOC entry 291 (class 1259 OID 17475)
-- Name: vista_estado_licencias; Type: VIEW; Schema: public; Owner: postgres
--

CREATE VIEW public.vista_estado_licencias AS
 SELECT id_licencia,
    codigo_licencia,
    nombre_cliente,
    email_cliente,
    activa,
    usada,
    fecha_emision,
    fecha_expiracion,
    fecha_activacion,
    id_maquina,
    id_comercio,
        CASE
            WHEN (NOT activa) THEN 'Desactivada'::text
            WHEN ((fecha_expiracion IS NOT NULL) AND (fecha_expiracion < CURRENT_TIMESTAMP)) THEN 'Expirada'::text
            WHEN usada THEN 'En uso'::text
            ELSE 'Disponible'::text
        END AS estado,
        CASE
            WHEN (fecha_expiracion IS NULL) THEN 'Permanente'::text
            ELSE to_char(fecha_expiracion, 'DD/MM/YYYY'::text)
        END AS vencimiento
   FROM public.licencias l;


ALTER VIEW public.vista_estado_licencias OWNER TO postgres;

--
-- TOC entry 3805 (class 2604 OID 17329)
-- Name: admin_modulos_habilitados id_admin_modulo; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.admin_modulos_habilitados ALTER COLUMN id_admin_modulo SET DEFAULT nextval('public.admin_modulos_habilitados_id_admin_modulo_seq'::regclass);


--
-- TOC entry 3778 (class 2604 OID 17234)
-- Name: administradores_allva id_administrador; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.administradores_allva ALTER COLUMN id_administrador SET DEFAULT nextval('public.administradores_allva_id_administrador_seq'::regclass);


--
-- TOC entry 3668 (class 2604 OID 16571)
-- Name: audit_log id_log; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.audit_log ALTER COLUMN id_log SET DEFAULT nextval('public.audit_log_id_log_seq'::regclass);


--
-- TOC entry 3705 (class 2604 OID 16759)
-- Name: balance_cuentas id_balance; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.balance_cuentas ALTER COLUMN id_balance SET DEFAULT nextval('public.balance_cuentas_id_balance_seq'::regclass);


--
-- TOC entry 3822 (class 2604 OID 17677)
-- Name: balance_divisas id_balance; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.balance_divisas ALTER COLUMN id_balance SET DEFAULT nextval('public.balance_divisas_id_balance_seq'::regclass);


--
-- TOC entry 3761 (class 2604 OID 17060)
-- Name: cambios_usuarios id_cambio; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.cambios_usuarios ALTER COLUMN id_cambio SET DEFAULT nextval('public.cambios_usuarios_id_cambio_seq'::regclass);


--
-- TOC entry 3831 (class 2604 OID 17768)
-- Name: cierres_dia id_cierre; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.cierres_dia ALTER COLUMN id_cierre SET DEFAULT nextval('public.cierres_dia_id_cierre_seq'::regclass);


--
-- TOC entry 3659 (class 2604 OID 16509)
-- Name: clientes id_cliente; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.clientes ALTER COLUMN id_cliente SET DEFAULT nextval('public.clientes_id_cliente_seq'::regclass);


--
-- TOC entry 3865 (class 2604 OID 18009)
-- Name: clientes_beneficiarios id_beneficiario; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.clientes_beneficiarios ALTER COLUMN id_beneficiario SET DEFAULT nextval('public.clientes_beneficiarios_id_beneficiario_seq'::regclass);


--
-- TOC entry 3624 (class 2604 OID 16406)
-- Name: comercios id_comercio; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.comercios ALTER COLUMN id_comercio SET DEFAULT nextval('public.comercios_id_comercio_seq'::regclass);


--
-- TOC entry 3774 (class 2604 OID 17139)
-- Name: configuracion_2fa id_2fa; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.configuracion_2fa ALTER COLUMN id_2fa SET DEFAULT nextval('public.configuracion_2fa_id_2fa_seq'::regclass);


--
-- TOC entry 3740 (class 2604 OID 17020)
-- Name: configuracion_seguridad id_config; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.configuracion_seguridad ALTER COLUMN id_config SET DEFAULT nextval('public.configuracion_seguridad_id_config_seq'::regclass);


--
-- TOC entry 3816 (class 2604 OID 17537)
-- Name: configuracion_sistema id_config; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.configuracion_sistema ALTER COLUMN id_config SET DEFAULT nextval('public.configuracion_sistema_id_config_seq'::regclass);


--
-- TOC entry 3793 (class 2604 OID 17268)
-- Name: correlativo_locales id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.correlativo_locales ALTER COLUMN id SET DEFAULT nextval('public.correlativo_locales_id_seq'::regclass);


--
-- TOC entry 3828 (class 2604 OID 17751)
-- Name: correlativos_operaciones id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.correlativos_operaciones ALTER COLUMN id SET DEFAULT nextval('public.correlativos_operaciones_id_seq'::regclass);


--
-- TOC entry 3709 (class 2604 OID 16797)
-- Name: cuentas_bancarias id_cuenta; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.cuentas_bancarias ALTER COLUMN id_cuenta SET DEFAULT nextval('public.cuentas_bancarias_id_cuenta_seq'::regclass);


--
-- TOC entry 3880 (class 2604 OID 18088)
-- Name: depositos_pack_alimentos id_deposito; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.depositos_pack_alimentos ALTER COLUMN id_deposito SET DEFAULT nextval('public.depositos_pack_alimentos_id_deposito_seq'::regclass);


--
-- TOC entry 3722 (class 2604 OID 16923)
-- Name: dispositivos_autorizados id_dispositivo; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.dispositivos_autorizados ALTER COLUMN id_dispositivo SET DEFAULT nextval('public.dispositivos_autorizados_id_dispositivo_seq'::regclass);


--
-- TOC entry 3819 (class 2604 OID 17558)
-- Name: divisas_favoritas id_favorita; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.divisas_favoritas ALTER COLUMN id_favorita SET DEFAULT nextval('public.divisas_favoritas_id_favorita_seq'::regclass);


--
-- TOC entry 3825 (class 2604 OID 17735)
-- Name: divisas_favoritas_local id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.divisas_favoritas_local ALTER COLUMN id SET DEFAULT nextval('public.divisas_favoritas_local_id_seq'::regclass);


--
-- TOC entry 3861 (class 2604 OID 17954)
-- Name: historial_generacion_pdf id_generacion; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.historial_generacion_pdf ALTER COLUMN id_generacion SET DEFAULT nextval('public.historial_generacion_pdf_id_generacion_seq'::regclass);


--
-- TOC entry 3728 (class 2604 OID 16953)
-- Name: historial_passwords id_historial; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.historial_passwords ALTER COLUMN id_historial SET DEFAULT nextval('public.historial_passwords_id_historial_seq'::regclass);


--
-- TOC entry 3715 (class 2604 OID 16826)
-- Name: incidencias id_incidencia; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.incidencias ALTER COLUMN id_incidencia SET DEFAULT nextval('public.incidencias_id_incidencia_seq'::regclass);


--
-- TOC entry 3731 (class 2604 OID 16974)
-- Name: intentos_login id_intento; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.intentos_login ALTER COLUMN id_intento SET DEFAULT nextval('public.intentos_login_id_intento_seq'::regclass);


--
-- TOC entry 3807 (class 2604 OID 17459)
-- Name: licencias id_licencia; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.licencias ALTER COLUMN id_licencia SET DEFAULT nextval('public.licencias_id_licencia_seq'::regclass);


--
-- TOC entry 3636 (class 2604 OID 16442)
-- Name: locales id_local; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.locales ALTER COLUMN id_local SET DEFAULT nextval('public.locales_id_local_seq'::regclass);


--
-- TOC entry 3764 (class 2604 OID 17090)
-- Name: notificaciones_seguridad id_notificacion; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.notificaciones_seguridad ALTER COLUMN id_notificacion SET DEFAULT nextval('public.notificaciones_seguridad_id_notificacion_seq'::regclass);


--
-- TOC entry 3671 (class 2604 OID 16621)
-- Name: operaciones id_operacion; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.operaciones ALTER COLUMN id_operacion SET DEFAULT nextval('public.operaciones_id_operacion_seq'::regclass);


--
-- TOC entry 3683 (class 2604 OID 16688)
-- Name: operaciones_billetes id_operacion_billete; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.operaciones_billetes ALTER COLUMN id_operacion_billete SET DEFAULT nextval('public.operaciones_billetes_id_operacion_billete_seq'::regclass);


--
-- TOC entry 3678 (class 2604 OID 16668)
-- Name: operaciones_divisas id_operacion_divisa; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.operaciones_divisas ALTER COLUMN id_operacion_divisa SET DEFAULT nextval('public.operaciones_divisas_id_operacion_divisa_seq'::regclass);


--
-- TOC entry 3690 (class 2604 OID 16712)
-- Name: operaciones_pack_alimentos id_operacion_pack_alimento; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.operaciones_pack_alimentos ALTER COLUMN id_operacion_pack_alimento SET DEFAULT nextval('public.operaciones_pack_alimentos_id_operacion_pack_alimento_seq'::regclass);


--
-- TOC entry 3694 (class 2604 OID 16732)
-- Name: operaciones_pack_viajes id_operacion_pack_viaje; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.operaciones_pack_viajes ALTER COLUMN id_operacion_pack_viaje SET DEFAULT nextval('public.operaciones_pack_viajes_id_operacion_pack_viaje_seq'::regclass);


--
-- TOC entry 3852 (class 2604 OID 17856)
-- Name: pack_alimentos_asignacion_comercios id_asignacion; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.pack_alimentos_asignacion_comercios ALTER COLUMN id_asignacion SET DEFAULT nextval('public.pack_alimentos_asignacion_comercios_id_asignacion_seq'::regclass);


--
-- TOC entry 3858 (class 2604 OID 17908)
-- Name: pack_alimentos_asignacion_global id_asignacion; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.pack_alimentos_asignacion_global ALTER COLUMN id_asignacion SET DEFAULT nextval('public.pack_alimentos_asignacion_global_id_asignacion_seq'::regclass);


--
-- TOC entry 3855 (class 2604 OID 17882)
-- Name: pack_alimentos_asignacion_locales id_asignacion; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.pack_alimentos_asignacion_locales ALTER COLUMN id_asignacion SET DEFAULT nextval('public.pack_alimentos_asignacion_locales_id_asignacion_seq'::regclass);


--
-- TOC entry 3844 (class 2604 OID 17822)
-- Name: pack_alimentos_imagenes id_imagen; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.pack_alimentos_imagenes ALTER COLUMN id_imagen SET DEFAULT nextval('public.pack_alimentos_imagenes_id_imagen_seq'::regclass);


--
-- TOC entry 3847 (class 2604 OID 17838)
-- Name: pack_alimentos_precios id_precio; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.pack_alimentos_precios ALTER COLUMN id_precio SET DEFAULT nextval('public.pack_alimentos_precios_id_precio_seq'::regclass);


--
-- TOC entry 3839 (class 2604 OID 17804)
-- Name: pack_alimentos_productos id_producto; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.pack_alimentos_productos ALTER COLUMN id_producto SET DEFAULT nextval('public.pack_alimentos_productos_id_producto_seq'::regclass);


--
-- TOC entry 3835 (class 2604 OID 17792)
-- Name: packs_alimentos id_pack; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.packs_alimentos ALTER COLUMN id_pack SET DEFAULT nextval('public.packs_alimentos_id_pack_seq'::regclass);


--
-- TOC entry 3877 (class 2604 OID 18070)
-- Name: paises_designados id_pais; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.paises_designados ALTER COLUMN id_pais SET DEFAULT nextval('public.paises_designados_id_pais_seq'::regclass);


--
-- TOC entry 3874 (class 2604 OID 18053)
-- Name: paises_destino id_pais; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.paises_destino ALTER COLUMN id_pais SET DEFAULT nextval('public.paises_destino_id_pais_seq'::regclass);


--
-- TOC entry 3719 (class 2604 OID 16894)
-- Name: permisos_locales_usuarios id_permiso_local; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.permisos_locales_usuarios ALTER COLUMN id_permiso_local SET DEFAULT nextval('public.permisos_locales_usuarios_id_permiso_local_seq'::regclass);


--
-- TOC entry 3629 (class 2604 OID 16422)
-- Name: permisos_modulos id_permiso; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.permisos_modulos ALTER COLUMN id_permiso SET DEFAULT nextval('public.permisos_modulos_id_permiso_seq'::regclass);


--
-- TOC entry 3622 (class 2604 OID 16394)
-- Name: roles id_rol; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.roles ALTER COLUMN id_rol SET DEFAULT nextval('public.roles_id_rol_seq'::regclass);


--
-- TOC entry 3664 (class 2604 OID 16543)
-- Name: sesiones id_sesion; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.sesiones ALTER COLUMN id_sesion SET DEFAULT nextval('public.sesiones_id_sesion_seq'::regclass);


--
-- TOC entry 3771 (class 2604 OID 17115)
-- Name: sesiones_historico id_sesion_hist; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.sesiones_historico ALTER COLUMN id_sesion_hist SET DEFAULT nextval('public.sesiones_historico_id_sesion_hist_seq'::regclass);


--
-- TOC entry 3735 (class 2604 OID 16996)
-- Name: tokens_recuperacion id_token; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.tokens_recuperacion ALTER COLUMN id_token SET DEFAULT nextval('public.tokens_recuperacion_id_token_seq'::regclass);


--
-- TOC entry 3797 (class 2604 OID 17294)
-- Name: usuario_locales id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.usuario_locales ALTER COLUMN id SET DEFAULT nextval('public.usuario_locales_id_seq'::regclass);


--
-- TOC entry 3651 (class 2604 OID 16466)
-- Name: usuarios id_usuario; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.usuarios ALTER COLUMN id_usuario SET DEFAULT nextval('public.usuarios_id_usuario_seq'::regclass);


--
-- TOC entry 4099 (class 2606 OID 17334)
-- Name: admin_modulos_habilitados admin_modulos_habilitados_id_administrador_nombre_modulo_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.admin_modulos_habilitados
    ADD CONSTRAINT admin_modulos_habilitados_id_administrador_nombre_modulo_key UNIQUE (id_administrador, nombre_modulo);


--
-- TOC entry 4101 (class 2606 OID 17332)
-- Name: admin_modulos_habilitados admin_modulos_habilitados_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.admin_modulos_habilitados
    ADD CONSTRAINT admin_modulos_habilitados_pkey PRIMARY KEY (id_admin_modulo);


--
-- TOC entry 4080 (class 2606 OID 17256)
-- Name: administradores_allva administradores_allva_correo_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.administradores_allva
    ADD CONSTRAINT administradores_allva_correo_key UNIQUE (correo);


--
-- TOC entry 4082 (class 2606 OID 17254)
-- Name: administradores_allva administradores_allva_nombre_usuario_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.administradores_allva
    ADD CONSTRAINT administradores_allva_nombre_usuario_key UNIQUE (nombre_usuario);


--
-- TOC entry 4084 (class 2606 OID 17252)
-- Name: administradores_allva administradores_allva_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.administradores_allva
    ADD CONSTRAINT administradores_allva_pkey PRIMARY KEY (id_administrador);


--
-- TOC entry 3951 (class 2606 OID 16577)
-- Name: audit_log audit_log_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.audit_log
    ADD CONSTRAINT audit_log_pkey PRIMARY KEY (id_log);


--
-- TOC entry 3992 (class 2606 OID 16766)
-- Name: balance_cuentas balance_cuentas_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.balance_cuentas
    ADD CONSTRAINT balance_cuentas_pkey PRIMARY KEY (id_balance);


--
-- TOC entry 4126 (class 2606 OID 17683)
-- Name: balance_divisas balance_divisas_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.balance_divisas
    ADD CONSTRAINT balance_divisas_pkey PRIMARY KEY (id_balance);


--
-- TOC entry 4056 (class 2606 OID 17066)
-- Name: cambios_usuarios cambios_usuarios_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.cambios_usuarios
    ADD CONSTRAINT cambios_usuarios_pkey PRIMARY KEY (id_cambio);


--
-- TOC entry 4143 (class 2606 OID 17775)
-- Name: cierres_dia cierres_dia_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.cierres_dia
    ADD CONSTRAINT cierres_dia_pkey PRIMARY KEY (id_cierre);


--
-- TOC entry 4182 (class 2606 OID 18021)
-- Name: clientes_beneficiarios clientes_beneficiarios_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.clientes_beneficiarios
    ADD CONSTRAINT clientes_beneficiarios_pkey PRIMARY KEY (id_beneficiario);


--
-- TOC entry 3927 (class 2606 OID 16516)
-- Name: clientes clientes_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.clientes
    ADD CONSTRAINT clientes_pkey PRIMARY KEY (id_cliente);


--
-- TOC entry 3898 (class 2606 OID 16415)
-- Name: comercios comercios_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.comercios
    ADD CONSTRAINT comercios_pkey PRIMARY KEY (id_comercio);


--
-- TOC entry 4074 (class 2606 OID 17146)
-- Name: configuracion_2fa configuracion_2fa_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.configuracion_2fa
    ADD CONSTRAINT configuracion_2fa_pkey PRIMARY KEY (id_2fa);


--
-- TOC entry 4051 (class 2606 OID 17042)
-- Name: configuracion_seguridad configuracion_seguridad_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.configuracion_seguridad
    ADD CONSTRAINT configuracion_seguridad_pkey PRIMARY KEY (id_config);


--
-- TOC entry 4117 (class 2606 OID 17545)
-- Name: configuracion_sistema configuracion_sistema_clave_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.configuracion_sistema
    ADD CONSTRAINT configuracion_sistema_clave_key UNIQUE (clave);


--
-- TOC entry 4119 (class 2606 OID 17543)
-- Name: configuracion_sistema configuracion_sistema_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.configuracion_sistema
    ADD CONSTRAINT configuracion_sistema_pkey PRIMARY KEY (id_config);


--
-- TOC entry 4115 (class 2606 OID 17495)
-- Name: correlativo_locales_global correlativo_locales_global_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.correlativo_locales_global
    ADD CONSTRAINT correlativo_locales_global_pkey PRIMARY KEY (id);


--
-- TOC entry 4089 (class 2606 OID 17272)
-- Name: correlativo_locales correlativo_locales_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.correlativo_locales
    ADD CONSTRAINT correlativo_locales_pkey PRIMARY KEY (id);


--
-- TOC entry 4138 (class 2606 OID 17757)
-- Name: correlativos_operaciones correlativos_operaciones_id_local_prefijo_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.correlativos_operaciones
    ADD CONSTRAINT correlativos_operaciones_id_local_prefijo_key UNIQUE (id_local, prefijo);


--
-- TOC entry 4140 (class 2606 OID 17755)
-- Name: correlativos_operaciones correlativos_operaciones_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.correlativos_operaciones
    ADD CONSTRAINT correlativos_operaciones_pkey PRIMARY KEY (id);


--
-- TOC entry 4000 (class 2606 OID 16806)
-- Name: cuentas_bancarias cuentas_bancarias_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.cuentas_bancarias
    ADD CONSTRAINT cuentas_bancarias_pkey PRIMARY KEY (id_cuenta);


--
-- TOC entry 4195 (class 2606 OID 18096)
-- Name: depositos_pack_alimentos depositos_pack_alimentos_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.depositos_pack_alimentos
    ADD CONSTRAINT depositos_pack_alimentos_pkey PRIMARY KEY (id_deposito);


--
-- TOC entry 4024 (class 2606 OID 16932)
-- Name: dispositivos_autorizados dispositivos_autorizados_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.dispositivos_autorizados
    ADD CONSTRAINT dispositivos_autorizados_pkey PRIMARY KEY (id_dispositivo);


--
-- TOC entry 4121 (class 2606 OID 17564)
-- Name: divisas_favoritas divisas_favoritas_id_local_codigo_divisa_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.divisas_favoritas
    ADD CONSTRAINT divisas_favoritas_id_local_codigo_divisa_key UNIQUE (id_local, codigo_divisa);


--
-- TOC entry 4134 (class 2606 OID 17739)
-- Name: divisas_favoritas_local divisas_favoritas_local_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.divisas_favoritas_local
    ADD CONSTRAINT divisas_favoritas_local_pkey PRIMARY KEY (id);


--
-- TOC entry 4123 (class 2606 OID 17562)
-- Name: divisas_favoritas divisas_favoritas_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.divisas_favoritas
    ADD CONSTRAINT divisas_favoritas_pkey PRIMARY KEY (id_favorita);


--
-- TOC entry 4176 (class 2606 OID 17961)
-- Name: historial_generacion_pdf historial_generacion_pdf_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.historial_generacion_pdf
    ADD CONSTRAINT historial_generacion_pdf_pkey PRIMARY KEY (id_generacion);


--
-- TOC entry 4032 (class 2606 OID 16957)
-- Name: historial_passwords historial_passwords_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.historial_passwords
    ADD CONSTRAINT historial_passwords_pkey PRIMARY KEY (id_historial);


--
-- TOC entry 4013 (class 2606 OID 16835)
-- Name: incidencias incidencias_numero_incidencia_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.incidencias
    ADD CONSTRAINT incidencias_numero_incidencia_key UNIQUE (numero_incidencia);


--
-- TOC entry 4015 (class 2606 OID 16833)
-- Name: incidencias incidencias_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.incidencias
    ADD CONSTRAINT incidencias_pkey PRIMARY KEY (id_incidencia);


--
-- TOC entry 4041 (class 2606 OID 16981)
-- Name: intentos_login intentos_login_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.intentos_login
    ADD CONSTRAINT intentos_login_pkey PRIMARY KEY (id_intento);


--
-- TOC entry 4109 (class 2606 OID 17469)
-- Name: licencias licencias_codigo_licencia_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.licencias
    ADD CONSTRAINT licencias_codigo_licencia_key UNIQUE (codigo_licencia);


--
-- TOC entry 4111 (class 2606 OID 17467)
-- Name: licencias licencias_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.licencias
    ADD CONSTRAINT licencias_pkey PRIMARY KEY (id_licencia);


--
-- TOC entry 3911 (class 2606 OID 16453)
-- Name: locales locales_codigo_local_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.locales
    ADD CONSTRAINT locales_codigo_local_key UNIQUE (codigo_local);


--
-- TOC entry 3913 (class 2606 OID 16451)
-- Name: locales locales_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.locales
    ADD CONSTRAINT locales_pkey PRIMARY KEY (id_local);


--
-- TOC entry 4097 (class 2606 OID 17324)
-- Name: niveles_acceso niveles_acceso_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.niveles_acceso
    ADD CONSTRAINT niveles_acceso_pkey PRIMARY KEY (id_nivel);


--
-- TOC entry 4067 (class 2606 OID 17100)
-- Name: notificaciones_seguridad notificaciones_seguridad_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.notificaciones_seguridad
    ADD CONSTRAINT notificaciones_seguridad_pkey PRIMARY KEY (id_notificacion);


--
-- TOC entry 4113 (class 2606 OID 17486)
-- Name: numeros_locales_liberados numeros_locales_liberados_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.numeros_locales_liberados
    ADD CONSTRAINT numeros_locales_liberados_pkey PRIMARY KEY (numero);


--
-- TOC entry 3980 (class 2606 OID 16698)
-- Name: operaciones_billetes operaciones_billetes_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.operaciones_billetes
    ADD CONSTRAINT operaciones_billetes_pkey PRIMARY KEY (id_operacion_billete);


--
-- TOC entry 3974 (class 2606 OID 16676)
-- Name: operaciones_divisas operaciones_divisas_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.operaciones_divisas
    ADD CONSTRAINT operaciones_divisas_pkey PRIMARY KEY (id_operacion_divisa);


--
-- TOC entry 3968 (class 2606 OID 17996)
-- Name: operaciones operaciones_numero_operacion_local_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.operaciones
    ADD CONSTRAINT operaciones_numero_operacion_local_key UNIQUE (id_local, numero_operacion);


--
-- TOC entry 3985 (class 2606 OID 16719)
-- Name: operaciones_pack_alimentos operaciones_pack_alimentos_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.operaciones_pack_alimentos
    ADD CONSTRAINT operaciones_pack_alimentos_pkey PRIMARY KEY (id_operacion_pack_alimento);


--
-- TOC entry 3990 (class 2606 OID 16746)
-- Name: operaciones_pack_viajes operaciones_pack_viajes_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.operaciones_pack_viajes
    ADD CONSTRAINT operaciones_pack_viajes_pkey PRIMARY KEY (id_operacion_pack_viaje);


--
-- TOC entry 3970 (class 2606 OID 16633)
-- Name: operaciones operaciones_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.operaciones
    ADD CONSTRAINT operaciones_pkey PRIMARY KEY (id_operacion);


--
-- TOC entry 4162 (class 2606 OID 17860)
-- Name: pack_alimentos_asignacion_comercios pack_alimentos_asignacion_comercios_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.pack_alimentos_asignacion_comercios
    ADD CONSTRAINT pack_alimentos_asignacion_comercios_pkey PRIMARY KEY (id_asignacion);


--
-- TOC entry 4172 (class 2606 OID 17912)
-- Name: pack_alimentos_asignacion_global pack_alimentos_asignacion_global_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.pack_alimentos_asignacion_global
    ADD CONSTRAINT pack_alimentos_asignacion_global_pkey PRIMARY KEY (id_asignacion);


--
-- TOC entry 4168 (class 2606 OID 17886)
-- Name: pack_alimentos_asignacion_locales pack_alimentos_asignacion_locales_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.pack_alimentos_asignacion_locales
    ADD CONSTRAINT pack_alimentos_asignacion_locales_pkey PRIMARY KEY (id_asignacion);


--
-- TOC entry 4153 (class 2606 OID 17828)
-- Name: pack_alimentos_imagenes pack_alimentos_imagenes_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.pack_alimentos_imagenes
    ADD CONSTRAINT pack_alimentos_imagenes_pkey PRIMARY KEY (id_imagen);


--
-- TOC entry 4156 (class 2606 OID 17844)
-- Name: pack_alimentos_precios pack_alimentos_precios_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.pack_alimentos_precios
    ADD CONSTRAINT pack_alimentos_precios_pkey PRIMARY KEY (id_precio);


--
-- TOC entry 4150 (class 2606 OID 17812)
-- Name: pack_alimentos_productos pack_alimentos_productos_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.pack_alimentos_productos
    ADD CONSTRAINT pack_alimentos_productos_pkey PRIMARY KEY (id_producto);


--
-- TOC entry 4147 (class 2606 OID 17799)
-- Name: packs_alimentos packs_alimentos_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.packs_alimentos
    ADD CONSTRAINT packs_alimentos_pkey PRIMARY KEY (id_pack);


--
-- TOC entry 4193 (class 2606 OID 18076)
-- Name: paises_designados paises_designados_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.paises_designados
    ADD CONSTRAINT paises_designados_pkey PRIMARY KEY (id_pais);


--
-- TOC entry 4190 (class 2606 OID 18059)
-- Name: paises_destino paises_destino_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.paises_destino
    ADD CONSTRAINT paises_destino_pkey PRIMARY KEY (id_pais);


--
-- TOC entry 4020 (class 2606 OID 16898)
-- Name: permisos_locales_usuarios permisos_locales_usuarios_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.permisos_locales_usuarios
    ADD CONSTRAINT permisos_locales_usuarios_pkey PRIMARY KEY (id_permiso_local);


--
-- TOC entry 3903 (class 2606 OID 16430)
-- Name: permisos_modulos permisos_modulos_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.permisos_modulos
    ADD CONSTRAINT permisos_modulos_pkey PRIMARY KEY (id_permiso);


--
-- TOC entry 3894 (class 2606 OID 16401)
-- Name: roles roles_nombre_rol_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.roles
    ADD CONSTRAINT roles_nombre_rol_key UNIQUE (nombre_rol);


--
-- TOC entry 3896 (class 2606 OID 16399)
-- Name: roles roles_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.roles
    ADD CONSTRAINT roles_pkey PRIMARY KEY (id_rol);


--
-- TOC entry 4072 (class 2606 OID 17121)
-- Name: sesiones_historico sesiones_historico_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.sesiones_historico
    ADD CONSTRAINT sesiones_historico_pkey PRIMARY KEY (id_sesion_hist);


--
-- TOC entry 3947 (class 2606 OID 16550)
-- Name: sesiones sesiones_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.sesiones
    ADD CONSTRAINT sesiones_pkey PRIMARY KEY (id_sesion);


--
-- TOC entry 3949 (class 2606 OID 16552)
-- Name: sesiones sesiones_token_jwt_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.sesiones
    ADD CONSTRAINT sesiones_token_jwt_key UNIQUE (token_jwt);


--
-- TOC entry 4047 (class 2606 OID 17004)
-- Name: tokens_recuperacion tokens_recuperacion_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.tokens_recuperacion
    ADD CONSTRAINT tokens_recuperacion_pkey PRIMARY KEY (id_token);


--
-- TOC entry 4049 (class 2606 OID 17006)
-- Name: tokens_recuperacion tokens_recuperacion_token_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.tokens_recuperacion
    ADD CONSTRAINT tokens_recuperacion_token_key UNIQUE (token);


--
-- TOC entry 4078 (class 2606 OID 17148)
-- Name: configuracion_2fa uq_2fa_usuario; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.configuracion_2fa
    ADD CONSTRAINT uq_2fa_usuario UNIQUE (id_usuario);


--
-- TOC entry 3939 (class 2606 OID 17730)
-- Name: clientes uq_cliente_comercio_documento_nombre; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.clientes
    ADD CONSTRAINT uq_cliente_comercio_documento_nombre UNIQUE (id_comercio_registro, documento_numero, nombre, apellidos);


--
-- TOC entry 3941 (class 2606 OID 17728)
-- Name: clientes uq_cliente_documento_comercio; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.clientes
    ADD CONSTRAINT uq_cliente_documento_comercio UNIQUE (id_comercio_registro, documento_numero);


--
-- TOC entry 3905 (class 2606 OID 16432)
-- Name: permisos_modulos uq_comercio_permiso; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.permisos_modulos
    ADD CONSTRAINT uq_comercio_permiso UNIQUE (id_comercio);


--
-- TOC entry 4054 (class 2606 OID 17044)
-- Name: configuracion_seguridad uq_config_comercio; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.configuracion_seguridad
    ADD CONSTRAINT uq_config_comercio UNIQUE (id_comercio);


--
-- TOC entry 4005 (class 2606 OID 16808)
-- Name: cuentas_bancarias uq_cuenta_bancaria; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.cuentas_bancarias
    ADD CONSTRAINT uq_cuenta_bancaria UNIQUE (numero_cuenta);


--
-- TOC entry 4030 (class 2606 OID 16934)
-- Name: dispositivos_autorizados uq_dispositivo_usuario; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.dispositivos_autorizados
    ADD CONSTRAINT uq_dispositivo_usuario UNIQUE (id_usuario, uuid_dispositivo);


--
-- TOC entry 4136 (class 2606 OID 17741)
-- Name: divisas_favoritas_local uq_local_divisa; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.divisas_favoritas_local
    ADD CONSTRAINT uq_local_divisa UNIQUE (id_local, codigo_divisa);


--
-- TOC entry 4164 (class 2606 OID 17862)
-- Name: pack_alimentos_asignacion_comercios uq_pack_comercio; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.pack_alimentos_asignacion_comercios
    ADD CONSTRAINT uq_pack_comercio UNIQUE (id_pack, id_comercio);


--
-- TOC entry 4174 (class 2606 OID 17914)
-- Name: pack_alimentos_asignacion_global uq_pack_global; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.pack_alimentos_asignacion_global
    ADD CONSTRAINT uq_pack_global UNIQUE (id_pack);


--
-- TOC entry 4170 (class 2606 OID 17888)
-- Name: pack_alimentos_asignacion_locales uq_pack_local; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.pack_alimentos_asignacion_locales
    ADD CONSTRAINT uq_pack_local UNIQUE (id_pack, id_local);


--
-- TOC entry 4158 (class 2606 OID 17846)
-- Name: pack_alimentos_precios uq_pack_precio_divisa; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.pack_alimentos_precios
    ADD CONSTRAINT uq_pack_precio_divisa UNIQUE (id_pack, divisa);


--
-- TOC entry 4022 (class 2606 OID 16900)
-- Name: permisos_locales_usuarios uq_usuario_local; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.permisos_locales_usuarios
    ADD CONSTRAINT uq_usuario_local UNIQUE (id_usuario, id_local);


--
-- TOC entry 4091 (class 2606 OID 17278)
-- Name: usuario_locales_flooter usuario_locales_flooter_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.usuario_locales_flooter
    ADD CONSTRAINT usuario_locales_flooter_pkey PRIMARY KEY (id_usuario, id_local);


--
-- TOC entry 4093 (class 2606 OID 17300)
-- Name: usuario_locales usuario_locales_id_usuario_id_local_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.usuario_locales
    ADD CONSTRAINT usuario_locales_id_usuario_id_local_key UNIQUE (id_usuario, id_local);


--
-- TOC entry 4095 (class 2606 OID 17298)
-- Name: usuario_locales usuario_locales_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.usuario_locales
    ADD CONSTRAINT usuario_locales_pkey PRIMARY KEY (id);


--
-- TOC entry 3921 (class 2606 OID 16481)
-- Name: usuarios usuarios_correo_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.usuarios
    ADD CONSTRAINT usuarios_correo_key UNIQUE (correo);


--
-- TOC entry 3923 (class 2606 OID 16483)
-- Name: usuarios usuarios_numero_usuario_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.usuarios
    ADD CONSTRAINT usuarios_numero_usuario_key UNIQUE (numero_usuario);


--
-- TOC entry 3925 (class 2606 OID 16479)
-- Name: usuarios usuarios_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.usuarios
    ADD CONSTRAINT usuarios_pkey PRIMARY KEY (id_usuario);


--
-- TOC entry 4075 (class 1259 OID 17155)
-- Name: idx_2fa_habilitado; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_2fa_habilitado ON public.configuracion_2fa USING btree (habilitado);


--
-- TOC entry 4076 (class 1259 OID 17154)
-- Name: idx_2fa_usuario; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_2fa_usuario ON public.configuracion_2fa USING btree (id_usuario);


--
-- TOC entry 4085 (class 1259 OID 17259)
-- Name: idx_admin_activo; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_admin_activo ON public.administradores_allva USING btree (activo);


--
-- TOC entry 4086 (class 1259 OID 17258)
-- Name: idx_admin_correo; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_admin_correo ON public.administradores_allva USING btree (correo);


--
-- TOC entry 4102 (class 1259 OID 17340)
-- Name: idx_admin_modulos; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_admin_modulos ON public.admin_modulos_habilitados USING btree (id_administrador);


--
-- TOC entry 4087 (class 1259 OID 17257)
-- Name: idx_admin_nombre_usuario; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_admin_nombre_usuario ON public.administradores_allva USING btree (nombre_usuario);


--
-- TOC entry 3952 (class 1259 OID 16596)
-- Name: idx_audit_accion; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_audit_accion ON public.audit_log USING btree (accion);


--
-- TOC entry 3953 (class 1259 OID 16594)
-- Name: idx_audit_comercio; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_audit_comercio ON public.audit_log USING btree (id_comercio);


--
-- TOC entry 3954 (class 1259 OID 16599)
-- Name: idx_audit_exitoso; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_audit_exitoso ON public.audit_log USING btree (exitoso);


--
-- TOC entry 3955 (class 1259 OID 16598)
-- Name: idx_audit_fecha; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_audit_fecha ON public.audit_log USING btree (fecha_hora DESC);


--
-- TOC entry 3956 (class 1259 OID 16595)
-- Name: idx_audit_local; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_audit_local ON public.audit_log USING btree (id_local);


--
-- TOC entry 3957 (class 1259 OID 16597)
-- Name: idx_audit_modulo; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_audit_modulo ON public.audit_log USING btree (modulo);


--
-- TOC entry 3958 (class 1259 OID 16593)
-- Name: idx_audit_usuario; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_audit_usuario ON public.audit_log USING btree (id_usuario);


--
-- TOC entry 3993 (class 1259 OID 16787)
-- Name: idx_balance_comercio; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_balance_comercio ON public.balance_cuentas USING btree (id_comercio);


--
-- TOC entry 4127 (class 1259 OID 17704)
-- Name: idx_balance_divisas_comercio; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_balance_divisas_comercio ON public.balance_divisas USING btree (id_comercio);


--
-- TOC entry 4128 (class 1259 OID 17707)
-- Name: idx_balance_divisas_divisa; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_balance_divisas_divisa ON public.balance_divisas USING btree (codigo_divisa);


--
-- TOC entry 4129 (class 1259 OID 17706)
-- Name: idx_balance_divisas_fecha; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_balance_divisas_fecha ON public.balance_divisas USING btree (fecha_registro DESC);


--
-- TOC entry 4130 (class 1259 OID 17705)
-- Name: idx_balance_divisas_local; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_balance_divisas_local ON public.balance_divisas USING btree (id_local);


--
-- TOC entry 4131 (class 1259 OID 17708)
-- Name: idx_balance_divisas_operacion; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_balance_divisas_operacion ON public.balance_divisas USING btree (id_operacion);


--
-- TOC entry 4132 (class 1259 OID 17709)
-- Name: idx_balance_divisas_reporte; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_balance_divisas_reporte ON public.balance_divisas USING btree (id_comercio, id_local, codigo_divisa, fecha_registro);


--
-- TOC entry 3994 (class 1259 OID 16791)
-- Name: idx_balance_fecha; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_balance_fecha ON public.balance_cuentas USING btree (fecha_movimiento DESC);


--
-- TOC entry 3995 (class 1259 OID 16788)
-- Name: idx_balance_local; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_balance_local ON public.balance_cuentas USING btree (id_local);


--
-- TOC entry 3996 (class 1259 OID 16792)
-- Name: idx_balance_operacion; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_balance_operacion ON public.balance_cuentas USING btree (id_operacion);


--
-- TOC entry 3997 (class 1259 OID 16790)
-- Name: idx_balance_tipo; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_balance_tipo ON public.balance_cuentas USING btree (tipo_movimiento);


--
-- TOC entry 3998 (class 1259 OID 16789)
-- Name: idx_balance_usuario; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_balance_usuario ON public.balance_cuentas USING btree (id_usuario);


--
-- TOC entry 4183 (class 1259 OID 18039)
-- Name: idx_beneficiarios_activo; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_beneficiarios_activo ON public.clientes_beneficiarios USING btree (activo);


--
-- TOC entry 4184 (class 1259 OID 18037)
-- Name: idx_beneficiarios_cliente; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_beneficiarios_cliente ON public.clientes_beneficiarios USING btree (id_cliente);


--
-- TOC entry 4185 (class 1259 OID 18040)
-- Name: idx_beneficiarios_cliente_comercio; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_beneficiarios_cliente_comercio ON public.clientes_beneficiarios USING btree (id_cliente, id_comercio, activo);


--
-- TOC entry 4186 (class 1259 OID 18038)
-- Name: idx_beneficiarios_comercio; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_beneficiarios_comercio ON public.clientes_beneficiarios USING btree (id_comercio);


--
-- TOC entry 4187 (class 1259 OID 18048)
-- Name: idx_beneficiarios_doc_comercio; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX idx_beneficiarios_doc_comercio ON public.clientes_beneficiarios USING btree (id_comercio, tipo_documento, numero_documento) WHERE (activo = true);


--
-- TOC entry 4057 (class 1259 OID 17084)
-- Name: idx_cambios_usuarios_campo; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_cambios_usuarios_campo ON public.cambios_usuarios USING btree (campo_modificado);


--
-- TOC entry 4058 (class 1259 OID 17085)
-- Name: idx_cambios_usuarios_fecha; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_cambios_usuarios_fecha ON public.cambios_usuarios USING btree (fecha_cambio DESC);


--
-- TOC entry 4059 (class 1259 OID 17083)
-- Name: idx_cambios_usuarios_modificador; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_cambios_usuarios_modificador ON public.cambios_usuarios USING btree (modificado_por);


--
-- TOC entry 4060 (class 1259 OID 17082)
-- Name: idx_cambios_usuarios_usuario; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_cambios_usuarios_usuario ON public.cambios_usuarios USING btree (id_usuario);


--
-- TOC entry 4144 (class 1259 OID 17787)
-- Name: idx_cierres_local_fecha; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_cierres_local_fecha ON public.cierres_dia USING btree (id_local, fecha_cierre);


--
-- TOC entry 4145 (class 1259 OID 17786)
-- Name: idx_cierres_local_fecha_unico; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX idx_cierres_local_fecha_unico ON public.cierres_dia USING btree (id_local, date(fecha_cierre));


--
-- TOC entry 3928 (class 1259 OID 16537)
-- Name: idx_clientes_activo; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_clientes_activo ON public.clientes USING btree (activo);


--
-- TOC entry 3929 (class 1259 OID 17660)
-- Name: idx_clientes_apellidos; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_clientes_apellidos ON public.clientes USING btree (apellidos);


--
-- TOC entry 3930 (class 1259 OID 17661)
-- Name: idx_clientes_busqueda; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_clientes_busqueda ON public.clientes USING btree (id_comercio_registro, activo, nombre, apellidos);


--
-- TOC entry 3931 (class 1259 OID 17659)
-- Name: idx_clientes_comercio_registro; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_clientes_comercio_registro ON public.clientes USING btree (id_comercio_registro);


--
-- TOC entry 3932 (class 1259 OID 16536)
-- Name: idx_clientes_correo; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_clientes_correo ON public.clientes USING btree (correo);


--
-- TOC entry 3933 (class 1259 OID 16534)
-- Name: idx_clientes_documento; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_clientes_documento ON public.clientes USING btree (documento_tipo, documento_numero);


--
-- TOC entry 3934 (class 1259 OID 17571)
-- Name: idx_clientes_nacionalidad; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_clientes_nacionalidad ON public.clientes USING btree (nacionalidad);


--
-- TOC entry 3935 (class 1259 OID 17570)
-- Name: idx_clientes_nombre; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_clientes_nombre ON public.clientes USING btree (nombre);


--
-- TOC entry 3936 (class 1259 OID 16538)
-- Name: idx_clientes_registro; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_clientes_registro ON public.clientes USING btree (id_comercio_registro, id_local_registro);


--
-- TOC entry 3937 (class 1259 OID 16535)
-- Name: idx_clientes_telefono; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_clientes_telefono ON public.clientes USING btree (telefono);


--
-- TOC entry 3899 (class 1259 OID 16416)
-- Name: idx_comercios_activo; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_comercios_activo ON public.comercios USING btree (activo);


--
-- TOC entry 3900 (class 1259 OID 17225)
-- Name: idx_comercios_nombre; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_comercios_nombre ON public.comercios USING btree (nombre_comercio);


--
-- TOC entry 3901 (class 1259 OID 16417)
-- Name: idx_comercios_pais; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_comercios_pais ON public.comercios USING btree (pais);


--
-- TOC entry 4052 (class 1259 OID 17055)
-- Name: idx_config_seguridad_comercio; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_config_seguridad_comercio ON public.configuracion_seguridad USING btree (id_comercio);


--
-- TOC entry 4141 (class 1259 OID 17763)
-- Name: idx_correlativos_local_prefijo; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_correlativos_local_prefijo ON public.correlativos_operaciones USING btree (id_local, prefijo);


--
-- TOC entry 4001 (class 1259 OID 16819)
-- Name: idx_cuentas_bancarias_comercio; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_cuentas_bancarias_comercio ON public.cuentas_bancarias USING btree (id_comercio);


--
-- TOC entry 4002 (class 1259 OID 16820)
-- Name: idx_cuentas_bancarias_local; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_cuentas_bancarias_local ON public.cuentas_bancarias USING btree (id_local);


--
-- TOC entry 4003 (class 1259 OID 16821)
-- Name: idx_cuentas_bancarias_modulo; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_cuentas_bancarias_modulo ON public.cuentas_bancarias USING btree (modulo);


--
-- TOC entry 4196 (class 1259 OID 18109)
-- Name: idx_depositos_alimentos_comercio; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_depositos_alimentos_comercio ON public.depositos_pack_alimentos USING btree (id_comercio);


--
-- TOC entry 4197 (class 1259 OID 18108)
-- Name: idx_depositos_alimentos_fecha; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_depositos_alimentos_fecha ON public.depositos_pack_alimentos USING btree (fecha_deposito);


--
-- TOC entry 4198 (class 1259 OID 18107)
-- Name: idx_depositos_alimentos_local; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_depositos_alimentos_local ON public.depositos_pack_alimentos USING btree (id_local);


--
-- TOC entry 4025 (class 1259 OID 16948)
-- Name: idx_dispositivos_activo; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_dispositivos_activo ON public.dispositivos_autorizados USING btree (activo);


--
-- TOC entry 4026 (class 1259 OID 16947)
-- Name: idx_dispositivos_mac; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_dispositivos_mac ON public.dispositivos_autorizados USING btree (mac_address);


--
-- TOC entry 4027 (class 1259 OID 16945)
-- Name: idx_dispositivos_usuario; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_dispositivos_usuario ON public.dispositivos_autorizados USING btree (id_usuario);


--
-- TOC entry 4028 (class 1259 OID 16946)
-- Name: idx_dispositivos_uuid; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_dispositivos_uuid ON public.dispositivos_autorizados USING btree (uuid_dispositivo);


--
-- TOC entry 4124 (class 1259 OID 17572)
-- Name: idx_divisas_favoritas_local; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_divisas_favoritas_local ON public.divisas_favoritas USING btree (id_local);


--
-- TOC entry 4033 (class 1259 OID 16969)
-- Name: idx_historial_passwords_fecha; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_historial_passwords_fecha ON public.historial_passwords USING btree (fecha_cambio DESC);


--
-- TOC entry 4034 (class 1259 OID 16968)
-- Name: idx_historial_passwords_usuario; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_historial_passwords_usuario ON public.historial_passwords USING btree (id_usuario);


--
-- TOC entry 4177 (class 1259 OID 17979)
-- Name: idx_historial_pdf_fecha; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_historial_pdf_fecha ON public.historial_generacion_pdf USING btree (fecha_generacion);


--
-- TOC entry 4178 (class 1259 OID 17977)
-- Name: idx_historial_pdf_local; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_historial_pdf_local ON public.historial_generacion_pdf USING btree (id_local);


--
-- TOC entry 4179 (class 1259 OID 17980)
-- Name: idx_historial_pdf_modulo; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_historial_pdf_modulo ON public.historial_generacion_pdf USING btree (modulo);


--
-- TOC entry 4180 (class 1259 OID 17978)
-- Name: idx_historial_pdf_usuario; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_historial_pdf_usuario ON public.historial_generacion_pdf USING btree (id_usuario);


--
-- TOC entry 4006 (class 1259 OID 16862)
-- Name: idx_incidencias_comercio; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_incidencias_comercio ON public.incidencias USING btree (id_comercio);


--
-- TOC entry 4007 (class 1259 OID 16864)
-- Name: idx_incidencias_estado; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_incidencias_estado ON public.incidencias USING btree (estado);


--
-- TOC entry 4008 (class 1259 OID 16866)
-- Name: idx_incidencias_fecha; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_incidencias_fecha ON public.incidencias USING btree (fecha_creacion DESC);


--
-- TOC entry 4009 (class 1259 OID 16863)
-- Name: idx_incidencias_local; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_incidencias_local ON public.incidencias USING btree (id_local);


--
-- TOC entry 4010 (class 1259 OID 16865)
-- Name: idx_incidencias_prioridad; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_incidencias_prioridad ON public.incidencias USING btree (prioridad);


--
-- TOC entry 4011 (class 1259 OID 16861)
-- Name: idx_incidencias_usuario; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_incidencias_usuario ON public.incidencias USING btree (id_usuario_reporta);


--
-- TOC entry 4035 (class 1259 OID 16988)
-- Name: idx_intentos_login_exitoso; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_intentos_login_exitoso ON public.intentos_login USING btree (exitoso);


--
-- TOC entry 4036 (class 1259 OID 16989)
-- Name: idx_intentos_login_fecha; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_intentos_login_fecha ON public.intentos_login USING btree (fecha_intento DESC);


--
-- TOC entry 4037 (class 1259 OID 16990)
-- Name: idx_intentos_login_ip; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_intentos_login_ip ON public.intentos_login USING btree (ip_address);


--
-- TOC entry 4038 (class 1259 OID 16991)
-- Name: idx_intentos_login_sospechoso; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_intentos_login_sospechoso ON public.intentos_login USING btree (es_sospechoso);


--
-- TOC entry 4039 (class 1259 OID 16987)
-- Name: idx_intentos_login_usuario; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_intentos_login_usuario ON public.intentos_login USING btree (numero_usuario);


--
-- TOC entry 4104 (class 1259 OID 17471)
-- Name: idx_licencias_activa; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_licencias_activa ON public.licencias USING btree (activa);


--
-- TOC entry 4105 (class 1259 OID 17470)
-- Name: idx_licencias_codigo; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_licencias_codigo ON public.licencias USING btree (codigo_licencia);


--
-- TOC entry 4106 (class 1259 OID 17473)
-- Name: idx_licencias_id_maquina; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_licencias_id_maquina ON public.licencias USING btree (id_maquina);


--
-- TOC entry 4107 (class 1259 OID 17472)
-- Name: idx_licencias_usada; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_licencias_usada ON public.licencias USING btree (usada);


--
-- TOC entry 3906 (class 1259 OID 16460)
-- Name: idx_locales_activo; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_locales_activo ON public.locales USING btree (activo);


--
-- TOC entry 3907 (class 1259 OID 16461)
-- Name: idx_locales_codigo; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_locales_codigo ON public.locales USING btree (codigo_local);


--
-- TOC entry 3908 (class 1259 OID 16459)
-- Name: idx_locales_comercio; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_locales_comercio ON public.locales USING btree (id_comercio);


--
-- TOC entry 3909 (class 1259 OID 17226)
-- Name: idx_locales_id_comercio_activo; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_locales_id_comercio_activo ON public.locales USING btree (id_comercio, activo);


--
-- TOC entry 4103 (class 1259 OID 17347)
-- Name: idx_modulo_nombre; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_modulo_nombre ON public.admin_modulos_habilitados USING btree (nombre_modulo);


--
-- TOC entry 4061 (class 1259 OID 17109)
-- Name: idx_notificaciones_fecha; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_notificaciones_fecha ON public.notificaciones_seguridad USING btree (fecha_creacion DESC);


--
-- TOC entry 4062 (class 1259 OID 17108)
-- Name: idx_notificaciones_leida; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_notificaciones_leida ON public.notificaciones_seguridad USING btree (leida);


--
-- TOC entry 4063 (class 1259 OID 17110)
-- Name: idx_notificaciones_severidad; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_notificaciones_severidad ON public.notificaciones_seguridad USING btree (nivel_severidad);


--
-- TOC entry 4064 (class 1259 OID 17107)
-- Name: idx_notificaciones_tipo; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_notificaciones_tipo ON public.notificaciones_seguridad USING btree (tipo);


--
-- TOC entry 4065 (class 1259 OID 17106)
-- Name: idx_notificaciones_usuario; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_notificaciones_usuario ON public.notificaciones_seguridad USING btree (id_usuario);


--
-- TOC entry 3975 (class 1259 OID 16706)
-- Name: idx_operaciones_billetes_destino; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_operaciones_billetes_destino ON public.operaciones_billetes USING btree (aeropuerto_destino);


--
-- TOC entry 3976 (class 1259 OID 16707)
-- Name: idx_operaciones_billetes_fecha; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_operaciones_billetes_fecha ON public.operaciones_billetes USING btree (fecha_salida);


--
-- TOC entry 3977 (class 1259 OID 16704)
-- Name: idx_operaciones_billetes_operacion; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_operaciones_billetes_operacion ON public.operaciones_billetes USING btree (id_operacion);


--
-- TOC entry 3978 (class 1259 OID 16705)
-- Name: idx_operaciones_billetes_origen; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_operaciones_billetes_origen ON public.operaciones_billetes USING btree (aeropuerto_origen);


--
-- TOC entry 3959 (class 1259 OID 16659)
-- Name: idx_operaciones_cliente; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_operaciones_cliente ON public.operaciones USING btree (id_cliente);


--
-- TOC entry 3960 (class 1259 OID 16656)
-- Name: idx_operaciones_comercio; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_operaciones_comercio ON public.operaciones USING btree (id_comercio);


--
-- TOC entry 3971 (class 1259 OID 16683)
-- Name: idx_operaciones_divisas_divisa; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_operaciones_divisas_divisa ON public.operaciones_divisas USING btree (divisa_origen, divisa_destino);


--
-- TOC entry 3972 (class 1259 OID 16682)
-- Name: idx_operaciones_divisas_operacion; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_operaciones_divisas_operacion ON public.operaciones_divisas USING btree (id_operacion);


--
-- TOC entry 3961 (class 1259 OID 16661)
-- Name: idx_operaciones_estado; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_operaciones_estado ON public.operaciones USING btree (estado);


--
-- TOC entry 3962 (class 1259 OID 16662)
-- Name: idx_operaciones_fecha; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_operaciones_fecha ON public.operaciones USING btree (fecha_operacion DESC);


--
-- TOC entry 3963 (class 1259 OID 16657)
-- Name: idx_operaciones_local; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_operaciones_local ON public.operaciones USING btree (id_local);


--
-- TOC entry 3964 (class 1259 OID 16660)
-- Name: idx_operaciones_modulo; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_operaciones_modulo ON public.operaciones USING btree (modulo);


--
-- TOC entry 3965 (class 1259 OID 16663)
-- Name: idx_operaciones_numero; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_operaciones_numero ON public.operaciones USING btree (numero_operacion);


--
-- TOC entry 3981 (class 1259 OID 16726)
-- Name: idx_operaciones_pack_alimentos_destino; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_operaciones_pack_alimentos_destino ON public.operaciones_pack_alimentos USING btree (pais_destino);


--
-- TOC entry 3982 (class 1259 OID 16727)
-- Name: idx_operaciones_pack_alimentos_estado; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_operaciones_pack_alimentos_estado ON public.operaciones_pack_alimentos USING btree (estado_envio);


--
-- TOC entry 3983 (class 1259 OID 16725)
-- Name: idx_operaciones_pack_alimentos_operacion; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_operaciones_pack_alimentos_operacion ON public.operaciones_pack_alimentos USING btree (id_operacion);


--
-- TOC entry 3986 (class 1259 OID 16753)
-- Name: idx_operaciones_pack_viajes_destino; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_operaciones_pack_viajes_destino ON public.operaciones_pack_viajes USING btree (destino);


--
-- TOC entry 3987 (class 1259 OID 16754)
-- Name: idx_operaciones_pack_viajes_fecha; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_operaciones_pack_viajes_fecha ON public.operaciones_pack_viajes USING btree (fecha_inicio);


--
-- TOC entry 3988 (class 1259 OID 16752)
-- Name: idx_operaciones_pack_viajes_operacion; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_operaciones_pack_viajes_operacion ON public.operaciones_pack_viajes USING btree (id_operacion);


--
-- TOC entry 3966 (class 1259 OID 16658)
-- Name: idx_operaciones_usuario; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_operaciones_usuario ON public.operaciones USING btree (id_usuario);


--
-- TOC entry 4159 (class 1259 OID 17929)
-- Name: idx_pack_asig_comercios_comercio; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_pack_asig_comercios_comercio ON public.pack_alimentos_asignacion_comercios USING btree (id_comercio);


--
-- TOC entry 4160 (class 1259 OID 17928)
-- Name: idx_pack_asig_comercios_pack; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_pack_asig_comercios_pack ON public.pack_alimentos_asignacion_comercios USING btree (id_pack);


--
-- TOC entry 4165 (class 1259 OID 17931)
-- Name: idx_pack_asig_locales_local; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_pack_asig_locales_local ON public.pack_alimentos_asignacion_locales USING btree (id_local);


--
-- TOC entry 4166 (class 1259 OID 17930)
-- Name: idx_pack_asig_locales_pack; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_pack_asig_locales_pack ON public.pack_alimentos_asignacion_locales USING btree (id_pack);


--
-- TOC entry 4151 (class 1259 OID 17926)
-- Name: idx_pack_imagenes_pack; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_pack_imagenes_pack ON public.pack_alimentos_imagenes USING btree (id_pack);


--
-- TOC entry 4154 (class 1259 OID 17927)
-- Name: idx_pack_precios_pack; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_pack_precios_pack ON public.pack_alimentos_precios USING btree (id_pack);


--
-- TOC entry 4148 (class 1259 OID 17925)
-- Name: idx_pack_productos_pack; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_pack_productos_pack ON public.pack_alimentos_productos USING btree (id_pack);


--
-- TOC entry 4191 (class 1259 OID 18077)
-- Name: idx_paises_designados_nombre; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_paises_designados_nombre ON public.paises_designados USING btree (nombre_pais);


--
-- TOC entry 4188 (class 1259 OID 18060)
-- Name: idx_paises_destino_nombre; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_paises_destino_nombre ON public.paises_destino USING btree (nombre_pais);


--
-- TOC entry 4016 (class 1259 OID 16918)
-- Name: idx_permisos_locales_activo; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_permisos_locales_activo ON public.permisos_locales_usuarios USING btree (activo);


--
-- TOC entry 4017 (class 1259 OID 16917)
-- Name: idx_permisos_locales_local; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_permisos_locales_local ON public.permisos_locales_usuarios USING btree (id_local);


--
-- TOC entry 4018 (class 1259 OID 16916)
-- Name: idx_permisos_locales_usuario; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_permisos_locales_usuario ON public.permisos_locales_usuarios USING btree (id_usuario);


--
-- TOC entry 3942 (class 1259 OID 16565)
-- Name: idx_sesiones_activa; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_sesiones_activa ON public.sesiones USING btree (sesion_activa);


--
-- TOC entry 3943 (class 1259 OID 16566)
-- Name: idx_sesiones_expiracion; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_sesiones_expiracion ON public.sesiones USING btree (fecha_expiracion);


--
-- TOC entry 4068 (class 1259 OID 17133)
-- Name: idx_sesiones_hist_fecha; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_sesiones_hist_fecha ON public.sesiones_historico USING btree (fecha_cierre DESC);


--
-- TOC entry 4069 (class 1259 OID 17134)
-- Name: idx_sesiones_hist_motivo; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_sesiones_hist_motivo ON public.sesiones_historico USING btree (motivo_cierre);


--
-- TOC entry 4070 (class 1259 OID 17132)
-- Name: idx_sesiones_hist_usuario; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_sesiones_hist_usuario ON public.sesiones_historico USING btree (id_usuario);


--
-- TOC entry 3944 (class 1259 OID 16564)
-- Name: idx_sesiones_token; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_sesiones_token ON public.sesiones USING btree (token_jwt);


--
-- TOC entry 3945 (class 1259 OID 16563)
-- Name: idx_sesiones_usuario; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_sesiones_usuario ON public.sesiones USING btree (id_usuario);


--
-- TOC entry 4042 (class 1259 OID 17015)
-- Name: idx_tokens_recuperacion_expiracion; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_tokens_recuperacion_expiracion ON public.tokens_recuperacion USING btree (fecha_expiracion);


--
-- TOC entry 4043 (class 1259 OID 17013)
-- Name: idx_tokens_recuperacion_token; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_tokens_recuperacion_token ON public.tokens_recuperacion USING btree (token);


--
-- TOC entry 4044 (class 1259 OID 17014)
-- Name: idx_tokens_recuperacion_usado; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_tokens_recuperacion_usado ON public.tokens_recuperacion USING btree (usado);


--
-- TOC entry 4045 (class 1259 OID 17012)
-- Name: idx_tokens_recuperacion_usuario; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_tokens_recuperacion_usuario ON public.tokens_recuperacion USING btree (id_usuario);


--
-- TOC entry 3914 (class 1259 OID 16503)
-- Name: idx_usuarios_activo; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_usuarios_activo ON public.usuarios USING btree (activo);


--
-- TOC entry 3915 (class 1259 OID 16499)
-- Name: idx_usuarios_comercio; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_usuarios_comercio ON public.usuarios USING btree (id_comercio);


--
-- TOC entry 3916 (class 1259 OID 16504)
-- Name: idx_usuarios_flotante; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_usuarios_flotante ON public.usuarios USING btree (es_flooter);


--
-- TOC entry 3917 (class 1259 OID 16500)
-- Name: idx_usuarios_local; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_usuarios_local ON public.usuarios USING btree (id_local);


--
-- TOC entry 3918 (class 1259 OID 16502)
-- Name: idx_usuarios_numero; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_usuarios_numero ON public.usuarios USING btree (numero_usuario);


--
-- TOC entry 3919 (class 1259 OID 16501)
-- Name: idx_usuarios_rol; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_usuarios_rol ON public.usuarios USING btree (id_rol);


--
-- TOC entry 4290 (class 2620 OID 17161)
-- Name: sesiones trg_archivar_sesion; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER trg_archivar_sesion AFTER UPDATE ON public.sesiones FOR EACH ROW EXECUTE FUNCTION public.trigger_archivar_sesion();


--
-- TOC entry 4293 (class 2620 OID 16872)
-- Name: balance_cuentas trg_balance_creado; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER trg_balance_creado AFTER INSERT ON public.balance_cuentas FOR EACH ROW EXECUTE FUNCTION public.trigger_balance_creado();


--
-- TOC entry 4300 (class 2620 OID 18042)
-- Name: clientes_beneficiarios trg_beneficiario_fecha_mod; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER trg_beneficiario_fecha_mod BEFORE UPDATE ON public.clientes_beneficiarios FOR EACH ROW EXECUTE FUNCTION public.actualizar_fecha_modificacion_beneficiario();


--
-- TOC entry 4287 (class 2620 OID 17163)
-- Name: usuarios trg_guardar_password_historial; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER trg_guardar_password_historial AFTER UPDATE ON public.usuarios FOR EACH ROW WHEN (((old.password_hash)::text IS DISTINCT FROM (new.password_hash)::text)) EXECUTE FUNCTION public.trigger_guardar_password_historial();


--
-- TOC entry 4291 (class 2620 OID 16868)
-- Name: operaciones trg_operacion_creada; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER trg_operacion_creada AFTER INSERT ON public.operaciones FOR EACH ROW EXECUTE FUNCTION public.trigger_operacion_creada();


--
-- TOC entry 4292 (class 2620 OID 16870)
-- Name: operaciones trg_operacion_modificada; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER trg_operacion_modificada AFTER UPDATE ON public.operaciones FOR EACH ROW EXECUTE FUNCTION public.trigger_operacion_modificada();


--
-- TOC entry 4298 (class 2620 OID 17933)
-- Name: packs_alimentos trg_pack_alimentos_modificacion; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER trg_pack_alimentos_modificacion BEFORE UPDATE ON public.packs_alimentos FOR EACH ROW EXECUTE FUNCTION public.actualizar_fecha_modificacion_pack();


--
-- TOC entry 4299 (class 2620 OID 17934)
-- Name: pack_alimentos_precios trg_pack_precios_modificacion; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER trg_pack_precios_modificacion BEFORE UPDATE ON public.pack_alimentos_precios FOR EACH ROW EXECUTE FUNCTION public.actualizar_fecha_modificacion_pack();


--
-- TOC entry 4286 (class 2620 OID 16604)
-- Name: permisos_modulos trg_permisos_modificacion; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER trg_permisos_modificacion BEFORE UPDATE ON public.permisos_modulos FOR EACH ROW EXECUTE FUNCTION public.actualizar_fecha_modificacion();


--
-- TOC entry 4288 (class 2620 OID 17312)
-- Name: usuarios trg_registrar_cambio_usuario; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER trg_registrar_cambio_usuario AFTER UPDATE ON public.usuarios FOR EACH ROW EXECUTE FUNCTION public.trigger_registrar_cambio_usuario();


--
-- TOC entry 4289 (class 2620 OID 16603)
-- Name: usuarios trg_usuarios_modificacion; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER trg_usuarios_modificacion BEFORE UPDATE ON public.usuarios FOR EACH ROW EXECUTE FUNCTION public.actualizar_fecha_modificacion();


--
-- TOC entry 4294 (class 2620 OID 17261)
-- Name: administradores_allva trigger_actualizar_fecha_admin; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER trigger_actualizar_fecha_admin BEFORE UPDATE ON public.administradores_allva FOR EACH ROW EXECUTE FUNCTION public.actualizar_fecha_modificacion_admin();


--
-- TOC entry 4297 (class 2620 OID 17547)
-- Name: configuracion_sistema trigger_actualizar_fecha_config; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER trigger_actualizar_fecha_config BEFORE UPDATE ON public.configuracion_sistema FOR EACH ROW EXECUTE FUNCTION public.actualizar_fecha_modificacion_config();


--
-- TOC entry 4295 (class 2620 OID 17364)
-- Name: administradores_allva trigger_asignar_modulos; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER trigger_asignar_modulos AFTER INSERT OR UPDATE OF nivel_acceso ON public.administradores_allva FOR EACH ROW EXECUTE FUNCTION public.asignar_modulos_por_nivel();


--
-- TOC entry 4296 (class 2620 OID 17263)
-- Name: administradores_allva trigger_generar_nombre_usuario_admin; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER trigger_generar_nombre_usuario_admin BEFORE INSERT ON public.administradores_allva FOR EACH ROW EXECUTE FUNCTION public.generar_nombre_usuario_admin();


--
-- TOC entry 4256 (class 2606 OID 17335)
-- Name: admin_modulos_habilitados admin_modulos_habilitados_id_administrador_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.admin_modulos_habilitados
    ADD CONSTRAINT admin_modulos_habilitados_id_administrador_fkey FOREIGN KEY (id_administrador) REFERENCES public.administradores_allva(id_administrador) ON DELETE CASCADE;


--
-- TOC entry 4250 (class 2606 OID 17342)
-- Name: administradores_allva administradores_allva_nivel_acceso_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.administradores_allva
    ADD CONSTRAINT administradores_allva_nivel_acceso_fkey FOREIGN KEY (nivel_acceso) REFERENCES public.niveles_acceso(id_nivel);


--
-- TOC entry 4209 (class 2606 OID 16583)
-- Name: audit_log audit_log_id_comercio_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.audit_log
    ADD CONSTRAINT audit_log_id_comercio_fkey FOREIGN KEY (id_comercio) REFERENCES public.comercios(id_comercio) ON DELETE SET NULL;


--
-- TOC entry 4210 (class 2606 OID 16588)
-- Name: audit_log audit_log_id_local_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.audit_log
    ADD CONSTRAINT audit_log_id_local_fkey FOREIGN KEY (id_local) REFERENCES public.locales(id_local) ON DELETE SET NULL;


--
-- TOC entry 4211 (class 2606 OID 16578)
-- Name: audit_log audit_log_id_usuario_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.audit_log
    ADD CONSTRAINT audit_log_id_usuario_fkey FOREIGN KEY (id_usuario) REFERENCES public.usuarios(id_usuario) ON DELETE SET NULL;


--
-- TOC entry 4221 (class 2606 OID 16767)
-- Name: balance_cuentas balance_cuentas_id_comercio_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.balance_cuentas
    ADD CONSTRAINT balance_cuentas_id_comercio_fkey FOREIGN KEY (id_comercio) REFERENCES public.comercios(id_comercio);


--
-- TOC entry 4222 (class 2606 OID 16772)
-- Name: balance_cuentas balance_cuentas_id_local_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.balance_cuentas
    ADD CONSTRAINT balance_cuentas_id_local_fkey FOREIGN KEY (id_local) REFERENCES public.locales(id_local);


--
-- TOC entry 4223 (class 2606 OID 16782)
-- Name: balance_cuentas balance_cuentas_id_operacion_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.balance_cuentas
    ADD CONSTRAINT balance_cuentas_id_operacion_fkey FOREIGN KEY (id_operacion) REFERENCES public.operaciones(id_operacion);


--
-- TOC entry 4224 (class 2606 OID 16777)
-- Name: balance_cuentas balance_cuentas_id_usuario_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.balance_cuentas
    ADD CONSTRAINT balance_cuentas_id_usuario_fkey FOREIGN KEY (id_usuario) REFERENCES public.usuarios(id_usuario);


--
-- TOC entry 4258 (class 2606 OID 17684)
-- Name: balance_divisas balance_divisas_id_comercio_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.balance_divisas
    ADD CONSTRAINT balance_divisas_id_comercio_fkey FOREIGN KEY (id_comercio) REFERENCES public.comercios(id_comercio);


--
-- TOC entry 4259 (class 2606 OID 17689)
-- Name: balance_divisas balance_divisas_id_local_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.balance_divisas
    ADD CONSTRAINT balance_divisas_id_local_fkey FOREIGN KEY (id_local) REFERENCES public.locales(id_local);


--
-- TOC entry 4260 (class 2606 OID 17699)
-- Name: balance_divisas balance_divisas_id_operacion_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.balance_divisas
    ADD CONSTRAINT balance_divisas_id_operacion_fkey FOREIGN KEY (id_operacion) REFERENCES public.operaciones(id_operacion);


--
-- TOC entry 4261 (class 2606 OID 17694)
-- Name: balance_divisas balance_divisas_id_usuario_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.balance_divisas
    ADD CONSTRAINT balance_divisas_id_usuario_fkey FOREIGN KEY (id_usuario) REFERENCES public.usuarios(id_usuario);


--
-- TOC entry 4243 (class 2606 OID 17077)
-- Name: cambios_usuarios cambios_usuarios_aprobado_por_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.cambios_usuarios
    ADD CONSTRAINT cambios_usuarios_aprobado_por_fkey FOREIGN KEY (aprobado_por) REFERENCES public.usuarios(id_usuario);


--
-- TOC entry 4244 (class 2606 OID 17067)
-- Name: cambios_usuarios cambios_usuarios_id_usuario_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.cambios_usuarios
    ADD CONSTRAINT cambios_usuarios_id_usuario_fkey FOREIGN KEY (id_usuario) REFERENCES public.usuarios(id_usuario) ON DELETE CASCADE;


--
-- TOC entry 4245 (class 2606 OID 17072)
-- Name: cambios_usuarios cambios_usuarios_modificado_por_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.cambios_usuarios
    ADD CONSTRAINT cambios_usuarios_modificado_por_fkey FOREIGN KEY (modificado_por) REFERENCES public.usuarios(id_usuario) ON DELETE SET NULL;


--
-- TOC entry 4264 (class 2606 OID 17776)
-- Name: cierres_dia cierres_dia_id_comercio_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.cierres_dia
    ADD CONSTRAINT cierres_dia_id_comercio_fkey FOREIGN KEY (id_comercio) REFERENCES public.comercios(id_comercio);


--
-- TOC entry 4265 (class 2606 OID 17781)
-- Name: cierres_dia cierres_dia_id_local_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.cierres_dia
    ADD CONSTRAINT cierres_dia_id_local_fkey FOREIGN KEY (id_local) REFERENCES public.locales(id_local);


--
-- TOC entry 4204 (class 2606 OID 16519)
-- Name: clientes clientes_id_comercio_registro_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.clientes
    ADD CONSTRAINT clientes_id_comercio_registro_fkey FOREIGN KEY (id_comercio_registro) REFERENCES public.comercios(id_comercio);


--
-- TOC entry 4205 (class 2606 OID 16524)
-- Name: clientes clientes_id_local_registro_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.clientes
    ADD CONSTRAINT clientes_id_local_registro_fkey FOREIGN KEY (id_local_registro) REFERENCES public.locales(id_local);


--
-- TOC entry 4206 (class 2606 OID 16529)
-- Name: clientes clientes_id_usuario_registro_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.clientes
    ADD CONSTRAINT clientes_id_usuario_registro_fkey FOREIGN KEY (id_usuario_registro) REFERENCES public.usuarios(id_usuario);


--
-- TOC entry 4249 (class 2606 OID 17149)
-- Name: configuracion_2fa configuracion_2fa_id_usuario_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.configuracion_2fa
    ADD CONSTRAINT configuracion_2fa_id_usuario_fkey FOREIGN KEY (id_usuario) REFERENCES public.usuarios(id_usuario) ON DELETE CASCADE;


--
-- TOC entry 4241 (class 2606 OID 17045)
-- Name: configuracion_seguridad configuracion_seguridad_id_comercio_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.configuracion_seguridad
    ADD CONSTRAINT configuracion_seguridad_id_comercio_fkey FOREIGN KEY (id_comercio) REFERENCES public.comercios(id_comercio) ON DELETE CASCADE;


--
-- TOC entry 4242 (class 2606 OID 17050)
-- Name: configuracion_seguridad configuracion_seguridad_modificado_por_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.configuracion_seguridad
    ADD CONSTRAINT configuracion_seguridad_modificado_por_fkey FOREIGN KEY (modificado_por) REFERENCES public.usuarios(id_usuario);


--
-- TOC entry 4263 (class 2606 OID 17758)
-- Name: correlativos_operaciones correlativos_operaciones_id_local_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.correlativos_operaciones
    ADD CONSTRAINT correlativos_operaciones_id_local_fkey FOREIGN KEY (id_local) REFERENCES public.locales(id_local);


--
-- TOC entry 4225 (class 2606 OID 16809)
-- Name: cuentas_bancarias cuentas_bancarias_id_comercio_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.cuentas_bancarias
    ADD CONSTRAINT cuentas_bancarias_id_comercio_fkey FOREIGN KEY (id_comercio) REFERENCES public.comercios(id_comercio);


--
-- TOC entry 4226 (class 2606 OID 16814)
-- Name: cuentas_bancarias cuentas_bancarias_id_local_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.cuentas_bancarias
    ADD CONSTRAINT cuentas_bancarias_id_local_fkey FOREIGN KEY (id_local) REFERENCES public.locales(id_local);


--
-- TOC entry 4235 (class 2606 OID 16940)
-- Name: dispositivos_autorizados dispositivos_autorizados_autorizado_por_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.dispositivos_autorizados
    ADD CONSTRAINT dispositivos_autorizados_autorizado_por_fkey FOREIGN KEY (autorizado_por) REFERENCES public.usuarios(id_usuario);


--
-- TOC entry 4236 (class 2606 OID 16935)
-- Name: dispositivos_autorizados dispositivos_autorizados_id_usuario_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.dispositivos_autorizados
    ADD CONSTRAINT dispositivos_autorizados_id_usuario_fkey FOREIGN KEY (id_usuario) REFERENCES public.usuarios(id_usuario) ON DELETE CASCADE;


--
-- TOC entry 4257 (class 2606 OID 17565)
-- Name: divisas_favoritas divisas_favoritas_id_local_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.divisas_favoritas
    ADD CONSTRAINT divisas_favoritas_id_local_fkey FOREIGN KEY (id_local) REFERENCES public.locales(id_local);


--
-- TOC entry 4281 (class 2606 OID 18022)
-- Name: clientes_beneficiarios fk_beneficiario_cliente; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.clientes_beneficiarios
    ADD CONSTRAINT fk_beneficiario_cliente FOREIGN KEY (id_cliente) REFERENCES public.clientes(id_cliente) ON DELETE CASCADE;


--
-- TOC entry 4282 (class 2606 OID 18027)
-- Name: clientes_beneficiarios fk_beneficiario_comercio; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.clientes_beneficiarios
    ADD CONSTRAINT fk_beneficiario_comercio FOREIGN KEY (id_comercio) REFERENCES public.comercios(id_comercio) ON DELETE CASCADE;


--
-- TOC entry 4283 (class 2606 OID 18032)
-- Name: clientes_beneficiarios fk_beneficiario_local; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.clientes_beneficiarios
    ADD CONSTRAINT fk_beneficiario_local FOREIGN KEY (id_local_registro) REFERENCES public.locales(id_local) ON DELETE SET NULL;


--
-- TOC entry 4284 (class 2606 OID 18102)
-- Name: depositos_pack_alimentos fk_depositos_comercio; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.depositos_pack_alimentos
    ADD CONSTRAINT fk_depositos_comercio FOREIGN KEY (id_comercio) REFERENCES public.comercios(id_comercio);


--
-- TOC entry 4285 (class 2606 OID 18097)
-- Name: depositos_pack_alimentos fk_depositos_local; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.depositos_pack_alimentos
    ADD CONSTRAINT fk_depositos_local FOREIGN KEY (id_local) REFERENCES public.locales(id_local);


--
-- TOC entry 4262 (class 2606 OID 17742)
-- Name: divisas_favoritas_local fk_divisas_fav_local; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.divisas_favoritas_local
    ADD CONSTRAINT fk_divisas_fav_local FOREIGN KEY (id_local) REFERENCES public.locales(id_local) ON DELETE CASCADE;


--
-- TOC entry 4251 (class 2606 OID 17348)
-- Name: administradores_allva fk_nivel_acceso; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.administradores_allva
    ADD CONSTRAINT fk_nivel_acceso FOREIGN KEY (nivel_acceso) REFERENCES public.niveles_acceso(id_nivel);


--
-- TOC entry 4278 (class 2606 OID 17962)
-- Name: historial_generacion_pdf historial_generacion_pdf_id_comercio_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.historial_generacion_pdf
    ADD CONSTRAINT historial_generacion_pdf_id_comercio_fkey FOREIGN KEY (id_comercio) REFERENCES public.comercios(id_comercio);


--
-- TOC entry 4279 (class 2606 OID 17967)
-- Name: historial_generacion_pdf historial_generacion_pdf_id_local_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.historial_generacion_pdf
    ADD CONSTRAINT historial_generacion_pdf_id_local_fkey FOREIGN KEY (id_local) REFERENCES public.locales(id_local);


--
-- TOC entry 4280 (class 2606 OID 17972)
-- Name: historial_generacion_pdf historial_generacion_pdf_id_usuario_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.historial_generacion_pdf
    ADD CONSTRAINT historial_generacion_pdf_id_usuario_fkey FOREIGN KEY (id_usuario) REFERENCES public.usuarios(id_usuario);


--
-- TOC entry 4237 (class 2606 OID 16963)
-- Name: historial_passwords historial_passwords_cambiado_por_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.historial_passwords
    ADD CONSTRAINT historial_passwords_cambiado_por_fkey FOREIGN KEY (cambiado_por) REFERENCES public.usuarios(id_usuario);


--
-- TOC entry 4238 (class 2606 OID 16958)
-- Name: historial_passwords historial_passwords_id_usuario_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.historial_passwords
    ADD CONSTRAINT historial_passwords_id_usuario_fkey FOREIGN KEY (id_usuario) REFERENCES public.usuarios(id_usuario) ON DELETE CASCADE;


--
-- TOC entry 4227 (class 2606 OID 16841)
-- Name: incidencias incidencias_id_comercio_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.incidencias
    ADD CONSTRAINT incidencias_id_comercio_fkey FOREIGN KEY (id_comercio) REFERENCES public.comercios(id_comercio);


--
-- TOC entry 4228 (class 2606 OID 16846)
-- Name: incidencias incidencias_id_local_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.incidencias
    ADD CONSTRAINT incidencias_id_local_fkey FOREIGN KEY (id_local) REFERENCES public.locales(id_local);


--
-- TOC entry 4229 (class 2606 OID 16851)
-- Name: incidencias incidencias_id_operacion_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.incidencias
    ADD CONSTRAINT incidencias_id_operacion_fkey FOREIGN KEY (id_operacion) REFERENCES public.operaciones(id_operacion);


--
-- TOC entry 4230 (class 2606 OID 16856)
-- Name: incidencias incidencias_id_usuario_asignado_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.incidencias
    ADD CONSTRAINT incidencias_id_usuario_asignado_fkey FOREIGN KEY (id_usuario_asignado) REFERENCES public.usuarios(id_usuario);


--
-- TOC entry 4231 (class 2606 OID 16836)
-- Name: incidencias incidencias_id_usuario_reporta_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.incidencias
    ADD CONSTRAINT incidencias_id_usuario_reporta_fkey FOREIGN KEY (id_usuario_reporta) REFERENCES public.usuarios(id_usuario);


--
-- TOC entry 4239 (class 2606 OID 16982)
-- Name: intentos_login intentos_login_id_usuario_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.intentos_login
    ADD CONSTRAINT intentos_login_id_usuario_fkey FOREIGN KEY (id_usuario) REFERENCES public.usuarios(id_usuario) ON DELETE SET NULL;


--
-- TOC entry 4200 (class 2606 OID 16454)
-- Name: locales locales_id_comercio_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.locales
    ADD CONSTRAINT locales_id_comercio_fkey FOREIGN KEY (id_comercio) REFERENCES public.comercios(id_comercio) ON DELETE CASCADE;


--
-- TOC entry 4246 (class 2606 OID 17101)
-- Name: notificaciones_seguridad notificaciones_seguridad_id_usuario_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.notificaciones_seguridad
    ADD CONSTRAINT notificaciones_seguridad_id_usuario_fkey FOREIGN KEY (id_usuario) REFERENCES public.usuarios(id_usuario) ON DELETE CASCADE;


--
-- TOC entry 4217 (class 2606 OID 16699)
-- Name: operaciones_billetes operaciones_billetes_id_operacion_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.operaciones_billetes
    ADD CONSTRAINT operaciones_billetes_id_operacion_fkey FOREIGN KEY (id_operacion) REFERENCES public.operaciones(id_operacion) ON DELETE CASCADE;


--
-- TOC entry 4216 (class 2606 OID 16677)
-- Name: operaciones_divisas operaciones_divisas_id_operacion_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.operaciones_divisas
    ADD CONSTRAINT operaciones_divisas_id_operacion_fkey FOREIGN KEY (id_operacion) REFERENCES public.operaciones(id_operacion) ON DELETE CASCADE;


--
-- TOC entry 4212 (class 2606 OID 16651)
-- Name: operaciones operaciones_id_cliente_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.operaciones
    ADD CONSTRAINT operaciones_id_cliente_fkey FOREIGN KEY (id_cliente) REFERENCES public.clientes(id_cliente);


--
-- TOC entry 4213 (class 2606 OID 16636)
-- Name: operaciones operaciones_id_comercio_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.operaciones
    ADD CONSTRAINT operaciones_id_comercio_fkey FOREIGN KEY (id_comercio) REFERENCES public.comercios(id_comercio);


--
-- TOC entry 4214 (class 2606 OID 16641)
-- Name: operaciones operaciones_id_local_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.operaciones
    ADD CONSTRAINT operaciones_id_local_fkey FOREIGN KEY (id_local) REFERENCES public.locales(id_local);


--
-- TOC entry 4215 (class 2606 OID 16646)
-- Name: operaciones operaciones_id_usuario_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.operaciones
    ADD CONSTRAINT operaciones_id_usuario_fkey FOREIGN KEY (id_usuario) REFERENCES public.usuarios(id_usuario);


--
-- TOC entry 4218 (class 2606 OID 18043)
-- Name: operaciones_pack_alimentos operaciones_pack_alimentos_id_beneficiario_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.operaciones_pack_alimentos
    ADD CONSTRAINT operaciones_pack_alimentos_id_beneficiario_fkey FOREIGN KEY (id_beneficiario) REFERENCES public.clientes_beneficiarios(id_beneficiario);


--
-- TOC entry 4219 (class 2606 OID 16720)
-- Name: operaciones_pack_alimentos operaciones_pack_alimentos_id_operacion_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.operaciones_pack_alimentos
    ADD CONSTRAINT operaciones_pack_alimentos_id_operacion_fkey FOREIGN KEY (id_operacion) REFERENCES public.operaciones(id_operacion) ON DELETE CASCADE;


--
-- TOC entry 4220 (class 2606 OID 16747)
-- Name: operaciones_pack_viajes operaciones_pack_viajes_id_operacion_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.operaciones_pack_viajes
    ADD CONSTRAINT operaciones_pack_viajes_id_operacion_fkey FOREIGN KEY (id_operacion) REFERENCES public.operaciones(id_operacion) ON DELETE CASCADE;


--
-- TOC entry 4270 (class 2606 OID 17868)
-- Name: pack_alimentos_asignacion_comercios pack_alimentos_asignacion_comercios_id_comercio_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.pack_alimentos_asignacion_comercios
    ADD CONSTRAINT pack_alimentos_asignacion_comercios_id_comercio_fkey FOREIGN KEY (id_comercio) REFERENCES public.comercios(id_comercio) ON DELETE CASCADE;


--
-- TOC entry 4271 (class 2606 OID 17863)
-- Name: pack_alimentos_asignacion_comercios pack_alimentos_asignacion_comercios_id_pack_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.pack_alimentos_asignacion_comercios
    ADD CONSTRAINT pack_alimentos_asignacion_comercios_id_pack_fkey FOREIGN KEY (id_pack) REFERENCES public.packs_alimentos(id_pack) ON DELETE CASCADE;


--
-- TOC entry 4272 (class 2606 OID 17873)
-- Name: pack_alimentos_asignacion_comercios pack_alimentos_asignacion_comercios_id_precio_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.pack_alimentos_asignacion_comercios
    ADD CONSTRAINT pack_alimentos_asignacion_comercios_id_precio_fkey FOREIGN KEY (id_precio) REFERENCES public.pack_alimentos_precios(id_precio) ON DELETE CASCADE;


--
-- TOC entry 4276 (class 2606 OID 17915)
-- Name: pack_alimentos_asignacion_global pack_alimentos_asignacion_global_id_pack_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.pack_alimentos_asignacion_global
    ADD CONSTRAINT pack_alimentos_asignacion_global_id_pack_fkey FOREIGN KEY (id_pack) REFERENCES public.packs_alimentos(id_pack) ON DELETE CASCADE;


--
-- TOC entry 4277 (class 2606 OID 17920)
-- Name: pack_alimentos_asignacion_global pack_alimentos_asignacion_global_id_precio_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.pack_alimentos_asignacion_global
    ADD CONSTRAINT pack_alimentos_asignacion_global_id_precio_fkey FOREIGN KEY (id_precio) REFERENCES public.pack_alimentos_precios(id_precio) ON DELETE CASCADE;


--
-- TOC entry 4273 (class 2606 OID 17894)
-- Name: pack_alimentos_asignacion_locales pack_alimentos_asignacion_locales_id_local_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.pack_alimentos_asignacion_locales
    ADD CONSTRAINT pack_alimentos_asignacion_locales_id_local_fkey FOREIGN KEY (id_local) REFERENCES public.locales(id_local) ON DELETE CASCADE;


--
-- TOC entry 4274 (class 2606 OID 17889)
-- Name: pack_alimentos_asignacion_locales pack_alimentos_asignacion_locales_id_pack_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.pack_alimentos_asignacion_locales
    ADD CONSTRAINT pack_alimentos_asignacion_locales_id_pack_fkey FOREIGN KEY (id_pack) REFERENCES public.packs_alimentos(id_pack) ON DELETE CASCADE;


--
-- TOC entry 4275 (class 2606 OID 17899)
-- Name: pack_alimentos_asignacion_locales pack_alimentos_asignacion_locales_id_precio_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.pack_alimentos_asignacion_locales
    ADD CONSTRAINT pack_alimentos_asignacion_locales_id_precio_fkey FOREIGN KEY (id_precio) REFERENCES public.pack_alimentos_precios(id_precio) ON DELETE CASCADE;


--
-- TOC entry 4268 (class 2606 OID 17829)
-- Name: pack_alimentos_imagenes pack_alimentos_imagenes_id_pack_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.pack_alimentos_imagenes
    ADD CONSTRAINT pack_alimentos_imagenes_id_pack_fkey FOREIGN KEY (id_pack) REFERENCES public.packs_alimentos(id_pack) ON DELETE CASCADE;


--
-- TOC entry 4269 (class 2606 OID 17847)
-- Name: pack_alimentos_precios pack_alimentos_precios_id_pack_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.pack_alimentos_precios
    ADD CONSTRAINT pack_alimentos_precios_id_pack_fkey FOREIGN KEY (id_pack) REFERENCES public.packs_alimentos(id_pack) ON DELETE CASCADE;


--
-- TOC entry 4267 (class 2606 OID 17813)
-- Name: pack_alimentos_productos pack_alimentos_productos_id_pack_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.pack_alimentos_productos
    ADD CONSTRAINT pack_alimentos_productos_id_pack_fkey FOREIGN KEY (id_pack) REFERENCES public.packs_alimentos(id_pack) ON DELETE CASCADE;


--
-- TOC entry 4266 (class 2606 OID 18061)
-- Name: packs_alimentos packs_alimentos_id_pais_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.packs_alimentos
    ADD CONSTRAINT packs_alimentos_id_pais_fkey FOREIGN KEY (id_pais) REFERENCES public.paises_destino(id_pais);


--
-- TOC entry 4232 (class 2606 OID 16911)
-- Name: permisos_locales_usuarios permisos_locales_usuarios_asignado_por_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.permisos_locales_usuarios
    ADD CONSTRAINT permisos_locales_usuarios_asignado_por_fkey FOREIGN KEY (asignado_por) REFERENCES public.usuarios(id_usuario);


--
-- TOC entry 4233 (class 2606 OID 16906)
-- Name: permisos_locales_usuarios permisos_locales_usuarios_id_local_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.permisos_locales_usuarios
    ADD CONSTRAINT permisos_locales_usuarios_id_local_fkey FOREIGN KEY (id_local) REFERENCES public.locales(id_local) ON DELETE CASCADE;


--
-- TOC entry 4234 (class 2606 OID 16901)
-- Name: permisos_locales_usuarios permisos_locales_usuarios_id_usuario_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.permisos_locales_usuarios
    ADD CONSTRAINT permisos_locales_usuarios_id_usuario_fkey FOREIGN KEY (id_usuario) REFERENCES public.usuarios(id_usuario) ON DELETE CASCADE;


--
-- TOC entry 4199 (class 2606 OID 16433)
-- Name: permisos_modulos permisos_modulos_id_comercio_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.permisos_modulos
    ADD CONSTRAINT permisos_modulos_id_comercio_fkey FOREIGN KEY (id_comercio) REFERENCES public.comercios(id_comercio) ON DELETE CASCADE;


--
-- TOC entry 4247 (class 2606 OID 17127)
-- Name: sesiones_historico sesiones_historico_id_local_activo_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.sesiones_historico
    ADD CONSTRAINT sesiones_historico_id_local_activo_fkey FOREIGN KEY (id_local_activo) REFERENCES public.locales(id_local) ON DELETE SET NULL;


--
-- TOC entry 4248 (class 2606 OID 17122)
-- Name: sesiones_historico sesiones_historico_id_usuario_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.sesiones_historico
    ADD CONSTRAINT sesiones_historico_id_usuario_fkey FOREIGN KEY (id_usuario) REFERENCES public.usuarios(id_usuario) ON DELETE SET NULL;


--
-- TOC entry 4207 (class 2606 OID 16558)
-- Name: sesiones sesiones_id_local_activo_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.sesiones
    ADD CONSTRAINT sesiones_id_local_activo_fkey FOREIGN KEY (id_local_activo) REFERENCES public.locales(id_local) ON DELETE SET NULL;


--
-- TOC entry 4208 (class 2606 OID 16553)
-- Name: sesiones sesiones_id_usuario_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.sesiones
    ADD CONSTRAINT sesiones_id_usuario_fkey FOREIGN KEY (id_usuario) REFERENCES public.usuarios(id_usuario) ON DELETE CASCADE;


--
-- TOC entry 4240 (class 2606 OID 17007)
-- Name: tokens_recuperacion tokens_recuperacion_id_usuario_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.tokens_recuperacion
    ADD CONSTRAINT tokens_recuperacion_id_usuario_fkey FOREIGN KEY (id_usuario) REFERENCES public.usuarios(id_usuario) ON DELETE CASCADE;


--
-- TOC entry 4252 (class 2606 OID 17284)
-- Name: usuario_locales_flooter usuario_locales_flooter_id_local_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.usuario_locales_flooter
    ADD CONSTRAINT usuario_locales_flooter_id_local_fkey FOREIGN KEY (id_local) REFERENCES public.locales(id_local) ON DELETE CASCADE;


--
-- TOC entry 4253 (class 2606 OID 17279)
-- Name: usuario_locales_flooter usuario_locales_flooter_id_usuario_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.usuario_locales_flooter
    ADD CONSTRAINT usuario_locales_flooter_id_usuario_fkey FOREIGN KEY (id_usuario) REFERENCES public.usuarios(id_usuario) ON DELETE CASCADE;


--
-- TOC entry 4254 (class 2606 OID 17306)
-- Name: usuario_locales usuario_locales_id_local_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.usuario_locales
    ADD CONSTRAINT usuario_locales_id_local_fkey FOREIGN KEY (id_local) REFERENCES public.locales(id_local) ON DELETE CASCADE;


--
-- TOC entry 4255 (class 2606 OID 17301)
-- Name: usuario_locales usuario_locales_id_usuario_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.usuario_locales
    ADD CONSTRAINT usuario_locales_id_usuario_fkey FOREIGN KEY (id_usuario) REFERENCES public.usuarios(id_usuario) ON DELETE CASCADE;


--
-- TOC entry 4201 (class 2606 OID 16484)
-- Name: usuarios usuarios_id_comercio_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.usuarios
    ADD CONSTRAINT usuarios_id_comercio_fkey FOREIGN KEY (id_comercio) REFERENCES public.comercios(id_comercio) ON DELETE CASCADE;


--
-- TOC entry 4202 (class 2606 OID 16489)
-- Name: usuarios usuarios_id_local_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.usuarios
    ADD CONSTRAINT usuarios_id_local_fkey FOREIGN KEY (id_local) REFERENCES public.locales(id_local) ON DELETE SET NULL;


--
-- TOC entry 4203 (class 2606 OID 16494)
-- Name: usuarios usuarios_id_rol_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.usuarios
    ADD CONSTRAINT usuarios_id_rol_fkey FOREIGN KEY (id_rol) REFERENCES public.roles(id_rol);


-- Completed on 2025-12-14 06:53:47

--
-- PostgreSQL database dump complete
--

