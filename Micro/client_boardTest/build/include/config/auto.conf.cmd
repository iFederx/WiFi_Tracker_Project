deps_config := \
	/Users/federicobrignone/esp/esp-idf/components/app_trace/Kconfig \
	/Users/federicobrignone/esp/esp-idf/components/aws_iot/Kconfig \
	/Users/federicobrignone/esp/esp-idf/components/bt/Kconfig \
	/Users/federicobrignone/esp/esp-idf/components/driver/Kconfig \
	/Users/federicobrignone/esp/esp-idf/components/esp32/Kconfig \
	/Users/federicobrignone/esp/esp-idf/components/esp_adc_cal/Kconfig \
	/Users/federicobrignone/esp/esp-idf/components/ethernet/Kconfig \
	/Users/federicobrignone/esp/esp-idf/components/fatfs/Kconfig \
	/Users/federicobrignone/esp/esp-idf/components/freertos/Kconfig \
	/Users/federicobrignone/esp/esp-idf/components/heap/Kconfig \
	/Users/federicobrignone/esp/esp-idf/components/libsodium/Kconfig \
	/Users/federicobrignone/esp/esp-idf/components/log/Kconfig \
	/Users/federicobrignone/esp/esp-idf/components/lwip/Kconfig \
	/Users/federicobrignone/esp/esp-idf/components/mbedtls/Kconfig \
	/Users/federicobrignone/esp/esp-idf/components/openssl/Kconfig \
	/Users/federicobrignone/esp/esp-idf/components/pthread/Kconfig \
	/Users/federicobrignone/esp/esp-idf/components/spi_flash/Kconfig \
	/Users/federicobrignone/esp/esp-idf/components/spiffs/Kconfig \
	/Users/federicobrignone/esp/esp-idf/components/tcpip_adapter/Kconfig \
	/Users/federicobrignone/esp/esp-idf/components/wear_levelling/Kconfig \
	/Users/federicobrignone/esp/esp-idf/components/bootloader/Kconfig.projbuild \
	/Users/federicobrignone/esp/esp-idf/components/esptool_py/Kconfig.projbuild \
	/Users/federicobrignone/Documents/GitHub/WiFi_Tracker_Project/Micro/client_boardTest/main/Kconfig.projbuild \
	/Users/federicobrignone/esp/esp-idf/components/partition_table/Kconfig.projbuild \
	/Users/federicobrignone/esp/esp-idf/Kconfig

include/config/auto.conf: \
	$(deps_config)


$(deps_config): ;
