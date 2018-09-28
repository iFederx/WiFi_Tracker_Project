deps_config := \
	/home/cekke/esp/esp-idf/components/app_trace/Kconfig \
	/home/cekke/esp/esp-idf/components/aws_iot/Kconfig \
	/home/cekke/esp/esp-idf/components/bt/Kconfig \
	/home/cekke/esp/esp-idf/components/driver/Kconfig \
	/home/cekke/esp/esp-idf/components/esp32/Kconfig \
	/home/cekke/esp/esp-idf/components/esp_adc_cal/Kconfig \
	/home/cekke/esp/esp-idf/components/esp_http_client/Kconfig \
	/home/cekke/esp/esp-idf/components/ethernet/Kconfig \
	/home/cekke/esp/esp-idf/components/fatfs/Kconfig \
	/home/cekke/esp/esp-idf/components/freertos/Kconfig \
	/home/cekke/esp/esp-idf/components/heap/Kconfig \
	/home/cekke/esp/esp-idf/components/http_server/Kconfig \
	/home/cekke/esp/esp-idf/components/libsodium/Kconfig \
	/home/cekke/esp/esp-idf/components/log/Kconfig \
	/home/cekke/esp/esp-idf/components/lwip/Kconfig \
	/home/cekke/esp/esp-idf/components/mbedtls/Kconfig \
	/home/cekke/esp/esp-idf/components/mdns/Kconfig \
	/home/cekke/esp/esp-idf/components/mqtt/Kconfig \
	/home/cekke/esp/esp-idf/components/openssl/Kconfig \
	/home/cekke/esp/esp-idf/components/pthread/Kconfig \
	/home/cekke/esp/esp-idf/components/spi_flash/Kconfig \
	/home/cekke/esp/my_trial/client_boardTest_1.0/components/spiffs/Kconfig \
	/home/cekke/esp/esp-idf/components/tcpip_adapter/Kconfig \
	/home/cekke/esp/esp-idf/components/vfs/Kconfig \
	/home/cekke/esp/esp-idf/components/wear_levelling/Kconfig \
	/home/cekke/esp/esp-idf/components/bootloader/Kconfig.projbuild \
	/home/cekke/esp/esp-idf/components/esptool_py/Kconfig.projbuild \
	/home/cekke/esp/my_trial/client_boardTest_1.0/main/Kconfig.projbuild \
	/home/cekke/esp/esp-idf/components/partition_table/Kconfig.projbuild \
	/home/cekke/esp/esp-idf/Kconfig

include/config/auto.conf: \
	$(deps_config)

ifneq "$(IDF_CMAKE)" "n"
include/config/auto.conf: FORCE
endif

$(deps_config): ;
