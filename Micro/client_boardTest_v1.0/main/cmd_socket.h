#include "esp_log.h"
#include <stdio.h>
#include <stdlib.h>

#include <stdio.h>
#include <string.h>
#include <inttypes.h>
#include <unistd.h>
#include <sys/socket.h>
#include <netinet/in.h>
#include <arpa/inet.h> // inet_aton()
#include <netdb.h>
//for the input-port cast
#ifndef SCNu8
  #define SCNu8 "hhu"
#endif

//credential [wifi&socket] definition (by menuconfig)
#define DEFAULT_SSID CONFIG_WIFI_SSID
#define DEFAULT_PWD CONFIG_WIFI_PASSWORD
/* it must be the IP of the pc(server) to find it out go to ifconfig->Wifi0->inetaddr:(ipv4) */
#define DEFAULT_SERVER_IP CONFIG_SERVER_IP
//#define DEFAULT_SERVER_IP "192.168.1.6"
#define DEFAULT_SERVER_PORT CONFIG_SERVER_PORT
//#define DEFAULT_SERVER_PORT "1500"
#define DEFAULT_CHANNEL CONFIG_WIFI_SELECTED_CHANNEL
//#define DEFAULT_CHANNEL "11"

static const char *TAGS = "Socket";

//Socket Group
#define BUFLEN	128  	/* BUFFER LENGTH */
char    buf[BUFLEN]; 	/* transmission buffer */
char    rbuf[BUFLEN];   /* transmission buffer */

//METHODS
int start_socket_connection();
int client_sync(int socket);
int send_sniffed_packages(int socket, char *path);
