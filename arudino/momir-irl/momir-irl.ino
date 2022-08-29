#include "Adafruit_Thermal.h"
#include "SoftwareSerial.h"

#define PRINTER_TX_PIN  3  // Arduino transmit  YELLOW WIRE   labeled RX  on printer
#define PRINTER_RX_PIN  2  // Arduino receive   GREEN  WIRE   labeled TX  on printer
// https://learn.adafruit.com/mini-thermal-receipt-printer/hacking
#define PRINTER_DTR_PIN 4  // Arduino DTR       ORANGE WIRE   labeled DTR on printer

#define BLUETHT_TX_PIN  1  // Bluetht transmit  BLUE   WIRE   labeled RX  on bluetooth
#define BLUETHT_RX_PIN  0  // Bluetht receive   PURPLE WIRE   labeled TX  on bluetooth
#define monochrome_width  384
#define monochrome_height 544
#define buffer_height 34

//user hardware serial to max out BT speed
//SoftwareSerial BluetoothSerial(BLUETHT_RX_PIN, BLUETHT_TX_PIN); // Declare SoftwareSerial obj first
SoftwareSerial PrinterSerial(PRINTER_RX_PIN, PRINTER_TX_PIN); // Declare SoftwareSerial obj first
Adafruit_Thermal Printer(&PrinterSerial, PRINTER_DTR_PIN);     // Pass addr to printer constructor

byte imageBuffer[monochrome_width/8 * buffer_height];
int bufferIndex = 0;
int imageIndex = 0;
const int duration = 2000;

void setup() {
  PrinterSerial.begin(19200);
  Printer.begin();

  Printer.setHeatConfig(4, 255, 255);
  //Printer.setTimes(30000, 2100);

  Serial.begin(115200); // bluetooth serial fast mode using hardware
  
  for (int i=0; i<monochrome_width/8 * buffer_height; i++)
    imageBuffer[i] = 0;
}

void loop() {
  byte d, len;

  while (Serial.available()) {
    d = Serial.read();
    if (d != 0 && d != 255) {
      imageBuffer[bufferIndex++] = d;
      imageIndex++;
    } else {
      while (!Serial.available()) { }
      len = Serial.read();
      for (int i=0; i<len; i++) {
        imageBuffer[bufferIndex++] = d;
      }
      imageIndex += len;
    }

    if (bufferIndex >= monochrome_width/8 * buffer_height) {
      // print
      Printer.printBitmap(monochrome_width, buffer_height, imageBuffer, false);
      bufferIndex = 0;

      if (imageIndex >= monochrome_width/8 * monochrome_height) {
        imageIndex = 0;
        Printer.feed(3);
        Serial.write(6); // let the program know we're done
      } else {
        Serial.write(5); // let the program know we're ready for more chunks
      }
    }
  }
}
