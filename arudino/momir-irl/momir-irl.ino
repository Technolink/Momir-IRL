#include "Adafruit_Thermal.h"
#include "SoftwareSerial.h"

#define LED_PIN         13 // debug led         GREEN  WIRE TO BOARD
#define PRINTER_TX_PIN  6  // Arduino transmit  YELLOW WIRE   labeled RX  on printer
#define PRINTER_RX_PIN  5  // Arduino receive   GREEN  WIRE   labeled TX  on printer
// https://learn.adafruit.com/mini-thermal-receipt-printer/hacking
#define PRINTER_DTR_PIN 4  // Arduino DTR       ORANGE WIRE   labeled DTR on printer

#define BLUETHT_TX_PIN  3  // Arduino transmit  BLUE   WIRE   labeled RX  on bluetooth
#define BLUETHT_RX_PIN  2  // Arduino receive   PURPLE WIRE   labeled TX  on bluetooth
#define monochrome_width  384
#define monochrome_height 544

SoftwareSerial BluetoothSerial(BLUETHT_RX_PIN, BLUETHT_TX_PIN); // Declare SoftwareSerial obj first
SoftwareSerial PrinterSerial(PRINTER_RX_PIN, PRINTER_TX_PIN); // Declare SoftwareSerial obj first
Adafruit_Thermal Printer(&PrinterSerial, PRINTER_DTR_PIN);     // Pass addr to printer constructor

byte bufferMaxHeight = 4;
byte rowBuffer[1536]; // monochrome_width * 4
int bufferIndex = 0;
int bufferHeight = 0;

void setup() {
  PrinterSerial.begin(19200);
  Printer.begin();

  Printer.setHeatConfig(8, 255, 255);
  //Printer.setTimes(30000, 2100);
  
  //Printer.printBitmap(monochrome_width, monochrome_height, monochrome_data);
  //Printer.feed(4);
  //Printer.setDefault(); // Restore printer to defaults

  BluetoothSerial.begin(9600); 
  Serial.begin(9600);

  for (int i=0; i<monochrome_width; i++)
    rowBuffer[i] = 0;
}

void loop() {
  byte i;
  while (BluetoothSerial.available()) {
    i = BluetoothSerial.read();
    rowBuffer[bufferIndex] = i;
    bufferIndex += 1;

    if (bufferIndex == monochrome_width/8 * bufferMaxHeight) {
      // print row
      bufferIndex = 0;

      bufferHeight += bufferMaxHeight;
      Printer.printBitmap(monochrome_width, bufferMaxHeight, rowBuffer, false);
      BluetoothSerial.write(5); // let the program know we're ready
      
      if (bufferHeight >= monochrome_height) {
        Printer.feed(3);
        bufferHeight = 0;
      }
    }
  }
}
