"""Coastal/valley sectors. Actual terrain and paths are compiled from continuous.json."""
from tiles import Map

def westcoast():return Map('goryeo_westcoast','서해안 둔덕',80,64,seed=301)
def tidalcoast():return Map('goryeo_tidalcoast','갯벌 해안길',80,64,seed=302)
def rivervalley():return Map('goryeo_rivervalley','예성 물굽이',80,64,seed=303)
def southcoast():return Map('goryeo_southcoast','남쪽 해안 구릉',80,64,seed=304)
