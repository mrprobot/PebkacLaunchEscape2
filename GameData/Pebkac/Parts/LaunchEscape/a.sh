#!/bin/bash
for i in *.cfg; do
	echo $i
	sed -i 's/MP_Nazari/Pebkac\/MP_Nazari/g' $i
done
