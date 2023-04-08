-- MariaDB dump 10.19  Distrib 10.11.2-MariaDB, for debian-linux-gnu (x86_64)
--
-- Host: localhost    Database: luje
-- ------------------------------------------------------
-- Server version	10.11.2-MariaDB-1:10.11.2+maria~ubu2204

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `peering`
--

DROP TABLE IF EXISTS `peering`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `peering` (
  `peering_id` int(11) NOT NULL AUTO_INCREMENT,
  `peering_peeringdb_id` int(11) DEFAULT NULL,
  `peering_name` varchar(255) DEFAULT NULL,
  `peering_as_set` varchar(255) DEFAULT NULL,
  `peering_asn` varchar(255) DEFAULT NULL,
  `peering_active` tinyint(1) DEFAULT NULL,
  `peering_deployed` tinyint(1) DEFAULT NULL,
  `peering_created` datetime DEFAULT NULL,
  `peering_modified` datetime DEFAULT NULL,
  `peering_deleted` tinyint(1) DEFAULT NULL,
  PRIMARY KEY (`peering_id`),
  KEY `peering_peeringdb_id_idx` (`peering_peeringdb_id`)
) ENGINE=InnoDB AUTO_INCREMENT=57 DEFAULT CHARSET=utf8mb3 COLLATE=utf8mb3_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `peering_ips`
--

DROP TABLE IF EXISTS `peering_ips`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `peering_ips` (
  `peering_ips_id` int(11) NOT NULL AUTO_INCREMENT,
  `peering_ips_peering_id` int(11) NOT NULL,
  `peering_ips_peeringdb_lanid` int(11) NOT NULL,
  `peering_ips_peeringdb_addrid` int(11) NOT NULL,
  `peering_ips_type` tinyint(4) NOT NULL,
  `peering_ips_addr` varchar(255) DEFAULT NULL,
  `peering_ips_active` tinyint(1) DEFAULT NULL,
  `peering_ips_deployed` tinyint(1) DEFAULT NULL,
  `peering_ips_rejected` tinyint(1) DEFAULT NULL,
  `peering_ips_notified` tinyint(1) DEFAULT NULL,
  `peering_ips_notified_approval` tinyint(1) DEFAULT NULL,
  `peering_ips_notified_skip` tinyint(1) DEFAULT NULL,
  `peering_ips_notified_email` varchar(255) DEFAULT NULL,
  `peering_ips_created` datetime DEFAULT NULL,
  `peering_ips_modified` datetime DEFAULT NULL,
  `peering_ips_deleted` tinyint(1) DEFAULT NULL,
  PRIMARY KEY (`peering_ips_id`),
  KEY `fk_peering_ips_peering_id_idx` (`peering_ips_peering_id`),
  KEY `peering_ips_peeringdb_lanid_idx` (`peering_ips_peeringdb_lanid`),
  KEY `peering_ips_peeringdb_addrid_idx` (`peering_ips_peeringdb_addrid`,`peering_ips_type`),
  CONSTRAINT `fk_peering_ips_peering_id` FOREIGN KEY (`peering_ips_peering_id`) REFERENCES `peering` (`peering_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB AUTO_INCREMENT=137 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `peering_ips_extra`
--

DROP TABLE IF EXISTS `peering_ips_extra`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `peering_ips_extra` (
  `peering_ips_extra_id` int(11) NOT NULL AUTO_INCREMENT,
  `peering_ips_extra_peering_id` int(11) NOT NULL,
  `peering_ips_extra_addr` varchar(255) DEFAULT NULL,
  `peering_ips_extra_active` tinyint(1) DEFAULT NULL,
  `peering_ips_extra_deployed` tinyint(1) DEFAULT NULL,
  `peering_ips_extra_created` datetime DEFAULT NULL,
  `peering_ips_extra_modified` datetime DEFAULT NULL,
  `peering_ips_extra_deleted` tinyint(1) DEFAULT NULL,
  PRIMARY KEY (`peering_ips_extra_id`),
  KEY `fk_peering_ips_extra_peering_id_idx` (`peering_ips_extra_peering_id`),
  CONSTRAINT `fk_peering_ips_extra_peering_id` FOREIGN KEY (`peering_ips_extra_peering_id`) REFERENCES `peering` (`peering_id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB AUTO_INCREMENT=13 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2023-04-08 14:29:28
