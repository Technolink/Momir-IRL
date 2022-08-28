#include "Adafruit_Thermal.h"
#include "SoftwareSerial.h"

#define LED_PIN 13
#define PRINTER_TX_PIN  3  // Arduino transmit  YELLOW WIRE   labeled RX  on printer
#define PRINTER_RX_PIN  2  // Arduino receive   GREEN  WIRE   labeled TX  on printer
// https://learn.adafruit.com/mini-thermal-receipt-printer/hacking
#define PRINTER_DTR_PIN 4  // Arduino DTR       ORANGE WIRE   labeled DTR on printer

#define BLUETHT_TX_PIN  1  // Arduino transmit  BLUE   WIRE   labeled RX  on bluetooth
#define BLUETHT_RX_PIN  0  // Arduino receive   PURPLE WIRE   labeled TX  on bluetooth
#define monochrome_width  384
#define monochrome_height 544
#define buffer_height 32

//user hardware serial for BT
//SoftwareSerial BluetoothSerial(BLUETHT_RX_PIN, BLUETHT_TX_PIN); // Declare SoftwareSerial obj first
SoftwareSerial PrinterSerial(PRINTER_RX_PIN, PRINTER_TX_PIN); // Declare SoftwareSerial obj first
Adafruit_Thermal Printer(&PrinterSerial, PRINTER_DTR_PIN);     // Pass addr to printer constructor

byte imageBuffer[monochrome_width/8 * buffer_height];
int bufferIndex = 0;
int bufferHeight = 0;
int imageIndex = 0;
const int duration = 2000;

void setup() {
  PrinterSerial.begin(19200);
  Printer.begin();
  pinMode(LED_PIN, OUTPUT);

  Printer.setHeatConfig(4, 255, 255);
  //Printer.setTimes(30000, 2100);

  Serial.begin(9600); // bluetooth serial

  for (int i=0; i<monochrome_width/8 * buffer_height; i++)
    imageBuffer[i] = 0;
}

void loop() {
  byte d, len;

  while (Serial.available()) {
    d = Serial.read();
    if (d != 0 && d != 255) {
      imageBuffer[bufferIndex] = d;
      bufferIndex += 1;
      imageIndex += 1;
    } else {
      while (!Serial.available()) { }
      len = Serial.read();
      for (int i=0; i<len; i++) {
        imageBuffer[bufferIndex] = d;
        bufferIndex += 1;
      }
      imageIndex += len;
    }

    if (bufferIndex == monochrome_width/8 * buffer_height) {
      // print
      bufferIndex = 0;
      Printer.printBitmap(monochrome_width, buffer_height, imageBuffer, false);
/*
      digitalWrite(LED_PIN, HIGH);   // turn the LED on (HIGH is the voltage level)
      delay(duration);               // wait for a second
      digitalWrite(LED_PIN, LOW);    // turn the LED off by making the voltage LOW
      delay(duration);               // wait for a second
*/
      Serial.write(5); // let the program know we're ready

      if (imageIndex == monochrome_width/8 * monochrome_height) {
        imageIndex = 0;
        Printer.feed(3);
        Serial.write(6); // let the program know we're done
      }
    }
  }
}
