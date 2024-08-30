--
-- PostgreSQL database dump
--

-- Dumped from database version 16.4
-- Dumped by pg_dump version 16.4

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

ALTER TABLE ONLY public.peering DROP CONSTRAINT peering_pk;
ALTER TABLE ONLY public.peering_ips DROP CONSTRAINT peering_ips_pk;
ALTER TABLE ONLY public.peering_ips_extra DROP CONSTRAINT peering_ips_extra_unique;
ALTER TABLE ONLY public.peering_acl DROP CONSTRAINT peering_acl_pk;
ALTER TABLE public.peering_ips_extra ALTER COLUMN peering_ips_extra_id DROP DEFAULT;
ALTER TABLE public.peering_ips ALTER COLUMN peering_ips_id DROP DEFAULT;
ALTER TABLE public.peering_acl ALTER COLUMN peering_acl_id DROP DEFAULT;
ALTER TABLE public.peering ALTER COLUMN peering_id DROP DEFAULT;
DROP SEQUENCE public.public_peering_peering_id_seq;
DROP SEQUENCE public.public_peering_ips_peering_ips_id_seq;
DROP SEQUENCE public.public_peering_ips_extra_peering_ips_extra_id_seq;
DROP SEQUENCE public.public_peering_acl_peering_acl_id_seq;
DROP TABLE public.peering_ips_extra;
DROP TABLE public.peering_ips;
DROP TABLE public.peering_acl;
DROP TABLE public.peering;
SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: peering; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.peering (
    peering_id integer NOT NULL,
    peering_peeringdb_id integer,
    peering_name character varying(255),
    peering_as_set character varying(255),
    peering_asn character varying(255),
    peering_active boolean,
    peering_deployed boolean,
    peering_created timestamp without time zone,
    peering_modified timestamp without time zone,
    peering_deleted boolean
);


--
-- Name: peering_acl; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.peering_acl (
    peering_acl_id integer NOT NULL,
    peering_acl_peeringdb_id integer,
    peering_acl_asn character varying(255),
    peering_acl_afi integer,
    peering_acl_prefix character varying(255),
    peering_acl_created timestamp without time zone,
    peering_acl_modified timestamp without time zone,
    peering_acl_deleted boolean
);


--
-- Name: peering_ips; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.peering_ips (
    peering_ips_id integer NOT NULL,
    peering_ips_peering_id integer NOT NULL,
    peering_ips_peeringdb_lanid integer NOT NULL,
    peering_ips_peeringdb_addrid integer NOT NULL,
    peering_ips_type integer NOT NULL,
    peering_ips_addr character varying(255),
    peering_ips_active boolean,
    peering_ips_deployed boolean,
    peering_ips_rejected boolean,
    peering_ips_notified boolean,
    peering_ips_notified_approval boolean,
    peering_ips_notified_skip boolean,
    peering_ips_notified_email character varying(255),
    peering_ips_created timestamp without time zone,
    peering_ips_modified timestamp without time zone,
    peering_ips_deleted boolean
);


--
-- Name: peering_ips_extra; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.peering_ips_extra (
    peering_ips_extra_id integer NOT NULL,
    peering_ips_extra_peering_id integer NOT NULL,
    peering_ips_extra_addr character varying(255),
    peering_ips_extra_active boolean,
    peering_ips_extra_deployed boolean,
    peering_ips_extra_created timestamp without time zone,
    peering_ips_extra_modified timestamp without time zone,
    peering_ips_extra_deleted boolean
);


--
-- Name: public_peering_acl_peering_acl_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.public_peering_acl_peering_acl_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: public_peering_acl_peering_acl_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.public_peering_acl_peering_acl_id_seq OWNED BY public.peering_acl.peering_acl_id;


--
-- Name: public_peering_ips_extra_peering_ips_extra_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.public_peering_ips_extra_peering_ips_extra_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: public_peering_ips_extra_peering_ips_extra_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.public_peering_ips_extra_peering_ips_extra_id_seq OWNED BY public.peering_ips_extra.peering_ips_extra_id;


--
-- Name: public_peering_ips_peering_ips_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.public_peering_ips_peering_ips_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: public_peering_ips_peering_ips_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.public_peering_ips_peering_ips_id_seq OWNED BY public.peering_ips.peering_ips_id;


--
-- Name: public_peering_peering_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.public_peering_peering_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: public_peering_peering_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.public_peering_peering_id_seq OWNED BY public.peering.peering_id;


--
-- Name: peering peering_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.peering ALTER COLUMN peering_id SET DEFAULT nextval('public.public_peering_peering_id_seq'::regclass);


--
-- Name: peering_acl peering_acl_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.peering_acl ALTER COLUMN peering_acl_id SET DEFAULT nextval('public.public_peering_acl_peering_acl_id_seq'::regclass);


--
-- Name: peering_ips peering_ips_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.peering_ips ALTER COLUMN peering_ips_id SET DEFAULT nextval('public.public_peering_ips_peering_ips_id_seq'::regclass);


--
-- Name: peering_ips_extra peering_ips_extra_id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.peering_ips_extra ALTER COLUMN peering_ips_extra_id SET DEFAULT nextval('public.public_peering_ips_extra_peering_ips_extra_id_seq'::regclass);


--
-- Name: peering_acl peering_acl_pk; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.peering_acl
    ADD CONSTRAINT peering_acl_pk PRIMARY KEY (peering_acl_id);


--
-- Name: peering_ips_extra peering_ips_extra_unique; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.peering_ips_extra
    ADD CONSTRAINT peering_ips_extra_unique UNIQUE (peering_ips_extra_id);


--
-- Name: peering_ips peering_ips_pk; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.peering_ips
    ADD CONSTRAINT peering_ips_pk PRIMARY KEY (peering_ips_id);


--
-- Name: peering peering_pk; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.peering
    ADD CONSTRAINT peering_pk PRIMARY KEY (peering_id);


--
-- PostgreSQL database dump complete
--

