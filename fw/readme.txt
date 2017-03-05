Debug bus protocol chars:

l: CLOS command
s: SELE command
c: CHIL command
k: SINK command
g: GUID command
r: READ command
w: WRIT command

=== CLIENT

K: init
^: address assign
": heartbeat
?: ready for hello
=: socket connect
!: unknown command (error)
-: not manager, bus reinit
>: bus socket over sent (client)
@: break received

