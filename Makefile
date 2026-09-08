# Atajos del proyecto. Ejecutar `make` a secas para ver los comandos.

REPORT_DIR := report
OUT        := .build
PDF        := $(REPORT_DIR)/$(OUT)/main.pdf

# texlive-publishers es el que trae IEEEtran.bst; sin el, bibtex falla y
# todas las citas salen sin resolver.
TEX_PKGS := latexmk \
            texlive-latex-recommended \
            texlive-fonts-recommended \
            texlive-latex-extra \
            texlive-pictures \
            texlive-publishers \
            poppler-utils

.DEFAULT_GOAL := help
.PHONY: help setup pdf watch clean

help:
	@echo "make setup  - instala TeX Live (una sola vez, ~2 min)"
	@echo "make pdf    - compila el informe -> $(PDF)"
	@echo "make watch  - recompila solo cada vez que guardas"
	@echo "make clean  - borra los auxiliares de compilacion"

setup:
	sudo apt-get update -qq
	sudo DEBIAN_FRONTEND=noninteractive apt-get install -y -qq --no-install-recommends $(TEX_PKGS)
	@echo ""
	@echo "Listo: $$(pdflatex --version | head -1)"
	@echo "Ahora: make pdf"

pdf:
	cd $(REPORT_DIR) && latexmk -pdf -interaction=nonstopmode -outdir=$(OUT) main.tex
	@echo ""
	@command -v pdfinfo >/dev/null && echo "PDF -> $(PDF) ($$(pdfinfo $(PDF) | awk '/^Pages/{print $$2}') paginas)" || echo "PDF -> $(PDF)"

watch:
	cd $(REPORT_DIR) && latexmk -pdf -pvc -interaction=nonstopmode -outdir=$(OUT) main.tex

clean:
	rm -rf $(REPORT_DIR)/$(OUT)
	@echo "auxiliares borrados"
