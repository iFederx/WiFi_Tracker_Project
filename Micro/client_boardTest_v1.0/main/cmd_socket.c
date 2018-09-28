#include "cmd_socket.h"

int start_socket_connection()
{
	uint16_t	   tport_n, tport_h;	/* server port number (net/host order) */
	int 		   skt;
	int		       result;				/* variable to check the build status */
	struct sockaddr_in	saddr;			/* server address structure */
	struct in_addr	sIPaddr; 			/* server IP address. structure */

	result = inet_aton(DEFAULT_SERVER_IP, &sIPaddr);
	if (!result)
	{
		ESP_LOGE(TAGS,"(!!)Error: Invalid address.\n");
		return -1;
	}
	if (sscanf(DEFAULT_SERVER_PORT, "%" SCNu16, &tport_h)!=1)
	{
		ESP_LOGE(TAGS,"(!!)Error: Invalid port number.\n");
		return -1;
	}
	tport_n = htons(tport_h);

	ESP_LOGI(TAGS,"(*)Creating socket");
	skt = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
	if(skt<0)
	{
		ESP_LOGE(TAGS,"\n(!!)Error: Impossible to open socket.\n");
		return -1;
	}ESP_LOGI(TAGS,"-> Done!\n");

	bzero(&saddr, sizeof(saddr));
		saddr.sin_family = AF_INET;
		saddr.sin_port = tport_n;
		saddr.sin_addr = sIPaddr;

	if(connect(skt, (struct sockaddr *) &saddr, sizeof(saddr))!=0)
	{
		ESP_LOGE(TAGS,"(!!)Error: Impossible to connect with the server.\n");
		return -1;
	}else{
		char *p = inet_ntoa(saddr.sin_addr);
		ESP_LOGI(TAGS,"\rConnected to target address %s:",p);
		ESP_LOGI(TAGS,"%" SCNu16, ntohs(saddr.sin_port));
		ESP_LOGI(TAGS,"\n");
	}

	return skt;
}

int client_sync(int skt){
	/* sending the get request to the server */
	strcpy(buf, "TIME= 5012792 \n");
	size_t	msg_len;
	msg_len = strlen(buf);
	if(write(skt,buf,msg_len) != msg_len){
	    ESP_LOGE(TAGS,"Error: Fail in sending data! \n");
	    return -1;
	}
	ESP_LOGI(TAGS,"(*) Timestamp sent successfully! \n");
	return 0;
}

int send_sniffed_packages(int skt, char *path){
	ESP_LOGI(TAGS,"(->)Sending the file to Server through TCP socket.\n\n");
	FILE *f = fopen(path, "r");
	if (f == NULL) {
		return -1;
	}
	// need malloc memory for line, if not, segmentation fault error will occurred.
	size_t	msg_len;
	while((msg_len = fread(buf, 1, BUFLEN, f)) != NULL) {
		//ESP_LOGI(TAGS,"%s\n", buf);
		if(write(skt,buf,msg_len) != msg_len){
			ESP_LOGE(TAGS,"Error: Fail in sending data! \n");
			return -1;
		}
	}
	ESP_LOGI(TAGS,"END-------------.\n");
	return 0;
}
