import RPi.GPIO as GPIO

GPIO.setwarnings(True)
GPIO.setmode(GPIO.BCM)
GPIO.setup(20, GPIO.OUT)
GPIO.setup(21, GPIO.OUT)

# 20 is DE, #21 is /RE
GPIO.output(20, 0)
GPIO.output(21, 0)



