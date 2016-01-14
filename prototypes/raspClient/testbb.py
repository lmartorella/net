import pigpio

RXD = 18

def handler(signum,frame):
	print('You pressed CTRL+C! Exiting...')
	pi.bb_serial_read_close(RXD)
	pi.stop()
	sys.exit(0)

try:
	pi = pigpio.pi()
except:
	pi.stop()
	pi = pigpio.pi()
	
print("pigpio connected!")

pi.set_mode(RXD, pigpio.INPUT)

try:
	pi.bb_serial_read_open(RXD, 19200, 8)
except:
	pi.bb_serial_read_close(RXD)
	pi.bb_serial_read_open(RXD, 19200, 8)

while True:
	(count, resp) = pi.bb_serial_read(RXD)
	if count > 0:
		print("Resp = " + str(resp))

