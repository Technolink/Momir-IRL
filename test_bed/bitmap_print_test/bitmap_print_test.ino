#include "Adafruit_Thermal.h"
//#include "adalogo.h"
//#include "monochrome.h"
#include "scarabgod.h"
//#include "aetherchaser.h"

#include "SoftwareSerial.h"
#define LED_PIN         13 // debug led         GREEN  WIRE TO BOARD
#define PRINTER_TX_PIN  3  // Arduino transmit  YELLOW WIRE   labeled RX  on printer
#define PRINTER_RX_PIN  2  // Arduino receive   GREEN  WIRE   labeled TX  on printer
// https://learn.adafruit.com/mini-thermal-receipt-printer/hacking
#define PRINTER_DTR_PIN 4  // Arduino DTR       ORANGE WIRE   labeled DTR on printer

#define BLUETHT_TX_PIN  1  // Arduino transmit  BLUE   WIRE   labeled RX  on bluetooth
#define BLUETHT_RX_PIN  0  // Arduino receive   PURPLE WIRE   labeled TX  on bluetooth

SoftwareSerial PrinterSerial(PRINTER_RX_PIN, PRINTER_TX_PIN); // Declare SoftwareSerial obj first
Adafruit_Thermal Printer(&PrinterSerial, PRINTER_DTR_PIN);     // Pass addr to printer constructor

void setup() {
  PrinterSerial.begin(19200);
  Printer.begin();

  Printer.setHeatConfig(4, 255, 255);
  //printer.setTimes(30000, 2100);
  
  Printer.printBitmap(monochrome_width, monochrome_height, monochrome_data);
  Printer.feed(4);
  //printer.setDefault(); // Restore printer to defaults
}

void loop() {

}
