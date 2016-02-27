import pigpio
import RPi.GPIO as GPIO
from datetime import datetime
import sys

# 20 is DE, #21 is /RE
DE = 20
nRE = 21
RXD = 18

# disengage RS485 line
GPIO.setwarnings(True)
GPIO.setmode(GPIO.BCM)
GPIO.setup(DE, GPIO.OUT)
GPIO.setup(nRE, GPIO.OUT)
GPIO.output(DE, 0)
GPIO.output(nRE, 0)


# Ctrl+C handler
def handler(signum,frame):
	print('You pressed CTRL+C! Exiting...')
	pi.bb_serial_read_close(RXD)
	pi.stop()
	sys.exit(0)

# Connect pigpio
try:
	pi = pigpio.pi()
except:
	pi.stop()
	pi = pigpio.pi()
print("pigpio connected!")

# rx as input
pi.set_mode(RXD, pigpio.INPUT)

# open bit-bang port
try:
	pi.bb_serial_read_open(RXD, 19200, 9)
except:
	pi.bb_serial_read_close(RXD)
	pi.bb_serial_read_open(RXD, 19200, 9)

# count should be even (9bit -> 2 bytes per char)
def processLine(arr, count, start):
	print()
	lineAddress = arr[start + 1]
	print(datetime.now().strftime('%H:%M:%S.%f')," ",lineAddress,":",sep="",end="  ")
	for i in range(int(count / 2)):
		if arr[start + i * 2 + 1] != lineAddress:
			processLine(arr, count - i * 2, i * 2)
			return
		val = arr[start + i * 2]
		print(format(val, "02x"), sep="", end=" ")

	for i in range(40-int(count/2*3)):
		print(" ", end="")
	for i in range(int(count / 2)):
		val = arr[start + i * 2]
		if val < 32 or val > 127:
			val = "."
		else:
			val = chr(val)
		print(val, end="")

# read data
while True:
	(count, resp) = pi.bb_serial_read(RXD)
	if count > 0:
		processLine(resp, count, 0)
		sys.stdout.flush()


