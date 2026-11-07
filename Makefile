SHELL = /bin/bash

GAME_MOD_PATH = $(HOME)/.config/unity3d/Freehold Games/CavesOfQud/Mods
GAME_BIN = $(HOME)/.var/app/com.valvesoftware.Steam/data/Steam/steamapps/common/Caves of Qud/CoQ.x86_64
MOD_DIR_NAME = CavesOfQudArchipelagoRandomizer

.ONESHELL:
.PHONY: collect
collect:
	rm -rf "bin/$(MOD_DIR_NAME)"
	mkdir -p "bin/$(MOD_DIR_NAME)"
	cp -r --parents manifest.json LICENSE.txt README.md src thirdparty Archipelago/worlds/cavesofqud/data Archipelago/worlds/cavesofqud/archipelago.json bin/$(MOD_DIR_NAME)

.ONESHELL:
.PHONY: install
install: collect
	rm -rf "$(GAME_MOD_PATH)/$(MOD_DIR_NAME)"
	cp -r "bin/$(MOD_DIR_NAME)" "$(GAME_MOD_PATH)"

.ONESHELL:
.PHONY: run
run: install
	"$(GAME_BIN)" -logFile -


.ONESHELL:
.PHONY: template
template:
	cd Archipelago
	python Launcher.py "Generate Template Options"

.ONESHELL:
.PHONY: package
package: clean collect
	cd bin
	zip -r "$(MOD_DIR_NAME).zip" "$(MOD_DIR_NAME)"
	cp -r ../Archipelago/worlds/cavesofqud .
	zip -r "cavesofqud.apworld" cavesofqud

.ONESHELL:
.PHONY: clean
clean:
	rm -rf "bin/$(MOD_DIR_NAME)"
	rm -rf "bin/$(MOD_DIR_NAME).zip"
	rm -rf bin/cavesofqud
	rm -rf bin/cavesofqud.apworld
