/*------------------------------------------------------------------------
  Example sketch for Adafruit Thermal Printer library for Arduino.
  Demonstrates a few text styles & layouts, bitmap printing, etc.

  IMPORTANT: DECLARATIONS DIFFER FROM PRIOR VERSIONS OF THIS LIBRARY.
  This is to support newer & more board types, especially ones that don't
  support SoftwareSerial (e.g. Arduino Due).  You can pass any Stream
  (e.g. Serial1) to the printer constructor.  See notes below.

  You may need to edit the PRINTER_FIRMWARE value in Adafruit_Thermal.h
  to match your printer (hold feed button on powerup for test page).
  ------------------------------------------------------------------------*/

#include "Adafruit_Thermal.h"
//#include "adalogo.h"
#include "monochrome.h"
//#include "scarabgod.h"

// Here's the new syntax when using SoftwareSerial (e.g. Arduino Uno) ----
// If using hardware serial instead, comment out or remove these lines:

#include "SoftwareSerial.h"
#define TX_PIN  6  // Arduino transmit  YELLOW WIRE   labeled RX  on printer
#define RX_PIN  5  // Arduino receive   GREEN  WIRE   labeled TX  on printer
// https://learn.adafruit.com/mini-thermal-receipt-printer/hacking
#define DTR_PIN 4 // Arduino DTR       ORANGE WIRE   labeled DTR on printer

SoftwareSerial mySerial(RX_PIN, TX_PIN); // Declare SoftwareSerial obj first
Adafruit_Thermal printer(&mySerial, DTR_PIN);     // Pass addr to printer constructor

void setup() {
  mySerial.begin(19200);  // Initialize SoftwareSerial
  printer.begin();        // Init printer (same regardless of serial type)

  printer.setHeatConfig(4, 255, 255);
  //printer.setTimes(30000, 2100);
  
  printer.printBitmap(monochrome_width, monochrome_height, monochrome_data);
  printer.feed(4);
 
  //printer.setDefault(); // Restore printer to defaults
}

void loop() {
}
