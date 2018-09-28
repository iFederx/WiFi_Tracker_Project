/* ESP HANDLE LIB */
#include <stdlib.h>
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "freertos/event_groups.h"
#include "esp_system.h"
#include "esp_wifi.h"
#include "esp_event_loop.h"
#include "esp_log.h"
#include "nvs_flash.h"
/* LED HANDLE LIB */
#include "driver/gpio.h"
#include "sdkconfig.h"
/* MY FILES INCLUSION */
#include "cmd_file.h"
#include "cmd_socket.h"
// VFS and SPIFFS includes
#include "esp_vfs.h"
#include "../components/spiffs/spiffs_vfs.h"

const char *TAGM = "Main";
//----------------------Connection group
static wifi_country_t wifi_country = {.cc="CN", .schan=1, .nchan=13, .policy=WIFI_COUNTRY_POLICY_AUTO};	// wifi policies (by country)
static EventGroupHandle_t wifi_event_group;		// Event group
const int CONNECTED_BIT = BIT0;					// CONNECTION pin for wifiGroup definition
//----------------------Sniffer group
FILE *fSniffs;	 												// sniffed_pkg.txt init (FILE CONTAINING THE SNIFFED PACKAGE)
const int TIMEOUT = 55000 / portTICK_RATE_MS;			    // sniffing timeout time
#define	CHANNEL_MAX		(13)
#define WIFI_PROMIS_FILTER_MASK_ALL         (0xFFFFFFFF)    /**< filter all packets */
#define WIFI_PROMIS_FILTER_MASK_MGMT        (1)             /**< filter the packets with type of WIFI_PKT_MGMT */
#define WIFI_EVENT_MASK_AP_PROBEREQRECVED   (BIT(0))        /**< mask SYSTEM_EVENT_AP_PROBEREQRECVED event */
#define PROBE_MASK 64										// my own defined PROBE_MASK
#define BEACON_MASK 128										// my own defined BEACON_MASK
//-----------------------Blink led
int BLINK_TIME_ON = 0;								// LED Blink time init
int BLINK_TIME_OFF = 0;								// LED Blink time init
#define BLINK_GPIO 2							// LED pin definition
TaskHandle_t xHandle = NULL;					//define handle for blink task
//-----------------------GLOBAL VARIABLE & STRUCTURES
typedef struct {
	unsigned frame_ctrl:16;
	unsigned duration_id:16;
	uint8_t addr1[6]; 							/* receiver address */
	uint8_t addr2[6]; 							/* sender address */
	uint8_t addr3[6]; 							/* filtering address */
	unsigned sequence_ctrl:16;
	uint8_t addr4[6]; /* optional */
} wifi_ieee80211_mac_hdr_t;

typedef struct {
	wifi_ieee80211_mac_hdr_t hdr;
	uint8_t payload[0]; 						/* network data ended with 4 bytes csum (CRC32) */
} wifi_ieee80211_packet_t;


//METHODS
static esp_err_t event_handler(void *ctx, system_event_t *event);
static void wifi_deinit();
static void wifi_init();
void sniffer_init(uint8_t chnl);
static void sniffer_set_channel(uint8_t channel);
static void sniffer_packet_handler(void *buff, wifi_promiscuous_pkt_type_t type);
static void sniffer_stop(void);
void blink_task(void *pvParameter);

// Wifi event handler
static esp_err_t event_handler(void *ctx, system_event_t *event)
{
    switch(event->event_id) {

    case SYSTEM_EVENT_STA_START:
        esp_wifi_connect();
        break;

    //if here then it successfull connected(return SYSTEM_EVENT_STA_GOT_IP), so print the Network info
	case SYSTEM_EVENT_STA_GOT_IP:
		ESP_LOGI(TAGM,"(*)Successfully Connected to %s!\n",DEFAULT_SSID);
		tcpip_adapter_ip_info_t ip_info;
		ESP_ERROR_CHECK(tcpip_adapter_get_ip_info(TCPIP_ADAPTER_IF_STA, &ip_info));
		ESP_LOGI(TAGM,"================================ INFO ===========================\n\r	");
		ESP_LOGI(TAGM,"\r   IP Address:  %s\n", ip4addr_ntoa(&ip_info.ip));
		ESP_LOGI(TAGM,"\r   Subnet mask: %s\n", ip4addr_ntoa(&ip_info.netmask));
		ESP_LOGI(TAGM,"\r   Gateway:     %s\n", ip4addr_ntoa(&ip_info.gw));
		ESP_LOGI(TAGM,"=================================================================\n\r	");
        xEventGroupSetBits(wifi_event_group, CONNECTED_BIT);
        break;

    //in case of disconnection(returned SYSTEM_EVENT_STA_DISCONNECTED) then  clean the Event group
	case SYSTEM_EVENT_STA_DISCONNECTED:
		ESP_LOGW(TAGM,"Warning: ESP is disconnected from the network!\n");
		xEventGroupClearBits(wifi_event_group, CONNECTED_BIT);
        break;
    
	default:
        break;
    }
   
	return ESP_OK;
}


// Main application
void app_main()
{
	/*	MAIN VARIABLE SPACE	*/
	int s=0;	/* socket ID */
	uint8_t channel = 0;

	esp_log_level_set("wifi", ESP_LOG_NONE);      // enable WARN logs from WiFi stack
	esp_log_level_set("dhcpc", ESP_LOG_INFO);     // enable INFO logs from DHCP client

	//Initialize NVS
	esp_err_t ret = nvs_flash_init();
	if (ret == ESP_ERR_NVS_NO_FREE_PAGES || ret == ESP_ERR_NVS_NEW_VERSION_FOUND) {
	  ESP_ERROR_CHECK(nvs_flash_erase());
	  ret = nvs_flash_init();
	}
	ESP_ERROR_CHECK(ret);

	//++++++++++++++++++++++ SPIFFS ++++++++++++++++++++++++++
	printf("\r\n\n");
	ESP_LOGI(TAGM, "(*) Starting spiffs memory function +++\n");

	vfs_spiffs_register();

	// the partition was mounted?
	if(spiffs_is_mounted) {
		ESP_LOGI(TAGM,"Partition correctly mounted!\r\n");
	}
	else {
		ESP_LOGE(TAGM,"Error: Issues while mounting the SPIFFS partition.\n\r"
				"REBOOTING\n");
		esp_restart();
	}
	//+++++++++++++++++++++++++++++++++++++++++++++++++++++++

	// create the event group to handle wifi events
	wifi_event_group = xEventGroupCreate();

	// initialize the tcp stack
	tcpip_adapter_init();

	// initialize the wifi event handler
	ESP_ERROR_CHECK(esp_event_loop_init(event_handler, NULL));

	//start blinking task
	xTaskCreate(&blink_task, "blink_task", configMINIMAL_STACK_SIZE, NULL, 5, &xHandle);

	// here we connect with the network and continue, if it doesnt work after portMAX_DELAY time then it abort!
	wifi_init();
	xEventGroupWaitBits(wifi_event_group, CONNECTED_BIT, false, true, portMAX_DELAY);

	ESP_LOGI(TAGM,"\r-> Opening the socket connection with %s:%s \n",DEFAULT_SERVER_IP,DEFAULT_SERVER_PORT);
	s=start_socket_connection();
	while(1)
	{
		//sync of timestamp (after the first time the socket is just open for sending the packages!)
		if (s>=0)
		{
			ESP_LOGI(TAGM,"(*)Socket created Successfully!\n");
			client_sync(s);
		}else
		{
			ESP_LOGE(TAGM,"(!)Error: Impossible to sync Timestamps. \n "
						"\nREBOOTING\n\r");
			esp_restart();
		}
		vTaskDelay(2000 / portTICK_RATE_MS);
		close(s);

		/* disconnecting and deinit the previous configuration of wifi for sniffing packages */
		wifi_deinit();

		//Sniff the selected channel for TIMEOUT seconds
		sscanf(DEFAULT_CHANNEL, "%" SCNu8, &channel);
		fSniffs = fopen("/spiffs/sniffed_pkg.txt", "w");
		if (fSniffs == NULL)
		{
			ESP_LOGE(TAGM,"Error opening file! I can't do anything... "
					"\n\rREBOOTING\n");
			esp_restart();
		}
		ESP_LOGI(TAGM, "File sniffed_pkg.txt created Successfully! \n");
		sniffer_init(channel);
		ESP_LOGI(TAGM,"\r=================================================================\n\r	");
		ESP_LOGI(TAGM,"\r(!!) Start sniffing in: %s\n"
						"\rCHANNEL: %d \n"
						"\rDURATION: %d \n"
						"vTYPE PKG: Probe/Beacons \n",
						DEFAULT_SSID, channel, (TIMEOUT/1000));
		vTaskDelay(TIMEOUT);
		ESP_LOGI(TAGM,"\r(!!) End of channel listening..\n");
		ESP_LOGI(TAGM,"\r=================================================================\n\r	");
		sniffer_stop();
		fclose(fSniffs);

		/* disconnecting and deinit the sniffing configuration for reconnecting the normal wifi */
		wifi_deinit();

		// here we connect with the network and continue, if it doesnt work after portMAX_DELAY time then it abort!
		wifi_init();
		xEventGroupWaitBits(wifi_event_group, CONNECTED_BIT, false, true, portMAX_DELAY);
		//sending sniffed packages
		ESP_LOGI(TAGM,"\r-> Opening the socket connection with %s:%s \n",DEFAULT_SERVER_IP,DEFAULT_SERVER_PORT);
		s=start_socket_connection();
		if (s>=0)
		{
			ESP_LOGI(TAGM,"(*)Socket created Successfully!\n");
			send_sniffed_packages(s,"/spiffs/sniffed_pkg.txt");
		}else
		{
			ESP_LOGE(TAGM,"(!)Error: Connection errors. Impossible to send packages. \n\n");
		}
		vTaskDelay(2000 / portTICK_RATE_MS);
	}
}

void wifi_init()
{
	BLINK_TIME_ON = 1000 / portTICK_RATE_MS;
	BLINK_TIME_OFF = 1000 / portTICK_RATE_MS;
	// initialize the wifi stack in STAtion mode with config in RAM
	xEventGroupClearBits(wifi_event_group, CONNECTED_BIT);
	wifi_init_config_t wifi_init_config = WIFI_INIT_CONFIG_DEFAULT();
	ESP_ERROR_CHECK(esp_wifi_init(&wifi_init_config));
	ESP_ERROR_CHECK(esp_wifi_set_storage(WIFI_STORAGE_RAM));
	ESP_ERROR_CHECK(esp_wifi_set_mode(WIFI_MODE_STA));

	// configure the wifi connection and start the interface
	wifi_config_t wifi_config = {
	        .sta = {
	            .ssid = DEFAULT_SSID,
	            .password = DEFAULT_PWD,
	        },
	};
	ESP_ERROR_CHECK(esp_wifi_set_config(ESP_IF_WIFI_STA, &wifi_config));
	ESP_ERROR_CHECK(esp_wifi_start());
	ESP_LOGI(TAGM,"-> Connecting to %s ... \n", DEFAULT_SSID);
}

void wifi_deinit()
{
	BLINK_TIME_ON = 100 / portTICK_RATE_MS;
	BLINK_TIME_OFF = 100 / portTICK_RATE_MS;
	ESP_ERROR_CHECK( esp_wifi_stop() );
	ESP_ERROR_CHECK( esp_wifi_deinit() );
}


void sniffer_init(uint8_t chnl)
{
	wifi_init_config_t cfg = WIFI_INIT_CONFIG_DEFAULT();
	ESP_ERROR_CHECK( esp_wifi_init(&cfg) );
	ESP_ERROR_CHECK( esp_wifi_set_country(&wifi_country) ); /* set country for channel range [1, 13] */
	ESP_ERROR_CHECK( esp_wifi_set_storage(WIFI_STORAGE_RAM) );
	ESP_ERROR_CHECK( esp_wifi_set_mode(WIFI_MODE_NULL) );
	ESP_ERROR_CHECK( esp_wifi_start() );
	sniffer_set_channel(chnl);
	wifi_promiscuous_filter_t filter = {.filter_mask = WIFI_EVENT_MASK_AP_PROBEREQRECVED};
	ESP_ERROR_CHECK(esp_wifi_set_promiscuous_filter(&filter));
	esp_wifi_set_promiscuous(true);
	esp_wifi_set_promiscuous_rx_cb(&sniffer_packet_handler);
}

void sniffer_stop(void)
{
	esp_wifi_set_promiscuous(false);
}

void sniffer_set_channel(uint8_t channel)
{
	esp_wifi_set_channel(channel, WIFI_SECOND_CHAN_NONE);
}

void sniffer_packet_handler(void* buff, wifi_promiscuous_pkt_type_t type)
{
	/* http://blog.podkalicki.com/esp32-wifi-sniffer/	-> structure here!
	 * il PACCHETTO completo si compone di [RX_CONTROL e PAYLOAD]
	 * il PAYLOAD a sua volta si compone di un header(wifi_ieee80211_packet_t) e di un altro livello di payload (wifi_ieee80211_packet_t)*/

	const wifi_promiscuous_pkt_t *ppkt = (wifi_promiscuous_pkt_t *)buff;				//wifi_promiscuous_pkt_t-> PACCHETTO
	const wifi_ieee80211_packet_t *ipkt = (wifi_ieee80211_packet_t *)ppkt->payload;     //wifi_ieee80211_packet_t-> PACCHETTO->PAYLOAD->payload
	const wifi_ieee80211_mac_hdr_t *hdr = &ipkt->hdr;									//wifi_ieee80211_packet_t-> PACCHETTO->PAYLOAD->header

	//sha function for encoding the hash
	if((hdr->frame_ctrl & PROBE_MASK) != 0)
	{
		fprintf(fSniffs,	"\rPACKET HDR=%x, RSSI=%02d,"
							" DEST=%02x:%02x:%02x:%02x:%02x:%02x,"
							" SRC =%02x:%02x:%02x:%02x:%02x:%02x,"
							" SSID=%02x:%02x:%02x:%02x:%02x:%02x,"
							" TIME= %d \n",
							hdr->frame_ctrl,
							ppkt->rx_ctrl.rssi,
							// DEST
							hdr->addr1[0],hdr->addr1[1],hdr->addr1[2],
							hdr->addr1[3],hdr->addr1[4],hdr->addr1[5],
							// SRC
							hdr->addr2[0],hdr->addr2[1],hdr->addr2[2],
							hdr->addr2[3],hdr->addr2[4],hdr->addr2[5],
							// SSID
							hdr->addr3[0],hdr->addr3[1],hdr->addr3[2],
							hdr->addr3[3],hdr->addr3[4],hdr->addr3[5],
							ppkt->rx_ctrl.timestamp);
	}
}

void blink_task(void *pvParameter)
{
    /* Configure the IOMUX register for pad BLINK_GPIO (some pads are
       muxed to GPIO on reset already, but some default to other
       functions and need to be switched to GPIO. Consult the
       Technical Reference for a list of pads and their default
       functions.)
    */
    gpio_pad_select_gpio(BLINK_GPIO);
    /* Set the GPIO as a push/pull output */
    gpio_set_direction(BLINK_GPIO, GPIO_MODE_OUTPUT);
    while(1) {
        /* Blink off (output low) */
        gpio_set_level(BLINK_GPIO, 0);
        vTaskDelay(BLINK_TIME_OFF);
        /* Blink on (output high) */
        gpio_set_level(BLINK_GPIO, 1);
        vTaskDelay(BLINK_TIME_ON);
    }
}
